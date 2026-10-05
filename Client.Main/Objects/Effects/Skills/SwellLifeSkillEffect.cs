#nullable enable

using Client.Main.Core.Utilities;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Classic BK Greater Fortitude / Swell Life / Inner.
    ///
    /// Original client:
    /// AT_SKILL_SWELL_LIFE = 48
    ///
    /// The visual is a CAST effect, not a persistent buff aura.
    /// </summary>
    [SkillVisualEffect(48)]
    public sealed class SwellLifeSkillEffect :
        ISkillVisualEffect
    {
        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster == null ||
                context.World == null)
            {
                return null;
            }

            return new SwellLifeCastEffect(
                context.Caster);
        }
    }
}