using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Data.BMD.Tooltip
{
    /// <summary>
    /// Legacy Season 6 English socketitem_eng.bmd option-name table.
    ///
    /// This is NOT the same format as modern socketitem.bmd:
    /// 150 XOR-encrypted records, 104 bytes per record (15,600 bytes).
    /// Only the translated option names are used; option values always
    /// come from the 168-byte modern BMD reader.
    /// </summary>
    public sealed class LegacySocketItemEnglishBMDReader
        : BaseReader<Dictionary<int, string>>
    {
        public const int RecordSize = 104;
        public const int RecordsPerTable = 50;
        public const int TableCount = 3;
        public const int RecordCount = RecordsPerTable * TableCount;
        public const int ExpectedFileSize = RecordSize * RecordCount;

        protected override Dictionary<int, string> Read(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length != ExpectedFileSize)
                throw new InvalidDataException(
                    $"socketitem_eng.bmd expects {ExpectedFileSize} bytes; found {buffer.Length}.");

            var result = new Dictionary<int, string>();

            for (int i = 0; i < RecordCount; i++)
            {
                byte[] encrypted = new byte[RecordSize];
                Buffer.BlockCopy(buffer, i * RecordSize,
                    encrypted, 0, RecordSize);

                // As with the original game BMD reader, the FC CF AB
                // encryption restarts at each physical record.
                byte[] record = BuxCryptor.Convert(encrypted);

                using var stream = new MemoryStream(record, writable: false);
                using var reader = new BinaryReader(stream);

                int id = reader.ReadInt32();
                int element = reader.ReadInt32();
                string name = MuTooltipEncoding.DecodeNullTerminated(
                    reader.ReadBytes(40));

                int table = i / RecordsPerTable;
                if (id < 0 || id >= RecordsPerTable ||
                    string.IsNullOrWhiteSpace(name))
                    continue;

                // The element field is not needed for localization. Values
                // and bonus types must come from modern socketitem.bmd.
                _ = element;
                result.TryAdd((table << 8) | id, name.Trim());
            }

            return result;
        }
    }
}
