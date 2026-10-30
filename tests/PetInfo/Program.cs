using Client.Main.Core.Client;
using Client.Main.Core.Items;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

foreach (int number in new[] { 0, 1, 2, 3, 4, 37, 64, 65, 67, 80, 106, 123 })
    Check(Season6PetRules.IsPet(8, 13, number), $"Companion {number} appears in its equipped helper slot");
Check(Season6PetRules.IsPet(1, 13, 5), "Dark Raven uses its S6 equipment slot");
Check(!Season6PetRules.IsPet(1, 13, 0), "An unrelated hand item is not a raven");
Check(!Season6PetRules.IsPet(12, 13, 4), "Pets in the backpack are not equipped pets");
Check(!Season6PetRules.IsPet(8, 12, 4), "Other item groups do not become pets");
Check(Season6PetRules.IsTrainable(13, 4) && Season6PetRules.IsTrainable(13, 5), "Horse and raven have training data");
Check(!Season6PetRules.IsTrainable(13, 37) && !Season6PetRules.IsTrainable(13, 80), "Fenrir and Panda do not acquire artificial experience");

Check(Season6PetRules.MatchesInfo(4, new PetInfoSnapshot(1, 20, 1_000_000, 100)), "Horse info type matches");
Check(!Season6PetRules.MatchesInfo(4, new PetInfoSnapshot(0, 20, 1_000_000, 100)), "Raven info cannot overwrite a horse");
Check(Season6PetRules.ExperienceAtLevel(1) == 1100, "Level 1 threshold");
Check(Season6PetRules.ExperienceAtLevel(2) == 9600, "Level 2 threshold");
Check(Season6PetRules.ExperienceAtLevel(50) == 750_000_000, "Level 50 threshold does not overflow");
Check(Season6PetRules.ExperienceProgress(1, 5350) == 0.5f, "Progress is measured within the level");
Check(Season6PetRules.ExperienceProgress(1, 0) == 0, "Fresh pet experience cannot underflow");
Check(Season6PetRules.ExperienceProgress(49, uint.MaxValue) == 1, "Progress is bounded");
Check(Season6PetRules.ExperienceProgress(50, 750_000_000) == 1, "Maximum level is complete");
Check(Season6PetRules.HorseDefense(50, 400) == 125, "OpenMU horse defense relationship");
Check(Season6PetRules.HorseAbsorption(50) == 40, "OpenMU horse damage absorption");
var raven = Season6PetRules.RavenStats(50, 400);
Check(raven.MinimumDamage == 980 && raven.MaximumDamage == 1050, "OpenMU base raven damage relationships");
Check(raven.AttackSpeed == 68 && raven.AttackRate == 1053, "OpenMU base raven speed and rate relationships");
Console.WriteLine("Pet information checks passed.");
