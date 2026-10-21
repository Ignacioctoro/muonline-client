using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public StoreCurrencyState StoreCurrencies { get; } = new();
    private DateTime _lastBalanceRequest;
    public async Task RequestStoreBalancesAsync()
    {
        if (!_connectionManager.IsConnected || DateTime.UtcNow - _lastBalanceRequest < TimeSpan.FromSeconds(2)) return;
        _lastBalanceRequest = DateTime.UtcNow;
        try
        {
            await _connectionManager.Connection.SendAsync(() =>
            {
                var data = _connectionManager.Connection.Output.GetSpan(4);
                data[0] = 0xC1; data[1] = 4; data[2] = 0xD2; data[3] = 1;
                return 4;
            });
        }
        catch { /* No estimated balance: the next inventory refresh retries. */ }
    }
    public async Task<bool> SendPlayerShopCurrencyPriceAsync(byte slot, uint amount, ushort type)
    {
        if (type == 0) return await SendPlayerShopSetItemPriceAsync(slot, amount);
        if (!_connectionManager.IsConnected || !StoreCurrencies.IsAllowed(type) ||
            slot < 204 || slot > 235 || amount > int.MaxValue ||
            (StoreCurrencyState.IsItem(type) && amount > ushort.MaxValue)) return false;
        try
        {
            await _connectionManager.Connection.SendAsync(() =>
            {
                var data = _connectionManager.Connection.Output.GetSpan(13).Slice(0, 13);
                data.Clear(); data[0] = 0xC3; data[1] = 13; data[2] = 0x3F; data[3] = 9;
                // A zero amount clears the price using the unambiguous Zen encoding.
                bool item = amount > 0 && StoreCurrencyState.IsItem(type);
                BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(4, 4), item ? 0 : amount);
                BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(8, 2), amount == 0 ? (ushort)0 : type);
                BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(10, 2), item ? (ushort)amount : (ushort)0);
                data[12] = slot; return 13;
            });
            return true;
        }
        catch { return false; }
    }
}
