using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Core.Items.Tooltips;
using Client.Data.BMD.Tooltip;

namespace Client.Main.Core.Utilities
{
    public static class TooltipDataDebug
    {
        private static bool _executed;

        public static async Task RunOnceAsync()
        {
            if (_executed)
            {
                return;
            }

            _executed = true;

            try
            {
                await TestItemTooltipAsync();
                await TestItemTooltipTextAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "================================================");

                Console.WriteLine(
                    "[Tooltip DEBUG ERROR]");

                Console.WriteLine(ex);

                Console.WriteLine(
                    "================================================");

                Console.WriteLine();
            }
        }

        private static async Task TestItemTooltipAsync()
        {
            string path = Path.Combine(
                Constants.DataPath,
                "Local",
                "itemtooltip.bmd");

            Console.WriteLine();
            Console.WriteLine(
                "================================================");

            Console.WriteLine(
                "[ItemTooltip DEBUG]");

            Console.WriteLine(
                $"File: {path}");

            Console.WriteLine(
                "================================================");

            var reader =
                new ItemTooltipBMDReader();

            var entries =
                await reader
                    .Load(path)
                    .ConfigureAwait(false);

            Console.WriteLine(
                $"Physical records: {entries.Count}");

            int activeEntries =
                entries.Count(IsActive);

            Console.WriteLine(
                $"Active records: {activeEntries}");

            var duplicateKeys =
                entries
                    .Where(IsActive)
                    .GroupBy(
                        x => (x.Category, x.Index))
                    .Where(x => x.Count() > 1)
                    .ToList();

            Console.WriteLine(
                $"Duplicate active keys: {duplicateKeys.Count}");

            Console.WriteLine();

            Console.WriteLine(
                "First active decoded records:");

            foreach (var entry in entries
                .Where(IsActive)
                .Take(20))
            {
                Console.WriteLine(
                    $"  {entry.Category}:{entry.Index,-3} " +
                    $"'{entry.Name}'");
            }

            DumpEntry(entries, 0, 0);
            DumpEntry(entries, 0, 1);
            DumpEntry(entries, 0, 2);

            DumpEntry(entries, 6, 0);
            DumpEntry(entries, 7, 0);
            DumpEntry(entries, 8, 0);

            DumpEntry(entries, 12, 0);
            DumpEntry(entries, 12, 1);
            DumpEntry(entries, 12, 2);

            Console.WriteLine(
                "================================================");

            Console.WriteLine();
        }

        private static async Task TestItemTooltipTextAsync()
        {
            string path = Path.Combine(
                Constants.DataPath,
                "Local",
                "itemtooltiptext.bmd");

            Console.WriteLine();
            Console.WriteLine(
                "================================================");

            Console.WriteLine(
                "[ItemTooltipText DEBUG]");

            Console.WriteLine(
                $"File: {path}");

            Console.WriteLine(
                "================================================");

            var reader =
                new ItemTooltipTextBMDReader();

            var entries =
                await reader
                    .Load(path)
                    .ConfigureAwait(false);

            Console.WriteLine(
                $"Loaded records: {entries.Count}");

            Console.WriteLine();

            Console.WriteLine(
                "First decoded tooltip texts:");

            foreach (var entry in entries.Take(30))
            {
                Console.WriteLine(
                    $"  ID={entry.Id,-5} " +
                    $"Type={entry.Type,-4} " +
                    $"Text='{entry.Text}'");
            }

            Console.WriteLine();

            DumpText(entries, 0);
            DumpText(entries, 6);
            DumpText(entries, 8);
            DumpText(entries, 11);
            DumpText(entries, 12);
            DumpText(entries, 13);
            DumpText(entries, 14);

            Console.WriteLine(
                "================================================");

            Console.WriteLine();
        }

        private static bool IsActive(
            ItemTooltipBMD entry)
        {
            return entry.Category != 0
                || entry.Index != 0
                || !string.IsNullOrEmpty(entry.Name);
        }

        private static void DumpEntry(
            IReadOnlyList<ItemTooltipBMD> entries,
            byte category,
            ushort index)
        {
            var entry =
                entries.FirstOrDefault(
                    x =>
                        x.Category == category &&
                        x.Index == index &&
                        IsActive(x));

            Console.WriteLine();

            Console.WriteLine(
                $"----- ItemTooltip {category}:{index} -----");

            if (entry == null)
            {
                Console.WriteLine("NOT FOUND");
                return;
            }

            Console.WriteLine(
                $"Name          : '{entry.Name}'");

            Console.WriteLine(
                $"NameColor     : {entry.NameColor}");

            Console.WriteLine(
                $"NameBold      : {entry.NameBold}");

            Console.WriteLine(
                $"NoTaxGold     : {entry.NoTaxGold}");

            Console.WriteLine(
                $"OptionSpecial : {entry.OptionSpecial}");

            Console.WriteLine(
                $"RenderLevel   : {entry.RenderLevel}");

            Console.WriteLine(
                $"ReservedFlag  : {entry.ReservedRenderFlag}");

            Console.WriteLine(
                $"ItemLevel     : {entry.ItemLevel}");

            Console.WriteLine(
                "Tooltip lines:");

            for (int i = 0;
                i < entry.Lines.Length;
                i++)
            {
                var line = entry.Lines[i];

                Console.WriteLine(
                    $"  [{i + 1:00}] " +
                    $"TextID={line.TextId,-6} " +
                    $"Color={line.Color,-3} " +
                    $"Bold={line.Bold}");
            }
        }

        private static void DumpText(
            IReadOnlyList<ItemTooltipTextBMD> entries,
            ushort id)
        {
            var entry =
                entries.FirstOrDefault(
                    x => x.Id == id);

            if (entry == null)
            {
                Console.WriteLine(
                    $"Text ID {id}: NOT FOUND");

                return;
            }

            Console.WriteLine(
                $"Text ID {id}: " +
                $"Type={entry.Type}, " +
                $"'{entry.Text}'");
        }
    }
}