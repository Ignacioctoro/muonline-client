using Client.Main.Controls.UI.Game.Common;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Networking;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Helpers;

namespace Client.Main.Controls.UI.Game.Character
{
    public class CharacterInfoWindowControl : UIControl, IUiTexturePreloadable
    {
        private const int WINDOW_WIDTH = GameWindowMetrics.SecondaryWidth;
        private const int WINDOW_HEIGHT = GameWindowMetrics.SecondaryHeight;
        private const int BTN_STAT_COUNT = 5;

        private static readonly float[] s_statRowY =
        {
            196f,
            269f,
            347f,
            421f,
            495f
        };

        private static readonly string[] s_statShortNames = { "STR", "AGI", "STA", "ENE", "CMD" };

        // Regions reconstructed from the Data_Broyal 1024x1024 OZD preview.
        // These are atlas coordinates, NOT recovered GFx/Scaleform transforms.
        // Keep them centralized until the native movie's placement records are decoded.
        private const string CharacterAtlasPath = "Interface/GFx/CharacterInfoFrame_I1.ozd";
        private const string StatIncreaseButtonPath = "Interface/newui_chainfo_btn_level.tga";
        // Original GFx close-button states from Data_Broyal, not a cropped neighboring atlas sprite.
        private const string CloseNormalPath = "Interface/GFx/btn_close01.ozd";
        private const string CloseHoverPath = "Interface/GFx/btn_close02.ozd";
        private const string ClosePressedPath = "Interface/GFx/btn_close03.ozd";
        // Crop packed-sprite interior instead of including adjacent colored atlas gutters.
        // These are visual reconstructions, not original Scaleform placement records.
        // Include the complete outer corner ornament, not just the inner vertical rail.
        private static readonly Rectangle s_panelSource = new(656, 0, 354, 633);
        private static readonly Rectangle s_informationSource = new(676, 645, 245, 56);
        // Neutral charcoal box (fourth small box in I1), without the gold button ornament.
        private static readonly Rectangle s_attributeBandSource = new(728, 720, 114, 26);
        private static readonly Rectangle s_buttonSource = new(353, 725, 109, 22);
        // Only the X sprite; the old crop included the neighboring button's border.
        private static readonly Rectangle s_closeSource = new(991, 752, 21, 21);
        // Complete inner panel in I1, including the top edge missing from I24.
        private static readonly Rectangle s_insetSource = new(16, 76, 322, 551);


        private enum TextAlignment
        {
            Left,
            Center,
            Right
        }

        private sealed class CharacterInfoTextEntry
        {
            public CharacterInfoTextEntry(Vector2 basePosition, float fontScale, Color color, TextAlignment alignment)
            {
                BasePosition = basePosition;
                FontScale = fontScale;
                Color = color;
                Alignment = alignment;
            }

            public Vector2 BasePosition { get; set; }
            public string Text { get; set; } = string.Empty;
            public Color Color { get; set; }
            public float FontScale { get; set; }
            public TextAlignment Alignment { get; set; }
            public bool Visible { get; set; } = true;
            public float MaxWidth { get; set; } = float.MaxValue;
        }

        private sealed class CharacterInfoButton
        {
            public CharacterInfoButton(Rectangle baseBounds, Texture2D texture, Rectangle? normalRect, Rectangle? hoverRect, Rectangle? pressedRect, Action onClick, string label)
            {
                BaseBounds = baseBounds;
                Texture = texture;
                NormalRect = normalRect;
                HoverRect = hoverRect;
                PressedRect = pressedRect;
                FrameSize = baseBounds.Size;
                OnClick = onClick;
                Label = label;
            }

            public Rectangle BaseBounds { get; set; }
            public Texture2D Texture { get; }
            public Rectangle? NormalRect { get; }
            public Rectangle? HoverRect { get; }
            public Rectangle? PressedRect { get; }
            public Point FrameSize { get; }
            public Action OnClick { get; }
            public string Label { get; }
            public bool Visible { get; set; } = true;
            public bool Enabled { get; set; } = true;
        }

        private Texture2D _characterAtlas;
        private Texture2D _statIncreaseButtonTexture;
        private Texture2D _closeNormalTexture;
        private Texture2D _closeHoverTexture;
        private Texture2D _closePressedTexture;

        // OZD stores straight alpha. Convert RGB while preserving alpha in the cached surface.
        private readonly BlendState _atlasCacheBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };


        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private readonly List<CharacterInfoTextEntry> _texts = new();
        private readonly List<CharacterInfoButton> _buttons = new();

        private CharacterInfoTextEntry _nameText;
        private CharacterInfoTextEntry _levelText;
        private CharacterInfoTextEntry _resetsText;
        private CharacterInfoTextEntry _informationClassText;
        private CharacterInfoTextEntry _serverText;
        private CharacterInfoTextEntry _statPointsText;
        private readonly CharacterInfoTextEntry[] _statValueTexts = new CharacterInfoTextEntry[BTN_STAT_COUNT];
        private CharacterInfoTextEntry _strDetail1Text;
        private CharacterInfoTextEntry _strDetail2Text;
        private CharacterInfoTextEntry _agiDetail1Text;
        private CharacterInfoTextEntry _agiDetail2Text;
        private CharacterInfoTextEntry _agiDetail3Text;
        private CharacterInfoTextEntry _vitDetail1Text;
        private CharacterInfoTextEntry _vitDetail2Text;
        private CharacterInfoTextEntry _eneDetail1Text;
        private CharacterInfoTextEntry _eneDetail2Text;
        private CharacterInfoTextEntry _eneDetail3Text;
        private CharacterInfoTextEntry _pvmInfo1Text;
        private CharacterInfoTextEntry _pvmInfo2Text;
        private readonly CharacterInfoButton[] _statButtons = new CharacterInfoButton[BTN_STAT_COUNT];

        private CharacterInfoButton _exitButton;
        private CharacterInfoButton _skillsButton;
        private CharacterInfoButton _petButton;
        private CharacterInfoButton _masterButton;

