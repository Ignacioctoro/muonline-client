using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    public sealed class MasterTreeRequirement
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool Met { get; set; }
    }

    public sealed class MasterTreeNode
    {
        public int Index { get; set; }
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Root { get; set; } = string.Empty;
        public int Rank { get; set; }
        public int Maximum { get; set; }
        public int Cost { get; set; }
        public int Level { get; set; }
        public float Current { get; set; }
        public float Next { get; set; }
        public bool RankMet { get; set; }
        public bool CanLearn { get; set; }
        public MasterTreeRequirement[] Requirements { get; set; } = Array.Empty<MasterTreeRequirement>();
    }

    public sealed class MasterTreeSnapshot
    {
        public string CharacterName { get; set; } = string.Empty;
        public int Class { get; set; }
        public int Points { get; set; }
        public MasterTreeNode[] Nodes { get; set; } = Array.Empty<MasterTreeNode>();
    }

    public sealed class MasterSkillTreeControl : UIControl, IUiTexturePreloadable
    {
        private const int Width = 930;
        private const int Height = 710;
        private static readonly Rectangle CloseButton = new(899, 34, 25, 28);
        private static readonly Rectangle MasterExperienceLabel = new(705, 39, 178, 24);
        private static readonly Rectangle RefreshButton = new(752, 660, 133, 28);
        private static readonly string[] Paths =
        {
            "Interface/GFx/masterskill_bg01.ozd",
            "Interface/new_Master_Icon.OZJ",
            "Interface/new_Master_non_Icon.OZJ",
            "Interface/GFx/masterskill_iconbox.ozd",
            "Interface/GFx/masterskill_bg02.ozd",
            "Interface/GFx/masterskill_bg03.ozd",
            "Interface/GFx/masterskill_bg04.ozd",
        };
        private static class Theme
        {
            public static readonly Color Background = new(14, 11, 10);
            public static readonly Color Tooltip = new(12, 10, 9, 245);
            public static readonly Color Border = new(97, 74, 39);
            public static readonly Color Muted = new(135, 132, 126);
            public static readonly Color Text = new(225, 217, 200);
            public static readonly Color Gold = new(221, 184, 87);
        }

        public static MasterSkillTreeControl Instance { get; } = new();
        public static bool IsOpen => Instance.Visible;
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private readonly Texture2D[] _textures = new Texture2D[Paths.Length];
        private readonly Dictionary<int, MasterTreeNode> _bySkill = new();
        private MasterTreeSnapshot _tree;
        private RenderTarget2D _background;
        private BlendState _backgroundBlend;
        private string _characterName;
        private int _characterClass;
        private bool _waiting;
        private DateTime _deadline;
        private string _status = string.Empty;
        private MasterTreeNode _hovered;
        private readonly List<string> _tooltip = new();

        private MasterSkillTreeControl()
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
            for (int i = 0; i < Paths.Length; i++)
                _textures[i] = await TextureLoader.Instance.PrepareAndGetTexture(Paths[i]);
        }

        public void Toggle()
        {
            if (Visible) { CloseWindow(); return; }
            var state = MuGame.Network?.GetCharacterState();
            if (state == null) return;
            _characterName = state.Name;
            _characterClass = (int)state.Class;
            _tree = null;
            _bySkill.Clear();
            _hovered = null;
            _waiting = false;
            Visible = true;
            PositionWindow();
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            Refresh();
        }

        public void CloseWindow()
        {
            Visible = false;
            _waiting = false;
            _hovered = null;
            _tooltip.Clear();
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
        }

        private void Refresh()
        {
            if (_waiting) return;
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            _waiting = true;
            _deadline = DateTime.UtcNow.AddSeconds(8);
            _status = "Loading master skill tree...";
            _ = service.RequestMasterTreeAsync();
            _ = service.RequestCharacterProgressionAsync();
        }

        public void SetSnapshot(MasterTreeSnapshot snapshot)
        {
            var state = MuGame.Network?.GetCharacterState();
            if (!Visible || state == null || snapshot.CharacterName != _characterName
                || snapshot.CharacterName != state.Name || snapshot.Class != _characterClass || snapshot.Class != (int)state.Class)
                return;

            _tree = snapshot;
            _bySkill.Clear();
            foreach (var node in snapshot.Nodes) _bySkill[node.SkillId] = node;
            _waiting = false;
            _hovered = null;
            state.MasterLevelUpPoints = (ushort)Math.Clamp(snapshot.Points, 0, ushort.MaxValue);
            _status = snapshot.Nodes.Length == 0 ? "The master tree is available after the master class advancement." :
                "Click an available skill to spend master points.";
        }

        public void LearnedSkillsChanged()
        {
            if (Visible && !_waiting) Refresh();
        }

        private static Rectangle NodeRect(MasterTreeNode node)
        {
            int slot = node.Index - 1;
            int branch = slot / 36;
            int column = slot % 4;
            return new Rectangle(43 + branch * 306 + column * 59, 142 + (node.Rank - 1) * 53, 44, 45);
        }

        private void PositionWindow()
        {
            var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
            Scale = MathHelper.Clamp(Math.Min((size.X - 12f) / Width, (size.Y - 12f) / Height), 0.1f, 1f);
            X = (size.X - DisplaySize.X) / 2;
            Y = (size.Y - DisplaySize.Y) / 2;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            var state = MuGame.Network?.GetCharacterState();
            if (state == null || state.Name != _characterName || (int)state.Class != _characterClass)
            { CloseWindow(); return; }

            PositionWindow();
            base.Update(gameTime);
            if (_waiting && DateTime.UtcNow >= _deadline)
            {
                _waiting = false;
                _status = "No response. Refresh to synchronize before spending points.";
                _tree = null;
                _bySkill.Clear();
            }
            if (Scene?.IsPointerInputBlockedFor(this) == true) { _hovered = null; return; }
            var game = MuGame.Instance;
            if (HasFocus && game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
            { Scene?.ConsumeKeyboardEscape(); CloseWindow(); return; }

            var point = ToLocal(game.UiMouseState.Position);
            MasterTreeNode hovered = null;
            if (_tree != null)
                foreach (var node in _tree.Nodes)
                    if (NodeRect(node).Contains(point)) { hovered = node; break; }
            if (hovered != _hovered) { _hovered = hovered; BuildTooltip(); }

            if (game.UiMouseState.LeftButton != ButtonState.Pressed || game.PrevUiMouseState.LeftButton != ButtonState.Released
                || !DisplayRectangle.Contains(game.UiMouseState.Position)) return;

            Scene?.SetMouseInputConsumed();
            if (CloseButton.Contains(point)) { CloseWindow(); return; }
            if (RefreshButton.Contains(point)) { Refresh(); return; }
            if (_waiting || hovered?.CanLearn != true) return;

            _waiting = true;
            _deadline = DateTime.UtcNow.AddSeconds(8);
            _status = "Waiting for the server...";
            _ = MuGame.Network.GetCharacterService().SendMasterSkillPointAsync((ushort)hovered.SkillId);
        }

        private void BuildTooltip()
        {
            _tooltip.Clear();
            if (_hovered == null) return;
            _tooltip.Add(_hovered.Name);
            _tooltip.Add($"Level {_hovered.Level} / {_hovered.Maximum}");
            _tooltip.Add($"Current value: {_hovered.Current.ToString("0.##", CultureInfo.InvariantCulture)}");
            _tooltip.Add($"Next value: {_hovered.Next.ToString("0.##", CultureInfo.InvariantCulture)}");
            _tooltip.Add($"Cost: {_hovered.Cost} master point(s)");
            if (!_hovered.RankMet) _tooltip.Add("Requires a previous-rank skill at level 10.");
            foreach (var required in _hovered.Requirements)
            {
                _tooltip.Add(required.Name);
                _tooltip.Add($"  Required level {required.Level} {(required.Met ? "(learned)" : "(missing)")}");
            }
            if (_hovered.Level >= _hovered.Maximum) _tooltip.Add("Maximum level reached.");
        }

        private void EnsureBackground()
        {
            if (_background != null && !_background.IsDisposed) return;
            var graphics = GraphicsManager.Instance;
            var device = graphics.GraphicsDevice;
            var previous = device.GetRenderTargets();
            var viewport = device.Viewport;
            _background = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
            _backgroundBlend ??= new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.InverseSourceAlpha,
            };

            try
            {
                var batch = graphics.Sprite;
                using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, _backgroundBlend, SamplerState.LinearClamp);
                device.SetRenderTarget(_background);
                device.Clear(Color.Transparent);
                for (int tile = 0; tile < 4; tile++)
                {
                    var texture = _textures[tile == 0 ? 0 : 3 + tile];
                    if (texture == null) continue;
                    var destination = new Rectangle(tile % 2 * 465, tile / 2 * 355, 465, 355);
                    batch.Draw(texture, destination, Color.White);
                }
            }
            finally
            {
                device.SetRenderTargets(previous);
                device.Viewport = viewport;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            EnsureBackground();
            var graphics = GraphicsManager.Instance;
            var batch = graphics.Sprite;
            var font = graphics.Font;
            var pixel = graphics.Pixel;
            if (font == null || pixel == null) return;
            var transform = Matrix.CreateScale(Scale, Scale, 1f) *
                Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0) * UiScaler.SpriteTransform;
            // The cached surface contains premultiplied alpha. Source textures below use straight alpha.
            using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                GraphicsManager.GetQualityLinearSamplerState(), transform: transform))
                batch.Draw(_background, new Rectangle(0, 0, Width, Height), Color.White);

            using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                GraphicsManager.GetQualityLinearSamplerState(), transform: transform);
            var state = MuGame.Network?.GetCharacterState();
            int points = state?.MasterLevelUpPoints ?? _tree?.Points ?? 0;
            Text(batch, font, "Master Skill Tree", new Rectangle(30, 39, 295, 24), Theme.Gold, 17, true);
            Text(batch, font, $"Master Level: {state?.MasterLevel ?? 0}", new Rectangle(340, 39, 160, 24), Theme.Gold, 13, true);
            Text(batch, font, $"Available Points: {points}", new Rectangle(512, 39, 180, 24), Theme.Gold, 13, true);
            string masterExperience = state?.HasMasterExperienceInfo == true
                ? $"EXP: {ExperienceProgressCalculator.GetCharacterFraction(state, true) * 100f:0.00}%"
                : "EXP: --";
            Text(batch, font, masterExperience, MasterExperienceLabel, Theme.Gold, 13, true);
            Text(batch, font, "X", CloseButton, Theme.Gold, 16, true);
            for (int branch = 0; branch < 3; branch++)
            {
                string name = string.Empty;
                int spent = 0;
                if (_tree != null)
                    foreach (var node in _tree.Nodes)
                        if ((node.Index - 1) / 36 == branch) { name = node.Root; spent += node.Level; }
                Text(batch, font, $"{name}  {spent}", new Rectangle(28 + branch * 306, 80, 270, 23), Theme.Gold, 14, true);
            }
            if (_tree != null)
            {
                foreach (var node in _tree.Nodes)
                    foreach (var required in node.Requirements)
                        if (_bySkill.TryGetValue(required.SkillId, out var parent))
                        {
                            var from = NodeRect(parent);
                            var to = NodeRect(node);
                            var color = required.Met ? Theme.Gold : Theme.Border;
                            int middle = (from.Bottom + to.Top) / 2;
                            batch.Draw(pixel, new Rectangle(from.Center.X, from.Bottom, 1, Math.Max(1, middle - from.Bottom)), color);
                            batch.Draw(pixel, new Rectangle(Math.Min(from.Center.X, to.Center.X), middle,
                                Math.Max(1, Math.Abs(to.Center.X - from.Center.X)), 1), color);
                            batch.Draw(pixel, new Rectangle(to.Center.X, middle, 1, Math.Max(1, to.Top - middle)), color);
                            Text(batch, font, "v", new Rectangle(to.Center.X - 6, to.Top - 11, 12, 12), color, 10, true);
                        }
                foreach (var node in _tree.Nodes) DrawNode(batch, pixel, font, node);
            }

            Text(batch, font, _status, new Rectangle(30, 660, 715, 27), Theme.Text, 12);
            Panel(batch, pixel, RefreshButton, Theme.Background);
            Text(batch, font, "Refresh", RefreshButton, Theme.Gold, 13, true);
            DrawTooltip(batch, pixel, font);
            if (state?.HasMasterExperienceInfo == true && MasterExperienceLabel.Contains(ToLocal(MuGame.Instance.UiMouseState.Position)))
            {
                var bounds = new Rectangle(500, 72, 390, 34);
                Panel(batch, pixel, bounds, Theme.Tooltip);
                Text(batch, font, $"Master EXP: {state.MasterExperience:N0} / {state.MasterExperienceForNextLevel:N0}",
                    bounds, Theme.Text, 12, true);
            }
        }

        private void DrawNode(SpriteBatch batch, Texture2D pixel, SpriteFont font, MasterTreeNode node)
        {
            var rect = NodeRect(node);
            bool active = node.CanLearn || node.Level > 0;
            Panel(batch, pixel, rect, active ? new Color(47, 33, 22) : Theme.Background);
            if (_textures[3] != null)
                batch.Draw(_textures[3], new Rectangle(rect.X + 7, rect.Y, 30, 37), new Rectangle(0, 0, 40, 60),
                    active ? Color.White : Color.Gray);
            var icon = SkillDatabase.GetSkillDefinition(node.SkillId)?.MagicIcon;
            var atlas = _textures[active ? 1 : 2];
            if (icon.HasValue && atlas != null)
            {
                var source = new Rectangle(icon.Value % 25 * 20, icon.Value / 25 * 28, 20, 28);
                if (source.Right <= atlas.Width && source.Bottom <= atlas.Height)
                    batch.Draw(atlas, new Rectangle(rect.X + 12, rect.Y + 3, 20, 28), source, Color.White);
            }
            if (node == _hovered) batch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), Theme.Gold);
            Text(batch, font, $"{node.Level}/{node.Maximum}", new Rectangle(rect.X, rect.Y + 31, rect.Width, 13),
                active ? Theme.Gold : Theme.Muted, 9, true);
        }

        private void DrawTooltip(SpriteBatch batch, Texture2D pixel, SpriteFont font)
        {
            if (_hovered == null || _tooltip.Count == 0) return;
            var point = ToLocal(MuGame.Instance.UiMouseState.Position);
            float width = 240;
            foreach (var line in _tooltip) width = Math.Max(width, font.MeasureString(line).X * 12f / Constants.BASE_FONT_SIZE + 24);
            int height = 20 + _tooltip.Count * 20;
            var rect = new Rectangle(Math.Clamp(point.X + 18, 8, Math.Max(8, Width - (int)width - 8)),
                Math.Clamp(point.Y + 18, 8, Math.Max(8, Height - height - 8)), (int)width, height);
            Panel(batch, pixel, rect, Theme.Tooltip);
            for (int i = 0; i < _tooltip.Count; i++)
                Text(batch, font, _tooltip[i], new Rectangle(rect.X + 12, rect.Y + 9 + i * 20, rect.Width - 24, 20),
                    i == 0 ? Theme.Gold : Theme.Text, 12);
        }

        private static void Panel(SpriteBatch batch, Texture2D pixel, Rectangle rect, Color fill)
        {
            batch.Draw(pixel, rect, Theme.Border);
            rect.Inflate(-1, -1);
            batch.Draw(pixel, rect, fill);
        }

        private static void Text(SpriteBatch batch, SpriteFont font, string value, Rectangle bounds, Color color, float size, bool center = false)
        {
            float scale = size / Constants.BASE_FONT_SIZE;
            var measured = font.MeasureString(value) * scale;
            batch.DrawString(font, value, new Vector2(center ? bounds.Center.X - measured.X / 2 : bounds.X,
                bounds.Center.Y - measured.Y / 2), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private Point ToLocal(Point point) => new((int)Math.Floor((point.X - DisplayPosition.X) / Scale),
            (int)Math.Floor((point.Y - DisplayPosition.Y) / Scale));

        public override void Dispose()
        {
            _background?.Dispose();
            _background = null;
            _backgroundBlend?.Dispose();
            _backgroundBlend = null;
            base.Dispose();
        }
    }
}
