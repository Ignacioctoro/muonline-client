using System;
using System.Collections.Generic;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    // The existing MoveJoint A/B/C files are consolidated here.
    // This reduces the movement source-file count and does not change their
    // family-specific math, behaviour or public API.
    public sealed partial class ClassicFxRuntime
    {
        private readonly HashSet<int> _unhandledJointMoveTypes = new();

        // MuMain: ZzzEffectJoint.cpp MoveJoint(), lines 2980-3040,
        // 3708-3745, 4202-4247 and 6863-6868, plus shared tail/lifetime
        // rules at lines 6939-6979. This is the FIRST movement family,
        // not a replacement for the native branches still being ported.
        //
        // Joint movement is NOT called automatically by Update() until
        // the full native type switch has been covered. Invoking the partial
        // dispatcher would otherwise leave other pooled joints frozen.
        public void MoveJoints()
        {
            // Native MoveJoints() iterates slots in increasing index order.
            // This is important for deterministic secondary spawns.
            for (int i = 0; i < _joints.Length; i++)
            {
                if (!Pools.Joints.IsActive(i))
                    continue;

                // Native MoveJoints() also skips joints created in frames
                // after their index was visited; the forward scan preserves it.
                ClassicFxHandle handle = Pools.Joints.GetHandle(i);
                ref ClassicJoint j = ref _joints[i];

                bool live;
                if (IsJointMoveFamilyA(j.Type))
                    live = MoveJointFamilyA(ref j);
                else if (j.Type == ClassicTextureIds.BitmapJointEnergy)
                    live = MoveJointFamilyB(ref j);
                else if (j.Type == ClassicTextureIds.BitmapJointHealing ||
                         j.Type == ClassicTextureIds.BitmapJointSpirit ||
                         j.Type == ClassicTextureIds.BitmapJointSpirit2)
                    live = MoveJointFamilyC(ref j, i, handle);
                else if (IsJointMoveFamilyD(j.Type))
                    live = MoveJointFamilyD(ref j);
                else if (IsJointMoveComplexType(j.Type))
                    live = MoveJointComplex(ref j, i, handle);
                else if (j.Type == ClassicTextureIds.BitmapFlare ||
                         j.Type == ClassicTextureIds.BitmapFlareBlue ||
                         j.Type == ClassicTextureIds.BitmapFlare + 1)
                    live = MoveFlareJointFull(ref j, i, handle);
                else
                {
                    // Never leave unsupported joints active forever: they
                    // exhaust the fixed pool and would be invisible/frozen.
                    if (_unhandledJointMoveTypes.Add(j.Type))
                        Console.WriteLine($"[ClassicFX][Joint] Unsupported MoveJoint type={j.Type}, subtype={j.SubType}; releasing the joint.");
                    ReleaseJoint(handle);
                    continue;
                }

                if (!live || j.LifeTime < 0f)
                    ReleaseJoint(handle);
            }
        }

        private static bool IsJointMoveFamilyA(int type) =>
            type == ClassicTextureIds.BitmapScolpionTail ||
            type == ClassicTextureIds.BitmapJointLaser ||
            type == ClassicTextureIds.BitmapJointSpark ||
            type == ClassicTextureIds.BitmapJointFire ||
            type == ClassicTextureIds.Bitmap2LineGhost ||
            type == ClassicTextureIds.BitmapPinLight;

        private bool MoveJointFamilyA(ref ClassicJoint j)
        {
            float factor = Clock.FrameFactor;
            if (factor <= 0f)
                return true;

            // Native common pre-switch movement (none of this block's
            // types are in the exclusion list at MoveJoint():2993-2999).
            // Save the matrix: native CreateTail() reuses the earlier Matrix
            // unless a type explicitly recalculates it during movement.
            Vector3 entryPosition = j.Position;
            Vector3 entryTargetPosition = j.TargetPosition;
            bool dieOnCollision = false;
            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            if (j.Velocity != 0f)
            {
                Vector3 delta = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), matrix);
                j.Position += delta * factor;
            }

            switch (j.Type)
            {
                case ClassicTextureIds.BitmapScolpionTail:
                    // Native copies Target->EyeLeft if nonzero, then rejects
                    // a dead Target. Never guess the eye/bone transform.
                    if (!TryGetOwnerSnapshot(j.Target, out _))
                        return false;
                    if (TryJointEye(j.Target, ClassicJointEye.Left,
                        out Vector3 leftEye))
                        j.Position = leftEye;
                    break;

                case ClassicTextureIds.BitmapJointLaser:
                {
                    // Target->Position updated every native frame.
                    if (!TryGetOwnerPosition(j.Target, out Vector3 ownerPos))
                        return false;
                    j.TargetPosition = ownerPos;
                    j.TargetPosition.Z += 130f;

                    // dx/dy are evaluated BEFORE common movement AND before
                    // replacing TargetPosition with the target's live point.
                    float dx = entryPosition.X - entryTargetPosition.X;
                    float dy = entryPosition.Y - entryTargetPosition.Y;
                    float planarDistance = MathF.Sqrt(dx * dx + dy * dy);
                    float turn = planarDistance > 0f
                        ? 3000f / planarDistance
                        : float.PositiveInfinity;
                    MoveJointHummingA(ref j.Angle, j.Position,
                        j.TargetPosition, turn, factor);
                    if (!j.Collision &&
                        planarDistance <= j.Velocity * 2f * factor)
                    {
                        j.Collision = true;
                        j.LifeTime = 5f;
                    }
                    float lum = j.LifeTime * 0.1f;
                    j.Light = new Vector3(lum);
                    float terrainLum = -(R(4) + 4f) * 0.01f;
                    AddClassicTerrainLight(j.Position.X, j.Position.Y,
                        new Vector3(terrainLum), 4f);
                    break;
                }

                case ClassicTextureIds.BitmapJointSpark:
                {
                    // Native subtypes 1, 3, 4 only; all others preserve
                    // their CreateJoint velocity/color during movement.
                    float acceleration;
                    float attenuation;
                    switch (j.SubType)
                    {
                        case 1:
                            acceleration = 0.3f;
                            attenuation = 1f / 1.4f;
                            break;
                        case 3:
                        case 4:
                            acceleration = 0.1f;
                            attenuation = 1f / 1.1f;
                            break;
                        default:
                            acceleration = 0f;
                            attenuation = 1f;
                            break;
                    }
                    if (acceleration > 0f)
                    {
                        j.Velocity += factor * acceleration;
                        float light = j.Light.X * MathF.Pow(attenuation, factor);
                        j.Light = new Vector3(light);
                    }
                    break;
                }

                case ClassicTextureIds.BitmapJointFire:
                {
                    // MuMain: MoveHumming(...,0), then CreateTail();
                    // collision/explosion or another separate move.
                    float distance = MoveJointHummingA(
                        ref j.Angle, j.Position, j.TargetPosition, 0f, factor);
                    matrix = ClassicMath.AngleMatrix(j.Angle);
                    AppendJointTailMoveA(ref j, matrix);
                    if (distance <= j.Velocity)
                    {
                        CreateParticle(ClassicTextureIds.BitmapExplotion,
                            j.Position, j.Angle, j.Light);
                        dieOnCollision = true;
                        break; // Native still reaches post-switch CreateTail.

                    }
                    float lum = (R(4) + 4f) * 0.1f;
                    AddClassicTerrainLight(j.Position.X, j.Position.Y,
                        new Vector3(lum, lum * 0.6f, lum * 0.2f), 4f);
                    // The Main intentionally multiplies by FPS factor
                    // twice in this branch. Do not simplify this formula.
                    Vector3 extra = ClassicMath.VectorRotate(
                        new Vector3(0f, -j.Velocity * factor, 0f), matrix);
                    j.Position += extra * factor;
                    break;
                }

                case ClassicTextureIds.Bitmap2LineGhost:
                    if (j.SubType == 0)
                    {
                        if (((int)j.LifeTime % 16) <= 7)
                            j.TargetIndexRef = 10;
                        else
                            j.TargetIndexRef = -10;
                        j.Angle.Z += j.TargetIndexRef * factor;
                        CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke,
                            j.Position, j.Angle, j.Light, 59, 1f);
                    }
                    else if (j.SubType == 1)
                    {
                        float horizontal = CreateJointAngleA(j.Position.X,
                            j.Position.Y, j.TargetPosition.X, j.TargetPosition.Y);
                        j.Angle.Z = horizontal - 65f;
                        Vector3 range = j.Position - j.TargetPosition;
                        float distance = MathF.Sqrt(range.X * range.X +
                            range.Y * range.Y);
                        if (distance > 60f)
                        {
                            float vertical = 360f - CreateJointAngleA(
                                j.Position.Z, distance, j.TargetPosition.Z, 0f);
                            j.Angle.X = TurnJointAngleA(j.Angle.X, vertical, 3f);
                        }
                        else
                        {
                            j.Angle.Z = horizontal - 90f;
                        }
                    }
                    break;

                case ClassicTextureIds.BitmapPinLight:
                    j.Velocity += factor * 0.1f;
                    j.Light *= MathF.Pow(0.9f, factor);
                    break;
            }

            // Native unconditional post-switch CreateTail() + lifetime step.
            if (j.CreateTails)
                AppendJointTailMoveA(ref j, matrix);
            j.LifeTime -= factor;
            return !dieOnCollision && j.LifeTime >= 0f;
        }

        // MuMain: CreateTail() with Blur=false. Append a quad in the fixed
        // 200x4 array; no heap allocations or double-buffer copies.
        private static void AppendJointTailMoveA(
            ref ClassicJoint j, in ClassicMatrix3x4 matrix)
        {
            if (j.Tails == null || j.MaxTails <= 0)
                return;
            int max = Math.Min(j.MaxTails, ClassicJoint.MaxTailSegments);
            j.NumTails = Math.Min(j.NumTails + 1, max - 1);
            for (int i = j.NumTails - 1; i >= 0; i--)
            {
                int source = i * ClassicJoint.VerticesPerTail;
                int target = source + ClassicJoint.VerticesPerTail;
                for (int k = 0; k < ClassicJoint.VerticesPerTail; k++)
                    j.Tails[target + k] = j.Tails[source + k];
            }
            float half = j.Scale * 0.5f;
            j.Tails[0] = j.Position + ClassicMath.VectorRotate(
                new Vector3(-half, 0f, 0f), matrix);
            j.Tails[1] = j.Position + ClassicMath.VectorRotate(
                new Vector3(half, 0f, 0f), matrix);
            j.Tails[2] = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, -half), matrix);
            j.Tails[3] = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, half), matrix);
        }

        // MuMain: AngleMath.cpp CreateAngle / TurnAngle2, and
        // ZzzAI.cpp MoveHumming. Shared by subsequent movement families.
        private static float NormalizeJointAngleA(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        private static float CreateJointAngleA(
            float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            if (MathF.Abs(dx) < float.Epsilon &&
                MathF.Abs(dy) < float.Epsilon)
                return 0f;
            return NormalizeJointAngleA(
                MathF.Atan2(dx, -dy) * 180f / MathF.PI);
        }

        private static float TurnJointAngleA(
            float current, float target, float step)
        {
            if (step <= 0f)
                return NormalizeJointAngleA(current);
            float delta = NormalizeJointAngleA(target) -
                NormalizeJointAngleA(current);
            if (delta > 180f) delta -= 360f;
            else if (delta < -180f) delta += 360f;
            delta = Math.Clamp(delta, -step, step);
            return NormalizeJointAngleA(current + delta);
        }

        private static float MoveJointHummingA(
            ref Vector3 angles, in Vector3 position,
            in Vector3 targetPosition, float turn, float factor)
        {
            float scaledTurn = turn * factor;
            float horizontal = CreateJointAngleA(position.X,
                position.Y, targetPosition.X, targetPosition.Y);
            angles.Z = TurnJointAngleA(angles.Z, horizontal, scaledTurn);
            Vector3 range = position - targetPosition;
            float distance = MathF.Sqrt(range.X * range.X + range.Y * range.Y);
            float vertical = 360f - CreateJointAngleA(
                position.Z, distance, targetPosition.Z, 0f);
            angles.X = TurnJointAngleA(angles.X, vertical, scaledTurn);
            return range.Length();
        }
    

        // ===== BITMAP_JOINT_ENERGY (formerly movement B) =====

        // MuMain ZzzEffectJoint.cpp: MoveJoint(), BITMAP_JOINT_ENERGY,
        // native lines 3041-3508 (including CreateTail(...,true)).
        // It is deliberately NOT scheduled in Update() until all Joint
        // movement families have been ported.
        // Attach to the existing SOUND_GET_ENERGY playback system. Never
        // synthesize another sound when this callback has no subscriber.
        public event Action JointEnergyGetSoundRequested;

        private bool MoveJointFamilyB(ref ClassicJoint j)
        {
            float factor = Clock.FrameFactor;
            if (factor <= 0f)
                return true;

            // Native pre-switch: ENERGY is NOT on the movement-exclusion list.
            // Keep the original matrix for the common post-switch CreateTail().
            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            if (j.Velocity != 0f)
            {
                Vector3 offset = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), matrix);
                j.Position += offset * factor;
            }

            bool alive = true;
            switch (j.SubType)
            {
                // Native target-attached energy joint group.
                case 2: case 3: case 4: case 5: case 8:
                case 10: case 11: case 14: case 15: case 17:
                case 18: case 19: case 20: case 21: case 26:
                case 27: case 28: case 29: case 30: case 31:
                case 32: case 33: case 22: case 23: case 24:
                case 25: case 47: case 48: case 49: case 50:
                case 51: case 52: case 53: case 54: case 55:
                case 56: case 57:
                    if (!MoveJointEnergyAttachedB(ref j, factor))
                        return false;
                    break;

                // Native travelling / homing energy joint group.
                case 0: case 1: case 6: case 9:
                case 12: case 13: case 16:
                case 44: case 45: case 46:
                    alive = MoveJointEnergyTravellingB(ref j, factor);
                    break;

                case 42:
                    alive = MoveJointEnergy42B(ref j, factor);
                    break;

                case 43:
                    alive = MoveJointEnergy43B(ref j, factor);
                    break;

                // Native 40/41 have no subtype-specific MoveJoint case.
                // They still receive the shared pre/post-switch updates.
            }

            // Native post-switch: subtype 54 uses the Blur=true variant,
            // exactly two shifted/updated tail quads per movement tick.
            if (j.CreateTails)
            {
                if (j.SubType == 54)
                    AppendJointEnergyBlurB(ref j, matrix);
                else
                    AppendJointTailMoveA(ref j, matrix);
            }

            j.LifeTime -= factor;
            return alive && j.LifeTime >= 0f;
        }

        private bool MoveJointEnergyAttachedB(ref ClassicJoint j, float factor)
        {
            // Native checks !Target->Live before following any attachment.
            // Snapshot resolution is the current safe OBJECT bridge check.
            if (!TryGetOwnerSnapshot(j.Target, out _))
                return false;

            switch (j.SubType)
            {
                case 2: case 4: case 5: case 8:
                case 10: case 14: case 17: case 18:
                case 28: case 22: case 24:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Left);
                    if (j.SubType == 8)
                        j.Scale += 10.1f * factor;
                    break;
                case 20: case 30:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Left2);
                    break;
                case 26: case 32:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Left3);
                    break;
                case 3: case 11: case 15: case 19:
                case 29: case 23: case 25: case 47:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Right);
                    break;
                case 21: case 31:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Right2);
                    break;
                case 54:
                    switch (j.PKKey)
                    {
                        case 0:
                            UpdateJointTargetPositionP(ref j, ClassicJointEye.Right2);
                            break;
                        case 1:
                            UpdateJointTargetPositionP(ref j, ClassicJointEye.Left2);
                            break;
                        case 2:
                            UpdateJointTargetPositionP(ref j, ClassicJointEye.Right3);
                            break;
                        case 3:
                            UpdateJointTargetPositionP(ref j, ClassicJointEye.Left3);
                            break;
                    }
                    break;
                case 27: case 33:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Right3);
                    break;
                case 55:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Left);
                    break;
                case 56:
                    UpdateJointTargetPositionP(ref j, ClassicJointEye.Right);
                    break;
                case 57:
                    // The native model animates before TransformByObjectBone.
                    // WorldBridge reads the current animated MonoGame pose;
                    // when unavailable it refuses the joint, not a fake bone.
                    if (!TryGetOwnerBonePosition(j.Target, j.PKKey,
                        out j.Position))
                        return false;
                    break;
                case 48: case 49: case 50:
                case 51: case 52: case 53:
                    int bone = j.SubType switch
                    {
                        48 => 24, 49 => 28, 50 => 32,
                        51 => 44, 52 => 48, _ => 52
                    };
                    if (!TryGetOwnerBonePosition(j.Target, bone,
                        out j.Position))
                        return false;
                    break;
            }

            float pulse = MathF.Sin((float)(Clock.WorldTimeMilliseconds *
                0.002)) * 0.3f + 0.8f;
            switch (j.SubType)
            {
                case 2: case 3:
                {
                    Vector3 light = new Vector3(pulse * 0.5f,
                        pulse * 0.1f, pulse) * j.Light;
                    CreateParticle(ClassicTextureIds.BitmapLightning + 1,
                        j.Position, j.Angle, light);
                    break;
                }
                case 14: case 15:
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                        j.Position, j.Angle,
                        new Vector3(pulse, pulse * 0.1f, pulse * 0.1f), 4);
                    break;
                case 22: case 23:
                {
                    float lum = MathF.Sin((float)(Clock.WorldTimeMilliseconds *
                        0.002)) * 0.2f + 0.8f;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                        j.Position, j.Angle, new Vector3(lum) * j.Light, 4);
                    break;
                }
                case 24: case 25:
                {
                    float lum = MathF.Sin((float)(Clock.WorldTimeMilliseconds *
                        0.002)) * 0.1f + 0.2f;
                    CreateSprite(ClassicTextureIds.BitmapFlare + 1,
                        j.Position, lum, new Vector3(lum) * j.Light, j.Target);
                    break;
                }
                case 54:
                {
                    float lum = MathF.Sin((float)(Clock.WorldTimeMilliseconds *
                        0.002)) * 0.1f;
                    CreateSprite(ClassicTextureIds.BitmapBlur,
                        j.Position, lum, new Vector3(lum) * j.Light, j.Target);
                    break;
                }
            }
            return true;
        }

        private bool MoveJointEnergyTravellingB(ref ClassicJoint j, float factor)
        {
            bool alive = true;
            float distance;

            if (j.LifeTime >= 100f && j.SubType != 44)
            {
                if (j.SubType == 0 || j.SubType == 45)
                {
                    j.Angle.Z += 10f * factor;
                    j.Position.Z += 6f * factor;
                }
                else if (j.SubType == 12)
                {
                    j.Angle.Z += 15f * factor;
                    j.Position.X += j.MultiUse * R(5) * factor;
                    j.Position.Y += j.MultiUse * R(5) * factor;
                    j.Position.Z += 6f * factor;
                }
                else if (j.SubType == 16 || j.SubType == 46)
                {
                    j.Angle.Z -= j.MultiUse * 20f * factor;
                    j.Position.X += j.MultiUse * (R(5) + 3f) * factor;
                    j.Position.Y += j.MultiUse * (R(5) + 3f) * factor;
                    j.Position.Z += 8f * factor;
                }
                else if (j.SubType == 13)
                {
                    j.Angle.Z -= 20f * factor;
                    j.Position.X += j.MultiUse * R(5) * factor;
                    j.Position.Y += j.MultiUse * R(5) * factor;
                    j.Position.Z += 6f * factor;
                }
                else
                {
                    j.Angle.Z -= 10f * factor;
                    j.Position.Z += 5f * factor;
                }
            }
            else if (j.SubType == 44 && j.LifeTime <= 120f &&
                     j.LifeTime >= 10f)
            {
                // Native 44 never enters the initial >=100 path. It homes
                // during >=20 and adds separate trig jitter below 20.
                if (j.LifeTime >= 20f)
                {
                    j.Velocity = MathF.Min(20f, j.Velocity + 10f * factor);
                    if (!TryGetOwnerPosition(j.Target, out Vector3 pos))
                        return false;
                    j.TargetPosition = pos;
                    j.TargetPosition.Z += 120f;
                }
                else
                {
                    j.Velocity = MathF.Min(20f, j.Velocity + 12f * factor);
                    j.Position.X += MathF.Cos(MathF.PI /
                        (R(180) + 150f)) * factor;
                    j.Position.Y += MathF.Sin(MathF.PI /
                        (R(180) + 150f)) * factor;
                }
                MoveJointHummingA(ref j.Angle, j.Position,
                    j.TargetPosition, j.Velocity, factor);
            }
            else
            {
                if (j.SubType == 12 || j.SubType == 13 ||
                    j.SubType == 16 || j.SubType == 46)
                    j.Velocity = MathF.Min(40f, j.Velocity + 7f * factor);
                else
                    j.Velocity = MathF.Min(30f, j.Velocity + 5f * factor);

                float oldAngle = j.Angle.Z;
                if (j.SubType != 6 && j.SubType != 9)
                {
                    if (!TryGetOwnerPosition(j.Target, out Vector3 pos))
                        return false;
                    j.TargetPosition = pos;
                    j.TargetPosition.Z += 120f;
                }
                distance = MoveJointHummingA(ref j.Angle, j.Position,
                    j.TargetPosition, j.Velocity, factor);
                if (distance <= 35f)
                {
                    alive = false;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                        j.Position, j.Angle, j.Light, 1);
                    JointEnergyGetSoundRequested?.Invoke();
                }
                else if (distance <= 70f &&
                    MathF.Abs(oldAngle - j.Angle.Z) > 20f &&
                    j.Velocity >= 20f)
                    j.Velocity -= 10f * factor;
            }

            float lumTerrain = (R(4) + 8f) * 0.03f;
            AddClassicTerrainLight(j.Position.X, j.Position.Y,
                new Vector3(lumTerrain * 0.4f,
                    lumTerrain, lumTerrain * 0.8f), 2f);

            if (j.SubType == 6 || j.SubType == 9)
                CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                    j.Position, j.Angle, j.Light, 3, 0.05f);
            else if (j.SubType == 12 || j.SubType == 13)
            {
                CreateSprite(ClassicTextureIds.BitmapLight,
                    j.Position, 1f, j.Light);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    j.Position, 1f, j.Light, R(360));
            }
            else if (j.SubType == 44)
            {
                CreateSprite(ClassicTextureIds.BitmapLight,
                    j.Position, 0.2f, j.Light);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    j.Position, 0.1f, j.Light, R(360));
            }
            else if (j.SubType == 16 || j.SubType == 46)
            {
                Vector3 light = j.SubType == 16
                    ? new Vector3(0.4f, 0.8f, 1f)
                    : new Vector3(0.4f, 1f, 0.4f);
                CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke,
                    j.Position, j.Angle, light, 31, 0.8f);
            }
            else if (j.SubType == 45)
                CreateSprite(ClassicTextureIds.BitmapFlareRed,
                    j.Position, 0.3f, Vector3.One, j.Target);
            else
                CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                    j.Position, j.Angle, j.Light);

            return alive;
        }

        private bool MoveJointEnergy42B(ref ClassicJoint j, float factor)
        {
            if (j.LifeTime >= 60f)
            {
                j.Position.Z += factor;
                j.Light += new Vector3(0.002f, 0.0015f, 0.011f) * factor;
            }
            else
            {
                j.Light = new Vector3(0.4f, 0.3f, 2.2f);
                j.Velocity = MathF.Min(30f, j.Velocity + 5f * factor);
            }

            if (!TryGetOwnerPosition(j.Target, out Vector3 pos))
                return false;
            float oldAngle = j.Angle.Z;
            j.TargetPosition = pos;
            j.TargetPosition.Z += 260f;
            float distance = MoveJointHummingA(ref j.Angle,
                j.Position, j.TargetPosition, j.Velocity, factor);
            bool alive = true;
            if (distance <= 35f)
            {
                alive = false;
                JointEnergyGetSoundRequested?.Invoke();
            }
            else if (distance <= 70f && MathF.Abs(oldAngle - j.Angle.Z) > 20f &&
                     j.Velocity >= 20f)
                j.Velocity -= 10f * factor;

            float lum = (R(4) + 8f) * 0.03f;
            AddClassicTerrainLight(j.Position.X, j.Position.Y,
                new Vector3(lum * 0.4f, lum, lum * 0.8f), 2f);
            CreateParticleFpsChecked(ClassicTextureIds.BitmapLightning + 1,
                j.Position, j.Angle, j.Light, 5, 0.12f);
            return alive;
        }

        private bool MoveJointEnergy43B(ref ClassicJoint j, float factor)
        {
            j.Velocity = MathF.Min(40f, j.Velocity + 5f * factor);
            if (!TryGetOwnerPosition(j.Target, out Vector3 pos))
                return false;
            j.TargetPosition = pos;
            j.TargetPosition.Z += 100f;
            j.Angle.Z = CreateJointAngleA(j.Position.X,
                j.Position.Y, j.TargetPosition.X, j.TargetPosition.Y);
            float distance = MoveJointHummingA(ref j.Angle,
                j.Position, j.TargetPosition, j.Velocity, factor);
            bool alive = true;
            if (distance <= 60f)
            {
                alive = false;
                Vector3 bombPos = j.Position;
                bombPos.Z -= 60f * factor;
                RequestClassicBomb(bombPos, true, 4);
            }
            else if (distance >= 550f)
                alive = false;

            float lum = (R(4) + 8f) * 0.03f;
            AddClassicTerrainLight(j.Position.X, j.Position.Y,
                new Vector3(lum, lum * 0.4f, lum * 0.3f), 1f);
            float time = (float)Clock.WorldTimeMilliseconds;
            float glow = (MathF.Sin(time * 0.002f) + 1f) * 0.1f;
            Vector3 light = new Vector3(2f + glow, 1f + glow, 1f + glow);
            CreateSprite(ClassicTextureIds.BitmapPoundingBall,
                j.Position, 0.7f + glow, light, time / 10f);
            // Native computes one additional unused luminosity value here;
            // it has no effect on any subsequent draw or random state.
            light = new Vector3(2f + R(10) * 0.03f,
                0.4f + R(10) * 0.03f, 0.4f + R(10) * 0.03f);
            CreateSprite(ClassicTextureIds.BitmapLight,
                j.Position, 2f, light, -(time * 0.1f));
            CreateSprite(ClassicTextureIds.BitmapLight,
                j.Position, 2f, light, time * 0.12f);
            CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke,
                j.Position, j.Angle, new Vector3(1f, 0.6f, 0.4f), 31, 1f);
            return alive;
        }

        // Exact CreateTail(..., true) two-pass branch in native
        // ZzzEffectJoint.cpp:2854-2930. Unlike normal tail creation, the
        // first newly inserted tail is midpoint-blended when NumTails>1.
        private static void AppendJointEnergyBlurB(
            ref ClassicJoint j, in ClassicMatrix3x4 matrix)
        {
            if (j.Tails == null || j.MaxTails < 1)
                return;
            int max = Math.Min(j.MaxTails, ClassicJoint.MaxTailSegments);
            float half = j.Scale * 0.5f;
            Vector3 a = j.Position + ClassicMath.VectorRotate(
                new Vector3(-half, 0f, 0f), matrix);
            Vector3 b = j.Position + ClassicMath.VectorRotate(
                new Vector3(half, 0f, 0f), matrix);
            Vector3 c = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, -half), matrix);
            Vector3 d = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, half), matrix);
            for (int pass = 0; pass < 2; pass++)
            {
                j.NumTails = Math.Min(j.NumTails + 1, max - 1);
                for (int n = j.NumTails - 1; n >= 0; n--)
                {
                    int from = n * 4;
                    int to = from + 4;
                    j.Tails[to] = j.Tails[from];
                    j.Tails[to + 1] = j.Tails[from + 1];
                    j.Tails[to + 2] = j.Tails[from + 2];
                    j.Tails[to + 3] = j.Tails[from + 3];
                }
                if (pass == 0 && j.NumTails > 1)
                {
                    j.Tails[0] = (a + j.Tails[4]) * 0.5f;
                    j.Tails[1] = (b + j.Tails[5]) * 0.5f;
                    j.Tails[2] = (c + j.Tails[6]) * 0.5f;
                    j.Tails[3] = (d + j.Tails[7]) * 0.5f;
                }
                else
                {
                    j.Tails[0] = a;
                    j.Tails[1] = b;
                    j.Tails[2] = c;
                    j.Tails[3] = d;
                }
            }
        }
    

        // ===== BITMAP_JOINT_HEALING / SPIRIT / SPIRIT2 (formerly movement C) =====

        // MuMain ZzzEffectJoint.cpp MoveJoint(): 3509-3707 (HEALING),
        // 3746-4201 (SPIRIT/SPIRIT2), plus shared pre/post movement.
        // Effect and OBJECT mutators are integration hooks, NOT substitutes.
        // No automatic scheduling until the remaining MoveJoint switch is ported.

        /// <summary>True ONLY while Castle Siege is running and this
        /// world position is inside TW_NOATTACKZONE.</summary>
        public Func<Vector3, bool> JointSpiritSiegeNoAttackZoneResolver { get; set; }

        /// <summary>True when target is the local Hero OBJECT.</summary>
        public Func<ClassicFxOwner, bool> JointSpiritIsLocalHeroResolver { get; set; }

        /// <summary>Native AttackCharacterRange(Skill,Pos,150,Weapon,PKKey,Serial).
        /// The game/network layer is responsible for sending that request.</summary>
        public Action<ushort, Vector3, float, float, short, byte>
            JointSpiritAttackRangeRequested { get; set; }

        /// <summary>
        /// Original SPIRIT 15/18/24 writes to Target->Position/Angle and
        /// sometimes Target->Alpha, based on Target->LifeTime. This event
        /// requests that the game OBJECT owner integration perform the
        /// corresponding write. Argument 3 is original subtype.
        /// No guessed owner lifetime or write is performed here.
        /// </summary>
        public Action<ClassicFxOwner, Vector3, Vector3, int>
            JointSpiritOwnerPoseRequested { get; set; }

        private bool MoveJointFamilyC(
            ref ClassicJoint j, int slotIndex, ClassicFxHandle handle)
        {
            float factor = Clock.FrameFactor;
            if (factor <= 0f)
                return true;

            // Both families receive the original common pre-switch move.
            // Matrix is intentionally NOT refreshed after a homing turn;
            // native post-switch CreateTail uses the earlier Matrix here.
            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            Vector3 entryPosition = j.Position;
            Vector3 entryTargetPosition = j.TargetPosition;
            if (j.Velocity != 0f)
            {
                Vector3 offset = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), matrix);
                j.Position += offset * factor;
            }

            bool alive = j.Type == ClassicTextureIds.BitmapJointHealing
                ? MoveJointHealingC(ref j, factor)
                : MoveJointSpiritC(ref j, factor, slotIndex, handle,
                    entryPosition, entryTargetPosition);

            // Original post-switch: unchanged for HEALING/SPIRIT/SPIRIT2.
            if (j.CreateTails)
                AppendJointTailMoveA(ref j, matrix);
            j.LifeTime -= factor;
            return alive && j.LifeTime >= 0f;
        }

        // -------------------- HEALING (3509-3707) --------------------

        private bool MoveJointHealingC(ref ClassicJoint j, float factor)
        {
            float lum;
            switch (j.SubType)
            {
                case 4:
                    if (!TryGetOwnerPosition(j.Target, out Vector3 anchor4))
                        return false;
                    j.Position = anchor4 + j.TargetPosition;
                    j.Position.Z += j.Velocity * factor;
                    j.Velocity += 10f * factor;
                    lum = (12f - j.LifeTime) * 0.1f;
                    j.Light = new Vector3(lum * 0.4f, lum * 0.6f, lum);
                    return true;
                case 5:
                    lum = (12f - j.LifeTime) * 0.1f;
                    j.Light = new Vector3(lum * 0.4f, lum * 0.6f, lum);
                    return true;
                case 9:
                case 10:
                    return MoveJointHealingOrbitC(ref j, factor);
                case 14:
                    if (j.LifeTime < 5f)
                        j.Light *= MathF.Pow(j.LifeTime / 5f, factor);
                    j.Scale *= MathF.Pow(1.05f, factor);
                    if (!TryGetOwnerPosition(j.Target, out Vector3 anchor14))
                        return false;
                    j.Position = anchor14;
                    return true;
                case 15:
                case 16:
                    if (!TryGetOwnerPosition(j.Target, out Vector3 target15))
                        return false;
                    j.Velocity += 4f * factor;
                    MoveJointHummingA(ref j.Angle, j.Position, target15, 10f, factor);
                    j.Light *= MathF.Pow(1f / 1.08f, factor);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        target15, (R(4) + 4f) * 0.2f, j.Light,
                        j.Target, R(360), j.SubType == 16 ? 1 : 0);
                    return true;
                default:
                    return MoveJointHealingOtherC(ref j, factor);
            }
        }

        private static bool IsHealingFixedTargetC(int subtype) =>
            subtype == 6 || subtype == 7 || subtype == 12;

        private bool MoveJointHealingOrbitC(ref ClassicJoint j, float factor)
        {
            if (!TryGetOwnerPosition(j.Target, out Vector3 target))
                return false;
            // Native shifts all existing tail quads with TargetPosition's
            // OLD owner delta before replacing TargetPosition.
            Vector3 diff = j.TargetPosition - target;
            int count = Math.Min(Math.Max(j.NumTails, 0), ClassicJoint.MaxTailSegments);
            if (j.Tails != null)
            {
                for (int n = count - 1; n >= 0; n--)
                {
                    int offset = n * ClassicJoint.VerticesPerTail;
                    for (int k = 0; k < ClassicJoint.VerticesPerTail; k++)
                        j.Tails[offset + k] -= diff;
                }
            }
            j.TargetPosition = target;
            float orbit = j.MultiUse * 0.1f;
            j.Position.X = target.X + MathF.Sin(orbit) * j.Direction.X;
            j.Position.Y = target.Y + MathF.Cos(orbit) * j.Direction.X;
            if (j.SubType == 9)
            {
                j.Position.Z = target.Z + 10f + (90f - j.LifeTime);
                j.MultiUse += 2f * factor;
            }
            else
            {
                j.Position.Z = target.Z + 300f * (j.LifeTime / 80f);
                j.MultiUse += (j.Collision ? 2f : -2f) * factor;
            }
            j.Direction.X -= factor;
            if (j.Direction.X < 0f)
            {
                j.Direction.X = 0f;
                j.LifeTime = 0f;
            }
            if (j.SubType == 9)
            {
                if (j.LifeTime < 50f)
                    j.Light *= MathF.Pow(1f / 1.1f, factor);
                else if (j.LifeTime > 80f)
                    j.Light *= MathF.Pow(1.25f, factor);
            }
            else
            {
                if (j.LifeTime < 40f)
                    j.Light *= MathF.Pow(1f / 1.1f, factor);
                else if (j.LifeTime > 68f)
                    j.Light *= MathF.Pow(1.2f, factor);
            }
            return true;
        }

        private bool MoveJointHealingOtherC(ref ClassicJoint j, float factor)
        {
            int subtype = j.SubType;
            j.Velocity += (subtype == 6 || subtype == 7 ? 2f : 4f) * factor;
            Vector3 target = j.TargetPosition;
            if (!IsHealingFixedTargetC(subtype))
            {
                if (subtype == 8 || subtype == 13 || subtype == 17)
                {
                    if (j.LifeTime > 10f)
                        MoveJointHummingA(ref j.Angle, j.Position, target, 10f, factor);
                    else if (j.LifeTime < 6f)
                        j.Light *= MathF.Pow(1f / 1.8f, factor);
                    // Native contains identical sprite branches for 8/13/17.
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        j.Position, 1f, j.Light);
                }
                else
                {
                    if (!TryGetOwnerPosition(j.Target, out Vector3 anchor))
                        return false;
                    target = anchor;
                    target.Z += 120f * factor;
                    MoveJointHummingA(ref j.Angle, j.Position, target, 10f, factor);
                }
            }
            float lum = (12f - j.LifeTime) * 0.1f;
            switch (subtype)
            {
                case 1: j.Light = new Vector3(lum * 0.4f, lum * 0.6f, lum); break;
                case 2: j.Light = new Vector3(lum * 0.4f, lum, lum * 0.6f); break;
                case 3: j.Light = new Vector3(lum, lum * 0.6f, lum * 0.4f); break;
                case 11: j.Light = new Vector3(lum * 0.9f, lum * 0.49f, lum * 0.04f); break;
                case 12: j.Light = new Vector3(lum * 0.9f, lum * 0.39f, lum * 0.03f); break;
            }
            if (subtype == 6)
            {
                if (j.LifeTime <= 10f)
                {
                    float alpha = (6f - MathF.Abs(j.LifeTime - 6f)) * 0.15f;
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        target, (R(8) + 8f) * 0.05f,
                        new Vector3(alpha), j.Target, R(360));
                }
            }
            else if (subtype != 7 && subtype != 8 && subtype != 12)
            {
                if ((int)j.LifeTime == 1)
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        target, (R(8) + 8f) * 0.2f,
                        j.Light, j.Target, R(360));
                if (subtype == 11)
                {
                    float light = (R(4) + 4f) * 0.01f;
                    AddClassicTerrainLight(target.X, target.Y,
                        new Vector3(light), 1f);
                }
            }
            return true;
        }

        // ---------------- SPIRIT / SPIRIT2 (3746-4201) ----------------

        private bool MoveJointSpiritC(
            ref ClassicJoint j, float factor, int index,
            ClassicFxHandle handle, in Vector3 entryPos,
            in Vector3 entryTarget)
        {
            switch (j.SubType)
            {
                case 0: case 5: case 19:
                    return MoveSpiritHomingC(ref j, factor, handle,
                        entryPos, entryTarget);
                case 1:
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        j.Position, 4f, new Vector3(0.8f, 0.4f, 1f),
                        j.Target, R(360));
                    return true;
                case 3: case 13:
                    return MoveSpiritScrewC(ref j, factor, index);
                case 2: case 6: case 7: case 21: case 22: case 23:
                    return MoveSpiritFlareC(ref j, factor);
                case 8: case 20:
                    if (j.LifeTime < 42f)
                    {
                        j.Velocity = 50f;
                        if (j.Angle.X <= 180f) j.Angle.X = 180f;
                        if (j.Angle.X <= 360f)
                            j.Angle.X += (R(3) + 2f) * factor;
                    }
                    else
                    {
                        j.Velocity = 10f;
                        j.Angle.X = 110f;
                    }
                    return true;
                case 10:
                    j.Velocity = 50f;
                    j.Angle.Z -= 50f * factor;
                    return true;
                case 9:
                    j.Position.X += (R(10) - 5f) * factor;
                    j.Position.Y += (R(10) - 5f) * factor;
                    j.Position.Z += j.Velocity * factor;
                    j.Velocity += factor;
                    if (j.LifeTime < 10f)
                        j.Light *= MathF.Pow(1f / 1.15f, factor);
                    return true;
                case 11:
                    j.Angle.X = j.LifeTime;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapFire + 1,
                        j.Position, j.Angle, Vector3.One, 5, 0.9f);
                    if (Random.FpsCheck(200, Clock))
                        CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                            j.Position, j.Position, j.Angle, 12,
                            scale: 0.9f);
                    j.Position.X += MathF.Cos(j.Angle.Z) * 20f * factor;
                    j.Position.Y += MathF.Sin(j.Angle.Z) * 20f * factor;
                    return true;
                case 12:
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke + 3,
                        j.Position, j.Angle, new Vector3(1f, 0.2f, 0.1f),
                        0, (R(32) + 48f) * 0.01f);
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapFire + 1,
                        j.Position, j.Angle, Vector3.One, 6, 0.6f);
                    return true;
                case 14:
                {
                    j.Angle.X -= 2f * factor;
                    int roll = R(5);
                    int type = roll == 0
                        ? ClassicTextureIds.BitmapWaterfall3
                        : roll <= 2 ? ClassicTextureIds.BitmapWaterfall4
                        : ClassicTextureIds.BitmapWaterfall5;
                    CreateParticleFpsChecked(type, j.Position, j.Angle,
                        j.Light, roll <= 2 ? 2 : 4);
                    return true;
                }
                case 15:
                    return MoveSpirit15C(ref j, factor);
                case 16:
                    j.Angle.X = j.LifeTime;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapWaterfall5,
                        j.Position, j.Angle, j.Light, 4);
                    j.Position.X += MathF.Cos(j.Angle.Z) * 30f * factor;
                    j.Position.Y += MathF.Sin(j.Angle.Z) * 30f * factor;
                    return true;
                case 17:
                    if ((int)j.LifeTime == 100)
                    {
                        for (int i = 0; i < 120; i++)
                        {
                            Vector3 angle = new Vector3(0f, 0f, i * 3f);
                            CreateJointFpsChecked(ClassicTextureIds.BitmapJointSpirit,
                                j.Position, j.Position, angle, 16,
                                scale: 0.9f);
                        }
                    }
                    return true;
                case 18:
                    return MoveSpirit18C(ref j, factor);
                case 24:
                    return MoveSpirit24C(ref j, factor);
                case 25:
                    return MoveSpirit25C(ref j, factor, handle);
                // Native 4 has no movement subtype branch: only common step.
                default:
                    return true;
            }
        }

        private bool MoveSpiritHomingC(
            ref ClassicJoint j, float factor, ClassicFxHandle handle,
            in Vector3 entryPos, in Vector3 entryTarget)
        {
            if (j.Scale == 80f)
            {
                // CreateEffectFpsChecked(MODEL_LASER). Native 'scale==80'
                // path, subtype 5 requests laser subtype 3.
                if (Random.FpsCheck(1, Clock))
                    JointSecondaryEffectRequested?.Invoke(handle,
                        "MODEL_LASER", j.Position, j.Angle, j.Light,
                        j.SubType == 5 ? 3 : 0, 1f);
                if (JointSpiritSiegeNoAttackZoneResolver?.Invoke(j.Position) == true)
                {
                    j.Velocity = 0f;
                    j.LifeTime *= MathF.Pow(1f / 5f, factor);
                    return true;
                }
                if ((int)j.LifeTime % 15 == 0 &&
                    JointSpiritIsLocalHeroResolver?.Invoke(j.Target) == true)
                    JointSpiritAttackRangeRequested?.Invoke(j.Skill,
                        j.Position, 150f, j.Weapon, j.PKKey,
                        j.SkillSerialNum);
            }
            else if (j.SubType == 19)
                JointSecondaryEffectRequested?.Invoke(handle,
                    "MODEL_SKULL", j.Position, j.Angle, j.Light, 0, 1f);

            if (!TryGetOwnerPosition(j.Target, out Vector3 ownerPosition))
                return false;
            j.TargetPosition = ownerPosition;
            j.TargetPosition.Z += 80f;
            float dx = entryPos.X - entryTarget.X;
            float dy = entryPos.Y - entryTarget.Y;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            MoveJointHummingA(ref j.Angle, j.Position, j.TargetPosition,
                j.SubType == 5 ? 2f : 10f, factor);
            if (!j.Collision && distance <= j.Velocity * 2f * factor)
                j.Collision = true;
            if (j.SubType != 5)
            {
                j.Direction.X += (R(32) - 16f) * 0.2f;
                j.Direction.Z += (R(32) - 16f) * 0.8f;
                j.Angle.X += j.Direction.X * factor;
                j.Angle.Z += j.Direction.Z * factor;
                j.Direction.X *= 0.6f;
                j.Direction.Z *= 0.8f;
            }
            float height = RequestTerrainHeight(j.Position.X, j.Position.Y);
            if (j.Position.Z < height + 100f)
            {
                j.Direction.X = 0f;
                j.Angle.X = -5f;
            }
            if (j.Position.Z > height + 400f)
            {
                j.Direction.X = 0f;
                j.Angle.X = 5f;
            }
            if (j.SubType != 19)
            {
                float lum = j.LifeTime * 0.1f;
                j.Light = new Vector3(lum);
                float terrainLum = -(R(4) + 4f) * 0.01f;
                AddClassicTerrainLight(j.Position.X, j.Position.Y,
                    new Vector3(terrainLum), 4f);
            }
            return true;
        }

        private Vector3 GetJointMagicScrewC(int index)
        {
            // Original GetMagicScrew(): ZzzEffectJoint.cpp, final function.
            float param = index + (int)(Clock.WorldTimeMilliseconds / 40f);
            float a = (param + 55555f) * 0.048f;
            float b = param * 0.0613f;
            float c = (param + 11111f) * 0.1113f;
            float v0 = MathF.Sin(a) * MathF.Cos(b);
            float v1 = MathF.Sin(a) * MathF.Sin(b);
            float v2 = MathF.Cos(a);
            float si = MathF.Sin(c);
            float co = MathF.Cos(c);
            return new Vector3(co * v1 - si * v2,
                si * v1 + co * v2, v0);
        }

        private bool MoveSpiritScrewC(ref ClassicJoint j, float factor, int index)
        {
            float scale = 3f;
            Vector3 color = new Vector3(1f, 0.5f, 0.1f);
            if (j.SubType != 13 || j.LifeTime > 28f)
                j.Position += GetJointMagicScrewC(index) * 50f;
            else if (TryGetOwnerPosition(j.Target, out Vector3 ownerPosition))
            {
                if (Random.FpsCheck(2, Clock))
                {
                    if (j.Angle.X < -90f) j.Angle.X += 20f;
                    else j.Angle.X -= 20f;
                }
                Vector3 target = ownerPosition;
                target.X += (R(150) - 70f) * factor;
                target.Y += (R(150) - 70f) * factor;
                target.Z += 50f * factor;
                float distance = MoveJointHummingA(ref j.Angle, j.Position,
                    target, j.Velocity - 20f, factor);
                if (j.Velocity > 50f) j.Velocity = 50f;
                else j.Velocity += factor;
                if (distance < 50f) j.LifeTime -= factor;
                if (j.LifeTime < 10f)
                {
                    j.Light *= MathF.Pow(1f / 1.2f, factor);
                    color = j.Light;
                }
                if (j.SubType == 13)
                {
                    color = j.Light;
                    scale = 2f * (j.LifeTime / 10f) + 2f;
                }
            }
            CreateSprite(ClassicTextureIds.BitmapLight,
                j.Position, scale, color, j.Target, R(360));
            CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                j.Position, scale / 2f, color, j.Target, R(360));
            return true;
        }

        private bool MoveSpiritFlareC(ref ClassicJoint j, float factor)
        {
            Vector3 light = j.Light;
            switch (j.SubType)
            {
                case 2: light = new Vector3(1f, 0.5f, 0.1f); break;
                case 21: light = new Vector3(0.1f, 0.5f, 1f); break;
                case 22: case 23:
                    if (j.Skill == 0 || j.Skill == 1)
                        light = Vector3.One;
                    break;
                case 6: case 7:
                    if (j.Skill == 0) light = new Vector3(0.3f, 0.3f, 1f);
                    else if (j.Skill == 1) light = new Vector3(0.5f);
                    break;
            }
            if (j.LifeTime < 10f)
            {
                j.Light *= MathF.Pow(1f / 1.2f, factor);
                light = j.Light;
            }
            else if (j.LifeTime > 18f && j.LifeTime < 20f &&
                     (j.SubType == 2 || j.SubType == 21))
            {
                Vector3 angle = new Vector3(0f, 0f, R(360));
                Vector3 position = j.StartPosition +
                    new Vector3(R(200) - 100f, R(200) - 100f, -200f);
                if (j.SubType == 2)
                    CreateJointFpsChecked(ClassicTextureIds.BitmapFlare,
                        position, position, angle, 2, scale: 40f);
            }
            int bit = j.SubType == 22 || j.SubType == 23
                ? ClassicTextureIds.BitmapFlame : ClassicTextureIds.BitmapLight;
            float scale = j.SubType == 22 || j.SubType == 23
                ? j.Scale + (20f - j.LifeTime) / 5f
                : 4f + (20f - j.LifeTime) / 5f;
            CreateSprite(bit, j.Position, scale, light, j.Target, R(360));
            j.Velocity += 5f * factor;
            return true;
        }

        private bool MoveSpirit15C(ref ClassicJoint j, float factor)
        {
            j.PKKey = unchecked((short)(j.PKKey + 30f * factor));
            if (j.LifeTime < 35f)
            {
                CreateParticleFpsChecked(ClassicTextureIds.BitmapFire,
                    j.Position, j.Angle, Vector3.One, 7, 1f);
                if (Random.FpsCheck(2, Clock))
                {
                    Vector3 pos = j.Position;
                    pos.X -= MathF.Sin(j.Angle.Z) *
                        (40f - j.PKKey * 0.1f) * factor;
                    pos.Y += MathF.Cos(j.Angle.Z) *
                        (40f - j.PKKey * 0.1f) * factor;
                    pos.Z = 350f;
                    for (int i = 0; i < 2; i++)
                        CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                            pos, pos, new Vector3(0f, 0f, i * 3f),
                            14, scale: 0.9f);
                }
                if (TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                {
                    Vector3 targetPos = j.Position;
                    targetPos.X += MathF.Sin(j.Angle.Z) *
                        (30f + j.PKKey * 0.1f) * factor;
                    targetPos.Y -= MathF.Cos(j.Angle.Z) *
                        (30f + j.PKKey * 0.1f) * factor;
                    // Main also resets target angles and optionally alpha.
                    JointSpiritOwnerPoseRequested?.Invoke(j.Target, targetPos,
                        new Vector3(0f, 0f, owner.Angle.Z), 15);
                }
            }
            j.Position.X += MathF.Sin(j.Angle.Z) *
                (30f + j.PKKey * 0.1f) * factor;
            j.Position.Y -= MathF.Cos(j.Angle.Z) *
                (30f + j.PKKey * 0.1f) * factor;
            return true;
        }

        private bool MoveSpirit18C(ref ClassicJoint j, float factor)
        {
            j.Velocity += 3f * factor;
            j.Angle.X = -90f;
            if ((int)j.LifeTime % 12 <= 5) j.PKKey = unchecked((short)(j.PKKey + 6f * factor));
            else j.PKKey = unchecked((short)(j.PKKey - 6f * factor));
            j.Angle.X -= j.PKKey * factor;
            j.Angle.Z += 10f * factor;
            if (TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                JointSpiritOwnerPoseRequested?.Invoke(j.Target, j.Position,
                    owner.Angle, 18);
            CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke,
                j.Position, j.Angle, Vector3.One, 19, 7f);
            return true;
        }

        private bool MoveSpirit24C(ref ClassicJoint j, float factor)
        {
            if (!TryGetOwnerSnapshot(j.Target, out _))
                return false;
            Vector3 target = j.TargetPosition;
            target.Z += 80f * factor;
            MoveJointHummingA(ref j.Angle, j.Position, target, 5f, factor);
            j.Direction.X += (R(32) - 16f) * 0.2f * factor;
            j.Direction.Z += (R(32) - 16f) * 0.8f * factor;
            j.Angle.X += j.Direction.X * factor;
            j.Angle.Z += j.Direction.Z * factor;
            j.Direction.X *= MathF.Pow(0.6f, factor);
            j.Direction.Z *= MathF.Pow(0.8f, factor);
            float lum = j.LifeTime < 30f
                ? j.LifeTime / 30f
                : j.LifeTime > 144f ? (160f - j.LifeTime) / 15f : 1f;
            j.Light = new Vector3(0.7f * lum, 0.7f * lum, 0.9f * lum);
            JointSpiritOwnerPoseRequested?.Invoke(j.Target, j.Position, j.Angle, 24);
            return true;
        }

        private bool MoveSpirit25C(ref ClassicJoint j, float factor,
            ClassicFxHandle handle)
        {
            bool alive = true;
            if (j.LifeTime < 10f)
            {
                j.Light *= MathF.Pow(1f / 1.45f, factor);
                if (j.Light.X < 0.2f) alive = false;
            }
            else
            {
                j.Position.X += (R(16) - 8f) * factor;
                j.Position.Y += (R(16) - 8f) * factor;
                j.Position.Z += j.Velocity * factor;
                j.Scale += 3f * factor;
            }
            CreateSprite(ClassicTextureIds.BitmapLight,
                j.Position, 0.5f, j.Light, j.Target);
            CreateSprite(ClassicTextureIds.BitmapDsShock,
                j.Position, 0.15f, j.Light, j.Target);
            if ((int)j.LifeTime == 10 && Random.FpsCheck(1, Clock))
                JointSecondaryEffectRequested?.Invoke(handle,
                    "BITMAP_FIRECRACKER0002", j.Position, j.Angle,
                    j.Light, j.Skill, 1f);
            return alive;
        }
    
    }
}
