using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Season 6 personal-store window, using the existing GFx NewUI frame.
    /// Phase A deliberately has no shop items, prices or network requests.
    /// Future phases should supply confirmed store data, not invent local items.
    /// </summary>
    public sealed class PersonalStoreControl : UIControl, IUiTexturePreloadable
    {
        public const int Columns = 8;
        public const int Rows = 4;
        public const int SlotCount = Columns * Rows;
        public const int CellSize = GameWindowMetrics.InventoryCell;

        private const int WindowWidth = GameWindowMetrics.SecondaryWidth;
        private const int WindowHeight = GameWindowMetrics.SecondaryHeight;

        private const string FramePath = NewUiFrameRenderer.PersonalShopAtlasPath;
        private const string SlotPath = "Interface/GFx/NpcShop_I3.ozd";
        private const string ButtonNormalPath = "Interface/GFx/btn_l01.ozd";
        private const string ButtonHoverPath = "Interface/GFx/btn_l02.ozd";
        private const string ButtonPressedPath = "Interface/GFx/btn_l03.ozd";
        private const string CloseNormalPath = "Interface/GFx/btn_close01.ozd";
        private const string CloseHoverPath = "Interface/GFx/btn_close02.ozd";
        private const string ClosePressedPath = "Interface/GFx/btn_close03.ozd";

        // Same verified cell crop used by Inventory and Resurrect Spirit.
        private static readonly Rectangle SlotSource = new(546, 220, 29, 29);
        private static readonly Rectangle NameFrame = new(18, 103, 312, 86);
        private static readonly Rectangle GridFrame = new(18, 199, 312, 191);
        private static readonly Rectangle ItemGrid = new(
            (WindowWidth - Columns * CellSize) / 2, 249,
            Columns * CellSize, Rows * CellSize);
        private static readonly Rectangle InfoFrame = new(18, 402, 312, 129);
        private static readonly Rectangle OpenButton = new(25, 542, 142, 35);
        private static readonly Rectangle CloseStoreButton = new(181, 542, 142, 35);
        private static readonly Rectangle ExitButton = new(105, 584, 138, 33);
        private static readonly Rectangle CloseButton = new(WindowWidth - 44, 35, 25, 25);

        private static PersonalStoreControl _instance;

        private Texture2D _frameAtlas;
        private Texture2D _slotAtlas;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _buttonPressed;
        private Texture2D _closeNormal;
        private Texture2D _closeHover;
        private Texture2D _closePressed;
        private SpriteFont _font;

        private RenderTarget2D _staticSurface;
        private bool _surfaceDirty = true;
        // GFx OZD has straight alpha; the cached surface must accumulate alpha
        // normally before being composited with premultiplied AlphaBlend.
        private readonly BlendState _surfaceBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

        private bool _closeHovered;
        private bool _exitHovered;

        private PersonalStoreControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.None;
        }

        public static PersonalStoreControl Instance => _instance ??= new PersonalStoreControl();
        public static bool IsOpen => _instance?.Visible == true;
        public override bool NonDisposable => true;

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return FramePath;
            yield return SlotPath;
            yield return ButtonNormalPath;
            yield return ButtonHoverPath;
            yield return ButtonPressedPath;
            yield return CloseNormalPath;
            yield return CloseHoverPath;
            yield return ClosePressedPath;
        }

        public override async Task Load()
        {
            await base.Load();
            var loader = TextureLoader.Instance;
            var textures = await Task.WhenAll(
                loader.PrepareAndGetTexture(FramePath),
                loader.PrepareAndGetTexture(SlotPath),
                loader.PrepareAndGetTexture(ButtonNormalPath),
                loader.PrepareAndGetTexture(ButtonHoverPath),
                loader.PrepareAndGetTexture(ButtonPressedPath),
                loader.PrepareAndGetTexture(CloseNormalPath),
                loader.PrepareAndGetTexture(CloseHoverPath),
                loader.PrepareAndGetTexture(ClosePressedPath));

            _frameAtlas = textures[0];
            _slotAtlas = textures[1];
            _buttonNormal = textures[2];
            _buttonHover = textures[3];
            _buttonPressed = textures[4];
            _closeNormal = textures[5];
            _closeHover = textures[6];
            _closePressed = textures[7];
            _font = GraphicsManager.Instance.Font;
            _surfaceDirty = true;
        }

        public void ShowWindow()
        {
            var inventory = InventoryControl.Instance;
            if (inventory?.Visible != true)
            {
                return;
            }

            // Use the same parent-space positioning as Resurrect Spirit.
            // Inventory remains visible and retains its own controls.
            Align = ControlAlign.None;
            X = Math.Max(12, inventory.X - WindowWidth - 12);
            Y = Math.Max(12, inventory.Y);
            if (Parent != null)
            {
                X = Math.Min(X, Math.Max(12, Parent.DisplaySize.X - DisplaySize.X - 12));
                Y = Math.Min(Y, Math.Max(12, Parent.DisplaySize.Y - DisplaySize.Y - 12));
            }

            if (!Visible)
            {
                SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
            }

            Visible = true;
            BringToFront();
            if (Scene != null)
            {
                Scene.FocusControl = this;
            }
        }

        public void CloseWindow()
        {
            if (!Visible)
            {
                return;
            }

            Visible = false;
            _closeHovered = false;
            _exitHovered = false;
            if (Scene?.FocusControl == this)
            {
                Scene.FocusControl = null;
            }

            // Closing this panel never sends a close-store request.
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
        }

        /// <summary>
        /// Returns a zero-based visual slot (0..31). The OpenMU storage mapping
        /// is intentionally not assumed by this visual-only phase.
        /// </summary>
        public bool TryGetVisualSlot(Point mouse, out int slot)
        {
            slot = -1;
            if (!Visible || Scale <= 0f)
            {
                return false;
            }

            float localX = (mouse.X - DisplayRectangle.X) / Scale - ItemGrid.X;
            float localY = (mouse.Y - DisplayRectangle.Y) / Scale - ItemGrid.Y;
            if (localX < 0 || localY < 0 ||
                localX >= ItemGrid.Width || localY >= ItemGrid.Height)
            {
                return false;
            }

            int column = (int)localX / CellSize;
            int row = (int)localY / CellSize;
            slot = row * Columns + column;
            return slot >= 0 && slot < SlotCount;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }

            // In this phase the empty store is paired with Inventory; do not
            // leave a disconnected preview behind after the player closes I.
            if (InventoryControl.Instance?.Visible != true)
            {
                CloseWindow();
                return;
            }

            base.Update(gameTime);
            var current = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool inside = DisplayRectangle.Contains(current.Position);
            _closeHovered = inside && ToScreen(CloseButton).Contains(current.Position);
            _exitHovered = inside && ToScreen(ExitButton).Contains(current.Position);

            if (inside && current.LeftButton == ButtonState.Pressed)
            {
                Scene?.SetMouseInputConsumed();
            }

            bool leftJustReleased = current.LeftButton == ButtonState.Released &&
                                    previous.LeftButton == ButtonState.Pressed;
            if (leftJustReleased && (_closeHovered || _exitHovered))
            {
                CloseWindow();
            }
            // Open Store, Close Store, name and slots are intentionally inert.
            // Server transactions and draggable items belong to phases B/C.
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready)
            {
                return;
            }

            var sprite = GraphicsManager.Instance?.Sprite;
            if (sprite == null)
            {
                return;
            }

            EnsureStaticSurface();
            if (_staticSurface != null && !_staticSurface.IsDisposed)
            {
                using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                    BlendState.AlphaBlend, SamplerState.PointClamp,
                    transform: UiScaler.SpriteTransform))
                {
                    sprite.Draw(_staticSurface, DisplayRectangle, Color.White * Alpha);
                }
            }

            using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                DrawExitButton(sprite);
                DrawCloseButton(sprite);
            }
        }

        private void EnsureStaticSurface()
        {
            if (!_surfaceDirty && _staticSurface != null && !_staticSurface.IsDisposed)
            {
                return;
            }

            var device = GraphicsManager.Instance?.GraphicsDevice;
            if (device == null)
            {
                return;
            }

            _staticSurface?.Dispose();
            _staticSurface = new RenderTarget2D(device, WindowWidth, WindowHeight,
                false, SurfaceFormat.Color, DepthFormat.None);
            var previousTargets = device.GetRenderTargets();
            try
            {
                device.SetRenderTarget(_staticSurface);
                device.Clear(Color.Transparent);
                using (new SpriteBatchScope(GraphicsManager.Instance.Sprite,
                    SpriteSortMode.Deferred, _surfaceBlend, SamplerState.PointClamp))
                {
                    DrawStaticSurface(GraphicsManager.Instance.Sprite);
                }
            }
            finally
            {
                device.SetRenderTargets(previousTargets);
            }

            _surfaceDirty = false;
        }

        private void DrawStaticSurface(SpriteBatch sprite)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null)
            {
                return;
            }

            // Underlay prevents holes in translucent atlas pixels.
            sprite.Draw(pixel, new Rectangle(12, 90, WindowWidth - 24, WindowHeight - 112),
                new Color(25, 23, 23, 255));
            NewUiFrameRenderer.DrawPersonalShopWindow(sprite, _frameAtlas,
                new Rectangle(0, 0, WindowWidth, WindowHeight));
            NewUiFrameRenderer.DrawPersonalShopInset(sprite, _frameAtlas, NameFrame);
            NewUiFrameRenderer.DrawPersonalShopInset(sprite, _frameAtlas, GridFrame);
            NewUiFrameRenderer.DrawPersonalShopInset(sprite, _frameAtlas, InfoFrame);

            DrawCenteredText(sprite, "PERSONAL STORE", 52, 0.53f, new Color(233, 219, 195));
            DrawCenteredText(sprite, "STORE NAME", 124, 0.42f, new Color(221, 206, 179));
            var nameBox = new Rectangle(NameFrame.X + 19, 143, NameFrame.Width - 38, 31);
            sprite.Draw(pixel, nameBox, new Color(18, 17, 18, 238));
            sprite.Draw(pixel, new Rectangle(nameBox.X, nameBox.Y, nameBox.Width, 1),
                new Color(103, 87, 72, 180));
            DrawCenteredText(sprite, "Name entry in phase C", 158, 0.37f, new Color(149, 142, 134));

            DrawCenteredText(sprite, "SHOP ITEMS (8 x 4)", 222, 0.43f,
                new Color(221, 206, 179));
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var destination = new Rectangle(
                        ItemGrid.X + column * CellSize,
                        ItemGrid.Y + row * CellSize, CellSize, CellSize);
                    sprite.Draw(pixel, destination, new Color(17, 20, 25, 255));
                    if (_slotAtlas != null && !_slotAtlas.IsDisposed)
                    {
                        sprite.Draw(_slotAtlas, destination, SlotSource, Color.White);
                    }
                }
            }

            DrawCenteredText(sprite, "ITEM INFORMATION", 425, 0.43f,
                new Color(221, 206, 179));
            DrawCenteredText(sprite, "Selected item: none", 457, 0.40f,
                new Color(169, 161, 153));
            DrawCenteredText(sprite, "Price: -- Zen", 482, 0.42f,
                new Color(221, 195, 134));
            DrawCenteredText(sprite, "Trading not connected (phase A)", 511, 0.35f,
                new Color(169, 141, 125));

            // Visible but explicitly disabled until OpenMU responses are wired.
            DrawThreeSlice(sprite, _buttonNormal, OpenButton,
                new Color(110, 103, 98, 160));
            DrawThreeSlice(sprite, _buttonNormal, CloseStoreButton,
                new Color(110, 103, 98, 160));
            DrawTextInRect(sprite, "Open Store", OpenButton, 0.42f,
                new Color(150, 144, 137));
            DrawTextInRect(sprite, "Close Store", CloseStoreButton, 0.42f,
                new Color(150, 144, 137));
        }

        private void DrawExitButton(SpriteBatch sprite)
        {
            var texture = _exitHovered
                ? (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    ? _buttonPressed ?? _buttonHover ?? _buttonNormal
                    : _buttonHover ?? _buttonNormal)
                : _buttonNormal;
            DrawThreeSlice(sprite, texture, ToScreen(ExitButton), Color.White * Alpha);
            DrawTextInRect(sprite, "Exit", ToScreen(ExitButton), 0.45f * Scale,
                new Color(237, 221, 196) * Alpha);
        }

        private void DrawCloseButton(SpriteBatch sprite)
        {
            var texture = _closeHovered
                ? (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    ? _closePressed ?? _closeHover ?? _closeNormal
                    : _closeHover ?? _closeNormal)
                : _closeNormal;
            if (texture != null && !texture.IsDisposed)
            {
                sprite.Draw(texture, ToScreen(CloseButton), Color.White * Alpha);
            }
        }

        private static void DrawThreeSlice(SpriteBatch sprite, Texture2D texture,
            Rectangle target, Color tint)
        {
            if (texture == null || texture.IsDisposed)
            {
                return;
            }

            int sourceCap = Math.Max(1, Math.Min(16, texture.Width / 4));
            int cap = Math.Max(1, (int)MathF.Round(sourceCap * (float)target.Height / texture.Height));
            if (target.Width <= 2 * cap)
            {
                sprite.Draw(texture, target, tint);
                return;
            }

            sprite.Draw(texture, new Rectangle(target.X, target.Y, cap, target.Height),
                new Rectangle(0, 0, sourceCap, texture.Height), tint);
            sprite.Draw(texture, new Rectangle(target.X + cap, target.Y,
                    target.Width - 2 * cap, target.Height),
                new Rectangle(sourceCap, 0, texture.Width - 2 * sourceCap, texture.Height), tint);
            sprite.Draw(texture, new Rectangle(target.Right - cap, target.Y, cap, target.Height),
                new Rectangle(texture.Width - sourceCap, 0, sourceCap, texture.Height), tint);
        }

        private void DrawCenteredText(SpriteBatch sprite, string text, int centerY,
            float fontScale, Color color)
        {
            DrawTextInRect(sprite, text, new Rectangle(0, centerY - 15, WindowWidth, 30),
                fontScale, color);
        }

        private void DrawTextInRect(SpriteBatch sprite, string text, Rectangle rect,
            float fontScale, Color color)
        {
            if (_font == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            Vector2 measure = _font.MeasureString(text);
            if (measure.X <= 0f || measure.Y <= 0f)
            {
                return;
            }

            float size = Math.Min(fontScale, (rect.Width - 8f) / measure.X);
            Vector2 scaled = measure * size;
            var origin = new Vector2(
                rect.X + (rect.Width - scaled.X) * 0.5f,
                rect.Y + (rect.Height - scaled.Y) * 0.5f);
            sprite.DrawString(_font, text, origin + Vector2.One,
                Color.Black * 0.65f, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
            sprite.DrawString(_font, text, origin,
                color, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
        }

        private Rectangle ToScreen(Rectangle local)
        {
            int left = DisplayRectangle.X + (int)MathF.Round(local.Left * Scale);
            int top = DisplayRectangle.Y + (int)MathF.Round(local.Top * Scale);
            int right = DisplayRectangle.X + (int)MathF.Round(local.Right * Scale);
            int bottom = DisplayRectangle.Y + (int)MathF.Round(local.Bottom * Scale);
            return new Rectangle(left, top,
                Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            _surfaceDirty = true;
        }

        public override void Dispose()
        {
            base.Dispose();
            _staticSurface?.Dispose();
            _staticSurface = null;
            _surfaceBlend.Dispose();
        }
    }
}
