using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Controls.UI.SelectCharacter
{
    public class CharacterCreationDialog : GameControl
    {
        public override bool BlocksPointerInput => true;
        public event EventHandler<(string Name, CharacterClassNumber Class)> CharacterCreateRequested;
        public event EventHandler CancelRequested;
        private sealed record ClassInfo(CharacterClassNumber Class, string Name, int Face,
            int Strength, int Agility, int Stamina, int Energy, int Command, string Description)
        {
            public string[] Lines { get; } = Description.Split('\n');
        }
        // OpenMU's Season Six base attributes; server configuration remains authoritative.
        private static readonly ClassInfo[] Classes =
        {
            new(CharacterClassNumber.DarkWizard, "Dark Wizard", 0, 18,18,15,30,0, "A master of destructive magic.\nUses powerful spells to attack\nenemies from a distance."),
            new(CharacterClassNumber.DarkKnight, "Dark Knight", 1, 28,20,25,10,0, "A knight of Lorencia, trained\nin strength and swordsmanship.\nExcels in close-range combat."),
            new(CharacterClassNumber.FairyElf, "Fairy Elf", 2, 22,25,20,15,0, "An agile archer from Noria.\nCombines bow attacks with\nhealing and support magic."),
            new(CharacterClassNumber.Summoner, "Summoner", 5, 21,21,18,23,0, "A mystic from Elbeland.\nUses curses and summoned\npowers to weaken enemies."),
            new(CharacterClassNumber.RageFighter, "Rage Fighter", 6, 32,27,25,20,0, "A powerful hand-to-hand fighter.\nUses martial arts and devastating\nphysical attacks."),
            new(CharacterClassNumber.MagicGladiator, "Magic Gladiator", 3, 26,26,26,26,0, "A warrior who combines\nswordsmanship and magic.\nSkilled in both forms of combat."),
            new(CharacterClassNumber.DarkLord, "Dark Lord", 4, 26,20,20,15,25, "A commander of dark powers.\nCommands the Dark Raven\nand rides the Dark Horse."),
        };
        private int _selected = 1;
        private double _portraitTime;
        private bool _submitted;
        private readonly CharacterCreationPortraitRenderer _portrait = new();
        private readonly NameInput _nameInput;
        private readonly Rectangle[] _classRects = new Rectangle[7];
        private Rectangle _portraitRect, _statsRect, _nameRect, _descriptionRect, _okRect, _cancelRect, _headerRect;
        private Texture2D _atlas, _sidebar, _buttons;
        private RenderTarget2D _staticSurface;
        private Texture2D _preparedPortrait;
        private bool _dirty = true;
        private readonly BlendState _surfaceBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };
        private static readonly Color Gold = new(234, 206, 132);

        public CharacterCreationDialog()
        {
            AutoViewSize = false; Interactive = true; Align = ControlAlign.None;
            _nameInput = new NameInput { MaxLength = 10, PlaceholderText = "Enter name", TextColor = Color.White };
            Controls.Add(_nameInput);
            Layout(UiScaler.VirtualSize);
        }

        public override async Task Load()
        {
            var textures = await Task.WhenAll(
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/CharacterSelect_I1.ozd"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/CharacterSelect_IA1.ozd"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/winLoginSceneBottomUI_I1.ozd"));
            _atlas = textures[0]; _sidebar = textures[1]; _buttons = textures[2];
            await _portrait.LoadAsync();
            _nameInput.Focus(); _dirty = true;
        }

        private void Layout(Point size)
        {
            if (_nameInput == null) return;
            ViewSize = size; X = Y = 0;
            float s = Math.Min(size.X / 1024f, size.Y / 768f);
            int ox = (int)((size.X - 1024 * s) / 2), oy = (int)((size.Y - 768 * s) / 2);
            Rectangle R(int x, int y, int w, int h) => new(ox + (int)(x * s), oy + (int)(y * s), Math.Max(1, (int)(w * s)), Math.Max(1, (int)(h * s)));
            _portraitRect = R(155, 160, 440, 385);
            _statsRect = R(590, 93, 134, 380);
            _headerRect = R(567, 65, 334, 29);
            for (int i = 0; i < Classes.Length; i++) _classRects[i] = R(725, 99 + i * 31, 153, 29);
            _nameRect = R(237, 503, 331, 33);
            _descriptionRect = R(272, 540, 330, 111);
            _okRect = R(641, 503, 79, 31); _cancelRect = R(641, 539, 79, 31);
            _nameInput.X = _nameRect.X; _nameInput.Y = _nameRect.Y;
            _nameInput.ViewSize = _nameRect.Size;
            _dirty = true;
        }

        public override void Update(GameTime gameTime)
        {
            if (Parent != null && ViewSize != Parent.ViewSize) Layout(Parent.ViewSize);
            base.Update(gameTime);
            if (!Visible || Status != GameControlStatus.Ready) return;
            _portraitTime += Math.Min(0.1, gameTime.ElapsedGameTime.TotalSeconds);
            bool click = MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
            var mouse = MuGame.Instance.UiMouseState.Position;
            if (click)
            {
                for (int i = 0; i < _classRects.Length; i++)
                    if (_classRects[i].Contains(mouse)) { _selected = i; _portraitTime = 0; Scene?.SetMouseInputConsumed(); break; }
                if (_okRect.Contains(mouse)) Submit();
                else if (_cancelRect.Contains(mouse)) CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
                CancelRequested?.Invoke(this, EventArgs.Empty);
            else if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Enter) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Enter)) Submit();
        }

        private void Submit()
        {
            if (_submitted) return;
            string name = _nameInput.Text.Trim();
            if (name.Length < 3 || name.Length > 10) { MessageWindow.Show("Character name must contain 3 to 10 characters."); return; }
            _submitted = true;
            CharacterCreateRequested?.Invoke(this, (name, Classes[_selected].Class));
        }

        private void EnsureSurface()
        {
            if (!_dirty && _staticSurface is { IsDisposed: false }) return;
            if (_atlas == null || _sidebar == null) return;
            _staticSurface?.Dispose();
            var gd = GraphicsDevice; var targets = gd.GetRenderTargets();
            _staticSurface = new RenderTarget2D(gd, ViewSize.X, ViewSize.Y, false, SurfaceFormat.Color, DepthFormat.None);
            try
            {
                gd.SetRenderTarget(_staticSurface); gd.Clear(Color.Transparent);
                using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred, _surfaceBlend, SamplerState.LinearClamp);
                var sb = GraphicsManager.Instance.Sprite; var pixel = GraphicsManager.Instance.Pixel;
                sb.Draw(_sidebar, _headerRect, new Rectangle(0, 0, 338, 30), Color.White);
                sb.Draw(_sidebar, _statsRect, new Rectangle(29, 31, 130, 440), Color.White);
                foreach (var rect in _classRects) sb.Draw(_atlas, rect, new Rectangle(146, 964, 145, 27), Color.White);
                sb.Draw(pixel, _descriptionRect, new Color(0, 0, 0, 185));
                sb.Draw(pixel, new Rectangle(_descriptionRect.X, _descriptionRect.Y, 1, _descriptionRect.Height), new Color(91, 75, 52));
                // Native nameplate/sword ornament and hooded figure.
                var namePlate = new Rectangle(_nameRect.X - _nameRect.Height * 2, _nameRect.Y - 4, _nameRect.Width + _nameRect.Height * 2, _nameRect.Height + 8);
                sb.Draw(_atlas, namePlate, new Rectangle(0, 621, 374, 67), Color.White);
                var hood = new Rectangle(_okRect.X - _okRect.Width, _okRect.Y - _okRect.Height * 2, _okRect.Width * 2, _okRect.Height * 6);
                sb.Draw(_atlas, hood, new Rectangle(315, 585, 265, 250), Color.White);
            }
            finally { gd.SetRenderTargets(targets); }
            _dirty = false;
        }

        // Called by the scene before drawing the world and before opening any UI batch.
        // Rebinding the main DiscardContents target after offscreen rendering invalidates
        // its previous pixels, so render-target switches must happen before world drawing.
        internal void PrepareDraw()
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            _preparedPortrait = _portrait.Render(Classes[_selected].Face, _portraitRect.Width, _portraitRect.Height, _portraitTime);
            EnsureSurface();
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, transform: UiScaler.SpriteTransform);
            var sb = GraphicsManager.Instance.Sprite;
            if (_preparedPortrait != null) sb.Draw(_preparedPortrait, _portraitRect, Color.White);
            if (_staticSurface != null) sb.Draw(_staticSurface, DisplayRectangle, Color.White);
            var mouse = MuGame.Instance.UiMouseState.Position;
            for (int i = 0; i < Classes.Length; i++)
            {
                var rect = _classRects[i];
                if (i == _selected || rect.Contains(mouse)) sb.Draw(_atlas, rect, new Rectangle(0, 964, 145, 27), Color.White);
                Text(sb, Classes[i].Name, rect, i == _selected ? Color.White : new Color(191, 178, 159), 0.40f, true);
            }
            Text(sb, "Create Character", _headerRect, Color.White, 0.42f, true);
            var selected = Classes[_selected];
            float scale = ViewSize.Y / 768f;
            int row = Math.Max(28, (int)(52 * scale));
            Text(sb, selected.Name, new Rectangle(_statsRect.X + 7, _statsRect.Y + 7, _statsRect.Width - 14, row / 2), Color.White, 0.38f, true);
            Stat(sb, "STR", selected.Strength, 0, row); Stat(sb, "AGI", selected.Agility, 1, row);
            Stat(sb, "STA", selected.Stamina, 2, row); Stat(sb, "ENG", selected.Energy, 3, row);
            if (selected.Command > 0) Stat(sb, "CMD", selected.Command, 4, row);
            Text(sb, "Name", new Rectangle(_nameRect.X - 63, _nameRect.Y, 58, _nameRect.Height), Color.White, 0.42f, true);
            var lines = selected.Lines;
            for (int i = 0; i < lines.Length; i++) Text(sb, lines[i], new Rectangle(_descriptionRect.X + 12, _descriptionRect.Y + 10 + i * 22, _descriptionRect.Width - 24, 22), new Color(190, 183, 176), 0.40f);
            DrawButton(sb, _okRect, "OK", mouse); DrawButton(sb, _cancelRect, "Cancel", mouse);
            _nameInput.Draw(gameTime);
        }

        private void Stat(SpriteBatch sb, string label, int value, int index, int row)
        {
            var rect = new Rectangle(_statsRect.X + 12, _statsRect.Y + row / 2 + index * row + 10, _statsRect.Width - 24, row - 4);
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Bottom, rect.Width, 1), new Color(78, 69, 53, 150));
            Text(sb, label, new Rectangle(rect.X, rect.Y, rect.Width, 20), new Color(178, 173, 165), 0.36f);
            Text(sb, value.ToString(), new Rectangle(rect.X, rect.Y + 19, rect.Width, 20), Color.White, 0.42f, right: true);
        }

        private void DrawButton(SpriteBatch sb, Rectangle rect, string caption, Point mouse)
        {
            var src = rect.Contains(mouse) ? new Rectangle(258, 2, 117, 28) : new Rectangle(131, 2, 117, 28);
            if (_buttons != null) sb.Draw(_buttons, rect, src, Color.White);
            Text(sb, caption, rect, Gold, 0.43f, true);
        }

        private static void Text(SpriteBatch sb, string value, Rectangle rect, Color color, float scale, bool centered = false, bool right = false)
        {
            var font = GraphicsManager.Instance.Font; var measured = font.MeasureString(value);
            if (measured.X > 0) scale = Math.Min(scale, rect.Width / measured.X);
            var size = measured * scale;
            var pos = new Vector2(centered ? rect.X + (rect.Width - size.X) / 2 : right ? rect.Right - size.X : rect.X, rect.Y + (rect.Height - size.Y) / 2);
            sb.DrawString(font, value, pos + Vector2.One, Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            sb.DrawString(font, value, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private sealed class NameInput : TextBoxControl
        {
            public override void Draw(GameTime gameTime)
            {
                var sb = GraphicsManager.Instance.Sprite; var rect = DisplayRectangle;
                sb.Draw(GraphicsManager.Instance.Pixel, rect, new Color(0, 0, 0, 175));
                string value = string.IsNullOrEmpty(Text) ? PlaceholderText : Text;
                TextValue(sb, value, rect, string.IsNullOrEmpty(Text) ? Color.Gray : Color.White);
                if ((gameTime.TotalGameTime.TotalSeconds % 1) < 0.5)
                {
                    float x = rect.X + 8 + GraphicsManager.Instance.Font.MeasureString(Text).X * 0.48f;
                    sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle((int)x, rect.Y + 6, 1, Math.Max(1, rect.Height - 12)), Color.White);
                }
            }
            private static void TextValue(SpriteBatch sb, string value, Rectangle rect, Color color) =>
                CharacterCreationDialog.Text(sb, value, new Rectangle(rect.X + 8, rect.Y, rect.Width - 16, rect.Height), color, 0.48f);
        }

        public override void Dispose()
        {
            _portrait.Dispose(); _staticSurface?.Dispose(); _surfaceBlend.Dispose();
            base.Dispose();
        }
    }
}
