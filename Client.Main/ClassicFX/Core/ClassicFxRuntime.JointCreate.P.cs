using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Named OBJECT eye locations from the native Main. MonoGame currently
    /// has no EyeLeft/Right fields; integrations must supply real positions.
    /// </summary>
    public enum ClassicJointEye : byte
    {
        Left, Right, Left2, Right2, Left3, Right3
    }

    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Optional bridge to the actual classic OBJECT eye locations.
        /// An absent bridge never fabricates a bone index or attachment.
        /// </summary>
        public Func<ClassicFxOwner, ClassicJointEye, Vector3?> JointEyeResolver { get; set; }

        // ZzzEffectJoint.cpp CreateJoint(): common initial tail and the
        // consecutive SCOLPION_TAIL, JOINT_ENERGY, JOINT_HEALING cases.
        // Unported joint types are rejected by the caller until their actual
        // native initializers are implemented (no invisible pool leaks).
        private bool InitializeJointCreateP(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale,
            bool hasPriorColor,
            int characterIndex)
        {
            if (j.Type != ClassicTextureIds.BitmapScolpionTail &&
                j.Type != ClassicTextureIds.BitmapJointEnergy &&
                j.Type != ClassicTextureIds.BitmapJointHealing)
                return false;

            bool skipFirstTail = j.Type == ClassicTextureIds.BitmapScolpionTail ||
                (j.Type == ClassicTextureIds.BitmapJointEnergy &&
                 (j.SubType == 10 || j.SubType == 11 ||
                  j.SubType == 14 || j.SubType == 15 ||
                  j.SubType == 55 || j.SubType == 56 || j.SubType == 57));

            if (skipFirstTail)
                j.NumTails = -1;
            else
                j.InitializeFirstTail();

            switch (j.Type)
            {
                case ClassicTextureIds.BitmapScolpionTail:
                    j.Scale = sourceScale;
                    j.LifeTime = 120f;
                    j.MaxTails = 28;
                    j.Velocity = 3f;
                    return true;

                case ClassicTextureIds.BitmapJointEnergy:
                    j.Scale = sourceScale;
                    return InitializeJointEnergyP(
                        ref j, sourceTargetPosition, hasPriorColor, characterIndex);

                case ClassicTextureIds.BitmapJointHealing:
                    j.Scale = sourceScale;
                    return InitializeJointHealingP(
                        ref j, sourceTargetPosition, sourceScale);
            }
            return false;
        }

        private bool InitializeJointEnergyP(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            bool hasPriorColor,
            int characterIndex)
        {
            switch (j.SubType)
            {
                case 0: case 1: case 6: case 7: case 12:
                case 13: case 16: case 44: case 45: case 46:
                    j.LifeTime = 120f;
                    j.MaxTails = 8;
                    j.Velocity = 3f;
                    if (j.SubType == 44)
                    {
                        j.LifeTime = 45f;
                        j.MaxTails = 20;
                    }
                    if (j.SubType == 12 || j.SubType == 13 ||
                        j.SubType == 16 || j.SubType == 46)
                    {
                        j.MultiUse = R(2) != 0 ? 1f : -1f;
                        j.Velocity = 10f;
                        j.Angle.X = R(45);
                        j.Angle.Y = R(45);
                    }
                    break;

                case 9:
                    j.Light = new Vector3(0.2f);
                    j.LifeTime = 120f;
                    j.MaxTails = 8;
                    j.Velocity = 3f;
                    break;

                case 17:
                case 47:
                    j.Velocity = 0f;
                    j.LifeTime = 12f;
                    // Direct EyeLeft/EyeRight copy in the original. If
                    // unknown we keep the call's Position, never invent eyes.
                    if (!j.Target.HasOwner) return false;
                    if (TryJointEye(j.Target,
                        j.SubType == 17 ? ClassicJointEye.Left : ClassicJointEye.Right,
                        out Vector3 directEye))
                        j.Position = directEye;
                    break;

                case 2: case 3: case 4: case 5: case 8:
                case 10: case 11: case 14: case 15:
                case 18: case 19: case 20: case 21:
                case 26: case 27: case 28: case 29:
                case 30: case 31: case 32: case 33:
                    j.Velocity = 0f;
                    j.LifeTime = 999999999f;
                    j.MaxTails = j.SubType == 5 ? 10 :
                        (j.SubType == 10 || j.SubType == 11) ? 5 :
                        (j.SubType == 14 || j.SubType == 15) ? 15 : 20;
                    if (!j.Target.HasOwner) return false;

                    ClassicJointEye eye;
                    if (j.SubType == 3 || j.SubType == 11 || j.SubType == 15 ||
                        j.SubType == 19 || j.SubType == 29)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Right;
                    }
                    else if (j.SubType == 18 || j.SubType == 28)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Left;
                    }
                    else if (j.SubType == 20 || j.SubType == 30)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Left2;
                    }
                    else if (j.SubType == 21 || j.SubType == 31)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Right2;
                    }
                    else if (j.SubType == 26 || j.SubType == 32)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Left3;
                    }
                    else if (j.SubType == 27 || j.SubType == 33)
                    {
                        j.MaxTails = 8;
                        eye = ClassicJointEye.Right3;
                    }
                    else
                    {
                        eye = ClassicJointEye.Left;
                    }
                    UpdateJointTargetPositionP(ref j, eye);
                    j.TexType = j.SubType >= 28 && j.SubType <= 33
                        ? ClassicTextureIds.BitmapFlare
                        : ClassicTextureIds.BitmapJointEnergy;
                    break;

                case 22: case 23: case 24:
                    j.Velocity = 0f;
                    j.LifeTime = 999999999f;
                    j.MaxTails = 10;
                    if (!j.Target.HasOwner) return false;
                    UpdateJointTargetPositionP(ref j,
                        j.SubType == 23 ? ClassicJointEye.Right : ClassicJointEye.Left);
                    break;

                case 25:
                    j.Velocity = 0f;
                    j.LifeTime = 999999999f;
                    j.MaxTails = 10;
                    if (!j.Target.HasOwner) return false;
                    if (TryJointEye(j.Target, ClassicJointEye.Right, out Vector3 right))
                        j.Position = right;
                    break;

                case 40: case 41:
                    j.Velocity = 0f;
                    j.LifeTime = 999999999f;
                    // Main does not set MaxTails for these subtypes.
                    j.Position = sourceTargetPosition;
                    break;

                case 42: case 43:
                    j.LifeTime = 100f;
                    j.MaxTails = 6;
                    j.Velocity = 3f;
                    break;

                case 48: case 49: case 50:
                case 51: case 52: case 53:
                    j.LifeTime = 999999999f;
                    j.MaxTails = 3;
                    j.Light = new Vector3(0.5f, 0.5f, 0.9f);
                    break;

                case 54:
                    j.Velocity = 0f;
                    j.LifeTime = 999999999f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapSpark + 1;
                    if (!j.Target.HasOwner) return false;
                    ClassicJointEye? attachment = j.PKKey switch
                    {
                        0 => ClassicJointEye.Right2,
                        1 => ClassicJointEye.Left2,
                        2 => ClassicJointEye.Right3,
                        3 => ClassicJointEye.Left3,
                        _ => null
                    };
                    if (attachment.HasValue &&
                        TryJointEye(j.Target, attachment.Value, out Vector3 eye54))
                        j.Position = eye54;
                    break;

                case 55: case 56:
                    j.Velocity = 0f;
                    j.MaxTails = 8;
                    j.LifeTime = 10f;
                    j.TexType = ClassicTextureIds.BitmapJointEnergy;
                    if (!j.Target.HasOwner) return false;
                    UpdateJointTargetPositionP(ref j,
                        j.SubType == 55 ? ClassicJointEye.Left : ClassicJointEye.Right);
                    break;

                case 57:
                    j.Velocity = 0f;
                    j.MaxTails = characterIndex; // Exactly iChaIndex in Main.
                    j.LifeTime = 10f;
                    j.TexType = ClassicTextureIds.BitmapJointEnergy;
                    break;

                default:
                    // Unknown subtype: native creation has no initialized
                    // lifetime; refuse an inert slot instead of leaking it.
                    return false;
            }

            if (!hasPriorColor)
                ApplyJointEnergyDefaultColorP(ref j);
            return true;
        }

        private static void ApplyJointEnergyDefaultColorP(ref ClassicJoint j)
        {
            switch (j.SubType)
            {
                case 0: case 6: j.Light = new Vector3(0.4f, 0.3f, 0.2f); break;
                case 1: j.Light = new Vector3(0.1f, 0.1f, 0.5f); break;
                case 2: case 3: case 5: j.Light = new Vector3(0.5f, 0.1f, 1f); break;
                case 4: j.Light = new Vector3(0.3f, 0.15f, 0.1f); break;
                case 8: j.Light = new Vector3(1f, 0f, 0.5f); break;
                case 10: case 11: j.Light = new Vector3(1f, 0.3f, 0.1f); break;
                case 12: case 13: j.Light = new Vector3(0.7f, 0.3f, 1f); break;
                case 14: case 15: j.Light = new Vector3(1f, 0.1f, 0.1f); break;
                case 16: j.Light = new Vector3(0.4f, 0.2f, 0.4f); break;
                case 17: j.Light = new Vector3(0.8f, 0.2f, 1f); break;
                case 18: case 19: case 20: case 21:
                case 26: case 27: j.Light = new Vector3(0.8f, 0.5f, 1f); break;
                case 42: j.Light = Vector3.Zero; break;
                case 43: j.Light = new Vector3(2.5f, 0f, 0f); break;
                case 46: j.Light = new Vector3(0.1f, 0.25f, 0.1f); break;
                case 47: j.Light = new Vector3(0.9f, 0f, 0f); break;
            }
        }

        private bool InitializeJointHealingP(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale)
        {
            j.LifeTime = 12f;
            j.Scale = sourceScale;
            j.MaxTails = 2;
            j.Velocity = 0f;
            switch (j.SubType)
            {
                case 4:
                    j.LifeTime = 30f;
                    j.TargetPosition = new Vector3(R(64) - 32f, -10f, 0f);
                    break;
                case 5:
                    j.Scale += R(10) - 5;
                    j.Velocity = R(20) + 6;
                    j.LifeTime = R(8) + 8;
                    j.MaxTails = 8;
                    j.Light = Vector3.One;
                    break;
                case 6:
                    j.MaxTails = 4;
                    j.Light = new Vector3(1f, 1f, 0.5f);
                    break;
                case 7:
                    j.MaxTails = 4;
                    j.Light = new Vector3(1f, 1f, 0f);
                    break;
                case 8:
                    j.LifeTime = 17f;
                    j.MaxTails = 3;
                    j.Light = new Vector3(0.5f, 0.5f, R(128) / 255f + 0.5f);
                    j.TargetPosition = sourceTargetPosition;
                    j.TargetPosition.Z += 100f;
                    break;
                case 9: case 10:
                    j.LifeTime = j.SubType == 9 ? 90f : 80f;
                    j.MaxTails = 20;
                    j.NumTails = 0;
                    j.MultiUse = (int)j.Angle.Z;
                    j.Direction.X = j.SubType == 9 ? 50f : 80f;
                    j.Collision = j.SubType == 10 &&
                        (j.MultiUse == 225f || j.MultiUse == 405f);
                    j.Light = new Vector3(1f / 11f, 0.5f / 11f, 1f / 11f);
                    if (!TryGetOwnerPosition(j.Target, out Vector3 ownerPosition))
                        return false;
                    j.TargetPosition = ownerPosition;
                    j.Position = ownerPosition;
                    break;
                case 13:
                    j.LifeTime = 17f;
                    j.MaxTails = 10;
                    j.Light = new Vector3(1f, 0.3f, 0.2f);
                    j.TargetPosition = sourceTargetPosition;
                    j.TargetPosition.Z += 200f;
                    break;
                case 14:
                    j.LifeTime = 10f;
                    j.MaxTails = 10;
                    j.Light = new Vector3(0.8f, 1f, 0.8f);
                    j.TargetPosition = sourceTargetPosition;
                    break;
                case 15:
                    j.LifeTime = 10f;
                    break;
                case 16:
                    j.LifeTime = 10f;
                    j.RenderType = 3; // RENDER_TYPE_ALPHA_BLEND_MINUS
                    break;
                case 17:
                    j.LifeTime = 17f;
                    j.MaxTails = 3;
                    j.Light = new Vector3(R(128) / 255f + 0.6f, 0.1f, 0f);
                    j.TargetPosition = sourceTargetPosition;
                    j.TargetPosition.Z += 100f;
                    break;
            }
            return true;
        }

        private bool TryJointEye(
            in ClassicFxOwner owner,
            ClassicJointEye eye,
            out Vector3 position)
        {
            position = Vector3.Zero;
            if (!owner.HasOwner) return false;
            Vector3? resolved = JointEyeResolver?.Invoke(owner, eye);
            if (!resolved.HasValue || resolved.Value == Vector3.Zero)
                return false;
            position = resolved.Value;
            return true;
        }

        // Native UpdateJointTargetPosition(): Eye if nonzero, otherwise
        // owner's Position if nonzero. This is not a guessed Eye location.
        private void UpdateJointTargetPositionP(
            ref ClassicJoint j,
            ClassicJointEye eye)
        {
            if (TryJointEye(j.Target, eye, out Vector3 eyePosition))
                j.Position = eyePosition;
            else if (TryGetOwnerPosition(j.Target, out Vector3 ownerPosition) &&
                     ownerPosition != Vector3.Zero)
                j.Position = ownerPosition;
        }
    }
}
