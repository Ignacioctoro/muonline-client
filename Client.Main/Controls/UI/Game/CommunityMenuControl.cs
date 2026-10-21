using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>Shared desktop/touch Community launcher above the bottom HUD.</summary>
    public sealed class CommunityMenuControl : UIControl
    {
        private Texture2D _frame;
        private GameControl _anchor;
        private bool _justOpened;
        public override bool BlocksPointerInput => true;

        public CommunityMenuControl()
        {
            int rowHeight = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 28;
            AutoViewSize = false;
            ViewSize = ControlSize = new Point(180, 24 + rowHeight * 3);
            Interactive = true;
            Visible = false;
            AddEntry("F   Amigos", 0, rowHeight, game => game.FriendsWindow?.Toggle());
            AddEntry("G   Guild", 1, rowHeight, game => game.OpenGuildMenu());
            AddEntry("P   Party", 2, rowHeight, game => game.PartyMatchingWindow?.Toggle());
        }

        private void AddEntry(string text, int index, int height, Action<GameScene> action)
        {
            var button = new ButtonControl
            {
                Text = text, X = 12, Y = 12 + index * height,
                AutoViewSize = false, ViewSize = new Point(156, height),
                ControlSize = new Point(156, height), FontSize = 12f,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = new Color(65, 48, 22, 190),
                PressedBackgroundColor = new Color(85, 62, 25, 220),
                TextColor = new Color(225, 218, 197), HoverTextColor = Color.Gold
            };
            button.Click += (_, _) =>
            {
                Visible = false;
                Scene?.SetMouseInputConsumed();
                if (Scene is GameScene game) action(game);
            };
            Controls.Add(button);
        }

        public override async Task Load()
        {
            _frame = await TextureLoader.Instance.PrepareAndGetTexture(NewUiFrameRenderer.PersonalShopAtlasPath);
            await base.Load();
        }

        public void Toggle(GameControl anchor)
        {
            if (Visible) { Visible = false; return; }
            _anchor = anchor;
            PositionAboveAnchor();
            Visible = true;
            _justOpened = true;
            BringToFront();
            Scene?.SetMouseInputConsumed();
        }

        private void PositionAboveAnchor()
        {
            if (_anchor == null) return;
            var bounds = _anchor.DisplayRectangle;
            X = Math.Clamp(bounds.Right - ViewSize.X, 0, Math.Max(0, UiScaler.VirtualSize.X - ViewSize.X));
            Y = Math.Max(0, bounds.Top - ViewSize.Y - 6);
        }

        public override void Update(GameTime gameTime)
        {
            PositionAboveAnchor();
            base.Update(gameTime);
            if (!Visible || Status != GameControlStatus.Ready) return;
            if (_justOpened) { _justOpened = false; return; }
            var mouse = CurrentMouseState;
            if (mouse.LeftButton == ButtonState.Pressed &&
                PreviousMouseState.LeftButton == ButtonState.Released &&
                !DisplayRectangle.Contains(mouse.Position) &&
                _anchor?.DisplayRectangle.Contains(mouse.Position) != true)
            {
                // Dismiss without passing this press to the world below.
                Visible = false;
                Scene?.SetMouseInputConsumed();
            }
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Visible = false;
                Scene?.ConsumeKeyboardEscape();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var sprite = GraphicsManager.Instance.Sprite;
            using var scope = new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform);
            sprite.Draw(GraphicsManager.Instance.Pixel, DisplayRectangle, new Color(14, 12, 10, 245));
            NewUiFrameRenderer.DrawPersonalShopInset(sprite, _frame, DisplayRectangle);
            foreach (var control in Controls) control.Draw(gameTime);
        }
    }
}
