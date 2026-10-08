using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Native ZzzEffectJoint.cpp MoveJoint(): terminal families 6239-6940.
    /// This file implements movement, not rendering. Missing integrations
    /// are exposed explicitly instead of being silently simulated.
    /// </summary>
    public sealed partial class ClassicFxRuntime
    {
        // The native client renders MODEL_SHADOW_BODY / MODEL_SKIN_SHELL
        // on FLASH 0/1 terrain impact, or creates a SHOCK_WAVE effect.
        // Event consumers should use the normal Effect system when ready.
        public event Action<string, Vector3, Vector3, Vector3, int> JointMoveEffectRequested;

        // Native DRAIN_LIFE_GHOST targets o->Target->Owner, which is distinct
        // from the immediate target. Resolve this OBJECT* relation externally.
        public Func<ClassicFxOwner, Vector3?> JointDrainDestinationResolver { get; set; }

        // MODEL_DOWN_ATTACK_DUMMY_L/R and MODEL_DRAGON_KICK_DUMMY must
        // be verified by a game-specific model ID bridge, not guessed.
        public Func<ClassicFxOwner, string, bool> JointTargetModelMatches { get; set; }

        // Native BMD::Animation at a chosen frame before bone transform.
        // Without this bridge the normal current-pose MonoGame skeleton is
        // used as an explicit fallback, not a fabricated animation frame.
        public Func<ClassicFxOwner, int, float, Vector3?> JointAnimatedBoneResolver { get; set; }

        // Native range-attack checks are gameplay/network functionality;
        // this event is NOT fired until the network bridge is connected.
        public event Action<ushort, Vector3, float, float, short> JointRangeAttackRequested;
        public Func<ClassicFxOwner, bool> JointOwnerIsLocalHero { get; set; }
        public Func<Vector3, bool> JointCastleNoAttackZone { get; set; }

        private static bool IsJointMoveFamilyD(int type) =>
            type == ClassicTextureIds.BitmapJointForce ||
            type == ClassicTextureIds.BitmapLight ||
            type == ClassicTextureIds.BitmapPiercing ||
            type == ClassicTextureIds.BitmapFlareForce ||
            type == ClassicTextureIds.BitmapFlash ||
            type == ClassicTextureIds.BitmapDrainLifeGhost ||
            type == ClassicTextureIds.BitmapForcePillar ||
            type == ClassicTextureIds.BitmapSwordEff ||
            type == ClassicTextureIds.BitmapGroundWind ||
            type == ClassicTextureIds.BitmapLava ||
            type == ClassicTextureIds.BitmapSmoke ||
            type == ClassicTextureIds.BitmapSpark + 1;

        private bool MoveJointFamilyD(ref ClassicJoint j)
        {
            float f = Clock.FrameFactor;
            if (f <= 0f) return true;

            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            bool excluded =
                (j.Type == ClassicTextureIds.BitmapJointForce && j.SubType == 0) ||
                j.Type == ClassicTextureIds.BitmapLight ||
                j.Type == ClassicTextureIds.BitmapPiercing ||
                j.Type == ClassicTextureIds.BitmapFlareForce ||
                (j.Type == ClassicTextureIds.BitmapFlash && j.SubType == 6);
            if (!excluded && j.Velocity != 0f)
                j.Position += ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), matrix) * f;

            bool alive = j.Type switch
            {
                ClassicTextureIds.BitmapJointForce => MoveForceJointD(ref j, f, ref matrix),
                ClassicTextureIds.BitmapLight => MoveLightJointD(ref j, f, ref matrix),
                ClassicTextureIds.BitmapPiercing => MovePiercingJointD(ref j, f, ref matrix),
                ClassicTextureIds.BitmapFlareForce => MoveFlareForceJointD(ref j, f, ref matrix),
                ClassicTextureIds.BitmapFlash => MoveFlashJointD(ref j, f, ref matrix),
                ClassicTextureIds.BitmapDrainLifeGhost => MoveDrainLifeJointD(ref j, f),
                ClassicTextureIds.BitmapForcePillar => MoveAttachmentJointD(ref j, "MODEL_DOWN_ATTACK_DUMMY", 0, f),
                ClassicTextureIds.BitmapSwordEff => MoveAttachmentJointD(ref j, null, 5, f),
                ClassicTextureIds.BitmapGroundWind => MoveAttachmentJointD(ref j, "MODEL_DRAGON_KICK_DUMMY", 2, f),
                ClassicTextureIds.BitmapLava => MoveLavaJointD(ref j, f),
                ClassicTextureIds.BitmapSmoke => MoveSmokeJointD(ref j),
                var type when type == ClassicTextureIds.BitmapSpark + 1 =>
                    MoveSparkPlusOneJointD(ref j, f),
                _ => false
            };

            if (!alive) return false;
            // Native CreateTail happens AFTER the type-specific branch.
            if (j.CreateTails)
                AppendJointTailMoveA(ref j, matrix);
            j.LifeTime -= f;
            return j.LifeTime >= 0f;
        }

        private bool MoveLightJointD(ref ClassicJoint j, float f,
            ref ClassicMatrix3x4 matrix)
        {
            int steps = j.SubType == 0 && j.LifeTime > 16f ? 10 :
                j.SubType == 1 && (j.Skill == 0 || j.LifeTime > 3f) ? 5 : 0;
            if (steps > 0)
            {
                matrix = ClassicMath.AngleMatrix(j.Angle);
                for (int n = 0; n < steps; n++)
                {
                    AppendJointTailMoveA(ref j, matrix);
                    // In the Main, -Velocity already contains one FPS
                    // factor here (the LIGHT branch does not multiply twice).
                    j.Position += ClassicMath.VectorRotate(
                        new Vector3(0f, -j.Velocity * f, 0f), matrix);
                    if (j.SubType == 1) j.Velocity += 0.5f * f;
                }
            }
            j.Light *= MathF.Pow(1f / 1.2f, f);
            return true;
        }

        private bool MovePiercingJointD(ref ClassicJoint j, float f,
            ref ClassicMatrix3x4 matrix)
        {
            if ((j.SubType == 0 || j.SubType == 1) && j.LifeTime > 9f)
            {
                matrix = ClassicMath.AngleMatrix(j.Angle);
                for (int n = 0; n < 30; n++)
                {
                    AppendJointTailMoveA(ref j, matrix);
                    // Native VectorAddScaled after p already contains f.
                    j.Position += ClassicMath.VectorRotate(
                        new Vector3(0f, 0f, -j.Velocity * f), matrix) * f;
                }
            }
            j.Velocity -= 2f * f;
            j.Light *= MathF.Pow(1f / 1.4f, f);
            return true;
        }

        private bool MoveFlareForceJointD(ref ClassicJoint j, float f,
            ref ClassicMatrix3x4 matrix)
        {
            if (j.SubType >= 5 && j.SubType <= 7)
            {
                if (!TryGetOwnerBoneWorldMatrix(j.Target, (int)j.MultiUse,
                        out Matrix bone))
                    return false;
                Vector3 direction = j.Direction;
                j.StartPosition = Vector3.Transform(new Vector3(0f, 20f, 0f), bone);
                j.Position = j.StartPosition;
                j.NumTails = 0;
                int count = Math.Min(j.MaxTails, Math.Max(0, (int)j.Weapon));
                j.MaxTails = count;
                j.TargetPosition.Y = j.TargetPosition.Z;
                // The Main uses target bone 0 as the quad orientation, not
                // the current joint Angle. TransformNormal rotates offsets.
                if (!TryGetOwnerBoneWorldMatrix(j.Target, 0, out Matrix rootBone))
                    return false;
                for (int n = 0; n < count; n++)
                {
                    j.StartPosition += Vector3.TransformNormal(direction, bone) * f;
                    j.TargetPosition.Y += (j.SubType % 2 == 1 ? 40f : -40f);
                    Vector3 angle = new Vector3(j.TargetPosition.Y, 0f, 0f);
                    Vector3 offset = ClassicMath.VectorRotate(
                        new Vector3(0f, 0f, j.TargetPosition.X),
                        ClassicMath.AngleMatrix(angle));
                    j.Position = j.StartPosition + offset;
                    AppendJointBoneTailD(ref j, rootBone);
                    j.TargetPosition.X -= 0.15f * f;
                    if (j.PKKey == -1) direction.Y -= 0.1f * f;
                    if (n % 2 == 0)
                        CreateSprite(ClassicTextureIds.BitmapFlare,
                            j.StartPosition, j.Light.X / 2f, j.Light);
                }
                j.MaxTails++;
                if (j.LifeTime < 7f)
                    j.Light *= MathF.Pow(1f / 1.5f, f);
                return true;
            }

            if (j.NumTails < j.MaxTails - 1)
            {
                if (j.SubType == 0)
                {
                    for (int n = 1; n < j.MultiUse; n++)
                    {
                        matrix = ClassicMath.AngleMatrix(j.Angle);
                        AppendJointTailMoveA(ref j, matrix);
                        j.StartPosition += ClassicMath.VectorRotate(
                            new Vector3(0f, j.Velocity * f, 0f), matrix);
                        j.Position = j.StartPosition;
                        j.Velocity -= 2f * f;
                    }
                    j.MultiUse += 2f * f;
                }
                else if ((j.SubType >= 1 && j.SubType <= 4) ||
                         (j.SubType >= 11 && j.SubType <= 13))
                {
                    if (j.Weapon <= 0f)
                    {
                        for (int n = 1; n < j.MultiUse; n++)
                        {
                            matrix = ClassicMath.AngleMatrix(j.Angle);
                            AppendJointTailMoveA(ref j, matrix);
                            j.StartPosition += ClassicMath.VectorRotate(
                                new Vector3(0f, j.Velocity * f, 0f), matrix);
                            // Preserve native (SubType>=11 || SubType<=13)
                            // condition: all these subtypes decrement Y.
                            j.TargetPosition.Y -= 20f * f;
                            Vector3 a = new Vector3(0f, j.TargetPosition.Y, j.Angle.Z);
                            Vector3 offset = ClassicMath.VectorRotate(
                                new Vector3(0f, 0f, j.TargetPosition.X),
                                ClassicMath.AngleMatrix(a));
                            j.Position = (j.SubType == 3 || j.SubType == 4)
                                ? j.StartPosition - offset
                                : j.StartPosition + offset;
                            j.Velocity -= 2f * f;
                            j.TargetPosition.X -= 2.5f * f;
                        }
                        j.MultiUse += 2f * f;
                    }
                    else j.Weapon -= f;
                }
            }
            if (((j.SubType >= 0 && j.SubType <= 4) ||
                 (j.SubType >= 11 && j.SubType <= 13)) && j.LifeTime < 10f)
                j.Light *= MathF.Pow(1f / 1.3f, f);
            return true;
        }

        private static void AppendJointBoneTailD(ref ClassicJoint j,
            in Matrix bone)
        {
            if (j.Tails == null || j.MaxTails < 1) return;
            j.NumTails = Math.Min(j.NumTails + 1,
                Math.Min(j.MaxTails - 1, ClassicJoint.MaxTailSegments - 1));
            for (int t = j.NumTails - 1; t >= 0; t--)
            {
                int src = t * 4, dst = src + 4;
                for (int k = 0; k < 4; k++) j.Tails[dst + k] = j.Tails[src + k];
            }
            float h = j.Scale * 0.5f;
            j.Tails[0] = j.Position + Vector3.TransformNormal(new Vector3(-h, 0f, 0f), bone);
            j.Tails[1] = j.Position + Vector3.TransformNormal(new Vector3(h, 0f, 0f), bone);
            j.Tails[2] = j.Position + Vector3.TransformNormal(new Vector3(0f, 0f, -h), bone);
            j.Tails[3] = j.Position + Vector3.TransformNormal(new Vector3(0f, 0f, h), bone);
        }

        private bool MoveFlashJointD(ref ClassicJoint j, float f,
            ref ClassicMatrix3x4 matrix)
        {
            if (j.SubType <= 3 || j.SubType == 5)
            {
                if (j.SubType == 2) j.Angle.Z += (R(10) + 10f) * f;
                j.Velocity += 10f * f;
                if (j.CreateTails)
                {
                    matrix = ClassicMath.AngleMatrix(j.Angle);
                    AppendJointTailMoveA(ref j, matrix);
                }
                j.Velocity += 10f * f;
                matrix = ClassicMath.AngleMatrix(j.HeadAngle);
                Vector3 offset = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity * f, 0f), matrix);
                j.Position += offset * f;

                float height = RequestTerrainHeight(j.Position.X, j.Position.Y)
                    - j.MultiUse;
                if (j.Position.Z < height)
                {
                    j.Position.Z = height;
                    if (j.CreateTails && j.PKKey != 1)
                    {
                        if (j.SubType < 2)
                        {
                            if (j.Target.HasOwner)
                                JointMoveEffectRequested?.Invoke(
                                    "MODEL_SKIN_SHELL", j.Position, j.Angle,
                                    j.Light, j.SubType);
                        }
                        else
                        {
                            Vector3 color = j.SubType == 5
                                ? new Vector3(1f, 0.8f, 0.3f)
                                : new Vector3(0.3f, 0.8f, 1f);
                            JointMoveEffectRequested?.Invoke(
                                "BITMAP_SHOCK_WAVE", j.Position, j.Angle, color, 4);
                        }
                    }
                    if (j.SubType == 5)
                    {
                        j.PKKey = 1;
                        j.Velocity = -20f;
                    }
                    else j.CreateTails = false;
                    j.Light *= MathF.Pow(1f / 1.2f, f);
                }
                if (j.SubType < 2)
                    CreateSprite(ClassicTextureIds.BitmapFlare,
                        j.Position, 1f, j.Light, R(360));
                else if (j.SubType == 5)
                    CreateSprite(ClassicTextureIds.BitmapFlare,
                        j.Position, 2.5f, j.Light, R(360));
                else
                    CreateSprite(ClassicTextureIds.BitmapShiny + 2,
                        j.Position, 1.6f, new Vector3(0f, 1f, 1f), R(360));
            }
            else if (j.SubType == 4)
            {
                if (!TryGetOwnerPosition(j.Target, out Vector3 position))
                    return false;
                j.Position = position;
                if (j.LifeTime < 15f)
                    j.Light *= MathF.Pow(1f / 1.3f, f);
            }
            else if (j.SubType == 6)
            {
                int fr = R(5) - 2;
                j.Direction.Y += fr * f;
                j.Direction.Z -= 10f * f;
                matrix = ClassicMath.AngleMatrix(j.Angle);
                j.Position += ClassicMath.VectorRotate(j.Direction, matrix) * f;
                if (j.Weapon - j.LifeTime < 10f)
                {
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapExplotion + 1,
                        j.StartPosition, j.Angle, j.Light, 0, 3f);
                    j.Scale = 100f;
                }
                else j.Scale = 40f;

                float height = RequestTerrainHeight(j.Position.X, j.Position.Y);
                if (j.Position.Z <= height)
                {
                    Vector3 smokePos = j.Position;
                    CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                        j.Position, j.Position, j.Angle, subType: 4, scale: 60f);
                    if (j.Direction.Z <= 0f)
                        j.Direction.Z = MathF.Sin(j.Velocity) * 20f;
                    j.Position.Z = height + 5f;
                    smokePos.Z = j.Position.Z + 30f;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapAdvSmoke + 1,
                        smokePos, j.Angle, j.Light, 2, 3f);
                }
                // Native is CreateTailAxis(Matrix, axis:1), not ordinary quad.
                AppendJointTailAxisY(ref j);
                if (j.LifeTime < j.Weapon / 2f)
                    j.Light *= MathF.Pow(1f / 1.3f, f);
            }
            return true;
        }

        private bool MoveDrainLifeJointD(ref ClassicJoint j, float f)
        {
            if (j.SubType != 0) return true;
            Vector3? ownerOfTarget = JointDrainDestinationResolver?.Invoke(j.Target);
            if (!ownerOfTarget.HasValue) return false;
            Vector3 target = ownerOfTarget.Value;
            target.Z = j.TargetPosition.Z;
            MoveJointHummingA(ref j.Angle, j.Position, target, j.Velocity, f);
            j.Velocity += 2f * f;
            if (j.LifeTime < 10f)
                j.Light *= MathF.Pow(1f / 1.2f, f);
            // The two CreateSprite calls in the native client are commented out.
            return true;
        }

        private bool MoveAttachmentJointD(ref ClassicJoint j, string model,
            int bone, float f)
        {
            if (!TryGetOwnerSnapshot(j.Target, out _)) return false;
            if (model != null &&
                !(JointTargetModelMatches?.Invoke(j.Target, model) ?? false))
                return false;
            float frame = 0f;
            if (JointAnimatedBoneResolver != null)
            {
                Vector3? animated = JointAnimatedBoneResolver(j.Target, bone, frame);
                if (!animated.HasValue) return false;
                j.Position = animated.Value;
            }
            else if (!TryGetOwnerBonePosition(j.Target, bone, out j.Position))
                return false;
            if (j.Type == ClassicTextureIds.BitmapGroundWind)
            {
                j.Position.Z += 20f * f;
                j.Light *= MathF.Pow(0.7f, f);
            }
            return true;
        }

        private bool MoveLavaJointD(ref ClassicJoint j, float f)
        {
            if (!TryGetOwnerSnapshot(j.Target, out _)) return false;
            int frameIndex = j.SubType >= 7 ? j.SubType - 7 : j.SubType;
            // Native animation frame: Target.AnimationFrame -
            // ((Velocity * 10 - TempFrame) * 0.1)
            float offsetFrames = -((j.Velocity * 10f - frameIndex) * 0.1f);
            int bone = j.SubType >= 7 ? 28 : 36;
            if (JointAnimatedBoneResolver != null)
            {
                Vector3? animated = JointAnimatedBoneResolver(
                    j.Target, bone, offsetFrames);
                if (!animated.HasValue) return false;
                j.Position = animated.Value;
            }
            else if (!TryGetOwnerBonePosition(j.Target, bone, out j.Position))
                return false;
            j.Light *= MathF.Pow(0.8f, f);
            return true;
        }

        private bool MoveForceJointD(ref ClassicJoint j, float f,
            ref ClassicMatrix3x4 matrix)
        {
            if (j.SubType == 0 || j.SubType == 8 || j.SubType == 10)
            {
                for (int n = 0; n < 8; n++)
                {
                    if (j.NumTails < j.MaxTails - 1)
                    {
                        Vector3 prior = j.Position;
                        matrix = ClassicMath.AngleMatrix(j.Direction);
                        j.Position = j.TargetPosition + ClassicMath.VectorRotate(
                            new Vector3(0f, -145f, 0f), matrix);
                        matrix = ClassicMath.AngleMatrix(j.Angle);
                        AppendJointTailMoveA(ref j, matrix);
                        j.Direction.Z -= 11f * f;
                        if (j.SubType != 8)
                        {
                            if (Random.FpsCheck(2, Clock))
                            {
                                if (j.SubType == 0)
                                    CreateParticle(ClassicTextureIds.BitmapFire,
                                        j.Position, j.Angle, j.Light);
                                else
                                    CreateParticleFpsChecked(ClassicTextureIds.BitmapFire,
                                        j.Position, j.Angle, j.Light);
                            }
                            CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                                prior, j.Position, j.Angle, 3,
                                scale: R(10) + 5f, pkKey: 5, skillIndex: 10);
                            CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                                prior, j.Position, j.Angle, 3,
                                scale: R(8) + 4f, pkKey: 5, skillIndex: 10);
                        }
                        else
                        {
                            CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                                j.Position, j.Position, j.Angle, 3,
                                scale: R(10) + 5f, pkKey: 5, skillIndex: 10);
                            CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                                j.Position, j.Position, j.Angle, 3,
                                scale: R(8) + 4f, pkKey: 5, skillIndex: 10);
                        }
                    }
                    if (j.SubType != 8 && j.LifeTime > 18f && n % 5 == 0 &&
                        JointOwnerIsLocalHero?.Invoke(j.Target) == true)
                    {
                        if (JointCastleNoAttackZone?.Invoke(j.Position) == true)
                        {
                            j.Velocity = 0f;
                            j.LifeTime *= MathF.Pow(1f / 5f, f);
                            break;
                        }
                        JointRangeAttackRequested?.Invoke(
                            j.Skill, j.Position, j.SubType == 10 ? 225f : 150f,
                            j.Weapon, j.PKKey);
                    }
                }
                if (j.SubType == 10 && j.SkillSerialNum != 0)
                {
                    if (j.LifeTime < 10f) j.Light /= 1.5f;
                }
                else
                {
                    float lum = j.LifeTime < 10f
                        ? j.Light.X / 1.5f : j.LifeTime / 30f;
                    j.Light = new Vector3(lum);
                }
            }
            else if (j.SubType == 1)
            {
                if (!TryGetOwnerPosition(j.Target, out Vector3 target))
                    return false;
                j.Position = target;
                j.Scale -= f;
                j.Position.Z += 100f * f;
                if (j.LifeTime < 15f)
                    j.Light *= MathF.Pow(1f / 1.3f, f);
            }
            else if (j.SubType >= 2 && j.SubType <= 7 || j.SubType == 20)
            {
                j.Velocity += f * j.Direction.Z;
                j.Direction.Z += j.LifeTime < (j.SubType == 7 || j.SubType == 20 ? 20f : 15f)
                    ? 0.5f * f : j.Direction.X * f;
                if (j.SubType != 4 && j.NumTails >= j.MaxTails - 1)
                    j.CreateTails = false;
                if (j.LifeTime < j.MultiUse)
                    j.Light *= MathF.Pow(1f / 1.3f, f);
                if (j.SubType == 2 && (int)j.LifeTime % 5 == 0)
                    JointMoveEffectRequested?.Invoke(
                        "MODEL_SKILL_INFERNO", j.StartPosition,
                        j.HeadAngle, j.Light, 6);
                if (j.SubType == 4)
                {
                    CreateJointFpsChecked(ClassicTextureIds.BitmapFlareBlue,
                        j.Position, j.Position, j.Angle, 6, scale: 30f);
                    Vector3 light = new Vector3(0.1f, 0.6f, 1f);
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        j.Position, 1.6f, light);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        j.Position, 1.5f, light,
                        (float)Clock.WorldTimeMilliseconds * 0.1f);
                }
                if (j.SubType == 7 || j.SubType == 20)
                    j.Position.Z = RequestTerrainHeight(j.Position.X,
                        j.Position.Y) + 3f;
            }
            return true;
        }

        // Native BITMAP_SMOKE follows MAYASTONE 1/2/3 and MODEL_FIRE only.
        // When the model identity bridge is absent, keep the common movement.
        private bool MoveSmokeJointD(ref ClassicJoint j)
        {
            if (JointTargetModelMatches == null ||
                !TryGetOwnerPosition(j.Target, out Vector3 targetPosition))
                return true;
            if (JointTargetModelMatches(j.Target, "MODEL_MAYASTONE1") ||
                JointTargetModelMatches(j.Target, "MODEL_MAYASTONE2") ||
                JointTargetModelMatches(j.Target, "MODEL_MAYASTONE3") ||
                JointTargetModelMatches(j.Target, "MODEL_FIRE"))
                j.Position = targetPosition;
            return true;
        }

        // Native BITMAP_SPARK + 1, including its unusual if/if/else layout.
        private static bool MoveSparkPlusOneJointD(ref ClassicJoint j, float f)
        {
            if (j.SubType == 0)
            {
                j.Direction.Z += 5f * f;
                j.Position.X = j.TargetPosition.X;
                j.Position.Y = j.TargetPosition.Y;
                j.Position.Z += j.Direction.Z * f;
            }
            if (j.SubType == 1)
            {
                j.Velocity += f * 0.1f;
                float light = j.Light.X * MathF.Pow(1f / 1.1f, f);
                j.Light = new Vector3(light);
            }
            else if (j.LifeTime < 5f)
            {
                j.Light *= MathF.Pow(1f / 1.3f, f);
            }
            return true;
        }
    }
}
