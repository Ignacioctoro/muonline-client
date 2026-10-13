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
                    Vector3 incomingAngle = angle;
                    angle = Vector3.Zero;
                    position.Z += 50f * Clock.FrameFactor;
                    // Native restores the incoming yaw for subtypes 1..6.
                    if (subType != 0)
                        angle.Z = incomingAngle.Z;
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
                // ZzzEffect.cpp leaves BlendMesh at the CreateEffect
                // default -1. ClassicFxEffectModelObject defaults to 0,
                // which wrongly sends mesh 0 to the blend-only pass: at
                // Medium quality the visible root mesh can disappear.
                // Keep the native mesh visibility and normal BMD pass.
                view.BlendMesh = -1;
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
            // Native MODEL_PIER_PART(1) is created when its mesh is
            // already loaded. The NeffisDev model loads asynchronously:
            // spawning Fire02 while it is not Ready produces the free
            // floating fireballs seen in the capture, with no visible
            // chain segment. The effect reuses its parent's loaded BMD
            // in CreateEffect, so this waits only for the child view to
            // enter the world, not for another disk load.
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
                // MODEL_PIER_PART retains its orientation in MonoGame radians.
                // Native CreateParticle copies angles in DEGREES and later
                // MovePosition calls the degree-based AngleMatrix. Passing
                // radians here makes every Fire02 projectile drift toward
                // world -Y (seen as bottom-left in the reported camera),
                // regardless of the monster followed by the parent.
                // Only this particle needs the boundary conversion; the BMD
                // continues to use radians.
                Vector3 nativeParticleAngle =
                    e.Angle * ClassicMath.RadiansToAngle;
                CreateParticle(ClassicTextureIds.BitmapFire + 1,
                    e.Position, nativeParticleAngle, e.Light,
                    subType: 0, scale: 1f,
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

        // MuMain ZzzAI.cpp MoveHumming + Engine/AI/AngleMath.cpp TurnAngle2.
        // Work in radians in ClassicFX but normalize after each turn, just as
        // the native degree-based implementation does. This is intentionally
        // independent of the Javelin helper: Fire Burst must retain the
        // original PierPart movement even when other skills are adjusted.
        private static float NormalizeS6PierPartAngle(float radians)
        {
            radians %= MathHelper.TwoPi;
            return radians < 0f ? radians + MathHelper.TwoPi : radians;
        }

        private static float TurnS6PierPartAngle(
            float current, float target, float turn)
        {
            current = NormalizeS6PierPartAngle(current);
            target = NormalizeS6PierPartAngle(target);
            if (turn <= 0f)
                return current;

            float difference = target - current;
            if (difference > MathHelper.Pi)
                difference -= MathHelper.TwoPi;
            else if (difference < -MathHelper.Pi)
                difference += MathHelper.TwoPi;

            return NormalizeS6PierPartAngle(
                current + Math.Clamp(difference, -turn, turn));
        }

        private static void MoveS6PierPartHumming(
            Vector3 from, Vector3 target, ref Vector3 angle,
            float velocity, float referenceFrameFactor)
        {
            float x = target.X - from.X;
            float y = target.Y - from.Y;
            // CreateAngle2D(from, target) = atan2(dx, -dy).
            float targetYaw = MathF.Abs(x) < float.Epsilon &&
                              MathF.Abs(y) < float.Epsilon
                ? 0f : MathF.Atan2(x, -y);
            float turn = MathHelper.ToRadians(
                velocity * referenceFrameFactor);
            angle.Z = TurnS6PierPartAngle(angle.Z, targetYaw, turn);

            // Native pitch: 360 - CreateAngle(
            //     Position[2], horizontalDistance, Target[2], 0).
            float horizontalDistance = MathF.Sqrt(x * x + y * y);
            float targetPitch = -MathF.Atan2(
                target.Z - from.Z, horizontalDistance);
            angle.X = TurnS6PierPartAngle(angle.X, targetPitch, turn);
        }

        // The original AngleMatrix multiplies Z * Y * X and VectorRotate
        // uses that exact 3x4 matrix. Matrix.CreateFromYawPitchRoll is not
        // interchangeable for this movement. It was making the three
        // Fire Burst branches drift away from the monster.
        private static Vector3 RotateS6PierPartNative(
            Vector3 direction, Vector3 anglesRadians)
        {
            const float radiansToDegrees = 180f / MathF.PI;
            Vector3 anglesDegrees = anglesRadians * radiansToDegrees;
            return ClassicMath.VectorRotate(
                direction, ClassicMath.AngleMatrix(anglesDegrees));
        }

        private bool MoveS6PierPart(ref EffectState e, float f)
        {
            if (e.SubType == 1)
            {
                // SubType 1 is a stationary child left by its moving parent;
                // the parent remaining lifetime was copied at creation.
                if (e.LifeTime < 5f)
                    e.Alpha *= MathF.Pow(1f / 1.3f, f);
                return true;
            }

            // MuMain keeps the target OBJECT* for the full effect life,
            // including its death animation. NeffisDev may detach the walker
            // during scope removal; TryGetOwnerSnapshot then fails and the
            // 20-tick PierPart paths freeze. The object reference still owns
            // its last real WorldPosition, even after detachment. Use that
            // position instead of inventing an unrelated fallback target.
            var targetObject = e.Owner.WorldObject;
            if (targetObject == null ||
                (targetObject.World != null &&
                 !ReferenceEquals(targetObject.World, World)))
                return true;

            Vector3 target = targetObject.WorldPosition.Translation;
            if (e.SubType == 0)
            {
                // ZzzEffect.cpp: Light is the target-relative height offset
                // copied into StartPosition; never treat it as a RGB color.
                target += e.StartPosition;
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

                    MoveS6PierPartHumming(
                        e.Position, target, ref e.Angle, e.Velocity, f);
                    e.Velocity += 0.4f * f;
                    if (e.LifeTime < 10f)
                        e.Velocity += 0.1f * f;

                    // Original: AngleMatrix(o->Angle); VectorRotate(
                    // Direction); VectorAddScaled(Position, direction).
                    e.Position += RotateS6PierPartNative(
                        e.Direction, e.Angle) * f;

                    // Keep the native MODEL_PIER_PART(1) trail: a 0.5-scale
                    // PierPart BMD and its BITMAP_FIRE+1 particle, attached
                    // to that BMD. It does not chase a random monster.
                    if (Clock.AdvancedReferenceFrame &&
                        e.ModelView != null &&
                        e.ModelView.Status == GameControlStatus.Ready &&
                        Random.FpsCheck(1, Clock))
                    {
                        CreateEffect(ClassicFxEffectType.PierPart,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.FromWorldObject(e.ModelView),
                            subType: 1);
                    }
                }
                e.Gravity += 0.1f * f;
            }
            else if (e.SubType == 2)
            {
                // This is also used by Space Split, not the Fire Burst cast.
                // Preserve its own native target (without StartPosition).
                MoveS6PierPartHumming(
                    e.Position, target, ref e.Angle, e.Velocity, f);
                e.Velocity += 2.4f * f;
                e.Position += RotateS6PierPartNative(
                    e.Direction, e.Angle) * f;

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
