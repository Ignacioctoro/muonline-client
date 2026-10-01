using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for:
    ///
    /// Data/Local/itemsetoption.bmd
    ///
    /// Data_Broyal modern layout:
    ///
    /// 250 records
    /// 1504 bytes per record
    /// 4-byte trailing CRC
    ///
    /// Total:
    ///
    /// (250 * 1504) + 4
    /// = 376004 bytes
    ///
    /// Every record resets the MU XOR3 sequence:
    ///
    /// FC CF AB
    /// </summary>
    public sealed class ItemSetOptionBMDReader
        : BaseReader<List<ItemSetOptionBMD>>
    {
        public const int RecordCount = 250;

        public const int RecordSize = 1504;

        public const int NameSize = 64;

        public const int ChecksumSize = 4;

        public const int ExpectedFileSize =
            (RecordCount * RecordSize) +
            ChecksumSize;

        protected override List<ItemSetOptionBMD> Read(
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
                    $"Invalid itemsetoption.bmd size. " +
                    $"Expected {ExpectedFileSize} bytes, " +
                    $"got {buffer.Length}.");
            }

            var result =
                new List<ItemSetOptionBMD>(
                    RecordCount);

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            for (int id = 0;
                 id < RecordCount;
                 id++)
            {
                byte[] encrypted =
                    reader.ReadBytes(
                        RecordSize);

                if (encrypted.Length !=
                    RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected EOF while reading " +
                        $"itemsetoption record {id}.");
                }

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

                // -----------------------------------------------------
                // NAME 1
                // -----------------------------------------------------

                byte[] name1Bytes =
                    recordReader.ReadBytes(
                        NameSize);

                string name1 =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            name1Bytes);

                // -----------------------------------------------------
                // NAME 2
                // -----------------------------------------------------

                byte[] name2Bytes =
                    recordReader.ReadBytes(
                        NameSize);

                string name2 =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            name2Bytes);

                // -----------------------------------------------------
                // REMAINING DATA
                // -----------------------------------------------------
                //
                // We intentionally don't interpret this yet.
                //
                // 1504 - 128 = 1376 bytes still contain the actual
                // set option definitions.
                //
                // Skipping them is safe because the whole record has
                // already been decrypted independently.
                // -----------------------------------------------------

                int remaining =
                    RecordSize -
                    (NameSize * 2);

                byte[] remainingData =
                    recordReader.ReadBytes(
                        remaining);

                if (remainingData.Length !=
                    remaining)
                {
                    throw new EndOfStreamException(
                        $"ItemSetOption record {id} " +
                        $"is incomplete.");
                }

                result.Add(
                new ItemSetOptionBMD
                {
                    Id =
                        id,

                    Name1 =
                        name1,

                    Name2 =
                        name2,

                    OptionData =
                        remainingData
                });
            }

            byte[] checksum =
                reader.ReadBytes(
                    ChecksumSize);

            if (checksum.Length !=
                ChecksumSize)
            {
                throw new EndOfStreamException(
                    "itemsetoption.bmd is missing " +
                    "its trailing CRC.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after " +
                    "itemsetoption.bmd CRC.");
            }

            return result;
        }
    }
}