using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ClientToServer;

namespace Client.Main.Networking.Services
{
    public partial class CharacterService
    {
        public async Task<bool> SendLahapJewelRequestAsync(bool dismantle, byte mixId, byte level, byte sourceSlot)
        {
            if (!_connectionManager.IsConnected || mixId > 9 || level > 2 ||
                (dismantle && (sourceSlot < 12 || sourceSlot == 0xFF))) return false;
            try
            {
                await _connectionManager.Connection.SendAsync(() =>
                {
                    int length = LahapJewelMixRequest.Length;
                    var data = _connectionManager.Connection.Output.GetMemory(length).Slice(0, length);
                    data.Span.Clear();
                    var request = new LahapJewelMixRequest(data);
                    request.Operation = dismantle ? LahapJewelMixRequest.MixType.Unmix : LahapJewelMixRequest.MixType.Mix;
                    request.Item = (LahapJewelMixRequest.ItemType)mixId;
                    request.MixingStackSize = (LahapJewelMixRequest.StackSize)level;
                    request.UnmixingSourceSlot = dismantle ? sourceSlot : (byte)0;
                    return length;
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to send Lahap jewel operation.");
                return false;
            }
        }
    }
}
