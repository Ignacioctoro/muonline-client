#nullable enable
// MuMain@21728b1e: ZzzCharacter.cpp FORCE / LONGPIER_ATTACK target-hit.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(60)]
    [SkillVisualEffect(66)]
    [SkillVisualEffect(509)]
    public sealed class ForceSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster || context.World == null ||
                context.IsAreaSkillAnimation)
                return null;
            return new ClassicFxForceImpactVisual(caster);
        }
    }

    internal sealed class ClassicFxForceImpactVisual : WorldObject
    {
        private readonly PlayerObject _caster;
        private float _elapsed;
        private bool _finished;

        internal ClassicFxForceImpactVisual(PlayerObject caster)
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
                _caster.IsDead || _caster.Status != GameControlStatus.Ready ||
                !ReferenceEquals(_caster.World, World))
            {
                Finish();
                return;
            }
            if (_caster.CurrentFrame < 3)
                return;

            Vector3 position = _caster.WorldPosition.Translation;
            Vector3 angle = _caster.TotalAngle;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_caster);
            // Native emits two MODEL_WAVES/1 and one MODEL_PIERCING2/0.
            fx.CreateEffect(ClassicFxEffectType.Waves,
                position, angle, Vector3.One, owner, subType: 1);
            fx.CreateEffect(ClassicFxEffectType.Waves,
                position, angle, Vector3.One, owner, subType: 1);
            fx.CreateEffect(ClassicFxEffectType.Piercing2,
                position, angle, Vector3.One, owner);
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
