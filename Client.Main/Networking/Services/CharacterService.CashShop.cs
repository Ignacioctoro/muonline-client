using System;
using System.Buffers.Binary;
using System.Threading.Tasks;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public Task<bool> SetCashShopOpenAsync(bool open) => SendCashShopAsync(0x02, new byte[] { open ? (byte)0 : (byte)1 });
    public Task<bool> RequestCashShopBalancesAsync() => SendCashShopAsync(0x01, Array.Empty<byte>());

    public Task<bool> RequestCashShopCatalogAsync(int request, int page, byte filter, int category = 0)
    {
        byte[] data = new byte[13];
        BinaryPrimitives.WriteInt32LittleEndian(data, request);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), page);
        data[8] = filter;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(9), category);
        return SendCashShopAsync(0xE0, data);
    }

    public Task<bool> RequestCashShopProductAsync(int request, int sequence, int option)
    {
        byte[] data = new byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(data, request);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), sequence);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), option);
        return SendCashShopAsync(0xE1, data);
    }

    public Task<bool> RequestCashShopStorageAsync(int page, bool gifts)
    {
        byte[] data = new byte[6];
        BinaryPrimitives.WriteInt32LittleEndian(data, page);
        data[4] = (byte)(gifts ? 'G' : 'S');
        return SendCashShopAsync(0x05, data);
    }

    public Task<bool> BuyCashShopProductAsync(int package, int option, int coin)
    {
        if (coin is < 0 or > 2) return Task.FromResult(false);
        byte[] data = new byte[19];
        BinaryPrimitives.WriteInt32LittleEndian(data, package);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8), option);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(14), coin == 0 ? 508u : coin == 1 ? 509u : 0u);
        return SendCashShopAsync(0x03, data);
    }

    public Task<bool> UseCashShopProductAsync(uint index, uint itemSequence)
    {
        byte[] data = new byte[11];
        BinaryPrimitives.WriteUInt32LittleEndian(data, index);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), itemSequence);
        data[10] = (byte)'P';
        return SendCashShopAsync(0x0B, data);
    }

    private async Task<bool> SendCashShopAsync(byte subCode, byte[] payload)
    {
        if (!_connectionManager.IsConnected) return false;
        try
        {
            var connection = _connectionManager.Connection;
            await connection.SendAsync(() =>
            {
                int length = 4 + payload.Length;
                var data = connection.Output.GetSpan(length)[..length];
                data[0] = 0xC1;
                data[1] = (byte)length;
                data[2] = 0xD2;
                data[3] = subCode;
                payload.CopyTo(data[4..]);
                return length;
            });
            return true;
        }
        catch { return false; }
    }
}
