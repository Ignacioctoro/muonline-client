using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game;

/// <summary>Native XShop skin with authoritative OpenMU prices, balances and storage.</summary>
public sealed class CashShopControl : UIControl, IUiTexturePreloadable
{
    private const int Width = 640, Height = 429;
    private const string AtlasPath = "Interface/GFx/IngameShop_I3.ozd";
    private const string SkinRoot = "Interface/InGameShop/";
    private static readonly string[] Categories = { "All Products", "Dark Wizard", "Dark Knight", "Fairy Elf", "Magic Gladiator", "Dark Lord", "Summoner", "Rage Fighter", "Extras", "Pets", "Seeds / Sockets", "Jewelry" };
    private static readonly string[] SkinFiles = { "ingame_Bt01.OZT", "ingame_Tab_Up.OZT", "ingame_Bt03.OZT", "ingame_itembox_logo.OZT", "ingame_banner.OZJ", "ingame_Bt_page_L.OZT", "ingame_Bt_page_R.OZT", "ingame_Tab01.OZT", "ingame_Tab02.OZT" };
    // OZTReader pads to powers of two; frame UVs must use the original image dimensions.
    private static readonly Point[] SkinSizes = { new(73, 81), new(76, 46), new(52, 78) };
    private static readonly Rectangle CloseButton = new(12, 392, 36, 28);
    private static readonly Rectangle RefreshButton = new(53, 392, 43, 28);
    private static readonly Rectangle CatalogPrevious = new(109, 374, 35, 20);
    private static readonly Rectangle CatalogNext = new(433, 374, 35, 20);
    private static readonly Rectangle StoragePrevious = new(491, 343, 25, 20);
    private static readonly Rectangle StorageNext = new(600, 343, 25, 20);
    private static readonly Rectangle UseButton = new(521, 315, 73, 24);
    private static readonly Rectangle BuyButton = new(261, 322, 118, 25);
    private static readonly Rectangle ModalClose = new(459, 75, 24, 23);
    private static class Theme
    {
        public static readonly Color Back = new(10, 9, 8, 248);
        public static readonly Color Panel = new(25, 20, 16, 250);
        public static readonly Color Button = new(49, 31, 20);
        public static readonly Color Hover = new(79, 53, 29);
        public static readonly Color Selected = new(57, 44, 24);
        public static readonly Color Border = new(108, 82, 42);
        public static readonly Color Gold = new(226, 190, 85);
        public static readonly Color Text = new(222, 216, 204);
        public static readonly Color Muted = new(137, 130, 119);
    }

    public static CashShopControl Instance { get; } = new();
    public static bool IsOpen => Instance.Visible;
    public override bool NonDisposable => true;
    public override bool BlocksPointerInput => true;
    private Texture2D _atlas;
    private readonly Texture2D[] _skin = new Texture2D[SkinFiles.Length];
    private CashShopCurrencyInfo[] _currencies = Array.Empty<CashShopCurrencyInfo>();
    private int _category;
    private RenderTarget2D _background;
    private BlendState _backgroundBlend;
    private readonly Dictionary<(int, int, int), Texture2D> _previews = new();
    private readonly Dictionary<(int, int), CashShopProductInfo> _products = new();
    private readonly HashSet<(int, int)> _requestedProducts = new();
    private readonly List<CashShopStorageEntry> _storage = new();
    private CashShopCatalogPage _catalog;
    private CashShopPackageInfo _detail;
    private CashShopStorageEntry _withdraw;
    private string _owner, _status = string.Empty, _operation = string.Empty;
    private int _ownerClass, _session, _catalogRequest, _filter, _catalogPage = 1;
    private int _storagePage = 1, _storagePages = 1, _expectedEntries = -1, _selectedStorage = -1;
    private int _option, _optionPage;
    private bool _opening, _cancelOpen, _allowed, _catalogWaiting, _storageWaiting, _gifts, _confirm;
    private DateTime _deadline, _catalogDeadline, _storageDeadline, _nextPreviewAttempt;
    private Point _mouse;

    private CashShopControl()
    {
        ControlSize = ViewSize = new Point(Width, Height);
        AutoViewSize = false;
        Interactive = true;
        Visible = false;
    }

    public IEnumerable<string> GetPreloadTexturePaths()
    {
        yield return AtlasPath;
        foreach (var file in SkinFiles) yield return SkinRoot + file;
    }
    public override async Task Load()
    {
        await base.Load();
        _atlas = await TextureLoader.Instance.PrepareAndGetTexture(AtlasPath);
        for (int i = 0; i < SkinFiles.Length; i++)
            _skin[i] = await TextureLoader.Instance.PrepareAndGetTexture(SkinRoot + SkinFiles[i]);
    }

