using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobilePvpAttackButton : UIControl
    {
        private readonly MobileButtonVisual _basicAttackVisual;
        private readonly MobileButtonVisual _skillAttackVisual;

        private readonly ButtonControl _basicAttackButton;
        private readonly ButtonControl _skillAttackButton;

        public event EventHandler BasicAttackClicked;
        public event EventHandler SkillAttackClicked;

        public MobilePvpAttackButton()
        {
            AutoViewSize = false;

            // Reducimos ligeramente el alto lógico porque
            // ahora los botones están más juntos.
            ControlSize = new Point(110, 106);
            ViewSize = ControlSize;

            // Más a la izquierda.
            X = UiScaler.VirtualSize.X - 150;

            // Más cerca de Cambiar Objetivo.
            Y = UiScaler.VirtualSize.Y - 245;

            BackgroundColor = Color.Transparent;

            // =====================================================
            // VISUAL: PVP ATTACK
            // =====================================================

            _basicAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/PvpAttack.png",

                X = -15,
                Y = -7,

                ControlSize = new Point(140, 69),
                ViewSize = new Point(140, 69),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            // =====================================================
            // VISUAL: PVP SKILL
            // =====================================================

            _skillAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/PvpSkill.png",

                X = -15,

                // Más cerca del botón superior.
                Y = 45,

                ControlSize = new Point(140, 69),
                ViewSize = new Point(140, 69),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            // =====================================================
            // HITBOX: PVP ATTACK
            // =====================================================

            _basicAttackButton = new ButtonControl
            {
                Text = string.Empty,

                X = 0,
                Y = 0,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                Interactive = true,

                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent
            };

            // =====================================================
            // HITBOX: PVP SKILL
            // =====================================================

            _skillAttackButton = new ButtonControl
            {
                Text = string.Empty,

                X = 0,

                // Antes 56.
                // Ahora más junto.
                Y = 52,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                Interactive = true,

                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent
            };

            _basicAttackButton.Click += OnBasicAttackClicked;
            _skillAttackButton.Click += OnSkillAttackClicked;

            Controls.Add(_basicAttackVisual);
            Controls.Add(_skillAttackVisual);

            Controls.Add(_basicAttackButton);
            Controls.Add(_skillAttackButton);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _basicAttackVisual.Pressed =
                _basicAttackButton.IsMousePressed;

            _skillAttackVisual.Pressed =
                _skillAttackButton.IsMousePressed;
        }

        private void OnBasicAttackClicked(object sender, EventArgs e)
        {
            BasicAttackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnSkillAttackClicked(object sender, EventArgs e)
        {
            SkillAttackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}