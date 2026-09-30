using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Client.Data.BMD.Tooltip;

namespace Client.Main.Core.Items.Tooltips
{
    /// <summary>
    /// Central access point for the original MU item tooltip data.
    ///
    /// Loads:
    ///
    /// Data/Local/itemtooltip.bmd
    /// Data/Local/itemtooltiptext.bmd
    ///
    /// Data is loaded once and then cached in dictionaries.
    /// </summary>
    public static class ItemTooltipDataRepository
    {
        private static readonly object _syncRoot = new();

        private static Task? _loadTask;

        private static Dictionary<int, ItemTooltipBMD> _items =
            new();
        private static Dictionary<int, ExcellentOptionBMD>
            _excellentCommonOptions =
                new();
        private static Dictionary<int, ExcellentOptionBMD>
            _excellentWingOptions =
                new();

        private static Dictionary<ushort, ItemTooltipTextBMD> _texts =
            new();

        public static bool IsLoaded { get; private set; }

        public static int ItemCount =>
            _items.Count;

        public static int TextCount =>
            _texts.Count;

        /// <summary>
        /// Loads the tooltip BMD files once.
        ///
        /// Calling this multiple times is safe.
        /// </summary>
        public static Task LoadAsync()
        {
            lock (_syncRoot)
            {
                _loadTask ??=
                    LoadInternalAsync();

                return _loadTask;
            }
        }

        private static async Task LoadInternalAsync()
        {
            string itemTooltipPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemtooltip.bmd");

            string itemTooltipTextPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemtooltiptext.bmd");

            string excellentCommonPath =
            Path.Combine(
                Constants.DataPath,
                "Local",
                "excellentcommonoption.bmd");
            string excellentWingPath =
            Path.Combine(
                Constants.DataPath,
                "Local",
                "excellentwingoption.bmd");

            var itemReader =
                new ItemTooltipBMDReader();

            var textReader =
                new ItemTooltipTextBMDReader();

            var excellentReader =
                new ExcellentOptionBMDReader();

            var itemEntries =
                await itemReader
                    .Load(itemTooltipPath)
                    .ConfigureAwait(false);

            var textEntries =
                await textReader
                    .Load(itemTooltipTextPath)
                    .ConfigureAwait(false);

            var excellentEntries =
                await excellentReader
                    .Load(excellentCommonPath)
                    .ConfigureAwait(false);

            var excellentWingEntries =
                await excellentReader
                    .Load(excellentWingPath)
                    .ConfigureAwait(false);

            var itemDictionary =
                new Dictionary<int, ItemTooltipBMD>();

            foreach (var item in itemEntries)
            {
                if (!IsActive(item))
                {
                    continue;
                }

                int key =
                    MakeItemKey(
                        item.Category,
                        item.Index);

                // Keep the first valid entry if malformed data ever
                // contains a duplicate.
                itemDictionary.TryAdd(
                    key,
                    item);
            }
            var excellentDictionary =
                new Dictionary<int, ExcellentOptionBMD>();

            foreach (var option in excellentEntries)
            {
                int key =
                    MakeExcellentOptionKey(
                        option.Category,
                        option.Number);

                excellentDictionary.TryAdd(
                    key,
                    option);
            }
        
            var excellentWingDictionary =
                new Dictionary<int, ExcellentOptionBMD>();

            foreach (var option in excellentWingEntries)
            {
                int key =
                    MakeExcellentOptionKey(
                        option.Category,
                        option.Number);

                excellentWingDictionary.TryAdd(
                    key,
                    option);
            }

            var textDictionary =
                new Dictionary<ushort, ItemTooltipTextBMD>();

            foreach (var text in textEntries)
            {
                textDictionary.TryAdd(
                    text.Id,
                    text);
            }

            _items =
                itemDictionary;

            _texts =
                textDictionary;

            _excellentCommonOptions =
                excellentDictionary;

            _excellentWingOptions =
                excellentWingDictionary;

            IsLoaded = true;

            Console.WriteLine(
            $"[ItemTooltipData] Loaded " +
            $"{_items.Count} item definitions, " +
            $"{_texts.Count} text definitions, " +
            $"{_excellentCommonOptions.Count} common excellent options and " +
            $"{_excellentWingOptions.Count} wing excellent options.");
        }

        /// <summary>
        /// Finds the tooltip definition for a MU item.
        /// </summary>
        public static ItemTooltipBMD? GetItem(
            byte group,
            ushort index)
        {
            EnsureLoaded();

            int key =
                MakeItemKey(
                    group,
                    index);

            _items.TryGetValue(
                key,
                out var result);

            return result;
        }

        /// <summary>
        /// Tries to find a tooltip definition.
        /// </summary>
        public static bool TryGetItem(
            byte group,
            ushort index,
            out ItemTooltipBMD? item)
        {
            EnsureLoaded();

            return _items.TryGetValue(
                MakeItemKey(group, index),
                out item);
        }

        /// <summary>
        /// Finds one text template from itemtooltiptext.bmd.
        /// </summary>
        public static ItemTooltipTextBMD? GetText(
            ushort id)
        {
            EnsureLoaded();

            _texts.TryGetValue(
                id,
                out var result);

            return result;
        }

        public static bool TryGetText(
            ushort id,
            out ItemTooltipTextBMD? text)
        {
            EnsureLoaded();

            return _texts.TryGetValue(
                id,
                out text);
        }

        /// <summary>
        /// MU item key:
        ///
        /// Group * 512 + Index
        /// </summary>
        public static int MakeItemKey(
            byte group,
            ushort index)
        {
            return
                (group *
                 ItemTooltipBMDReader.ItemsPerCategory)
                + index;
        }

        private static bool IsActive(
            ItemTooltipBMD item)
        {
            return
                item.Category != 0 ||
                item.Index != 0 ||
                !string.IsNullOrEmpty(item.Name);
        }
        public static ExcellentOptionBMD?
            GetExcellentCommonOption(
                byte category,
                byte number)
        {
            EnsureLoaded();

            _excellentCommonOptions.TryGetValue(
                MakeExcellentOptionKey(
                    category,
                    number),
                out var result);

            return result;
        }

        public static bool TryGetExcellentCommonOption(
            byte category,
            byte number,
            out ExcellentOptionBMD? option)
        {
            EnsureLoaded();

            return
                _excellentCommonOptions.TryGetValue(
                    MakeExcellentOptionKey(
                        category,
                        number),
                    out option);
        }
        public static ExcellentOptionBMD?
            GetExcellentWingOption(
                byte category,
                byte number)
        {
            EnsureLoaded();

            _excellentWingOptions.TryGetValue(
                MakeExcellentOptionKey(
                    category,
                    number),
                out var result);

            return result;
        }

        private static int MakeExcellentOptionKey(
            byte category,
            byte number)
        {
            return
                (category << 8) |
                number;
        }

        private static void EnsureLoaded()
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException(
                    "ItemTooltipDataRepository has not been loaded. " +
                    "Call ItemTooltipDataRepository.LoadAsync() first.");
            }
        }
    }
}