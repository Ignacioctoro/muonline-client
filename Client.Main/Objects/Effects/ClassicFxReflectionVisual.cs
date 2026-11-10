#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>Owns the classic Thorns flare emitter while Reflection is active.</summary>
    public sealed class ClassicFxReflectionVisual : WorldObject
    {
        private ClassicFxRuntime? _runtime;
        private ClassicFxHandle _joint = ClassicFxHandle.Invalid;

        public PlayerObject Owner { get; }

        public ClassicFxReflectionVisual(PlayerObject owner)
        {
            Owner = owner;
            Position = owner.WorldPosition.Translation;
            Interactive = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            var runtime = World?.ClassicFx;
            if (!ReferenceEquals(runtime, _runtime))
            {
                ReleaseJoint();
                _runtime = runtime;
            }

            if (runtime == null || !runtime.Enabled || runtime.IsDisposed ||
                !ReferenceEquals(Owner.World, World) || Owner.Status != GameControlStatus.Ready ||
                Owner.Hidden || Owner.IsDead || MuGame.Instance?.IsActive == false)
            {
                ReleaseJoint();
                return;
            }

            Position = Owner.WorldPosition.Translation;
            if (_joint.IsValid && runtime.TryGetJoint(_joint, out _))
                return;

            _joint = runtime.CreateJoint(ClassicTextureIds.BitmapFlare,
                Position, Position, Owner.TotalAngle, subType: 43,
                target: ClassicFxOwner.FromWorldObject(Owner), scale: 50f,
                priorColor: new Vector3(0.9f, 0.6f, 0.1f));
        }

        private void ReleaseJoint()
        {
            if (_joint.IsValid && _runtime is { IsDisposed: false })
                _runtime.ReleaseJoint(_joint);
            _joint = ClassicFxHandle.Invalid;
        }

        public override void Dispose()
        {
            ReleaseJoint();
            _runtime = null;
            base.Dispose();
        }
    }
}
