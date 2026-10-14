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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Client.Main.Helpers;

namespace Client.Main.Controls.UI.Game.Character
{
    public class CharacterInfoWindowControl : UIControl, IUiTexturePreloadable
    {
        private const int WINDOW_WIDTH = 300;
        private const int WINDOW_HEIGHT = 580;
        private const int STAT_BOX_WIDTH = 190;
        private const int STAT_BOX_HEIGHT = 21;
        private const int BTN_STAT_COUNT = 5;

        private static readonly float[] s_statRowY =
        {
            141f,
            205f,
            269f,
            333f,
            397f
        };

        private static readonly string[] s_statShortNames = { "STR", "AGI", "STA", "ENE", "CMD" };

        private static readonly string[] s_tableTexturePaths =
        {
            "Interface/newui_item_table01(L).tga",
            "Interface/newui_item_table01(R).tga",
            "Interface/newui_item_table02(L).tga",
            "Interface/newui_item_table02(R).tga",
            "Interface/newui_item_table03(Up).tga",
            "Interface/newui_item_table03(Dw).tga",
            "Interface/newui_item_table03(L).tga",
            "Interface/newui_item_table03(R).tga"
        };

        private static readonly string[] s_additionalPreloadTextures =
        {
            "Interface/newui_msgbox_back.jpg",
            "Interface/newui_item_back04.tga",
            "Interface/newui_item_back02-L.tga",
            "Interface/newui_item_back02-R.tga",
            "Interface/newui_item_back03.tga",
            "Interface/newui_cha_textbox02.tga",
            "Interface/newui_chainfo_btn_level.tga",
            "Interface/newui_exit_00.tga",
            "Interface/newui_chainfo_btn_quest.tga",
            "Interface/newui_chainfo_btn_pet.tga",
            "Interface/newui_chainfo_btn_master.tga"
        };

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
        }

        private sealed class CharacterInfoButton
        {
            public CharacterInfoButton(Rectangle baseBounds, Texture2D texture, Rectangle? normalRect, Rectangle? hoverRect, Rectangle? pressedRect, Action onClick)
            {
                BaseBounds = baseBounds;
                Texture = texture;
                NormalRect = normalRect;
                HoverRect = hoverRect;
                PressedRect = pressedRect;
                FrameSize = normalRect?.Size ?? baseBounds.Size;
                OnClick = onClick;
            }

            public Rectangle BaseBounds { get; set; }
            public Texture2D Texture { get; }
            public Rectangle? NormalRect { get; }
            public Rectangle? HoverRect { get; }
            public Rectangle? PressedRect { get; }
            public Point FrameSize { get; }
            public Action OnClick { get; }
            public bool Visible { get; set; } = true;
            public bool Enabled { get; set; } = true;
        }

        private Texture2D _backgroundTexture;
        private Texture2D _topFrameTexture;
        private Texture2D _leftFrameTexture;
        private Texture2D _rightFrameTexture;
        private Texture2D _bottomFrameTexture;
        private Texture2D _statTextboxTexture;
        private Texture2D _buttonExitTexture;
        private Texture2D _buttonQuestTexture;
        private Texture2D _buttonPetTexture;
        private Texture2D _buttonMasterTexture;
        private Texture2D _statIncreaseButtonTexture;
        private Texture2D _tableTopLeftTexture;
        private Texture2D _tableTopRightTexture;
        private Texture2D _tableBottomLeftTexture;
        private Texture2D _tableBottomRightTexture;
        private Texture2D _tableTopLineTexture;
        private Texture2D _tableBottomLineTexture;
        private Texture2D _tableLeftLineTexture;
        private Texture2D _tableRightLineTexture;

        private RenderTarget2D _staticSurface;
        private bool _staticSurfaceDirty = true;

        private readonly List<CharacterInfoTextEntry> _texts = new();
        private readonly List<CharacterInfoButton> _buttons = new();

        private CharacterInfoTextEntry _nameText;
        private CharacterInfoTextEntry _classText;
        private CharacterInfoTextEntry _levelText;
        private CharacterInfoTextEntry _expText;
        private CharacterInfoTextEntry _fruitProbText;
        private CharacterInfoTextEntry _fruitStatsText;
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
        private CharacterInfoButton _questButton;
        private CharacterInfoButton _petButton;
        private CharacterInfoButton _masterButton;

        private int _hoveredButtonIndex = -1;
        private int _pressedButtonIndex = -1;

        private CharacterState _characterState;
        private NetworkManager _networkManager;
        private readonly ILogger<CharacterInfoWindowControl> _logger;

        private CharacterInfoSnapshot _cachedSnapshot;
        private bool _hasCachedSnapshot;
        private bool _lastIsDarkLordFamily;
        private int _selectedStatIndex = 0;
        private CharacterInfoTextEntry _selectionText;
        private CharacterInfoTextEntry _allocationStatusText;
        private string _amountText = "1";
        private bool _amountFocused, _keypadOpen;
        private KeyboardState _previousAmountKeys;
        private static readonly Rectangle s_amountBounds = new(121, 483, 78, 29);
        private static readonly Rectangle s_confirmBounds = new(209, 483, 74, 29);
        private readonly ConcurrentQueue<(CharacterStatAttribute attr, bool success, ushort amount)> _replyQueue = new();
        private bool _statReplySubscribed, _awaitingResponse;
        private int _remaining, _completed, _requested;
        private CharacterStatAttribute _allocationAttribute;
        private double _responseSeconds, _sendCooldown;

