using System;
using Client.Main.Core.Items.Tooltips;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Networking;
using Client.Main.Models;
using Client.Main.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;
using MUnique.OpenMU.Network.Packets.ClientToServer;

namespace Client.Main.Controls.UI.Game
{
    public class VaultControl : UIControl, IUiTexturePreloadable
    {
        // ═══════════════════════════════════════════════════════════════
        // CLASSIC GFx LAYOUT
        // Usa el mismo frame/atlas que el NPC Shop clásico ya probado.
        // ═══════════════════════════════════════════════════════════════
        private const string LayoutJsonResource =
            "Client.Main.Controls.UI.Game.Layouts.NpcShopLayout.json";

        private const string TextureRectJsonResource =
            "Client.Main.Controls.UI.Game.Layouts.NpcShopRect.json";

        private const string LayoutTexturePath =
            "Interface/GFx/NpcShop_I3.ozd";

        public const int Columns = 8;
        public const int Rows = 15;

        private const int VAULT_SQUARE_WIDTH = GameWindowMetrics.InventoryCell;
        private const int VAULT_SQUARE_HEIGHT = GameWindowMetrics.InventoryCell;

        private const float CLASSIC_UI_SCALE =
            (GameWindowMetrics.FullHeightWindowHeight - 16f) / CLASSIC_PANEL_HEIGHT;

        private const int CLASSIC_ORIGIN_X = 148;
        private const int CLASSIC_ORIGIN_Y = 89;
        private const int CLASSIC_PANEL_WIDTH = 275;
        private const int CLASSIC_PANEL_HEIGHT = 535;
        // Same tall-panel footprint as the inventory; compact dialogs are separate.
        private const int WINDOW_WIDTH = GameWindowMetrics.FullHeightWindowWidth;
        private const int WINDOW_HEIGHT = GameWindowMetrics.FullHeightWindowHeight;
        private const int PANEL_MARGIN_X = 8;
        private const int PANEL_MARGIN_Y = 8;
        private const float CLASSIC_UI_SCALE_X =
            (GameWindowMetrics.FullHeightWindowWidth - PANEL_MARGIN_X * 2f) / CLASSIC_PANEL_WIDTH;

        // Misma celda interior usada por el NPC Shop limpio.
        private static readonly Rectangle SlotSourceRect =
            new(548, 223, 25, 25);

        private static readonly int GRID_WIDTH =
            Columns * VAULT_SQUARE_WIDTH;

        private static readonly int GRID_HEIGHT =
            Rows * VAULT_SQUARE_HEIGHT;

        // Mismos offsets lógicos del NPC Shop terminado.
        private const int GRID_LOGIC_OFFSET_Y = 5;

        private const int GRID_ART_OFFSET_X = 0;
        private const int GRID_ART_OFFSET_Y = 0;

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

        // ═══════════════════════════════════════════════════════════════
        // UI COLORS / CLASSIC VAULT HELPERS
        // ═══════════════════════════════════════════════════════════════
        private static class Theme
        {
            public static readonly Color BgLight = new(35, 42, 52, 245);

            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);

            public static readonly Color Secondary = new(90, 140, 200);

            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);

            public static readonly Color SlotBg = new(12, 15, 20, 240);
            public static readonly Color SlotHover = new(70, 85, 110, 150);

