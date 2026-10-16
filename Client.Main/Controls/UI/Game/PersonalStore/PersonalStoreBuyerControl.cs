using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Items.Tooltips;
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
    /// Read-only view of another player's Season 6 Personal Store. All contents
    /// come from C2 3F 05; never shares the seller's editable store state.
    /// </summary>
    public sealed class PersonalStoreBuyerControl : UIControl, IUiTexturePreloadable
    {
        private const int WidthPx = 348;
        private const int HeightPx = 640;
        private const int Columns = 8;
        private const int Rows = 4;
        private const int Cell = GameWindowMetrics.InventoryCell;
        private const int FirstSlot = PersonalStoreControl.FirstStoreSlot;
        private static readonly Rectangle Grid = new((WidthPx - Columns * Cell) / 2, 249, Columns * Cell, Rows * Cell);
        private static readonly Rectangle Close = new(WidthPx - 44, 35, 25, 25);
        private static readonly Rectangle Buy = new(25, 542, 142, 35);
        private static readonly Rectangle Exit = new(181, 542, 142, 35);
        private static readonly Rectangle ConfirmPanel = new(30, 226, 288, 174);
        private static readonly Rectangle ConfirmYes = new(44, 350, 122, 34);
        private static readonly Rectangle ConfirmNo = new(182, 350, 122, 34);
        private static readonly Rectangle SlotSource = new(546, 220, 29, 29);
        private static PersonalStoreBuyerControl _instance;
        public static PersonalStoreBuyerControl Instance => _instance ??= new PersonalStoreBuyerControl();
        // Buyer and seller windows belong to the current GameScene, not the app lifetime.
        public override bool NonDisposable => false;
        public ushort SellerId => _sellerId;
        public bool IsViewing(ushort sellerId) => Visible && _sellerId == (sellerId & 0x7FFF);

        private readonly Dictionary<byte, Listing> _listings = new();
        private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
        private Texture2D _atlas, _slots, _button, _close;
        private SpriteFont _font;
        private ushort _sellerId;
        private string _sellerName = string.Empty;
        private string _shopName = string.Empty;
        private byte? _selectedSlot;
        private byte? _hoveredSlot;
        private bool _confirm;
        private bool _buyPending;
        private int _buyRequestGeneration;
        private string _feedback = "Select an item to see its Zen price.";
        private float _previewCooldown;
        private int _previewCursor;

        public sealed class Listing
        {
            public byte Slot { get; init; }
            public uint Price { get; init; }
            public InventoryItem Item { get; init; }
            public List<(string text, Color color)> TooltipLines { get; init; }
            public string DisplayName { get; init; }
            public Color NameColor { get; init; }
        }

        private PersonalStoreBuyerControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(WidthPx, HeightPx);
            ViewSize = ControlSize;
            Align = ControlAlign.None;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return NewUiFrameRenderer.PersonalShopAtlasPath;
            yield return "Interface/GFx/NpcShop_I3.ozd";
            yield return "Interface/GFx/btn_l01.ozd";
            yield return "Interface/GFx/btn_close01.ozd";
        }

        public override async Task Load()
        {
            await base.Load();
            var tex = await Task.WhenAll(
                TextureLoader.Instance.PrepareAndGetTexture(NewUiFrameRenderer.PersonalShopAtlasPath),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/NpcShop_I3.ozd"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_close01.ozd"));
            _atlas = tex[0];
            _slots = tex[1];
            _button = tex[2];
            _close = tex[3];
            _font = GraphicsManager.Instance.Font;
        }

        /// <summary>Called on the MonoGame main thread after parsing a confirmed server list.</summary>
        public void ShowListing(ushort sellerId, string sellerName, string storeName,
            IReadOnlyList<(byte Slot, uint Price, byte[] Data)> received)
        {
            bool sameSeller = Visible && IsViewing(sellerId);
            _sellerId = (ushort)(sellerId & 0x7FFF);
            _sellerName = sellerName ?? string.Empty;
            _shopName = storeName ?? string.Empty;
            _listings.Clear();
            _hoveredSlot = null;
            foreach (var entry in received)
            {
                int cell = entry.Slot - FirstSlot;
                if (cell < 0 || cell >= Columns * Rows || entry.Data == null)
                    continue;
                var def = ItemDatabase.GetItemDefinition(entry.Data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(entry.Data) ?? "Unknown Item",
                        1, 1, "Interface/newui_item_box.tga");
                var item = new InventoryItem(def, new Point(cell % Columns, cell / Columns),
                    entry.Data);
                // These options and the item title only change on authoritative
                // list updates. Compute once, not every frame (important on Android).
                var tooltip = ClassicItemTooltipBuilder.Build(item);
                string fullName = tooltip.Count > 0 ? tooltip[0].text : def.Name;
                Color nameColor = tooltip.Count > 0 ? tooltip[0].color : Color.Wheat;
                tooltip.Add(("Store Price: " + entry.Price.ToString("N0", CultureInfo.InvariantCulture)
                    + " Zen", new Color(255, 220, 130)));
                _listings[entry.Slot] = new Listing
                {
                    Slot = entry.Slot, Price = entry.Price, Item = item,
                    TooltipLines = tooltip, DisplayName = fullName, NameColor = nameColor
                };
                if (!string.IsNullOrEmpty(def.TexturePath) &&
                    !def.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                    _ = TextureLoader.Instance.Prepare(def.TexturePath);
            }

            if (!_selectedSlot.HasValue || !_listings.ContainsKey(_selectedSlot.Value))
                _selectedSlot = null;
            _confirm = false;
            if (!sameSeller)
            {
                _buyPending = false;
                _buyRequestGeneration++;
            }
            _feedback = _listings.Count == 0
                ? "This store has no items for sale."
                : "Select an item to see its Zen price.";
            _previewCursor = 0;
            _previewCooldown = 0;

            if (Parent != null)
            {
                X = Math.Max(8, (Parent.DisplaySize.X - WidthPx) / 2);
                Y = Math.Max(8, (Parent.DisplaySize.Y - HeightPx) / 2);
            }
            if (!Visible)
                SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
            Visible = true;
            BringToFront();
        }

        public void HideWindow(bool notifyServer = true)
        {
            if (notifyServer && Visible && _sellerName.Length > 0)
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service != null)
                    _ = service.SendPlayerStoreCloseOtherAsync(_sellerId, _sellerName);
            }
            Visible = false;
            _confirm = false;
            _buyPending = false;
            _buyRequestGeneration++;
            _listings.Clear();
            _selectedSlot = null;
            _hoveredSlot = null;
        }

        public void HandleSellerClosed(ushort id)
        {
            if ((id & 0x7FFF) != _sellerId || !Visible)
                return;
            HideWindow(false);
            PostMessage("The seller closed the store.");
        }

        public void HandleBuyResult(byte result)
        {
            if (!_buyPending)
                return;
            _buyPending = false;
            _buyRequestGeneration++;
            _confirm = false;
            string reason = result switch
            {
                1 => "Purchase confirmed by OpenMU.",
                2 => "The seller is unavailable.",
                3 => "The store is closed.",
                4 => "The seller is busy.",
                5 => "This item is no longer available.",
                6 => "Seller name or item price no longer matches.",
                7 => "Not enough Zen.",
                8 => "Not enough inventory space, or seller Zen limit reached.",
                9 => "This item is blocked.",
                _ => $"Purchase rejected by OpenMU (result {result})."
            };
            _feedback = reason;
            PostMessage(reason);
            if (result == 1)
            {
                // Wait for the authoritative C2 3F 05 update. Never invent
                // a replacement item, slot, or Zen balance locally.
                _selectedSlot = null;
            }
        }

        private static void PostMessage(string message)
        {
            if (MuGame.Instance?.ActiveScene is Client.Main.Scenes.GameScene scene)
                scene.ChatLog?.AddMessage("Personal Store", message, MessageType.System);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
                return;
            base.Update(gameTime);

            // BMD previews must be produced outside the active Draw SpriteBatch.
            _previewCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_previewCooldown <= 0 && _listings.Count > 0)
            {
                _previewCooldown = 0.25f;
                var values = _listings.Values.ToArray();
                for (int attempt = 0; attempt < values.Length; attempt++)
                {
                    var item = values[_previewCursor++ % values.Length].Item;
                    if (string.IsNullOrEmpty(item.Definition.TexturePath) ||
                        !item.Definition.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int w = item.Definition.Width * Cell;
                    int h = item.Definition.Height * Cell;
                    if (BmdPreviewRenderer.TryGetCachedPreview(item, w, h) == null)
                    {
                        try { BmdPreviewRenderer.RetryMissingPreview(item, w, h); }
                        catch { /* Asset can still be loading after reconnection. */ }
                        break;
                    }
                }
            }

            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool inside = DisplayRectangle.Contains(mouse.Position);
            _hoveredSlot = !_confirm && inside ? FindItemAt(mouse.Position)?.Slot : null;
            if (inside && mouse.LeftButton == ButtonState.Pressed)
                Scene?.SetMouseInputConsumed();

            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                if (_confirm) _confirm = false;
                else HideWindow();
                return;
            }

            if (!inside || mouse.LeftButton != ButtonState.Released ||
                previous.LeftButton != ButtonState.Pressed)
                return;

            Scene?.SetMouseInputConsumed();
            if (_confirm)
            {
                if (ToScreen(ConfirmYes).Contains(mouse.Position))
                    _ = SendBuyRequestAsync();
                else if (ToScreen(ConfirmNo).Contains(mouse.Position))
                    _confirm = false;
                return;
            }

            if (ToScreen(Close).Contains(mouse.Position) || ToScreen(Exit).Contains(mouse.Position))
            {
                HideWindow();
                return;
            }
            if (ToScreen(Buy).Contains(mouse.Position))
            {
                if (!_buyPending && _selectedSlot.HasValue &&
                    _listings.TryGetValue(_selectedSlot.Value, out var selected) &&
                    selected.Price > 0)
                    _confirm = true;
                return;
            }

            // Hit-test item rectangles, not only their first cell. Boots/armor/
            // large weapons must show the correct item on any occupied cell.
            if (ToScreen(Grid).Contains(mouse.Position))
                _selectedSlot = _hoveredSlot;
        }

        private Listing FindItemAt(Point mousePosition)
        {
            foreach (var listing in _listings.Values)
            {
                if (GetItemRect(listing.Item).Contains(mousePosition))
                    return listing;
            }
            return null;
        }

        private Rectangle GetItemRect(InventoryItem item) =>
            ToScreen(new Rectangle(Grid.X + item.GridPosition.X * Cell,
                Grid.Y + item.GridPosition.Y * Cell,
                item.Definition.Width * Cell, item.Definition.Height * Cell));

        private async Task SendBuyRequestAsync()
        {
            if (!_selectedSlot.HasValue || !_listings.ContainsKey(_selectedSlot.Value) || _buyPending)
                return;
            byte slot = _selectedSlot.Value;
            _confirm = false;
            _buyPending = true;
            int generation = ++_buyRequestGeneration;
            _feedback = "Waiting for purchase confirmation from OpenMU...";
            var service = MuGame.Network?.GetCharacterService();
            bool sent = service != null && await service.SendPlayerStoreBuyRequestAsync(
                _sellerId, _sellerName, slot);
            if (!sent)
            {
                MuGame.ScheduleOnMainThread(() => HandleBuyFailure(generation, "Could not send the purchase request."));
                return;
            }
            await Task.Delay(8000);
            MuGame.ScheduleOnMainThread(() => HandleBuyFailure(generation, "No purchase response from OpenMU."));
        }

        private void HandleBuyFailure(int generation, string text)
        {
            if (!_buyPending || generation != _buyRequestGeneration) return;
            _buyPending = false;
            _feedback = text;
            PostMessage(text);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready)
                return;
            var sprite = GraphicsManager.Instance?.Sprite;
            var pixel = GraphicsManager.Instance?.Pixel;
            if (sprite == null || pixel == null) return;

            var loader = TextureLoader.Instance;
            if (_atlas == null || _atlas.IsDisposed)
                _atlas = loader.GetTexture2D(NewUiFrameRenderer.PersonalShopAtlasPath);
            if (_slots == null || _slots.IsDisposed)
                _slots = loader.GetTexture2D("Interface/GFx/NpcShop_I3.ozd");
            if (_button == null || _button.IsDisposed)
                _button = loader.GetTexture2D("Interface/GFx/btn_l01.ozd");
            if (_close == null || _close.IsDisposed)
                _close = loader.GetTexture2D("Interface/GFx/btn_close01.ozd");

            using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                // Match the seller's finished NewUI renderer: no black opaque
                // full-window rectangle behind the transparent GFx atlas.
                // Only the interior gets a dark backing like the original store.
                sprite.Draw(pixel, ToScreen(new Rectangle(12, 90, WidthPx - 24, HeightPx - 112)),
                    new Color(25, 23, 23, 255));
                if (_atlas != null && !_atlas.IsDisposed)
                {
                    NewUiFrameRenderer.DrawPersonalShopWindow(sprite, _atlas, DisplayRectangle);
                    NewUiFrameRenderer.DrawPersonalShopInset(sprite, _atlas, ToScreen(new Rectangle(18, 103, 312, 86)));
                    NewUiFrameRenderer.DrawPersonalShopInset(sprite, _atlas, ToScreen(new Rectangle(18, 199, 312, 191)));
                    NewUiFrameRenderer.DrawPersonalShopInset(sprite, _atlas, ToScreen(new Rectangle(18, 402, 312, 129)));
                }

                // Same dark name field and subtle border as PersonalStoreControl.
                var nameBox = ToScreen(new Rectangle(37, 143, 274, 31));
                sprite.Draw(pixel, nameBox, new Color(18, 17, 18, 238));
                sprite.Draw(pixel, new Rectangle(nameBox.X, nameBox.Y, nameBox.Width, 1),
                    new Color(103, 87, 72, 180));

                Text(sprite, "VIEW PERSONAL STORE", ToScreen(new Rectangle(15, 46, 318, 28)), .53f * Scale, new Color(247, 218, 172));
                Text(sprite, _shopName, ToScreen(new Rectangle(32, 126, 284, 29)), .48f * Scale, Color.Wheat);
                Text(sprite, "Seller: " + _sellerName, ToScreen(new Rectangle(32, 157, 284, 24)), .40f * Scale, Color.LightGray);
                Text(sprite, "SHOP ITEMS (8 x 4)", ToScreen(new Rectangle(20, 216, 308, 24)), .43f * Scale, Color.Wheat);

                for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                {
                    var r = ToScreen(new Rectangle(Grid.X + x * Cell, Grid.Y + y * Cell, Cell, Cell));
                    sprite.Draw(pixel, r, new Color(17, 20, 25, 255));
                    if (_slots != null) sprite.Draw(_slots, r, SlotSource, Color.White);
                }

                foreach (var value in _listings.Values)
                {
                    var item = value.Item;
                    var rect = GetItemRect(item);
                    if (value.Slot == _selectedSlot)
                        sprite.Draw(pixel, rect, new Color(181, 128, 56, 110));
                    var texture = ResolveTexture(item);
                    if (texture != null && !texture.IsDisposed)
                        sprite.Draw(texture, rect, Color.White);
                    else
                        ItemGridRenderHelper.DrawItemPlaceholder(sprite, pixel, _font, rect,
                            item, new Color(39, 34, 32, 225), new Color(225, 213, 196));
                }

                Text(sprite, "ITEM INFORMATION", ToScreen(new Rectangle(20, 412, 308, 27)), .43f * Scale, Color.Wheat);
                Listing current = _selectedSlot.HasValue &&
                    _listings.TryGetValue(_selectedSlot.Value, out var found) ? found : null;
                Text(sprite, current?.DisplayName ?? "Select an item",
                    ToScreen(new Rectangle(30, 445, 288, 30)), .42f * Scale,
                    current?.NameColor ?? Color.Wheat);
                Text(sprite, current != null ? "Price: " + current.Price.ToString("N0", CultureInfo.InvariantCulture) + " Zen" : "Price: --",
                    ToScreen(new Rectangle(30, 477, 288, 27)), .43f * Scale, Color.Goldenrod);
                Text(sprite, _feedback, ToScreen(new Rectangle(22, 506, 304, 23)), .32f * Scale, new Color(172, 208, 154));

                Button(sprite, Buy, "Buy Item");
                Button(sprite, Exit, "Exit");
                if (_close != null) sprite.Draw(_close, ToScreen(Close), Color.White);

                if (_confirm)
                {
                    sprite.Draw(pixel, ToScreen(ConfirmPanel), new Color(18, 19, 24, 250));
                    Text(sprite, "CONFIRM PURCHASE", ToScreen(new Rectangle(38, 241, 272, 35)), .53f * Scale, Color.Wheat);
                    Text(sprite, current?.DisplayName ?? "",
                        ToScreen(new Rectangle(38, 283, 272, 28)), .40f * Scale, Color.White);
                    Text(sprite, current == null ? "" : current.Price.ToString("N0", CultureInfo.InvariantCulture) + " Zen",
                        ToScreen(new Rectangle(38, 312, 272, 26)), .43f * Scale, Color.Goldenrod);
                    Button(sprite, ConfirmYes, "Confirm");
                    Button(sprite, ConfirmNo, "Cancel");
                }
            }
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || _confirm)
                return;

            byte? slot = _hoveredSlot ?? _selectedSlot;
            if (!slot.HasValue || !_listings.TryGetValue(slot.Value, out var listing))
                return;
            var sprite = GraphicsManager.Instance?.Sprite;
            if (sprite == null || _font == null)
                return;

            var itemRect = GetItemRect(listing.Item);
            var mouse = MuGame.Instance.UiMouseState.Position;
            if (!_hoveredSlot.HasValue)
                mouse = new Point(itemRect.Right + 8, itemRect.Top);

            using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp,
                transform: UiScaler.SpriteTransform))
            {
                ClassicItemTooltipRenderer.Draw(sprite, _font, listing.Item,
                    listing.TooltipLines, itemRect, mouse);
            }
        }

        private Texture2D ResolveTexture(InventoryItem item)
        {
            string path = item.Definition.TexturePath;
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (path.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                return BmdPreviewRenderer.TryGetCachedPreview(item,
                    item.Definition.Width * Cell, item.Definition.Height * Cell);
            if (!_textures.TryGetValue(path, out var texture) || texture == null || texture.IsDisposed)
            {
                texture = TextureLoader.Instance.GetTexture2D(path);
                if (texture != null) _textures[path] = texture;
            }
            return texture;
        }

        private void Button(SpriteBatch sprite, Rectangle local, string caption)
        {
            Rectangle r = ToScreen(local);
            if (_button != null) sprite.Draw(_button, r, Color.White);
            else sprite.Draw(GraphicsManager.Instance.Pixel, r, new Color(80, 50, 35, 245));
            Text(sprite, caption, r, .44f * Scale, Color.Wheat);
        }

        private void Text(SpriteBatch sprite, string text, Rectangle rect, float scale, Color color)
        {
            if (_font == null || string.IsNullOrWhiteSpace(text)) return;
            var size = _font.MeasureString(text);
            if (size.X <= 0) return;
            scale = Math.Min(scale, (rect.Width - 8f) / size.X);
            var pos = new Vector2(rect.X + (rect.Width - size.X * scale) * .5f,
                rect.Y + (rect.Height - size.Y * scale) * .5f);
            sprite.DrawString(_font, text, pos + Vector2.One, Color.Black,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            sprite.DrawString(_font, text, pos, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public override void Dispose()
        {
            // Do not send a network unsubscribe during an entire scene teardown.
            // Drop stale listings, cached GPU references and the singleton so the
            // next login receives a fully initialized buyer window.
            Visible = false;
            _listings.Clear();
            _textures.Clear();
            _confirm = false;
            _buyPending = false;
            _buyRequestGeneration++;
            _selectedSlot = null;
            _hoveredSlot = null;
            _atlas = null;
            _slots = null;
            _button = null;
            _close = null;
            _font = null;
            if (ReferenceEquals(_instance, this))
                _instance = null;
            base.Dispose();
        }

        private Rectangle ToScreen(Rectangle r)
        {
            int left = DisplayRectangle.X + (int)MathF.Round(r.Left * Scale);
            int top = DisplayRectangle.Y + (int)MathF.Round(r.Top * Scale);
            int right = DisplayRectangle.X + (int)MathF.Round(r.Right * Scale);
            int bottom = DisplayRectangle.Y + (int)MathF.Round(r.Bottom * Scale);
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }
    }
}
