// ClassicFX S6 Batch 39 — pinned MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Native ZzzEffect.cpp BITMAP_* roots. Pooled children only, no new renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch39LogicalType(ClassicFxEffectType t, int sub) =>
            t switch
            {
                ClassicFxEffectType.BossLaserBlue => sub is 0 or 1 or 2,
                ClassicFxEffectType.BossLaserRed => sub == 0,
                ClassicFxEffectType.BossLaserShort => sub == 0,
                ClassicFxEffectType.FireTrail02 => sub == 0,
                ClassicFxEffectType.LightProjectile => sub == 0,
                ClassicFxEffectType.MagicGroundBase => sub == 1,
                ClassicFxEffectType.ShotgunJointBurst => sub == 0,
                ClassicFxEffectType.FlareForceJointBurst => sub is >= 0 and <= 7,
                ClassicFxEffectType.LightRedGround => sub is 3 or 4,
                ClassicFxEffectType.ChromeEnergyGround => sub == 0,
                _ => false
            };

        private static bool S6Batch39NeedsOwner(ClassicFxEffectType t, int sub) =>
            t == ClassicFxEffectType.FlareForceJointBurst ||
            (t == ClassicFxEffectType.LightRedGround && sub == 3);

        private static bool IsS6Batch39TerrainType(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.MagicGroundBase or
                ClassicFxEffectType.LightRedGround or
                ClassicFxEffectType.ChromeEnergyGround;

        private void InitializeS6Batch39Logical(
            ClassicFxEffectType t, int sub, ref Vector3 pos,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref Vector3 direction, out float life)
        {
            life = 20f;
            switch (t)
            {
                case ClassicFxEffectType.BossLaserBlue:
                case ClassicFxEffectType.BossLaserRed:
                case ClassicFxEffectType.BossLaserShort:
                    life = t == ClassicFxEffectType.BossLaserBlue && sub == 2
                        ? 35f : 20f;
                    if (t == ClassicFxEffectType.BossLaserShort)
                    {
                        scale = 5f;
                        light = new Vector3(1f, 0.4f, 0.2f);
                    }
                    else if (t == ClassicFxEffectType.BossLaserRed)
                    {
                        scale = 16f;
                        light = new Vector3(1f, 0.4f, 0.2f);
                    }
                    else
                    {
                        scale = sub == 1 ? 3f : sub == 2 ? 2.5f : 16f;
                        light = sub is 1 or 2 ? Vector3.One
                            : new Vector3(0.5f, 0.7f, 1f);
                    }
                    direction = ClassicMath.VectorRotate(
                        new Vector3(0f,
                            t == ClassicFxEffectType.BossLaserShort ? -15f : -50f,
                            0f), ClassicMath.AngleMatrix(angle));
                    break;

                case ClassicFxEffectType.FireTrail02:
                    life = 10f;
                    pos += ClassicMath.VectorRotate(
                        new Vector3(0f, -60f, 0f),
                        ClassicMath.AngleMatrix(angle)) * Clock.FrameFactor;
                    pos.Z += 130f * Clock.FrameFactor;
                    break;

                case ClassicFxEffectType.LightProjectile:
                {
                    life = 400f;
                    scale = 3f;
                    light = new Vector3(0.3f);
                    float speed = (9 + Random.Modulo(5)) * 0.5f;
                    const float el = 70f * MathF.PI / 180f;
                    const float az = 30f * MathF.PI / 180f;
                    direction = new Vector3(
                        speed * MathF.Cos(el) * MathF.Sin(az),
                        speed * MathF.Cos(el) * MathF.Cos(az),
                        speed * MathF.Sin(el));
                    break;
                }
                case ClassicFxEffectType.MagicGroundBase:
                    scale = 0.5f;
                    life = 20f;
                    break;
                case ClassicFxEffectType.ShotgunJointBurst:
                    life = 10f;
                    direction = new Vector3(0f, -30f, 0f);
                    pos += ClassicMath.VectorRotate(
                        new Vector3(0f, -20f, 50f),
                        ClassicMath.AngleMatrix(angle)) * Clock.FrameFactor;
                    break;
                case ClassicFxEffectType.FlareForceJointBurst:
                    life = 1f; // one-time joint fan-out
                    break;
                case ClassicFxEffectType.LightRedGround:
                    light = Vector3.One;
                    life = sub == 4 ? 50f : 100f;
                    break;
                case ClassicFxEffectType.ChromeEnergyGround:
                    // No explicit native CreateEffect initializer.
                    life = 30f;
                    break;
            }
        }

        private void EmitS6Batch39OnCreate(ClassicFxEffectType t,
            Vector3 pos, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, int sub)
        {
            if (t == ClassicFxEffectType.FlareForceJointBurst)
            {
                // Native CreateEffect(BITMAP_FLARE_FORCE): joint subtypes 0-13.
                if (sub == 0)
                {
                    for (int i = 0; i < 5; ++i)
                        CreateJoint(ClassicTextureIds.BitmapFlareForce,
                            pos, pos, angle, i, owner, i == 0 ? 250f : 100f);
                }
                else if (sub == 1)
                {
                    for (int i = 5; i <= 7; ++i)
                        CreateJoint(ClassicTextureIds.BitmapFlareForce,
                            pos, pos, angle, i, owner, 20f);
                }
                else
                {
                    int a = sub <= 4 ? 8 + (sub - 2) : 11 + (sub - 5);
                    int b = sub <= 4 ? 0 : 1;
                    CreateJoint(ClassicTextureIds.BitmapFlareForce,
                        pos, pos, angle, a, owner, 100f);
                    CreateJoint(ClassicTextureIds.BitmapFlareForce,
                        pos, pos, angle, b, owner, sub <= 4 ? 150f : 100f);
                }
            }
            else if (t == ClassicFxEffectType.ShotgunJointBurst)
            {
                // Two muzzle origins; 20 original BITMAP_JOINT_SPARK each.
                var m = ClassicMath.AngleMatrix(angle);
                for (int side = 0; side < 2; ++side)
                {
                    Vector3 p = pos + ClassicMath.VectorRotate(
                        new Vector3(side == 0 ? -20f : 30f, -20f, 60f), m);
                    Vector3 a = angle;
                    for (int i = 0; i < 20; ++i)
                    {
                        a.X = angle.X + Random.Modulo(20) + 5;
                        a.Y += i * 18f;
                        CreateJoint(ClassicTextureIds.BitmapJointSpark,
                            p, p, a, 1);
                    }
                }
            }
        }

        private bool MoveS6Batch39Logical(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.BossLaserBlue:
                case ClassicFxEffectType.BossLaserRed:
                case ClassicFxEffectType.BossLaserShort:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 p = e.Position;
                        for (int i = 0; i < 20; ++i)
                        {
                            int texture = ClassicTextureIds.BitmapSpark + 1;
                            Vector3 c = e.Light;
                            if (e.SubType is 1 or 2)
                            {
                                e.Angle.Z -= 0.1f * f;
                                texture = ClassicTextureIds.BitmapFire + 1;
                                c = e.SubType == 1 ? Vector3.One
                                    : new Vector3(0f, 0.2f, 1f);
                            }
                            CreateSprite(texture, p, e.Scale, c, e.Owner, e.Angle.Z);
                            p += e.Direction;
                        }
                    }
                    return true;
                case ClassicFxEffectType.FireTrail02:
                    if (Clock.AdvancedReferenceFrame)
                        CreateParticle(ClassicTextureIds.BitmapFire + 1,
                            e.Position, e.Angle, Vector3.One, 1);
                    return true;
                case ClassicFxEffectType.LightProjectile:
                    e.Position += e.Direction * f;
                    e.Direction.Z -= 0.01f * f;
                    if (Clock.AdvancedReferenceFrame)
                        CreateParticle(ClassicTextureIds.BitmapLight,
                            e.Position, e.Angle, e.Light, 1, e.Scale);
                    return e.Direction.Z >= -2f;
                case ClassicFxEffectType.MagicGroundBase:
                    e.Light *= MathF.Pow(1f / 1.1f, f);
                    e.Scale += 0.05f * f;
                    return true;
                case ClassicFxEffectType.LightRedGround:
                    if (e.SubType == 3)
                    {
                        if (e.Owner.WorldObject == null ||
                            !ReferenceEquals(e.Owner.WorldObject.World, World))
                            return false;
                        e.Position = e.Owner.WorldObject.WorldPosition.Translation;
                    }
                    return true;
                case ClassicFxEffectType.ShotgunJointBurst:
                case ClassicFxEffectType.FlareForceJointBurst:
                case ClassicFxEffectType.ChromeEnergyGround:
                    return true;
            }
            return false;
        }

        private void RenderS6Batch39Terrain(ref EffectState e)
        {
            int texture = e.Type switch
            {
                ClassicFxEffectType.MagicGroundBase => ClassicTextureIds.BitmapMagic,
                ClassicFxEffectType.LightRedGround => ClassicTextureIds.BitmapLightRed,
                ClassicFxEffectType.ChromeEnergyGround =>
                    ClassicTextureIds.BitmapChromeEnergy2,
                _ => -1
            };
            if (texture < 0 ||
                !Textures.TryGet(texture, out ClassicTextureResource resource))
                return;
            if (e.Type == ClassicFxEffectType.MagicGroundBase)
                QueueTerrainEffect(ref e, resource,
                    scaleOverride: e.Scale, angleZOverride: -e.Angle.Z);
            else if (e.Type == ClassicFxEffectType.LightRedGround)
            {
                Vector3 light = new Vector3(MathF.Max(0f, e.LifeTime * 0.01f));
                QueueTerrainEffect(ref e, resource,
                    scaleOverride: e.Scale, lightOverride: light,
                    angleZOverride: e.Angle.X);
                if (e.SubType == 4)
                    QueueTerrainEffect(ref e, resource,
                        scaleOverride: e.Scale, lightOverride: light,
                        angleZOverride: e.Angle.X);
            }
            else
                QueueTerrainEffect(ref e, resource, scaleOverride: e.Scale);
        }
    }
}
