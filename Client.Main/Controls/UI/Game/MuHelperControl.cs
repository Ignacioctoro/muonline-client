using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game;

/// <summary>Native NewUI MU Helper configuration. Does not start an automation session.</summary>
public sealed class MuHelperControl : UIControl, IUiTexturePreloadable
{
    private const int Width = 332, Height = 632;
    private const string AtlasPath = "Interface/GFx/MacroMain_I1.ozd";
    private static readonly Color Gold = new(221, 184, 83), Ink = new(222, 217, 203), Muted = new(132, 128, 117);
    private static readonly Rectangle SaveButton = new(180, 580, 90, 27), CloseButton = new(292, 35, 20, 20);
    private static readonly string[] SlotNames = { "Basic skill", "Activation 1", "Activation 2", "Buff 1", "Buff 2", "Buff 3" };
    public static bool IsOpen => Instance.Visible;
    public override bool NonDisposable => true;
    public override bool BlocksPointerInput => true;
    private readonly TextFieldControl _filterInput;
    private readonly List<string> _filters = new();
    private int _selectedFilter = -1, _filterScroll;
    private static readonly Rectangle FilterList = new(29, 395, 274, 140);
    private static readonly string[] ListFiles = {
        "newui_item_table01(L).OZT", "newui_item_table01(R).OZT",
        "newui_item_table02(L).OZT", "newui_item_table02(R).OZT",
        "newui_item_table03(L).OZT", "newui_item_table03(R).OZT",
        "newui_item_table03(Up).OZT", "newui_item_table03(Dw).OZT",
        "newui_scrollbar_up.OZT", "newui_scrollbar_down.OZT", "newui_scrollbar_m.OZT"
    };
    // All static resources used by instance field initializers must exist before construction.
    public static MuHelperControl Instance { get; } = new();
    private readonly Texture2D[] _listSkin = new Texture2D[ListFiles.Length];
    private readonly Dictionary<string, Texture2D> _icons = new();
    private Texture2D _atlas, _subAtlas;
    private MuHelperConfigurationData _optionSnapshot;
    private bool _optionWasDirty;
    private MuHelperConfigurationData _saved = MuHelperConfigurationData.Defaults(), _draft = MuHelperConfigurationData.Defaults();
    private byte[] _pending;
    private string _owner, _status = string.Empty;
    private int _ownerClass, _tab, _picker = -1, _page, _modal, _revision = -1;
    private bool _dirty;
    private DateTime _deadline;
    private Point _mouse;
    private Point? _click;
    private bool _rightClick;
    private SkillEntryState[] _skills = Array.Empty<SkillEntryState>();
    private SkillEntryState[] _choices = Array.Empty<SkillEntryState>();
    private int _choiceRevision = -2;
    private bool _choiceBuff;

    private MuHelperControl()
    {
        ControlSize = ViewSize = new Point(Width, Height);
        AutoViewSize = false; Interactive = true; Visible = false;
        _filterInput = TextFieldControl.Create();
        _filterInput.X = 31; _filterInput.Y = 340;
        _filterInput.ControlSize = _filterInput.ViewSize = new Point(202, 22);
        _filterInput.MaxLength = 14; _filterInput.FontSize = 10;
        _filterInput.BackgroundColor = new Color(10, 9, 8);
        _filterInput.BorderColor = new Color(108, 82, 42);
        _filterInput.BorderThickness = 1;
        _filterInput.Placeholder = "Item name"; _filterInput.Visible = false;
        Controls.Add(_filterInput);
    }

    public IEnumerable<string> GetPreloadTexturePaths()
    {
        yield return AtlasPath;
        yield return "Interface/GFx/MacroSub_I1.ozd";
        foreach (var file in ListFiles) yield return "Interface/" + file;
        yield return SkillIconDatabase.NewUiAtlas;
        yield return "Interface/newui_command.OZJ";
    }

    public override async Task Load()
    {
        await base.Load();
        _atlas = await TextureLoader.Instance.PrepareAndGetTexture(AtlasPath);
        _subAtlas = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/MacroSub_I1.ozd");
        _icons[SkillIconDatabase.NewUiAtlas] = await TextureLoader.Instance.PrepareAndGetTexture(SkillIconDatabase.NewUiAtlas);
        _icons["Interface/newui_command.OZJ"] = await TextureLoader.Instance.PrepareAndGetTexture("Interface/newui_command.OZJ");
        for (int i = 0; i < ListFiles.Length; i++)
            _listSkin[i] = await TextureLoader.Instance.PrepareAndGetTexture("Interface/" + ListFiles[i]);
    }

    private bool OwnsCurrentCharacter()
    {
        var state = MuGame.Network?.GetCharacterState();
        return state != null && state.Name == _owner && (int)state.Class == _ownerClass;
    }

    private void RestoreSavedConfiguration(CharacterState state)
    {
        if (OwnsCurrentCharacter()) return;
        var saved = state.GetHelperConfiguration();
        if (saved != null) Receive(state.Name, (int)state.Class, saved);
    }

