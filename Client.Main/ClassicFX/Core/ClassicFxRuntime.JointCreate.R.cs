using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain ZzzEffectJoint.cpp / CreateJoint(), lines 944-1023, 1072-1100.
        // Laser, Spark, Smoke, Laser+1 and Blur+1.
        // MODEL_FENRIR_SKILL_THUNDER (1024-1071) follows with Thunder in S.
        // MoveJoints()/RenderJoints() are not enabled yet.
        private bool InitializeJointCreateR(ref ClassicJoint j, float sourceScale)
        {
            switch (j.Type)
            {
                case ClassicTextureIds.BitmapJointLaser:
                    j.InitializeFirstTail();
                    j.TileMapping = true;
                    j.Velocity = 70f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 6;
                    return true;

                case ClassicTextureIds.BitmapJointSpark:
                    j.InitializeFirstTail();
                    return InitializeJointSparkR(ref j, sourceScale);

                case ClassicTextureIds.BitmapSmoke:
                    j.InitializeFirstTail();
                    return InitializeJointSmokeR(ref j, sourceScale);

                case ClassicTextureIds.BitmapJointLaser + 1:
                case ClassicTextureIds.BitmapBlur + 1:
                    j.InitializeFirstTail();
                    return InitializeJointLaserBlurR(ref j);

                default:
                    return false;
            }
        }

        private bool InitializeJointSparkR(ref ClassicJoint j, float sourceScale)
        {
            switch (j.SubType)
            {
                case 0:
                case 3:
                    j.Scale = 2f;
                    j.Velocity = R(20) + 6f;
                    j.LifeTime = R(8) + 8f;
                    j.MaxTails = 2;
                    j.Light = Vector3.One;
                    return true;

                case 1:
                    j.Scale = 2f;
                    j.Velocity = R(20) + 16f;
                    j.LifeTime = R(4) + 4f;
                    j.MaxTails = 2;
                    j.Light = Vector3.One;
                    return true;

                case 2:
                    j.Scale = 4f;
                    j.Velocity = 30f;
                    j.LifeTime = R(4) + 4f;
                    j.MaxTails = 2;
                    j.Light = new Vector3(0.3f, 0.3f, 1f);
                    return true;

                case 4:
                    j.Scale = sourceScale * 2f + 4f;
                    j.Velocity = R(5) + 4f;
                    j.LifeTime = R(10) + 8f;
                    j.MaxTails = 4;
                    j.Light = new Vector3(1f, 0.2f, 0.2f);
                    return true;

                case 5:
                    j.Scale = 2f;
                    j.Velocity = R(20) + 6f;
                    j.LifeTime = R(8) + 8f;
                    j.MaxTails = 2;
                    j.Light = new Vector3(0.7f, 0.1f, 0.1f);
                    return true;

                default:
                    // Native code has no initialization for other subtypes.
                    // Do not retain an invisible/uninitialized pool slot.
                    return false;
            }
        }

        private static bool InitializeJointSmokeR(ref ClassicJoint j, float sourceScale)
        {
            j.Scale = sourceScale;
            j.Light = Vector3.One;
            j.Velocity = 0f;

            switch (j.SubType)
            {
                case 0:
                    j.MaxTails = 20;
                    j.LifeTime = 20f;
                    j.TexType = ClassicTextureIds.BitmapFlare;
                    return true;

                case 1:
                    j.MaxTails = 15;
                    j.LifeTime = 15f;
                    return true;

                case 2:
                    j.MaxTails = 25;
                    j.LifeTime = 22f;
                    j.TexType = ClassicTextureIds.BitmapFlare;
                    j.Light = new Vector3(0.1f, 0.3f, 1f);
                    return true;

                default:
                    return false;
            }
        }

        private static bool InitializeJointLaserBlurR(ref ClassicJoint j)
        {
            // Native fall-through: BITMAP_JOINT_LASER+1 enables tiling,
            // then shares this exact initialization with BITMAP_BLUR+1.
            if (j.Type == ClassicTextureIds.BitmapJointLaser + 1)
                j.TileMapping = true;

            j.Scale = 60f;
            j.Velocity = 40f;
            j.MaxTails = 50;
            j.LifeTime = 2f;

            if (j.SubType == 0)
            {
                j.Light = Vector3.One;
            }
            else if (j.SubType == 3)
            {
                j.LifeTime = 20f;
                j.Velocity = 0f;
                j.Light = Vector3.One;
            }
            else if (j.Type == ClassicTextureIds.BitmapJointLaser + 1)
            {
                j.Light = new Vector3(0.35f, 0.1f, 1f);
            }
            else
            {
                j.Light = new Vector3(0f, 0.3f, 1f);
            }
            return true;
        }
    }
}
