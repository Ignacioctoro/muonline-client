using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for:
    ///
    /// Data/Local/itemsettype.bmd
    ///
    /// Data_Broyal modern layout:
    ///
    /// 21 item categories
    /// 512 items per category
    /// 10 bytes per record
    ///
    /// Record:
    ///
    /// ushort Tier1
    /// ushort Tier2
    /// ushort Tier3
    /// ushort Tier4
    /// ushort Tier5
    ///
    /// Followed by a 4-byte CRC.
    ///
    /// Total:
    ///
    /// (21 * 512 * 10) + 4
    /// = 107524 bytes
    ///
    /// Each 10-byte record is independently encrypted
    /// with MU XOR3:
    ///
    /// FC CF AB
    /// </summary>
    public sealed class ItemSetTypeBMDReader
        : BaseReader<List<ItemSetTypeBMD>>
    {
        public const int CategoryCount = 21;

        public const int ItemsPerCategory = 512;

        public const int RecordSize = 10;

        public const int RecordCount =
            CategoryCount *
            ItemsPerCategory;

        public const int ChecksumSize = 4;

        public const int ExpectedFileSize =
            (RecordCount * RecordSize) +
            ChecksumSize;

        protected override List<ItemSetTypeBMD> Read(
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
                    $"Invalid itemsettype.bmd size. " +
                    $"Expected {ExpectedFileSize} bytes, " +
                    $"got {buffer.Length}.");
            }

            var result =
                new List<ItemSetTypeBMD>();

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            // ---------------------------------------------------------
            // RECORDS
            // ---------------------------------------------------------

            for (int globalIndex = 0;
                 globalIndex < RecordCount;
                 globalIndex++)
            {
                byte[] encrypted =
                    reader.ReadBytes(
                        RecordSize);

                if (encrypted.Length !=
                    RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected EOF while reading " +
                        $"itemsettype record {globalIndex}.");
                }

                // Every ITEM_SET_TYPE record resets the
                // FC CF AB XOR sequence.
                byte[] record =
                    BuxCryptor.Convert(
                        encrypted);

                using var recordStream =
                    new MemoryStream(
                        record,
                        writable: false);

                using var recordReader =
                    new BinaryReader(
                        recordStream);

                ushort tier1 =
                    recordReader.ReadUInt16();

                ushort tier2 =
                    recordReader.ReadUInt16();

                ushort tier3 =
                    recordReader.ReadUInt16();

                ushort tier4 =
                    recordReader.ReadUInt16();

                ushort tier5 =
                    recordReader.ReadUInt16();

                if (recordReader.BaseStream.Position !=
                    RecordSize)
                {
                    throw new InvalidDataException(
                        $"ItemSetType record " +
                        $"{globalIndex} ended at " +
                        $"{recordReader.BaseStream.Position}, " +
                        $"expected {RecordSize}.");
                }

                // Empty slot.
                if (tier1 == 0 &&
                    tier2 == 0 &&
                    tier3 == 0 &&
                    tier4 == 0 &&
                    tier5 == 0)
                {
                    continue;
                }

                int group =
                    globalIndex /
                    ItemsPerCategory;

                int index =
                    globalIndex %
                    ItemsPerCategory;

                result.Add(
                    new ItemSetTypeBMD
                    {
                        Group =
                            group,

                        Index =
                            index,

                        GlobalIndex =
                            globalIndex,

                        Tier1 =
                            tier1,

                        Tier2 =
                            tier2,

                        Tier3 =
                            tier3,

                        Tier4 =
                            tier4,

                        Tier5 =
                            tier5
                    });
            }

            // ---------------------------------------------------------
            // CRC
            // ---------------------------------------------------------

            byte[] checksum =
                reader.ReadBytes(
                    ChecksumSize);

            if (checksum.Length !=
                ChecksumSize)
            {
                throw new EndOfStreamException(
                    "itemsettype.bmd is missing " +
                    "its trailing CRC.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after " +
                    "itemsettype.bmd CRC.");
            }

            return result;
        }
    }
}