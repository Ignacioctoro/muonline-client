using Client.Main.Core.Items;
using MUnique.OpenMU.Network.Packets.ClientToServer;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

Check(LahapJewels.Types.Count == 10, "All ten S6 jewel types are present");
Check(LahapJewels.Types[(int)LahapJewelMixRequest.ItemType.JewelOfChaos].PackNumber == 141, "Chaos mix ID matches the protocol");
Check(LahapJewels.CombineFee(10) == 500_000 && LahapJewels.CombineFee(20) == 1_000_000 &&
    LahapJewels.CombineFee(30) == 1_500_000, "Combination fees match OpenMU");
Check(LahapJewels.DismantleFee == 1_000_000, "Dismantle fee matches OpenMU");
bool rejected = false;
try { LahapJewels.CombineFee(15); } catch (ArgumentOutOfRangeException) { rejected = true; }
Check(rejected, "Unsupported quantities cannot be sent");

var bless = LahapJewels.Types[0];
var items = new List<LahapJewels.InventoryEntry>
{
    new(12, 12, 30, 2, 1, 1), // Bundle of 30 Bless.
    new(13, 0, 26, 0, 2, 4), // Eight occupied cells, not one.
    new(15, 14, 13, 0, 1, 1),
    new(23, 14, 13, 0, 1, 1)
};
Check(LahapJewels.GetPackType(items[0]) == bless, "Bundle resolves to the correct mix type");
Check(LahapJewels.CountSingles(items, bless) == 2, "Bundles are not counted as loose jewels");
Check(LahapJewels.CountPacks(items, bless, 2) == 1 && LahapJewels.CountPacks(items, bless, 0) == 0,
    "Bundle counts distinguish quantities");
Check(LahapJewels.FreeCells(items, 0) == 53, "Space check uses multi-cell footprints");
Check(LahapJewels.FreeCells(items, 1) == 85, "Unlocked extension contributes 32 cells");
items.Add(new(76, 0, 26, 0, 2, 4));
Check(LahapJewels.FreeCells(items, 1) == 77, "Extension item footprints are counted");
Check(LahapJewels.GetPackType(new(12, 12, 30, 3, 1, 1)) == null, "Unsupported bundle levels are excluded");
Check(LahapJewels.GetPackType(new(12, 14, 30, 0, 1, 1)) == null, "Unrelated item IDs are excluded");

var packetData = new byte[LahapJewelMixRequest.Length];
var packet = new LahapJewelMixRequest(packetData);
packet.Operation = LahapJewelMixRequest.MixType.Unmix;
packet.Item = LahapJewelMixRequest.ItemType.JewelOfChaos;
packet.MixingStackSize = LahapJewelMixRequest.StackSize.Thirty;
packet.UnmixingSourceSlot = 76;
Check(packetData.SequenceEqual(new byte[] { 0xC1, 7, 0xBC, 1, 7, 2, 76 }),
    "Lahap request keeps the absolute inventory slot, including extended inventory");
Console.WriteLine("Lahap checks passed.");
