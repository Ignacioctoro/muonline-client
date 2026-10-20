using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Core.Client;
using Client.Main.Core.Client.PartyMatching;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Messenger;
using Client.Main.Networking.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Party;

public sealed class PartyMatchingControl : MessengerWindow
{
    private readonly CharacterService _service;
    private PartyMatchingState State => _service.PartyMatching;
    private readonly List<GameControl> _browse = new(), _recruit = new(), _request = new(), _mine = new();
    private readonly TextFieldControl _search, _joinPassword, _title, _min, _max, _password;
    private readonly ButtonControl[] _classes = new ButtonControl[7];
    private readonly ButtonControl _autoButton;
    private readonly PartyManager _party;
    private readonly List<PartyMemberInfo> _members = new();
    private readonly List<string> _noticeLines = new();
    private string _notice = "", _selectedId = "";
    private int _tab, _page, _requestScroll, _classMask = 127, _selectedMember = -1;
    private bool _auto, _rowsDirty = true;
    private readonly string[] _rowTitles = new string[8], _rowDetails = new string[8];
    private int _seenRevision = -1, _seenScroll = -1;
    private string _pageLabel = "1/1", _requestsLabel = "Pending requests: 0";
    private string _selectionInfo = "Select a party to see its class requirements.";
    private double _elapsed;
    private static readonly string[] ClassNames = { "Wizard", "Knight", "Elf", "MG", "DL", "Summoner", "RF" };
    protected override string FramePath => "Interface/GFx/MatchingParty_I1.ozd";
    protected override Rectangle FrameHeader => new(0, 0, 416, 78);
    protected override Rectangle FrameBody => new(0, 78, 416, 560);

    public PartyMatchingControl() : base("Party Matching", 410, 630)
    {
        _service = MuGame.Network.GetCharacterService(); _party = MuGame.Network.GetPartyManager();
        Visible = false;
        string[] tabs = { "My Party", "Search", "Recruit", "Requests" };
        for (int i = 0; i < 4; i++) { int tab = i; AddButton(tabs[i], 19 + i * 94, 82, 92, () => SelectTab(tab)); }
        _search = AddInput(24, 118, 260, 40, "Search title / leader"); _browse.Add(_search);
        _search.EnterKeyPressed += (_, _) => { _page = 0; Refresh(); };
        _browse.Add(AddButton("Search", 292, 116, 90, () => { _page = 0; Refresh(); }));
        _joinPassword = AddInput(24, 505, 200, 16, "Party password"); _joinPassword.MaskValue = true; _browse.Add(_joinPassword);
        _browse.Add(AddButton("<", 292, 503, 40, () => { _page = Math.Max(0, _page - 1); Refresh(); }));
        _browse.Add(AddButton(">", 340, 503, 40, () => { _page = Math.Min(State.Snapshot.Pages - 1, _page + 1); Refresh(); }));
        _browse.Add(AddButton("Apply", 24, 541, 120, Apply));
        _browse.Add(AddButton("Cancel application", 153, 541, 229, () => Send(new() { Operation = 4 })));

        _title = AddInput(24, 143, 358, 40, "Recruitment title"); _recruit.Add(_title);
        _min = AddInput(24, 205, 160, 5, "Minimum level"); _min.Value = "1"; _recruit.Add(_min);
        _max = AddInput(222, 205, 160, 5, "Maximum level"); _max.Value = "400"; _recruit.Add(_max);
        _password = AddInput(24, 267, 358, 16, "Optional password"); _password.MaskValue = true; _recruit.Add(_password);
        for (int i = 0; i < 7; i++)
        {
            int which = i;
            _classes[i] = AddButton("", 24 + (i % 3) * 121, 330 + (i / 3) * 34, 116, () => { _classMask ^= 1 << which; UpdateClassButtons(); });
            _recruit.Add(_classes[i]);
        }
        _autoButton = AddButton("[ ] Auto accept", 24, 442, 200, () => { _auto = !_auto; _autoButton.Text = _auto ? "[x] Auto accept" : "[ ] Auto accept"; });
        _recruit.Add(_autoButton);
        _recruit.Add(AddButton("Publish / replace", 24, 541, 174, Publish));
        _recruit.Add(AddButton("Stop recruiting", 208, 541, 174, () => Send(new() { Operation = 2 })));
        _request.Add(AddButton("<", 292, 503, 40, () => _requestScroll = Math.Max(0, _requestScroll - 8)));
        _request.Add(AddButton(">", 340, 503, 40, () => _requestScroll = Math.Min(Math.Max(0, State.Snapshot.Requests.Length - 8), _requestScroll + 8)));
        _request.Add(AddButton("Accept", 24, 541, 174, () => Decide(true)));
        _request.Add(AddButton("Reject", 208, 541, 174, () => Decide(false)));
        _mine.Add(AddButton("Leave party", 24, 541, 174, () => Kick(true)));
        _mine.Add(AddButton("Remove member", 208, 541, 174, () => Kick(false)));
        _party.PartyUpdated += UpdateParty; UpdateParty(); UpdateClassButtons(); SelectTab(0, false);
    }

