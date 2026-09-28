using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Client.Main;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI;
using Client.Main.Models;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    public class NpcShopControl : UIControl, IUiTexturePreloadable
    {
        // ═══════════════════════════════════════════════════════════════
        // SHOP MODE
        // ═══════════════════════════════════════════════════════════════
        public enum ShopMode
        {
            BuyAndSell = 1,
            Repair = 2
        }

        // ═══════════════════════════════════════════════════════════════
        // CLASSIC GFx LAYOUT (pre-redesign Naffis)
        // ═══════════════════════════════════════════════════════════════
        private const string LayoutJsonResource =
            "Client.Main.Controls.UI.Game.Layouts.NpcShopLayout.json";

        private const string TextureRectJsonResource =
            "Client.Main.Controls.UI.Game.Layouts.NpcShopRect.json";

        private const string LayoutTexturePath =
            "Interface/GFx/NpcShop_I3.ozd";

        private const int SHOP_COLUMNS = 8;

        // Conservamos las 15 filas de tu implementación actual.
        private const int SHOP_ROWS = 15;

        // Tamaño elegido para los slots/items.
        private const int SHOP_SQUARE_WIDTH = 32;
        private const int SHOP_SQUARE_HEIGHT = 32;

        // El layout original de Naffis estaba construido para celdas de 25 px.
        // Escalamos TODO el skin clásico en la misma proporción que la grilla:
        // 32 / 25 = 1.28. Así fondo, grilla, botones e hitboxes vuelven a coincidir.
        private const float CLASSIC_UI_SCALE =
            SHOP_SQUARE_WIDTH / 25f;

        // Bounding box real de NpcShopLayout.json:
        // X 148..423 / Y 89..624.
        private const int CLASSIC_ORIGIN_X = 148;
        private const int CLASSIC_ORIGIN_Y = 89;
        private const int CLASSIC_PANEL_WIDTH = 275;
        private const int CLASSIC_PANEL_HEIGHT = 535;
        private const int PANEL_MARGIN = 8;

        private static readonly int WINDOW_WIDTH =
            PANEL_MARGIN * 2 +
            (int)MathF.Ceiling(
                CLASSIC_PANEL_WIDTH * CLASSIC_UI_SCALE);

        private static readonly int WINDOW_HEIGHT =
            PANEL_MARGIN * 2 +
            (int)MathF.Ceiling(
                CLASSIC_PANEL_HEIGHT * CLASSIC_UI_SCALE);

        // El crop antiguo 29x31 contiene margen/borde extra.
        // Usamos únicamente la celda interior real de 25x25 para evitar
        // la doble rejilla (línea exterior + línea interior desplazada).
        private static readonly Rectangle SlotSourceRect =
            new(548, 223, 25, 25);

        
        private static readonly int GRID_WIDTH =
            SHOP_COLUMNS * SHOP_SQUARE_WIDTH;

        private static readonly int GRID_HEIGHT =
            SHOP_ROWS * SHOP_SQUARE_HEIGHT;

        // Posición LÓGICA del grid: items, hover, clicks e hitboxes.
        // No usar estos valores para corregir el dibujo blanco del grid.
        private const int GRID_LOGIC_OFFSET_X = 4;
        private const int GRID_LOGIC_OFFSET_Y = 5;

        // La celda visual ahora es exactamente 25x25 en origen y se escala
        // al mismo 32x32 que la lógica. No necesita desplazamiento adicional.
        private const int GRID_ART_OFFSET_X = 0;
        private const int GRID_ART_OFFSET_Y = 0;

        private int WindowHeight => WINDOW_HEIGHT;
        // ═══════════════════════════════════════════════════════════════
        // UI COLORS / CLASSIC SHOP HELPERS
        // ═══════════════════════════════════════════════════════════════
        private static class Theme
        {
            public static readonly Color BgLight = new(35, 42, 52, 245);

            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);

            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);

            public static readonly Color SlotHover = new(70, 85, 110, 150);

            public static readonly Color GlowNormal = new(150, 150, 150, 25);
            public static readonly Color GlowMagic = new(100, 150, 255, 50);
            public static readonly Color GlowExcellent = new(120, 255, 120, 60);
            public static readonly Color GlowAncient = new(80, 200, 255, 70);
            public static readonly Color GlowLegendary = new(255, 180, 80, 70);

            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);

            // Colores temporales más acordes al skin clásico GFx.
            // Más adelante se pueden reemplazar por sprites originales.
            public static readonly Color ClassicButtonBg = new(48, 31, 25, 245);
            public static readonly Color ClassicButtonHover = new(72, 42, 30, 245);
            public static readonly Color ClassicButtonActive = new(100, 48, 30, 245);
            public static readonly Color ClassicButtonBorder = new(112, 76, 46, 220);
            public static readonly Color ClassicButtonBorderHover = new(178, 122, 70, 240);
        }

        private static readonly ItemGlowPalette GlowPalette = new(
            Theme.GlowNormal,
            Theme.GlowMagic,
            Theme.GlowExcellent,
            Theme.GlowAncient,
            Theme.GlowLegendary);
        private readonly struct LayoutInfo
        {
            public string Name { get; init; }
            public float ScreenX { get; init; }
            public float ScreenY { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public int Z { get; init; }
        }

        private readonly struct TextureRectData
        {
            public string Name { get; init; }
            public int X { get; init; }
            public int Y { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
        }

        private static NpcShopControl _instance;

        private readonly List<InventoryItem> _items = new();
        private readonly Dictionary<string, Texture2D> _itemTextureCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(InventoryItem item, int width, int height, bool animated), Texture2D> _bmdPreviewCache = new();

        private readonly List<LayoutInfo> _layoutInfos = new();

        private readonly Dictionary<string, TextureRectData> _textureRectLookup =
            new(StringComparer.OrdinalIgnoreCase);


        private Texture2D _layoutTexture;
        private Texture2D _slotTexture;

        private Rectangle _headerRect;
        private Rectangle _gridRect;
        private Rectangle _buttonAreaRect;
        private Rectangle _footerRect;
        private Rectangle _closeButtonRect;
        private Rectangle _repairButtonRect;
        private Rectangle _repairAllButtonRect;
        private bool _repairButtonHovered;
        private bool _repairAllButtonHovered;

        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private SpriteFont _font;
        private CharacterState _characterState;

        private InventoryItem _hoveredItem;
        private Point _hoveredSlot = new(-1, -1);
        private GameTime _currentGameTime;

        private bool _wasVisible;
        private bool _closeRequestSent;
        private bool _closeHovered;
        private bool _pendingShow;
        private bool _warmupComplete;

        // Drag support
        private bool _isDragging;
        private Point _dragOffset;
        private DateTime _lastClickTime = DateTime.MinValue;

        // Repair mode
        private ShopMode _shopMode = ShopMode.BuyAndSell;
        private bool _isRepairShop = false;

        private NpcShopControl()
        {
            LoadLayoutDefinitions();
            BuildLayoutMetrics();

            ControlSize = new Point(WINDOW_WIDTH, WindowHeight);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.VerticalCenter | ControlAlign.Left;

            EnsureCharacterState();
        }

        public override bool NonDisposable => true;
        public static NpcShopControl Instance => _instance ??= new NpcShopControl();
        public static bool IsOpen => _instance?.Visible == true;

        /// <summary>
        /// Forces immediate position calculation based on Align property.
        /// Call this before showing the control to prevent position flickering.
        /// </summary>
        private void ForceAlignNow()
        {
            if (Parent == null || Align == ControlAlign.None)
                return;

            const int padding = 20;

            if (Align.HasFlag(ControlAlign.Top))
                Y = padding;
            else if (Align.HasFlag(ControlAlign.Bottom))
                Y = Parent.DisplaySize.Y - DisplaySize.Y - padding;
            else if (Align.HasFlag(ControlAlign.VerticalCenter))
                Y = (Parent.DisplaySize.Y / 2) - (DisplaySize.Y / 2);

            if (Align.HasFlag(ControlAlign.Left))
                X = padding;
            else if (Align.HasFlag(ControlAlign.Right))
                X = Parent.DisplaySize.X - DisplaySize.X - padding;
            else if (Align.HasFlag(ControlAlign.HorizontalCenter))
                X = (Parent.DisplaySize.X / 2) - (DisplaySize.X / 2);
        }

        private static int ScaleClassicX(float sourceX)
            => PANEL_MARGIN +
               (int)MathF.Round(
                   (sourceX - CLASSIC_ORIGIN_X) *
                   CLASSIC_UI_SCALE);

        private static int ScaleClassicY(float sourceY)
            => PANEL_MARGIN +
               (int)MathF.Round(
                   (sourceY - CLASSIC_ORIGIN_Y) *
                   CLASSIC_UI_SCALE);

        private static int ScaleClassicSize(float value)
            => (int)MathF.Round(
                value * CLASSIC_UI_SCALE);

        private void BuildLayoutMetrics()
        {
            // Coordenadas originales del renderer pre-redesign:
            // TopCorner 149.3,89 / grid 170,180.
            // El skin completo se escala con CLASSIC_UI_SCALE, mientras que
            // estos offsets de pocos píxeles corrigen únicamente el calce
            // visual del grid con el dibujo del Board.

            _headerRect = new Rectangle(
                ScaleClassicX(149.3f),
                ScaleClassicY(89f),
                ScaleClassicSize(273f),
                ScaleClassicSize(70f));

            _gridRect = new Rectangle(
                ScaleClassicX(170f) + GRID_LOGIC_OFFSET_X,
                ScaleClassicY(180f) + GRID_LOGIC_OFFSET_Y,
                GRID_WIDTH,
                GRID_HEIGHT);

// A partir de aquí el layout inferior se calcula RELATIVO al grid.
            // Así el tamaño de los slots puede cambiar sin que Repair/Footer
            // vuelvan a salir del panel.
            int lowerGap = ScaleClassicSize(1f);

            int buttonAreaHeight =
                _isRepairShop
                    ? ScaleClassicSize(21f)
                    : 0;

            _buttonAreaRect = new Rectangle(
                _gridRect.X,
                _gridRect.Bottom + lowerGap,
                _gridRect.Width,
                buttonAreaHeight);

            int footerY =
                _isRepairShop
                    ? _buttonAreaRect.Bottom + ScaleClassicSize(3f)
                    : _gridRect.Bottom + lowerGap;

            _footerRect = new Rectangle(
                _gridRect.X,
                footerY,
                _gridRect.Width,
                ScaleClassicSize(18f));

            // Botones más compactos y centrados en el ancho del grid.
            int sidePadding = ScaleClassicSize(6f);
            int repairGap = ScaleClassicSize(6f);

            int repairButtonWidth =
                (_buttonAreaRect.Width -
                 sidePadding * 2 -
                 repairGap) / 2;

            int repairButtonHeight =
                ScaleClassicSize(18f);

            int repairButtonY =
                _buttonAreaRect.Y +
                Math.Max(
                    0,
                    (_buttonAreaRect.Height -
                     repairButtonHeight) / 2);

            _repairButtonRect = new Rectangle(
                _buttonAreaRect.X + sidePadding,
                repairButtonY,
                repairButtonWidth,
                repairButtonHeight);

            _repairAllButtonRect = new Rectangle(
                _repairButtonRect.Right + repairGap,
                repairButtonY,
                repairButtonWidth,
                repairButtonHeight);

            _closeButtonRect = new Rectangle(
                ScaleClassicX(392f),
                ScaleClassicY(98f),
                ScaleClassicSize(20f),
                ScaleClassicSize(20f));
        }

        public override async System.Threading.Tasks.Task Load()
        {
            await base.Load();

            var loader = TextureLoader.Instance;

            _layoutTexture =
                await loader.PrepareAndGetTexture(
                    LayoutTexturePath);

            _slotTexture = _layoutTexture;

            _font = GraphicsManager.Instance.Font;

            InvalidateStaticSurface();
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return LayoutTexturePath;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            EnsureCharacterState();

            // Handle deferred show - wait one frame after warmup to avoid black screen
            if (_pendingShow && !Visible)
            {
                if (_warmupComplete)
                {
                    // Warmup done in previous frame, now safe to show
                    Visible = true;
                    BringToFront();
                    SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
                    _pendingShow = false;
                    _warmupComplete = false;
                }
                else
                {
                    // Do warmup this frame, show next frame
                    WarmupTexturesSync();
                    InvalidateStaticSurface();
                    EnsureStaticSurface();
                    _warmupComplete = true;
                }
            }

            if (Visible)
            {
                _currentGameTime = gameTime;

                if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                    MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
                {
                    Visible = false;
                    HandleVisibilityLost();
                    _wasVisible = false;
                    return;
                }

                // Handle 'L' key for repair mode toggle (only if repair shop and no dragged item)
                if (_isRepairShop &&
                    MuGame.Instance.Keyboard.IsKeyDown(Keys.L) &&
                    MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.L))
                {
                    // Only toggle if not dragging an item
                    if (InventoryControl.Instance?.GetDraggedItem() == null)
                    {
                        ToggleRepairMode();
                        SoundController.Instance.PlayBuffer("Sound/iButton.wav");
                    }
                }

                Point mousePos = MuGame.Instance.UiMouseState.Position;
                bool leftPressed = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
                bool leftJustPressed = leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
                bool leftJustReleased = !leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Pressed;

                UpdateChromeHover(mousePos);

                // Handle close button
                if (leftJustPressed && _closeHovered)
                {
                    Visible = false;
                    HandleVisibilityLost();
                    return;
                }

                // Handle repair buttons (only if repair shop)
                if (_isRepairShop && leftJustPressed)
                {
                    if (_repairButtonHovered)
                    {
                        // Toggle repair mode
                        ToggleRepairMode();
                        SoundController.Instance.PlayBuffer("Sound/iButton.wav");
                        return;
                    }
                    else if (_repairAllButtonHovered)
                    {
                        // Repair all items
                        var svc = MuGame.Network?.GetCharacterService();
                        if (svc != null)
                        {
                            _ = svc.SendRepairItemRequestAsync(0xFF, false); // 0xFF = repair all
                            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
                        }
                        return;
                    }
                }

                // Handle window dragging
                if (leftJustPressed && IsMouseOverDragArea(mousePos) && !_isDragging)
                {
                    DateTime now = DateTime.Now;
                    if ((now - _lastClickTime).TotalMilliseconds < 500)
                    {
                        // Double-click to reset position
                        Align = ControlAlign.None;
                        _lastClickTime = DateTime.MinValue;
                    }
                    else
                    {
                        _isDragging = true;
                        _dragOffset = new Point(mousePos.X - X, mousePos.Y - Y);
                        Align = ControlAlign.None;
                        _lastClickTime = now;
                    }
                }
                else if (leftJustReleased && _isDragging)
                {
                    _isDragging = false;
                }
                else if (_isDragging && leftPressed)
                {
                    X = mousePos.X - _dragOffset.X;
                    Y = mousePos.Y - _dragOffset.Y;
                }

                if (!_isDragging)
                {
                    UpdateHoverState();
                    HandleMouseInput();
                }
            }
            else if (_wasVisible)
            {
                HandleVisibilityLost();
            }

            _wasVisible = Visible;
        }

        private bool IsMouseOverDragArea(Point mousePos)
        {
            Rectangle headerScreen = Translate(_headerRect);
            Rectangle closeScreen = Translate(_closeButtonRect);
            return headerScreen.Contains(mousePos) && !closeScreen.Contains(mousePos);
        }

        private void UpdateChromeHover(Point mousePos)
        {
            var closeRect = Translate(_closeButtonRect);
            _closeHovered = closeRect.Contains(mousePos);

            // Handle repair button hover (only show if repair shop)
            if (_isRepairShop)
            {
                var repairRect = Translate(_repairButtonRect);
                var repairAllRect = Translate(_repairAllButtonRect);
                _repairButtonHovered = repairRect.Contains(mousePos);
                _repairAllButtonHovered = repairAllRect.Contains(mousePos);
            }
            else
            {
                _repairButtonHovered = false;
                _repairAllButtonHovered = false;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible) return;

            EnsureStaticSurface();

            var gm = GraphicsManager.Instance;
            var spriteBatch = gm?.Sprite;
            if (spriteBatch == null) return;

            SpriteBatchScope? scope = null;

            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    transform: UiScaler.SpriteTransform);
            }

            try
            {
                if (_staticSurface != null &&
                    !_staticSurface.IsDisposed)
                {
                    spriteBatch.Draw(
                        _staticSurface,
                        DisplayRectangle,
                        Color.White * Alpha);
                }

                // Título dinámico.
                DrawShopTitle(spriteBatch);

                var pixel = GraphicsManager.Instance.Pixel;

                ItemGridRenderHelper.DrawGridOverlays(
                    spriteBatch,
                    pixel,
                    DisplayRectangle,
                    _gridRect,
                    _hoveredItem,
                    _hoveredSlot,
                    SHOP_SQUARE_WIDTH,
                    SHOP_SQUARE_HEIGHT,
                    Theme.SlotHover,
                    Theme.Accent,
                    Alpha);

                DrawShopItems(spriteBatch);

                DrawCloseButton(spriteBatch);

                // IMPORTANTE:
                // Estos son los dos botones Repair item / Repair all.
                if (_isRepairShop)
                {
                    DrawRepairButtons(spriteBatch);
                }
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || _hoveredItem == null) return;

            var gm = GraphicsManager.Instance;
            var spriteBatch = gm?.Sprite;
            if (spriteBatch == null) return;

            SpriteBatchScope? scope = null;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend, transform: UiScaler.SpriteTransform);
            }

            try
            {
                DrawTooltip(spriteBatch);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_characterState != null)
            {
                _characterState.ShopItemsChanged -= RefreshShopContent;
                _characterState = null;
            }

            _staticSurface?.Dispose();
            _staticSurface = null;
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            InvalidateStaticSurface();
        }

        // ═══════════════════════════════════════════════════════════════
        // DRAWING PRIMITIVES
        // ═══════════════════════════════════════════════════════════════


        private void DrawPanel(SpriteBatch spriteBatch, Rectangle rect, Color bgColor, bool withBorder = true)
        {
            UiDrawHelper.DrawPanel(spriteBatch, rect, bgColor,
                withBorder ? Theme.BorderInner * 0.8f : (Color?)null,
                withBorder ? Theme.BorderOuter : (Color?)null,
                withBorder ? Theme.BorderInner * 0.6f : null);
        }


        // ═══════════════════════════════════════════════════════════════
        // STATIC SURFACE RENDERING
        // ═══════════════════════════════════════════════════════════════

        private void EnsureStaticSurface()
        {
            if (!_staticSurfaceDirty && _staticSurface != null && !_staticSurface.IsDisposed)
                return;

            var gd = GraphicsManager.Instance?.GraphicsDevice;
            if (gd == null) return;

            _staticSurface?.Dispose();
            _staticSurface = new RenderTarget2D(gd, WINDOW_WIDTH, WindowHeight, false, SurfaceFormat.Color, DepthFormat.None);

            var previousTargets = gd.GetRenderTargets();
            gd.SetRenderTarget(_staticSurface);
            gd.Clear(Color.Transparent);

            var spriteBatch = GraphicsManager.Instance.Sprite;
            using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend))
            {
                DrawStaticElements(spriteBatch);
            }

            gd.SetRenderTargets(previousTargets);
            _staticSurfaceDirty = false;
        }

        private void InvalidateStaticSurface() => _staticSurfaceDirty = true;
        private void LoadLayoutDefinitions()
        {
            try
            {
                var layoutData =
                    LoadEmbeddedJson<List<LayoutInfo>>(
                        LayoutJsonResource);

                if (layoutData != null)
                {
                    _layoutInfos.Clear();

                    _layoutInfos.AddRange(
                        layoutData.OrderBy(
                            info => info.Z));
                }

                var rectData =
                    LoadEmbeddedJson<List<TextureRectData>>(
                        TextureRectJsonResource);

                if (rectData != null)
                {
                    _textureRectLookup.Clear();

                    foreach (var rect in rectData)
                    {
                        _textureRectLookup[
                            rect.Name] = rect;
                    }
                }
            }
            catch
            {
                // Si algún recurso falla, DrawStaticElements usa el fallback.
            }
        }

        private static T LoadEmbeddedJson<T>(
            string resourceName)
        {
            var assembly =
                Assembly.GetExecutingAssembly();

            using Stream stream =
                assembly.GetManifestResourceStream(
                    resourceName)
                ?? throw new FileNotFoundException(
                    $"Resource not found: {resourceName}");

            using var reader =
                new StreamReader(stream);

            string json =
                reader.ReadToEnd();

            return JsonSerializer.Deserialize<T>(
                json);
        }

        private void DrawStaticElements(SpriteBatch spriteBatch)
        {
            // Fondo/frame clásico completo. Se dibuja dentro de un control
            // normalizado, sin los 148 px / 89 px de espacio vacío que tenía
            // el stage antiguo.
            if (_layoutTexture != null &&
                _layoutInfos.Count > 0)
            {
                foreach (var info in
                         _layoutInfos.OrderBy(i => i.Z))
                {
                    var destRect = new Rectangle(
                        ScaleClassicX(info.ScreenX),
                        ScaleClassicY(info.ScreenY),
                        ScaleClassicSize(info.Width),
                        ScaleClassicSize(info.Height));

                    if (_textureRectLookup.TryGetValue(
                            info.Name,
                            out var src))
                    {
                        var sourceRect =
                            new Rectangle(
                                src.X,
                                src.Y,
                                src.Width,
                                src.Height);

                        spriteBatch.Draw(
                            _layoutTexture,
                            destRect,
                            sourceRect,
                            Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(
                            _layoutTexture,
                            destRect,
                            Color.White);
                    }
                }
            }
            else if (GraphicsManager.Instance?.Pixel != null)
            {
                spriteBatch.Draw(
                    GraphicsManager.Instance.Pixel,
                    new Rectangle(
                        0,
                        0,
                        WINDOW_WIDTH,
                        WINDOW_HEIGHT),
                    new Color(10, 10, 10, 220));
            }

            DrawClassicGridBackground(spriteBatch);

            if (_isRepairShop)
            {
                DrawRepairButtonArea(spriteBatch);
            }

            DrawFooter(spriteBatch);
        }

        private void DrawShopTitle(SpriteBatch spriteBatch)
        {
            if (_font == null)
            {
                return;
            }

            const string title = "NPC SHOP";

            // Mismo tamaño de fuente que InventoryControl.
            float textScale =
                (14f / Constants.BASE_FONT_SIZE) * Scale;

            Vector2 size =
                _font.MeasureString(title) * textScale;

            // Inventory:
            // TopCorner Y = -20
            // título Y = 29
            // => 49 / 90 = posición relativa dentro del TopCorner.
            const float titleRatioInTopCorner = 49f / 90f;

            float titleX =
                _headerRect.X +
                _headerRect.Width * 0.5f;

            float titleY =
                _headerRect.Y +
                _headerRect.Height * titleRatioInTopCorner;

            Vector2 pos =
                DisplayRectangle.Location.ToVector2() +
                new Vector2(
                    titleX * Scale,
                    titleY * Scale);

            // Centrado horizontal.
            pos.X -= size.X * 0.5f;

            spriteBatch.DrawString(
                _font,
                title,
                pos,
                Color.White * Alpha,
                0f,
                Vector2.Zero,
                textScale,
                SpriteEffects.None,
                0f);
        }

        private void DrawClassicGridBackground(
            SpriteBatch spriteBatch)
        {
            if (_slotTexture == null)
            {
                return;
            }

            for (int y = 0; y < SHOP_ROWS; y++)
            {
                for (int x = 0; x < SHOP_COLUMNS; x++)
                {
                    var destRect = new Rectangle(
                        _gridRect.X +
                            GRID_ART_OFFSET_X +
                            x * SHOP_SQUARE_WIDTH,
                        _gridRect.Y +
                            GRID_ART_OFFSET_Y +
                            y * SHOP_SQUARE_HEIGHT,
                        SHOP_SQUARE_WIDTH,
                        SHOP_SQUARE_HEIGHT);

                    spriteBatch.Draw(
                        _slotTexture,
                        destRect,
                        SlotSourceRect,
                        Color.White);
                }
            }
        }


        private void DrawRepairButtonArea(SpriteBatch spriteBatch)
        {
            if (_buttonAreaRect.Height == 0) return;

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            DrawPanel(
                spriteBatch,
                _buttonAreaRect,
                new Color(30, 22, 19, 225));
        }

        private void DrawFooter(SpriteBatch spriteBatch)
        {
            var pixel =
                GraphicsManager.Instance.Pixel;

            if (pixel == null)
            {
                return;
            }

            DrawPanel(
                spriteBatch,
                _footerRect,
                new Color(27, 20, 18, 230));

            if (_font == null)
            {
                return;
            }

            string hint =
                _isRepairShop
                    ? (_shopMode == ShopMode.Repair
                        ? "Repair mode - Click items"
                        : "Buy/Sell - Press 'L' to repair")
                    : "Click item to buy";

            float scale =
                0.32f * CLASSIC_UI_SCALE;

            Vector2 size =
                _font.MeasureString(hint) * scale;

            Vector2 pos = new(
                _footerRect.X +
                    (_footerRect.Width - size.X) / 2f,
                _footerRect.Y +
                    (_footerRect.Height - size.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                hint,
                pos + Vector2.One,
                Color.Black * 0.5f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.DrawString(
                _font,
                hint,
                pos,
                Theme.TextWhite,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        private void DrawRepairButtons(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;

            if (pixel == null || _font == null)
            {
                return;
            }

            var repairRect =
                Translate(_repairButtonRect);

            Color repairBg =
                _shopMode == ShopMode.Repair
                    ? Theme.ClassicButtonActive
                    : (_repairButtonHovered
                        ? Theme.ClassicButtonHover
                        : Theme.ClassicButtonBg);

            Color repairBorder =
                _repairButtonHovered
                    ? Theme.ClassicButtonBorderHover
                    : Theme.ClassicButtonBorder;

            UiDrawHelper.DrawPanel(
                spriteBatch,
                repairRect,
                repairBg,
                repairBorder,
                Theme.BorderOuter);

            string repairText = "Repair item";
            float scale =
                0.34f * CLASSIC_UI_SCALE;

            Vector2 textSize =
                _font.MeasureString(repairText) * scale;

            Vector2 textPos = new(
                repairRect.X +
                    (repairRect.Width - textSize.X) / 2f,
                repairRect.Y +
                    (repairRect.Height - textSize.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                repairText,
                textPos + Vector2.One,
                Color.Black * 0.6f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.DrawString(
                _font,
                repairText,
                textPos,
                Theme.TextWhite,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            var repairAllRect =
                Translate(_repairAllButtonRect);

            Color repairAllBg =
                _repairAllButtonHovered
                    ? Theme.ClassicButtonHover
                    : Theme.ClassicButtonBg;

            Color repairAllBorder =
                _repairAllButtonHovered
                    ? Theme.ClassicButtonBorderHover
                    : Theme.ClassicButtonBorder;

            UiDrawHelper.DrawPanel(
                spriteBatch,
                repairAllRect,
                repairAllBg,
                repairAllBorder,
                Theme.BorderOuter);

            string allText = "Repair all";

            textSize =
                _font.MeasureString(allText) * scale;

            textPos = new(
                repairAllRect.X +
                    (repairAllRect.Width - textSize.X) / 2f,
                repairAllRect.Y +
                    (repairAllRect.Height - textSize.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                allText,
                textPos + Vector2.One,
                Color.Black * 0.6f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.DrawString(
                _font,
                allText,
                textPos,
                Theme.TextWhite,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        // ═══════════════════════════════════════════════════════════════
        // DYNAMIC DRAWING
        // ═══════════════════════════════════════════════════════════════

        private void DrawCloseButton(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;

            if (pixel == null)
            {
                return;
            }

            var rect =
                Translate(_closeButtonRect);

            Color btnColor =
                _closeHovered
                    ? Theme.Accent
                    : Theme.TextGray;

            int cx =
                rect.X + rect.Width / 2;

            int cy =
                rect.Y + rect.Height / 2;

            int halfSize =
                Math.Max(
                    5,
                    rect.Width / 4);

            const int thickness = 2;

            for (int i = -halfSize;
                 i <= halfSize;
                 i++)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        cx + i - thickness / 2,
                        cy + i - thickness / 2,
                        thickness,
                        thickness),
                    btnColor);

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        cx + i - thickness / 2,
                        cy - i - thickness / 2,
                        thickness,
                        thickness),
                    btnColor);
            }
        }

        private void DrawShopItems(SpriteBatch spriteBatch)
        {
            var font = _font ?? GraphicsManager.Instance.Font;
            Point gridOrigin = new(DisplayRectangle.X + _gridRect.X, DisplayRectangle.Y + _gridRect.Y);
            var pixel = GraphicsManager.Instance.Pixel;
            var jewelEntries = new List<(InventoryItem Item, Rectangle Rect)>();

            foreach (var item in _items)
            {
                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * SHOP_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * SHOP_SQUARE_HEIGHT,
                    item.Definition.Width * SHOP_SQUARE_WIDTH,
                    item.Definition.Height * SHOP_SQUARE_HEIGHT);

                bool isHovered = item == _hoveredItem;
                Texture2D texture = ResolveItemTexture(item, rect.Width, rect.Height, isHovered);

                // Glow similar to inventory/vault
                Color glowColor = ItemUiHelper.GetItemGlowColor(item, GlowPalette);
                if (glowColor.A > 0 || isHovered)
                {
                    Color finalGlow = isHovered ? Color.Lerp(glowColor, Theme.Accent, 0.4f) : glowColor;
                    finalGlow.A = (byte)Math.Min(255, finalGlow.A + (isHovered ? 40 : 0));
                    ItemUiHelper.DrawItemGlow(spriteBatch, pixel, rect, finalGlow, glowSize: 1);
                }


                if (texture != null)
                {
                    spriteBatch.Draw(texture, rect, Color.White * Alpha);

                    if (JewelShineOverlay.ShouldShine(item))
                    {
                        jewelEntries.Add((item, rect));
                    }
                }
                else if (pixel != null)
                {
                    ItemGridRenderHelper.DrawItemPlaceholder(spriteBatch, pixel, font, rect, item, Theme.BgLight, Theme.TextGray * 0.8f);
                }

                if (font != null && item.Definition.BaseDurability == 0 && item.Definition.MagicDurability == 0 && item.Durability > 1)
                {
                    ItemGridRenderHelper.DrawItemStackCount(spriteBatch, font, rect, item.Durability, Theme.TextGold, Alpha);
                }

                ItemGridRenderHelper.DrawItemLevelBadge(spriteBatch, GraphicsManager.Instance.Pixel, font, rect, item.Details.Level,
               lvl => lvl >= 9 ? Theme.AccentBright :
                      lvl >= 7 ? Theme.Accent :
                      lvl >= 4 ? Theme.AccentDim :
                      Theme.TextGray,
               new Color(0, 0, 0, 180));

                if (jewelEntries.Count > 0)
                {
                    JewelShineOverlay.DrawBatch(spriteBatch, jewelEntries, _currentGameTime, Alpha, UiScaler.SpriteTransform);
                }
            }
        }

        private void DrawTooltip(SpriteBatch spriteBatch)
        {
            if (_hoveredItem == null || _font == null) return;

            var lines = ItemUiHelper.BuildTooltipLines(_hoveredItem);
            int buyPrice = ItemPriceCalculator.CalculateBuyPrice(_hoveredItem);
            if (buyPrice > 0)
            {
                lines.Add(($"Buy Price: {buyPrice} Zen", Theme.TextGold));
            }
            const float scale = 0.44f;
            const int lineSpacing = 4;
            const int paddingX = 14;
            const int paddingY = 12;

            int maxWidth = 0;
            int totalHeight = 0;
            foreach (var (text, _) in lines)
            {
                Vector2 sz = _font.MeasureString(text) * scale;
                maxWidth = Math.Max(maxWidth, (int)MathF.Ceiling(sz.X));
                totalHeight += (int)MathF.Ceiling(sz.Y) + lineSpacing;
            }
            totalHeight += 6;

            int tooltipWidth = maxWidth + paddingX * 2;
            int tooltipHeight = totalHeight + paddingY * 2;

            Point mouse = MuGame.Instance.UiMouseState.Position;
            var itemRect = new Rectangle(
                DisplayRectangle.X + _gridRect.X + _hoveredItem.GridPosition.X * SHOP_SQUARE_WIDTH,
                DisplayRectangle.Y + _gridRect.Y + _hoveredItem.GridPosition.Y * SHOP_SQUARE_HEIGHT,
                _hoveredItem.Definition.Width * SHOP_SQUARE_WIDTH,
                _hoveredItem.Definition.Height * SHOP_SQUARE_HEIGHT);

            Rectangle tooltipRect = new(mouse.X + 16, mouse.Y + 16, tooltipWidth, tooltipHeight);
            Rectangle screenBounds = new(0, 0, UiScaler.VirtualSize.X, UiScaler.VirtualSize.Y);

            if (tooltipRect.Intersects(itemRect))
            {
                tooltipRect.X = itemRect.X - tooltipWidth - 8;
                tooltipRect.Y = itemRect.Y;

                if (tooltipRect.X < 10 || tooltipRect.Intersects(itemRect))
                {
                    tooltipRect.X = itemRect.X;
                    tooltipRect.Y = itemRect.Y - tooltipHeight - 8;

                    if (tooltipRect.Y < 10)
                    {
                        tooltipRect.X = itemRect.X;
                        tooltipRect.Y = itemRect.Bottom + 8;
                    }
                }
            }

            tooltipRect.X = Math.Clamp(tooltipRect.X, 10, screenBounds.Right - tooltipRect.Width - 10);
            tooltipRect.Y = Math.Clamp(tooltipRect.Y, 10, screenBounds.Bottom - tooltipRect.Height - 10);

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            var shadowRect = new Rectangle(tooltipRect.X + 4, tooltipRect.Y + 4, tooltipRect.Width, tooltipRect.Height);
            spriteBatch.Draw(pixel, shadowRect, Color.Black * 0.5f);

            UiDrawHelper.DrawVerticalGradient(spriteBatch, tooltipRect, new Color(20, 24, 32, 252), new Color(12, 14, 18, 254));

            bool isExcellent = _hoveredItem.Details.IsExcellent;
            bool isAncient = _hoveredItem.Details.IsAncient;
            bool isHighLevel = _hoveredItem.Details.Level >= 7;

            Color borderColor = isExcellent ? Theme.GlowExcellent :
                                isAncient ? Theme.GlowAncient :
                                isHighLevel ? Theme.Accent :
                                Theme.TextWhite;

            const int borderThickness = 2;
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Bottom - borderThickness, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.Right - borderThickness, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);

            int textY = tooltipRect.Y + paddingY;
            bool firstLine = true;
            foreach (var (text, color) in lines)
            {
                Vector2 textSize = _font.MeasureString(text) * scale;
                int textX = tooltipRect.X + (tooltipRect.Width - (int)textSize.X) / 2;

                spriteBatch.DrawString(_font, text, new Vector2(textX + 1, textY + 1), Color.Black * 0.7f,
                                       0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Color lineColor = firstLine ? borderColor : color;
                spriteBatch.DrawString(_font, text, new Vector2(textX, textY), lineColor,
                                       0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                textY += (int)textSize.Y + lineSpacing;

                if (firstLine)
                {
                    textY += 2;
                    spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X + 8, textY, tooltipRect.Width - 16, 1), borderColor * 0.3f);
                    textY += 4;
                    firstLine = false;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HANDLING
        // ═══════════════════════════════════════════════════════════════

        private void HandleMouseInput()
        {
            var mouse = MuGame.Instance.UiMouseState;
            var prev = MuGame.Instance.PrevUiMouseState;

            bool leftJustPressed = mouse.LeftButton == ButtonState.Pressed &&
                                   prev.LeftButton == ButtonState.Released;

            if (!leftJustPressed) return;

            // Prevent input when a modal dialog is open (e.g., sell confirmation)
            if (IsModalDialogOpen()) return;
            if (Scene?.FocusControl != this) return;

            // Ignore shop clicks while dragging an item from inventory/vault (so a sell drop doesn't auto-buy a shop item)
            if (InventoryControl.Instance?.GetDraggedItem() != null || VaultControl.Instance?.GetDraggedItem() != null) return;

            Point mousePos = mouse.Position;

            if (DisplayRectangle.Contains(mousePos))
            {
                Scene?.SetMouseInputConsumed();
            }

            if (_hoveredItem == null) return;

            byte slot = (byte)(_hoveredItem.GridPosition.Y * SHOP_COLUMNS + _hoveredItem.GridPosition.X);
            var svc = MuGame.Network?.GetCharacterService();
            if (svc != null)
            {
                _ = svc.SendBuyItemFromNpcRequestAsync(slot);
            }
        }

        private void UpdateHoverState()
        {
            var mousePos = MuGame.Instance.UiMouseState.Position;
            _hoveredSlot = ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _gridRect, SHOP_COLUMNS, SHOP_ROWS, SHOP_SQUARE_WIDTH, SHOP_SQUARE_HEIGHT, mousePos);
            _hoveredItem = GetItemAt(mousePos);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private Rectangle Translate(Rectangle rect)
            => new(DisplayRectangle.X + rect.X, DisplayRectangle.Y + rect.Y, rect.Width, rect.Height);

        private InventoryItem GetItemAt(Point mousePos)
        {
            if (!DisplayRectangle.Contains(mousePos)) return null;

            Point gridOrigin = new(DisplayRectangle.X + _gridRect.X, DisplayRectangle.Y + _gridRect.Y);

            foreach (var item in _items)
            {
                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * SHOP_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * SHOP_SQUARE_HEIGHT,
                    item.Definition.Width * SHOP_SQUARE_WIDTH,
                    item.Definition.Height * SHOP_SQUARE_HEIGHT);

                if (rect.Contains(mousePos)) return item;
            }

            return null;
        }

        private void HandleVisibilityLost()
        {
            SendCloseNpcRequest();
            _characterState?.ClearShopItems();
            _items.Clear();
            _itemTextureCache.Clear();
            _bmdPreviewCache.Clear();
            _hoveredItem = null;
            _hoveredSlot = new Point(-1, -1);
            _isDragging = false;
            _pendingShow = false;

            // Reset repair mode when closing shop
            _shopMode = ShopMode.BuyAndSell;
            _warmupComplete = false;
        }

        private bool IsModalDialogOpen()
        {
            var scene = Scene;
            if (scene == null) return false;

            for (int i = scene.Controls.Count - 1; i >= 0; i--)
            {
                if (scene.Controls[i] is DialogControl dialog && dialog.Visible)
                {
                    return true;
                }
            }

            return false;
        }

        private void SendCloseNpcRequest()
        {
            if (_closeRequestSent) return;
            _closeRequestSent = true;
            var svc = MuGame.Network?.GetCharacterService();
            if (svc != null)
            {
                _ = svc.SendCloseNpcRequestAsync();
            }
        }

        private void EnsureCharacterState()
        {
            if (_characterState != null) return;

            _characterState = MuGame.Network?.GetCharacterState();
            if (_characterState != null)
            {
                _characterState.ShopItemsChanged += RefreshShopContent;
            }
        }

        private void RefreshShopContent()
        {
            if (_characterState == null) return;

            _items.Clear();
            _itemTextureCache.Clear();
            _bmdPreviewCache.Clear();

            var shopItems = _characterState.GetShopItems();
            int maxSlots = SHOP_COLUMNS * SHOP_ROWS;
            foreach (var kv in shopItems)
            {
                byte slot = kv.Key;
                if (slot >= maxSlots)
                    continue;

                byte[] data = kv.Value;

                int gridX = slot % SHOP_COLUMNS;
                int gridY = slot / SHOP_COLUMNS;

                var def = ItemDatabase.GetItemDefinition(data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(data) ?? "Unknown Item", 1, 1, "Interface/newui_item_box.tga");

                var item = new InventoryItem(def, new Point(gridX, gridY), data);
                if (data.Length > 2)
                {
                    item.Durability = data[2];
                }

                _items.Add(item);
            }

            foreach (var item in _items)
            {
                if (!string.IsNullOrEmpty(item.Definition.TexturePath) &&
                    !item.Definition.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                {
                    _ = TextureLoader.Instance.Prepare(item.Definition.TexturePath);
                }
            }

            if (_items.Count > 0)
            {
                // Align left with padding before showing, then freeze position to avoid auto realignment
                ForceAlignNow();
                Align = ControlAlign.None;
                // Use deferred show - warmup happens in Update(), window shows one frame later
                // to avoid black screen flicker from render target switches during Draw().
                _pendingShow = true;
                _warmupComplete = false;
                _closeRequestSent = false;
                _isDragging = false;
            }
        }

        private void WarmupTexturesSync()
        {
            if (GraphicsManager.Instance?.Sprite == null)
                return;

            foreach (var item in _items)
            {
                int w = item.Definition.Width * SHOP_SQUARE_WIDTH;
                int h = item.Definition.Height * SHOP_SQUARE_HEIGHT;
                _ = ResolveItemTexture(item, w, h, animated: false);
            }
        }

        private Texture2D ResolveItemTexture(InventoryItem item, int width, int height, bool animated)
        {
            if (item?.Definition == null) return null;

            string texturePath = item.Definition.TexturePath;
            if (string.IsNullOrEmpty(texturePath)) return null;

            bool isBmd = texturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase);

            if (!isBmd)
            {
                if (_itemTextureCache.TryGetValue(texturePath, out var cached) && cached != null)
                    return cached;

                var tex = TextureLoader.Instance.GetTexture2D(texturePath);
                if (tex != null) _itemTextureCache[texturePath] = tex;
                return tex;
            }

            bool isHovered = animated;

            // Material animation for non-hovered items (if enabled)
            if (!isHovered && Constants.ENABLE_ITEM_MATERIAL_ANIMATION)
            {
                try
                {
                    var mat = BmdPreviewRenderer.GetMaterialAnimatedPreview(item, width, height, _currentGameTime);
                    if (mat != null)
                    {
                        return mat;
                    }
                }
                catch { }
            }

            if (isHovered)
            {
                try
                {
                    return BmdPreviewRenderer.GetSmoothAnimatedPreview(item, width, height, _currentGameTime);
                }
                catch { return null; }
            }

            var cacheKey = (item, width, height, false);
            if (_bmdPreviewCache.TryGetValue(cacheKey, out var cachedPreview) && cachedPreview != null)
                return cachedPreview;

            try
            {
                var preview = BmdPreviewRenderer.GetPreview(item, width, height);
                if (preview != null)
                {
                    _bmdPreviewCache[cacheKey] = preview;
                }
                return preview;
            }
            catch { return null; }
        }

        // ═══════════════════════════════════════════════════════════════
        // REPAIR MODE
        // ═══════════════════════════════════════════════════════════════

        public ShopMode GetShopMode() => _shopMode;
        public bool IsRepairShop => _isRepairShop;
        public bool IsRepairMode => _shopMode == ShopMode.Repair;

        public void SetRepairShop(bool canRepair)
        {
            _isRepairShop = canRepair;
            if (!canRepair && _shopMode == ShopMode.Repair)
            {
                // If NPC can't repair, reset to buy/sell mode
                _shopMode = ShopMode.BuyAndSell;
            }
            BuildLayoutMetrics();
            var newSize = new Point(WINDOW_WIDTH, WindowHeight);
            ControlSize = newSize;
            ViewSize = newSize;              // <-- KLUCZ: utrzymuj ViewSize = ControlSize gdy AutoViewSize=false
            InvalidateStaticSurface();
        }

        public void ToggleRepairMode()
        {
            if (!_isRepairShop) return;

            if (_shopMode == ShopMode.BuyAndSell)
            {
                _shopMode = ShopMode.Repair;
            }
            else
            {
                _shopMode = ShopMode.BuyAndSell;
            }

            InvalidateStaticSurface();

            // TODO: Notify inventory control of mode change
            // InventoryControl.Instance?.SetRepairMode(_shopMode == ShopMode.Repair);
        }

    }
}

