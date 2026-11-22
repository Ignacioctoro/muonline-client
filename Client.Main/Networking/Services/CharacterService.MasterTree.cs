using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services
{
    public partial class CharacterService
    {
        public Task RequestCharacterProgressionAsync() => SendMasterTreeRequestAsync(0xE1, null);

        public Task RequestMasterTreeAsync() => SendMasterTreeRequestAsync(0xE0, null);

        public Task SendMasterSkillPointAsync(ushort skillId) => SendMasterTreeRequestAsync(0x52, skillId);

        private async Task SendMasterTreeRequestAsync(byte subCode, ushort? skillId)
        {
            if (!_connectionManager.IsConnected) return;
            try
            {
                var connection = _connectionManager.Connection;
                await connection.SendAsync(() =>
                {
                    int length = skillId.HasValue ? 6 : 4;
                    var data = connection.Output.GetSpan(length)[..length];
                    data[0] = 0xC1;
                    data[1] = (byte)length;
                    data[2] = 0xF3;
                    data[3] = subCode;
                    if (skillId is { } id)
                    {
                        data[4] = (byte)id;
                        data[5] = (byte)(id >> 8);
                    }
                    return length;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending master tree request.");
            }
        }
    }
}
