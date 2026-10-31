namespace Client.Main.Core.Utilities
{
    /// <summary>Season 6 IsCanBCSkill rules. An unknown battle state is inactive.</summary>
    public static class CastleSiegeSkillRules
    {
        public const ushort ValleyOfLoren = 30;

        public static bool IsCastleSiegeOnly(ushort skillId) =>
            skillId is 44 or 45 or 46 or 57 or 73 or 74 or 269 or
                67 or 68 or 69 or 70 or 71 or 72;

        public static bool CanUse(ushort skillId, ushort mapId, bool battleActive) =>
            !IsCastleSiegeOnly(skillId) || (mapId == ValleyOfLoren && battleActive);

        public static bool CanUse(ushort skillId)
        {
            if (!IsCastleSiegeOnly(skillId)) return true;
            var state = MuGame.Network?.GetCharacterState();
            return state != null && state.IsInGame &&
                CanUse(skillId, state.MapId, state.IsCastleSiegeBattleActive);
        }
    }
}
