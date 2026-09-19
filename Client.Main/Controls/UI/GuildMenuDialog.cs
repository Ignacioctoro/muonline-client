using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using System;

namespace Client.Main.Controls.UI
{
    public class GuildMenuDialog : PopupFieldDialog
    {
        private const int MembersPerPage = 8;
        private const int AllianceRowsCount = 5;

        private readonly string _localPlayerName;
        private readonly ushort _localPlayerId;

        private readonly LabelControl _guildNameLabel;
        private readonly LabelControl _scoreLabel;
        private readonly LabelControl _memberCountLabel;
        private readonly LabelControl _pageLabel;

        private readonly GuildEmblemDisplayControl _emblemControl;

        private readonly LabelButtonSmall _membersTabButton;
        private readonly LabelButtonSmall _relationsTabButton;

        private readonly LabelControl _memberHeaderLabel;
        private readonly LabelControl _rankHeaderLabel;
        private readonly LabelControl _statusHeaderLabel;

        private readonly MemberRow[] _memberRows =
            new MemberRow[MembersPerPage];

        private readonly LabelButtonSmall _previousButton;
        private readonly LabelButtonSmall _nextButton;
        private readonly LabelButton _disbandButton;

        private readonly LabelControl _allianceHeaderLabel;
        private readonly LabelControl _allianceEmptyLabel;
        private readonly AllianceRow[] _allianceRows =
            new AllianceRow[AllianceRowsCount];
        private readonly LabelButton _endAllianceButton;

        private readonly LabelControl _hostilityHeaderLabel;
        private readonly LabelControl _rivalGuildLabel;
        private readonly LabelButton _endHostilityButton;

        private readonly LabelButtonSmall _closeButton;

        private GuildRosterData _roster;
        private AllianceListData _allianceList;
        private GuildMenuTab _activeTab = GuildMenuTab.Members;
        private int _currentPage;

        public event EventHandler DisbandRequested;
        public event EventHandler EndAllianceRequested;
        public event EventHandler EndHostilityRequested;

        public event EventHandler<GuildMemberActionEventArgs>
            KickMemberRequested;

        public event EventHandler<GuildMemberRoleActionEventArgs>
            ChangeRoleRequested;

        public GuildMenuDialog(
            string localPlayerName,
            ushort localPlayerId)
        {
            _localPlayerName =
                localPlayerName ?? string.Empty;

            _localPlayerId =
                localPlayerId;

            Interactive = true;

            ControlSize =
                new Point(
                    460,
                    500);

            // --------------------------------------------------------
            // TÍTULO
            // --------------------------------------------------------

            Controls.Add(
                new LabelControl
                {
                    Text = "Gremio",
                    Align =
                        ControlAlign.HorizontalCenter,
                    Y = 18,
                    FontSize = 14,
                    IsBold = true
                });

            // --------------------------------------------------------
            // EMBLEMA + INFORMACIÓN GENERAL
            // --------------------------------------------------------

            _emblemControl =
                new GuildEmblemDisplayControl
                {
                    X = 35,
                    Y = 52
                };

            Controls.Add(
                _emblemControl);

            _guildNameLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 55,
                    Text = "Gremio: Cargando...",
                    FontSize = 13,
                    IsBold = true,
                    TextColor =
                        new Color(
                            255,
                            220,
                            120)
                };

            Controls.Add(
                _guildNameLabel);

            _scoreLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 80,
                    Text = "Puntaje: --",
                    FontSize = 11
                };

            Controls.Add(
                _scoreLabel);

