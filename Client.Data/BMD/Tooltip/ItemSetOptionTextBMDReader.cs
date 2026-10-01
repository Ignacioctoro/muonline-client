using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for:
    ///
    /// Data/Local/itemsetoptiontext.bmd
    ///
    /// Layout:
    ///
    /// DWORD count
    /// 255 × ITEM_SET_OPTION_TEXT
    /// DWORD CRC
    ///
    /// ITEM_SET_OPTION_TEXT:
    ///
    /// BYTE ID
    /// char Text[100]
    /// BYTE Unknown
    ///
    /// File size:
    ///
    /// 4 + (255 * 102) + 4
    /// = 26018 bytes
    /// </summary>
    public sealed class ItemSetOptionTextBMDReader
        : BaseReader<List<ItemSetOptionTextBMD>>
    {
        public const int RecordCount = 255;

        public const int RecordSize = 102;

        public const int HeaderSize = 4;

        public const int ChecksumSize = 4;

        public const int TextSize = 100;

        public const int ExpectedFileSize =
            HeaderSize +
            (RecordCount * RecordSize) +
            ChecksumSize;

        protected override List<ItemSetOptionTextBMD> Read(
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
                    $"Invalid itemsetoptiontext.bmd size. " +
                    $"Expected {ExpectedFileSize} bytes, " +
                    $"got {buffer.Length}.");
            }

            var result =
                new List<ItemSetOptionTextBMD>(
                    RecordCount);

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            // ---------------------------------------------------------
            // HEADER / COUNTER
            // ---------------------------------------------------------

            uint declaredCount =
                reader.ReadUInt32();

            // Some client versions don't necessarily use this value
            // strictly, so the physical file structure remains
            // authoritative.
            _ = declaredCount;

            // ---------------------------------------------------------
            // RECORDS
            // ---------------------------------------------------------

            for (int i = 0;
                 i < RecordCount;
                 i++)
            {
                byte[] encrypted =
                    reader.ReadBytes(
                        RecordSize);

                if (encrypted.Length !=
                    RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected EOF while reading " +
                        $"itemsetoptiontext record {i}.");
                }

                byte[] record =
                    BuxCryptor.Convert(
                        encrypted);

                byte id =
                    record[0];

                var textBytes =
                    new byte[TextSize];

                Buffer.BlockCopy(
                    record,
                    1,
                    textBytes,
                    0,
                    TextSize);

                string text =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            textBytes);

                byte unknown =
                    record[101];

                result.Add(
                    new ItemSetOptionTextBMD
                    {
                        Id =
                            id,

                        Text =
                            text,

                        Unknown =
                            unknown
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
                    "itemsetoptiontext.bmd is missing " +
                    "its trailing CRC.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after " +
                    "itemsetoptiontext.bmd CRC.");
            }

            return result;
        }
    }
}