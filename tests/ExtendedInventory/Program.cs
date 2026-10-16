using Client.Main;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Client;
using Client.Main.Networking.PacketHandling.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xna.Framework;
using MUnique.OpenMU.Network.Packets;
using System.Reflection;

if (args.Length > 0) Constants.DataPath = args[0];
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
for (int unlocked = 0; unlocked <= 4; unlocked++)
for (int page = 0; page < 2; page++)
for (int block = 0; block < 2; block++)
for (int y = 0; y < 4; y++)
for (int x = 0; x < 8; x++)
{
    var grid = ExtendedInventoryLayout.Grid(block);
    int expected = page * 2 + block < unlocked ? 76 + (page * 2 + block) * 32 + y * 8 + x : -1;
    Check(ExtendedInventoryLayout.SlotAt(new Point(grid.X + x * 32 + 16, grid.Y + y * 32 + 16), page, unlocked) == expected, "Unlocked slot mapping");
}
Check(ExtendedInventoryLayout.Fits(76, 2, 3, 1), "Multicell item fits");
Check(!ExtendedInventoryLayout.Fits(83, 2, 1, 4), "Cannot cross right edge");
Check(!ExtendedInventoryLayout.Fits(100, 1, 2, 4), "Cannot cross extension boundary");
Check(!ExtendedInventoryLayout.Fits(108, 1, 1, 1), "Cannot use locked extension");
Check(!ExtendedInventoryLayout.Fits(75, 1, 1, 4) && !ExtendedInventoryLayout.Fits(204, 1, 1, 4), "Equipment/backpack/store excluded");
Check(ExtendedInventoryLayout.SlotAt(Point.Zero, 0, 4) == -1, "Header excluded");
var window = ExtendedInventoryControl.Instance;
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
window.GetType().GetField("_unlocked", flags)!.SetValue(window, 4);
var items = (Dictionary<byte, InventoryItem>)window.GetType().GetField("_items", flags)!.GetValue(window)!;
var large = new InventoryItem(new ItemDefinition(0, "Large", 2, 3), Point.Zero, new byte[12]);
items[76] = large;
bool CanPlace(int slot, byte from) => (bool)window.GetType().GetMethod("CanPlace", flags)!.Invoke(window, new object[] {slot, large, from})!;
Check(CanPlace(76, 76), "Own source does not collide");
Check(!CanPlace(77, 12), "Occupied cells reject external item");
Check(CanPlace(108, 76), "Different extension has independent occupancy");
Console.WriteLine("PASS: 640 slot/lock cases, multicell boundaries, store isolation and source-aware collision checks.");
var state = new CharacterState(NullLoggerFactory.Instance);
var handler = new InventoryHandler(NullLoggerFactory.Instance, state, null!, default);
byte[] item = new byte[12]; item[0] = 13; item[9] = 0xE0;
byte[] Reply(byte slot) { var p = new byte[17]; p[0] = 0xC3; p[1] = 17; p[2] = 0x24; p[3] = (byte)ItemStorageKind.Inventory; p[4] = slot; item.CopyTo(p, 5); return p; }
state.AddOrUpdateInventoryItem(12, item);
foreach (var (from, to) in new (byte, byte)[] { (12, 76), (76, 107), (107, 108), (108, 203), (203, 20) })
{
    state.StashPendingInventoryMove(from, to);
    Check(state.IsExtendedInventoryMovePending, "Extension request identified");
    Check(state.TryGetInventoryItem(from, out _), "Original retained until acknowledgement");
    await handler.HandleItemMovedOrFailedAsync(Reply(to));
    Check(!state.TryGetInventoryItem(from, out _) && state.TryGetInventoryItem(to, out var confirmed) && confirmed.SequenceEqual(item), "Confirmed move commits exactly once");
    Check(!state.IsExtendedInventoryMovePending, "Confirmation unlocks input");
}
state.StashPendingInventoryMove(20, 76);
await handler.HandleItemMovedOrFailedAsync(new byte[] { 0xC3, 5, 0x24, 0xFF, 0 });
Check(state.TryGetInventoryItem(20, out _) && !state.TryGetInventoryItem(76, out _) && !state.IsExtendedInventoryMovePending, "Rejected move retains original and unlocks");
Console.WriteLine("PASS: real inventory replies across all extension ranges, return to backpack and failed move recovery.");
