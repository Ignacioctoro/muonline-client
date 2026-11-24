using System;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public async Task<bool> SetMuHelperPausedAsync(bool paused)
    {
        if (!_connectionManager.IsConnected) return false;
        try
        {
            var connection = _connectionManager.Connection;
            await connection.SendAsync(() =>
            {
                var packet = connection.Output.GetSpan(5)[..5];
                packet[0] = 0xC1; packet[1] = 5; packet[2] = 0xBF; packet[3] = 0x51;
                packet[4] = paused ? (byte)1 : (byte)0;
                return 5;
            });
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> SaveMuHelperConfigurationAsync(byte[] configuration)
    {
        if (!_connectionManager.IsConnected || configuration?.Length != MuHelperConfigurationData.Length) return false;
        var snapshot = (byte[])configuration.Clone();
        try
        {
            var connection = _connectionManager.Connection;
            await connection.SendAsync(() =>
            {
                const int length = 261;
                var packet = connection.Output.GetSpan(length)[..length];
                packet[0] = 0xC2;
                packet[1] = 1;
                packet[2] = 5;
                packet[3] = 0xAE;
                snapshot.CopyTo(packet[4..]);
                return length;
            });
            return true;
        }
        catch { return false; }
    }
}
