using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.Main.Controls.UI;
using Client.Main.Networking.Services;

namespace Client.Main.Core.Client;

/// <summary>Messenger data is changed only on the UI thread, after packet validation.</summary>
public sealed class MessengerState
{
    public sealed class Friend
    {
        public string Name;
        public byte Server = 255;
        public string Location => Server == 255 ? "Offline" : Server == 254 ? "Unavailable" : $"Server {Server}";
    }
    public sealed class Letter
    {
        public ushort Index;
        public string Sender, Date, Subject, Body;
        public byte State;
    }
    public List<Friend> Friends { get; } = new();
    public List<Letter> Letters { get; } = new();
    public List<MessengerChatRoom> Rooms { get; } = new();
    public int Revision { get; private set; }
    public bool Initialized { get; private set; }
    public bool RefuseChat { get; set; }
    public string Notice { get; set; } = "Waiting for messenger data from OpenMU.";
    public uint LastLetterRequest;
    public uint LastChatInvitation;
    public string PendingChatName { get; private set; }
    private DateTime _chatDeadline;
    private readonly Dictionary<uint, (MessengerChatRoom Room, DateTime Deadline)> _invitations = new();
    public int MaximumLetters { get; private set; }
    public event Action<Letter> LetterOpened;
    public event Action<MessengerChatRoom> RoomOpened;
    public event Action<uint, bool> LetterSent;
    public void Changed() => Revision++;
    public void Reset()
    {
        foreach (var room in Rooms) room.Dispose();
        Rooms.Clear(); Friends.Clear(); Letters.Clear(); Initialized = false; RefuseChat = false;
        PendingChatName = null; _invitations.Clear();
        Notice = "Waiting for messenger data from OpenMU."; Changed();
    }
    private static string Text(ReadOnlySpan<byte> p, int offset, int length)
    {
        var bytes = p.Slice(offset, length); int end = bytes.IndexOf((byte)0);
        return Encoding.UTF8.GetString(end < 0 ? bytes : bytes[..end]);
    }
    private void SetFriend(string name, byte server)
    {
        var friend = Friends.Find(f => f.Name == name);
        if (friend == null) { friend = new Friend { Name = name }; Friends.Add(friend); }
        friend.Server = server;
        Friends.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    public bool BeginChat(string name)
    {
        var existing = Rooms.Find(r => !r.IsClosed && string.Equals(r.FriendName, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { RoomOpened?.Invoke(existing); Notice = "Conversation already open."; return false; }
        var friend = Friends.Find(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        if (friend == null || friend.Server >= 254) { Notice = "Choose an online friend who accepts chat."; return false; }
        if (PendingChatName != null) { Notice = "A chat request is already pending. Please wait."; return false; }
        if (Rooms.Count >= 10 && !Rooms.Exists(r => r.IsClosed && string.Equals(r.FriendName, name, StringComparison.OrdinalIgnoreCase)))
        { Notice = "Close a chat first (maximum 10)."; return false; }
        PendingChatName = name; _chatDeadline = DateTime.UtcNow.AddSeconds(15);
        Notice = "Requesting chat with " + name + "..."; return true;
    }
    public void TrackInvitation(uint id, MessengerChatRoom room)
    {
        _invitations[id] = (room, DateTime.UtcNow.AddSeconds(15));
        room.SetInvitationStatus("Invitation sent; waiting for OpenMU.");
    }
    public void UpdateTimeouts()
    {
        var now = DateTime.UtcNow;
        if (PendingChatName != null && now >= _chatDeadline)
        { PendingChatName = null; Notice = "No chat response. Check friend visibility and ChatServer, then retry."; }
        // Remove one expired request per frame without allocating a list.
        uint? expired = null;
        foreach (var entry in _invitations)
            if (now >= entry.Value.Deadline)
            { entry.Value.Room.SetInvitationStatus("Invitation timed out. Check whether your friend joined before retrying."); expired = entry.Key; break; }
        if (expired.HasValue) _invitations.Remove(expired.Value);
    }

    public void Receive(byte[] packet, CharacterService service)
    {
        var p = packet.AsSpan();
        if (p.Length < 3) return;
        bool wide = p[0] is 0xC2 or 0xC4;
        if (wide && p.Length < 4) return;
        int length = wide ? BinaryPrimitives.ReadUInt16BigEndian(p.Slice(1, 2)) : p[1];
        if (length != p.Length) return;
        byte code = p[wide ? 3 : 2];
        switch (code)
        {
            case 0xC0 when wide && p.Length >= 7 && p.Length == 7 + p[6] * 11:
                Reset(); MaximumLetters = p[5];
                for (int i = 0; i < p[6]; i++) SetFriend(Text(p, 7 + 11 * i, 10), p[17 + 11 * i]);
                Initialized = true; Notice = ""; break;
            case 0xC1 when p.Length >= 15:
                if (p[3] == 1) { SetFriend(Text(p, 4, 10), p[14]); Notice = "Friend added."; }
                else Notice = "The server could not add this friend.";
                break;
            case 0xC2 when p.Length >= 13:
                string requester = Text(p, 3, 10);
                RequestDialog.Show($"Accept {requester} as a friend?",
                    () => { _ = service.AnswerFriendAsync(requester, true); },
                    () => { _ = service.AnswerFriendAsync(requester, false); });
                break;
            case 0xC3 when p.Length >= 14:
                if (p[3] == 1) { string name = Text(p, 4, 10); Friends.RemoveAll(f => f.Name == name); Notice = "Friend deleted."; }
                else Notice = "The server could not delete this friend.";
                break;
            case 0xC4 when p.Length >= 14: SetFriend(Text(p, 3, 10), p[13]); break;
            case 0xC5 when p.Length >= 8:
                Notice = p[3] switch { 1 => "Letter sent.", 2 => "Recipient mailbox is full.", 3 => "Recipient does not exist.",
                    4 => "You cannot send a letter to yourself.", 7 => "Not enough Zen to send a letter.", _ => "Letter was not sent. Try again." };
                LetterSent?.Invoke(BinaryPrimitives.ReadUInt32LittleEndian(p.Slice(4)), p[3] == 1); break;
            case 0xC6 when p.Length >= 79:
                ushort index = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(4));
                var letter = Letters.Find(l => l.Index == index);
                if (letter == null) { letter = new Letter { Index = index }; Letters.Add(letter); }
                letter.Sender = Text(p, 6, 10); letter.Date = Text(p, 16, 30); letter.Subject = Text(p, 46, 32); letter.State = p[78];
                Letters.Sort((a, b) => b.Index.CompareTo(a.Index)); break;
            case 0xC7 when wide && p.Length >= 28:
                ushort readIndex = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(4));
                bool extended = !(MuGame.AppSettings?.ClientVersion ?? "2.04d").StartsWith("1.", StringComparison.Ordinal);
                int offset = extended ? 50 : 28;
                if (p.Length < offset) return;
                var opened = Letters.Find(l => l.Index == readIndex);
                if (opened != null) { opened.Body = Text(p, offset, p.Length - offset); opened.State = 0; Notice = ""; LetterOpened?.Invoke(opened); }
                break;
            case 0xC8 when p.Length >= 6:
                if (p[3] == 0) { Notice = "The server could not delete this letter."; break; }
                ushort removed = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(4));
                Letters.RemoveAll(l => l.Index == removed);
                // OpenMU addresses letters by list position, not by a persistent id.
                foreach (var remaining in Letters) if (remaining.Index > removed) remaining.Index--;
                Notice = "Letter deleted."; break;
            case 0xCA when p.Length >= 36:
                PendingChatName = null;
                if (p[35] == 0) { Notice = "Chat unavailable. Check that your friend is online."; break; }
                ushort roomId = BinaryPrimitives.ReadUInt16LittleEndian(p.Slice(18));
                var old = Rooms.Find(r => r.Id == roomId);
                if (old != null && !old.IsClosed) { RoomOpened?.Invoke(old); break; }
                if (old != null) { old.Dispose(); Rooms.Remove(old); }
                string host = Text(p, 3, 15), friendName = Text(p, 25, 10);
                for (int i = Rooms.Count - 1; i >= 0; i--)
                    if (Rooms[i].IsClosed && string.Equals(Rooms[i].FriendName, friendName, StringComparison.OrdinalIgnoreCase))
                    { Rooms[i].Dispose(); Rooms.RemoveAt(i); }
                uint token = BinaryPrimitives.ReadUInt32LittleEndian(p.Slice(20));
                int port = int.TryParse(MuGame.AppConfiguration?["MuOnlineSettings:ChatServerPort"], out int configured) ? configured : 45980;
                if (port < 1 || port > 65535 || string.IsNullOrWhiteSpace(host)) { Notice = "Invalid ChatServer address or port."; break; }
                if (Rooms.Count >= 10) { Notice = "Close a chat before opening another (maximum 10)."; break; }
                var room = new MessengerChatRoom(roomId, friendName);
                Rooms.Add(room); Notice = "Opening chat with " + friendName + ".";
                _ = room.ConnectAsync(host, port, token); RoomOpened?.Invoke(room); break;
            case 0xCB when p.Length >= 8:
                uint request = BinaryPrimitives.ReadUInt32BigEndian(p.Slice(4));
                if (_invitations.Remove(request, out var invitation))
                {
                    Notice = p[3] != 0 ? "Chat invitation accepted by the server." : "Could not invite this friend. Check online status and friendship.";
                    invitation.Room.SetInvitationStatus(Notice);
                }
                break;
        }
        Changed();
    }
}
