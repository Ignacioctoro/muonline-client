#nullable enable
using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for Power Slash skill visual effect (Skill ID 56).
    /// </summary>
    [SkillVisualEffect(56)]
    public sealed class PowerSlashSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            return new PowerSlashEffectGroup(context.Caster);
        }
    }
}