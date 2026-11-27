using System;
using System.Threading.Tasks;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public Task<bool> RequestGensInfoAsync() => SendGensRequestAsync(0x0B, 0, false);
    public Task<bool> JoinGensAsync(byte family) => family is 1 or 2 ? SendGensRequestAsync(1, family, true) : Task.FromResult(false);
    public Task<bool> LeaveGensAsync() => SendGensRequestAsync(3, 0, false);
    public Task<bool> RequestGensRewardAsync(byte family) => family is 1 or 2 ? SendGensRequestAsync(9, family, true) : Task.FromResult(false);

    private async Task<bool> SendGensRequestAsync(byte operation, byte family, bool includeFamily)
    {
        var state = MuGame.Network?.GetCharacterState();
        if (!_connectionManager.IsConnected || state?.IsInGame != true) return false;
        int session = state.SkillSessionRevision;
        var connection = _connectionManager.Connection;
        bool sent = false;
        try
        {
            await connection.SendAsync(() =>
            {
                if (state.SkillSessionRevision != session || !state.IsInGame || _connectionManager.Connection != connection) return 0;
                int length = includeFamily ? 5 : 4;
                var packet = connection.Output.GetSpan(length)[..length];
                packet[0] = 0xC1; packet[1] = (byte)length; packet[2] = 0xF8; packet[3] = operation;
                if (includeFamily) packet[4] = family;
                sent = true; return length;
            });
        }
        catch { return false; }
        return sent;
    }
}
