namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for Data/Local/itemtooltip.bmd.
    ///
    /// Data_Broyal / modern MU format:
    ///
    /// 21 categories
    /// 512 entries per category
    /// 10,752 physical records
    /// 136 bytes per record
    /// 4 trailing checksum bytes
    ///
    /// IMPORTANT:
    /// The physical record position is NOT the item ID.
    /// Records must later be indexed using:
    ///
    ///     Category * 512 + Index
    ///
    /// This matches the behavior of the original MU tools/client data.
    /// </summary>
    public sealed class ItemTooltipBMDReader : BaseReader<List<ItemTooltipBMD>>
    {
        public const int CategoryCount = 21;
        public const int ItemsPerCategory = 512;

        public const int RecordCount =
            CategoryCount * ItemsPerCategory;

        public const int RecordSize = 136;

        public const int ChecksumSize = 4;

        public const int ExpectedFileSize =
            (RecordCount * RecordSize) + ChecksumSize;

        protected override List<ItemTooltipBMD> Read(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length != ExpectedFileSize)
            {
                throw new InvalidDataException(
                    $"Invalid itemtooltip.bmd size. " +
                    $"Expected {ExpectedFileSize:N0} bytes, " +
                    $"but received {buffer.Length:N0} bytes.");
            }

            var result =
                new List<ItemTooltipBMD>(RecordCount);

            using var stream =
                new MemoryStream(buffer, writable: false);

            using var reader =
                new BinaryReader(stream);

            for (int recordIndex = 0;
                 recordIndex < RecordCount;
                 recordIndex++)
            {
                byte[] encryptedRecord =
                    reader.ReadBytes(RecordSize);

                if (encryptedRecord.Length != RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected end of itemtooltip.bmd " +
                        $"while reading record {recordIndex}.");
                }

                // Each record resets the FC CF AB XOR sequence.
                byte[] record =
                    BuxCryptor.Convert(encryptedRecord);

                result.Add(
                    ReadRecord(record, recordIndex));
            }

            // Final 4 bytes are the file checksum.
            // We don't validate it yet.
            byte[] checksum =
                reader.ReadBytes(ChecksumSize);

            if (checksum.Length != ChecksumSize)
            {
                throw new EndOfStreamException(
                    "itemtooltip.bmd is missing its trailing checksum.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected data after itemtooltip.bmd checksum.");
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
                    $"Tooltip record {recordIndex} " +
                    $"has an invalid size.");
            }

            using var stream =
                new MemoryStream(record, writable: false);

            using var reader =
                new BinaryReader(stream);

            // byte 0
            byte category =
                reader.ReadByte();

            // byte 1:
            // compiler padding before WORD Index.
            _ = reader.ReadByte();

            // bytes 2-3
            ushort index =
                reader.ReadUInt16();

            // bytes 4-67
            byte[] nameBytes =
                reader.ReadBytes(64);

            string name =
                MuTooltipEncoding
                    .DecodeNullTerminated(nameBytes);

            // bytes 68-75
            short packedNameStyle =
                reader.ReadInt16();

            short packedFlags =
                reader.ReadInt16();

            short packedRenderFlags =
                reader.ReadInt16();

            short itemLevel =
                reader.ReadInt16();

            var lines =
                new ItemTooltipLineBMD[
                    ItemTooltipBMD.TextEntryCount];

            for (int i = 0;
                i < ItemTooltipBMD.TextEntryCount;
                i++)
            {
                short textId =
                    reader.ReadInt16();

                short packedStyle =
                    reader.ReadInt16();

                lines[i] =
                    new ItemTooltipLineBMD
                    {
                        TextId = textId,
                        PackedStyle = packedStyle
                    };
            }

            if (reader.BaseStream.Position != RecordSize)
            {
                throw new InvalidDataException(
                    $"Tooltip record {recordIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new ItemTooltipBMD
            {
                Category = category,
                Index = index,
                Name = name,

                PackedNameStyle = packedNameStyle,
                PackedFlags = packedFlags,
                PackedRenderFlags = packedRenderFlags,

                ItemLevel = itemLevel,

                Lines = lines
            };
        }
    }
}