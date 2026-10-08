using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain: src/source/Render/Effects/ZzzEffectJoint.cpp,
        // CreateJoint() lines 2579-2755. Creation state ONLY:
        // MoveJoint() and RenderJoints() are separate ports.
        private bool InitializeJointCreateY(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale)
        {
            switch (j.Type)
            {
                case ClassicTextureIds.BitmapFlash:
                    return InitializeFlashJointY(ref j, sourceScale);
                case ClassicTextureIds.BitmapDrainLifeGhost:
                    return InitializeDrainLifeGhostJointY(
                        ref j, sourceTargetPosition, sourceScale);
                case ClassicTextureIds.BitmapPinLight:
                    j.InitializeFirstTail();
                    j.Scale = sourceScale;
                    j.Velocity = R(60) + 15f;
                    j.LifeTime = R(3) + 1f;
                    j.MaxTails = 5;
                    return true;
                case ClassicTextureIds.BitmapForcePillar:
                    j.InitializeFirstTail();
                    InitializeFlatJointY(ref j, sourceScale, 7f, 5,
                        new Vector3(0.95f, 0.72f, 0.48f));
                    return true;
                case ClassicTextureIds.BitmapSwordEff:
                    j.InitializeFirstTail();
                    InitializeFlatJointY(ref j, sourceScale, 10f, 50,
                        new Vector3(0.6f, 0.6f, 1f));
                    return true;
                case ClassicTextureIds.BitmapGroundWind:
                    j.InitializeFirstTail();
                    InitializeFlatJointY(ref j, sourceScale, 10f, 8,
                        new Vector3(0.7f, 0.7f, 1f));
                    return true;
                case ClassicTextureIds.BitmapLava:
                    return InitializeLavaJointY(ref j, sourceScale);
                default:
                    return false;
            }
        }

        private bool InitializeFlashJointY(ref ClassicJoint j, float sourceScale)
        {
            // The original CreateJoint() skips the common initial quad
            // ONLY for BITMAP_FLASH subtype 6.
            if (j.SubType != 6)
                j.InitializeFirstTail();

            if (j.SubType <= 3 || j.SubType == 5)
            {
                j.MaxTails = 10;
                j.LifeTime = 40f;
                j.Velocity = 70f;
                j.Scale = sourceScale;
                j.ReverseUv = (byte)R(2);
                j.MultiUse = 150f;
                j.PKKey = 0;
                if (j.SubType == 2)
                {
                    j.Velocity = 30f;
                    j.MultiUse = 300f;
                    j.TexType = ClassicTextureIds.BitmapFlareForce;
                }
                else if (j.SubType == 3)
                {
                    j.Velocity = 30f;
                    j.MultiUse = 300f;
                    j.TexType = ClassicTextureIds.BitmapFlareBlue;
                }

                if (j.SubType == 5)
                {
                    j.TexType = ClassicTextureIds.BitmapFlare;
                    j.MaxTails = 15;
                    j.Velocity = R(20) + 10f;
                    j.ReverseUv = 2;
                }
                else
                {
                    j.Angle = new Vector3(90f, 0f, 0f);
                }
                j.HeadAngle = j.Angle;
                return true;
            }

            if (j.SubType == 4)
            {
                // Native requires Target->Position and Target->Angle.
                if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                    return false;

                j.Scale = sourceScale;
                j.MaxTails = 20;
                j.LifeTime = 30f;
                j.Velocity = 0f;
                j.OnlyOneRender = 1;
                j.Weapon = 0f;
                j.TexType = ClassicTextureIds.BitmapFlareBlue;
                j.Light = Vector3.One;
                j.Direction = Vector3.Zero;
                j.Position = owner.Position;
                j.Angle = owner.Angle;
                return true;
            }

            if (j.SubType == 6)
            {
                j.Scale = sourceScale;
                j.MaxTails = 30;
                j.LifeTime = 25f;
                j.Weapon = j.LifeTime;
                j.Velocity = (70f + R(3)) * MathF.PI / 180f;
                j.Light = new Vector3(0.8f, 0.8f, 1f);
                j.Direction = new Vector3(
                    0f, -(40f + R(4)), MathF.Sin(j.Velocity) * 10f);
                j.Position.Y -= 20f;
                j.Position.Z += 130f;
                j.StartPosition = j.Position;
                // j.Angle is the original Angle input, still unchanged.
                j.CreateTails = false;
                j.ReverseUv = 3;
                // Native CreateTailAxis(o, AngleMatrix(Angle), 1), then
                // resets NumTails to zero (but retains the generated quad).
                AppendJointTailAxisY(ref j);
                j.NumTails = 0;
                return true;
            }

            if (j.SubType == 7)
            {
                // Original "6 Opener": the parent immediately spawns three
                // FLASH subtype 6 children, then expires (LifeTime = 0).
                j.CreateTails = false;
                j.LifeTime = 0f;
                ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 offset = new Vector3((i - 1) * 100f, -50f, 0f);
                    Vector3 position = j.Position +
                        ClassicMath.VectorRotate(offset, matrix);
                    CreateJoint(
                        ClassicTextureIds.BitmapFlash,
                        position, position, j.Angle,
                        subType: 6, scale: 40f);
                }
                return true;
            }

            // The native initializer provides no lifetime for other subtypes.
            return false;
        }

        private bool InitializeDrainLifeGhostJointY(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale)
        {
            if (j.SubType != 0)
                return false;

            j.InitializeFirstTail();
            j.RenderType = 1; // RENDER_TYPE_ALPHA_BLEND
            j.Angle.Z += Random.FpsCheck(2, Clock) ? 90f : -90f;
            j.TargetPosition = sourceTargetPosition;
            j.Angle.X += R(100) - 50f;
            j.Angle.Y += R(100) - 50f;
            j.Angle.Z += R(100) - 50f;
            j.Velocity = 1f + R(10) * 0.2f;
            j.LifeTime = 30f + (R(20) - 10f);
            j.Scale = sourceScale + (R(60) - 30f);
            j.MaxTails = 20 + (R(10) - 5);
            return true;
        }

        private static void InitializeFlatJointY(
            ref ClassicJoint j, float sourceScale,
            float lifetime, int maxTails, in Vector3 light)
        {
            j.RenderType = 1; // RENDER_TYPE_ALPHA_BLEND
            j.RenderFace = 2; // RENDER_FACE_TWO
            j.Scale = sourceScale;
            j.LifeTime = lifetime;
            j.MaxTails = maxTails;
            j.CreateTails = true;
            j.Light = light;
        }

        private bool InitializeLavaJointY(ref ClassicJoint j, float sourceScale)
        {
            // Native: Models[Target->Type].Actions[Target->CurrentAction]
            //            .PlaySpeed. MonoGame stores the same field in BMD.
            // Never substitute AnimationSpeed or a guessed constant.
            if (!TryGetOwnerWorldObject(j.Target, out WorldObject worldObject) ||
                worldObject is not ModelObject modelObject ||
                modelObject.Model?.Actions == null ||
                (uint)modelObject.CurrentAction >= (uint)modelObject.Model.Actions.Length)
            {
                return false;
            }

            j.InitializeFirstTail();
            InitializeFlatJointY(
                ref j, sourceScale, 10f, 10, Vector3.One);
            j.Velocity = modelObject.Model.Actions[modelObject.CurrentAction].PlaySpeed;
            return true;
        }

        // Exact axis=1 variant of native CreateTailAxis(): same fixed
        // storage, shifts existing segments and constructs XY-oriented
        // segment 0. For FLASH/6 the native calls it once at creation.
        private static void AppendJointTailAxisY(ref ClassicJoint j)
        {
            if (j.Tails == null || j.MaxTails <= 0)
                return;

            j.NumTails = Math.Min(j.NumTails + 1, j.MaxTails - 1);
            j.NumTails = Math.Min(j.NumTails, ClassicJoint.MaxTailSegments - 1);
            for (int t = j.NumTails - 1; t >= 0; t--)
            {
                int from = t * ClassicJoint.VerticesPerTail;
                int to = (t + 1) * ClassicJoint.VerticesPerTail;
                for (int k = 0; k < ClassicJoint.VerticesPerTail; k++)
                    j.Tails[to + k] = j.Tails[from + k];
            }

            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            float halfScale = j.Scale * 0.5f;
            j.Tails[0] = j.Position + ClassicMath.VectorRotate(
                new Vector3(-halfScale, 0f, 0f), matrix);
            j.Tails[1] = j.Position + ClassicMath.VectorRotate(
                new Vector3(halfScale, 0f, 0f), matrix);
            j.Tails[2] = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, -halfScale, 0f), matrix);
            j.Tails[3] = j.Position + ClassicMath.VectorRotate(
                new Vector3(0f, halfScale, 0f), matrix);
        }
    }
}
