using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Client.Main.Core.Utilities;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [PacketHandler(0x3F, 0x0A)]
    public Task HandleStoreCurrenciesAsync(Memory<byte> packet)
    {
        var data = packet.Span;
        if (data.Length < 5 || data[0] != 0xC1 || data[1] != data.Length || data.Length != 5 + data[4] * 2) return Task.CompletedTask;
        var types = new ushort[data[4]];
        for (int i = 0; i < types.Length; i++) types[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(5 + i * 2, 2));
        MuGame.ScheduleOnMainThread(() => _networkManager.GetCharacterService().StoreCurrencies.SetAllowed(types));
        return Task.CompletedTask;
    }
    [PacketHandler(0xD2, 0x01)]
    public Task HandleAccountBalancesAsync(Memory<byte> packet)
    {
        var data = packet.Span;
        if (data.Length != 45 || data[0] != 0xC1 || data[1] != 45) return Task.CompletedTask;
        double wc = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data.Slice(13, 8)));
        double wp = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data.Slice(21, 8)));
        double gp = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data.Slice(37, 8)));
        if (!Valid(wc) || !Valid(wp) || !Valid(gp)) return Task.CompletedTask;
        MuGame.ScheduleOnMainThread(() => _networkManager.GetCharacterService().StoreCurrencies.SetBalances((long)wc, (long)wp, (long)gp));
        return Task.CompletedTask;
    }
    private static bool Valid(double value) => double.IsFinite(value) && value >= 0 && value <= int.MaxValue && Math.Truncate(value) == value;
}
