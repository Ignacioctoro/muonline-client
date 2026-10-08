using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain ZzzEffectJoint.cpp: CreateJoint, BITMAP_FLARE + 1,
        // native lines 2069-2284. This is creation state only; the movement
        // and rendering switches will be ported separately.
        private bool InitializeJointCreateV(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale,
            short originalPkKey,
            Vector3? priorColor)
        {
            if (j.Type != ClassicTextureIds.BitmapFlare + 1)
                return false;

            // Native CreateJoint special-case: subtype 8 does NOT build the
            // first tail and starts NumTails at -1. Every other subtype
            // receives the initial quad before the Type switch is executed.
            if (j.SubType == 8)
                j.NumTails = -1;
            else
                j.InitializeFirstTail();

            // The native branch uses the PKKey *argument* as its initial
            // lifetime, then resets the stored PKKey. Do not swap these.
            j.LifeTime = originalPkKey;
            j.PKKey = 0;
            j.MaxTails = 50;

            switch (j.Skill)
            {
                case 0:
                    j.Scale = 20f;
                    j.Light = new Vector3(0.5f);
                    break;
                case 1:
                    j.Scale = 40f;
                    j.Light = new Vector3(1f, 0.5f, 0f);
                    break;
                case 3:
                    j.Scale = 20f;
                    if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot skillOwner))
                        return false;
                    j.Light = skillOwner.Light;
                    break;
                // Main has no default case: keep InitializeCommon defaults.
            }

            switch (j.SubType)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    // Native: 0, 90, 180, 240 (not 90 * subtype).
                    j.Velocity = j.SubType switch
                    {
                        0 => 0f,
                        1 => 90f,
                        2 => 180f,
                        _ => 240f
                    };
                    j.Direction = new Vector3(sourceScale * 1.5f, 0f, 0f);
                    break;

                case 4:
                    j.RenderType = 3; // RENDER_TYPE_ALPHA_BLEND_MINUS
                    j.LifeTime = 0f;
                    j.Scale = sourceScale;
                    j.MaxTails = 10;
                    j.Angle = Vector3.Zero;
                    j.Light = Vector3.One;
                    j.StartPosition = (sourceTargetPosition - j.Position) / j.MaxTails;
                    // Native: creates ten tail quads during CreateJoint.
                    // AppendJointTailQ uses the same head/shift convention.
                    for (int n = 0; n < j.MaxTails; n++)
                    {
                        j.Position += j.StartPosition;
                        AppendJointTailQ(ref j);
                    }
                    break;

                case 5:
                    j.Light = Vector3.One;
                    j.RenderType = 4; // RENDER_TYPE_ALPHA_BLEND_OTHER
                    j.RenderFace = 2; // RENDER_FACE_TWO
                    j.LifeTime = 50f;
                    j.MaxTails = 8;
                    j.Velocity = 3f;
                    j.OnlyOneRender = 2;
                    // Native intentionally keeps Scale from Skill switch.
                    break;

                case 6:
                    j.Light = Vector3.One;
                    j.RenderType = 4;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 5f;
                    break;

                case 7:
                    j.Light = Vector3.One;
                    j.Direction = Vector3.Zero;
                    j.RenderType = 4;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 10f;
                    j.TexType = ClassicTextureIds.BitmapFlare;
                    j.Direction.X = 15f;
                    // The native code passes rand()%360 straight to cos/sin,
                    // which interpret the value in RADIANS, not degrees.
                    j.Position.X = sourceTargetPosition.X + MathF.Cos(R(360)) * 40f;
                    j.Position.Y = sourceTargetPosition.Y - MathF.Sin(R(360)) * 40f;
                    j.StartPosition = j.Position;
                    break;

                case 8:
                case 9:
                case 10:
                case 11:
                {
                    if (j.SubType == 8 || j.SubType == 9)
                        j.RenderFace = 2;
                    j.RenderType = 4;
                    j.Scale = sourceScale;
                    j.MaxTails = (j.SubType == 10 || j.SubType == 11) ? 10 : 20;
                    j.LifeTime = 100f;
                    j.Velocity = 0f;
                    j.OnlyOneRender = 1;
                    j.MultiUse = j.Skill;
                    j.Light = (j.SubType == 10 || j.SubType == 11)
                        ? new Vector3(0.7f) : Vector3.One;
                    j.Direction = Vector3.Zero;

                    // Native code requires Target->Position AND Target->Angle.
                    // A missing owner invalidates this joint safely.
                    if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot owner))
                        return false;
                    j.Position = owner.Position;
                    j.Angle = owner.Angle;
                    if (j.SubType == 8)
                        j.Position.Z = 300f;
                    else if (j.SubType == 9)
                        j.Position.Z += j.MultiUse;
                    break;
                }

                case 12:
                    j.Light = new Vector3(0.6f, 0.2f, 0.8f);
                    j.RenderType = 4;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 70f;
                    break;
                case 13:
                    j.Light = new Vector3(0.7f, 0.7f, 0.3f);
                    j.RenderType = 4;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 70f;
                    break;
                case 14:
                    j.Light = Vector3.One;
                    j.RenderType = 1; // RENDER_TYPE_ALPHA_BLEND
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 70f;
                    break;
                case 15:
                    j.Light = new Vector3(0.4f, 0.9f, 0.5f);
                    j.RenderType = 1;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 70f;
                    break;
                case 16:
                    j.RenderType = 4;
                    j.RenderFace = 2;
                    j.LifeTime = 35f;
                    j.MaxTails = 8;
                    j.Velocity = 7f;
                    j.OnlyOneRender = 2;
                    j.Light = new Vector3(1f, 0.5f, 0.3f);
                    break;
                case 17:
                    j.Light = Vector3.One;
                    j.RenderType = 1;
                    j.Scale = sourceScale;
                    j.MaxTails = 16;
                    j.Velocity = 70f;
                    break;
                case 18:
                    // Native overrides Skill-based Light with vPriorColor
                    // specifically in subtype 18 when the argument exists.
                    if (priorColor.HasValue)
                        j.Light = priorColor.Value;
                    j.RenderType = 1;
                    j.Scale = sourceScale;
                    j.MaxTails = 7;
                    j.Velocity = 70f;
                    break;
                case 19:
                case 20:
                    j.Light = new Vector3(0.5f, 0.5f, 1f);
                    j.RenderType = 1;
                    j.Scale = sourceScale;
                    j.MaxTails = 14;
                    j.Velocity = 30f;
                    j.LifeTime = 50f;
                    break;
                // No native default: retain the common fields, Skill-based
                // light/scale, LifeTime=originalPkKey, and MaxTails=50.
            }

            // Native VectorCopy(TargetPosition, o->TargetPosition) occurs
            // unconditionally after the subtype switch.
            j.TargetPosition = sourceTargetPosition;
            return true;
        }
    }
}
