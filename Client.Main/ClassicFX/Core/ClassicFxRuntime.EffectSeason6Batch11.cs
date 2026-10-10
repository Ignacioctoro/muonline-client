// BroyalMU ClassicFX S6 Batch 11 — Blow of Destruction impact chain.
// Pinned native: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp / MoveHandlers.cpp / ZzzOpenData.cpp / EffectTypes.json.
// Visual-only; BMD views use the existing MonoGame renderer, never another.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch11ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.NightWater01 or
                ClassicFxEffectType.KnightPlancrackB;

        private static bool TryGetS6Batch11ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (subType != 0) return false;

            switch (type)
            {
                case ClassicFxEffectType.NightWater01:
                    definition = new Season6ModelDefinition(
                        "Effect/nightwater01.bmd", 25f, 1f,
                        useCallerScale: true);
                    return true;
                case ClassicFxEffectType.KnightPlancrackB:
                    definition = new Season6ModelDefinition(
                        "Effect/knight_plancrack_b.bmd", 25f, 1f,
                        offsetZ: 15f, useCallerScale: true);
                    return true;
                default:
                    return false;
            }
        }

        private void InitializeS6Batch11Model(
            ClassicFxEffectType type, ref Vector3 angle)
        {
            if (type == ClassicFxEffectType.NightWater01)
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            else if (type == ClassicFxEffectType.KnightPlancrackB)
                angle.Z += MathHelper.ToRadians(90f * Clock.FrameFactor);
        }

        private bool MoveS6Batch11Model(ref EffectState effect, float f)
        {
            // Original Nightwater01 and KnightPlancrackB share fade rate.
            effect.Alpha = MathF.Max(0f, effect.Alpha - 0.04f * f);
            return true;
        }

        private void EmitS6BlowOfDestructionImpact(ref EffectState e)
        {
            Vector3 blue = new Vector3(0.3f, 0.3f, 1f);
            if (e.SubType == 0)
            {
                for (int i = 0; i < 2; i++)
                    CreateEffect(ClassicFxEffectType.NightWater01,
                        e.Position, e.Angle, blue, ClassicFxOwner.None);
                CreateEffect(ClassicFxEffectType.KnightPlancrackA,
                    e.Position, e.Angle, blue, ClassicFxOwner.None,
                    subType: 0, scale: 1.2f);

                // Native creates a sequence of BMD ground cracks along
                // the vector toward the original incoming Light/target.
                Vector3 delta = e.StartPosition - e.Position;
                float length = delta.Length();
                if (length > 0.001f)
                {
                    Vector3 direction = delta / length;
                    Vector3 casterAngle = e.Angle;
                    if (TryGetOwnerSnapshot(e.Owner,
                            out ClassicFxOwnerSnapshot owner))
                        casterAngle = owner.Angle;

                    // Native count is (int)(length/100)+1. Cap pathological
                    // targets to preserve fixed pools on Android.
                    int count = Math.Min(32, (int)(length / 100f) + 1);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 position = e.Position + direction * (55f * i);
                        Vector3 rotation = casterAngle;
                        float yaw = Random.Modulo(20) + 10f;
                        rotation.Z += MathHelper.ToRadians(
                            i % 2 == 0 ? yaw : -yaw);
                        CreateEffect(ClassicFxEffectType.KnightPlancrackB,
                            position, rotation, blue, ClassicFxOwner.None,
                            subType: 0, scale: 1f);
                    }
                }
            }
            else if (e.SubType == 1)
            {
                CreateEffect(ClassicFxEffectType.NightWater01,
                    e.Position, e.Angle, blue, ClassicFxOwner.None,
                    scale: 2f);
                CreateEffect(ClassicFxEffectType.NightWater01,
                    e.Position, e.Angle, blue, ClassicFxOwner.None,
                    scale: 1f);

                // The original additionally spawns RaklionBossCrackEffect,
                // which is not yet ported. Do not fabricate an alternative.
                int count = 5 + Random.Modulo(3);
                for (int i = 0; i < count; i++)
                {
                    float yaw = MathHelper.ToRadians(Random.Modulo(360));
                    Vector3 displacement = Vector3.TransformNormal(
                        new Vector3(0f, Random.Modulo(150), 0f),
                        Matrix.CreateRotationZ(yaw));
                    CreateEffect(Random.Modulo(2) == 0
                            ? ClassicFxEffectType.Stone1
                            : ClassicFxEffectType.Stone2,
                        e.Position + displacement, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: 13);
                }
            }
        }

        private void EmitS6BlowOfDestructionSprites(ref EffectState e)
        {
            Vector3 position = e.Position;
            if (e.SubType == 0)
            {
                position.Z += 65f;
                CreateSprite(ClassicTextureIds.BitmapSwordEffectMono,
                    position, 3f + Random.Modulo(10) * 0.011f,
                    new Vector3(0.5f, 0.5f, 1f));
            }
            else
            {
                position.Z += 100f;
                CreateSprite(ClassicTextureIds.BitmapLight,
                    position, e.Scale,
                    new Vector3(e.Light.X * 0.5f,
                        e.Light.Y * 0.5f, e.Light.Z));
            }
        }

        private void EmitS6BlowOfDestructionWaterfall(ref EffectState e)
        {
            // Native subtype 1: fifteen Waterfall3/5 particles per tick,
            // with conditional Smoke55.
            for (int i = 0; i < 15; i++)
            {
                Vector3 position = e.Position + new Vector3(
                    Random.Modulo(300) - 150f,
                    Random.Modulo(300) - 150f,
                    Random.Modulo(300) - 150f);
                float scale = 1.6f + Random.Modulo(10) * 0.1f;
                int texture = Random.FpsCheck(2, Clock)
                    ? ClassicTextureIds.BitmapWaterfall5
                    : ClassicTextureIds.BitmapWaterfall3;
                CreateParticle(texture, position, e.Angle,
                    new Vector3(0.5f, 0.5f, 1f),
                    subType: 8, scale: scale);
                if (Random.FpsCheck(2, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        position, e.Angle, Vector3.One,
                        subType: 55, scale: 1f);
            }
        }

        private void MoveS6BlowOfDestruction(ref EffectState e, float f)
        {
            if (e.LifeTime > 24f)
                return;

            if (Clock.AdvancedReferenceFrame)
            {
                // Render-time native sprites are queued at 25Hz here to
                // avoid inflating pools at 60/120Hz in Android.
                EmitS6BlowOfDestructionSprites(ref e);

                if (e.SubType == 1 && e.LifeTime >= 15f)
                    EmitS6BlowOfDestructionWaterfall(ref e);

                // Native first impact is at LifeTime == 23.
                if (e.LifeTime <= 23f && (e.TriggerMask & 1) == 0)
                {
                    e.TriggerMask |= 1;
                    EmitS6BlowOfDestructionImpact(ref e);
                }
            }

            e.Light *= MathF.Pow(1f / 1.05f, f);
            // Native EarthQuake modifies the camera; preserve separation
            // from effect rendering and existing gameplay.
        }
    }
}
