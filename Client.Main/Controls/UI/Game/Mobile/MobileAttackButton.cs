using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileAttackButton : UIControl
    {
        private readonly ButtonControl _button;

        public event EventHandler AttackClicked;

        public MobileAttackButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(110, 110);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 130;
            Y = UiScaler.VirtualSize.Y - 140;

            BackgroundColor = Color.Transparent;

            _button = new ButtonControl
            {
                Text = "ATTACK",
                X = 0,
                Y = 0,
                ControlSize = new Point(110, 110),
                ViewSize = new Point(110, 110),
                AutoViewSize = false,

                BackgroundColor = new Color(120, 40, 40, 220),
                HoverBackgroundColor = new Color(170, 60, 60, 240),
                PressedBackgroundColor = new Color(90, 30, 30, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,
                FontSize = 16f
            };

            _button.Click += OnAttackClicked;

            Controls.Add(_button);
        }

        private void OnAttackClicked(object sender, EventArgs e)
        {
            AttackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}