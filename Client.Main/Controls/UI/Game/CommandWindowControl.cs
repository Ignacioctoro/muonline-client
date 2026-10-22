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
    public enum PlayerCommand
    {
        Trade, Buy, Party, Whisper, Guild, Alliance, Hostility,
        SuspendHostilities, AddFriend, Follow, Duel
    }

    public sealed class CommandWindowControl : UIControl
    {
        internal const string AtlasPath = "Interface/GFx/CommandWindow_I1.ozd";
        private Texture2D _atlas;
        private string _hint = "Elige una acción y luego un jugador.";
        public override bool BlocksPointerInput => true;

        public CommandWindowControl()
        {
            AutoViewSize = false;
            ViewSize = ControlSize = new Point(250, 560);
            Align = ControlAlign.Top | ControlAlign.Right;
            Margin = new Margin { Top = 60, Right = 10 };
            Interactive = true;
            Visible = false;
            string[] labels = { "Trade", "Buy", "Party", "Whisper", "Guild", "Alliance",
                "Hostility guild", "Suspend hostilities", "Add Friend", "Follow", "Duel" };
            for (int i = 0; i < labels.Length; i++)
            {
                var action = (PlayerCommand)i;
                var button = new CommandButton
                {
                    Text = labels[i], X = 55, Y = 72 + i * 40,
                    AutoViewSize = false, ViewSize = new Point(140, 34), ControlSize = new Point(140, 34)
                };
                button.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();
                    (Scene as GameScene)?.BeginPlayerCommand(action);
                };
                Controls.Add(button);
            }
            var close = new CommandButton
            {
                Text = "", IsClose = true, X = 220, Y = 25,
                AutoViewSize = false, ViewSize = new Point(20, 20), ControlSize = new Point(20, 20)
            };
            close.Click += (_, _) => Close();
            Controls.Add(close);
        }

        public void SetHint(string hint) => _hint = hint;
        public void Toggle()
        {
            if (Visible) { Close(); return; }
            Visible = true;
            BringToFront();
            Scene?.SetMouseInputConsumed();
        }
        public void Close()
        {
            Visible = false;
            (Scene as GameScene)?.CancelPlayerCommand();
            Scene?.SetMouseInputConsumed();
        }
        public override async Task Load()
        {
            _atlas = await TextureLoader.Instance.PrepareAndGetTexture(AtlasPath);
            await base.Load();
        }
        public override void Update(GameTime time)
        {
            base.Update(time);
            if (!Visible) return;
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                Close();
            }
        }
        public override void Draw(GameTime time)
        {
            if (!Visible || Status != GameControlStatus.Ready || _atlas == null) return;
            var sprite = GraphicsManager.Instance.Sprite;
            using var scope = new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform);
            var b = DisplayRectangle;
            // Exact CommandWindow.ozg subimages: body 1, header 3.
            NewUiFrameRenderer.DrawNineSlice(sprite, _atlas, new Rectangle(0, 0, 327, 386),
                new Rectangle(b.X, b.Y + 48, b.Width, b.Height - 48), 16, 12, 16, 20);
            sprite.Draw(_atlas, new Rectangle(b.X, b.Y, b.Width, 56),
                new Rectangle(652, 0, 327, 73), Color.White);
            var font = GraphicsManager.Instance.Font;
            if (font != null)
            {
                float scale = 12f / Constants.BASE_FONT_SIZE;
                var size = font.MeasureString("Command Window") * scale;
                sprite.DrawString(font, "Command Window",
                    new Vector2(b.Center.X - size.X / 2, b.Y + 33), Color.Gold,
                    0, Vector2.Zero, scale, SpriteEffects.None, 0);
                float hintScale = Math.Min(10f / Constants.BASE_FONT_SIZE,
                    (b.Width - 28f) / Math.Max(1f, font.MeasureString(_hint).X));
                sprite.DrawString(font, _hint, new Vector2(b.X + 14, b.Bottom - 30),
                    Color.Gold, 0, Vector2.Zero, hintScale, SpriteEffects.None, 0);
            }
            foreach (var control in Controls) control.Draw(time);
        }

        private sealed class CommandButton : ButtonControl
        {
            private Texture2D _atlas;
            public bool IsClose { get; set; }
            public override async Task Load()
            {
                _atlas = await TextureLoader.Instance.PrepareAndGetTexture(AtlasPath);
                await base.Load();
            }
            public override void Draw(GameTime time)
            {
                if (!Visible || Status != GameControlStatus.Ready || _atlas == null) return;
                Rectangle source = IsClose
                    ? new Rectangle(IsMouseOver ? 1002 : 981, 21, 19, 19)
                    : !Enabled ? new Rectangle(779, 112, 125, 35)
                    : IsMousePressed ? new Rectangle(652, 112, 125, 35)
                    : IsMouseOver ? new Rectangle(779, 75, 125, 35)
                    : new Rectangle(652, 75, 125, 35);
                var sprite = GraphicsManager.Instance.Sprite;
                sprite.Draw(_atlas, DisplayRectangle, source, Color.White);
                var font = GraphicsManager.Instance.Font;
                if (font == null || IsClose) return;
                float scale = 11f / Constants.BASE_FONT_SIZE;
                var size = font.MeasureString(Text) * scale;
                sprite.DrawString(font, Text,
                    new Vector2(DisplayRectangle.Center.X - size.X / 2, DisplayRectangle.Center.Y - size.Y / 2),
                    IsMouseOver ? Color.Gold : Color.LightGray, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }
    }
}
