namespace Client.Main.Core.Client
{
    public sealed class CharacterProgressionInfo
    {
        public string CharacterName { get; set; } = string.Empty;
        public int Class { get; set; }
        public bool IsMasterClass { get; set; }
        public int MaximumLevel { get; set; }
        public int MaximumMasterLevel { get; set; }
        public int[] BaseStatLimits { get; set; } = System.Array.Empty<int>();
        public int Level { get; set; }
        public int MasterLevel { get; set; }
        public ulong NormalStart { get; set; }
        public ulong NormalNext { get; set; }
        public ulong MasterStart { get; set; }
        public ulong MasterNext { get; set; }
    }
}
