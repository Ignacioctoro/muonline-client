using System;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Main.Controls.UI.Game;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;

namespace Client.Main.Networking.PacketHandling.Handlers;

public partial class ShopHandler
{
    [PacketHandler(0xD2, 0x02)]
    public Task HandleCashShopOpenAsync(Memory<byte> packet)
    {
        if (packet.Length != 5) return Task.CompletedTask;
        bool allowed = packet.Span[4] != 0;
        MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.OpenResult(allowed));
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0xE0)]
    public Task HandleCashShopCatalogAsync(Memory<byte> packet)
    {
        if (packet.Length < 5 || packet.Span[0] != 0xC2) return Task.CompletedTask;
        try
        {
            var page = JsonSerializer.Deserialize<CashShopCatalogPage>(packet.Span[5..]);
            if (page != null) MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.SetCatalog(page));
        }
        catch (JsonException) { }
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0xE1)]
    public Task HandleCashShopProductAsync(Memory<byte> packet)
    {
        if (packet.Length < 5 || packet.Span[0] != 0xC2) return Task.CompletedTask;
        try
        {
            var reply = JsonSerializer.Deserialize<CashShopProductReply>(packet.Span[5..]);
            if (reply != null) MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.SetProduct(reply));
        }
        catch (JsonException) { }
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0x03)]
    public Task HandleCashShopBuyAsync(Memory<byte> packet)
    {
        if (packet.Length != 9) return Task.CompletedTask;
        byte result = packet.Span[4];
        MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.BuyResult(result));
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0x0B)]
    public Task HandleCashShopUseAsync(Memory<byte> packet)
    {
        if (packet.Length != 5) return Task.CompletedTask;
        byte result = packet.Span[4];
        MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.UseResult(result));
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0x06)]
    public Task HandleCashShopStoragePageAsync(Memory<byte> packet)
    {
        if (packet.Length != 12) return Task.CompletedTask;
        int count = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[6..]);
        int page = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[8..]);
        int pages = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[10..]);
        if (count > 9 || page < 1 || pages < page) return Task.CompletedTask;
        MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.BeginStorage(count, page, pages));
        return Task.CompletedTask;
    }

    [PacketHandler(0xD2, 0x0D)]
    public Task HandleCashShopStorageItemAsync(Memory<byte> packet) => ReadCashShopStorage(packet, false);

    [PacketHandler(0xD2, 0x0E)]
    public Task HandleCashShopGiftItemAsync(Memory<byte> packet) => ReadCashShopStorage(packet, true);

    private static Task ReadCashShopStorage(Memory<byte> packet, bool gift)
    {
        if (packet.Length < (gift ? 244 : 33)) return Task.CompletedTask;
        var data = packet.Span;
        var entry = new CashShopStorageEntry
        {
            Index = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]),
            ItemSequence = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]),
            Sequence = BinaryPrimitives.ReadInt32LittleEndian(data[16..]),
            Option = BinaryPrimitives.ReadInt32LittleEndian(data[20..]),
            Sender = gift ? Encoding.ASCII.GetString(data.Slice(33, 11)).TrimEnd('\0', ' ') : string.Empty,
        };
        MuGame.ScheduleOnMainThread(() => CashShopControl.Instance.AddStorage(entry, gift));
        return Task.CompletedTask;
    }
}
