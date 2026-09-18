using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Client.Main.Networking
{
    public sealed class GuildInfoData
    {
        public uint GuildId { get; init; }

        public byte GuildType { get; init; }

        public string AllianceGuildName { get; init; } =
            string.Empty;

        public string GuildName { get; init; } =
            string.Empty;

        public byte[] Logo { get; init; } =
            new byte[32];
    }

    public sealed class GuildMemberData
    {
        public string Name { get; init; } =
            string.Empty;

        public byte ServerId { get; init; }

        public byte ServerId2 { get; init; }

        public byte Role { get; init; }

        public bool IsOnline =>
            ServerId != 0xFF;

        public bool IsGuildMaster =>
            Role == 0x80;

        public string RoleName =>
            Role switch
            {
                0x80 => "Guild Master",
                0x40 => "Assistant Master",
                0x20 => "Battle Master",
                0x00 => "Member",
                _ => "Unknown"
            };
    }

    public sealed class GuildRosterData
    {
        public bool IsInGuild { get; init; }

        public uint Score { get; init; }

        public byte CurrentScore { get; init; }

        public string RivalGuildName { get; init; } =
            string.Empty;

        public GuildMemberData[] Members { get; init; } =
            Array.Empty<GuildMemberData>();

        public GuildMemberData FindMember(
            string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return null;

            return Members.FirstOrDefault(
                member =>
                    string.Equals(
                        member.Name,
                        playerName,
                        StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class GuildInfoCache
    {
        private static readonly ConcurrentDictionary<uint, GuildInfoData>
            _guilds = new();

        private static readonly ConcurrentDictionary<ushort, uint>
            _playerGuilds = new();
        private static readonly ConcurrentDictionary<ushort, byte>
            _playerGuildRoles = new();

        private static readonly ConcurrentDictionary<uint, byte>
            _pendingGuildRequests = new();

        private static GuildRosterData _currentRoster;

        public static event Action<GuildRosterData>
            GuildRosterUpdated;

        public static GuildRosterData CurrentRoster =>
            _currentRoster;

        public static void AssignPlayerToGuild(
            ushort playerId,
            uint guildId)
        {
            if (guildId == 0)
            {
                _playerGuilds.TryRemove(
                    playerId,
                    out _);

                return;
            }

            _playerGuilds[playerId] =
                guildId;
        }
        public static void AssignPlayerToGuild(
        ushort playerId,
        uint guildId,
        byte role)
    {
        AssignPlayerToGuild(
            playerId,
            guildId);

        if (guildId == 0 ||
            role == 0xFF)
        {
            _playerGuildRoles.TryRemove(
                playerId,
                out _);

            return;
        }

        _playerGuildRoles[playerId] =
            role;
    }

        public static void RemovePlayerFromGuild(
        ushort playerId)
    {
        _playerGuilds.TryRemove(
            playerId,
            out _);

        _playerGuildRoles.TryRemove(
            playerId,
            out _);
    }

        public static bool TryGetPlayerGuildId(
            ushort playerId,
            out uint guildId)
        {
            return _playerGuilds.TryGetValue(
                playerId,
                out guildId);
        }
        public static bool TryGetPlayerGuildRole(
        ushort playerId,
        out byte role)
    {
        return _playerGuildRoles.TryGetValue(
            playerId,
            out role);
    }

    public static bool PlayerHasGuild(
        ushort playerId)
    {
        return TryGetPlayerGuildId(
            playerId,
            out uint guildId) &&
            guildId != 0;
    }

    public static bool IsPlayerGuildMaster(
        ushort playerId)
    {
        return TryGetPlayerGuildRole(
            playerId,
            out byte role) &&
            role == 0x80;
    }

        public static bool TryGetGuild(
            uint guildId,
            out GuildInfoData guild)
        {
            return _guilds.TryGetValue(
                guildId,
                out guild);
        }

        public static bool TryGetGuildForPlayer(
            ushort playerId,
            out GuildInfoData guild)
        {
            guild = null;

            if (!TryGetPlayerGuildId(
                    playerId,
                    out uint guildId))
            {
                return false;
            }

            return TryGetGuild(
                guildId,
                out guild);
        }

        public static void StoreGuild(
            GuildInfoData guild)
        {
            if (guild == null ||
                guild.GuildId == 0)
            {
                return;
            }

            _guilds[guild.GuildId] =
                guild;

            _pendingGuildRequests.TryRemove(
                guild.GuildId,
                out _);
        }

        public static void StoreRoster(
            GuildRosterData roster)
        {
            _currentRoster =
                roster;

            GuildRosterUpdated?.Invoke(
                roster);
        }

        public static GuildMemberData GetLocalMember(
            string playerName)
        {
            return _currentRoster?
                .FindMember(playerName);
        }

        public static bool IsLocalPlayerGuildMaster(
            string playerName)
        {
            return GetLocalMember(playerName)?
                .IsGuildMaster == true;
        }

        public static bool TryMarkGuildRequestPending(
            uint guildId)
        {
            if (guildId == 0)
                return false;

            if (_guilds.ContainsKey(guildId))
                return false;

            return _pendingGuildRequests.TryAdd(
                guildId,
                0);
        }

        public static void CancelPendingRequest(
            uint guildId)
        {
            _pendingGuildRequests.TryRemove(
                guildId,
                out _);
        }

        public static void ClearRoster()
        {
            _currentRoster =
                null;
        }

        public static void Clear()
        {
            _guilds.Clear();
            _playerGuilds.Clear();
            _pendingGuildRequests.Clear();
            _playerGuildRoles.Clear();

            _currentRoster =
                null;
        }
    }
}