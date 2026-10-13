#nullable enable
// BroyalMU / Season 6 Dark Lord Fire Burst.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// ZzzCharacter.cpp AT_SKILL_FIREBURST[_STR/_MASTERY] target-hit branch.
// Three MODEL_PIER_PART (0) effects followed by two
// MODEL_DARKLORD_SKILL effects. Their model, mesh and particle movement
// already belongs to the shared ClassicFX runtime.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(61)]
    [SkillVisualEffect(508)]
    [SkillVisualEffect(514)]
    public sealed class FireBurstSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                context.TargetId == 0 ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed ||
                !context.World.TryGetWalkerById(context.TargetId,
                    out WalkerObject? target) ||
                target == null)
                return null;

            return new ClassicFxFireBurstImpactVisual(caster, target);
        }
    }

    internal sealed class ClassicFxFireBurstImpactVisual : WorldObject
    {
        private readonly PlayerObject _caster;
        private readonly WalkerObject _target;
        private float _waitingSeconds;
        private bool _completed;

        internal ClassicFxFireBurstImpactVisual(
            PlayerObject caster, WalkerObject target)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f), new Vector3(250f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_completed || Status != GameControlStatus.Ready)
                return;

            // Original hit processing waits for the DL strike at
            // AnimationFrame >= 3. Unlike a cast-time approximation,
            // do not emit the target-hit visuals during windup.
            _waitingSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_waitingSeconds > 1.2f)
            {
                Finish();
                return;
            }

            if (_caster.CurrentFrame < 3)
                return;

            ClassicFxRuntime? fx = World?.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed ||
                !ReferenceEquals(_caster.World, World) ||
                !ReferenceEquals(_target.World, World) ||
                _caster.Status != GameControlStatus.Ready ||
                _target.Status != GameControlStatus.Ready)
            {
                Finish();
                return;
            }

            ClassicFxOwner casterOwner =
                ClassicFxOwner.FromWorldObject(_caster);
            // Native b->TransformPosition(BoneTransform[0],
            // Vector(40,0,10), Position, true).
            if (!fx.TryTransformOwnerBonePosition(casterOwner, 0,
                    new Vector3(40f, 0f, 10f),
                    out Vector3 impactOrigin))
                return;

            Vector3 angle = _caster.TotalAngle;
            float targetHeight = _target.BoundingBoxLocal.Max.Z;
            ClassicFxOwner targetOwner =
                ClassicFxOwner.FromWorldObject(_target);
            ClassicFxOwner noOwner = ClassicFxOwner.None;
            bool emitted = false;

            // Native order and target-relative Light (= StartPosition):
            // yaw +90 at full target height; yaw 0 and -90 at half.
            for (int branch = 0; branch < 3; branch++)
            {
                Vector3 pierAngle = new Vector3(0f, 0f,
                    angle.Z + MathHelper.ToRadians(90f - branch * 90f));
                Vector3 relativeTarget = new Vector3(0f, 0f,
                    branch == 0 ? targetHeight : targetHeight * 0.5f);
                emitted |= fx.CreateEffect(ClassicFxEffectType.PierPart,
                    impactOrigin, pierAngle, relativeTarget,
                    targetOwner, subType: 0).IsValid;
            }

            Vector3 fireLight = new Vector3(1f, 0.6f, 0.3f);
            emitted |= fx.CreateEffect(ClassicFxEffectType.DarkLordSkill,
                impactOrigin, angle, fireLight,
                noOwner, subType: 0).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.DarkLordSkill,
                impactOrigin, angle, fireLight,
                noOwner, subType: 1).IsValid;

            // Native Move_MODEL_PIER_PART plays the second Fire Burst
            // sample (SOUND_ATTACK_FIRE_BUST_EXP). Emit once per impact
            // rather than overlapping it for each of the three branches.
            // The first sound is already mapped by SkillDefinitions.
            if (emitted)
                SoundController.Instance.PlayBuffer("Sound/eFirebustBoom.wav");

            Finish();
        }

        private void Finish()
        {
            _completed = true;
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
