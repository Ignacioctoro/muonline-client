using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // Native CreateJoint() branches in ZzzEffectJoint.cpp, lines 603-943.
        // Hooks deliberately defer the native Effect/audio integration until
        // those engines exist; no placeholder visuals or invented sound.
        public Func<ushort> JointPacketSerialResolver { get; set; }
        public Action JointBrandishSwordSoundRequested { get; set; }
        public Action<ClassicFxHandle, string, Vector3, Vector3, Vector3, int, float>
            JointSecondaryEffectRequested { get; set; }

        private bool InitializeJointCreateQ(
            ClassicFxHandle handle,
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale)
        {
            if (j.Type != ClassicTextureIds.Bitmap2LineGhost &&
                j.Type != ClassicTextureIds.BitmapJointSpirit &&
                j.Type != ClassicTextureIds.BitmapJointSpirit2)
                return false;

            // None of these three families suppresses the native first tail.
            j.InitializeFirstTail();

            if (j.Type == ClassicTextureIds.Bitmap2LineGhost)
                return InitializeJointGhostQ(handle, ref j, sourceScale);

            return InitializeJointSpiritQ(handle, ref j,
                sourceTargetPosition, sourceScale);
        }

        private bool InitializeJointGhostQ(
            ClassicFxHandle handle, ref ClassicJoint j, float sourceScale)
        {
            if (j.SubType == 0)
            {
                j.RenderType = 3; // ALPHA_BLEND_MINUS
                j.Velocity = 40f + R(20);
                j.LifeTime = Random.FpsCheck(2, Clock) ? 67f : 75f;
                j.MaxTails = 26;
                j.Scale = sourceScale + R(200) + 1f;
                j.Direction.X = 0f;
                j.TargetIndexRef = 2;
                if (R(3) < 2)
                {
                    j.Light = new Vector3(0.5f);
                    // Main: CreateEffect(MODEL_DESAIR, ..., owner=NULL,
                    // scale=1, m_sTargetIndex = this joint pool index).
                    JointSecondaryEffectRequested?.Invoke(
                        handle, "MODEL_DESAIR", j.Position, j.Angle,
                        j.Light, 0, 1f);
                }
                return true;
            }

            if (j.SubType == 1)
            {
                j.RenderType = 1; // ALPHA_BLEND
                j.Scale = sourceScale;
                j.LifeTime = 25f + R(10);
                j.MaxTails = 15;
                j.Velocity = 20f + R(10);
                j.TargetPosition = j.Position;

                Vector3 randomAngle = new Vector3(0f, 0f, R(360));
                Vector3 radialOffset = ClassicMath.VectorRotate(
                    new Vector3(0f, 300f, 0f),
                    ClassicMath.AngleMatrix(randomAngle));
                j.Position.X += radialOffset.X;
                j.Position.Y += radialOffset.Y;
                j.Position.Z = R(100) + 200f;
                j.TargetPosition.Z = j.Position.Z -
                    (R(100) - 100f) * (Random.FpsCheck(2, Clock) ? 1f : -1f);
                j.Direction = j.TargetPosition - j.Position;
                j.Angle.Z = CreateNativeJointAngleQ(j.Position, j.TargetPosition);
                return true;
            }

            return false;
        }

        // MuMain AngleMath.cpp: atan2(dx, -dy), normalized to [0, 360).
        private static float CreateNativeJointAngleQ(in Vector3 from, in Vector3 to)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;
            if (MathF.Abs(dx) < float.Epsilon && MathF.Abs(dy) < float.Epsilon)
                return 0f;
            float angle = MathF.Atan2(dx, -dy) * (180f / MathF.PI);
            return (angle % 360f + 360f) % 360f;
        }

        private bool InitializeJointSpiritQ(
            ClassicFxHandle handle, ref ClassicJoint j,
            in Vector3 sourceTargetPosition, float sourceScale)
        {
            bool spirit2 = j.Type == ClassicTextureIds.BitmapJointSpirit2;
            j.RenderType = 3; // Native default: ALPHA_BLEND_MINUS

            switch (j.SubType)
            {
                case 0:
                    // Packet serial is a runtime/network dependency.
                    // A missing resolver is left explicitly unresolved.
                    j.Weapon = JointPacketSerialResolver?.Invoke() ?? 0f;
                    j.Velocity = 70f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 6;
                    break;
                case 1:
                    j.Velocity = 70f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 6;
                    JointBrandishSwordSoundRequested?.Invoke();
                    break;
                case 2:
                case 21:
                    j.RenderType = 1;
                    j.RenderFace = 2;
                    j.Velocity = 50f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 3;
                    j.Light = new Vector3(0.5f);
                    j.StartPosition = j.Position;
                    break;
                case 3:
                    j.Velocity = 140f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 10;
                    if (spirit2)
                        j.Light = Vector3.One;
                    else
                    {
                        j.RenderType = 1;
                        j.Light = new Vector3(1f, 0.5f, 0.1f);
                    }
                    break;
                case 4:
                {
                    j.RenderType = 1;
                    j.LifeTime = 0f; // Native: initial tail is prebuilt.
                    j.Scale = sourceScale;
                    j.MaxTails = 10;
                    j.Angle = Vector3.Zero;
                    j.Light = new Vector3(0.3f, 0.3f, 1f);
                    j.StartPosition = (sourceTargetPosition - j.Position) / j.MaxTails;
                    Vector3 step = j.StartPosition;
                    for (int i = 0; i < j.MaxTails - 1; i++)
                    {
                        j.Position += step * Clock.FrameFactor;
                        AppendJointTailQ(ref j);
                        j.Position.X -= step.X;
                        j.Position.X += j.StartPosition.X;
                        j.Position.Y -= step.Y;
                        j.Position.Y += j.StartPosition.Y;
                    }
                    // Exactly the native final copy from o->TargetPosition.
                    j.Position = j.TargetPosition;
                    break;
                }
                case 5:
                    j.RenderType = 1;
                    j.Weapon = JointPacketSerialResolver?.Invoke() ?? 0f;
                    j.Velocity = 30f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 12;
                    break;
                case 6:
                    j.RenderType = 1;
                    j.RenderFace = 2;
                    j.Velocity = 50f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 5;
                    j.PKKey = (short)R(5);
                    if (j.PKKey != 0) j.CreateTails = false;
                    ApplySpiritSkillLightQ(ref j);
                    j.StartPosition = j.Position;
                    break;
                case 7:
                    j.RenderFace = 0;
                    j.Velocity = 10f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 5;
                    ApplySpiritSkillLightQ(ref j);
                    j.StartPosition = j.Position;
                    break;
                case 8:
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    break;
                case 9:
                    j.RenderFace = 2;
                    j.Velocity = 10f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    j.Light = Vector3.One;
                    j.Angle = new Vector3(-90f, 0f, 0f);
                    j.Direction = Vector3.Zero;
                    j.StartPosition = j.Position;
                    break;
                case 10:
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 6;
                    break;
                case 11:
                    j.Velocity = 0f;
                    j.LifeTime = 50f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    break;
                case 12:
                    j.Velocity = 0f;
                    j.LifeTime = 30f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    break;
                case 13:
                    if (!spirit2) j.RenderType = 1;
                    j.Velocity = 40f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 10;
                    j.Light = new Vector3(1f, 0.6f, 0.4f);
                    break;
                case 14:
                    j.Velocity = 0f;
                    j.LifeTime = 10f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    j.Angle.X = 20f;
                    break;
                case 15:
                    j.Velocity = 0f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    break;
                case 16:
                    j.Velocity = 0f;
                    j.LifeTime = 50f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    break;
                case 17:
                    j.Velocity = 0f;
                    j.LifeTime = 100f;
                    j.Scale = sourceScale;
                    j.MaxTails = 1;
                    break;
                case 18:
                    if (spirit2) j.RenderType = 1;
                    j.Velocity = 50f;
                    j.LifeTime = 39f;
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    j.Light = new Vector3(0.7f);
                    break;
                case 19:
                    if (!spirit2) j.RenderType = 1;
                    j.Velocity = 70f;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 9;
                    j.Light = new Vector3(0.1f, 0.5f, 0.2f);
                    break;
                case 20:
                    if (spirit2) j.RenderType = 1;
                    j.LifeTime = 49f;
                    j.Scale = sourceScale;
                    j.MaxTails = 15;
                    break;
                case 22:
                    j.RenderType = 1;
                    j.RenderFace = 2;
                    j.Velocity = 60f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 0;
                    j.StartPosition = j.Position;
                    break;
                case 23:
                    j.RenderFace = 0;
                    j.Velocity = 10f;
                    j.LifeTime = 20f;
                    j.Scale = sourceScale;
                    j.MaxTails = 0;
                    j.StartPosition = j.Position;
                    break;
                case 24:
                    j.RenderType = 1;
                    j.RenderFace = 2;
                    j.Velocity = 10f;
                    j.LifeTime = 160f;
                    j.Scale = sourceScale;
                    j.MaxTails = 40;
                    j.Light = Vector3.Zero;
                    j.Position.X += R(300) - 150f;
                    j.Position.Y += R(300) - 150f;
                    float effectScale = j.Scale / 70f;
                    if (effectScale >= 0.9f)
                        JointSecondaryEffectRequested?.Invoke(
                            handle, "MODEL_SUMMONER_SUMMON_LAGUL", j.Position,
                            j.Angle, new Vector3(0f, 0f, 0.1f), 1, effectScale);
                    break;
                case 25:
                    j.RenderType = 1;
                    j.TexType = ClassicTextureIds.BitmapShiny;
                    j.Velocity = 9f;
                    j.LifeTime = 26f;
                    j.Scale = sourceScale;
                    j.MaxTails = 30;
                    j.Light = new Vector3(0.9f, 0.8f, 1f);
                    j.Angle = new Vector3(-90f, 0f, 0f);
                    j.Direction = Vector3.Zero;
                    j.StartPosition = j.Position;
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static void ApplySpiritSkillLightQ(ref ClassicJoint j)
        {
            if (j.Skill == 0)
                j.Light = new Vector3(0.3f, 0.3f, 1f);
            else if (j.Skill == 1)
            {
                j.Velocity = 10f;
                j.Light = new Vector3(0.5f);
            }
        }

        // Non-blur variant of native CreateTail(); appends one quad into the
        // existing fixed-size 200 x 4 tail array without allocating memory.
        private static void AppendJointTailQ(ref ClassicJoint j)
        {
            if (j.MaxTails < 1 || j.Tails == null)
                return;
            j.NumTails = Math.Min(j.NumTails + 1, j.MaxTails - 1);
            int count = Math.Min(j.NumTails, ClassicJoint.MaxTailSegments - 1);
            for (int t = count - 1; t >= 0; t--)
            {
                int from = t * 4;
                int to = (t + 1) * 4;
                j.Tails[to] = j.Tails[from];
                j.Tails[to + 1] = j.Tails[from + 1];
                j.Tails[to + 2] = j.Tails[from + 2];
                j.Tails[to + 3] = j.Tails[from + 3];
            }
            // Exactly the same four vectors as native CreateTail() without Blur.
            j.InitializeFirstTail();
        }
    }
}