    public void Toggle()
    {
        if (Visible) { CloseWindow(); return; }
        if (_opening || _operation.Length > 0)
        { RequestDialog.ShowInfo("Waiting for the previous shop request. Reconnect if the server does not respond."); return; }
        var state = MuGame.Network?.GetCharacterState();
        var service = MuGame.Network?.GetCharacterService();
        if (state == null || service == null) return;
        _owner = state.Name;
        _ownerClass = (int)state.Class;
        _session++;
        _catalog = null;
        _products.Clear();
        _requestedProducts.Clear();
        _storage.Clear();
        _catalogWaiting = _storageWaiting = _allowed = _cancelOpen = false;
        _filter = _category = _optionPage = 0;
        _currencies = Array.Empty<CashShopCurrencyInfo>();
        _catalogPage = _storagePage = _storagePages = 1;
        _gifts = false;
        _opening = true;
        _deadline = DateTime.UtcNow.AddSeconds(10);
        _status = "Opening XShop...";
        Visible = true;
        PositionWindow();
        BringToFront();
        if (Scene != null) Scene.FocusControl = this;
        _ = ObserveSend(service.SetCashShopOpenAsync(true), _session);
    }

    public void CloseWindow()
    {
        Visible = false;
        _detail = null;
        _withdraw = null;
        _cancelOpen = _opening;
        _allowed = false;
        if (Scene?.FocusControl == this) Scene.FocusControl = null;
        var service = MuGame.Network?.GetCharacterService();
        if (service != null) _ = service.SetCashShopOpenAsync(false);
    }

    public void ResetSession()
    {
        CloseWindow();
        _session++;
        _opening = false;
        _operation = string.Empty;
        _catalog = null;
        _products.Clear();
        _storage.Clear();
        _previews.Clear(); // Preview textures belong to BmdPreviewRenderer.
    }

    public void OpenResult(bool allowed)
    {
        if (!_opening) return;
        _opening = false;
        if (_cancelOpen || !Visible)
        {
            _cancelOpen = false;
            if (allowed) _ = MuGame.Network?.GetCharacterService()?.SetCashShopOpenAsync(false);
            return;
        }
        if (!allowed)
        {
            CloseWindow();
            RequestDialog.ShowInfo("XShop is unavailable. Stand in a safe zone and check that Cash Shop is enabled on the server.");
            return;
        }
        _allowed = true;
        _status = "Select a product. Purchases are delivered to storage.";
        Refresh();
    }

    private async Task ObserveSend(Task<bool> task, int session)
    {
        bool sent = await task;
        if (sent) return;
        MuGame.ScheduleOnMainThread(() =>
        {
            if (_session != session) return;
            _status = "Request could not be sent. Reconnect to synchronize XShop.";
            // Do not retry a purchase after an uncertain send.
        });
    }

    private void Refresh()
    {
        if (!_allowed || _operation.Length > 0 || _catalogWaiting || _storageWaiting) return;
        _products.Clear();
        _requestedProducts.Clear();
        RequestCatalog();
        RequestStorage();
        _ = ObserveSend(MuGame.Network.GetCharacterService().RequestCashShopBalancesAsync(), _session);
    }

    private void RequestCatalog()
    {
        _catalogWaiting = true;
        _catalog = null;
        _catalogDeadline = DateTime.UtcNow.AddSeconds(10);
        _catalogRequest++;
        byte filter = (byte)_filter;
        _ = ObserveSend(MuGame.Network.GetCharacterService().RequestCashShopCatalogAsync(_catalogRequest, _catalogPage, filter, _category), _session);
    }

    public void SetCatalog(CashShopCatalogPage page)
    {
        if (!Visible || !_allowed) return;
        if (page.ErrorCode != 0)
        { _catalogWaiting = false; _status = "This catalog page exceeds the packet size limit. Check its product options on the server."; return; }
        if (page.Request != _catalogRequest || page.CharacterName != _owner || page.Packages == null
            || page.Filter != _filter || page.Category != _category || page.Pages < 1 || page.Page < 1) return;
        _catalogWaiting = false;
        _catalog = page;
        _currencies = page.Currencies ?? Array.Empty<CashShopCurrencyInfo>();
        if (!CurrencyEnabled(_filter))
        {
            for (int coin = 0; coin < 3; coin++)
                if (CurrencyEnabled(coin)) { _filter = coin; _catalogPage = 1; RequestCatalog(); return; }
        }
        _catalogPage = page.Page;
        foreach (var package in page.Packages)
            foreach (var product in package.Products) _products[(product.Sequence, product.Option)] = product;
        if (page.Packages.Length == 0) _status = "No products for sale in this category.";
    }

    private void RequestStorage()
    {
        _storageWaiting = true;
        _storageDeadline = DateTime.UtcNow.AddSeconds(10);
        _expectedEntries = -1;
        _selectedStorage = -1;
        _storage.Clear();
        _ = ObserveSend(MuGame.Network.GetCharacterService().RequestCashShopStorageAsync(_storagePage, _gifts), _session);
    }

    public void BeginStorage(int count, int page, int pages)
    {
        if (!Visible || !_allowed || !_storageWaiting || _expectedEntries != -1) return;
        _storagePage = page;
        _storagePages = pages;
        _expectedEntries = count;
        _storageWaiting = count > 0;
    }

