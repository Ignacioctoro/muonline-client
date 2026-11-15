using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    public sealed class EventsInfoControl : UIControl, IUiTexturePreloadable
    {
        private const int Width = 924;
        private const int Height = 712;
        private const string FramePath = "Interface/GFx/EventMapEntranceUI_I1.ozd";
        private const string BloodCastlePath = "Interface/GFx/EventMapEntranceUI_I95.ozd";
        private static readonly Rectangle FrameSource = new(0, 0, Width, Height);
        private static readonly Rectangle ArtSource = new(0, 0, 700, 572);
        private static readonly Rectangle CloseButton = new(879, 28, 26, 24);
        private static readonly Rectangle TimeButton = new(725, 192, 154, 32);
        private static readonly Rectangle EntryButton = new(766, 615, 113, 41);
        private static readonly Color Gold = new(219, 182, 82);
        private static readonly Color TextColor = new(222, 214, 201);

        public static EventsInfoControl Instance { get; } = new();
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private Texture2D _frame;
        private Texture2D _bloodCastleArt;
        private byte _gateLevel;
        private bool _waitingForOpening;
        private DateTime _openingReplyDeadline;
        private string _openingStatus = string.Empty;

        private EventsInfoControl()
        {
            ControlSize = ViewSize = new Point(Width, Height);
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths() => new[] { FramePath, BloodCastlePath };

        public override async Task Load()
        {
            await base.Load();
            _frame = await TextureLoader.Instance.PrepareAndGetTexture(FramePath);
            _bloodCastleArt = await TextureLoader.Instance.PrepareAndGetTexture(BloodCastlePath);
        }

        public void Toggle()
        {
            if (Visible)
            {
                CloseWindow();
                return;
            }

            _gateLevel = BloodCastleEnterControl.Instance.GetEventGateLevel();
            Visible = true;
            PositionWindow();
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            RefreshOpening();
        }

        public void CloseWindow()
        {
            Visible = false;
            _waitingForOpening = false;
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
        }

        internal bool TrySetBloodCastleOpening(string message)
        {
            if (!Visible || !_waitingForOpening) return false;
            _waitingForOpening = false;
            _openingStatus = message;
            return true;
        }

        private void RefreshOpening()
        {
            if (_waitingForOpening) return;
            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                _openingStatus = "Opening time unavailable.";
                return;
            }

            _waitingForOpening = true;
            _openingReplyDeadline = DateTime.UtcNow.AddSeconds(6);
            _openingStatus = "Checking opening time...";
            _ = service.RequestBloodCastleOpeningAsync(_gateLevel);
        }

        private void PositionWindow()
        {
            var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
            Scale = MathHelper.Clamp(Math.Min((size.X - 16f) / Width, (size.Y - 16f) / Height), 0.1f, 1f);
            X = (size.X - DisplaySize.X) / 2;
            Y = (size.Y - DisplaySize.Y) / 2;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            PositionWindow();
            base.Update(gameTime);
            if (_waitingForOpening && DateTime.UtcNow >= _openingReplyDeadline)
            {
                _waitingForOpening = false;
                _openingStatus = "No opening time reported.";
            }

            if (Scene?.IsPointerInputBlockedFor(this) == true) return;
            var game = MuGame.Instance;
            if (HasFocus && game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                CloseWindow();
                return;
            }

            var mouse = game.UiMouseState;
            if (mouse.LeftButton != ButtonState.Pressed || game.PrevUiMouseState.LeftButton != ButtonState.Released ||
                !DisplayRectangle.Contains(mouse.Position))
                return;

            Scene?.SetMouseInputConsumed();
            var point = ToLocal(mouse.Position);
            if (CloseButton.Contains(point))
                CloseWindow();
            else if (TimeButton.Contains(point))
                RefreshOpening();
            else if (EntryButton.Contains(point))
            {
                CloseWindow();
                BloodCastleEnterControl.Instance.ShowWindow();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var graphics = GraphicsManager.Instance;
            var batch = graphics.Sprite;
            var pixel = graphics.Pixel;
            var font = graphics.Font;
            if (pixel == null || font == null) return;

            var transform = Matrix.CreateScale(Scale, Scale, 1f) *
                Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0f) * UiScaler.SpriteTransform;
            var mouse = ToLocal(MuGame.Instance.UiMouseState.Position);
            using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, GraphicsManager.GetQualityLinearSamplerState(), transform: transform);

            batch.Draw(pixel, new Rectangle(0, 0, Width, Height), new Color(15, 11, 9));
            if (_frame != null) batch.Draw(_frame, FrameSource, FrameSource, Color.White);
            if (_bloodCastleArt != null)
                batch.Draw(_bloodCastleArt, new Rectangle(205, 111, 699, 576), ArtSource, Color.White);

            DrawPanel(batch, pixel, new Rectangle(18, 76, 888, 31), new Color(20, 16, 13));
            DrawButton(batch, pixel, font, new Rectangle(27, 78, 103, 27), "Event Map", true, false);
            DrawButton(batch, pixel, font, new Rectangle(134, 78, 103, 27), "Boss Battle", false, true);
            DrawButton(batch, pixel, font, new Rectangle(241, 78, 103, 27), "Competition", false, true);
            DrawText(batch, font, "Events Info", new Rectangle(50, 29, 824, 23), Gold, 17f, true);
            DrawText(batch, font, "X", CloseButton, CloseButton.Contains(mouse) ? Gold : new Color(211, 117, 69), 15f, true);

            var row = new Rectangle(31, 119, 155, 48);
            DrawPanel(batch, pixel, row, new Color(127, 88, 27));
            if (_bloodCastleArt != null)
                batch.Draw(_bloodCastleArt, new Rectangle(33, 121, 151, 44), new Rectangle(100, 190, 500, 146), Color.White);
            batch.Draw(pixel, new Rectangle(33, 121, 151, 44), new Color(12, 9, 7, 130));
            DrawText(batch, font, "Blood Castle", row, Gold, 16f, true);

            DrawText(batch, font, "Blood Castle", new Rectangle(222, 123, 470, 41), Gold, 34f);
            DrawText(batch, font, "Break through the castle defenses and return the\nArchangel's weapon before time runs out.",
                new Rectangle(222, 170, 470, 45), TextColor, 16f);
            DrawButton(batch, pixel, font, TimeButton, "Event Entry Time", TimeButton.Contains(mouse), _waitingForOpening);

            var info = new Rectangle(599, 320, 280, 139);
            DrawPanel(batch, pixel, info, new Color(12, 10, 9, 207));
            DrawText(batch, font, "Entry Information", new Rectangle(615, 332, 248, 24), Gold, 17f);
            DrawText(batch, font, $"Suggested gate: Blood Castle {_gateLevel}", new Rectangle(615, 370, 248, 21), TextColor, 15f);
            DrawText(batch, font, "See level ranges in the entry window.", new Rectangle(615, 399, 248, 21), TextColor, 15f);

            DrawPanel(batch, pixel, new Rectangle(222, 577, 211, 86), new Color(12, 10, 9, 215));
            DrawText(batch, font, "Required Item", new Rectangle(238, 585, 179, 24), Gold, 17f, true);
            DrawText(batch, font, "Invisibility Cloak", new Rectangle(238, 617, 179, 24), TextColor, 15f, true);
            DrawText(batch, font, _openingStatus, new Rectangle(449, 577, 430, 29), Gold, 15f, true);
            DrawButton(batch, pixel, font, EntryButton, "Enter", EntryButton.Contains(mouse), false);
        }

        private Point ToLocal(Point p) => new((int)((p.X - DisplayPosition.X) / Scale), (int)((p.Y - DisplayPosition.Y) / Scale));

        private static void DrawPanel(SpriteBatch batch, Texture2D pixel, Rectangle rect, Color fill)
        {
            batch.Draw(pixel, rect, new Color(111, 82, 46));
            rect.Inflate(-1, -1);
            batch.Draw(pixel, rect, fill);
        }

        private static void DrawButton(SpriteBatch batch, Texture2D pixel, SpriteFont font,
            Rectangle rect, string label, bool highlighted, bool disabled)
        {
            var fill = highlighted ? new Color(108, 44, 16) : new Color(45, 30, 23);
            DrawPanel(batch, pixel, rect, fill);
            DrawText(batch, font, label, rect, disabled ? new Color(140, 129, 116) : TextColor, 15f, true);
        }

        private static void DrawText(SpriteBatch batch, SpriteFont font, string text, Rectangle rect,
            Color color, float size, bool centered = false)
        {
            float scale = size / Constants.BASE_FONT_SIZE;
            var measured = font.MeasureString(text) * scale;
            var position = new Vector2(centered ? rect.Center.X - measured.X / 2 : rect.X,
                rect.Center.Y - measured.Y / 2);
            batch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
