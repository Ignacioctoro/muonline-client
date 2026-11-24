using System;
using System.Buffers.Binary;
using System.Text;

namespace Client.Main.Core.Client;

/// <summary>OpenMU's 257-byte per-character helper configuration. Unknown bytes are preserved.</summary>
public sealed class MuHelperConfigurationData
{
    public const int Length = 257;
    public static readonly int[] SkillOffsets = { 5, 7, 11, 17, 19, 21 };
    private readonly byte[] _data;

    public MuHelperConfigurationData(byte[] data)
    {
        if (data == null || data.Length != Length) throw new ArgumentException("Invalid MU Helper data.", nameof(data));
        _data = (byte[])data.Clone();
    }

    public static MuHelperConfigurationData Defaults()
    {
        var result = new MuHelperConfigurationData(new byte[Length]);
        result._data[1] = 1;
        result._data[2] = 0x11;
        result._data[23] = result._data[24] = 0x55;
        result.SetWord(3, 10);
        result.SetWord(15, 30);
        result.SetFlag(25, 0, true);
        return result;
    }

    public byte[] ToArray() => (byte[])_data.Clone();
    public bool Flag(int offset, int bit) => (_data[offset] & (1 << bit)) != 0;
    public void SetFlag(int offset, int bit, bool value) => _data[offset] = (byte)(value ? _data[offset] | (1 << bit) : _data[offset] & ~(1 << bit));
    public int Word(int offset) => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(offset, 2));
    public void SetWord(int offset, int value) => BinaryPrimitives.WriteUInt16LittleEndian(_data.AsSpan(offset, 2), (ushort)Math.Clamp(value, 0, ushort.MaxValue));
    public int Bits(int offset, int shift, int mask) => (_data[offset] >> shift) & mask;
    public void SetBits(int offset, int shift, int mask, int value) => _data[offset] = (byte)((_data[offset] & ~(mask << shift)) | ((value & mask) << shift));

    // Match MuHelperSettingsSerializer in the active OpenMU branch: twelve 15-byte ASCII filters.
    public string ItemName(int index)
    {
        int start = 65 + index * 15, count = 0;
        while (count < 15 && _data[start + count] != 0) count++;
        return Encoding.ASCII.GetString(_data, start, count);
    }
    public void SetItemName(int index, string value)
    {
        value ??= string.Empty;
        // Imported configurations may fill the entire 15-byte slot without a terminator.
        // The editor still limits new names to 14 characters; preserve existing full slots.
        if (value.Length > 15) throw new ArgumentException("Item filter is too long.");
        foreach (char c in value) if (c > 127 || c == '\0') throw new ArgumentException("Use ASCII characters for item filters.");
        int start = 65 + index * 15;
        Array.Clear(_data, start, 15);
        Encoding.ASCII.GetBytes(value).CopyTo(_data, start);
    }
}
