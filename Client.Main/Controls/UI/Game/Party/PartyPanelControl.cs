using System.Linq;
using System.Threading.Tasks;
using Client.Main.Core.Client;
using Client.Main.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Party
{
    public class PartyPanelControl : UIControl
    {
        private const int VerticalSpacing = 3;
        private const double UpdateIntervalSeconds = 5.0;

        private PartyManager _partyManager;
        private readonly ILogger<PartyPanelControl> _logger;
        private CharacterState _characterState;

        private double _timeSinceLastUpdate = 0; // Timer

        public PartyPanelControl()
        {
            _logger = MuGame.AppLoggerFactory.CreateLogger<PartyPanelControl>();
            Align = ControlAlign.Top | ControlAlign.Right;
            AutoViewSize = false;
            ViewSize = new Point(PartyMemberControl.RowWidth, 22);
            Margin = new Margin { Top = 12, Right = 10 };
            Controls.Add(new TextureControl
            {
                Name = "PartyHeader", AutoViewSize = false, ViewSize = new Point(PartyMemberControl.RowWidth, 20),
                TexturePath = "Interface/GFx/party_gragbar.ozd"
            });
            Controls.Add(new LabelControl { Text = "Party", X = 70, Y = 3, FontSize = 10, TextColor = Color.Gold });
            Visible = false;
        }

        public override async Task Load()
        {
            _partyManager = MuGame.Network.GetPartyManager();
            _characterState = MuGame.Network.GetCharacterState();

            if (_partyManager != null)
            {
                _partyManager.PartyUpdated += OnPartyUpdated;
            }
            await base.Load();
            if (_partyManager != null) MuGame.ScheduleOnMainThread(OnPartyUpdated);
        }

        private void OnPartyUpdated()
        {
            _logger.LogDebug("PartyPanelControl.OnPartyUpdated fired.");
            var members = _partyManager.GetPartyMembers();
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                if (Controls[i] is PartyMemberControl row &&
                    !members.Any(m => m.Index == row.MemberInfo.Index && m.Name == row.MemberInfo.Name))
                { row.Dispose(); Controls.Remove(row); }
            }
            Visible = members.Count > 1;
            int currentY = 22;
            foreach (var member in members)
            {
                var row = Controls.OfType<PartyMemberControl>().FirstOrDefault(c => c.MemberInfo.Index == member.Index);
                if (row == null)
                {
                    row = new PartyMemberControl();
                    row.UpdateData(member, IsCurrentPlayer(member));
                    Controls.Add(row);
                }
                else row.UpdateData(member, IsCurrentPlayer(member));
                row.Y = currentY;
                currentY += PartyMemberControl.RowHeight + VerticalSpacing;
            }
            ViewSize = new Point(PartyMemberControl.RowWidth, currentY);
        }

        private bool IsCurrentPlayer(PartyMemberInfo member)
        {
            if (_characterState == null)
                return false;

            if (!string.IsNullOrEmpty(member.Name) && !string.IsNullOrEmpty(_characterState.Name))
            {
                return string.Equals(member.Name, _characterState.Name, System.StringComparison.OrdinalIgnoreCase);
            }

            if (member.Id != 0 && _characterState.Id != 0)
            {
                return member.Id == _characterState.Id;
            }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Visible)
            {
                _timeSinceLastUpdate = 0;
                return;
            }

            _timeSinceLastUpdate += gameTime.ElapsedGameTime.TotalSeconds;

            if (_timeSinceLastUpdate >= UpdateIntervalSeconds)
            {
                _timeSinceLastUpdate = 0;
                _logger.LogTrace("PartyPanel timer elapsed. Requesting party list update.");

                var characterService = MuGame.Network?.GetCharacterService();
                if (characterService != null)
                {
                    _ = characterService.RequestPartyListAsync();
                }
            }
        }

        public override void Dispose()
        {
            if (_partyManager != null)
            {
                _partyManager.PartyUpdated -= OnPartyUpdated;
            }
            base.Dispose();
        }
    }
}
