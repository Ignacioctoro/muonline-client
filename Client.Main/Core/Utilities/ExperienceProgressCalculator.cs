using System;
using Client.Main.Core.Client;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// MU's normal cumulative experience thresholds. The server-supplied
    /// next-level threshold remains authoritative whenever it is available.
    /// </summary>
    public static class ExperienceProgressCalculator
    {
        public static float GetCharacterFraction(CharacterState state, bool master)
        {
            var info = state.Progression;
            int level = master ? state.MasterLevel : state.Level;
            int maximum = master ? info?.MaximumMasterLevel ?? 200 : info?.MaximumLevel ?? 400;
            if (level >= maximum) return 1f;

            ulong current = master ? state.MasterExperience : state.Experience;
            ulong next = master ? state.MasterExperienceForNextLevel : state.ExperienceForNextLevel;
            ulong start;
            if (master)
            {
                start = info?.MasterLevel == level ? info.MasterStart : DefaultMasterThreshold(level);
            }
            else
            {
                start = info?.Level == level ? info.NormalStart :
                    level > 1 ? NextLevelThreshold((ushort)(level - 1)) : 0;
            }

            if (next <= start) return 0f;
            ulong gained = current > start ? current - start : 0;
            return (float)Math.Clamp(gained / (double)(next - start), 0d, 1d);
        }

        public static void GetCharacterDecile(CharacterState state, out int digit, out float fraction)
        {
            double progress = GetCharacterFraction(state, state.UsesMasterExperience) * 10d;
            digit = Math.Clamp((int)progress, 0, 9);
            fraction = progress >= 10d ? 1f : (float)(progress - digit);
        }

        private static ulong DefaultMasterThreshold(int level)
        {
            ulong n = (ulong)Math.Max(0, level);
            return 505UL * n * n * n + 228045UL * n * n + 35278500UL * n;
        }

        public static ulong NextLevelThreshold(ushort level)
        {
            ulong n = level;
            ulong total = (n + 9UL) * n * n * 10UL;
            if (n > 255UL)
            {
                ulong extraLevel = n - 255UL;
                total += (extraLevel + 9UL) * extraLevel * extraLevel * 1000UL;
            }
            return total;
        }

        public static float GetNormalLevelFraction(
            ushort level, ulong currentExperience, ulong nextLevelExperience)
            => (float)GetFraction(level, currentExperience, nextLevelExperience);

        /// <summary>
        /// The original MU frame divides each level into ten 10% stages.
        /// The digit at the right shows the stage (0..9); the gauge only
        /// shows the progress inside that stage, not the whole level.
        /// </summary>
        public static void GetNormalLevelDecile(
            ushort level, ulong currentExperience, ulong nextLevelExperience,
            out int digit, out float fillFraction)
        {
            double progress = GetFraction(level, currentExperience, nextLevelExperience);
            double scaled = progress * 10.0;

            if (scaled >= 10.0)
            {
                digit = 9;
                fillFraction = 1f;
                return;
            }

            digit = Math.Clamp((int)scaled, 0, 9);
            fillFraction = (float)Math.Clamp(scaled - digit, 0.0, 1.0);
        }

        private static double GetFraction(
            ushort level, ulong currentExperience, ulong nextLevelExperience)
        {
            ulong previous = level > 1
                ? NextLevelThreshold((ushort)(level - 1))
                : 0UL;

            if (nextLevelExperience <= previous)
                return 0.0;

            ulong clamped = currentExperience < previous
                ? previous
                : currentExperience > nextLevelExperience
                    ? nextLevelExperience
                    : currentExperience;

            return (clamped - previous) /
                (double)(nextLevelExperience - previous);
        }
    }
}
