#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Persistent visual adapter for Dark Lord's Increase Critical Damage buff.
    /// Native MuMain: ZzzCharacter.cpp emits BITMAP_FLARE_FORCE Effect(1)
    /// each 1200 ms; Effect(1) creates Joint(5), Joint(6), Joint(7),
    /// each scale 20, using Fire04.
    ///
    /// BROYALMU REQUIREMENT: always render on BOTH player hands (bones
    /// 33 and 42), whether weapons are equipped or not. Never gate on
    /// Weapon1/Weapon2. This is deliberate vs Main's equipped-weapon path.
    ///
    /// This object does not draw any geometry: all six native joints are
    /// simulated and rendered by the shared ClassicFxRuntime.
    /// </summary>
    public sealed class ClassicFxCriticalDamageAuraVisual : WorldObject
    {
        private const double PulseIntervalMilliseconds = 1200.0;
        private static readonly int[] JointSubTypes = { 5, 6, 7 };
        private readonly PlayerObject _owner;
        private ClassicFxRuntime? _runtime;
        private double _nextPulseMilliseconds;
        private bool _readyForPulse = true;
        private bool _reported;

        public PlayerObject Owner => _owner;

        public ClassicFxCriticalDamageAuraVisual(PlayerObject owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Position = owner.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-180f, -180f, -60f),
                new Vector3(180f, 180f, 300f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (World == null || _owner.World == null ||
                !ReferenceEquals(_owner.World, World) ||
                _owner.Status != GameControlStatus.Ready)
            {
                Hidden = true;
                return;
            }

            Position = _owner.WorldPosition.Translation;
            Hidden = _owner.Hidden || _owner.IsDead;
            if (Hidden)
                return;

            ClassicFxRuntime? fx = World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return;

            if (!ReferenceEquals(_runtime, fx))
            {
                _runtime = fx;
                _readyForPulse = true;
                _reported = false;
            }

            double now = fx.Clock.WorldTimeMilliseconds;
            if (!_readyForPulse && now < _nextPulseMilliseconds)
                return;

            int created = EmitForHand(fx, true) + EmitForHand(fx, false);
            // Retry if bones are not available yet, without firing per-frame.
            _nextPulseMilliseconds = now + (created > 0 ? PulseIntervalMilliseconds : 200.0);
            _readyForPulse = false;

            if (created > 0 && !_reported)
            {
                _reported = true;
                Console.WriteLine($"[ClassicFX][DL Aura] {created}/6 FLARE_FORCE joints (5/6/7), scale=20, 1200ms.");
            }
        }

        private int EmitForHand(ClassicFxRuntime fx, bool left)
        {
            if (!_owner.TryGetHandWorldMatrix(left, out Matrix handWorld))
                return 0;

            int boneIndex = left
                ? PlayerObject.LeftHandBoneIndex
                : PlayerObject.RightHandBoneIndex;
            Vector3 position = handWorld.Translation;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_owner);
            int count = 0;

            // Native BITMAP_FLARE_FORCE Effect(1) creates these three joints.
            // SkillIndex is the player's HAND bone (33/42), regardless
            // of equipped weapon. Joint Move() follows this animated bone.
            // PKKey=-1 is the default native path.
            foreach (int subType in JointSubTypes)
            {
                ClassicFxHandle handle = fx.CreateJoint(
                    ClassicTextureIds.BitmapFlareForce,
                    position, position, _owner.TotalAngle,
                    subType: subType,
                    target: owner,
                    scale: 20f,
                    pkKey: -1,
                    skillIndex: (ushort)boneIndex);
                if (handle.IsValid)
                    count++;
            }
            return count;
        }
    }
}
