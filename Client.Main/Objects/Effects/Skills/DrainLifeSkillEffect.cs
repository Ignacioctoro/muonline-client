#nullable enable
// Season 6 Summoner: Drain Life / MODEL_ALICE_DRAIN_LIFE (skill 214).
// One-shot adapter, all 70 native frames are owned by ClassicFX.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(214)]
    public sealed class DrainLifeSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                context.TargetId == 0 ||
                !context.World.TryGetWalkerById(context.TargetId,
                    out var target) || target == null ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;
            return new ClassicFxDrainLifeCastVisual(context.Caster, target);
        }
    }

    internal sealed class ClassicFxDrainLifeCastVisual : WorldObject
    {
        private readonly WalkerObject _source;
        private readonly WalkerObject _target;
        private bool _started;

        internal ClassicFxDrainLifeCastVisual(WalkerObject source,
            WalkerObject target)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Position = _source.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-200f), new Vector3(200f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;

            if (!_started && World != null &&
                ReferenceEquals(_source.World, World) &&
                ReferenceEquals(_target.World, World) &&
                _source.Status == GameControlStatus.Ready &&
                _target.Status == GameControlStatus.Ready)
            {
                _started = true;
                World.ClassicFx?.CreateDrainLife(
                    _source, _target, _source.TotalAngle);
            }

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
