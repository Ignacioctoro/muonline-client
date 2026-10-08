using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain: ZzzEffectJoint.cpp, CreateJoint() lines 2422-2578.
        // Only the creation state is ported here; MoveJoint/RenderJoints
        // will consume these fields when those systems are implemented.
        private bool InitializeJointCreateX(
            ref ClassicJoint j,
            float sourceScale,
            in Vector3 sourceAngle)
        {
            switch (j.Type)
            {
                case ClassicTextureIds.BitmapLight:
                    return InitializeLightJointX(ref j, sourceScale);
                case ClassicTextureIds.BitmapPiercing:
                    return InitializePiercingJointX(ref j, sourceScale, sourceAngle);
                case ClassicTextureIds.BitmapFlareForce:
                    return InitializeFlareForceJointX(ref j, sourceScale);
                default:
                    return false;
            }
        }

        private bool InitializeLightJointX(ref ClassicJoint j, float sourceScale)
        {
            // All LIGHT joints receive the original initial quad before
            // m_bCreateTails is switched off in this family.
            j.InitializeFirstTail();
            j.CreateTails = false;

            switch (j.SubType)
            {
                case 0:
                    j.LifeTime = 20f;
                    j.MaxTails = 30;
                    j.Scale = sourceScale;
                    j.Velocity = R(10) + 10f;
                    j.Angle = new Vector3(30f - R(40), 0f, R(360));
                    j.Direction = Vector3.Zero;
                    j.Light = new Vector3(0.1f, 0.5f, 1f);
                    j.StartPosition = j.Position;
                    return true;

                case 1:
                    j.LifeTime = 10f;
                    j.MaxTails = 20;
                    j.Scale = sourceScale;
                    j.Skill = (ushort)R(5);
                    j.Velocity = R(5) + 1f;
                    j.Angle = new Vector3(30f, 0f, R(360));
                    j.Direction = Vector3.Zero;
                    j.Light = new Vector3(1f, 0.8f, 0.6f);
                    j.StartPosition = j.Position;
                    return true;

                default:
                    // Native sets no lifetime for unlisted LIGHT subtypes.
                    // Do not retain an immortal, invisible pooled joint.
                    return false;
            }
        }

        private bool InitializePiercingJointX(
            ref ClassicJoint j,
            float sourceScale,
            in Vector3 sourceAngle)
        {
            j.InitializeFirstTail();
            j.CreateTails = false;

            if (j.SubType != 0 && j.SubType != 1)
                return false;

            j.LifeTime = 10f;
            j.TileMapping = j.SubType == 1;
            j.MaxTails = 30;
            j.Scale = sourceScale;
            j.Velocity = 20f;
            // Native uses the *incoming Angle[2]*, not a calculated heading.
            j.Angle = new Vector3(-90f, 0f, sourceAngle.Z);
            j.Direction = Vector3.Zero;
            j.Light = Random.FpsCheck(2, Clock)
                ? new Vector3(1f, 0.8f, 0.6f)
                : Vector3.One;
            j.StartPosition = j.Position;
            return true;
        }

        private bool InitializeFlareForceJointX(
            ref ClassicJoint j,
            float sourceScale)
        {
            // Original CreateJoint() suppresses the first tail for 5, 6, 7.
            // For other FlareForce subtypes the initial quad is created before
            // the initializer mutates Position/Scale.
            if (j.SubType < 5 || j.SubType > 7)
                j.InitializeFirstTail();

            j.LifeTime = 20f;
            j.CreateTails = false;
            j.MaxTails = 30;
            j.Scale = sourceScale;
            j.MultiUse = 1f;
            j.Velocity = -3f;
            // Unlike most initializers, this happens at CreateJoint time.
            float frameFactor = Clock.FrameFactor > 0f ? Clock.FrameFactor : 1f;
            j.Position.Z += 150f * frameFactor;
            j.Direction = Vector3.Zero;
            j.TargetPosition = Vector3.Zero;
            j.Light = new Vector3(1f, 0.8f, 1f);
            j.StartPosition = j.Position;
            j.Weapon = 0f;

            if ((j.SubType >= 1 && j.SubType <= 4) ||
                (j.SubType >= 11 && j.SubType <= 13))
            {
                j.TexType = ClassicTextureIds.BitmapJointThunder;
                j.Velocity = -3f;
                j.TargetPosition = new Vector3(80f, 180f, 0f);
                j.Weapon = R(3) + 2f;

                if (j.SubType == 2 || j.SubType == 4 ||
                    (j.SubType >= 11 && j.SubType <= 13))
                {
                    switch (j.SubType)
                    {
                        case 11: j.Light = new Vector3(0.7f, 1f, 0.7f); break;
                        case 12: j.Light = new Vector3(1f, 0.6f, 0.6f); break;
                        case 13: j.Light = new Vector3(0.7f, 0.7f, 1f); break;
                    }
                    j.TargetPosition.Y = -180f;
                }

                if (j.SubType == 3 || j.SubType == 4)
                    j.Weapon = 0f;

                j.LifeTime += j.Weapon;
                // The original uses TargetPosition AS EULER ANGLES here,
                // then rotates (0,0,TargetPosition[0]) to form initial offset.
                Vector3 rotated = ClassicMath.VectorRotate(
                    new Vector3(0f, 0f, j.TargetPosition.X),
                    ClassicMath.AngleMatrix(j.TargetPosition));
                j.Position = j.StartPosition + rotated;
            }
            else if (j.SubType != 0)
            {
                ushort incomingSkill = j.Skill;
                j.LifeTime = 15f;
                j.Skill = 0;
                j.MaxTails = 1;
                j.Weapon = 30f;
                // Main stores the original SkillIndex, not the zeroed Skill.
                j.MultiUse = incomingSkill;
                j.Velocity = 0f;
                j.TargetPosition.X = 30f;
                j.TargetPosition.Z = j.SubType * 90f;
                j.Direction = new Vector3(0f, -4f, 0f);
                j.StartPosition = j.Position;
                if (j.PKKey != -1)
                    j.Direction.Y = 0f;
            }

            if (j.SubType >= 5 && j.SubType <= 7)
                j.TexType = ClassicTextureIds.BitmapFirecracker;
            else if (j.SubType == 0)
                j.TexType = ClassicTextureIds.BitmapJointThunder;

            return true;
        }
    }
}
