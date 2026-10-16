using System;
using Client.Main.Core.Items.Tooltips;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
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
using MUnique.OpenMU.Network.Packets.ServerToClient;

namespace Client.Main.Controls.UI.Game.Trade
{
    /// <summary>
    /// Player-to-player trade window with two 8x4 grids (partner + player).
    /// </summary>
    public class TradeControl : UIControl
    {
        // ═══════════════════════════════════════════════════════════════
        // WINDOW DIMENSIONS
        // ═══════════════════════════════════════════════════════════════
        private const int WINDOW_WIDTH = GameWindowMetrics.InventoryWidth;
        private const int WINDOW_HEIGHT = GameWindowMetrics.InventoryHeight;

        private const int TRADE_COLUMNS = 8;
        private const int TRADE_ROWS = 4;
        private const int TRADE_SQUARE_WIDTH = GameWindowMetrics.InventoryCell;
        private const int TRADE_SQUARE_HEIGHT = GameWindowMetrics.InventoryCell;
        // Matches the original client behavior (~150 frames @ 60fps).
        private const int TRADE_RED_SECONDS = 3;

        private static readonly int GRID_WIDTH = TRADE_COLUMNS * TRADE_SQUARE_WIDTH;
        private static readonly int GRID_HEIGHT = TRADE_ROWS * TRADE_SQUARE_HEIGHT;

        // ═══════════════════════════════════════════════════════════════
        // NEWUI COLORS AND TRADE STATUS COLORS
        // ═══════════════════════════════════════════════════════════════
        private static class Theme
        {
            public static readonly Color BgDarkest = new(8, 10, 14, 252);
            public static readonly Color BgDark = new(16, 20, 26, 250);
            public static readonly Color BgMid = new(24, 30, 38, 248);
            public static readonly Color BgLight = new(35, 42, 52, 245);

            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);
            public static readonly Color AccentGlow = new(255, 200, 80, 40);

            public static readonly Color Secondary = new(90, 140, 200);

            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);
            public static readonly Color BorderHighlight = new(100, 110, 130, 120);

            public static readonly Color SlotBg = new(12, 15, 20, 240);
            public static readonly Color SlotBorder = new(90, 74, 49, 180);
            public static readonly Color SlotHover = new(132, 104, 61, 150);
            public static readonly Color SlotSelected = new(212, 175, 85, 100);

