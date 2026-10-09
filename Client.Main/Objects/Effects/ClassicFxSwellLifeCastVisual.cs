#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// One-shot bridge for AT_SKILL_SWELL_LIFE (48).
    ///
    /// MuMain ZzzCharacter.cpp creates 36 SPIRIT subtype 2 joints and two
    /// BITMAP_MAGIC+1 subtype 4 effects. Movement, tails, particles, and
    /// drawing belong to ClassicFxRuntime, not this WorldObject.
    ///
    /// The persistent Greater Fortitude hair/body aura is deliberately
    /// still managed by BuffVisualManager.
    /// </summary>
    public sealed class ClassicFxSwellLifeCastVisual : WorldObject
    {
        private const int SpiritJointCount = 36;
        private readonly WalkerObject _caster;
        private bool _emitted;

        public ClassicFxSwellLifeCastVisual(WalkerObject caster)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-300f, -300f, -80f),
                new Vector3(300f, 300f, 340f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status != GameControlStatus.Ready)
                return;

            if (_emitted || _caster.Status != GameControlStatus.Ready ||
                _caster.World == null || World == null ||
                !ReferenceEquals(World, _caster.World))
            {
                RemoveSelf();
                return;
            }

            ClassicFxRuntime fx = World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
            {
                RemoveSelf();
                return;
            }

            _emitted = true;
            Vector3 basePosition = _caster.WorldPosition.Translation;
            Vector3 jointPosition = basePosition + new Vector3(0f, 0f, 100f);
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_caster);
            Vector3 light = Vector3.One;

            int joints = 0;
            int groundEffects = 0;
            for (int i = 0; i < SpiritJointCount; i++)
            {
                // MuMain: Angle(-10, 0, i*10); SPIRIT subtype 2,
                // 60-unit width. Tail geometry is produced by ClassicFX.
                Vector3 angle = new Vector3(-10f, 0f, i * 10f);
                if (fx.CreateJoint(
                    ClassicTextureIds.BitmapJointSpirit,
                    jointPosition, jointPosition, angle,
                    subType: 2, target: owner, scale: 60f).IsValid)
                {
                    joints++;
                }

                // Exactly i == 0 and i == 20 in the original 36 iterations.
                if (i % 20 == 0 && fx.CreateEffect(
                    ClassicFxEffectType.MagicGround2,
                    basePosition, angle, light, owner,
                    subType: 4).IsValid)
                {
                    groundEffects++;
                }
            }

            Console.WriteLine(
                $"[ClassicFX][InnerBK] Created {joints}/36 SPIRIT(2), " +
                $"{groundEffects}/2 MAGIC+1(4). Remaining visuals are owned by ClassicFX.");

            // A one-frame adapter; the pooled transient effects outlive it.
            // Crucially, their Owner is _caster and NOT this disposable adapter.
            RemoveSelf();
        }

        private void RemoveSelf()
        {
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
