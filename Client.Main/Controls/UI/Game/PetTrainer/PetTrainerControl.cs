using Client.Main.Controls.UI.Game.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
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
        private const int WindowWidth = GameWindowMetrics.SecondaryWidth;
        private const int MainWindowHeight = 346;
        private const int RestoreWindowHeight = 442;
        private const byte HorseSlot = 8;
        private const byte RavenSlot = 1;
        private const int MaximumPetLife = 255;

        private enum TrainerPage
        {
            Menu,
            RestoreLife
        }

        private TrainerPage _page = TrainerPage.Menu;
        private int WindowHeight => _page == TrainerPage.RestoreLife ? RestoreWindowHeight : MainWindowHeight;

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
        // Verified CharacterInfoFrame right-hand panel. y=633 excluded a few
        // bottom border pixels: extend the source to y=638 as in the original atlas.
        // Preserve the title/footer and scale only the uninterrupted side-wall.
        private static readonly Rectangle FrameSource = new(656, 0, 354, 638);
        private static readonly Rectangle InformationSource = new(676, 645, 245, 56);
        private const int FrameTopHeight = 96;
        private const int FrameBottomHeight = 39;

        private static readonly Rectangle InformationTarget = new(27, 99, 296, 75);
        private static readonly Rectangle RestoreButtonTarget = new(57, 187, 236, 56);
        private static readonly Rectangle ResurrectButtonTarget = new(57, 249, 236, 56);

        // Restore Life: original Season 6 order, a status directly under each pet.
        private static readonly Rectangle RestoreInformationTarget = new(27, 99, 296, 68);
        private static readonly Rectangle HorseButtonTarget = new(57, 171, 236, 55);
        private static readonly Rectangle RavenButtonTarget = new(57, 266, 236, 55);
        private static readonly Rectangle BackButtonTarget = new(102, 376, 146, 44);
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
        private bool _repairPending;

        private readonly struct PetRecoveryState
        {
            public PetRecoveryState(bool canRestore, long cost, string message)
            {
                CanRestore = canRestore;
                Cost = cost;
                Message = message;
            }

            public bool CanRestore { get; }
            public long Cost { get; }
            public string Message { get; }
        }

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

            SetPage(TrainerPage.Menu);
            _repairPending = false;
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
            _repairPending = false;
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

        private void OpenResurrectSpirit()
        {
            // The NPC conversation remains open while the existing ChaosMix
            // inventory operates in Trainer mode. Do NOT send CloseNpcRequest
            // here: OpenMU requires OpenedNpc to validate recipes 13 and 14.
            var state = MuGame.Network?.GetCharacterState();
            var scene = MuGame.Instance?.ActiveScene as Client.Main.Scenes.GameScene;
            if (state == null || scene == null)
            {
                return;
            }

            state.ClearChaosMachineItems();
            HideWithoutServerRequest();

            var mixWindow = ChaosMixControl.Instance;
            scene.InventoryControl?.Show();
            mixWindow.ShowTrainer();
            mixWindow.BringToFront();
        }

        private void SetPage(TrainerPage page)
        {
            _page = page;
            ControlSize = new Point(WindowWidth, WindowHeight);
            ViewSize = ControlSize;
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
            _hoveredButton = -1;
            _pressedButton = -1;

            if (page == TrainerPage.RestoreLife)
            {
                // OpenMU A9 provides the *pet's* level; +level from item bytes is
                // not a substitute. Request only the equipped pet slots.
                _ = RequestEquippedPetInfoAsync();
            }
        }

        private async Task RequestEquippedPetInfoAsync()
        {
            try
            {
                var state = MuGame.Network?.GetCharacterState();
                var service = MuGame.Network?.GetCharacterService();
                if (state == null || service == null)
                {
                    return;
                }

                if (IsEquippedPet(state, RavenSlot, 5))
                {
                    await service.SendPetInfoRequestAsync(0, RavenSlot);
                }

                if (IsEquippedPet(state, HorseSlot, 4))
                {
                    await service.SendPetInfoRequestAsync(1, HorseSlot);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to request pet information for Restore Life.");
            }
        }

        private static bool IsEquippedPet(CharacterState state, byte slot, short number)
        {
            return state != null &&
                state.TryGetInventoryItem(slot, out var data) &&
                data != null &&
                ItemDatabase.TryGetItemGroupAndNumber(data, out var group, out var itemNumber) &&
                group == 13 && itemNumber == number;
        }

        private static long RoundOpenMuPrice(long value)
        {
            if (value >= 1000) return value / 100 * 100;
            if (value >= 100) return value / 10 * 10;
            return value;
        }

        private static long CalculateOpenMuPetRecoveryCost(byte petLevel, byte durability, bool isHorse)
        {
            // From OpenMU ItemPriceCalculator.CalculateRepairPrice, with NPC discount:
            // pet buying price = pet level * 2m (horse) or 1m (raven).
            // Trainable pets have 255 base durability in OpenMU Season Six.
            long basePrice = RoundOpenMuPrice(
                Math.Min((long)petLevel * (isHorse ? 2_000_000L : 1_000_000L),
                    400_000_000L));
            float root = (float)Math.Sqrt(basePrice);
            float fourthRoot = (float)Math.Sqrt(root);
            float missingLife = 1f - (float)durability / MaximumPetLife;
            float cost = 3f * root * fourthRoot * missingLife + 1f;
            if (durability == 0)
            {
                cost *= 2.0f; // OpenMU DestroyedPetPenalty
            }

            return RoundOpenMuPrice((long)cost);
        }

        private PetRecoveryState GetPetRecoveryState(byte slot, short number)
        {
            var state = MuGame.Network?.GetCharacterState();
            if (!IsEquippedPet(state, slot, number))
            {
                return new PetRecoveryState(false, 0, "Pet is not equipped.");
            }

            if (!state.TryGetPetInfo(slot, out var petInfo) ||
                petInfo.PetType != (number == 5 ? (byte)0 : (byte)1))
            {
                return new PetRecoveryState(false, 0, "Checking pet information...");
            }

            if (petInfo.Health >= MaximumPetLife)
            {
                return new PetRecoveryState(false, 0, "Life has already been recovered.");
            }

            long cost = CalculateOpenMuPetRecoveryCost(petInfo.Level, petInfo.Health, number == 4);
            if (state.InventoryZen < cost)
            {
                long missingZen = cost - state.InventoryZen;
                return new PetRecoveryState(false, cost,
                    $"{missingZen.ToString("N0", CultureInfo.InvariantCulture)} Zen lacking to restore life.");
            }

            return new PetRecoveryState(true, cost,
                $"{cost.ToString("N0", CultureInfo.InvariantCulture)} Zen required to restore life.");
        }

        private async Task RestoreEquippedPetAsync(byte slot, short number)
        {
            if (_repairPending || _page != TrainerPage.RestoreLife)
            {
                return;
            }

            // Recheck right before sending. The server validates money and
            // durability again and sends authoritative inventory/Zen updates.
            var pet = GetPetRecoveryState(slot, number);
            if (!pet.CanRestore)
            {
                return;
            }

            var service = MuGame.Network?.GetCharacterService();
            if (service == null)
            {
                return;
            }

            _repairPending = true;
            try
            {
                await service.SendRepairItemRequestAsync(slot, isSelfRepair: false);
                // Preserve order: the repair request must reach the server before
                // closing the NPC dialog; otherwise pet-slot repair is rejected.
                MuGame.ScheduleOnMainThread(CloseWindow);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send Pet Trainer repair request.");
                MuGame.ScheduleOnMainThread(() => _repairPending = false);
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
                    if (_page == TrainerPage.Menu)
                    {
                        switch (action)
                        {
                            case 0:
                                SetPage(TrainerPage.RestoreLife);
                                RestoreLifeRequested?.Invoke();
                                break;
                            case 1:
                                OpenResurrectSpirit();
                                ResurrectSpiritRequested?.Invoke();
                                break;
                            case 2:
                                CloseWindow();
                                break;
                        }
                    }
                    else
                    {
                        switch (action)
                        {
                            case 0:
                                _ = RestoreEquippedPetAsync(HorseSlot, 4);
                                break;
                            case 1:
                                _ = RestoreEquippedPetAsync(RavenSlot, 5);
                                break;
                            case 2:
                                CloseWindow();
                                break;
                            case 3:
                                SetPage(TrainerPage.Menu);
                                break;
                        }
                    }

                    Scene?.SetMouseInputConsumed();
                }
            }
        }

        private int HitTestButton(Point mouse)
        {
            if (ToScreen(CloseButtonTarget).Contains(mouse)) return 2;
            if (_page == TrainerPage.RestoreLife)
            {
                if (ToScreen(HorseButtonTarget).Contains(mouse)) return 0;
                if (ToScreen(RavenButtonTarget).Contains(mouse)) return 1;
                if (ToScreen(BackButtonTarget).Contains(mouse)) return 3;
            }
            else
            {
                if (ToScreen(RestoreButtonTarget).Contains(mouse)) return 0;
                if (ToScreen(ResurrectButtonTarget).Contains(mouse)) return 1;
            }

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
                if (_page == TrainerPage.RestoreLife)
                {
                    DrawNineSlice(sb, _characterAtlas, InformationSource, RestoreInformationTarget, 10);
                    DrawButtonSprite(sb, 0, HorseButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                    DrawButtonSprite(sb, 1, RavenButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                    DrawButtonSprite(sb, 3, BackButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                }
                else
                {
                    DrawNineSlice(sb, _characterAtlas, InformationSource, InformationTarget, 10);
                    DrawButtonSprite(sb, 0, RestoreButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                    DrawButtonSprite(sb, 1, ResurrectButtonTarget, _buttonNormal, _buttonHover, _buttonPressed, true);
                }

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
                DrawCenteredText(sb, font, "Pet Trainer", 52, 12f, new Color(233, 219, 195));
                if (_page == TrainerPage.RestoreLife)
                {
                    DrawCenteredText(sb, font, "Select the pet to recover life.", 135, 10f, new Color(215, 210, 200));
                    DrawCenteredText(sb, font, "Dark Horse", HorseButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));
                    DrawCenteredText(sb, font, "Dark Raven", RavenButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));

                    var horse = GetPetRecoveryState(HorseSlot, 4);
                    var raven = GetPetRecoveryState(RavenSlot, 5);
                    var statusColor = new Color(206, 192, 146);
                    DrawCenteredText(sb, font, horse.Message, 240, 9f, statusColor);
                    DrawCenteredText(sb, font, raven.Message, 335, 9f, statusColor);
                    DrawCenteredText(sb, font, "Back", BackButtonTarget.Center.Y, 10f, new Color(226, 217, 202));
                }
                else
                {
                    string characterName = MuGame.Network?.GetCharacterState()?.Name;
                    string greeting = string.IsNullOrWhiteSpace(characterName) || characterName == "???"
                        ? "Hello!"
                        : $"Hello, {characterName}!";

                    DrawCenteredText(sb, font, greeting, 124, 10f, new Color(215, 210, 200));
                    DrawCenteredText(sb, font, "What is your command?", 148, 10f, new Color(191, 182, 166));
                    DrawCenteredText(sb, font, "Restore Life", RestoreButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));
                    DrawCenteredText(sb, font, "Resurrect Spirit", ResurrectButtonTarget.Center.Y, 10.5f, new Color(226, 217, 202));
                }
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
