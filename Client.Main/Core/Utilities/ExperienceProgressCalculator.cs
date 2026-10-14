using System;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// MU's normal cumulative experience thresholds. The server-supplied
    /// next-level threshold remains authoritative whenever it is available.
    /// </summary>
    public static class ExperienceProgressCalculator
    {
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
        {
            ulong previous = level > 1
                ? NextLevelThreshold((ushort)(level - 1))
                : 0UL;

            if (nextLevelExperience <= previous)
                return 0f;

            ulong clamped = currentExperience < previous
                ? previous
                : currentExperience > nextLevelExperience
                    ? nextLevelExperience
                    : currentExperience;

            return (float)((clamped - previous) /
                (double)(nextLevelExperience - previous));
        }
    }
}
