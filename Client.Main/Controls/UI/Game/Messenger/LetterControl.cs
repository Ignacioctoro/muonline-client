using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Client;
using Client.Main.Networking.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Messenger;

public sealed class LetterControl : MessengerWindow
{
    private readonly CharacterService _service;
    private readonly TextFieldControl _recipient, _subject;
    private readonly TextFieldControl[] _body = new TextFieldControl[6];
    private readonly ButtonControl _send, _reply;
    private readonly List<string> _lines = new();
    private MessengerState.Letter _letter;
    private bool _writing, _sending;
    private uint _request;
    private string _sentBody = "", _sentRecipient = "", _sentSubject = "";
    private DateTime _sentAt;
    private int _scroll;
    private string _notice = "";
    public LetterControl(CharacterService service) : base("Letter", 530, 435)
    {
        _service = service; Visible = false;
        _recipient = AddInput(110, 86, 380, 10, "Character name");
        _subject = AddInput(110, 120, 380, 59, "Subject");
        for (int i = 0; i < _body.Length; i++) _body[i] = AddInput(30, 178 + i * 28, 465, 140, "Message line " + (i + 1));
        _send = AddButton("Send", 280, 361, 100, Send);
        _reply = AddButton("Reply", 170, 361, 100, () => Compose(_letter?.Sender ?? "", "Re: " + (_letter?.Subject ?? "")));
        AddButton("Close", 390, 361, 105, CloseWindow);
        AddButton("<", 30, 361, 28, () => _scroll = Math.Max(0, _scroll - 7));
        AddButton(">", 64, 361, 28, () => _scroll = Math.Min(Math.Max(0, _lines.Count - 10), _scroll + 7));
        _service.Messenger.LetterSent += OnSent;
    }
    public void Compose(string recipient, string subject)
    {
        // Keep an unfinished draft; opening Write again must not silently erase it.
        bool hasDraft = _writing && _body.Any(f => !string.IsNullOrWhiteSpace(f.Value));
        _writing = true; _letter = null; _scroll = 0; _notice = "Sending a letter may cost Zen, according to the server.";
        if (!hasDraft) { _recipient.Value = recipient; _subject.Value = subject; foreach (var field in _body) field.Value = ""; }
        SetMode(); Visible = true; BringToFront(); _recipient.Focus();
    }
    public void Read(MessengerState.Letter letter)
    {
        if (_writing && _body.Any(f => !string.IsNullOrWhiteSpace(f.Value)))
        { RequestDialog.ShowInfo("You have an unfinished letter. Send it or clear its text before reading another."); return; }
        _letter = letter; _writing = false; _scroll = 0; _notice = "";
        _lines.Clear(); _lines.AddRange(MessengerText.Wrap(letter.Body ?? "", 76));
        SetMode(); Visible = true; BringToFront();
    }
    private void SetMode()
    {
        _recipient.Visible = _subject.Visible = _send.Visible = _writing;
        foreach (var field in _body) { field.Visible = _writing; BlurInput(field); }
        BlurInput(_recipient); BlurInput(_subject); _reply.Visible = !_writing;
    }
    private void Send()
    {
        if (_sending) return;
        string message = string.Join("\n", _body.Select(f => f.Value)).Trim();
        uint before = _service.Messenger.LastLetterRequest;
        _ = _service.SendLetterAsync(_recipient.Value, _subject.Value, message);
        _notice = _service.Messenger.Notice;
        if (_service.Messenger.LastLetterRequest == before) return;
        _request = _service.Messenger.LastLetterRequest;
        _sentBody = message; _sentRecipient = _recipient.Value; _sentSubject = _subject.Value;
        _sending = true; _sentAt = DateTime.UtcNow;
    }
    private void OnSent(uint id, bool success)
    {
        if (id != _request) return;
        _sending = false; _notice = _service.Messenger.Notice;
        // Clear only the exact draft acknowledged by the server; late replies must not erase edits.
        if (success && _recipient.Value == _sentRecipient && _subject.Value == _sentSubject &&
            string.Join("\n", _body.Select(f => f.Value)).Trim() == _sentBody)
            foreach (var field in _body) field.Value = "";
    }
    public override void Update(GameTime time)
    {
        base.Update(time); if (!Visible) return;
        if (_sending && DateTime.UtcNow - _sentAt > TimeSpan.FromSeconds(15))
        { _sending = false; _notice = "No confirmation yet. Check your mailbox before retrying."; }
        _send.Enabled = !_sending;
        if (!_writing) _scroll = Math.Clamp(_scroll - Wheel * 3, 0, Math.Max(0, _lines.Count - 10));
    }
    protected override void DrawContent(SpriteBatch sb)
    {
        Text(sb, _writing ? "To:" : "From: " + (_letter?.Sender ?? ""), 30, 92, 450, Theme.Gold);
        Text(sb, _writing ? "Subject:" : "Subject: " + (_letter?.Subject ?? ""), 30, 126, 465, Theme.Gold);
        Text(sb, "Message", 30, 157, 465, Theme.Gold);
        if (!_writing)
        {
            Fill(sb, new Rectangle(28, 176, 472, 177), Theme.Darkest * .8f);
            for (int i = 0; i < 10 && i + _scroll < _lines.Count; i++) Text(sb, _lines[i + _scroll], 34, 179 + i * 17, 455);
        }
        Text(sb, _notice, 30, 400, 465, Theme.Gold, 10);
    }
    public override void Dispose() { _service.Messenger.LetterSent -= OnSent; base.Dispose(); }
}

internal static class MessengerText
{
    // Wrap on data changes, never each frame. Keep explicit line breaks in letters.
    public static IEnumerable<string> Wrap(string text, int columns)
    {
        foreach (string paragraph in text.Replace("\r", "").Split('\n'))
        {
            string rest = paragraph;
            while (rest.Length > columns)
            {
                int cut = rest.LastIndexOf(' ', columns, columns);
                if (cut < columns / 2) cut = columns;
                yield return rest[..cut]; rest = rest[cut..].TrimStart();
            }
            yield return rest;
        }
    }
}
