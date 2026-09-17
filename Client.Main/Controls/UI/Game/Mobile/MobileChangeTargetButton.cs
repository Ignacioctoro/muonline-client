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

            ControlSize = new Point(82, 82);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 200;
            Y = UiScaler.VirtualSize.Y - 470;

            BackgroundColor = Color.Transparent;

            _visual = new MobileButtonVisual
            {
                TexturePath = "Mobile/Target.png",

                X = 0,
                Y = 0,

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
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

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
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

        public void SetOpacity(float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0f, 1f);

            _visual.Alpha = opacity;
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