            public static readonly Color GlowNormal = new(150, 150, 150, 25);
            public static readonly Color GlowMagic = new(100, 150, 255, 50);
            public static readonly Color GlowExcellent = new(120, 255, 120, 60);
            public static readonly Color GlowAncient = new(80, 200, 255, 70);
            public static readonly Color GlowLegendary = new(255, 180, 80, 70);

            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);

            public static readonly Color Danger = new(220, 80, 80);

            // Paleta clásica para los botones IN / OUT.
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

        private static VaultControl _instance;

        private readonly List<InventoryItem> _items = new();
        private InventoryItem[,] _itemGrid = new InventoryItem[Columns, Rows];

        private readonly Dictionary<string, Texture2D> _itemTextureCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(InventoryItem item, int width, int height, bool animated), Texture2D> _bmdPreviewCache = new();

        private readonly List<LayoutInfo> _layoutInfos = new();

        private readonly Dictionary<string, TextureRectData> _textureRectLookup =
            new(StringComparer.OrdinalIgnoreCase);

        private Texture2D _layoutTexture;
        private Texture2D _slotTexture;

        private Rectangle _headerRect;
        private Rectangle _gridRect;
        private Rectangle _footerRect;
        private Rectangle _zenFieldRect;
        private Rectangle _depositButtonRect;
        private Rectangle _withdrawButtonRect;
        private Rectangle _closeButtonRect;
        private Rectangle _pageButtonRect;
        private Texture2D _pageNormal, _pageHover, _pagePressed;
        private int _vaultPage;
        private byte _draggedSourceSlot;
        private bool _pageHovered;
        private volatile bool _vaultContentDirty = true;
        public override bool BlocksPointerInput => true;
        private bool HasPendingMove => _characterState?.PendingMoveFromSlot.HasValue == true ||
            _characterState?.HasPendingVaultMove == true || _characterState?.HasPendingTradeMove == true;

        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private SpriteFont _font;

        private CharacterState _characterState;
        private readonly NetworkManager _networkManager;
        private readonly ILogger<VaultControl> _logger;

        private InventoryItem _hoveredItem;
        private Point _hoveredSlot = new(-1, -1);
        private Point _pendingDropSlot = new(-1, -1);

        private InventoryItem _draggedItem;
        private Point _draggedOriginalSlot = new(-1, -1);

        private GameTime _currentGameTime;
        private bool _wasVisible;
        private bool _closeRequestSent;
        private bool _closeHovered;
        private bool _pendingShow;
        private bool _warmupComplete;

        // Zen transfer
        private const uint MaxZen = 2_000_000_000;
        private const int ZenInputMaxDigits = 10;
        private const double CursorBlinkIntervalMs = 500;
        private bool _zenFieldHovered;
        private bool _depositHovered;
        private bool _withdrawHovered;
        private bool _isZenInputActive;
        private string _zenInputText = string.Empty;
        private double _zenInputBlinkTimer;
        private bool _zenInputShowCursor;
        private VaultMoveMoneyRequest.VaultMoneyMoveDirection _zenMoveDirection = VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault;
        private long _vaultZen;

        // Window drag support
        private bool _isDragging;
        private Point _dragOffset;
        private DateTime _lastClickTime = DateTime.MinValue;

        private VaultControl(NetworkManager networkManager = null, ILoggerFactory loggerFactory = null)
        {
            _networkManager = networkManager ?? MuGame.Network;
            var factory = loggerFactory ?? MuGame.AppLoggerFactory;
            _logger = factory?.CreateLogger<VaultControl>();

            LoadLayoutDefinitions();
            BuildLayoutMetrics();

            ControlSize = new Point(WINDOW_WIDTH, WINDOW_HEIGHT);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.VerticalCenter | ControlAlign.Left;

            EnsureCharacterState();
        }

        public override bool NonDisposable => true;
        public static VaultControl Instance => _instance ??= new VaultControl();

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
            => PANEL_MARGIN_X +
               (int)MathF.Round(
                   (sourceX - CLASSIC_ORIGIN_X) *
                   CLASSIC_UI_SCALE_X);

        private static int ScaleClassicY(float sourceY)
            => PANEL_MARGIN_Y +
               (int)MathF.Round(
                   (sourceY - CLASSIC_ORIGIN_Y) *
                   CLASSIC_UI_SCALE);

        private static int ScaleClassicSize(float value)
            => (int)MathF.Round(
                value * CLASSIC_UI_SCALE);

        // The cropped GFx frame pieces and logical grid share one X center.
        // Preserve their individual widths and the original vertical spacing.
        private static int CenteredFrameX(float sourceWidth)
            => (WINDOW_WIDTH - (int)MathF.Round(sourceWidth * CLASSIC_UI_SCALE_X)) / 2;

        // Keep the actual GFx widths rather than widening Board and
        // ContentBorder to match TopCorner. The ContentBorder crop has
        // asymmetric artwork; a small independent X adjustment is needed
        // to align its visible inner edge with the 253-unit Board piece.
        
        private const int TOP_CORNER_OFFSET_X = 10;
        private const int CONTENT_BORDER_OFFSET_X = -4;
        private const int BOARD_OFFSET_X = -2;

        private static Rectangle GetFramePieceDestination(LayoutInfo info)
        {
            int width = (int)MathF.Round(
                info.Width * CLASSIC_UI_SCALE_X);

            int offsetX = info.Name switch
            {
                "TopCorner" => TOP_CORNER_OFFSET_X,
                "ContentBorder" => CONTENT_BORDER_OFFSET_X,
                "Board" => BOARD_OFFSET_X,
                _ => 0
            };

            return new Rectangle(
                CenteredFrameX(info.Width) + offsetX,
                ScaleClassicY(info.ScreenY),
                width,
                ScaleClassicSize(info.Height));
        }

        private void BuildLayoutMetrics()
        {
            // Mismo TopCorner del NPC Shop clásico.
            _headerRect = new Rectangle(
                CenteredFrameX(273f),
                ScaleClassicY(89f),
                (int)MathF.Round(273f * CLASSIC_UI_SCALE_X),
                ScaleClassicSize(70f));

            // El baúl también es 8x15 y 32x32, así que usa exactamente
            // el mismo encaje de grilla que ya quedó correcto en el Shop.
            _gridRect = new Rectangle(
                (WINDOW_WIDTH - GRID_WIDTH) / 2,
                ScaleClassicY(180f) + GRID_LOGIC_OFFSET_Y,
                GRID_WIDTH,
                GRID_HEIGHT);

            // Zona inferior dedicada al Zen.
            int footerY =
                _gridRect.Bottom +
                ScaleClassicSize(2f);

            _footerRect = new Rectangle(
                _gridRect.X,
                footerY,
                _gridRect.Width,
                ScaleClassicSize(39f));

            int innerPadding = ScaleClassicSize(5f);
            int labelWidth = ScaleClassicSize(25f);
            int buttonWidth = ScaleClassicSize(28f);
            int buttonGap = ScaleClassicSize(4f);
            int fieldHeight = ScaleClassicSize(18f);

            int controlsY =
                _footerRect.Y +
                Math.Max(
                    0,
                    (_footerRect.Height -
                     fieldHeight) / 2);

            int fieldX =
                _footerRect.X +
                labelWidth;

            int fieldWidth =
                _footerRect.Width -
                labelWidth -
                buttonWidth * 2 -
                buttonGap * 3 -
                innerPadding;

            _zenFieldRect = new Rectangle(
                fieldX,
                controlsY,
                Math.Max(
                    ScaleClassicSize(55f),
                    fieldWidth),
                fieldHeight);

            _depositButtonRect = new Rectangle(
                _zenFieldRect.Right +
                    buttonGap,
                controlsY,
                buttonWidth,
                fieldHeight);

            _withdrawButtonRect = new Rectangle(
                _depositButtonRect.Right +
                    buttonGap,
                controlsY,
                buttonWidth,
                fieldHeight);

            _pageButtonRect = new Rectangle(_gridRect.Right - 112, _gridRect.Y - 30, 112, 26);
            _closeButtonRect = new Rectangle(
                ScaleClassicX(392f),
                ScaleClassicY(98f),
                ScaleClassicSize(20f),
                ScaleClassicSize(20f));
        }

        public override async Task Load()
        {
            await base.Load();

            var loader =
                TextureLoader.Instance;

            _layoutTexture =
                await loader.PrepareAndGetTexture(
                    LayoutTexturePath);

            _slotTexture = _layoutTexture;
            _pageNormal = await loader.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd");
            _pageHover = await loader.PrepareAndGetTexture("Interface/GFx/btn_l02.ozd");
            _pagePressed = await loader.PrepareAndGetTexture("Interface/GFx/btn_l03.ozd");

            _font =
                GraphicsManager.Instance.Font;

            InvalidateStaticSurface();
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return LayoutTexturePath;
            yield return "Interface/GFx/btn_l01.ozd";
            yield return "Interface/GFx/btn_l02.ozd";
            yield return "Interface/GFx/btn_l03.ozd";
        }

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
                // DrawStaticElements tiene fallback.
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

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            EnsureCharacterState();
            _currentGameTime = gameTime;
            if (Visible && !_wasVisible)
            {
                _vaultPage = 0;
                _vaultContentDirty = true;
                InvalidateStaticSurface();
                ForceAlignNow();
                Align = ControlAlign.None;
            }
            if (_vaultPage != 0 && _characterState?.IsVaultExtended != true)
            {
                _vaultPage = 0;
                _vaultContentDirty = true;
                InvalidateStaticSurface();
            }
            if (_vaultContentDirty) { _vaultContentDirty = false; RefreshVaultContent(); }

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
                if (!_wasVisible)
                {
                    _closeRequestSent = false;
                }

                if (_isZenInputActive &&
                    MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                    MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
                {
                    CancelZenInput();
                    Scene?.ConsumeKeyboardEscape();
                    return;
                }

                if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                    MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
                {
                    CloseWindow();
                    return;
                }

                Point mousePos = MuGame.Instance.UiMouseState.Position;
                bool leftPressed = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
                bool leftJustPressed = leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
                bool leftJustReleased = !leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Pressed;

                if (Scene?.IsPointerInputBlockedFor(this) == true &&
                    !(_draggedItem != null && PersonalStoreLayout.IsTopmostAtPointer(InventoryControl.Instance, mousePos)))
                { _hoveredItem = null; return; }
                UpdateChromeHover(mousePos);
                _pageHovered = Translate(_pageButtonRect).Contains(mousePos);
                if (leftJustPressed && _pageHovered)
                {
                    if (_characterState?.IsVaultExtended == true && !HasPendingMove)
                    {
                        CancelZenInput();
                        _vaultPage = 1 - _vaultPage;
                        _hoveredItem = null;
                        _hoveredSlot = _pendingDropSlot = new Point(-1, -1);
                        RefreshVaultContent();
                        InvalidateStaticSurface();
                    }
                    Scene?.SetMouseInputConsumed();
                    return;
                }

                // Handle close button
                if (leftJustPressed && _closeHovered)
                {
                    CloseWindow();
                    return;
                }

                HandleZenInputKeyboard();

                // Handle window dragging
                if (leftJustPressed && IsMouseOverDragArea(mousePos) && !_isDragging && _draggedItem == null)
                {
                    DateTime now = DateTime.Now;
                    if ((now - _lastClickTime).TotalMilliseconds < 500)
                    {
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
                    if (HandleZenControlsMouseInput(mousePos, leftJustPressed))
                    {
                        Scene?.SetMouseInputConsumed();
                    }
                    else
                    {
                        UpdateHoverState();
                        HandleMouseInput();
                    }
                }

                PrecacheAnimatedPreviews();
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

            var zenRect = Translate(_zenFieldRect);
            _zenFieldHovered = zenRect.Contains(mousePos);

            var depRect = Translate(_depositButtonRect);
            _depositHovered = depRect.Contains(mousePos);

            var wdrRect = Translate(_withdrawButtonRect);
            _withdrawHovered = wdrRect.Contains(mousePos);
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
                scope = new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend, transform: UiScaler.SpriteTransform);
            }

            try
            {
                if (_staticSurface != null && !_staticSurface.IsDisposed)
                {
                    spriteBatch.Draw(_staticSurface, DisplayRectangle, Color.White * Alpha);
                }

                DrawVaultTitle(spriteBatch);

                DrawGridOverlays(spriteBatch);
                DrawVaultItems(spriteBatch);
                DrawCloseButton(spriteBatch);
                DrawZenButtons(spriteBatch);
                DrawZenText(spriteBatch);
                DrawPageButton(spriteBatch);
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
                _characterState.VaultItemsChanged -= OnVaultItemsChanged;
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

        public void CloseWindow()
        {
            if (!Visible) return;
            Visible = false;
            HandleVisibilityLost();
            _wasVisible = false;
        }

        public InventoryItem GetDraggedItem() => _draggedItem;

        public void DrawPickedPreview(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_draggedItem == null || spriteBatch == null) return;

            int w = _draggedItem.Definition.Width * VAULT_SQUARE_WIDTH;
            int h = _draggedItem.Definition.Height * VAULT_SQUARE_HEIGHT;

            var mouse = MuGame.Instance.UiMouseState.Position;
            var destRect = new Rectangle(mouse.X - w / 2, mouse.Y - h / 2, w, h);

            // Use cached previews only to avoid render-target switches while a sprite batch is active.
            Texture2D texture = ResolveItemTexture(_draggedItem, w, h, animated: false, allowGenerate: false)
                                ?? ResolveItemTexture(
                                    _draggedItem,
                                    _draggedItem.Definition.Width * InventoryControl.INVENTORY_SQUARE_WIDTH,
                                    _draggedItem.Definition.Height * InventoryControl.INVENTORY_SQUARE_HEIGHT,
                                    animated: false,
                                    allowGenerate: false);

            if (texture != null)
            {
                spriteBatch.Draw(texture, destRect, Color.White * 0.9f);
            }
            else if (GraphicsManager.Instance?.Pixel != null)
            {
                spriteBatch.Draw(GraphicsManager.Instance.Pixel, destRect, Theme.Accent * 0.8f);
            }
        }

        public Point GetSlotAtScreenPosition(Point screenPos)
        {
            return ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _gridRect, Columns, Rows, VAULT_SQUARE_WIDTH, VAULT_SQUARE_HEIGHT, screenPos);
        }

        public bool CanPlaceAt(Point gridSlot, InventoryItem item)
        {
            if (item?.Definition == null || HasPendingMove ||
                !VaultPageLayout.Fits(_vaultPage, gridSlot, item.Definition.Width, item.Definition.Height, _characterState?.IsVaultExtended == true)) return false;

            for (int y = 0; y < item.Definition.Height; y++)
            {
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = gridSlot.X + x;
                    int gy = gridSlot.Y + y;

                    if (gx < 0 || gx >= Columns || gy < 0 || gy >= Rows)
                        return false;

                    var occupant = _itemGrid[gx, gy];
                    if (occupant != null && occupant != item)
                        return false;
                }
            }

            return true;
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
            _staticSurface = new RenderTarget2D(gd, WINDOW_WIDTH, WINDOW_HEIGHT, false, SurfaceFormat.Color, DepthFormat.None);

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

        private void DrawStaticElements(
            SpriteBatch spriteBatch)
        {
            // Frame clásico completo reutilizando exactamente
            // el atlas/layout que ya quedó correcto en NpcShopControl.
            if (_layoutTexture != null &&
                _layoutInfos.Count > 0)
            {
                // Respect the atlas' original per-piece widths: the 273-unit
                // header must stay visibly wider than the 253-unit body.
                // Draw in layout order (Board, TopCorner, ContentBorder), as
                // before the over-wide frame-rail regression.
                foreach (var info in _layoutInfos.OrderBy(i => i.Z))
                {
                    Rectangle destRect = GetFramePieceDestination(info);

                    if (_textureRectLookup.TryGetValue(info.Name, out var srcData))
                    {
                        var sourceRect = new Rectangle(
                            srcData.X, srcData.Y, srcData.Width, srcData.Height);
                        spriteBatch.Draw(_layoutTexture, destRect, sourceRect, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(_layoutTexture, destRect, Color.White);
                    }
                }
            }
            else if (
                GraphicsManager.Instance?.Pixel != null)
            {
                spriteBatch.Draw(
                    GraphicsManager.Instance.Pixel,
                    new Rectangle(
                        0,
                        0,
                        WINDOW_WIDTH,
                        WINDOW_HEIGHT),
                    new Color(
                        10,
                        10,
                        10,
                        220));
            }

            DrawClassicGridBackground(
                spriteBatch);

            DrawClassicFooter(
                spriteBatch);
        }

        private void DrawVaultTitle(
            SpriteBatch spriteBatch)
        {
            if (_font == null)
            {
                return;
            }

            string title = _vaultPage == 0 ? "VAULT" : "EXTENDED VAULT";

            float textScale =
                (14f /
                 Constants.BASE_FONT_SIZE) *
                Scale;

            Vector2 size =
                _font.MeasureString(title) *
                textScale;

            // Misma posición relativa que dejó NPC SHOP
            // correctamente centrado en su barra roja.
            const float titleRatioInTopCorner =
                49f / 90f;

            float titleX =
                _headerRect.X +
                _headerRect.Width * 0.5f;

            float titleY =
                _headerRect.Y +
                _headerRect.Height *
                titleRatioInTopCorner;

            Vector2 pos =
                DisplayRectangle.Location.ToVector2() +
                new Vector2(
                    titleX * Scale,
                    titleY * Scale);

            pos.X -=
                size.X * 0.5f;

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

            for (int y = 0;
                 y < Rows;
                 y++)
            {
                for (int x = 0;
                     x < Columns;
                     x++)
                {
                    var destRect =
                        new Rectangle(
                            _gridRect.X +
                                GRID_ART_OFFSET_X +
                                x *
                                VAULT_SQUARE_WIDTH,
                            _gridRect.Y +
                                GRID_ART_OFFSET_Y +
                                y *
                                VAULT_SQUARE_HEIGHT,
                            VAULT_SQUARE_WIDTH,
                            VAULT_SQUARE_HEIGHT);

                    spriteBatch.Draw(
                        _slotTexture,
                        destRect,
                        SlotSourceRect,
                        Color.White);
                }
            }
        }

        private void DrawClassicFooter(
            SpriteBatch spriteBatch)
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
                new Color(
                    27,
                    20,
                    18,
                    230));

            DrawPanel(
                spriteBatch,
                _zenFieldRect,
                new Color(
                    18,
                    14,
                    12,
                    240));

            if (_font == null)
            {
                return;
            }

            const string label =
                "ZEN";

            float scale =
                0.30f *
                CLASSIC_UI_SCALE;

            Vector2 size =
                _font.MeasureString(label) *
                scale;

            Vector2 pos =
                new(
                    _footerRect.X +
                        ScaleClassicSize(4f),
                    _footerRect.Y +
                        (_footerRect.Height -
                         size.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                label,
                pos + Vector2.One,
                Color.Black * 0.6f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.DrawString(
                _font,
                label,
                pos,
                Theme.TextGold,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        private void DrawCloseButton(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            var rect = Translate(_closeButtonRect);
            Color btnColor = _closeHovered ? Theme.Accent : Theme.TextGray;

            // Draw X symbol
            int cx = rect.X + rect.Width / 2;
            int cy = rect.Y + rect.Height / 2;
            int halfSize = 6;
            int thickness = 2;

            for (int i = -halfSize; i <= halfSize; i++)
            {
                spriteBatch.Draw(pixel, new Rectangle(cx + i - thickness / 2, cy + i - thickness / 2, thickness, thickness), btnColor);
                spriteBatch.Draw(pixel, new Rectangle(cx + i - thickness / 2, cy - i - thickness / 2, thickness, thickness), btnColor);
            }
        }

        private void DrawZenText(
            SpriteBatch spriteBatch)
        {
            if (_font == null)
            {
                return;
            }

            var zenRect =
                Translate(_zenFieldRect);

            var pixel =
                GraphicsManager.Instance.Pixel;

            if (_isZenInputActive &&
                pixel != null)
            {
                Color borderColor =
                    Theme.ClassicButtonBorderHover;

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        zenRect.X,
                        zenRect.Y,
                        zenRect.Width,
                        1),
                    borderColor);

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        zenRect.X,
                        zenRect.Bottom - 1,
                        zenRect.Width,
                        1),
                    borderColor);

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        zenRect.X,
                        zenRect.Y,
                        1,
                        zenRect.Height),
                    borderColor);

                spriteBatch.Draw(
                    pixel,
                    new Rectangle(
                        zenRect.Right - 1,
                        zenRect.Y,
                        1,
                        zenRect.Height),
                    borderColor);
            }

            string zenText =
                _isZenInputActive
                    ? $"{(_zenMoveDirection == VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault ? "IN" : "OUT")}: {(_zenInputText.Length == 0 ? "0" : _zenInputText)}{(_zenInputShowCursor ? "|" : string.Empty)}"
                    : _vaultZen.ToString();

            float scale =
                0.32f *
                CLASSIC_UI_SCALE;

            Vector2 size =
                _font.MeasureString(zenText) *
                scale;

            Vector2 pos =
                new(
                    zenRect.X +
                        ScaleClassicSize(4f),
                    zenRect.Y +
                        (zenRect.Height -
                         size.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                zenText,
                pos,
                Theme.TextGold * Alpha,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        private void DrawZenButtons(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null) return;

            DrawZenButton(spriteBatch, _depositButtonRect, "IN", _depositHovered, VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault);
            DrawZenButton(spriteBatch, _withdrawButtonRect, "OUT", _withdrawHovered, VaultMoveMoneyRequest.VaultMoneyMoveDirection.VaultToInventory);
        }

        private void DrawZenButton(
            SpriteBatch spriteBatch,
            Rectangle localRect,
            string label,
            bool hovered,
            VaultMoveMoneyRequest.VaultMoneyMoveDirection direction)
        {
            var pixel =
                GraphicsManager.Instance.Pixel;

            if (pixel == null ||
                _font == null)
            {
                return;
            }

            var rect =
                Translate(localRect);

            bool selected =
                _isZenInputActive &&
                _zenMoveDirection ==
                    direction;

            Color bg =
                selected
                    ? Theme.ClassicButtonActive
                    : (hovered
                        ? Theme.ClassicButtonHover
                        : Theme.ClassicButtonBg);

            Color border =
                hovered || selected
                    ? Theme.ClassicButtonBorderHover
                    : Theme.ClassicButtonBorder;

            UiDrawHelper.DrawPanel(
                spriteBatch,
                rect,
                bg,
                border,
                Theme.BorderOuter);

            float scale =
                0.30f *
                CLASSIC_UI_SCALE;

            Vector2 size =
                _font.MeasureString(label) *
                scale;

            Vector2 pos =
                new(
                    rect.X +
                        (rect.Width -
                         size.X) / 2f,
                    rect.Y +
                        (rect.Height -
                         size.Y) / 2f);

            spriteBatch.DrawString(
                _font,
                label,
                pos + Vector2.One,
                Color.Black * 0.6f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            spriteBatch.DrawString(
                _font,
                label,
                pos,
                Theme.TextWhite,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
        }

        private void DrawGridOverlays(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            Point gridOrigin = new(DisplayRectangle.X + _gridRect.X, DisplayRectangle.Y + _gridRect.Y);
            var activeDragged = _draggedItem ?? InventoryControl.Instance?.GetDraggedItem();

            // Drag preview highlight
            if (activeDragged != null && _pendingDropSlot.X >= 0)
            {
                // Match inventory: highlight the entire footprint (green=valid, red=invalid)
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        var highlight = GetSlotHighlightColor(new Point(x, y), activeDragged);
                        if (!highlight.HasValue)
                            continue;

                        var rect = new Rectangle(
                            gridOrigin.X + x * VAULT_SQUARE_WIDTH,
                            gridOrigin.Y + y * VAULT_SQUARE_HEIGHT,
                            VAULT_SQUARE_WIDTH, VAULT_SQUARE_HEIGHT);
                        spriteBatch.Draw(pixel, rect, highlight.Value);
                    }
                }
            }

            // Hovered slot highlight (when not dragging item)
            if (activeDragged == null)
            {
                ItemGridRenderHelper.DrawGridOverlays(spriteBatch, pixel, DisplayRectangle, _gridRect, _hoveredItem, _hoveredSlot,
                                 VAULT_SQUARE_WIDTH, VAULT_SQUARE_HEIGHT, Theme.SlotHover, Theme.Accent, Alpha);
            }
        }

        private void DrawVaultItems(SpriteBatch spriteBatch)
        {
            var font = _font ?? GraphicsManager.Instance.Font;
            Point gridOrigin = new(DisplayRectangle.X + _gridRect.X, DisplayRectangle.Y + _gridRect.Y);
            var pixel = GraphicsManager.Instance.Pixel;
            var jewelEntries = new List<(InventoryItem Item, Rectangle Rect)>();

            foreach (var item in _items)
            {
                if (item == _draggedItem) continue;

                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * VAULT_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * VAULT_SQUARE_HEIGHT,
                    item.Definition.Width * VAULT_SQUARE_WIDTH,
                    item.Definition.Height * VAULT_SQUARE_HEIGHT);

                bool isHovered = item == _hoveredItem;
                Texture2D texture = ResolveItemTexture(item, rect.Width, rect.Height, isHovered, allowGenerate: true);

                // Glow similar to inventory
                Color glowColor = ItemUiHelper.GetItemGlowColor(item, GlowPalette);
                if (glowColor.A > 0 || isHovered)
                {
                    Color finalGlow = isHovered ? Color.Lerp(glowColor, Theme.Accent, 0.4f) : glowColor;
                    finalGlow.A = (byte)Math.Min(255, finalGlow.A + (isHovered ? 40 : 0));
                    ItemUiHelper.DrawItemGlow(spriteBatch, pixel, rect, finalGlow);
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

                if (font != null && item.Details.Level > 0)
                {
                    ItemGridRenderHelper.DrawItemLevelBadge(spriteBatch, pixel, font, rect, item.Details.Level,
                                       lvl => lvl >= 9 ? Theme.AccentBright :
                                              lvl >= 7 ? Theme.Accent :
                                              lvl >= 4 ? Theme.AccentDim :
                                              Theme.TextGray,
                                       new Color(0, 0, 0, 180));
                }
            }

            if (jewelEntries.Count > 0)
            {
                JewelShineOverlay.DrawBatch(spriteBatch, jewelEntries, _currentGameTime, Alpha, UiScaler.SpriteTransform);
            }
        }

        // Called by GameScene after the world and all window post-passes.
        public void DrawTooltipOverlay(SpriteBatch spriteBatch)
        {
            if (!Visible || Status != GameControlStatus.Ready || _hoveredItem == null || _font == null ||
                _draggedItem != null ||
                InventoryControl.Instance?.GetDraggedItem() != null ||
                !PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position))
                return;

            var grid = _gridRect;
            var itemRect = new Rectangle(
                DisplayRectangle.X + grid.X + _hoveredItem.GridPosition.X * VAULT_SQUARE_WIDTH,
                DisplayRectangle.Y + grid.Y + _hoveredItem.GridPosition.Y * VAULT_SQUARE_HEIGHT,
                _hoveredItem.Definition.Width * VAULT_SQUARE_WIDTH,
                _hoveredItem.Definition.Height * VAULT_SQUARE_HEIGHT);
            var mouse = MuGame.Instance.UiMouseState.Position;
            if (!itemRect.Contains(mouse)) return;

            var lines = ClassicItemTooltipBuilder.Build(_hoveredItem);
            ClassicItemTooltipRenderer.Draw(spriteBatch, _font, _hoveredItem, lines, itemRect, mouse);
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HANDLING
        // ═══════════════════════════════════════════════════════════════

        private void HandleMouseInput()
        {
            if (HasPendingMove || InventoryControl.Instance.GetDraggedItem() != null) return;
            var mouse = MuGame.Instance.UiMouseState;
            var prev = MuGame.Instance.PrevUiMouseState;

            bool leftJustPressed = mouse.LeftButton == ButtonState.Pressed &&
                                   prev.LeftButton == ButtonState.Released;

            if (!leftJustPressed) return;

            Point mousePos = mouse.Position;

            if (_draggedItem == null)
            {
                if (_hoveredItem != null)
                {
                    BeginDrag(_hoveredItem);
                    Scene?.SetMouseInputConsumed();
                }
                return;
            }

            AttemptDrop(mousePos);
            Scene?.SetMouseInputConsumed();
        }

        private void UpdateHoverState()
        {
            var mouse = MuGame.Instance.UiMouseState.Position;
            var externalDragged = InventoryControl.Instance?.GetDraggedItem();

            if (_draggedItem != null)
            {
                var dropSlot = GetSlotAtScreenPosition(mouse);
                _pendingDropSlot = dropSlot.X >= 0 ? dropSlot : new Point(-1, -1);
                _hoveredItem = null;
                _hoveredSlot = dropSlot;
                return;
            }
            else if (externalDragged != null)
            {
                var dropSlot = GetSlotAtScreenPosition(mouse);
                _pendingDropSlot = dropSlot.X >= 0 ? dropSlot : new Point(-1, -1);
                _hoveredItem = null;
                _hoveredSlot = dropSlot;
                return;
            }

            _hoveredSlot = GetSlotAtScreenPosition(mouse);
            _hoveredItem = GetItemAt(mouse);
        }

        private void PrecacheAnimatedPreviews()
        {
            if (_currentGameTime == null)
                return;

            CacheAnimatedPreview(_hoveredItem);
            CacheAnimatedPreview(_draggedItem);
        }

        private void CacheAnimatedPreview(InventoryItem item)
        {
            if (item == null)
                return;

            int w = item.Definition.Width * VAULT_SQUARE_WIDTH;
            int h = item.Definition.Height * VAULT_SQUARE_HEIGHT;
            var key = (item, w, h, true);
            if (_bmdPreviewCache.TryGetValue(key, out var existing) && existing != null)
            {
                return;
            }

            try
            {
                var animated = BmdPreviewRenderer.GetAnimatedPreview(item, w, h, _currentGameTime);
                if (animated != null)
                {
                    _bmdPreviewCache[key] = animated;
                }
            }
            catch
            {
                // ignore and leave cache empty; will fall back to static
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // DRAG & DROP
        // ═══════════════════════════════════════════════════════════════

        private void BeginDrag(InventoryItem item)
        {
            if (HasPendingMove) return;
            _draggedSourceSlot = (byte)VaultPageLayout.Slot(_vaultPage, item.GridPosition);
            _draggedItem = item;
            _draggedOriginalSlot = item.GridPosition;
            RemoveItemFromGrid(item);
            _hoveredItem = null;
            _pendingDropSlot = new Point(-1, -1);

            // Pre-cache drag preview while we're outside the draw pass to avoid render-target thrashing.
            int w = item.Definition.Width * VAULT_SQUARE_WIDTH;
            int h = item.Definition.Height * VAULT_SQUARE_HEIGHT;
            _ = ResolveItemTexture(item, w, h, animated: false);
        }

        private void AttemptDrop(Point mousePos)
        {
            if (_draggedItem == null || HasPendingMove) return;
            var drop = GetSlotAtScreenPosition(mousePos);
            var inventory = InventoryControl.Instance;
            if (drop.X >= 0 && PersonalStoreLayout.IsTopmostAtPointer(this, mousePos) && CanPlaceAt(drop, _draggedItem))
            {
                byte target = (byte)VaultPageLayout.Slot(_vaultPage, drop);
                if (target != _draggedSourceSlot) RequestMove(ItemStorageKind.Vault,
                    _draggedSourceSlot, ItemStorageKind.Vault, target, _draggedItem);
            }
            else if (inventory.Visible && PersonalStoreLayout.IsTopmostAtPointer(inventory, mousePos))
            {
                var target = inventory.GetSlotAtScreenPositionPublic(mousePos);
                if (target.X >= 0 && inventory.CanPlaceAt(target, _draggedItem))
                    RequestMove(ItemStorageKind.Vault, _draggedSourceSlot, ItemStorageKind.Inventory,
                        (byte)(InventoryControl.InventorySlotOffsetConstant + target.Y * InventoryControl.Columns + target.X), _draggedItem);
            }
            // A rejected local placement cancels pickup. Both pages are rebuilt from server snapshots.
            _draggedItem = null;
            _draggedOriginalSlot = _pendingDropSlot = new Point(-1, -1);
            RefreshVaultContent();
        }

        private void ReturnDraggedItem()
        {
            if (_draggedItem != null && VaultPageLayout.Contains(_vaultPage, _draggedSourceSlot))
            {
                if (!_items.Contains(_draggedItem)) _items.Add(_draggedItem);
                PlaceItemOnGrid(_draggedItem, _draggedOriginalSlot);
            }
        }







        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        public bool TryHandleInventoryDrop(Point mouse, InventoryControl inventory)
        {
            if (!Visible || !DisplayRectangle.Contains(mouse) || inventory.GetDraggedItem() is not { } item) return false;
            if (Translate(_pageButtonRect).Contains(mouse)) return false; // Allow changing pages while holding a backpack item.
            if (!PersonalStoreLayout.IsTopmostAtPointer(this, mouse)) return true;
            var position = GetSlotAtScreenPosition(mouse);
            if (inventory.GetPickedInventorySlot() is byte source && CanPlaceAt(position, item) &&
                RequestMove(ItemStorageKind.Inventory, source, ItemStorageKind.Vault, (byte)VaultPageLayout.Slot(_vaultPage, position), item))
                inventory.CompleteExtendedInventoryDrop();
            Scene?.SetMouseInputConsumed();
            return true;
        }

        private bool RequestMove(ItemStorageKind fromStore, byte source, ItemStorageKind toStore, byte target, InventoryItem item)
        {
            var state = _characterState;
            var service = _networkManager?.GetCharacterService();
            if (state == null || service == null || _networkManager?.IsConnected != true || HasPendingMove) return false;
            if (fromStore == ItemStorageKind.Inventory) state.StashPendingInventoryMove(source, source);
            else state.StashPendingVaultMove(source, toStore == ItemStorageKind.Vault ? target : (byte)0xFF);
            _ = SendAsync();
            return true;
            async Task SendAsync()
            {
                try
                {
                    await service.SendStorageItemMoveAsync(fromStore, source, toStore, target,
                        _networkManager.TargetVersion, item.RawData, throwOnFailure: true);
                }
                catch (Exception ex)
                {
                    if (fromStore == ItemStorageKind.Inventory && state.IsInventoryMovePending(source, source)) state.TryConsumePendingInventoryMove(out _, out _);
                    else if (fromStore == ItemStorageKind.Vault && state.IsVaultMovePending(source, toStore == ItemStorageKind.Vault ? target : (byte)0xFF)) state.TryConsumePendingVaultMove(out _, out _);
                    state.RaiseInventoryChanged();
                    state.RaiseVaultItemsChanged();
                    _logger?.LogError(ex, "Could not send vault item move.");
                }
            }
        }

        private void DrawPageButton(SpriteBatch sprite)
        {
            if (_pageNormal == null || _font == null) return;
            bool enabled = _characterState?.IsVaultExtended == true;
            var rect = Translate(_pageButtonRect);
            using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred, BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
            {
                bool pressed = _pageHovered && MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
                sprite.Draw(enabled && _pageHovered ? (pressed ? _pagePressed : _pageHover) : _pageNormal, rect, enabled ? Color.White : Color.Gray);
                string text = enabled ? (_vaultPage == 0 ? "Extended >" : "< Main Vault") : "Locked";
                const float scale = 0.32f;
                var size = _font.MeasureString(text) * scale;
                var pos = new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2);
                sprite.DrawString(_font, text, pos + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                sprite.DrawString(_font, text, pos, Theme.TextGold, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                string page = enabled ? $"{_vaultPage + 1} / 2" : "1 / 1";
                sprite.DrawString(_font, page, new Vector2(DisplayRectangle.X + _gridRect.X, rect.Y + 7), Theme.TextGold, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        private Rectangle Translate(Rectangle rect)
            => new(DisplayRectangle.X + rect.X, DisplayRectangle.Y + rect.Y, rect.Width, rect.Height);

        private InventoryItem GetItemAt(Point mousePos)
        {
            if (!DisplayRectangle.Contains(mousePos)) return null;

            Point gridOrigin = new(DisplayRectangle.X + _gridRect.X, DisplayRectangle.Y + _gridRect.Y);

            foreach (var item in _items)
            {
                if (item == _draggedItem) continue;

                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * VAULT_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * VAULT_SQUARE_HEIGHT,
                    item.Definition.Width * VAULT_SQUARE_WIDTH,
                    item.Definition.Height * VAULT_SQUARE_HEIGHT);

                if (rect.Contains(mousePos)) return item;
            }

            return null;
        }

        private void HandleVisibilityLost()
        {
            CancelZenInput();
            _vaultPage = 0;
            InvalidateStaticSurface();
            SendCloseNpcRequest();
            _characterState?.ClearVaultItems();
            _items.Clear();
            ClearGrid();
            _itemTextureCache.Clear();
            _bmdPreviewCache.Clear();
            _draggedItem = null;
            _hoveredItem = null;
            _pendingDropSlot = new Point(-1, -1);
            _isDragging = false;
            _pendingShow = false;
            _warmupComplete = false;
        }

        public void SetVaultZen(uint amount)
        {
            _vaultZen = amount;
        }

        private void SendCloseNpcRequest()
        {
            if (_closeRequestSent) return;
            _closeRequestSent = true;
            var svc = _networkManager?.GetCharacterService();
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
                _characterState.VaultItemsChanged += OnVaultItemsChanged;
            }
        }

        private void OnVaultItemsChanged() => _vaultContentDirty = true;

        private void RefreshVaultContent()
        {
            if (_characterState == null) return;

            _items.Clear();
            ClearGrid();

            var vaultItems = _characterState.GetVaultItems();
            foreach (var kv in vaultItems)
            {
                byte slot = kv.Key;
                if (!VaultPageLayout.Contains(_vaultPage, slot)) continue;
                if (_draggedItem != null && slot == _draggedSourceSlot)
                {
                    _items.Add(_draggedItem);
                    continue;
                }
                byte[] data = kv.Value;

                int localSlot = slot - _vaultPage * VaultPageLayout.PageSize;
                int gridX = localSlot % Columns;
                int gridY = localSlot / Columns;

                var def = ItemDatabase.GetItemDefinition(data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(data) ?? "Unknown Item", 1, 1, "Interface/newui_item_box.tga");

                var item = new InventoryItem(def, new Point(gridX, gridY), data);
                if (data.Length > 2)
                {
                    item.Durability = data[2];
                }

                _items.Add(item);
                PlaceItemOnGrid(item, item.GridPosition);
            }

            foreach (var item in _items)
            {
                if (!string.IsNullOrEmpty(item.Definition.TexturePath) &&
                    !item.Definition.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                {
                    _ = TextureLoader.Instance.Prepare(item.Definition.TexturePath);
                }
            }

        }

        private bool HandleZenControlsMouseInput(Point mousePos, bool leftJustPressed)
        {
            if (_draggedItem != null)
            {
                return false;
            }

            if (_isZenInputActive)
            {
                if (!leftJustPressed)
                {
                    return false;
                }

                if (_depositHovered)
                {
                    BeginZenInput(VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault);
                    return true;
                }

                if (_withdrawHovered)
                {
                    BeginZenInput(VaultMoveMoneyRequest.VaultMoneyMoveDirection.VaultToInventory);
                    return true;
                }

                if (_zenFieldHovered)
                {
                    return true;
                }

                CancelZenInput();
                return true;
            }

            if (!leftJustPressed)
            {
                return false;
            }

            if (_depositHovered)
            {
                BeginZenInput(VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault);
                return true;
            }

            if (_withdrawHovered)
            {
                BeginZenInput(VaultMoveMoneyRequest.VaultMoneyMoveDirection.VaultToInventory);
                return true;
            }

            if (_zenFieldHovered)
            {
                BeginZenInput(_zenMoveDirection);
                return true;
            }

            return false;
        }

        private void BeginZenInput(VaultMoveMoneyRequest.VaultMoneyMoveDirection direction)
        {
            _zenMoveDirection = direction;
            if (!_isZenInputActive)
            {
                _zenInputText = string.Empty;
                _zenInputBlinkTimer = 0;
                _zenInputShowCursor = true;
            }

            _isZenInputActive = true;
        }

        private void CancelZenInput()
        {
            _isZenInputActive = false;
            _zenInputText = string.Empty;
            _zenInputBlinkTimer = 0;
            _zenInputShowCursor = false;
        }

        private void HandleZenInputKeyboard()
        {
            if (!_isZenInputActive || !Visible)
            {
                return;
            }

            if (_currentGameTime != null)
            {
                _zenInputBlinkTimer += _currentGameTime.ElapsedGameTime.TotalMilliseconds;
                if (_zenInputBlinkTimer >= CursorBlinkIntervalMs)
                {
                    _zenInputShowCursor = !_zenInputShowCursor;
                    _zenInputBlinkTimer = 0;
                }
            }

            var keysPressed = MuGame.Instance.Keyboard.GetPressedKeys();
            foreach (var key in keysPressed)
            {
                if (MuGame.Instance.PrevKeyboard.IsKeyUp(key))
                {
                    if (key == Keys.Back)
                    {
                        if (_zenInputText.Length > 0)
                        {
                            _zenInputText = _zenInputText[..^1];
                        }
                    }
                    else if (key == Keys.Enter)
                    {
                        Scene?.ConsumeKeyboardEnter();
                        CommitZenMove();
                        return;
                    }
                    else if (key == Keys.Escape)
                    {
                        Scene?.ConsumeKeyboardEscape();
                        CancelZenInput();
                        return;
                    }
                    else if (TryGetDigitKey(key, out char digit))
                    {
                        if (_zenInputText.Length < ZenInputMaxDigits)
                        {
                            _zenInputText += digit;
                        }
                    }
                }
            }
        }

        private static bool TryGetDigitKey(Keys key, out char digit)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                digit = (char)('0' + (key - Keys.D0));
                return true;
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                digit = (char)('0' + (key - Keys.NumPad0));
                return true;
            }

            digit = '\0';
            return false;
        }

        private void CommitZenMove()
        {
            if (_networkManager == null || _characterState == null)
            {
                CancelZenInput();
                return;
            }

            uint amount = 0;
            if (!string.IsNullOrWhiteSpace(_zenInputText))
            {
                _ = uint.TryParse(_zenInputText, out amount);
            }

            if (amount == 0)
            {
                CancelZenInput();
                return;
            }

            uint inventoryZen = _characterState.InventoryZen;
            uint vaultZen = _vaultZen <= 0 ? 0 : (uint)Math.Min(_vaultZen, uint.MaxValue);

            if (_zenMoveDirection == VaultMoveMoneyRequest.VaultMoneyMoveDirection.InventoryToVault)
            {
                if (amount > inventoryZen)
                {
                    RequestDialog.ShowInfo("Not enough Zen in inventory.");
                    return;
                }
            }
            else
            {
                if (amount > vaultZen)
                {
                    RequestDialog.ShowInfo("Not enough Zen in vault.");
                    return;
                }

                if (inventoryZen >= MaxZen || (ulong)inventoryZen + amount > MaxZen)
                {
                    RequestDialog.ShowInfo("Inventory Zen limit reached.");
                    return;
                }
            }

            var svc = _networkManager.GetCharacterService();
            if (svc != null)
            {
                _ = svc.SendVaultMoveMoneyAsync(_zenMoveDirection, amount);
            }

            CancelZenInput();
        }

        private void WarmupTexturesSync()
        {
            if (GraphicsManager.Instance?.Sprite == null)
                return;

            foreach (var item in _items)
            {
                int w = item.Definition.Width * VAULT_SQUARE_WIDTH;
                int h = item.Definition.Height * VAULT_SQUARE_HEIGHT;
                _ = ResolveItemTexture(item, w, h, animated: false);
            }
        }

        private void PlaceItemOnGrid(InventoryItem item, Point slot)
        {
            item.GridPosition = slot;
            for (int y = 0; y < item.Definition.Height; y++)
            {
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = slot.X + x;
                    int gy = slot.Y + y;
                    if (gx >= 0 && gx < Columns && gy >= 0 && gy < Rows)
                    {
                        _itemGrid[gx, gy] = item;
                    }
                }
            }
        }

        private void RemoveItemFromGrid(InventoryItem item)
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (_itemGrid[x, y] == item)
                    {
                        _itemGrid[x, y] = null;
                    }
                }
            }
        }

        private void ClearGrid() => Array.Clear(_itemGrid, 0, _itemGrid.Length);

        private Color? GetSlotHighlightColor(Point slot, InventoryItem draggedItem)
        {
            if (draggedItem == null || _hoveredSlot.X == -1 || _hoveredSlot.Y == -1)
            {
                return null;
            }

            if (!IsSlotInDropArea(slot, _hoveredSlot, draggedItem))
            {
                return null;
            }

            return CanPlaceAt(_hoveredSlot, draggedItem)
                ? Color.GreenYellow * 0.5f
                : Color.Red * 0.6f;
        }

        private static bool IsSlotInDropArea(Point slot, Point dropPosition, InventoryItem item)
        {
            return slot.X >= dropPosition.X &&
                   slot.X < dropPosition.X + item.Definition.Width &&
                   slot.Y >= dropPosition.Y &&
                   slot.Y < dropPosition.Y + item.Definition.Height;
        }

        private Texture2D ResolveItemTexture(InventoryItem item, int width, int height, bool animated, bool allowGenerate = true)
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
            if (!allowGenerate)
            {
                var rendererCached = BmdPreviewRenderer.TryGetCachedPreview(item, width, height);
                if (rendererCached != null)
                    return rendererCached;
                return null;
            }

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
                    var animatedTexture = BmdPreviewRenderer.GetAnimatedPreview(item, width, height, _currentGameTime);
                    if (animatedTexture != null)
                    {
                        return animatedTexture;
                    }
                }
                catch { return null; }
            }

            var cacheKey = (item, width, height, false);
            if (_bmdPreviewCache.TryGetValue(cacheKey, out var cachedPreview) && cachedPreview != null)
                return cachedPreview;

            // Use cached static preview
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

    }
}
