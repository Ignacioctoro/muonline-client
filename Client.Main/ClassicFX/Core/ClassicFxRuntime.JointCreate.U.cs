using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain ZzzEffectJoint.cpp: CreateJoint, BITMAP_FLARE/FLARE_BLUE
        // (native lines 1677-2068). Creation only; MoveJoints and RenderJoints
        // remain separate. All mutations use classic coordinates and frame factor.
        private bool InitializeJointCreateU(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale)
        {
            if (j.Type != ClassicTextureIds.BitmapFlare &&
                j.Type != ClassicTextureIds.BitmapFlareBlue)
                return false;

            // Both families use the default starting quad. Subsequent
            // CreateTails=false does not erase this original first tail.
            j.InitializeFirstTail();
            j.LifeTime = 100f;
            j.MaxTails = 20;
            j.Scale = 10f;

            switch (j.SubType)
            {
                case 0:
                case 18:
                {
                    if (j.Type == ClassicTextureIds.BitmapFlare && j.SubType == 18)
                        j.RenderType = 4; // RENDER_TYPE_ALPHA_BLEND_OTHER
                    j.Scale = sourceScale;
                    j.Direction.Z = R(150) / 100f;
                    j.Direction.Y = R(500) - 250f;
                    j.Velocity = 40f;
                    if (j.Scale > 10f)
                    {
                        j.LifeTime = 50f;
                        j.Direction.Z = (R(250) + 200f) / 100f;
                    }
                    // The Main dereferences Target->Light without a null check.
                    if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                        return false;
                    j.Light = owner.Light;
                    break;
                }

                case 20:
                {
                    // Flare joint uses the actual owner model's bone 33.
                    // Never substitute a guessed attachment coordinate.
                    if (!TryGetOwnerBonePosition(j.Target, 33, out Vector3 bone))
                        return false;
                    j.LifeTime = 30f;
                    j.MaxTails = 10;
                    j.Scale = sourceScale;
                    j.TexType = ClassicTextureIds.BitmapFirecracker;
                    j.Light = new Vector3(0.8f, 0.3f, 1f);
                    j.Position = bone;
                    break;
                }

                case 10:
                {
                    j.Scale = sourceScale;
                    j.Direction.Z = R(150) / 100f;
                    j.Direction.Y = R(500) - 250f;
                    // Original nested 'if (SubType==25)' is unreachable here.
                    j.Velocity = 40f;
                    if (j.Scale > 10f)
                    {
                        j.LifeTime = 50f;
                        j.Direction.Z = (R(250) + 200f) / 100f;
                    }
                    if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                        return false;
                    j.Light = owner.Light;
                    break;
                }

                case 14:
                case 15:
                    j.RenderType = 4; // ALPHA_BLEND_OTHER
                    j.Scale = sourceScale;
                    j.TexType = ClassicTextureIds.BitmapJointSpirit;
                    j.Direction.Z = R(150) / 100f;
                    j.Direction.Y = R(500) - 250f;
                    j.Velocity = 30f;
                    j.Light = new Vector3(0.2f, 0.4f, 0.8f);
                    if (j.Scale > 10f)
                        j.Direction.Z = (R(250) + 150f) / 100f;
                    j.LifeTime = 20f;
                    j.Position.Z += R(25);
                    break;

                case 16:
                    j.LifeTime = 60f;
                    j.CreateTails = false;
                    j.Light = new Vector3(0.1f);
                    j.TargetPosition = sourceTargetPosition;
                    j.Scale = sourceScale;
                    // Native creates MaxTails quads immediately; the helper
                    // clips NumTails at MaxTails-1, as original CreateTail().
                    for (int n = 0; n < j.MaxTails; n++)
                    {
                        j.Position.Z += 50f;
                        AppendJointTailQ(ref j);
                    }
                    break;

                case 17:
                    j.Position.X += R(500) - 500f;
                    j.Position.Y += R(500) - 500f;
                    j.Direction = Vector3.Zero;
                    j.Velocity = (R(200) + 10f) / 25f;
                    j.Scale = sourceScale;
                    j.LifeTime = 20f + R(10);
                    j.Light = new Vector3(0f, 0f, 1f);
                    break;

                case 2:
                case 24:
                case 50:
                case 51:
                    if (j.SubType == 24)
                        j.TexType = ClassicTextureIds.BitmapFlareRed;
                    else if (j.SubType == 50 || j.SubType == 51)
                        j.TexType = ClassicTextureIds.BitmapFlareBlue;
                    j.Direction.Z = R(20) + 35f;
                    j.Scale = sourceScale;
                    j.LifeTime = 25f + R(50);
                    j.Light = Vector3.One;
                    break;

                // These three cases are guarded by GUILD_WAR_EVENT in Main.
                // Preserve the native branches for builds that emit these
                // subtypes, without depending on a game-specific build flag.
                case 21:
                    j.Direction.Z = R(20) + 35f;
                    j.Scale = sourceScale;
                    j.LifeTime = 40f + R(10);
                    j.Light = new Vector3(
                        0.7f, 0.5f + R(127) / 255f,
                        0.5f + R(127) / 255f);
                    break;

                case 22:
                    if (!TryGetOwnerPosition(j.Target, out Vector3 guildOwnerPosition))
                        return false;
                    j.StartPosition = guildOwnerPosition;
                    j.Direction.Z = R(20) + 35f;
                    j.Scale = sourceScale;
                    j.LifeTime = 40f + R(10);
                    j.Light = new Vector3(
                        0.5f + R(127) / 255f,
                        0.2f + R(204) / 255f,
                        0.2f + R(204) / 255f);
                    break;

                case 40:
                    j.Scale = sourceScale;
                    j.LifeTime = 50f;
                    j.MaxTails = 50;
                    j.Light = Vector3.One;
                    j.Direction = new Vector3(
                        -2f * MathF.Sin(-MathHelper.ToRadians(j.Angle.Z)),
                        -2f * MathF.Cos(-MathHelper.ToRadians(j.Angle.Z)), 0f);
                    break;

                case 41:
                    j.Scale = sourceScale;
                    j.LifeTime = 40f;
                    j.MaxTails = 50;
                    float redBias = R(300) / 1000f;
                    float greenBias = R(300) / 1000f;
                    j.Light = new Vector3(1f - redBias, 1f - greenBias, 1f);
                    j.Direction.Z = R(5) + 5f;
                    break;

                case 42:
                    j.Direction.Y = -15f;
                    Vector3 movement = ClassicMath.VectorRotate(
                        j.Direction, ClassicMath.AngleMatrix(j.Angle));
                    j.Position += movement * Clock.FrameFactor;
                    j.StartPosition = j.Position;
                    j.Direction.Y = -50f;
                    j.CreateTails = false;
                    j.Scale = sourceScale;
                    j.Light = Vector3.One;
                    break;

                case 19:
                    j.Direction.Z = -(R(20) + 35f);
                    j.Scale = sourceScale;
                    j.LifeTime = 25f + R(50);
                    j.Light = Vector3.One;
                    break;

                case 3:
                {
                    j.Velocity = 50f;
                    j.Scale = sourceScale;
                    j.LifeTime = 5f;
                    j.MaxTails = 10;
                    j.Angle = new Vector3(0f, 45f, -90f);
                    j.Position += ClassicMath.VectorRotate(
                        new Vector3(0f, 100f, 0f),
                        ClassicMath.AngleMatrix(j.Angle)) * Clock.FrameFactor;
                    // CreateSprite(SHINY+1) is a genuine original secondary
                    // effect. The sprite pool owns its lifetime and rendering.
                    CreateSprite(
                        ClassicTextureIds.BitmapShiny + 1,
                        j.Position, (R(8) + 8f) * 0.3f,
                        j.Light, R(360));
                    j.Light = Vector3.One;
                    j.Angle *= -1f;
                    break;
                }

                case 4:
                case 6:
                case 12:
                    j.Scale = sourceScale;
                    if (j.SubType == 12)
                    {
                        j.LifeTime = 70f;
                        j.MaxTails = 50;
                        j.Light = new Vector3(0.1f, 0.1f, 1f);
                        j.Direction.X = R(360);
                        j.Direction.Y = -4f * MathF.Sin(-MathHelper.ToRadians(j.Angle.Z));
                        j.Direction.Z = -4f * MathF.Cos(-MathHelper.ToRadians(j.Angle.Z));
                    }
                    else
                    {
                        if (j.SubType == 6)
                        {
                            j.LifeTime = j.Type == ClassicTextureIds.BitmapFlareBlue ? 15f : 20f;
                            j.MaxTails = j.Type == ClassicTextureIds.BitmapFlareBlue ? 15 : 30;
                            j.Light = Vector3.One;
                        }
                        else
                        {
                            j.LifeTime = 110f;
                            j.MaxTails = 200;
                            if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                                return false;
                            j.Light = owner.Light;
                        }
                        j.Direction.X = R(360);
                        j.Direction.Y = -2f * MathF.Sin(-MathHelper.ToRadians(j.Angle.Z));
                        j.Direction.Z = -2f * MathF.Cos(-MathHelper.ToRadians(j.Angle.Z));
                    }
                    break;

                case 5:
                    j.LifeTime = 2f;
                    j.MaxTails = 3;
                    j.Direction.Z = -(R(3) + 40f);
                    j.Light = Vector3.One;
                    break;

                case 7:
                    j.Light = new Vector3(0.2f, 0.2f, 1f);
                    j.MultiUse = R(10);
                    j.LifeTime = 30f + j.MultiUse;
                    j.MaxTails = 15;
                    j.Direction.X = R(3000);
                    j.Scale = 30f;
                    break;

                case 8:
                    j.Angle = Vector3.Zero;
                    j.Direction = Vector3.Zero;
                    j.StartPosition = j.Position;
                    j.Position = j.StartPosition + ClassicMath.VectorRotate(
                        new Vector3(0f, -50f, 0f),
                        ClassicMath.AngleMatrix(j.TargetPosition));
                    break;

                case 9:
                    j.LifeTime = 0f;
                    j.Scale = sourceScale;
                    j.MaxTails = 10;
                    j.Angle = Vector3.Zero;
                    j.Light = new Vector3(0.3f, 0.3f, 1f);
                    j.StartPosition = (sourceTargetPosition - j.Position) / j.MaxTails;
                    for (int n = 0; n < j.MaxTails; n++)
                    {
                        j.Position += j.StartPosition;
                        AppendJointTailQ(ref j);
                    }
                    break;

                case 11:
                case 25:
                    j.Light = j.SubType == 25
                        ? new Vector3(0.9f, 0.4f, 0.6f)
                        : new Vector3(0.2f, 0.2f, 1f);
                    j.MultiUse = 0f;
                    j.LifeTime = 30f;
                    j.MaxTails = 15;
                    j.Direction.X = R(3000);
                    j.Scale = 30f;
                    break;

                case 13:
                    j.Direction.Z = R(20) + 35f;
                    j.Scale = sourceScale;
                    j.LifeTime = 25f + R(50);
                    j.Light = new Vector3(0.5f);
                    break;

                case 23:
                    j.LifeTime = 20f + (4 - j.PKKey);
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    j.NumTails = -1;
                    j.Velocity = 0f;
                    j.Direction = new Vector3(1f, 5f, 1f);
                    j.CreateTails = false;
                    j.HeadAngle = j.Angle;
                    j.MultiUse = j.PKKey;
                    switch (j.PKKey)
                    {
                        case 0: j.HeadAngle.Z += 90f; j.Position.Z += 200f; break;
                        case 1: j.HeadAngle.Z += 90f; j.Position.Z += 10f; break;
                        case 2: j.HeadAngle.Z -= 90f; j.Position.Z += 200f; break;
                        case 3: j.HeadAngle.Z -= 90f; j.Position.Z += 10f; break;
                        case 4: j.HeadAngle.Z += 180f; j.Position.Z += 100f; break;
                        case 5:
                            j.HeadAngle.X = 90f;
                            j.Position.Z += 100f;
                            j.LifeTime = 10f;
                            j.MaxTails = 20;
                            j.RenderFace = 2; // RENDER_FACE_TWO
                            break;
                    }
                    j.StartPosition = j.Position;
                    break;

                case 43:
                    j.LifeTime = 100f;
                    j.MaxTails = 0;
                    j.CreateTails = false;
                    break;

                case 44:
                    j.Scale = sourceScale;
                    j.LifeTime = 15f;
                    j.MaxTails = 30;
                    j.CreateTails = true;
                    j.Direction.Z = R(2) + 2f;
                    break;

                case 45:
                case 46:
                case 47:
                    j.Light = j.SubType == 47
                        ? new Vector3(0.7f, 0.7f, 1f)
                        : new Vector3(0.2f, 0.2f, 1f);
                    j.MultiUse = R(10);
                    j.LifeTime = 30f + j.MultiUse;
                    j.MaxTails = 15;
                    j.Direction.X = R(3000);
                    j.Scale = 30f;
                    break;

                default:
                    // Source fallback only disables future tail emission.
                    // Preserve default LifeTime=100, MaxTails=20, Scale=10.
                    j.CreateTails = false;
                    break;
            }

            // In Main this copy is unconditional, even if Target was supplied.
            j.TargetPosition = sourceTargetPosition;
            return true;
        }
    }
}