    public void Toggle()
    {
        if (Visible) { CloseWindow(); return; }
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || !state.IsInGame) return;
        RestoreSavedConfiguration(state);
        if (!OwnsCurrentCharacter())
        {
            ResetSession(); _owner = state.Name; _ownerClass = (int)state.Class;
        }
        (Scene as Client.Main.Scenes.GameScene)?.StopMuHelper();
        _draft = new(_saved.ToArray()); _dirty = false;
        _tab = _modal = _page = 0; _picker = -1;
        _status = _pending == null ? string.Empty : "Waiting for server confirmation...";
        LoadNames(); Visible = true; PositionWindow(); BringToFront();
        if (Scene != null) { Scene.FocusControl = this; Scene.Cursor.BringToFront(); }
    }

    public void CloseWindow()
    {
        Visible = false; _picker = -1; _modal = 0; _optionSnapshot = null;
        _filterInput.Blur(); _filterInput.Visible = false;
        if (Scene?.FocusControl == this) Scene.FocusControl = null;
    }

    public void ResetSession()
    {
        CloseWindow(); _owner = null; _pending = null; _dirty = false; _revision = -1; _choiceRevision = -2;
        _choices = Array.Empty<SkillEntryState>();
        _saved = MuHelperConfigurationData.Defaults(); _draft = MuHelperConfigurationData.Defaults();
        _skills = Array.Empty<SkillEntryState>(); _status = string.Empty;
    }

    public void Receive(string owner, int characterClass, byte[] data)
    {
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || state.Name != owner || (int)state.Class != characterClass || data?.Length != MuHelperConfigurationData.Length) return;
        if (_owner != owner || _ownerClass != characterClass) { ResetSession(); _owner = owner; _ownerClass = characterClass; }
        _saved = new(data);
        if (_pending != null)
        {
            bool confirmed = _pending.AsSpan().SequenceEqual(data);
            _pending = null;
            _status = confirmed ? "Configuration saved." : "Server settings differ. Reopen to reload them.";
            if (!confirmed) return;
            _dirty = false;
        }
        if (!_dirty) { _draft = new(data); LoadNames(); }
        else _status = "Server settings received; your unsaved edits were kept.";
    }

    public MuHelperConfigurationData SavedConfigurationForCurrentCharacter()
    {
        var state = MuGame.Network?.GetCharacterState();
        if (state == null || !state.IsInGame) return null;
        RestoreSavedConfiguration(state);
        if (!OwnsCurrentCharacter()) { ResetSession(); _owner = state.Name; _ownerClass = (int)state.Class; }
        if (_pending != null || Visible && _dirty) return null;
        return new MuHelperConfigurationData(_saved.ToArray());
    }

    private void LoadNames()
    {
        _filters.Clear();
        for (int i = 0; i < 12; i++)
        {
            string name = _draft.ItemName(i);
            if (!string.IsNullOrWhiteSpace(name)) _filters.Add(name);
        }
        _filterInput.Value = string.Empty; _selectedFilter = -1; _filterScroll = 0;
    }

    private async Task SaveAsync()
    {
        if (_pending != null || !OwnsCurrentCharacter()) return;
        if (_draft.Flag(1, 7) && !string.IsNullOrWhiteSpace(_filterInput.Value)) { _status = "Press Add to include the item name first."; return; }
        try { StoreFilters(); }
        catch (ArgumentException) { _status = "Item filters: use up to 14 ASCII characters."; return; }
        var snapshot = _draft.ToArray();
        _pending = snapshot; _deadline = DateTime.UtcNow.AddSeconds(10);
        _status = "Waiting for server confirmation...";
        string owner = _owner; int characterClass = _ownerClass;
        bool sent = await MuGame.Network.GetCharacterService().SaveMuHelperConfigurationAsync(snapshot);
        if (!sent) MuGame.ScheduleOnMainThread(() =>
        {
            if (_owner == owner && _ownerClass == characterClass && ReferenceEquals(_pending, snapshot))
            { _pending = null; _status = "Could not send settings. Your edits were kept."; }
        });
    }

    private int WindowHeight => _modal is 1 or 2 ? 495 : _modal != 0 ? 380 : _picker >= 0 ? 222 + Math.Max(1, (Math.Min(20, Math.Max(0, PickerSkills().Length - _page * 20)) + 4) / 5) * 54 : Height;

    private void PositionWindow()
    {
        ControlSize = ViewSize = new Point(Width, WindowHeight);
        var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
        Scale = MathHelper.Clamp(Math.Min((size.X - 20f) / Width, (size.Y - 20f) / WindowHeight), .1f, 1f);
        X = (size.X - DisplaySize.X) / 2; Y = (size.Y - DisplaySize.Y) / 2;
    }

    public override void Update(GameTime gameTime)
    {
        if (!Visible) return;
        if (!OwnsCurrentCharacter()) { ResetSession(); return; }
        PositionWindow();
        if (_pending != null && DateTime.UtcNow > _deadline)
        { _pending = null; _status = "Save not confirmed. Your edits were kept; try saving again."; }
        var state = MuGame.Network.GetCharacterState();
        if (_revision != state.SkillListRevision)
        {
            _revision = state.SkillListRevision;
            _skills = state.GetSkills().Where(s => s.SkillId > 0 && !DarkRavenCommands.IsCommandSkill(s.SkillId)
                && !CastleSiegeSkillRules.IsCastleSiegeOnly(s.SkillId)).ToArray();
        }
        bool editingNames = _tab == 1 && _picker < 0 && _modal == 0;
        _filterInput.Visible = editingNames;
        _filterInput.Interactive = _pending == null && _draft.Flag(1, 7);
        _filterInput.FontSize = 10 * Scale;
        if (!editingNames || !_filterInput.Interactive) _filterInput.Blur();
        base.Update(gameTime);
        var game = MuGame.Instance;
        if (Scene?.IsPointerInputBlockedFor(this) == true) return;
        if (game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
        {
            Scene?.ConsumeKeyboardEscape();
            if (_modal != 0) CancelOptions();
            else if (_picker >= 0) _picker = -1;
            else CloseWindow();
            return;
        }
        _mouse = new((int)((game.UiMouseState.X - DisplayPosition.X) / Scale), (int)((game.UiMouseState.Y - DisplayPosition.Y) / Scale));
        if (editingNames && _pending == null)
        {
            if (_filterInput.IsFocused && game.Keyboard.IsKeyDown(Keys.Enter) && game.PrevKeyboard.IsKeyUp(Keys.Enter))
            { Scene?.ConsumeKeyboardEnter(); AddFilter(); return; }
            int wheel = game.UiMouseState.ScrollWheelValue - game.PrevUiMouseState.ScrollWheelValue;
            if (wheel != 0 && FilterList.Contains(_mouse))
            { ScrollFilters(wheel > 0 ? -1 : 1); Scene?.SetMouseInputConsumed(); }
        }
        bool left = game.UiMouseState.LeftButton == ButtonState.Pressed && game.PrevUiMouseState.LeftButton == ButtonState.Released;
        _rightClick = game.UiMouseState.RightButton == ButtonState.Pressed && game.PrevUiMouseState.RightButton == ButtonState.Released;
        if ((!left && !_rightClick) || !DisplayRectangle.Contains(game.UiMouseState.Position)) return;
        Scene?.SetMouseInputConsumed();
        if (_filterInput.Visible && _filterInput.DisplayRectangle.Contains(game.UiMouseState.Position)) return;
        _filterInput.Blur();
        if (Scene != null) Scene.FocusControl = this;
        _click = _mouse; Build(null); _click = null;
    }

    private static Rectangle Slot(int index) => index < 3
        ? new Rectangle(46 + index * 99, 330, 32, 42)
        : new Rectangle(42 + (index - 3) * 54, 518, 32, 42);
    private bool IsBuff(ushort id)
    {
        var definition = SkillDatabase.GetSkillDefinition(id);
        if (definition?.Type is not (TypeSkill.Buff or TypeSkill.FriendlySkill)) return false;
        return Client.Main.Objects.Effects.Season6BuffMapping.TryGetEffectIdFromSkillId(id, out _)
            || definition.SkillBrand is > 0 and < 300
                && Client.Main.Objects.Effects.Season6BuffMapping.TryGetEffectIdFromSkillId((ushort)definition.SkillBrand, out _);
    }
    private SkillEntryState[] PickerSkills()
    {
        bool buffs = _picker >= 3;
        if (_choiceRevision != _revision || _choiceBuff != buffs)
        {
            _choiceRevision = _revision; _choiceBuff = buffs;
            _choices = _skills.Where(s => buffs ? IsBuff(s.SkillId)
                : SkillDatabase.GetSkillDefinition(s.SkillId)?.Type is not (TypeSkill.Buff or TypeSkill.FriendlySkill)).ToArray();
        }
        return _choices;
    }

    private void Build(SpriteBatch batch)
    {
        if (_modal != 0) { BuildOptions(batch); return; }
        if (_picker >= 0) { BuildPicker(batch); return; }
        Label(batch, "MU Helper", new(35, 37, 254, 22), Gold, 11, true);
        CloseIcon(batch);
        Button(batch, new(24, 80, 91, 28), "Hunting", () => _tab = 0, selected: _tab == 0);
        Button(batch, new(119, 80, 91, 28), "Obtaining", () => _tab = 1, selected: _tab == 1);
        Button(batch, new(214, 80, 91, 28), "Others", () => _tab = 2, selected: _tab == 2);
        if (_tab == 0) BuildHunting(batch);
        else if (_tab == 1) BuildObtaining(batch);
        else BuildOthers(batch);
        Label(batch, _status, new(22, 610, 288, 15), Gold, 9, true);
        Button(batch, new(66, 580, 90, 27), "Reset", () => { _draft = MuHelperConfigurationData.Defaults(); LoadNames(); _dirty = true; _status = "Defaults restored. Press Save to keep them."; });
        Button(batch, SaveButton, _pending == null ? "Save" : "Saving...", () => { _ = SaveAsync(); });
    }

    private void BuildHunting(SpriteBatch batch)
    {
        Section(batch, new(23, 111, 286, 37));
        Label(batch, "Range", new(30, 120, 49, 20), Ink, 10);
        for (int i = 1; i <= 8; i++)
        {
            int range = i;
            Button(batch, new(86 + (i - 1) * 27, 117, 25, 25), i.ToString(),
                () => { _draft.SetBits(2, 0, 15, range); _dirty = true; }, selected: i <= _draft.Bits(2, 0, 15));
        }
        Section(batch, new(23, 151, 286, 141));
        CheckAt(batch, 31, 160, "Regular attack", 29, 3, width: 123);
        CheckAt(batch, 178, 160, "Potion", 25, 0, width: 76);
        Gear(batch, new(282, 160, 20, 20), () => OpenOptions(4));
        int root = _ownerClass >> 4;
        if (root is 2 or 5)
        {
            CheckAt(batch, 31, 185, root == 2 ? "Auto Heal" : "Drain Life", 25, root == 2 ? 1 : 2);
            Gear(batch, new(282, 185, 20, 20), () => OpenOptions(5));
        }
        CheckAt(batch, 31, 213, "Long-distance counterattack", 25, 3, width: 250);
        CheckAt(batch, 31, 239, "Original position", 25, 4, width: 250);
        ValueField(batch, new(75, 264, 38, 19), _draft.Word(3), d => _draft.SetWord(3, Math.Clamp(_draft.Word(3) + d, 1, 3600)));
        Label(batch, "Sec", new(118, 264, 50, 19), Ink, 9);
        Section(batch, new(23, 297, 286, 107));
        if (batch != null)
        {
            Line(batch, new(116, 302, 1, 96)); Line(batch, new(213, 302, 1, 96));
        }
        for (int i = 0; i < 3; i++) SkillSlot(batch, i);
        Label(batch, "Con", new(131, 379, 41, 20), Ink, 9);
        Gear(batch, new(181, 379, 20, 20), () => OpenOptions(1));
        Label(batch, "Con", new(230, 379, 41, 20), Ink, 9);
        Gear(batch, new(280, 379, 20, 20), () => OpenOptions(2));
        Section(batch, new(23, 409, 286, 66));
        CheckAt(batch, 31, 419, "Combo", 25, 5, () =>
        {
            if (!_draft.Flag(25, 5)) return;
            _draft.SetFlag(26, 3, false); _draft.SetFlag(26, 4, false);
            _draft.SetFlag(27, 0, false); _draft.SetFlag(27, 1, false);
        }, available: root == 1);
        Section(batch, new(23, 480, 286, 91));
        CheckAt(batch, 31, 487, "Buffs", 26, 2);
        for (int i = 3; i < 6; i++) SkillSlot(batch, i);
        Gear(batch, new(282, 486, 20, 20), () => _tab = 2);
    }

    private void BuildOthers(SpriteBatch batch)
    {
        Section(batch, new(23, 113, 286, 458));
        CheckAt(batch, 31, 130, "Party support", 25, 6);
        Gear(batch, new(282, 130, 20, 20), () => OpenOptions(3));
        Number(batch, 175, "Buff interval", _draft.Word(15), d => _draft.SetWord(15, Math.Clamp(_draft.Word(15) + d * 5, 0, 3600)), "s");
        CheckAt(batch, 31, 217, "Buff duration for party", 26, 0, width: 250);
        CheckAt(batch, 31, 260, "Accept friends automatically", 29, 1, width: 250);
        CheckAt(batch, 31, 296, "Accept guild automatically", 29, 2, width: 250);
        if ((_ownerClass >> 4) == 4)
        {
            CheckAt(batch, 31, 350, "Use Dark Raven", 26, 1, width: 145);
            Button(batch, new(183, 350, 115, 25), _draft.Bits(28, 0, 255) switch { 1 => "Automatic", 2 => "With owner", _ => "Cease" },
                () => { _draft.SetBits(28, 0, 255, (_draft.Bits(28, 0, 255) + 1) % 3); _dirty = true; });
        }
    }

    private void BuildObtaining(SpriteBatch batch)
    {
        Section(batch, new(23, 111, 286, 37));
        Label(batch, "Range", new(30, 120, 49, 20), Ink, 10);
        for (int i = 1; i <= 8; i++)
        {
            int range = i;
            Button(batch, new(86 + (i - 1) * 27, 117, 25, 25), i.ToString(),
                () => { _draft.SetBits(2, 4, 15, range); _dirty = true; }, selected: i <= _draft.Bits(2, 4, 15));
        }
        Section(batch, new(23, 151, 286, 420));
        CheckAt(batch, 31, 161, "Repair equipment", 27, 5);
        CheckAt(batch, 31, 189, "Pick all nearby items", 27, 6, () => _draft.SetFlag(27, 7, !_draft.Flag(27, 6)));
        CheckAt(batch, 31, 217, "Pick selected items", 27, 7, () => _draft.SetFlag(27, 6, !_draft.Flag(27, 7)));
        // Keep the two columns' hit regions disjoint, including their labels.
        CheckAt(batch, 47, 248, "Jewels / Gems", 1, 3, width: 124);
        CheckAt(batch, 180, 248, "Ancient", 1, 4, width: 100);
        CheckAt(batch, 47, 277, "Zen", 1, 6, width: 124);
        CheckAt(batch, 180, 277, "Excellent", 1, 5, width: 100);
        CheckAt(batch, 31, 309, "Add extra item", 1, 7, width: 208);
        Button(batch, new(245, 339, 58, 24), "Add", AddFilter, available: _draft.Flag(1, 7) && _filters.Count < 12);
        Label(batch, "Other items", new(31, 372, 220, 21), Ink, 10);
        BuildFilterList(batch);
        Button(batch, new(239, 542, 64, 25), "Delete", DeleteFilter, available: _selectedFilter >= 0 && _selectedFilter < _filters.Count);
    }

    private void StoreFilters()
    {
        for (int i = 0; i < 12; i++) _draft.SetItemName(i, i < _filters.Count ? _filters[i] : string.Empty);
    }

    private void AddFilter()
    {
        if (_pending != null || !_draft.Flag(1, 7)) return;
        string value = _filterInput.Value.Trim();
        if (value.Length == 0) { _status = "Enter an item name."; return; }
        if (value.Length > 14 || value.Any(c => c > 127 || c == '\0'))
        { _status = "Use up to 14 ASCII characters."; return; }
        if (_filters.Count >= 12) { _status = "The item list is full (12)."; return; }
        if (_filters.Any(name => name.Equals(value, StringComparison.OrdinalIgnoreCase)))
        { _status = "This item name is already in the list."; return; }
        _filters.Add(value); StoreFilters(); _dirty = true; _status = string.Empty;
        _selectedFilter = _filters.Count - 1; _filterScroll = Math.Max(0, _filters.Count - 6);
        _filterInput.Value = string.Empty; _filterInput.Focus();
    }

    private void DeleteFilter()
    {
        if (_pending != null || _selectedFilter < 0 || _selectedFilter >= _filters.Count) return;
        _filters.RemoveAt(_selectedFilter);
        _selectedFilter = Math.Min(_selectedFilter, _filters.Count - 1);
        ScrollFilters(0); StoreFilters(); _dirty = true; _status = string.Empty;
    }

    private void ScrollFilters(int delta) => _filterScroll = Math.Clamp(_filterScroll + delta, 0, Math.Max(0, _filters.Count - 6));

    private void BuildFilterList(SpriteBatch batch)
    {
        if (batch != null)
        {
            batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(FilterList.X + 4, FilterList.Y + 4, FilterList.Width - 8, FilterList.Height - 8), new Color(0, 0, 0, 245));
            // OZT textures are padded by the loader; sample their native dimensions explicitly.
            void Art(int i, Rectangle to, int sourceWidth, int sourceHeight)
            {
                if (_listSkin[i] != null) batch.Draw(_listSkin[i], to, new Rectangle(0, 0, sourceWidth, sourceHeight), Color.White);
            }
            int x = FilterList.X, y = FilterList.Y, w = FilterList.Width, h = FilterList.Height;
            Art(0, new(x, y, 14, 14), 14, 14); Art(1, new(x + w - 14, y, 14, 14), 14, 14);
            Art(2, new(x, y + h - 14, 14, 14), 14, 14); Art(3, new(x + w - 14, y + h - 14, 14, 14), 14, 14);
            Art(4, new(x, y + 14, 14, h - 28), 14, 1); Art(5, new(x + w - 14, y + 14, 14, h - 28), 14, 1);
            Art(6, new(x + 14, y, w - 28, 14), 1, 14); Art(7, new(x + 14, y + h - 14, w - 28, 14), 1, 14);
            batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(291, 413, 7, 102), new Color(44, 29, 20));
            Art(8, new(291, 404, 7, 3), 7, 3); Art(9, new(291, 522, 7, 3), 7, 3);
            int thumbY = 413 + (_filters.Count <= 6 ? 0 : (int)(87f * _filterScroll / (_filters.Count - 6)));
            Art(10, new(291, thumbY, 7, 15), 7, 15);
        }
        for (int row = 0; row < 6 && _filterScroll + row < _filters.Count; row++)
        {
            int index = _filterScroll + row;
            var rect = new Rectangle(43, 409 + row * 18, 236, 18);
            if (batch != null)
            {
                if (index == _selectedFilter) batch.Draw(GraphicsManager.Instance.Pixel, rect, new Color(72, 49, 25, 230));
                Label(batch, _filters[index], new(rect.X + 4, rect.Y, rect.Width - 8, rect.Height), index == _selectedFilter ? Gold : Ink, 10);
            }
            if (_pending == null && _click is { } p && rect.Contains(p) && !_rightClick)
            { _click = null; _selectedFilter = index; }
        }
        if (_pending == null && _click is { } click && !_rightClick)
        {
            if (new Rectangle(287, 398, 15, 16).Contains(click)) { _click = null; ScrollFilters(-1); }
            else if (new Rectangle(287, 516, 15, 16).Contains(click)) { _click = null; ScrollFilters(1); }
            else if (new Rectangle(287, 414, 15, 102).Contains(click))
            {
                _click = null;
                _filterScroll = (int)Math.Round(Math.Clamp((click.Y - 414) / 102f, 0, 1) * Math.Max(0, _filters.Count - 6));
            }
        }
    }

    private void OpenOptions(int modal)
    {
        _optionSnapshot = new(_draft.ToArray()); _optionWasDirty = _dirty; _modal = modal;
    }

    private void CancelOptions()
    {
        if (_optionSnapshot != null) { _draft = _optionSnapshot; _dirty = _optionWasDirty; }
        _optionSnapshot = null; _modal = 0;
    }

    private void ResetOptions()
    {
        var defaults = MuHelperConfigurationData.Defaults();
        if (_modal is 1 or 2)
        {
            int flags = _modal == 1 ? 26 : 27, shift = _modal == 1 ? 3 : 0;
            _draft.SetBits(flags, shift, 31, defaults.Bits(flags, shift, 31));
            _draft.SetWord(_modal == 1 ? 9 : 13, 0);
        }
        else if (_modal is 4 or 5)
        {
            int offset = _modal == 4 || (_ownerClass >> 4) == 2 ? 23 : 24;
            int shift = _modal == 4 ? 0 : 4;
            _draft.SetBits(offset, shift, 15, defaults.Bits(offset, shift, 15));
        }
        else
        {
            _draft.SetFlag(25, 7, defaults.Flag(25, 7));
            _draft.SetBits(24, 0, 15, defaults.Bits(24, 0, 15));
            _draft.SetFlag(26, 0, defaults.Flag(26, 0));
            _draft.SetFlag(29, 1, defaults.Flag(29, 1));
            _draft.SetFlag(29, 2, defaults.Flag(29, 2));
        }
        _dirty = true;
    }

    private void BuildOptions(SpriteBatch batch)
    {
        string title = _modal is 4 or 5 ? "Auto Recovery" : _modal == 3 ? "Party Settings" : "Activation Skill " + _modal;
        Label(batch, title, new(24, 17, 284, 24), Gold, 11, true);
        if (_modal is 4 or 5)
        {
            string caption = _modal == 4 ? "Auto Potion" : (_ownerClass >> 4) == 2 ? "Auto Heal" : "Drain Life";
            Label(batch, caption, new(30, 64, 270, 22), Ink, 11);
            OptionGroup(batch, new(28, 99, 276, 86));
            int offset = _modal == 4 || (_ownerClass >> 4) == 2 ? 23 : 24;
            int shift = _modal == 4 ? 0 : 4;
            HpSegments(batch, 120, offset, shift);
        }
        else if (_modal is 1 or 2)
        {
            int flags = _modal == 1 ? 26 : 27, shift = _modal == 1 ? 3 : 0, delay = _modal == 1 ? 9 : 13;
            CheckAt(batch, 27, 61, "Delay condition setting", flags, shift, () =>
            { if (_draft.Flag(flags, shift)) { _draft.SetFlag(flags, shift + 1, false); _draft.SetFlag(25, 5, false); } }, width: 276);
            OptionGroup(batch, new(27, 91, 278, 56));
            Number(batch, 108, "Interval", _draft.Word(delay), d => _draft.SetWord(delay, Math.Clamp(_draft.Word(delay) + d, 0, 3600)), "s");
            CheckAt(batch, 27, 165, "Monster condition setting", flags, shift + 1, () =>
            { if (_draft.Flag(flags, shift + 1)) { _draft.SetFlag(flags, shift, false); _draft.SetFlag(25, 5, false); } }, width: 276);
            Label(batch, "Basic condition", new(31, 200, 260, 22), Ink, 10);
            OptionGroup(batch, new(27, 228, 278, 66));
            bool conditional = _draft.Flag(flags, shift + 1);
            Radio(batch, new(40, 237, 254, 21), "Monster within hunting range", !_draft.Flag(flags, shift + 2),
                () => _draft.SetFlag(flags, shift + 2, false), conditional);
            Radio(batch, new(40, 266, 254, 21), "Monster attacking me", _draft.Flag(flags, shift + 2),
                () => _draft.SetFlag(flags, shift + 2, true), conditional);
            Label(batch, "Minimum monsters", new(31, 309, 260, 22), Ink, 10);
            OptionGroup(batch, new(27, 339, 278, 73));
            for (int i = 0; i < 4; i++)
            {
                int count = i;
                Radio(batch, new(40 + i % 2 * 130, 348 + i / 2 * 29, 123, 22), (i + 2) + " monsters",
                    _draft.Bits(flags, shift + 3, 3) == i, () => _draft.SetBits(flags, shift + 3, 3, count), conditional);
            }
        }
        else
        {
            OptionGroup(batch, new(27, 59, 278, 118));
            CheckAt(batch, 39, 70, "Heal party members", 25, 7, width: 251);
            HpSegments(batch, 111, 24, 0);
            OptionGroup(batch, new(27, 194, 278, 119));
            CheckAt(batch, 39, 207, "Buff duration for party", 26, 0, width: 251);
            CheckAt(batch, 39, 244, "Accept friends automatically", 29, 1, width: 251);
            CheckAt(batch, 39, 279, "Accept guild automatically", 29, 2, width: 251);
        }
        int footer = WindowHeight - 48;
        PopupButton(batch, new(29, footer, 86, 29), "Initialize", ResetOptions);
        PopupButton(batch, new(123, footer, 86, 29), "Save", () =>
        { _optionSnapshot = null; _modal = 0; _ = SaveAsync(); });
        PopupButton(batch, new(217, footer, 86, 29), "Cancel", CancelOptions);
    }

    private void HpSegments(SpriteBatch batch, int y, int offset, int shift)
    {
        int value = Math.Clamp(_draft.Bits(offset, shift, 15), 1, 10);
        for (int i = 0; i < 10; i++)
        {
            var rect = new Rectangle(103 + i * 13, y, 11, 16);
            if (batch != null)
                batch.Draw(GraphicsManager.Instance.Pixel, rect, i < value ? new Color(255, 139, 0) : new Color(49, 49, 49));
            if (_pending == null && _click is { } p && rect.Contains(p) && !_rightClick)
            { _click = null; _draft.SetBits(offset, shift, 15, i + 1); _dirty = true; }
        }
        Label(batch, "HP Status: " + value * 10 + "%", new(47, y + 24, 238, 21), Gold, 10, true);
    }

    private void Radio(SpriteBatch batch, Rectangle rect, string label, bool selected, Action action, bool available)
    {
        bool enabled = available && _pending == null;
        if (batch != null)
        {
            DrawRadio(batch, new Point(rect.X + 8, rect.Center.Y), selected, enabled);
            Label(batch, label, new(rect.X + 22, rect.Y, rect.Width - 22, rect.Height), enabled ? Ink : Muted, 10);
        }
        if (enabled && _click is { } p && rect.Contains(p) && !_rightClick)
        { _click = null; action(); _dirty = true; }
    }

    // Rasterize indicators because the UI font does not contain Unicode radio glyphs.
    private static void DrawRadio(SpriteBatch batch, Point center, bool selected, bool enabled)
    {
        var pixel = GraphicsManager.Instance.Pixel;
        for (int y = -5; y <= 5; y++)
        {
            int halfWidth = (int)Math.Sqrt(25 - y * y);
            batch.Draw(pixel, new Rectangle(center.X - halfWidth, center.Y + y, halfWidth * 2 + 1, 1), enabled ? Ink : Muted);
        }
        for (int y = -3; y <= 3; y++)
        {
            int halfWidth = (int)Math.Sqrt(9 - y * y);
            batch.Draw(pixel, new Rectangle(center.X - halfWidth, center.Y + y, halfWidth * 2 + 1, 1),
                selected ? enabled ? Gold : Muted : new Color(20, 20, 20));
        }
    }


    private static void OptionGroup(SpriteBatch batch, Rectangle rect)
    {
        if (batch == null) return;
        batch.Draw(GraphicsManager.Instance.Pixel, rect, new Color(0, 0, 0, 48));
        Section(batch, rect);
    }

    private void PopupButton(SpriteBatch batch, Rectangle rect, string label, Action action)
    {
        if (batch != null)
        {
            bool hover = rect.Contains(_mouse) && _pending == null;
            if (_subAtlas != null)
                batch.Draw(_subAtlas, rect, new Rectangle(hover ? 112 : 37, 0, 70, 32), _pending == null ? Color.White : Color.Gray);
            else OptionGroup(batch, rect);
            Label(batch, label, rect, _pending == null ? Ink : Muted, 10, true);
        }
        if (_pending == null && _click is { } p && rect.Contains(p) && !_rightClick)
        { _click = null; action(); }
    }

    private void DrawOptionsFrame(SpriteBatch batch)
    {
        var pixel = GraphicsManager.Instance.Pixel;
        batch.Draw(pixel, new Rectangle(8, 7, Width - 16, WindowHeight - 14), new Color(0, 0, 0, 180));
        Section(batch, new(8, 7, Width - 16, WindowHeight - 14));
        Section(batch, new(12, 11, Width - 24, WindowHeight - 22));
        Line(batch, new(20, 48, Width - 40, 1));
        Line(batch, new(20, WindowHeight - 60, Width - 40, 1));
    }


    private void BuildPicker(SpriteBatch batch)
    {
        var choices = PickerSkills();
        int pages = Math.Max(1, (choices.Length + 19) / 20);
        _page = Math.Clamp(_page, 0, pages - 1);
        int count = Math.Min(20, choices.Length - _page * 20);
        ushort current = (ushort)_draft.Word(MuHelperConfigurationData.SkillOffsets[_picker]);
        string hoveredName = null;
        Label(batch, "Select " + SlotNames[_picker], new(24, 17, 284, 24), Gold, 11, true);
        PopupButton(batch, new(103, 62, 126, 29), _picker == 0 ? "Normal attack" : "Clear slot", () =>
        { _draft.SetWord(MuHelperConfigurationData.SkillOffsets[_picker], 0); _dirty = true; _picker = -1; });
        if (_picker < 0) return;
        for (int i = 0; i < count; i++)
        {
            ushort id = choices[_page * 20 + i].SkillId;
            int rowCount = Math.Min(5, count - i / 5 * 5);
            int rowWidth = (rowCount - 1) * 52 + 36;
            var rect = new Rectangle((Width - rowWidth) / 2 + i % 5 * 52, 112 + i / 5 * 54, 36, 44);
            bool hover = rect.Contains(_mouse);
            if (hover) hoveredName = SkillDatabase.GetSkillName(id);
            if (batch != null)
            {
                if (_atlas != null) batch.Draw(_atlas, rect, new Rectangle(651, 75, 32, 42), Color.White);
                else OptionGroup(batch, rect);
                if (hover || id == current)
                {
                    var edge = new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                    var color = hover ? new Color(241, 205, 106) : Gold;
                    batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(edge.X, edge.Y, edge.Width, 1), color);
                    batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(edge.X, edge.Bottom - 1, edge.Width, 1), color);
                    batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(edge.X, edge.Y, 1, edge.Height), color);
                    batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(edge.Right - 1, edge.Y, 1, edge.Height), color);
                }
                DrawIcon(batch, id, rect);
            }
            if (_pending == null && _click is { } p && rect.Contains(p) && !_rightClick)
            {
                _click = null; _draft.SetWord(MuHelperConfigurationData.SkillOffsets[_picker], id);
                _dirty = true; _picker = -1; return;
            }
        }
        string name = hoveredName ?? (current == 0 ? _picker == 0 ? "Normal attack" : "No skill selected" : SkillDatabase.GetSkillName(current));
        if (count == 0) Label(batch, "No available skills", new(30, 117, 272, 24), Muted, 10, true);
        OptionGroup(batch, new(27, WindowHeight - 104, 278, 32));
        Label(batch, name, new(34, WindowHeight - 101, 264, 26), Gold, 10, true);
        int footer = WindowHeight - 48;
        if (pages > 1)
        {
            Button(batch, new(29, footer, 28, 29), "<", () => _page--, available: _page > 0);
            Label(batch, $"{_page + 1} / {pages}", new(60, footer, 69, 29), Ink, 10, true);
            Button(batch, new(132, footer, 28, 29), ">", () => _page++, available: _page < pages - 1);
        }
        PopupButton(batch, new(217, footer, 86, 29), "Back", () => _picker = -1);
    }


    private void SkillSlot(SpriteBatch batch, int index)
    {
        var rect = Slot(index);
        ushort id = (ushort)_draft.Word(MuHelperConfigurationData.SkillOffsets[index]);
        bool known = id == 0 || _skills.Any(s => s.SkillId == id);
        if (index < 3) Label(batch, index == 0 ? "Basic skill" : "Activation", new(rect.X - 21, 306, 74, 18), Ink, 9, true);
        if (batch != null)
        {
            if (_atlas != null) batch.Draw(_atlas, rect, new Rectangle(651, 75, 32, 42), Color.White);
            if (id != 0) DrawIcon(batch, id, rect, known ? Color.White : Color.Gray);
            // Empty slots keep the original recessed frame, without a placeholder plus.
            if (rect.Contains(_mouse)) Label(batch, id == 0 ? "Click to choose a skill" : SkillDatabase.GetSkillName(id) + (known ? "" : " (not available)"), new(22, 610, 288, 15), Gold, 9, true);
        }
        if (_pending == null && _click is { } point && rect.Contains(point))
        {
            _click = null;
            if (_rightClick) { _draft.SetWord(MuHelperConfigurationData.SkillOffsets[index], 0); _dirty = true; }
            else { _picker = index; _page = 0; }
        }
    }

    private void DrawIcon(SpriteBatch batch, ushort id, Rectangle rect, Color? tint = null)
    {
        var info = SkillIconDatabase.GetIcon(id);
        if (!info.HasValue && SkillDatabase.GetSkillDefinition(id) is { SkillBrand: > 0 and < 300 } definition)
            info = SkillIconDatabase.GetIcon((ushort)definition.SkillBrand);
        if (info is { } icon && _icons.TryGetValue(icon.TexturePath, out var texture) && texture != null)
            batch.Draw(texture, new Rectangle(rect.Center.X - 10, rect.Center.Y - 14, 20, 28), icon.SourceRectangle, tint ?? Color.White);
        else Label(batch, id.ToString(), rect, tint ?? Ink, 8, true);
    }

    private void Check(SpriteBatch batch, int y, string label, int offset, int bit, Action after = null)
    {
        bool value = _draft.Flag(offset, bit);
        if (batch != null)
        {
            if (_atlas != null) batch.Draw(_atlas, new Rectangle(16, y + 3, 12, 12), new Rectangle(value ? 344 : 359, 361, 13, 13), Color.White);
            else Label(batch, value ? "[x]" : "[ ]", new(16, y, 18, 20), Gold, 9);
            Label(batch, label, new(33, y, 178, 20), Ink, 9);
        }
        if (_pending == null && _click is { } point && new Rectangle(16, y, 186, 20).Contains(point) && !_rightClick)
        { _click = null; _draft.SetFlag(offset, bit, !value); after?.Invoke(); _dirty = true; }
    }

    private void Number(SpriteBatch batch, int y, string label, int value, Action<int> change, string suffix = "")
    {
        Label(batch, label, new(40, y, 156, 20), Ink, 10);
        Button(batch, new(210, y, 20, 20), "-", () => { change(-1); _dirty = true; });
        Label(batch, $"{value}{suffix}", new(231, y, 49, 20), Gold, 9, true);
        Button(batch, new(282, y, 20, 20), "+", () => { change(1); _dirty = true; });
    }

    private void Button(SpriteBatch batch, Rectangle rect, string label, Action action, bool allowPending = false, bool selected = false, bool available = true)
    {
        bool enabled = available && (allowPending || _pending == null);
        if (batch != null)
        {
            var source = selected ? new Rectangle(412, 259, 61, 24)
                : new Rectangle(enabled && rect.Contains(_mouse) ? 431 : 334, 185, 88, 29);
            Slice(batch, rect, source, 4, enabled ? Color.White : Color.Gray);
            Label(batch, label, rect, enabled ? selected ? Gold : Ink : Muted, 10, true);
        }
        if (enabled && _click is { } point && rect.Contains(point) && !_rightClick) { _click = null; action(); }
    }

    private void Gear(SpriteBatch batch, Rectangle rect, Action action)
    {
        bool enabled = _pending == null;
        if (batch != null && _atlas != null)
            batch.Draw(_atlas, rect, new Rectangle(!enabled ? 361 : rect.Contains(_mouse) ? 392 : 330, 290, 28, 28), Color.White);
        if (enabled && _click is { } p && rect.Contains(p) && !_rightClick) { _click = null; action(); }
    }

    private void CloseIcon(SpriteBatch batch)
    {
        if (batch != null && _atlas != null) batch.Draw(_atlas, CloseButton, new Rectangle(651, 159, 22, 22), Color.White);
        if (_click is { } p && CloseButton.Contains(p) && !_rightClick) { _click = null; CloseWindow(); }
    }

    private void ValueField(SpriteBatch batch, Rectangle rect, int value, Action<int> change)
    {
        if (batch != null) { Section(batch, rect); Label(batch, value.ToString(), rect, Ink, 10, true); }
        if (_pending == null && _click is { } p && rect.Contains(p))
        { _click = null; change(_rightClick ? -1 : 1); _dirty = true; }
    }

    private void CheckAt(SpriteBatch batch, int x, int y, string label, int offset, int bit, Action after = null, bool available = true, int width = 180)
    {
        bool enabled = available && _pending == null;
        if (batch != null)
        {
            if (_atlas != null) batch.Draw(_atlas, new Rectangle(x, y + 4, 12, 12),
                new Rectangle(_draft.Flag(offset, bit) ? 344 : 359, 361, 13, 13), enabled ? Color.White : Color.Gray);
            Label(batch, label, new(x + 18, y, width - 18, 20), enabled ? Ink : Muted, 10);
        }
        if (enabled && _click is { } p && new Rectangle(x, y, width, 20).Contains(p) && !_rightClick)
        { _click = null; _draft.SetFlag(offset, bit, !_draft.Flag(offset, bit)); after?.Invoke(); _dirty = true; }
    }

    private static void Line(SpriteBatch batch, Rectangle rect) => batch.Draw(GraphicsManager.Instance.Pixel, rect, new Color(75, 58, 35));

    private static void Section(SpriteBatch batch, Rectangle rect)
    {
        if (batch == null) return;
        Line(batch, new(rect.X, rect.Y, rect.Width, 1)); Line(batch, new(rect.X, rect.Bottom - 1, rect.Width, 1));
        Line(batch, new(rect.X, rect.Y, 1, rect.Height)); Line(batch, new(rect.Right - 1, rect.Y, 1, rect.Height));
    }

    private void Slice(SpriteBatch batch, Rectangle rect, Rectangle source, int edge, Color tint)
    {
        if (_atlas == null) return;
        for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
        {
            int sw = col == 1 ? source.Width - edge * 2 : edge, sh = row == 1 ? source.Height - edge * 2 : edge;
            int dw = col == 1 ? rect.Width - edge * 2 : edge, dh = row == 1 ? rect.Height - edge * 2 : edge;
            int sx = source.X + (col == 0 ? 0 : col == 1 ? edge : source.Width - edge);
            int sy = source.Y + (row == 0 ? 0 : row == 1 ? edge : source.Height - edge);
            int dx = rect.X + (col == 0 ? 0 : col == 1 ? edge : rect.Width - edge);
            int dy = rect.Y + (row == 0 ? 0 : row == 1 ? edge : rect.Height - edge);
            batch.Draw(_atlas, new Rectangle(dx, dy, dw, dh), new Rectangle(sx, sy, sw, sh), tint);
        }
    }

    private static void Label(SpriteBatch batch, string value, Rectangle rect, Color color, float size, bool centered = false)
    {
        if (batch == null) return;
        var font = GraphicsManager.Instance.Font;
        value ??= string.Empty;
        var measure = font.MeasureString(value);
        float scale = Math.Min(size / Constants.BASE_FONT_SIZE, rect.Width / Math.Max(1f, measure.X));
        batch.DrawString(font, value, new Vector2(centered ? rect.Center.X - measure.X * scale / 2 : rect.X, rect.Center.Y - measure.Y * scale / 2), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || GraphicsManager.Instance.Font == null) return;
        var batch = GraphicsManager.Instance.Sprite;
        var transform = Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0) * UiScaler.SpriteTransform;
        using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, transform: transform))
        {
            if (_modal != 0 || _picker >= 0) DrawOptionsFrame(batch);
            else
            {
                // Back only the usable interior. The textured frame, ornament and rounded
                // corners retain their original alpha instead of exposing a rectangular backing.
                var backing = _atlas == null
                    ? new Rectangle(0, 0, Width, WindowHeight)
                    : new Rectangle(20, 73, Width - 40, WindowHeight - 91);
                batch.Draw(GraphicsManager.Instance.Pixel, backing, new Color(13, 11, 9));
                if (_atlas != null)
                {
                    Slice(batch, new Rectangle(6, 23, 320, WindowHeight - 23), new Rectangle(0, 0, 320, 380), 14, Color.White);
                    // Keep the native ornament, header and corners at their original pixel size.
                    // The next atlas sprite starts below the 74px header; never include its top rows.
                    batch.Draw(_atlas, new Rectangle(0, 0, 332, 74), new Rectangle(648, 0, 332, 74), Color.White);
                }
            }
            Build(batch);
        }
        if (_tab == 1 && _picker < 0 && _modal == 0 && _filterInput.Visible)
            _filterInput.Draw(gameTime);
    }
}
