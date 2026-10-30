using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Client;
using Client.Main.Core.Items;
using Client.Main.Core.Items.Tooltips;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>Information about S6 equipped pets, using the shared modern GFx skin.</summary>
    public sealed class PetInfoControl : UIControl, IUiTexturePreloadable
    {
        private const int Width = 348;
        private const int Height = 620;
        private const string FramePath = "Interface/GFx/CharacterInfoFrame_I1.ozd";
        private const string CardPath = "Interface/GFx/pet_bg.ozd";
        private const string LifePath = "Interface/GFx/pethp_guege.ozd";
        private static readonly Rectangle FrameSource = new(656, 0, 354, 638);
        private static readonly Rectangle PreviewRect = new(36, 169, 80, 80);
        private static readonly Rectangle CloseRect = new(307, 36, 25, 25);
        private static readonly Rectangle FooterRect = new(108, 570, 132, 34);
        private static readonly Rectangle OptionsRect = new(30, 344, 288, 166);
        private static readonly Color Gold = new(230, 197, 115);
        private static readonly string[] Commands = { "Standby", "Automatic attack", "With owner", "Attack target" };
        private static readonly byte[] Slots = { Season6PetRules.CompanionSlot, Season6PetRules.RavenSlot };
        private static readonly string[] Paths =
        {
            FramePath, CardPath, LifePath,
            "Interface/GFx/btn_m01.ozd", "Interface/GFx/btn_m02.ozd", "Interface/GFx/btn_m03.ozd",
            "Interface/GFx/btn_close01.ozd", "Interface/GFx/btn_close02.ozd", "Interface/GFx/btn_close03.ozd"
        };

        private static PetInfoControl _instance;
        public static PetInfoControl Instance => _instance ??= new PetInfoControl();
        public static bool IsOpen => _instance?.Visible == true;
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private readonly Texture2D[] _textures = new Texture2D[Paths.Length];
        private readonly InventoryItem[] _pets = new InventoryItem[2];
        private readonly byte[][] _raw = new byte[2][];
        private readonly int[] _health = { -1, -1 };
        private readonly PetInfoSnapshot?[] _info = new PetInfoSnapshot?[2];
        private readonly double[] _nextRequest = new double[2];
        private readonly List<(string text, Color color)>[] _tooltips = new List<(string text, Color color)>[2];
        private readonly List<string> _options = new();
        private CharacterState _state;
        private Texture2D _preview;
        private RenderTarget2D _surface;
        private BlendState _surfaceBlend;
        private int _selected, _hovered = -1, _pressed = -1, _scroll, _lastWheel;
        private int _lastAgility = -1, _lastLeadership = -1;
        private bool _dataLoaded;
        private byte? _pendingMode;
        private int _pendingRevision;
        private double _pendingSeconds;
        private string _commandStatus = string.Empty;

        private PetInfoControl()
        {
            ControlSize = ViewSize = new Point(Width, Height);
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths() => Paths;

        public override async Task Load()
        {
            await base.Load();
            var tasks = new Task<Texture2D>[Paths.Length];
            for (int i = 0; i < Paths.Length; i++) tasks[i] = TextureLoader.Instance.PrepareAndGetTexture(Paths[i]);
            var textures = await Task.WhenAll(tasks);
            Array.Copy(textures, _textures, textures.Length);
        }

        public void Toggle()
        {
            if (Visible) HideWindow();
            else ShowWindow();
        }

        public void ShowWindow(byte preferredSlot = Season6PetRules.CompanionSlot)
        {
            _state = MuGame.Network?.GetCharacterState();
            if (_state == null) return;
            _selected = preferredSlot == Season6PetRules.RavenSlot ? 1 : 0;
            _pendingMode = null;
            _commandStatus = string.Empty;
            _scroll = 0;
            _pressed = -1;
            _preview = null;
            _lastWheel = MuGame.Instance.UiMouseState.ScrollWheelValue;
            for (int i = 0; i < 2; i++) _nextRequest[i] = 0;
            RefreshPets(force: true);
            PositionWindow();
            Visible = true;
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            Scene?.SetMouseInputConsumed();
            SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void HideWindow()
        {
            Visible = false;
            _pendingMode = null;
            _pressed = _hovered = -1;
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
            Scene?.SetMouseInputConsumed();
        }

        private void PositionWindow()
        {
            int availableWidth = Parent?.DisplaySize.X ?? UiScaler.VirtualSize.X;
            int availableHeight = Parent?.DisplaySize.Y ?? UiScaler.VirtualSize.Y;
            int right = availableWidth - DisplaySize.X - 8;
            var inventory = InventoryControl.Instance;
            if (inventory.Visible) right = Math.Min(right, inventory.DisplayRectangle.X - DisplaySize.X - 8);
            X = Math.Max(0, right);
            Y = Math.Max(0, (availableHeight - DisplaySize.Y) / 2);
        }

        private bool IsRaven => _pets[_selected]?.Definition is { Group: 13, Id: 5 };
        private int VisibleOptionRows => IsRaven ? 4 : 8;

        private void RefreshPets(bool force = false)
        {
            if (_state == null) return;
            bool changed = force || _lastAgility != _state.TotalAgility || _lastLeadership != _state.TotalLeadership ||
                _dataLoaded != ItemTooltipDataRepository.IsLoaded;
            for (int i = 0; i < 2; i++)
            {
                _state.TryGetInventoryItem(Slots[i], out var raw);
                var definition = raw == null ? null : ItemDatabase.GetItemDefinition(raw);
                if (definition == null || !Season6PetRules.IsPet(Slots[i], definition.Group, definition.Id)) raw = null;
                PetInfoSnapshot? info = raw != null && _state.TryGetPetInfo(Slots[i], out var snapshot) &&
                    Season6PetRules.MatchesInfo(definition.Id, snapshot) ? snapshot : null;
                int health = raw == null ? -1 : info?.Health ?? ItemDatabase.GetItemDurability(raw);
                bool itemChanged = !ReferenceEquals(raw, _raw[i]);
                if (force || itemChanged || health != _health[i] || info != _info[i])
                {
                    _raw[i] = raw;
                    _health[i] = health;
                    _info[i] = info;
                    _pets[i] = raw == null ? null : new InventoryItem(definition, Point.Zero, raw, health);
                    if (itemChanged) _nextRequest[i] = 0;
                    changed = true;
                    if (i == _selected) _preview = null;
                }
            }
            if (_pets[_selected] == null && _pets[1 - _selected] != null)
            { _selected = 1 - _selected; changed = true; _preview = null; }
            if (!changed) return;
            _lastAgility = _state.TotalAgility;
            _lastLeadership = _state.TotalLeadership;
            _dataLoaded = ItemTooltipDataRepository.IsLoaded;
            for (int i = 0; i < 2; i++)
                _tooltips[i] = _pets[i] == null ? null : ClassicItemTooltipBuilder.Build(_pets[i], _info[i]);
            BuildOptions();
        }

        private string PetName(int index) => _tooltips[index]?.Count > 0 ? _tooltips[index][0].text
            : _pets[index]?.Definition.Name ?? (index == 0 ? "No companion equipped" : "No Dark Raven equipped");

        private void BuildOptions()
        {
            _options.Clear();
            var pet = _pets[_selected];
            if (pet == null) return;
            if (_info[_selected] is { } info)
            {
                if (IsRaven)
                {
                    var stats = Season6PetRules.RavenStats(info.Level, _state.TotalLeadership);
                    _options.Add($"Base damage: {stats.MinimumDamage} - {stats.MaximumDamage}");
                    _options.Add($"Base attack speed: {stats.AttackSpeed}");
                    _options.Add($"Base attack success rate: {stats.AttackRate}");
                }
                else
                {
                    _options.Add($"Defense bonus: +{Season6PetRules.HorseDefense(info.Level, _state.TotalAgility)}");
                    _options.Add($"Damage absorption: {Season6PetRules.HorseAbsorption(info.Level).ToString("0.#", CultureInfo.InvariantCulture)}%");
                }
            }
            else if (!Season6PetRules.IsTrainable(pet.Definition.Group, pet.Definition.Id))
            {
                var lines = _tooltips[_selected];
                if (lines != null)
                    for (int i = 1; i < lines.Count; i++)
                    {
                        string line = lines[i].text;
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Life", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("Durability", StringComparison.OrdinalIgnoreCase)) continue;
                        WrapOption(line);
                    }
            }
            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _options.Count - VisibleOptionRows));
        }

        private void WrapOption(string text)
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null) { _options.Add(text); return; }
            string line = string.Empty;
            foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string next = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(next).X * 0.36f > OptionsRect.Width - 8)
                { _options.Add(line); line = word; }
                else line = next;
            }
            if (line.Length > 0) _options.Add(line);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            PositionWindow();
            RefreshPets();
            double now = gameTime.TotalGameTime.TotalSeconds;
            for (int i = 0; i < 2; i++)
            {
                var pet = _pets[i];
                if (pet != null && Season6PetRules.IsTrainable(pet.Definition.Group, pet.Definition.Id) && now >= _nextRequest[i])
                {
                    _nextRequest[i] = now + 5;
                    var service = MuGame.Network?.GetCharacterService();
                    if (service != null) _ = service.SendPetInfoRequestAsync(pet.Definition.Id == 5 ? (byte)0 : (byte)1, Slots[i]);
                }
            }
            var selectedPet = _pets[_selected];
            if (selectedPet != null && (_preview == null || _preview.IsDisposed))
                _preview = BmdPreviewRenderer.GetPreview(selectedPet, 80, 80);
            if (_pendingMode is byte pending)
            {
                _pendingSeconds += gameTime.ElapsedGameTime.TotalSeconds;
                if (_state.RavenCommandRevision != _pendingRevision && _state.RavenCommandMode == pending)
                { _pendingMode = null; _commandStatus = string.Empty; }
                else if (_pendingSeconds >= 8 || !_state.HasEquippedDarkRaven || _health[1] <= 0)
                { _pendingMode = null; _commandStatus = "Command not confirmed. Please try again."; }
            }
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            { Scene?.ConsumeKeyboardEscape(); HideWindow(); return; }
            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool inside = DisplayRectangle.Contains(mouse.Position);
            _hovered = inside ? HitTest(mouse.Position) : -1;
            if (inside) Scene?.SetMouseInputConsumed();
            if (Screen(OptionsRect).Contains(mouse.Position) && mouse.ScrollWheelValue != _lastWheel)
                ScrollOptions(mouse.ScrollWheelValue < _lastWheel ? 1 : -1);
            _lastWheel = mouse.ScrollWheelValue;
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released) _pressed = _hovered;
            if (mouse.LeftButton == ButtonState.Released && previous.LeftButton == ButtonState.Pressed)
            {
                int pressed = _pressed;
                _pressed = -1;
                if (pressed >= 0 && pressed == _hovered) Activate(pressed);
            }
        }

        private bool CanCommand => IsRaven && _state?.HasEquippedDarkRaven == true && _health[1] > 0 && _pendingMode == null;
        private static Rectangle Selector(int index) => new(24 + index * 154, 96, 146, 54);
        private static Rectangle CommandRect(int mode) => new(26 + mode % 2 * 150, 449 + mode / 2 * 40, 146, 34);

        private int HitTest(Point mouse)
        {
            if (Screen(CloseRect).Contains(mouse) || Screen(FooterRect).Contains(mouse)) return 90;
            for (int i = 0; i < 2; i++) if (_pets[i] != null && Screen(Selector(i)).Contains(mouse)) return i;
            if (CanCommand)
                for (int i = 0; i < 4; i++) if (Screen(CommandRect(i)).Contains(mouse)) return 10 + i;
            if (_options.Count > VisibleOptionRows)
            {
                if (Screen(new Rectangle(264, 516, 25, 24)).Contains(mouse)) return 80;
                if (Screen(new Rectangle(291, 516, 25, 24)).Contains(mouse)) return 81;
            }
            return -1;
        }

        private void ScrollOptions(int direction) => _scroll = Math.Clamp(_scroll + direction, 0, Math.Max(0, _options.Count - VisibleOptionRows));

        private void Activate(int action)
        {
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
            if (action == 90) { HideWindow(); return; }
            if (action is 0 or 1)
            { _selected = action; _scroll = 0; _preview = null; _commandStatus = string.Empty; BuildOptions(); return; }
            if (action == 80) { ScrollOptions(-1); return; }
            if (action == 81) { ScrollOptions(1); return; }
            if (action < 10 || action > 13 || !CanCommand) return;
            byte mode = (byte)(action - 10);
            if (mode == 3)
            {
                if (Scene is GameScene game && game.SelectPetTargetCommand()) HideWindow();
                return;
            }
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            _pendingMode = mode;
            _pendingRevision = _state.RavenCommandRevision;
            _pendingSeconds = 0;
            _commandStatus = "Sending command...";
            _ = service.SendDarkRavenCommandAsync(mode);
        }

        private Rectangle Screen(Rectangle local)
        {
            var origin = DisplayRectangle.Location;
            int left = origin.X + (int)MathF.Round(local.Left * Scale);
            int top = origin.Y + (int)MathF.Round(local.Top * Scale);
            int right = origin.X + (int)MathF.Round(local.Right * Scale);
            int bottom = origin.Y + (int)MathF.Round(local.Bottom * Scale);
            return new(left, top, right - left, bottom - top);
        }

        private void EnsureSurface(SpriteBatch sb)
        {
            var gd = MuGame.Instance.GraphicsDevice;
            if (_surface != null && !_surface.IsDisposed) return;
            _surfaceBlend ??= new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
            };
            _surface = new RenderTarget2D(gd, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
            var targets = gd.GetRenderTargets();
            var viewport = gd.Viewport;
            try
            {
                using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, _surfaceBlend, SamplerState.PointClamp))
                {
                    gd.SetRenderTarget(_surface);
                    gd.Clear(Color.Transparent);
                    var frame = _textures[0];
                    if (frame != null)
                    {
                        sb.Draw(frame, new Rectangle(0, 0, Width, 96), new Rectangle(FrameSource.X, 0, FrameSource.Width, 96), Color.White);
                        sb.Draw(frame, new Rectangle(0, 96, Width, Height - 135), new Rectangle(FrameSource.X, 96, FrameSource.Width, 503), Color.White);
                        sb.Draw(frame, new Rectangle(0, Height - 39, Width, 39), new Rectangle(FrameSource.X, 599, FrameSource.Width, 39), Color.White);
                    }
                    if (_textures[1] != null)
                        NewUiFrameRenderer.DrawNineSlice(sb, _textures[1], new Rectangle(0, 0, 128, 64), new Rectangle(24, 160, 300, 166), 6, 6, 6, 6);
                }
            }
            finally { gd.SetRenderTargets(targets); gd.Viewport = viewport; }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var manager = GraphicsManager.Instance;
            var sb = manager?.Sprite;
            var font = manager?.Font;
            if (sb == null || font == null) return;
            EnsureSurface(sb);
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, transform: UiScaler.SpriteTransform))
                sb.Draw(_surface, DisplayRectangle, Color.White * Alpha);
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, transform: UiScaler.SpriteTransform))
            {
                for (int i = 0; i < 2; i++)
                {
                    if (_textures[1] != null) sb.Draw(_textures[1], Screen(Selector(i)), Color.White * Alpha);
                    if (_selected == i && _pets[i] != null) Border(sb, Screen(Selector(i)), Gold);
                }
                DrawButton(sb, FooterRect, 90, false);
                var close = _textures[_hovered == 90 ? _pressed == 90 ? 8 : 7 : 6];
                if (close != null) sb.Draw(close, Screen(CloseRect), Color.White * Alpha);
                if (IsRaven)
                    for (int i = 0; i < 4; i++) DrawButton(sb, CommandRect(i), 10 + i, !CanCommand);
            }
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                GraphicsManager.GetQualityLinearSamplerState(), transform: UiScaler.SpriteTransform))
            {
                Text(sb, font, "Pets", new(40, 43, 268, 26), Gold, 0.48f);
                for (int i = 0; i < 2; i++) Text(sb, font, PetName(i), Selector(i), _pets[i] == null ? Color.Gray : Gold, 0.36f);
                Text(sb, font, "Close", FooterRect, Color.White, 0.38f);
                var pet = _pets[_selected];
                if (pet == null)
                    Text(sb, font, "Equip a pet in Inventory to view it here.", new(30, 224, 288, 44), Color.LightGray, 0.36f);
                else
                {
                    if (_preview != null && !_preview.IsDisposed) sb.Draw(_preview, Screen(PreviewRect), Color.White * Alpha);
                    Text(sb, font, PetName(_selected), new(126, 172, 190, 24), Gold, 0.40f);
                    Text(sb, font, $"Life: {Math.Max(0, _health[_selected])} / 255", new(126, 204, 190, 20), Color.White, 0.36f);
                    Bar(sb, new(130, 233, 178, 8), Math.Clamp(_health[_selected] / 255f, 0, 1), false);
                    bool trained = Season6PetRules.IsTrainable(pet.Definition.Group, pet.Definition.Id);
                    if (trained && _info[_selected] is { } info)
                    {
                        Text(sb, font, $"Level: {info.Level}", new(36, 262, 276, 20), Color.White, 0.36f);
                        string exp = info.Level >= 50 ? $"Experience: {info.Experience:N0} (MAX)"
                            : $"Experience: {info.Experience:N0} / {Season6PetRules.ExperienceAtLevel(Math.Max(1, info.Level + 1)):N0}";
                        Text(sb, font, exp, new(30, 284, 288, 20), Color.White, 0.34f);
                        Bar(sb, new(36, 310, 276, 6), Season6PetRules.ExperienceProgress(info.Level, info.Experience), true);
                    }
                    else
                        Text(sb, font, trained ? "Loading pet information..." : "Equipped companion", new(32, 274, 284, 24), Color.LightGray, 0.36f);
                    Text(sb, font, IsRaven ? "Base pet stats" : "Pet options", new(30, 327, 288, 18), Gold, 0.36f);
                    for (int i = 0; i < VisibleOptionRows && i + _scroll < _options.Count; i++)
                        Text(sb, font, _options[i + _scroll], new(30, 348 + i * 18, 288, 18), Color.LightGray, 0.35f);
                    if (_options.Count > VisibleOptionRows)
                    {
                        Text(sb, font, "<", new(264, 516, 25, 24), Gold, 0.40f);
                        Text(sb, font, ">", new(291, 516, 25, 24), Gold, 0.40f);
                    }
                    if (IsRaven)
                    {
                        Text(sb, font, "Dark Raven commands", new(30, 422, 288, 22), Gold, 0.38f);
                        for (int i = 0; i < 4; i++)
                        {
                            Color color = _state.RavenCommandMode == i ? Gold : CanCommand ? Color.White : Color.Gray;
                            Text(sb, font, Commands[i], CommandRect(i), color, 0.35f);
                        }
                        Text(sb, font, _commandStatus, new(25, 530, 298, 30), Color.LightGray, 0.32f);
                    }
                    if (Screen(PreviewRect).Contains(MuGame.Instance.UiMouseState.Position) && _tooltips[_selected] != null)
                        ClassicItemTooltipRenderer.Draw(sb, font, pet, _tooltips[_selected], Screen(PreviewRect), MuGame.Instance.UiMouseState.Position);
                }
            }
        }

        private void DrawButton(SpriteBatch sb, Rectangle bounds, int action, bool disabled)
        {
            var texture = _textures[_hovered == action ? _pressed == action ? 5 : 4 : 3];
            if (texture != null) sb.Draw(texture, Screen(bounds), (disabled ? Color.Gray : Color.White) * Alpha);
        }

        private void Bar(SpriteBatch sb, Rectangle local, float fraction, bool experience)
        {
            var rect = Screen(local);
            sb.Draw(GraphicsManager.Instance.Pixel, rect, new Color(12, 8, 5) * Alpha);
            int width = (int)MathF.Round(rect.Width * fraction);
            if (width <= 0) return;
            var fill = new Rectangle(rect.X, rect.Y, width, rect.Height);
            if (!experience && _textures[2] != null) sb.Draw(_textures[2], fill, Color.White * Alpha);
            else sb.Draw(GraphicsManager.Instance.Pixel, fill, Gold * Alpha);
        }

        private void Border(SpriteBatch sb, Rectangle bounds, Color color)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            sb.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), color * Alpha);
            sb.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), color * Alpha);
            sb.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), color * Alpha);
            sb.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), color * Alpha);
        }

        private void Text(SpriteBatch sb, SpriteFont font, string text, Rectangle local, Color color, float scale)
        {
            if (string.IsNullOrEmpty(text)) return;
            var rect = Screen(local);
            var measured = font.MeasureString(text);
            float fitted = Math.Min(scale * Scale, (rect.Width - 6) / Math.Max(1, measured.X));
            var size = measured * fitted;
            var position = new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2);
            sb.DrawString(font, text, position + Vector2.One, Color.Black * Alpha, 0, Vector2.Zero, fitted, SpriteEffects.None, 0);
            sb.DrawString(font, text, position, color * Alpha, 0, Vector2.Zero, fitted, SpriteEffects.None, 0);
        }

        public override void Dispose()
        {
            HideWindow();
            _state = null;
            _preview = null; // Shared BMD cache owns preview textures.
            _surface?.Dispose();
            _surface = null;
            _surfaceBlend?.Dispose();
            _surfaceBlend = null;
            base.Dispose();
        }
    }
}
