namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One decoded entry from Data/Local/itemtooltip.bmd.
    ///
    /// Modern MU format:
    ///
    /// Category
    /// Index
    /// Name
    ///
    /// Packed tooltip properties
    ///
    /// 15 tooltip text definitions
    /// </summary>
    public sealed class ItemTooltipBMD
    {
        public const int TextEntryCount = 15;

        public byte Category { get; init; }

        public ushort Index { get; init; }

        public string Name { get; init; } =
            string.Empty;

        // -------------------------------------------------------------
        // Raw packed fields
        //
        // We keep them because this is reverse-engineered game data
        // and they are useful if a future client version changes.
        // -------------------------------------------------------------

        public short PackedNameStyle { get; init; }

        public short PackedFlags { get; init; }

        public short PackedRenderFlags { get; init; }

        public short ItemLevel { get; init; }

        // -------------------------------------------------------------
        // Decoded properties
        // -------------------------------------------------------------

        /// <summary>
        /// Original MU text color index for the item name.
        /// </summary>
        public byte NameColor =>
            (byte)(PackedNameStyle & 0xFF);

        /// <summary>
        /// Whether the item name is rendered bold.
        /// </summary>
        public bool NameBold =>
            ((PackedNameStyle >> 8) & 0xFF) != 0;

        /// <summary>
        /// Original NoTaxGold tooltip flag.
        /// </summary>
        public bool NoTaxGold =>
            (PackedFlags & 0xFF) != 0;

        /// <summary>
        /// Original OptionSpecial tooltip flag.
        /// </summary>
        public bool OptionSpecial =>
            ((PackedFlags >> 8) & 0xFF) != 0;

        /// <summary>
        /// Determines whether +Level is rendered with the item name.
        /// </summary>
        public bool RenderLevel =>
            (PackedRenderFlags & 0xFF) != 0;

        /// <summary>
        /// Reserved/unknown high byte.
        ///
        /// Preserved until its exact purpose is confirmed.
        /// </summary>
        public byte ReservedRenderFlag =>
            (byte)((PackedRenderFlags >> 8) & 0xFF);

        /// <summary>
        /// Tooltip text entries used by this item.
        /// </summary>
        public ItemTooltipLineBMD[] Lines { get; init; } =
            new ItemTooltipLineBMD[TextEntryCount];

        public override string ToString()
        {
            return $"{Category}:{Index} - {Name}";
        }
    }

    /// <summary>
    /// One text reference inside an ItemTooltip entry.
    ///
    /// The original client structure represented this as:
    ///
    /// short Text;
    /// byte  Color;
    /// bool  FontBold;
    ///
    /// The modern BMD packs Color + FontBold into one short.
    /// </summary>
    public sealed class ItemTooltipLineBMD
    {
        public short TextId { get; init; }

        public short PackedStyle { get; init; }

        /// <summary>
        /// Original MU color index.
        /// </summary>
        public byte Color =>
            (byte)(PackedStyle & 0xFF);

        /// <summary>
        /// Whether the line should use bold text.
        /// </summary>
        public bool Bold =>
            ((PackedStyle >> 8) & 0xFF) != 0;

        public bool HasText =>
            TextId >= 0;

        public override string ToString()
        {
            return
                $"TextID={TextId}, Color={Color}, Bold={Bold}";
        }
    }
}