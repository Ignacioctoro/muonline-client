namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One entry from Data/Local/itemsetoption.bmd.
    ///
    /// Modern Data_Broyal record:
    /// 1504 bytes.
    ///
    /// Confirmed:
    ///
    /// char Name1[64]
    /// char Name2[64]
    ///
    /// The remaining 1376 bytes contain the Ancient
    /// option definitions.
    /// </summary>
    public sealed class ItemSetOptionBMD
    {
        public int Id { get; init; }

        public string Name1 { get; init; } =
            string.Empty;

        public string Name2 { get; init; } =
            string.Empty;

        /// <summary>
        /// Raw decrypted bytes after Name1 + Name2.
        ///
        /// 1504 - 128 = 1376 bytes.
        /// </summary>
        public byte[] OptionData { get; init; } =
            System.Array.Empty<byte>();

        public bool HasName =>
            !string.IsNullOrWhiteSpace(Name1) ||
            !string.IsNullOrWhiteSpace(Name2);

        public override string ToString()
        {
            return
                $"Id={Id}, " +
                $"Name1='{Name1}', " +
                $"Name2='{Name2}'";
        }
    }
}