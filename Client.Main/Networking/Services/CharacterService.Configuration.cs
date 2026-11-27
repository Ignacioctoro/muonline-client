using System;
using System.Threading.Tasks;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public async Task<bool> SaveHudConfigurationAsync(byte[] configuration, string owner, int session)
    {
        if (!_connectionManager.IsConnected || configuration == null || configuration.Length > 251) return false;
        var connection = _connectionManager.Connection;
        bool sent = false;
        try
        {
            await connection.SendAsync(() =>
            {
                var state = MuGame.Network?.GetCharacterState();
                if (state == null || !state.IsInGame || state.Name != owner || state.SkillSessionRevision != session
                    || _connectionManager.Connection != connection) return 0;
                int length = configuration.Length + 4;
                var packet = connection.Output.GetSpan(length)[..length];
                packet[0] = 0xC1; packet[1] = (byte)length; packet[2] = 0xF3; packet[3] = 0x30;
                configuration.CopyTo(packet[4..]); sent = true;
                return length;
            });
        }
        catch { return false; }
        return sent;
    }
}
