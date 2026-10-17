using Client.Main.Graphics;
using Client.Main.Content;
using Client.Main.Controls.UI.Game.Common;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;

namespace Client.Main.Controls.UI
{
    public class RequestDialog : DialogControl
    {
        // ═══════════════════════════════════════════════════════════════
        // MODERN DARK THEME (muonline-ui-design skill)
        // ═══════════════════════════════════════════════════════════════
        private static class Theme
        {
            // Background layers (5 levels for depth)
            public static readonly Color BgDarkest = new(8, 10, 14, 252);
            public static readonly Color BgDark = new(16, 20, 26, 250);
            public static readonly Color BgMid = new(24, 30, 38, 248);
            public static readonly Color BgLight = new(35, 42, 52, 245);
            public static readonly Color BgLighter = new(48, 56, 68, 240);

            // Primary Accent - Warm Gold (MU signature color)
            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);
            public static readonly Color AccentGlow = new(255, 200, 80, 40);

            // Secondary accent - Cool Blue (optional)
            public static readonly Color Secondary = new(90, 140, 200);
            public static readonly Color SecondaryBright = new(130, 180, 240);
            public static readonly Color SecondaryDim = new(50, 80, 120);

            // Borders (3 levels)
            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);
            public static readonly Color BorderHighlight = new(100, 110, 130, 120);

            // Interactive Elements
            public static readonly Color SlotBg = new(12, 15, 20, 240);
            public static readonly Color SlotBorder = new(45, 52, 65, 180);
            public static readonly Color SlotHover = new(70, 85, 110, 150);
            public static readonly Color SlotSelected = new(212, 175, 85, 100);

