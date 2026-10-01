using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for Data/Local/socketitem.bmd.
    ///
    /// File layout used by this MU Data:
    ///
    /// 3 tables
    /// 50 records per table
    /// 168 bytes per record
    ///
    /// Total:
    /// 3 * 50 * 168 = 25200 bytes
    ///
    /// Every record uses MU XOR3 encryption:
    /// FC CF AB
    /// </summary>
    public sealed class SocketItemBMDReader
        : BaseReader<List<SocketItemBMD>>
    {
        public const int TableCount = 3;

        public const int RecordsPerTable = 50;

        public const int RecordSize = 168;

        public const int TotalRecords =
            TableCount *
            RecordsPerTable;

        public const int ExpectedFileSize =
            TotalRecords *
            RecordSize;

        protected override List<SocketItemBMD> Read(
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
                    $"Invalid socketitem.bmd size. " +
                    $"Expected {ExpectedFileSize} bytes, " +
                    $"got {buffer.Length}.");
            }

            var result =
                new List<SocketItemBMD>(
                    TotalRecords);

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

                    result.Add(
                        ReadRecord(
                            record,
                            table,
                            physicalIndex));
                }
            }

            return result;
        }

        private static SocketItemBMD ReadRecord(
            byte[] record,
            int table,
            int physicalIndex)
        {
            if (record.Length !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Socket item record " +
                    $"{physicalIndex} has invalid size.");
            }

            using var stream =
                new MemoryStream(
                    record,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            int id =
                reader.ReadInt32();

            int elementType =
                reader.ReadInt32();

            int level =
                reader.ReadInt32();

            byte[] nameBytes =
                reader.ReadBytes(64);

            string name =
                MuTooltipEncoding
                    .DecodeNullTerminated(
                        nameBytes);

            int bonusType =
                reader.ReadInt32();

            int[] values =
                new int[20];

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                values[i] =
                    reader.ReadInt32();
            }

            byte fireNeed =
                reader.ReadByte();

            byte waterNeed =
                reader.ReadByte();

            byte iceNeed =
                reader.ReadByte();

            byte windNeed =
                reader.ReadByte();

            byte lightningNeed =
                reader.ReadByte();

            byte earthNeed =
                reader.ReadByte();

            // C++ struct alignment:
            // actual data = 166 bytes,
            // physical struct = 168 bytes.
            _ = reader.ReadByte();
            _ = reader.ReadByte();

            if (reader.BaseStream.Position !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Socket item record " +
                    $"{physicalIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new SocketItemBMD
            {
                Table =
                    table,

                Id =
                    id,

                ElementType =
                    elementType,

                Level =
                    level,

                Name =
                    name,

                BonusType =
                    bonusType,

                Values =
                    values,

                FireNeed =
                    fireNeed,

                WaterNeed =
                    waterNeed,

                IceNeed =
                    iceNeed,

                WindNeed =
                    windNeed,

                LightningNeed =
                    lightningNeed,

                EarthNeed =
                    earthNeed
            };
        }
    }
}