    public void AddStorage(CashShopStorageEntry entry, bool gift)
    {
        if (!Visible || !_allowed || !_storageWaiting || gift != _gifts || _expectedEntries <= _storage.Count) return;
        _storage.Add(entry);
        if (_storage.Count == _expectedEntries) _storageWaiting = false;
        var key = (entry.Sequence, entry.Option);
        if (!_products.ContainsKey(key) && _requestedProducts.Add(key))
            _ = ObserveSend(MuGame.Network.GetCharacterService().RequestCashShopProductAsync(_session, entry.Sequence, entry.Option), _session);
    }

    public void SetProduct(CashShopProductReply reply)
    {
        if (Visible && reply.Request == _session && reply.CharacterName == _owner)
            _products[(reply.Sequence, reply.Option)] = reply.Product;
    }

    public void BuyResult(byte result)
    {
        if (_operation != "buy") return;
        _operation = string.Empty;
        if (!Visible) return;
        _detail = null;
        _status = result switch
        {
            0 => "Purchase successful. Select the product in storage and press Use.",
            1 => "Not enough coins.", 2 => "XShop storage is full.",
            3 or 4 or 5 => "This product is no longer available. Refresh the catalog.",
            6 or 7 or 8 => "The server rejected this product or price option.",
            9 => "Currency mismatch. Refresh the catalog.",
            _ => "The server could not complete the purchase. Check your balances and storage.",
        };
        if (result == 0) { _gifts = false; _storagePage = 1; }
        RequestStorage();
        _ = ObserveSend(MuGame.Network.GetCharacterService().RequestCashShopBalancesAsync(), _session);
    }

    public void UseResult(byte result)
    {
        if (_operation != "use") return;
        _operation = string.Empty;
        if (!Visible) return;
        _withdraw = null;
        _status = result switch
        {
            0 => "Product received successfully.", 1 => "Storage entry changed. Please select it again.",
            21 => "Not enough space in your inventory.",
            22 or 24 => "This product cannot be used now. It remains in storage.",
            _ => "The server could not deliver the product. Refresh to synchronize storage.",
        };
        RequestStorage();
    }

    private static Rectangle Card(int i) => new(104 + i % 3 * 122, 35 + i / 3 * 112, 110, 88);
    private static Rectangle CardBuy(int i) => new(Card(i).Right - 52, Card(i).Bottom + 3, 52, 19);
    private static Rectangle StorageRow(int i) => new(489, 198 + i * 13, 137, 13);
    private static Rectangle FilterRow(int i) => new(8, 39 + i * 25, 86, 23);
    private Rectangle CurrencyTab(int coin)
    {
        int index = 0;
        for (int i = 0; i < coin; i++) if (CurrencyVisible(i)) index++;
        return new Rectangle(104 + index * 122, 8, 110, 23);
    }
    private bool CurrencyVisible(int coin)
    {
        foreach (var currency in _currencies) if (currency.Coin == coin) return currency.Visible;
        // A server without access metadata must never enable additional currencies locally.
        return _currencies.Length == 0;
    }
    private bool CurrencyEnabled(int coin)
    {
        foreach (var currency in _currencies) if (currency.Coin == coin) return currency.Visible && currency.Enabled;
        return _currencies.Length == 0 && coin == 0;
    }
    private static string DurationLabel(double seconds) => seconds <= 0 ? "--"
        : seconds >= 86400 ? $"{seconds / 86400:0.##} d"
        : seconds >= 3600 ? $"{seconds / 3600:0.##} h" : $"{Math.Ceiling(seconds / 60)} m";
    private static string CatalogPrice(CashShopPackageInfo package)
    {
        if (package.Bundle) return $"{package.Price:N0}";
        int minimum = int.MaxValue, maximum = -1;
        foreach (var product in package.Products)
        {
            // Only include options the server currently allows the player to purchase.
            if (!product.Available || product.Price < 0) continue;
            minimum = Math.Min(minimum, product.Price);
            maximum = Math.Max(maximum, product.Price);
        }
        if (maximum < 0) return "--";
        return minimum == maximum ? $"{minimum:N0}" : $"From {minimum:N0}";
    }

    private static string CoinName(int coin) => coin == 0 ? "WCoin C" : coin == 1 ? "WCoin P" : "Goblin Points";
    private static bool CanBuy(CashShopPackageInfo package, int option)
    {
        if (package.Products.Length == 0 || package.Coin is < 0 or > 2) return false;
        if (!package.Bundle) return option >= 0 && option < package.Products.Length
            && package.Products[option].Available && package.Products[option].Price >= 0;
        if (package.Price < 0) return false;
        foreach (var product in package.Products) if (!product.Available) return false;
        return true;
    }

