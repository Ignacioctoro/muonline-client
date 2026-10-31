#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Buff-state adapter. The pooled SwellMagicBuffCarrier alone renders
    /// native bone sprites and six-second hand rings; no second sprite loop.
    /// </summary>
    public sealed class ClassicFxWizardryEnhancementVisual : WorldObject
    {
        private readonly PlayerObject _owner;
        private ClassicFxRuntime? _runtime;
        private ClassicFxHandle _handle = ClassicFxHandle.Invalid;
        public PlayerObject Owner => _owner;

        public ClassicFxWizardryEnhancementVisual(PlayerObject owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Position = owner.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-140f, -140f, -40f),
                new Vector3(140f, 140f, 310f));
        }

        public static bool IsWizardryEnhancementSkill(ushort skillId) =>
            skillId is 233 or 380 or 383;

        // This is a cast, not an active-buff/scope restoration event.
        public static bool TriggerCast(PlayerObject caster)
        {
            if (caster == null ||
                caster.Status != GameControlStatus.Ready ||
                caster.World == null)
                return false;

            ClassicFxRuntime fx = caster.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            return fx.CreateEffect(
                ClassicFxEffectType.SwellOfMagicPower,
                caster.WorldPosition.Translation,
                caster.TotalAngle,
                new Vector3(0.4f, 0.3f, 0.9f),
                ClassicFxOwner.FromWorldObject(caster)).IsValid;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (World == null || _owner.World == null ||
                !ReferenceEquals(World, _owner.World) ||
                _owner.Status != GameControlStatus.Ready)
            {
                Hidden = true;
                ReleaseCarrier();
                return;
            }

            if (MuGame.Instance?.IsActive == false)
            {
                ReleaseCarrier();
                return;
            }

            Position = _owner.WorldPosition.Translation;
            Hidden = _owner.Hidden || _owner.IsDead;
            if (Hidden)
            {
                ReleaseCarrier();
                return;
            }

            ClassicFxRuntime current = World.ClassicFx;
            if (!ReferenceEquals(_runtime, current))
            {
                ReleaseCarrier();
                _runtime = current;
            }

            if (_runtime == null || !_runtime.Enabled || _runtime.IsDisposed)
            {
                ReleaseCarrier();
                return;
            }

            if (_handle.IsValid && _runtime.IsEffectAlive(_handle))
                return;

            _handle = _runtime.CreateEffect(
                ClassicFxEffectType.SwellMagicBuffCarrier,
                _owner.WorldPosition.Translation,
                _owner.TotalAngle,
                Vector3.One,
                ClassicFxOwner.FromWorldObject(_owner),
                subType: 0);
        }

        private void ReleaseCarrier()
        {
            if (_handle.IsValid && _runtime != null && !_runtime.IsDisposed)
                _runtime.ReleaseEffect(_handle);
            _handle = ClassicFxHandle.Invalid;
        }

        public override void Dispose()
        {
            ReleaseCarrier();
            _runtime = null;
            base.Dispose();
        }
    }
}
