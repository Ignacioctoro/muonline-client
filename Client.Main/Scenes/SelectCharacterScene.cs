using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game;
using Client.Main.Controls.UI.SelectCharacter;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Networking;
using Client.Main.Objects.Player;
using Client.Main.Worlds;
using Client.Main.Scenes.SelectCharacter;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MUnique.OpenMU.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Main.Scenes
{
    public partial class SelectCharacterScene : BaseScene
    {
        private static class Theme
        {
            // Background layers
            public static readonly Color BgDarkest = new(8, 10, 14, 252);
            public static readonly Color BgDark = new(16, 20, 26, 250);
            public static readonly Color BgMid = new(24, 30, 38, 248);
            public static readonly Color BgLight = new(35, 42, 52, 245);
            public static readonly Color BgLighter = new(48, 56, 68, 240);

            // Accent - Warm Gold
            public static readonly Color Accent = new(212, 175, 85);
            public static readonly Color AccentBright = new(255, 215, 120);
            public static readonly Color AccentDim = new(140, 115, 55);
            public static readonly Color AccentGlow = new(255, 200, 80, 40);

            // Secondary accent - Cool Blue
            public static readonly Color Secondary = new(90, 140, 200);
            public static readonly Color SecondaryBright = new(130, 180, 240);
            public static readonly Color SecondaryDim = new(50, 80, 120);

            // Borders
            public static readonly Color BorderOuter = new(5, 6, 8, 255);
            public static readonly Color BorderInner = new(60, 70, 85, 200);
            public static readonly Color BorderHighlight = new(100, 110, 130, 120);

            // Text
            public static readonly Color TextWhite = new(240, 240, 245);
            public static readonly Color TextGold = new(255, 220, 130);
            public static readonly Color TextGray = new(160, 165, 175);
            public static readonly Color TextDark = new(100, 105, 115);

            // Status colors
            public static readonly Color Success = new(80, 200, 120);
            public static readonly Color Warning = new(240, 180, 60);
            public static readonly Color Danger = new(220, 80, 80);
        }

        private const int PANEL_WIDTH = 280;
        private const int PANEL_MARGIN = 30;
        private const int HEADER_HEIGHT = 45;
        private const int BUTTON_HEIGHT = 36;
        private const int BUTTON_SPACING = 8;
        private const int INNER_PADDING = 12;
        private const int CHAR_CARD_HEIGHT = 66;
        private const int CHAR_CARD_SPACING = 6;

        // Fields
        private readonly List<(string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance)> _characters;
        private SelectWorld _selectWorld;
        private CharacterSelectionController _characterController;
        private readonly NetworkManager _networkManager;
        private ILogger<SelectCharacterScene> _logger;
        private (string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance)? _selectedCharacterInfo = null;
        private LoadingScreenControl _loadingScreen;
        private bool _initialLoadComplete = false;
        private ButtonControl _previousCharacterButton;
        private ButtonControl _nextCharacterButton;
        private int _currentCharacterIndex = -1;
        private bool _isSelectionInProgress = false;
        private Texture2D _backgroundTexture;
        private ProgressBarControl _progressBar;
        private bool _previousDayNightEnabled;
        private Vector3 _previousSunDirection;
        private bool _dayNightPatched;
        private ButtonControl _createCharacterButton;
        private ButtonControl _deleteCharacterButton;
        private ButtonControl _enterGameButton;
        private ButtonControl _exitButton;
        private CharacterCreationDialog _characterCreationDialog;
        private CharacterDeletionDialog _characterDeletionDialog;
        private string _currentlySelectedCharacterName = null;
        private bool _isIntentionalLogout = false;

        // UI Panel rendering
        private Rectangle _characterPanelRect;
        private Rectangle _buttonSectionRect;
        private Rectangle _characterListRect;
        private List<Rectangle> _characterCardRects = new List<Rectangle>();
        private int _hoveredCardIndex = -1;
        private bool _previousMousePressed = false;

        // Constructors
        public SelectCharacterScene(List<(string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance)> characters, NetworkManager networkManager)
        {
            _characters = characters ?? new List<(string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance)>();
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _logger = MuGame.AppLoggerFactory.CreateLogger<SelectCharacterScene>();

            _loadingScreen = new LoadingScreenControl { Visible = true, Message = "Loading Characters..." };
            Controls.Add(_loadingScreen);
            _loadingScreen.BringToFront();

            InitializeModernUI();

            try
            {
                _backgroundTexture = MuGame.Instance.Content.Load<Texture2D>("Background");
            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"[SelectCharacterScene] Background load failed: {ex.Message}");
            }

            _progressBar = new ProgressBarControl();
            Controls.Add(_progressBar);

            SubscribeToNetworkEvents();
        }

        private void DisableDayNightCycleForScene()
        {
            if (_dayNightPatched) return;

            _dayNightPatched = true;
            _previousDayNightEnabled = Constants.ENABLE_DAY_NIGHT_CYCLE;
            _previousSunDirection = Constants.SUN_DIRECTION;
            Constants.ENABLE_DAY_NIGHT_CYCLE = false;
            SunCycleManager.ResetToDefault();
        }

        private void RestoreDayNightCycle()
        {
            if (!_dayNightPatched) return;

            Constants.ENABLE_DAY_NIGHT_CYCLE = _previousDayNightEnabled;
            Constants.SUN_DIRECTION = _previousSunDirection;
            _dayNightPatched = false;
        }

        private void UpdateLoadProgress(string message, float progress)
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                if (_loadingScreen != null && _loadingScreen.Visible)
                {
                    _loadingScreen.Message = message;
                    _loadingScreen.Progress = progress;
                }
            });
        }

        private void InitializeModernUI()
        {
            // Previous/Next character arrows (disabled)
            _previousCharacterButton = CreateModernNavigationButton("<");
            _previousCharacterButton.Click += (s, e) => MoveSelection(-1);
            _previousCharacterButton.Enabled = false;
            _previousCharacterButton.Visible = false;
            Controls.Add(_previousCharacterButton);

            _nextCharacterButton = CreateModernNavigationButton(">");
            _nextCharacterButton.Click += (s, e) => MoveSelection(1);
            _nextCharacterButton.Enabled = false;
            _nextCharacterButton.Visible = false;
            Controls.Add(_nextCharacterButton);

            // Action buttons
            _enterGameButton = CreateModernButton("ENTER GAME", Theme.Success);
            _enterGameButton.Click += OnEnterGameButtonClick;
            Controls.Add(_enterGameButton);

            _createCharacterButton = CreateModernButton("CREATE CHARACTER", Theme.Secondary);
            _createCharacterButton.Click += OnCreateCharacterButtonClick;
            Controls.Add(_createCharacterButton);

            _deleteCharacterButton = CreateModernButton("DELETE CHARACTER", Theme.Danger);
            _deleteCharacterButton.Click += OnDeleteCharacterButtonClick;
            Controls.Add(_deleteCharacterButton);

            _exitButton = CreateModernButton("EXIT", Theme.BgLight);
            _exitButton.Click += OnExitButtonClick;
            Controls.Add(_exitButton);

            CalculatePanelLayout();
        }

        private ButtonControl CreateModernNavigationButton(string arrow)
        {
            return new ButtonControl
            {
                Text = arrow,
                FontSize = 48f,
                AutoViewSize = false,
                ViewSize = new Point(70, 70),
                BackgroundColor = Theme.BgMid,
                HoverBackgroundColor = Theme.BgLight,
                PressedBackgroundColor = Theme.BgDark,
                TextColor = Theme.Accent,
                HoverTextColor = Theme.AccentBright,
                DisabledTextColor = Theme.TextDark,
                Interactive = true,
                Visible = false,
                Enabled = false,
                BorderThickness = 2,
                BorderColor = Theme.BorderInner
            };
        }

        private ButtonControl CreateModernButton(string text, Color baseColor)
        {
            // Keep the existing click/state logic. The native atlas is drawn
            // in the scene overlay, after the 3D world and before the cursor.
            return new NativeSelectionButton
            {
                Text = text, AutoViewSize = false, ViewSize = new Point(100, 34),
                Interactive = true, Visible = false, Enabled = false,
            };
        }

        private void CalculatePanelLayout()
        {
            int cardHeight = Math.Min(CHAR_CARD_HEIGHT, Math.Max(40, (ViewSize.Y - 180) / 5 - CHAR_CARD_SPACING));
            int width = Math.Min(PANEL_WIDTH, Math.Max(210, ViewSize.X / 4));
            int listHeight = cardHeight * 5 + CHAR_CARD_SPACING * 4;
            int panelX = Math.Max(12, ViewSize.X - width - PANEL_MARGIN);
            int panelY = Math.Max(70, (ViewSize.Y - listHeight) / 2 - 14);
            var nextRect = new Rectangle(panelX, panelY, width, listHeight);
            if (_characterListRect != nextRect) _nativeSelectionSurfaceDirty = true;
            _characterPanelRect = nextRect;
            _characterListRect = nextRect;
            _buttonSectionRect = new Rectangle(0, ViewSize.Y - 40, ViewSize.X, 30);
            _characterCardRects.Clear();
            for (int i = 0; i < 5; i++)
                _characterCardRects.Add(new Rectangle(panelX, panelY + i * (cardHeight + CHAR_CARD_SPACING), width, cardHeight));
        }

        private void PositionNavigationButtons()
        {
            // BaseScene sets ViewSize from its constructor, invoking our
            // resize override before this constructor assigns _characters.
            // InitializeModernUI/AfterLoad will lay out the completed scene.
            if (_characters == null || _characterCardRects == null) return;
            CalculatePanelLayout();
            bool ready = _initialLoadComplete && (_loadingScreen == null || !_loadingScreen.Visible) && !_isSelectionInProgress;
            bool hasSelection = !string.IsNullOrEmpty(_currentlySelectedCharacterName);
            int y = _buttonSectionRect.Y;
            void Place(ButtonControl button, int x, int width, bool enabled)
            {
                if (button == null) return;
                button.X = x; button.Y = y;
                button.ViewSize = new Point(width, 30);
                button.Visible = ready;
                button.Enabled = ready && enabled && _characterDeletionDialog == null && _characterCreationDialog == null;
            }
            Place(_createCharacterButton, 20, 54, _characters.Count < 5);
            Place(_exitButton, 75, 54, true);
            Place(_enterGameButton, Math.Max(135, ViewSize.X - 132), 54, hasSelection && _characters.Count > 0);
            Place(_deleteCharacterButton, Math.Max(190, ViewSize.X - 77), 54, hasSelection);
            if (_previousCharacterButton != null) { _previousCharacterButton.Visible = false; _previousCharacterButton.Enabled = false; }
            if (_nextCharacterButton != null) { _nextCharacterButton.Visible = false; _nextCharacterButton.Enabled = false; }
        }

        private void UpdateNavigationButtonState()
        {
            // Navigation buttons are permanently disabled
            if (_previousCharacterButton != null)
            {
                _previousCharacterButton.Enabled = false;
                _previousCharacterButton.Visible = false;
            }

            if (_nextCharacterButton != null)
            {
                _nextCharacterButton.Enabled = false;
                _nextCharacterButton.Visible = false;
            }
        }

        private void MoveSelection(int direction)
        {
            if (_characters.Count == 0 || _characterController == null)
            {
                return;
            }

            if (!_initialLoadComplete || (_loadingScreen != null && _loadingScreen.Visible) || _isSelectionInProgress)
            {
                return;
            }

            int currentIndex = _currentCharacterIndex;
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            if (_characters.Count == 1)
            {
                return;
            }

            int nextIndex = (currentIndex + direction) % _characters.Count;
            if (nextIndex < 0)
            {
                nextIndex += _characters.Count;
            }

            if (nextIndex == _currentCharacterIndex)
            {
                return;
            }

            _currentCharacterIndex = nextIndex;
            _characterController.SetActiveCharacter(_currentCharacterIndex);

            if (_currentCharacterIndex >= 0 && _currentCharacterIndex < _characters.Count)
            {
                _currentlySelectedCharacterName = _characters[_currentCharacterIndex].Name;
                PositionNavigationButtons();
                UpdateNavigationButtonState();
            }
            else
            {
                _currentlySelectedCharacterName = null;
                UpdateNavigationButtonState();
            }

        }

        protected override async Task LoadSceneContentWithProgress(Action<string, float> progressCallback)
        {
            DisableDayNightCycleForScene();
            UpdateLoadProgress("Initializing Character Selection...", 0.0f);
            _logger.LogInformation(">>> SelectCharacterScene LoadSceneContentWithProgress starting...");

            try
            {
                await LoadNativeSelectionTexturesAsync();
                UpdateLoadProgress("Creating Select World...", 0.05f);
                _selectWorld = new SelectWorld();
                Controls.Add(_selectWorld);

                UpdateLoadProgress("Initializing Select World (Graphics)...", 0.1f);
                await _selectWorld.Initialize();
                World = _selectWorld;
                UpdateLoadProgress("Select World Initialized.", 0.35f);
                _logger.LogInformation("--- SelectCharacterScene: SelectWorld initialized and set.");

                if (_selectWorld.Terrain != null)
                {
                    _selectWorld.Terrain.AmbientLight = 0.6f;
                }

                // Create controller
                _characterController = new CharacterSelectionController(
                    MuGame.AppLoggerFactory.CreateLogger<CharacterSelectionController>());

                // Subscribe to events
                _characterController.CharacterClicked += OnControllerCharacterClicked;
                _characterController.CharacterDoubleClicked += OnControllerCharacterDoubleClicked;

                // Connect to world
                _selectWorld.SetController(_characterController);

                if (_characters.Any())
                {
                    UpdateLoadProgress("Preparing Character Data...", 0.40f);
                    await _characterController.CreateCharactersAsync(
                        _characters,
                        _selectWorld,
                        this,
                        _selectWorld.CharacterDisplayPosition,
                        _selectWorld.CharacterDisplayAngle);

                    if (_characters.Count > 0)
                    {
                        _currentCharacterIndex = 0;
                        _currentlySelectedCharacterName = _characters[0].Name;
                    }
                    else
                    {
                        _currentCharacterIndex = -1;
                    }

                    PositionNavigationButtons();
                    UpdateNavigationButtonState();

                    float characterCreationStartProgress = 0.45f;
                    float characterCreationEndProgress = 0.85f;
                    float totalCharacterProgressSpan = characterCreationEndProgress - characterCreationStartProgress;

                    if (_characters.Count > 0)
                    {
                        float progressPerCharacter = totalCharacterProgressSpan / _characters.Count;
                        for (int i = 0; i < _characters.Count; i++)
                        {
                            UpdateLoadProgress($"Configuring character {i + 1}/{_characters.Count}...", characterCreationStartProgress + (i + 1) * progressPerCharacter);
                        }
                    }
                    else
                    {
                        UpdateLoadProgress("No characters to configure.", characterCreationEndProgress);
                    }

                    UpdateLoadProgress("Character Objects Ready.", 0.90f);
                    _logger.LogInformation("--- SelectCharacterScene: Character creation finished.");
                }
                else
                {
                    _currentCharacterIndex = -1;
                    string message = "No characters found on this account.";
                    _logger.LogWarning("--- SelectCharacterScene: {Message}", message);
                    UpdateLoadProgress(message, 0.90f);
                    UpdateNavigationButtonState();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "!!! SelectCharacterScene: Error during world initialization or character creation.");
                UpdateLoadProgress("Error loading character selection.", 1.0f);
                UpdateNavigationButtonState();
            }
            finally
            {
                _initialLoadComplete = true;
                UpdateNavigationButtonState();
                UpdateLoadProgress("Character Selection Ready.", 1.0f);
                _logger.LogInformation("<<< SelectCharacterScene LoadSceneContentWithProgress finished.");
            }
        }

        public override void AfterLoad()
        {
            base.AfterLoad();
            _logger.LogInformation("SelectCharacterScene.AfterLoad() called.");
            if (_loadingScreen != null)
            {
                MuGame.ScheduleOnMainThread(() =>
                {
                    if (_loadingScreen != null)
                    {
                        Controls.Remove(_loadingScreen);
                        _loadingScreen.Dispose();
                        _loadingScreen = null;
                        if (_progressBar != null)
                        {
                            _progressBar.Visible = false;
                        }
                        PositionNavigationButtons();
                        UpdateNavigationButtonState();
                        _previousCharacterButton?.BringToFront();
                        _nextCharacterButton?.BringToFront();
                        _deleteCharacterButton?.BringToFront();
                        _createCharacterButton?.BringToFront();
                        _enterGameButton?.BringToFront();
                        _exitButton?.BringToFront();
                        Cursor?.BringToFront();
                        DebugPanel?.BringToFront();
                    }
                });
            }
        }

        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();
            PositionNavigationButtons();
        }

        public override async Task Load()
        {
            if (Status == GameControlStatus.Initializing)
            {
                await LoadSceneContentWithProgress(UpdateLoadProgress);
            }
            else
            {
                _logger.LogDebug("SelectCharacterScene.Load() called outside of InitializeWithProgressReporting flow. Re-routing to progressive load.");
                await LoadSceneContentWithProgress(UpdateLoadProgress);
            }
        }


        public void CharacterSelected(string characterName)
        {
            if (_loadingScreen != null && _loadingScreen.Visible)
            {
                _logger.LogInformation("Character selection attempted while loading screen is visible. Ignoring.");
                return;
            }

            int matchedIndex = -1;
            for (int i = 0; i < _characters.Count; i++)
            {
                if (string.Equals(_characters[i].Name, characterName, StringComparison.Ordinal))
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex < 0)
            {
                _logger.LogError("Character '{CharacterName}' selected, but not found in the character list.", characterName);
                MessageWindow.Show($"Error selecting character '{characterName}'.");
                return;
            }

            _selectedCharacterInfo = _characters[matchedIndex];
            _currentCharacterIndex = matchedIndex;
            _characterController?.SetActiveCharacter(_currentCharacterIndex);

            ClientConnectionState currentState = _networkManager.CurrentState;
            bool canSelect = currentState == ClientConnectionState.ConnectedToGameServer ||
                             currentState == ClientConnectionState.SelectingCharacter;

            if (!canSelect)
            {
                _logger.LogWarning("Character selection attempted but NetworkManager state is not ConnectedToGameServer or SelectingCharacter. State: {State}", currentState);
                MessageWindow.Show($"Cannot select character. Invalid network state: {currentState}");
                _selectedCharacterInfo = null;
                return;
            }

            _logger.LogInformation("Character '{CharacterName}' (Class: {Class}) selected in scene. Sending request...",
                                   _selectedCharacterInfo.Value.Name, _selectedCharacterInfo.Value.Class);

            DisableInteractionDuringSelection(characterName);
            _ = _networkManager.SendSelectCharacterRequestAsync(characterName);
        }

        public override void Dispose()
        {
            _logger.LogDebug("Disposing SelectCharacterScene.");
            UnsubscribeFromNetworkEvents();

            if (_characterController != null)
            {
                _characterController.CharacterClicked -= OnControllerCharacterClicked;
                _characterController.CharacterDoubleClicked -= OnControllerCharacterDoubleClicked;
                _characterController.Dispose();
                _characterController = null;
            }

            CloseCharacterCreationDialog();
            CloseCharacterDeletionDialog();
            if (_loadingScreen != null)
            {
                Controls.Remove(_loadingScreen);
                _loadingScreen.Dispose();
                _loadingScreen = null;
            }
            DisposeNativeSelectionSurface();
            RestoreDayNightCycle();
            base.Dispose();
        }

        private void SubscribeToNetworkEvents()
        {
            if (_networkManager != null)
            {
                _networkManager.EnteredGame += HandleEnteredGame;
                _networkManager.ErrorOccurred += HandleNetworkError;
                _networkManager.ConnectionStateChanged += HandleConnectionStateChange;
                _networkManager.CharacterListReceived += HandleCharacterListReceived;
                _networkManager.LogoutResponseReceived += HandleLogoutResponseReceived;
                _logger.LogDebug("SelectCharacterScene subscribed to NetworkManager events (including LogoutResponseReceived).");
            }
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (_networkManager != null)
            {
                _networkManager.EnteredGame -= HandleEnteredGame;
                _networkManager.ErrorOccurred -= HandleNetworkError;
                _networkManager.ConnectionStateChanged -= HandleConnectionStateChange;
                _networkManager.CharacterListReceived -= HandleCharacterListReceived;
                _networkManager.LogoutResponseReceived -= HandleLogoutResponseReceived;
                _logger.LogDebug("SelectCharacterScene unsubscribed from NetworkManager events.");
            }
        }

        private void HandleLogoutResponseReceived(object sender, LogOutType logoutType)
        {
            _logger.LogInformation("SelectCharacterScene.HandleLogoutResponseReceived: Type={Type}", logoutType);
            // Intentional logout handling is now done in HandleConnectionStateChange
            // which reacts to the Disconnected state after logout
        }

        private void HandleCharacterListReceived(object sender,
            List<(string Name, CharacterClassNumber Class, ushort Level, byte[] Appearance)> characters)
        {
            _logger.LogInformation("SelectCharacterScene.HandleCharacterListReceived: Received {Count} characters", characters?.Count ?? 0);
            
            MuGame.ScheduleOnMainThread(() =>
            {
                if (MuGame.Instance.ActiveScene != this)
                {
                    _logger.LogWarning("Scene changed, aborting character list refresh.");
                    return;
                }

                if (characters == null || characters.Count == 0)
                {
                    _logger.LogError("Received null or empty character list.");
                    return;
                }

                try
                {
                    _logger.LogInformation("Reloading SelectCharacterScene with updated list...");
                    var newScene = new SelectCharacterScene(characters, _networkManager);
                    MuGame.Instance.ChangeScene(newScene);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading SelectCharacterScene.");
                }
            });
        }

        private void HandleEnteredGame(object sender, EventArgs e)
        {
            _logger.LogInformation(">>> SelectCharacterScene.HandleEnteredGame: Event received.");

            if (!_selectedCharacterInfo.HasValue)
            {
                _logger.LogError("!!! SelectCharacterScene.HandleEnteredGame: EnteredGame event received, but _selectedCharacterInfo is null. Cannot change to GameScene.");
                if (_loadingScreen != null)
                {
                    MuGame.ScheduleOnMainThread(() =>
                    {
                        Controls.Remove(_loadingScreen);
                        _loadingScreen.Dispose();
                        _loadingScreen = null;
                        EnableInteractionAfterSelection();
                    });
                }
                return;
            }

            var characterInfo = _selectedCharacterInfo.Value;
            _logger.LogInformation("--- SelectCharacterScene.HandleEnteredGame: Scheduling scene change to GameScene for character: {Name} ({Class})",
                characterInfo.Name, characterInfo.Class);

            MuGame.ScheduleOnMainThread(() =>
            {
                _logger.LogInformation("--- SelectCharacterScene.HandleEnteredGame (UI Thread): Executing scheduled scene change...");
                if (MuGame.Instance.ActiveScene == this)
                {
                    try
                    {
                        MuGame.Instance.ChangeScene(new GameScene(characterInfo, _networkManager));
                        _logger.LogInformation("<<< SelectCharacterScene.HandleEnteredGame (UI Thread): ChangeScene to GameScene call completed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "!!! SelectCharacterScene.HandleEnteredGame (UI Thread): Exception during ChangeScene to GameScene.");
                        EnableInteractionAfterSelection();
                    }
                }
                else
                {
                    _logger.LogWarning("<<< SelectCharacterScene.HandleEnteredGame (UI Thread): Scene changed before execution. Aborting change to GameScene.");
                }
            });
        }

        private void HandleNetworkError(object sender, string errorMessage)
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                _logger.LogError("SelectCharacterScene received NetworkError: {Error}", errorMessage);
                MessageWindow.Show($"Network Error: {errorMessage}");
                EnableInteractionAfterSelection();
                if (MuGame.Instance.ActiveScene == this)
                {
                    MuGame.Instance.ChangeScene<LoginScene>();
                }
            });
        }

        private void HandleConnectionStateChange(object sender, ClientConnectionState newState)
        {
            MuGame.ScheduleOnMainThread(() =>
            {
                _logger.LogDebug("SelectCharacterScene received ConnectionStateChanged: {NewState}", newState);
                if (newState == ClientConnectionState.Disconnected)
                {
                    if (_isIntentionalLogout)
                    {
                        _logger.LogInformation("Intentional logout - returning to LoginScene.");
                    }
                    else
                    {
                        _logger.LogWarning("Disconnected while in character selection. Returning to LoginScene.");
                        MessageWindow.Show("Connection lost.");
                    }

                    if (MuGame.Instance.ActiveScene == this)
                    {
                        MuGame.Instance.ChangeScene<LoginScene>();
                    }
                }
            });
        }

        private void DisableInteractionDuringSelection(string characterName)
        {
            _isSelectionInProgress = true;
            if (_selectWorld != null)
            {
                _selectWorld.Interactive = false;
            }
            if (_characterController != null)
            {
                foreach (var player in _characterController.Characters)
                {
                    player.Interactive = false;
                }
                foreach (var label in _characterController.Labels.Values)
                {
                    label.Visible = false;
                }
            }
            if (_loadingScreen == null)
            {
                _loadingScreen = new LoadingScreenControl { Visible = true };
                Controls.Add(_loadingScreen);
            }
            _loadingScreen.Message = $"Entering game as {characterName}...";
            _loadingScreen.Progress = 0f;
            _loadingScreen.Visible = true;
            _loadingScreen.BringToFront();
            Cursor?.BringToFront();
            UpdateNavigationButtonState();
        }

        private void EnableInteractionAfterSelection()
        {
            _isSelectionInProgress = false;
            if (_selectWorld != null)
            {
                _selectWorld.Interactive = true;
            }
            if (_characterController != null)
            {
                foreach (var player in _characterController.Characters)
                {
                    player.Interactive = true;
                }
                // Labels visibility will be restored by controller's active character logic
                if (_characterController.ActiveCharacter != null)
                {
                    var activePlayer = _characterController.ActiveCharacter;
                    if (_characterController.Labels.TryGetValue(activePlayer, out var label))
                    {
                        label.Visible = true;
                    }
                }
            }
            _selectedCharacterInfo = null;

            if (_loadingScreen != null)
            {
                Controls.Remove(_loadingScreen);
                _loadingScreen.Dispose();
                _loadingScreen = null;
            }

            UpdateNavigationButtonState();
        }

        private void OnCreateCharacterButtonClick(object sender, EventArgs e)
        {
            if (_characterCreationDialog != null)
            {
                // Dialog already open
                return;
            }

            _logger.LogInformation("Opening character creation dialog...");

            // Creation uses the original animated NewFace portrait instead of
            // showing the selected equipped character behind the dialog.
            if (_characterController != null)
                foreach (var player in _characterController.Characters)
                {
                    player.Hidden = true;
                    player.Interactive = false;
                    if (_characterController.Labels.TryGetValue(player, out var label)) label.Visible = false;
                }
            foreach (var button in new[] { _enterGameButton, _deleteCharacterButton, _createCharacterButton, _exitButton })
                if (button != null) button.Enabled = false;

            // Create and show dialog
            _characterCreationDialog = new CharacterCreationDialog();
            _characterCreationDialog.CharacterCreateRequested += OnCharacterCreateRequested;
            _characterCreationDialog.CancelRequested += OnCharacterCreationCancelled;
            
            Controls.Add(_characterCreationDialog);
            _characterCreationDialog.BringToFront();
            Cursor?.BringToFront();

            // Disable interactions with world
            if (_selectWorld != null)
            {
                _selectWorld.Interactive = false;
            }
            if (_createCharacterButton != null)
            {
                _createCharacterButton.Enabled = false;
            }
        }

        private void OnCharacterCreateRequested(object sender, (string Name, CharacterClassNumber Class) data)
        {
            _logger.LogInformation("Character creation requested: Name={Name}, Class={Class}", data.Name, data.Class);

            // Close dialog
            CloseCharacterCreationDialog();

            // Send create character request
            var characterService = _networkManager?.GetCharacterService();
            if (characterService != null)
            {
                _ = characterService.SendCreateCharacterRequestAsync(data.Name, data.Class);
                MessageWindow.Show($"Creating character '{data.Name}'...\nPlease wait for server response.");
                
                // Request updated character list after a short delay
                _ = RefreshCharacterListAfterDelay();
            }
            else
            {
                _logger.LogError("CharacterService not available - cannot create character.");
                MessageWindow.Show("Error: Cannot create character at this time.");
            }
        }

        private async Task RefreshCharacterListAfterDelay()
        {
            // Wait for server to process creation
            await Task.Delay(2000);
            
            _logger.LogInformation("Requesting updated character list after creation...");
            var characterService = _networkManager?.GetCharacterService();
            if (characterService != null)
            {
                await characterService.RequestCharacterListAsync();
                // Note: The character list handler will update the scene
            }
        }

        private void OnCharacterCreationCancelled(object sender, EventArgs e)
        {
            _logger.LogInformation("Character creation cancelled.");
            CloseCharacterCreationDialog();
        }
        
        private void OnControllerCharacterClicked(object sender, string characterName)
        {
            if (_characterDeletionDialog != null || _characterCreationDialog != null) return;
            _logger.LogInformation("Controller: Character '{Name}' clicked.", characterName);
            _currentlySelectedCharacterName = characterName;

            // Find index
            for (int i = 0; i < _characters.Count; i++)
            {
                if (_characters[i].Name == characterName)
                {
                    _currentCharacterIndex = i;
                    break;
                }
            }

            PositionNavigationButtons();
            UpdateNavigationButtonState();
        }

        private void OnControllerCharacterDoubleClicked(object sender, string characterName)
        {
            if (_characterDeletionDialog != null || _characterCreationDialog != null) return;
            _logger.LogInformation("Controller: Character '{Name}' double-clicked.", characterName);
            CharacterSelected(characterName);
        }
        
        private void OnDeleteCharacterButtonClick(object sender, EventArgs e)
        {
            if (_characterDeletionDialog != null || _characterCreationDialog != null || string.IsNullOrEmpty(_currentlySelectedCharacterName)) return;
            string characterToDelete = _currentlySelectedCharacterName;
            var dialog = new CharacterDeletionDialog(characterToDelete);
            _characterDeletionDialog = dialog;
            dialog.DeleteConfirmed += (s, secret) =>
            {
                CloseCharacterDeletionDialog();
                var service = _networkManager?.GetCharacterService();
                if (service == null) { MessageWindow.Show("Cannot delete character at this time."); return; }
                // This is the only request, after all three confirmations. Never log the secret.
                _ = service.SendDeleteCharacterRequestAsync(characterToDelete, secret);
            };
            dialog.CancelRequested += (s, args) => CloseCharacterDeletionDialog();
            Controls.Add(dialog);
            dialog.BringToFront();
            Cursor?.BringToFront();
            if (_selectWorld != null) _selectWorld.Interactive = false;
            PositionNavigationButtons();
        }

        private void CloseCharacterDeletionDialog()
        {
            if (_characterDeletionDialog == null) return;
            var dialog = _characterDeletionDialog;
            _characterDeletionDialog = null;
            Controls.Remove(dialog);
            dialog.Dispose();
            if (_selectWorld != null) _selectWorld.Interactive = true;
            PositionNavigationButtons();
        }

        private void CloseCharacterCreationDialog()
        {
            if (_characterCreationDialog != null)
            {
                _characterCreationDialog.CharacterCreateRequested -= OnCharacterCreateRequested;
                _characterCreationDialog.CancelRequested -= OnCharacterCreationCancelled;
                Controls.Remove(_characterCreationDialog);
                _characterCreationDialog.Dispose();
                _characterCreationDialog = null;
            }

            // Re-enable interactions
            if (_selectWorld != null)
            {
                _selectWorld.Interactive = true;
            }
            if (_characterController != null)
                for (int i = 0; i < _characterController.Characters.Count; i++)
                {
                    var player = _characterController.Characters[i];
                    bool selected = i == _currentCharacterIndex;
                    player.Hidden = !selected;
                    player.Interactive = selected;
                    if (_characterController.Labels.TryGetValue(player, out var label)) label.Visible = selected;
                }
            PositionNavigationButtons();
            UpdateNavigationButtonState();
        }

        public override void Update(GameTime gameTime)
        {
            if (_loadingScreen != null && _loadingScreen.Visible)
            {
                _loadingScreen.Update(gameTime);
                Cursor?.Update(gameTime);
                DebugPanel?.Update(gameTime);
                return;
            }
            if (!_initialLoadComplete && Status == GameControlStatus.Initializing)
            {
                Cursor?.Update(gameTime);
                DebugPanel?.Update(gameTime);
                return;
            }

            // Handle character card mouse interaction
            UpdateCharacterCardInteraction();

            base.Update(gameTime);
        }

        private void UpdateCharacterCardInteraction()
        {
            if (_characterCardRects.Count == 0 || !_initialLoadComplete || Cursor == null || _isSelectionInProgress || _characterCreationDialog != null || _characterDeletionDialog != null)
                return;

            Point mousePos = new Point((int)Cursor.X, (int)Cursor.Y);
            int previousHovered = _hoveredCardIndex;
            _hoveredCardIndex = -1;

            var mouseState = MuGame.Instance.UiMouseState;
            bool mousePressed = mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            bool mouseClicked = mousePressed && !_previousMousePressed;
            _previousMousePressed = mousePressed;

            // Only check cards if mouse is in the character list area
            if (!_characterListRect.Contains(mousePos))
                return;

            // Check if mouse is over any character card
            for (int i = 0; i < _characterCardRects.Count; i++)
            {
                if (_characterCardRects[i].Contains(mousePos))
                {
                    _hoveredCardIndex = i;
                    
                    if (previousHovered != _hoveredCardIndex)
                    {
                                }

                    // Handle click (on mouse release)
                    if (mouseClicked)
                    {
                        if (i < _characters.Count) SelectCharacterByIndex(i);
                        else if (_createCharacterButton.Enabled) OnCreateCharacterButtonClick(this, EventArgs.Empty);
                    }
                    break;
                }
            }

            if (previousHovered != _hoveredCardIndex && previousHovered != -1)
            {
                }
        }

        private void SelectCharacterByIndex(int index)
        {
            if (index < 0 || index >= _characters.Count || _characterController == null)
                return;

            _currentCharacterIndex = index;
            var character = _characters[index];
            _currentlySelectedCharacterName = character.Name;
            _characterController.SetActiveCharacter(_currentCharacterIndex);
            PositionNavigationButtons();
            UpdateNavigationButtonState();

            _logger.LogInformation("Character '{Name}' selected via card click.", character.Name);
        }

        public override void Draw(GameTime gameTime)
        {
            if (_loadingScreen != null && _loadingScreen.Visible)
            {
                GraphicsDevice.Clear(new Color(12, 12, 20));
                DrawBackground();
                _progressBar.Progress = _loadingScreen.Progress;
                _progressBar.StatusText = _loadingScreen.Message;
                _progressBar.Visible = true;
                _progressBar.Draw(gameTime);
                return;
            }

            // Finish all offscreen work before drawing into the main DiscardContents
            // target. The UI pass below only composites textures and never rebinds it.
            EnsureNativeSelectionSurface();
            _characterCreationDialog?.PrepareDraw();
            _characterDeletionDialog?.PrepareDraw();

            // Draw 3D world first
            base.Draw(gameTime);

            // Draw modern UI overlay
            DrawModernUI(gameTime);
        }

        private void DrawModernUI(GameTime gameTime)
        {
            // Draw character info panel
            using (var scope = new SpriteBatchScope(
                GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone,
                null, UiScaler.SpriteTransform))
            {
                var sb = GraphicsManager.Instance.Sprite;
                DrawCharacterPanel(sb);
            }

            // Draw cursor and debug panel on top of everything
            using (var scope = new SpriteBatchScope(
                GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone,
                null, UiScaler.SpriteTransform))
            {
                Cursor?.Draw(gameTime);
                DebugPanel?.Draw(gameTime);
            }
        }

        private void DrawCharacterPanel(SpriteBatch sb) => DrawNativeCharacterSelection(sb);



        private new void DrawBackground()
        {
            if (_backgroundTexture == null) return;

            using var scope = new SpriteBatchScope(
                GraphicsManager.Instance.Sprite, SpriteSortMode.Deferred,
                BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone,
                null, UiScaler.SpriteTransform);

            GraphicsManager.Instance.Sprite.Draw(_backgroundTexture,
                new Rectangle(0, 0, UiScaler.VirtualSize.X, UiScaler.VirtualSize.Y), Color.White);
        }

        private void OnEnterGameButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentlySelectedCharacterName))
            {
                MessageWindow.Show("Please select a character first.");
                return;
            }

            // Enter game with selected character
            var matchedIndex = _characters.FindIndex(c => c.Name.Equals(_currentlySelectedCharacterName, StringComparison.OrdinalIgnoreCase));
            if (matchedIndex < 0)
            {
                _logger.LogWarning("Character '{Name}' not found in character list.", _currentlySelectedCharacterName);
                MessageWindow.Show($"Error: Character '{_currentlySelectedCharacterName}' not found.");
                return;
            }

            _selectedCharacterInfo = _characters[matchedIndex];
            _currentCharacterIndex = matchedIndex;
            _characterController?.SetActiveCharacter(_currentCharacterIndex);

            ClientConnectionState currentState = _networkManager.CurrentState;
            bool canSelect = currentState == ClientConnectionState.ConnectedToGameServer ||
                             currentState == ClientConnectionState.SelectingCharacter;

            if (!canSelect)
            {
                _logger.LogWarning("Character selection attempted but NetworkManager state is not ConnectedToGameServer or SelectingCharacter. State: {State}", currentState);
                MessageWindow.Show($"Cannot select character. Invalid network state: {currentState}");
                _selectedCharacterInfo = null;
                return;
            }

            _logger.LogInformation("Character '{CharacterName}' (Class: {Class}) selected in scene. Sending request...",
                                   _selectedCharacterInfo.Value.Name, _selectedCharacterInfo.Value.Class);

            DisableInteractionDuringSelection(_currentlySelectedCharacterName);
            _ = _networkManager.SendSelectCharacterRequestAsync(_currentlySelectedCharacterName);
        }

        private void OnExitButtonClick(object sender, EventArgs e)
        {
            _logger.LogInformation("Exit button clicked - returning to login.");
            _isIntentionalLogout = true;
            _ = _networkManager.GetCharacterService().SendLogoutRequestAsync(LogOutType.BackToServerSelection);
        }
    }
}
