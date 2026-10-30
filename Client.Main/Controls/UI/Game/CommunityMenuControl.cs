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
            int rowHeight = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 18;
            AutoViewSize = false;
            ViewSize = ControlSize = new Point(145, 29 + rowHeight * 4);
            Interactive = true;
            Visible = false;
            AddEntry("F", "Amigos", 0, rowHeight, game => game.FriendsWindow?.Toggle());
            AddEntry("G", "Guild", 1, rowHeight, game => game.OpenGuildMenu());
            AddEntry("P", "Party", 2, rowHeight, game => game.PartyMatchingWindow?.Toggle());
            AddEntry("Y", "Pets", 3, rowHeight, _ => PetInfoControl.Instance.Toggle());
        }

        private void AddEntry(string key, string text, int index, int height, Action<GameScene> action)
        {
            var button = new CommunityEntry(key)
            {
                Text = text, X = 9, Y = 14 + index * height,
                AutoViewSize = false, ViewSize = new Point(127, height),
                ControlSize = new Point(127, height), FontSize = 12f,
                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent,
                TextColor = new Color(145, 145, 145), HoverTextColor = Color.Red
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
            _frame = await TextureLoader.Instance.PrepareAndGetTexture(NativeHudMenuSkin.AtlasPath);
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
            NativeHudMenuSkin.DrawCommunity(sprite, _frame, DisplayRectangle);
            foreach (var control in Controls) control.Draw(gameTime);
        }

        // LRSubMenu in main.ozg uses separate key/name fields, no colored row fill.
        private sealed class CommunityEntry : ButtonControl
        {
            private readonly string _key;
            public CommunityEntry(string key) => _key = key;

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready) return;
                var font = GraphicsManager.Instance.Font;
                if (font == null) return;
                var sprite = GraphicsManager.Instance.Sprite;
                var bounds = DisplayRectangle;
                float scale = 12f / Constants.BASE_FONT_SIZE;
                var keySize = font.MeasureString(_key) * scale;
                var textSize = font.MeasureString(Text) * scale;
                sprite.DrawString(font, _key,
                    new Vector2(bounds.X + 16 - keySize.X / 2, bounds.Center.Y - keySize.Y / 2),
                    new Color(254, 42, 42), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                sprite.DrawString(font, Text,
                    new Vector2(bounds.X + 75 - textSize.X / 2, bounds.Center.Y - textSize.Y / 2),
                    IsMouseOver ? Color.Red : new Color(145, 145, 145),
                    0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }
    }
}
