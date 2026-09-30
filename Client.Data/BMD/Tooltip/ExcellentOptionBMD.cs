namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// One entry from excellentcommonoption.bmd or
    /// excellentwingoption.bmd.
    ///
    /// Modern MU structure size: 120 bytes.
    /// </summary>
    public sealed class ExcellentOptionBMD
    {
        public byte Category { get; init; }

        public byte Number { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public byte Operator { get; init; }

        public int Value { get; init; }

        public int Damage { get; init; }

        public byte Zen { get; init; }

        public byte DamageChance { get; init; }

        public byte Offense { get; init; }

        public byte Defense { get; init; }

        public byte Life { get; init; }

        public byte Mana { get; init; }

        public byte Other { get; init; }

        public override string ToString()
        {
            return
                $"{Category}:{Number} - {Name} " +
                $"Value={Value}";
        }
    }
}