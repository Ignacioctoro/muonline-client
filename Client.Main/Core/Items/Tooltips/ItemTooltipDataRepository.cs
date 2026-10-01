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
    /// Data/Local/excellentcommonoption.bmd
    /// Data/Local/excellentwingoption.bmd
    /// Data/Local/socketitem.bmd
    /// Data/Local/jewelofharmonyoption.bmd
    ///
    /// Data is loaded once and then cached in dictionaries.
    /// </summary>
    public static class ItemTooltipDataRepository
    {
        private static readonly object _syncRoot =
            new();

        private static Task? _loadTask;

        private static Dictionary<int, ItemTooltipBMD>
            _items =
                new();

        private static Dictionary<ushort, ItemTooltipTextBMD>
            _texts =
                new();

        private static Dictionary<int, ExcellentOptionBMD>
            _excellentCommonOptions =
                new();

        private static Dictionary<int, ExcellentOptionBMD>
            _excellentWingOptions =
                new();

        private static Dictionary<int, SocketItemBMD>
            _socketOptions =
                new();

        private static Dictionary<int, HarmonyOptionBMD>
            _harmonyOptions =
                new();

        public static bool IsLoaded
        {
            get;
            private set;
        }

        public static int ItemCount =>
            _items.Count;

        public static int TextCount =>
            _texts.Count;

        public static int ExcellentCommonOptionCount =>
            _excellentCommonOptions.Count;

        public static int ExcellentWingOptionCount =>
            _excellentWingOptions.Count;

        public static int SocketOptionCount =>
            _socketOptions.Count;

        public static int HarmonyOptionCount =>
            _harmonyOptions.Count;

        /// <summary>
        /// Loads all original MU tooltip-related BMD files once.
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
            // ---------------------------------------------------------
            // PATHS
            // ---------------------------------------------------------

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

            string socketItemPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "socketitem.bmd");

            string harmonyOptionPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "jewelofharmonyoption.bmd");

            // ---------------------------------------------------------
            // READERS
            // ---------------------------------------------------------

            var itemReader =
                new ItemTooltipBMDReader();

            var textReader =
                new ItemTooltipTextBMDReader();

            var excellentReader =
                new ExcellentOptionBMDReader();

            var socketReader =
                new SocketItemBMDReader();

            var harmonyReader =
                new HarmonyOptionBMDReader();

            // ---------------------------------------------------------
            // LOAD FILES
            // ---------------------------------------------------------

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

            var socketEntries =
                await socketReader
                    .Load(socketItemPath)
                    .ConfigureAwait(false);

            var harmonyEntries =
                await harmonyReader
                    .Load(harmonyOptionPath)
                    .ConfigureAwait(false);

            // ---------------------------------------------------------
            // ITEM TOOLTIP DEFINITIONS
            // ---------------------------------------------------------

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

                itemDictionary.TryAdd(
                    key,
                    item);
            }

            // ---------------------------------------------------------
            // ITEM TOOLTIP TEXT
            // ---------------------------------------------------------

            var textDictionary =
                new Dictionary<ushort, ItemTooltipTextBMD>();

            foreach (var text in textEntries)
            {
                textDictionary.TryAdd(
                    text.Id,
                    text);
            }

            // ---------------------------------------------------------
            // COMMON EXCELLENT OPTIONS
            // ---------------------------------------------------------

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

            // ---------------------------------------------------------
            // WING EXCELLENT OPTIONS
            // ---------------------------------------------------------

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

            // ---------------------------------------------------------
            // SOCKET OPTIONS
            // ---------------------------------------------------------

            var socketDictionary =
                new Dictionary<int, SocketItemBMD>();

            foreach (var socket in socketEntries)
            {
                int key =
                    MakeSocketOptionKey(
                        socket.Table,
                        socket.Id);

                socketDictionary.TryAdd(
                    key,
                    socket);
            }

            // ---------------------------------------------------------
            // HARMONY OPTIONS
            // ---------------------------------------------------------

            var harmonyDictionary =
                new Dictionary<int, HarmonyOptionBMD>();

            foreach (var harmony in harmonyEntries)
            {
                int key =
                    MakeHarmonyOptionKey(
                        harmony.Table,
                        harmony.Index);

                harmonyDictionary.TryAdd(
                    key,
                    harmony);
            }

            // ---------------------------------------------------------
            // PUBLISH
            // ---------------------------------------------------------

            _items =
                itemDictionary;

            _texts =
                textDictionary;

            _excellentCommonOptions =
                excellentDictionary;

            _excellentWingOptions =
                excellentWingDictionary;

            _socketOptions =
                socketDictionary;

            _harmonyOptions =
                harmonyDictionary;

            IsLoaded = true;

            Console.WriteLine(
                $"[ItemTooltipData] Loaded " +
                $"{_items.Count} item definitions, " +
                $"{_texts.Count} text definitions, " +
                $"{_excellentCommonOptions.Count} common excellent options, " +
                $"{_excellentWingOptions.Count} wing excellent options, " +
                $"{_socketOptions.Count} socket entries and " +
                $"{_harmonyOptions.Count} harmony entries.");
        }

        // -------------------------------------------------------------
        // ITEM TOOLTIP
        // -------------------------------------------------------------

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

        public static bool TryGetItem(
            byte group,
            ushort index,
            out ItemTooltipBMD? item)
        {
            EnsureLoaded();

            return _items.TryGetValue(
                MakeItemKey(
                    group,
                    index),
                out item);
        }

        // -------------------------------------------------------------
        // ITEM TOOLTIP TEXT
        // -------------------------------------------------------------

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

        // -------------------------------------------------------------
        // COMMON EXCELLENT
        // -------------------------------------------------------------

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

        // -------------------------------------------------------------
        // WING EXCELLENT
        // -------------------------------------------------------------

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

        public static bool TryGetExcellentWingOption(
            byte category,
            byte number,
            out ExcellentOptionBMD? option)
        {
            EnsureLoaded();

            return
                _excellentWingOptions.TryGetValue(
                    MakeExcellentOptionKey(
                        category,
                        number),
                    out option);
        }

        // -------------------------------------------------------------
        // SOCKET OPTIONS
        // -------------------------------------------------------------

        public static SocketItemBMD?
            GetSocketOption(
                int table,
                int id)
        {
            EnsureLoaded();

            _socketOptions.TryGetValue(
                MakeSocketOptionKey(
                    table,
                    id),
                out var result);

            return result;
        }

        public static bool TryGetSocketOption(
            int table,
            int id,
            out SocketItemBMD? option)
        {
            EnsureLoaded();

            return
                _socketOptions.TryGetValue(
                    MakeSocketOptionKey(
                        table,
                        id),
                    out option);
        }

        // -------------------------------------------------------------
        // HARMONY OPTIONS
        // -------------------------------------------------------------

        /// <summary>
        /// Gets one Jewel of Harmony option.
        ///
        /// Table:
        /// 0 = physical weapons
        /// 1 = staff / wizardry
        /// 2 = defensive equipment
        ///
        /// optionNumber corresponds directly to the Harmony
        /// option number encoded by OpenMU.
        /// </summary>
        public static HarmonyOptionBMD?
            GetHarmonyOption(
                int table,
                int optionNumber)
        {
            EnsureLoaded();

            _harmonyOptions.TryGetValue(
                MakeHarmonyOptionKey(
                    table,
                    optionNumber),
                out var result);

            return result;
        }

        public static bool TryGetHarmonyOption(
            int table,
            int optionNumber,
            out HarmonyOptionBMD? option)
        {
            EnsureLoaded();

            return
                _harmonyOptions.TryGetValue(
                    MakeHarmonyOptionKey(
                        table,
                        optionNumber),
                    out option);
        }

        // -------------------------------------------------------------
        // KEYS
        // -------------------------------------------------------------

        public static int MakeItemKey(
            byte group,
            ushort index)
        {
            return
                (group *
                 ItemTooltipBMDReader.ItemsPerCategory)
                + index;
        }

        private static int MakeExcellentOptionKey(
            byte category,
            byte number)
        {
            return
                (category << 8) |
                number;
        }

        private static int MakeSocketOptionKey(
            int table,
            int id)
        {
            return
                (table << 8) |
                (id & 0xFF);
        }

        private static int MakeHarmonyOptionKey(
            int table,
            int optionNumber)
        {
            return
                (table << 16) |
                (optionNumber & 0xFFFF);
        }

        // -------------------------------------------------------------
        // HELPERS
        // -------------------------------------------------------------

        private static bool IsActive(
            ItemTooltipBMD item)
        {
            return
                item.Category != 0 ||
                item.Index != 0 ||
                !string.IsNullOrEmpty(
                    item.Name);
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