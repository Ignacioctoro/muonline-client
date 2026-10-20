using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Core.Client;
using Client.Main.Controls.UI.Common;

namespace Client.Main.Controls.UI.Game.Messenger;

public sealed class ConversationControl : MessengerWindow
{
    public MessengerChatRoom Room { get; }
    private readonly TextFieldControl _input;
    private readonly TextFieldControl _inviteName;
    private readonly ButtonControl _sendButton, _retryButton, _inviteButton;
    private readonly List<string> _lines = new();
    private int _revision = -1, _scroll;
    private bool _sending, _disposed;
    public ConversationControl(MessengerChatRoom room) : base("Chat: " + room.FriendName, 420, 430)
    {
        Room = room;
        _input = AddInput(26, 254, 280, 200, "Message");
        _input.EnterKeyPressed += (_, _) => Send();
        _sendButton = AddButton("Send", 314, 252, 80, Send);
        _inviteName = AddInput(26, 303, 280, 10, "Online friend to invite");
        _inviteName.EnterKeyPressed += (_, _) => Invite();
        _inviteButton = AddButton("Invite", 314, 301, 80, Invite);
        _retryButton = AddButton("Retry", 26, 380, 80, () => { _ = MuGame.Network.GetCharacterService().RequestChatAsync(Room.FriendName); });
        AddButton("Hide", 314, 380, 80, CloseWindow);
        AddButton("<", 246, 380, 28, () => _scroll = Math.Max(0, _scroll - 8));
        AddButton(">", 280, 380, 28, () => _scroll = Math.Min(Math.Max(0, _lines.Count - 9), _scroll + 8));
    }
    private void Invite() => _ = MuGame.Network.GetCharacterService().InviteToChatAsync(Room, _inviteName.Value);
    private async void Send()
    {
        if (_sending) return;
        _sending = true; string value = _input.Value;
        bool sent = await Room.SendAsync(value);
        MuGame.ScheduleOnMainThread(() => { if (_disposed) return; if (sent && _input.Value == value) _input.Value = ""; _sending = false; });
    }
    public override void Update(GameTime time)
    {
        base.Update(time); if (!Visible) return;
        _sendButton.Enabled = Room.Connected && !Room.IsClosed && !_sending;
        _inviteButton.Enabled = Room.Connected && !Room.IsClosed;
        _retryButton.Visible = Room.IsClosed;
        if (_revision != Room.Revision)
        {
            bool atEnd = _scroll >= Math.Max(0, _lines.Count - 9);
            _revision = Room.Revision; _lines.Clear();
            foreach (var message in Room.Messages) _lines.AddRange(MessengerText.Wrap(message, 58));
            if (atEnd) _scroll = Math.Max(0, _lines.Count - 9);
        }
        _scroll = Math.Clamp(_scroll - Wheel * 3, 0, Math.Max(0, _lines.Count - 9));
    }
    protected override void DrawContent(SpriteBatch sb)
    {
        Text(sb, Room.Participants, 26, 80, 366, Theme.Gold, 10);
        Fill(sb, new Rectangle(24, 96, 372, 151), Theme.Darkest * .85f);
        for (int i = 0; i < 9 && _scroll + i < _lines.Count; i++) Text(sb, _lines[_scroll + i], 29, 99 + 16 * i, 362);
        Text(sb, "Invite an online friend to this conversation:", 26, 286, 366, Theme.Gold, 10);
        Text(sb, Room.InvitationStatus, 26, 334, 366, Theme.Gold, 10);
        Text(sb, Room.Status, 26, 356, 366, Theme.Gold, 10);
    }
    public override void Dispose() { _disposed = true; Room.Dispose(); base.Dispose(); }
}
