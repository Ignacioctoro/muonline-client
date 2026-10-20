using System;
using System.Buffers.Binary;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Main.Core.Client.PartyMatching;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public PartyMatchingState PartyMatching { get; } = new();
    public async Task SendPartyMatchingAsync(PartyMatchingRequest request)
    {
        if (!PartyMatching.Begin()) return;
        try
        {
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(request);
            if (body.Length > 2043 || !_connectionManager.IsConnected) throw new InvalidOperationException();
            var packet = new byte[body.Length + 5]; packet[0] = 0xC4; packet[3] = 0xED; packet[4] = 1;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1), (ushort)packet.Length);
            body.CopyTo(packet, 5);
            await _connectionManager.Connection.SendAsync(() =>
            {
                packet.AsSpan().CopyTo(_connectionManager.Connection.Output.GetSpan(packet.Length));
                return packet.Length;
            });
        }
        catch (Exception)
        { MuGame.ScheduleOnMainThread(() => PartyMatching.Fail("Could not send the Party Matching request.")); }
    }
}
