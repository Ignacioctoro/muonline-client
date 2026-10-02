using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Client.Data.BMD.Tooltip;

namespace Client.Main.Core.Items.Tooltips
{
    /// <summary>
    /// Central access point for original MU tooltip data.
    ///
    /// Loads:
    ///
    /// Data/Local/itemtooltip.bmd
    /// Data/Local/itemtooltiptext.bmd
    /// Data/Local/excellentcommonoption.bmd
    /// Data/Local/excellentwingoption.bmd
    /// Data/Local/socketitem.bmd
    /// Data/Local/jewelofharmonyoption.bmd
    /// Data/Local/itemsettype.bmd
    /// Data/Local/itemsetoption.bmd
    /// Data/Local/itemsetoptiontext.bmd
    ///
    /// Data is loaded once and cached in dictionaries.
    /// </summary>
    public static class ItemTooltipDataRepository
    {
        private static readonly object _syncRoot =
            new();

        private static Task? _loadTask;

        private static Dictionary<int, ItemTooltipBMD>
            _items =
                new();

        private static Dictionary<int, ItemTooltipBMD>
            _legacyItems =
                new();

        private static Dictionary<ushort, ItemTooltipTextBMD>
            _texts =
                new();

        private static Dictionary<ushort, ItemTooltipTextBMD>
            _legacyLocalizedTexts =
                new();

        private static Dictionary<int, string>
            _globalTexts =
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

        private static Dictionary<int, ItemSetTypeBMD>
            _itemSetTypes =
                new();

        private static Dictionary<int, ItemSetOptionBMD>
            _itemSetOptions =
                new();

        private static Dictionary<byte, ItemSetOptionTextBMD>
            _itemSetOptionTexts =
                new();

        public static bool IsLoaded
        {
            get;
            private set;
        }
        public static int GetAncientSetItemCount(
            int setId)
        {
            EnsureLoaded();

            if (setId <= 0)
            {
                return 0;
            }

            int count =
                0;

            foreach (var setType in
                    _itemSetTypes.Values)
            {
                if (setType.Tier1 == setId ||
                    setType.Tier2 == setId ||
                    setType.Tier3 == setId ||
                    setType.Tier4 == setId ||
                    setType.Tier5 == setId)
                {
                    count++;
                }
            }

            return count;
        }

        public static int ItemCount =>
            _items.Count;
        
        public static int LegacyItemCount =>
            _legacyItems.Count;

        public static int TextCount =>
            _texts.Count;
        public static int LegacyLocalizedTextCount =>
            _legacyLocalizedTexts.Count;

        public static int GlobalTextCount =>
            _globalTexts.Count;

        public static int ExcellentCommonOptionCount =>
            _excellentCommonOptions.Count;

        public static int ExcellentWingOptionCount =>
            _excellentWingOptions.Count;

        public static int SocketOptionCount =>
            _socketOptions.Count;

        public static int HarmonyOptionCount =>
            _harmonyOptions.Count;

        public static int ItemSetTypeCount =>
            _itemSetTypes.Count;

        public static int ItemSetOptionCount =>
            _itemSetOptions.Count;

        public static int ItemSetOptionTextCount =>
            _itemSetOptionTexts.Count;

        // -------------------------------------------------------------
        // LOAD
        // -------------------------------------------------------------

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
            
            string legacyItemTooltipPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "ItemTooltip_s6.bmd");

