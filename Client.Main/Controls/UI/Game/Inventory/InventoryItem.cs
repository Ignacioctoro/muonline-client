using System;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

public class InventoryItem
{
    public ItemDefinition Definition { get; }
    public Point GridPosition { get; set; }
    public int Durability { get; set; }
    public int Level { get; set; }

    //  ▼▼ NOWE ▼▼
    public byte[] RawData { get; }
    public ItemDatabase.ItemDetails Details { get; }

    public InventoryItem(ItemDefinition def, Point pos,
                         int durability = 255, int level = 0)
        : this(def, pos, Array.Empty<byte>(), durability, level) { }

    public InventoryItem(ItemDefinition def, Point pos,
                         byte[] rawData,
                         int durability = 255, int level = 0)
    {
        Definition = def;
        GridPosition = pos;
        Durability = durability;
        Level = level;

        RawData = rawData ?? Array.Empty<byte>();
        Details = ItemDatabase.ParseItemDetails(RawData);
    }

    // Lahap packs encode 10/20/30 pieces in level, not durability.
    public int DisplayQuantity
    {
        get
        {
            if (Definition.IsPackedJewel())
            {
                int level = RawData.Length > 0 ? Details.Level : Level;
                return level is >= 0 and <= 2 ? (level + 1) * 10 : 0;
            }

            return Definition.BaseDurability == 0 && Definition.MagicDurability == 0 && Durability > 1
                ? Durability : 0;
        }
    }

    public bool ShowLevelBadge => !Definition.IsPackedJewel() && Details.Level > 0;

    public Rectangle GetBounds() => new(GridPosition.X, GridPosition.Y,
                                        Definition.Width, Definition.Height);
}
