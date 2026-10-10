#nullable enable
// Deep Impact (46): native MODEL_ARROW_IMPACT projectile. The existing
// combat system owns targeting and damage. Visual only, bound to target.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(46)]
    public sealed class DeepImpactSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.World == null || context.Caster == null ||
                context.TargetId == 0 ||
                !context.World.TryGetWalkerById(context.TargetId,
                    out var target) || target == null ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;

            return new ClassicFxDeepImpactCastVisual(
                context.Caster, target);
        }
    }

    internal sealed class ClassicFxDeepImpactCastVisual : WorldObject
    {
        private readonly WalkerObject _caster;
        private readonly WalkerObject _target;
        private bool _emitted;

        public ClassicFxDeepImpactCastVisual(
            WalkerObject caster, WalkerObject target)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Position = caster.WorldPosition.Translation;
            IsTransparent = true;
            Interactive = false;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-300f), new Vector3(300f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready || _emitted)
                return;
            _emitted = true;

            if (World != null && World.ClassicFx != null &&
                World.ClassicFx.Enabled &&
                !World.ClassicFx.IsDisposed &&
                ReferenceEquals(_caster.World, World) &&
                ReferenceEquals(_target.World, World) &&
                _caster.Status == GameControlStatus.Ready &&
                _target.Status == GameControlStatus.Ready)
            {
                // The original calculates ArrowPos from the bow/crossbow
                // skeleton. Use the validated caster pose as a conservative
                // launch origin until that common equipment bridge exposes
                // the actual weapon-local arrow position.
                World.ClassicFx.CreateEffect(
                    ClassicFxEffectType.ArrowImpact,
                    _caster.WorldPosition.Translation,
                    _caster.TotalAngle, Vector3.One,
                    ClassicFxOwner.FromWorldObject(_target));
            }

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
