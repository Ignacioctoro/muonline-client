using System;
using System.Collections.Generic;
using Client.Main.Core.Utilities;

namespace Client.Main.Core.Items
{
    /// <summary>OpenMU S6 JewelMix IDs, fees and inventory addressing.</summary>
    public static class LahapJewels
    {
        public sealed record Jewel(byte MixId, byte Group, short Number, short PackNumber, string Name);

        public static readonly IReadOnlyList<Jewel> Types = Array.AsReadOnly(new[]
        {
            new Jewel(0, 14, 13, 30, "Jewel of Bless"),
            new Jewel(1, 14, 14, 31, "Jewel of Soul"),
            new Jewel(2, 14, 16, 136, "Jewel of Life"),
            new Jewel(3, 14, 22, 137, "Jewel of Creation"),
            new Jewel(4, 14, 31, 138, "Jewel of Guardian"),
            new Jewel(5, 14, 41, 139, "Gemstone"),
            new Jewel(6, 14, 42, 140, "Jewel of Harmony"),
            new Jewel(7, 12, 15, 141, "Jewel of Chaos"),
            new Jewel(8, 14, 43, 142, "Lower Refining Stone"),
            new Jewel(9, 14, 44, 143, "Higher Refining Stone")
        });

        public const uint DismantleFee = 1_000_000;
        public static uint CombineFee(int quantity) => quantity switch
        {
            10 => 500_000,
            20 => 1_000_000,
            30 => 1_500_000,
            _ => throw new ArgumentOutOfRangeException(nameof(quantity))
        };

        public sealed record InventoryEntry(byte Slot, byte Group, short Number, int Level, int Width, int Height);

        public static List<InventoryEntry> ReadInventory(IReadOnlyDictionary<byte, byte[]> items, int extensions)
        {
            int end = 76 + Math.Clamp(extensions, 0, 4) * 32;
            var result = new List<InventoryEntry>();
            foreach (var pair in items)
            {
                if (pair.Key < 12 || pair.Key >= end ||
                    !ItemDatabase.TryGetItemGroupAndNumber(pair.Value, out byte group, out short number))
                    continue;
                var definition = ItemDatabase.GetItemDefinition(group, number);
                result.Add(new InventoryEntry(pair.Key, group, number,
                    ItemDatabase.ParseItemDetails(pair.Value).Level, definition?.Width ?? 0, definition?.Height ?? 0));
            }
            result.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            return result;
        }

        public static Jewel GetPackType(InventoryEntry entry)
        {
            if (entry.Group != 12 || entry.Level is < 0 or > 2) return null;
            for (int i = 0; i < Types.Count; i++)
                if (Types[i].PackNumber == entry.Number) return Types[i];
            return null;
        }

        public static int CountSingles(IReadOnlyList<InventoryEntry> items, Jewel jewel)
        {
            int count = 0;
            // OpenMU ItemStackAction takes individual item instances, not durability units.
            for (int i = 0; i < items.Count; i++)
                if (items[i].Group == jewel.Group && items[i].Number == jewel.Number) count++;
            return count;
        }

        public static int CountPacks(IReadOnlyList<InventoryEntry> items, Jewel jewel, int level)
        {
            int count = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].Group == 12 && items[i].Number == jewel.PackNumber && items[i].Level == level) count++;
            return count;
        }

        public static int FreeCells(IReadOnlyList<InventoryEntry> items, int extensions)
        {
            int total = 64 + Math.Clamp(extensions, 0, 4) * 32;
            var occupied = new bool[total];
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Width <= 0 || item.Height <= 0) return 0; // Unknown footprints cannot be estimated.
                int index = item.Slot - 12;
                int regionStart = index < 64 ? 0 : 64 + (index - 64) / 32 * 32;
                int rows = index < 64 ? 8 : 4;
                int x = (index - regionStart) % 8;
                int y = (index - regionStart) / 8;
                for (int dy = 0; dy < item.Height && y + dy < rows; dy++)
                for (int dx = 0; dx < item.Width && x + dx < 8; dx++)
                {
                    int cell = regionStart + (y + dy) * 8 + x + dx;
                    if (cell >= 0 && cell < total) occupied[cell] = true;
                }
            }
            int free = 0;
            for (int i = 0; i < occupied.Length; i++)
                if (!occupied[i]) free++;
            return free;
        }
    }
}
