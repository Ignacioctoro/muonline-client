namespace Client.Main.Objects.Monsters
{
    // GM3rdChangeUp: shared bodies, separate authoritative server identities.
    [NpcInfo(409, "Balram (Trainee Soldier)")]
    public sealed class BalramTrainee : Balram
    {
        public BalramTrainee() { Scale = 1.25f; }
    }
    [NpcInfo(410, "Death Spirit (Trainee Soldier)")]
    public sealed class DeathSpiritTrainee : DeathSpirit
    {
        public DeathSpiritTrainee() { Scale = 1.25f; }
    }
    [NpcInfo(411, "Soram (Trainee Soldier)")]
    public sealed class SoramTrainee : Soram
    {
        public SoramTrainee() { Scale = 1.3f; }
    }
    // GMCrywolf1st: hero variants and the original non-event aliases.
    [NpcInfo(314, "Scout (Hero)")]
    public sealed class CrywolfScoutHero : HammerScout
    {
        public CrywolfScoutHero() { Scale = 1.6f; }
    }
    [NpcInfo(315, "Werewolf (Hero)")]
    public sealed class CrywolfWerewolfHero : Werewolf1
    {
        public CrywolfWerewolfHero() { Scale = 1.65f; }
    }
    [NpcInfo(316, "Balram")]
    public sealed class CrywolfBalram : Balram
    {
        public CrywolfBalram() { Scale = 1.25f; }
    }
    [NpcInfo(317, "Soram")]
    public sealed class CrywolfSoram : Soram
    {
        public CrywolfSoram() { Scale = 1.3f; }
    }
}
