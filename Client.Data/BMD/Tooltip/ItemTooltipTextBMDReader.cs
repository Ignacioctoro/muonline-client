namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for Data/Local/itemtooltiptext.bmd.
    ///
    /// Modern MU format:
    ///
    /// DWORD record count
    /// N * 260-byte encrypted records
    /// DWORD checksum
    ///
    /// Unlike itemtooltip.bmd, records use Xor3Byte2:
    ///
    /// byte ^= FC/CF/AB
    /// byte ^= low byte of 0xE2F1
    ///
    /// Low byte of 0xE2F1 = F1.
    /// </summary>
    public sealed class ItemTooltipTextBMDReader
        : BaseReader<List<ItemTooltipTextBMD>>
    {
        public const int RecordSize = 260;

        public const int HeaderSize = 4;

        public const int ChecksumSize = 4;

        public const ushort EncryptionWordKey = 0xE2F1;

        protected override List<ItemTooltipTextBMD> Read(
            byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length <
                HeaderSize + ChecksumSize)
            {
                throw new InvalidDataException(
                    "itemtooltiptext.bmd is too small.");
            }

            using var stream =
                new MemoryStream(buffer, writable: false);

            using var reader =
                new BinaryReader(stream);

            uint declaredCount =
                reader.ReadUInt32();

            long encryptedDataSize =
                buffer.Length
                - HeaderSize
                - ChecksumSize;

            if (encryptedDataSize % RecordSize != 0)
            {
                throw new InvalidDataException(
                    $"Invalid itemtooltiptext.bmd data size. " +
                    $"{encryptedDataSize:N0} bytes cannot be divided " +
                    $"into {RecordSize}-byte records.");
            }

            int physicalRecordCount =
                checked((int)(
                    encryptedDataSize / RecordSize));

            if (declaredCount != physicalRecordCount)
            {
                throw new InvalidDataException(
                    $"itemtooltiptext.bmd count mismatch. " +
                    $"Header says {declaredCount}, " +
                    $"but file contains {physicalRecordCount} records.");
            }

            var result =
                new List<ItemTooltipTextBMD>(
                    physicalRecordCount);

            for (int recordIndex = 0;
                 recordIndex < physicalRecordCount;
                 recordIndex++)
            {
                byte[] encrypted =
                    reader.ReadBytes(RecordSize);

                if (encrypted.Length != RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected end of itemtooltiptext.bmd " +
                        $"at record {recordIndex}.");
                }

                DecryptRecord(encrypted);

                result.Add(
                    ReadRecord(
                        encrypted,
                        recordIndex));
            }

            // Read but do not validate the CRC yet.
            byte[] checksum =
                reader.ReadBytes(ChecksumSize);

            if (checksum.Length != ChecksumSize)
            {
                throw new EndOfStreamException(
                    "itemtooltiptext.bmd is missing its checksum.");
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    "Unexpected additional data at the end " +
                    "of itemtooltiptext.bmd.");
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

            byte secondaryKey =
                (byte)(EncryptionWordKey & 0xFF);

            for (int i = 0;
                 i < data.Length;
                 i++)
            {
                data[i] ^=
                    xorKey[i % xorKey.Length];

                data[i] ^=
                    secondaryKey;
            }
        }

        private static ItemTooltipTextBMD ReadRecord(
            byte[] record,
            int recordIndex)
        {
            if (record.Length != RecordSize)
            {
                throw new InvalidDataException(
                    $"Tooltip text record {recordIndex} " +
                    $"has invalid size.");
            }

            using var stream =
                new MemoryStream(record, writable: false);

            using var reader =
                new BinaryReader(stream);

            ushort id =
                reader.ReadUInt16();

            byte[] textBytes =
                reader.ReadBytes(256);

            string text =
                MuTooltipEncoding
                    .DecodeNullTerminated(textBytes);

            short type =
                reader.ReadInt16();

            if (reader.BaseStream.Position != RecordSize)
            {
                throw new InvalidDataException(
                    $"Tooltip text record {recordIndex} ended at " +
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