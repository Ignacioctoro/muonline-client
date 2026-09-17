using System.Collections.Concurrent;

namespace Client.Main.Networking
{
    public sealed class GuildInfoData
    {
        public uint GuildId { get; init; }

        public byte GuildType { get; init; }

        public string AllianceGuildName { get; init; } = string.Empty;

        public string GuildName { get; init; } = string.Empty;

        public byte[] Logo { get; init; } = new byte[32];
    }

    public static class GuildInfoCache
    {
        private static readonly ConcurrentDictionary<uint, GuildInfoData>
            _guilds = new();

        private static readonly ConcurrentDictionary<ushort, uint>
            _playerGuilds = new();

        private static readonly ConcurrentDictionary<uint, byte>
            _pendingGuildRequests = new();

        public static void AssignPlayerToGuild(
            ushort playerId,
            uint guildId)
        {
            if (guildId == 0)
            {
                _playerGuilds.TryRemove(playerId, out _);
                return;
            }

            _playerGuilds[playerId] = guildId;
        }

        public static void RemovePlayerFromGuild(
            ushort playerId)
        {
            _playerGuilds.TryRemove(playerId, out _);
        }

        public static bool TryGetPlayerGuildId(
            ushort playerId,
            out uint guildId)
        {
            return _playerGuilds.TryGetValue(
                playerId,
                out guildId);
        }

        public static bool TryGetGuild(
            uint guildId,
            out GuildInfoData guild)
        {
            return _guilds.TryGetValue(
                guildId,
                out guild);
        }

        public static void StoreGuild(
            GuildInfoData guild)
        {
            if (guild == null || guild.GuildId == 0)
                return;

            _guilds[guild.GuildId] = guild;

            _pendingGuildRequests.TryRemove(
                guild.GuildId,
                out _);
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

        public static void Clear()
        {
            _guilds.Clear();
            _playerGuilds.Clear();
            _pendingGuildRequests.Clear();
        }
    }
}