using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Controls.UI.Game
{
    public sealed class EventsInfoControl : UIControl, IUiTexturePreloadable
    {
        private const int Width = 924;
        private const int Height = 712;
        private const string FramePath = "Interface/GFx/EventMapEntranceUI_I1.ozd";
        private sealed record NpcEntry(string Name, string Location, string TicketNote);
        private sealed record EventPage(MiniGameType? Type, string Name, string ArtPath,
            string Description, string Ticket, NpcEntry Npc = null);

        private static readonly EventPage[] Pages =
        {
            new(MiniGameType.ChaosCastle, "Chaos Castle", "Interface/GFx/EventMapEntranceUI_I98.ozd",
                "Defeat monsters and other players.\nSurvive as the arena shrinks.", "Armor of Guardsman"),
            new(MiniGameType.BloodCastle, "Blood Castle", "Interface/GFx/EventMapEntranceUI_I95.ozd",
                "Break through the castle defenses and return the\nArchangel's weapon before time runs out.", "Invisibility Cloak"),
            new(MiniGameType.DevilSquare, "Devil Square", "Interface/GFx/EventMapEntranceUI_I92.ozd",
                "Fight waves of monsters and earn points\nbefore time runs out.", "Devil's Invitation"),
            new(null, "Doppelganger", "Interface/GFx/EventMapEntranceUI_I8F.ozd",
                "Stop the monsters before they\nreach the magic circle.", "Mirror of Dimensions",
                new("Lugard", "Elbeland (49, 216)", "Free Ticket also accepted.")),
            new(null, "Imperial Guardian", "Interface/GFx/EventMapEntranceUI_I89.ozd",
                "Clear the fortress and defeat its guardians\nbefore time runs out.", "Gaion's Order",
                new("Jerint", "Devias (229, 221)", "Sunday: Complete Secromicon.")),
            new(null, "Illusion of Kundun", "Interface/GFx/EventMapEntranceUI_I80.ozd",
                "Open a portal to Kalima 7 and challenge\nIllusion of Kundun.", "Lost Map +7"),
            new(null, "Selupan", "Interface/GFx/EventMapEntranceUI_I77.ozd",
                "Destroy the Spider Eggs in Raklion Hatchery\nto awaken Selupan.", "None"),
            new(null, "Nightmare", "Interface/GFx/EventMapEntranceUI_I7A.ozd",
                "Defeat Maya in Kanturu to face Nightmare\nand open the Tower of Refinement.", "Moonstone Pendant"),
            new(null, "Crywolf", "Interface/GFx/EventMapEntranceUI_I5F.ozd",
                "Defend the Wolf Statue and its altars\nfrom Balgass and his army.", "None"),
        };
        private static readonly int FirstBossIndex = Pages.Length - 4;
        private static readonly int SelupanIndex = FirstBossIndex + 1;
        private static readonly int NightmareIndex = Pages.Length - 2;
        private static readonly int CrywolfIndex = Pages.Length - 1;
        private const ushort CrywolfWarpIndex = 26;
        private static readonly Rectangle CompetitionTab = new(241, 78, 103, 27);
        private const ushort RaklionWarpIndex = 34;
        private const ushort KanturuRelicsWarpIndex = 30;
        private static readonly Rectangle EventMapTab = new(27, 78, 103, 27);
        private static readonly Rectangle BossBattleTab = new(134, 78, 103, 27);
        private static readonly Rectangle FrameSource = new(0, 0, Width, Height);
        private static readonly Rectangle ArtSource = new(0, 0, 700, 572);
        private static readonly Rectangle CloseButton = new(879, 28, 26, 24);
        private static readonly Rectangle TimeButton = new(725, 192, 154, 32);
        private static readonly Rectangle EntryButton = new(766, 615, 113, 41);
        private static readonly Color Gold = new(219, 182, 82);
        private static readonly Color TextColor = new(222, 214, 201);

        public static EventsInfoControl Instance { get; } = new();
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private Texture2D _frame;
        private readonly Texture2D[] _art = new Texture2D[Pages.Length];
        private readonly Dictionary<MiniGameType, DateTime> _openingRequests = new();
        private int _selectedIndex = 1;
        private int _eventMapIndex = 1;
        private int _bossIndex = SelupanIndex;
        private DateTime? _encounterReplyDeadline;
        private string _bossEntranceStatus = string.Empty;
        private string _crywolfSchedule = string.Empty;
        private string _crywolfTiming = string.Empty;
        private byte _gateLevel;
        private string _openingStatus = string.Empty;

        private EventPage SelectedPage => Pages[_selectedIndex];
        private bool IsBossBattle => _selectedIndex >= FirstBossIndex && _selectedIndex < CrywolfIndex;
        private bool IsCompetition => _selectedIndex == CrywolfIndex;
        private bool IsEncounter => IsBossBattle || IsCompetition;
        private int FirstVisiblePage => IsCompetition ? CrywolfIndex : IsBossBattle ? FirstBossIndex : 0;
        private int EndVisiblePage => IsCompetition ? Pages.Length : IsBossBattle ? CrywolfIndex : FirstBossIndex;
        private bool IsKundun => _selectedIndex == FirstBossIndex;
        private bool IsNightmare => _selectedIndex == NightmareIndex;
        private bool IsWaitingForOpening => SelectedPage.Type is { } type && _openingRequests.ContainsKey(type);

        private EventsInfoControl()
        {
            ControlSize = ViewSize = new Point(Width, Height);
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return FramePath;
            foreach (var page in Pages) yield return page.ArtPath;
        }

        public override async Task Load()
        {
            await base.Load();
            _frame = await TextureLoader.Instance.PrepareAndGetTexture(FramePath);
            for (int i = 0; i < Pages.Length; i++)
                _art[i] = await TextureLoader.Instance.PrepareAndGetTexture(Pages[i].ArtPath);
        }

        public void Toggle()
        {
            if (Visible)
            {
                CloseWindow();
                return;
            }

            Visible = true;
            PositionWindow();
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            SelectPage(_selectedIndex);
        }

        public void CloseWindow()
        {
            Visible = false;
            _openingRequests.Clear();
            _encounterReplyDeadline = null;
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
        }

        internal bool TrySetOpening(MiniGameType type, string message)
        {
            if (!Visible || !_openingRequests.Remove(type)) return false;
            if (SelectedPage.Type == type) _openingStatus = message;
            return true;
        }

        private void SelectPage(int index)
        {
            if (_selectedIndex != index) _encounterReplyDeadline = null;
            _selectedIndex = index;
            if (IsEncounter)
            {
                if (IsBossBattle) _bossIndex = index;
                RefreshEncounterStatus();
                return;
            }

            _eventMapIndex = index;
            _gateLevel = SelectedPage.Type switch
            {
                MiniGameType.BloodCastle => BloodCastleEnterControl.Instance.GetEventGateLevel(),
                MiniGameType.DevilSquare => DevilSquareEnterControl.Instance.GetEventGateLevel(),
                _ => 1, // OpenMU selects the suitable Chaos Castle tier from the character.
            };
            if (SelectedPage.Npc is { } npc)
                _openingStatus = $"Entry on demand through {npc.Name}.";
            else
                RefreshOpening();
        }

        private void RefreshOpening()
        {
            if (SelectedPage.Type is not { } type || IsWaitingForOpening) return;
            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                _openingStatus = "Opening time unavailable.";
                return;
            }

            _openingRequests[type] = DateTime.UtcNow.AddSeconds(6);
            _openingStatus = "Checking opening time...";
            _ = service.RequestMiniGameOpeningAsync(type, _gateLevel);
        }

        private void RefreshEncounterStatus()
        {
            if (_encounterReplyDeadline.HasValue) return;
            _bossEntranceStatus = string.Empty;
            _crywolfSchedule = string.Empty;
            _crywolfTiming = string.Empty;
            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                _openingStatus = $"{SelectedPage.Name} status unavailable.";
                return;
            }

            _openingStatus = $"Checking {SelectedPage.Name} status...";
            _encounterReplyDeadline = DateTime.UtcNow.AddSeconds(6);
            _ = IsCompetition ? service.RequestCrywolfMenuStateAsync() :
                IsKundun ? service.RequestKundunMenuStateAsync() :
                IsNightmare ? service.RequestKanturuMenuStateAsync() : service.RequestRaklionStateAsync();
        }

        internal void SetRaklionStatus(string phase, bool canEnter) =>
            SetBossStatus(SelupanIndex, phase, canEnter ? "Hatchery gate open." : "Hatchery gate closed.");

        internal void SetKundunStatus(string phase) =>
            SetBossStatus(FirstBossIndex, phase, "Portal opened with Lost Map +7.");

        internal void SetKanturuStatus(string phase, bool canEnter) =>
            SetBossStatus(NightmareIndex, phase, canEnter ? "Gateway open." : "Gateway closed.");

        internal void SetCrywolfStatus(string phase, string details, string schedule, string timing)
        {
            if (!Visible || !IsCompetition || !_encounterReplyDeadline.HasValue) return;
            SetBossStatus(CrywolfIndex, phase, details);
            _crywolfSchedule = schedule;
            _crywolfTiming = timing;
        }

        private void SetBossStatus(int index, string phase, string entranceStatus)
        {
            if (!Visible || _selectedIndex != index || !_encounterReplyDeadline.HasValue) return;
            _encounterReplyDeadline = null;
            _openingStatus = phase;
            _bossEntranceStatus = entranceStatus;
        }

        private void PositionWindow()
        {
            var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
            Scale = MathHelper.Clamp(Math.Min((size.X - 16f) / Width, (size.Y - 16f) / Height), 0.1f, 1f);
            X = (size.X - DisplaySize.X) / 2;
            Y = (size.Y - DisplaySize.Y) / 2;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            PositionWindow();
            base.Update(gameTime);
            if (_encounterReplyDeadline is { } bossDeadline && DateTime.UtcNow >= bossDeadline)
            {
                _encounterReplyDeadline = null;
                if (IsEncounter) _openingStatus = $"{SelectedPage.Name} status unavailable.";
            }
            foreach (var page in Pages)
            {
                if (page.Type is not { } type || !_openingRequests.TryGetValue(type, out var deadline) || DateTime.UtcNow < deadline)
                    continue;

                _openingRequests.Remove(type);
                if (SelectedPage.Type == type) _openingStatus = "No opening time reported.";
            }

            if (Scene?.IsPointerInputBlockedFor(this) == true) return;
            var game = MuGame.Instance;
            if (HasFocus && game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                CloseWindow();
                return;
            }

            var mouse = game.UiMouseState;
            if (mouse.LeftButton != ButtonState.Pressed || game.PrevUiMouseState.LeftButton != ButtonState.Released ||
                !DisplayRectangle.Contains(mouse.Position))
                return;

            Scene?.SetMouseInputConsumed();
            var point = ToLocal(mouse.Position);
            if (CloseButton.Contains(point))
                CloseWindow();
            else if (EventMapTab.Contains(point) && IsEncounter)
                SelectPage(_eventMapIndex);
            else if (BossBattleTab.Contains(point) && !IsBossBattle)
                SelectPage(_bossIndex);
            else if (CompetitionTab.Contains(point) && !IsCompetition)
                SelectPage(CrywolfIndex);
            else if (TimeButton.Contains(point) && SelectedPage.Npc == null)
            {
                if (IsEncounter) RefreshEncounterStatus();
                else RefreshOpening();
            }
            else if (EntryButton.Contains(point))
            {
                EnterSelectedEvent();
            }
            else
            {
                int firstPage = FirstVisiblePage;
                int lastPage = EndVisiblePage;
                for (int i = firstPage; i < lastPage; i++)
                {
                    if (!EventRow(i - firstPage).Contains(point) || i == _selectedIndex) continue;
                    SelectPage(i);
                    break;
                }
            }
        }

        private void EnterSelectedEvent()
        {
            CloseWindow();
            if (IsKundun)
            {
                RequestDialog.ShowInfo("Drop a Lost Map +7 outside a safe zone to open a portal to Kalima 7.\nThen use the portal to enter.");
                return;
            }

            if (IsEncounter)
            {
                var characterService = MuGame.Network?.GetCharacterService();
                if (characterService != null)
                {
                    ushort warpIndex = IsCompetition ? CrywolfWarpIndex : IsNightmare ? KanturuRelicsWarpIndex : RaklionWarpIndex;
                    _ = characterService.SendWarpCommandRequestAsync(warpIndex);
                }
                return;
            }

            if (SelectedPage.Npc is { } npc)
            {
                RequestDialog.ShowInfo($"Talk to {npc.Name} in {npc.Location}.\nRequired item: {SelectedPage.Ticket}.\n{npc.TicketNote}");
                return;
            }

            switch (SelectedPage.Type)
            {
                case MiniGameType.BloodCastle:
                    BloodCastleEnterControl.Instance.ShowWindow();
                    break;
                case MiniGameType.DevilSquare:
                    DevilSquareEnterControl.Instance.ShowWindow();
                    break;
                case MiniGameType.ChaosCastle:
                    var service = MuGame.Network?.GetCharacterService();
                    if (service != null) _ = service.SendChaosCastleEnterRequestAsync();
                    break;

            }
        }

        private static Rectangle EventRow(int index) => new(31, 119 + index * 58, 155, 48);

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var graphics = GraphicsManager.Instance;
            var batch = graphics.Sprite;
            var pixel = graphics.Pixel;
            var font = graphics.Font;
            if (pixel == null || font == null) return;

            var transform = Matrix.CreateScale(Scale, Scale, 1f) *
                Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0f) * UiScaler.SpriteTransform;
            var mouse = ToLocal(MuGame.Instance.UiMouseState.Position);
            using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, GraphicsManager.GetQualityLinearSamplerState(), transform: transform);

            batch.Draw(pixel, new Rectangle(0, 0, Width, Height), new Color(15, 11, 9));
            if (_frame != null) batch.Draw(_frame, FrameSource, FrameSource, Color.White);
            var selectedArt = _art[_selectedIndex];
            if (selectedArt != null)
                batch.Draw(selectedArt, new Rectangle(205, 111, 699, 576), ArtSource, Color.White);

            DrawPanel(batch, pixel, new Rectangle(18, 76, 888, 31), new Color(20, 16, 13));
            DrawButton(batch, pixel, font, EventMapTab, "Event Map", !IsEncounter, false);
            DrawButton(batch, pixel, font, BossBattleTab, "Boss Battle", IsBossBattle, false);
            DrawButton(batch, pixel, font, CompetitionTab, "Competition", IsCompetition, false);
            DrawText(batch, font, "Events Info", new Rectangle(50, 29, 824, 23), Gold, 17f, true);
            DrawText(batch, font, "X", CloseButton, CloseButton.Contains(mouse) ? Gold : new Color(211, 117, 69), 15f, true);

            int firstPage = FirstVisiblePage;
            int lastPage = EndVisiblePage;
            for (int i = firstPage; i < lastPage; i++)
            {
                var row = EventRow(i - firstPage);
                bool highlighted = i == _selectedIndex || row.Contains(mouse);
                DrawPanel(batch, pixel, row, highlighted ? new Color(160, 116, 33) : new Color(74, 53, 29));
                var inner = row;
                inner.Inflate(-2, -2);
                if (_art[i] != null)
                    batch.Draw(_art[i], inner, new Rectangle(100, 190, 500, 146), Color.White);
                batch.Draw(pixel, inner, new Color(12, 9, 7, 130));
                DrawText(batch, font, Pages[i].Name, row, highlighted ? Gold : TextColor, 15f, true);
            }

            DrawText(batch, font, SelectedPage.Name, new Rectangle(222, 123, 470, 41), Gold, 34f);
            DrawText(batch, font, SelectedPage.Description, new Rectangle(222, 170, 470, 45), TextColor, 16f);
            if (SelectedPage.Npc == null)
                DrawButton(batch, pixel, font, TimeButton, IsEncounter ? "Refresh Status" : "Event Entry Time",
                    TimeButton.Contains(mouse), IsEncounter ? _encounterReplyDeadline.HasValue : IsWaitingForOpening);

            var info = new Rectangle(599, 320, 280, 139);
            DrawPanel(batch, pixel, info, new Color(12, 10, 9, 207));
            DrawText(batch, font, IsCompetition ? "Fortress Information" : IsBossBattle ? "Boss Battle Information" : "Entry Information",
                new Rectangle(615, 332, 248, 24), Gold, 17f);
            bool isChaosCastle = SelectedPage.Type == MiniGameType.ChaosCastle;
            var npc = SelectedPage.Npc;
            string gateInfo = npc != null ? $"{npc.Name}, {npc.Location}" :
                isChaosCastle ? "Gate assigned by the server." : $"Suggested gate: {SelectedPage.Name} {_gateLevel}";
            string entryInfo = npc?.TicketNote ?? (isChaosCastle ? "Entry costs Zen." : "See the entry window for level ranges.");
            if (IsBossBattle)
            {
                gateInfo = IsKundun ? "Kalima 7" :
                    IsNightmare ? "Gateway Machine (141, 191)" : "Raklion Hatchery (171, 23-25)";
                entryInfo = _bossEntranceStatus;
            }
            if (IsCompetition)
            {
                gateInfo = "Crywolf Fortress";
                entryInfo = _bossEntranceStatus;

                DrawPanel(batch, pixel, new Rectangle(222, 462, 657, 100), new Color(12, 10, 9, 215));
                DrawText(batch, font, "Weekly Schedule (Server Time)", new Rectangle(238, 468, 625, 21), Gold, 15f);
                DrawText(batch, font, _crywolfSchedule, new Rectangle(238, 491, 625, 38), TextColor, 12f);
                DrawText(batch, font, _crywolfTiming, new Rectangle(238, 535, 625, 20), Gold, 13f);
            }
            DrawText(batch, font, gateInfo, new Rectangle(615, 370, 248, 21), TextColor, 13f);
            DrawText(batch, font, entryInfo, new Rectangle(615, 399, 248, 21), TextColor, 13f);

            DrawPanel(batch, pixel, new Rectangle(222, 577, 211, 86), new Color(12, 10, 9, 215));
            DrawText(batch, font, "Required Item", new Rectangle(238, 585, 179, 24), Gold, 17f, true);
            DrawText(batch, font, SelectedPage.Ticket, new Rectangle(238, 617, 179, 24), TextColor, 15f, true);
            DrawText(batch, font, _openingStatus, new Rectangle(449, 577, 430, 29), Gold, 15f, true);
            string action = IsKundun ? "How to Enter" : IsEncounter ? "Move" : npc != null ? $"Find {npc.Name}" : "Enter";
            DrawButton(batch, pixel, font, EntryButton, action, EntryButton.Contains(mouse), false);
        }

        private Point ToLocal(Point p) => new((int)((p.X - DisplayPosition.X) / Scale), (int)((p.Y - DisplayPosition.Y) / Scale));

        private static void DrawPanel(SpriteBatch batch, Texture2D pixel, Rectangle rect, Color fill)
        {
            batch.Draw(pixel, rect, new Color(111, 82, 46));
            rect.Inflate(-1, -1);
            batch.Draw(pixel, rect, fill);
        }

        private static void DrawButton(SpriteBatch batch, Texture2D pixel, SpriteFont font,
            Rectangle rect, string label, bool highlighted, bool disabled)
        {
            var fill = highlighted ? new Color(108, 44, 16) : new Color(45, 30, 23);
            DrawPanel(batch, pixel, rect, fill);
            DrawText(batch, font, label, rect, disabled ? new Color(140, 129, 116) : TextColor, 15f, true);
        }

        private static void DrawText(SpriteBatch batch, SpriteFont font, string text, Rectangle rect,
            Color color, float size, bool centered = false)
        {
            float scale = size / Constants.BASE_FONT_SIZE;
            var measured = font.MeasureString(text) * scale;
            var position = new Vector2(centered ? rect.Center.X - measured.X / 2 : rect.X,
                rect.Center.Y - measured.Y / 2);
            batch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
