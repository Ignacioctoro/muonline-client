// BroyalMU ClassicFX Season 6 / Batch 10.
// Exact original reference: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp, Engine/Object/ZzzOpenData.cpp, Behaviors/MoveHandlers.cpp.
// Families: MODEL_WAVES (0-6), MODEL_PIERCING2 (0-2), MODEL_PIER_PART (0-2).
// Uses existing Effect/Joint/Particle pools and MonoGame BMD renderer.
// All child emissions use native 25-Hz ticks, never render FPS.
// Combat and networking remain exclusively with OpenMU/client gameplay.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch10ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Waves or
                ClassicFxEffectType.Piercing2 or
                ClassicFxEffectType.PierPart;

        private static bool TryGetS6Batch10ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.Waves:
                    if (subType is < 0 or > 6) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/m_waves.bmd", 20f, 1f);
                    return true;
                case ClassicFxEffectType.Piercing2:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/m_Piercing.bmd", subType == 1 ? 6f : 10f,
                        subType == 2 ? 3f : 2f);
                    return true;
                case ClassicFxEffectType.PierPart:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/PierPart.bmd", 20f,
                        subType == 0 ? 1.2f : 0.5f,
                        needsOwner: subType == 1 || subType == 2);
                    return true;
                default:
                    return false;
            }
        }

        private void InitializeS6Batch10Spawn(
            ClassicFxEffectType type, int subType, int nativePkKey,
            ref Vector3 position, ref Vector3 angle,
            ref Vector3 light, ref float scale, ref float life,
            ref float alpha, ref float meshLight,
            ref Vector3 direction, ref float velocity,
            ref float gravity, ref Vector3 headAngle)
        {
            switch (type)
            {
                case ClassicFxEffectType.Waves:
                    // MODEL_WAVES copies Position into StartPosition, resets
                    // Angle, raises Z by 50 and starts as a bright 20-tick BMD.
                    life = 20f;
                    gravity = 0.1f;
                    meshLight = 1f;
                    angle = Vector3.Zero;
                    position.Z += 50f * Clock.FrameFactor;
                    switch (subType)
                    {
                        case 1:
                            life = 15f;
                            position.Z += 80f * Clock.FrameFactor;
                            scale = 0.1f + Random.Modulo(50) / 100f;
                            gravity = 0.01f;
                            angle.X = MathHelper.PiOver2;
                            position.X += (Random.Modulo(100) - 50f) *
                                Clock.FrameFactor;
                            position.Y += (Random.Modulo(100) - 50f) *
                                Clock.FrameFactor;
                            break;
                        case 2:
                        case 3:
                        case 4:
                            life = 15f;
                            // Native passes its PKKey via CreateEffect;
                            // ClassicFX reserves boneIndex for this integer
                            // only for the three MODEL_WAVES render variants.
                            scale = Math.Max(0, nativePkKey) * 0.05f;
                            gravity = 0.01f;
                            position.Z -= 50f * Clock.FrameFactor;
                            angle.X = MathHelper.PiOver2;
                            break;
                        case 5:
                        case 6:
                            life = 10f;
                            scale = Math.Max(0, nativePkKey) * 0.05f;
                            gravity = 0.08f;
                            position.Z -= 50f * Clock.FrameFactor;
                            angle.X = -MathHelper.PiOver2;
                            break;
                    }
                    break;

                case ClassicFxEffectType.Piercing2:
                    life = subType == 1 ? 6f : 10f;
                    scale = subType == 2 ? 3f : 2f;
                    direction = new Vector3(0f, -60f, 0f);
                    position.Z += 130f * Clock.FrameFactor;
                    meshLight = 1f;
                    break;

                case ClassicFxEffectType.PierPart:
                    life = 20f;
                    if (subType == 0)
                    {
                        gravity = 2f;
                        velocity = 10f;
                        scale = 1.2f;
                        light = Vector3.One;
                        direction = new Vector3(0f, -26f, 0f);
                        headAngle = angle;
                        angle = new Vector3(0f, 0f, angle.Z);
                    }
                    else if (subType == 1)
                    {
                        scale = 0.5f;
                        alpha = 1f;
                        direction = Vector3.Zero;
                    }
                    else
                    {
                        velocity = 50f;
                        position.Z -= 20f * Clock.FrameFactor;
                        direction = new Vector3(0f, -40f, 0f);
                        headAngle = angle;
                        angle = new Vector3(0f, 0f, angle.Z);
                    }
                    break;
            }
        }

        private static void ConfigureS6Batch10ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            if (type == ClassicFxEffectType.Waves)
            {
                // Native sets BlendMesh = -2 for MODEL_WAVES.
                view.BlendMesh = -2;
            }
            else if (type == ClassicFxEffectType.Piercing2)
            {
                view.BlendMesh = -2;
                // RenderEffects intentionally omits Piercing2 variants 1/2.
                if (subType is 1 or 2) view.HiddenMesh = -2;
            }
            else if (type == ClassicFxEffectType.PierPart)
            {
                view.HiddenMesh = subType == 0 ? 1 :
                    subType == 1 ? 0 : -2;
            }
        }

        private bool MoveS6Batch10Model(ref EffectState e, float f)
        {
            if (e.FirstMove)
                SpawnS6Batch10InitialChildren(ref e);
            switch (e.Type)
            {
                case ClassicFxEffectType.Waves:
                    return MoveS6Waves(ref e, f);
                case ClassicFxEffectType.Piercing2:
                    return MoveS6Piercing2(ref e, f);
                case ClassicFxEffectType.PierPart:
                    return MoveS6PierPart(ref e, f);
                default:
                    return false;
            }
        }

        private void SpawnS6Batch10InitialChildren(ref EffectState e)
        {
            // Wait for the owner model to be usable when the first particle
            // must follow it; Wave joints do not depend on a model owner.
            if (e.Type == ClassicFxEffectType.PierPart &&
                e.SubType == 1 &&
                (e.ModelView == null ||
                 e.ModelView.Status != GameControlStatus.Ready))
                return;

            e.FirstMove = false;
            if (e.Type == ClassicFxEffectType.Waves && e.SubType == 0)
            {
                // Original CreateEffect(MODEL_WAVES,0): sixty LIGHT joints.
                for (int j = 0; j < 60; j++)
                    CreateJoint(ClassicTextureIds.BitmapLight,
                        e.Position, e.Position, e.Angle,
                        subType: 0, scale: 70f + Random.Modulo(40));
            }
            else if (e.Type == ClassicFxEffectType.Waves && e.SubType == 1)
            {
                for (int j = 0; j < 2; j++)
                    CreateJoint(ClassicTextureIds.BitmapPiercing,
                        e.Position, e.Position, e.Angle,
                        subType: 0, scale: 70f + Random.Modulo(40));
            }
            else if (e.Type == ClassicFxEffectType.PierPart && e.SubType == 1)
            {
                CreateParticle(ClassicTextureIds.BitmapFire + 1,
                    e.Position, e.Angle, e.Light, subType: 0, scale: 1f,
                    target: ClassicFxOwner.FromWorldObject(e.ModelView));
            }
        }

        private bool MoveS6Waves(ref EffectState e, float f)
        {
            if (e.SubType == 0)
            {
                if (e.LifeTime > 4f)
                {
                    e.Scale += e.Gravity * f;
                    e.Gravity += 0.1f * f;
                }
                e.BlendMeshLight *= MathF.Pow(1f / 1.4f, f);
                return true;
            }

            e.Scale += e.Gravity * f;
            float acceleration, cap, fade;
            switch (e.SubType)
            {
                case 1:
                    acceleration = 0.07f; cap = 2f; fade = 1.5f;
                    break;
                case 2:
                    acceleration = 0.01f; cap = 1.5f; fade = 1.5f;
                    break;
                case 3:
                    acceleration = 0.005f; cap = 1.5f; fade = 1.3f;
                    break;
                case 4:
                    acceleration = 0.002f; cap = 2.5f; fade = 1.2f;
                    break;
                case 5:
                case 6:
                    acceleration = 0.015f; cap = 2.5f; fade = 1.3f;
                    break;
                default:
                    return false;
            }

            e.Gravity += acceleration * f;
            if (e.Scale > cap) e.Scale = cap;
            e.BlendMeshLight *= MathF.Pow(1f / fade, f);
            if (e.SubType >= 3)
                e.Angle.Y += MathHelper.ToRadians(45f * f);
            return true;
        }

        private bool MoveS6Piercing2(ref EffectState e, float f)
        {
            // Native rotates Direction but leaves the actual Position add
            // commented out: no invented translational movement here.
            e.Direction.Y = MathF.Min(0f, e.Direction.Y + 12f * f);
            int tick = (int)e.LifeTime;
            int threshold = e.SubType == 0 ? 5 : 1;
            if (Clock.AdvancedReferenceFrame && tick > threshold &&
                tick != e.LastChildNativeTick)
            {
                e.LastChildNativeTick = tick;
                e.BlendMeshLight *= MathF.Pow(1f / 1.6f, f);
                CreateEffect(ClassicFxEffectType.Waves,
                    e.Position, e.Angle, e.Light,
                    ClassicFxOwner.None, subType: 2,
                    boneIndex: tick);
            }
            return true;
        }

        // MODEL_PIER_PART(1) uses its Effect owner remaining lifetime, not
        // an arbitrarily fixed duration; resolve it from the same pool.
        private float GetS6ParentEffectLifetime(ClassicFxOwner parent)
        {
            if (parent.WorldObject == null) return 20f;
            for (int i = 0; i < _effects.Length; ++i)
                if (Pools.Effects.IsActive(i) &&
                    ReferenceEquals(_effects[i].ModelView, parent.WorldObject))
                    return _effects[i].LifeTime;
            return 20f;
        }

        private bool MoveS6PierPart(ref EffectState e, float f)
        {
            if (e.SubType == 1)
            {
                if (e.LifeTime < 5f)
                    e.Alpha *= MathF.Pow(1f / 1.3f, f);
                return true;
            }
            if (!TryGetOwnerSnapshot(e.Owner,
                    out ClassicFxOwnerSnapshot owner) ||
                owner.WorldObject == null ||
                !ReferenceEquals(owner.WorldObject.World, World))
                return true;

            Vector3 target = owner.Position;
            if (e.SubType == 0)
            {
                target += e.StartPosition; // native target-relative Light
                for (int i = 1; i < e.Gravity; ++i)
                {
                    if (Clock.AdvancedReferenceFrame &&
                        Random.FpsCheck(2, Clock))
                    {
                        if (e.Angle.X < MathHelper.ToRadians(-90f))
                            e.Angle.X += MathHelper.ToRadians(20f * f);
                        else
                            e.Angle.X -= MathHelper.ToRadians(20f * f);
                    }
                    TurnS6JavelinHeading(e.Position, target,
                        ref e.Angle, e.Velocity, f);
                    e.Velocity += 0.4f * f;
                    if (e.LifeTime < 10f)
                        e.Velocity += 0.1f * f;
                    e.Position += Vector3.TransformNormal(
                        e.Direction, Matrix.CreateFromYawPitchRoll(
                            e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;

                    // Native child Owner is the parent Effect, not caster.
                    if (Clock.AdvancedReferenceFrame &&
                        e.ModelView != null &&
                        e.ModelView.Status == GameControlStatus.Ready)
                        CreateEffect(ClassicFxEffectType.PierPart,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.FromWorldObject(e.ModelView),
                            subType: 1);
                }
                e.Gravity += 0.1f * f;
            }
            else if (e.SubType == 2)
            {
                TurnS6JavelinHeading(e.Position, target,
                    ref e.Angle, e.Velocity, f);
                e.Velocity += 2.4f * f;
                e.Position += Vector3.TransformNormal(
                    e.Direction, Matrix.CreateFromYawPitchRoll(
                        e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;

                int tick = (int)e.LifeTime;
                if (Clock.AdvancedReferenceFrame &&
                    tick % 3 == 0 &&
                    tick != e.LastChildNativeTick)
                {
                    e.LastChildNativeTick = tick;
                    CreateJoint(ClassicTextureIds.BitmapJointForce,
                        e.Position, e.Position,
                        new Vector3(MathHelper.ToRadians(-90f), 0f,
                            e.Angle.Z),
                        subType: 2, scale: 150f);
                }
            }
            return true;
        }
    }
}
