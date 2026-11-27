using System;
using System.Threading.Tasks;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [Client.Main.Core.Utilities.PacketHandler(0xF3, 0x30)]
    public Task HandleKeyConfigurationAsync(Memory<byte> packet)
    {
        if (packet.Length >= 34 && packet.Span[0] == 0xC1 && packet.Span[1] == packet.Length)
            _characterState.ReceiveKeyConfiguration(packet.Span[4..].ToArray());
        return Task.CompletedTask;
    }
}
