using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for Data/Local/itemsetoption.bmd.
    ///
    /// Data_Broyal:
    ///
    /// 250 records
    /// 1504 bytes per record
    /// 4 byte trailing CRC
    ///
    /// (250 * 1504) + 4 = 376004
    /// </summary>
    public sealed class ItemSetOptionBMDReader
        : BaseReader<List<ItemSetOptionBMD>>
    {
        public const int RecordCount = 250;

        public const int RecordSize = 1504;

        public const int NameSize = 64;

        public const int ChecksumSize = 4;

        private const int NormalOptionCount = 12;

        private const int FullOptionCount = 9;

        private const int ClassCount = 11;

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

                // Each record resets the FC CF AB XOR sequence.
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
                // NAMES
                // -----------------------------------------------------

                string name1 =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            recordReader.ReadBytes(
                                NameSize));

                string name2 =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            recordReader.ReadBytes(
                                NameSize));

                // -----------------------------------------------------
                // NORMAL ANCIENT OPTION INDEXES
                // -----------------------------------------------------

                var optionIndexes =
                    new int[NormalOptionCount];

                for (int i = 0;
                     i < NormalOptionCount;
                     i++)
                {
                    optionIndexes[i] =
                        recordReader.ReadInt32();
                }

                // -----------------------------------------------------
                // NORMAL ANCIENT OPTION VALUES
                // -----------------------------------------------------

                var optionValues =
                    new int[NormalOptionCount];

                for (int i = 0;
                     i < NormalOptionCount;
                     i++)
                {
                    optionValues[i] =
                        recordReader.ReadInt32();
                }

                // -----------------------------------------------------
                // EXTRA OPTIONS
                // -----------------------------------------------------

                int firstExtraOptionIndex =
                    recordReader.ReadInt32();

                int secondExtraOptionIndex =
                    recordReader.ReadInt32();

                int firstExtraOptionValue =
                    recordReader.ReadInt32();

                int secondExtraOptionValue =
                    recordReader.ReadInt32();

                // -----------------------------------------------------
                // FLAG
                // -----------------------------------------------------

                int flag =
                    recordReader.ReadInt32();

                // -----------------------------------------------------
                // FULL SET OPTION INDEXES
                // -----------------------------------------------------

                var fullOptionIndexes =
                    new int[FullOptionCount];

                for (int i = 0;
                     i < FullOptionCount;
                     i++)
                {
                    fullOptionIndexes[i] =
                        recordReader.ReadInt32();
                }

                // -----------------------------------------------------
                // FULL SET OPTION VALUES
                // -----------------------------------------------------

                var fullOptionValues =
                    new int[FullOptionCount];

                for (int i = 0;
                     i < FullOptionCount;
                     i++)
                {
                    fullOptionValues[i] =
                        recordReader.ReadInt32();
                }

                // -----------------------------------------------------
                // CLASS FLAGS
                // -----------------------------------------------------

                var classFlags =
                    new int[ClassCount];

                for (int i = 0;
                     i < ClassCount;
                     i++)
                {
                    classFlags[i] =
                        recordReader.ReadInt32();
                }

                // -----------------------------------------------------
                // MODERN DATA
                // -----------------------------------------------------
                //
                // Everything after the classic 360-byte structure
                // belongs to later MU versions.
                //
                // We deliberately ignore it for our Season 6 tooltip.
                // -----------------------------------------------------

                int remaining =
                    RecordSize -
                    (int)recordReader.BaseStream.Position;

                if (remaining < 0)
                {
                    throw new InvalidDataException(
                        $"ItemSetOption record {id} " +
                        $"exceeded its expected size.");
                }

                if (remaining > 0)
                {
                    recordReader.ReadBytes(
                        remaining);
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

                        OptionIndexes =
                            optionIndexes,

                        OptionValues =
                            optionValues,

                        FirstExtraOptionIndex =
                            firstExtraOptionIndex,

                        SecondExtraOptionIndex =
                            secondExtraOptionIndex,

                        FirstExtraOptionValue =
                            firstExtraOptionValue,

                        SecondExtraOptionValue =
                            secondExtraOptionValue,

                        Flag =
                            flag,

                        FullOptionIndexes =
                            fullOptionIndexes,

                        FullOptionValues =
                            fullOptionValues,

                        ClassFlags =
                            classFlags
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