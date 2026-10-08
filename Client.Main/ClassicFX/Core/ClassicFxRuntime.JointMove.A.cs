using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
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
                else
                    continue; // Other native branches are still pending.

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
    }
}
