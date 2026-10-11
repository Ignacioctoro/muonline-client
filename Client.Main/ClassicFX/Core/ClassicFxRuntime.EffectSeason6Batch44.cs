// BroyalMU ClassicFX S6 — Batch 44.
// Pinned sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp CreateEffect/MoveEffects and Behaviors/MoveHandlers.cpp.
// Real model-less roots only. Uses the established pooled joints, particles,
// sprites and BMD effect models; no extra renderer or new texture alias.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch44LogicalType(ClassicFxEffectType type, int sub) =>
            type switch
            {
                ClassicFxEffectType.KundunSkillCarrier => sub is 0 or 1 or 2,
                ClassicFxEffectType.ShineFlareCarrier => sub == 0,
                ClassicFxEffectType.SpearHealingCarrier => sub == 0,
                ClassicFxEffectType.ThunderPlusOneCarrier => sub == 0,
                _ => false
            };

        private void InitializeS6Batch44Logical(
            ClassicFxEffectType type, ClassicFxOwner owner,
            ref Vector3 position, ref Vector3 angle, int subType, ref int nativePkKey,
            out float life)
        {
            switch (type)
            {
                case ClassicFxEffectType.KundunSkillCarrier:
                    // MODEL_CUNDUN_SKILL: subtype 2 additionally owns one ghost
                    // child; all three carrier variants have no model rendering.
                    life = 30f;
                    if (subType == 2) { life = 40f; nativePkKey = 0; }
                    return;
                case ClassicFxEffectType.ShineFlareCarrier:
                    life = 50f;
                    return;
                case ClassicFxEffectType.SpearHealingCarrier:
                    // Move_MODEL__SPEAR uses a zero angle and three healing
                    // joints each reference tick; EffectTypes.json: five ticks.
                    angle = Vector3.Zero;
                    life = 5f;
                    return;
                case ClassicFxEffectType.ThunderPlusOneCarrier:
                    life = 10f;
                    if (owner.WorldObject != null)
                    {
                        // CreateEffect(BITMAP_JOINT_THUNDER+1) subtype zero.
                        position += ClassicMath.VectorRotate(
                            new Vector3(-25f, -80f, 0f),
                            ClassicMath.AngleMatrix(new Vector3(
                                MathHelper.ToDegrees(owner.WorldObject.Angle.X),
                                MathHelper.ToDegrees(owner.WorldObject.Angle.Y),
                                MathHelper.ToDegrees(owner.WorldObject.Angle.Z))));
                    }
                    return;
                default:
                    life = 0f;
                    return;
            }
        }

        private void EmitS6Batch44OnCreate(
            ClassicFxHandle self, ref EffectState e)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.KundunSkillCarrier:
                    if (e.SubType == 2)
                    {
                        // Native CreateEffect(MODEL_CUNDUN_GHOST, ..., owner=o).
                        // This already registered BMD is rendered by ModelObject.
                        CreateEffect(ClassicFxEffectType.KundunGhost,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.FromClassicFx(self), subType: 0);
                    }
                    break;

                case ClassicFxEffectType.ShineFlareCarrier:
                    // MODEL_SHINE spawns exactly ten flare joints on creation.
                    // MODEL_SHINE itself is deliberately invisible in RenderEffects.
                    for (int j = 0; j < 10; j++)
                    {
                        Vector3 p = new Vector3(
                            e.Position.X + (j - 5) * 12f - 30f,
                            e.Position.Y + (j - 5) * 12f + 30f,
                            e.Position.Z - 300f);
                        CreateJoint(ClassicTextureIds.BitmapFlare,
                            p, e.Position, e.Angle, 16, e.Owner, 120f);
                    }
                    break;
            }
        }

        private bool MoveS6Batch44Logical(ref EffectState e)
        {
            // In the native Main these child emitters are evaluated at the
            // logical effect tick, not per display refresh. At >25 FPS this
            // gate avoids duplicated pooled objects and allocations.
            if (!Clock.AdvancedReferenceFrame)
                return true;

            switch (e.Type)
            {
                case ClassicFxEffectType.KundunSkillCarrier:
                    MoveS6Batch44Kundun(ref e);
                    return true;

                case ClassicFxEffectType.ShineFlareCarrier:
                    if (e.LifeTime > 10f && e.Owner.WorldObject != null &&
                        ReferenceEquals(e.Owner.WorldObject.World, World) &&
                        e.Owner.WorldObject.Status == GameControlStatus.Ready)
                    {
                        // Move_MODEL_SHINE subtype 0: local X random -114..13;
                        // local Y is always local X + 100; Z = Owner.Z + 360.
                        float dx = Random.Modulo(128) - 114f;
                        Vector3 p = e.Owner.WorldObject.WorldPosition.Translation +
                            new Vector3(dx, dx + 100f, 360f);
                        float size = (Random.Modulo(30) + 50) * 0.01f;
                        CreateParticle(ClassicTextureIds.BitmapShiny,
                            p, e.Angle, e.Light, 2, size);
                    }
                    return true;

                case ClassicFxEffectType.SpearHealingCarrier:
                    // Move_MODEL__SPEAR: three independent randomized healing
                    // trails, not a fabricated model sprite.
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 a = new Vector3(
                            Random.Modulo(90), 0f, Random.Modulo(360));
                        Vector3 offset = ClassicMath.VectorRotate(
                            new Vector3(0f, -100f, 0f),
                            ClassicMath.AngleMatrix(a));
                        Vector3 p = e.Position - offset;
                        CreateJoint(ClassicTextureIds.BitmapJointHealing,
                            p, e.Position, a, 6, ClassicFxOwner.None, 5f);
                    }
                    return true;

                case ClassicFxEffectType.ThunderPlusOneCarrier:
                    // Native 10 ticks; only at remaining 4 and 2 does the
                    // invisible subtype-zero effect create joint subtype 5.
                    int tick = (int)e.LifeTime;
                    if ((tick == 4 || tick == 2) &&
                        (e.TriggerMask & (tick == 4 ? 1 : 2)) == 0)
                    {
                        e.TriggerMask |= (byte)(tick == 4 ? 1 : 2);
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(64) - 32,
                            Random.Modulo(64) - 32, 0f);
                        CreateJoint(ClassicTextureIds.BitmapJointThunder + 1,
                            p, p, e.Angle, 5, ClassicFxOwner.None,
                            50f + Random.Modulo(10));
                    }
                    return true;
            }
            return false;
        }

        private void MoveS6Batch44Kundun(ref EffectState e)
        {
            // MODEL_CUNDUN_SKILL: native subtype 0 fires ten phoenixes at
            // tick 30; subtype 1 fires twenty dragon heads at tick 30 and
            // one at PKKey 6..9. Subtype 2 only spawns the ghost on creation.
            if (e.SubType == 0 && e.LifeTime <= 30f &&
                (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                for (int i = 0; i < 10; i++)
                {
                    Vector3 a = new Vector3(
                        0f, 0f, e.Angle.Z + MathHelper.ToRadians(320f + i * 8f));
                    CreateEffect(ClassicFxEffectType.KundunPhoenix,
                        e.Position, a, e.Light, ClassicFxOwner.None);
                }
            }
            else if (e.SubType == 1)
            {
                Vector3 a = new Vector3(0f, 0f, e.NativePkKey * 30f);
                if (e.LifeTime <= 30f && (e.TriggerMask & 1) == 0)
                {
                    e.TriggerMask |= 1;
                    for (int i = 0; i < 20; i++)
                        CreateEffect(ClassicFxEffectType.KundunDragonHead,
                            e.Position, a, e.Light, ClassicFxOwner.None);
                }
                if (e.NativePkKey > 5 && e.NativePkKey < 10)
                    CreateEffect(ClassicFxEffectType.KundunDragonHead,
                        e.Position, a, e.Light, ClassicFxOwner.None);
                ++e.NativePkKey;
            }
        }
    }
}
