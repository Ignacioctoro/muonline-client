using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileAttackButton : UIControl
    {
        private readonly MobileButtonVisual _basicAttackVisual;
        private readonly MobileButtonVisual _skillAttackVisual;

        private readonly ButtonControl _basicAttackButton;
        private readonly ButtonControl _skillAttackButton;

        public event EventHandler BasicAttackClicked;
        public event EventHandler SkillAttackClicked;

        public MobileAttackButton()
        {
            AutoViewSize = false;

            ControlSize = new Point(120, 150);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 280;
            Y = UiScaler.VirtualSize.Y - 245;

            BackgroundColor = Color.Transparent;

            _basicAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/AttackMonster.png",

                X = 38,
                Y = 68,

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            _skillAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/SkillMonster.png",

                X = 0,
                Y = 0,

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            _basicAttackButton = new ButtonControl
            {
                Text = string.Empty,

                X = 38,
                Y = 68,

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
                AutoViewSize = false,

                Interactive = true,

                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent
            };

            _skillAttackButton = new ButtonControl
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

            _basicAttackButton.Click += OnBasicAttackClicked;
            _skillAttackButton.Click += OnSkillAttackClicked;

            Controls.Add(_basicAttackVisual);
            Controls.Add(_skillAttackVisual);

            Controls.Add(_basicAttackButton);
            Controls.Add(_skillAttackButton);
        }

        public void SetOpacity(float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0f, 1f);

            _basicAttackVisual.Alpha = opacity;
            _skillAttackVisual.Alpha = opacity;
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