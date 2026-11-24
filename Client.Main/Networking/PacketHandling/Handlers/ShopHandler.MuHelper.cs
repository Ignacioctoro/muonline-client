using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Client.Main.Controls.UI.Game;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [PacketHandler(0xBF, 0x51)]
    public Task HandleMuHelperStatusAsync(Memory<byte> packet)
    {
        if (packet.Length != 16 || packet.Span[0] != 0xC1) return Task.CompletedTask;
        string owner = _characterState.Name;
        int characterClass = (int)_characterState.Class;
        bool consumeMoney = packet.Span[4] != 0, paused = packet.Span[12] != 0;
        uint money = BinaryPrimitives.ReadUInt32LittleEndian(packet.Span.Slice(8, 4));
        MuGame.ScheduleOnMainThread(() =>
        {
            if (_characterState.Name == owner && (int)_characterState.Class == characterClass
                && MuGame.Instance.ActiveScene is Client.Main.Scenes.GameScene scene)
                scene.ReceiveMuHelperStatus(owner, characterClass, consumeMoney, paused, money);
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0xAE, 0xFF)]
    public Task HandleMuHelperConfigurationAsync(Memory<byte> packet)
    {
        if (packet.Length != 261 || packet.Span[0] != 0xC2) return Task.CompletedTask;
        string owner = _characterState.Name;
        int characterClass = (int)_characterState.Class;
        byte[] configuration = packet.Span.Slice(4, MuHelperConfigurationData.Length).ToArray();
        MuGame.ScheduleOnMainThread(() => MuHelperControl.Instance.Receive(owner, characterClass, configuration));
        return Task.CompletedTask;
    }
}
