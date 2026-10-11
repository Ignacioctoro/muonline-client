// ClassicFX Season 6 Batch 37 — ten native bitmap Effect roots.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// ZzzEffect.cpp, Behaviors/MoveHandlers.cpp, EffectTypes.json.
// All are model-free; use the existing pooled sprites, particles and joints.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static readonly int[] S6Batch37FirecrackerTicks =
            { 31, 24, 17, 9, 1 };

        private static bool IsS6Batch37LogicalType(ClassicFxEffectType t, int sub) =>
            t switch
            {
                ClassicFxEffectType.FirecrackerRise => sub == 0,
                ClassicFxEffectType.FirecrackerBurst => sub is 0 or 1,
                ClassicFxEffectType.FirecrackerSequence => sub is >= 0 and <= 3,
                ClassicFxEffectType.FirecrackerExplosion => sub is 0 or 1,
                ClassicFxEffectType.FirecrackerFlash => sub == 0,
                ClassicFxEffectType.CloudEffect => sub == 0,
                ClassicFxEffectType.OroraEffect => sub is >= 0 and <= 3,
                ClassicFxEffectType.GatheringEffect => sub is >= 0 and <= 3,
                ClassicFxEffectType.FireHik2MonoEffect => sub is 0 or 1,
                ClassicFxEffectType.PinLightEffect => sub is >= 0 and <= 4,
                _ => false
            };

        private static bool S6Batch37NeedsOwner(ClassicFxEffectType t, int sub) =>
            t == ClassicFxEffectType.OroraEffect ||
            (t == ClassicFxEffectType.GatheringEffect && sub is 1 or 2) ||
            (t == ClassicFxEffectType.PinLightEffect && sub is 1 or 2 or 4);

        private static bool IsS6Batch37GroundType(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.CloudEffect or
                ClassicFxEffectType.FireHik2MonoEffect;

        // Out parameter guarantees definite assignment in CreateEffect().
        private void InitializeS6Batch37Logical(
            ClassicFxEffectType type, int sub, ClassicFxOwner owner,
            ref Vector3 pos, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float alpha, ref Vector3 direction,
            out float life)
        {
            life = 30f; // Original Effect initializer's default lifetime.
            switch (type)
            {
                case ClassicFxEffectType.FirecrackerRise:
                    life = 75f;
                    pos.Z += 100f;
                    angle = Vector3.Zero;
                    break;
                case ClassicFxEffectType.FirecrackerBurst:
                    light = Vector3.One;
                    direction = new Vector3(Random.Modulo(9) - 4,
                        Random.Modulo(9) - 4, 26f);
                    life = sub == 1 ? 4f + Random.Modulo(3) : 12f;
                    break;
                case ClassicFxEffectType.FirecrackerSequence:
                    life = 31f;
                    break;
                case ClassicFxEffectType.FirecrackerExplosion:
                    life = 30f;
                    // Native creation burst: allocation bounded by existing
                    // ClassicFX particle/sprite pools, not a new renderer.
                    EmitS6Batch37Explosion(pos, angle, light, owner, sub);
                    break;
                case ClassicFxEffectType.FirecrackerFlash:
                    life = 15f;
                    angle.Z = Random.Modulo(360);
                    break;
                case ClassicFxEffectType.CloudEffect:
                    life = 60f;
                    angle.Z = Random.Modulo(360);
                    break;
                case ClassicFxEffectType.OroraEffect:
                    life = sub <= 1 ? 100f : 25f;
                    CreateParticle(ClassicTextureIds.BitmapOrora,
                        pos, angle, light, sub, 1f, owner);
                    break;
                case ClassicFxEffectType.GatheringEffect:
                    life = sub is 1 or 2 ? 20f : 10f;
                    if (sub == 0 || sub == 3)
                    {
                        Vector3 offset = sub == 0
                            ? new Vector3(0f, -100f, 0f)
                            : new Vector3(-10f, 10f, 0f);
                        pos += Vector3.TransformNormal(offset,
                            Matrix.CreateFromYawPitchRoll(
                                angle.Z, angle.X, angle.Y)) *
                            Clock.FrameFactor;
                        if (sub == 0) pos.Z += 150f * Clock.FrameFactor;
                    }
                    break;
                case ClassicFxEffectType.FireHik2MonoEffect:
                    // No explicit lifetime override in the native switch.
                    break;
                case ClassicFxEffectType.PinLightEffect:
                    life = sub is 1 or 2 ? 100f : sub == 4 ? 30f : 40f;
                    if (sub == 4)
                    {
                        alpha = 1f;
                        scale += Random.Modulo(5) * 0.1f * Clock.FrameFactor;
                        angle.Y = Random.Modulo(360);
                    }
                    break;
            }
        }

        private void EmitS6Batch37Explosion(
            Vector3 pos, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, int sub)
        {
            CreateParticle(ClassicTextureIds.BitmapExplotionMono,
                pos, angle, light, 0, 0.6f);
            for (int i = 0; i < 60; i++)
                CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                    pos, angle, Vector3.One, 27);
            for (int i = 0; i < 30; i++)
                CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                    pos, angle, Vector3.One, 28);

            Vector3 hit = pos + new Vector3(
                Random.Modulo(100) - 50, Random.Modulo(100) - 50, 0f);
            CreateSprite(ClassicTextureIds.BitmapDsShock, hit,
                1.5f + Random.Modulo(10) * 0.1f, light, owner);

            for (int i = 0; i < 60; i++)
            {
                Vector3 c = new Vector3(
                    0.3f + Random.Modulo(700) * 0.001f,
                    0.3f + Random.Modulo(700) * 0.001f,
                    0.3f + Random.Modulo(700) * 0.001f);
                CreateParticle(ClassicTextureIds.BitmapShiny,
                    pos, angle, c, 6);
            }
            if (sub == 1)
            {
                CreateEffect(ClassicFxEffectType.HalloweenCandyStar,
                    pos, angle, light, owner, subType: 1);
                for (int i = 0; i < 2; i++)
                    CreateEffect((ClassicFxEffectType)(
                        (int)ClassicFxEffectType.XmasEventBox +
                        Random.Modulo(4)), pos, angle, light, owner);
            }
        }

        private bool MoveS6Batch37Logical(ref EffectState e, float f)
        {
            // Native stochastic emissions occur at 25 Hz; physical fades
            // and displacement use FrameFactor at the display frame rate.
            switch (e.Type)
            {
                case ClassicFxEffectType.FirecrackerRise:
                    if (Clock.AdvancedReferenceFrame &&
                        ((int)e.LifeTime) % 5 == 0 &&
                        Random.Modulo(3) == 0)
                        CreateEffect(ClassicFxEffectType.FirecrackerBurst,
                            e.Position, e.Angle, e.Light, e.Owner);
                    return true;

                case ClassicFxEffectType.FirecrackerBurst:
                    if (!Clock.AdvancedReferenceFrame) return true;
                    if (e.LifeTime <= 1f && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        int sub = Random.Modulo(30);
                        Vector3 light = new Vector3(
                            0.4f + Random.Modulo(3) * 0.3f,
                            Random.Modulo(4) * 0.1f, 0f);
                        for (int i = 0; i < 80; i++)
                            CreateParticle(ClassicTextureIds.BitmapFirecracker,
                                e.Position, e.Angle, light, sub);
                    }
                    CreateParticle(ClassicTextureIds.BitmapFirecracker,
                        e.Position, e.Angle, e.Light, -1);
                    // Pending: native AddTerrainLight + audio event bridge.
                    return true;

                case ClassicFxEffectType.FirecrackerSequence:
                    if (Clock.AdvancedReferenceFrame)
                        EmitS6Batch37Sequence(ref e);
                    return true;

                case ClassicFxEffectType.FirecrackerExplosion:
                    if (Clock.AdvancedReferenceFrame && Random.Modulo(5) == 0)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(300) - 150,
                            Random.Modulo(300) - 150, 0f);
                        Vector3 c = new Vector3(
                            0.3f + Random.Modulo(700) * 0.001f,
                            0.3f + Random.Modulo(700) * 0.001f,
                            0.3f + Random.Modulo(700) * 0.001f);
                        CreateEffect(ClassicFxEffectType.FirecrackerFlash,
                            p, e.Angle, c, e.Owner,
                            scale: 0.5f + Random.Modulo(5) * 0.1f);
                    }
                    return true;

                case ClassicFxEffectType.FirecrackerFlash:
                    if (e.LifeTime <= 7f)
                    {
                        e.Position.Z -= f;
                        e.Light *= MathF.Pow(1f / 1.05f, f);
                        e.Scale *= MathF.Pow(1.02f, f);
                    }
                    if (Clock.AdvancedReferenceFrame)
                    {
                        int frame = e.LifeTime > 7f
                            ? Math.Clamp((int)((15f - e.LifeTime) * 7f / 8f), 0, 6)
                            : 6;
                        CreateSprite(ClassicTextureIds.BitmapFirecrackerFrame1 + frame,
                            e.Position, e.Scale, e.Light, e.Owner, e.Angle.Z);
                    }
                    return true;

                case ClassicFxEffectType.CloudEffect:
                    e.Light *= MathF.Pow(1f / 1.05f, f);
                    e.Scale += 0.03f * f;
                    return true;

                case ClassicFxEffectType.OroraEffect:
                    if (e.Owner.WorldObject == null ||
                        !ReferenceEquals(e.Owner.WorldObject.World, World) ||
                        e.Owner.WorldObject.Status != GameControlStatus.Ready)
                        return false;
                    if (e.LifeTime <= 5f)
                    {
                        if ((e.TriggerMask & 1) == 0)
                        {
                            e.TriggerMask |= 1;
                            CreateEffect(ClassicFxEffectType.OroraEffect,
                                e.Position, e.Angle, e.Light,
                                e.Owner, subType: e.SubType);
                        }
                        return false;
                    }
                    return true;

                case ClassicFxEffectType.GatheringEffect:
                    if (e.SubType is 1 or 2)
                    {
                        if (!TryGetOwnerBonePosition(e.Owner, 33,
                                out Vector3 hand)) return false;
                        e.Position = hand + new Vector3(0f, 0f, 10f * f);
                    }
                    if (Clock.AdvancedReferenceFrame)
                        EmitS6Batch37Gathering(ref e);
                    return true;

                case ClassicFxEffectType.FireHik2MonoEffect:
                    if (Clock.AdvancedReferenceFrame)
                        EmitS6Batch37FireHik(ref e);
                    return true;

                case ClassicFxEffectType.PinLightEffect:
                    return MoveS6Batch37PinLight(ref e, f);
            }
            return false;
        }

        private void EmitS6Batch37Sequence(ref EffectState e)
        {
            // Original 31/24/17/9/1 ticks, one joint per crossing.
            for (int i = 0; i < S6Batch37FirecrackerTicks.Length; i++)
            {
                byte mask = (byte)(1 << i);
                if (e.LifeTime > S6Batch37FirecrackerTicks[i] ||
                    (e.TriggerMask & mask) != 0)
                    continue;
                e.TriggerMask |= mask;
                Vector3 p = e.Position + new Vector3(
                    Random.Modulo(200) - 100,
                    Random.Modulo(200) - 100, 0f);
                CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                    p, p, e.Angle, 25, e.Owner, 1f);
            }
        }

        private void EmitS6Batch37Gathering(ref EffectState e)
        {
            for (int j = 0; j < 3; j++)
            {
                float length = e.SubType == 3 ? 25f : 120f;
                Vector3 a = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)), 0f,
                    MathHelper.ToRadians(Random.Modulo(360)));
                Vector3 p = e.Position + Vector3.TransformNormal(
                    new Vector3(0f, length, 0f),
                    Matrix.CreateFromYawPitchRoll(a.Z, a.X, a.Y));
                if (e.SubType is 1 or 2)
                {
                    if (((int)e.LifeTime % 2) == 0)
                        CreateJoint(ClassicTextureIds.BitmapJointThunder,
                            p, e.Position, a, 3, ClassicFxOwner.None, 10f);
                    else if (e.SubType == 1)
                        CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                            p, a, Vector3.One, 2,
                            (Random.Modulo(50) + 10) / 100f);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        e.Position, (Random.Modulo(8) + 8) * 0.2f,
                        e.Light, e.Owner, Random.Modulo(360));
                }
                else if (e.SubType == 3)
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        p, a, e.Light, 26,
                        (Random.Modulo(10) + 5) / 25f);
                else
                {
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        e.Position, (Random.Modulo(8) + 8) * 0.3f,
                        e.Light, e.Owner, Random.Modulo(360));
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        p, a, Vector3.One, 2,
                        (Random.Modulo(50) + 10) / 100f);
                }
            }
        }

        private void EmitS6Batch37FireHik(ref EffectState e)
        {
            if (e.SubType == 0)
            {
                for (int i = 0; i < 2; ++i)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(30) - 15,
                        Random.Modulo(30) - 15, 0f);
                    if (Random.Modulo(3) != 0)
                        CreateParticle(ClassicTextureIds.BitmapFlame,
                            p, e.Angle, e.Light, 11, 1.4f);
                    if (Random.Modulo(8) == 0)
                        CreateEffect(ClassicFxEffectType.IceSmall,
                            p, e.Angle, e.Light);
                }
            }
            else
            {
                for (int j = 0; j < 9; ++j)
                {
                    float angle = MathHelper.ToRadians(j * 40f);
                    Vector3 p = e.Position + new Vector3(
                        -250f * MathF.Sin(angle) + Random.Modulo(64) - 32,
                        250f * MathF.Cos(angle) + Random.Modulo(64) - 32,
                        0f);
                    CreateParticle(ClassicTextureIds.BitmapFire + 3,
                        p, e.Angle, Vector3.One, 13, 2.5f);
                }
            }
        }

        private bool MoveS6Batch37PinLight(ref EffectState e, float f)
        {
            if (e.SubType is 1 or 2 or 4)
            {
                if (e.Owner.WorldObject is not ModelObject model ||
                    !ReferenceEquals(model.World, World) ||
                    model.Status != GameControlStatus.Ready)
                    return false;
                if (e.SubType is 1 or 2)
                {
                    e.LifeTime = 100f;
                    if (!Clock.AdvancedReferenceFrame) return true;
                    Matrix[] bones = model.GetBoneTransforms();
                    if (bones == null || bones.Length == 0) return true;
                    int bone = Random.Modulo(bones.Length);
                    Vector3 p = Vector3.Transform(
                        new Vector3(0f, 0f, 100f),
                        bones[bone] * model.WorldPosition);
                    p.Z -= 20f;
                    if (Random.Modulo(2) == 0)
                        CreateParticle(ClassicTextureIds.BitmapPinLight,
                            p, e.Angle, e.Light, 0, e.Scale);
                    return true;
                }
                e.Scale -= 0.02f * f;
                e.Alpha -= 0.001f * f;
                if (e.Alpha <= 0f || e.Scale <= 0f) return false;
                if (Clock.AdvancedReferenceFrame &&
                    TryGetOwnerBonePosition(e.Owner, 11, out Vector3 p11))
                {
                    e.Position = p11;
                    CreateSprite(ClassicTextureIds.BitmapPinLight,
                        p11, e.Scale, e.Light, e.Owner, e.Angle.Y);
                }
                return true;
            }
            if (!Clock.AdvancedReferenceFrame) return true;
            Vector3 p0 = e.Position + new Vector3(
                Random.Modulo(500) - 250,
                Random.Modulo(500) - 250,
                150f - Random.Modulo(100));
            if (Random.Modulo(2) == 0)
                CreateParticle(ClassicTextureIds.BitmapPinLight,
                    p0, e.Angle, e.Light,
                    e.SubType == 3 ? 1 : 0, e.Scale);
            return true;
        }

        private void RenderS6Batch37Ground(ref EffectState e)
        {
            if (e.Type == ClassicFxEffectType.FireHik2MonoEffect &&
                e.SubType != 0) return;
            int texId = e.Type == ClassicFxEffectType.CloudEffect
                ? ClassicTextureIds.BitmapCloud
                : ClassicTextureIds.BitmapLightning + 1;
            if (!Textures.TryGet(texId, out ClassicTextureResource tex))
                return;
            QueueTerrainEffect(ref e, tex,
                scaleOverride: e.Type == ClassicFxEffectType.CloudEffect
                    ? e.Scale : 2f);
        }
    }
}
