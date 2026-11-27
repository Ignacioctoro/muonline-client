namespace Client.Main.Objects.Monsters
{
    // Same native models as their ordinary families, with the boss subtype's scale.
    [NpcInfo(145, "Death Centurion")]
    public sealed class DeathCenturion : LordCenturion
    {
        public DeathCenturion() { Scale = 1.5f; }
    }

    [NpcInfo(161, "Illusion of Kundun")]
    public sealed class IllusionOfKundun : Schriker
    {
        public IllusionOfKundun() { Scale = 1.5f; }
    }
}
