namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One entry from Data/Local/jewelofharmonyoption.bmd.
    ///
    /// Table:
    /// 0 = Physical weapons
    /// 1 = Staff / Wizardry
    /// 2 = Defensive equipment
    /// </summary>
    public sealed class HarmonyOptionBMD
    {
        /// <summary>
        /// Harmony table:
        /// 0 = weapon
        /// 1 = staff
        /// 2 = defense
        /// </summary>
        public int Table { get; init; }

        /// <summary>
        /// Physical position inside the corresponding
        /// Harmony table.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// Option type stored by the original MU data.
        /// </summary>
        public int OptionType { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public int MinimumLevel { get; init; }

        /// <summary>
        /// Harmony values for levels 0..13.
        /// </summary>
        public int[] Values { get; init; } =
            new int[14];

        /// <summary>
        /// Zen requirements for levels 0..13.
        /// </summary>
        public int[] ZenRequirements { get; init; } =
            new int[14];

        public override string ToString()
        {
            return
                $"Table={Table}, " +
                $"Index={Index}, " +
                $"OptionType={OptionType}, " +
                $"Name='{Name}', " +
                $"MinimumLevel={MinimumLevel}";
        }
    }
}