using Client.Data.BMD;
using Client.Main.Core.Utilities;
using Client.Main.Models;

internal static class Program
{
    private static int Main()
    {
        try
        {
            // Forces the real static initializer (BuildRoutes) to execute.
            var definitions = Season6SkillActionDatabase.Definitions;
            if (definitions.Count == 0)
                throw new Exception("Season 6 skill table is empty.");

            foreach (ushort id in new ushort[] { 61, 508, 514 })
            {
                if (!Season6SkillActionDatabase.TryGet(id, out var route))
                    throw new Exception($"Fire Burst route {id} is missing.");

                if (route.SkillId != id ||
                    route.SkillType != SkillType.Target ||
                    route.AnimationMode != Season6SkillAnimationMode.Explicit ||
                    route.ExplicitAction != PlayerAction.PlayerAttackStrike)
                {
                    throw new Exception($"Fire Burst route {id} has incorrect skill/animation mapping.");
                }
            }

            Console.WriteLine($"PASS: initialized {definitions.Count} unique Season 6 skill routes.");
            Console.WriteLine("PASS: Fire Burst 61, Strengthener 508, Mastery 514 use PlayerAttackStrike.");
            return 0;
        }
        catch (Exception ex)
        {
            // Print the complete exception chain, including InnerException.
            Console.Error.WriteLine("FAIL: Season 6 skill route initialization: " + ex);
            return 1;
        }
    }
}
