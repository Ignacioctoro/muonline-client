using System;
using Client.Main.Core.Client;

namespace Client.Main.Core.Items
{
    /// <summary>S6 equipment addressing and OpenMU's standard trainable-pet progression.</summary>
    public static class Season6PetRules
    {
        public const byte CompanionSlot = 8;
        public const byte RavenSlot = 1;
        public const int MaximumHealth = 255;
        public const int MaximumLevel = 50;

        public static bool IsPet(byte slot, int group, int number) => group == 13 &&
            (slot == CompanionSlot || slot == RavenSlot && number == 5);

        public static bool IsTrainable(int group, int number) => group == 13 && number is 4 or 5;

        public static bool MatchesInfo(int number, PetInfoSnapshot info) =>
            number == 4 && info.PetType == 1 || number == 5 && info.PetType == 0;

        public static uint ExperienceAtLevel(int level)
        {
            if (level is < 1 or > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return (uint)((long)level * level * level * 100 * (level + 10));
        }

        public static float ExperienceProgress(byte level, uint experience)
        {
            if (level >= MaximumLevel) return 1;
            if (level < 1) return 0;
            uint floor = ExperienceAtLevel(level);
            uint next = ExperienceAtLevel(level + 1);
            return (float)Math.Clamp(((double)experience - floor) / (next - floor), 0, 1);
        }

        public readonly record struct RavenBaseStats(int MinimumDamage, int MaximumDamage, int AttackSpeed, int AttackRate);

        // ClassDarkLord's base relationships, before master skills and combat multipliers.
        public static RavenBaseStats RavenStats(byte level, int leadership) => new(
            180 + 15 * level + leadership / 8,
            200 + 15 * level + leadership / 4,
            20 + (int)(level * 0.8f + leadership / 50f),
            1000 + level * 16 / 15);

        public static int HorseDefense(byte level, int agility) => 5 + 2 * level + agility / 20;
        public static float HorseAbsorption(byte level) => 15 + level * 0.5f;
    }
}