        public CharacterInfoWindowControl()
        {
            _logger = MuGame.AppLoggerFactory.CreateLogger<CharacterInfoWindowControl>();
            ControlSize = new Point(WINDOW_WIDTH, WINDOW_HEIGHT);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;

            _previousAmountKeys = Keyboard.GetState();
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
            => s_additionalPreloadTextures.Concat(s_tableTexturePaths);

        public override async Task Load()
        {
            await base.Load();

            var tl = TextureLoader.Instance;

            _backgroundTexture = await tl.PrepareAndGetTexture("Interface/newui_msgbox_back.jpg");
            _topFrameTexture = await tl.PrepareAndGetTexture("Interface/newui_item_back04.tga");
            _leftFrameTexture = await tl.PrepareAndGetTexture("Interface/newui_item_back02-L.tga");
            _rightFrameTexture = await tl.PrepareAndGetTexture("Interface/newui_item_back02-R.tga");
            _bottomFrameTexture = await tl.PrepareAndGetTexture("Interface/newui_item_back03.tga");
            _statTextboxTexture = await tl.PrepareAndGetTexture("Interface/newui_cha_textbox02.tga");

            _buttonExitTexture = await tl.PrepareAndGetTexture("Interface/newui_exit_00.tga");
            _buttonQuestTexture = await tl.PrepareAndGetTexture("Interface/newui_chainfo_btn_quest.tga");
            _buttonPetTexture = await tl.PrepareAndGetTexture("Interface/newui_chainfo_btn_pet.tga");
            _buttonMasterTexture = await tl.PrepareAndGetTexture("Interface/newui_chainfo_btn_master.tga");
            _statIncreaseButtonTexture = await tl.PrepareAndGetTexture("Interface/newui_chainfo_btn_level.tga");

            var tableTextures = await Task.WhenAll(s_tableTexturePaths.Select(path => tl.PrepareAndGetTexture(path)));
            _tableTopLeftTexture = tableTextures.ElementAtOrDefault(0);
            _tableTopRightTexture = tableTextures.ElementAtOrDefault(1);
            _tableBottomLeftTexture = tableTextures.ElementAtOrDefault(2);
            _tableBottomRightTexture = tableTextures.ElementAtOrDefault(3);
            _tableTopLineTexture = tableTextures.ElementAtOrDefault(4);
            _tableBottomLineTexture = tableTextures.ElementAtOrDefault(5);
            _tableLeftLineTexture = tableTextures.ElementAtOrDefault(6);
            _tableRightLineTexture = tableTextures.ElementAtOrDefault(7);

            InitializeLayout();
            InvalidateStaticSurface();
            UpdateDisplayData();
        }

        private void InitializeLayout()
        {
            _texts.Clear();
            _buttons.Clear();

            float statBoxLeft = GetStatBoxLeft();
            float statValueLeft = statBoxLeft + 70f;
            float statValueYoffset = -2f;
            float statDetailYOffset = 15f;

            _nameText = CreateText(new Vector2(WINDOW_WIDTH / 2f, 5f), 13f, Color.White, TextAlignment.Center);
            _classText = CreateText(new Vector2(WINDOW_WIDTH / 2f, 23f), 12f, Color.LightGray, TextAlignment.Center);
            _levelText = CreateText(new Vector2(28f, 60f), 11f, new Color(230, 230, 0));
            _expText = CreateText(new Vector2(28f, 78f), 10f, Color.WhiteSmoke);
            _fruitProbText = CreateText(new Vector2(28f, 96f), 10f, new Color(76, 197, 254));
            _fruitStatsText = CreateText(new Vector2(28f, 114f), 10f, new Color(76, 197, 254));
            _statPointsText = CreateText(new Vector2(169f, 60f), 11f, new Color(255, 165, 66));
            _selectionText = CreateText(new Vector2(29f, 489f), 10f, new Color(228, 177, 109));
            _allocationStatusText = CreateText(new Vector2(27f, 519f), 9f, Color.LightGray);

            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                float rowY = s_statRowY[i];
                _statValueTexts[i] = CreateText(new Vector2(statValueLeft, rowY + statValueYoffset), 11f, new Color(230, 230, 0), TextAlignment.Left);
            }

            _strDetail1Text = CreateText(new Vector2(25f, s_statRowY[0] + statDetailYOffset), 10f, Color.LightGray);
            _strDetail2Text = CreateText(new Vector2(25f, s_statRowY[0] + statDetailYOffset + 16f), 10f, Color.LightGray);

            _agiDetail1Text = CreateText(new Vector2(25f, s_statRowY[1] + statDetailYOffset), 10f, Color.LightGray);
            _agiDetail2Text = CreateText(new Vector2(25f, s_statRowY[1] + statDetailYOffset + 16f), 10f, Color.LightGray);
            _agiDetail3Text = CreateText(new Vector2(25f, s_statRowY[1] + statDetailYOffset + 32f), 10f, Color.LightGray);

            _vitDetail1Text = CreateText(new Vector2(25f, s_statRowY[2] + statDetailYOffset), 10f, Color.LightGray);
            _vitDetail2Text = CreateText(new Vector2(25f, s_statRowY[2] + statDetailYOffset + 16f), 10f, Color.LightGray);

            _eneDetail1Text = CreateText(new Vector2(25f, s_statRowY[3] + statDetailYOffset), 10f, Color.LightGray);
            _eneDetail2Text = CreateText(new Vector2(25f, s_statRowY[3] + statDetailYOffset + 16f), 10f, Color.LightGray);
            _eneDetail3Text = CreateText(new Vector2(25f, s_statRowY[3] + statDetailYOffset + 32f), 10f, Color.LightGray);

            _pvmInfo1Text = CreateText(new Vector2(25f, s_statRowY[4] + 19f), 10f, Color.LightGray);
            _pvmInfo2Text = CreateText(new Vector2(25f, s_statRowY[4] + 36f), 10f, Color.LightGray);

            // Stat increase buttons
            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                int statIndex = i;
                var bounds = new Rectangle(
                    WINDOW_WIDTH - 61,
                    (int)(s_statRowY[i] - 2f),
                    16,
                    15);

                Rectangle? normal = null;
                Rectangle? hover = null;
                Rectangle? pressed = null;
                if (_statIncreaseButtonTexture != null)
                {
                    int frameCount = DetermineFrameCount(_statIncreaseButtonTexture);
                    int frameHeight = Math.Max(1, _statIncreaseButtonTexture.Height / frameCount);
                    normal = new Rectangle(0, 0, _statIncreaseButtonTexture.Width, frameHeight);
                    if (frameCount > 1)
                    {
                        hover = new Rectangle(0, frameHeight, _statIncreaseButtonTexture.Width, frameHeight);
                    }
                    if (frameCount > 2)
                    {
                        pressed = new Rectangle(0, frameHeight * 2, _statIncreaseButtonTexture.Width, frameHeight);
                    }
                    else
                    {
                        pressed = hover;
                    }
                    bounds.Width = _statIncreaseButtonTexture.Width;
                    bounds.Height = frameHeight;
                }

                _statButtons[i] = CreateButton(bounds, _statIncreaseButtonTexture, normal, hover, pressed, () => OnStatButtonClicked(statIndex));
            }

