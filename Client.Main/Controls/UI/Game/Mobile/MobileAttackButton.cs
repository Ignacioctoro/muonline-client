using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileAttackButton : UIControl
    {
        private readonly ButtonControl _basicAttackButton;
        private readonly ButtonControl _skillAttackButton;

        public event EventHandler BasicAttackClicked;
        public event EventHandler SkillAttackClicked;

        public MobileAttackButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(110, 110);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 130;
            Y = UiScaler.VirtualSize.Y - 140;

            BackgroundColor = Color.Transparent;

            // Mitad superior: ataque básico / arma
            _basicAttackButton = new ButtonControl
            {
                Text = "ATTACK",

                X = 0,
                Y = 0,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                BackgroundColor = new Color(120, 40, 40, 220),
                HoverBackgroundColor = new Color(170, 60, 60, 240),
                PressedBackgroundColor = new Color(90, 30, 30, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 14f
            };

            // Mitad inferior: skill seleccionado
            _skillAttackButton = new ButtonControl
            {
                Text = "SKILL",

                X = 0,
                Y = 56,

                ControlSize = new Point(110, 54),
                ViewSize = new Point(110, 54),
                AutoViewSize = false,

                BackgroundColor = new Color(120, 70, 40, 220),
                HoverBackgroundColor = new Color(170, 100, 60, 240),
                PressedBackgroundColor = new Color(90, 50, 30, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 14f
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