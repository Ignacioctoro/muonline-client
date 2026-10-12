#nullable enable
// ClassicFX S6 Batch 47: actual AreaSkillAnimation (0x1E) emitter.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ClassAttack.cpp AT_SKILL_MULTI_SHOT: 2x MODEL_MULTI_SHOT3,
// 3x MODEL_MULTI_SHOT1, 2x MODEL_MULTI_SHOT2 and BLADE_SKILL subtype 1.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(235)]
    public sealed class ElfMultiShotSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            Vector3 light = new(0.8f, 0.9f, 1.6f);
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(caster);
            Matrix rotation = Matrix.CreateFromYawPitchRoll(
                angle.Y, angle.X, angle.Z);

            Vector3 offsetPlus = Vector3.TransformNormal(
                new Vector3(0f, 20f, 0f), rotation);
            // Keep native duplicate calls: each instance has its own
            // lifetime and draw; no additional WorldObjects are allocated.
            for (int i = 0; i < 2; ++i)
                fx.CreateEffect(ClassicFxEffectType.MultiShot3,
                    origin + offsetPlus, angle, light, ClassicFxOwner.None);
            for (int i = 0; i < 3; ++i)
                fx.CreateEffect(ClassicFxEffectType.MultiShot1,
                    origin, angle, light, ClassicFxOwner.None);
            for (int i = 0; i < 2; ++i)
                fx.CreateEffect(ClassicFxEffectType.MultiShot2,
                    origin + offsetPlus, angle, light, ClassicFxOwner.None);

            Vector3 bladeOffset = Vector3.TransformNormal(
                new Vector3(0f, -120f, 145f), rotation);
            return fx.CreateEffect(ClassicFxEffectType.BladeSkillModel,
                origin + bladeOffset, angle, light, owner, subType: 1)
                .IsValid;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
