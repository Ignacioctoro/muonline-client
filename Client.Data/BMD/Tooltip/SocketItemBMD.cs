namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One entry from Data/Local/socketitem.bmd.
    ///
    /// The file contains three groups of 50 records:
    ///
    /// 0..49   = seed/socket options
    /// 50..99  = socket set/bonus entries
    /// 100..149 = socket bonus entries
    ///
    /// Each physical record is 168 bytes.
    /// </summary>
    public sealed class SocketItemBMD
    {
        public int Table { get; init; }

        public int Id { get; init; }

        public int ElementType { get; init; }

        public int Level { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public int BonusType { get; init; }

        public int[] Values { get; init; } =
            new int[20];

        public byte FireNeed { get; init; }

        public byte WaterNeed { get; init; }

        public byte IceNeed { get; init; }

        public byte WindNeed { get; init; }

        public byte LightningNeed { get; init; }

        public byte EarthNeed { get; init; }

        public override string ToString()
        {
            return
                $"Table={Table}, Id={Id}, " +
                $"Element={ElementType}, " +
                $"Level={Level}, Name='{Name}'";
        }
    }
}