using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Data.BMD.Tooltip;
using Client.Main.Controls.UI.Game.Inventory;

namespace Client.Main.Core.Items.Tooltips
{
    /// <summary>
    /// OpenMU Season 6 equipment rules. Modern asset set IDs are not S6 set IDs.
    /// The embedded catalog mirrors OpenMU's AncientSets initializer; it contains
    /// configuration only, with no proprietary model or texture data.
    /// </summary>
    public static class EquipmentBonusCalculator
    {
        public sealed class SetMember
        {
            public int Group { get; set; }
            public int Number { get; set; }
            public int Discriminator { get; set; }
        }

        public sealed class AncientSet
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public SetMember[] Items { get; set; } = Array.Empty<SetMember>();
            public string[] Options { get; set; } = Array.Empty<string>();
        }

        private static readonly Dictionary<(int group, int number, int discriminator), AncientSet> SetsByItem = LoadSets();

        private static Dictionary<(int, int, int), AncientSet> LoadSets()
        {
            using var stream = typeof(EquipmentBonusCalculator).Assembly.GetManifestResourceStream(
                "Client.Main.Data.openmu-season6-ancient-sets.json")
                ?? throw new InvalidOperationException("Missing OpenMU Season 6 ancient set catalog.");
            var sets = JsonSerializer.Deserialize<AncientSet[]>(stream)
                ?? throw new InvalidOperationException("Invalid OpenMU Season 6 ancient set catalog.");
            var result = new Dictionary<(int, int, int), AncientSet>();
            foreach (var set in sets)
            foreach (var member in set.Items)
                result.Add((member.Group, member.Number, member.Discriminator), set);
            return result;
        }

        public static AncientSet FindAncientSet(InventoryItem item)
        {
            if (item?.Definition == null || !item.Details.IsAncient)
                return null;
            SetsByItem.TryGetValue((item.Definition.Group, item.Definition.Id,
                item.Details.AncientDiscriminator), out var set);
            return set;
        }

        public static int CountAncientPieces(IReadOnlyDictionary<byte, InventoryItem> equipment, AncientSet set)
        {
            var distinct = new HashSet<(int, int)>();
            foreach (var pair in equipment)
            {
                var item = pair.Value;
                if (pair.Key >= 12 || item == null || item.Durability <= 0 || FindAncientSet(item)?.Id != set.Id)
                    continue;
                distinct.Add((item.Definition.Group, item.Definition.Id));
            }
            return distinct.Count;
        }

        public static int ActiveAncientOptionCount(AncientSet set, int distinctPieces)
        {
            // ItemPowerUpFactory.GetSetPowerUps: n-1 options until complete.
            if (distinctPieces < 2)
                return 0;
            return distinctPieces >= set.Items.Length ? set.Options.Length
                : Math.Min(distinctPieces - 1, set.Options.Length);
        }

        public static int[] CountSocketElements(IReadOnlyDictionary<byte, InventoryItem> equipment,
            Func<int, SocketItemBMD> resolveSeed)
        {
            var counts = new int[6];
            foreach (var pair in equipment)
            {
                var item = pair.Value;
                if (pair.Key >= 12 || item == null || item.Durability <= 0 || !item.Details.HasSockets)
                    continue;
                var sockets = item.Details.SocketOptions;
                if (sockets == null)
                    continue;
                int count = Math.Min(item.Details.SocketCount, sockets.Length);
                for (int i = 0; i < count; i++)
                {
                    // FE = empty, FF = absent; 250..253 have no valid S6 sphere level.
                    if (sockets[i] >= 250)
                        continue;
                    var seed = resolveSeed(sockets[i] % 50);
                    if (seed?.ElementType is >= 1 and <= 6)
                        counts[seed.ElementType - 1]++;
                }
            }
            return counts;
        }

        public static bool IsSocketPackageActive(SocketItemBMD package, ReadOnlySpan<int> counts)
        {
            if (package == null || counts.Length < 6)
                return false;
            // Empty physical records are not bonus definitions.
            int total = package.FireNeed + package.WaterNeed + package.IceNeed +
                package.WindNeed + package.LightningNeed + package.EarthNeed;
            return total > 0 && counts[0] >= package.FireNeed && counts[1] >= package.WaterNeed &&
                counts[2] >= package.IceNeed && counts[3] >= package.WindNeed &&
                counts[4] >= package.LightningNeed && counts[5] >= package.EarthNeed;
        }
    }
}
