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

        private readonly string _localPlayerName;
        private readonly ushort _localPlayerId;

        private readonly LabelControl _guildNameLabel;
        private readonly LabelControl _scoreLabel;
        private readonly LabelControl _memberCountLabel;
        private readonly LabelControl _pageLabel;

        private readonly GuildEmblemDisplayControl _emblemControl;

        private readonly MemberRow[] _memberRows =
            new MemberRow[MembersPerPage];

        private readonly LabelButtonSmall _previousButton;
        private readonly LabelButtonSmall _nextButton;

        private readonly LabelButton _disbandButton;
        private readonly LabelButtonSmall _closeButton;

        private GuildRosterData _roster;

        private int _currentPage;

        public event EventHandler DisbandRequested;

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
                    410);

            // --------------------------------------------------------
            // TÍTULO
            // --------------------------------------------------------

            Controls.Add(
                new LabelControl
                {
                    Text = "Guild",
                    Align =
                        ControlAlign.HorizontalCenter,
                    Y = 18,
                    FontSize = 14,
                    IsBold = true
                });

            // --------------------------------------------------------
            // EMBLEMA
            // --------------------------------------------------------

            _emblemControl =
                new GuildEmblemDisplayControl
                {
                    X = 35,
                    Y = 52
                };

            Controls.Add(
                _emblemControl);

            // --------------------------------------------------------
            // NOMBRE
            // --------------------------------------------------------

            _guildNameLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 55,
                    Text = "Guild: Loading...",
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

            // --------------------------------------------------------
            // SCORE
            // --------------------------------------------------------

            _scoreLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 80,
                    Text = "Score: --",
                    FontSize = 11
                };

            Controls.Add(
                _scoreLabel);

            // --------------------------------------------------------
            // MEMBER COUNT
            // --------------------------------------------------------

            _memberCountLabel =
                new LabelControl
                {
                    X = 120,
                    Y = 102,
                    Text = "Members: --",
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
            // HEADERS
            // --------------------------------------------------------

            Controls.Add(
                new LabelControl
                {
                    X = 35,
                    Y = 140,
                    Text = "Member",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                });

            Controls.Add(
                new LabelControl
                {
                    X = 155,
                    Y = 140,
                    Text = "Rank",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                });

            Controls.Add(
                new LabelControl
                {
                    X = 260,
                    Y = 140,
                    Text = "Status",
                    FontSize = 10,
                    IsBold = true,
                    TextColor =
                        new Color(
                            220,
                            220,
                            220)
                });

            // --------------------------------------------------------
            // FILAS
            // --------------------------------------------------------

            const int firstRowY =
                164;

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

            // --------------------------------------------------------
            // PAGINACIÓN
            // --------------------------------------------------------

            _previousButton =
                new LabelButtonSmall
                {
                    X = 135,
                    Y = 365,

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
                    Y = 372,
                    Text = "1 / 1",
                    FontSize = 10
                };

            Controls.Add(
                _pageLabel);

            _nextButton =
                new LabelButtonSmall
                {
                    X = 260,
                    Y = 365,

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
            // DISBAND
            // --------------------------------------------------------

            _disbandButton =
                new LabelButton
                {
                    X = 35,
                    Y = 330,

                    Label =
                        new LabelControl
                        {
                            Text =
                                "Disband Guild",

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

            // --------------------------------------------------------
            // CLOSE
            // --------------------------------------------------------

            _closeButton =
                new LabelButtonSmall
                {
                    X = 360,
                    Y = 365,

                    Label =
                        new LabelControl
                        {
                            Text = "Close",

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

            // --------------------------------------------------------
            // ESCUCHAR ACTUALIZACIONES DEL ROSTER
            // --------------------------------------------------------

            GuildInfoCache.GuildRosterUpdated +=
                OnGuildRosterUpdated;

            LoadCachedGuildInfo();

            SetLoadingState();
        }

        private void LoadCachedGuildInfo()
        {
            if (!GuildInfoCache
                .TryGetGuildForPlayer(
                    _localPlayerId,
                    out GuildInfoData guild))
            {
                _guildNameLabel.Text =
                    "Guild: --";

                return;
            }

            _guildNameLabel.Text =
                $"Guild: {guild.GuildName}";

            _emblemControl.SetEmblem(
                guild.Logo);
        }

        private void SetLoadingState()
        {
            _scoreLabel.Text =
                "Score: Loading...";

            _memberCountLabel.Text =
                "Members: Loading...";

            _currentPage = 0;

            HideRows();

            _previousButton.Visible =
                false;

            _nextButton.Visible =
                false;

            _pageLabel.Visible =
                false;

            _disbandButton.Visible =
                false;
        }

        private void OnGuildRosterUpdated(
            GuildRosterData roster)
        {
            // La respuesta de red puede llegar fuera
            // del thread principal.
            MuGame.ScheduleOnMainThread(
                () =>
                {
                    ApplyRoster(
                        roster);
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
                    "Score: --";

                _memberCountLabel.Text =
                    "Not in a Guild";

                HideRows();

                _disbandButton.Visible =
                    false;

                return;
            }

            _scoreLabel.Text =
                $"Score: {_roster.Score}";

            _memberCountLabel.Text =
                $"Members: {_roster.Members.Length}";

            RefreshMemberRows();
        }

        private void RefreshMemberRows()
        {
            HideRows();

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
                    member.RoleName;

                row.StatusLabel.Text =
                    member.IsOnline
                        ? "Online"
                        : "Offline";

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

                row.NameLabel.Visible =
                    true;

                row.RoleLabel.Visible =
                    true;

                row.StatusLabel.Visible =
                    true;

                bool isLocalPlayer =
                    string.Equals(
                        member.Name,
                        _localPlayerName,
                        StringComparison.OrdinalIgnoreCase);

                // Solo el Guild Master puede
                // administrar a OTROS miembros.
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

        private void HideRows()
        {
            foreach (MemberRow row
                     in _memberRows)
            {
                row.Member =
                    null;

                row.NameLabel.Visible =
                    false;

                row.RoleLabel.Visible =
                    false;

                row.StatusLabel.Visible =
                    false;

                row.RoleButton.Visible =
                    false;

                row.KickButton.Visible =
                    false;
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
            // Ciclo administrativo:
            //
            // Member
            // -> Battle Master
            // -> Assistant Master
            // -> Member
            //
            // NO asignamos Guild Master desde aquí.
            return currentRole switch
            {
                0x00 => 0x20,
                0x20 => 0x40,
                0x40 => 0x00,

                // GuildMaster o valor desconocido:
                // vuelve a Member únicamente si
                // alguna situación extraña lo dispara.
                _ => 0x00
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

            base.Dispose();
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
                                Text = "Rank",

                                Align =
                                    ControlAlign.HorizontalCenter |
                                    ControlAlign.VerticalCenter,

                                FontSize = 8
                            },

                        Visible =
                            false
                    };

                KickButton =
                    new LabelButtonVerySmall
                    {
                        X = 380,
                        Y = y,

                        Label =
                            new LabelControl
                            {
                                Text = "Kick",

                                Align =
                                    ControlAlign.HorizontalCenter |
                                    ControlAlign.VerticalCenter,

                                FontSize = 8
                            },

                        Visible =
                            false
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