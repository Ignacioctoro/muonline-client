using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileChangeTargetButton : UIControl
    {
        private readonly MobileButtonVisual _visual;
        private readonly ButtonControl _button;

        public event EventHandler ChangeTargetClicked;

        public MobileChangeTargetButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(110, 55);
            ViewSize = ControlSize;

            // Más a la izquierda.
            X = UiScaler.VirtualSize.X - 150;

            // Un poco más cerca del grupo PvP.
            Y = UiScaler.VirtualSize.Y - 320;

            BackgroundColor = Color.Transparent;

            _visual = new MobileButtonVisual
            {
                TexturePath = "Mobile/Target.png",

                // Imagen más grande, centrada sobre el hitbox.
                X = -15,
                Y = -7,

                ControlSize = new Point(140, 70),
                ViewSize = new Point(140, 70),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            _button = new ButtonControl
            {
                Text = string.Empty,

                X = 0,
                Y = 0,

                // Hitbox original.
                ControlSize = new Point(110, 55),
                ViewSize = new Point(110, 55),
                AutoViewSize = false,

                Interactive = true,

                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent
            };

            _button.Click += OnChangeTargetClicked;

            Controls.Add(_visual);
            Controls.Add(_button);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _visual.Pressed = _button.IsMousePressed;
        }

        private void OnChangeTargetClicked(object sender, EventArgs e)
        {
            ChangeTargetClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}