            // Bottom buttons
            _exitButton = CreateButton(CreateBottomBounds(20, _buttonExitTexture), _buttonExitTexture, CreateButtonFrames(_buttonExitTexture, out var exitHover, out var exitPressed), exitHover, exitPressed, () =>
            {
                HideWindow();
                SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            });

            _questButton = CreateButton(CreateBottomBounds(70, _buttonQuestTexture), _buttonQuestTexture, CreateButtonFrames(_buttonQuestTexture, out var questHover, out var questPressed), questHover, questPressed, () =>
            {
                MessageWindow.Show("Quest journal is not yet available.");
                SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            });

            _petButton = CreateButton(CreateBottomBounds(120, _buttonPetTexture), _buttonPetTexture, CreateButtonFrames(_buttonPetTexture, out var petHover, out var petPressed), petHover, petPressed, () =>
            {
                MessageWindow.Show("Pet information window is not yet available.");
                SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            });

            _masterButton = CreateButton(CreateBottomBounds(170, _buttonMasterTexture), _buttonMasterTexture, CreateButtonFrames(_buttonMasterTexture, out var masterHover, out var masterPressed), masterHover, masterPressed, () =>
            {
                MessageWindow.Show("Master skill tree is not yet available.");
                SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            });
        }

        private CharacterInfoTextEntry CreateText(Vector2 basePosition, float fontSize, Color color, TextAlignment alignment = TextAlignment.Left)
        {
            float fontScale = fontSize / Constants.BASE_FONT_SIZE;
            var entry = new CharacterInfoTextEntry(basePosition, fontScale, color, alignment);
            _texts.Add(entry);
            return entry;
        }

        private static Rectangle CreateBottomBounds(int xOffset, Texture2D texture)
        {
            if (texture == null)
            {
                return new Rectangle(xOffset, WINDOW_HEIGHT - 40, 36, 29);
            }

            int frameHeight = texture.Height / 2;
            if (frameHeight <= 0)
            {
                frameHeight = texture.Height;
            }

            return new Rectangle(xOffset, WINDOW_HEIGHT - frameHeight - 11, texture.Width, frameHeight);
        }

        private static Rectangle? CreateButtonFrames(Texture2D texture, out Rectangle? hover, out Rectangle? pressed)
        {
            hover = null;
            pressed = null;

            if (texture == null)
            {
                return null;
            }

            int frameCount = DetermineFrameCount(texture);
            int frameHeight = Math.Max(1, texture.Height / frameCount);

            if (frameHeight <= 0)
            {
                frameHeight = texture.Height;
            }

            var normal = new Rectangle(0, 0, texture.Width, frameHeight);

            if (texture.Height >= frameHeight * 2)
            {
                hover = new Rectangle(0, frameHeight, texture.Width, frameHeight);
            }

            if (texture.Height >= frameHeight * 3)
            {
                pressed = new Rectangle(0, frameHeight * 2, texture.Width, frameHeight);
            }
            else
            {
                pressed = hover;
            }

            return normal;
        }

        private static int DetermineFrameCount(Texture2D texture)
        {
            if (texture == null)
            {
                return 1;
            }

            if (texture.Height % 3 == 0)
            {
                return 3;
            }

            if (texture.Height % 2 == 0)
            {
                return 2;
            }

            return 1;
        }

        private CharacterInfoButton CreateButton(Rectangle baseBounds, Texture2D texture, Rectangle? normalRect, Rectangle? hoverRect, Rectangle? pressedRect, Action onClick)
        {
            if (normalRect.HasValue)
            {
                baseBounds.Width = normalRect.Value.Width;
                baseBounds.Height = normalRect.Value.Height;
            }

            var button = new CharacterInfoButton(baseBounds, texture, normalRect, hoverRect, pressedRect, onClick);
            _buttons.Add(button);
            return button;
        }

        private static float GetStatBoxLeft()
        {
            float value = (WINDOW_WIDTH - STAT_BOX_WIDTH) / 2f - 40f;
            return value < 10f ? 15f : value;
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
            using (new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend))
            {
                DrawStaticElements(spriteBatch);
            }

            graphicsDevice.SetRenderTargets(previousTargets);
            _staticSurfaceDirty = false;
        }

        private void DrawStaticElements(SpriteBatch spriteBatch)
        {
            if (_backgroundTexture != null)
            {
                spriteBatch.Draw(_backgroundTexture, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);
            }

            DrawModernPanelBackground(spriteBatch);

            if (_topFrameTexture != null)
            {
                spriteBatch.Draw(_topFrameTexture, new Rectangle(0, 0, WINDOW_WIDTH + 97, 74), Color.White);
            }

            if (_leftFrameTexture != null)
            {
                spriteBatch.Draw(_leftFrameTexture, new Rectangle(0, 0, 26, WINDOW_HEIGHT + 250), Color.White);
            }

            if (_rightFrameTexture != null)
            {
                spriteBatch.Draw(_rightFrameTexture, new Rectangle(WINDOW_WIDTH - 17, 0, 26, WINDOW_HEIGHT + 250), Color.White);
            }

            if (_bottomFrameTexture != null)
            {
                spriteBatch.Draw(_bottomFrameTexture, new Rectangle(0, WINDOW_HEIGHT - 40, WINDOW_WIDTH + 97, 55), Color.White);
            }

            if (_statTextboxTexture != null)
            {
                float statBoxLeft = GetStatBoxLeft();

                // Draw stat boxes - only show Leadership (5th stat) for Dark Lord family
                int statBoxCount = _lastIsDarkLordFamily ? BTN_STAT_COUNT : BTN_STAT_COUNT - 1;
                for (int i = 0; i < statBoxCount; i++)
                {
                    spriteBatch.Draw(_statTextboxTexture,
                        new Rectangle((int)statBoxLeft, (int)s_statRowY[i], STAT_BOX_WIDTH, STAT_BOX_HEIGHT),
                        Color.White);
                }

                // Draw stat short names
                var font = GraphicsManager.Instance.Font;
                if (font != null)
                {
                    float labelScale = 10f / Constants.BASE_FONT_SIZE;
                    float labelX = statBoxLeft + 8f;
                    for (int i = 0; i < statBoxCount; i++)
                    {
                        spriteBatch.DrawString(font,
                            s_statShortNames[i],
                            new Vector2(labelX, s_statRowY[i] - 1f),
                            new Color(230, 230, 0),
                            0f,
                            Vector2.Zero,
                            labelScale,
                            SpriteEffects.None,
                            0f);
                    }
                }
            }

            // Draw info table background
            DrawInfoTableSurface(spriteBatch, new Vector2(18f, 55f));
        }


        private void DrawModernPanelBackground(SpriteBatch b)
        {
            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;
            b.Draw(pixel, new Rectangle(10, 40, WINDOW_WIDTH - 20, WINDOW_HEIGHT - 81), new Color(12, 10, 14, 240));
            b.Draw(pixel, new Rectangle(18, 47, WINDOW_WIDTH - 36, 83), new Color(40, 19, 19, 200));
            b.Draw(pixel, new Rectangle(18, 47, WINDOW_WIDTH - 36, 2), new Color(166, 74, 54));
            int count = _lastIsDarkLordFamily ? 5 : 4;
            for (int i = 0; i < count; i++)
            {
                int y = (int)s_statRowY[i] - 3;
                b.Draw(pixel, new Rectangle(18, y, WINDOW_WIDTH - 36, 58),
                    i % 2 == 0 ? new Color(35, 28, 30, 220) : new Color(24, 20, 23, 220));
                b.Draw(pixel, new Rectangle(18, y, WINDOW_WIDTH - 36, 1), new Color(111, 74, 57));
            }
            b.Draw(pixel, new Rectangle(18, 462, WINDOW_WIDTH - 36, 72), new Color(33, 17, 20, 235));
            b.Draw(pixel, new Rectangle(18, 462, WINDOW_WIDTH - 36, 2), new Color(153, 76, 59));
        }

        private Rectangle ScreenRect(Rectangle local)
        {
            float s = Scale;
            return new Rectangle(DisplayRectangle.X + (int)MathF.Round(local.X * s),
                DisplayRectangle.Y + (int)MathF.Round(local.Y * s),
                Math.Max(1, (int)MathF.Round(local.Width * s)),
                Math.Max(1, (int)MathF.Round(local.Height * s)));
        }

        private void DrawPanelRectangle(SpriteBatch b, Rectangle r, Color fill, Color border)
        {
            var p = GraphicsManager.Instance.Pixel;
            if (p == null) return;
            b.Draw(p, ScreenRect(r), fill * Alpha);
            b.Draw(p, ScreenRect(new Rectangle(r.X, r.Y, r.Width, 1)), border * Alpha);
            b.Draw(p, ScreenRect(new Rectangle(r.X, r.Bottom - 1, r.Width, 1)), border * Alpha);
            b.Draw(p, ScreenRect(new Rectangle(r.X, r.Y, 1, r.Height)), border * Alpha);
            b.Draw(p, ScreenRect(new Rectangle(r.Right - 1, r.Y, 1, r.Height)), border * Alpha);
        }

        private void DrawPanelString(SpriteBatch b, SpriteFont font, string value, int x, int y, float px, Color color)
        {
            float s = Scale;
            b.DrawString(font, value, new Vector2(DisplayRectangle.X + x * s, DisplayRectangle.Y + y * s),
                color * Alpha, 0f, Vector2.Zero, s * px / Constants.BASE_FONT_SIZE, SpriteEffects.None, 0);
        }

        private void DrawAllocationUi(SpriteBatch b, SpriteFont font)
        {
            var p = GraphicsManager.Instance.Pixel;
            if (p != null && _selectedStatIndex >= 0 && (_selectedStatIndex < 4 || _lastIsDarkLordFamily))
                b.Draw(p, ScreenRect(new Rectangle(18, (int)s_statRowY[_selectedStatIndex] - 3, 3, 58)),
                    new Color(227, 160, 84) * Alpha);
            DrawPanelRectangle(b, s_amountBounds, new Color(8, 9, 12, 245),
                _amountFocused ? new Color(236, 170, 88) : new Color(117, 91, 68));
            DrawPanelRectangle(b, s_confirmBounds, new Color(107, 39, 35, 238),
                new Color(178, 105, 67));
            DrawPanelString(b, font, string.IsNullOrEmpty(_amountText) ? "0" : _amountText, 128, 490,
                11, Color.WhiteSmoke);
            DrawPanelString(b, font, _remaining > 0 ? "ENVIANDO" : "ASIGNAR", 215, 491,
                9, Color.WhiteSmoke);
            if (!_keypadOpen) return;

            DrawPanelRectangle(b, new Rectangle(89, 287, 137, 169), new Color(13, 12, 15, 253),
                new Color(187, 119, 73));
            DrawPanelString(b, font, "CANTIDAD", 113, 292, 10, new Color(235, 187, 105));
            string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
            for (int i = 0; i < labels.Length; i++)
            {
                var box = new Rectangle(97 + i % 3 * 41, 314 + i / 3 * 34, 37, 29);
                DrawPanelRectangle(b, box, new Color(56, 42, 43, 250), new Color(128, 84, 64));
                DrawPanelString(b, font, labels[i], box.X + 9, box.Y + 6,
                    labels[i].Length == 1 ? 12 : 9, Color.White);
            }
        }

        private void DrawInfoTableSurface(SpriteBatch spriteBatch, Vector2 origin)
        {
            const int tableWidth = WINDOW_WIDTH - 36;
            const int tableHeight = 80;

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel != null)
            {
                spriteBatch.Draw(pixel, new Rectangle((int)origin.X, (int)origin.Y, tableWidth, tableHeight), Color.Black * 0.3f);
            }

            int cornerSizeTopLeftWidth = _tableTopLeftTexture?.Width ?? 0;
            int cornerSizeTopLeftHeight = _tableTopLeftTexture?.Height ?? 0;
            int cornerSizeTopRightWidth = _tableTopRightTexture?.Width ?? 0;
            int cornerSizeTopRightHeight = _tableTopRightTexture?.Height ?? 0;
            int cornerSizeBottomLeftWidth = _tableBottomLeftTexture?.Width ?? 0;
            int cornerSizeBottomLeftHeight = _tableBottomLeftTexture?.Height ?? 0;
            int cornerSizeBottomRightWidth = _tableBottomRightTexture?.Width ?? 0;
            int cornerSizeBottomRightHeight = _tableBottomRightTexture?.Height ?? 0;

            if (_tableTopLeftTexture != null)
            {
                spriteBatch.Draw(_tableTopLeftTexture,
                    new Rectangle((int)origin.X, (int)origin.Y, cornerSizeTopLeftWidth, cornerSizeTopLeftHeight),
                    Color.White);
            }

            if (_tableTopRightTexture != null)
            {
                spriteBatch.Draw(_tableTopRightTexture,
                    new Rectangle((int)(origin.X + tableWidth - cornerSizeTopRightWidth), (int)origin.Y, cornerSizeTopRightWidth, cornerSizeTopRightHeight),
                    Color.White);
            }

            if (_tableBottomLeftTexture != null)
            {
                spriteBatch.Draw(_tableBottomLeftTexture,
                    new Rectangle((int)origin.X, (int)(origin.Y + tableHeight - cornerSizeBottomLeftHeight), cornerSizeBottomLeftWidth, cornerSizeBottomLeftHeight),
                    Color.White);
            }

            if (_tableBottomRightTexture != null)
            {
                spriteBatch.Draw(_tableBottomRightTexture,
                    new Rectangle((int)(origin.X + tableWidth - cornerSizeBottomRightWidth), (int)(origin.Y + tableHeight - cornerSizeBottomRightHeight), cornerSizeBottomRightWidth, cornerSizeBottomRightHeight),
                    Color.White);
            }

            int horizontalStartX = cornerSizeTopLeftWidth;
            int horizontalEndX = tableWidth - cornerSizeTopRightWidth;
            int horizontalWidth = Math.Max(0, horizontalEndX - horizontalStartX);

            if (_tableTopLineTexture != null && horizontalWidth > 0)
            {
                spriteBatch.Draw(_tableTopLineTexture,
                    new Rectangle((int)(origin.X + horizontalStartX), (int)origin.Y, horizontalWidth, _tableTopLineTexture.Height),
                    Color.White);
            }

            if (_tableBottomLineTexture != null && horizontalWidth > 0)
            {
                spriteBatch.Draw(_tableBottomLineTexture,
                    new Rectangle((int)(origin.X + horizontalStartX), (int)(origin.Y + tableHeight - _tableBottomLineTexture.Height), horizontalWidth, _tableBottomLineTexture.Height),
                    Color.White);
            }

            int verticalStartY = cornerSizeTopLeftHeight;
            int verticalEndY = tableHeight - cornerSizeBottomLeftHeight;
            int verticalHeight = Math.Max(0, verticalEndY - verticalStartY);

            if (_tableLeftLineTexture != null && verticalHeight > 0)
            {
                spriteBatch.Draw(_tableLeftLineTexture,
                    new Rectangle((int)origin.X, (int)(origin.Y + verticalStartY), _tableLeftLineTexture.Width, verticalHeight),
                    Color.White);
            }

            if (_tableRightLineTexture != null && verticalHeight > 0)
            {
                spriteBatch.Draw(_tableRightLineTexture,
                    new Rectangle((int)(origin.X + tableWidth - _tableRightLineTexture.Width), (int)(origin.Y + verticalStartY), _tableRightLineTexture.Width, verticalHeight),
                    Color.White);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }

            base.Update(gameTime);

            HandleButtonInput();
            UpdateAmountKeyboard();
            UpdateDisplayData();
            UpdateAllocationBatch(gameTime.ElapsedGameTime.TotalSeconds);
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

            if (leftJustReleased)
            {
                if (_keypadOpen)
                {
                    HandleKeypadTap(mousePosition);
                    _pressedButtonIndex = -1;
                    return;
                }
                if (ScreenRect(s_amountBounds).Contains(mousePosition))
                {
                    _amountFocused = _keypadOpen = true;
                    _previousAmountKeys = Keyboard.GetState();
                    _pressedButtonIndex = -1;
                    return;
                }
                if (ScreenRect(s_confirmBounds).Contains(mousePosition))
                {
                    ConfirmAmount();
                    _pressedButtonIndex = -1;
                    return;
                }
                for (int stat = 0; stat < (_lastIsDarkLordFamily ? 5 : 4); stat++)
                {
                    if (ScreenRect(new Rectangle(20, (int)s_statRowY[stat], WINDOW_WIDTH - 40, 25))
                        .Contains(mousePosition))
                    {
                        _selectedStatIndex = stat;
                        UpdateAllocationLabels();
                        break;
                    }
                }
            }

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
                scope = new SpriteBatchScope(spriteBatch, SpriteSortMode.Deferred, BlendState.AlphaBlend, transform: UiScaler.SpriteTransform);
            }

            try
            {
                if (_staticSurface != null && !_staticSurface.IsDisposed)
                {
                    spriteBatch.Draw(_staticSurface, DisplayRectangle, Color.White * Alpha);
                }

                DrawTexts(spriteBatch, font);
                DrawButtons(spriteBatch);
                DrawAllocationUi(spriteBatch, font);
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
                Vector2 size = font.MeasureString(entry.Text) * textScale;

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
            for (int i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                if (!button.Visible)
                {
                    continue;
                }

                Rectangle destRect = GetButtonRectangle(button, controlScale);
                Rectangle? sourceRect = button.NormalRect;
                if (_pressedButtonIndex == i && button.Enabled && button.PressedRect.HasValue)
                {
                    sourceRect = button.PressedRect;
                }
                else if (_hoveredButtonIndex == i && button.Enabled && button.HoverRect.HasValue)
                {
                    sourceRect = button.HoverRect;
                }

                Color baseColor = button.Enabled ? Color.White : new Color(120, 120, 120);
                if (_pressedButtonIndex == i && button.Enabled)
                {
                    baseColor = new Color(200, 200, 180);
                }
                else if (_hoveredButtonIndex == i && button.Enabled)
                {
                    baseColor = new Color(240, 240, 200);
                }

                baseColor *= Alpha;

                if (button.Texture != null)
                {
                    spriteBatch.Draw(button.Texture, destRect, sourceRect, baseColor);
                }
                else if (_hoveredButtonIndex == i || _pressedButtonIndex == i)
                {
                    var overlayColor = _pressedButtonIndex == i
                        ? new Color(200, 200, 180, 200)
                        : new Color(240, 240, 200, 160);
                    spriteBatch.Draw(GraphicsManager.Instance.Pixel, destRect, overlayColor * Alpha);
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
            if (_characterState == null)
            {
                _characterState = _networkManager?.GetCharacterState();
            }

            if (_characterState == null) return;
            if (!_statReplySubscribed)
            {
                _characterState.StatIncreaseResponseReceived += OnStatIncreaseResponse;
                _statReplySubscribed = true;
            }
            var snapshot = CharacterInfoSnapshot.Create(_characterState);
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
            _classText.Text = $"({CharacterClassDatabase.GetClassName(snapshot.Class)})";

            _levelText.Text = snapshot.MasterLevel > 0
                ? $"Lv. {snapshot.Level} / Master {snapshot.MasterLevel}" : $"Level: {snapshot.Level}";
            _expText.Text = snapshot.MasterLevel > 0
                ? $"Master Exp: {snapshot.MasterExperience} / {snapshot.MasterExperienceForNextLevel}"
                : $"Exp: {snapshot.Experience} / {snapshot.ExperienceForNextLevel}";
            _statPointsText.Visible = true;
            _statPointsText.Text = $"Points: {snapshot.LevelUpPoints}";

            _fruitProbText.Text = "[+]100%|[-]100%";
            _fruitStatsText.Text = "Create 0/0 | Decrease 0/0";

            for (int i = 0; i < BTN_STAT_COUNT; i++)
            {
                ushort baseValue = snapshot.GetBaseStat(i);
                ushort addedValue = snapshot.GetAddedStat(i);
                int total = baseValue + addedValue;
                _statValueTexts[i].Text = addedValue > 0 ? $"{baseValue}+{addedValue}" : total.ToString();
                _statValueTexts[i].Color = addedValue > 0 ? new Color(100, 150, 255) : new Color(230, 230, 0);
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

            // Command is the actual fifth stat for DL; don't fabricate pet combat values.
            _pvmInfo1Text.Visible = isDarkLordFamily;
            _pvmInfo1Text.Text = $"Command: {snapshot.BaseLeadership + snapshot.AddedLeadership}";
            _pvmInfo2Text.Visible = isDarkLordFamily;
            _pvmInfo2Text.Text = "Dark Raven / Dark Horse leadership";

            // Show/hide Leadership (CMD) stat value text based on character class
            _statValueTexts[4].Visible = isDarkLordFamily;

            bool hasLevelUpPoints = snapshot.LevelUpPoints > 0;
            for (int i = 0; i < BTN_STAT_COUNT - 1; i++)
            {
                if (_statButtons[i] != null)
                {
                    _statButtons[i].Visible = hasLevelUpPoints;
                    _statButtons[i].Enabled = hasLevelUpPoints;
                }
            }

            // Leadership button visible only for Dark Lord family with available points
            if (_statButtons[4] != null)
            {
                _statButtons[4].Visible = isDarkLordFamily && hasLevelUpPoints;
                _statButtons[4].Enabled = isDarkLordFamily && hasLevelUpPoints;
            }

            if (_masterButton != null)
            {
                bool masterActive = snapshot.MasterLevel > 0 && snapshot.CanBeMaster;
                _masterButton.Visible = true;
                _masterButton.Enabled = masterActive;
            }

            _exitButton.Visible = true;
            _exitButton.Enabled = true;
            _questButton.Visible = true;
            _questButton.Enabled = true;
            _petButton.Visible = true;
            _petButton.Enabled = true;
            UpdateAllocationLabels();
        }


        private void UpdateAllocationLabels()
        {
            if (_selectionText == null || _allocationStatusText == null) return;
            if (_selectedStatIndex == 4 && !_lastIsDarkLordFamily) _selectedStatIndex = 0;
            _selectionText.Text = $"A {s_statShortNames[_selectedStatIndex]}";
            if (_remaining > 0)
                _allocationStatusText.Text = $"Asignados {_completed}/{_requested} | Pendientes: {_remaining}";
            else if (string.IsNullOrEmpty(_allocationStatusText.Text))
                _allocationStatusText.Text = "Pulsa + para 1 punto o ingresa una cantidad.";
        }

        private static CharacterStatAttribute AttributeAt(int index) => index switch
        {
            0 => CharacterStatAttribute.Strength,
            1 => CharacterStatAttribute.Agility,
            2 => CharacterStatAttribute.Vitality,
            3 => CharacterStatAttribute.Energy,
            4 => CharacterStatAttribute.Leadership,
            _ => CharacterStatAttribute.Strength
        };

        private void OnStatButtonClicked(int index)
        {
            SoundController.Instance.PlayBuffer("Sound/iButtonClick.wav");
            _selectedStatIndex = index;
            UpdateAllocationLabels();
            BeginAllocation(index, 1);
        }

        private void ConfirmAmount()
        {
            if (!int.TryParse(_amountText, out int amount) || amount < 1)
            {
                _allocationStatusText.Text = "Ingresa una cantidad positiva.";
                return;
            }
            _keypadOpen = _amountFocused = false;
            BeginAllocation(_selectedStatIndex, amount);
        }

        private void BeginAllocation(int statIndex, int amount)
        {
            if (_remaining > 0 || _awaitingResponse)
            {
                _allocationStatusText.Text = "Ya hay una asignacion en curso.";
                return;
            }
            if (_networkManager == null || !_networkManager.IsConnected ||
                _networkManager.CurrentState < ClientConnectionState.InGame || _characterState == null)
            {
                _allocationStatusText.Text = "No hay conexion con el servidor.";
                return;
            }
            if (statIndex < 0 || statIndex >= 5 ||
                (statIndex == 4 && !_lastIsDarkLordFamily))
            {
                _allocationStatusText.Text = "Atributo no disponible para esta clase.";
                return;
            }

            int current = statIndex switch
            {
                0 => _characterState.Strength,
                1 => _characterState.Agility,
                2 => _characterState.Vitality,
                3 => _characterState.Energy,
                4 => _characterState.Leadership,
                _ => 0
            };
            if (amount < 1 || amount > _characterState.LevelUpPoints ||
                amount > ushort.MaxValue - current)
            {
                _allocationStatusText.Text = "Supera los puntos disponibles o el limite del atributo.";
                return;
            }

            _selectedStatIndex = statIndex;
            _allocationAttribute = AttributeAt(statIndex);
            _remaining = _requested = amount;
            _completed = 0;
            _responseSeconds = _sendCooldown = 0;
            _allocationStatusText.Text = "";
            UpdateAllocationLabels();
        }

        // Network callbacks can be delivered off the render thread. Consume on Update().
        private void OnStatIncreaseResponse(CharacterStatAttribute attr, bool success, ushort amount)
            => _replyQueue.Enqueue((attr, success, amount));

        private void UpdateAllocationBatch(double deltaSeconds)
        {
            while (_replyQueue.TryDequeue(out var result))
            {
                if (!_awaitingResponse || result.attr != _allocationAttribute) continue;
                _awaitingResponse = false;
                if (!result.success || result.amount == 0)
                {
                    StopAllocation("Servidor rechazo el incremento.");
                    continue;
                }
                int applied = Math.Min(result.amount, _remaining);
                _completed += applied;
                _remaining -= applied;
                _sendCooldown = 0.09; // Avoid flooding the connection even on fast replies.
                if (_remaining == 0)
                    StopAllocation($"Completado: {_completed} puntos en {s_statShortNames[_selectedStatIndex]}.");
            }
            if (_remaining <= 0) return;

            if (_awaitingResponse)
            {
                _responseSeconds += deltaSeconds;
                if (_responseSeconds > 6)
                    StopAllocation("No hubo respuesta del servidor; comprueba tus puntos.");
                return;
            }

            _sendCooldown -= deltaSeconds;
            if (_sendCooldown > 0) return;
            if (_networkManager == null || !_networkManager.IsConnected ||
                _networkManager.CurrentState < ClientConnectionState.InGame ||
                _characterState == null || _characterState.LevelUpPoints == 0)
            {
                StopAllocation("Se detuvo: sin conexion o sin puntos disponibles.");
                return;
            }

            var service = _networkManager.GetCharacterService();
            if (service == null)
            {
                StopAllocation("No se pudo acceder al servicio de stats.");
                return;
            }
            _awaitingResponse = true;
            _responseSeconds = 0;
            // There is no amount field in the existing client request. Send only one
            // and wait for the authoritative F3:06 response before the next request.
            _ = service.SendIncreaseCharacterStatPointRequestAsync(_allocationAttribute);
            UpdateAllocationLabels();
        }

        private void StopAllocation(string feedback)
        {
            _remaining = 0;
            _awaitingResponse = false;
            _sendCooldown = 0;
            if (feedback != null && _allocationStatusText != null)
                _allocationStatusText.Text = feedback;
        }

        private void AppendAmountDigit(char digit)
        {
            if (_amountText.Length >= 5) return;
            _amountText = (_amountText == "0" ? "" : _amountText) + digit;
        }

        private void UpdateAmountKeyboard()
        {
            KeyboardState keys = Keyboard.GetState();
            if (_amountFocused)
            {
                foreach (Keys key in keys.GetPressedKeys())
                {
                    if (!_previousAmountKeys.IsKeyUp(key)) continue;
                    if (key == Keys.Back)
                    {
                        if (_amountText.Length > 0)
                            _amountText = _amountText.Substring(0, _amountText.Length - 1);
                    }
                    else if (key == Keys.Delete) _amountText = "";
                    else if (key == Keys.Enter)
                    {
                        _keypadOpen = false;
                        ConfirmAmount();
                    }
                    else if (key == Keys.Escape) _amountFocused = _keypadOpen = false;
                    else if (key >= Keys.D0 && key <= Keys.D9)
                        AppendAmountDigit((char)('0' + (int)(key - Keys.D0)));
                    else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                        AppendAmountDigit((char)('0' + (int)(key - Keys.NumPad0)));
                }
            }
            _previousAmountKeys = keys;
        }

        private void HandleKeypadTap(Point point)
        {
            if (!ScreenRect(new Rectangle(89, 287, 137, 169)).Contains(point))
            {
                _keypadOpen = false;
                return;
            }
            const string digits = "123456789C0K";
            for (int i = 0; i < 12; i++)
            {
                var rect = new Rectangle(97 + i % 3 * 41, 314 + i / 3 * 34, 37, 29);
                if (!ScreenRect(rect).Contains(point)) continue;
                if (digits[i] == 'C') _amountText = "";
                else if (digits[i] == 'K') _keypadOpen = false;
                else AppendAmountDigit(digits[i]);
                return;
            }
        }

        private CharacterInfoTextEntry CreateText(Vector2 basePosition, float fontSize, Color color)
            => CreateText(basePosition, fontSize, color, TextAlignment.Left);

        public void ShowWindow()
        {
            Visible = true;
            _pressedButtonIndex = -1;
            _hoveredButtonIndex = -1;
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
            StopAllocation(null);
            _amountFocused = _keypadOpen = false;
            Visible = false;
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

            if (_statReplySubscribed && _characterState != null)
                _characterState.StatIncreaseResponseReceived -= OnStatIncreaseResponse;
            base.Dispose();
            _staticSurface?.Dispose();
            _staticSurface = null;
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
                MasterExperience = state.MasterExperience;
                MasterExperienceForNextLevel = state.MasterExperienceForNextLevel;
                Experience = state.Experience;
                ExperienceForNextLevel = state.ExperienceForNextLevel;
                LevelUpPoints = state.LevelUpPoints;
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
            public readonly ulong MasterExperience;
            public readonly ulong MasterExperienceForNextLevel;
            public readonly ulong Experience;
            public readonly ulong ExperienceForNextLevel;
            public readonly ushort LevelUpPoints;
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
                       MasterExperience == other.MasterExperience &&
                       MasterExperienceForNextLevel == other.MasterExperienceForNextLevel &&
                       Experience == other.Experience &&
                       ExperienceForNextLevel == other.ExperienceForNextLevel &&
                       LevelUpPoints == other.LevelUpPoints &&
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
