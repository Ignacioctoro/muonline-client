using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.SelectCharacter
{
    public class CharacterDeletionDialog : GameControl
    {
        private static class Theme
        {
            public static readonly Color BgDarkest = new(0, 0, 0, 185);
            public static readonly Color BgDark = new(0, 0, 0, 210);
            public static readonly Color BgMid = new(35, 30, 22);
            public static readonly Color BgLight = new(74, 61, 42);
            public static readonly Color BgLighter = new(130, 108, 66);
            public static readonly Color Accent = new(234, 206, 132);
        }

        private enum Step { ConfirmCharacter, Credentials, FinalWarning }
        private Step _step;
        private readonly string _characterName;
        private readonly SecretInput _input;
        private readonly Texture2D[] _frame = new Texture2D[9];
        private Texture2D _buttons;
        private RenderTarget2D _surface;
        private bool _finished;
        private int _pressed = -1;
        private readonly BlendState _surfaceBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };
        public override bool BlocksPointerInput => true;
        public event EventHandler<string> DeleteConfirmed;
        public event EventHandler CancelRequested;

        public CharacterDeletionDialog(string characterName)
        {
            _characterName = characterName;
            AutoViewSize = false;
            ViewSize = new Point(304, 186);
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
            Interactive = true;
            _input = new SecretInput
            {
                X = 70, Y = 67, ViewSize = new Point(164, 24), MaxLength = 20,
                Visible = false, Interactive = false,
            };
            Controls.Add(_input);
        }

        public override async Task Load()
        {
            var tasks = new Task<Texture2D>[10];
            for (int i = 0; i < 9; i++) tasks[i] = TextureLoader.Instance.PrepareAndGetTexture($"Interface/GFx/popupbg{i + 1:00}.ozd");
            tasks[9] = TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/winLoginSceneBottomUI_I1.ozd");
            var textures = await Task.WhenAll(tasks);
            Array.Copy(textures, _frame, 9);
            _buttons = textures[9];
        }

        // Pre-rendered before the world: never switch the main target in the UI pass.
        internal void PrepareDraw()
        {
            if (!Visible || Status != GameControlStatus.Ready || _surface != null) return;
            var gd = GraphicsDevice;
            var targets = gd.GetRenderTargets();
            _surface = new RenderTarget2D(gd, 304, 186, false, SurfaceFormat.Color, DepthFormat.None);
            try
            {
                gd.SetRenderTarget(_surface);
                gd.Clear(Color.Transparent);
                using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred, _surfaceBlend, SamplerState.LinearClamp);
                var sb = GraphicsManager.Instance.Sprite;
                for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    int left = x == 0 ? 0 : x == 1 ? 16 : 288;
                    int top = y == 0 ? 0 : y == 1 ? 16 : 170;
                    var rect = new Rectangle(left, top, x == 1 ? 272 : 16, y == 1 ? 154 : 16);
                    if (x == 1 && y == 1) sb.Draw(GraphicsManager.Instance.Pixel, rect, Theme.BgDarkest);
                    else if (_frame[y * 3 + x] != null) sb.Draw(_frame[y * 3 + x], rect, Color.White);
                }
                sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(13, 34, 278, 1), Theme.BgLight);
            }
            finally { gd.SetRenderTargets(targets); }
        }

        private Rectangle ButtonRect(int index) => new(DisplayRectangle.X + 77 + index * 80, DisplayRectangle.Y + 140, 72, 30);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!Visible || Status != GameControlStatus.Ready || _finished) return;
            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released)
            {
                _pressed = ButtonRect(0).Contains(mouse.Position) ? 0 : ButtonRect(1).Contains(mouse.Position) ? 1 : -1;
                Scene?.SetMouseInputConsumed();
            }
            if (mouse.LeftButton == ButtonState.Released && previous.LeftButton == ButtonState.Pressed)
            {
                int pressed = _pressed;
                _pressed = -1;
                if (pressed >= 0 && ButtonRect(pressed).Contains(mouse.Position))
                {
                    if (pressed == 0) Advance(); else Cancel();
                }
            }
            if (_finished) return;
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape)) Cancel();
            else if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Enter) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Enter)) Advance();
        }

        private void Advance()
        {
            if (_finished) return;
            Scene?.SetMouseInputConsumed();
            if (_step == Step.ConfirmCharacter)
            {
                _step = Step.Credentials;
                _input.Visible = _input.Interactive = true;
                _input.Focus();
            }
            else if (_step == Step.Credentials)
            {
                if (string.IsNullOrEmpty(_input.Text)) { _input.Focus(); return; }
                _step = Step.FinalWarning;
                _input.Visible = _input.Interactive = false;
                _input.Unfocus();
            }
            else
            {
                _finished = true;
                string secret = _input.Text;
                _input.Text = string.Empty;
                DeleteConfirmed?.Invoke(this, secret);
            }
        }

        private void Cancel()
        {
            _finished = true;
            _input.Text = string.Empty;
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, transform: UiScaler.SpriteTransform);
            var sb = GraphicsManager.Instance.Sprite;
            var rect = DisplayRectangle;
            if (_surface != null) sb.Draw(_surface, rect, Color.White);
            string text = _step switch
            {
                Step.ConfirmCharacter => $"Would you like to delete\n{_characterName}?",
                Step.Credentials => "Enter your security code\nor account password.",
                _ => "You cannot restore a deleted character!\nIts equipment and items\ncannot be recovered.",
            };
            DrawText(sb, text, new Rectangle(rect.X + 14, rect.Y + 38, rect.Width - 28, _step == Step.Credentials ? 28 : 66), Color.White);
            if (_input.Visible) _input.Draw(gameTime);
            for (int i = 0; i < 2; i++)
            {
                var button = ButtonRect(i);
                bool hover = button.Contains(MuGame.Instance.UiMouseState.Position);
                var source = hover && _pressed == i ? new Rectangle(385, 2, 117, 28)
                    : hover ? new Rectangle(258, 2, 117, 28) : new Rectangle(131, 2, 117, 28);
                if (_buttons != null) sb.Draw(_buttons, button, source, Color.White);
                DrawText(sb, i == 0 ? "OK" : "Cancel", button, Theme.Accent);
            }
        }

        private static void DrawText(SpriteBatch sb, string text, Rectangle rect, Color color)
        {
            var font = GraphicsManager.Instance.Font;
            var size = font.MeasureString(text);
            float scale = Math.Min(0.39f, rect.Width / Math.Max(1, size.X));
            var position = new Vector2(rect.X + (rect.Width - size.X * scale) / 2, rect.Y + (rect.Height - size.Y * scale) / 2);
            sb.DrawString(font, text, position + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            sb.DrawString(font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private sealed class SecretInput : TextBoxControl
        {
            public override void Draw(GameTime gameTime)
            {
                var sb = GraphicsManager.Instance.Sprite;
                var rect = DisplayRectangle;
                sb.Draw(GraphicsManager.Instance.Pixel, rect, Theme.BgDark);
                sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), Theme.BgLight);
                string masked = new('*', Text.Length);
                DrawText(sb, masked, rect, Color.White);
                if (gameTime.TotalGameTime.TotalSeconds % 1 < 0.5)
                {
                    int x = rect.Center.X + (int)(GraphicsManager.Instance.Font.MeasureString(masked).X * 0.39f / 2);
                    sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(x, rect.Y + 5, 1, rect.Height - 10), Theme.Accent);
                }
            }
        }

        public override void Dispose()
        {
            _input.Text = string.Empty;
            _surface?.Dispose();
            _surfaceBlend.Dispose();
            base.Dispose();
        }
    }
}
