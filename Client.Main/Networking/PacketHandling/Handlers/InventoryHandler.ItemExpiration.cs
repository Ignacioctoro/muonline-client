using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Client.Main.Core.Utilities;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class InventoryHandler
{
    [PacketHandler(0xD2, 0x11)]
    public Task HandlePeriodItemListAsync(Memory<byte> packet)
    {
        // This is a header-only reset marker, despite the original packet's name.
        if (packet.Length == 4 && packet.Span[0] == 0xC1 && packet.Span[1] == 4)
            _characterState.ClearItemExpirationDates();
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0x12)]
    public Task HandlePeriodItemInfoAsync(Memory<byte> packet)
    {
        // Bytes 4..5 are reserved. The slot is ushort at 6, not byte at 4.
        if (packet.Length != 12 || packet.Span[0] != 0xC1 || packet.Span[1] != 12)
            return Task.CompletedTask;
        ushort slot = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[6..]);
        uint expiresAt = BinaryPrimitives.ReadUInt32LittleEndian(packet.Span[8..]);
        _characterState.SetItemExpiration(slot, expiresAt);
        return Task.CompletedTask;
    }
}
