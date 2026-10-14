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
        private const int WindowHeight = 360;

        private const string DialogueAtlasPath = "Interface/GFx/NpcDialogue_I1.ozd";
        private const string ButtonNormalPath = "Interface/GFx/btn_l01.ozd";
        private const string ButtonHoverPath = "Interface/GFx/btn_l02.ozd";
        private const string ButtonPressedPath = "Interface/GFx/btn_l03.ozd";
        private const string CloseNormalPath = "Interface/GFx/btn_close01.ozd";
        private const string CloseHoverPath = "Interface/GFx/btn_close02.ozd";
        private const string ClosePressedPath = "Interface/GFx/btn_close03.ozd";

        // Source rectangles from the 1024x512 NpcDialogue_I1 atlas preview.
        // Keep gutters and adjacent glyphs out of the source rectangles.
        private static readonly Rectangle PanelSource = new(1, 1, 309, 380);
        private static readonly Rectangle HeaderSource = new(656, 1, 321, 91);
        // Standalone GFx button sprites have transparent padding below the ornament.
        private static readonly Rectangle ButtonSource = new(0, 0, 128, 46);

        private static readonly Rectangle PanelTarget = new(0, 0, WindowWidth, WindowHeight);
        private static readonly Rectangle HeaderTarget = new(11, 9, 328, 94);
        private static readonly Rectangle RestoreButtonTarget = new(61, 188, 228, 52);
        private static readonly Rectangle ResurrectButtonTarget = new(61, 250, 228, 52);
        private static readonly Rectangle CloseButtonTarget = new(313, 18, 28, 28);

        private static PetTrainerControl _instance;

        private readonly ILogger<PetTrainerControl> _logger =
            MuGame.AppLoggerFactory?.CreateLogger<PetTrainerControl>();

        private Texture2D _dialogueAtlas;
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
            yield return DialogueAtlasPath;
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
                loader.PrepareAndGetTexture(DialogueAtlasPath),
                loader.PrepareAndGetTexture(ButtonNormalPath),
                loader.PrepareAndGetTexture(ButtonHoverPath),
                loader.PrepareAndGetTexture(ButtonPressedPath),
                loader.PrepareAndGetTexture(CloseNormalPath),
                loader.PrepareAndGetTexture(CloseHoverPath),
                loader.PrepareAndGetTexture(ClosePressedPath));

            _dialogueAtlas = textures[0];
            _buttonNormal = textures[1];
            _buttonHover = textures[2];
            _buttonPressed = textures[3];
            _closeNormal = textures[4];
            _closeHover = textures[5];
            _closePressed = textures[6];

            if (_dialogueAtlas == null || _buttonNormal == null)
            {
                _logger?.LogWarning("Pet Trainer GFx textures are missing from Data/Interface/GFx.");
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
            return new Rectangle(
                origin.X + (int)MathF.Round(local.X * Scale),
                origin.Y + (int)MathF.Round(local.Y * Scale),
                Math.Max(1, (int)MathF.Round(local.Width * Scale)),
                Math.Max(1, (int)MathF.Round(local.Height * Scale)));
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

            // GFx OZD sprites use straight alpha; keep the existing NewUI draw path.
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                DrawSprite(sb, _dialogueAtlas, PanelTarget, PanelSource);
                DrawSprite(sb, _dialogueAtlas, HeaderTarget, HeaderSource);
                DrawButtonSprite(sb, 0, RestoreButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, ButtonSource);
                DrawButtonSprite(sb, 1, ResurrectButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, ButtonSource);
                DrawButtonSprite(sb, 2, CloseButtonTarget, _closeNormal, _closeHover, _closePressed, null);
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

                DrawCenteredText(sb, font, "Pet Trainer", 55, 13f, new Color(233, 219, 195));
                DrawCenteredText(sb, font, greeting, 134, 10f, new Color(215, 210, 200));
                DrawCenteredText(sb, font, "What is your command?", 157, 10f, new Color(191, 182, 166));
                DrawCenteredText(sb, font, "Restore Life", RestoreButtonTarget.Center.Y, 11f, new Color(226, 217, 202));
                DrawCenteredText(sb, font, "Resurrect Spirit", ResurrectButtonTarget.Center.Y, 11f, new Color(226, 217, 202));
            }
        }

        private void DrawSprite(SpriteBatch sb, Texture2D texture, Rectangle target, Rectangle? source)
        {
            if (texture != null && !texture.IsDisposed)
            {
                sb.Draw(texture, ToScreen(target), source, Color.White * Alpha);
            }
        }

        private void DrawButtonSprite(SpriteBatch sb, int index, Rectangle target,
            Texture2D normal, Texture2D hover, Texture2D pressed, Rectangle? source)
        {
            bool isPressed = _pressedButton == index && _hoveredButton == index;
            var image = isPressed ? pressed ?? hover ?? normal
                : _hoveredButton == index ? hover ?? normal : normal;
            DrawSprite(sb, image, target, source);
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
