#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    internal sealed class ClassicFxIncreaseBlockVisual : WorldObject
    {
        private ClassicFxRuntime? _runtime;
        private ClassicFxHandle _aura = ClassicFxHandle.Invalid;

        public PlayerObject Owner { get; }

        public ClassicFxIncreaseBlockVisual(PlayerObject owner)
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
                ReleaseAura();
                _runtime = runtime;
            }

            if (runtime is not { Enabled: true, IsDisposed: false } ||
                !ReferenceEquals(Owner.World, World) || Owner.Status != GameControlStatus.Ready ||
                Owner.Hidden || Owner.IsDead || MuGame.Instance?.IsActive == false)
            {
                ReleaseAura();
                return;
            }

            Position = Owner.WorldPosition.Translation;
            if (!_aura.IsValid || !runtime.IsEffectAlive(_aura))
                _aura = runtime.CreateEffect(ClassicFxEffectType.WindForce,
                    Position, Owner.TotalAngle, new Vector3(0.6f, 0.6f, 1f),
                    ClassicFxOwner.FromWorldObject(Owner), subType: 1,
                    scale: ClassicFxRuntime.RageFighterBuffVisualScale);
        }

        private void ReleaseAura()
        {
            if (_aura.IsValid && _runtime is { IsDisposed: false })
                _runtime.ReleaseEffect(_aura);
            _aura = ClassicFxHandle.Invalid;
        }

        public override void Dispose()
        {
            ReleaseAura();
            _runtime = null;
            base.Dispose();
        }
    }
}