    private void PositionWindow()
    {
        var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
        Scale = MathHelper.Clamp(Math.Min((size.X - 20f) / Width, (size.Y - 20f) / Height), 0.1f, 1.6f);
        X = (size.X - DisplaySize.X) / 2;
        Y = (size.Y - DisplaySize.Y) / 2;
    }

    public override void Update(GameTime gameTime)
    {
        if (!Visible) return;
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || state.Name != _owner || (int)state.Class != _ownerClass)
        { ResetSession(); return; }
        PositionWindow();
        base.Update(gameTime);
        PreparePreviews();
        if ((_opening || _operation.Length > 0) && DateTime.UtcNow > _deadline)
            _status = "No response. Do not repeat the purchase; reconnect to synchronize.";
        if (_catalogWaiting && DateTime.UtcNow > _catalogDeadline)
        { _catalogWaiting = false; _status = "Catalog unavailable. Install the server patch, then refresh."; }
        if (_storageWaiting && DateTime.UtcNow > _storageDeadline)
            _status = "Storage response incomplete. Close and reopen XShop to synchronize.";
        if (Scene?.IsPointerInputBlockedFor(this) == true) return;
        var game = MuGame.Instance;
        if (HasFocus && game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
        {
            Scene?.ConsumeKeyboardEscape();
            if (_detail != null || _withdraw != null) { if (_operation.Length == 0) { _detail = null; _withdraw = null; } else CloseWindow(); }
            else CloseWindow();
            return;
        }
        _mouse = new Point((int)Math.Floor((game.UiMouseState.X - DisplayPosition.X) / Scale),
            (int)Math.Floor((game.UiMouseState.Y - DisplayPosition.Y) / Scale));
        if (game.UiMouseState.LeftButton != ButtonState.Pressed || game.PrevUiMouseState.LeftButton != ButtonState.Released
            || !DisplayRectangle.Contains(game.UiMouseState.Position)) return;
        Scene?.SetMouseInputConsumed();
        if (_detail != null || _withdraw != null) { ClickModal(); return; }
        if (CloseButton.Contains(_mouse)) { CloseWindow(); return; }
        if (!_allowed || _operation.Length > 0) return;
        if (RefreshButton.Contains(_mouse)) { Refresh(); return; }
        if (!_catalogWaiting)
        {
            for (int coin = 0; coin < 3; coin++)
                if (CurrencyVisible(coin) && CurrencyTab(coin).Contains(_mouse))
                {
                    if (CurrencyEnabled(coin)) { _filter = coin; _catalogPage = 1; _status = string.Empty; RequestCatalog(); }
                    else _status = "This currency is currently disabled.";
                    return;
                }
            for (int i = 0; i < Categories.Length; i++)
                if (FilterRow(i).Contains(_mouse)) { _category = i; _catalogPage = 1; _status = string.Empty; RequestCatalog(); return; }
            if (_catalog != null)
            {
                if (CatalogPrevious.Contains(_mouse) && _catalogPage > 1) { _catalogPage--; RequestCatalog(); return; }
                if (CatalogNext.Contains(_mouse) && _catalogPage < _catalog.Pages) { _catalogPage++; RequestCatalog(); return; }
                for (int i = 0; i < _catalog.Packages.Length; i++)
                    if (CurrencyEnabled(_filter) && (Card(i).Contains(_mouse) || CardBuy(i).Contains(_mouse)))
                    { _detail = _catalog.Packages[i]; _option = _optionPage = 0; _confirm = false; return; }
            }
        }
        if (_storageWaiting) return;
        if (new Rectangle(488, 159, 70, 22).Contains(_mouse) || new Rectangle(558, 159, 70, 22).Contains(_mouse))
        { _gifts = _mouse.X >= 558; _storagePage = 1; RequestStorage(); return; }
        if (StoragePrevious.Contains(_mouse) && _storagePage > 1) { _storagePage--; RequestStorage(); return; }
        if (StorageNext.Contains(_mouse) && _storagePage < _storagePages) { _storagePage++; RequestStorage(); return; }
        for (int i = 0; i < _storage.Count; i++)
            if (StorageRow(i).Contains(_mouse)) { _selectedStorage = i; return; }
        if (UseButton.Contains(_mouse) && _selectedStorage >= 0 && _selectedStorage < _storage.Count)
        {
            var entry = _storage[_selectedStorage];
            if (_products.TryGetValue((entry.Sequence, entry.Option), out var product) && product?.Available == true)
            { _withdraw = entry; _confirm = true; }
            else _status = "Product information is unavailable or delivery is disabled on the server.";
        }
    }

