using System;
using Client.Main.Core.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Scenes
{
    internal sealed class GameScenePlayerMenuController
    {
        private const double PlayerMenuHintCooldownSeconds = 0.3;

        private readonly GameScene _scene;
        private readonly Action<string> _whisperRequested;
        private readonly Action<ushort, string> _duelRequested;

        private PlayerContextMenu _playerContextMenu;
        private LabelControl _playerMenuHint;
        private double _playerMenuHoverTime;
        private ushort? _playerMenuHoverId;
        private double _playerMenuHintCooldown;
        private float _playerMenuHintAlpha;
        private readonly PlayerMenuHoldGesture _touchHold = new();
        private bool _ignoreOpeningRelease;
        private PlayerCommand? _pendingCommand;
        private bool _commandPointerCaptured;
        private PlayerObject _followTarget;
        private double _followElapsed;
        private ushort _followMap;

        public void BeginCommand(PlayerCommand action)
        {
            HideMenu();
            if (_followTarget != null) _scene.Hero?.StopMovement();
            _followTarget = null;
            _pendingCommand = action;
            _scene.CommandWindow?.SetHint("Toca un jugador para aplicar la acción.");
        }

        public void CancelCommand()
        {
            _pendingCommand = null;
            _commandPointerCaptured = false;
            _scene.CommandWindow?.SetHint("Elige una acción y luego un jugador.");
        }

        private void ExecuteCommand(PlayerCommand action, PlayerObject target)
        {
            if (action == PlayerCommand.Follow)
            {
                _followTarget = target;
                _followMap = MuGame.Network.GetCharacterState().MapId;
                _followElapsed = 0.5;
                _scene.CommandWindow?.SetHint("Siguiendo a " + target.Name);
                return;
            }
            if (action == PlayerCommand.SuspendHostilities)
            {
                if (GuildInfoCache.IsPlayerGuildMaster(_scene.Hero.NetworkId))
                {
                    ushort localId = _scene.Hero.NetworkId;
                    RequestDialog.Show("¿Seguro que quieres finalizar la hostilidad?",
                        onAccept: () => { _ = MuGame.Network.GetCharacterService().SendEndHostilityRequestAsync(localId); },
                        onReject: () => { });
                }
                else
                    _scene.CommandWindow?.SetHint("Solo el Guild Master puede hacerlo.");
                return;
            }
            ShowPlayerContextMenu(target, Point.Zero, false);
            bool applied = _playerContextMenu.TryExecuteCommand(action);
            _scene.CommandWindow?.SetHint(applied ? "Acción enviada para " + target.Name :
                "Este jugador no cumple los requisitos.");
        }

        private void UpdateFollow(GameTime time, KeyboardState keyboard, MouseState mouse, MouseState previous)
        {
            if (_followTarget == null) return;
            var hero = _scene.Hero;
            var state = MuGame.Network?.GetCharacterState();
            bool manual = _scene.IsMobileJoystickActive ||
                (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released &&
                 !_scene.IsInteractiveUIUnderMouse() && !_commandPointerCaptured) ||
                (mouse.RightButton == ButtonState.Pressed && previous.RightButton == ButtonState.Released) ||
                keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.Down) ||
                keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right) ||
                keyboard.IsKeyDown(Keys.Escape);
            if (manual)
            {
                // A ground click/joystick has already supplied a new movement
                // intent this frame. Stop only for cancellation keys/right click.
                if (keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.Up) ||
                    keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.Left) ||
                    keyboard.IsKeyDown(Keys.Right) || mouse.RightButton == ButtonState.Pressed)
                    hero?.StopMovement();
                _followTarget = null;
                _scene.CommandWindow?.SetHint("Seguimiento cancelado.");
                return;
            }
            if (hero == null || hero.IsDead || _followTarget.IsDead || _followTarget.World != _scene.World ||
                !IsPlayerAvailable(_followTarget.NetworkId, _followTarget) || state == null || state.MapId != _followMap ||
                state.IsTeleporting || !state.IsInGame)
            {
                _followTarget = null;
                hero?.StopMovement();
                return;
            }
            if (_scene.PauseMenu?.Visible == true ||
                Client.Main.Controls.UI.Game.Inventory.InventoryControl.Instance.Visible)
            {
                hero.StopMovement();
                return;
            }
            _followElapsed += time.ElapsedGameTime.TotalSeconds;
            if (_followElapsed < 0.5) return;
            _followElapsed = 0;
            float distance = Vector2.DistanceSquared(hero.Location, _followTarget.Location);
            if (distance > 400) { _followTarget = null; hero.StopMovement(); return; }
            if (distance <= 4) { hero.StopMovement(); return; }
            // Normal walk/pathfinding packets; never teleport or move the target.
            hero.MoveTo(_followTarget.Location);
        }


        public GameScenePlayerMenuController(GameScene scene, Action<string> whisperRequested, Action<ushort, string> duelRequested)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _whisperRequested = whisperRequested;
            _duelRequested = duelRequested;
        }

        public void Initialize()
        {
            if (_playerMenuHint != null)
                return;

            _playerMenuHint = new LabelControl
            {
                Text = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
                    ? "Hold on player: player menu" : "Hold LMB / ALT + RMB: player menu",
                FontSize = 11f,
                Padding = new Margin { Left = 6, Right = 6, Top = 4, Bottom = 4 },
                BackgroundColor = new Color(0, 0, 0, 180),
                TextColor = Color.White,
                HasShadow = true,
                Visible = false,
                Interactive = false
            };
            _scene.Controls.Add(_playerMenuHint);
            _playerMenuHint.BringToFront();
        }

        public bool IsMenuVisible => _playerContextMenu?.Visible == true;

        public void HideMenu()
        {
            if (_playerContextMenu?.Visible == true)
            {
                _playerContextMenu.Visible = false;
                _playerMenuHintCooldown = PlayerMenuHintCooldownSeconds;
            }
        }

        public void ResetOnWorldUnavailable()
        {
            CancelCommand();
            _followTarget = null;
            if (_playerContextMenu?.Visible == true)
            {
                _playerContextMenu.Visible = false;
            }
            if (_playerMenuHint?.Visible == true)
            {
                _playerMenuHint.Visible = false;
            }
            _playerMenuHoverTime = 0;
            _playerMenuHoverId = null;
            _playerMenuHintCooldown = 0;
            _playerMenuHintAlpha = 0;
            _touchHold.Reset();
            _ignoreOpeningRelease = false;
        }

        // Called after world picking, before click-to-move/attacks can use the touch.
        public bool HandlePlayerTouch(GameTime gameTime)
        {
            bool hasTouch = TouchInputRouter.TryGetPointerTouch(out var touch);
            var mouse = MuGame.Instance.UiMouseState;
            var previousMouse = MuGame.Instance.PrevUiMouseState;
            bool desktopMouse = !hasTouch && !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS();
            bool down = hasTouch
                ? touch.State is TouchLocationState.Pressed or TouchLocationState.Moved
                : desktopMouse && mouse.LeftButton == ButtonState.Pressed;
            bool pressed = hasTouch ? touch.State == TouchLocationState.Pressed
                : down && previousMouse.LeftButton == ButtonState.Released;
            int? pointer = hasTouch ? touch.Id : desktopMouse ? -1 : null;
            var keyboard = MuGame.Instance.Keyboard;
            bool pvpModifier = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            var position = mouse.Position;
            var player = _scene.MouseHoverObject as PlayerObject;
            ushort? target = player != null && player != _scene.Hero && player.World == _scene.World &&
                IsPlayerAvailable(player.NetworkId) ? player.NetworkId : null;
            var state = MuGame.Network?.GetCharacterState();
            bool duelClick = desktopMouse && state?.IsDuelActive == true && target.HasValue &&
                state.GetDuelPlayerId(Client.Main.Core.Client.CharacterState.DuelPlayerType.Enemy) == target.Value;
            // Preserve Ctrl-click/duel attacks and exclude the mouse-driven test joystick.
            if (_commandPointerCaptured)
            {
                _scene.SetMouseInputConsumed();
                if (!down) _commandPointerCaptured = false;
                return true;
            }
            if (_pendingCommand.HasValue && pressed &&
                !_scene.IsMouseInputConsumedThisFrame && !_scene.IsInteractiveUIUnderMouse())
            {
                _commandPointerCaptured = true;
                _scene.SetMouseInputConsumed();
                _touchHold.Reset();
                if (target.HasValue && player != null && !player.IsDead)
                {
                    var action = _pendingCommand.Value;
                    _pendingCommand = null;
                    ExecuteCommand(action, player);
                }
                else
                    _scene.CommandWindow?.SetHint("Selecciona otro jugador cercano.");
                return true;
            }
            bool consumed = _touchHold.Update(pointer, pressed, down,
                target, pvpModifier || duelClick || _scene.IsMobileJoystickCapturingMouse ||
                    _scene.IsMouseInputConsumedThisFrame || _scene.IsInteractiveUIUnderMouse(),
                position.X, position.Y, gameTime.ElapsedGameTime.TotalSeconds, out bool openMenu);
            if (consumed) _scene.SetMouseInputConsumed();
            if (openMenu && player != null)
            {
                ShowPlayerContextMenu(player, position);
                _ignoreOpeningRelease = true;
            }
            return consumed;
        }

        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState uiMouse, MouseState prevUiMouse)
        {
            UpdateFollow(gameTime, keyboardState, uiMouse, prevUiMouse);
            bool contextMenuOpened = false;

            if (!_scene.IsMouseInputConsumedThisFrame &&
                _scene.MouseHoverObject is PlayerObject hoveredPlayer &&
                hoveredPlayer != _scene.Hero)
            {
                bool shopAdvertised = MuGame.Network?.GetScopeManager()?
                    .TryGetPlayerShop(hoveredPlayer.NetworkId, out _) == true;
                bool altPressed = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
                bool rightClickReleased = prevUiMouse.RightButton == ButtonState.Pressed &&
                                          uiMouse.RightButton == ButtonState.Released;

                // Season 6: right click a selling player to view their context menu.
                // Keep ALT+right click for other player interactions.
                if (rightClickReleased && (altPressed || shopAdvertised))
                {
                    ShowPlayerContextMenu(hoveredPlayer, uiMouse.Position);
                    _scene.SetMouseInputConsumed();
                    contextMenuOpened = true;
                }
            }

            if (_playerContextMenu?.Visible == true && !contextMenuOpened)
            {
                bool clickReleasedOutside =
                    ((prevUiMouse.LeftButton == ButtonState.Pressed && uiMouse.LeftButton == ButtonState.Released) ||
                     (prevUiMouse.RightButton == ButtonState.Pressed && uiMouse.RightButton == ButtonState.Released))
                    && !_playerContextMenu.IsMouseOver;

                bool targetMissing = !IsPlayerAvailable(_playerContextMenu.TargetPlayerId);

                if ((!_ignoreOpeningRelease && clickReleasedOutside) || targetMissing)
                {
                    _playerContextMenu.Visible = false;
                    _playerMenuHintCooldown = PlayerMenuHintCooldownSeconds;
                }
            }

            if (_ignoreOpeningRelease && uiMouse.LeftButton == ButtonState.Released)
                _ignoreOpeningRelease = false;

            UpdatePlayerMenuHint(gameTime, keyboardState, _scene.MouseHoverObject as PlayerObject, uiMouse.Position);
        }

        private void ShowPlayerContextMenu(PlayerObject targetPlayer, Point mousePos, bool show = true)
        {
            if (_playerContextMenu == null)
            {
                _playerContextMenu = new PlayerContextMenu();
                _scene.Controls.Add(_playerContextMenu);
                _playerContextMenu.WhisperRequested += _whisperRequested;
                _playerContextMenu.DuelRequested += _duelRequested;
            }

            _playerContextMenu.SetTarget(targetPlayer.NetworkId, targetPlayer.Name);
            _playerContextMenu.SetStoreButtonEnabled(
                MuGame.Network?.GetScopeManager()?.TryGetPlayerShop(targetPlayer.NetworkId, out _) == true);
            _playerContextMenu.SetDuelButtonEnabled(true);
            bool localHasGuild =
                GuildInfoCache.PlayerHasGuild(
                    _scene.Hero.NetworkId);

            bool targetHasGuild =
                GuildInfoCache.PlayerHasGuild(
                    targetPlayer.NetworkId);

            bool targetIsGuildMaster =
                GuildInfoCache.IsPlayerGuildMaster(
                    targetPlayer.NetworkId);

            bool canRequestGuild =
                !localHasGuild &&
                targetHasGuild &&
                targetIsGuildMaster;

            bool localIsGuildMaster =
                GuildInfoCache.IsPlayerGuildMaster(
                    _scene.Hero.NetworkId);

            bool differentGuilds =
                false;

            bool sameAlliance =
                false;

            if (GuildInfoCache.TryGetPlayerGuildId(
                    _scene.Hero.NetworkId,
                    out uint localGuildId) &&
                GuildInfoCache.TryGetPlayerGuildId(
                    targetPlayer.NetworkId,
                    out uint targetGuildId))
            {
                differentGuilds =
                    localGuildId != 0 &&
                    targetGuildId != 0 &&
                    localGuildId != targetGuildId;

                if (differentGuilds &&
                    GuildInfoCache.TryGetGuildForPlayer(
                        _scene.Hero.NetworkId,
                        out GuildInfoData localGuild) &&
                    GuildInfoCache.TryGetGuildForPlayer(
                        targetPlayer.NetworkId,
                        out GuildInfoData targetGuild) &&
                    GuildInfoCache.CurrentAllianceList is
                        { Guilds: { } allianceGuilds })
                {
                    bool localInAlliance =
                        false;

                    bool targetInAlliance =
                        false;

                    foreach (AllianceGuildData allianceGuild
                        in allianceGuilds)
                    {
                        if (string.Equals(
                                allianceGuild.GuildName,
                                localGuild.GuildName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            localInAlliance =
                                true;
                        }

                        if (string.Equals(
                                allianceGuild.GuildName,
                                targetGuild.GuildName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            targetInAlliance =
                                true;
                        }
                    }

                    sameAlliance =
                        localInAlliance &&
                        targetInAlliance;
                }
            }

            bool canRequestAlliance =
                localHasGuild &&
                targetHasGuild &&
                localIsGuildMaster &&
                targetIsGuildMaster &&
                differentGuilds &&
                !sameAlliance;

            bool canRequestHostility =
                localHasGuild &&
                targetHasGuild &&
                localIsGuildMaster &&
                targetIsGuildMaster &&
                differentGuilds &&
                !sameAlliance;

            _playerContextMenu
                .SetGuildRequestVisible(
                    canRequestGuild);

            _playerContextMenu
                .SetAllianceRequestVisible(
                    canRequestAlliance);

            _playerContextMenu
                .SetHostilityRequestVisible(
                    canRequestHostility);
                    
            if (show)
            {
                _playerContextMenu.ShowAt(mousePos.X, mousePos.Y);
                _playerContextMenu.BringToFront();
            }
            else _playerContextMenu.Visible = false;
            _playerMenuHintCooldown = PlayerMenuHintCooldownSeconds;
            _playerMenuHintAlpha = 0;
            if (_playerMenuHint != null)
            {
                _playerMenuHint.Visible = false;
            }
        }

        private void UpdatePlayerMenuHint(GameTime gameTime, KeyboardState keyboardState, PlayerObject hoveredPlayer, Point mousePos)
        {
            const double hintDelaySeconds = 0.35;
            double dt = gameTime.ElapsedGameTime.TotalSeconds;

            if (_playerMenuHintCooldown > 0)
            {
                _playerMenuHintCooldown = Math.Max(0, _playerMenuHintCooldown - dt);
            }

            bool ctrlDown = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            bool menuOpen = _playerContextMenu?.Visible == true;
            bool playerValid = hoveredPlayer != null && hoveredPlayer != _scene.Hero && hoveredPlayer.World == _scene.World;
            bool shouldShow = !_scene.IsInteractiveUIUnderMouse() && playerValid && !ctrlDown && !menuOpen && _playerMenuHintCooldown <= 0;

            if (!playerValid)
            {
                _playerMenuHoverId = null;
                _playerMenuHoverTime = 0;
            }

            if (shouldShow && hoveredPlayer != null && _playerMenuHoverId == hoveredPlayer.NetworkId)
            {
                _playerMenuHoverTime += gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (hoveredPlayer != null)
            {
                _playerMenuHoverId = hoveredPlayer.NetworkId;
                _playerMenuHoverTime = 0;
            }

            bool pastDelay = _playerMenuHoverTime >= hintDelaySeconds;
            float targetAlpha = (shouldShow && pastDelay) ? 1f : 0f;
            _playerMenuHintAlpha = MathHelper.Lerp(_playerMenuHintAlpha, targetAlpha, (float)Math.Min(1, dt * 8));

            if (_playerMenuHint == null)
            {
                return;
            }

            _playerMenuHint.Alpha = _playerMenuHintAlpha;

            if (_playerMenuHintAlpha < 0.01f)
            {
                _playerMenuHint.Visible = false;
                return;
            }

            var hintPosition = new Point(mousePos.X + 14, mousePos.Y + 18);
            int hintWidth = _playerMenuHint.ControlSize.X + _playerMenuHint.Padding.Left + _playerMenuHint.Padding.Right;
            int hintHeight = _playerMenuHint.ControlSize.Y + _playerMenuHint.Padding.Top + _playerMenuHint.Padding.Bottom;
            _playerMenuHint.ViewSize = new Point(hintWidth, hintHeight);

            int maxX = UiScaler.VirtualSize.X - hintWidth - 4;
            int maxY = UiScaler.VirtualSize.Y - hintHeight - 4;
            _playerMenuHint.X = Math.Clamp(hintPosition.X, 4, Math.Max(4, maxX));
            _playerMenuHint.Y = Math.Clamp(hintPosition.Y, 4, Math.Max(4, maxY));
            _playerMenuHint.Visible = true;
            _playerMenuHint.BringToFront();
        }

        private bool IsPlayerAvailable(ushort playerId, PlayerObject expected = null)
        {
            if (playerId == 0 || _scene.World is not WalkableWorldControl walkableWorld)
                return false;

            if (walkableWorld.WalkerObjectsById.TryGetValue(playerId, out var walker) && walker is PlayerObject po)
            {
                return po.World == _scene.World && (expected == null || ReferenceEquals(po, expected));
            }

            return false;
        }
    }
}
