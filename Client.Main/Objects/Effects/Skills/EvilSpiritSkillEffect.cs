#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(9)]
    public sealed class EvilSpiritSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (!context.IsAreaSkillAnimation ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                context.Caster == null || !ReferenceEquals(context.Caster.World, context.World))
                return false;

            Vector3 origin = context.Caster.WorldPosition.Translation;
            Vector3 start = origin + new Vector3(0f, 0f, 100f);
            var owner = ClassicFxOwner.FromWorldObject(context.Caster);
            bool emitted = false;

            // MuMain emits two spirit trails at each fixed compass heading.
            // The joint bridge takes native degrees.
            for (int i = 0; i < 4; i++)
            {
                Vector3 angle = new Vector3(0f, 0f, i * 90f);
                emitted |= fx.CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                    start, origin, angle, target: owner, scale: 80f).IsValid;
                emitted |= fx.CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                    start, origin, angle, target: owner, scale: 20f).IsValid;
            }

            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
