#nullable enable
// Season 6 Summoner: Drain Life / MODEL_ALICE_DRAIN_LIFE (skill 214).
// One-shot adapter, all 70 native frames are owned by ClassicFX.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(214)]
    [SkillVisualEffect(458)] // Drain Life Strengthener
    [SkillVisualEffect(462)] // Drain Life Mastery
    public sealed class DrainLifeSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;

            var target = context.TargetId != 0
                ? context.World.FindWalkerById(context.TargetId)
                : FindAreaTarget(context);
            return target != null && !ReferenceEquals(context.Caster, target)
                ? new ClassicFxDrainLifeCastVisual(context.Caster, target)
                : null;
        }

        private static WalkerObject? FindAreaTarget(SkillEffectContext context)
        {
            if (!context.IsAreaSkillAnimation || context.TargetPosition is not Vector3 position)
                return null;

            int tileX = (int)(position.X / Constants.TERRAIN_SCALE);
            int tileY = (int)(position.Y / Constants.TERRAIN_SCALE);
            WalkerObject? target = null;
            // The area animation only includes a tile. Use it if it identifies one target.
            foreach (var walker in context.World.Walkers)
            {
                if (ReferenceEquals(walker, context.Caster) ||
                    walker is not (MonsterObject or PlayerObject) ||
                    walker.Status != GameControlStatus.Ready ||
                    (int)walker.Location.X != tileX || (int)walker.Location.Y != tileY)
                    continue;

                if (target != null)
                    return null;

                target = walker;
            }

            return target;
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
