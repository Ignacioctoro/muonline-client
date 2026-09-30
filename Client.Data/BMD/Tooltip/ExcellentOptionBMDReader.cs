using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for modern MU excellent option BMD files.
    ///
    /// Used by:
    /// - excellentcommonoption.bmd
    /// - excellentwingoption.bmd
    ///
    /// Format:
    ///
    /// DWORD count
    /// count * 120-byte encrypted records
    /// DWORD CRC
    ///
    /// Records use the standard MU XOR3 encryption:
    /// FC CF AB
    /// </summary>
    public sealed class ExcellentOptionBMDReader
        : BaseReader<List<ExcellentOptionBMD>>
    {
        public const int HeaderSize = 4;
        public const int RecordSize = 120;
        public const int ChecksumSize = 4;

        protected override List<ExcellentOptionBMD> Read(
            byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(
                    nameof(buffer));
            }

            if (buffer.Length <
                HeaderSize + ChecksumSize)
            {
                throw new InvalidDataException(
                    "Excellent option BMD is too small.");
            }

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            uint declaredCount =
                reader.ReadUInt32();

            long dataSize =
                buffer.Length -
                HeaderSize -
                ChecksumSize;

            if (dataSize % RecordSize != 0)
            {
                throw new InvalidDataException(
                    $"Invalid excellent option BMD size. " +
                    $"{dataSize} data bytes cannot be divided " +
                    $"into {RecordSize}-byte records.");
            }

            int physicalCount =
                checked(
                    (int)(dataSize / RecordSize));

            if (declaredCount != physicalCount)
            {
                throw new InvalidDataException(
                    $"Excellent option count mismatch. " +
                    $"Header={declaredCount}, " +
                    $"physical={physicalCount}.");
            }

            var result =
                new List<ExcellentOptionBMD>(
                    physicalCount);

            for (int i = 0;
                 i < physicalCount;
                 i++)
            {
                byte[] encrypted =
                    reader.ReadBytes(
                        RecordSize);

                if (encrypted.Length !=
                    RecordSize)
                {
                    throw new EndOfStreamException(
                        $"Unexpected EOF at excellent " +
                        $"option record {i}.");
                }

                // IMPORTANT:
                //
                // Unlike itemtooltiptext.bmd,
                // ExcellentOption uses only XOR3.
                //
                // Do NOT apply the 0xE2F1 secondary key.
                byte[] record =
                    BuxCryptor.Convert(
                        encrypted);

                result.Add(
                    ReadRecord(
                        record,
                        i));
            }

            byte[] checksum =
                reader.ReadBytes(
                    ChecksumSize);

            if (checksum.Length !=
                ChecksumSize)
            {
                throw new EndOfStreamException(
                    "Excellent option BMD is missing CRC.");
            }

            return result;
        }

        private static ExcellentOptionBMD ReadRecord(
            byte[] record,
            int recordIndex)
        {
            using var stream =
                new MemoryStream(
                    record,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            byte category =
                reader.ReadByte();

            byte number =
                reader.ReadByte();

            byte[] nameBytes =
                reader.ReadBytes(100);

            string name =
                MuTooltipEncoding
                    .DecodeNullTerminated(
                        nameBytes);

            byte @operator =
                reader.ReadByte();

            // C++ alignment padding before int.
            _ = reader.ReadByte();

            int value =
                reader.ReadInt32();

            int damage =
                reader.ReadInt32();

            byte zen =
                reader.ReadByte();

            byte damageChance =
                reader.ReadByte();

            byte offense =
                reader.ReadByte();

            byte defense =
                reader.ReadByte();

            byte life =
                reader.ReadByte();

            byte mana =
                reader.ReadByte();

            byte other =
                reader.ReadByte();

            // Final C++ struct alignment byte.
            _ = reader.ReadByte();

            if (reader.BaseStream.Position !=
                RecordSize)
            {
                throw new InvalidDataException(
                    $"Excellent option record " +
                    $"{recordIndex} ended at " +
                    $"{reader.BaseStream.Position}, " +
                    $"expected {RecordSize}.");
            }

            return new ExcellentOptionBMD
            {
                Category = category,
                Number = number,
                Name = name,

                Operator = @operator,

                Value = value,
                Damage = damage,

                Zen = zen,
                DamageChance = damageChance,
                Offense = offense,
                Defense = defense,
                Life = life,
                Mana = mana,
                Other = other
            };
        }
    }
}