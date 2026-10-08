using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain: _enum.h MODEL_FENRIR_SKILL_THUNDER = 390.
        // Joint creation accepts a *model type* as well as BITMAP types.
        // This is not a texture ID: TexType is set explicitly in its switch.
        public const int ClassicModelFenrirSkillThunder = 390;

        /// <summary>
        /// Integrate the original CHARACTER scan for Thunder subtype 15.
        /// Implementations should write up to destination.Length monster indices
        /// (excluding the owner) and return the written count; destination is
        /// the persistent joint buffer, not a per-frame allocation.
        /// </summary>
        public Func<Vector3, ClassicFxOwner, int[], int> JointThunderMonsterScanResolver { get; set; }

        // MuMain: ZzzEffectJoint.cpp, CreateJoint(), lines 1024-1071,
        // 1101-1515. This only builds native *creation* state.
        private bool InitializeJointCreateS(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale,
            Vector3? priorColor, short originalPkKey)
        {
            if (j.Type != ClassicModelFenrirSkillThunder &&
                j.Type != ClassicTextureIds.BitmapJointThunder &&
                j.Type != ClassicTextureIds.BitmapJointThunder + 1)
                return false;

            // All three native families start with the default first tail.
            // Specific SubTypes may disable FUTURE tail emission after that.
            j.InitializeFirstTail();

            if (j.Type == ClassicModelFenrirSkillThunder)
                return InitializeFenrirThunderS(ref j, sourceScale);

            if (j.Type == ClassicTextureIds.BitmapJointThunder)
                return InitializeThunderS(ref j, sourceTargetPosition, sourceScale, priorColor, originalPkKey);

            return InitializeThunderPlusOneS(ref j, sourceTargetPosition);
        }

        private static bool InitializeFenrirThunderS(ref ClassicJoint j, float sourceScale)
        {
            j.Scale = sourceScale;
            j.MaxTails = 50;
            j.Velocity = 50f;
            j.LifeTime = 20f;
            j.TileMapping = true;
            switch (j.SubType)
            {
                case 0: j.TexType = ClassicTextureIds.BitmapJointThunder;
                    j.Light = new Vector3(0.7f, 1f, 0.7f); break;
                case 1: j.TexType = ClassicTextureIds.BitmapJointThunder;
                    j.Light = new Vector3(1f, 0.6f, 0.6f); break;
                case 2: j.TexType = ClassicTextureIds.BitmapJointThunder;
                    j.Light = new Vector3(0.7f, 0.7f, 1f); break;
                case 3: j.TexType = ClassicTextureIds.BitmapJointThunder;
                    j.Light = new Vector3(0.9f, 0.9f, 0.3f); break;
                case 4: j.TexType = ClassicTextureIds.BitmapFlash;
                    j.Light = new Vector3(0.1f, 0.8f, 0.1f); break;
                case 5: j.TexType = ClassicTextureIds.BitmapFlash;
                    j.Light = new Vector3(1f, 0.3f, 0.2f); break;
                case 6: j.TexType = ClassicTextureIds.BitmapFlash;
                    j.Light = new Vector3(0.2f, 0.3f, 1f); break;
                case 7: j.TexType = ClassicTextureIds.BitmapFlash;
                    j.Light = new Vector3(0.8f, 0.8f, 0.1f); break;
                default: return false; // No native texture initialization.
            }
            return true;
        }

        private bool InitializeThunderS(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale,
            Vector3? priorColor, short originalPkKey)
        {
            j.Scale = sourceScale;
            j.MaxTails = 50;
            j.Velocity = 50f;
            switch (j.SubType)
            {
                case 0:
                case 20:
                    j.LifeTime = 2f;
                    return true;
                case 1:
                    j.Velocity = 20f + R(10);
                    j.LifeTime = R(8) + 8f;
                    return true;
                case 2:
                case 21:
                    j.LifeTime = 2f;
                    j.Light = j.SubType == 2
                        ? new Vector3(1f, 0.1f, 0f)
                        : new Vector3(1f, 0.5f, 0.4f);
                    // Native: MODEL_PLAYER BoneTransform[33], local (0,-150,0).
                    // Do not fabricate a bone if the real model is unavailable.
                    return TryTransformOwnerBonePosition(
                        j.Target, 33, new Vector3(0f, -150f, 0f),
                        out j.TargetPosition);
                case 3:
                    j.Velocity = j.Skill;
                    j.PKKey = -1;
                    j.LifeTime = originalPkKey; // Main reads original PKKey, then stores -1 in the joint.
                    j.MaxTails = j.Skill;
                    j.Light = new Vector3(0.5f, 0.5f, 1f);
                    j.StartPosition = j.Position;
                    return true;
                case 4:
                case 5:
                    j.Velocity = j.SubType == 4 ? 60f : -60f;
                    j.LifeTime = 20f;
                    j.MaxTails = 10;
                    j.Direction = Vector3.Zero;
                    j.Light = new Vector3(0.5f, 0.5f, 1f);
                    j.TargetPosition = sourceTargetPosition;
                    j.StartPosition = j.Position;
                    if (j.SubType == 5) j.TargetPosition.Z += 600f;
                    return true;
                case 6:
                    j.LifeTime = R(20) + 6f;
                    j.CreateTails = false;
                    j.Velocity = 15f + R(10);
                    j.Light = new Vector3(R(10) / 15f + 0.1f);
                    j.StartPosition = j.Position;
                    return true;
                case 7:
                    j.TileMapping = true;
                    j.Velocity = 5f;
                    j.MaxTails = 3;
                    j.LifeTime = 1f;
                    j.Light = new Vector3(0.5f, 0.5f, 1f);
                    j.StartPosition = j.Position;
                    NativeJointMoveHummingS(ref j, 360f);
                    return true;
                case 8:
                    j.Scale = 5f;
                    j.Velocity = R(10) + 6f;
                    j.LifeTime = R(8) + 8f;
                    j.MaxTails = 2;
                    j.Light = Vector3.One;
                    return true;
                case 9:
                    j.Velocity = 80f + R(20);
                    j.LifeTime = 30f;
                    j.Light = new Vector3(R(10) / 15f + 0.1f);
                    j.StartPosition = j.Position;
                    return true;
                case 10:
                case 14:
                    InitializeThunderStaticTailS(ref j, sourceTargetPosition, j.SubType == 10);
                    return true;
                case 11:
                    j.Light = new Vector3(1f, 0.5f, 0.1f);
                    j.LifeTime = 2f;
                    return true;
                case 12:
                    j.Light = new Vector3(1f, 0.1f, 0.1f);
                    j.LifeTime = 10f;
                    return true;
                case 13:
                    j.Light = new Vector3(1f, 0.1f, 0.1f);
                    j.LifeTime = 2f;
                    j.SubType = 11; // Native changes the active SubType.
                    return true;
                case 15:
                    return InitializeThunderTargetsS(ref j, sourceTargetPosition);
                case 16:
                    j.Velocity = 20f + R(10);
                    j.LifeTime = R(2) + 2f;
                    return true;
                case 17:
                    j.Velocity = 20f + R(10);
                    j.LifeTime = 10f;
                    return true;
                case 18:
                    j.Velocity = 80f;
                    j.MaxTails = 5;
                    j.LifeTime = 10f;
                    j.Light = Vector3.One;
                    j.StartPosition = j.Position;
                    return true;
                case 19:
                    j.MaxTails = 30;
                    j.LifeTime = 2f;
                    j.Velocity = j.Scale * 3f;
                    j.StartPosition = j.Position;
                    return true;
                case 22:
                case 23:
                case 24:
                    j.LifeTime = 15f;
                    j.Scale = sourceScale + (R(50) + 50f) * 0.1f;
                    j.MaxTails = 30;
                    j.Velocity = 20f;
                    return true;
                case 25:
                    j.LifeTime = 20f;
                    j.MaxTails = 8;
                    j.Velocity = 20f + R(10);
                    return true;
                case 26:
                    j.Velocity = 20f + R(10);
                    j.LifeTime = R(8) + 8f;
                    return true;
                case 27:
                case 28:
                    j.LifeTime = 2f;
                    // Original unconditionally VectorCopy(vPriorColor,...).
                    // Reject absent colors instead of dereferencing null.
                    if (!priorColor.HasValue) return false;
                    j.Light = priorColor.Value;
                    return true;
                case 33:
                    j.Light = new Vector3(0.3f, 0.3f, 1f);
                    j.Velocity = 20f + R(10);
                    j.LifeTime = R(2) + 2f;
                    return true;
                default:
                    return false;
            }
        }

        private bool InitializeThunderTargetsS(
            ref ClassicJoint j, in Vector3 sourceTargetPosition)
        {
            j.RenderFace = 0;
            j.LifeTime = 80f;
            j.MaxTails = 0;
            j.MultiUse = 0f;
            // Native scans visible living monsters within XY distance 400;
            // MonoGame's index convention is not equivalent to the C++ array.
            // The resolver must provide genuine indices using the game state.
            int count = JointThunderMonsterScanResolver?.Invoke(
                j.Position, j.Target, j.TargetIndices) ?? 0;
            count = Math.Clamp(count, 0, j.TargetIndices.Length);
            j.Weapon = count * Clock.FrameFactor * 15f;
            j.MultiUse = 0f;
            j.StartPosition = sourceTargetPosition;
            j.StartPosition.Z += 150f;
            return true;
        }

        private void InitializeThunderStaticTailS(
            ref ClassicJoint j, in Vector3 sourceTargetPosition, bool jitter)
        {
            j.TileMapping = jitter;
            j.LifeTime = 0f;
            j.MaxTails = 10;
            j.Angle = Vector3.Zero;
            j.Light = new Vector3(0.3f, 0.3f, 1f);
            j.StartPosition = (sourceTargetPosition - j.Position) / j.MaxTails;
            Vector3 step = j.StartPosition;
            for (int i = 0; i < j.MaxTails - 1; i++)
            {
                Vector3 displacement = step;
                if (jitter)
                {
                    int range = j.Target.HasOwner ? 16 : 8;
                    displacement.X += R(range) - range / 2;
                    displacement.Y += R(range) - range / 2;
                }
                j.Position += displacement * Clock.FrameFactor;
                // CreateTail() uses AngleMatrix(Angle=0) and the current j.Position.
                AppendJointTailQ(ref j);
                j.Position.X += step.X - displacement.X;
                j.Position.Y += step.Y - displacement.Y;
            }
            j.Position = j.TargetPosition;
        }

        private void NativeJointMoveHummingS(ref ClassicJoint j, float turn)
        {
            float step = turn * Clock.FrameFactor;
            float targetYaw = CreateNativeJointAngleQ(j.Position, j.TargetPosition);
            j.Angle.Z = StepJointAngleS(j.Angle.Z, targetYaw, step);
            Vector3 diff = j.Position - j.TargetPosition;
            float xyDistance = MathF.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
            float pitch = 360f - NativeJointCreateAngleS(
                j.Position.Z, xyDistance, j.TargetPosition.Z, 0f);
            j.Angle.X = StepJointAngleS(j.Angle.X, pitch, step);
        }

        private static float NativeJointCreateAngleS(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            if (MathF.Abs(dx) < float.Epsilon && MathF.Abs(dy) < float.Epsilon) return 0f;
            return NormalizeJointAngleS(MathF.Atan2(dx, -dy) * (180f / MathF.PI));
        }

        private static float NormalizeJointAngleS(float a) => (a % 360f + 360f) % 360f;

        private static float StepJointAngleS(float value, float target, float maxStep)
        {
            float delta = NormalizeJointAngleS(target) - NormalizeJointAngleS(value);
            if (delta > 180f) delta -= 360f;
            else if (delta < -180f) delta += 360f;
            delta = MathHelper.Clamp(delta, -maxStep, maxStep);
            return NormalizeJointAngleS(value + delta);
        }

        private bool InitializeThunderPlusOneS(ref ClassicJoint j, in Vector3 sourceTargetPosition)
        {
            j.TexType = ClassicTextureIds.BitmapJointThunder;
            switch (j.SubType)
            {
                case 0:
                    j.MaxTails = 50;
                    j.Velocity = 80f + R(20);
                    j.LifeTime = 20f;
                    j.Light.Z = j.MaxTails;
                    j.StartPosition = j.Position;
                    return true;
                case 1: case 2: case 3: case 5: case 6: case 7:
                    j.MaxTails = 50;
                    j.LifeTime = 20f;
                    j.Position.X += R(10) - 5f;
                    j.Position.Y += R(10) - 5f;
                    j.Position.Z += j.SubType == 7 ? 1050f : 800f;
                    j.Angle = Vector3.Zero;
                    j.TargetPosition = sourceTargetPosition;
                    j.CreateTails = j.SubType != 1 && j.SubType != 7;
                    j.StartPosition = j.Position;
                    if (j.SubType == 5)
                        j.Light = new Vector3(1f, 0.5f, 0.2f);
                    else if (j.SubType == 6)
                    {
                        j.Velocity = 0.3f;
                        j.Light = Vector3.One;
                        j.Position.Z += 700f; // Main adjusts after StartPosition copy.
                    }
                    return true;
                case 4:
                    j.MaxTails = 50;
                    j.LifeTime = 20f;
                    j.CreateTails = false;
                    j.OnlyOneRender = 2;
                    j.TargetIndices[0] = (int)(j.MaxTails / 1.5f);
                    j.TargetIndices[1] = j.MaxTails - 1;
                    j.StartPosition.X = 1f / j.MaxTails;
                    j.StartPosition.Y = 1f / (j.MaxTails - j.TargetIndices[0] - 1);
                    j.Angle = Vector3.Zero;
                    j.Light = new Vector3(1f, 0.8f, 1f);
                    j.TargetPosition = sourceTargetPosition;
                    j.TargetPosition.Z += 100f;
                    // Native assigns to the caller's Angle argument, not j.Angle.
                    return true;
                case 8:
                case 9:
                case 10:
                    j.MaxTails = 50;
                    j.LifeTime = 20f;
                    j.CreateTails = false;
                    if (j.SubType == 8)
                    {
                        j.Position.X += R(10) - 5f;
                        j.Position.Y += R(10) - 5f;
                        j.Position.Z += 1100f;
                    }
                    else if (j.SubType == 9) j.Position.X -= 50f;
                    else j.Position.Y -= 350f;
                    j.Angle = Vector3.Zero;
                    j.TargetPosition = sourceTargetPosition;
                    j.StartPosition = j.Position;
                    return true;
                case 11:
                    j.Velocity = 80f + R(20);
                    j.Light.Z += 0.2f;
                    j.LifeTime = 5f;
                    j.MaxTails = 30;
                    j.StartPosition = j.Position;
                    j.TargetPosition = sourceTargetPosition;
                    return true;
                case 12:
                    j.Velocity = 80f + R(20);
                    j.Light.Z = j.MaxTails; // Original reads zero before assigning 30.
                    j.MaxTails = 30;
                    j.LifeTime = 4f;
                    j.CreateTails = false;
                    j.StartPosition = j.Position;
                    Vector3 offset = ClassicMath.VectorRotate(
                        new Vector3(0f, j.Scale, 0f),
                        ClassicMath.AngleMatrix(j.Angle));
                    offset *= j.Scale * 0.5f;
                    j.Position += offset * Clock.FrameFactor;
                    j.StartPosition = j.Position;
                    int spread = (int)(j.Scale / 4f);
                    if (spread <= 0) return false; // Native modulo zero undefined.
                    j.TargetPosition = sourceTargetPosition + new Vector3(
                        R(spread * 2) - spread,
                        R(spread * 2) - spread,
                        R(spread * 2) - spread);
                    return true;
                default:
                    return false;
            }
        }
    }
}
