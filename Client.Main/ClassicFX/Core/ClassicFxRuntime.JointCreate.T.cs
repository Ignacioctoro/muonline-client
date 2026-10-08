using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // From _enum.h. MODEL_SPEARSKILL is a model ID, not a bitmap ID.
        public const int ClassicModelSpearSkill = 322;

        // Native MODEL_SPEARSKILL (subtypes 15/17) reads Target->Owner->Position.
        // This relationship is not automatically identical to WorldObject.Parent.
        // Supply it only when the actual object ownership mapping is known.
        public Func<ClassicFxOwner, Vector3?> JointSpearOwnerPositionResolver { get; set; }

        // Native FindCharacterIndex(iChaIndex) for SpearSkill 10/11.
        public Func<int, int> JointSpearCharacterIndexResolver { get; set; }

        // MuMain ZzzEffectJoint.cpp, CreateJoint(), source lines 1516–1676.
        // Creation ONLY: movement and rendering remain separate.
        private bool InitializeJointCreateT(
            ref ClassicJoint j,
            in Vector3 sourceTargetPosition,
            float sourceScale,
            Vector3? priorColor)
        {
            if (j.Type == ClassicTextureIds.BitmapJointFire)
            {
                j.InitializeFirstTail();
                j.Scale = 70f;
                j.Velocity = 50f;
                j.MaxTails = 8;
                j.LifeTime = 20f;
                j.TargetPosition.Z += 130f;
                return true;
            }

            if (j.Type == ClassicTextureIds.BitmapSpark + 1)
            {
                j.InitializeFirstTail();
                j.LifeTime = 100f;
                j.MaxTails = 20;
                j.Scale = 10f;
                if (j.SubType == 0)
                {
                    j.Direction.Z = R(20) + 35f;
                    j.Scale = R(20) + 20f;
                    j.LifeTime = 25f;
                }
                else if (j.SubType == 1)
                {
                    j.Scale = sourceScale;
                    j.Velocity = R(55) + 14f;
                    j.LifeTime = R(10) + 8f;
                    j.MaxTails = 4;
                }
                j.Light = Vector3.One;
                j.TargetPosition = j.Position;
                return true;
            }

            if (j.Type != ClassicModelSpearSkill)
                return false;

            // Native CreateJoint() dereferences Target on entry for this model.
            if (!TryGetOwnerPosition(j.Target, out Vector3 targetPosition))
                return false;
            j.TargetPosition = targetPosition;

            // The native initial tail is skipped only for subtypes 5, 6 and 7.
            bool firstTailSkipped = j.SubType >= 5 && j.SubType <= 7;
            if (!firstTailSkipped)
                j.InitializeFirstTail();

            switch (j.SubType)
            {
                case 0:
                    j.Light = Vector3.One;
                    j.LifeTime = 999999f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapFlareBlue;
                    break;
                case 4:
                case 9:
                    j.Light = new Vector3(0.4f, 0.8f, 0.2f);
                    j.LifeTime = 10000f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapFlareBlue;
                    break;
                case 10:
                case 11:
                    if (j.CharacterIndex != -1)
                    {
                        if (JointSpearCharacterIndexResolver == null)
                            return false;
                        j.CharacterIndex = JointSpearCharacterIndexResolver(j.CharacterIndex);
                    }
                    j.Light = new Vector3(1f, 0.6f, 0.6f);
                    j.LifeTime = 10000f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapLuckySealEffect;
                    break;
                case 1:
                    j.Light = new Vector3(0.2f);
                    j.LifeTime = 999999f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapJointSpirit;
                    break;
                case 2:
                    j.TargetPosition = sourceTargetPosition;
                    j.Light = new Vector3(1f, 0.3f, 0.3f);
                    j.LifeTime = 20f;
                    j.MaxTails = 5;
                    j.TexType = ClassicTextureIds.BitmapFlareForce;
                    break;
                case 3:
                    if (TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot snapshot))
                        j.Light = snapshot.Light;
                    else
                        j.Light = new Vector3(0.5f, 0f, 0f);
                    j.Angle = new Vector3(-90f, 0f, 0f);
                    j.Direction = new Vector3(0f, R(500), 0f);
                    j.Velocity = R(5) + 5f;
                    j.LifeTime = 999999f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapJointSpirit;
                    break;
                case 5:
                case 6:
                case 7:
                    j.RenderFace = 1; // RENDER_FACE_ONE
                    j.LifeTime = 60f;
                    j.MaxTails = 30;
                    j.Weapon = 0f;
                    j.CreateTails = false;
                    j.Light = j.SubType switch
                    {
                        5 => new Vector3(1f, 1f, 0.8f),
                        6 => new Vector3(1f, 0.8f, 1f),
                        _ => new Vector3(0.8f, 1f, 1f)
                    };
                    j.TexType = ClassicTextureIds.BitmapFlare + 1;
                    j.Direction = new Vector3(0f, 800f, 0f);
                    j.StartPosition = sourceTargetPosition;
                    j.Position = j.StartPosition + ClassicMath.VectorRotate(
                        j.Direction, ClassicMath.AngleMatrix(j.Angle));
                    // Native: NumTails=-1, then CreateTail() creates slot 0.
                    j.NumTails = -1;
                    AppendJointTailQ(ref j);
                    break;
                case 8:
                    j.RenderFace = 1;
                    j.LifeTime = 40f;
                    j.MaxTails = 30;
                    j.CreateTails = false;
                    j.Light = new Vector3(0.5f);
                    j.TexType = ClassicTextureIds.BitmapLight;
                    j.Direction = new Vector3(0f, -40f, 0f);
                    j.StartPosition = sourceTargetPosition;
                    j.Position = j.StartPosition + ClassicMath.VectorRotate(
                        j.Direction, ClassicMath.AngleMatrix(j.Angle));
                    break;
                case 14:
                    // Native dereferences vPriorColor unconditionally.
                    if (!priorColor.HasValue)
                        return false;
                    j.Light = priorColor.Value;
                    j.LifeTime = 100f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapLight;
                    break;
                case 15:
                case 17:
                    Vector3? parentPosition = JointSpearOwnerPositionResolver?.Invoke(j.Target);
                    if (!parentPosition.HasValue)
                        return false;
                    j.Light = j.SubType == 15
                        ? Vector3.One
                        : new Vector3(0.7f, 0.2f, 1f);
                    j.LifeTime = 100f;
                    j.MaxTails = 20;
                    j.TexType = ClassicTextureIds.BitmapJointSpirit;
                    j.StartPosition = parentPosition.Value;
                    break;
                case 16:
                    j.Light = new Vector3(1f, 1f, 0f);
                    j.LifeTime = 100f;
                    j.MaxTails = 30;
                    j.TexType = ClassicTextureIds.BitmapLight;
                    break;
                default:
                    return false;
            }

            j.Scale = sourceScale; // Native final assignment.
            return true;
        }
    }
}
