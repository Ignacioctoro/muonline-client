namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One entry from Data/Local/itemsettype.bmd.
    ///
    /// Data_Broyal modern layout:
    ///
    /// ushort Tier1
    /// ushort Tier2
    /// ushort Tier3
    /// ushort Tier4
    /// ushort Tier5
    /// </summary>
    public sealed class ItemSetTypeBMD
    {
        public int Group { get; init; }

        public int Index { get; init; }

        public int GlobalIndex { get; init; }

        public ushort Tier1 { get; init; }

        public ushort Tier2 { get; init; }

        public ushort Tier3 { get; init; }

        public ushort Tier4 { get; init; }

        public ushort Tier5 { get; init; }

        public bool HasAnySet =>
            Tier1 != 0 ||
            Tier2 != 0 ||
            Tier3 != 0 ||
            Tier4 != 0 ||
            Tier5 != 0;

        public ushort GetSetId(
            byte discriminator)
        {
            return discriminator switch
            {
                1 => Tier1,
                2 => Tier2,
                3 => Tier3,
                4 => Tier4,
                5 => Tier5,
                _ => 0
            };
        }

        public override string ToString()
        {
            return
                $"Item={Group},{Index}, " +
                $"Tier1={Tier1}, " +
                $"Tier2={Tier2}, " +
                $"Tier3={Tier3}, " +
                $"Tier4={Tier4}, " +
                $"Tier5={Tier5}";
        }
    }
}