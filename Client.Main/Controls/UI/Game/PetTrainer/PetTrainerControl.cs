using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Season 6 Pet Trainer entry dialog, skinned with original Data_Broyal GFx sprites.
    /// The two operations are deliberately left as events for the next implementation stages.
    /// The NPC session is opened by OpenMU's NpcWindowResponse (PetTrainer = 7).
    /// </summary>
    public sealed class PetTrainerControl : UIControl, IUiTexturePreloadable
    {
        private const int WindowWidth = 350;
        private const int WindowHeight = 342;

        // Match CharacterInfoWindowControl: use the same verified right-side GFx panel.
        // Do not crop across the separate NpcDialogue sprites (arrows / separator).
        private const string CharacterAtlasPath = "Interface/GFx/CharacterInfoFrame_I1.ozd";
        private const string ButtonNormalPath = "Interface/GFx/btn_l01.ozd";
        private const string ButtonHoverPath = "Interface/GFx/btn_l02.ozd";
        private const string ButtonPressedPath = "Interface/GFx/btn_l03.ozd";
        private const string CloseNormalPath = "Interface/GFx/btn_close01.ozd";
        private const string CloseHoverPath = "Interface/GFx/btn_close02.ozd";
        private const string ClosePressedPath = "Interface/GFx/btn_close03.ozd";

        // Same Character GFx source regions as CharacterInfoWindowControl.cs.
        // Original Character panel is 354 x 633: preserve the complete 96px title
        // and 34px footer when shortening it into a compact trainer dialog.
        private static readonly Rectangle FrameSource = new(656, 0, 354, 633);
        private static readonly Rectangle InformationSource = new(676, 645, 245, 56);
        private const int FrameTopHeight = 96;
        private const int FrameBottomHeight = 34;

        private static readonly Rectangle InformationTarget = new(27, 99, 296, 75);
        private static readonly Rectangle RestoreButtonTarget = new(57, 187, 236, 56);
        private static readonly Rectangle ResurrectButtonTarget = new(57, 249, 236, 56);
        private static readonly Rectangle CloseButtonTarget = new(307, 36, 25, 25);

        private static PetTrainerControl _instance;

        private readonly ILogger<PetTrainerControl> _logger =
            MuGame.AppLoggerFactory?.CreateLogger<PetTrainerControl>();

        private Texture2D _characterAtlas;
        private Texture2D _buttonNormal;
        private Texture2D _buttonHover;
        private Texture2D _buttonPressed;
        private Texture2D _closeNormal;
        private Texture2D _closeHover;
        private Texture2D _closePressed;

        private int _hoveredButton = -1;
        private int _pressedButton = -1;
        private bool _closeRequestSent;

        private PetTrainerControl()
        {
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
        }

        public static PetTrainerControl Instance => _instance ??= new PetTrainerControl();

        public static bool IsOpen => _instance?.Visible == true;

        public override bool NonDisposable => true;

        public event Action RestoreLifeRequested;
        public event Action ResurrectSpiritRequested;

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            yield return CharacterAtlasPath;
            yield return ButtonNormalPath;
            yield return ButtonHoverPath;
            yield return ButtonPressedPath;
            yield return CloseNormalPath;
            yield return CloseHoverPath;
            yield return ClosePressedPath;
        }

        public override async Task Load()
        {
            await base.Load();

            var loader = TextureLoader.Instance;
            var textures = await Task.WhenAll(
                loader.PrepareAndGetTexture(CharacterAtlasPath),
                loader.PrepareAndGetTexture(ButtonNormalPath),
                loader.PrepareAndGetTexture(ButtonHoverPath),
                loader.PrepareAndGetTexture(ButtonPressedPath),
                loader.PrepareAndGetTexture(CloseNormalPath),
                loader.PrepareAndGetTexture(CloseHoverPath),
                loader.PrepareAndGetTexture(ClosePressedPath));

            _characterAtlas = textures[0];
            _buttonNormal = textures[1];
            _buttonHover = textures[2];
            _buttonPressed = textures[3];
            _closeNormal = textures[4];
            _closeHover = textures[5];
            _closePressed = textures[6];

            if (_characterAtlas == null || _buttonNormal == null)
            {
                _logger?.LogWarning("Pet Trainer Character/NewUI GFx textures are missing from Data/Interface/GFx.");
            }
        }

        public void ShowWindow()
        {
            if (Visible)
            {
                BringToFront();
                return;
            }

            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
            _hoveredButton = -1;
            _pressedButton = -1;
            _closeRequestSent = false;
            Visible = true;
            BringToFront();
            if (Scene != null)
            {
                Scene.FocusControl = this;
            }

            SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void CloseWindow()
        {
            if (!Visible)
            {
                return;
            }

            HideWithoutServerRequest();
            if (!_closeRequestSent)
            {
                _closeRequestSent = true;
                _ = SendCloseNpcRequestAsync();
            }
        }

        /// <summary>Used when leaving GameScene; the old network session may already be gone.</summary>
        public void HideWithoutServerRequest()
        {
            Visible = false;
            _hoveredButton = -1;
            _pressedButton = -1;
            if (Scene?.FocusControl == this)
            {
                Scene.FocusControl = null;
            }
        }

        private async Task SendCloseNpcRequestAsync()
        {
            try
            {
                var service = MuGame.Network?.GetCharacterService();
                if (service != null)
                {
                    await service.SendCloseNpcRequestAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Unable to close the Pet Trainer NPC session.");
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }

            base.Update(gameTime);

            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) &&
                MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                CloseWindow();
                return;
            }

            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool pressed = mouse.LeftButton == ButtonState.Pressed;
            bool justPressed = pressed && previous.LeftButton == ButtonState.Released;
            bool justReleased = !pressed && previous.LeftButton == ButtonState.Pressed;
            bool inside = DisplayRectangle.Contains(mouse.Position);

            _hoveredButton = inside ? HitTestButton(mouse.Position) : -1;

            if (inside && pressed)
            {
                // No movement/attacks underneath the NPC dialog.
                Scene?.SetMouseInputConsumed();
            }

            if (justPressed)
            {
                _pressedButton = inside ? _hoveredButton : -1;
            }

            if (justReleased)
            {
                int action = _pressedButton;
                _pressedButton = -1;
                if (inside && action >= 0 && action == _hoveredButton)
                {
                    SoundController.Instance.PlayBuffer("Sound/iButton.wav");
                    switch (action)
                    {
                        case 0:
                            RestoreLifeRequested?.Invoke();
                            break;
                        case 1:
                            ResurrectSpiritRequested?.Invoke();
                            break;
                        case 2:
                            CloseWindow();
                            break;
                    }

                    Scene?.SetMouseInputConsumed();
                }
            }
        }

        private int HitTestButton(Point mouse)
        {
            if (ToScreen(RestoreButtonTarget).Contains(mouse)) return 0;
            if (ToScreen(ResurrectButtonTarget).Contains(mouse)) return 1;
            if (ToScreen(CloseButtonTarget).Contains(mouse)) return 2;
            return -1;
        }

        private Rectangle ToScreen(Rectangle local)
        {
            var origin = DisplayRectangle.Location;
            // Round edges, rather than independently rounding X/Y/width/height.
            // This keeps the 3-slice frame continuous at fractional UI scales.
            int left = origin.X + (int)MathF.Round(local.Left * Scale);
            int top = origin.Y + (int)MathF.Round(local.Top * Scale);
            int right = origin.X + (int)MathF.Round(local.Right * Scale);
            int bottom = origin.Y + (int)MathF.Round(local.Bottom * Scale);
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready)
            {
                return;
            }

            var sb = GraphicsManager.Instance?.Sprite;
            if (sb == null)
            {
                return;
            }

            // Character and Inventory use point sampling for packed GFx atlases.
            // Preserve the original header/footer/corners; never stretch the full
            // 633px atlas panel vertically or clip the bottom of button textures.
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                DrawCharacterFrame(sb);
                DrawNineSlice(sb, _characterAtlas, InformationSource, InformationTarget, 10);
                DrawButtonSprite(sb, 0, RestoreButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                DrawButtonSprite(sb, 1, ResurrectButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                DrawButtonSprite(sb, 2, CloseButtonTarget, _closeNormal, _closeHover, _closePressed, false);
            }

            var font = GraphicsManager.Instance.Font;
            if (font == null)
            {
                return;
            }

            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, GraphicsManager.GetQualityLinearSamplerState(),
                transform: UiScaler.SpriteTransform))
            {
                string characterName = MuGame.Network?.GetCharacterState()?.Name;
                string greeting = string.IsNullOrWhiteSpace(characterName) || characterName == "???"
                    ? "Hello!"
                    : $"Hello, {characterName}!";

                DrawCenteredText(sb, font, "Pet Trainer", 52, 12f, new Color(233, 219, 195));
                DrawCenteredText(sb, font, greeting, 124, 10f, new Color(215, 210, 200));
                DrawCenteredText(sb, font, "What is your command?", 148, 10f, new Color(191, 182, 166));
                DrawCenteredText(sb, font, "Restore Life", RestoreButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));
                DrawCenteredText(sb, font, "Resurrect Spirit", ResurrectButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));
            }
        }

        private void DrawSprite(SpriteBatch sb, Texture2D texture, Rectangle target, Rectangle? source)
        {
            if (texture != null && !texture.IsDisposed)
            {
                sb.Draw(texture, ToScreen(target), source, Color.White * Alpha);
            }
        }

        private void DrawCharacterFrame(SpriteBatch sb)
        {
            if (_characterAtlas == null || _characterAtlas.IsDisposed)
            {
                return;
            }

            int middleHeight = WindowHeight - FrameTopHeight - FrameBottomHeight;
            // Vertical three-slice: the complete original decorative title and
            // bottom corners survive, only the uninterrupted side-wall is scaled.
            DrawSprite(sb, _characterAtlas,
                new Rectangle(0, 0, WindowWidth, FrameTopHeight),
                new Rectangle(FrameSource.X, FrameSource.Y, FrameSource.Width, FrameTopHeight));

            DrawSprite(sb, _characterAtlas,
                new Rectangle(0, FrameTopHeight, WindowWidth, middleHeight),
                new Rectangle(FrameSource.X, FrameSource.Y + FrameTopHeight,
                    FrameSource.Width, FrameSource.Height - FrameTopHeight - FrameBottomHeight));

            DrawSprite(sb, _characterAtlas,
                new Rectangle(0, WindowHeight - FrameBottomHeight, WindowWidth, FrameBottomHeight),
                new Rectangle(FrameSource.X, FrameSource.Bottom - FrameBottomHeight,
                    FrameSource.Width, FrameBottomHeight));
        }

        private void DrawNineSlice(SpriteBatch sb, Texture2D atlas, Rectangle source,
            Rectangle destination, int border)
        {
            if (atlas == null || atlas.IsDisposed)
            {
                return;
            }

            // Identical corner-preserving approach to CharacterInfoWindowControl.
            // Crops contain only the verified Character information panel.
            for (int row = 0; row < 3; row++)
            {
                int srcY = row == 0 ? source.Y : row == 1 ? source.Y + border : source.Bottom - border;
                int dstY = row == 0 ? destination.Y : row == 1 ? destination.Y + border : destination.Bottom - border;
                int srcH = row == 1 ? source.Height - border * 2 : border;
                int dstH = row == 1 ? destination.Height - border * 2 : border;
                for (int col = 0; col < 3; col++)
                {
                    int srcX = col == 0 ? source.X : col == 1 ? source.X + border : source.Right - border;
                    int dstX = col == 0 ? destination.X : col == 1 ? destination.X + border : destination.Right - border;
                    int srcW = col == 1 ? source.Width - border * 2 : border;
                    int dstW = col == 1 ? destination.Width - border * 2 : border;
                    DrawSprite(sb, atlas, new Rectangle(dstX, dstY, dstW, dstH),
                        new Rectangle(srcX, srcY, srcW, srcH));
                }
            }
        }

        private void DrawThreeSliceButton(SpriteBatch sb, Texture2D texture, Rectangle destination)
        {
            if (texture == null || texture.IsDisposed)
            {
                return;
            }

            // btn_l01/02/03 are *individual* 128x64 OZD images. The old 128x46
            // source rectangle discarded the lower button border. Use the full
            // native height, and stretch only its flat horizontal center.
            int sourceCap = Math.Max(1, Math.Min(16, texture.Width / 4));
            int targetCap = Math.Max(1, (int)MathF.Round(sourceCap *
                (float)destination.Height / texture.Height));
            if (destination.Width <= targetCap * 2 || texture.Width <= sourceCap * 2)
            {
                DrawSprite(sb, texture, destination, null);
                return;
            }

            int centerWidth = destination.Width - targetCap * 2;
            int centerSourceWidth = texture.Width - sourceCap * 2;
            DrawSprite(sb, texture,
                new Rectangle(destination.Left, destination.Top, targetCap, destination.Height),
                new Rectangle(0, 0, sourceCap, texture.Height));
            DrawSprite(sb, texture,
                new Rectangle(destination.Left + targetCap, destination.Top, centerWidth, destination.Height),
                new Rectangle(sourceCap, 0, centerSourceWidth, texture.Height));
            DrawSprite(sb, texture,
                new Rectangle(destination.Right - targetCap, destination.Top, targetCap, destination.Height),
                new Rectangle(texture.Width - sourceCap, 0, sourceCap, texture.Height));
        }

        private void DrawButtonSprite(SpriteBatch sb, int index, Rectangle target,
            Texture2D normal, Texture2D hover, Texture2D pressed, bool threeSlice)
        {
            bool isPressed = _pressedButton == index && _hoveredButton == index;
            var image = isPressed ? pressed ?? hover ?? normal
                : _hoveredButton == index ? hover ?? normal : normal;
            if (threeSlice)
            {
                DrawThreeSliceButton(sb, image, target);
            }
            else
            {
                // Standalone close icon is already an isolated sprite.
                DrawSprite(sb, image, target, null);
            }
        }

        private void DrawCenteredText(SpriteBatch sb, SpriteFont font, string value,
            int centerY, float fontSize, Color color)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            float size = fontSize / Constants.BASE_FONT_SIZE * Scale;
            var dimensions = font.MeasureString(value) * size;
            float maxWidth = (WindowWidth - 52) * Scale;
            if (dimensions.X > maxWidth && dimensions.X > 0f)
            {
                size *= maxWidth / dimensions.X;
                dimensions = font.MeasureString(value) * size;
            }

            Vector2 center = new(
                DisplayRectangle.X + WindowWidth * 0.5f * Scale,
                DisplayRectangle.Y + centerY * Scale);
            var origin = center - dimensions * 0.5f;
            sb.DrawString(font, value, origin + new Vector2(1f, 1f), Color.Black * (0.70f * Alpha),
                0f, Vector2.Zero, size, SpriteEffects.None, 0f);
            sb.DrawString(font, value, origin, color * Alpha,
                0f, Vector2.Zero, size, SpriteEffects.None, 0f);
        }
    }
}