    private void ClickModal()
    {
        if (ModalClose.Contains(_mouse))
        {
            if (_operation.Length > 0) CloseWindow();
            else { _detail = null; _withdraw = null; }
            return;
        }
        if (_operation.Length > 0) return;
        if (_detail != null && !_confirm)
        {
            int pages = Math.Max(1, (_detail.Products.Length + 5) / 6);
            if (new Rectangle(161, 284, 30, 22).Contains(_mouse) && _optionPage > 0) { _optionPage--; return; }
            if (new Rectangle(449, 284, 30, 22).Contains(_mouse) && _optionPage < pages - 1) { _optionPage++; return; }
            if (!_detail.Bundle)
                for (int i = 0; i < 6 && _optionPage * 6 + i < _detail.Products.Length; i++)
                    if (new Rectangle(161, 124 + i * 25, 318, 23).Contains(_mouse)) { _option = _optionPage * 6 + i; return; }
        }
        if (!BuyButton.Contains(_mouse) || _storageWaiting || _catalogWaiting || !_allowed) return;
        if (_withdraw != null)
        {
            _operation = "use";
            _deadline = DateTime.UtcNow.AddSeconds(10);
            _ = ObserveSend(MuGame.Network.GetCharacterService().UseCashShopProductAsync(_withdraw.Index, _withdraw.ItemSequence), _session);
        }
        else if (_detail != null && CurrencyEnabled(_detail.Coin) && CanBuy(_detail, _option))
        {
            if (!_confirm) { _confirm = true; return; }
            _operation = "buy";
            _deadline = DateTime.UtcNow.AddSeconds(10);
            _ = ObserveSend(MuGame.Network.GetCharacterService().BuyCashShopProductAsync(_detail.Sequence,
                _detail.Bundle ? 0 : _detail.Products[_option].Option, _detail.Coin), _session);
        }
    }

    private void PreparePreviews()
    {
        if (_catalog == null || Status != GameControlStatus.Ready || DateTime.UtcNow < _nextPreviewAttempt) return;
        _nextPreviewAttempt = DateTime.UtcNow.AddMilliseconds(500);
        foreach (var package in _catalog.Packages)
        {
            if (package.Products.Length == 0) continue;
            var product = package.Products[0];
            var key = (product.Group, product.Number, product.Level);
            if (_previews.TryGetValue(key, out var preview) && preview != null && !preview.IsDisposed) continue;
            var definition = product.Group is >= 0 and <= 15 && product.Number is >= 0 and <= 511
                ? ItemDatabase.GetItemDefinition((byte)product.Group, (short)product.Number) : null;
            var raw = new byte[12];
            raw[0] = (byte)product.Number;
            raw[1] = (byte)(Math.Clamp(product.Level, 0, 15) << 3);
            raw[3] = (byte)((product.Number & 256) >> 1);
            raw[5] = (byte)(product.Group << 4);
            _previews[key] = definition == null ? null : BmdPreviewRenderer.RetryMissingPreview(new InventoryItem(definition, Point.Zero, raw, 255, product.Level), 96, 96);
        }
    }

