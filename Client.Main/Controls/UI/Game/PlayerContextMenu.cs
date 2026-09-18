using System;
using Client.Main;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Context menu for remote player interactions.
    /// Triggered by ALT + Right Click on a remote player.
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
        private readonly ButtonControl _storeButton;
        private readonly ButtonControl _whisperButton;

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

            BackgroundColor =
                new Color(
                    30,
                    30,
                    50,
                    240);

            BorderColor =
                new Color(
                    100,
                    100,
                    150,
                    255);

            BorderThickness = 1;
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
                _storeButton);

            Controls.Add(
                _whisperButton);

            // Por defecto no aparece.
            _guildRequestButton.Visible =
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

        public void SetGuildRequestVisible(
            bool visible)
        {
            _guildRequestButton.Visible =
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
                    maxX);

            Y =
                Math.Clamp(
                    Y,
                    0,
                    maxY);

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
                5;

            const int firstY =
                5;

            const int width =
                160;

            const int height =
                28;

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
                _whisperButton);

            int totalHeight =
                y + 5;

            ControlSize =
                new Point(
                    170,
                    totalHeight);

            ViewSize =
                ControlSize;
        }

        private (
            ButtonControl party,
            ButtonControl trade,
            ButtonControl duel,
            ButtonControl guildRequest,
            ButtonControl store,
            ButtonControl whisper)
            CreateButtons()
        {
            const int width =
                160;

            const int height =
                28;

            ButtonControl Make(
                string text)
            {
                return new ButtonControl
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

                    BackgroundColor =
                        new Color(
                            50,
                            50,
                            80,
                            200),

                    HoverBackgroundColor =
                        new Color(
                            80,
                            80,
                            120,
                            220),

                    PressedBackgroundColor =
                        new Color(
                            40,
                            40,
                            70,
                            220),

                    FontSize =
                        12f,

                    TextColor =
                        Color.White
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
                store,
                whisper);
        }
    }
}