#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(55)]
    public sealed class FireSlashSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            // Automatic area damage is handled by OpenMU, once per 0x1E.
            // Ignore the supplementary 0x19 target animation to avoid duplicates.
            if (!context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject caster || context.World == null)
                return null;
            return new ClassicFxFireSlashVisual(caster);
        }
    }

    internal sealed class ClassicFxFireSlashVisual : WorldObject
    {
        private readonly PlayerObject _caster;
        private float _elapsed;
        private int _gatheringFrame;
        private bool _finished;

        internal ClassicFxFireSlashVisual(PlayerObject caster)
        {
            _caster = caster;
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_finished || Status != GameControlStatus.Ready)
                return;
            _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            ClassicFxRuntime? fx = World?.ClassicFx;
            if (_elapsed > 1.2f || fx == null || !fx.Enabled || fx.IsDisposed ||
                _caster.Status != GameControlStatus.Ready || _caster.IsDead ||
                !ReferenceEquals(_caster.World, World))
            {
                Finish();
                return;
            }

            Vector3 position = _caster.WorldPosition.Translation;
            Vector3 angle = _caster.TotalAngle;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_caster);
            // MuMain ZzzCharacter.cpp FIRE_SLASH: gathering at frames 1/2,
            // then BITMAP_SWORD_FORCE at frame 3. The bitmap carrier creates
            // exactly one JOINT_FORCE/0; reuse its native joint implementation.
            while (_gatheringFrame < 2 && _caster.CurrentFrame >= _gatheringFrame + 1)
            {
                fx.CreateEffect(ClassicFxEffectType.GatheringEffect,
                    position, angle, Vector3.One, owner, subType: 1);
                _gatheringFrame++;
            }
            if (_caster.CurrentFrame < 3)
                return;

            position.Z += 100f;
            // Effect/model angles are radians; native joint math uses degrees.
            Vector3 jointAngle = new Vector3(0f, 0f, MathHelper.ToDegrees(angle.Z) + 45f);
            fx.CreateJoint(ClassicTextureIds.BitmapJointForce,
                position, position, jointAngle, subType: 0,
                target: owner, scale: 150f);
            // No range-attack callback is connected: this is visual only.
            Finish();
        }

        private void Finish()
        {
            _finished = true;
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
