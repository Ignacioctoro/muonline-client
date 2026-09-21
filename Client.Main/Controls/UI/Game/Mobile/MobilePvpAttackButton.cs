using System;
using Client.Main.Scenes;
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

            ControlSize = new Point(110, 154);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 295;
            Y = UiScaler.VirtualSize.Y - 410;

            BackgroundColor = Color.Transparent;

            _basicAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/PvpAttack.png",

                X = 0,
                Y = 72,

                ControlSize = new Point(82, 82),
                ViewSize = new Point(82, 82),
                AutoViewSize = false,

                Interactive = false,
                BackgroundColor = Color.Transparent,

                PressedTint = new Color(170, 170, 170)
            };

            _skillAttackVisual = new MobileButtonVisual
            {
                TexturePath = "Mobile/PvpSkill.png",

                X = 28,
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

                X = 0,
                Y = 72,

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

                X = 28,
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
            bool coveredByWindow =
                Scene is GameScene gameScene &&
                gameScene.IsMobileControlCovered(this);

            _basicAttackButton.Interactive =
                !coveredByWindow;

            _skillAttackButton.Interactive =
                !coveredByWindow;

            if (coveredByWindow)
            {
                _basicAttackButton.IsMousePressed = false;
                _skillAttackButton.IsMousePressed = false;
            }

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