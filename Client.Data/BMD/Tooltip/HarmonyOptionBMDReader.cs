using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for:
    ///
    /// Data/Local/jewelofharmonyoption.bmd
    ///
    /// Original MU layout:
    ///
    /// 3 tables
    /// 250 records per table
    /// 180 bytes per record
    ///
    /// 750 * 180 = 135000 bytes
    ///
    /// Record:
    ///
    /// int  OptionType
    /// char Name[60]
    /// int  MinimumLevel
    /// int  Values[14]
    /// int  ZenRequirements[14]
    ///
    /// Records use the standard MU XOR3 encryption.
    /// </summary>
    public sealed class HarmonyOptionBMDReader
        : BaseReader<List<HarmonyOptionBMD>>
    {
        public const int TableCount = 3;

        public const int RecordsPerTable = 250;

        public const int RecordCount =
            TableCount *
            RecordsPerTable;

        public const int RecordSize = 180;

        public const int ExpectedFileSize =
            RecordCount *
            RecordSize;

        public const int ValueCount = 14;

        protected override List<HarmonyOptionBMD> Read(
            byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(
                    nameof(buffer));
            }

            if (buffer.Length !=
                ExpectedFileSize)
            {
                throw new InvalidDataException(
                    $"Invalid jewelofharmonyoption.bmd size. " +
                    $"Expected {ExpectedFileSize} bytes, " +
                    $"got {buffer.Length}.");
            }

            var result =
                new List<HarmonyOptionBMD>(
                    RecordCount);

            for (int table = 0;
                 table < TableCount;
                 table++)
            {
                for (int index = 0;
                     index < RecordsPerTable;
                     index++)
                {
                    int physicalIndex =
                        table *
                        RecordsPerTable +
                        index;

                    int offset =
                        physicalIndex *
                        RecordSize;

                    byte[] encrypted =
                        new byte[RecordSize];

                    Buffer.BlockCopy(
                        buffer,
                        offset,
                        encrypted,
                        0,
                        RecordSize);

                    byte[] record =
                        BuxCryptor.Convert(
                            encrypted);

                    var option =
                        ReadRecord(
                            record,
                            table,
                            index,
                            physicalIndex);

                    // Unused records in the original BMD are
                    // initialized with -1.
                    if (option.OptionType < 0)
                    {
                        continue;
                    }

                    result.Add(
                        option);
                }
            }

            return result;
        }

        private static HarmonyOptionBMD ReadRecord(
            byte[] record,
            int table,
            int index,
            int physicalIndex)
        {
            if (record.Length !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Harmony record " +
                    $"{physicalIndex} has invalid size.");
            }

            using var stream =
                new MemoryStream(
                    record,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            // -----------------------------------------------------
            // OPTION TYPE
            // -----------------------------------------------------

            int optionType =
                reader.ReadInt32();

            // -----------------------------------------------------
            // NAME
            // -----------------------------------------------------

            byte[] nameBytes =
                reader.ReadBytes(60);

            if (nameBytes.Length != 60)
            {
                throw new EndOfStreamException(
                    $"Harmony record {physicalIndex} " +
                    $"has an incomplete name.");
            }

            string name =
                MuTooltipEncoding
                    .DecodeNullTerminated(
                        nameBytes);

            // -----------------------------------------------------
            // MINIMUM ITEM LEVEL
            // -----------------------------------------------------

            int minimumLevel =
                reader.ReadInt32();

            // -----------------------------------------------------
            // VALUES
            // -----------------------------------------------------

            int[] values =
                new int[ValueCount];

            for (int i = 0;
                 i < ValueCount;
                 i++)
            {
                values[i] =
                    reader.ReadInt32();
            }

            // -----------------------------------------------------
            // ZEN REQUIREMENTS
            // -----------------------------------------------------

            int[] zenRequirements =
                new int[ValueCount];

            for (int i = 0;
                 i < ValueCount;
                 i++)
            {
                zenRequirements[i] =
                    reader.ReadInt32();
            }

            if (reader.BaseStream.Position !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Harmony record " +
                    $"{physicalIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new HarmonyOptionBMD
            {
                Table =
                    table,

                Index =
                    index,

                OptionType =
                    optionType,

                Name =
                    name,

                MinimumLevel =
                    minimumLevel,

                Values =
                    values,

                ZenRequirements =
                    zenRequirements
            };
        }
    }
}