            public static readonly Color GlowNormal = new(150, 150, 150, 25);
            public static readonly Color GlowMagic = new(100, 150, 255, 50);
            public static readonly Color GlowExcellent = new(120, 255, 120, 60);
            public static readonly Color GlowAncient = new(80, 200, 255, 70);
            public static readonly Color GlowLegendary = new(255, 180, 80, 70);

            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);

            public static readonly Color Success = new(80, 200, 120);
            public static readonly Color Warning = new(240, 180, 60);
            public static readonly Color Danger = new(220, 80, 80);

            // Level-based partner name colors
            public static readonly Color LevelRed = new(220, 80, 80);      // 1-49
            public static readonly Color LevelOrange = new(240, 160, 60);  // 50-99
            public static readonly Color LevelGreen = new(80, 200, 120);   // 100-199
            public static readonly Color LevelWhite = new(240, 240, 245);  // 200+
        }

        private static readonly ItemGlowPalette GlowPalette = new(
            Theme.GlowNormal,
            Theme.GlowMagic,
            Theme.GlowExcellent,
            Theme.GlowAncient,
            Theme.GlowLegendary);

        // ═══════════════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════════════
        private static TradeControl _instance;

        private readonly List<InventoryItem> _partnerItems = new();
        private readonly List<InventoryItem> _myItems = new();
        private InventoryItem[,] _partnerGrid = new InventoryItem[TRADE_COLUMNS, TRADE_ROWS];
        private InventoryItem[,] _myGrid = new InventoryItem[TRADE_COLUMNS, TRADE_ROWS];

        private readonly Dictionary<string, Texture2D> _itemTextureCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(InventoryItem item, int width, int height, bool animated), Texture2D> _bmdPreviewCache = new();

        private Rectangle _headerRect;
        private Rectangle _partnerInfoRect;
        private Rectangle _partnerGridFrameRect;
        private Rectangle _partnerGridRect;
        private Rectangle _partnerMoneyRect;
        private Rectangle _dividerRect;
        private Rectangle _myInfoRect;
        private Rectangle _myGridFrameRect;
        private Rectangle _myGridRect;
        private Rectangle _myMoneyInputRect;
        private Rectangle _tradeButtonRect;
        private Rectangle _closeButtonRect;

        private Rectangle _priceButtonRect;
        private bool _priceButtonHovered;
        // All skins come from the verified NewUI GFx directory, not classic OZT UI.
        private static readonly string[] SkinNames = {
            "PersonalShop_I1", "NpcShop_I3", "invenframe_01", "invenframe_02",
            "coin", "btn_l01", "btn_l02", "btn_l03",
            "btn_close01", "btn_close02", "btn_close03"
        };
        private Texture2D[] _skin;
        public override bool BlocksPointerInput => true;

        // GFx uses straight alpha. Source-over alpha must use One, not SourceAlpha:
        // squaring sprite alpha punches translucent holes into an opaque cached grid.
        private readonly BlendState _skinCacheBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private SpriteFont _font;
        private CharacterState _characterState;
        private readonly NetworkManager _networkManager;
        private readonly ILogger<TradeControl> _logger;

        private InventoryItem _hoveredItem;
        private Point _hoveredSlot = new(-1, -1);
        private bool _hoverIsPartnerGrid;

        private InventoryItem _draggedItem;
        private Point _draggedOriginalSlot = new(-1, -1);
        private Point _pendingDropSlot = new(-1, -1);

        private GameTime _currentGameTime;
        private bool _closeHovered;
        private bool _tradeButtonHovered;

        // Money input
        private const uint MaxZen = 2_000_000_000;
        private const int MoneyInputMaxDigits = 10;
        private const double CursorBlinkIntervalMs = 500;
        private bool _isMoneyInputActive;
        private bool _moneyInputHovered;
        private string _moneyInputText = string.Empty;
        private double _moneyInputBlinkTimer;
        private bool _moneyInputShowCursor;

        // Window drag support
        private bool _isDragging;
        private Point _dragOffset;
        private DateTime _lastClickTime = DateTime.MinValue;

        // Trade state
        private string _partnerName = string.Empty;
        private ushort _partnerLevel;
        private string _partnerGuild = string.Empty;
        private uint _partnerMoney;
        private uint _myMoney;
        private TradeButtonStateChanged.TradeButtonState _myButtonState = TradeButtonStateChanged.TradeButtonState.Unchecked;
        private TradeButtonStateChanged.TradeButtonState _partnerButtonState = TradeButtonStateChanged.TradeButtonState.Unchecked;
        private DateTime _myLockEndTime = DateTime.MinValue;
        private DateTime _partnerLockEndTime = DateTime.MinValue;

        private bool IsMyTradeLocked => _myButtonState == TradeButtonStateChanged.TradeButtonState.Checked;
        private bool IsPartnerTradeLocked => _partnerButtonState == TradeButtonStateChanged.TradeButtonState.Checked;
        private bool IsMyButtonCoolingDown => _myButtonState == TradeButtonStateChanged.TradeButtonState.Red && _myLockEndTime > DateTime.UtcNow;

        private readonly object _tradeSendLock = new();
        private Task _tradeSendChain = Task.CompletedTask;

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════
        private TradeControl(NetworkManager networkManager = null, ILoggerFactory loggerFactory = null)
        {
            _networkManager = networkManager ?? MuGame.Network;
            var factory = loggerFactory ?? MuGame.AppLoggerFactory;
            _logger = factory?.CreateLogger<TradeControl>();

            BuildLayoutMetrics();

            ControlSize = new Point(WINDOW_WIDTH, WINDOW_HEIGHT);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.VerticalCenter | ControlAlign.HorizontalCenter;

            EnsureCharacterState();
        }

        public static TradeControl Instance => _instance ??= new TradeControl();

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API (for inventory drop support)
        // ═══════════════════════════════════════════════════════════════
        public InventoryItem GetDraggedItem() => _draggedItem;

        public Point GetSlotAtScreenPosition(Point screenPos)
        {
            return ItemGridRenderHelper.GetSlotAtScreenPosition(
                DisplayRectangle, _myGridRect, TRADE_COLUMNS, TRADE_ROWS,
                TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, screenPos);
        }

        public bool CanPlaceAt(Point gridSlot, InventoryItem item)
        {
            return CanPlaceAt(_myGrid, gridSlot, item);
        }

        public void AcceptItemFromInventory(InventoryItem item, Point dropSlot, byte inventorySlot)
        {
            if (item == null || !CanPlaceAt(dropSlot, item)) return;

            UnacceptTradeIfNeeded();

            // Calculate trade slot (0-31 for 8x4 grid)
            byte tradeSlot = (byte)(dropSlot.Y * TRADE_COLUMNS + dropSlot.X);

            // Add to my grid (visual feedback)
            PlaceItemOnGrid(_myGrid, item, dropSlot);
            _myItems.Add(item);

            // Send to server using ItemMoveRequest (0x24) with ItemStorageKind.Trade
            var svc = _networkManager?.GetCharacterService();
            var state = _networkManager?.GetCharacterState();
            if (svc != null && state != null)
            {
                var raw = item.RawData ?? Array.Empty<byte>();
                state.AddOrUpdateMyTradeItem(tradeSlot, raw);

                // Mark inventory item as pending to prevent visual duplication
                state.StashPendingInventoryMove(inventorySlot, inventorySlot);

                EnqueueTradeSend(async () =>
                {
                    await svc.SendStorageItemMoveAsync(
                        ItemStorageKind.Inventory,  // FromStorage = Inventory (0)
                        inventorySlot,              // FromSlot = inventory slot
                        ItemStorageKind.Trade,      // ToStorage = Trade (1)
                        tradeSlot,                  // ToSlot = trade slot (0-31)
                        _networkManager.TargetVersion,
                        raw);
                });

                MuGame.ScheduleOnMainThread(() => state.RaiseInventoryChanged());
            }
        }

        public void DrawPickedPreview(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_draggedItem == null || spriteBatch == null) return;

            int w = _draggedItem.Definition.Width * TRADE_SQUARE_WIDTH;
            int h = _draggedItem.Definition.Height * TRADE_SQUARE_HEIGHT;

            Point mouse = MuGame.Instance.UiMouseState.Position;
            var destRect = new Rectangle(mouse.X - w / 2, mouse.Y - h / 2, w, h);

            Texture2D texture = ResolveItemTexture(_draggedItem, w, h, animated: false)
                                ?? BmdPreviewRenderer.GetPreview(
                                    _draggedItem,
                                    _draggedItem.Definition.Width * InventoryControl.INVENTORY_SQUARE_WIDTH,
                                    _draggedItem.Definition.Height * InventoryControl.INVENTORY_SQUARE_HEIGHT);

            if (texture != null)
            {
                spriteBatch.Draw(texture, destRect, Color.White * 0.85f);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LAYOUT
        // ═══════════════════════════════════════════════════════════════
        private void BuildLayoutMetrics()
        {
            _headerRect = new Rectangle(16, 24, WINDOW_WIDTH - 32, 48);
            _closeButtonRect = new Rectangle(WINDOW_WIDTH - 44, 35, 25, 25);
            _partnerInfoRect = new Rectangle(32, 94, 296, 24);
            _partnerGridRect = new Rectangle(52, 176, GRID_WIDTH, GRID_HEIGHT);
            _partnerGridFrameRect = new Rectangle(40, 164, GRID_WIDTH + 24, GRID_HEIGHT + 24);
            _partnerMoneyRect = new Rectangle(66, 324, 164, 26);
            _dividerRect = new Rectangle(20, 363, WINDOW_WIDTH - 40, 32);
            _myInfoRect = new Rectangle(32, 401, 296, 24);
            _myGridRect = new Rectangle(52, 446, GRID_WIDTH, GRID_HEIGHT);
            _myGridFrameRect = new Rectangle(40, 434, GRID_WIDTH + 24, GRID_HEIGHT + 24);
            _myMoneyInputRect = new Rectangle(66, 594, 164, 26);
            _priceButtonRect = new Rectangle(248, 592, 68, 30);
            _tradeButtonRect = new Rectangle(120, 638, 120, 32);
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════
        public override async Task Load()
        {
            await base.Load();
            _font = GraphicsManager.Instance.Font;
            var tasks = new Task<Texture2D>[SkinNames.Length];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = TextureLoader.Instance.PrepareAndGetTexture($"Interface/GFx/{SkinNames[i]}.ozd");
            _skin = await Task.WhenAll(tasks);
            InvalidateStaticSurface();
        }

        public void Show()
        {
            var inventory = InventoryControl.Instance;
            if (inventory?.Visible == true)
            {
                Align = ControlAlign.None;
                X = Math.Max(0, inventory.X - WINDOW_WIDTH);
                Y = Math.Max(12, inventory.Y);
            }
            Visible = true;
            BringToFront();
            InvalidateStaticSurface();
            SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void Hide()
        {
            Visible = false;
            ClearTradeData();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_characterState != null)
            {
                _characterState.TradeWindowOpened -= OnTradeWindowOpened;
                _characterState.TradeFinished -= OnTradeFinished;
                _characterState.TradeItemsChanged -= OnTradeItemsChanged;
                _characterState.TradeMoneyChanged -= OnTradeMoneyChanged;
                _characterState.TradeButtonStateChanged -= OnTradeButtonStateChanged;
                _characterState = null;
            }

            _staticSurface?.Dispose();
            _staticSurface = null;
            _skinCacheBlend.Dispose();
        }

        // ═══════════════════════════════════════════════════════════════
        // UPDATE
        // ═══════════════════════════════════════════════════════════════
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            EnsureCharacterState();
            _currentGameTime = gameTime;

            if (!Visible) return;

            UpdateTradeLockTimers();
            if (Scene?.IsPointerInputBlockedFor(this) == true)
            {
                _hoveredItem = null;
                _hoveredSlot = new Point(-1, -1);
                _pendingDropSlot = new Point(-1, -1);
                _isDragging = false;
                return;
            }

            // ESC: close money input first, otherwise cancel trade
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                if (_isMoneyInputActive)
                {
                    CancelMoneyInput();
                    Scene?.ConsumeKeyboardEscape();
                    return;
                }

                CancelTrade();
                return;
            }

            Point mousePos = MuGame.Instance.UiMouseState.Position;
            bool leftPressed = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
            bool leftJustReleased = !leftPressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Pressed;

            UpdateChromeHover(mousePos);

            // Click outside money input commits it before other interactions (e.g. accept/cancel).
            if (_isMoneyInputActive && leftJustPressed && !_moneyInputHovered)
            {
                CommitMoneyInput();
                _isMoneyInputActive = false;
            }

            HandleMoneyInputKeyboard();

            // Handle close button
            if (leftJustPressed && _closeHovered)
            {
                CancelTrade();
                return;
            }

            // Handle trade button
            if (leftJustPressed && _tradeButtonHovered)
            {
                ToggleTradeButton();
                return;
            }

            // Handle window dragging
            if (leftJustPressed && IsMouseOverDragArea(mousePos) && !_isDragging && _draggedItem == null)
            {
                DateTime now = DateTime.Now;
                if ((now - _lastClickTime).TotalMilliseconds < 500)
                {
                    Align = ControlAlign.VerticalCenter | ControlAlign.HorizontalCenter;
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
                HandleMouseInput(leftJustPressed, leftJustReleased);
            }
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

            var tradeButtonRect = Translate(_tradeButtonRect);
            _tradeButtonHovered = tradeButtonRect.Contains(mousePos);

            var moneyRect = Translate(_myMoneyInputRect);
            _priceButtonHovered = Translate(_priceButtonRect).Contains(mousePos);
            _moneyInputHovered = moneyRect.Contains(mousePos) || _priceButtonHovered;
        }

        // ═══════════════════════════════════════════════════════════════
        // DRAW
        // ═══════════════════════════════════════════════════════════════
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

                var pixel = GraphicsManager.Instance.Pixel;

                // Draw grid overlays
                if (_hoveredItem != null)
                {
                    Rectangle gridRect = _hoverIsPartnerGrid ? _partnerGridRect : _myGridRect;
                    ItemGridRenderHelper.DrawGridOverlays(spriteBatch, pixel, DisplayRectangle, gridRect, _hoveredItem, _hoveredSlot,
                                     TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, Theme.SlotHover, Theme.Accent, Alpha);
                }

                DrawPartnerItems(spriteBatch);
                DrawMyItems(spriteBatch);

                // Draw pending drop overlay when dragging (internal or from inventory)
                var inventoryDragging = InventoryControl.Instance?._pickedItemRenderer?.Item;
                if ((_draggedItem != null || inventoryDragging != null) && _pendingDropSlot.X >= 0)
                {
                    DrawPendingDropOverlay(spriteBatch, _draggedItem ?? inventoryDragging);
                }

                DrawTradeLockOverlays(spriteBatch);
                using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
                {
                    DrawTradeButton(spriteBatch);
                    DrawCloseButton(spriteBatch);
                    DrawSkinButton(spriteBatch, Translate(_priceButtonRect), "Price",
                        _priceButtonHovered, IsMyTradeLocked ? Color.Gray : Color.White);
                }
                DrawDynamicText(spriteBatch);
            }
            finally
            {
                scope?.Dispose();
            }
        }



        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            InvalidateStaticSurface();
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
            var spriteBatch = GraphicsManager.Instance.Sprite;
            using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, _skinCacheBlend))
            {
                gd.SetRenderTarget(_staticSurface);
                gd.Clear(Color.Transparent);
                DrawStaticElements(spriteBatch);
            }

            gd.SetRenderTargets(previousTargets);
            _staticSurfaceDirty = false;
        }

        private void InvalidateStaticSurface() => _staticSurfaceDirty = true;

        private Texture2D Skin(int index)
        {
            var texture = _skin?[index];
            return texture != null && !texture.IsDisposed ? texture : null;
        }

        private void DrawStaticElements(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            spriteBatch.Draw(pixel, new Rectangle(12, 90, WINDOW_WIDTH - 24, WINDOW_HEIGHT - 112), new Color(25, 23, 23));
            NewUiFrameRenderer.DrawPersonalShopWindow(spriteBatch, Skin(0), new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT));
            DrawLabel(spriteBatch, "Trade", new Rectangle(40, 36, WINDOW_WIDTH - 80, 32), Theme.TextGold, 0.48f);
            DrawOfferGrid(spriteBatch, _partnerGridRect, _partnerGridFrameRect);
            DrawOfferGrid(spriteBatch, _myGridRect, _myGridFrameRect);
            DrawMoneyField(spriteBatch, _partnerMoneyRect);
            DrawMoneyField(spriteBatch, _myMoneyInputRect);
            DrawField(spriteBatch, new Rectangle(84, 126, 114, 24));
            DrawField(spriteBatch, new Rectangle(252, 126, 56, 24));
            DrawLabel(spriteBatch, "Guild", new Rectangle(32, 126, 48, 24), Theme.TextGray);
            DrawLabel(spriteBatch, "Level", new Rectangle(204, 126, 46, 24), Theme.TextGray);
            spriteBatch.Draw(pixel, new Rectangle(20, _dividerRect.Y, WINDOW_WIDTH - 40, 1), new Color(107, 83, 52));
            DrawLabel(spriteBatch, "Warning: Check items before trading.", _dividerRect, Theme.Warning, 0.32f);
        }

        private void DrawOfferGrid(SpriteBatch spriteBatch, Rectangle grid, Rectangle frame)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            spriteBatch.Draw(pixel, frame, new Color(14, 13, 11));
            for (int y = 0; y < TRADE_ROWS; y++)
            for (int x = 0; x < TRADE_COLUMNS; x++)
            {
                var cell = new Rectangle(grid.X + x * TRADE_SQUARE_WIDTH, grid.Y + y * TRADE_SQUARE_HEIGHT,
                    TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT);
                spriteBatch.Draw(pixel, cell, new Color(14, 15, 13));
                if (Skin(1) is Texture2D slots)
                    spriteBatch.Draw(slots, cell, new Rectangle(546, 220, 29, 29), Color.White);
            }
            // The same standalone ornamental frame used by NewUI Inventory.
            if (Skin(3) is Texture2D sides)
                spriteBatch.Draw(sides, frame, Color.White);
            if (Skin(2) is Texture2D rail)
            {
                spriteBatch.Draw(rail, new Rectangle(frame.X, frame.Y, frame.Width, 32), Color.White);
                spriteBatch.Draw(rail, new Rectangle(frame.X, frame.Bottom - 32, frame.Width, 32), null,
                    Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);
            }
        }

        private void DrawField(SpriteBatch spriteBatch, Rectangle rect)
        {
            UiDrawHelper.DrawPanel(spriteBatch, rect, new Color(12, 11, 9), new Color(68, 57, 39));
        }

        private void DrawMoneyField(SpriteBatch spriteBatch, Rectangle rect)
        {
            DrawField(spriteBatch, rect);
            if (Skin(4) is Texture2D coin)
                spriteBatch.Draw(coin, new Rectangle(rect.X - 30, rect.Y + 1, 24, 24), Color.White);
        }

        private void DrawLabel(SpriteBatch spriteBatch, string text, Rectangle rect, Color color, float scale = 0.35f)
        {
            if (_font == null) return;
            var measured = _font.MeasureString(text);
            if (measured.X > 0) scale = Math.Min(scale, (rect.Width - 8) / measured.X);
            var size = measured * scale;
            var pos = new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2);
            spriteBatch.DrawString(_font, text, pos + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void DrawSkinButton(SpriteBatch spriteBatch, Rectangle rect, string text, bool hovered, Color tint)
        {
            bool pressed = hovered && MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            if (Skin(pressed ? 7 : hovered ? 6 : 5) is Texture2D button)
                spriteBatch.Draw(button, rect, tint * Alpha);
            DrawLabel(spriteBatch, text, rect, Theme.TextGold * Alpha);
        }

        private void DrawCloseButton(SpriteBatch spriteBatch)
        {
            bool pressed = _closeHovered && MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            if (Skin(pressed ? 10 : _closeHovered ? 9 : 8) is Texture2D close)
                spriteBatch.Draw(close, Translate(_closeButtonRect), Color.White * Alpha);
        }

        private void DrawTradeButton(SpriteBatch spriteBatch)
        {
            string text = _myButtonState switch
            {
                TradeButtonStateChanged.TradeButtonState.Red => $"Changed ({Math.Max(1, GetRemainingSeconds(_myLockEndTime))}s)",
                TradeButtonStateChanged.TradeButtonState.Checked => "Accepted",
                _ => "Trade"
            };
            Color tint = _myButtonState switch
            {
                TradeButtonStateChanged.TradeButtonState.Red => Theme.Danger,
                TradeButtonStateChanged.TradeButtonState.Checked => Theme.Success,
                _ => Color.White
            };
            DrawSkinButton(spriteBatch, Translate(_tradeButtonRect), text, _tradeButtonHovered, tint);
        }

        private void DrawDynamicText(SpriteBatch spriteBatch)
        {
            if (_font == null) return;

            var pixel = GraphicsManager.Instance.Pixel;

            DrawLabel(spriteBatch, $"Trade Target: {_partnerName}", Translate(_partnerInfoRect), GetLevelColor(_partnerLevel));
            DrawLabel(spriteBatch, string.IsNullOrEmpty(_partnerGuild) ? "--" : _partnerGuild,
                Translate(new Rectangle(84, 126, 114, 24)), Theme.TextGold);
            DrawLabel(spriteBatch, _partnerLevel.ToString(), Translate(new Rectangle(252, 126, 56, 24)), Theme.TextGold);
            if (IsPartnerTradeLocked)
                DrawLabel(spriteBatch, "Accepted", Translate(new Rectangle(234, 324, 92, 26)), Theme.Success, 0.30f);

            // Partner money
            {
                string moneyText = _partnerMoney.ToString();
                float scale = 0.35f;
                Vector2 pos = new(
                    DisplayRectangle.X + _partnerMoneyRect.X + 8,
                    DisplayRectangle.Y + _partnerMoneyRect.Y + (_partnerMoneyRect.Height - _font.MeasureString(moneyText).Y * scale) / 2);
                spriteBatch.DrawString(_font, moneyText, pos, Theme.TextGold * Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            // My name
            if (_characterState != null)
            {
                string myText = $"Name: {_characterState.Name}";
                float scale = 0.32f;
                Vector2 size = _font.MeasureString(myText) * scale;
                Vector2 pos = new(
                    DisplayRectangle.X + _myInfoRect.X + (_myInfoRect.Width - size.X) / 2,
                    DisplayRectangle.Y + _myInfoRect.Y + (_myInfoRect.Height - size.Y) / 2);

                spriteBatch.DrawString(_font, myText, pos + Vector2.One, Color.Black * 0.6f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, myText, pos, Theme.TextWhite, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            // My money
            {
                string moneyText = _isMoneyInputActive
                    ? (_moneyInputText.Length == 0 ? "0" : _moneyInputText) + (_moneyInputShowCursor ? "|" : string.Empty)
                    : _myMoney.ToString();
                float scale = 0.35f;

                var screenRect = Translate(_myMoneyInputRect);
                if (_isMoneyInputActive && pixel != null)
                {
                    var borderColor = Theme.AccentBright * 0.9f;
                    spriteBatch.Draw(pixel, new Rectangle(screenRect.X, screenRect.Y, screenRect.Width, 2), borderColor);
                    spriteBatch.Draw(pixel, new Rectangle(screenRect.X, screenRect.Bottom - 2, screenRect.Width, 2), borderColor);
                    spriteBatch.Draw(pixel, new Rectangle(screenRect.X, screenRect.Y, 2, screenRect.Height), borderColor);
                    spriteBatch.Draw(pixel, new Rectangle(screenRect.Right - 2, screenRect.Y, 2, screenRect.Height), borderColor);
                }

                Vector2 pos = new(
                    screenRect.X + 8,
                    screenRect.Y + (screenRect.Height - _font.MeasureString(moneyText).Y * scale) / 2);
                spriteBatch.DrawString(_font, moneyText, pos, Theme.TextGold * Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private Color GetLevelColor(ushort level)
        {
            if (level >= 200) return Theme.LevelWhite;
            if (level >= 100) return Theme.LevelGreen;
            if (level >= 50) return Theme.LevelOrange;
            return Theme.LevelRed;
        }

        private void DrawPartnerItems(SpriteBatch spriteBatch)
        {
            var font = _font ?? GraphicsManager.Instance.Font;
            Point gridOrigin = new(DisplayRectangle.X + _partnerGridRect.X, DisplayRectangle.Y + _partnerGridRect.Y);
            var pixel = GraphicsManager.Instance.Pixel;

            foreach (var item in _partnerItems)
            {
                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * TRADE_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * TRADE_SQUARE_HEIGHT,
                    item.Definition.Width * TRADE_SQUARE_WIDTH,
                    item.Definition.Height * TRADE_SQUARE_HEIGHT);

                bool isHovered = item == _hoveredItem && _hoverIsPartnerGrid;
                Texture2D texture = ResolveItemTexture(item, rect.Width, rect.Height, isHovered);

                // Glow
                Color glowColor = ItemUiHelper.GetItemGlowColor(item, GlowPalette);
                if (glowColor.A > 0 || isHovered)
                {
                    Color finalGlow = isHovered ? Color.Lerp(glowColor, Theme.Accent, 0.4f) : glowColor;
                    finalGlow.A = (byte)Math.Min(255, finalGlow.A + (isHovered ? 40 : 0));
                    ItemUiHelper.DrawItemGlow(spriteBatch, pixel, rect, finalGlow);
                }

                // Cell background
                if (pixel != null)
                {
                    var bgRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
                    spriteBatch.Draw(pixel, bgRect, isHovered ? Theme.SlotHover : Theme.SlotBg);
                }

                if (texture != null)
                {
                    spriteBatch.Draw(texture, rect, Color.White * Alpha);
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
                                       lvl => lvl >= 9 ? Theme.Danger :
                                              lvl >= 7 ? Theme.Accent :
                                              lvl >= 4 ? Theme.AccentDim :
                                              Theme.TextGray,
                                       new Color(0, 0, 0, 180));
                }
            }
        }

        private void DrawMyItems(SpriteBatch spriteBatch)
        {
            var font = _font ?? GraphicsManager.Instance.Font;
            Point gridOrigin = new(DisplayRectangle.X + _myGridRect.X, DisplayRectangle.Y + _myGridRect.Y);
            var pixel = GraphicsManager.Instance.Pixel;

            foreach (var item in _myItems)
            {
                if (item == _draggedItem) continue; // Don't draw dragged item

                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * TRADE_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * TRADE_SQUARE_HEIGHT,
                    item.Definition.Width * TRADE_SQUARE_WIDTH,
                    item.Definition.Height * TRADE_SQUARE_HEIGHT);

                bool isHovered = item == _hoveredItem && !_hoverIsPartnerGrid;
                Texture2D texture = ResolveItemTexture(item, rect.Width, rect.Height, isHovered);

                // Glow
                Color glowColor = ItemUiHelper.GetItemGlowColor(item, GlowPalette);
                if (glowColor.A > 0 || isHovered)
                {
                    Color finalGlow = isHovered ? Color.Lerp(glowColor, Theme.Accent, 0.4f) : glowColor;
                    finalGlow.A = (byte)Math.Min(255, finalGlow.A + (isHovered ? 40 : 0));
                    ItemUiHelper.DrawItemGlow(spriteBatch, pixel, rect, finalGlow);
                }

                // Cell background
                if (pixel != null)
                {
                    var bgRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
                    spriteBatch.Draw(pixel, bgRect, isHovered ? Theme.SlotHover : Theme.SlotBg);
                }

                if (texture != null)
                {
                    spriteBatch.Draw(texture, rect, Color.White * Alpha);
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
                                       lvl => lvl >= 9 ? Theme.Danger :
                                              lvl >= 7 ? Theme.Accent :
                                              lvl >= 4 ? Theme.AccentDim :
                                              Theme.TextGray,
                                       new Color(0, 0, 0, 180));
                }
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

            var grid = _hoverIsPartnerGrid ? _partnerGridRect : _myGridRect;
            var itemRect = new Rectangle(
                DisplayRectangle.X + grid.X + _hoveredItem.GridPosition.X * TRADE_SQUARE_WIDTH,
                DisplayRectangle.Y + grid.Y + _hoveredItem.GridPosition.Y * TRADE_SQUARE_HEIGHT,
                _hoveredItem.Definition.Width * TRADE_SQUARE_WIDTH,
                _hoveredItem.Definition.Height * TRADE_SQUARE_HEIGHT);
            var mouse = MuGame.Instance.UiMouseState.Position;
            if (!itemRect.Contains(mouse)) return;

            var lines = ClassicItemTooltipBuilder.Build(_hoveredItem);
            ClassicItemTooltipRenderer.Draw(spriteBatch, _font, _hoveredItem, lines, itemRect, mouse);
        }

        private void DrawPendingDropOverlay(SpriteBatch spriteBatch, InventoryItem itemBeingDragged)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || itemBeingDragged == null) return;

            Point gridOrigin = new(DisplayRectangle.X + _myGridRect.X, DisplayRectangle.Y + _myGridRect.Y);

            bool canPlace = CanPlaceAt(_myGrid, _pendingDropSlot, itemBeingDragged);
            Color highlightColor = canPlace ? Theme.Success * 0.4f : Theme.Danger * 0.4f;

            for (int y = 0; y < itemBeingDragged.Definition.Height; y++)
            {
                for (int x = 0; x < itemBeingDragged.Definition.Width; x++)
                {
                    int sx = _pendingDropSlot.X + x;
                    int sy = _pendingDropSlot.Y + y;

                    if (sx >= 0 && sx < TRADE_COLUMNS && sy >= 0 && sy < TRADE_ROWS)
                    {
                        var rect = new Rectangle(
                            gridOrigin.X + sx * TRADE_SQUARE_WIDTH,
                            gridOrigin.Y + sy * TRADE_SQUARE_HEIGHT,
                            TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT);
                        spriteBatch.Draw(pixel, rect, highlightColor);
                    }
                }
            }
        }

        private void DrawTradeLockOverlays(SpriteBatch spriteBatch)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null) return;

            if (IsMyTradeLocked)
            {
                int remaining = _myButtonState == TradeButtonStateChanged.TradeButtonState.Red
                    ? GetRemainingSeconds(_myLockEndTime)
                    : 0;

                DrawLockOverlay(spriteBatch, _myGridRect, true, _myButtonState, remaining);
            }

            if (IsPartnerTradeLocked)
            {
                int remaining = _partnerButtonState == TradeButtonStateChanged.TradeButtonState.Red
                    ? GetRemainingSeconds(_partnerLockEndTime)
                    : 0;

                DrawLockOverlay(spriteBatch, _partnerGridRect, false, _partnerButtonState, remaining);
            }
        }

        private void DrawLockOverlay(SpriteBatch spriteBatch, Rectangle localRect, bool isMine, TradeButtonStateChanged.TradeButtonState state, int remainingSeconds)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null || _font == null) return;

            var rect = Translate(localRect);
            Color overlayColor = state == TradeButtonStateChanged.TradeButtonState.Red
                ? Theme.Danger * 0.35f
                : Theme.Success * 0.22f;

            spriteBatch.Draw(pixel, rect, overlayColor);

            string label = isMine ? "YOUR OFFER LOCKED" : "PARTNER READY";
            if (state == TradeButtonStateChanged.TradeButtonState.Red)
            {
                label = isMine ? "LOCKING" : "PARTNER LOCKING";
            }

            if (remainingSeconds > 0)
            {
                label += $" ({remainingSeconds}s)";
            }

            float scale = 0.35f;
            Vector2 size = _font.MeasureString(label) * scale;
            var pos = new Vector2(
                rect.X + (rect.Width - size.X) / 2f,
                rect.Y + (rect.Height - size.Y) / 2f);

            spriteBatch.DrawString(_font, label, pos + Vector2.One, Color.Black * 0.6f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            Color labelColor = state == TradeButtonStateChanged.TradeButtonState.Red ? Theme.TextWhite : Theme.Success;
            spriteBatch.DrawString(_font, label, pos, labelColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HANDLING
        // ═══════════════════════════════════════════════════════════════
        private void UpdateHoverState()
        {
            var mousePos = MuGame.Instance.UiMouseState.Position;

            // Check if dragging from inventory
            var inventoryItem = InventoryControl.Instance?._pickedItemRenderer?.Item;

            // If dragging (either internal or from inventory), show pending drop slot
            InventoryItem draggedItemToCheck = IsMyTradeLocked ? null : _draggedItem ?? inventoryItem;

            if (draggedItemToCheck != null)
            {
                var myGridScreen = Translate(_myGridRect);
                if (myGridScreen.Contains(mousePos))
                {
                    var dropSlot = ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _myGridRect, TRADE_COLUMNS, TRADE_ROWS, TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, mousePos);
                    _pendingDropSlot = (dropSlot.X >= 0 && CanPlaceAt(_myGrid, dropSlot, draggedItemToCheck)) ? dropSlot : new Point(-1, -1);
                    _hoveredItem = null;
                    _hoveredSlot = dropSlot;
                    _hoverIsPartnerGrid = false;
                }
                else
                {
                    _pendingDropSlot = new Point(-1, -1);
                    _hoveredItem = null;
                    _hoveredSlot = new Point(-1, -1);
                    _hoverIsPartnerGrid = false;
                }
                return;
            }
            else
            {
                _pendingDropSlot = new Point(-1, -1);
            }

            // Check partner grid
            var partnerGridScreen = Translate(_partnerGridRect);
            if (partnerGridScreen.Contains(mousePos))
            {
                _hoveredSlot = ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _partnerGridRect, TRADE_COLUMNS, TRADE_ROWS, TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, mousePos);
                _hoveredItem = GetItemAt(mousePos, _partnerItems, _partnerGridRect);
                _hoverIsPartnerGrid = true;
                return;
            }

            // Check my grid
            var myGridScreenRect = Translate(_myGridRect);
            if (myGridScreenRect.Contains(mousePos))
            {
                _hoveredSlot = ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _myGridRect, TRADE_COLUMNS, TRADE_ROWS, TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, mousePos);
                _hoveredItem = GetItemAt(mousePos, _myItems, _myGridRect);
                _hoverIsPartnerGrid = false;
                return;
            }

            _hoveredItem = null;
            _hoveredSlot = new Point(-1, -1);
            _hoverIsPartnerGrid = false;
        }

        private void HandleMouseInput(bool leftJustPressed, bool leftJustReleased)
        {
            if (IsMyTradeLocked)
            {
                CancelDragIfLocked();
                return;
            }

            if (!leftJustPressed) return;

            var mousePos = MuGame.Instance.UiMouseState.Position;

            // Money input focus
            if (_moneyInputHovered && _draggedItem == null)
            {
                if (_priceButtonHovered && _isMoneyInputActive)
                {
                    CommitMoneyInput();
                    _isMoneyInputActive = false;
                }
                else if (!_isMoneyInputActive)
                {
                    BeginMoneyInput();
                }
                Scene?.SetMouseInputConsumed();
                return;
            }

            // If we're already dragging an item, try to drop it (click-click behavior)
            if (_draggedItem != null)
            {
                AttemptDrop(mousePos);
                Scene?.SetMouseInputConsumed();
                return;
            }

            // Otherwise, try to pick up an item from my grid (partner grid is read-only)
            var myGridScreen = Translate(_myGridRect);
            if (myGridScreen.Contains(mousePos) && _hoveredItem != null && !_hoverIsPartnerGrid)
            {
                BeginDrag(_hoveredItem);
                Scene?.SetMouseInputConsumed();
                return;
            }
        }

        private void BeginDrag(InventoryItem item)
        {
            UnacceptTradeIfNeeded();
            _draggedItem = item;
            _draggedOriginalSlot = item.GridPosition;
            RemoveItemFromGrid(_myGrid, item);
            _hoveredItem = null;
            _pendingDropSlot = new Point(-1, -1);
        }

        private void AttemptDrop(Point mousePos)
        {
            if (_draggedItem == null) return;
            UnacceptTradeIfNeeded();

            var myGridScreen = Translate(_myGridRect);
            bool dropped = false;

            // Check drop back to my grid
            if (myGridScreen.Contains(mousePos))
            {
                var dropSlot = ItemGridRenderHelper.GetSlotAtScreenPosition(DisplayRectangle, _myGridRect, TRADE_COLUMNS, TRADE_ROWS, TRADE_SQUARE_WIDTH, TRADE_SQUARE_HEIGHT, mousePos);
                if (dropSlot.X >= 0 && CanPlaceAt(_myGrid, dropSlot, _draggedItem))
                {
                    PlaceDraggedItem(dropSlot);
                    if (dropSlot != _draggedOriginalSlot)
                    {
                        // Item moved within trade grid - no network call needed
                    }
                    dropped = true;
                }
            }

            // Check drop back to inventory
            var inventory = InventoryControl.Instance;
            if (!dropped && inventory != null && inventory.Visible && inventory.DisplayRectangle.Contains(mousePos))
            {
                Point invSlot = inventory.GetSlotAtScreenPositionPublic(mousePos);
                if (invSlot.X >= 0 && inventory.CanPlaceAt(invSlot, _draggedItem))
                {
                    // Calculate trade slot and target inventory slot
                    byte tradeSlot = (byte)(_draggedOriginalSlot.Y * TRADE_COLUMNS + _draggedOriginalSlot.X);
                    byte targetInventorySlot = (byte)(InventoryControl.InventorySlotOffsetConstant + (invSlot.Y * InventoryControl.Columns) + invSlot.X);
                    var svc = _networkManager?.GetCharacterService();
                    var state = _networkManager?.GetCharacterState();
                    if (svc != null && state != null)
                    {
                        byte[] raw = _draggedItem.RawData ?? Array.Empty<byte>();

                        state.RemoveMyTradeItem(tradeSlot);
                        EnqueueTradeSend(async () =>
                        {
                            await svc.SendStorageItemMoveAsync(
                                ItemStorageKind.Trade,
                                tradeSlot,
                                ItemStorageKind.Inventory,
                                targetInventorySlot,
                                _networkManager.TargetVersion,
                                raw);
                        });

                        MuGame.ScheduleOnMainThread(() => state.RaiseInventoryChanged());
                    }

                    dropped = true;
                }
            }

            if (!dropped)
            {
                // Return to original position
                ReturnDraggedItem();
            }

            _draggedItem = null;
            _draggedOriginalSlot = new Point(-1, -1);
            _pendingDropSlot = new Point(-1, -1);
        }

        private void ReturnDraggedItem()
        {
            if (_draggedItem != null && _draggedOriginalSlot.X >= 0)
            {
                PlaceItemOnGrid(_myGrid, _draggedItem, _draggedOriginalSlot);
            }
        }

        private void PlaceDraggedItem(Point newSlot)
        {
            if (_draggedItem == null) return;
            PlaceItemOnGrid(_myGrid, _draggedItem, newSlot);
        }

        private void CancelDragIfLocked()
        {
            if (_draggedItem == null) return;

            ReturnDraggedItem();
            _draggedItem = null;
            _draggedOriginalSlot = new Point(-1, -1);
            _pendingDropSlot = new Point(-1, -1);
        }

        // ═══════════════════════════════════════════════════════════════
        // STATE MANAGEMENT
        // ═══════════════════════════════════════════════════════════════
        private void EnsureCharacterState()
        {
            if (_characterState != null) return;

            _characterState = _networkManager?.GetCharacterState();
            if (_characterState != null)
            {
                _characterState.TradeWindowOpened += OnTradeWindowOpened;
                _characterState.TradeFinished += OnTradeFinished;
                _characterState.TradeItemsChanged += OnTradeItemsChanged;
                _characterState.TradeMoneyChanged += OnTradeMoneyChanged;
                _characterState.TradeButtonStateChanged += OnTradeButtonStateChanged;
            }
        }

        private void ApplyMyButtonState(TradeButtonStateChanged.TradeButtonState state, bool syncCharacterState = false)
        {
            _myButtonState = state;

            if (state == TradeButtonStateChanged.TradeButtonState.Red)
            {
                _myLockEndTime = DateTime.UtcNow.AddSeconds(TRADE_RED_SECONDS);
            }
            else
            {
                _myLockEndTime = DateTime.MinValue;
            }

            if (syncCharacterState)
            {
                _characterState?.SetMyTradeButtonState(state);
            }

            if (IsMyTradeLocked)
            {
                CancelDragIfLocked();
            }
        }

        private void ApplyPartnerButtonState(TradeButtonStateChanged.TradeButtonState state)
        {
            _partnerButtonState = state;

            if (state == TradeButtonStateChanged.TradeButtonState.Red)
            {
                _partnerLockEndTime = DateTime.UtcNow.AddSeconds(TRADE_RED_SECONDS);
            }
            else
            {
                _partnerLockEndTime = DateTime.MinValue;
            }
        }

        private void OnTradeWindowOpened()
        {
            if (_characterState == null) return;

            _partnerName = _characterState.TradePartnerName;
            _partnerLevel = _characterState.TradePartnerLevel;
            _partnerGuild = _characterState.TradePartnerGuild;

            MuGame.ScheduleOnMainThread(() =>
            {
                // Inventory must be positioned first so Trade opens beside it.
                var inventory = InventoryControl.Instance;
                inventory?.Show();
                Show();
            });
        }

        private void OnTradeFinished(TradeFinished.TradeResult result)
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                Hide();

                string message = result switch
                {
                    TradeFinished.TradeResult.Success => "Trade completed successfully!",
                    TradeFinished.TradeResult.Cancelled => "Trade was cancelled.",
                    TradeFinished.TradeResult.FailedByFullInventory => "Trade failed: Inventory is full.",
                    TradeFinished.TradeResult.TimedOut => "Trade timed out.",
                    TradeFinished.TradeResult.FailedByItemsNotAllowedToTrade => "Trade failed: One or more items cannot be traded.",
                    _ => "Trade ended."
                };

                MessageWindow.Show(message);
            });
        }

        private void OnTradeItemsChanged()
        {
            if (_characterState == null) return;

            MuGame.ScheduleOnMainThread(() =>
            {
                RefreshTradeItems();
            });
        }

        private void OnTradeMoneyChanged()
        {
            if (_characterState == null) return;

            MuGame.ScheduleOnMainThread(() =>
            {
                _partnerMoney = _characterState.TradePartnerMoney;
                _myMoney = _characterState.MyTradeMoney;
            });
        }

        private void OnTradeButtonStateChanged()
        {
            if (_characterState == null) return;

            MuGame.ScheduleOnMainThread(() =>
            {
                ApplyMyButtonState(_characterState.MyTradeButtonState);
                ApplyPartnerButtonState(_characterState.PartnerTradeButtonState);
            });
        }

        private void UpdateTradeLockTimers()
        {
            var now = DateTime.UtcNow;

            if (_myButtonState == TradeButtonStateChanged.TradeButtonState.Red &&
                _myLockEndTime != DateTime.MinValue &&
                now >= _myLockEndTime)
            {
                // "Red" is a temporary warning state; it always returns to Unchecked.
                ApplyMyButtonState(TradeButtonStateChanged.TradeButtonState.Unchecked, true);
            }

            if (_partnerButtonState == TradeButtonStateChanged.TradeButtonState.Red &&
                _partnerLockEndTime != DateTime.MinValue &&
                now >= _partnerLockEndTime)
            {
                _partnerLockEndTime = DateTime.MinValue;
                _partnerButtonState = TradeButtonStateChanged.TradeButtonState.Unchecked;
                _characterState?.SetPartnerTradeButtonState(TradeButtonStateChanged.TradeButtonState.Unchecked);
            }
        }

        private void RefreshTradeItems()
        {
            if (_characterState == null) return;

            // Refresh partner items
            _partnerItems.Clear();
            ClearGrid(_partnerGrid);
            var partnerItems = _characterState.GetPartnerTradeItems();
            foreach (var kv in partnerItems)
            {
                byte slot = kv.Key;
                byte[] data = kv.Value;

                int gridX = slot % TRADE_COLUMNS;
                int gridY = slot / TRADE_COLUMNS;

                var def = ItemDatabase.GetItemDefinition(data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(data) ?? "Unknown Item", 1, 1, "Interface/newui_item_box.tga");

                var item = new InventoryItem(def, new Point(gridX, gridY), data);
                if (data.Length > 2)
                {
                    item.Durability = data[2];
                }

                _partnerItems.Add(item);
                PlaceItemOnGrid(_partnerGrid, item, item.GridPosition);
            }

            // Refresh my items
            _myItems.Clear();
            ClearGrid(_myGrid);
            var myItems = _characterState.GetMyTradeItems();
            foreach (var kv in myItems)
            {
                byte slot = kv.Key;
                byte[] data = kv.Value;

                int gridX = slot % TRADE_COLUMNS;
                int gridY = slot / TRADE_COLUMNS;

                var def = ItemDatabase.GetItemDefinition(data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(data) ?? "Unknown Item", 1, 1, "Interface/newui_item_box.tga");

                var item = new InventoryItem(def, new Point(gridX, gridY), data);
                if (data.Length > 2)
                {
                    item.Durability = data[2];
                }

                _myItems.Add(item);
                PlaceItemOnGrid(_myGrid, item, item.GridPosition);
            }
        }

        private void ClearTradeData()
        {
            _partnerName = string.Empty;
            _partnerLevel = 0;
            _partnerGuild = string.Empty;
            _partnerMoney = 0;
            _myMoney = 0;
            _myButtonState = TradeButtonStateChanged.TradeButtonState.Unchecked;
            _partnerButtonState = TradeButtonStateChanged.TradeButtonState.Unchecked;
            _myLockEndTime = DateTime.MinValue;
            _partnerLockEndTime = DateTime.MinValue;
            _isMoneyInputActive = false;
            _moneyInputText = string.Empty;
            _moneyInputBlinkTimer = 0;
            _moneyInputShowCursor = false;

            _partnerItems.Clear();
            _myItems.Clear();
            ClearGrid(_partnerGrid);
            ClearGrid(_myGrid);

            _draggedItem = null;
            _draggedOriginalSlot = new Point(-1, -1);
            _hoveredItem = null;
            _hoveredSlot = new Point(-1, -1);
            _pendingDropSlot = new Point(-1, -1);
            _itemTextureCache.Clear();
            _bmdPreviewCache.Clear();
        }

        private void BeginMoneyInput()
        {
            if (_isDragging || IsMyTradeLocked)
            {
                return;
            }

            _isMoneyInputActive = true;
            _moneyInputText = _myMoney == 0 ? string.Empty : _myMoney.ToString();
            _moneyInputBlinkTimer = 0;
            _moneyInputShowCursor = true;
        }

        private void CancelMoneyInput()
        {
            _isMoneyInputActive = false;
            _moneyInputText = string.Empty;
            _moneyInputBlinkTimer = 0;
            _moneyInputShowCursor = false;
        }

        private void HandleMoneyInputKeyboard()
        {
            if (!_isMoneyInputActive || !Visible)
            {
                return;
            }

            if (_currentGameTime != null)
            {
                _moneyInputBlinkTimer += _currentGameTime.ElapsedGameTime.TotalMilliseconds;
                if (_moneyInputBlinkTimer >= CursorBlinkIntervalMs)
                {
                    _moneyInputShowCursor = !_moneyInputShowCursor;
                    _moneyInputBlinkTimer = 0;
                }
            }

            var keysPressed = MuGame.Instance.Keyboard.GetPressedKeys();
            foreach (var key in keysPressed)
            {
                if (MuGame.Instance.PrevKeyboard.IsKeyUp(key))
                {
                    if (key == Keys.Back)
                    {
                        if (_moneyInputText.Length > 0)
                        {
                            _moneyInputText = _moneyInputText[..^1];
                        }
                    }
                    else if (key == Keys.Enter)
                    {
                        Scene?.ConsumeKeyboardEnter();
                        CommitMoneyInput();
                        _isMoneyInputActive = false;
                        return;
                    }
                    else if (key == Keys.Escape)
                    {
                        Scene?.ConsumeKeyboardEscape();
                        CancelMoneyInput();
                        return;
                    }
                    else if (TryGetDigitKey(key, out char digit))
                    {
                        if (_moneyInputText.Length < MoneyInputMaxDigits)
                        {
                            _moneyInputText += digit;
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

        private void CommitMoneyInput()
        {
            if (_characterState == null)
            {
                return;
            }

            uint requested = 0;
            if (!string.IsNullOrWhiteSpace(_moneyInputText))
            {
                _ = uint.TryParse(_moneyInputText, out requested);
            }

            ulong available = (ulong)_characterState.InventoryZen + (ulong)_myMoney;
            ulong clamped = requested;
            if (clamped > available)
            {
                clamped = available;
            }
            if (clamped > MaxZen)
            {
                clamped = MaxZen;
            }
            uint amount = (uint)clamped;

            if (amount == _myMoney)
            {
                _moneyInputText = string.Empty;
                return;
            }

            _characterState.SetMyTradeMoney(amount);

            var svc = _networkManager?.GetCharacterService();
            if (svc != null)
            {
                EnqueueTradeSend(() => svc.SendTradeMoneyAsync(amount));
            }

            _moneyInputText = string.Empty;
        }

        private void CancelTrade()
        {
            var svc = _networkManager?.GetCharacterService();
            if (svc != null)
            {
                EnqueueTradeSend(svc.SendTradeCancelAsync);
            }
            Hide();
        }

        private void ToggleTradeButton()
        {
            bool accept = _myButtonState != TradeButtonStateChanged.TradeButtonState.Checked;

            if (accept && IsMyButtonCoolingDown)
            {
                // Still in "red" warning state; ignore accept attempts.
                return;
            }

            var svc = _networkManager?.GetCharacterService();
            if (svc == null)
            {
                _logger?.LogWarning("Not connected — cannot toggle trade button.");
                return;
            }

            ApplyMyButtonState(
                accept ? TradeButtonStateChanged.TradeButtonState.Checked : TradeButtonStateChanged.TradeButtonState.Unchecked,
                true);
            EnqueueTradeSend(() => svc.SendTradeButtonAsync(accept));
        }

        private void UnacceptTradeIfNeeded()
        {
            if (_myButtonState != TradeButtonStateChanged.TradeButtonState.Checked)
            {
                return;
            }

            var svc = _networkManager?.GetCharacterService();
            if (svc == null)
            {
                ApplyMyButtonState(TradeButtonStateChanged.TradeButtonState.Unchecked, true);
                return;
            }

            ApplyMyButtonState(TradeButtonStateChanged.TradeButtonState.Unchecked, true);
            EnqueueTradeSend(() => svc.SendTradeButtonAsync(false));
        }

        private void EnqueueTradeSend(Func<Task> sendAsync)
        {
            if (sendAsync == null) return;

            lock (_tradeSendLock)
            {
                _tradeSendChain = _tradeSendChain.ContinueWith(async previous =>
                {
                    try
                    {
                        if (previous.Exception != null)
                        {
                            _logger?.LogDebug(previous.Exception, "Previous trade send task faulted.");
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        await sendAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Trade send task failed.");
                    }
                }, System.Threading.Tasks.TaskScheduler.Default).Unwrap();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════
        private Rectangle Translate(Rectangle rect)
            => new(DisplayRectangle.X + rect.X, DisplayRectangle.Y + rect.Y, rect.Width, rect.Height);

        private InventoryItem GetItemAt(Point mousePos, List<InventoryItem> items, Rectangle gridRect)
        {
            if (!DisplayRectangle.Contains(mousePos)) return null;

            Point gridOrigin = new(DisplayRectangle.X + gridRect.X, DisplayRectangle.Y + gridRect.Y);

            foreach (var item in items)
            {
                var rect = new Rectangle(
                    gridOrigin.X + item.GridPosition.X * TRADE_SQUARE_WIDTH,
                    gridOrigin.Y + item.GridPosition.Y * TRADE_SQUARE_HEIGHT,
                    item.Definition.Width * TRADE_SQUARE_WIDTH,
                    item.Definition.Height * TRADE_SQUARE_HEIGHT);

                if (rect.Contains(mousePos)) return item;
            }

            return null;
        }

        private bool CanPlaceAt(InventoryItem[,] grid, Point gridSlot, InventoryItem item)
        {
            if (item?.Definition == null) return false;

            for (int y = 0; y < item.Definition.Height; y++)
            {
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = gridSlot.X + x;
                    int gy = gridSlot.Y + y;

                    if (gx < 0 || gx >= TRADE_COLUMNS || gy < 0 || gy >= TRADE_ROWS)
                        return false;

                    var occupant = grid[gx, gy];
                    if (occupant != null && occupant != item)
                        return false;
                }
            }

            return true;
        }

        private void PlaceItemOnGrid(InventoryItem[,] grid, InventoryItem item, Point slot)
        {
            item.GridPosition = slot;
            for (int y = 0; y < item.Definition.Height; y++)
            {
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = slot.X + x;
                    int gy = slot.Y + y;
                    if (gx >= 0 && gx < TRADE_COLUMNS && gy >= 0 && gy < TRADE_ROWS)
                    {
                        grid[gx, gy] = item;
                    }
                }
            }
        }

        private void RemoveItemFromGrid(InventoryItem[,] grid, InventoryItem item)
        {
            for (int y = 0; y < TRADE_ROWS; y++)
            {
                for (int x = 0; x < TRADE_COLUMNS; x++)
                {
                    if (grid[x, y] == item)
                    {
                        grid[x, y] = null;
                    }
                }
            }
        }

        private void ClearGrid(InventoryItem[,] grid) => Array.Clear(grid, 0, grid.Length);

        private int GetRemainingSeconds(DateTime until)
        {
            if (until == DateTime.MinValue) return 0;

            int remaining = (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds);
            return Math.Max(0, remaining);
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

            var cacheKey = (item, width, height, animated);
            if (_bmdPreviewCache.TryGetValue(cacheKey, out var cachedPreview) && cachedPreview != null)
                return cachedPreview;

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
                    var animatedTexture = BmdPreviewRenderer.GetAnimatedPreview(item, width, height, _currentGameTime);
                    if (animatedTexture != null)
                    {
                        _bmdPreviewCache[cacheKey] = animatedTexture;
                        return animatedTexture;
                    }
                }
                catch { return null; }
            }

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