    public void Toggle()
    {
        if (Visible) CloseWindow();
        else { Visible = true; BringToFront(); Refresh(); }
    }
    private void SelectTab(int tab, bool refresh = true)
    {
        _tab = tab; _rowsDirty = true; _selectedId = ""; _selectedMember = -1;
        foreach (var group in new[] { _mine, _browse, _recruit, _request })
            foreach (var control in group) { control.Visible = false; if (control is TextFieldControl input) BlurInput(input); }
        foreach (var control in new[] { _mine, _browse, _recruit, _request }[tab]) control.Visible = true;
        if (refresh) Refresh();
    }
    private void UpdateClassButtons()
    { for (int i = 0; i < 7; i++) _classes[i].Text = ((_classMask & (1 << i)) != 0 ? "[x] " : "[ ] ") + ClassNames[i]; }
    private void UpdateParty() { _members.Clear(); _members.AddRange(_party.GetPartyMembers()); _rowsDirty = true; }
    private void Refresh() => Send(new());
    private void Send(PartyMatchingRequest request)
    {
        request.Page = _page; request.Search = _search.Value.Trim(); _elapsed = 0;
        _ = _service.SendPartyMatchingAsync(request);
    }
    private void Publish()
    {
        if (!int.TryParse(_min.Value, out int min) || !int.TryParse(_max.Value, out int max))
        { State.Notice = "Enter valid minimum and maximum levels."; return; }
        Send(new() { Operation = 1, Title = _title.Value.Trim(), Password = _password.Value,
            MinLevel = min, MaxLevel = max, ClassMask = _classMask, AutoAccept = _auto });
    }
    private void Apply()
    {
        if (!State.Snapshot.Ads.Any(a => a.Id == _selectedId)) { State.Notice = "Select an advertisement."; return; }
        Send(new() { Operation = 3, Id = _selectedId, Password = _joinPassword.Value });
    }
    private void Decide(bool accept)
    {
        if (!State.Snapshot.Requests.Any(a => a.Id == _selectedId)) { State.Notice = "Select an application."; return; }
        Send(new() { Operation = accept ? 5 : 6, Id = _selectedId });
    }
    private void Kick(bool self)
    {
        string ownName = MuGame.Network.GetCharacterState().Name;
        var member = self ? _members.Find(m => m.Name == ownName) : _members.Find(m => m.Index == _selectedMember);
        if (member == null) { State.Notice = "Select a party member."; return; }
        if (!self && (_members.Count == 0 || _members[0].Name != ownName)) { State.Notice = "Only the leader can remove other members."; return; }
        string name = member.Name; byte index = member.Index;
        RequestDialog.Show(self ? "Leave your party?" : "Remove " + name + " from the party?", () =>
        {
            if (_members.Any(m => m.Index == index && m.Name == name)) _ = _service.SendPartyKickRequestAsync(index);
        });
    }
    public override void Update(GameTime time)
    {
        base.Update(time); State.Update(); _elapsed += time.ElapsedGameTime.TotalSeconds;
        if (_elapsed >= 5 && !State.Waiting && State.Available && (Visible || State.Snapshot.OwnAd.Length > 0 || State.Snapshot.PendingAd.Length > 0)) Refresh();
        if (!Visible) return;
        if (_rowsDirty || _seenRevision != State.Revision || _seenScroll != _requestScroll) RebuildRows();
        if (_notice != State.Notice)
        { _notice = State.Notice; _noticeLines.Clear(); _noticeLines.AddRange(MessengerText.Wrap(_notice, 60)); }
        if (ClickAt(out var point) && point.Y >= 158 && point.Y < 478 && point.X >= 24 && point.X < 382)
        {
            int row = (point.Y - 158) / 40;
            if (_tab == 1 && row < State.Snapshot.Ads.Length) _selectedId = State.Snapshot.Ads[row].Id;
            if (_tab == 3 && row + _requestScroll < State.Snapshot.Requests.Length) _selectedId = State.Snapshot.Requests[row + _requestScroll].Id;
            if (_tab == 0 && row < _members.Count) _selectedMember = _members[row].Index;
            _rowsDirty = true;
            Scene?.SetMouseInputConsumed();
        }
    }
    private void RebuildRows()
    {
        _rowsDirty = false; _seenRevision = State.Revision; _seenScroll = _requestScroll;
        _pageLabel = $"{State.Snapshot.Page + 1}/{State.Snapshot.Pages}";
        _requestsLabel = "Pending requests: " + State.Snapshot.Requests.Length;
        _page = State.Snapshot.Page;
        var selected = State.Snapshot.Ads.FirstOrDefault(a => a.Id == _selectedId);
        _selectionInfo = selected == null ? "Select a party to see its class requirements." :
            "Classes: " + string.Join(", ", ClassNames.Where((_, i) => (selected.ClassMask & (1 << i)) != 0));
        for (int i = 0; i < 8; i++)
        {
            _rowTitles[i] = _rowDetails[i] = "";
            if (_tab == 1 && i < State.Snapshot.Ads.Length)
            {
                var a = State.Snapshot.Ads[i];
                _rowTitles[i] = a.Title + (a.Locked ? " [locked]" : "") + (a.AutoAccept ? " [auto]" : "");
                _rowDetails[i] = $"{a.Leader}  {a.Count}/{a.Capacity}  Lv.{a.MinLevel}-{a.MaxLevel}  Map {a.Map}";
            }
            if (_tab == 3 && i + _requestScroll < State.Snapshot.Requests.Length)
            {
                var a = State.Snapshot.Requests[i + _requestScroll];
                _rowTitles[i] = a.Name;
                _rowDetails[i] = $"Level {a.Level}   {(a.Class >= 0 && a.Class < 7 ? ClassNames[a.Class] : "Unknown")}";
            }
            if (_tab == 0 && i < _members.Count)
            {
                var m = _members[i]; _rowTitles[i] = (i == 0 ? "Leader: " : "") + m.Name;
                _rowDetails[i] = $"HP {m.CurrentHealth}/{m.MaximumHealth}   Map {m.MapId}";
            }
        }
    }
    protected override void DrawContent(SpriteBatch sb)
    {
        if (_tab == 2)
        {
            Text(sb, "Recruitment title", 24, 122, 358, Theme.Gold);
            Text(sb, "Minimum level", 24, 184, 170, Theme.Gold); Text(sb, "Maximum level", 222, 184, 170, Theme.Gold);
            Text(sb, "Optional password (leave empty for public party)", 24, 246, 358, Theme.Gold, 10);
            Text(sb, "Allowed classes", 24, 309, 358, Theme.Gold);
            Text(sb, "Advertises your current map. Same GameServer only.", 24, 489, 358, Color.LightGray, 10);
            Text(sb, State.Snapshot.OwnAd.Length > 0 ? "Recruitment is active." : "No active advertisement.", 24, 509, 358, Theme.Gold, 10);
        }
        else
        {
            if (_tab == 0) Text(sb, _members.Count == 0 ? "You are not in a party." : "Your party", 24, 125, 358, Theme.Gold);
            if (_tab == 3) Text(sb, _requestsLabel, 24, 125, 358, Theme.Gold);
            if (_tab == 1) Text(sb, _selectionInfo, 24, 145, 358, Theme.Gold, 9);
            for (int i = 0; i < 8; i++)
            {
                int y = 158 + 40 * i; bool selected = false;
                if (_tab == 1 && i < State.Snapshot.Ads.Length) selected = State.Snapshot.Ads[i].Id == _selectedId;
                if (_tab == 3 && i + _requestScroll < State.Snapshot.Requests.Length) selected = State.Snapshot.Requests[i + _requestScroll].Id == _selectedId;
                if (_tab == 0 && i < _members.Count) selected = _members[i].Index == _selectedMember;
                Fill(sb, new Rectangle(24, y, 358, 38), selected ? new Color(0, 65, 78, 220) : Theme.Darkest * .85f);
                Text(sb, _rowTitles[i], 29, y + 3, 348, Theme.Gold);
                Text(sb, _rowDetails[i], 29, y + 21, 348, null, 10);
            }
            if (_tab == 1)
            {
                Text(sb, "Password if required", 24, 486, 210, Theme.Gold, 10);
                Text(sb, _pageLabel, 243, 512, 45, Theme.Gold, 10);
            }
        }
        for (int i = 0; i < Math.Min(3, _noticeLines.Count); i++) Text(sb, _noticeLines[i], 24, 581 + i * 12, 358, Theme.Gold, 10);
    }
    public override void Dispose() { _party.PartyUpdated -= UpdateParty; State.Reset(); base.Dispose(); }
}
