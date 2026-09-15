using System;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobilePvpAttackButton : UIControl
    {
        private readonly ButtonControl _button;

        public event EventHandler PvpAttackClicked;

        public MobilePvpAttackButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(110, 110);
            ViewSize = ControlSize;

            // Debajo del botón ATTACK normal
            X = UiScaler.VirtualSize.X - 130;
            Y = UiScaler.VirtualSize.Y - 260;

            BackgroundColor = Color.Transparent;

            _button = new ButtonControl
            {
                Text = "ATTACK PvP",
                X = 0,
                Y = 0,

                ControlSize = new Point(110, 110),
                ViewSize = new Point(110, 110),
                AutoViewSize = false,

                BackgroundColor = new Color(60, 70, 120, 220),
                HoverBackgroundColor = new Color(80, 90, 160, 240),
                PressedBackgroundColor = new Color(40, 50, 90, 240),

                TextColor = Color.White,
                HoverTextColor = Color.White,

                FontSize = 14f
            };

            _button.Click += OnPvpAttackClicked;

            Controls.Add(_button);
        }

        private void OnPvpAttackClicked(object sender, EventArgs e)
        {
            PvpAttackClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}