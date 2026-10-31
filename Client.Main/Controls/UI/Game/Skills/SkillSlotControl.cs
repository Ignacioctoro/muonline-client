#nullable enable

using System;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Skill slot using the Data_Broyal NewUI HUD socket and selection art.
    /// Supports independent visual scaling so menus can use
    /// larger icons without affecting quick-slot sizes.
    /// </summary>
    public class SkillSlotControl : UIControl
    {
        private bool _isUnavailable;

        private SkillEntryState? _skill;

        private readonly TextureControl _slotFrame;
        private readonly TextureControl _skillIcon;
        private readonly LabelControl _fallbackLabel;
        private readonly SkillTooltip _tooltip = new();
        private float _iconScale = 1f;

        private bool _isSelected;
        private bool _wasHovered;

        private float _visualScale = 1f;

        // =============================================================
        // ORIGINAL MU SIZES
        // =============================================================

        public const int SLOT_WIDTH = NewUiSkillSkin.Width;
        public const int SLOT_HEIGHT = NewUiSkillSkin.Height;

        private const int ICON_WIDTH = NewUiSkillSkin.IconWidth;
        private const int ICON_HEIGHT = NewUiSkillSkin.IconHeight;

        private const int ICON_OFFSET_X = NewUiSkillSkin.IconX;
        private const int ICON_OFFSET_Y = NewUiSkillSkin.IconY;

        // =============================================================
        // PROPERTIES
        // =============================================================

        public SkillEntryState? Skill
        {
            get => _skill;

            set
            {
                _skill = value;
                UpdateDisplay();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;

            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Scales the actual frame + skill icon.
        ///
        /// 1.0 = original MU size.
        /// 1.25 = 25% larger.
        ///
        /// This is intentionally separate from GameControl.Scale,
        /// because normal Scale only changes the outer control size.
        /// </summary>
        public float VisualScale
        {
            get => _visualScale;

            set
            {
                _visualScale =
                    Math.Max(
                        0.5f,
                        value);

                ApplyVisualScale();
            }
        }

        public float IconScale
        {
            get => _iconScale;
            set
            {
                _iconScale = Math.Clamp(value, 0.5f, 1f);
                ApplyVisualScale();
            }
        }

        public bool ShowFrame
        {
            get => _slotFrame.Visible;
            set => _slotFrame.Visible = value;
        }

        public bool IsTooltipEnabled { get; set; } = true;

        public event Action<SkillEntryState?>? HoverChanged;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public SkillSlotControl()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    SLOT_WIDTH,
                    SLOT_HEIGHT);

            ViewSize =
                ControlSize;

            Interactive = true;

            BackgroundColor =
                Color.Transparent;

            BorderThickness = 0;

            // =========================================================
            // SLOT FRAME
            // =========================================================

            _slotFrame =
                new TextureControl
                {
                    TexturePath =
                        NewUiSkillSkin.AtlasPath,
                    TextureRectangle = NewUiSkillSkin.NormalFrame,

                    AutoViewSize = false,

                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            SLOT_WIDTH,
                            SLOT_HEIGHT),

                    ViewSize =
                        new Point(
                            SLOT_WIDTH,
                            SLOT_HEIGHT),

                    Interactive = false
                };

            Controls.Add(
                _slotFrame);

            // =========================================================
            // SKILL ICON
            // =========================================================

            _skillIcon =
                new TextureControl
                {
                    AutoViewSize = false,

                    X =
                        ICON_OFFSET_X,

                    Y =
                        ICON_OFFSET_Y,

                    ControlSize =
                        new Point(
                            ICON_WIDTH,
                            ICON_HEIGHT),

                    ViewSize =
                        new Point(
                            ICON_WIDTH,
                            ICON_HEIGHT),

                    Visible = false,
                    Interactive = false
                };

            Controls.Add(
                _skillIcon);

            // =========================================================
            // FALLBACK
            // =========================================================

            _fallbackLabel =
                new LabelControl
                {
                    Text = "?",

                    TextColor =
                        Color.White,

                    FontSize = 10f,

                    X = 0,
                    Y = 10,

                    ViewSize =
                        new Point(
                            SLOT_WIDTH,
                            16),

                    TextAlign =
                        HorizontalAlign.Center,

                    Visible = false
                };

            Controls.Add(
                _fallbackLabel);

            ApplyVisualScale();
            UpdateDisplay();
        }

        // =============================================================
        // VISUAL SCALE
        // =============================================================

        private void ApplyVisualScale()
        {
            int slotWidth =
                (int)MathF.Round(
                    SLOT_WIDTH *
                    _visualScale);

            int slotHeight =
                (int)MathF.Round(
                    SLOT_HEIGHT *
                    _visualScale);

            int iconWidth =
                (int)MathF.Round(
                    ICON_WIDTH *
                    _visualScale * _iconScale);

            int iconHeight =
                (int)MathF.Round(
                    ICON_HEIGHT *
                    _visualScale * _iconScale);

            int iconOffsetX =
                (int)MathF.Round(
                    ICON_OFFSET_X *
                    _visualScale);

            int iconOffsetY =
                (int)MathF.Round(
                    ICON_OFFSET_Y *
                    _visualScale);

            if (_iconScale < 1f)
            {
                iconOffsetX = (slotWidth - iconWidth) / 2;
                iconOffsetY = (slotHeight - iconHeight) / 2;
            }

            // Outer clickable area
            ControlSize =
                new Point(
                    slotWidth,
                    slotHeight);

            ViewSize =
                ControlSize;

            // Frame
            _slotFrame.ControlSize =
                new Point(
                    slotWidth,
                    slotHeight);

            _slotFrame.ViewSize =
                _slotFrame.ControlSize;

            // Actual skill icon
            _skillIcon.X =
                iconOffsetX;

            _skillIcon.Y =
                iconOffsetY;

            _skillIcon.ControlSize =
                new Point(
                    iconWidth,
                    iconHeight);

            _skillIcon.ViewSize =
                _skillIcon.ControlSize;

            // Fallback label
            _fallbackLabel.X = 0;

            _fallbackLabel.Y =
                Math.Max(
                    0,
                    (slotHeight - 16) / 2);

            _fallbackLabel.ViewSize =
                new Point(
                    slotWidth,
                    16);


        }

        // =============================================================
        // DISPLAY
        // =============================================================

        private void UpdateDisplay()
        {
            if (_skill == null)
            {
                _skillIcon.Visible =
                    false;

                _fallbackLabel.Visible =
                    false;

                _tooltip.SetSkill(null, DisplayRectangle);

                UpdateVisualState();

                return;
            }

            var iconInfo =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId, _isUnavailable);

            if (iconInfo.HasValue)
            {
                var icon =
                    iconInfo.Value;

                _skillIcon.TexturePath =
                    icon.TexturePath;

                _skillIcon.TextureRectangle =
                    icon.SourceRectangle;

                _skillIcon.Visible =
                    true;

                _fallbackLabel.Visible =
                    false;
            }
            else
            {
                _skillIcon.Visible =
                    false;

                _fallbackLabel.Text =
                    _skill.SkillId
                        .ToString();

                _fallbackLabel.Visible =
                    true;
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _slotFrame.TextureRectangle = _isSelected && !_isUnavailable
                ? NewUiSkillSkin.SelectedFrame : NewUiSkillSkin.NormalFrame;
            _slotFrame.Alpha = _isUnavailable ? 0.65f : 1f;
            _fallbackLabel.TextColor = _isUnavailable ? Color.Gray : Color.White;
        }

        // =============================================================
        // UPDATE / HOVER
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            bool unavailable = _skill != null && !CastleSiegeSkillRules.CanUse(_skill.SkillId);
            if (unavailable != _isUnavailable)
            {
                _isUnavailable = unavailable;
                UpdateDisplay();
            }

            base.Update(
                gameTime);

            bool isHovered =
                IsMouseOver;

            _tooltip.SetSkill(IsTooltipEnabled && isHovered ? _skill : null, DisplayRectangle);

            if (_wasHovered !=
                isHovered)
            {
                _wasHovered =
                    isHovered;

                HoverChanged?.Invoke(
                    isHovered
                        ? _skill
                        : null);
            }
        }

        // =============================================================
        // TOOLTIP
        // =============================================================

        public bool DrawTooltipOverlay(SpriteBatch sprite)
        {
            if (!Visible || !IsTooltipEnabled || !IsMouseOver || _skill == null ||
                Scene?.IsPointerInputBlockedFor(this) == true) return false;
            return _tooltip.Draw(sprite);
        }
    }
}
