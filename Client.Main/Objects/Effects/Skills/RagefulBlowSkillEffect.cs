#nullable enable
using Client.Main.Core.Utilities;
// Skill 42: keep the complete legacy presentation until the native
// FuryStrike weapon/impact bridge is equivalent. Never draw both.
namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(42)]
    public sealed class RagefulBlowSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            return new RagefulBlowEffect(context.Caster, context.TargetPosition);
        }
    }
}