        private int _hoveredButtonIndex = -1;
        private int _pressedButtonIndex = -1;
        private int _clickFlashButtonIndex = -1;
        private double _clickFlashSeconds;
        private bool _isDragging;
        private Point _dragOffset;

        private CharacterState _characterState;
        private NetworkManager _networkManager;
        private readonly ILogger<CharacterInfoWindowControl> _logger;

        private CharacterInfoSnapshot _cachedSnapshot;
        private bool _hasCachedSnapshot;
        private bool _lastIsDarkLordFamily;
        private ushort? _displayServerId;

        public CharacterInfoWindowControl()
        {
            _logger = MuGame.AppLoggerFactory.CreateLogger<CharacterInfoWindowControl>();
            ControlSize = new Point(WINDOW_WIDTH, WINDOW_HEIGHT);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;

            _networkManager = MuGame.Network;
            if (_networkManager == null)
            {
                _logger.LogWarning("NetworkManager is null in CharacterInfoWindowControl constructor. Stat increase functionality may not work.");
            }
            else
            {
                // Subscribe to attack speed changes to refresh UI
                var characterState = _networkManager.GetCharacterState();
                if (characterState != null)
                {
                    characterState.AttackSpeedsChanged += OnAttackSpeedsChanged;
                }
            }
        }

        public IEnumerable<string> GetPreloadTexturePaths()
            => new[] { CharacterAtlasPath, StatIncreaseButtonPath, CloseNormalPath, CloseHoverPath, ClosePressedPath };

        public override async Task Load()
        {
            await base.Load();
            var textures = await Task.WhenAll(
                TextureLoader.Instance.PrepareAndGetTexture(CharacterAtlasPath),
                TextureLoader.Instance.PrepareAndGetTexture(StatIncreaseButtonPath),
                TextureLoader.Instance.PrepareAndGetTexture(CloseNormalPath),
                TextureLoader.Instance.PrepareAndGetTexture(CloseHoverPath),
                TextureLoader.Instance.PrepareAndGetTexture(ClosePressedPath));
            _characterAtlas = textures[0];
            _statIncreaseButtonTexture = textures[1];
            _closeNormalTexture = textures[2];
            _closeHoverTexture = textures[3];
            _closePressedTexture = textures[4];
            if (_characterAtlas == null)
            {
                _logger.LogWarning("Character GFx atlas could not be loaded from Data. Check CharacterInfoFrame_I1.ozd.");
            }
            InitializeLayout();
            InvalidateStaticSurface();
            UpdateDisplayData();
        }


        private void InitializeLayout()
        {
            _texts.Clear();
            _buttons.Clear();

            // CharacterInfoFrame's right-hand panel is composed at 1:1 atlas scale.
            // All placements below are BroyalMU reconstruction coordinates, not movie coordinates.
            _nameText = CreateText(new Vector2(WINDOW_WIDTH / 2f, 39f), 13f, Color.White, TextAlignment.Center);
            _nameText.MaxWidth = 240f;
            string[] informationLabels = { "Class", "Resets", "Level", "Points", "Server" };
            for (int i = 0; i < informationLabels.Length; i++)
            {
                CreateText(new Vector2(29f, 84f + i * 18f), 10f, new Color(177, 172, 160)).Text = informationLabels[i];
            }
            _informationClassText = CreateInformationValue(84f);
            _resetsText = CreateInformationValue(102f);
            _resetsText.Text = "--";
            _levelText = CreateInformationValue(120f);
            _statPointsText = CreateInformationValue(138f);
            _statPointsText.Color = new Color(220, 204, 161);
            _serverText = CreateInformationValue(156f);
            _serverText.Text = "--";
            _displayServerId = null;

            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                _statValueTexts[i] = CreateText(new Vector2(280f, s_statRowY[i] - 1f), 11f,
                    new Color(226, 224, 216), TextAlignment.Right);
                _statValueTexts[i].MaxWidth = 180f;
            }

            // A fixed-height row accommodates the native stat and all existing combat detail lines.
            _strDetail1Text = CreateText(new Vector2(29f, s_statRowY[0] + 24f), 10f, Color.LightGray);
            _strDetail2Text = CreateText(new Vector2(29f, s_statRowY[0] + 40f), 10f, Color.LightGray);
            _agiDetail1Text = CreateText(new Vector2(29f, s_statRowY[1] + 24f), 10f, Color.LightGray);
            _agiDetail2Text = CreateText(new Vector2(29f, s_statRowY[1] + 39f), 10f, Color.LightGray);
            _agiDetail3Text = CreateText(new Vector2(29f, s_statRowY[1] + 54f), 10f, Color.LightGray);
            _vitDetail1Text = CreateText(new Vector2(29f, s_statRowY[2] + 24f), 10f, Color.LightGray);
            _vitDetail2Text = CreateText(new Vector2(29f, s_statRowY[2] + 40f), 10f, Color.LightGray);
            _eneDetail1Text = CreateText(new Vector2(29f, s_statRowY[3] + 24f), 10f, Color.LightGray);
            _eneDetail2Text = CreateText(new Vector2(29f, s_statRowY[3] + 39f), 10f, Color.LightGray);
            _eneDetail3Text = CreateText(new Vector2(29f, s_statRowY[3] + 54f), 10f, Color.LightGray);
            _pvmInfo1Text = CreateText(new Vector2(29f, 530f), 10f, new Color(225, 197, 148));
            _pvmInfo2Text = CreateText(new Vector2(29f, 546f), 10f, new Color(225, 197, 148));
            _pvmInfo1Text.MaxWidth = _pvmInfo2Text.MaxWidth = 286f;

