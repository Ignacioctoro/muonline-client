using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Core.Items.Tooltips;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Networking.PacketHandling.Handlers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Extensions.Logging;

namespace Client.Main.Controls.UI.Game.Quest;

/// <summary>NewUI journal backed by the Season 6 quest protocol.</summary>
public sealed class MyQuestsControl : UIControl
{
    private static MyQuestsControl _instance;
    public static MyQuestsControl Instance => _instance ??= new();
    public override bool NonDisposable => true;
    public override bool BlocksPointerInput => true;
    private const int Width = 530, Height = 620;
    private static readonly string[] Categories = { "General Quest", "Class Upgrade Quest", "Repeatable Quest" };
    private static class Theme
    {
        public static readonly Color Background = new(17, 15, 12);
        public static readonly Color Gold = new(230, 188, 72);
        public static readonly Color Text = new(205, 205, 205);
        public static readonly Color Complete = new(110, 220, 90);
        public static readonly Color Reward = new(120, 155, 255);
    }
    private Texture2D _atlas, _frame, _button, _hover;
    private RenderTarget2D _surface;
    private SpriteFont _font;
    private readonly BlendState _surfaceBlend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
    };
    private CharacterState State => MuGame.Network?.GetCharacterState();
    private QuestJournal Journal => State?.Quests;
    private readonly List<QuestJournal.Entry> _rows = new();
    private readonly List<string> _titles = new();
    private readonly List<(string Text, Color Color, InventoryItem Item)> _lines = new();
    private readonly List<(string Text, Color Color, InventoryItem Item)> _rewards = new();
    private int _rewardScroll;
    private int _category, _selected, _listScroll, _detailScroll, _revision = -1;
    private bool _npcSession, _busy;
    private double _nextRefresh, _pendingSince;
    private string _message = "";
    private QuestJournal.Entry Selected => _selected >= 0 && _selected < _rows.Count ? _rows[_selected] : null;
    private static readonly Rectangle ListRect = new(215, 86, 291, 184);
    private static readonly Rectangle DetailRect = new(215, 283, 291, 158);
    private static readonly Rectangle RewardRect = new(215, 444, 291, 102);
    private static readonly Rectangle ActionRect = new(376, 558, 130, 36);
    private static readonly Rectangle CancelRect = new(234, 558, 132, 36);
    private static readonly Rectangle CloseRect = new(488, 40, 24, 24);

    private MyQuestsControl()
    {
        AutoViewSize = false;
        ControlSize = ViewSize = new(Width, Height);
        Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
        Interactive = true;
        Visible = false;
    }

    public override async Task Load()
    {
        _frame = await TextureLoader.Instance.PrepareAndGetTexture(NewUiFrameRenderer.PersonalShopAtlasPath);
        _atlas = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/MyQuestInfo_I1.ozd");
        _button = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd");
        _hover = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_l02.ozd");
        _font = GraphicsManager.Instance.Font;
        await base.Load();
        Rebuild();
    }

    public void ResetForCharacter()
    {
        Visible = false; _npcSession = false; _busy = false;
        _rows.Clear(); _titles.Clear(); _lines.Clear(); _rewards.Clear();
        Journal?.Reset(); _revision = -1;
    }

    public void Toggle()
    {
        if (Visible) CloseWindow();
        else
        {
            _npcSession = false; _busy = false;
            Journal?.CloseNpc();
            _category = 0; _selected = 0; _listScroll = _detailScroll = 0;
            Visible = true; BringToFront();
            _message = "Loading quests...";
            Rebuild();
            _ = Send(0x1A);
        }
    }

    public async Task OpenNpcAsync(ushort networkId)
    {
        if (_npcSession) CloseWindow();
        _busy = false;
        Journal?.CloseNpc();
        _npcSession = true;
        Visible = true; BringToFront();
        _category = 0; _selected = 0; _listScroll = _detailScroll = 0;
        _message = "Loading NPC quests...";
        Rebuild();
        try
        {
            // Packets are sent in order on the same connection. No timer-based
            // prediction of quest acceptance is necessary.
            await MuGame.Network.GetCharacterService().SendTalkToNpcRequestAsync(networkId);
            await Send(0x30);
            await Send(0x1A);
        }
        catch (Exception ex) { ReportError(ex); }
    }

    public void CloseWindow()
    {
        Visible = false;
        if (_npcSession) _ = MuGame.Network?.GetCharacterService().SendCloseNpcRequestAsync();
        _npcSession = false;
        Journal?.CloseNpc();
        Scene?.SetMouseInputConsumed();
    }

    public void OnResponse(byte sub)
    {
        if (sub == 0x0A && !_npcSession) return;
        _busy = false;
        _message = Journal?.Notice ?? "";
        if (sub == 0x0A)
        {
            Visible = true; BringToFront();
        }
        Rebuild();
        if (sub == 0x1A && Selected is { Active: true } entry) _ = Send(0x1B, entry);
        if (sub is 0x0D or 0x0F)
        {
            _ = Send(0x1A);
            if (_npcSession) _ = Send(0x30);
        }
    }

    private async Task Send(byte sub, QuestJournal.Entry entry = null)
    {
        try { await MuGame.Network.GetCharacterService().SendQuestRequestAsync(sub, entry?.Group ?? 0, entry?.Number ?? 0); }
        catch (Exception ex) { MuGame.ScheduleOnMainThread(() => ReportError(ex)); }
    }

    private void ReportError(Exception ex)
    {
        _busy = false;
        _message = "Could not contact the server. Try again.";
        MuGame.AppLoggerFactory?.CreateLogger<MyQuestsControl>().LogWarning(ex, "Quest request failed.");
    }

    private void Rebuild()
    {
        var previous = Selected;
        _rows.Clear();
        if (Journal == null) return;
        _revision = Journal.Revision;
        if (_category == 1)
        {
            if (State.HasLegacyQuestStateList)
                for (ushort i = 0; i < 7; i++)
                    if (State.GetLegacyQuestState((byte)i) != MUnique.OpenMU.Network.Packets.LegacyQuestState.Undefined)
                        _rows.Add(new(0, i, true));
        }
        else
        {
            foreach (var entry in Journal.Active)
                if ((_category == 2) == (entry.Group == 19)) _rows.Add(entry);
            if (_npcSession)
                foreach (var entry in Journal.Available)
                    if ((_category == 2) == (entry.Group == 19) && !_rows.Any(e => e.Group == entry.Group)) _rows.Add(entry);
        }
        int old = previous == null ? -1 : _rows.FindIndex(e => e.Group == previous.Group && e.Number == previous.Number);
        _selected = old >= 0 ? old : Math.Clamp(_selected, 0, Math.Max(0, _rows.Count - 1));
        _listScroll = Math.Clamp(_listScroll, 0, Math.Max(0, _rows.Count - 7));
        _titles.Clear();
        foreach (var row in _rows) _titles.Add(Title(row));
        BuildLines();
    }

    private void AddLine(string text, Color color, InventoryItem item = null, bool reward = false)
    {
        if (_font == null) return;
        foreach (var line in RequestDialog.WrapMessage(text, DetailRect.Width - 20, t => _font.MeasureString(t).X * 0.39f).Split('\n'))
            (reward ? _rewards : _lines).Add((line, color, item));
    }
    private static InventoryItem Item(ushort id, byte[] raw)
    {
        var definition = ItemDatabase.GetItemDefinition((byte)(id >> 9), (short)(id & 511));
        return definition == null ? null : new InventoryItem(definition, Point.Zero, raw);
    }
    private static string ItemName(ushort id) => ItemDatabase.GetItemName((byte)(id >> 9), (short)(id & 511)) ?? $"Item {id >> 9}:{id & 511}";

    private void BuildLines()
    {
        _lines.Clear(); _rewards.Clear();
        var entry = Selected;
        if (entry == null)
        {
            AddLine(_category == 1 ? "Class quest state has not arrived yet." : "No quests in this category. Visit the Elf Soldier to see available missions.", Theme.Text);
            return;
        }
        if (entry.Group == 0)
        {
            var data = LegacyQuestHandler.BuildQuestData((byte)entry.Number, State.GetLegacyQuestState((byte)entry.Number), State, null);
            AddLine(data.NpcName + " - " + data.StateText, Theme.Gold);
            AddLine(data.Description, Theme.Text);
            AddLine("Requirements", Theme.Gold);
            foreach (var requirement in data.Requirements)
                AddLine($"{requirement.Label}: {requirement.CurrentValue} / {requirement.RequiredValue}", requirement.IsMet ? Theme.Complete : Theme.Text);
            foreach (var item in data.RequiredItems)
                AddLine($"{item.Name}: {item.CurrentCount}/{item.RequiredCount}", item.IsMet ? Theme.Complete : Theme.Text,
                    Item((ushort)((item.Group << 9) | item.Id), Array.Empty<byte>()));
            AddLine("Accept and complete this quest by talking to its NPC.", Theme.Gold);
        }
        else
        {
            var info = QuestCatalog.Find(entry);
            if (info != null)
            {
                AddLine(NpcDatabase.GetNpcName(info.Npc), Theme.Gold);
                AddLine($"Level: {info.MinLevel} - {info.MaxLevel}", Theme.Text);
            }
            if (entry.Active && Journal.Details.TryGetValue(entry.Group, out var detail) && detail.Number == entry.Number)
            {
                AddLine("Requirements", Theme.Gold);
                foreach (var condition in detail.Conditions)
                {
                    string name = condition.Type switch
                    {
                        1 => "Hunt " + NpcDatabase.GetNpcName(condition.Id),
                        4 => ItemName(condition.Id), 8 => "Level", 261 => "Zen",
                        2 => "Learn skill " + condition.Id, 16 => "Tutorial action", 32 => "Receive NPC buff",
                        _ => "Objective " + condition.Type
                    };
                    AddLine($"{name}: {condition.Current:N0}/{condition.Required:N0}", condition.Current >= condition.Required ? Theme.Complete : Theme.Text,
                        condition.Type == 4 ? Item(condition.Id, condition.ItemData) : null);
                }
                AddLine("Reward" + (detail.RandomRewards > 0 ? " (random selection)" : ""), Theme.Gold, reward: true);
                foreach (var reward in detail.Rewards)
                    AddLine(reward.Type switch
                    {
                        1 => $"EXP: {reward.Count:N0}", 2 => $"Zen: {reward.Count:N0}",
                        4 => $"{ItemName(reward.Id)} x{reward.Count}", 16 => $"Gens points: {reward.Count:N0}",
                        _ => $"Reward {reward.Type}: {reward.Count:N0}"
                    }, Theme.Reward, reward.Type == 4 ? Item(reward.Id, reward.ItemData) : null, reward: true);
            }
            else AddLine(entry.Active ? "Loading progress..." : "Available from this NPC. Select this quest, then accept to receive its objectives and rewards.", Theme.Text);
        }
        _rewardScroll = Math.Clamp(_rewardScroll, 0, Math.Max(0, _rewards.Count - 4));
        _detailScroll = Math.Clamp(_detailScroll, 0, Math.Max(0, _lines.Count - 7));
    }

    private string Title(QuestJournal.Entry entry) => entry.Group == 0
        ? LegacyQuestHandler.BuildQuestData((byte)entry.Number, State.GetLegacyQuestState((byte)entry.Number), State, null).Title
        : QuestCatalog.Name(entry);
    private string ActionLabel => Selected is not { } e ? "Refresh" : e.Group == 0 ? "NPC Info" : !e.Active
        ? Journal.OfferedStep == (e.Group, e.Number) ? "Accept Quest" : "View Quest"
        : _npcSession && (QuestCatalog.Find(e)?.Npc == Journal.Npc || Journal.Available.Any(a => a.Group == e.Group)) ? "Complete" : "Refresh";

    private bool ActionEnabled => !_busy && (ActionLabel != "Complete" ||
        (Selected != null && Journal.Details.TryGetValue(Selected.Group, out var detail) &&
         detail.Conditions.All(c => c.Current >= c.Required)));

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!Visible || Journal == null) return;
        if (_revision != Journal.Revision) Rebuild();
        double now = gameTime.TotalGameTime.TotalSeconds;
        if (_busy && now - _pendingSince > 8) { _busy = false; _message = "No confirmation received. Refresh or try again."; }
        if (now >= _nextRefresh)
        {
            _nextRefresh = now + 3;
            if (Selected is { Group: not 0, Active: true } entry && !_busy) _ = Send(0x1B, entry);
            if (_category == 1) BuildLines();
        }
        if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
        { Scene?.ConsumeKeyboardEscape(); CloseWindow(); return; }
        var mouse = MuGame.Instance.UiMouseState;
        if (!PersonalStoreLayout.IsTopmostAtPointer(this, mouse.Position)) return;
        Point local = mouse.Position - DisplayPosition;
        int wheel = mouse.ScrollWheelValue - MuGame.Instance.PrevUiMouseState.ScrollWheelValue;
        if (wheel != 0 && DetailRect.Contains(local)) _detailScroll = Math.Clamp(_detailScroll - Math.Sign(wheel) * 3, 0, Math.Max(0, _lines.Count - 7));
        if (wheel != 0 && RewardRect.Contains(local)) _rewardScroll = Math.Clamp(_rewardScroll - Math.Sign(wheel), 0, Math.Max(0, _rewards.Count - 4));
        if (wheel != 0 && ListRect.Contains(local)) _listScroll = Math.Clamp(_listScroll - Math.Sign(wheel), 0, Math.Max(0, _rows.Count - 7));
        if (mouse.LeftButton != ButtonState.Pressed || MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Pressed) return;
        Scene?.SetMouseInputConsumed();
        if (CloseRect.Contains(local)) { CloseWindow(); return; }
        for (int i = 0; i < Categories.Length; i++)
            if (new Rectangle(25, 115 + i * 28, 180, 28).Contains(local))
            { _category = i; _selected = _listScroll = _detailScroll = 0; Rebuild(); return; }
        if (ListRect.Contains(local))
        {
            int index = _listScroll + (local.Y - ListRect.Y) / 24;
            if (index < _rows.Count)
            {
                _selected = index; _detailScroll = 0; BuildLines();
                if (Selected.Group != 0) _ = Send(Selected.Active ? (byte)0x1B : (byte)0x0A, Selected);
            }
            return;
        }
        if (_busy) return;
        if (CancelRect.Contains(local) && Selected is { Active: true, Group: not 0 } cancel)
            RequestDialog.Show("Give up this quest?", () => { _busy = true; _pendingSince = now; _ = Send(0x0F, cancel); }, acceptText: "Give Up", rejectText: "Cancel");
        if (ActionRect.Contains(local) && ActionEnabled)
        {
            if (Selected == null) { _ = Send(0x1A); return; }
            if (Selected.Group == 0)
            {
                string npc = LegacyQuestHandler.BuildQuestData((byte)Selected.Number, State.GetLegacyQuestState((byte)Selected.Number), State, null).NpcName;
                string location = Selected.Number <= 1
                    ? "Priestess Sevina (Sabina)\n\nDevias - Coordinates: 183, 31\n\nTalk to her to accept or complete this quest."
                    : "Talk to " + npc + " to accept or complete this quest.";
                RequestDialog.ShowInfo(location);
                return;
            }
            byte sub = ActionLabel switch { "Accept Quest" => 0x0B, "View Quest" => 0x0A, "Complete" => 0x0D, _ => 0x1B };
            _busy = true; _pendingSince = now;
            _ = Send(sub, Selected);
        }
    }

    private void EnsureSurface()
    {
        if (_surface != null) return;
        var device = GraphicsManager.Instance.GraphicsDevice;
        _surface = new RenderTarget2D(device, Width, Height);
        var targets = device.GetRenderTargets();
        device.SetRenderTarget(_surface); device.Clear(Color.Transparent);
        var sb = GraphicsManager.Instance.Sprite;
        using (new SpriteBatchScope(sb, blend: _surfaceBlend))
        {
            // Keep the ornament silhouette transparent; fill only the interior.
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(16, 78, Width - 32, Height - 94), Theme.Background);
            NewUiFrameRenderer.DrawPersonalShopWindow(sb, _frame, new Rectangle(0, 0, Width, Height));
            NewUiFrameRenderer.DrawPersonalShopInset(sb, _frame, new Rectangle(18, 80, 187, 522));
            NewUiFrameRenderer.DrawPersonalShopInset(sb, _frame, new Rectangle(209, 80, 307, 194));
            NewUiFrameRenderer.DrawPersonalShopInset(sb, _frame, new Rectangle(209, 278, 307, 164));
            NewUiFrameRenderer.DrawPersonalShopInset(sb, _frame, new Rectangle(209, 443, 307, 106));
        }
        device.SetRenderTargets(targets);
    }

    private Rectangle Screen(Rectangle rect) => new(rect.X + DisplayPosition.X, rect.Y + DisplayPosition.Y, rect.Width, rect.Height);
    private void Text(SpriteBatch sb, string text, int x, int y, Color color, int maxWidth = 280, float scale = 0.39f)
    {
        while (text.Length > 3 && _font.MeasureString(text).X * scale > maxWidth) text = text[..^4] + "...";
        sb.DrawString(_font, text, new Vector2(DisplayPosition.X + x + 1, DisplayPosition.Y + y + 1), Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(_font, text, new Vector2(DisplayPosition.X + x, DisplayPosition.Y + y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || _font == null) return;
        EnsureSurface();
        var sb = GraphicsManager.Instance.Sprite;
        using var scope = new SpriteBatchScope(sb, blend: BlendState.AlphaBlend, transform: UiScaler.SpriteTransform);
        sb.Draw(_surface, DisplayRectangle, Color.White);
        var titleSize = _font.MeasureString("My Quests") * 0.48f;
        Text(sb, "My Quests", (Width - (int)titleSize.X) / 2, 52 - (int)titleSize.Y / 2, Theme.Gold, 400, 0.48f);
        // The same atlas provides the NewUI close states.
        using (new SpriteBatchScope(sb, blend: BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
            sb.Draw(_atlas, Screen(CloseRect), new Rectangle(40, 532, 20, 20), Color.White);
        Text(sb, "Quest Group", 38, 94, Theme.Text, 165);
        for (int i = 0; i < Categories.Length; i++) Text(sb, Categories[i], 28, 120 + i * 28, _category == i ? Theme.Gold : Theme.Text, 174);
        for (int i = 0; i < 7 && _listScroll + i < _rows.Count; i++)
        {
            int index = _listScroll + i;
            if (index == _selected) sb.Draw(GraphicsManager.Instance.Pixel, Screen(new Rectangle(220, 86 + i * 24, 278, 23)), new Color(68, 56, 24, 190));
            Text(sb, _titles[index], 224, 89 + i * 24, _rows[index].Active ? Theme.Text : Theme.Gold, 271);
        }
        for (int i = 0; i < 7 && _detailScroll + i < _lines.Count; i++)
        {
            var line = _lines[_detailScroll + i];
            Text(sb, line.Text, 224, 291 + i * 20, line.Color, 273);
        }
        for (int i = 0; i < 4 && _rewardScroll + i < _rewards.Count; i++)
        {
            var line = _rewards[_rewardScroll + i];
            Text(sb, line.Text, 224, 451 + i * 20, line.Color, 273);
        }
        if (_rewards.Count == 0) Text(sb, "Rewards are confirmed by the server.", 224, 457, Theme.Reward, 270, 0.34f);
        if (_lines.Count > 7) Text(sb, "Mouse wheel: scroll", 325, 429, Theme.Gold, 175, 0.32f);
        DrawButton(sb, ActionRect, _busy ? "Waiting..." : ActionLabel, ActionEnabled);
        DrawButton(sb, CancelRect, "Give Up Quest", Selected is { Active: true, Group: not 0 });
        Text(sb, _message, 23, 597, Theme.Text, 480, 0.31f);
    }
    private void DrawButton(SpriteBatch sb, Rectangle rect, string text, bool enabled)
    {
        var screen = Screen(rect);
        using (new SpriteBatchScope(sb, blend: BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform))
            sb.Draw(enabled && screen.Contains(MuGame.Instance.UiMouseState.Position) ? _hover : _button, screen, enabled ? Color.White : Color.Gray);
        Text(sb, text, rect.X + 10, rect.Y + 10, enabled ? Theme.Gold : Color.Gray, rect.Width - 20);
    }
    public void DrawTooltipOverlay(SpriteBatch sb)
    {
        if (!Visible || _font == null || !PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position)) return;
        var local = MuGame.Instance.UiMouseState.Position - DisplayPosition;
        bool rewards = RewardRect.Contains(local);
        var lines = rewards ? _rewards : _lines;
        int top = rewards ? 451 : 291;
        int index = (rewards ? _rewardScroll : _detailScroll) + (local.Y - top) / 20;
        var rect = rewards ? RewardRect : DetailRect;
        if (local.Y < top || !rect.Contains(local) || index < 0 || index >= lines.Count || lines[index].Item == null) return;
        var item = lines[index].Item;
        ClassicItemTooltipRenderer.Draw(sb, _font, item, ClassicItemTooltipBuilder.Build(item), Screen(rect), MuGame.Instance.UiMouseState.Position);
    }
    public override void Dispose()
    {
        _surface?.Dispose(); _surface = null;
        Visible = false; _npcSession = false; _revision = -1;
        base.Dispose();
    }
}
