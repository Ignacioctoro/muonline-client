using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Models;
using Client.Main.Networking.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Messenger;

public sealed class FriendsControl : MessengerWindow
{
    private readonly CharacterService _service;
    private MessengerState State => _service.Messenger;
    private readonly ButtonControl[] _actions = new ButtonControl[5];
    private readonly ButtonControl _refuse;
    private readonly List<ConversationControl> _windows = new();
    private LetterControl _letter;
    private int _tab, _selected = -1, _scroll, _revision = -1;
    private readonly List<string> _left = new(), _right = new();
    private string _countLabel = "";
    private readonly TextFieldControl _name;
    private bool _adding;
    public FriendsControl() : base("My Friend", 530, 390)
    {
        _service = MuGame.Network.GetCharacterService(); Visible = false;
        string[] tabs = { "Friends List", "Letter Box", "Window List" };
        for (int i = 0; i < tabs.Length; i++)
        { int tab = i; AddButton(tabs[i], 28 + 108 * i, 82, 104, () => SelectTab(tab)); }
        _refuse = AddButton("[ ] Refuse Chat", 362, 82, 140, () => { _ = _service.SetMessengerVisibleAsync(State.RefuseChat); });
        for (int i = 0; i < 5; i++) { int action = i; _actions[i] = AddButton("", 27 + i * 96, 322, 92, () => Act(action)); }
        AddButton("<", 448, 280, 25, () => _scroll = Math.Max(0, _scroll - 6));
        AddButton(">", 478, 280, 25, () => _scroll = Math.Min(Math.Max(0, Count - 6), _scroll + 6));
        _name = AddInput(28, 281, 300, 10, "Character name"); _name.Visible = false;
        _name.EnterKeyPressed += (_, _) => AddFriend();
        State.LetterOpened += OpenLetter; State.RoomOpened += OpenRoom;
        SelectTab(0);
    }
    private int Count => _tab == 0 ? State.Friends.Count : _tab == 1 ? State.Letters.Count : State.Rooms.Count;
    private string SelectedName => _selected < 0 || _selected >= Count ? null :
        _tab == 0 ? State.Friends[_selected].Name : _tab == 1 ? State.Letters[_selected].Sender : State.Rooms[_selected].FriendName;
    public void Toggle() { if (Visible) CloseWindow(); else { Visible = true; BringToFront(); } }
    private void SelectTab(int tab)
    {
        _tab = tab; _revision = -1; _selected = -1; _scroll = 0; _adding = false; _name.Visible = false; BlurInput(_name);
        string[] labels = tab switch
        { 0 => new[] { "Add Friend", "Delete Friend", "Chat", "Write", "Invite to Party" },
          1 => new[] { "Write", "Read", "Reply", "Delete", "Invite to Party" },
          _ => new[] { "Show", "Hide", "Close Chat", "Hide All", "Invite to Party" } };
        for (int i = 0; i < 5; i++) _actions[i].Text = labels[i];
    }
    private void AddFriend()
    {
        if (string.IsNullOrWhiteSpace(_name.Value)) { _name.Focus(); return; }
        _ = _service.SendFriendActionAsync(0xC1, _name.Value); _adding = false; _name.Visible = false; BlurInput(_name);
    }
    private void Act(int action)
    {
        string name = SelectedName;
        if (action == 4) { Invite(name); return; }
        if (_tab == 0)
        {
            if (action == 0) { if (_adding) AddFriend(); else { _adding = true; _name.Visible = true; _name.Focus(); } return; }
            if (name == null) { State.Notice = "Select a friend."; return; }
            if (action == 1) RequestDialog.Show($"Delete {name} from your friends?", () => { _ = _service.SendFriendActionAsync(0xC3, name); });
            if (action == 2) _ = _service.SendFriendActionAsync(0xCA, name);
            if (action == 3) Compose(name, "");
        }
        else if (_tab == 1)
        {
            if (action == 0) { Compose(name ?? "", ""); return; }
            if (_selected < 0 || _selected >= State.Letters.Count) { State.Notice = "Select a letter."; return; }
            var letter = State.Letters[_selected];
            if (action == 1) _ = _service.RequestLetterAsync(letter.Index);
            if (action == 2) Compose(letter.Sender, "Re: " + letter.Subject);
            if (action == 3) RequestDialog.Show("Delete this letter?", () => { _ = _service.RequestLetterAsync(letter.Index, true); _selected = -1; });
        }
        else
        {
            if (action == 3) { foreach (var w in _windows) w.CloseWindow(); return; }
            if (_selected < 0 || _selected >= State.Rooms.Count) return;
            var room = State.Rooms[_selected]; var window = _windows.Find(w => w.Room == room);
            if (action == 0) OpenRoom(room);
            if (action == 1) window?.CloseWindow();
            if (action == 2)
            {
                room.Dispose(); State.Rooms.Remove(room); State.Changed(); _selected = -1;
                if (window != null) { window.CloseWindow(); Scene.Controls.Remove(window); window.Dispose(); _windows.Remove(window); }
            }
        }
    }
    private void Invite(string name)
    {
        if (name == null) { State.Notice = "Select a player first."; return; }
        var scope = MuGame.Network.GetScopeManager();
        foreach (var item in scope.GetScopeItems(ScopeObjectType.Player))
            if (item is PlayerScopeObject player && string.Equals(player.Name, name, StringComparison.OrdinalIgnoreCase))
            { _ = _service.SendPartyInviteAsync(player.Id); State.Notice = "Party invitation requested."; return; }
        State.Notice = "The player must be nearby to invite them to Party.";
    }
    private void Compose(string name, string subject)
    {
        if (_letter == null) { _letter = new LetterControl(_service); Scene.Controls.Add(_letter); }
        _letter.Compose(name, subject);
    }
    private void OpenLetter(MessengerState.Letter letter)
    {
        if (_letter == null) { _letter = new LetterControl(_service); Scene.Controls.Add(_letter); }
        _letter.Read(letter);
    }
    private void OpenRoom(MessengerChatRoom room)
    {
        var window = _windows.Find(w => w.Room == room);
        if (window == null) { window = new ConversationControl(room); _windows.Add(window); Scene.Controls.Add(window); }
        window.Visible = true; window.BringToFront();
    }
    public override void Update(GameTime time)
    {
        base.Update(time); if (!Visible) return;
        int revision = State.Revision;
        if (_tab == 2) foreach (var room in State.Rooms) revision = unchecked(revision * 31 + room.Revision);
        if (_revision != revision)
        {
            // Room status strings are already cached by each connection.
            _revision = revision; _left.Clear(); _right.Clear();
            if (_tab == 0) foreach (var f in State.Friends) { _left.Add(f.Name); _right.Add(f.Location); }
            else if (_tab == 1) foreach (var l in State.Letters) { _left.Add((l.State > 0 ? "* " : "") + l.Sender + "  " + l.Date); _right.Add(l.Subject); }
            else foreach (var room in State.Rooms) { _left.Add(room.FriendName); _right.Add(room.Status); }
            _countLabel = Count + " entries";
        }
        _refuse.Text = State.RefuseChat ? "[x] Refuse Chat" : "[ ] Refuse Chat";
        _scroll = Math.Clamp(_scroll - Wheel, 0, Math.Max(0, Count - 6));
        if (_selected >= Count) _selected = -1;
        if (ClickAt(out var p) && new Rectangle(28, 138, 472, 138).Contains(p))
        { int row = _scroll + (p.Y - 138) / 23; _selected = row < Count ? row : -1; Scene?.SetMouseInputConsumed(); }
    }
    protected override void DrawContent(SpriteBatch sb)
    {
        Fill(sb, new Rectangle(26, 115, 478, 164), Theme.Darkest * .85f);
        Text(sb, _tab == 0 ? "Friend's Name" : _tab == 1 ? "Sender / Date received" : "Conversation", 38, 119, 230, Theme.Gold);
        Text(sb, _tab == 0 ? "Server" : _tab == 1 ? "Title" : "Status", 264, 119, 230, Theme.Gold);
        for (int row = 0; row < 6; row++)
        {
            int i = _scroll + row, y = 138 + row * 23;
            Fill(sb, new Rectangle(29, y, 470, 21), i == _selected ? new Color(0, 70, 86, 210) : row % 2 == 0 ? Theme.Mid : Theme.Dark);
            if (i >= Count) continue;
            if (i < _left.Count) { Text(sb, _left[i], 38, y + 4, 215); Text(sb, _right[i], 264, y + 4, 230); }
        }
        Text(sb, _adding ? "Enter name; press Add Friend or Enter." : _countLabel, 30, 309, 400, Theme.Gold, 10);
        Text(sb, State.Notice, 28, 359, 475, Theme.Gold, 10);
    }
    public override void Dispose()
    {
        State.LetterOpened -= OpenLetter; State.RoomOpened -= OpenRoom;
        State.Reset(); base.Dispose();
    }
}
