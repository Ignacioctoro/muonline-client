#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(11)] // Power Wave
    [SkillVisualEffect(13)] // CometFall
    [SkillVisualEffect(38)] // Decay
    [SkillVisualEffect(387)] // Decay strengthener
    public sealed class ClassicFxWizardAttackSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                context.Caster == null || !ReferenceEquals(context.Caster.World, context.World))
                return false;

            Vector3 angle = context.Caster.TotalAngle;
            if (context.SkillId == 11)
            {
                if (context.IsAreaSkillAnimation)
                    return false;

                Vector3 origin = context.Caster.WorldPosition.Translation;
                if (context.TargetPosition is { } target &&
                    (target.X != origin.X || target.Y != origin.Y))
                {
                    angle = new Vector3(0f, 0f, MathHelper.ToRadians(
                        AngleUtils.CreateAngleDegrees(origin.X, origin.Y, target.X, target.Y)));
                }

                // Magic02 is a low wave. Lift it above the ground so the
                // terrain depth pass cannot cover it at the caster's feet.
                float ground = context.World.Terrain.RequestTerrainHeight(origin.X, origin.Y);
                origin.Z = MathF.Max(origin.Z, ground) + 40f;
                return fx.CreateEffect(ClassicFxEffectType.Magic2, origin, angle,
                    Vector3.One, ClassicFxOwner.None).IsValid;
            }

            // Area casts can also produce a targeted animation. Only 0x1E
            // has the authoritative impact tile and should emit these effects.
            if (!context.IsAreaSkillAnimation || context.TargetPosition is not { } center)
                return false;

            bool decay = context.SkillId is 38 or 387;
            var type = decay ? ClassicFxEffectType.Fire : ClassicFxEffectType.SkillBlast;
            Vector3 light = decay ? new Vector3(0.8f, 0.5f, 0.1f) : Vector3.One;
            bool emitted = false;
            for (int i = 0; i < 2; i++)
                emitted |= fx.CreateEffect(type, center, angle, light,
                    ClassicFxOwner.None, subType: decay ? 6 : 0).IsValid;

            if (emitted && decay)
                SoundController.Instance.PlayBuffer("Sound/eBlastPoison_1.wav");

            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
