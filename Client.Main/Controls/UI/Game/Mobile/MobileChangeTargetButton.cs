using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileChangeTargetButton : UIControl
    {
        private readonly ButtonControl _button;

        public event EventHandler ChangeTargetClicked;

        public MobileChangeTargetButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(110, 110);
            ViewSize = ControlSize;

            // Arriba de ATTACK PvP
            X = UiScaler.VirtualSize.X - 130;
            Y = UiScaler.VirtualSize.Y - 380;

            BackgroundColor = Color.Transparent;

            _button = new ButtonControl
            {
                Text = "TARGET",
                X = 0,
                Y = 0,

                ControlSize = new Point(110, 110),
                ViewSize = new Point(110, 110),
                AutoViewSize = false,

                BackgroundColor = new Color(70, 100, 70, 220),
                HoverBackgroundColor = new Color(90, 140, 90, 240),
                PressedBackgroundColor = new Color(50, 70, 50, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 14f
            };

            _button.Click += OnChangeTargetClicked;

            Controls.Add(_button);
        }

        private void OnChangeTargetClicked(object sender, EventArgs e)
        {
            ChangeTargetClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}