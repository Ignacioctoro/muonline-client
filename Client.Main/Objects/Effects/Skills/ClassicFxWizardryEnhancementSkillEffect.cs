#nullable enable
using Client.Main.Objects.Player;

namespace Client.Main.Objects.Effects.Skills
{
    // Skill IDs are independent of the internal ClassicFxEffectType IDs.
    // Animation packets cast once; 0x07/0x1B own the persistent buff.
    [SkillVisualEffect(233)]
    [SkillVisualEffect(380)]
    [SkillVisualEffect(383)]
    public sealed class ClassicFxWizardryEnhancementSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            return ClassicFxWizardryEnhancementVisual.TriggerCast(caster);
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
