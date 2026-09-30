namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One decoded entry from itemtooltiptext.bmd.
    ///
    /// Binary structure:
    ///
    /// WORD  ID
    /// char  Text[256]
    /// short Type
    ///
    /// Total: 260 bytes.
    /// </summary>
    public sealed class ItemTooltipTextBMD
    {
        public ushort Id { get; init; }

        public string Text { get; init; } =
            string.Empty;

        /// <summary>
        /// Tooltip text/status type used by the original client.
        ///
        /// We preserve the raw value until its exact behavior
        /// is mapped during tooltip implementation.
        /// </summary>
        public short Type { get; init; }

        public override string ToString()
        {
            return $"{Id}: {Text} (Type={Type})";
        }
    }
}