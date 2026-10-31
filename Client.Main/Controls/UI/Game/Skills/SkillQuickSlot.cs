#nullable enable

using System;
using System.Linq;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Skill activo del HUD.
    ///
    /// Click / tap corto:
    /// abre la barra clásica de skills.
    ///
    /// Mantener pulsado:
    /// abre el panel completo de administración.
    /// </summary>
    public class SkillQuickSlot : UIControl
    {
        private readonly GameControl? _hudAnchor;

        private readonly CharacterState
            _characterState;

        private SkillSelectionPanel?
            _selectionPanel;

        private readonly SkillSlotControl
            _currentSkillSlot;

        private readonly ClassicSkillBarControl
            _classicSkillBar;

        private const int SLOT_SIZE =
            52;

        // 550 ms evita que un click normal
        // se confunda fácilmente con long press.
        private const double LONG_PRESS_SECONDS =
            0.55;

        private bool _trackingPress;
        private bool _longPressTriggered;
        private bool _pressCancelled;

        private double
            _pressDurationSeconds;

        public SkillEntryState?
            SelectedSkill
        {
            get;
            private set;
        }
    
        /// <summary>
        /// Skill bajo el mouse dentro de la barra clásica.
        /// Se usa para CTRL + 1...0.
        /// </summary>
        public SkillEntryState?
            HoveredClassicSkill =>
                _classicSkillBar.Visible
                    ? _classicSkillBar.HoveredSkill
                    : null;

        public SkillQuickSlot(
            CharacterState characterState, GameControl? hudAnchor = null)
        {
            _hudAnchor = hudAnchor;
            _characterState =
                characterState;

            // Es importante dejarlo en false:
            // la barra clásica tendrá hijos fuera
            // del rectángulo del skill principal.
            AutoViewSize = false;

            Align = hudAnchor == null
                ? ControlAlign.HorizontalCenter | ControlAlign.Bottom
                : ControlAlign.None;
            Margin = hudAnchor == null ? new Margin { Bottom = 15 } : Margin.Empty;
            ViewSize = new Point(42, SLOT_SIZE);
            ControlSize = ViewSize;

            // El contenedor ya no será el sensor del click.
            // El propio icono central manejará mouse/touch.
            Interactive = false;

            // =========================================================
            // CURRENT SKILL
            // =========================================================

            float slotScale =
                SLOT_SIZE /
                (float)
                SkillSlotControl.SLOT_HEIGHT;

            int scaledWidth =
                (int)(
                    SkillSlotControl.SLOT_WIDTH *
                    slotScale);

            int scaledHeight =
                (int)(
                    SkillSlotControl.SLOT_HEIGHT *
                    slotScale);

            _currentSkillSlot =
                new SkillSlotControl
                {
                    IsSelected =
                        false,

                    Skill =
                        null,

                    ShowFrame = hudAnchor == null,

                    VisualScale =
                        slotScale
                };

            _currentSkillSlot.X = (ViewSize.X - scaledWidth) / 2;
            _currentSkillSlot.Y = (SLOT_SIZE - scaledHeight) / 2;

            Controls.Add(
                _currentSkillSlot);
            
            // El sensor debe ser exactamente el mismo rectángulo
            // que ocupa visualmente el skill central.
            _currentSkillSlot.Click +=
                (_, _) =>
                {
                    HandleCurrentSkillClick();
                };

            // =========================================================
            // CLASSIC SKILL BAR
            // =========================================================

            _classicSkillBar =
                new ClassicSkillBarControl(
                    _characterState);

            // MU clásico deja unos pocos pixels
            // entre el skill actual y la lista.
            const int CLASSIC_BAR_GAP =
                3;

            // El primer cuadro de la lista queda
            // centrado horizontalmente sobre
            // el skill activo.
            _classicSkillBar.X =
                _currentSkillSlot.X
                +
                (
                    scaledWidth -
                    SkillSlotControl.SLOT_WIDTH
                ) / 2;

            _classicSkillBar.Y =
                _currentSkillSlot.Y
                -
                SkillSlotControl.SLOT_HEIGHT
                -
                CLASSIC_BAR_GAP;

            _classicSkillBar.SkillSelected +=
                OnClassicSkillSelected;

            Controls.Add(
                _classicSkillBar);

            // =========================================================
            // VISUAL
            // =========================================================

            BackgroundColor =
                Color.Transparent;

            BorderColor =
                Color.Transparent;

            BorderThickness =
                0;

            // =========================================================
            // DEFAULT SKILL
            // =========================================================

            SkillEntryState?
                defaultSkill =
                    _characterState
                        .GetSkills()
                        .FirstOrDefault();

            if (defaultSkill != null)
            {
                ApplySelectedSkill(
                    defaultSkill);
            }
        }

        // =============================================================
        // MANAGEMENT PANEL
        // =============================================================

        /// <summary>
        /// Conecta el menú grande de skills.
        /// </summary>
        public void SetSelectionPanel(
            SkillSelectionPanel panel)
        {
            if (_selectionPanel != null)
            {
                _selectionPanel.SkillSelected -=
                    ApplySelectedSkill;
            }

            _selectionPanel =
                panel;

            _selectionPanel.SkillSelected +=
                ApplySelectedSkill;

            if (SelectedSkill != null)
            {
                _selectionPanel.HighlightSkill(
                    SelectedSkill.SkillId);
            }
        }

        // =============================================================
        // SELECT SKILL
        // =============================================================

        private void ApplySelectedSkill(
            SkillEntryState skill)
        {
            if (skill == null)
            {
                return;
            }

            SelectedSkill =
                skill;

            _currentSkillSlot.Skill =
                skill;

            _currentSkillSlot.IsSelected =
                false;

            _classicSkillBar.SetSelectedSkill(
                skill);

            _selectionPanel?
                .HighlightSkill(
                    skill.SkillId);
        }

        private void OnClassicSkillSelected(
            SkillEntryState skill)
        {
            ApplySelectedSkill(
                skill);
        }

        /// <summary>
        /// Usado también por los hotkeys 1-0
        /// y por controles móviles.
        /// </summary>
        public void SelectSkill(
            SkillEntryState skill)
        {
            if (skill == null)
            {
                return;
            }

            ApplySelectedSkill(
                skill);
        }

        // =============================================================
        // SHORT CLICK
        // =============================================================

        private void HandleCurrentSkillClick()
        {
            // Si este release corresponde al long press,
            // no ejecutar además el click corto.
            if (_longPressTriggered ||
                _pressCancelled)
            {
                return;
            }

            // Nunca mantener simultáneamente
            // barra clásica + menú grande.
            if (_selectionPanel?.Visible == true)
            {
                _selectionPanel.Close();
            }

            _classicSkillBar.Toggle();
        }

        // =============================================================
        // LONG PRESS
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            UpdateHudPlacement();

            // Native Main clears the pet-command selection when the Raven
            // is unequipped. Don't leave an unusable A7 mode as active skill.
            if (SelectedSkill != null &&
                DarkRavenCommands.IsCommandSkill(SelectedSkill.SkillId) &&
                !_characterState.HasEquippedDarkRaven)
            {
                SkillEntryState? replacement = _characterState.GetSkills().FirstOrDefault();
                if (replacement != null)
                {
                    ApplySelectedSkill(replacement);
                }
                else
                {
                    SelectedSkill = null;
                    _currentSkillSlot.Skill = null;
                    _classicSkillBar.SetSelectedSkill(null);
                }
            }

            base.Update(
                gameTime);

            MouseState mouse =
                CurrentMouseState;

            MouseState previousMouse =
                PreviousMouseState;

            // =========================================================
            // SENSOR REAL DEL SKILL CENTRAL
            // =========================================================

            bool pointerOverCurrentSkill =
                _currentSkillSlot
                    .DisplayRectangle
                    .Contains(
                        mouse.Position);

            // =========================================================
            // NUEVO PRESS
            // =========================================================

            if (!_trackingPress &&
                mouse.LeftButton ==
                    ButtonState.Pressed &&
                previousMouse.LeftButton ==
                    ButtonState.Released &&
                pointerOverCurrentSkill)
            {
                _trackingPress =
                    true;

                _longPressTriggered =
                    false;

                _pressCancelled =
                    false;

                _pressDurationSeconds =
                    0.0;
            }

            // =========================================================
            // PRESS ACTIVO
            // =========================================================

            if (_trackingPress &&
                mouse.LeftButton ==
                    ButtonState.Pressed)
            {
                // Si el dedo/mouse abandona el icono,
                // cancelamos el long press.
                if (!pointerOverCurrentSkill)
                {
                    _trackingPress =
                        false;

                    _pressCancelled =
                        true;

                    _pressDurationSeconds =
                        0.0;

                    return;
                }

                if (!_longPressTriggered)
                {
                    _pressDurationSeconds +=
                        gameTime
                            .ElapsedGameTime
                            .TotalSeconds;

                    if (_pressDurationSeconds >=
                        LONG_PRESS_SECONDS)
                    {
                        TriggerLongPress();
                    }
                }

                return;
            }

            // =========================================================
            // RELEASE
            // =========================================================
            //
            // El Click del _currentSkillSlot ocurre durante
            // base.Update(), antes de llegar aquí.
            //
            // Por eso _longPressTriggered todavía está activo
            // cuando HandleCurrentSkillClick() es llamado.
            // =========================================================

            if (mouse.LeftButton ==
                    ButtonState.Released &&
                previousMouse.LeftButton ==
                    ButtonState.Pressed)
            {
                ResetPressState();
            }
        }

        private void UpdateHudPlacement()
        {
            if (_hudAnchor == null) return;
            Rectangle bounds = _hudAnchor.DisplayRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            // Use the live HUD socket, including its parent offset and layout scale.
            Point origin = Parent?.DisplayRectangle.Location ?? Point.Zero;
            X = bounds.X - origin.X;
            Y = bounds.Y - origin.Y;
            ViewSize = ControlSize = bounds.Size;
            float scale = bounds.Height / (float)SkillSlotControl.SLOT_HEIGHT;
            if (Math.Abs(_currentSkillSlot.VisualScale - scale) > 0.0001f)
                _currentSkillSlot.VisualScale = scale;
            _currentSkillSlot.X = (bounds.Width - _currentSkillSlot.ViewSize.X) / 2;
            _currentSkillSlot.Y = (bounds.Height - _currentSkillSlot.ViewSize.Y) / 2;
            _classicSkillBar.X = (bounds.Width - SkillSlotControl.SLOT_WIDTH) / 2;
            _classicSkillBar.Y = -SkillSlotControl.SLOT_HEIGHT - 3;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || GraphicsManager.Instance == null)
                return;
            // One linear batch for both the current skill and the entire expanded list.
            using (new SpriteBatchScope(GraphicsManager.Instance.Sprite,
                SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                transform: UiScaler.SpriteTransform))
                base.Draw(gameTime);
        }

        private void TriggerLongPress()
        {
            _longPressTriggered =
                true;

            _classicSkillBar.Close();

            if (_selectionPanel != null &&
                !_selectionPanel.Visible)
            {
                _selectionPanel.Open(
                    _characterState);
            }

            Scene?
                .SetMouseInputConsumed();
        }

        private void ResetPressState()
        {
            _trackingPress =
                false;

            _longPressTriggered =
                false;

            _pressCancelled =
                false;

            _pressDurationSeconds =
                0.0;
        }
    }
}