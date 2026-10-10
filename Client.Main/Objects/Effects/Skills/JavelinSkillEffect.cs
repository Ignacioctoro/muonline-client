#nullable enable
// Native Javelin skill 45: three target-bound MODEL_SKILL_JAVELIN
// variants from ZzzCharacter.cpp. Visual-only one-shot dispatcher.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(45)]
    public sealed class JavelinSkillEffect : ISkillVisualEffect
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

            return new ClassicFxJavelinCastVisual(context.Caster, target);
        }
    }

    internal sealed class ClassicFxJavelinCastVisual : WorldObject
    {
        private readonly WalkerObject _caster;
        private readonly WalkerObject _target;
        private bool _emitted;

        public ClassicFxJavelinCastVisual(WalkerObject caster,
            WalkerObject target)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
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
                Vector3 position = _caster.WorldPosition.Translation;
                Vector3 angle = _caster.TotalAngle;
                ClassicFxOwner target = ClassicFxOwner.FromWorldObject(_target);
                for (int subType = 0; subType <= 2; subType++)
                    World.ClassicFx.CreateEffect(
                        ClassicFxEffectType.Javelin,
                        position, angle, Vector3.One,
                        target, subType: subType);
            }

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
