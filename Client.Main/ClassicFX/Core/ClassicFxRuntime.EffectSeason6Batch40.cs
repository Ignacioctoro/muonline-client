// BroyalMU ClassicFX — Season 6 Batch 40.
// Pinned MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp / Behaviors/MoveHandlers.cpp; real bitmap / carrier roots.
// Existing Effect/Particle/Sprite/Joint/terrain pools only.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch40LogicalType(ClassicFxEffectType t, int sub) =>
            t switch
            {
                ClassicFxEffectType.SkullEffect => sub is 1 or 2 or 3 or 4 or 5,
                ClassicFxEffectType.FlareParticleEffect => sub is 1 or 2 or 3,
                ClassicFxEffectType.SwordEffCarrier => sub == 0,
                ClassicFxEffectType.JointForceCarrier => sub == 0,
                ClassicFxEffectType.SbumbImpactEmitter => sub == 0,
                ClassicFxEffectType.Damage1ImpactEmitter => sub == 0,
                ClassicFxEffectType.IceBreathCloudCarrier => sub == 0,
                ClassicFxEffectType.LavaGiantFootprintRed => sub == 0,
                ClassicFxEffectType.LavaGiantFootprintViolet => sub == 0,
                ClassicFxEffectType.FireHik3MonoCarrier => sub == 0,
                _ => false
            };

        private static bool S6Batch40NeedsOwner(ClassicFxEffectType t, int sub) =>
            (t == ClassicFxEffectType.SkullEffect && sub != 4) ||
            t is ClassicFxEffectType.SwordEffCarrier or
                ClassicFxEffectType.SbumbImpactEmitter or
                ClassicFxEffectType.Damage1ImpactEmitter;

        private static bool S6Batch40NeedsModelOwner(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.SwordEffCarrier or
                ClassicFxEffectType.SbumbImpactEmitter or
                ClassicFxEffectType.Damage1ImpactEmitter;

        private static bool S6Batch40NeedsTarget(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.SbumbImpactEmitter or
                ClassicFxEffectType.Damage1ImpactEmitter;

        private static bool IsS6Batch40TerrainType(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.LavaGiantFootprintRed or
                ClassicFxEffectType.LavaGiantFootprintViolet;

        private void InitializeS6Batch40Logical(
            ClassicFxEffectType t, int sub, ClassicFxOwner owner,
            ref Vector3 pos, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref Vector3 direction,
            ref float velocity, ref float alpha, out float life)
        {
            life = 20f;
            switch (t)
            {
                case ClassicFxEffectType.SkullEffect:
                    life = sub == 2 ? 8f : sub == 4 ? 20f : 1000f;
                    if (sub == 1)
                        life -= 60f * Clock.FrameFactor;
                    else if (sub == 2)
                        pos.Z += 150f * Clock.FrameFactor;
                    else if (sub == 4)
                    {
                        direction = new Vector3(0f, -10f, 0f);
                        velocity = 2f;
                    }
                    break;

                case ClassicFxEffectType.FlareParticleEffect:
                    life = sub == 3 ? 60f : 30f;
                    break;

                case ClassicFxEffectType.SwordEffCarrier:
                    life = 200f;
                    break;

                case ClassicFxEffectType.JointForceCarrier:
                    life = 20f;
                    direction = new Vector3(0f, -550f, 0f);
                    angle = new Vector3(-90f, 0f, angle.Z);
                    break;

                case ClassicFxEffectType.SbumbImpactEmitter:
                case ClassicFxEffectType.Damage1ImpactEmitter:
                    life = 20f;
                    // Native CreateEffect() anchors impact height at owner bone 27.
                    if (TryGetOwnerBonePosition(owner, 27, out Vector3 bone))
                        pos.Z = bone.Z;
                    break;

                case ClassicFxEffectType.IceBreathCloudCarrier:
                    life = 17f;
                    light = new Vector3(0.6f);
                    pos += ClassicMath.VectorRotate(new Vector3(0f, -1f, 0f),
                        ClassicMath.AngleMatrix(angle)) * 50f;
                    break;

                case ClassicFxEffectType.LavaGiantFootprintRed:
                case ClassicFxEffectType.LavaGiantFootprintViolet:
                    life = 200f;
                    pos.Z = 0f;
                    alpha = 1f;
                    break;

                case ClassicFxEffectType.FireHik3MonoCarrier:
                    life = 1f; // original case only emits a particle on creation
                    break;
            }
        }

        private void EmitS6Batch40OnCreate(ClassicFxEffectType t, int sub,
            Vector3 pos, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, float scale)
        {
            switch (t)
            {
                case ClassicFxEffectType.JointForceCarrier:
                    CreateJoint(ClassicTextureIds.BitmapJointForce,
                        pos, pos, angle, 3, ClassicFxOwner.None, 80f);
                    break;

                case ClassicFxEffectType.IceBreathCloudCarrier:
                    // Native computes one random displacement shared by 4
                    // initial particles, all at the 50-unit forward point.
                    EmitS6Batch40IceClouds(pos, angle, light, scale, 0);
                    break;

                case ClassicFxEffectType.FireHik3MonoCarrier:
                    if (Random.Modulo(100) > 20)
                        CreateParticle(ClassicTextureIds.BitmapFireHik3Mono,
                            pos, angle, light, 1, scale);
                    break;
            }
        }

        private void EmitS6Batch40IceClouds(
            Vector3 pos, Vector3 angle, Vector3 light, float scale, int sub)
        {
            float random = (Random.Modulo(2000) - 1000) * 0.001f;
            float offset = 20f * random;
            float rotation = (sub == 0 ? 5f : 15f) * random;
            float size = scale + 0.01f * random;
            Vector3 p = pos + new Vector3(offset);
            Vector3 a = angle + new Vector3(rotation);
            for (int i = 0; i < 4; i++)
                CreateParticle(ClassicTextureIds.BitmapRaklionClouds,
                    p, a, light, sub, size);
        }

        private bool MoveS6Batch40Logical(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.SkullEffect:
                    return MoveS6Batch40Skull(ref e, f);

                case ClassicFxEffectType.FlareParticleEffect:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        if (e.SubType == 1)
                            CreateParticle(ClassicTextureIds.BitmapFlare,
                                e.Position, e.Angle, Vector3.One, 11, 1f);
                        else if (e.SubType == 2)
                            CreateParticle(ClassicTextureIds.BitmapFlareBlue,
                                e.Position, e.Angle, Vector3.One, 1, 1f);
                        else
                            CreateParticle(ClassicTextureIds.BitmapLight,
                                e.Position, e.Angle,
                                new Vector3(0.9f, 0.4f, 0.1f), 5,
                                3.5f + (Random.Modulo(20) - 10) * 0.1f);
                    }
                    return true;

                case ClassicFxEffectType.SwordEffCarrier:
                    if (!S6Batch40OwnerIsAlive(e.Owner))
                        return false;
                    if (Clock.AdvancedReferenceFrame &&
                        TryGetOwnerBonePosition(e.Owner, 5, out Vector3 from) &&
                        TryGetOwnerBonePosition(e.Owner, 4, out Vector3 to))
                        CreateJoint(ClassicTextureIds.BitmapSwordEff,
                            from, to, e.Owner.WorldObject.Angle, 0,
                            e.Owner, 100f);
                    return true;

                case ClassicFxEffectType.JointForceCarrier:
                    if (Clock.AdvancedReferenceFrame &&
                        e.LifeTime < 11f && ((int)e.LifeTime & 1) == 0)
                    {
                        if ((e.TriggerMask & 1) == 0)
                        {
                            e.TriggerMask |= 1;
                            e.HeadAngle.Z = Random.Modulo(360);
                        }
                        e.HeadAngle.Z += 72f * f;
                        Vector3 p = e.StartPosition +
                            ClassicMath.VectorRotate(e.Direction,
                                ClassicMath.AngleMatrix(e.HeadAngle));
                        p.Z += 700f + Random.Modulo(400);
                        CreateJoint(ClassicTextureIds.BitmapFlash,
                            p, p, new Vector3(90f, 0f, 0f),
                            5, ClassicFxOwner.None, 110f);
                    }
                    return true;

                case ClassicFxEffectType.SbumbImpactEmitter:
                case ClassicFxEffectType.Damage1ImpactEmitter:
                    return MoveS6Batch40Impact(ref e);

                case ClassicFxEffectType.IceBreathCloudCarrier:
                    if (Clock.AdvancedReferenceFrame && e.LifeTime > 1f)
                        EmitS6Batch40IceClouds(e.StartPosition, e.Angle,
                            new Vector3(0.5f, 0.6f, 0.94f), e.Scale, 1);
                    return true;

                case ClassicFxEffectType.LavaGiantFootprintRed:
                case ClassicFxEffectType.LavaGiantFootprintViolet:
                    e.Angle.X += 0.1f * f;
                    e.Alpha -= 0.01f * f;
                    e.Light *= MathF.Pow(0.93f, f);
                    return e.Alpha > 0f;

                case ClassicFxEffectType.FireHik3MonoCarrier:
                    return false; // no native ongoing emitter; release after creation
            }
            return false;
        }

        private bool S6Batch40OwnerIsAlive(ClassicFxOwner owner) =>
            owner.WorldObject != null &&
            ReferenceEquals(owner.WorldObject.World, World) &&
            owner.WorldObject.Status == GameControlStatus.Ready;

        private bool MoveS6Batch40Skull(ref EffectState e, float f)
        {
            if (e.SubType == 4)
            {
                e.Direction.Y -= e.Velocity * f;
                e.Angle.X -= f;
                if (Clock.AdvancedReferenceFrame)
                    CreateSprite(ClassicTextureIds.BitmapSkull,
                        e.Position, 10f, e.Light, ClassicFxOwner.None);
                return true;
            }
            if (!S6Batch40OwnerIsAlive(e.Owner)) return false;
            if (!Clock.AdvancedReferenceFrame) return true;

            Vector3 ownerPos = e.Owner.WorldObject.WorldPosition.Translation;
            if (e.SubType is 1 or 3)
            {
                float time = (float)Clock.WorldTimeMilliseconds * 0.0031f;
                for (int i = 0; i < 3; i++)
                {
                    float theta = i * MathF.PI * (2f / 3f) +
                                  e.LifeTime * 0.17f;
                    if (e.SubType == 3) theta = -theta;
                    float distance = 50f + 20f * MathF.Sin(i * 15.37f + time);
                    Vector3 p = ownerPos + new Vector3(
                        distance * MathF.Sin(theta),
                        distance * MathF.Cos(theta),
                        e.SubType == 3 ? 250f : 200f);
                    CreateSprite(ClassicTextureIds.BitmapSkull,
                        p, 1f, Vector3.One, e.Owner);
                }
            }
            else if (e.SubType == 2)
            {
                CreateSprite(ClassicTextureIds.BitmapSkull, e.Position,
                    1.5f, new Vector3(e.LifeTime * 0.1f), e.Owner);
            }
            else if (e.SubType == 5)
            {
                float time = (float)Clock.WorldTimeMilliseconds * 0.003f;
                for (int i = 0; i < 3; i++)
                {
                    float theta = i * MathF.PI * (2f / 3f) +
                                  e.LifeTime * 0.17f + time;
                    Vector3 p = ownerPos + new Vector3(
                        50f * MathF.Sin(theta), 50f * MathF.Cos(theta),
                        (i + 1) * 50f);
                    CreateParticle(ClassicTextureIds.BitmapLight, p, e.Angle,
                        new Vector3(0.6f, 0.6f, 1f), 5, 0.7f);
                }
            }
            return true;
        }

        private bool MoveS6Batch40Impact(ref EffectState e)
        {
            if (!S6Batch40OwnerIsAlive(e.Owner) ||
                e.TargetWorldObject == null ||
                !ReferenceEquals(e.TargetWorldObject.World, World) ||
                e.TargetWorldObject.Status != GameControlStatus.Ready)
                return false;
            if (!Clock.AdvancedReferenceFrame || e.LifeTime >= 17f)
                return true;
            bool sbumb = e.Type == ClassicFxEffectType.SbumbImpactEmitter;
            if (!sbumb && ((int)e.LifeTime % 4) == 0)
                return true;
            Vector3 p = e.TargetWorldObject.WorldPosition.Translation;
            p += sbumb
                ? new Vector3(Random.Modulo(80) - 40,
                    Random.Modulo(80) - 40, Random.Modulo(120) + 30)
                : new Vector3(Random.Modulo(90) - 40,
                    Random.Modulo(90) - 40, Random.Modulo(100) + 50);
            Vector3 light = sbumb ? Vector3.One
                : new Vector3(0.6f, 0.94f, 1f);
            float scale = sbumb
                ? 1.5f + Random.Modulo(10) * 0.02f
                : 1.5f + Random.Modulo(5) * 0.01f;
            CreateParticle(sbumb ? ClassicTextureIds.BitmapSbumb
                    : ClassicTextureIds.BitmapDamage1,
                p, e.Angle, light, 0, scale * e.Scale, e.Owner);
            return true;
        }

        private void RenderS6Batch40Terrain(ref EffectState e)
        {
            int texture = e.Type == ClassicFxEffectType.LavaGiantFootprintRed
                ? ClassicTextureIds.BitmapLavaGiantFootprintR
                : ClassicTextureIds.BitmapLavaGiantFootprintV;
            if (!Textures.TryGet(texture, out ClassicTextureResource res))
                return;
            QueueTerrainEffect(ref e, res, scaleOverride: e.Scale,
                lightOverride: e.Light * MathF.Max(0f, e.Alpha));
        }
    }
}
