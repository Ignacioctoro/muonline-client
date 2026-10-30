using System;
using System.Buffers.Binary;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Networking;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI;
using Client.Main.Scenes;
using Client.Main.Controls.UI.Game;

namespace Client.Main.Networking.PacketHandling.Handlers
{
    /// <summary>
    /// Handles packets related to NPC shops (StoreItemList etc.).
    /// </summary>
    public partial class ShopHandler : IGamePacketHandler
    {
        private readonly ILogger<ShopHandler> _logger;
        private readonly CharacterState _characterState;
        private readonly NetworkManager _networkManager;
        private readonly TargetProtocolVersion _targetVersion;
        private NpcWindowResponse.NpcWindow? _lastWindow;

        public ShopHandler(
            ILoggerFactory loggerFactory,
            CharacterState characterState,
            NetworkManager networkManager,
            TargetProtocolVersion targetVersion)
        {
            _logger = loggerFactory.CreateLogger<ShopHandler>();
            _characterState = characterState;
            _networkManager = networkManager;
            _targetVersion = targetVersion;
        }

        /// <summary>
        /// C1 3F 02: server accepted the owner's open request.
        /// </summary>
        [PacketHandler(0x3F, 0x02)]
        public Task HandlePlayerShopOpenSuccessfulAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 5 || span[0] != 0xC1 ||
                span[2] != 0x3F || span[3] != 0x02)
                return Task.CompletedTask;
            bool success = span[4] != 0;
            _logger.LogInformation("Personal store opened: {Success}", success);
            MuGame.ScheduleOnMainThread(() =>
                PersonalStoreControl.Instance.HandleStoreOpenResult(success));
            return Task.CompletedTask;
        }

        /// <summary>
        /// C1 3F 03: shop closure, broadcast to owner and observers.
        /// Do not close our store when another player closes theirs.
        /// </summary>
        [PacketHandler(0x3F, 0x03)]
        public Task HandlePlayerShopClosedAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 7 || span[0] != 0xC1 ||
                span[2] != 0x3F || span[3] != 0x03)
                return Task.CompletedTask;
            bool success = span[4] != 0;
            ushort ownerId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(5, 2));
            if (success)
            {
                // Closures are broadcast to everyone in scope, not just owner.
                MuGame.Network?.GetScopeManager()?.RemovePlayerShop(ownerId);
                MuGame.ScheduleOnMainThread(() =>
                    PersonalStoreBuyerControl.Instance.HandleSellerClosed(ownerId));
                if ((ownerId & 0x7FFF) == (_characterState.Id & 0x7FFF))
                {
                    MuGame.ScheduleOnMainThread(() =>
                        PersonalStoreControl.Instance.HandleStoreClosed(true));
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// C1 3F 12: OpenMU orders clients viewing this seller to close the dialog.
        /// </summary>
        [PacketHandler(0x3F, 0x12)]
        public Task HandleClosePlayerShopDialogAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length >= 6 && span[0] == 0xC1 &&
                span[2] == 0x3F && span[3] == 0x12)
            {
                ushort sellerId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));
                MuGame.ScheduleOnMainThread(() =>
                    PersonalStoreBuyerControl.Instance.HandleSellerClosed(sellerId));
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// C2 3F 00: store names in nearby player scope. Includes ourselves
        /// after successful opening and (in supported clients) on relog.
        /// </summary>
        [PacketHandler(0x3F, 0x00)]
        public Task HandlePlayerShopAnnouncementsAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 6 || span[0] != 0xC2 ||
                span[3] != 0x3F || span[4] != 0x00)
                return Task.CompletedTask;
            int count = span[5];
            if (span.Length < 6 + count * 38)
            {
                _logger.LogWarning("Truncated PlayerShops packet: {Length} bytes, {Count} stores.", span.Length, count);
                return Task.CompletedTask;
            }

            var scope = MuGame.Network?.GetScopeManager();
            for (int i = 0; i < count; i++)
            {
                int offset = 6 + i * 38;
                ushort ownerId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset, 2));
                string name = Encoding.UTF8.GetString(span.Slice(offset + 2, 36))
                    .TrimEnd('\0');
                // The packet can precede the player's spawn packet.
                // Store by server network ID and resolve when the model appears.
                scope?.SetPlayerShop(ownerId, name);
                _logger.LogInformation("Personal Store advertised: networkId={Id:X4}, title={Title}", ownerId, name);
                if ((ownerId & 0x7FFF) == (_characterState.Id & 0x7FFF))
                {
                    var ownName = name;
                    MuGame.ScheduleOnMainThread(() =>
                        PersonalStoreControl.Instance.HandleOwnStoreAnnounced(ownName));
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// C2 3F 05: the real item list for another player's public shop.
        /// OpenMU's generated packet definition writes Action at byte 4,
        /// overlapping the nominal C2 subcode. Success is byte 5, not 6.
        /// Extended items are variable-sized; classic items are 20 bytes.
        /// </summary>
        [PacketHandler(0x3F, 0x05)]
        public Task HandlePlayerShopItemListAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 55 || span[0] != 0xC2 ||
                span[3] != 0x3F || (span[4] != 0x05 && span[4] != 0x13))
            {
                _logger.LogWarning("Invalid Personal Store item list packet, length {Length}.", span.Length);
                return Task.CompletedTask;
            }

            // Byte 4 is Action (05=ByRequest, 13=UpdateAfterItemChange).
            // The generated OpenMU Ref structure deliberately writes this at
            // the nominal subcode position. Byte 5 is Success.
            byte action = span[4];
            if (action != 5 && action != 19)
            {
                _logger.LogWarning("Unexpected Personal Store list action {Action}.", action);
                return Task.CompletedTask;
            }

            if (span[5] == 0)
            {
                MuGame.ScheduleOnMainThread(() =>
                {
                    var scene = MuGame.Instance?.ActiveScene as GameScene;
                    scene?.ChatLog?.AddMessage("Personal Store",
                        "OpenMU refused the request to view this store.",
                        Client.Main.Models.MessageType.System);
                });
                return Task.CompletedTask;
            }

            ushort sellerId = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2));
            string sellerName = Encoding.UTF8.GetString(span.Slice(8, 10)).TrimEnd('\0');
            string shopName = Encoding.UTF8.GetString(span.Slice(18, 36)).TrimEnd('\0');
            int itemCount = span[54];
            int position = 55;
            // Extended entries may have the same byte length as classic entries.
            // Use the negotiated protocol, never infer currency layout from length.
            bool classic = _targetVersion < TargetProtocolVersion.Season6;
            var entries = new System.Collections.Generic.List<(byte Slot, uint Price, ushort Currency, byte[] Data)>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                byte slot;
                ushort currency = 0;
                uint price;
                byte[] data;
                if (classic)
                {
                    if (position + 20 > span.Length)
                    {
                        _logger.LogWarning("Truncated classic Personal Store list.");
                        return Task.CompletedTask;
                    }
                    slot = span[position];
                    data = span.Slice(position + 1, 12).ToArray();
                    price = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position + 16, 4));
                    position += 20;
                }
                else
                {
                    if (position + 9 > span.Length)
                    {
                        _logger.LogWarning("Truncated extended Personal Store list.");
                        return Task.CompletedTask;
                    }
                    price = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(position, 4));
                    currency = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position + 4, 2));
                    ushort pieces = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position + 6, 2));
                    if (pieces > 0) price = pieces;
                    else if (!Client.Main.Core.Client.StoreCurrencyState.IsAccountCoin(currency)) currency = 0;
                    slot = span[position + 8];
                    position += 9;
                    if (!ItemDataParser.TryGetExtendedItemLength(span.Slice(position),
                        out int itemLength) || itemLength <= 0 ||
                        position + itemLength > span.Length)
                    {
                        _logger.LogWarning("Unparseable extended Personal Store item {Index}.", i);
                        return Task.CompletedTask;
                    }
                    data = span.Slice(position, itemLength).ToArray();
                    position += itemLength;
                }
                if (slot < PersonalStoreControl.FirstStoreSlot ||
                    slot >= PersonalStoreControl.FirstStoreSlot + PersonalStoreControl.SlotCount)
                {
                    _logger.LogWarning("Ignoring invalid Personal Store slot {Slot}.", slot);
                    continue;
                }
                entries.Add((slot, price, currency, data));
            }

            if (position != span.Length)
            {
                _logger.LogWarning("Personal Store list has {Bytes} unexpected trailing bytes.",
                    span.Length - position);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Personal Store list from {Name} id={Id:X4}: {Count} items, action={Action}, classic={Classic}",
                sellerName, sellerId, entries.Count, action, classic);
            MuGame.ScheduleOnMainThread(() =>
            {
                var buyer = PersonalStoreBuyerControl.Instance;
                if (action == 5 || buyer.IsViewing(sellerId))
                {
                    buyer.ShowListing(sellerId, sellerName, shopName, entries);
                }
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// OpenMU sends action 0x13 at C2 offset 4 after item changes,
        /// so route those updates through the same verified parser.
        /// </summary>
        [PacketHandler(0x3F, 0x13)]
        public Task HandlePlayerShopItemListUpdateAsync(Memory<byte> packet) =>
            HandlePlayerShopItemListAsync(packet);

        /// <summary>
        /// C1 3F 06: purchase acknowledgement. Server-provided inventory slot,
        /// item bytes and Zen updates are authoritative, not client projections.
        /// </summary>
        [PacketHandler(0x3F, 0x06)]
        public Task HandlePlayerShopBuyResultAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 8 || span[0] != 0xC1 ||
                span[2] != 0x3F || span[3] != 0x06)
                return Task.CompletedTask;

            // Season 6 uses PlayerShopBuyResultExtended. Earlier client versions
            // use a fixed 21-byte packet with a different field order.
            bool extended = _targetVersion >= TargetProtocolVersion.Season6;
            // A 21-byte result may be classic or extended with 13 item bytes.
            // Resolve ambiguous layouts by comparing the server seller ID with
            // the actual store currently being inspected.
            if (span.Length == 21)
            {
                ushort classicSeller = (ushort)(BinaryPrimitives.ReadUInt16BigEndian(span.Slice(5, 2)) & 0x7FFF);
                ushort extendedSeller = (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4, 2)) & 0x7FFF);
                ushort viewedSeller = PersonalStoreBuyerControl.Instance.SellerId;
                if (classicSeller == viewedSeller && extendedSeller != viewedSeller)
                    extended = false;
                else if (extendedSeller == viewedSeller && classicSeller != viewedSeller)
                    extended = true;
            }
            byte result = extended ? span[6] : span[4];
            if (result > 10)
            {
                _logger.LogWarning("Invalid Personal Store buy result: {Result}.", result);
                return Task.CompletedTask;
            }
            byte inventorySlot = extended ? span[7] : span.Length >= 21 ? span[20] : (byte)255;
            int dataStart = extended ? 8 : 7;
            int dataLength = extended ? span.Length - 8 : Math.Min(13, span.Length - 8);
            if (result == 1 && inventorySlot < PersonalStoreControl.FirstStoreSlot && dataLength > 0)
            {
                // OpenMU has already completed and committed the transfer.
                _characterState.AddOrUpdateInventoryItem(inventorySlot,
                    span.Slice(dataStart, dataLength).ToArray());
            }
            MuGame.ScheduleOnMainThread(() =>
            {
                PersonalStoreBuyerControl.Instance.HandleBuyResult(result);
                _ = _networkManager.GetCharacterService().RequestStoreBalancesAsync();
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// C1 3F 08: an item from our public store was sold. OpenMU removes
        /// it server-side and delivers this notification to the owner.
        /// </summary>
        [PacketHandler(0x3F, 0x08)]
        public Task HandlePlayerShopItemSoldAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 15 || span[0] != 0xC1 ||
                span[2] != 0x3F || span[3] != 0x08)
                return Task.CompletedTask;
            byte soldSlot = span[4];
            if (soldSlot >= PersonalStoreControl.FirstStoreSlot &&
                soldSlot < PersonalStoreControl.FirstStoreSlot + PersonalStoreControl.SlotCount)
            {
                _characterState.RemoveInventoryItem(soldSlot);
                _logger.LogInformation("Personal store item sold at slot {Slot}.", soldSlot);
                MuGame.ScheduleOnMainThread(() =>
                    PersonalStoreControl.Instance.HandleStoreItemSold(soldSlot));
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Personal Store item-price acknowledgement (C3 3F 01).
        /// Byte 4 is ItemPriceSetResult and byte 5 the absolute item slot.
        /// Only a matching pending request may update the displayed price.
        /// </summary>
        [PacketHandler(0x3F, 0x01)]
        public Task HandlePlayerShopSetItemPriceResponseAsync(Memory<byte> packet)
        {
            var span = packet.Span;
            if (span.Length < 6 || span[0] != 0xC3 ||
                span[2] != 0x3F || span[3] != 0x01)
            {
                _logger.LogWarning("Invalid PlayerShopSetItemPriceResponse length/header: {Length}",
                    packet.Length);
                return Task.CompletedTask;
            }

            byte result = span[4];
            byte slot = span[5];
            _logger.LogInformation("Personal Store price response: slot {Slot}, result {Result}",
                slot, result);
            MuGame.ScheduleOnMainThread(() =>
                PersonalStoreControl.Instance.HandlePriceSetResponse(slot, result));
            return Task.CompletedTask;
        }

        /// <summary>
        /// NpcWindowResponse: Open shop window when server requests a merchant window.
        /// </summary>
        [PacketHandler(0x30, PacketRouter.NoSubCode)]
        public Task HandleNpcWindowResponseAsync(Memory<byte> packet)
        {
            try
            {
                var resp = new NpcWindowResponse(packet);
                _lastWindow = resp.Window;
                if (resp.Window != NpcWindowResponse.NpcWindow.Lahap)
                    MuGame.ScheduleOnMainThread(() => LahapControl.Instance.HideWithoutServerRequest());

                if (resp.Window == NpcWindowResponse.NpcWindow.Merchant || resp.Window == NpcWindowResponse.NpcWindow.Merchant1)
                {
                    _characterState.ClearShopItems();

                    // Check if the NPC can repair items using the last NPC type number
                    bool canRepair = false;
                    if (_characterState.LastNpcTypeNumber != 0)
                    {
                        canRepair = NpcDatabase.CanNpcRepair(_characterState.LastNpcTypeNumber);
                        _logger.LogDebug("NPC type {TypeId} CanRepair = {CanRepair}",
                            _characterState.LastNpcTypeNumber, canRepair);
                    }

                    MuGame.ScheduleOnMainThread(() =>
                    {
                        var shop = NpcShopControl.Instance;
                        shop.SetRepairShop(canRepair);
                        shop.Visible = true;
                        shop.BringToFront();

                        // Also open the inventory side-by-side
                        var scene = MuGame.Instance?.ActiveScene as GameScene;
                        scene?.InventoryControl?.Show();
                    });
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.VaultStorage)
                {
                    _characterState.ClearVaultItems();
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        var vault = VaultControl.Instance;
                        vault.Visible = true;
                        vault.BringToFront();
                        var scene = MuGame.Instance?.ActiveScene as GameScene;
                        scene?.InventoryControl?.Show();
                    });
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.Lahap)
                {
                    // Open only after the server accepts the NPC conversation.
                    MuGame.ScheduleOnMainThread(() => LahapControl.Instance.ShowWindow());
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.PetTrainer)
                {
                    // The server explicitly identifies the trainer window (7).
                    // Do not open this dialog just by matching an NPC display name.
                    MuGame.ScheduleOnMainThread(() => PetTrainerControl.Instance.ShowWindow());
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.ChaosMachine)
                {
                    _characterState.ClearChaosMachineItems();
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        var chaos = ChaosMixControl.Instance;
                        chaos.ShowSelection();
                    });
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.DevilSquare)
                {
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        DevilSquareEnterControl.Instance.ShowWindow();
                    });
                }
                else if (resp.Window == NpcWindowResponse.NpcWindow.BloodCastle)
                {
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        BloodCastleEnterControl.Instance.ShowWindow();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing NpcWindowResponse packet.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// StoreItemList: Sent when opening merchant NPC or vault. Displays items in the opened window.
        /// </summary>
        [PacketHandler(0x31, PacketRouter.NoSubCode)]
        public Task HandleStoreItemListAsync(Memory<byte> packet)
        {
            try
            {
                var span = packet.Span;
                if (span.Length < 7)
                {
                    _logger.LogWarning("StoreItemList packet too short: {Length}", packet.Length);
                    return Task.CompletedTask;
                }

                var list = new StoreItemList(packet);
                byte count = list.ItemCount;
                int offset = 6;

                // Determine item data size by protocol version (matches inventory parsing)
                int dataSize = _targetVersion switch
                {
                    TargetProtocolVersion.Season6 => 12,
                    TargetProtocolVersion.Version097 => 11,
                    TargetProtocolVersion.Version075 => 7,
                    _ => 12
                };

                _logger.LogInformation("StoreItemList received: Type={Type}, Count={Count}, DataSize={Size}", list.Type, count, dataSize);

                // The Pet Trainer reuses the crafting storage (4) without an
                // additional NpcWindowResponse after choosing Resurrect Spirit.
                // On failure OpenMU sends StoreItemList type 5, not type 3.
                bool toChaosMachine =
                    _lastWindow == NpcWindowResponse.NpcWindow.ChaosMachine ||
                    list.Type == StoreItemList.ItemWindow.ChaosMachine ||
                    (_lastWindow == NpcWindowResponse.NpcWindow.PetTrainer &&
                     list.Type == StoreItemList.ItemWindow.ResurrectionFailed);
                bool toVault = !toChaosMachine &&
                    _lastWindow == NpcWindowResponse.NpcWindow.VaultStorage;

                if (toChaosMachine)
                {
                    _characterState.ClearChaosMachineItems();
                }
                else if (toVault)
                {
                    _characterState.ClearVaultItems();
                }
                else
                {
                    _characterState.ClearShopItems();
                }

                int parsedItems = 0;

                if (_targetVersion >= TargetProtocolVersion.Season6)
                {
                    int pos = offset;
                    int remaining = Math.Max(0, span.Length - offset);
                    bool fixedLength = count > 0 && remaining == count * (1 + 12);

                    for (int i = 0; i < count; i++)
                    {
                        if (pos + 1 > span.Length)
                        {
                            _logger.LogWarning("StoreItemList too short while reading slot at item {Index}.", i);
                            break;
                        }

                        byte slot = span[pos];
                        pos += 1;

                        ReadOnlySpan<byte> itemSpan = span.Slice(pos);
                        int length = 12;

                        if (!fixedLength)
                        {
                            if (!ItemDataParser.TryGetExtendedItemLength(itemSpan, out length) || pos + length > span.Length)
                            {
                                // Backward compatibility with classic fixed-length store items.
                                if (pos + 12 <= span.Length)
                                {
                                    length = 12;
                                    _logger.LogDebug(
                                        "StoreItemList item {Index}: extended length parse failed, falling back to 12-byte item data.",
                                        i);
                                }
                                else
                                {
                                    _logger.LogWarning("StoreItemList item {Index} has invalid data length.", i);
                                    break;
                                }
                            }
                        }

                        if (pos + length > span.Length)
                        {
                            _logger.LogWarning("StoreItemList too short while reading item {Index} data.", i);
                            break;
                        }

                        var data = span.Slice(pos, length).ToArray();
                        pos += length;

                        if (toChaosMachine) _characterState.AddOrUpdateChaosMachineItem(slot, data);
                        else if (toVault) _characterState.AddOrUpdateVaultItem(slot, data);
                        else _characterState.AddOrUpdateShopItem(slot, data);

                        parsedItems++;
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        var si = list[i, StoredItem.GetRequiredSize(dataSize)];
                        byte slot = si.ItemSlot;
                        var data = si.ItemData.Slice(0, dataSize).ToArray();

                        if (toChaosMachine) _characterState.AddOrUpdateChaosMachineItem(slot, data);
                        else if (toVault) _characterState.AddOrUpdateVaultItem(slot, data);
                        else _characterState.AddOrUpdateShopItem(slot, data);

                        parsedItems++;
                    }
                }

                _logger.LogInformation(
                    "StoreItemList parsed: requested={Requested}, parsed={Parsed}, target={Target}",
                    count,
                    parsedItems,
                    toChaosMachine ? "ChaosMachine" : toVault ? "Vault" : "Shop");

                if (toChaosMachine) _characterState.RaiseChaosMachineItemsChanged();
                else if (toVault) _characterState.RaiseVaultItemsChanged();
                else _characterState.RaiseShopItemsChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing StoreItemList packet.");
            }
            return Task.CompletedTask;
        }

        [PacketHandler(0x86, PacketRouter.NoSubCode)] // ItemCraftingResult
        public Task HandleItemCraftingResultAsync(Memory<byte> packet)
        {
            try
            {
                var res = new ItemCraftingResult(packet);
                _logger.LogInformation("ItemCraftingResult: Result={Result}", res.Result);

                MuGame.ScheduleOnMainThread(() =>
                {
                    ChaosMixControl.Instance?.NotifyCraftingResult(res.Result);

                    var scene = MuGame.Instance?.ActiveScene as GameScene;
                    string msg = res.Result switch
                    {
                        ItemCraftingResult.CraftingResult.Success => "Mix succeeded.",
                        ItemCraftingResult.CraftingResult.Failed => "Mix failed.",
                        ItemCraftingResult.CraftingResult.NotEnoughMoney => "Not enough Zen.",
                        ItemCraftingResult.CraftingResult.TooManyItems => "Too many items in mix box.",
                        ItemCraftingResult.CraftingResult.CharacterLevelTooLow => "Character level too low.",
                        ItemCraftingResult.CraftingResult.LackingMixItems => "Missing required items.",
                        ItemCraftingResult.CraftingResult.IncorrectMixItems => "Incorrect mix items.",
                        ItemCraftingResult.CraftingResult.InvalidItemLevel => "Invalid item level.",
                        ItemCraftingResult.CraftingResult.CharacterClassTooLow => "Character class too low.",
                        _ => $"Mix result: {res.Result}"
                    };

                    scene?.ChatLog?.AddMessage("System", msg, Client.Main.Models.MessageType.System);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ItemCraftingResult packet.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle 0x32 (Buy) results. For this family, some packets have no subcode (ItemBought),
        /// and failure (NpcItemBuyFailed) is a short 4-byte packet which some servers send with subcode.
        /// We treat all 0x32 as 'no subcode' in the router and differentiate by length here.
        /// </summary>
        [PacketHandler(0x32, PacketRouter.NoSubCode)]
        public Task HandleNpcBuyResultsAsync(Memory<byte> packet)
        {
            try
            {
                // Failure case: C1 04 32 FF (len = 4)
                if (packet.Length == NpcItemBuyFailed.Length)
                {
                    _logger.LogWarning("NPC item buy failed. Packet len={Len}", packet.Length);
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        string reason = ChatMessageHandler.TryGetRecentBlueSystemMessage(2000);
                        if (string.IsNullOrWhiteSpace(reason))
                        {
                            reason = "Purchase failed. Please check Zen or space.";
                        }
                        RequestDialog.ShowInfo(reason);
                    });
                    return Task.CompletedTask;
                }

                // Success case: ItemBought (C1 0x32, no subcode). Contains inventory slot + item data
                var bought = new ItemBought(packet);
                byte slot = bought.InventorySlot;
                var data = bought.ItemData.ToArray();

                _characterState.AddOrUpdateInventoryItem(slot, data);
                _logger.LogInformation("Item bought to inventory slot {Slot}: {Name}", slot, ItemDatabase.GetItemName(data) ?? "Item");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling 0x32 buy result packet.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles result of sell item to NPC (0x33). Updates money is handled elsewhere; here we cleanup inventory.
        /// </summary>
        [PacketHandler(0x33, PacketRouter.NoSubCode)]
        public Task HandleNpcItemSellResultAsync(Memory<byte> packet)
        {
            try
            {
                var res = new NpcItemSellResult(packet);
                _logger.LogInformation("NpcItemSellResult: Success={Success}, Money={Money}", res.Success, res.Money);
                if (res.Success)
                {
                    if (_characterState.TryConsumePendingSellSlot(out byte soldSlot))
                    {
                        _characterState.RemoveInventoryItem(soldSlot);
                    }
                    else
                    {
                        // No pending slot tracked; trigger a full refresh to stay in sync
                        _characterState.RaiseInventoryChanged();
                    }
                }
                else
                {
                    // Sell failed; just raise refresh to restore UI state, if needed
                    _characterState.RaiseInventoryChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling NpcItemSellResult packet.");
            }
            return Task.CompletedTask;
        }
    }
}
