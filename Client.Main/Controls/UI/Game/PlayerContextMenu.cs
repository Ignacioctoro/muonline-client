using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Controls.UI.Game.Common;
using Microsoft.Xna.Framework.Graphics;
using Client.Main;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Context menu for remote player interactions.
    /// Triggered by ALT + Right Click or a touch hold on a remote player.
    /// </summary>
    public class PlayerContextMenu : UIControl
    {
        private static readonly ILogger _logger =
            MuGame.AppLoggerFactory?
                .CreateLogger<PlayerContextMenu>();

        private readonly ButtonControl _partyButton;
        private readonly ButtonControl _tradeButton;
        private readonly ButtonControl _duelButton;
        private readonly ButtonControl _guildRequestButton;
        private readonly ButtonControl _allianceRequestButton;
        private readonly ButtonControl _hostilityRequestButton;
        private readonly ButtonControl _storeButton;
        private readonly ButtonControl _whisperButton;
        private readonly ButtonControl _friendButton;

        private static class Theme
        {
            public static readonly Color BgDarkest = new(10, 9, 8);
            public static readonly Color BgDark = new(20, 18, 16);
            public static readonly Color BgMid = new(31, 27, 22);
            public static readonly Color BgLight = new(48, 40, 30);
            public static readonly Color BgLighter = new(64, 53, 36);
            public static readonly Color Gold = new(212, 175, 85);
        }
        private Texture2D _header, _body, _bottom;
        private RenderTarget2D _surface;
        private readonly BlendState _surfaceBlend = new()
        {
            ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        public override bool BlocksPointerInput => true;

        private ushort _targetPlayerId;
        private string _targetPlayerName;

        public event Action<string> WhisperRequested;
        public event Action<ushort, string> DuelRequested;

        public PlayerContextMenu()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    170,
                    150);

            ViewSize =
                ControlSize;

            BackgroundColor = Color.Transparent;
            BorderThickness = 0;
            Interactive = true;
            Visible = false;

            var buttons =
                CreateButtons();

            _partyButton =
                buttons.party;

            _tradeButton =
                buttons.trade;

            _duelButton =
                buttons.duel;

            _guildRequestButton =
                buttons.guildRequest;

            _allianceRequestButton =
                buttons.allianceRequest;

            _hostilityRequestButton =
                buttons.hostilityRequest;

            _storeButton =
                buttons.store;

            _whisperButton =
                buttons.whisper;

            _friendButton = buttons.friend;
            _friendButton.Click += OnAddFriendClicked;
            Controls.Add(_friendButton);

            _partyButton.Click +=
                OnPartyInviteClicked;

            _tradeButton.Click +=
                OnTradeRequestClicked;

            _duelButton.Click +=
                OnDuelRequestClicked;

            _guildRequestButton.Click +=
                OnGuildRequestClicked;
            _allianceRequestButton.Click +=
                OnAllianceRequestClicked;

            _hostilityRequestButton.Click +=
                OnHostilityRequestClicked;

            _storeButton.Click +=
                OnStoreRequestClicked;

            _whisperButton.Click +=
                OnWhisperClicked;

            Controls.Add(
                _partyButton);

            Controls.Add(
                _tradeButton);

            Controls.Add(
                _duelButton);

            Controls.Add(
                _guildRequestButton);

            Controls.Add(
                _allianceRequestButton);

            Controls.Add(
                _hostilityRequestButton);

            Controls.Add(
                _storeButton);

            Controls.Add(
                _whisperButton);

            // Por defecto no aparece.
            _guildRequestButton.Visible =
                false;
            _guildRequestButton.Visible =
                false;

            _allianceRequestButton.Visible =
                false;

            _hostilityRequestButton.Visible =
                false;

            RefreshButtonLayout();
        }

        public void SetTarget(
            ushort playerId,
            string playerName)
        {
            _targetPlayerId =
                playerId;

            _targetPlayerName =
                playerName ??
                string.Empty;
        }

        public ushort TargetPlayerId =>
            _targetPlayerId;

        public string TargetPlayerName =>
            _targetPlayerName;

        public void SetDuelButtonEnabled(
            bool enabled)
        {
            _duelButton.Enabled =
                enabled;
        }

        public void SetStoreButtonEnabled(bool enabled)
        {
            _storeButton.Enabled = enabled;
        }

        public void SetGuildRequestVisible(
            bool visible)
        {
            _guildRequestButton.Visible =
                visible;

            RefreshButtonLayout();
        }
        public void SetAllianceRequestVisible(
            bool visible)
        {
            _allianceRequestButton.Visible =
                visible;

            RefreshButtonLayout();
        }
        public void SetHostilityRequestVisible(
            bool visible)
        {
            _hostilityRequestButton.Visible =
                visible;

            RefreshButtonLayout();
        }

        public void ShowAt(
            int x,
            int y)
        {
            X = x;
            Y = y;

            int maxX =
                UiScaler.VirtualSize.X -
                ViewSize.X;

            int maxY =
                UiScaler.VirtualSize.Y -
                ViewSize.Y;

            X =
                Math.Clamp(
                    X,
                    0,
                    Math.Max(0, maxX));

            Y =
                Math.Clamp(
                    Y,
                    0,
                    Math.Max(0, maxY));

            Visible =
                true;
        }

        private async void OnPartyInviteClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Party invite requested for player: {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                await characterService
                    .SendPartyInviteAsync(
                        _targetPlayerId);
            }

            Visible =
                false;
        }

        private async void OnTradeRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Trade request sent to player: {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                await characterService
                    .SendTradeRequestAsync(
                        _targetPlayerId);
            }

            Visible =
                false;
        }

        private void OnDuelRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Duel request clicked for player: {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            DuelRequested?.Invoke(
                _targetPlayerId,
                _targetPlayerName);

            Visible =
                false;
        }

        private async void OnGuildRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Guild join request sent to Guild Master {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                await characterService
                    .SendGuildJoinRequestAsync(
                        _targetPlayerId);
            }

            Visible =
                false;
        }
        private async void OnAllianceRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Alliance request sent to Guild Master {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                await characterService
                    .SendAllianceRequestAsync(
                        _targetPlayerId);
            }

            Visible =
                false;
        }
        private async void OnHostilityRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Hostility request sent to Guild Master {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                await characterService
                    .SendHostilityRequestAsync(
                        _targetPlayerId);
            }

            Visible =
                false;
        }

        private async void OnStoreRequestClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Requesting store list from player: {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            var characterService =
                MuGame.Network?
                    .GetCharacterService();

            if (characterService != null)
            {
                if (MuGame.Instance?.ActiveScene is Client.Main.Scenes.GameScene scene)
                    scene.ChatLog?.AddMessage("Personal Store",
                        "Requesting store items from OpenMU...",
                        Client.Main.Models.MessageType.System);
                await characterService
                    .SendPlayerStoreListRequestAsync(
                        _targetPlayerId,
                        _targetPlayerName);
            }

            Visible =
                false;
        }

        private void OnAddFriendClicked(object sender, EventArgs e)
        {
            string name = _targetPlayerName;
            Visible = false;
            Scene?.SetMouseInputConsumed();
            var service = MuGame.Network?.GetCharacterService();
            if (service == null || string.IsNullOrWhiteSpace(name)) return;

            // Keep the server's response visible in the friends window.
            if (MuGame.Instance?.ActiveScene is Client.Main.Scenes.GameScene scene &&
                scene.FriendsWindow != null)
            {
                scene.FriendsWindow.Visible = true;
                scene.FriendsWindow.BringToFront();
            }
            _ = service.SendFriendActionAsync(0xC1, name);
        }

        private void OnWhisperClicked(
            object sender,
            EventArgs e)
        {
            _logger?.LogInformation(
                "Starting whisper to player: {Name} (ID: {Id})",
                _targetPlayerName,
                _targetPlayerId);

            WhisperRequested?.Invoke(
                _targetPlayerName);

            Visible =
                false;
        }

        public override bool OnClick()
        {
            base.OnClick();

            return true;
        }

        private void RefreshButtonLayout()
        {
            const int x =
                10;

            const int firstY =
                36;

            const int width =
                150;

            int height = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 24;

            int y =
                firstY;

            void PositionButton(
                ButtonControl button)
            {
                if (!button.Visible)
                    return;

                button.X =
                    x;

                button.Y =
                    y;

                button.ControlSize =
                    new Point(
                        width,
                        height);

                button.ViewSize =
                    button.ControlSize;

                y +=
                    height;
            }

            PositionButton(
                _partyButton);

            PositionButton(
                _tradeButton);

            PositionButton(
                _duelButton);

            PositionButton(
                _guildRequestButton);

            PositionButton(
                _storeButton);

            PositionButton(
                _allianceRequestButton);

            PositionButton(
                _hostilityRequestButton);

            PositionButton(
                _whisperButton);

            PositionButton(_friendButton);

            int totalHeight =
                y + 10;

            ControlSize =
                new Point(
                    170,
                    totalHeight);

            ViewSize =
                ControlSize;
        }

        public override async Task Load()
        {
            _header = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/command_chatip_bg01.ozd");
            _body = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/command_chatip_bg02.ozd");
            _bottom = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/command_chatip_bg03.ozd");
            await base.Load();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!Visible || Status != GameControlStatus.Ready) return;
            // Cache outside Draw so an enclosing UI batch cannot flush into this target.
            EnsureSurface();
        }

        private void EnsureSurface()
        {
            var graphics = GraphicsManager.Instance;
            var sb = graphics.Sprite;
            if (_surface == null || _surface.IsDisposed || _surface.Width != ViewSize.X || _surface.Height != ViewSize.Y)
            {
                _surface?.Dispose();
                var gd = graphics.GraphicsDevice;
                _surface = new RenderTarget2D(gd, ViewSize.X, ViewSize.Y);
                var previous = gd.GetRenderTargets();
                gd.SetRenderTarget(_surface);
                gd.Clear(Color.Transparent);
                using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, _surfaceBlend))
                {
                    if (_body != null) sb.Draw(_body, new Rectangle(0, 42, ViewSize.X, ViewSize.Y - 50), Color.White);
                    if (_header != null) sb.Draw(_header, new Rectangle(0, 0, ViewSize.X, 42), Color.White);
                    if (_bottom != null) sb.Draw(_bottom, new Rectangle(0, ViewSize.Y - 8, ViewSize.X, 8), Color.White);
                }
                gd.SetRenderTargets(previous);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready || _surface == null) return;
            var sb = GraphicsManager.Instance.Sprite;
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend, transform: UiScaler.SpriteTransform))
            {
                sb.Draw(_surface, DisplayRectangle, Color.White);
                var font = GraphicsManager.Instance.Font;
                if (font != null && !string.IsNullOrEmpty(_targetPlayerName))
                {
                    float scale = 13f / Constants.BASE_FONT_SIZE;
                    var size = font.MeasureString(_targetPlayerName) * scale;
                    var bounds = DisplayRectangle;
                    sb.DrawString(font, _targetPlayerName,
                        new Vector2(bounds.Center.X - size.X / 2, bounds.Y + 23 - size.Y / 2),
                        Theme.Gold, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                }
                for (int i = 0; i < Controls.Count; i++) Controls[i].Draw(gameTime);
            }
        }

        public override void Dispose()
        {
            _surface?.Dispose();
            _surfaceBlend.Dispose();
            base.Dispose();
        }

        private sealed class NewUiMenuButton : ButtonControl
        {
            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready) return;
                var sb = GraphicsManager.Instance.Sprite;
                var font = GraphicsManager.Instance.Font;
                if (font == null) return;
                using var scope = new SpriteBatchScope(sb, SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform);
                var bounds = DisplayRectangle;
                float scale = FontSize / Constants.BASE_FONT_SIZE;
                var size = font.MeasureString(Text) * scale;
                var color = !Enabled ? Color.Gray : IsMouseOver || IsMousePressed ? Color.Gold : new Color(220, 220, 210);
                sb.DrawString(font, Text, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2),
                    color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        private (
            ButtonControl party,
            ButtonControl trade,
            ButtonControl duel,
            ButtonControl guildRequest,
            ButtonControl allianceRequest,
            ButtonControl hostilityRequest,
            ButtonControl store,
            ButtonControl whisper,
            ButtonControl friend)
            CreateButtons()
        {
            const int width =
                150;

            int height = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 24;

            ButtonControl Make(
                string text)
            {
                return new NewUiMenuButton
                {
                    Text =
                        text,

                    ControlSize =
                        new Point(
                            width,
                            height),

                    ViewSize =
                        new Point(
                            width,
                            height),

                    AutoViewSize =
                        false,

                    BackgroundColor = Theme.BgDark,
                    HoverBackgroundColor = Theme.BgLight,
                    PressedBackgroundColor = Theme.BgDarkest,
                    FontSize = 11f,
                    TextColor = Theme.Gold,
                    HoverTextColor = new Color(255, 225, 155)
                };
            }

            var party =
                Make(
                    "Party Invite");

            var trade =
                Make(
                    "Trade Request");

            var duel =
                Make(
                    "Duel Request");

            var guildRequest =
                Make(
                    "Request Guild");

            var allianceRequest =
                Make(
                    "Request Alliance");
                    
            var hostilityRequest =
                Make(
                    "Declare Hostility");
                        

            var store =
                Make(
                    "View Store");

            var whisper =
                Make(
                    "Whisper");

            return (
                party,
                trade,
                duel,
                guildRequest,
                allianceRequest,
                hostilityRequest,
                store,
                whisper,
                Make("Add Friend"));
        }
    }
}
