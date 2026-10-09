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
        // Toggle to false to compare with the original MonoGame renderer.
        private const bool UseClassicFxInnerPilot = true;
        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster == null ||
                context.World == null)
            {
                return null;
            }

            if (UseClassicFxInnerPilot &&
                context.World.ClassicFx != null &&
                context.World.ClassicFx.Enabled &&
                !context.World.ClassicFx.IsDisposed)
            {
                return new ClassicFxSwellLifeCastVisual(context.Caster);
            }

            return new SwellLifeCastEffect(context.Caster);
        }
    }
}