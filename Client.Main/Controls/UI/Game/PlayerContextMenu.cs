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

        private static class Theme
        {
            public static readonly Color BgDarkest = new(10, 9, 8);
            public static readonly Color BgDark = new(20, 18, 16);
            public static readonly Color BgMid = new(31, 27, 22);
            public static readonly Color BgLight = new(48, 40, 30);
            public static readonly Color BgLighter = new(64, 53, 36);
            public static readonly Color Gold = new(212, 175, 85);
        }
        private readonly LabelControl _title;
        private Texture2D _frame;
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
            _title = new LabelControl
            {
                X = 16, Y = 17, ViewSize = new Point(188, 24), AutoViewSize = false,
                FontSize = 13f, TextColor = Theme.Gold, TextAlign = HorizontalAlign.Center,
                Interactive = false
            };
            Controls.Add(_title);
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
            _title.Text = _targetPlayerName;
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
                16;

            const int firstY =
                48;

            const int width =
                188;

            int height = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 34;

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
                    height + 5;
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

            int totalHeight =
                y + 10;

            ControlSize =
                new Point(
                    220,
                    totalHeight);

            ViewSize =
                ControlSize;
        }

        public override async Task Load()
        {
            _frame = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/FriendMenu_I1.ozd");
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
                gd.Clear(Theme.BgDarkest);
                using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, _surfaceBlend))
                {
                    NewUiFrameRenderer.DrawNineSlice(sb, _frame, new Rectangle(0, 0, 529, 297),
                        new Rectangle(0, 0, ViewSize.X, ViewSize.Y), 18, 18, 18, 18);
                    if (_frame != null)
                        sb.Draw(_frame, new Rectangle(12, 12, ViewSize.X - 24, 29),
                            new Rectangle(18, 340, 490, 31), Color.White);
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
            private Texture2D _normal, _hover, _pressed;
            public override async Task Load()
            {
                var loader = TextureLoader.Instance;
                _normal = await loader.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd");
                _hover = await loader.PrepareAndGetTexture("Interface/GFx/btn_l02.ozd");
                _pressed = await loader.PrepareAndGetTexture("Interface/GFx/btn_l03.ozd");
                await base.Load();
                SetTexture(_normal);
            }
            public override void Draw(GameTime gameTime)
            {
                if (!Visible) return;
                SetTexture(!Enabled ? _normal : IsMousePressed ? _pressed ?? _normal : IsMouseOver ? _hover ?? _normal : _normal);
                using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite,
                    SpriteSortMode.Deferred, BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform);
                base.Draw(gameTime);
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
            ButtonControl whisper)
            CreateButtons()
        {
            const int width =
                188;

            int height = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() ? 44 : 34;

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
                    FontSize = 13f,
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
                whisper);
        }
    }
}