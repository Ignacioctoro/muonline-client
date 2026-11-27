using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Utilities;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Scenes
{
    public partial class SelectCharacterScene
    {
        private Texture2D _nativeSelectAtlas;
        private readonly Texture2D[] _nativeActionTextures = new Texture2D[4];
        private RenderTarget2D _nativeSelectionSurface;
        private bool _nativeSelectionSurfaceDirty = true;
        private static readonly Rectangle NativeCardNormal = new(584, 838, 225, 54);
        private static readonly Rectangle NativeCardHover = new(584, 782, 225, 54);
        private static readonly Rectangle NativeCardSelected = new(584, 670, 225, 54);

        // ButtonControl owns the tested hit/Enabled/Click behavior. Its visual
        // is rendered with the other native screen elements in one overlay.
        private sealed class NativeSelectionButton : ButtonControl
        {
            public override void Draw(GameTime gameTime) { }
        }

        private async Task LoadNativeSelectionTexturesAsync()
        {
            var textures = await Task.WhenAll(
                TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/CharacterSelect_I1.ozd"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/b_create.OZT"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/server_menu_b_all.OZT"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/b_connect.OZT"),
                TextureLoader.Instance.PrepareAndGetTexture("Interface/b_delete.OZT"));
            _nativeSelectAtlas = textures[0];
            for (int i = 0; i < 4; i++) _nativeActionTextures[i] = textures[i + 1];
            _nativeSelectionSurfaceDirty = true;
        }

        private void DisposeNativeSelectionSurface()
        {
            _nativeSelectionSurface?.Dispose();
            _nativeSelectionSurface = null;
        }

        private void EnsureNativeSelectionSurface()
        {
            if (!_nativeSelectionSurfaceDirty && _nativeSelectionSurface is { IsDisposed: false }) return;
            if (_nativeSelectAtlas == null || _nativeSelectAtlas.IsDisposed || _characterListRect.Width <= 0 || _characterListRect.Height <= 0) return;
            var gd = GraphicsManager.Instance.GraphicsDevice;
            var previous = gd.GetRenderTargets();
            DisposeNativeSelectionSurface();
            _nativeSelectionSurface = new RenderTarget2D(gd, _characterListRect.Width, _characterListRect.Height,
                false, SurfaceFormat.Color, DepthFormat.None);
            try
            {
                gd.SetRenderTarget(_nativeSelectionSurface);
                gd.Clear(Color.Transparent);
                using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied, SamplerState.LinearClamp);
                foreach (var card in _characterCardRects)
                    GraphicsManager.Instance.Sprite.Draw(_nativeSelectAtlas,
                        new Rectangle(0, card.Y - _characterListRect.Y, card.Width, card.Height), NativeCardNormal, Color.White);
            }
            finally { gd.SetRenderTargets(previous); }
            _nativeSelectionSurfaceDirty = false;
        }

        private void DrawNativeCharacterSelection(SpriteBatch sb)
        {
            if (_characterCreationDialog != null) return;
            var font = GraphicsManager.Instance.Font;
            var pixel = GraphicsManager.Instance.Pixel;
            if (font == null || pixel == null) return;
            if (_nativeSelectionSurface is { IsDisposed: false }) sb.Draw(_nativeSelectionSurface, _characterListRect, Color.White);
            for (int i = 0; i < _characterCardRects.Count; i++)
            {
                var card = _characterCardRects[i];
                bool occupied = i < _characters.Count;
                bool selected = occupied && i == _currentCharacterIndex;
                bool hovered = i == _hoveredCardIndex && !_isSelectionInProgress;
                if (_nativeSelectAtlas != null && (selected || hovered))
                    sb.Draw(_nativeSelectAtlas, card, selected ? NativeCardSelected : NativeCardHover, Color.White);
                if (!occupied)
                {
                    DrawSelectionText(sb, font, "Vacant Character Slot", card, 0.43f, new Color(194, 181, 168), centered: true);
                    continue;
                }
                var character = _characters[i];
                var classRect = new Rectangle(card.X + 13, card.Y + 8, card.Width - 26, card.Height / 2 - 5);
                var nameRect = new Rectangle(card.X + 13, card.Y + card.Height / 2, card.Width - 80, card.Height / 2 - 6);
                var levelRect = new Rectangle(card.Right - 62, nameRect.Y, 48, nameRect.Height);
                DrawSelectionText(sb, font, CharacterClassDatabase.GetClassName(character.Class), classRect, 0.40f,
                    selected ? Color.White : new Color(212, 198, 183));
                DrawSelectionText(sb, font, character.Name, nameRect, 0.46f, new Color(255, 216, 90));
                DrawSelectionText(sb, font, character.Level.ToString(), levelRect, 0.43f, new Color(255, 216, 90), right: true);
            }

            var banner = new Rectangle(18, 45, Math.Min(470, Math.Max(200, _characterListRect.X - 40)), 29);
            sb.Draw(pixel, banner, Color.Black * 0.6f);
            sb.Draw(pixel, new Rectangle(banner.X, banner.Y, banner.Width, 1), new Color(144, 129, 96));
            sb.Draw(pixel, new Rectangle(banner.X, banner.Bottom - 1, banner.Width, 1), new Color(144, 129, 96));
            DrawSelectionText(sb, font, $"Select your character   {_characters.Count} / 5", new Rectangle(banner.X + 10, banner.Y, banner.Width - 20, banner.Height), 0.38f, Color.White);
            DrawNativeSelectionButton(sb, _createCharacterButton, "Create");
            DrawNativeSelectionButton(sb, _exitButton, "Menu");
            DrawNativeSelectionButton(sb, _enterGameButton, "Connect");
            DrawNativeSelectionButton(sb, _deleteCharacterButton, "Delete");
        }

        private void DrawNativeSelectionButton(SpriteBatch sb, ButtonControl button, string caption)
        {
            if (button == null || !button.Visible) return;
            var rect = button.DisplayRectangle;
            bool hover = button.Enabled && rect.Contains(MuGame.Instance.UiMouseState.Position);
            bool pressed = hover && MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed;
            int index = button == _createCharacterButton ? 0 : button == _exitButton ? 1 : button == _enterGameButton ? 2 : 3;
            // Original CButton: four vertical 54x30 frames, normal/hover/down/disabled.
            // Menu has three frames; its disabled state uses a dimmed normal frame.
            int frame = !button.Enabled ? (index == 1 ? 0 : 3) : pressed ? 2 : hover ? 1 : 0;
            var texture = _nativeActionTextures[index];
            if (texture != null) sb.Draw(texture, rect, new Rectangle(0, frame * 30, 54, 30),
                !button.Enabled && index == 1 ? Color.Gray : Color.White);
            else DrawSelectionText(sb, GraphicsManager.Instance.Font, caption, rect, 0.40f, Theme.Accent, centered: true);
        }

        private static void DrawSelectionText(SpriteBatch sb, SpriteFont font, string text, Rectangle rect,
            float scale, Color color, bool centered = false, bool right = false)
        {
            var measured = font.MeasureString(text);
            if (measured.X > 0) scale = Math.Min(scale, rect.Width / measured.X);
            var size = measured * scale;
            float x = centered ? rect.X + (rect.Width - size.X) / 2 : right ? rect.Right - size.X : rect.X;
            var pos = new Vector2(x, rect.Y + (rect.Height - size.Y) / 2);
            sb.DrawString(font, text, pos + Vector2.One, Color.Black * 0.9f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            sb.DrawString(font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