            // Reuse native S6 single-point buttons; the numeric allocation UI is phase 2.
            int frames = GetTextureFrameCount(_statIncreaseButtonTexture);
            int frameHeight = _statIncreaseButtonTexture != null
                ? Math.Max(1, _statIncreaseButtonTexture.Height / frames) : 0;
            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                int statIndex = i;
                var normal = _statIncreaseButtonTexture != null
                    ? new Rectangle(0, 0, _statIncreaseButtonTexture.Width, frameHeight)
                    : s_buttonSource;
                var hover = frames > 1
                    ? new Rectangle(0, frameHeight, _statIncreaseButtonTexture.Width, frameHeight)
                    : normal;
                var pressed = frames > 2
                    ? new Rectangle(0, frameHeight * 2, _statIncreaseButtonTexture.Width, frameHeight)
                    : hover;
                _statButtons[i] = CreateButton(
                    new Rectangle(294, (int)s_statRowY[i] - 5, 26, 24),
                    _statIncreaseButtonTexture ?? _characterAtlas,
                    normal, hover, pressed,
                    () => OnStatButtonClicked(statIndex),
                    _statIncreaseButtonTexture == null ? "+" : null);
            }

            // Season 6 compatibility buttons: visual feedback only, no nonexistent windows.
            Rectangle buttonNormal = s_attributeBandSource;
            _petButton = CreateButton(new Rectangle(23, 588, 94, 30), _characterAtlas,
                buttonNormal, buttonNormal, buttonNormal,
                () => SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav"), "PETS");
            _skillsButton = CreateButton(new Rectangle(127, 588, 94, 30), _characterAtlas,
                buttonNormal, buttonNormal, buttonNormal,
                () => SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav"), "SKILLS");
            _masterButton = CreateButton(new Rectangle(231, 588, 94, 30), _characterAtlas,
                buttonNormal, buttonNormal, buttonNormal,
                () => SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav"), "MASTER");

            // Keep the native GFx X compact and slightly inset/lower inside the title plaque.
            _exitButton = CreateButton(new Rectangle(303, 44, 20, 20), _characterAtlas,
                s_closeSource, s_closeSource, s_closeSource, () =>
                {
                    HideWindow();
                    SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
                }, null);
        }

        private CharacterInfoTextEntry CreateText(Vector2 basePosition, float fontSize, Color color, TextAlignment alignment = TextAlignment.Left)
        {
            var entry = new CharacterInfoTextEntry(basePosition, fontSize / Constants.BASE_FONT_SIZE, color, alignment);
            _texts.Add(entry);
            return entry;
        }

        private CharacterInfoTextEntry CreateInformationValue(float y)
        {
            var entry = CreateText(new Vector2(316f, y), 10f, new Color(222, 220, 213), TextAlignment.Right);
            entry.MaxWidth = 218f;
            return entry;
        }

        private CharacterInfoButton CreateButton(Rectangle bounds, Texture2D atlas, Rectangle normal,
            Rectangle hover, Rectangle pressed, Action onClick, string label)
        {
            var button = new CharacterInfoButton(bounds, atlas, normal, hover, pressed, onClick, label);
            _buttons.Add(button);
            return button;
        }


        private static int GetTextureFrameCount(Texture2D texture)
        {
            if (texture == null) return 1;
            if (texture.Height % 3 == 0) return 3;
            if (texture.Height % 2 == 0) return 2;
            return 1;
        }

        private void InvalidateStaticSurface()
        {
            _staticSurfaceDirty = true;
        }

        private void EnsureStaticSurface()
        {
            if (!_staticSurfaceDirty && _staticSurface != null && !_staticSurface.IsDisposed)
            {
                return;
            }

            var graphicsDevice = GraphicsManager.Instance.GraphicsDevice;
            if (graphicsDevice == null)
            {
                return;
            }

            _staticSurface?.Dispose();
            _staticSurface = new RenderTarget2D(graphicsDevice, WINDOW_WIDTH, WINDOW_HEIGHT, false, SurfaceFormat.Color, DepthFormat.None);

            var previousTargets = graphicsDevice.GetRenderTargets();
            graphicsDevice.SetRenderTarget(_staticSurface);
            graphicsDevice.Clear(Color.Transparent);

            var spriteBatch = GraphicsManager.Instance.Sprite;
            // Point sampling prevents colors from neighboring packed atlas sprites bleeding in.
            using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred,
                _atlasCacheBlend, SamplerState.PointClamp))
            {
                DrawStaticElements(spriteBatch);
            }
            using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.PointClamp))
            {
                DrawStaticLabels(spriteBatch);
            }

            graphicsDevice.SetRenderTargets(previousTargets);
            _staticSurfaceDirty = false;
        }

        private void DrawStaticElements(SpriteBatch spriteBatch)
        {
            if (_characterAtlas == null)
            {
                return;
            }

            // Authentic GFx OZD sprites, drawn by the existing MonoGame SpriteBatch.
            spriteBatch.Draw(_characterAtlas, new Rectangle(0, 0, WINDOW_WIDTH, 638), s_panelSource, Color.White);

            DrawAtlasNineSlice(spriteBatch, _characterAtlas, s_insetSource, new Rectangle(22, 180, 304, 394), 6);

            spriteBatch.Draw(_characterAtlas, new Rectangle(16, 77, 316, 97), s_informationSource, Color.White);

            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                if (i == 4 && !_lastIsDarkLordFamily)
                {
                    break;
                }

                var band = new Rectangle(28, (int)s_statRowY[i] - 7, 292, 27);
                DrawAtlasThreeSlice(spriteBatch, _characterAtlas, s_attributeBandSource, band, 10);
            }
        }

        private void DrawStaticLabels(SpriteBatch spriteBatch)
        {
            var font = GraphicsManager.Instance.Font;
            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                if (i == 4 && !_lastIsDarkLordFamily) break;
                if (font != null)
                {
                    spriteBatch.DrawString(font, s_statShortNames[i],
                        new Vector2(34f, s_statRowY[i] - 1f), new Color(205, 198, 180),
                        0f, Vector2.Zero, 11f / Constants.BASE_FONT_SIZE, SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawAtlasNineSlice(SpriteBatch batch, Texture2D atlas,
            Rectangle source, Rectangle target, int border)
        {
            // Keep all four corners and border thickness intact; stretch only the interior.
            for (int row = 0; row < 3; row++)
            {
                int sy = row == 0 ? source.Y : row == 1 ? source.Y + border : source.Bottom - border;
                int dy = row == 0 ? target.Y : row == 1 ? target.Y + border : target.Bottom - border;
                int sh = row == 1 ? source.Height - border * 2 : border;
                int dh = row == 1 ? target.Height - border * 2 : border;
                for (int column = 0; column < 3; column++)
                {
                    int sx = column == 0 ? source.X : column == 1 ? source.X + border : source.Right - border;
                    int dx = column == 0 ? target.X : column == 1 ? target.X + border : target.Right - border;
                    int sw = column == 1 ? source.Width - border * 2 : border;
                    int dw = column == 1 ? target.Width - border * 2 : border;
                    batch.Draw(atlas, new Rectangle(dx, dy, dw, dh), new Rectangle(sx, sy, sw, sh), Color.White);
                }
            }
        }

        // The left/right ornaments keep their unscaled width. Only the middle segment stretches.
        private static void DrawAtlasThreeSlice(SpriteBatch batch, Texture2D atlas,
            Rectangle source, Rectangle target, int capWidth)
        {
            int srcMiddle = source.Width - capWidth * 2;
            int dstMiddle = target.Width - capWidth * 2;
            if (srcMiddle <= 0 || dstMiddle <= 0)
            {
                return;
            }
            batch.Draw(atlas, new Rectangle(target.X, target.Y, capWidth, target.Height),
                new Rectangle(source.X, source.Y, capWidth, source.Height), Color.White);
            batch.Draw(atlas, new Rectangle(target.X + capWidth, target.Y, dstMiddle, target.Height),
                new Rectangle(source.X + capWidth, source.Y, srcMiddle, source.Height), Color.White);
            batch.Draw(atlas, new Rectangle(target.Right - capWidth, target.Y, capWidth, target.Height),
                new Rectangle(source.Right - capWidth, source.Y, capWidth, source.Height), Color.White);
        }


        public override void Update(GameTime gameTime)
        {
            if (!Visible)
            {
                _isDragging = false;
                return;
            }

            base.Update(gameTime);

            _clickFlashSeconds = Math.Max(0d, _clickFlashSeconds - gameTime.ElapsedGameTime.TotalSeconds);
            if (!HandleWindowDrag())
            {
                HandleButtonInput();
            }
            UpdateDisplayData();
        }

        private bool HandleWindowDrag()
        {
            var mouse = MuGame.Instance.UiMouseState;
            bool pressed = mouse.LeftButton == ButtonState.Pressed;
            if (_isDragging)
            {
                Scene?.SetMouseInputConsumed();
                if (!pressed || !MuGame.Instance.IsActive)
                {
                    _isDragging = false;
                    return true;
                }

                // UI mouse coordinates and X/Y use the same virtual coordinate system,
                // as in InventoryControl. The saved offset prevents a jump on mouse-down.
                X = mouse.X - _dragOffset.X;
                Y = mouse.Y - _dragOffset.Y;
                return true;
            }

            bool justPressed = pressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
            if (!justPressed || !MuGame.Instance.IsActive)
            {
                return false;
            }

            var bounds = DisplayRectangle;
            var header = new Rectangle(bounds.X + (int)(16 * Scale), bounds.Y + (int)(29 * Scale),
                (int)(316 * Scale), (int)(42 * Scale));
            if (!header.Contains(mouse.Position) ||
                (_exitButton != null && GetButtonRectangle(_exitButton, Scale).Contains(mouse.Position)))
            {
                return false;
            }

            // Fresh hit-test: Scene.MouseControl uses the previous frame's hover flags.
            if (Scene != null)
            {
                for (int i = Scene.Controls.Count - 1; i >= 0; i--)
                {
                    var control = Scene.Controls[i];
                    if (control == this) break;
                    if (control.Visible && control.Interactive && control != Scene.World &&
                        control.DisplayRectangle.Contains(mouse.Position))
                    {
                        return false;
                    }
                }
            }

            _isDragging = true;
            _dragOffset = new Point(mouse.X - X, mouse.Y - Y);
            Align = ControlAlign.None;
            _pressedButtonIndex = _hoveredButtonIndex = -1;
            BringToFront();
            Scene?.SetMouseInputConsumed();
            return true;
        }

        private void HandleButtonInput()
        {
            var mouseState = MuGame.Instance.UiMouseState;
            var prevMouseState = MuGame.Instance.PrevUiMouseState;
            var mousePosition = mouseState.Position;

            bool leftPressed = mouseState.LeftButton == ButtonState.Pressed;
            bool leftJustPressed = leftPressed && prevMouseState.LeftButton == ButtonState.Released;
            bool leftJustReleased = mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed;

            _hoveredButtonIndex = -1;

            float controlScale = Scale;

            for (int i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                if (!button.Visible)
                {
                    continue;
                }

                Rectangle destRect = GetButtonRectangle(button, controlScale);
                bool hovered = destRect.Contains(mousePosition);

                if (hovered)
                {
                    _hoveredButtonIndex = i;
                    if (leftJustPressed)
                    {
                        _pressedButtonIndex = i;
                    }

                    if (leftJustReleased && _pressedButtonIndex == i && button.Enabled)
                    {
                        _clickFlashButtonIndex = i;
                        _clickFlashSeconds = 0.13d;
                        button.OnClick?.Invoke();
                    }
                }
            }

            if (!leftPressed)
            {
                _pressedButtonIndex = -1;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }

            var font = GraphicsManager.Instance.Font;
            if (font == null)
            {
                return;
            }

            EnsureStaticSurface();

            var spriteBatch = GraphicsManager.Instance.Sprite;
            SpriteBatchScope? scope = null;
            if (!SpriteBatchScope.BatchIsBegun)
            {
                scope = new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    GraphicsManager.GetQualityLinearSamplerState(), transform: UiScaler.SpriteTransform);
            }

            try
            {
                if (_staticSurface != null && !_staticSurface.IsDisposed)
                {
                    spriteBatch.Draw(_staticSurface, DisplayRectangle, Color.White * Alpha);
                }

                DrawTexts(spriteBatch, font);
                DrawButtons(spriteBatch);
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private void DrawTexts(SpriteBatch spriteBatch, SpriteFont font)
        {
            float controlScale = Scale;
            Vector2 basePosition = DisplayRectangle.Location.ToVector2();

            foreach (var entry in _texts)
            {
                if (!entry.Visible || string.IsNullOrEmpty(entry.Text))
                {
                    continue;
                }

                float textScale = entry.FontScale * controlScale;
                Vector2 pos = basePosition + entry.BasePosition * controlScale;
                float widthLimit = entry.Alignment switch
                {
                    TextAlignment.Right => entry.BasePosition.X - 24f,
                    TextAlignment.Center => WINDOW_WIDTH - 46f,
                    _ => WINDOW_WIDTH - entry.BasePosition.X - 24f
                };
                widthLimit = Math.Min(widthLimit, entry.MaxWidth);
                Vector2 size = font.MeasureString(entry.Text) * textScale;
                if (size.X > widthLimit * controlScale && size.X > 0f)
                {
                    textScale *= widthLimit * controlScale / size.X;
                    size = font.MeasureString(entry.Text) * textScale;
                }

                switch (entry.Alignment)
                {
                    case TextAlignment.Center:
                        pos.X -= size.X * 0.5f;
                        break;
                    case TextAlignment.Right:
                        pos.X -= size.X;
                        break;
                }

                spriteBatch.DrawString(font, entry.Text, pos, entry.Color * Alpha, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawButtons(SpriteBatch spriteBatch)
        {
            float controlScale = Scale;
            var font = GraphicsManager.Instance.Font;
            for (int i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                if (!button.Visible || button.Texture == null)
                {
                    continue;
                }

                bool pressed = button.Enabled && (_pressedButtonIndex == i ||
                    (_clickFlashButtonIndex == i && _clickFlashSeconds > 0d));
                bool hovered = button.Enabled && _hoveredButtonIndex == i;
                Rectangle target = GetButtonRectangle(button, controlScale);
                // The GFx close button has dedicated normal/hover/pressed OZD images.
                // Other Character buttons keep their existing atlas-based rendering.
                bool nativeClose = ReferenceEquals(button, _exitButton) && _closeNormalTexture != null;
                Texture2D buttonTexture = nativeClose
                    ? pressed ? (_closePressedTexture ?? _closeHoverTexture ?? _closeNormalTexture)
                        : hovered ? (_closeHoverTexture ?? _closeNormalTexture) : _closeNormalTexture
                    : button.Texture;
                Rectangle? source = nativeClose ? null
                    : pressed ? button.PressedRect : hovered ? button.HoverRect : button.NormalRect;
                if (pressed && !nativeClose)
                {
                    target.Offset(0, Math.Max(1, (int)MathF.Round(controlScale)));
                }

                var tint = nativeClose ? Color.White
                    : pressed ? new Color(203, 189, 161) : hovered ? new Color(255, 236, 192) : Color.White;
                using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied, SamplerState.PointClamp, transform: UiScaler.SpriteTransform))
                {
                    // Both OZD and the native TGA button use straight alpha.
                    spriteBatch.Draw(buttonTexture, target, source, new Color(tint.R, tint.G, tint.B, (byte)(255 * Alpha)));
                }

                if (font != null && !string.IsNullOrEmpty(button.Label))
                {
                    float textScale = (10f / Constants.BASE_FONT_SIZE) * controlScale;
                    Vector2 textSize = font.MeasureString(button.Label) * textScale;
                    var position = new Vector2(target.X + (target.Width - textSize.X) * 0.5f,
                        target.Y + (target.Height - textSize.Y) * 0.5f);
                    spriteBatch.DrawString(font, button.Label, position,
                        (hovered ? Color.White : new Color(213, 207, 191)) * Alpha,
                        0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
            }
        }


        private Rectangle GetButtonRectangle(CharacterInfoButton button, float controlScale)
        {
            var baseRect = button.BaseBounds;
            int x = DisplayRectangle.X + (int)MathF.Round(baseRect.X * controlScale);
            int y = DisplayRectangle.Y + (int)MathF.Round(baseRect.Y * controlScale);
            int width = (int)MathF.Round(button.FrameSize.X * controlScale);
            int height = (int)MathF.Round(button.FrameSize.Y * controlScale);
            return new Rectangle(x, y, width, height);
        }

        private void OnAttackSpeedsChanged()
        {
            // Attack speeds changed - invalidate cached snapshot to force UI refresh
            _hasCachedSnapshot = false;
        }

        private void UpdateDisplayData()
        {
            var currentState = _networkManager?.GetCharacterState();
            if (!ReferenceEquals(_characterState, currentState))
            {
                _characterState = currentState;
                _hasCachedSnapshot = false;
            }

            if (_characterState == null)
            {
                return;
            }

            var snapshot = CharacterInfoSnapshot.Create(_characterState);
            // Connection metadata is independent of the character snapshot cache.
            ushort? serverId = _networkManager?.SelectedServerId;
            if (serverId != _displayServerId)
            {
                _displayServerId = serverId;
                _serverText.Text = serverId?.ToString() ?? "--";
            }
            if (_hasCachedSnapshot && snapshot.Equals(_cachedSnapshot))
            {
                return;
            }

            ApplySnapshot(snapshot);
            _cachedSnapshot = snapshot;
            _hasCachedSnapshot = true;
        }

        private void ApplySnapshot(CharacterInfoSnapshot snapshot)
        {
            // Check if class family changed - need to redraw static surface
            bool isDarkLordFamily = snapshot.IsDarkLordFamily;
            if (isDarkLordFamily != _lastIsDarkLordFamily)
            {
                _lastIsDarkLordFamily = isDarkLordFamily;
                InvalidateStaticSurface();
            }

            _nameText.Text = snapshot.Name;

            _informationClassText.Text = CharacterClassDatabase.GetClassName(snapshot.Class);
            _resetsText.Text = snapshot.Resets?.ToString() ?? "--";
            _levelText.Text = snapshot.MasterLevel > 0
                ? $"{snapshot.Level} (Master {snapshot.MasterLevel})" : snapshot.Level.ToString();
            // These are the regular points consumed by the existing + buttons, including at master level.
            _statPointsText.Text = snapshot.LevelUpPoints.ToString();
            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                ushort baseValue = snapshot.GetBaseStat(i);
                ushort addedValue = snapshot.GetAddedStat(i);
                int total = baseValue + addedValue;
                _statValueTexts[i].Text = addedValue > 0 ? $"{baseValue}+{addedValue}" : total.ToString();
                _statValueTexts[i].Color = addedValue > 0 ? new Color(160, 190, 220) : new Color(226, 224, 216);
            }

            if (snapshot.HasPhysicalDamage)
            {
                _strDetail1Text.Visible = true;
                _strDetail1Text.Text = $"Attack Damage: {snapshot.PhysicalMin}~{snapshot.PhysicalMax}";
                _strDetail2Text.Visible = true;
                _strDetail2Text.Text = $"Attack Speed: +{snapshot.AttackSpeed}";
            }
            else
            {
                ushort totalStr = (ushort)(snapshot.BaseStrength + snapshot.AddedStrength);
                _strDetail1Text.Visible = true;
                _strDetail1Text.Text = $"Strength: {totalStr}";
                _strDetail2Text.Visible = false;
                _strDetail2Text.Text = string.Empty;
            }

            _agiDetail1Text.Visible = true;
            _agiDetail1Text.Text = $"Defense: +{snapshot.Defense}";
            _agiDetail2Text.Visible = true;
            _agiDetail2Text.Text = $"Attack Rate: {snapshot.PvPAttackRate}%";
            _agiDetail3Text.Visible = true;
            _agiDetail3Text.Text = $"Defense Rate: {snapshot.PvPDefenseRate}%";

            _vitDetail1Text.Visible = true;
            _vitDetail1Text.Text = $"HP: {snapshot.CurrentHealth}/{snapshot.MaxHealth}";
            _vitDetail2Text.Visible = true;
            _vitDetail2Text.Text = $"SD: {snapshot.CurrentShield}/{snapshot.MaxShield}";

            if (snapshot.HasMagicalDamage)
            {
                _eneDetail1Text.Visible = true;
                _eneDetail1Text.Text = $"Wizardry Damage: {snapshot.MagicalMin}~{snapshot.MagicalMax}";
                _eneDetail2Text.Visible = true;
                _eneDetail2Text.Text = $"Mana: {snapshot.CurrentMana}/{snapshot.MaxMana}";
                _eneDetail3Text.Visible = true;
                _eneDetail3Text.Text = $"AG: {snapshot.CurrentAbility}/{snapshot.MaxAbility}";
            }
            else
            {
                _eneDetail1Text.Visible = true;
                _eneDetail1Text.Text = $"Mana: {snapshot.CurrentMana}/{snapshot.MaxMana}";
                _eneDetail2Text.Visible = true;
                _eneDetail2Text.Text = $"AG: {snapshot.CurrentAbility}/{snapshot.MaxAbility}";
                _eneDetail3Text.Visible = false;
                _eneDetail3Text.Text = string.Empty;
            }

            _pvmInfo1Text.BasePosition = new Vector2(29f, isDarkLordFamily ? 537f : 510f);
            _pvmInfo2Text.BasePosition = new Vector2(29f, isDarkLordFamily ? 553f : 526f);
            _pvmInfo1Text.Visible = true;
            _pvmInfo1Text.Text = $"PvM Attack Success Rate: {snapshot.PvMAttackRate}%";
            _pvmInfo2Text.Visible = true;
            _pvmInfo2Text.Text = $"PvM Defense Success Rate: {snapshot.PvMDefenseRate}%";

            // Show/hide Leadership (CMD) stat value text based on character class
            _statValueTexts[4].Visible = isDarkLordFamily;

            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                if (_statButtons[i] == null) continue;
                bool available = snapshot.LevelUpPoints > 0 && (i != 4 || isDarkLordFamily);
                _statButtons[i].Visible = available;
                _statButtons[i].Enabled = available;
            }

            // All three are future Season 6 navigation shortcuts, not active windows.
            _exitButton.Visible = _exitButton.Enabled = true;
            _skillsButton.Visible = _skillsButton.Enabled = true;
            _petButton.Visible = _petButton.Enabled = true;
            _masterButton.Visible = _masterButton.Enabled = true;
        }

        // Phase 1 keeps the original single-point operation; no amount editor yet.
        private void OnStatButtonClicked(int statIndex)
        {
            if (_networkManager == null || !_networkManager.IsConnected ||
                _networkManager.CurrentState < ClientConnectionState.InGame)
                return;

            var state = _networkManager.GetCharacterState();
            if (state == null || state.LevelUpPoints == 0)
                return;

            if (statIndex == 4 && state.Class != CharacterClassNumber.DarkLord &&
                state.Class != CharacterClassNumber.LordEmperor)
                return;

            CharacterStatAttribute attribute = statIndex switch
            {
                0 => CharacterStatAttribute.Strength,
                1 => CharacterStatAttribute.Agility,
                2 => CharacterStatAttribute.Vitality,
                3 => CharacterStatAttribute.Energy,
                4 => CharacterStatAttribute.Leadership,
                _ => throw new ArgumentOutOfRangeException(nameof(statIndex)),
            };
            SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            var service = _networkManager.GetCharacterService();
            if (service != null)
            {
                _ = service.SendIncreaseCharacterStatPointRequestAsync(attribute);
            }
        }

        public void ShowWindow()
        {
            Visible = true;
            _isDragging = false;
            _pressedButtonIndex = -1;
            _hoveredButtonIndex = -1;
            _clickFlashButtonIndex = -1;
            _clickFlashSeconds = 0d;
            _hasCachedSnapshot = false;
            UpdateDisplayData();
            InvalidateStaticSurface();
            BringToFront();
            SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
            if (Scene != null)
            {
                Scene.FocusControl = this;
            }
        }

        public void HideWindow()
        {
            Visible = false;
            _isDragging = false;
            if (Scene != null && Scene.FocusControl == this)
            {
                Scene.FocusControl = null;
            }
        }

        public override void Dispose()
        {
            // Unsubscribe from attack speed changes
            if (_networkManager != null)
            {
                var characterState = _networkManager.GetCharacterState();
                if (characterState != null)
                {
                    characterState.AttackSpeedsChanged -= OnAttackSpeedsChanged;
                }
            }

            base.Dispose();
            _staticSurface?.Dispose();
            _staticSurface = null;
            _atlasCacheBlend.Dispose();
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            InvalidateStaticSurface();
        }

        private readonly struct CharacterInfoSnapshot : IEquatable<CharacterInfoSnapshot>
        {
            public CharacterInfoSnapshot(CharacterState state)
            {
                Name = state.Name ?? string.Empty;
                Class = state.Class;
                Level = state.Level;
                MasterLevel = state.MasterLevel;
                Experience = state.Experience;
                ExperienceForNextLevel = state.ExperienceForNextLevel;
                LevelUpPoints = state.LevelUpPoints;
                Resets = state.Resets;
                MasterLevelUpPoints = state.MasterLevelUpPoints;
                BaseStrength = state.Strength;
                AddedStrength = state.AddedStrength;
                BaseAgility = state.Agility;
                AddedAgility = state.AddedAgility;
                BaseVitality = state.Vitality;
                AddedVitality = state.AddedVitality;
                BaseEnergy = state.Energy;
                AddedEnergy = state.AddedEnergy;
                BaseLeadership = state.Leadership;
                AddedLeadership = state.AddedLeadership;
                CurrentHealth = state.CurrentHealth;
                MaxHealth = state.MaximumHealth;
                CurrentShield = state.CurrentShield;
                MaxShield = state.MaximumShield;
                CurrentMana = state.CurrentMana;
                MaxMana = state.MaximumMana;
                CurrentAbility = state.CurrentAbility;
                MaxAbility = state.MaximumAbility;

                var phys = GetPhysicalDamage(state);
                PhysicalMin = phys.min;
                PhysicalMax = phys.max;

                var magic = GetMagicalDamage(state);
                MagicalMin = magic.min;
                MagicalMax = magic.max;

                // Use AttackSpeed from CharacterState which includes equipment bonuses
                // (weapon speed, excellent options, wizard ring, etc.)
                AttackSpeed = state.AttackSpeed;

                // Use calculated values for other stats (server may not provide these)
                Defense = GetDefense(state);
                PvPAttackRate = GetPvPAttackRate(state);
                PvPDefenseRate = GetPvPDefenseRate(state);
                PvMAttackRate = GetPvMAttackRate(state);
                PvMDefenseRate = GetPvMDefenseRate(state);

                IsDarkLordFamily = state.Class == CharacterClassNumber.DarkLord || state.Class == CharacterClassNumber.LordEmperor;
                CanBeMaster = state.Class != CharacterClassNumber.DarkWizard;
            }

            public static CharacterInfoSnapshot Create(CharacterState state) => new(state);

            public readonly string Name;
            public readonly CharacterClassNumber Class;
            public readonly ushort Level;
            public readonly ushort MasterLevel;
            public readonly ulong Experience;
            public readonly ulong ExperienceForNextLevel;
            public readonly ushort LevelUpPoints;
            public readonly ushort? Resets;
            public readonly ushort MasterLevelUpPoints;
            public readonly ushort BaseStrength;
            public readonly ushort AddedStrength;
            public readonly ushort BaseAgility;
            public readonly ushort AddedAgility;
            public readonly ushort BaseVitality;
            public readonly ushort AddedVitality;
            public readonly ushort BaseEnergy;
            public readonly ushort AddedEnergy;
            public readonly ushort BaseLeadership;
            public readonly ushort AddedLeadership;
            public readonly uint CurrentHealth;
            public readonly uint MaxHealth;
            public readonly uint CurrentShield;
            public readonly uint MaxShield;
            public readonly uint CurrentMana;
            public readonly uint MaxMana;
            public readonly uint CurrentAbility;
            public readonly uint MaxAbility;
            public readonly int PhysicalMin;
            public readonly int PhysicalMax;
            public readonly int MagicalMin;
            public readonly int MagicalMax;
            public readonly int AttackSpeed;
            public readonly int Defense;
            public readonly int PvPAttackRate;
            public readonly int PvPDefenseRate;
            public readonly int PvMAttackRate;
            public readonly int PvMDefenseRate;
            public readonly bool IsDarkLordFamily;
            public readonly bool CanBeMaster;

            public bool HasPhysicalDamage => PhysicalMin > 0 || PhysicalMax > 0;
            public bool HasMagicalDamage => MagicalMin > 0 || MagicalMax > 0;

            public ushort GetBaseStat(int index) => index switch
            {
                0 => BaseStrength,
                1 => BaseAgility,
                2 => BaseVitality,
                3 => BaseEnergy,
                4 => BaseLeadership,
                _ => 0
            };

            public ushort GetAddedStat(int index) => index switch
            {
                0 => AddedStrength,
                1 => AddedAgility,
                2 => AddedVitality,
                3 => AddedEnergy,
                4 => AddedLeadership,
                _ => 0
            };

            public bool Equals(CharacterInfoSnapshot other)
            {
                return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                       Class == other.Class &&
                       Level == other.Level &&
                       MasterLevel == other.MasterLevel &&
                       Experience == other.Experience &&
                       ExperienceForNextLevel == other.ExperienceForNextLevel &&
                       LevelUpPoints == other.LevelUpPoints &&
                       Resets == other.Resets &&
                       MasterLevelUpPoints == other.MasterLevelUpPoints &&
                       BaseStrength == other.BaseStrength &&
                       AddedStrength == other.AddedStrength &&
                       BaseAgility == other.BaseAgility &&
                       AddedAgility == other.AddedAgility &&
                       BaseVitality == other.BaseVitality &&
                       AddedVitality == other.AddedVitality &&
                       BaseEnergy == other.BaseEnergy &&
                       AddedEnergy == other.AddedEnergy &&
                       BaseLeadership == other.BaseLeadership &&
                       AddedLeadership == other.AddedLeadership &&
                       CurrentHealth == other.CurrentHealth &&
                       MaxHealth == other.MaxHealth &&
                       CurrentShield == other.CurrentShield &&
                       MaxShield == other.MaxShield &&
                       CurrentMana == other.CurrentMana &&
                       MaxMana == other.MaxMana &&
                       CurrentAbility == other.CurrentAbility &&
                       MaxAbility == other.MaxAbility &&
                       PhysicalMin == other.PhysicalMin &&
                       PhysicalMax == other.PhysicalMax &&
                       MagicalMin == other.MagicalMin &&
                       MagicalMax == other.MagicalMax &&
                       AttackSpeed == other.AttackSpeed &&
                       Defense == other.Defense &&
                       PvPAttackRate == other.PvPAttackRate &&
                       PvPDefenseRate == other.PvPDefenseRate &&
                       PvMAttackRate == other.PvMAttackRate &&
                       PvMDefenseRate == other.PvMDefenseRate &&
                       IsDarkLordFamily == other.IsDarkLordFamily &&
                       CanBeMaster == other.CanBeMaster;
            }
        }

        private static (int min, int max) GetPhysicalDamage(CharacterState state)
        {
            if (state == null) return (0, 0);

            var str = state.TotalStrength;
            var agi = state.TotalAgility;
            var ene = state.TotalEnergy;

            return state.Class switch
            {
                CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster =>
                    (str / 6, str / 4),
                CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf =>
                    ((str + agi * 2) / 14, (str + agi * 2) / 8),
                CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster =>
                    ((str * 2 + ene) / 12, (str * 2 + ene) / 8),
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    ((str * 2 + ene) / 14, (str * 2 + ene) / 10),
                CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster =>
                    (str / 6 + state.TotalVitality / 10, str / 4 + state.TotalVitality / 8),
                _ => (0, 0)
            };
        }

        private static (int min, int max) GetMagicalDamage(CharacterState state)
        {
            if (state == null) return (0, 0);

            var ene = state.TotalEnergy;

            return state.Class switch
            {
                CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster =>
                    (ene / 9, ene / 4),
                CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster =>
                    (ene / 9, ene / 4),
                CharacterClassNumber.Summoner or CharacterClassNumber.BloodySummoner or CharacterClassNumber.DimensionMaster =>
                    (ene / 5, ene / 2),
                _ => (0, 0)
            };
        }

        private static int GetDefense(CharacterState state)
        {
            if (state == null) return 0;

            var agi = state.TotalAgility;

            return state.Class switch
            {
                CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster =>
                    agi / 3,
                CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster =>
                    agi / 4,
                CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf =>
                    agi / 10,
                CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster =>
                    agi / 5,
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    agi / 7,
                CharacterClassNumber.Summoner or CharacterClassNumber.BloodySummoner or CharacterClassNumber.DimensionMaster =>
                    agi / 4,
                CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster =>
                    agi / 7,
                _ => 0
            };
        }

        private static int GetPvPAttackRate(CharacterState state)
        {
            if (state == null) return 0;

            var lvl = state.Level;
            var agi = state.TotalAgility;

            return state.Class switch
            {
                CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster =>
                    lvl * 3 + (int)(agi * 4.5f),
                CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster =>
                    lvl * 3 + agi * 4,
                CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf =>
                    (int)(lvl * 3 + agi * 0.6f),
                CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster =>
                    (int)(lvl * 3 + agi * 3.5f),
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    lvl * 3 + agi * 4,
                _ => 0
            };
        }

        private static int GetPvPDefenseRate(CharacterState state)
        {
            if (state == null) return 0;

            var lvl = state.Level;
            var agi = state.TotalAgility;

            return state.Class switch
            {
                CharacterClassNumber.DarkKnight or CharacterClassNumber.BladeKnight or CharacterClassNumber.BladeMaster =>
                    lvl * 2 + agi / 2,
                CharacterClassNumber.DarkWizard or CharacterClassNumber.SoulMaster or CharacterClassNumber.GrandMaster =>
                    lvl * 2 + agi / 4,
                CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf =>
                    lvl * 2 + agi / 10,
                CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster =>
                    lvl * 2 + agi / 4,
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    lvl * 2 + agi / 2,
                _ => 0
            };
        }

        private static int GetPvMAttackRate(CharacterState state)
        {
            if (state == null) return 0;

            var lvl = state.Level;
            var agi = state.TotalAgility;
            var str = state.TotalStrength;
            var cmd = state.TotalLeadership;

            return state.Class switch
            {
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    (int)((lvl * 2 + agi) * 2.5f + str / 6 + cmd / 10),
                _ => (int)(lvl * 5 + agi * 1.5f + str / 4)
            };
        }

        private static int GetPvMDefenseRate(CharacterState state)
        {
            if (state == null) return 0;

            var agi = state.TotalAgility;

            return state.Class switch
            {
                CharacterClassNumber.FairyElf or CharacterClassNumber.MuseElf or CharacterClassNumber.HighElf =>
                    agi / 4,
                CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor =>
                    agi / 7,
                _ => agi / 3
            };
        }
    }
}
