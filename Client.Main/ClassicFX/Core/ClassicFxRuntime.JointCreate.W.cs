using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain ZzzEffectJoint.cpp CreateJoint(), lines 2285-2421.
        // This method initializes JOINT state only. Movement and rendering
        // are deliberately not approximated here.
        private bool InitializeJointCreateW(
            ref ClassicJoint j,
            float sourceScale)
        {
            if (j.Type != ClassicTextureIds.BitmapJointForce)
                return false;

            // All native JOINT_FORCE branches start with the common initial
            // quad. NumTails and CreateTails changes below occur afterwards.
            j.InitializeFirstTail();

            switch (j.SubType)
            {
                case 0:
                case 10:
                    j.LifeTime = 20f;
                    j.CreateTails = false;
                    j.Scale = sourceScale;
                    j.Velocity = 0f;
                    j.MaxTails = 18;
                    j.NumTails = -1;
                    j.TargetPosition = j.Position;
                    j.Angle.Z += 30f;
                    j.Direction = j.Angle;
                    j.Position += ClassicMath.VectorRotate(
                        new Vector3(0f, -180f, 0f),
                        ClassicMath.AngleMatrix(j.Angle));
                    return true;

                case 1:
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    j.LifeTime = 30f;
                    j.Velocity = 0f;
                    j.OnlyOneRender = 1;
                    j.Weapon = 0f;
                    j.Light = Vector3.One;
                    j.Direction = Vector3.Zero;
                    // Native dereferences Target->Position and Target->Angle.
                    // Do not populate either with fabricated player data.
                    if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                        return false;
                    j.Position = owner.Position;
                    j.Angle = owner.Angle;
                    return true;

                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.Velocity = 8f;
                    j.Direction.X = 3.5f;
                    j.Direction.Z = 1f;
                    j.MaxTails = 12;
                    j.NumTails = -1;
                    j.MultiUse = 5f;

                    switch (j.SubType)
                    {
                        case 3:
                            j.TexType = ClassicTextureIds.BitmapFlare;
                            j.MultiUse = 10f;
                            j.Velocity = 10f;
                            j.Direction.Z = 2f;
                            j.ReverseUv = 0;
                            j.Direction.X = 10.5f;
                            break;
                        case 4:
                            j.LifeTime = 20f;
                            j.MaxTails = 13;
                            j.TexType = ClassicTextureIds.BitmapHole;
                            j.Direction.X = 15.5f;
                            j.Direction.Z = 3f;
                            j.MultiUse = 10f;
                            break;
                        case 5:
                            j.TexType = ClassicTextureIds.BitmapLava;
                            break;
                        case 6:
                            j.TexType = ClassicTextureIds.BitmapLava;
                            j.Velocity = 20f;
                            j.LifeTime = 16f;
                            break;
                        default: // Native subtype 2.
                            j.Position.Z += 10f;
                            j.TexType = ClassicTextureIds.BitmapInferno;
                            j.LifeTime = 15f;
                            j.MultiUse = 5f;
                            j.Direction.X = 5f;
                            j.Direction.Z = 5f;
                            break;
                    }
                    j.HeadAngle = new Vector3(0f, 0f, j.Angle.Z);
                    j.StartPosition = j.Position;
                    return true;

                case 7:
                case 20:
                    j.Scale = sourceScale;
                    j.Velocity = 10f;
                    j.Direction.X = 3.5f;
                    j.Direction.Z = 1f;
                    j.MaxTails = j.SubType == 7 ? 13 : 15;
                    j.NumTails = -1;
                    j.MultiUse = 5f;
                    j.TexType = ClassicTextureIds.BitmapInferno;
                    j.LifeTime = 20f;
                    return true;

                case 8:
                    j.LifeTime = 20f;
                    j.CreateTails = false;
                    j.Scale = sourceScale;
                    j.Velocity = 0f;
                    j.MaxTails = 18;
                    j.NumTails = -1;
                    j.TargetPosition = j.Position;
                    j.Angle.Z += 30f;
                    j.Direction = j.Angle;
                    j.Position += ClassicMath.VectorRotate(
                        new Vector3(0f, -180f, 0f),
                        ClassicMath.AngleMatrix(j.Angle)) *
                        (Clock.FrameFactor > 0f ? Clock.FrameFactor : 1f);
                    return true;
            }

            // Native has no initialized LifeTime/MaxTails for other subtypes.
            // Refuse inert entries rather than consuming the finite joint pool.
            return false;
        }
    }
}
