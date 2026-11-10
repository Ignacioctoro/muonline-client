using System;
using System.Buffers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private const int FlameStrikeHandBone = 33;
        private const float FlameStrikeTrailStart = 5f;
        private const float FlameStrikeTrailEnd = 13f;

        private bool MoveFlameStrike(ref EffectState effect, float frameFactor)
        {
            if (effect.Owner.WorldObject is not PlayerObject caster ||
                caster.Status != GameControlStatus.Ready || caster.IsDead ||
                !ReferenceEquals(caster.World, World))
                return false;

            if (effect.LifeTime < 20f)
            {
                effect.Alpha -= 0.1f * frameFactor;
                if (effect.Alpha < 0.1f)
                    return false;
            }
            else
            {
                effect.Alpha = MathF.Min(1f, effect.Alpha + 0.1f * frameFactor);
            }

            effect.Position = caster.WorldPosition.Translation;
            effect.Angle = caster.TotalAngle;
            var view = effect.ModelView;
            var casterPose = caster.GetBoneTransforms();
            if (view.Status != GameControlStatus.Ready ||
                view.Model?.Bones is not { Length: > 9 } effectBones ||
                caster.Model?.Bones is not { Length: > FlameStrikeHandBone } casterBones ||
                casterPose is not { Length: > FlameStrikeHandBone })
                return true;

            bool swinging = caster.CurrentAction == PlayerAction.PlayerSkillFlamestrike;
            float frame = swinging
                ? Math.Max(effect.Phase, Math.Min(caster.AnimationFramePosition, FlameStrikeTrailEnd))
                : effect.Phase;
            var pool = ArrayPool<Matrix>.Shared;
            Matrix[] bladePose = pool.Rent(effectBones.Length);
            try
            {
                if (view.TrySampleAnimationPose(0, frame, bladePose))
                    view.SetFlameStrikePose(bladePose, casterPose[FlameStrikeHandBone]);

                if (!swinging || !Clock.AdvancedReferenceFrame || frame <= effect.Phase)
                    return true;

                if (frame >= FlameStrikeTrailStart)
                {
                    Matrix[] handPose = pool.Rent(casterBones.Length);
                    try
                    {
                        AppendFlameStrikeTrails(caster, view, handPose, bladePose,
                            Math.Max(effect.Phase, FlameStrikeTrailStart), frame);
                    }
                    finally
                    {
                        pool.Return(handPose);
                    }
                }

                effect.Phase = frame;
            }
            finally
            {
                pool.Return(bladePose);
            }

            return true;
        }

        private void AppendFlameStrikeTrails(PlayerObject caster, ClassicFxEffectModelObject view,
            Matrix[] handPose, Matrix[] bladePose, float fromFrame, float toFrame)
        {
            // MuMain samples both animations ten times during the swing.
            var blurOwner = ClassicFxOwner.FromWorldObject(view);
            Matrix casterWorld = caster.WorldPosition;
            for (int i = 1; i <= 10; i++)
            {
                float frame = MathHelper.Lerp(fromFrame, toFrame, i / 10f);
                if (!caster.TrySampleAnimationPose((int)PlayerAction.PlayerSkillFlamestrike, frame, handPose) ||
                    !view.TrySampleAnimationPose(0, frame, bladePose))
                    return;

                Matrix hand = handPose[FlameStrikeHandBone] * casterWorld;
                Vector3 start = Vector3.Transform(bladePose[9].Translation, hand);
                Vector3 end = Vector3.Transform(bladePose[6].Translation, hand);
                CreateObjectBlur(blurOwner, start, end, Vector3.One, 2, subType: 1);
                CreateObjectBlur(blurOwner, start, end, Vector3.One, 2, subType: 2);
                start = Vector3.Transform(bladePose[8].Translation, hand);
                end = Vector3.Transform(bladePose[5].Translation, hand);
                CreateObjectBlur(blurOwner, start, end, Vector3.One, 5, subType: 3);
            }
        }
    }
}
