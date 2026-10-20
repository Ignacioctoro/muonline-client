using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Main.Core.Client;

/// <summary>A room uses its own unencrypted TCP connection; only token/message payloads use XOR3.</summary>
public sealed class MessengerChatRoom : IDisposable
{
    private readonly TcpClient _client = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Dictionary<byte, string> _names = new();
    private volatile bool _disposed;
    private NetworkStream _stream;
    public ushort Id { get; }
    public string FriendName { get; }
    public string Status { get; private set; } = "Connecting...";
    public bool Connected { get; private set; }
    public List<string> Messages { get; } = new();
    public int Revision { get; private set; }
    public MessengerChatRoom(ushort id, string name) { Id = id; FriendName = name; }
    private void OnUi(Action action) => MuGame.ScheduleOnMainThread(() => { if (!_disposed) { action(); Revision++; } });
    private static void Xor(byte[] packet, int start)
    {
        for (int i = start; i < packet.Length; i++) packet[i] ^= (byte)(((i - start) % 3) switch { 0 => 0xFC, 1 => 0xCF, _ => 0xAB });
    }
    private static string Text(byte[] p, int start, int length)
    {
        int end = Array.IndexOf(p, (byte)0, start, length);
        return Encoding.UTF8.GetString(p, start, end < 0 ? length : end - start);
    }
    public async Task ConnectAsync(string host, int port, uint token)
    {
        Task keepAlive = Task.CompletedTask;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            await _client.ConnectAsync(host, port, timeout.Token);
            _stream = _client.GetStream();
            var auth = new byte[16]; auth[0] = 0xC1; auth[1] = 16;
            BinaryPrimitives.WriteUInt16LittleEndian(auth.AsSpan(4), Id);
            Encoding.ASCII.GetBytes(token.ToString(CultureInfo.InvariantCulture), auth.AsSpan(6, 10));
            Xor(auth, 6); await WriteAsync(auth);
            OnUi(() => Status = "Authenticating...");
            keepAlive = KeepAliveAsync();
            while (!_stop.IsCancellationRequested)
            {
                var header = new byte[3]; await ReadExactlyAsync(header, 0, 2);
                int length;
                if (header[0] == 0xC1) length = header[1];
                else if (header[0] == 0xC2) { await ReadExactlyAsync(header, 2, 1); length = (header[1] << 8) | header[2]; }
                else throw new IOException("Invalid chat packet header.");
                int used = header[0] == 0xC1 ? 2 : 3;
                if (length < used + 1 || length > 4096) throw new IOException("Invalid chat packet size.");
                var packet = new byte[length]; Array.Copy(header, packet, used);
                await ReadExactlyAsync(packet, used, length - used);
                OnUi(() => Receive(packet));
            }
        }
        catch (Exception) when (_disposed) { }
        catch (Exception) { OnUi(() => { Connected = false; Status = "Chat disconnected. Check ChatServer and port, then reopen the chat."; }); }
        finally
        {
            _stop.Cancel(); _client.Dispose();
            try { await keepAlive; } catch (Exception) { }
        }
    }
    private async Task ReadExactlyAsync(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int read = await _stream.ReadAsync(buffer.AsMemory(offset, count), _stop.Token);
            if (read == 0) throw new EndOfStreamException();
            offset += read; count -= read;
        }
    }
    private async Task WriteAsync(byte[] packet)
    {
        await _sendLock.WaitAsync(_stop.Token);
        try { await _stream.WriteAsync(packet.AsMemory(), _stop.Token); }
        finally { _sendLock.Release(); }
    }
    private async Task KeepAliveAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
            while (await timer.WaitForNextTickAsync(_stop.Token)) await WriteAsync(new byte[] { 0xC1, 3, 5 });
        }
        catch (OperationCanceledException) { }
        catch (Exception) { _client.Dispose(); }
    }
    public async Task<bool> SendAsync(string message)
    {
        if (!Connected || _disposed || string.IsNullOrWhiteSpace(message)) return false;
        byte[] text = Encoding.UTF8.GetBytes(message);
        if (text.Length > 200 || message.Contains('\0')) { Status = "Message too long (maximum 200 bytes)."; Revision++; return false; }
        byte[] packet = new byte[text.Length + 6]; packet[0] = 0xC1; packet[1] = (byte)packet.Length; packet[2] = 4; packet[4] = (byte)(text.Length + 1);
        text.CopyTo(packet, 5); Xor(packet, 5);
        try { await WriteAsync(packet); return true; }
        catch (Exception) { OnUi(() => Status = "Message could not be sent."); return false; }
    }
    private void Receive(byte[] p)
    {
        int code = p[p[0] == 0xC2 ? 3 : 2];
        if (code == 2 && p[0] == 0xC2 && p.Length >= 8 && p.Length >= 8 + p[6] * 11)
        {
            _names.Clear();
            for (int i = 0; i < p[6]; i++) _names[p[8 + 11 * i]] = Text(p, 9 + 11 * i, 10);
            Connected = true; Status = "Connected";
        }
        else if (code == 1 && p.Length >= 15)
        {
            string name = Text(p, 5, 10);
            if (p[3] == 0) { _names[p[4]] = name; Connected = true; Status = "Connected"; Add(name + " joined."); }
            else { _names.Remove(p[4]); Add(name + " left."); }
        }
        else if (code == 4 && p.Length >= 5 && p[4] <= p.Length - 5)
        {
            Xor(p, 5);
            string name = _names.TryGetValue(p[3], out var sender) ? sender : FriendName;
            Add(name + ": " + Text(p, 5, p[4]));
        }
    }
    private void Add(string text) { Messages.Add(text); if (Messages.Count > 200) Messages.RemoveAt(0); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Connected = false; Status = "Closed"; Revision++; _stop.Cancel(); _client.Dispose();
    }
}