            _memberCountLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 102,
                    Text = "Miembros: --",
                    FontSize = 11,
                    TextColor =
                        new Color(
                            200,
                            200,
                            200)
                };

            Controls.Add(
                _memberCountLabel);

            // --------------------------------------------------------
            // PESTAÑAS
            // --------------------------------------------------------

            _membersTabButton =
                new LabelButtonSmall
                {
                    X = 125,
                    Y = 128,
                    Label =
                        new LabelControl
                        {
                            Text = "Miembros",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 9
                        }
                };

            _membersTabButton.Click +=
                (sender, args) =>
                {
                    SetActiveTab(
                        GuildMenuTab.Members);
                };

            Controls.Add(
                _membersTabButton);

            _relationsTabButton =
                new LabelButtonSmall
                {
                    X = 235,
                    Y = 128,
                    Label =
                        new LabelControl
                        {
                            Text = "Relaciones",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 9
                        }
                };

            _relationsTabButton.Click +=
                (sender, args) =>
                {
                    SetActiveTab(
                        GuildMenuTab.Relations);
                };

            Controls.Add(
                _relationsTabButton);

            // --------------------------------------------------------
            // PESTAÑA MIEMBROS - HEADERS
            // --------------------------------------------------------

            _memberHeaderLabel =
                new LabelControl
                {
                    X = 35,
                    Y = 165,
                    Text = "Miembro",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                };

            Controls.Add(
                _memberHeaderLabel);

            _rankHeaderLabel =
                new LabelControl
                {
                    X = 155,
                    Y = 165,
                    Text = "Rango",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                };

            Controls.Add(
                _rankHeaderLabel);

            _statusHeaderLabel =
                new LabelControl
                {
                    X = 260,
                    Y = 165,
                    Text = "Estado",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                };

            Controls.Add(
                _statusHeaderLabel);

            const int firstRowY =
                188;

            const int rowHeight =
                25;

            for (int i = 0;
                 i < MembersPerPage;
                 i++)
            {
                int rowY =
                    firstRowY +
                    i * rowHeight;

                var row =
                    new MemberRow(
                        rowY);

                _memberRows[i] =
                    row;

                Controls.Add(
                    row.NameLabel);

                Controls.Add(
                    row.RoleLabel);

                Controls.Add(
                    row.StatusLabel);

                Controls.Add(
                    row.RoleButton);

                Controls.Add(
                    row.KickButton);

                int capturedIndex =
                    i;

                row.RoleButton.Click +=
                    (sender, args) =>
                    {
                        OnRoleButtonClicked(
                            capturedIndex);
                    };

                row.KickButton.Click +=
                    (sender, args) =>
                    {
                        OnKickButtonClicked(
                            capturedIndex);
                    };
            }

            _disbandButton =
                new LabelButton
                {
                    X = 35,
                    Y = 402,
                    Label =
                        new LabelControl
                        {
                            Text = "Disolver gremio",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 10
                        }
                };

            _disbandButton.Click +=
                (sender, args) =>
                {
                    if (!IsLocalPlayerGuildMaster())
                        return;

                    DisbandRequested?.Invoke(
                        this,
                        EventArgs.Empty);
                };

            Controls.Add(
                _disbandButton);

            _previousButton =
                new LabelButtonSmall
                {
                    X = 135,
                    Y = 447,
                    Label =
                        new LabelControl
                        {
                            Text = "<",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 11
                        }
                };

            _previousButton.Click +=
                (sender, args) =>
                {
                    if (_currentPage <= 0)
                        return;

                    _currentPage--;
                    RefreshMemberRows();
                };

            Controls.Add(
                _previousButton);

            _pageLabel =
                new LabelControl
                {
                    X = 213,
                    Y = 454,
                    Text = "1 / 1",
                    FontSize = 10
                };

            Controls.Add(
                _pageLabel);

            _nextButton =
                new LabelButtonSmall
                {
                    X = 260,
                    Y = 447,
                    Label =
                        new LabelControl
                        {
                            Text = ">",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 11
                        }
                };

            _nextButton.Click +=
                (sender, args) =>
                {
                    int pageCount =
                        GetPageCount();

                    if (_currentPage >=
                        pageCount - 1)
                    {
                        return;
                    }

                    _currentPage++;
                    RefreshMemberRows();
                };

            Controls.Add(
                _nextButton);

            // --------------------------------------------------------
            // PESTAÑA RELACIONES
            // --------------------------------------------------------

            _allianceHeaderLabel =
                new LabelControl
                {
                    X = 35,
                    Y = 170,
                    Text = "Alianza",
                    FontSize = 11,
                    IsBold = true,
                    TextColor =
                        new Color(
                            255,
                            220,
                            120),
                    Visible = false
                };

            Controls.Add(
                _allianceHeaderLabel);

            _allianceEmptyLabel =
                new LabelControl
                {
                    X = 45,
                    Y = 195,
                    Text = "Sin alianza activa.",
                    FontSize = 10,
                    TextColor =
                        new Color(
                            160,
                            160,
                            160),
                    Visible = false
                };

            Controls.Add(
                _allianceEmptyLabel);

            for (int i = 0;
                 i < AllianceRowsCount;
                 i++)
            {
                int rowY =
                    195 +
                    i * 25;

                var row =
                    new AllianceRow(
                        rowY);

                _allianceRows[i] =
                    row;

                Controls.Add(
                    row.NameLabel);

                Controls.Add(
                    row.MemberCountLabel);
            }

            _endAllianceButton =
                new LabelButton
                {
                    X = 35,
                    Y = 327,
                    Label =
                        new LabelControl
                        {
                            Text = "Finalizar alianza",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 10
                        },
                    Visible = false
                };

            _endAllianceButton.Click +=
                (sender, args) =>
                {
                    if (!IsLocalPlayerGuildMaster())
                        return;

                    EndAllianceRequested?.Invoke(
                        this,
                        EventArgs.Empty);
                };

            Controls.Add(
                _endAllianceButton);

            _hostilityHeaderLabel =
                new LabelControl
                {
                    X = 35,
                    Y = 370,
                    Text = "Hostilidad",
                    FontSize = 11,
                    IsBold = true,
                    TextColor =
                        new Color(
                            255,
                            120,
                            120),
                    Visible = false
                };

            Controls.Add(
                _hostilityHeaderLabel);

            _rivalGuildLabel =
                new LabelControl
                {
                    X = 45,
                    Y = 395,
                    Text = "Sin hostilidad activa.",
                    FontSize = 10,
                    Visible = false
                };

            Controls.Add(
                _rivalGuildLabel);

            _endHostilityButton =
                new LabelButton
                {
                    X = 250,
                    Y = 402,
                    Label =
                        new LabelControl
                        {
                            Text = "Finalizar hostilidad",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 9
                        },
                    Visible = false
                };

            _endHostilityButton.Click +=
                (sender, args) =>
                {
                    if (!IsLocalPlayerGuildMaster())
                        return;

                    EndHostilityRequested?.Invoke(
                        this,
                        EventArgs.Empty);
                };

            Controls.Add(
                _endHostilityButton);

            // --------------------------------------------------------
            // CERRAR
            // --------------------------------------------------------

            _closeButton =
                new LabelButtonSmall
                {
                    X = 360,
                    Y = 447,
                    Label =
                        new LabelControl
                        {
                            Text = "Cerrar",
                            Align =
                                ControlAlign.HorizontalCenter |
                                ControlAlign.VerticalCenter,
                            FontSize = 10
                        }
                };

            _closeButton.Click +=
                (sender, args) =>
                {
                    Close();
                };

            Controls.Add(
                _closeButton);

            GuildInfoCache.GuildRosterUpdated +=
                OnGuildRosterUpdated;

            GuildInfoCache.AllianceListUpdated +=
                OnAllianceListUpdated;

            LoadCachedGuildInfo();
            SetLoadingState();
            SetActiveTab(
                GuildMenuTab.Members);
        }

        private void LoadCachedGuildInfo()
        {
            if (!GuildInfoCache
                .TryGetGuildForPlayer(
                    _localPlayerId,
                    out GuildInfoData guild))
            {
                _guildNameLabel.Text =
                    "Gremio: --";

                return;
            }

            _guildNameLabel.Text =
                $"Gremio: {guild.GuildName}";

            _emblemControl.SetEmblem(
                guild.Logo);
        }

        private void SetLoadingState()
        {
            _scoreLabel.Text =
                "Puntaje: Cargando...";

            _memberCountLabel.Text =
                "Miembros: Cargando...";

            _currentPage = 0;
            _roster = null;
            _allianceList = null;

            HideRows();
            HideAllianceRows();

            _previousButton.Visible = false;
            _nextButton.Visible = false;
            _pageLabel.Visible = false;
            _disbandButton.Visible = false;
            _endAllianceButton.Visible = false;
            _endHostilityButton.Visible = false;
        }

        private void OnGuildRosterUpdated(
            GuildRosterData roster)
        {
            MuGame.ScheduleOnMainThread(
                () =>
                {
                    ApplyRoster(
                        roster);
                });
        }

        private void OnAllianceListUpdated(
            AllianceListData allianceList)
        {
            MuGame.ScheduleOnMainThread(
                () =>
                {
                    ApplyAllianceList(
                        allianceList);
                });
        }

        private void ApplyRoster(
            GuildRosterData roster)
        {
            _roster =
                roster;

            _currentPage =
                0;

            LoadCachedGuildInfo();

            if (_roster == null ||
                !_roster.IsInGuild)
            {
                _scoreLabel.Text =
                    "Puntaje: --";

                _memberCountLabel.Text =
                    "No perteneces a un gremio";

                HideRows();
                _disbandButton.Visible = false;

                if (_activeTab ==
                    GuildMenuTab.Relations)
                {
                    RefreshRelationsView();
                }

                return;
            }

            _scoreLabel.Text =
                $"Puntaje: {_roster.Score}";

            _memberCountLabel.Text =
                $"Miembros: {_roster.Members.Length}";

            if (_activeTab ==
                GuildMenuTab.Members)
            {
                RefreshMemberRows();
            }
            else
            {
                RefreshRelationsView();
            }
        }

        private void ApplyAllianceList(
            AllianceListData allianceList)
        {
            _allianceList =
                allianceList;

            if (_activeTab ==
                GuildMenuTab.Relations)
            {
                RefreshRelationsView();
            }
        }

        private void SetActiveTab(
            GuildMenuTab tab)
        {
            _activeTab =
                tab;

            bool showMembers =
                tab == GuildMenuTab.Members;

            _memberHeaderLabel.Visible =
                showMembers;

            _rankHeaderLabel.Visible =
                showMembers;

            _statusHeaderLabel.Visible =
                showMembers;

            _allianceHeaderLabel.Visible =
                !showMembers;

            _hostilityHeaderLabel.Visible =
                !showMembers;

            if (showMembers)
            {
                HideRelationsControls();
                RefreshMemberRows();
            }
            else
            {
                HideMemberControls();
                RefreshRelationsView();
            }
        }

        private void RefreshMemberRows()
        {
            HideRows();

            if (_activeTab !=
                GuildMenuTab.Members)
            {
                return;
            }

            if (_roster == null ||
                _roster.Members == null)
            {
                return;
            }

            int memberCount =
                _roster.Members.Length;

            int startIndex =
                _currentPage *
                MembersPerPage;

            bool localIsMaster =
                IsLocalPlayerGuildMaster();

            for (int rowIndex = 0;
                 rowIndex < MembersPerPage;
                 rowIndex++)
            {
                int memberIndex =
                    startIndex +
                    rowIndex;

                if (memberIndex >=
                    memberCount)
                {
                    break;
                }

                GuildMemberData member =
                    _roster.Members[
                        memberIndex];

                MemberRow row =
                    _memberRows[
                        rowIndex];

                row.Member =
                    member;

                row.NameLabel.Text =
                    member.Name;

                row.RoleLabel.Text =
                    GetRoleNameSpanish(
                        member.Role);

                row.StatusLabel.Text =
                    member.IsOnline
                        ? "En línea"
                        : "Desconectado";

                row.StatusLabel.TextColor =
                    member.IsOnline
                        ? new Color(
                            120,
                            255,
                            120)
                        : new Color(
                            160,
                            160,
                            160);

                row.NameLabel.Visible = true;
                row.RoleLabel.Visible = true;
                row.StatusLabel.Visible = true;

                bool isLocalPlayer =
                    string.Equals(
                        member.Name,
                        _localPlayerName,
                        StringComparison.OrdinalIgnoreCase);

                bool canManage =
                    localIsMaster &&
                    !isLocalPlayer;

                row.RoleButton.Visible =
                    canManage;

                row.KickButton.Visible =
                    canManage;
            }

            int pageCount =
                GetPageCount();

            _pageLabel.Text =
                $"{_currentPage + 1} / {pageCount}";

            _pageLabel.Visible =
                pageCount > 1;

            _previousButton.Visible =
                pageCount > 1 &&
                _currentPage > 0;

            _nextButton.Visible =
                pageCount > 1 &&
                _currentPage <
                pageCount - 1;

            _disbandButton.Visible =
                localIsMaster;
        }

        private void RefreshRelationsView()
        {
            HideAllianceRows();

            if (_activeTab !=
                GuildMenuTab.Relations)
            {
                return;
            }

            bool localIsMaster =
                IsLocalPlayerGuildMaster();

            AllianceGuildData[] guilds =
                _allianceList?.Guilds ??
                Array.Empty<AllianceGuildData>();

            bool hasAlliance =
                guilds.Length > 1;

            _allianceEmptyLabel.Visible =
                !hasAlliance;

            if (hasAlliance)
            {
                int visibleCount =
                    Math.Min(
                        guilds.Length,
                        AllianceRowsCount);

                string localGuildName =
                    string.Empty;

                if (GuildInfoCache
                    .TryGetGuildForPlayer(
                        _localPlayerId,
                        out GuildInfoData localGuild))
                {
                    localGuildName =
                        localGuild.GuildName;
                }

                for (int i = 0;
                     i < visibleCount;
                     i++)
                {
                    AllianceGuildData guild =
                        guilds[i];

                    AllianceRow row =
                        _allianceRows[i];

                    bool isLocalGuild =
                        string.Equals(
                            guild.GuildName,
                            localGuildName,
                            StringComparison.OrdinalIgnoreCase);

                    row.NameLabel.Text =
                        isLocalGuild
                            ? $"{guild.GuildName} (tu gremio)"
                            : guild.GuildName;

                    row.MemberCountLabel.Text =
                        $"{guild.MemberCount} miembros";

                    row.NameLabel.Visible = true;
                    row.MemberCountLabel.Visible = true;
                }
            }

            _endAllianceButton.Visible =
                localIsMaster &&
                hasAlliance;

            string rivalGuildName =
                _roster?.RivalGuildName ??
                string.Empty;

            bool hasHostility =
                !string.IsNullOrWhiteSpace(
                    rivalGuildName);

            _rivalGuildLabel.Text =
                hasHostility
                    ? $"Gremio rival: {rivalGuildName}"
                    : "Sin hostilidad activa.";

            _rivalGuildLabel.TextColor =
                hasHostility
                    ? new Color(
                        255,
                        150,
                        150)
                    : new Color(
                        160,
                        160,
                        160);

            _rivalGuildLabel.Visible =
                true;

            _endHostilityButton.Visible =
                localIsMaster &&
                hasHostility;
        }

        private void HideMemberControls()
        {
            HideRows();

            _previousButton.Visible = false;
            _nextButton.Visible = false;
            _pageLabel.Visible = false;
            _disbandButton.Visible = false;
        }

        private void HideRelationsControls()
        {
            HideAllianceRows();
            _allianceEmptyLabel.Visible = false;
            _rivalGuildLabel.Visible = false;
            _endAllianceButton.Visible = false;
            _endHostilityButton.Visible = false;
        }

        private void HideRows()
        {
            foreach (MemberRow row
                     in _memberRows)
            {
                row.Member = null;
                row.NameLabel.Visible = false;
                row.RoleLabel.Visible = false;
                row.StatusLabel.Visible = false;
                row.RoleButton.Visible = false;
                row.KickButton.Visible = false;
            }
        }

        private void HideAllianceRows()
        {
            foreach (AllianceRow row
                     in _allianceRows)
            {
                row.NameLabel.Visible = false;
                row.MemberCountLabel.Visible = false;
            }
        }

        private void OnKickButtonClicked(
            int rowIndex)
        {
            if (!IsLocalPlayerGuildMaster())
                return;

            if (rowIndex < 0 ||
                rowIndex >=
                _memberRows.Length)
            {
                return;
            }

            GuildMemberData member =
                _memberRows[
                    rowIndex]
                .Member;

            if (member == null)
                return;

            if (string.Equals(
                    member.Name,
                    _localPlayerName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            KickMemberRequested?.Invoke(
                this,
                new GuildMemberActionEventArgs(
                    member.Name));
        }

        private void OnRoleButtonClicked(
            int rowIndex)
        {
            if (!IsLocalPlayerGuildMaster())
                return;

            if (rowIndex < 0 ||
                rowIndex >=
                _memberRows.Length)
            {
                return;
            }

            GuildMemberData member =
                _memberRows[
                    rowIndex]
                .Member;

            if (member == null)
                return;

            if (string.Equals(
                    member.Name,
                    _localPlayerName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            byte nextRole =
                GetNextRole(
                    member.Role);

            ChangeRoleRequested?.Invoke(
                this,
                new GuildMemberRoleActionEventArgs(
                    member.Name,
                    (GuildMemberRole)nextRole));
        }

        private static byte GetNextRole(
            byte currentRole)
        {
            return currentRole switch
            {
                0x00 => 0x20,
                0x20 => 0x40,
                0x40 => 0x00,
                _ => 0x00
            };
        }

        private static string GetRoleNameSpanish(
            byte role)
        {
            return role switch
            {
                0x80 => "Maestro de gremio",
                0x40 => "Maestro asistente",
                0x20 => "Maestro de batalla",
                0x00 => "Miembro",
                _ => "Desconocido"
            };
        }

        private bool IsLocalPlayerGuildMaster()
        {
            return GuildInfoCache
                .IsLocalPlayerGuildMaster(
                    _localPlayerName);
        }

        private int GetPageCount()
        {
            int count =
                _roster?.Members?.Length ??
                0;

            if (count <= 0)
                return 1;

            return
                (count +
                 MembersPerPage -
                 1) /
                MembersPerPage;
        }

        public override void Dispose()
        {
            GuildInfoCache.GuildRosterUpdated -=
                OnGuildRosterUpdated;

            GuildInfoCache.AllianceListUpdated -=
                OnAllianceListUpdated;

            base.Dispose();
        }

        private enum GuildMenuTab
        {
            Members,
            Relations
        }

        private sealed class MemberRow
        {
            public GuildMemberData Member;

            public LabelControl NameLabel { get; }
            public LabelControl RoleLabel { get; }
            public LabelControl StatusLabel { get; }
            public LabelButtonVerySmall RoleButton { get; }
            public LabelButtonVerySmall KickButton { get; }

            public MemberRow(
                int y)
            {
                NameLabel =
                    new LabelControl
                    {
                        X = 35,
                        Y = y + 5,
                        Text = string.Empty,
                        FontSize = 9
                    };

                RoleLabel =
                    new LabelControl
                    {
                        X = 155,
                        Y = y + 5,
                        Text = string.Empty,
                        FontSize = 9
                    };

                StatusLabel =
                    new LabelControl
                    {
                        X = 260,
                        Y = y + 5,
                        Text = string.Empty,
                        FontSize = 9
                    };

                RoleButton =
                    new LabelButtonVerySmall
                    {
                        X = 320,
                        Y = y,
                        Label =
                            new LabelControl
                            {
                                Text = "Rango",
                                Align =
                                    ControlAlign.HorizontalCenter |
                                    ControlAlign.VerticalCenter,
                                FontSize = 8
                            },
                        Visible = false
                    };

                KickButton =
                    new LabelButtonVerySmall
                    {
                        X = 380,
                        Y = y,
                        Label =
                            new LabelControl
                            {
                                Text = "Expulsar",
                                Align =
                                    ControlAlign.HorizontalCenter |
                                    ControlAlign.VerticalCenter,
                                FontSize = 7
                            },
                        Visible = false
                    };
            }
        }

        private sealed class AllianceRow
        {
            public LabelControl NameLabel { get; }
            public LabelControl MemberCountLabel { get; }

            public AllianceRow(
                int y)
            {
                NameLabel =
                    new LabelControl
                    {
                        X = 45,
                        Y = y,
                        Text = string.Empty,
                        FontSize = 9,
                        Visible = false
                    };

                MemberCountLabel =
                    new LabelControl
                    {
                        X = 280,
                        Y = y,
                        Text = string.Empty,
                        FontSize = 9,
                        TextColor =
                            new Color(
                                180,
                                180,
                                180),
                        Visible = false
                    };
            }
        }
    }

    public sealed class GuildMemberActionEventArgs
        : EventArgs
    {
        public string PlayerName { get; }

        public GuildMemberActionEventArgs(
            string playerName)
        {
            PlayerName =
                playerName;
        }
    }

    public sealed class GuildMemberRoleActionEventArgs
        : EventArgs
    {
        public string PlayerName { get; }
        public GuildMemberRole Role { get; }

        public GuildMemberRoleActionEventArgs(
            string playerName,
            GuildMemberRole role)
        {
            PlayerName =
                playerName;

            Role =
                role;
        }
    }

    internal sealed class GuildEmblemDisplayControl
        : UIControl
    {
        private const int GridSize =
            8;

        private const int PixelSize =
            7;

        private readonly byte[,]
            _pixels =
                new byte[
                    GridSize,
                    GridSize];

        private readonly Color[]
            _palette =
            {
                new Color(0, 0, 0),
                new Color(255, 255, 255),
                new Color(255, 0, 0),
                new Color(0, 255, 0),
                new Color(0, 0, 255),
                new Color(255, 255, 0),
                new Color(255, 128, 0),
                new Color(128, 0, 255),
                new Color(0, 255, 255),
                new Color(255, 0, 255),
                new Color(128, 128, 128),
                new Color(192, 192, 192),
                new Color(128, 0, 0),
                new Color(0, 128, 0),
                new Color(0, 0, 128),
                new Color(180, 120, 60)
            };

        public GuildEmblemDisplayControl()
        {
            Interactive =
                false;

            AutoViewSize =
                false;

            ViewSize =
                new Point(
                    GridSize *
                    PixelSize,
                    GridSize *
                    PixelSize);

            ControlSize =
                ViewSize;
        }

        public void SetEmblem(
            byte[] emblem)
        {
            if (emblem == null ||
                emblem.Length < 32)
            {
                Clear();
                return;
            }

            for (int y = 0;
                 y < GridSize;
                 y++)
            {
                for (int x = 0;
                     x < GridSize;
                     x++)
                {
                    int byteIndex =
                        y * 4 +
                        x / 2;

                    byte value =
                        emblem[
                            byteIndex];

                    byte colorIndex;

                    if ((x & 1) == 0)
                    {
                        colorIndex =
                            (byte)(
                                (value >> 4) &
                                0x0F);
                    }
                    else
                    {
                        colorIndex =
                            (byte)(
                                value &
                                0x0F);
                    }

                    _pixels[x, y] =
                        colorIndex;
                }
            }
        }

        private void Clear()
        {
            for (int y = 0;
                 y < GridSize;
                 y++)
            {
                for (int x = 0;
                     x < GridSize;
                     x++)
                {
                    _pixels[x, y] =
                        0;
                }
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible)
                return;

            var sprite =
                GraphicsManager
                    .Instance
                    .Sprite;

            Rectangle background =
                new Rectangle(
                    DisplayRectangle.X - 3,
                    DisplayRectangle.Y - 3,
                    ControlSize.X + 6,
                    ControlSize.Y + 6);

            sprite.Draw(
                GraphicsManager.Instance.Pixel,
                background,
                new Color(
                    20,
                    20,
                    20,
                    220));

            for (int y = 0;
                 y < GridSize;
                 y++)
            {
                for (int x = 0;
                     x < GridSize;
                     x++)
                {
                    Rectangle rect =
                        new Rectangle(
                            DisplayRectangle.X +
                            x * PixelSize,

                            DisplayRectangle.Y +
                            y * PixelSize,

                            PixelSize,
                            PixelSize);

                    sprite.Draw(
                        GraphicsManager.Instance.Pixel,
                        rect,
                        _palette[
                            _pixels[x, y]]);
                }
            }

            base.Draw(
                gameTime);
        }
    }
}