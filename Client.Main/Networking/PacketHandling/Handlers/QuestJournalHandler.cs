using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Controls.UI.Game.Quest;
using Microsoft.Extensions.Logging;

namespace Client.Main.Networking.PacketHandling.Handlers;

public sealed class QuestJournalHandler : IGamePacketHandler
{
    private readonly CharacterState _state;
    private readonly ILogger _logger;
    public QuestJournalHandler(ILoggerFactory factory, CharacterState state)
    {
        _state = state;
        _logger = factory.CreateLogger<QuestJournalHandler>();
    }

    [PacketHandler(0xF6, 0x0A)]
    public Task Handle0A(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x0B)]
    public Task Handle0B(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x0C)]
    public Task Handle0C(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x0D)]
    public Task Handle0D(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x0F)]
    public Task Handle0F(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x1A)]
    public Task Handle1A(Memory<byte> packet) => HandleAsync(packet);
    [PacketHandler(0xF6, 0x1B)]
    public Task Handle1B(Memory<byte> packet) => HandleAsync(packet);

    public Task HandleAsync(Memory<byte> packet)
    {
        byte[] copy = packet.ToArray();
        MuGame.ScheduleOnMainThread(() =>
        {
            try
            {
                byte sub = _state.Quests.Apply(copy);
                MyQuestsControl.Instance.OnResponse(sub);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ignored invalid Season 6 quest response.");
            }
        });
        return Task.CompletedTask;
    }
}
