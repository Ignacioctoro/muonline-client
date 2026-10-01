using System;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Ancient set definition from itemsetoption.bmd.
    ///
    /// The Data_Broyal record is 1504 bytes.
    ///
    /// The first 360 bytes correspond to the classic MU structure:
    ///
    /// Name1[64]
    /// Name2[64]
    /// 12 normal option indexes
    /// 12 normal option values
    /// 2 extra option indexes
    /// 2 extra option values
    /// Flag
    /// 9 full-set option indexes
    /// 9 full-set option values
    /// 11 class flags
    ///
    /// Remaining bytes belong to newer MU versions.
    /// </summary>
    public sealed class ItemSetOptionBMD
    {
        public int Id { get; init; }

        public string Name1 { get; init; } =
            string.Empty;

        public string Name2 { get; init; } =
            string.Empty;

        /// <summary>
        /// Six pairs of Ancient options.
        ///
        /// Layout:
        ///
        /// [0] = first option for stage 1
        /// [1] = second option for stage 1
        /// [2] = first option for stage 2
        /// [3] = second option for stage 2
        /// ...
        /// </summary>
        public int[] OptionIndexes { get; init; } =
            Array.Empty<int>();

        public int[] OptionValues { get; init; } =
            Array.Empty<int>();

        public int FirstExtraOptionIndex { get; init; }

        public int SecondExtraOptionIndex { get; init; }

        public int FirstExtraOptionValue { get; init; }

        public int SecondExtraOptionValue { get; init; }

        public int Flag { get; init; }

        public int[] FullOptionIndexes { get; init; } =
            Array.Empty<int>();

        public int[] FullOptionValues { get; init; } =
            Array.Empty<int>();

        /// <summary>
        /// Class flags in original order:
        ///
        /// DW, DK, FE, MG, DL, SU, RF, GL, RW, SL, GC.
        /// </summary>
        public int[] ClassFlags { get; init; } =
            Array.Empty<int>();

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