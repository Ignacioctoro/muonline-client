using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Player
{
    public partial class PlayerObject
    {
        // Native CSItemOption::calcSetOptionList -> ExtendState; ZzzCharacter
        // emits BITMAP_LIGHT/3 every 100 reference frames while standing.
        // Its 31-frame controller creates JOINT_HEALING/9 at life 30 and 15.
        private bool _fullAncientSetEquipped;
        private double _ancientAuraCooldown;
        private double _ancientAuraSecondDelay = -1;
        private ClassicFxRuntime _ancientAuraRuntime;
        private ClassicFxHandle _ancientAuraFirst = ClassicFxHandle.Invalid;
        private ClassicFxHandle _ancientAuraSecond = ClassicFxHandle.Invalid;
        private float _ancientAuraSeedAngle;

        public void SetAncientSetComplete(bool complete)
        {
            if (_fullAncientSetEquipped == complete) return;
            _fullAncientSetEquipped = complete;
            ClearAncientSetAura();
        }

        private void ClearAncientSetAura()
        {
            if (_ancientAuraRuntime != null && !_ancientAuraRuntime.IsDisposed)
            {
                _ancientAuraRuntime.ReleaseJoint(_ancientAuraFirst);
                _ancientAuraRuntime.ReleaseJoint(_ancientAuraSecond);
            }
            _ancientAuraFirst = ClassicFxHandle.Invalid;
            _ancientAuraSecond = ClassicFxHandle.Invalid;
            _ancientAuraCooldown = 0;
            _ancientAuraSecondDelay = -1;
        }

        private void UpdateAncientSetAura(GameTime gameTime)
        {
            var fx = World?.ClassicFx;
            if (!ReferenceEquals(fx, _ancientAuraRuntime))
            {
                ClearAncientSetAura();
                _ancientAuraRuntime = fx;
            }
            double seconds = gameTime.ElapsedGameTime.TotalSeconds;
            if (!_fullAncientSetEquipped || !Visible || IsDead || TotalAlpha <= 0.01f ||
                fx == null || fx.IsDisposed || !fx.Enabled || IsChaosCastleMap(World.WorldIndex) ||
                MuGame.Instance?.IsActive != true || seconds > 0.25)
            {
                ClearAncientSetAura();
                return;
            }
            if (seconds <= 0) return;

            // Advance real time only once per frame. Never replay missed bursts
            // after tabbing out, a loading pause, or a long frame.
            _ancientAuraCooldown -= seconds;
            if (_ancientAuraSecondDelay >= 0)
            {
                _ancientAuraSecondDelay -= seconds;
                if (_ancientAuraSecondDelay <= 0)
                {
                    _ancientAuraSecond = EmitAncientSetJoint(fx, _ancientAuraSeedAngle + 360f);
                    _ancientAuraSecondDelay = -1;
                }
            }

            bool standing = CurrentAction < PlayerAction.PlayerWalkMale ||
                CurrentAction is PlayerAction.PlayerDarklordStand or PlayerAction.PlayerStopRideHorse
                    or PlayerAction.PlayerStopTwoHandSwordTwo;
            if (_ancientAuraCooldown > 0 || !standing) return;

            _ancientAuraSeedAngle = MathHelper.ToDegrees(Angle.Z);
            _ancientAuraFirst = EmitAncientSetJoint(fx, _ancientAuraSeedAngle + 180f);
            _ancientAuraSecondDelay = 15.0 / ClassicFxClock.ReferenceFps;
            _ancientAuraCooldown = 100.0 / ClassicFxClock.ReferenceFps;
        }

        private ClassicFxHandle EmitAncientSetJoint(ClassicFxRuntime fx, float seedAngle)
        {
            // The complete native subtype already supplies its orbit, rising
            // trail, lifetime, fade and blue flare sprites. Keep that renderer.
            return fx.CreateJoint(ClassicTextureIds.BitmapJointHealing,
                WorldPosition.Translation, WorldPosition.Translation, new Vector3(0, 0, seedAngle),
                subType: 9, target: ClassicFxOwner.FromWorldObject(this), scale: 10f);
        }

        public override void Dispose()
        {
            ClearEquipmentSetTrails();
            ClearAncientSetAura();
            _ancientAuraRuntime = null;
            base.Dispose();
        }
    }
}