            // Text (4 levels)
            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);
            public static readonly Color TextDark = new(100, 105, 115);

            // Status colors
            public static readonly Color Success = new(80, 200, 120);
            public static readonly Color Warning = new(240, 180, 60);
            public static readonly Color Danger = new(220, 80, 80);
        }

        private const int BASE_BG_WIDTH = 420;
        private const int BASE_BG_HEIGHT = 113;
        private const int SIDE_PAD = 28;
        private const int TOP_PAD = 30;
        private const int BTN_GAP = 10;

        private readonly LabelControl _label;
        private readonly ButtonControl _acceptButton;
        private readonly ButtonControl _rejectButton;

        public override bool BlocksPointerInput => true;
        private Texture2D _newUiFrame;
        private bool _choice;
        private Action _cancelChoice;
        private readonly BlendState _surfaceBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        private bool _infoOnly = false; // single OK button mode

        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private static readonly ILogger _logger
            = MuGame.AppLoggerFactory?.CreateLogger<RequestDialog>();

        public event EventHandler Accepted;
        public event EventHandler Rejected;

        private string _rawText = string.Empty;

        public string Text
        {
            get => _rawText;
            set
            {
                _rawText = value ?? string.Empty;
                UpdateWrappedText();
                AdjustSizeAndLayout();
            }
        }

        private RequestDialog()
        {
            Interactive = true; // consume mouse interactions over the dialog
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
            AutoViewSize = false;
            BorderThickness = 0;
            BackgroundColor = Color.Transparent;

            _label = new LabelControl
            {
                FontSize = 14f,
                TextColor = Theme.TextWhite,
                TextAlign = HorizontalAlign.Center
            };
            Controls.Add(_label);

            _acceptButton = MakeButton(
                text: "Accept",
                bg: Theme.BgDarkest,
                bgH: Color.Lerp(Theme.BgLight, Theme.AccentDim, 0.35f),
                bgP: Color.Lerp(Theme.BgDarkest, Theme.AccentDim, 0.55f),
                border: Theme.BorderInner,
                textColor: Theme.TextWhite,
                hoverTextColor: Theme.TextGold,
                disabledTextColor: Theme.TextDark);
            _acceptButton.Click += (s, e) => { Accepted?.Invoke(this, EventArgs.Empty); Close(); };
            Controls.Add(_acceptButton);

            _rejectButton = MakeButton(
                text: "Reject",
                bg: Theme.BgDarkest,
                bgH: Color.Lerp(Theme.BgLight, Theme.Danger, 0.25f),
                bgP: Color.Lerp(Theme.BgDarkest, Theme.Danger, 0.45f),
                border: Theme.BorderInner,
                textColor: Theme.TextWhite,
                hoverTextColor: Theme.TextGold,
                disabledTextColor: Theme.TextDark);
            _rejectButton.Click += (s, e) => { Rejected?.Invoke(this, EventArgs.Empty); Close(); };
            Controls.Add(_rejectButton);

            AdjustButtonSize(_acceptButton);
            AdjustButtonSize(_rejectButton);

            UpdateWrappedText();
            AdjustSizeAndLayout();
        }

        private void SetButtonLabels(string acceptText, string rejectText)
        {
            bool layoutNeedsUpdate = false;
            if (!string.IsNullOrWhiteSpace(acceptText) && !_acceptButton.Text.Equals(acceptText, StringComparison.Ordinal))
            {
                _acceptButton.Text = acceptText;
                AdjustButtonSize(_acceptButton);
                layoutNeedsUpdate = true;
            }

            if (!string.IsNullOrWhiteSpace(rejectText) && !_rejectButton.Text.Equals(rejectText, StringComparison.Ordinal))
            {
                _rejectButton.Text = rejectText;
                AdjustButtonSize(_rejectButton);
                layoutNeedsUpdate = true;
            }

            if (layoutNeedsUpdate)
            {
                AdjustSizeAndLayout();
            }
        }

        private static void AdjustButtonSize(ButtonControl button)
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null)
            {
                return;
            }

            float scale = button.FontSize / Constants.BASE_FONT_SIZE;
            float width = font.MeasureString(button.Text ?? string.Empty).X * scale;
            int paddedWidth = (int)Math.Ceiling(width) + 24;
            int height = button.ViewSize.Y;
            int finalWidth = Math.Max(paddedWidth, button.ViewSize.X);
            button.ViewSize = new Point(finalWidth, height);
            button.ControlSize = new Point(finalWidth, height);
        }

        private void SetInfoMode()
        {
            _infoOnly = true;
            _acceptButton.Text = "OK";
            AdjustButtonSize(_acceptButton);
            _rejectButton.Visible = false;
            AdjustSizeAndLayout();
        }

        private void AdjustSizeAndLayout()
        {
            int width = BASE_BG_WIDTH;

            int textHeight = _label.ControlSize.Y;
            int buttonsBlock = _acceptButton.ViewSize.Y + 20;
            int wantedH = TOP_PAD + textHeight + 20 + buttonsBlock;
            int height = Math.Max(BASE_BG_HEIGHT, wantedH);

            ControlSize = new Point(width, height);
            ViewSize = ControlSize;

            _label.X = (width - _label.ControlSize.X) / 2;
            _label.Y = TOP_PAD;

            int btnY = height - _acceptButton.ViewSize.Y - 20;
            if (_infoOnly)
            {
                // Center single OK button
                int startX = (width - _acceptButton.ViewSize.X) / 2;
                _acceptButton.X = startX;
                _acceptButton.Y = btnY;
            }
            else
            {
                int totalBtnsWidth = _acceptButton.ViewSize.X + _rejectButton.ViewSize.X + BTN_GAP;
                int startX = (width - totalBtnsWidth) / 2;
                _acceptButton.X = startX;
                _acceptButton.Y = btnY;
                _rejectButton.X = startX + _acceptButton.ViewSize.X + BTN_GAP;
                _rejectButton.Y = btnY;
            }

            InvalidateStaticSurface();
        }

        private void UpdateWrappedText()
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null) { _label.Text = _rawText; return; }

            float scale = _label.FontSize / Constants.BASE_FONT_SIZE;
            float maxWidth = BASE_BG_WIDTH - SIDE_PAD * 2;
            _label.Text = WrapMessage(_rawText, maxWidth, text => font.MeasureString(text).X * scale);
        }

        public static string WrapMessage(string text, float maxWidth, Func<string, float> measure)
        {
            var output = new StringBuilder();
            foreach (var paragraph in (text ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                if (output.Length > 0) output.Append('\n');
                var line = new StringBuilder();
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string next = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && measure(next) > maxWidth)
                    {
                        output.Append(line).Append('\n');
                        line.Clear();
                    }
                    if (line.Length > 0) line.Append(' ');
                    line.Append(word);
                }
                output.Append(line);
            }
            return output.ToString();
        }

        public override async Task Load()
        {
            _newUiFrame = await TextureLoader.Instance.PrepareAndGetTexture(NewUiFrameRenderer.PersonalShopAtlasPath);
            await base.Load();
            UpdateWrappedText();
            AdjustSizeAndLayout();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Visible && MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                if (_choice) _cancelChoice?.Invoke();
                else Rejected?.Invoke(this, EventArgs.Empty);
                Close();
            }
        }

        public static RequestDialog ShowChoice(string text, Action first, Action second, Action cancel, string firstText, string secondText)
        {
            var dialog = Show(text, first, second, firstText, secondText);
            if (dialog != null) { dialog._choice = true; dialog._cancelChoice = cancel; }
            return dialog;
        }

        private sealed class NewUiButton : ButtonControl
        {
            private Texture2D _normal, _hover, _pressed;
            public override async Task Load()
            {
                var loader = TextureLoader.Instance;
                _normal = await loader.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd");
                _hover = await loader.PrepareAndGetTexture("Interface/GFx/btn_l02.ozd");
                _pressed = await loader.PrepareAndGetTexture("Interface/GFx/btn_l03.ozd");
                BackgroundColor = HoverBackgroundColor = PressedBackgroundColor = Color.Transparent;
                BorderThickness = 0;
                await base.Load();
            }
            public override void Draw(GameTime gameTime)
            {
                if (!Visible) return;
                var sprite = GraphicsManager.Instance.Sprite;
                if (_normal != null)
                using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred, BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
                    sprite.Draw(IsMousePressed ? _pressed ?? _normal : IsMouseOver ? _hover ?? _normal : _normal,
                        DisplayRectangle, Enabled ? Color.White : Color.Gray);
                base.Draw(gameTime);
            }
        }

        private static ButtonControl MakeButton(string text,
                                                Color bg, Color bgH, Color bgP,
                                                Color border,
                                                Color textColor, Color hoverTextColor, Color disabledTextColor)
            => new NewUiButton
            {
                Text = text,
                FontSize = 12f,
                ViewSize = new Point(94, 38),
                ControlSize = new Point(94, 38),
                BackgroundColor = bg,
                HoverBackgroundColor = bgH,
                PressedBackgroundColor = bgP,
                BorderColor = border,
                BorderThickness = 1,
                TextColor = textColor,
                HoverTextColor = hoverTextColor,
                DisabledTextColor = disabledTextColor
            };

        public static RequestDialog Show(string text,
                                         Action onAccept = null,
                                         Action onReject = null,
                                         string acceptText = null,
                                         string rejectText = null)
        {
            var scene = MuGame.Instance?.ActiveScene;
            if (scene == null)
            {
                _logger?.LogDebug("[RequestDialog.Show] Error: ActiveScene is null.");
                return null;
            }

            foreach (var r in scene.Controls.OfType<RequestDialog>().ToList())
                r.Close();

            var dlg = new RequestDialog { Text = text };
            dlg.SetButtonLabels(acceptText, rejectText);
            if (onAccept != null) dlg.Accepted += (s, e) => onAccept();
            if (onReject != null) dlg.Rejected += (s, e) => onReject();

            dlg.ShowDialog();
            dlg.BringToFront();
            return dlg;
        }

        /// <summary>
        /// Shows a simple informational dialog with a centered OK button.
        /// </summary>
        public static RequestDialog ShowInfo(string text)
        {
            var scene = MuGame.Instance?.ActiveScene;
            if (scene == null)
            {
                _logger?.LogDebug("[RequestDialog.ShowInfo] Error: ActiveScene is null.");
                return null;
            }

            // Close existing dialogs
            foreach (var r in scene.Controls.OfType<RequestDialog>().ToList())
                r.Close();

            var dlg = new RequestDialog { Text = text };
            dlg.SetInfoMode();
            dlg.ShowDialog();
            dlg.BringToFront();
            return dlg;
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible) return;

            EnsureStaticSurface();

            var sb = GraphicsManager.Instance.Sprite;
            using var scope = new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend, GraphicsManager.GetQualityLinearSamplerState(), transform: UiScaler.SpriteTransform);

            sb.Draw(_staticSurface, DisplayRectangle, Color.White * Alpha);

            for (int i = 0; i < Controls.Count; i++)
            {
                Controls[i].Draw(gameTime);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _staticSurface?.Dispose();
            _staticSurface = null;
            _surfaceBlend.Dispose();
        }

        // Consume clicks anywhere on the dialog (background or label), so they don't reach the world.
        public override bool OnClick()
        {
            return true; // don't propagate; buttons will handle their own click events
        }

        private void EnsureStaticSurface()
        {
            if (!_staticSurfaceDirty && _staticSurface != null && !_staticSurface.IsDisposed)
            {
                return;
            }

            var gd = GraphicsManager.Instance?.GraphicsDevice;
            if (gd == null || ControlSize.X <= 0 || ControlSize.Y <= 0)
            {
                return;
            }

            _staticSurface?.Dispose();
            _staticSurface = new RenderTarget2D(gd, ControlSize.X, ControlSize.Y, false, SurfaceFormat.Color, DepthFormat.None);

            var prevTargets = gd.GetRenderTargets();
            gd.SetRenderTarget(_staticSurface);
            gd.Clear(Color.Transparent);

            var sb = GraphicsManager.Instance.Sprite;
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, _surfaceBlend))
            {
                DrawStaticElements(sb);
            }

            gd.SetRenderTargets(prevTargets);
            _staticSurfaceDirty = false;
        }

        private void InvalidateStaticSurface() => _staticSurfaceDirty = true;

        private void DrawStaticElements(SpriteBatch sb)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null)
            {
                return;
            }

            var windowRect = new Rectangle(0, 0, ControlSize.X, ControlSize.Y);
            sb.Draw(pixel, windowRect, new Color(20, 18, 16));
            if (_newUiFrame != null)
                NewUiFrameRenderer.DrawPersonalShopInset(sb, _newUiFrame, windowRect);
        }

        private void DrawWindowBackground(SpriteBatch sb, Rectangle rect)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            sb.Draw(pixel, rect, Theme.BorderOuter);

            var inner = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            UiDrawHelper.DrawVerticalGradient(sb, inner, Theme.BgDark, Theme.BgDarkest);

            sb.Draw(pixel, new Rectangle(inner.X, inner.Y, inner.Width, 1), Theme.BorderInner * 0.5f);
            UiDrawHelper.DrawCornerAccents(sb, rect, Theme.Accent * 0.35f, size: 10);
        }

        private static void DrawPanel(SpriteBatch sb, Rectangle rect, Color bg, bool withGlow = false)
        {
            UiDrawHelper.DrawPanel(sb, rect, bg,
                Theme.BorderInner,
                Theme.BorderOuter,
                Theme.BorderHighlight * 0.3f,
                withGlow,
                withGlow ? Theme.Accent * 0.15f : null);
        }
    }
}
