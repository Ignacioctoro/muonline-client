using System.Collections.Generic;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Items.Tooltips
{
    public static class SpiritItemInfo
    {
        public static bool IsSpirit(ItemDefinition definition) => definition?.Group == 13 && definition.Id == 31;
        public static string Name(int level) => level switch
        {
            0 => "Spirit of Dark Horse",
            1 => "Spirit of Dark Raven",
            _ => $"Spirit (unsupported level +{level})"
        };
        public static List<(string text, Color color)> BuildTooltip(InventoryItem item)
        {
            int level = item.Details.Level;
            return new()
            {
                (Name(level), new Color(255, 204, 25)),
                (level is 0 or 1 ? "Resurrection material for the Pet Trainer." : "This Spirit cannot be used for resurrection.",
                    level is 0 or 1 ? Color.White : Color.OrangeRed),
                ("Use Resurrect Spirit with the required jewels.", Color.LightGray)
            };
        }
    }
}
