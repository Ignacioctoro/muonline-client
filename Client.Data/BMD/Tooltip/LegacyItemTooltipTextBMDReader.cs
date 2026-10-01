using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for legacy / Season 6 ItemTooltipText_*.bmd files.
    ///
    /// Legacy format:
    ///
    /// 512 * 260-byte encrypted records
    /// DWORD checksum
    ///
    /// There is NO record-count header.
    ///
    /// Each record:
    ///
    /// WORD  ID
    /// char  Text[256]
    /// short Type
    ///
    /// Total:
    ///
    /// 512 * 260 = 133120 bytes
    /// + 4-byte checksum
    /// = 133124 bytes
    ///
    /// Encryption:
    ///
    /// byte ^= FC / CF / AB
    ///
    /// The XOR sequence restarts for every record.
    /// </summary>
    public sealed class LegacyItemTooltipTextBMDReader
        : BaseReader<List<ItemTooltipTextBMD>>
    {
        public const int RecordCount = 512;

        public const int RecordSize = 260;

        public const int ChecksumSize = 4;

        public const int ExpectedFileSize =
            (RecordCount * RecordSize) +
            ChecksumSize;

        protected override List<ItemTooltipTextBMD> Read(
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
                    $"Invalid legacy ItemTooltipText BMD size. " +
                    $"Expected {ExpectedFileSize:N0} bytes, " +
                    $"but received {buffer.Length:N0} bytes.");
            }

            var result =
                new List<ItemTooltipTextBMD>(
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
                        $"ItemTooltipText BMD while reading " +
                        $"record {recordIndex}.");
                }

                DecryptRecord(
                    encryptedRecord);

                result.Add(
                    ReadRecord(
                        encryptedRecord,
                        recordIndex));
            }

            // Final 4 bytes are the checksum.
            // We preserve the same behavior as the modern reader:
            // read it, but don't validate it yet.
            byte[] checksum =
                reader.ReadBytes(
                    ChecksumSize);

            if (checksum.Length !=
                ChecksumSize)
            {
                throw new EndOfStreamException(
                    "Legacy ItemTooltipText BMD is " +
                    "missing its trailing checksum.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after legacy " +
                    "ItemTooltipText BMD checksum.");
            }

            return result;
        }

        private static void DecryptRecord(
            byte[] data)
        {
            byte[] xorKey =
            {
                0xFC,
                0xCF,
                0xAB
            };

            for (int i = 0;
                 i < data.Length;
                 i++)
            {
                data[i] ^=
                    xorKey[
                        i %
                        xorKey.Length];
            }
        }

        private static ItemTooltipTextBMD
            ReadRecord(
                byte[] record,
                int recordIndex)
        {
            if (record.Length !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Legacy tooltip text record " +
                    $"{recordIndex} has invalid size.");
            }

            using var stream =
                new MemoryStream(
                    record,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            ushort id =
                reader.ReadUInt16();

            byte[] textBytes =
                reader.ReadBytes(256);

            string text =
                MuTooltipEncoding
                    .DecodeNullTerminated(
                        textBytes);

            short type =
                reader.ReadInt16();

            if (reader.BaseStream.Position !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Legacy tooltip text record " +
                    $"{recordIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new ItemTooltipTextBMD
            {
                Id = id,
                Text = text,
                Type = type
            };
        }
    }
}