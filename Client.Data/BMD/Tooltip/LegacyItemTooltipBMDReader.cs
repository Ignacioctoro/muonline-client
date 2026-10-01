using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for Season 6 ItemTooltip.bmd.
    ///
    /// Legacy format:
    ///
    /// 16 categories
    /// 512 possible item slots per category
    /// 8192 physical records
    /// 124 bytes per record
    /// 4 trailing checksum bytes
    ///
    /// Record:
    ///
    /// byte   Category
    /// byte   Padding
    /// ushort Index
    /// char   Name[64]
    ///
    /// short  PackedNameStyle
    /// short  PackedFlags
    /// short  PackedRenderFlags
    /// short  ItemLevel
    ///
    /// 12 tooltip lines:
    ///     short TextId
    ///     short PackedStyle
    ///
    /// Records use the classic FC CF AB XOR encryption,
    /// restarting for each record.
    /// </summary>
    public sealed class LegacyItemTooltipBMDReader
        : BaseReader<List<ItemTooltipBMD>>
    {
        public const int CategoryCount = 16;

        public const int ItemsPerCategory = 512;

        public const int RecordCount =
            CategoryCount *
            ItemsPerCategory;

        public const int RecordSize = 124;

        public const int LegacyTextEntryCount = 12;

        public const int ChecksumSize = 4;

        public const int ExpectedFileSize =
            (RecordCount * RecordSize) +
            ChecksumSize;

        protected override List<ItemTooltipBMD> Read(
            byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(
                    nameof(buffer));
            }

            if (buffer.Length != ExpectedFileSize)
            {
                throw new InvalidDataException(
                    $"Invalid legacy ItemTooltip BMD size. " +
                    $"Expected {ExpectedFileSize:N0} bytes, " +
                    $"but received {buffer.Length:N0} bytes.");
            }

            var result =
                new List<ItemTooltipBMD>(
                    RecordCount);

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            for (int recordIndex = 0;
                 recordIndex < RecordCount;
                 recordIndex++)
            {
                byte[] encryptedRecord =
                    reader.ReadBytes(
                        RecordSize);

                if (encryptedRecord.Length !=
                    RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected end of legacy " +
                        $"ItemTooltip BMD at record " +
                        $"{recordIndex}.");
                }

                byte[] record =
                    BuxCryptor.Convert(
                        encryptedRecord);

                result.Add(
                    ReadRecord(
                        record,
                        recordIndex));
            }

            byte[] checksum =
                reader.ReadBytes(
                    ChecksumSize);

            if (checksum.Length !=
                ChecksumSize)
            {
                throw new EndOfStreamException(
                    "Legacy ItemTooltip BMD is " +
                    "missing its checksum.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after legacy " +
                    "ItemTooltip BMD checksum.");
            }

            return result;
        }

        private static ItemTooltipBMD ReadRecord(
            byte[] record,
            int recordIndex)
        {
            if (record.Length != RecordSize)
            {
                throw new InvalidDataException(
                    $"Legacy tooltip record " +
                    $"{recordIndex} has invalid size.");
            }

            using var stream =
                new MemoryStream(
                    record,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            byte category =
                reader.ReadByte();

            // Compiler padding before WORD Index.
            _ =
                reader.ReadByte();

            ushort index =
                reader.ReadUInt16();

            byte[] nameBytes =
                reader.ReadBytes(64);

            string name =
                MuTooltipEncoding
                    .DecodeNullTerminated(
                        nameBytes);

            short packedNameStyle =
                reader.ReadInt16();

            short packedFlags =
                reader.ReadInt16();

            short packedRenderFlags =
                reader.ReadInt16();

            short itemLevel =
                reader.ReadInt16();

            // ItemTooltipBMD currently exposes 15 slots because
            // the modern format has 15.
            //
            // Legacy S6 contains only 12, so the remaining 3
            // are initialized as unused.
            var lines =
                new ItemTooltipLineBMD[
                    ItemTooltipBMD.TextEntryCount];

            for (int i = 0;
                 i < lines.Length;
                 i++)
            {
                lines[i] =
                    new ItemTooltipLineBMD
                    {
                        TextId = -1,
                        PackedStyle = 0
                    };
            }

            for (int i = 0;
                 i < LegacyTextEntryCount;
                 i++)
            {
                short textId =
                    reader.ReadInt16();

                short packedStyle =
                    reader.ReadInt16();

                lines[i] =
                    new ItemTooltipLineBMD
                    {
                        TextId =
                            textId,

                        PackedStyle =
                            packedStyle
                    };
            }

            if (reader.BaseStream.Position !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Legacy tooltip record " +
                    $"{recordIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new ItemTooltipBMD
            {
                Category =
                    category,

                Index =
                    index,

                Name =
                    name,

                PackedNameStyle =
                    packedNameStyle,

                PackedFlags =
                    packedFlags,

                PackedRenderFlags =
                    packedRenderFlags,

                ItemLevel =
                    itemLevel,

                Lines =
                    lines
            };
        }
    }
}