    private void EnsureBackground()
    {
        if (_background != null && !_background.IsDisposed) return;
        var graphics = GraphicsManager.Instance;
        var device = graphics.GraphicsDevice;
        var targets = device.GetRenderTargets();
        var viewport = device.Viewport;
        _background = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
        _backgroundBlend ??= new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };
        try
        {
            using var scope = new SpriteBatchScope(graphics.Sprite, SpriteSortMode.Deferred, _backgroundBlend, SamplerState.LinearClamp);
            device.SetRenderTarget(_background);
            device.Clear(Theme.Back);
            if (_atlas != null)
            {
                graphics.Sprite.Draw(_atlas, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, Width, Height), Color.White);
                // Reuse the atlas stone and item frame, with space for each original Buy button.
                graphics.Sprite.Draw(_atlas, new Rectangle(99, 31, 373, 337), new Rectangle(40, 250, 48, 100), Color.White);
                for (int i = 0; i < 9; i++)
                {
                    var card = Card(i); card.Inflate(2, 2);
                    graphics.Sprite.Draw(_atlas, card, new Rectangle(101, 34, 114, 92), Color.White);
                }
            }
        }
        finally { device.SetRenderTargets(targets); device.Viewport = viewport; }
    }

    public override void Draw(GameTime gameTime) { }

    /// <summary>Draws once after scene notifications and sibling tooltips.</summary>
    public void DrawOverlay(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready) return;
        EnsureBackground();
        var graphics = GraphicsManager.Instance;
        var batch = graphics.Sprite;
        var font = graphics.Font;
        var pixel = graphics.Pixel;
        if (font == null || pixel == null) return;
        var transform = Matrix.CreateScale(Scale, Scale, 1f) * Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0) * UiScaler.SpriteTransform;
        using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, transform: transform))
            batch.Draw(_background, new Rectangle(0, 0, Width, Height), Color.White);
        using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, transform: transform);
        Text(batch, font, "Categories", new Rectangle(9, 12, 85, 17), Theme.Gold, 9, true);
        for (int i = 0; i < Categories.Length; i++)
            SkinButton(batch, font, pixel, FilterRow(i), Categories[i], _allowed && !_catalogWaiting, _category == i, 0, 3);
        for (int coin = 0; coin < 3; coin++)
            if (CurrencyVisible(coin)) SkinButton(batch, font, pixel, CurrencyTab(coin), CoinName(coin),
                _allowed && !_catalogWaiting && CurrencyEnabled(coin), _filter == coin, 1, 2);
        var balances = MuGame.Network?.GetCharacterService()?.StoreCurrencies;
        Text(batch, font, "Account Balance", new Rectangle(490, 14, 135, 17), Theme.Gold, 9, true);
        int balanceRow = 0;
        for (int coin = 0; coin < 3; coin++)
        {
            if (!CurrencyVisible(coin)) continue;
            string value = balances?.HasBalance != true ? "--" : (coin == 0 ? balances.WCoinC : coin == 1 ? balances.WCoinP : balances.GoblinPoints).ToString("N0");
            Text(batch, font, CoinName(coin), new Rectangle(496, 38 + balanceRow * 16, 72, 15), CurrencyEnabled(coin) ? Theme.Text : Theme.Muted, 8);
            Text(batch, font, value, new Rectangle(570, 38 + balanceRow * 16, 51, 15), CurrencyEnabled(coin) ? Theme.Gold : Theme.Muted, 8, right: true);
            balanceRow++;
        }
        // The supplied original banner is decorative; it does not advertise products absent from this server.
        if (_skin[4] != null) batch.Draw(_skin[4], new Rectangle(489, 114, 138, 42), Color.White);
        Panel(batch, pixel, new Rectangle(487, 181, 142, 134), Color.Black);
        Text(batch, font, "Item Name", new Rectangle(491, 183, 94, 12), Theme.Gold, 7);
        Text(batch, font, "Duration", new Rectangle(585, 183, 40, 12), Theme.Gold, 7, true);
        if (_catalog != null)
            for (int i = 0; i < _catalog.Packages.Length && i < 9; i++)
            {
                var package = _catalog.Packages[i];
                var card = Card(i);
                bool available = false;
                for (int j = 0; j < package.Products.Length; j++) if (CanBuy(package, j)) { available = true; break; }
                if (card.Contains(_mouse) || CardBuy(i).Contains(_mouse))
                    batch.Draw(pixel, new Rectangle(card.X + 2, card.Y + 17, card.Width - 4, 51), new Color(105, 82, 40, 38));
                if (_skin[3] != null) batch.Draw(_skin[3], new Rectangle(card.Center.X - 19, card.Y + 23, 38, 38), new Rectangle(0, 0, 57, 57), new Color(255, 255, 255, 45));
                Text(batch, font, package.Name, new Rectangle(card.X + 3, card.Y, card.Width - 6, 15), available ? Theme.Gold : Theme.Muted, 8, true);
                if (package.Products.Length > 0)
                {
                    var first = package.Products[0];
                    if (_previews.TryGetValue((first.Group, first.Number, first.Level), out var preview) && preview != null && !preview.IsDisposed)
                    {
                        using var alpha = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, transform: transform);
                        int size = first.Group <= 11 ? 42 : 32;
                        batch.Draw(preview, new Rectangle(card.Center.X - size / 2, card.Y + 17 + (51 - size) / 2, size, size), available ? Color.White : Color.Gray);
                    }
                    string price = CatalogPrice(package);
                    Text(batch, font, available ? $"{price} {CoinName(package.Coin)}" : "Unavailable", new Rectangle(card.X + 3, card.Y + 69, card.Width - 6, 17), available ? Theme.Text : Theme.Muted, 8, true);
                }
                SkinButton(batch, font, pixel, CardBuy(i), "Buy", available && CurrencyEnabled(package.Coin) && !_catalogWaiting, false, 2, 3);
            }
        Arrow(batch, font, pixel, CatalogPrevious, _catalogPage > 1, false);
        Arrow(batch, font, pixel, CatalogNext, _catalog != null && _catalogPage < _catalog.Pages, true);
        Text(batch, font, _catalogWaiting ? "Loading..." : $"Page {_catalogPage} / {_catalog?.Pages ?? 1}", new Rectangle(146, 374, 285, 20), Theme.Gold, 10, true);
        StorageTab(batch, font, pixel, new Rectangle(488, 159, 70, 22), "Storage", !_storageWaiting, !_gifts);
        StorageTab(batch, font, pixel, new Rectangle(558, 159, 70, 22), "Gifts", !_storageWaiting, _gifts);
        for (int i = 0; i < _storage.Count; i++)
        {
            var row = StorageRow(i);
            if (i == _selectedStorage) Panel(batch, pixel, row, Theme.Selected);
            var entry = _storage[i];
            bool known = _products.TryGetValue((entry.Sequence, entry.Option), out var product);
            string label = product != null ? $"{product.Name} x{product.Quantity}" : known ? "Product not configured" : "Loading product...";
            Text(batch, font, label, new Rectangle(row.X + 3, row.Y, 93, row.Height), i == _selectedStorage ? Theme.Gold : Theme.Text, 7);
            Text(batch, font, product == null ? "--" : DurationLabel(product.Duration), new Rectangle(585, row.Y, 39, row.Height), Theme.Text, 7, true);
        }
        if (_storage.Count == 0) Text(batch, font, _storageWaiting ? "Loading..." : "Empty storage", new Rectangle(490, 200, 134, 22), Theme.Muted, 10, true);
        SkinButton(batch, font, pixel, UseButton, "Use", !_storageWaiting && _selectedStorage >= 0, false, 2, 3);
        Button(batch, font, pixel, StoragePrevious, "<", !_storageWaiting && _storagePage > 1);
        Button(batch, font, pixel, StorageNext, ">", !_storageWaiting && _storagePage < _storagePages);
        Text(batch, font, $"{_storagePage} / {_storagePages}", new Rectangle(518, 343, 80, 20), Theme.Gold, 10, true);
        Button(batch, font, pixel, CloseButton, "X", true);
        Button(batch, font, pixel, RefreshButton, "Reload", _allowed && !_catalogWaiting && !_storageWaiting && _operation.Length == 0);
        Text(batch, font, _status, new Rectangle(105, 399, 520, 17), Theme.Text, 10);
        if (_detail != null || _withdraw != null) DrawModal(batch, font, pixel);
    }

    private void DrawModal(SpriteBatch batch, SpriteFont font, Texture2D pixel)
    {
        batch.Draw(pixel, new Rectangle(0, 0, Width, Height), new Color(0, 0, 0, 175));
        Panel(batch, pixel, new Rectangle(151, 70, 338, 291), Theme.Panel);
        Text(batch, font, _withdraw == null ? "Purchase" : "Receive Product", new Rectangle(161, 79, 286, 22), Theme.Gold, 14, true);
        Button(batch, font, pixel, ModalClose, "X", true);
        if (_withdraw != null)
        {
            _products.TryGetValue((_withdraw.Sequence, _withdraw.Option), out var product);
            Text(batch, font, product?.Name ?? "Product", new Rectangle(165, 135, 310, 25), Theme.Gold, 13, true);
            Text(batch, font, $"Quantity: {product?.Quantity ?? 0}", new Rectangle(165, 169, 310, 25), Theme.Text, 12, true);
            Text(batch, font, "Receive this product now?", new Rectangle(165, 226, 310, 25), Theme.Text, 12, true);
            if (_withdraw.Sender.Length > 0) Text(batch, font, $"From: {_withdraw.Sender}", new Rectangle(165, 264, 310, 25), Theme.Gold, 11, true);
        }
        else if (_detail != null)
        {
            Text(batch, font, _detail.Name, new Rectangle(165, 103, 310, 19), Theme.Gold, 12, true);
            if (!_confirm)
            {
                for (int i = 0; i < 6 && _optionPage * 6 + i < _detail.Products.Length; i++)
                {
                    int index = _optionPage * 6 + i;
                    var product = _detail.Products[index];
                    string duration = product.Duration > 0 ? $" ({product.Duration / 86400:0.##} days)" : string.Empty;
                    string line = $"{product.Name} x{product.Quantity}{duration}";
                    if (!_detail.Bundle) line += $" - {product.Price:N0}";
                    if (!product.Available) line += " (Unavailable)";
                    Button(batch, font, pixel, new Rectangle(161, 124 + i * 25, 318, 23), line, product.Available, !_detail.Bundle && index == _option);
                }
                int pages = Math.Max(1, (_detail.Products.Length + 5) / 6);
                Button(batch, font, pixel, new Rectangle(161, 284, 30, 22), "<", _optionPage > 0);
                Button(batch, font, pixel, new Rectangle(449, 284, 30, 22), ">", _optionPage < pages - 1);
                Text(batch, font, _detail.Bundle ? $"Bundle: {_detail.Price:N0} {CoinName(_detail.Coin)}" : CoinName(_detail.Coin), new Rectangle(194, 284, 252, 22), Theme.Gold, 11, true);
            }
            else
            {
                string product = _detail.Bundle ? $"Bundle ({_detail.Products.Length} products)" : $"{_detail.Products[_option].Name} x{_detail.Products[_option].Quantity}";
                int price = _detail.Bundle ? _detail.Price : _detail.Products[_option].Price;
                Text(batch, font, product, new Rectangle(165, 143, 310, 25), Theme.Text, 12, true);
                Text(batch, font, $"Total: {price:N0} {CoinName(_detail.Coin)}", new Rectangle(165, 183, 310, 25), Theme.Gold, 14, true);
                Text(batch, font, "Confirm purchase to XShop storage?", new Rectangle(165, 237, 310, 25), Theme.Text, 12, true);
            }
        }
        bool canBuy = _withdraw != null || _detail != null && CurrencyEnabled(_detail.Coin) && CanBuy(_detail, _option);
        Button(batch, font, pixel, BuyButton, _operation.Length > 0 ? "Waiting..." : _withdraw != null ? "Use" : _confirm ? "Confirm" : canBuy ? "Buy" : "Unavailable",
            canBuy && _allowed && !_storageWaiting && !_catalogWaiting && _operation.Length == 0);
        if (_operation.Length > 0 && DateTime.UtcNow > _deadline)
            Text(batch, font, "No response. Close and reconnect to synchronize.", new Rectangle(161, 347, 318, 12), Theme.Gold, 9, true);
    }

    private void StorageTab(SpriteBatch batch, SpriteFont font, Texture2D pixel, Rectangle rect, string label, bool enabled, bool selected)
    {
        var texture = _skin[label == "Storage" ? 7 : 8];
        if (texture == null) { Button(batch, font, pixel, rect, label, enabled, selected); return; }
        batch.Draw(texture, rect, new Rectangle(0, 0, 49, 20), selected ? Color.White : Color.Gray);
        Text(batch, font, label, rect, selected ? Theme.Gold : Theme.Muted, 8, true);
    }

    private void SkinButton(SpriteBatch batch, SpriteFont font, Texture2D pixel, Rectangle rect, string label, bool enabled, bool selected, int skin, int states)
    {
        var texture = _skin[skin];
        if (texture == null) { Button(batch, font, pixel, rect, label, enabled, selected); return; }
        int frame = states == 2 ? (selected ? 1 : 0) : selected ? 1 : enabled && rect.Contains(_mouse) ? 2 : 0;
        int height = SkinSizes[skin].Y / states;
        batch.Draw(texture, rect, new Rectangle(0, frame * height, SkinSizes[skin].X, height), enabled ? Color.White : Color.Gray);
        Text(batch, font, label, new Rectangle(rect.X + 4, rect.Y, rect.Width - 8, rect.Height), enabled ? Theme.Text : Theme.Muted, 8, true);
    }

    private void Arrow(SpriteBatch batch, SpriteFont font, Texture2D pixel, Rectangle rect, bool enabled, bool right)
    {
        var texture = _skin[right ? 6 : 5];
        if (texture == null) { Button(batch, font, pixel, rect, right ? ">" : "<", enabled); return; }
        const int height = 23;
        int frame = !enabled ? 2 : rect.Contains(_mouse) ? 1 : 0;
        batch.Draw(texture, new Rectangle(rect.Center.X - 10, rect.Y, 20, rect.Height), new Rectangle(0, frame * height, 20, height), enabled ? Color.White : Color.Gray);
    }

    private void Button(SpriteBatch batch, SpriteFont font, Texture2D pixel, Rectangle rect, string label, bool enabled, bool selected = false)
    {
        if (_atlas == null) Panel(batch, pixel, rect, selected ? Theme.Selected : Theme.Button);
        else
        {
            // Original GFx normal, hover and selected button faces. Preserve the four-pixel borders.
            int sx = selected || enabled && rect.Contains(_mouse) ? 722 : 646;
            int sy = selected ? 40 : 1;
            var source = new Rectangle(sx, sy, 69, 29);
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    int sw = column == 1 ? source.Width - 8 : 4;
                    int sh = row == 1 ? source.Height - 8 : 4;
                    int dw = column == 1 ? rect.Width - 8 : 4;
                    int dh = row == 1 ? rect.Height - 8 : 4;
                    var from = new Rectangle(source.X + (column == 0 ? 0 : column == 1 ? 4 : source.Width - 4),
                        source.Y + (row == 0 ? 0 : row == 1 ? 4 : source.Height - 4), sw, sh);
                    var to = new Rectangle(rect.X + (column == 0 ? 0 : column == 1 ? 4 : rect.Width - 4),
                        rect.Y + (row == 0 ? 0 : row == 1 ? 4 : rect.Height - 4), dw, dh);
                    batch.Draw(_atlas, to, from, enabled ? Color.White : Color.Gray);
                }
        }
        Text(batch, font, label, new Rectangle(rect.X + 3, rect.Y, rect.Width - 6, rect.Height), enabled ? Theme.Gold : Theme.Muted, 9, true);
    }

    private static void Panel(SpriteBatch batch, Texture2D pixel, Rectangle rect, Color fill)
    {
        batch.Draw(pixel, rect, Theme.Border);
        rect.Inflate(-1, -1);
        batch.Draw(pixel, rect, fill);
    }

    private static void Text(SpriteBatch batch, SpriteFont font, string value, Rectangle bounds, Color color, float size, bool center = false, bool right = false)
    {
        value ??= string.Empty;
        var measure = font.MeasureString(value);
        float scale = Math.Min(size / Constants.BASE_FONT_SIZE, bounds.Width / Math.Max(1f, measure.X));
        var measured = measure * scale;
        batch.DrawString(font, value, new Vector2(right ? bounds.Right - measured.X : center ? bounds.Center.X - measured.X / 2 : bounds.X,
            bounds.Center.Y - measured.Y / 2), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    public override void Dispose()
    {
        ResetSession();
        _background?.Dispose();
        _background = null;
        _backgroundBlend?.Dispose();
        _backgroundBlend = null;
        base.Dispose();
    }
}
