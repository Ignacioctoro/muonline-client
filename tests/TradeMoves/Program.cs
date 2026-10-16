// Protocol regression: trade slots overlap equipment numbers, but not storage identity.
if (args.Length > 0) Client.Main.Constants.DataPath = args[0];
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
var state = new Client.Main.Core.Client.CharacterState(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
state.StartTrade(1, "Partner", 400);
var handler = new Client.Main.Networking.PacketHandling.Handlers.InventoryHandler(
    Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance, state, null!, default);
var inv = MUnique.OpenMU.Network.Packets.ItemStorageKind.Inventory;
var tr = MUnique.OpenMU.Network.Packets.ItemStorageKind.Trade;
byte[] wings = new byte[12]; wings[0] = 3; wings[9] = 0xC0;
byte[] bless = new byte[12]; bless[0] = 13; bless[9] = 0xE0;
byte[] pot = new byte[12]; pot[0] = 0; pot[9] = 0xE0;
state.AddOrUpdateInventoryItem(7, wings);
state.AddOrUpdateInventoryItem(40, bless);
byte[] Reply(MUnique.OpenMU.Network.Packets.ItemStorageKind storage, byte slot, byte[] raw)
{
    var packet = new byte[raw.Length + 5]; packet[0] = 0xC3; packet[1] = (byte)packet.Length;
    packet[2] = 0x24; packet[3] = (byte)storage; packet[4] = slot; raw.CopyTo(packet, 5); return packet;
}
Check(state.TryBeginTradeMove(inv, 40, tr, 7), "Begin inventory to trade");
Check(!state.TryBeginTradeMove(inv, 41, tr, 8), "Prevent overlapping trade moves");
await handler.HandleItemMovedOrFailedAsync(Reply(tr, 7, bless));
Check(state.TryGetInventoryItem(7, out var keptWings) && keptWings.SequenceEqual(wings), "Trade slot 7 must not overwrite equipped wings");
Check(!state.TryGetInventoryItem(40, out _) && state.GetMyTradeItems()[7].SequenceEqual(bless), "Confirm source removal and trade item once");
Check(state.TryBeginTradeMove(tr, 7, tr, 15), "Begin internal move");
await handler.HandleItemMovedOrFailedAsync(Reply(tr, 15, bless));
Check(state.GetMyTradeItems().Count == 1 && state.GetMyTradeItems().ContainsKey(15), "Internal move updates authoritative slot without duplicates");
Check(state.TryBeginTradeMove(tr, 15, inv, 52), "Begin return to inventory");
await handler.HandleItemMovedOrFailedAsync(Reply(inv, 52, bless));
Check(state.GetMyTradeItems().Count == 0 && state.TryGetInventoryItem(52, out var returned) && returned.SequenceEqual(bless), "Return removes trade source");
state.AddOrUpdateInventoryItem(41, pot);
Check(state.TryBeginTradeMove(inv, 41, tr, 7), "Begin rejected deposit");
await handler.HandleItemMovedOrFailedAsync(new byte[] { 0xC3, 5, 0x24, 0xFF, 0 });
Check(state.TryGetInventoryItem(41, out var keptPot) && keptPot.SequenceEqual(pot) && state.GetMyTradeItems().Count == 0 && !state.HasPendingTradeMove, "Failure retains original and unlocks");
Check(state.TryBeginTradeMove(inv, 41, tr, 7), "Begin potion deposit");
await handler.HandleItemMovedOrFailedAsync(Reply(tr, 7, pot));
Check(state.TryGetInventoryItem(7, out keptWings) && keptWings.SequenceEqual(wings), "Potion cannot replace equipped wings");
Check(state.TryBeginTradeMove(tr, 7, inv, 41), "Begin rejected withdrawal");
await handler.HandleItemMovedOrFailedAsync(new byte[] { 0xC3, 5, 0x24, 0xFF, 0 });
Check(state.GetMyTradeItems().ContainsKey(7) && !state.TryGetInventoryItem(41, out _), "Rejected withdrawal retains trade source");
Console.WriteLine("PASS: real 0x24 packets for deposit, internal move, withdrawal, failure recovery, pending guard and wings-slot isolation.");

Check(state.TryBeginTradeMove(tr, 7, tr, 8), "Begin rejected internal move");
await handler.HandleItemMovedOrFailedAsync(new byte[] { 0xC3, 5, 0x24, 0xFF, 0 });
Check(state.GetMyTradeItems().Count == 1 && state.GetMyTradeItems().ContainsKey(7), "Rejected internal move keeps source slot");
Check(state.TryBeginTradeMove(tr, 7, tr, 8), "Begin before cancellation");
state.EndTrade(MUnique.OpenMU.Network.Packets.ServerToClient.TradeFinished.TradeResult.Cancelled);
await handler.HandleItemMovedOrFailedAsync(Reply(tr, 8, pot));
Check(!state.HasPendingTradeMove && state.GetMyTradeItems().Count == 0, "Late trade reply cannot resurrect cancelled offer");
Check(state.TryGetInventoryItem(7, out keptWings) && keptWings.SequenceEqual(wings), "Late reply does not change equipment");
Console.WriteLine("PASS: rejected internal move, cancellation and late reply isolation.");

state.StartTrade(1, "Partner", 400);
state.AddOrUpdateMyTradeItem(7, pot);
var window = Client.Main.Controls.UI.Game.Trade.TradeControl.Instance;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
window.GetType().GetField("_characterState", flags)!.SetValue(window, state);
void Call(string method, params object[] values) => window.GetType().GetMethod(method, flags)!.Invoke(window, values);
Call("RefreshTradeItems");
var items = (System.Collections.IList)window.GetType().GetField("_myItems", flags)!.GetValue(window)!;
var picked = items[0]!;
Call("BeginDrag", picked);
state.AddOrUpdatePartnerTradeItem(0, bless);
Call("RefreshTradeItems");
Check(items.Count == 1 && ReferenceEquals(items[0], picked), "Partner update must retain dragged object identity");
var grid = (Array)window.GetType().GetField("_myGrid", flags)!.GetValue(window)!;
Check(grid.GetValue(7, 0) == null, "Partner update must not resurrect picked item in grid");
Console.WriteLine("PASS: partner updates preserve the picked item without duplicating its grid cells.");