            string itemTooltipTextPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemtooltiptext.bmd");
            string globalTextPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "text.bmd");
                            
            string legacyItemTooltipTextPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "ItemTooltipText_eng.bmd");

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

            string itemSetTypePath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemsettype.bmd");

            string itemSetOptionPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemsetoption.bmd");

            string itemSetOptionTextPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "itemsetoptiontext.bmd");

            // ---------------------------------------------------------
            // READERS
            // ---------------------------------------------------------

            var itemReader =
                new ItemTooltipBMDReader();
            
            var legacyItemReader =
                new LegacyItemTooltipBMDReader();

            var textReader =
                new ItemTooltipTextBMDReader();

            var legacyTextReader =
                new LegacyItemTooltipTextBMDReader();

            var globalTextReader =
                new LegacyGlobalTextBMDReader();

            var excellentReader =
                new ExcellentOptionBMDReader();

            var socketReader =
                new SocketItemBMDReader();

            var harmonyReader =
                new HarmonyOptionBMDReader();

            var itemSetTypeReader =
                new ItemSetTypeBMDReader();

            var itemSetOptionReader =
                new ItemSetOptionBMDReader();

            var itemSetOptionTextReader =
                new ItemSetOptionTextBMDReader();

            // ---------------------------------------------------------
            // LOAD FILES
            // ---------------------------------------------------------

            var itemEntries =
                await itemReader
                    .Load(itemTooltipPath)
                    .ConfigureAwait(false);

            var legacyItemEntries =
                await legacyItemReader
                    .Load(legacyItemTooltipPath)
                    .ConfigureAwait(false);

            var textEntries =
                await textReader
                    .Load(itemTooltipTextPath)
                    .ConfigureAwait(false);

            var legacyTextEntries =
                await legacyTextReader
                    .Load(legacyItemTooltipTextPath)
                    .ConfigureAwait(false);

            var globalTextEntries =
                await globalTextReader
                    .Load(globalTextPath)
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

            var itemSetTypeEntries =
                await itemSetTypeReader
                    .Load(itemSetTypePath)
                    .ConfigureAwait(false);

            var itemSetOptionEntries =
                await itemSetOptionReader
                    .Load(itemSetOptionPath)
                    .ConfigureAwait(false);

            var itemSetOptionTextEntries =
                await itemSetOptionTextReader
                    .Load(itemSetOptionTextPath)
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

            var legacyItemDictionary =
                new Dictionary<int, ItemTooltipBMD>();

            foreach (var item in legacyItemEntries)
            {
                if (!IsActive(item))
                {
                    continue;
                }

                int key =
                    MakeItemKey(
                        item.Category,
                        item.Index);

                legacyItemDictionary.TryAdd(
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
            var legacyLocalizedTextDictionary =
                new Dictionary<ushort, ItemTooltipTextBMD>();

            foreach (var text in legacyTextEntries)
            {
                legacyLocalizedTextDictionary.TryAdd(
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
            // ANCIENT ITEM SET TYPES
            // ---------------------------------------------------------

            var itemSetTypeDictionary =
                new Dictionary<int, ItemSetTypeBMD>();

            foreach (var setType in itemSetTypeEntries)
            {
                int key =
                    MakeItemKey(
                        (byte)setType.Group,
                        (ushort)setType.Index);

                itemSetTypeDictionary.TryAdd(
                    key,
                    setType);
            }

            // ---------------------------------------------------------
            // ANCIENT SET OPTIONS
            // ---------------------------------------------------------

            var itemSetOptionDictionary =
                new Dictionary<int, ItemSetOptionBMD>();

            foreach (var setOption in itemSetOptionEntries)
            {
                itemSetOptionDictionary.TryAdd(
                    setOption.Id,
                    setOption);
            }

            // ---------------------------------------------------------
            // ANCIENT OPTION TEXTS
            // ---------------------------------------------------------

            var itemSetOptionTextDictionary =
                new Dictionary<byte, ItemSetOptionTextBMD>();

            foreach (var optionText in itemSetOptionTextEntries)
            {
                itemSetOptionTextDictionary.TryAdd(
                    optionText.Id,
                    optionText);
            }

            // ---------------------------------------------------------
            // PUBLISH
            // ---------------------------------------------------------

            _items =
                itemDictionary;
            
            _legacyItems =
                legacyItemDictionary;

            _texts =
                textDictionary;
            
            _legacyLocalizedTexts =
                legacyLocalizedTextDictionary;

            _globalTexts =
                globalTextEntries;

            _excellentCommonOptions =
                excellentDictionary;

            _excellentWingOptions =
                excellentWingDictionary;

            _socketOptions =
                socketDictionary;

            _harmonyOptions =
                harmonyDictionary;

            _itemSetTypes =
                itemSetTypeDictionary;

            _itemSetOptions =
                itemSetOptionDictionary;

            _itemSetOptionTexts =
                itemSetOptionTextDictionary;

            IsLoaded = true;

            // ---------------------------------------------------------
            // LOAD SUMMARY
            // ---------------------------------------------------------

            Console.WriteLine(
                $"[ItemTooltipData] Loaded " +
                $"{_items.Count} item definitions, " +
                $"{_texts.Count} modern text definitions, " +
                $"{_legacyLocalizedTexts.Count} legacy localized texts, " +
                $"{_excellentCommonOptions.Count} common excellent options, " +
                $"{_excellentWingOptions.Count} wing excellent options, " +
                $"{_socketOptions.Count} socket entries, " +
                $"{_globalTexts.Count} global texts, " +
                $"{_harmonyOptions.Count} harmony entries, " +
                $"{_itemSetTypes.Count} ancient item set entries, " +
                $"{_itemSetOptions.Count} ancient set option entries and " +
                $"{_itemSetOptionTexts.Count} ancient option texts.");
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

        public static ItemTooltipBMD? GetLegacyItem(
            byte group,
            ushort index)
        {
            EnsureLoaded();

            _legacyItems.TryGetValue(
                MakeItemKey(
                    group,
                    index),
                out var result);

            return result;
        }

        public static bool TryGetItem(
            byte group,
            ushort index,
            out ItemTooltipBMD? item)
        {
            EnsureLoaded();

            return
                _items.TryGetValue(
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

            if (!_texts.TryGetValue(
                    id,
                    out var modernText))
            {
                return null;
            }

            // ---------------------------------------------------------
            // LEGACY LOCALIZATION OVERRIDE
            //
            // The modern Data_Broyal ItemTooltipText is Korean.
            //
            // For TextIds available in the Season 6 English language
            // file, use its localized string while preserving the
            // Type from the modern BMD.
            //
            // This is important because the modern tooltip structure
            // remains authoritative, while the legacy file is used
            // only as a localization source.
            // ---------------------------------------------------------

            if (_legacyLocalizedTexts.TryGetValue(
                    id,
                    out var localizedText) &&
                !string.IsNullOrWhiteSpace(
                    localizedText.Text))
            {
                return new ItemTooltipTextBMD
                {
                    Id = modernText.Id,

                    Text =
                        localizedText.Text,

                    Type =
                        modernText.Type
                };
            }

            return modernText;
        }
        public static ItemTooltipTextBMD? GetText(
            ushort id,
            bool preferLegacy)
        {
            EnsureLoaded();

            if (preferLegacy &&
                _legacyLocalizedTexts.TryGetValue(
                    id,
                    out var legacyText) &&
                !string.IsNullOrWhiteSpace(
                    legacyText.Text))
            {
                return legacyText;
            }

            return GetText(id);
        }

        public static bool TryGetText(
            ushort id,
            out ItemTooltipTextBMD? text)
        {
            text =
                GetText(id);

            return
                text != null;
        }
        public static bool HasLegacyLocalizedText(
            ushort id)
        {
            EnsureLoaded();

            return
                _legacyLocalizedTexts.TryGetValue(
                    id,
                    out var text) &&
                !string.IsNullOrWhiteSpace(
                    text.Text);
        }

        // -------------------------------------------------------------
        // GLOBAL TEXT
        //
        // Original MU Data/Local/text.bmd.
        // Used for global/localized strings which don't belong to
        // ItemTooltipText, such as the special Fenrir strings.
        // -------------------------------------------------------------

        public static string GetGlobalText(
            int id)
        {
            EnsureLoaded();

            return
                _globalTexts.TryGetValue(
                    id,
                    out var text)
                    ? text
                    : string.Empty;
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
        // ANCIENT ITEM SET TYPES
        // -------------------------------------------------------------

        public static ItemSetTypeBMD?
            GetItemSetType(
                byte group,
                ushort index)
        {
            EnsureLoaded();

            _itemSetTypes.TryGetValue(
                MakeItemKey(
                    group,
                    index),
                out var result);

            return result;
        }

        public static bool TryGetItemSetType(
            byte group,
            ushort index,
            out ItemSetTypeBMD? setType)
        {
            EnsureLoaded();

            return
                _itemSetTypes.TryGetValue(
                    MakeItemKey(
                        group,
                        index),
                    out setType);
        }

        // -------------------------------------------------------------
        // ANCIENT SET OPTIONS
        // -------------------------------------------------------------

        public static ItemSetOptionBMD?
            GetItemSetOption(
                int id)
        {
            EnsureLoaded();

            _itemSetOptions.TryGetValue(
                id,
                out var result);

            return result;
        }

        public static bool TryGetItemSetOption(
            int id,
            out ItemSetOptionBMD? option)
        {
            EnsureLoaded();

            return
                _itemSetOptions.TryGetValue(
                    id,
                    out option);
        }

        // -------------------------------------------------------------
        // ANCIENT OPTION TEXTS
        // -------------------------------------------------------------

        public static ItemSetOptionTextBMD?
            GetItemSetOptionText(
                int id)
        {
            EnsureLoaded();

            if (id < 0 ||
                id > byte.MaxValue)
            {
                return null;
            }

            _itemSetOptionTexts.TryGetValue(
                (byte)id,
                out var result);

            return result;
        }

        public static bool TryGetItemSetOptionText(
            int id,
            out ItemSetOptionTextBMD? optionText)
        {
            EnsureLoaded();

            optionText = null;

            if (id < 0 ||
                id > byte.MaxValue)
            {
                return false;
            }

            return
                _itemSetOptionTexts.TryGetValue(
                    (byte)id,
                    out optionText);
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