using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Main.Core.Client;

namespace Client.Main.Controls.UI.Game.Messenger;

public sealed class ConversationControl : MessengerWindow
{
    public MessengerChatRoom Room { get; }
    private readonly TextFieldControl _input;
    private readonly List<string> _lines = new();
    private int _revision = -1, _scroll;
    private bool _sending, _disposed;
    public ConversationControl(MessengerChatRoom room) : base("Chat: " + room.FriendName, 420, 340)
    {
        Room = room;
        _input = AddInput(26, 254, 280, 200, "Message");
        _input.EnterKeyPressed += (_, _) => Send();
        AddButton("Send", 314, 252, 80, Send);
        AddButton("Hide", 314, 288, 80, CloseWindow);
        AddButton("<", 246, 288, 28, () => _scroll = Math.Max(0, _scroll - 8));
        AddButton(">", 280, 288, 28, () => _scroll = Math.Min(Math.Max(0, _lines.Count - 9), _scroll + 8));
    }
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
        Fill(sb, new Rectangle(24, 82, 372, 163), Theme.Darkest * .85f);
        for (int i = 0; i < 9 && _scroll + i < _lines.Count; i++) Text(sb, _lines[_scroll + i], 29, 87 + 17 * i, 362);
        Text(sb, Room.Status, 26, 297, 215, Theme.Gold, 10);
    }
    public override void Dispose() { _disposed = true; Room.Dispose(); base.Dispose(); }
}
