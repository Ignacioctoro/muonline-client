using System;
using System.Buffers.Binary;
using System.Text;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using MUnique.OpenMU.Network;

namespace Client.Main.Networking.Services;

public partial class CharacterService
{
    public MessengerState Messenger { get; } = new();

    public async Task SendMessengerPacketAsync(byte[] packet)
    {
        try
        {
            if (!_connectionManager.IsConnected) throw new InvalidOperationException("Disconnected from the game server.");
            await _connectionManager.Connection.SendAsync(() =>
            {
                packet.AsSpan().CopyTo(_connectionManager.Connection.Output.GetSpan(packet.Length));
                return packet.Length;
            });
        }
        catch (Exception)
        {
            MuGame.ScheduleOnMainThread(() => Messenger.Notice = "Could not send the request. Check your connection.");
        }
    }

    public Task SendFriendActionAsync(byte code, string name)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0 || Encoding.UTF8.GetByteCount(name) > 10 || name.IndexOf('\0') >= 0)
        { Messenger.Notice = "Enter a valid character name (maximum 10 bytes)."; return Task.CompletedTask; }
        var packet = new byte[13]; packet[0] = 0xC1; packet[1] = 13; packet[2] = code;
        Encoding.UTF8.GetBytes(name, packet.AsSpan(3));
        Messenger.Notice = "Request sent; waiting for the server.";
        return SendMessengerPacketAsync(packet);
    }

    public Task AnswerFriendAsync(string name, bool accept)
    {
        var packet = new byte[14]; packet[0] = 0xC1; packet[1] = 14; packet[2] = 0xC2;
        packet[3] = accept ? (byte)1 : (byte)0;
        Encoding.UTF8.GetBytes(name, packet.AsSpan(4, 10));
        return SendMessengerPacketAsync(packet);
    }

    public Task SetMessengerVisibleAsync(bool online)
    {
        Messenger.RefuseChat = !online;
        return SendMessengerPacketAsync(new byte[] { 0xC1, 4, 0xC4, online ? (byte)1 : (byte)0 });
    }

    public Task RequestLetterAsync(ushort index, bool delete = false)
    {
        var packet = new byte[] { 0xC1, 6, delete ? (byte)0xC8 : (byte)0xC7, 0, 0, 0 };
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4), index);
        Messenger.Notice = delete ? "Deleting letter..." : "Reading letter...";
        return SendMessengerPacketAsync(packet);
    }

    public Task SendLetterAsync(string receiver, string title, string message)
    {
        receiver = receiver.Trim(); title = title.Trim();
        if (receiver.Length == 0 || Encoding.UTF8.GetByteCount(receiver) > 10 ||
            title.Length == 0 || Encoding.UTF8.GetByteCount(title) > 59 ||
            string.IsNullOrWhiteSpace(message) || Encoding.UTF8.GetByteCount(message) > 1000 ||
            receiver.Contains('\0') || title.Contains('\0') || message.Contains('\0'))
        { Messenger.Notice = "Check recipient, subject (59 bytes) and message (1000 bytes)."; return Task.CompletedTask; }
        byte[] body = Encoding.UTF8.GetBytes(message);
        var packet = new byte[83 + body.Length]; packet[0] = 0xC4; packet[3] = 0xC5;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(1), (ushort)packet.Length);
        uint id = ++Messenger.LastLetterRequest;
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(4), id);
        Encoding.UTF8.GetBytes(receiver, packet.AsSpan(8, 10));
        Encoding.UTF8.GetBytes(title, packet.AsSpan(18, 60));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(80), (ushort)body.Length);
        body.CopyTo(packet, 82);
        Messenger.Notice = "Sending letter...";
        return SendMessengerPacketAsync(packet);
    }
}
