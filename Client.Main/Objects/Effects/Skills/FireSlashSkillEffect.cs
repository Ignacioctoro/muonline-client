#nullable enable

using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(55)]
    public sealed class FireSlashSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster == null ||
                context.World == null)
            {
                return null;
            }

            return new FireSlashEffect(
                context.Caster);
        }
    }
}