namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One Ancient option text entry from:
    ///
    /// Data/Local/itemsetoptiontext.bmd
    /// </summary>
    public sealed class ItemSetOptionTextBMD
    {
        public byte Id { get; init; }

        public string Text { get; init; } =
            string.Empty;

        public byte Unknown { get; init; }

        public bool HasText =>
            !string.IsNullOrWhiteSpace(
                Text);

        public override string ToString()
        {
            return
                $"Id={Id}, " +
                $"Text='{Text}', " +
                $"Unknown={Unknown}";
        }
    }
}