using System.Collections.Generic;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Items.Tooltips
{
    public static partial class ClassicItemTooltipBuilder
    {
        private static readonly Color SummaryTitle = new(255, 0, 255);
        private static readonly Color SummaryOption = new(128, 179, 255);

        public static List<(string text, Color color)> BuildEquipmentSetSummary(
            IReadOnlyDictionary<byte, InventoryItem> equipment)
        {
            var lines = new List<(string text, Color color)> { ("Set Item Option", SummaryTitle) };
            var seen = new HashSet<int>();
            foreach (var pair in equipment)
            {
                var set = EquipmentBonusCalculator.FindAncientSet(pair.Value);
                if (pair.Key >= 12 || set == null || !seen.Add(set.Id))
                    continue;
                int count = EquipmentBonusCalculator.ActiveAncientOptionCount(set,
                    EquipmentBonusCalculator.CountAncientPieces(equipment, set));
                if (count == 0)
                    continue;
                lines.Add((set.Name, Color.Gold));
                for (int i = 0; i < count; i++)
                    lines.Add((set.Options[i], SummaryOption));
            }
            if (lines.Count == 1)
                lines.Add(("No active set options", Color.Gray));
            return lines;
        }

        public static List<(string text, Color color)> BuildEquipmentSocketSummary(
            IReadOnlyDictionary<byte, InventoryItem> equipment)
        {
            var lines = new List<(string text, Color color)> { ("Socket Package Option", SummaryTitle) };
            if (!ItemTooltipDataRepository.IsLoaded)
            {
                lines.Add(("Socket data not loaded", Color.Gray));
                return lines;
            }
            var counts = EquipmentBonusCalculator.CountSocketElements(equipment,
                id => ItemTooltipDataRepository.GetSocketOption(0, id));
            for (int id = 0; id < 50; id++)
            {
                // Table 1 is an individual item's bonus; table 2 is the package.
                var package = ItemTooltipDataRepository.GetSocketOption(2, id);
                if (!EquipmentBonusCalculator.IsSocketPackageActive(package, counts))
                    continue;
                string name = ItemTooltipDataRepository.GetEnglishSocketOptionName(2, id);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = id switch
                    {
                        0 => "Double damage rate increase",
                        1 => "Opponent defensibility ignorance attack",
                        _ => package.Name
                    };
                }
                if (string.IsNullOrWhiteSpace(name) || package.Values.Length == 0)
                    continue;
                string suffix = package.BonusType == 2 ? "%" : string.Empty;
                lines.Add(($"{name} +{package.Values[0]}{suffix}", SummaryOption));
            }
            if (lines.Count == 1)
                lines.Add(("No active socket package options", Color.Gray));
            return lines;
        }

        private static void AppendOpenMuAncientOptions(List<(string text, Color color)> lines,
            InventoryItem item, EquipmentBonusCalculator.AncientSet set)
        {
            var inventory = InventoryControl.Instance;
            int pieces = EquipmentBonusCalculator.CountAncientPieces(inventory.EquippedItems, set);
            int active = inventory.IsEquippedItem(item) && item.Durability > 0
                ? EquipmentBonusCalculator.ActiveAncientOptionCount(set, pieces) : 0;
            lines.Add(("Ancient Options", ResolveMuColor(3)));
            AppendSectionSeparator(lines);
            for (int i = 0; i < set.Options.Length; i++)
                lines.Add((set.Options[i], i < active ? ResolveMuColor(1) : ResolveMuColor(10)));
        }
    }
}
