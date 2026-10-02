using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Reader for classic MU Data/Local/text.bmd.
    ///
    /// Format used by the supplied Season 6 file:
    ///
    /// WORD  Magic      = 0x5447 ("GT")
    /// DWORD EntryCount
    ///
    /// Repeated EntryCount times:
    /// DWORD Id
    /// DWORD TextLength
    /// BYTE  EncryptedText[TextLength]
    ///
    /// Only the text bytes are encrypted.
    /// The FC / CF / AB XOR sequence restarts for every entry.
    /// </summary>
    public sealed class LegacyGlobalTextBMDReader
        : BaseReader<Dictionary<int, string>>
    {
        private const ushort ExpectedMagic =
            0x5447;

        protected override Dictionary<int, string> Read(
            byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(
                    nameof(buffer));
            }

            if (buffer.Length < 6)
            {
                throw new InvalidDataException(
                    "Global text.bmd is too small.");
            }

            using var stream =
                new MemoryStream(
                    buffer,
                    writable: false);

            using var reader =
                new BinaryReader(stream);

            ushort magic =
                reader.ReadUInt16();

            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException(
                    $"Invalid text.bmd magic. " +
                    $"Expected 0x{ExpectedMagic:X4}, " +
                    $"received 0x{magic:X4}.");
            }

            int entryCount =
                reader.ReadInt32();

            if (entryCount < 0)
            {
                throw new InvalidDataException(
                    $"Invalid text.bmd entry count: " +
                    $"{entryCount}.");
            }

            var result =
                new Dictionary<int, string>(
                    entryCount);

            for (int recordIndex = 0;
                 recordIndex < entryCount;
                 recordIndex++)
            {
                if (reader.BaseStream.Length -
                    reader.BaseStream.Position < 8)
                {
                    throw new EndOfStreamException(
                        $"Unexpected end of text.bmd " +
                        $"before record {recordIndex}.");
                }

                int id =
                    reader.ReadInt32();

                int textLength =
                    reader.ReadInt32();

                if (textLength < 0)
                {
                    throw new InvalidDataException(
                        $"Invalid text length " +
                        $"{textLength} for global text " +
                        $"record {recordIndex}.");
                }

                long remaining =
                    reader.BaseStream.Length -
                    reader.BaseStream.Position;

                if (textLength > remaining)
                {
                    throw new EndOfStreamException(
                        $"Global text record {recordIndex} " +
                        $"requests {textLength} bytes, but " +
                        $"only {remaining} remain.");
                }

                byte[] encryptedText =
                    reader.ReadBytes(
                        textLength);

                DecryptText(
                    encryptedText);

                string text =
                    MuTooltipEncoding
                        .DecodeNullTerminated(
                            encryptedText);

                result[id] =
                    text;
            }

            if (reader.BaseStream.Position !=
                reader.BaseStream.Length)
            {
                throw new InvalidDataException(
                    $"Unexpected trailing data in text.bmd: " +
                    $"{reader.BaseStream.Length - reader.BaseStream.Position} bytes.");
            }

            return result;
        }

        private static void DecryptText(
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
    }
}
