using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Client.Main.Controls.UI.Game;
using Client.Main.Core.Utilities;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [PacketHandler(0xF9, 0x01)]
    public Task HandleGensNpcDialogAsync(Memory<byte> packet)
    {
        if (packet.Length != 12 || packet.Span[0] is not (0xC1 or 0xC3)) return Task.CompletedTask;
        ushort npc = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span.Slice(4, 2));
        int session = _characterState.SkillSessionRevision;
        MuGame.ScheduleOnMainThread(() =>
        {
            if (_characterState.SkillSessionRevision != session || !_characterState.IsInGame) return;
            if (npc is 543 or 544) GensControl.Instance.ShowNpc(npc);
            else GensControl.Instance.HideWithoutServerRequest();
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0xF8, 0x07)]
    public Task HandleGensInfoAsync(Memory<byte> packet)
    {
        var span = packet.Span;
        if (span.Length != 24 || span[0] != 0xC1 || span[4] > 2) return Task.CompletedTask;
        _characterState.SetGensInfo(span[4], BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12, 4)), BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(16, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20, 4)));
        return Task.CompletedTask;
    }

    [PacketHandler(0xF8, 0x02)]
    public Task HandleGensJoinAsync(Memory<byte> packet) => GensResult(packet, 1, 6);
    [PacketHandler(0xF8, 0x04)]
    public Task HandleGensLeaveAsync(Memory<byte> packet) => GensResult(packet, 3, 5);
    [PacketHandler(0xF8, 0x0A)]
    public Task HandleGensRewardAsync(Memory<byte> packet) => GensResult(packet, 9, 5);

    private Task GensResult(Memory<byte> packet, byte operation, int length)
    {
        if (packet.Length != length || packet.Span[0] != 0xC1) return Task.CompletedTask;
        byte result = packet.Span[4];
        int session = _characterState.SkillSessionRevision;
        int dialog = GensControl.Instance.DialogRevision;
        MuGame.ScheduleOnMainThread(() =>
        {
            if (_characterState.SkillSessionRevision == session)
                GensControl.Instance.ReceiveResult(operation, result, dialog);
        });
        return Task.CompletedTask;
    }

    [PacketHandler(0xF8, 0x05)]
    public Task HandleGensRelationsAsync(Memory<byte> packet)
    {
        var span = packet.Span;
        if (span.Length < 6 || span[0] != 0xC2 || span.Length != 6 + span[5] * 16) return Task.CompletedTask;
        var scope = _networkManager.GetScopeManager();
        for (int i = 0; i < span[5]; i++)
        {
            int offset = 6 + i * 16;
            if (span[offset] > 2) continue;
            ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset + 1, 2));
            scope.SetGensRelation(id, new(span[offset], BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 4, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 8, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset + 12, 4)), 0));
        }
        return Task.CompletedTask;
    }
}
