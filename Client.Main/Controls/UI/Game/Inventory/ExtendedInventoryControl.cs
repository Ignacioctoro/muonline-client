using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Items.Tooltips;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Inventory
{
    public sealed class ExtendedInventoryControl : UIControl, IUiTexturePreloadable
    {
        private static ExtendedInventoryControl _instance;
        public static ExtendedInventoryControl Instance => _instance ??= new();
        public static InventoryItem PickedItem => _instance?.GetPickedItem();
        public override bool BlocksPointerInput => true;
        public override bool NonDisposable => false;
        private static readonly string[] SkinNames = { "PersonalShop_I1", "NpcShop_I3", "invenframe_01", "invenframe_02",
            "btn_close01", "btn_close02", "btn_l01", "btn_l02", "inven_no1_1", "inven_no2_1", "inven_no3_1", "inven_no4_1" };
        private static readonly Rectangle CloseRect = new(316, 35, 25, 25);
        private static readonly Rectangle PreviousRect = new(54, 638, 70, 30);
        private static readonly Rectangle NextRect = new(236, 638, 70, 30);
        private readonly Dictionary<byte, InventoryItem> _items = new();
        private readonly BlendState _cacheBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        private Texture2D[] _skin;
        private RenderTarget2D _background;
        private SpriteFont _font;
        private CharacterState _state;
        private GameTime _time;
        private int _page;
        private int _unlocked;
        private byte? _pickedSlot;
        private byte? _hoveredSlot;
        private Point _pickMouse;
        private bool _dragMoved;
        private bool _dirty = true;
        private string _feedback = "";

        private ExtendedInventoryControl()
        {
            ControlSize = new Point(360, 690);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Align = ControlAlign.None;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            foreach (var name in SkinNames) yield return $"Interface/GFx/{name}.ozd";
        }

        public override async Task Load()
        {
            await base.Load();
            var tasks = new Task<Texture2D>[SkinNames.Length];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = TextureLoader.Instance.PrepareAndGetTexture($"Interface/GFx/{SkinNames[i]}.ozd");
            _skin = await Task.WhenAll(tasks);
            _font = GraphicsManager.Instance.Font;
            BindState();
        }

        private void BindState()
        {
            var state = MuGame.Network?.GetCharacterState();
            if (ReferenceEquals(state, _state)) return;
            if (_state != null) _state.InventoryChanged -= OnInventoryChanged;
            _state = state;
            if (_state != null) _state.InventoryChanged += OnInventoryChanged;
            Refresh();
        }

        private void OnInventoryChanged() => MuGame.ScheduleOnMainThread(() => { _dirty = true; });

        private void Refresh()
        {
            _items.Clear();
            if (_state != null)
            foreach (var pair in _state.GetInventoryItems())
            {
                if (pair.Key < ExtendedInventoryLayout.FirstSlot || pair.Key >= ExtendedInventoryLayout.EndSlot) continue;
                var def = ItemDatabase.GetItemDefinition(pair.Value) ??
                    new ItemDefinition(0, ItemDatabase.GetItemName(pair.Value) ?? "Unknown Item", 1, 1);
                var item = new InventoryItem(def, ExtendedInventoryLayout.Position(pair.Key), pair.Value);
                item.Durability = ItemDatabase.GetItemDurability(pair.Value);
                _items[pair.Key] = item;
                if (!string.IsNullOrEmpty(def.TexturePath) && !def.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                    _ = TextureLoader.Instance.Prepare(def.TexturePath);
            }
            if (_pickedSlot.HasValue && !_items.ContainsKey(_pickedSlot.Value)) _pickedSlot = null;
            _dirty = false;
        }

        public void Toggle()
        {
            if (Visible) { Hide(); return; }
            var inventory = InventoryControl.Instance;
            if (!inventory.Visible) return;
            BindState();
            _unlocked = Math.Clamp((int)(_state?.InventoryExpansionState ?? 0), 0, ExtendedInventoryLayout.MaximumExtensions);
            _page = 0;
            _feedback = "";
            X = Math.Max(0, inventory.X - 360);
            Y = inventory.Y;
            Visible = true;
            BringToFront();
            Refresh();
        }

        public void Hide()
        {
            Visible = false;
            _pickedSlot = null;
            _hoveredSlot = null;
        }

        public InventoryItem GetPickedItem() => _pickedSlot is byte slot && _items.TryGetValue(slot, out var item) ? item : null;
        private Rectangle Screen(Rectangle rect) => new(DisplayRectangle.X + rect.X, DisplayRectangle.Y + rect.Y, rect.Width, rect.Height);
        private int SlotAt(Point mouse) => ExtendedInventoryLayout.SlotAt(mouse - DisplayRectangle.Location, _page, _unlocked);
        private Rectangle ItemRect(byte slot, InventoryItem item)
        {
            var grid = ExtendedInventoryLayout.Grid(ExtendedInventoryLayout.Extension(slot) % 2);
            var pos = ExtendedInventoryLayout.Position(slot);
            return Screen(new Rectangle(grid.X + pos.X * 32, grid.Y + pos.Y * 32, item.Definition.Width * 32, item.Definition.Height * 32));
        }
        private byte? ItemAt(Point mouse)
        {
            foreach (var pair in _items)
                if (ExtendedInventoryLayout.Extension(pair.Key) / 2 == _page && ItemRect(pair.Key, pair.Value).Contains(mouse)) return pair.Key;
            return null;
        }
        private bool CanPlace(int slot, InventoryItem item, byte from)
        {
            if (!ExtendedInventoryLayout.Fits(slot, item.Definition.Width, item.Definition.Height, _unlocked)) return false;
            var position = ExtendedInventoryLayout.Position(slot);
            var target = new Rectangle(position.X, position.Y, item.Definition.Width, item.Definition.Height);
            foreach (var pair in _items)
            {
                if (pair.Key == from || ExtendedInventoryLayout.Extension(pair.Key) != ExtendedInventoryLayout.Extension(slot)) continue;
                var p = ExtendedInventoryLayout.Position(pair.Key);
                if (target.Intersects(new Rectangle(p.X, p.Y, pair.Value.Definition.Width, pair.Value.Definition.Height))) return false;
            }
            return true;
        }

        private bool Move(byte from, byte to, InventoryItem item)
        {
            var network = MuGame.Network;
            var service = network?.GetCharacterService();
            if (_state == null || service == null || network?.IsConnected != true || _state.PendingMoveFromSlot.HasValue || _state.HasPendingTradeMove || _state.HasPendingVaultMove) return false;
            if (from == to) return true;
            var state = _state;
            state.StashPendingInventoryMove(from, to);
            _feedback = "Waiting for server...";
            _ = SendMoveAsync();
            return true;
            async Task SendMoveAsync()
            {
                try { await service.SendItemMoveRequestAsync(from, to, network.TargetVersion, item.RawData, throwOnFailure: true); }
                catch
                {
                    if (state.IsInventoryMovePending(from, to)) state.TryConsumePendingInventoryMove(out _, out _);
                    state.RaiseInventoryChanged();
                    MuGame.ScheduleOnMainThread(() => _feedback = "Unable to send item move.");
                }
            }
        }

        // Both click-click and drag-release from the base inventory enter through here.
        public bool TryHandleInventoryDrop(Point mouse, InventoryControl inventory)
        {
            if (!Visible || !DisplayRectangle.Contains(mouse) || inventory.GetDraggedItem() is not { } item) return false;
            if (!PersonalStoreLayout.IsTopmostAtPointer(this, mouse)) return true;
            int target = SlotAt(mouse);
            if (inventory.GetPickedInventorySlot() is byte from && CanPlace(target, item, from) && Move(from, (byte)target, item))
                inventory.CompleteExtendedInventoryDrop();
            else _feedback = "Choose an unlocked, empty space.";
            Scene?.SetMouseInputConsumed();
            return true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _time = gameTime;
            if (!Visible) return;
            if (!InventoryControl.Instance.Visible) { Hide(); return; }
            BindState();
            _unlocked = Math.Clamp((int)(_state?.InventoryExpansionState ?? 0), 0, 4);
            if (_unlocked <= 2) _page = 0;
            if (_dirty) Refresh();
            if (_feedback == "Waiting for server..." && _state?.PendingMoveFromSlot == null) _feedback = "";
            var mouse = MuGame.Instance.UiMouseState.Position;
            _hoveredSlot = null;
            if (Scene?.IsPointerInputBlockedFor(this) == true) return;
            bool press = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
            bool release = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Released && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Pressed;
            bool over = PersonalStoreLayout.IsTopmostAtPointer(this, mouse);
            bool rightPress = MuGame.Instance.UiMouseState.RightButton == ButtonState.Pressed && MuGame.Instance.PrevUiMouseState.RightButton == ButtonState.Released;
            if (rightPress && _pickedSlot.HasValue) { _pickedSlot = null; Scene?.SetMouseInputConsumed(); return; }
            if (press && over && Screen(CloseRect).Contains(mouse)) { Hide(); Scene?.SetMouseInputConsumed(); return; }
            if (press && over && _unlocked > 2 && (Screen(PreviousRect).Contains(mouse) || Screen(NextRect).Contains(mouse)))
            {
                _page = 1 - _page;
                Scene?.SetMouseInputConsumed();
                return;
            }
            if (_state?.PendingMoveFromSlot != null || _state?.HasPendingTradeMove == true || _state?.HasPendingVaultMove == true) return;
            if (InventoryControl.Instance.GetDraggedItem() != null) return;
            if (GetPickedItem() is { } picked)
            {
                _dragMoved |= Vector2.Distance(mouse.ToVector2(), _pickMouse.ToVector2()) > 3;
                if (press || (release && _dragMoved))
                {
                    int to = over ? SlotAt(mouse) : -1;
                    bool accepted = to >= 0 && CanPlace(to, picked, _pickedSlot.Value) && Move(_pickedSlot.Value, (byte)to, picked);
                    var inventory = InventoryControl.Instance;
                    if (!over && PersonalStoreLayout.IsTopmostAtPointer(inventory, mouse))
                    {
                        var pos = inventory.GetSlotAtScreenPositionPublic(mouse);
                        if (pos.X >= 0 && inventory.CanPlaceAt(pos, picked))
                            accepted = Move(_pickedSlot.Value, (byte)(12 + pos.Y * 8 + pos.X), picked);
                        else if (inventory.GetEmptyEquipmentSlotAtScreenPosition(mouse) is int equipmentSlot && equipmentSlot >= 0)
                            accepted = Move(_pickedSlot.Value, (byte)equipmentSlot, picked); // OpenMU validates class and item requirements.
                    }
                    if (accepted) _pickedSlot = null;
                    else _feedback = "Choose an unlocked, empty space.";
                    Scene?.SetMouseInputConsumed();
                }
                return;
            }
            if (!over) return;
            _hoveredSlot = ItemAt(mouse);
            if (rightPress && _hoveredSlot is byte consumableSlot && _items.TryGetValue(consumableSlot, out var consumable) &&
                consumable.Definition.IsConsumable() && !consumable.Definition.IsUpgradeJewel() &&
                ExtendedInventoryLayout.Extension(consumableSlot) < _unlocked)
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service != null) _ = service.SendConsumeItemRequestAsync(consumableSlot);
                Scene?.SetMouseInputConsumed();
                return;
            }
            if (press && _hoveredSlot is byte slot && ExtendedInventoryLayout.Extension(slot) < _unlocked)
            {
                _pickedSlot = slot;
                _pickMouse = mouse;
                _dragMoved = false;
                Scene?.SetMouseInputConsumed();
            }
        }

        private void EnsureBackground()
        {
            if (_background != null && !_background.IsDisposed) return;
            var gm = GraphicsManager.Instance;
            var gd = gm.GraphicsDevice;
            _background = new RenderTarget2D(gd, 360, 690);
            var previous = gd.GetRenderTargets();
            using (new SpriteBatchScope(gm.Sprite, SpriteSortMode.Deferred, _cacheBlend))
            {
                gd.SetRenderTarget(_background);
                gd.Clear(Color.Transparent);
                gm.Sprite.Draw(gm.Pixel, new Rectangle(12, 90, 336, 578), new Color(25, 23, 23));
                NewUiFrameRenderer.DrawPersonalShopWindow(gm.Sprite, _skin[0], new Rectangle(0, 0, 360, 690));
                for (int block = 0; block < 2; block++)
                {
                    var grid = ExtendedInventoryLayout.Grid(block);
                    var frame = new Rectangle(grid.X - 12, grid.Y - 12, grid.Width + 24, grid.Height + 24);
                    gm.Sprite.Draw(gm.Pixel, frame, new Color(14, 13, 11));
                    for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 8; x++)
                        gm.Sprite.Draw(_skin[1], new Rectangle(grid.X + x * 32, grid.Y + y * 32, 32, 32), new Rectangle(546, 220, 29, 29), Color.White);
                    gm.Sprite.Draw(_skin[3], frame, Color.White);
                    gm.Sprite.Draw(_skin[2], new Rectangle(frame.X, frame.Y, frame.Width, 32), Color.White);
                    gm.Sprite.Draw(_skin[2], new Rectangle(frame.X, frame.Bottom - 32, frame.Width, 32), null, Color.White, 0, Vector2.Zero, SpriteEffects.FlipVertically, 0);
                }
            }
            gd.SetRenderTargets(previous);
        }

        private Texture2D Preview(InventoryItem item, Rectangle rect, bool hovered)
        {
            string path = item.Definition.TexturePath;
            if (string.IsNullOrEmpty(path)) return null;
            if (!path.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase)) return TextureLoader.Instance.GetTexture2D(path);
            try
            {
                if (hovered) return BmdPreviewRenderer.GetSmoothAnimatedPreview(item, rect.Width, rect.Height, _time);
                if (Constants.ENABLE_ITEM_MATERIAL_ANIMATION ||
                    (item.Definition.Group == 0 && item.Definition.Id == 26) ||
                    (item.Definition.Group == 12 && item.Definition.Id is 36 or 39))
                    return BmdPreviewRenderer.GetMaterialAnimatedPreview(item, rect.Width, rect.Height, _time);
                return BmdPreviewRenderer.GetPreview(item, rect.Width, rect.Height);
            }
            catch { return null; }
        }
        private void Label(SpriteBatch sprite, string text, Rectangle rect, float scale = 0.35f)
        {
            var size = _font.MeasureString(text);
            scale = Math.Min(scale, (rect.Width - 8) / Math.Max(1, size.X));
            var pos = new Vector2(rect.Center.X - size.X * scale / 2, rect.Center.Y - size.Y * scale / 2);
            sprite.DrawString(_font, text, pos + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            sprite.DrawString(_font, text, pos, new Color(238, 215, 160), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || _skin == null || _font == null) return;
            EnsureBackground();
            var gm = GraphicsManager.Instance;
            using (new SpriteBatchScope(gm.Sprite, SpriteSortMode.Deferred, BlendState.AlphaBlend, transform: UiScaler.SpriteTransform))
            {
                gm.Sprite.Draw(_background, DisplayRectangle, Color.White);
                foreach (var pair in _items)
                {
                    if (ExtendedInventoryLayout.Extension(pair.Key) / 2 != _page || pair.Key == _pickedSlot || _state?.PendingMoveFromSlot == pair.Key) continue;
                    var rect = ItemRect(pair.Key, pair.Value);
                    var texture = Preview(pair.Value, rect, pair.Key == _hoveredSlot);
                    if (texture != null) gm.Sprite.Draw(texture, rect, Color.White);
                    else ItemGridRenderHelper.DrawItemPlaceholder(gm.Sprite, gm.Pixel, _font, rect, pair.Value, new Color(25, 23, 23), Color.Gray);
                    if (pair.Value.Definition.BaseDurability == 0 && pair.Value.Definition.MagicDurability == 0 && pair.Value.Durability > 1)
                        ItemGridRenderHelper.DrawItemStackCount(gm.Sprite, _font, rect, pair.Value.Durability, Color.Gold, 1);
                }
                if (_hoveredSlot is byte hovered && _items.TryGetValue(hovered, out var item))
                {
                    int slot = SlotAt(MuGame.Instance.UiMouseState.Position);
                    ItemGridRenderHelper.DrawGridOverlays(gm.Sprite, gm.Pixel, DisplayRectangle,
                        ExtendedInventoryLayout.Grid(ExtendedInventoryLayout.Extension(hovered) % 2), item,
                        slot >= 0 ? ExtendedInventoryLayout.Position(slot) : new Point(-1, -1), 32, 32,
                        new Color(70, 85, 110, 150), new Color(90, 140, 200), 1);
                }
                var movingItem = GetPickedItem() ?? InventoryControl.Instance.GetDraggedItem();
                if (movingItem != null && PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position))
                {
                    int slot = SlotAt(MuGame.Instance.UiMouseState.Position);
                    if (slot >= 0)
                    {
                        byte from = _pickedSlot ?? InventoryControl.Instance.GetPickedInventorySlot() ?? byte.MaxValue;
                        var rect = ItemRect((byte)slot, movingItem);
                        var bounds = Screen(ExtendedInventoryLayout.Grid(ExtendedInventoryLayout.Extension(slot) % 2));
                        gm.Sprite.Draw(gm.Pixel, Rectangle.Intersect(rect, bounds), CanPlace(slot, movingItem, from) ? Color.GreenYellow * 0.4f : Color.Red * 0.5f);
                    }
                }
                for (int block = 0; block < 2; block++)
                    if (_page * 2 + block >= _unlocked)
                    {
                        var rect = Screen(ExtendedInventoryLayout.Grid(block));
                        gm.Sprite.Draw(gm.Pixel, rect, Color.Black * 0.78f);
                        Label(gm.Sprite, "Locked", rect);
                    }
                Label(gm.Sprite, "Extended Inventory", Screen(new Rectangle(36, 36, 280, 32)), 0.45f);
                Label(gm.Sprite, _feedback, Screen(new Rectangle(24, 574, 312, 38)), 0.31f);
                Label(gm.Sprite, _unlocked == 0 ? "No expansions unlocked" : $"Unlocked: {_unlocked} / 4", Screen(new Rectangle(35, 610, 290, 24)), 0.31f);
            }
            using (new SpriteBatchScope(gm.Sprite, SpriteSortMode.Deferred, BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
            {
                var mouse = MuGame.Instance.UiMouseState.Position;
                gm.Sprite.Draw(_skin[Screen(CloseRect).Contains(mouse) ? 5 : 4], Screen(CloseRect), Color.White);
                for (int block = 0; block < 2; block++)
                    gm.Sprite.Draw(_skin[8 + _page * 2 + block], Screen(new Rectangle(148, 104 + block * 236, 64, 64)), Color.White);
                if (_unlocked > 2)
                {
                    gm.Sprite.Draw(_skin[Screen(PreviousRect).Contains(mouse) ? 7 : 6], Screen(PreviousRect), Color.White);
                    gm.Sprite.Draw(_skin[Screen(NextRect).Contains(mouse) ? 7 : 6], Screen(NextRect), Color.White);
                    Label(gm.Sprite, "<", Screen(PreviousRect)); Label(gm.Sprite, ">", Screen(NextRect));
                    Label(gm.Sprite, $"{_page + 1} / 2", Screen(new Rectangle(130, 638, 100, 30)));
                }
            }
        }

        public void DrawOverlay(SpriteBatch sprite)
        {
            if (!Visible || _font == null) return;
            var mouse = MuGame.Instance.UiMouseState.Position;
            if (GetPickedItem() is { } picked)
            {
                var rect = new Rectangle(mouse.X - picked.Definition.Width * 16, mouse.Y - picked.Definition.Height * 16, picked.Definition.Width * 32, picked.Definition.Height * 32);
                var texture = Preview(picked, rect, false);
                if (texture != null) sprite.Draw(texture, rect, Color.White * 0.85f);
                return;
            }
            if (InventoryControl.Instance.GetDraggedItem() != null || !PersonalStoreLayout.IsTopmostAtPointer(this, mouse)) return;
            if (_hoveredSlot is byte slot && _items.TryGetValue(slot, out var item))
            {
                PetInfoSnapshot? pet = null;
                if (_state.TryGetPetInfo(slot, out var info)) pet = info;
                ClassicItemTooltipRenderer.Draw(sprite, _font, item, ClassicItemTooltipBuilder.Build(item, pet), ItemRect(slot, item), mouse);
            }
        }
        public override void Dispose()
        {
            if (_state != null) _state.InventoryChanged -= OnInventoryChanged;
            _background?.Dispose();
            _cacheBlend.Dispose();
            _items.Clear();
            if (ReferenceEquals(_instance, this)) _instance = null;
            base.Dispose();
        }
    }
}
