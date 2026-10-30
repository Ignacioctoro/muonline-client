using Client.Data.BMD.Tooltip;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Items.Tooltips;
using Microsoft.Xna.Framework;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static InventoryItem Item(int group, int number, byte level = 0, byte discriminator = 0,
    byte durability = 100, byte[] sockets = null)
{
    // Extended OpenMU item: header, optional Ancient byte, optional socket block.
    var raw = new List<byte> { (byte)(group << 4 | number >> 8), (byte)number,
        level, durability, (byte)((discriminator > 0 ? 0x10 : 0) | (sockets != null ? 0x80 : 0)) };
    if (discriminator > 0) raw.Add(discriminator);
    if (sockets != null)
    {
        raw.Add((byte)sockets.Length);
        raw.AddRange(sockets);
    }
    return new InventoryItem(new ItemDefinition(number, "Fixture", 1, 1, "")
        { Group = group, BaseDurability = 1 }, Point.Zero, raw.ToArray(), durability, level);
}

foreach (int id in new[] { 30, 31, 136, 137, 138, 139, 140, 141, 142, 143 })
{
    for (byte level = 0; level < 3; level++)
    {
        var pack = Item(12, id, level, durability: 1);
        Check(pack.DisplayQuantity == (level + 1) * 10, $"Pack 12:{id} quantity");
        Check(!pack.ShowLevelBadge, "Pack level must not look like an upgrade");
        var tooltip = ClassicItemTooltipBuilder.Build(pack);
        Check(tooltip.Any(l => l.text == $"Quantity: {(level + 1) * 10}"), "Pack tooltip quantity");
    }
}
Check(Item(12, 30, 3).DisplayQuantity == 0, "Do not invent quantities for unsupported pack levels");
var stack = Item(14, 13, durability: 160);
stack.Definition.BaseDurability = 0;
Check(stack.DisplayQuantity == 160, "Ordinary stack quantity is preserved");
Check(Item(12, 15, durability: 1).DisplayQuantity == 0, "Single Chaos is not a pack");
Check(Item(0, 26, 9).ShowLevelBadge, "Equipment still shows its upgrade level");

var hyon = new Dictionary<byte, InventoryItem>
{
    [0] = Item(0, 14, discriminator: 1),
    [2] = Item(7, 1, discriminator: 1)
};
var definition = EquipmentBonusCalculator.FindAncientSet(hyon[0]);
Check(definition?.Name == "Hyon", "Resolve Hyon from server membership, not modern BMD IDs");
var summary = ClassicItemTooltipBuilder.BuildEquipmentSetSummary(hyon);
Check(summary.Any(l => l.text == "Increase Defense +25"), "Two Hyon pieces grant defense");
Check(!summary.Any(l => l.text.Contains("Double Damage")), "Two Hyon pieces do not grant double damage");
hyon[6] = Item(11, 1, discriminator: 1);
summary = ClassicItemTooltipBuilder.BuildEquipmentSetSummary(hyon);
Check(summary.Any(l => l.text == "Increase Double Damage Rate +10%"), "Three Hyon pieces grant double damage");
hyon[5] = Item(10, 1, discriminator: 1);
Check(EquipmentBonusCalculator.ActiveAncientOptionCount(definition,
    EquipmentBonusCalculator.CountAncientPieces(hyon, definition)) == 7, "Complete Hyon grants all seven options");
hyon[5].Durability = 0;
Check(EquipmentBonusCalculator.CountAncientPieces(hyon, definition) == 3, "Broken pieces do not count");
hyon[1] = Item(0, 14, discriminator: 1);
Check(EquipmentBonusCalculator.CountAncientPieces(hyon, definition) == 3, "Duplicate set items do not count twice");
hyon[12] = Item(10, 1, discriminator: 1);
Check(EquipmentBonusCalculator.CountAncientPieces(hyon, definition) == 3, "Backpack pieces do not count");
Check(ClassicItemTooltipBuilder.BuildEquipmentSetSummary(new Dictionary<byte, InventoryItem>())
    .Any(l => l.text == "No active set options"), "Empty equipment has no set bonuses");

// Each fixture option maps to one element; higher sphere ranks must not add extra seeds.
SocketItemBMD Seed(int id) => id < 6 ? new SocketItemBMD { ElementType = id + 1 } : null;
var equipment = new Dictionary<byte, InventoryItem>
{
    [0] = Item(0, 26, sockets: new byte[] { 0, 51, 102, 0xFE, 0xFF }),
    [3] = Item(8, 45, sockets: new byte[] { 153, 204, 5 })
};
var firstPackage = new SocketItemBMD
{
    FireNeed = 1, WaterNeed = 1, IceNeed = 1, WindNeed = 1, LightningNeed = 1, EarthNeed = 1
};
var secondPackage = new SocketItemBMD
{
    FireNeed = 1, WaterNeed = 3, IceNeed = 1, WindNeed = 3, LightningNeed = 1, EarthNeed = 2
};
var counts = EquipmentBonusCalculator.CountSocketElements(equipment, Seed);
Check(counts.SequenceEqual(new[] { 1, 1, 1, 1, 1, 1 }), "Count elements across equipment and sphere ranks");
Check(EquipmentBonusCalculator.IsSocketPackageActive(firstPackage, counts), "First package is active");
Check(!EquipmentBonusCalculator.IsSocketPackageActive(secondPackage, counts), "Second package needs more seeds");
equipment[4] = Item(9, 45, sockets: new byte[] { 1, 1, 3, 3, 5 });
counts = EquipmentBonusCalculator.CountSocketElements(equipment, Seed);
Check(EquipmentBonusCalculator.IsSocketPackageActive(firstPackage, counts) &&
    EquipmentBonusCalculator.IsSocketPackageActive(secondPackage, counts), "Both packages can be active");
equipment[3].Durability = 0;
counts = EquipmentBonusCalculator.CountSocketElements(equipment, Seed);
Check(!EquipmentBonusCalculator.IsSocketPackageActive(firstPackage, counts), "Broken equipment removes its seeds");
Check(!EquipmentBonusCalculator.IsSocketPackageActive(new SocketItemBMD(), counts), "Empty BMD records are not bonuses");
Console.WriteLine("Inventory bonus checks passed.");
