using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
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
    }
}
