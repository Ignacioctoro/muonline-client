// ClassicFX native Season 6 MODEL_LIGHTNING_SHOCK: procedural emitter,
// no fake BMD. MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect + Behaviors/MoveHandlers.cpp.
// BitmapDamage01Mono and KnightPlancrackA use the same Effect pool.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Start original lightning-shock emitter on caster (subtype 0)
        /// or its impact (1), or a target-bound residual (2).
        /// Null/model-free emitter survives only while its owner is valid.
        /// </summary>
        public ClassicFxHandle CreateLightningShock(
            WorldObject owner, Vector3 position, Vector3 angle,
            int subType = 0)
        {
            if (_disposed || !Enabled || owner == null ||
                !ReferenceEquals(owner.World, World) ||
                owner.Status != GameControlStatus.Ready ||
                (subType != 0 && subType != 1 && subType != 2) ||
                !Pools.Effects.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            float lifespan = subType == 0 ? 20f : subType == 1 ? 12f : 15f;
            _effects[handle.Index] = new EffectState
            {
                Type = ClassicFxEffectType.LightningShock,
                SubType = subType,
                Owner = ClassicFxOwner.FromWorldObject(owner),
                Position = position + (subType == 0
                    ? new Vector3(0f, 0f, 280f * Clock.FrameFactor) :
                    Vector3.Zero),
                Angle = angle,
                Light = Vector3.One,
                Scale = 1f,
                Gravity = subType == 0 ? 1f : 0f,
                Alpha = 1f,
                LifeTime = lifespan,
                FirstMove = true
            };
            return handle;
        }

        private static void MoveDamage01Mono(ref EffectState e, float f)
        {
            e.Scale += (e.SubType == 0 ? 5f : 0.5f) * f;
            if (e.SubType == 1 && e.Scale > 3.5f)
                e.Light *= MathF.Pow(0.5f, f);
        }

        private bool MoveLightningShock(ref EffectState e, float f)
        {
            WorldObject owner = e.Owner.WorldObject;
            if (owner == null || owner.Status != GameControlStatus.Ready ||
                !ReferenceEquals(owner.World, World))
                return false;

            if (e.SubType == 0)
                return MoveLightningShockCaster(ref e, owner, f);

            if (e.SubType == 1)
            {
                if (e.FirstMove)
                {
                    e.FirstMove = false;
                    EmitLightningShockImpact(ref e, owner,
                        new Vector3(1f, 0.4f, 0.2f), 1f);
                }
                if (Clock.AdvancedReferenceFrame)
                    EmitLightningShockAftermath(ref e);
                return true;
            }

            if (e.SubType == 2)
            {
                if (e.FirstMove)
                {
                    e.FirstMove = false;
                    EmitLightningShockImpact(ref e, owner,
                        new Vector3(1f, 0.4f, 0.2f), 0.5f);
                }
                if (Clock.AdvancedReferenceFrame)
                    EmitLightningShockTargetSparks(ref e, owner);
                return true;
            }
            return false;
        }

        private bool MoveLightningShockCaster(
            ref EffectState e, WorldObject owner, float f)
        {
            if (e.LifeTime < 15f)
            {
                // After cast animation, native direction descends into
                // terrain; all speeds originate in Move_MODEL_LIGHTNING_SHOCK.
                e.Gravity += 0.1f * f;
                e.Velocity += e.Gravity * f;
                e.Position += new Vector3(0f, -20f, -75f - e.Velocity) * f;
            }

            if (Clock.AdvancedReferenceFrame)
            {
                ClassicFxOwner fxOwner = e.Owner;
                float spin = (float)(Clock.WorldTimeMilliseconds * 0.0006) * 360f;
                Vector3 hot = new Vector3(1f, 0.4f, 0.4f);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, 3.2f, hot, fxOwner, spin);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, 2.4f, hot, fxOwner, -spin);
                hot = new Vector3(1f, 0.2f, 0.2f);
                CreateSprite(ClassicTextureIds.BitmapMagic,
                    e.Position, 0.8f, hot, fxOwner, spin);
                CreateSprite(ClassicTextureIds.BitmapMagic,
                    e.Position, 0.4f, hot, fxOwner, -spin);
                for (int k = 0; k < 2; k++)
                    CreateSprite(ClassicTextureIds.BitmapPinLight,
                        e.Position, 1.6f, new Vector3(1f, 0.4f, 0.4f),
                        fxOwner, Random.Modulo(360));
                CreateParticle(ClassicTextureIds.BitmapMagic,
                    e.Position, e.Angle, new Vector3(1f, 0.3f, 0.3f),
                    subType: 0, scale: 0.8f);

                // MuMain: 11 light flashes with independent random offsets.
                for (int k = 0; k < 11; k++)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(70) - 35,
                        Random.Modulo(70) - 35,
                        Random.Modulo(70) - 35);
                    CreateSprite(ClassicTextureIds.BitmapLight, p, 2.2f,
                        new Vector3(1f, 0.2f, 0.1f), fxOwner);
                    if (Random.Modulo(3) == 0)
                        CreateParticle(ClassicTextureIds.BitmapLightningMega1
                            + Random.Modulo(3), p, e.Angle,
                            new Vector3(1f, 0.7f, 0.4f), 0,
                            (32f + Random.Modulo(80)) * 0.01f);
                }

                if (owner is ModelObject actor)
                {
                    Matrix[] bones = actor.GetBoneTransforms();
                    if (bones != null && bones.Length > 0)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            int i = Random.Modulo(Math.Min(bones.Length, 41));
                            if (!TryGetOwnerBonePosition(e.Owner, i,
                                out Vector3 bonePos)) continue;
                            bonePos += new Vector3(
                                Random.Modulo(30) - 15,
                                Random.Modulo(30) - 15,
                                Random.Modulo(30) - 15);
                            CreateParticle(ClassicTextureIds.BitmapLightningMega1
                                + Random.Modulo(3), bonePos, e.Angle,
                                new Vector3(1f, 0.5f, 0.4f), 0,
                                (22f + Random.Modulo(60)) * 0.01f);
                        }
                    }
                }
            }

            if (World?.Terrain != null &&
                e.Position.Z < World.Terrain.RequestTerrainRenderHeight(
                    e.Position.X, e.Position.Y))
            {
                // The native emitter creates subtype 1 on terrain impact.
                CreateLightningShock(owner, e.Position, e.Angle, subType: 1);
                return false;
            }
            return true;
        }

        private void EmitLightningShockImpact(ref EffectState e,
            WorldObject owner, Vector3 light, float baseScale)
        {
            // Exactly two expanding mono terrain textures and five
            // BITMAP_MAGIC/12 positions from native CreateEffect().
            if (e.SubType == 1)
            {
                CreateEffect(ClassicFxEffectType.Damage01Mono,
                    e.Position, e.Angle, new Vector3(1f, 0.8f, 0.5f),
                    ClassicFxOwner.None, subType: 1);
                CreateEffect(ClassicFxEffectType.Damage01Mono,
                    e.Position, e.Angle, new Vector3(1f, 0f, 0f),
                    ClassicFxOwner.None, subType: 1);
                for (int i = 0; i < 5; i++)
                {
                    float theta = e.Angle.Z + MathHelper.ToRadians(72f * i);
                    Vector3 p = e.Position + new Vector3(
                        -MathF.Sin(theta) * 150f,
                        MathF.Cos(theta) * 150f, 0f);
                    CreateEffect(ClassicFxEffectType.MagicGround, p,
                        new Vector3(0f, 0f, MathHelper.ToDegrees(theta)),
                        new Vector3(1f, 0.2f, 0.05f),
                        ClassicFxOwner.None, subType: 12);
                }
            }

            Vector3 groundPos = e.Position;
            if (World?.Terrain != null)
                groundPos.Z = World.Terrain.RequestTerrainRenderHeight(
                    groundPos.X, groundPos.Y) + 10f;
            for (int i = 0; i < 3; i++)
                CreateEffect(ClassicFxEffectType.KnightPlancrackA,
                    groundPos, e.Angle, light, ClassicFxOwner.None,
                    subType: 1,
                    scale: baseScale + Random.Modulo(4) * 0.1f);
        }

        private void EmitLightningShockAftermath(ref EffectState e)
        {
            Vector3 red = new Vector3(1f, 0f, 0f);
            for (int i = 0; i < 11; i++)
            {
                Vector3 p = e.Position + new Vector3(
                    Random.Modulo(70) - 35,
                    Random.Modulo(70) - 35,
                    Random.Modulo(70) - 35);
                CreateParticle(ClassicTextureIds.BitmapLightningMega1
                    + Random.Modulo(3), p, e.Angle,
                    new Vector3(1f, 0.7f, 0.4f), 0,
                    (32f + Random.Modulo(80)) * 0.01f);
            }
            for (int i = 0; i < 6; i++)
            {
                float radius = Random.Modulo(400);
                float theta = MathHelper.ToRadians(Random.Modulo(360));
                Vector3 p = e.Position + new Vector3(
                    -MathF.Sin(theta) * radius,
                    MathF.Cos(theta) * radius, 0f);
                if (World?.Terrain != null)
                    p.Z = World.Terrain.RequestTerrainRenderHeight(
                        p.X, p.Y) + 20f;
                CreateParticle(ClassicTextureIds.BitmapLightningMega1
                    + Random.Modulo(3), p, e.Angle, red, 0,
                    (22f + Random.Modulo(60)) * 0.01f);
            }
            Vector3 ground = e.Position;
            if (World?.Terrain != null)
                ground.Z = World.Terrain.RequestTerrainRenderHeight(
                    ground.X, ground.Y) + 10f;
            for (int i = 0; i < 2; i++)
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    ground, e.Angle, red, 58, 1f);
            if (Random.Modulo(2) == 0)
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    ground, e.Angle, red, 54, 2.8f);
            CreateEffect(ClassicFxEffectType.Stone1, ground,
                e.Angle, red, ClassicFxOwner.None, subType: 0);
        }

        private void EmitLightningShockTargetSparks(
            ref EffectState e, WorldObject owner)
        {
            if (owner is not ModelObject actor)
                return;
            Matrix[] bones = actor.GetBoneTransforms();
            if (bones == null || bones.Length == 0)
                return;
            for (int i = 0; i < bones.Length; i++)
            {
                if (!TryGetOwnerBonePosition(e.Owner, i, out Vector3 bonePos))
                    continue;
                if (Random.Modulo(60) == 0)
                    CreateParticle(ClassicTextureIds.BitmapLight,
                        bonePos, e.Angle, new Vector3(0.8f, 0f, 0f),
                        5, 5f + (Random.Modulo(20) - 10f) * 0.1f);
                if (Random.Modulo(5) == 0)
                    CreateParticle(ClassicTextureIds.BitmapLightningMega1
                        + Random.Modulo(3), bonePos, e.Angle,
                        new Vector3(1f, 0.8f, 0.4f), 0,
                        (22f + Random.Modulo(70)) * 0.01f);
            }
            CreateParticle(ClassicTextureIds.BitmapSmoke,
                e.Position, e.Angle, new Vector3(1f, 0f, 0f), 58);
        }
    }
}
