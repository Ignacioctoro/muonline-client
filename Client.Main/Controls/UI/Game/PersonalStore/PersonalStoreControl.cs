using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Client.Main.Core.Client;
using Client.Main.Core.Items.Tooltips;
using MUnique.OpenMU.Network.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Season 6 personal-store window, using the existing GFx NewUI frame.
    /// Item placement, prices and publication follow authoritative server acknowledgements.
    /// </summary>
    public sealed class PersonalStoreControl : UIControl, IUiTexturePreloadable
    {
        public const int Columns = 8;
        public const int Rows = 4;
        public const int SlotCount = Columns * Rows;
        public const int CellSize = GameWindowMetrics.InventoryCell;
        public const int FirstStoreSlot = 204; // OpenMU InventoryConstants.FirstStoreItemSlotIndex

        private const int WindowWidth = PersonalStoreLayout.Width;
        private const int WindowHeight = PersonalStoreLayout.Height;

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
        private static readonly Rectangle ItemGrid = PersonalStoreLayout.Grid;
        private static readonly Rectangle OpenButton = PersonalStoreLayout.Open;
        private static readonly Rectangle CloseStoreButton = PersonalStoreLayout.CloseStore;
        private static readonly Rectangle ExitButton = PersonalStoreLayout.Exit;
        private static readonly Rectangle CloseButton = PersonalStoreLayout.Close;
        private static readonly Rectangle PriceDialog = new(31, 226, 286, 181);
        private static readonly Rectangle PriceConfirmButton = new(48, 350, 119, 32);
        private static readonly Rectangle PriceCancelButton = new(180, 350, 119, 32);
        private static readonly Rectangle StoreNameEditBox = PersonalStoreLayout.Name;
        private const double LongPressPriceMilliseconds = 620d;
        private const int LongPressMovementTolerance = 10;
        private const uint MaximumPriceZen = int.MaxValue; // OpenMU casts the protocol price to int

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

        // Static panels render directly in the UI batch. Keeping them in a
        // RenderTarget2D loses their contents on certain scene/device resets
        // (Select Character / Select Server), even though the UI is still Ready.

        private bool _closeHovered;
        private bool _exitHovered;
        private bool _storePublic;
        private bool _openingRequested;
        private bool _closingRequested;
        private string _publishedStoreName = string.Empty;
        private readonly List<InventoryItem> _items = new();
        private readonly InventoryItem[,] _occupied = new InventoryItem[Columns, Rows];
        private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
        private CharacterState _state;
        private InventoryItem _hoveredItem;
        private InventoryItem _pickedItem;
        private byte _pickedSlot;
        private Point _pickStartPoint;
        private bool _requestInFlight;
        private int _previewIndex;
        private double _missingPreviewCooldown;
        private readonly Dictionary<string, int> _previewRetryCounts = new(StringComparer.Ordinal);
        private const int MaxMissingPreviewRetries = 90; // allow slower Android/reconnect asset loading
        private readonly TextFieldControl _priceInput;
        private readonly TextFieldControl _storeNameInput;
        private bool _priceEditorVisible;
        private double _cursorTimeSeconds;
        private InventoryItem _longPressItem;
        private Point _longPressOrigin;
        private double _longPressElapsedMilliseconds;
        private bool _suppressLongPressRelease;
        private byte _priceEditorSlot;
        private string _priceInputError = string.Empty;
        private string _priceFeedback = "Right-click an item to set its price.";
        private bool _priceFeedbackError;
        private byte? _pendingPriceSlot;
        private uint _pendingPriceZen;
        private byte[] _pendingPriceRaw;
        // Confirmed by OpenMU in this session; item bytes guard against
        // displaying an old price when another item occupies the same slot.
        private readonly Dictionary<byte, (uint Price, byte[] Raw)> _confirmedPrices = new();

        private PersonalStoreControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.None;

            _priceInput = TextFieldControl.Create();
            _priceInput.X = 60;
            _priceInput.Y = 301;
            _priceInput.ViewSize = new Point(228, 31);
            _priceInput.ControlSize = _priceInput.ViewSize;
            _priceInput.AutoViewSize = false;
            _priceInput.MaxLength = 10;
            // WinDX can miss the GameWindow.TextInput event. Use the same
            // KeyboardState digit path as the existing Trade/Vault Zen editors.
            _priceInput.CaptureDigitsFromKeyboardState = true;
            _priceInput.FontSize = 15;
            _priceInput.TextColor = new Color(242, 224, 192);
            _priceInput.BackgroundColor = new Color(17, 18, 22, 245);
            _priceInput.BorderColor = new Color(129, 103, 65);
            _priceInput.BorderThickness = 1;
            _priceInput.Placeholder = "Enter Zen amount";
            _priceInput.Visible = false;
            _priceInput.Click += (sender, args) =>
            {
                _priceInput.Focus();
                if (Scene != null)
                    Scene.FocusControl = _priceInput;
            };
            _priceInput.EnterKeyPressed += (sender, args) =>
            {
                Scene?.ConsumeKeyboardEnter();
                SubmitPrice();
            };
            _priceInput.ValueChanged += (sender, args) =>
            {
                string oldValue = _priceInput.Value;
                string numeric = new string(oldValue.Where(c => c >= '0' && c <= '9').ToArray());
                if (!string.Equals(oldValue, numeric, StringComparison.Ordinal))
                    _priceInput.Value = numeric;
                _priceInputError = string.Empty;
            };
            Controls.Add(_priceInput);

            // 26-byte name field on OpenMU's C3 3F 02 shop-open packet.
            // Reserve one byte for the terminator in a future server request.
            // This is an editable draft; publication remains disabled.
            _storeNameInput = TextFieldControl.Create();
            _storeNameInput.X = StoreNameEditBox.X;
            _storeNameInput.Y = StoreNameEditBox.Y;
            _storeNameInput.ViewSize = new Point(StoreNameEditBox.Width, StoreNameEditBox.Height);
            _storeNameInput.ControlSize = _storeNameInput.ViewSize;
            _storeNameInput.AutoViewSize = false;
            _storeNameInput.MaxLength = 25;
            _storeNameInput.FontSize = 14f;
            _storeNameInput.TextColor = new Color(246, 227, 186);
            _storeNameInput.BackgroundColor = new Color(19, 18, 19, 255);
            _storeNameInput.BorderColor = new Color(124, 101, 74);
            _storeNameInput.BorderThickness = 1;
            _storeNameInput.Placeholder = "Enter store name";
            _storeNameInput.Visible = false;
            _storeNameInput.Click += (sender, args) => FocusNameInput();
            _storeNameInput.EnterKeyPressed += (sender, args) =>
            {
                Scene?.ConsumeKeyboardEnter();
                _storeNameInput.Blur();
                if (Scene != null)
                    Scene.FocusControl = this;
            };
            Controls.Add(_storeNameInput);
        }

        public static PersonalStoreControl Instance => _instance ??= new PersonalStoreControl();
        public static bool IsOpen => _instance?.Visible == true;
        public static bool IsEditingPrice => _instance?._priceEditorVisible == true;
        // Used by the inventory's Escape handler while either editor is active.
        public static bool IsEditingText =>
            _instance?._priceEditorVisible == true ||
            _instance?._storeNameInput?.IsFocused == true;
        public string StoreNameDraft => _storeNameInput.Value.Trim();
        public bool IsStorePublic => _storePublic;
        // Recreate the per-scene GPU render target and UI textures on each GameScene.
        public override bool NonDisposable => false;

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
            X = Math.Max(0, inventory.X - DisplaySize.X);
            Y = Math.Max(12, inventory.Y);
            if (Parent != null)
            {
                X = Math.Min(X, Math.Max(12, Parent.DisplaySize.X - DisplaySize.X - 12));
                Y = Math.Min(Y, Math.Max(12, Parent.DisplaySize.Y - DisplaySize.Y - 12));
            }

            EnsureCharacterState();
            RefreshShopItems();
            _storeNameInput.Visible = !_storePublic;
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

            CancelPriceEditor();
            _storeNameInput.Blur();
            _storeNameInput.Visible = false;
            // The scene focus must never remain on a hidden text field.
            if (Scene != null &&
                (Scene.FocusControl == _storeNameInput ||
                 Scene.FocusControl == _priceInput))
                Scene.FocusControl = null;
            _longPressItem = null;
            _longPressElapsedMilliseconds = 0;
            _suppressLongPressRelease = false;
            Visible = false;
            _pickedItem = null;
            _requestInFlight = false;
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
        /// Returns a zero-based visual slot (0..31). Network 0x24 uses
        /// FirstStoreSlot + this visual index.
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

        public bool HasPickedItem => _pickedItem != null;
        public InventoryItem GetPickedItem() =>
            _requestInFlight || _storePublic ? null : _pickedItem;

        /// <summary>
        /// Draws the shop selection in the final UI pass, above both windows.
        /// Never removes local items before a 0x24 server confirmation.
        /// </summary>
        public void DrawPickedPreview(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!Visible || _pickedItem == null || spriteBatch == null || _requestInFlight)
                return;

            var mouse = MuGame.Instance.UiMouseState.Position;
            int width = Math.Max(1, (int)MathF.Round(_pickedItem.Definition.Width * CellSize * Scale));
            int height = Math.Max(1, (int)MathF.Round(_pickedItem.Definition.Height * CellSize * Scale));
            Rectangle target = new(mouse.X - width / 2, mouse.Y - height / 2, width, height);
            var texture = ResolveItemTexture(_pickedItem);
            if (texture != null && !texture.IsDisposed)
                spriteBatch.Draw(texture, target, Color.White * 0.82f);
            else if (GraphicsManager.Instance?.Pixel is { } pixel)
                spriteBatch.Draw(pixel, target, new Color(190, 172, 130, 115));
        }

        public bool CanPlaceAt(Point target, InventoryItem item)
        {
            if (_storePublic || _openingRequested || _closingRequested ||
                item?.Definition == null) return false;
            for (int y = 0; y < item.Definition.Height; y++)
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = target.X + x;
                    int gy = target.Y + y;
                    if (gx < 0 || gy < 0 || gx >= Columns || gy >= Rows)
                        return false;
                    var occupant = _occupied[gx, gy];
                    if (occupant != null && occupant != _pickedItem)
                        return false;
                }
            return true;
        }

        // Called from Inventory.Update before the latter picks up an item.
        // An accepted move is NOT displayed locally until OpenMU replies 0x24.
        public bool TryHandleInventoryDrop(Point mouse, InventoryControl inventory)
        {
            if (!Visible || _storePublic || _pickedItem == null || inventory == null ||
                !inventory.Visible || !inventory.DisplayRectangle.Contains(mouse) ||
                inventory.GetDraggedItem() != null)
                return false;

            var target = inventory.GetSlotAtScreenPositionPublic(mouse);
            if (!_requestInFlight && target.X >= 0 && inventory.CanPlaceAt(target, _pickedItem))
            {
                byte to = (byte)(InventoryControl.InventorySlotOffsetConstant +
                    target.Y * InventoryControl.Columns + target.X);
                SendConfirmedStorageMove(_pickedSlot, ItemStorageKind.Inventory, to);
                SoundController.Instance.PlayBuffer("Sound/iButton.wav");
            }
            else if (!_requestInFlight)
            {
                _pickedItem = null;
            }
            Scene?.SetMouseInputConsumed();
            return true;
        }

        private void EnsureCharacterState()
        {
            var current = MuGame.Network?.GetCharacterState();
            if (ReferenceEquals(_state, current)) return;
            if (_state != null)
                _state.InventoryChanged -= OnInventoryChanged;
            _state = current;
            _confirmedPrices.Clear();
            _storeNameInput.Value = string.Empty;
            _pendingPriceSlot = null;
            _pendingPriceRaw = null;
            _priceFeedback = "Right-click an item to set its price.";
            _priceFeedbackError = false;
            _storePublic = false;
            _openingRequested = false;
            _closingRequested = false;
            _publishedStoreName = string.Empty;
            if (_state != null)
                _state.InventoryChanged += OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            if (Visible)
                MuGame.ScheduleOnMainThread(RefreshShopItems);
        }

        private void RefreshShopItems()
        {
            if (_state == null) return;
            _items.Clear();
            Array.Clear(_occupied, 0, _occupied.Length);
            var snapshot = _state.GetInventoryItems();
            foreach (var entry in _confirmedPrices.ToArray())
            {
                if (!snapshot.TryGetValue(entry.Key, out var raw) ||
                    !raw.SequenceEqual(entry.Value.Raw))
                    _confirmedPrices.Remove(entry.Key);
            }
            foreach (var pair in snapshot)
            {
                int index = pair.Key - FirstStoreSlot;
                if (index < 0 || index >= SlotCount) continue;
                var data = pair.Value;
                var def = ItemDatabase.GetItemDefinition(data)
                    ?? new ItemDefinition(0, ItemDatabase.GetItemName(data) ?? "Unknown Item",
                        1, 1, "Interface/newui_item_box.tga");
                var item = new InventoryItem(def, new Point(index % Columns, index / Columns), data)
                {
                    Durability = ItemDatabase.GetItemDurability(data)
                };
                _items.Add(item);
                for (int y = 0; y < def.Height; y++)
                    for (int x = 0; x < def.Width; x++)
                    {
                        int gx = item.GridPosition.X + x, gy = item.GridPosition.Y + y;
                        if (gx < Columns && gy < Rows)
                            _occupied[gx, gy] = item;
                    }
                if (!string.IsNullOrEmpty(def.TexturePath) &&
                    !def.TexturePath.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                    _ = TextureLoader.Instance.Prepare(def.TexturePath);
            }
            // InventoryChanged rebuilds InventoryItem objects. Keep a picked
            // shop item bound to its confirmed absolute slot, not a stale
            // InventoryItem reference (which would fail CanPlaceAt checks).
            if (_pickedItem != null)
            {
                int selected = _pickedSlot - FirstStoreSlot;
                _pickedItem = selected >= 0 && selected < SlotCount
                    ? _occupied[selected % Columns, selected / Columns]
                    : null;
            }
            _previewIndex = 0;
            _previewRetryCounts.Clear();
            _missingPreviewCooldown = 0;
        }

        private void SendConfirmedStorageMove(byte fromSlot, ItemStorageKind targetStorage, byte toSlot)
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service == null || _state == null || _pickedItem == null ||
                _storePublic || _openingRequested || _closingRequested ||
                _requestInFlight || _state.IsPlayerShopMovePending)
                return;

            var raw = _pickedItem.RawData ?? Array.Empty<byte>();
            _state.StashPendingPlayerShopMove(fromSlot, toSlot);
            _requestInFlight = true;
            _pickedItem = null;
            _ = Task.Run(async () =>
            {
                try
                {
                    await service.SendStorageItemMoveAsync(ItemStorageKind.PlayerShop, fromSlot,
                        targetStorage, toSlot,
                        MuGame.Network.TargetVersion, raw);
                }
                catch (Exception)
                {
                    // Keep the last confirmed snapshot on an unsuccessful send.
                    // The game server remains the authority for these slots.
                }
            });
        }

        private static string FormatZen(uint amount) =>
            amount.ToString("N0", CultureInfo.InvariantCulture);

        private void FocusNameInput()
        {
            if (!Visible || _priceEditorVisible || _storePublic ||
                _openingRequested || _closingRequested)
                return;
            _pickedItem = null;
            _storeNameInput.Focus();
            if (Scene != null)
                Scene.FocusControl = _storeNameInput;
        }

        private void OpenPriceEditor(InventoryItem item)
        {
            if (item == null || _pendingPriceSlot.HasValue ||
                _storePublic || _openingRequested || _closingRequested ||
                _requestInFlight || _state?.IsPlayerShopMovePending == true)
                return;

            _pickedItem = null;
            _priceEditorSlot = (byte)(FirstStoreSlot +
                item.GridPosition.Y * Columns + item.GridPosition.X);
            _priceInputError = string.Empty;
            _priceInput.Value = _confirmedPrices.TryGetValue(_priceEditorSlot, out var price)
                ? price.Price.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            _storeNameInput.Blur();
            _storeNameInput.Visible = false;
            _priceEditorVisible = true;
            _priceInput.Visible = true;
            _priceInput.Focus();
            if (Scene != null)
                Scene.FocusControl = _priceInput;
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
        }

        private void CancelPriceEditor()
        {
            _priceEditorVisible = false;
            // Restore the name editor only while the shop panel is active.
            _storeNameInput.Visible = Visible;
            if (_priceInput != null)
            {
                _priceInput.Blur();
                _priceInput.Visible = false;
            }
            if (Scene != null && Scene.FocusControl == _priceInput)
                Scene.FocusControl = this;
            _priceInputError = string.Empty;
        }

        private void SubmitPrice()
        {
            if (!_priceEditorVisible || _pendingPriceSlot.HasValue || _storePublic)
                return;
            if (!uint.TryParse(_priceInput.Value, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var price) || price > MaximumPriceZen)
            {
                _priceInputError = "Enter 0 to 2,147,483,647 Zen (0 clears the price).";
                return;
            }

            var service = MuGame.Network?.GetCharacterService();
            if (service == null || _state == null ||
                !_state.GetInventoryItems().TryGetValue(_priceEditorSlot, out var itemRaw))
            {
                _priceInputError = "This item is unavailable or the server is disconnected.";
                return;
            }

            byte slot = _priceEditorSlot;
            _pendingPriceSlot = slot;
            _pendingPriceZen = price;
            _pendingPriceRaw = (byte[])itemRaw.Clone();
            CancelPriceEditor();
            _priceFeedback = "Waiting for OpenMU price confirmation...";
            _priceFeedbackError = false;
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");

            _ = Task.Run(async () =>
            {
                bool sent;
                try { sent = await service.SendPlayerShopSetItemPriceAsync(slot, price); }
                catch { sent = false; }
                if (!sent)
                {
                    MuGame.ScheduleOnMainThread(() => FailPriceRequest(slot,
                        "Could not send price to OpenMU."));
                    return;
                }

                await Task.Delay(6000);
                MuGame.ScheduleOnMainThread(() => FailPriceRequest(slot,
                    "No price confirmation received from OpenMU. Try again."));
            });
        }

        private void FailPriceRequest(byte slot, string message)
        {
            if (!_pendingPriceSlot.HasValue || _pendingPriceSlot.Value != slot) return;
            _pendingPriceSlot = null;
            _pendingPriceRaw = null;
            _priceFeedback = message;
            _priceFeedbackError = true;
        }

        /// <summary>
        /// Called on the UI thread by the C3 3F 01 response packet handler.
        /// No price becomes visible as confirmed before result == Success.
        /// </summary>
        public void HandlePriceSetResponse(byte slot, byte result)
        {
            if (!_pendingPriceSlot.HasValue || _pendingPriceSlot.Value != slot)
                return;

            uint attempted = _pendingPriceZen;
            byte[] raw = _pendingPriceRaw;
            _pendingPriceSlot = null;
            _pendingPriceRaw = null;

            if (result == 1 && _state != null &&
                _state.GetInventoryItems().TryGetValue(slot, out var currentRaw) &&
                raw != null && currentRaw.SequenceEqual(raw))
            {
                _confirmedPrices[slot] = (attempted, raw);
                _priceFeedback = attempted == 0
                    ? "Price removed. Item is not for sale."
                    : $"Price confirmed by OpenMU: {FormatZen(attempted)} Zen.";
                _priceFeedbackError = false;
            }
            else
            {
                _priceFeedbackError = true;
                _priceFeedback = result switch
                {
                    0 => "OpenMU rejected price (shop may already be open).",
                    2 => "OpenMU rejected: invalid item slot.",
                    3 => "OpenMU rejected: item not found.",
                    4 => "OpenMU rejected: invalid price.",
                    5 => "OpenMU rejected: item is blocked.",
                    6 => "OpenMU requires character level 6 or higher.",
                    1 => "Item changed before price confirmation.",
                    _ => "OpenMU rejected the price request."
                };
            }
        }

        private void RequestOpenStore()
        {
            if (_storePublic || _openingRequested || _closingRequested ||
                _priceEditorVisible || _pendingPriceSlot.HasValue ||
                _requestInFlight || _state == null ||
                _state.IsPlayerShopMovePending)
                return;

            string shopName = StoreNameDraft;
            if (shopName.Length == 0 ||
                System.Text.Encoding.UTF8.GetByteCount(shopName) > 25)
            {
                SetStoreFeedback("Enter a store name (maximum 25 UTF-8 bytes).", true);
                FocusNameInput();
                return;
            }

            if (_items.Count == 0)
            {
                SetStoreFeedback("Add at least one item before opening the store.", true);
                return;
            }

            var inventory = _state.GetInventoryItems();
            foreach (var item in _items)
            {
                byte slot = (byte)(FirstStoreSlot +
                    item.GridPosition.Y * Columns + item.GridPosition.X);
                if (!_confirmedPrices.TryGetValue(slot, out var price) ||
                    price.Price == 0 || !inventory.TryGetValue(slot, out var raw) ||
                    !raw.SequenceEqual(price.Raw))
                {
                    SetStoreFeedback("Set a confirmed Zen price on every shop item.", true);
                    return;
                }
            }

            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                SetStoreFeedback("Not connected to the game server.", true);
                return;
            }

            _storeNameInput.Blur();
            _storeNameInput.Visible = false;
            if (Scene?.FocusControl == _storeNameInput)
                Scene.FocusControl = this;
            _pickedItem = null;
            _openingRequested = true;
            _publishedStoreName = shopName;
            SetStoreFeedback("Waiting for OpenMU to publish the store...", false);
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");

            _ = Task.Run(async () =>
            {
                bool sent = await service.SendPlayerShopOpenRequestAsync(shopName);
                if (!sent)
                {
                    MuGame.ScheduleOnMainThread(() => FailStoreRequest(true,
                        "Could not send store opening request."));
                    return;
                }
                // OpenMU's OpenStoreAction can reject silently when a store
                // contains unpriced/Harmony items. Do not assume success.
                await Task.Delay(8000);
                MuGame.ScheduleOnMainThread(() => FailStoreRequest(true,
                    "No opening confirmation. Check prices and Harmony options."));
            });
        }

        private void RequestCloseStore()
        {
            if (!_storePublic || _openingRequested || _closingRequested)
                return;
            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                SetStoreFeedback("Not connected to the game server.", true);
                return;
            }
            _closingRequested = true;
            SetStoreFeedback("Waiting for OpenMU to close the store...", false);
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
            _ = Task.Run(async () =>
            {
                bool sent = await service.SendPlayerShopCloseRequestAsync();
                if (!sent)
                {
                    MuGame.ScheduleOnMainThread(() => FailStoreRequest(false,
                        "Could not send store closing request."));
                    return;
                }
                await Task.Delay(8000);
                MuGame.ScheduleOnMainThread(() => FailStoreRequest(false,
                    "No closing confirmation from OpenMU."));
            });
        }

        private void SetStoreFeedback(string message, bool error)
        {
            _priceFeedback = message;
            _priceFeedbackError = error;
        }

        private static void NotifyStoreInChat(string message)
        {
            var scene = MuGame.Instance?.ActiveScene as Client.Main.Scenes.GameScene;
            scene?.ChatLog?.AddMessage("Personal Store", message, MessageType.System);
        }

        private void FailStoreRequest(bool opening, string message)
        {
            if (opening)
            {
                if (!_openingRequested) return;
                _openingRequested = false;
                _storeNameInput.Visible = Visible;
            }
            else
            {
                if (!_closingRequested) return;
                _closingRequested = false;
            }
            SetStoreFeedback(message, true);
            NotifyStoreInChat(message);
        }

        public void HandleStoreOpenResult(bool successful)
        {
            // Only trust server packets; never turn a sent request into an
            // open store without 3F 02 or the scope announcement.
            if (!successful || (!_openingRequested && !_storePublic))
            {
                if (_openingRequested)
                    FailStoreRequest(true, "OpenMU rejected the opening request.");
                return;
            }
            bool wasPublic = _storePublic;
            _storePublic = true;
            _openingRequested = false;
            _closingRequested = false;
            if (!wasPublic)
                NotifyStoreInChat($"Store opened: {_publishedStoreName}.");
            _storeNameInput.Visible = false;
            _storeNameInput.Blur();
            _pickedItem = null;
            SetStoreFeedback("Store PUBLIC: nearby players can browse and buy.", false);
        }

        public void HandleOwnStoreAnnounced(string storeName)
        {
            // Scope announcement can arrive BEFORE the inventory/store window
            // was ever opened, e.g. when OpenMU restores a shop after relog.
            EnsureCharacterState();
            if (_state == null || string.IsNullOrWhiteSpace(storeName))
                return;
            bool wasPublic = _storePublic;
            _publishedStoreName = storeName;
            _storePublic = true;
            if (!wasPublic)
                NotifyStoreInChat($"Store published by OpenMU: {storeName}.");
            _openingRequested = false;
            _closingRequested = false;
            _storeNameInput.Value = storeName;
            _storeNameInput.Visible = false;
            _storeNameInput.Blur();
            _pickedItem = null;
            SetStoreFeedback("Store PUBLIC: announced by OpenMU.", false);
        }

        public void HandleStoreClosed(bool successful)
        {
            if (!successful) return;
            bool wasPublic = _storePublic;
            _storePublic = false;
            if (wasPublic)
                NotifyStoreInChat("Store closed. Items and prices can be edited again.");
            _closingRequested = false;
            _openingRequested = false;
            _storeNameInput.Visible = Visible;
            if (!string.IsNullOrWhiteSpace(_publishedStoreName))
                _storeNameInput.Value = _publishedStoreName;
            SetStoreFeedback("Store closed by OpenMU. You may edit items.", false);
        }

        public void HandleStoreItemSold(byte slot)
        {
            _confirmedPrices.Remove(slot);
            if (Visible) RefreshShopItems();
            SetStoreFeedback("An item was sold from your public store.", false);
        }

        private InventoryItem ItemAt(Point mouse)
        {
            if (!TryGetVisualSlot(mouse, out int slot)) return null;
            return _occupied[slot % Columns, slot / Columns];
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
            _cursorTimeSeconds += gameTime.ElapsedGameTime.TotalSeconds;
            EnsureCharacterState();
            if (_requestInFlight && _state?.IsPlayerShopMovePending == false)
                _requestInFlight = false;
            // Render 3D shop icons only in Update (never inside Draw's
            // SpriteBatch). Assets can still be loading during the first
            // frames after login, so one failed attempt must not hide an icon
            // forever until a vault move forces another preview.
            _missingPreviewCooldown -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_items.Count > 0 && _missingPreviewCooldown <= 0)
            {
                _missingPreviewCooldown = 0.20;
                for (int inspected = 0; inspected < _items.Count; inspected++)
                {
                    var item = _items[_previewIndex++ % _items.Count];
                    string path = item.Definition.TexturePath;
                    if (string.IsNullOrEmpty(path) ||
                        !path.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                        continue;
                    int width = item.Definition.Width * CellSize;
                    int height = item.Definition.Height * CellSize;
                    if (BmdPreviewRenderer.TryGetCachedPreview(item, width, height) != null)
                        continue;
                    string key = path + ":" + width + "x" + height;
                    _previewRetryCounts.TryGetValue(key, out int retryCount);
                    if (retryCount >= MaxMissingPreviewRetries)
                        continue;
                    _previewRetryCounts[key] = retryCount + 1;
                    try
                    {
                        BmdPreviewRenderer.RetryMissingPreview(item, width, height);
                    }
                    catch { /* Keep the normal inventory placeholder on failure. */ }
                    break; // At most one render attempt per 200 ms.
                }
            }
            var current = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool inside = DisplayRectangle.Contains(current.Position);
            _hoveredItem = !_priceEditorVisible && inside ? ItemAt(current.Position) : null;
            _closeHovered = inside && ToScreen(CloseButton).Contains(current.Position);
            _exitHovered = inside && ToScreen(ExitButton).Contains(current.Position);

            if (inside && current.LeftButton == ButtonState.Pressed)
            {
                Scene?.SetMouseInputConsumed();
            }

            bool leftJustReleased = current.LeftButton == ButtonState.Released &&
                                    previous.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = current.LeftButton == ButtonState.Pressed &&
                                   previous.LeftButton == ButtonState.Released;

            if (!_priceEditorVisible && _storeNameInput.IsFocused &&
                MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                _storeNameInput.Blur();
                if (Scene?.FocusControl == _storeNameInput)
                    Scene.FocusControl = this;
                return;
            }

            if (_priceEditorVisible)
            {
                _longPressItem = null;
                if (current.LeftButton == ButtonState.Released)
                    _suppressLongPressRelease = false;
                bool escPressed = MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                                  MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape);
                if (escPressed)
                {
                    Scene?.ConsumeKeyboardEscape();
                    CancelPriceEditor();
                    return;
                }

                if (leftJustPressed)
                {
                    if (_priceInput.DisplayRectangle.Contains(current.Position))
                    {
                        // Preserve input focus when clicking directly inside the
                        // price bar. The shop parent is itself interactive and
                        // may otherwise steal focus on mouse-down.
                        _priceInput.Focus();
                        if (Scene != null)
                            Scene.FocusControl = _priceInput;
                    }
                    else if (ToScreen(PriceConfirmButton).Contains(current.Position))
                        SubmitPrice();
                    else if (ToScreen(PriceCancelButton).Contains(current.Position))
                        CancelPriceEditor();
                    // Price dialog is modal; never pick up or move the item behind it.
                }

                if (leftJustPressed || current.LeftButton == ButtonState.Pressed ||
                    current.RightButton == ButtonState.Pressed)
                    Scene?.SetMouseInputConsumed();
                return;
            }

            // Editable name field is inside the parent interactive store window.
            // Explicit focus protects it from parent mouse-down focus stealing.
            if (leftJustPressed && !_storePublic &&
                ToScreen(StoreNameEditBox).Contains(current.Position))
            {
                FocusNameInput();
                Scene?.SetMouseInputConsumed();
                return;
            }

            // Long press (~620 ms) opens the same price dialog on Android,
            // using the touch-to-UiMouseState conversion already in BaseScene.
            // Dragging farther than 10 virtual px cancels the gesture so item
            // rearrangement and inventory transfers are unaffected.
            if (_longPressItem != null)
            {
                if (current.LeftButton != ButtonState.Pressed)
                {
                    _longPressItem = null;
                    _longPressElapsedMilliseconds = 0;
                }
                else if (Math.Abs(current.Position.X - _longPressOrigin.X) >
                             LongPressMovementTolerance ||
                         Math.Abs(current.Position.Y - _longPressOrigin.Y) >
                             LongPressMovementTolerance)
                {
                    _longPressItem = null;
                    _longPressElapsedMilliseconds = 0;
                }
                else
                {
                    _longPressElapsedMilliseconds += gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (_longPressElapsedMilliseconds >= LongPressPriceMilliseconds)
                    {
                        var pressedItem = _longPressItem;
                        _longPressItem = null;
                        _longPressElapsedMilliseconds = 0;
                        _suppressLongPressRelease = true;
                        OpenPriceEditor(pressedItem);
                        if (_priceEditorVisible)
                        {
                            Scene?.SetMouseInputConsumed();
                            return;
                        }
                        _suppressLongPressRelease = false;
                    }
                }
            }

            if (leftJustReleased && _suppressLongPressRelease)
            {
                _suppressLongPressRelease = false;
                Scene?.SetMouseInputConsumed();
                return;
            }

            bool rightJustPressed = current.RightButton == ButtonState.Pressed &&
                                    previous.RightButton == ButtonState.Released;
            if (rightJustPressed && inside && !_requestInFlight &&
                !_pendingPriceSlot.HasValue)
            {
                var itemForPrice = ItemAt(current.Position);
                if (itemForPrice != null)
                {
                    OpenPriceEditor(itemForPrice);
                    Scene?.SetMouseInputConsumed();
                    return;
                }
            }

            if (leftJustReleased && (_closeHovered || _exitHovered))
            {
                CloseWindow();
                return;
            }

            if (leftJustPressed && inside)
            {
                if (ToScreen(OpenButton).Contains(current.Position))
                {
                    RequestOpenStore();
                    Scene?.SetMouseInputConsumed();
                    return;
                }
                if (ToScreen(CloseStoreButton).Contains(current.Position))
                {
                    RequestCloseStore();
                    Scene?.SetMouseInputConsumed();
                    return;
                }
            }

            if (_storePublic || _openingRequested || _closingRequested)
            {
                _pickedItem = null;
                _longPressItem = null;
                if (inside && (leftJustPressed || current.LeftButton == ButtonState.Pressed))
                    Scene?.SetMouseInputConsumed();
                return;
            }

            // Support the classic press-drag-release gesture as well as the
            // existing click-pick / click-place interaction.
            if (leftJustReleased && _pickedItem != null &&
                (Math.Abs(current.Position.X - _pickStartPoint.X) > 3 ||
                 Math.Abs(current.Position.Y - _pickStartPoint.Y) > 3))
            {
                var inventory = InventoryControl.Instance;
                if (inventory?.Visible == true &&
                    inventory.DisplayRectangle.Contains(current.Position))
                {
                    TryHandleInventoryDrop(current.Position, inventory);
                }
                else if (TryGetVisualSlot(current.Position, out int dragSlot))
                {
                    var destination = new Point(dragSlot % Columns, dragSlot / Columns);
                    if (CanPlaceAt(destination, _pickedItem))
                    {
                        byte toSlot = (byte)(FirstStoreSlot + dragSlot);
                        if (toSlot != _pickedSlot)
                            SendConfirmedStorageMove(_pickedSlot, ItemStorageKind.PlayerShop, toSlot);
                        else
                            _pickedItem = null;
                    }
                    else _pickedItem = null;
                }
                else _pickedItem = null;

                Scene?.SetMouseInputConsumed();
                return;
            }

            _hoveredItem = inside ? ItemAt(current.Position) : null;
            if (leftJustPressed && inside && !_requestInFlight &&
                TryGetVisualSlot(current.Position, out int targetSlot))
            {
                if (_pickedItem == null)
                {
                    if (_hoveredItem != null)
                    {
                        _pickedItem = _hoveredItem;
                        _longPressItem = _hoveredItem;
                        _longPressOrigin = current.Position;
                        _longPressElapsedMilliseconds = 0;
                        _pickStartPoint = current.Position;
                        _pickedSlot = (byte)(FirstStoreSlot +
                            _hoveredItem.GridPosition.Y * Columns +
                            _hoveredItem.GridPosition.X);
                        SoundController.Instance.PlayBuffer("Sound/iButton.wav");
                    }
                }
                else
                {
                    Point target = new(targetSlot % Columns, targetSlot / Columns);
                    if (CanPlaceAt(target, _pickedItem))
                    {
                        byte toSlot = (byte)(FirstStoreSlot + targetSlot);
                        if (toSlot != _pickedSlot)
                            SendConfirmedStorageMove(_pickedSlot, ItemStorageKind.PlayerShop, toSlot);
                        else
                            _pickedItem = null;
                    }
                    else
                        _pickedItem = null;
                }
                Scene?.SetMouseInputConsumed();
            }
            // Shop publication remains disabled until the next phase.
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

            EnsureUiTextureReferences();
            using (new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                // Both fixed and dynamic elements share a single draw batch,
                // avoiding stale RenderTarget2D content after character/server selection.
                DrawStaticSurface(sprite);
                DrawItems(sprite);
                if (_storePublic)
                    DrawTextInRect(sprite, _publishedStoreName,
                        ToScreen(StoreNameEditBox), 0.43f * Scale,
                        new Color(247, 223, 175) * Alpha);
                else
                    DrawEditableText(sprite, _storeNameInput, StoreNameEditBox,
                        "Click to name your store", 0.43f);
                if (!_priceEditorVisible)
                    DrawPlacementPreview(sprite);
                DrawTextInRect(sprite, _priceFeedback,
                    ToScreen(PersonalStoreLayout.Feedback), 0.32f * Scale,
                    _priceFeedbackError ? new Color(247, 130, 105) : new Color(172, 205, 148));
                DrawTextInRect(sprite, _storePublic ? "Store open" : "Store closed",
                    ToScreen(PersonalStoreLayout.State), 0.40f * Scale, Color.Wheat);
                DrawStoreActionButtons(sprite);
                DrawExitButton(sprite);
                DrawCloseButton(sprite);
                if (_priceEditorVisible)
                    DrawPriceEditor(sprite);
            }
            // Input controls continue updating keyboard, clipboard and Android
            // text handling through base.Update(). Their nested Draw() creates a
            // scissored SpriteBatch with a different blend state, leaving digits
            // almost black on the NewUI panel. Values are rendered legibly in
            // the same NonPremultiplied batch as the surrounding NewUI text.
        }

        /// <summary>
        /// Textures are managed by TextureLoader across scenes. Recover local
        /// references when a graphics/device reset invalidated one of them.
        /// This is a cheap no-op during normal gameplay.
        /// </summary>
        private void EnsureUiTextureReferences()
        {
            var loader = TextureLoader.Instance;
            if (_frameAtlas == null || _frameAtlas.IsDisposed)
                _frameAtlas = loader.GetTexture2D(FramePath);
            if (_slotAtlas == null || _slotAtlas.IsDisposed)
                _slotAtlas = loader.GetTexture2D(SlotPath);
            if (_buttonNormal == null || _buttonNormal.IsDisposed)
                _buttonNormal = loader.GetTexture2D(ButtonNormalPath);
            if (_buttonHover == null || _buttonHover.IsDisposed)
                _buttonHover = loader.GetTexture2D(ButtonHoverPath);
            if (_buttonPressed == null || _buttonPressed.IsDisposed)
                _buttonPressed = loader.GetTexture2D(ButtonPressedPath);
            if (_closeNormal == null || _closeNormal.IsDisposed)
                _closeNormal = loader.GetTexture2D(CloseNormalPath);
            if (_closeHover == null || _closeHover.IsDisposed)
                _closeHover = loader.GetTexture2D(CloseHoverPath);
            if (_closePressed == null || _closePressed.IsDisposed)
                _closePressed = loader.GetTexture2D(ClosePressedPath);
        }

        private void DrawStaticSurface(SpriteBatch sprite)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null)
            {
                return;
            }

            // Underlay prevents holes in translucent atlas pixels.
            sprite.Draw(pixel, ToScreen(new Rectangle(12, 90, WindowWidth - 24, WindowHeight - 112)),
                new Color(25, 23, 23, 255) * Alpha);
            NewUiFrameRenderer.DrawPersonalShopWindow(sprite, _frameAtlas,
                ToScreen(new Rectangle(0, 0, WindowWidth, WindowHeight)));
            DrawCenteredText(sprite, "PERSONAL STORE", 52, 0.53f, new Color(233, 219, 195));

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var destination = new Rectangle(
                        ItemGrid.X + column * CellSize,
                        ItemGrid.Y + row * CellSize, CellSize, CellSize);
                    var gridCell = ToScreen(destination);
                    sprite.Draw(pixel, gridCell, new Color(17, 20, 25, 255) * Alpha);
                    if (_slotAtlas != null && !_slotAtlas.IsDisposed)
                    {
                        sprite.Draw(_slotAtlas, gridCell, SlotSource, Color.White * Alpha);
                    }
                }
            }

        }

        private void DrawItems(SpriteBatch sprite)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            foreach (var item in _items)
            {
                var local = new Rectangle(ItemGrid.X + item.GridPosition.X * CellSize,
                    ItemGrid.Y + item.GridPosition.Y * CellSize,
                    item.Definition.Width * CellSize, item.Definition.Height * CellSize);
                var rect = ToScreen(local);
                if (pixel != null && (item == _hoveredItem || item.GridPosition.Y * Columns +
                                      item.GridPosition.X + FirstStoreSlot == _pickedSlot && _pickedItem != null))
                    sprite.Draw(pixel, rect, new Color(170, 125, 65, 75));

                var texture = ResolveItemTexture(item);
                bool selectedFromStore = item == _pickedItem;
                if (texture != null && !texture.IsDisposed)
                    sprite.Draw(texture, rect, Color.White * (selectedFromStore ? 0.28f : Alpha));
                else
                    ItemGridRenderHelper.DrawItemPlaceholder(sprite, pixel, _font, rect,
                        item, new Color(39, 34, 32, 225), new Color(225, 213, 196));

                if (_font != null && item.Definition.BaseDurability == 0 &&
                    item.Definition.MagicDurability == 0 && item.Durability > 1)
                    ItemGridRenderHelper.DrawItemStackCount(sprite, _font, rect, item.Durability,
                        new Color(240, 210, 140), Alpha);

            }
        }


        /// <summary>
        /// Match the inventory/vault slot shadows. The entire item's footprint
        /// is painted, not just the slot under the mouse. Invalid targets are
        /// red; valid targets use the same green-yellow as Inventory.
        /// </summary>
        private void DrawPlacementPreview(SpriteBatch sprite)
        {
            var pixel = GraphicsManager.Instance?.Pixel;
            if (pixel == null || _requestInFlight ||
                !TryGetVisualSlot(MuGame.Instance.UiMouseState.Position, out int targetIndex))
                return;

            InventoryItem item = InventoryControl.Instance?.GetDraggedItem() ?? _pickedItem;
            if (item?.Definition == null)
            {
                // An empty square still gets a subtle hover, matching vault.
                var emptyCell = ToScreen(new Rectangle(
                    ItemGrid.X + (targetIndex % Columns) * CellSize,
                    ItemGrid.Y + (targetIndex / Columns) * CellSize, CellSize, CellSize));
                sprite.Draw(pixel, emptyCell, new Color(95, 92, 83, 45));
                return;
            }

            Point target = new(targetIndex % Columns, targetIndex / Columns);
            bool canPlace = CanPlaceAt(target, item);
            Color fill = canPlace ? Color.GreenYellow * 0.36f : Color.Red * 0.48f;
            Color border = canPlace ? new Color(184, 243, 112, 195) : new Color(242, 106, 94, 215);

            for (int y = 0; y < item.Definition.Height; y++)
                for (int x = 0; x < item.Definition.Width; x++)
                {
                    int gx = target.X + x, gy = target.Y + y;
                    if (gx < 0 || gy < 0 || gx >= Columns || gy >= Rows)
                        continue;

                    var cell = ToScreen(new Rectangle(
                        ItemGrid.X + gx * CellSize, ItemGrid.Y + gy * CellSize,
                        CellSize, CellSize));
                    sprite.Draw(pixel, cell, fill);
                    sprite.Draw(pixel, new Rectangle(cell.Left, cell.Top, cell.Width, 1), border);
                    sprite.Draw(pixel, new Rectangle(cell.Left, cell.Bottom - 1, cell.Width, 1), border);
                    sprite.Draw(pixel, new Rectangle(cell.Left, cell.Top, 1, cell.Height), border);
                    sprite.Draw(pixel, new Rectangle(cell.Right - 1, cell.Top, 1, cell.Height), border);
                }

            // A faint item image is useful when moving a multi-cell item.
            // Clipped preview is not drawn outside the 8x4 grid.
            if (canPlace)
            {
                var ghost = ResolveItemTexture(item);
                if (ghost != null && !ghost.IsDisposed)
                {
                    var rect = ToScreen(new Rectangle(ItemGrid.X + target.X * CellSize,
                        ItemGrid.Y + target.Y * CellSize,
                        item.Definition.Width * CellSize, item.Definition.Height * CellSize));
                    sprite.Draw(ghost, rect, Color.White * 0.38f);
                }
            }
        }

        private Texture2D ResolveItemTexture(InventoryItem item)
        {
            if (item?.Definition == null) return null;
            string path = item.Definition.TexturePath;
            if (string.IsNullOrEmpty(path)) return null;
            if (path.EndsWith(".bmd", StringComparison.OrdinalIgnoreCase))
                return BmdPreviewRenderer.TryGetCachedPreview(item,
                    item.Definition.Width * CellSize, item.Definition.Height * CellSize);

            if (!_textures.TryGetValue(path, out var texture) ||
                texture == null || texture.IsDisposed)
            {
                texture = TextureLoader.Instance.GetTexture2D(path);
                if (texture != null) _textures[path] = texture;
            }
            return texture;
        }

        /// <summary>Drawn by GameScene after the world and all window post-passes.</summary>
        public void DrawTooltipOverlay(SpriteBatch sprite)
        {
            if (!Visible || Status != GameControlStatus.Ready || _priceEditorVisible ||
                _pickedItem != null || _requestInFlight || _font == null ||
                InventoryControl.Instance.GetDraggedItem() != null ||
                !PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position))
                return;

            // Resolve from the current pointer and occupied cells, including published stores.
            var mouse = MuGame.Instance.UiMouseState.Position;
            var item = ItemAt(mouse);
            if (item == null) return;
            byte slot = (byte)(FirstStoreSlot + item.GridPosition.Y * Columns + item.GridPosition.X);
            PetInfoSnapshot? petInfo = null;
            if (_state != null && _state.TryGetPetInfo(slot, out var info)) petInfo = info;
            var lines = ClassicItemTooltipBuilder.Build(item, petInfo);
            if (_confirmedPrices.TryGetValue(slot, out var price))
                lines.Add((price.Price > 0 ? "Store Price: " + FormatZen(price.Price) + " Zen"
                    : "Not for sale", Color.Goldenrod));
            var rect = ToScreen(new Rectangle(ItemGrid.X + item.GridPosition.X * CellSize,
                ItemGrid.Y + item.GridPosition.Y * CellSize,
                item.Definition.Width * CellSize, item.Definition.Height * CellSize));
            ClassicItemTooltipRenderer.Draw(sprite, _font, item, lines, rect, mouse);
        }

        private void DrawPriceEditor(SpriteBatch sprite)
        {
            var pixel = GraphicsManager.Instance?.Pixel;
            if (pixel == null) return;
            var modal = ToScreen(PriceDialog);
            sprite.Draw(pixel, modal, new Color(16, 19, 25, 248));
            Color outline = new Color(142, 111, 69, 220);
            sprite.Draw(pixel, new Rectangle(modal.X, modal.Y, modal.Width, 2), outline);
            sprite.Draw(pixel, new Rectangle(modal.X, modal.Bottom - 2, modal.Width, 2), outline);
            sprite.Draw(pixel, new Rectangle(modal.X, modal.Y, 2, modal.Height), outline);
            sprite.Draw(pixel, new Rectangle(modal.Right - 2, modal.Y, 2, modal.Height), outline);

            DrawTextInRect(sprite, "SET ITEM PRICE",
                ToScreen(new Rectangle(45, 238, 257, 30)), 0.50f * Scale,
                new Color(245, 221, 183));
            DrawTextInRect(sprite, "Price in Zen (0 removes price)",
                ToScreen(new Rectangle(51, 268, 246, 24)), 0.40f * Scale,
                new Color(187, 181, 170));
            DrawEditableText(sprite, _priceInput, new Rectangle(60, 301, 228, 31),
                "Type Zen amount", 0.51f);
            DrawTextInRect(sprite, _priceInputError,
                ToScreen(new Rectangle(44, 333, 260, 17)), 0.28f * Scale,
                new Color(246, 123, 112));
            DrawThreeSlice(sprite, _buttonNormal, ToScreen(PriceConfirmButton),
                Color.White * Alpha);
            DrawThreeSlice(sprite, _buttonNormal, ToScreen(PriceCancelButton),
                Color.White * Alpha);
            DrawTextInRect(sprite, "Confirm", ToScreen(PriceConfirmButton),
                0.43f * Scale, new Color(240, 218, 182));
            DrawTextInRect(sprite, "Cancel", ToScreen(PriceCancelButton),
                0.43f * Scale, new Color(240, 218, 182));
        }

        /// <summary>
        /// Shares the NewUI batch for full-bright readable field text. The
        /// TextFieldControl still handles all editing, selection and platform
        /// keyboard input; this is only a visual replacement for its clipped
        /// nested SpriteBatch rendering.
        /// </summary>
        private void DrawEditableText(SpriteBatch sprite, TextFieldControl field,
            Rectangle localRect, string placeholder, float baseFontScale)
        {
            var pixel = GraphicsManager.Instance?.Pixel;
            if (_font == null || pixel == null || field == null || !field.Visible)
                return;
            var rect = ToScreen(localRect);
            sprite.Draw(pixel, rect, new Color(18, 20, 24, 255));
            Color border = field.IsFocused
                ? new Color(210, 167, 100, 255)
                : new Color(116, 91, 61, 215);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), border);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
            sprite.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);

            string value = field.Value ?? string.Empty;
            bool placeholderActive = value.Length == 0;
            string display = placeholderActive ? placeholder : value;
            float scale = baseFontScale * Scale;
            var measured = _font.MeasureString(display);
            if (measured.X > 0 && measured.X * scale > rect.Width - 18)
                scale = (rect.Width - 18f) / measured.X;
            Vector2 textPosition = new(
                rect.X + 8f,
                rect.Y + (rect.Height - measured.Y * scale) * 0.5f);
            Color color = placeholderActive ? new Color(160, 146, 130)
                : new Color(250, 231, 193);
            sprite.DrawString(_font, display, textPosition + Vector2.One,
                Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sprite.DrawString(_font, display, textPosition,
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (field.IsFocused && (int)(_cursorTimeSeconds * 2) % 2 == 0)
            {
                float caretX = textPosition.X + (placeholderActive ? 0 :
                    measured.X * scale);
                if (caretX < rect.Right - 4)
                    sprite.Draw(pixel, new Rectangle((int)caretX + (placeholderActive ? 0 : 2),
                        rect.Y + 5, 1, Math.Max(1, rect.Height - 10)),
                        new Color(245, 225, 180));
            }
        }

        private void DrawStoreActionButtons(SpriteBatch sprite)
        {
            bool busy = _openingRequested || _closingRequested || _requestInFlight ||
                _pendingPriceSlot.HasValue;
            bool canOpen = !busy && !_storePublic;
            bool canClose = !busy && _storePublic;
            var mouse = MuGame.Instance.UiMouseState;
            DrawStoreActionButton(sprite, OpenButton, "Open", canOpen,
                canOpen && ToScreen(OpenButton).Contains(mouse.Position));
            DrawStoreActionButton(sprite, CloseStoreButton, "Close", canClose,
                canClose && ToScreen(CloseStoreButton).Contains(mouse.Position));
        }

        private void DrawStoreActionButton(SpriteBatch sprite, Rectangle local, string label,
            bool enabled, bool hovered)
        {
            bool pressed = hovered &&
                MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            var tex = pressed ? _buttonPressed ?? _buttonNormal
                : hovered ? _buttonHover ?? _buttonNormal : _buttonNormal;
            DrawThreeSlice(sprite, tex, ToScreen(local),
                enabled ? Color.White * Alpha : new Color(100, 95, 90, 160));
            DrawTextInRect(sprite, label, ToScreen(local), 0.42f * Scale,
                enabled ? new Color(241, 221, 188) * Alpha :
                    new Color(151, 145, 138) * Alpha);
        }

        private void DrawExitButton(SpriteBatch sprite)
        {
            var texture = _exitHovered
                ? (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    ? _buttonPressed ?? _buttonHover ?? _buttonNormal
                    : _buttonHover ?? _buttonNormal)
                : _buttonNormal;
            DrawThreeSlice(sprite, texture, ToScreen(ExitButton), Color.White * Alpha);
            if (_closeNormal != null)
                sprite.Draw(_closeNormal, ToScreen(new Rectangle(ExitButton.X + 11,
                    ExitButton.Y + 5, 22, 22)), Color.White * Alpha);
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
            DrawTextInRect(sprite, text, ToScreen(new Rectangle(0, centerY - 15, WindowWidth, 30)),
                fontScale * Scale, color * Alpha);
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

        public override void Dispose()
        {
            // Release resources belonging to the outgoing GameScene. The next
            // character/session must initialize its own window and UI surface.
            if (_state != null)
                _state.InventoryChanged -= OnInventoryChanged;
            _state = null;
            _priceInput.Blur();
            _storeNameInput.Blur();
            _textures.Clear(); // TextureLoader owns the underlying textures.
            _items.Clear();
            _previewRetryCounts.Clear();
            _confirmedPrices.Clear();
            if (ReferenceEquals(_instance, this))
                _instance = null;
            base.Dispose();
        }
    }
}
