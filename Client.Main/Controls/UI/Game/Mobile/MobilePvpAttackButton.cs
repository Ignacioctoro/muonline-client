using System;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobilePvpAttackButton : UIControl
    {
        private readonly ButtonControl _basicAttackButton;
        private readonly ButtonControl _skillAttackButton;

        public event EventHandler BasicAttackClicked;
        public event EventHandler SkillAttackClicked;

        public MobilePvpAttackButton()
        {
            AutoViewSize = false;

            // Mismo tamaño total que antes.
            ControlSize = new Point(110, 110);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 130;
            Y = UiScaler.VirtualSize.Y - 260;

            BackgroundColor = Color.Transparent;

            // ─────────────────────────────────────────────
            // MITAD SUPERIOR
            // Equivalente aproximado al click izquierdo.
            // ─────────────────────────────────────────────
            _basicAttackButton = new ButtonControl
            {
                Text = "PvP ATTACK",

                X = 0,
                Y = 0,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                BackgroundColor = new Color(60, 70, 120, 220),
                HoverBackgroundColor = new Color(80, 90, 160, 240),
                PressedBackgroundColor = new Color(40, 50, 90, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 12f
            };

            // ─────────────────────────────────────────────
            // MITAD INFERIOR
            // Equivalente aproximado al click derecho.
            // Usa el skill seleccionado.
            // ─────────────────────────────────────────────
            _skillAttackButton = new ButtonControl
            {
                Text = "SKILL",

                X = 0,
                Y = 56,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                BackgroundColor = new Color(90, 60, 120, 220),
                HoverBackgroundColor = new Color(120, 80, 160, 240),
                PressedBackgroundColor = new Color(65, 40, 90, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 13f
            };

            _basicAttackButton.Click += OnBasicAttackClicked;
            _skillAttackButton.Click += OnSkillAttackClicked;

            Controls.Add(_basicAttackButton);
            Controls.Add(_skillAttackButton);
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