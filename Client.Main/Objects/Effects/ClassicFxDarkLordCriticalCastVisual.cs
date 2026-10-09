#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// One-shot bridge for AT_SKILL_ADD_CRITICAL (64).
    ///
    /// Native ZzzCharacter.cpp creates MODEL_DARKLORD_SKILL on the two
    /// equipment hand bones, subtypes 0 and 1. ClassicFX owns the BMD
    /// instances, their 10-frame lifetime, scale curve and render pass.
    /// This object contains no mesh renderer or particle simulation.
    /// </summary>
    public sealed class ClassicFxDarkLordCriticalCastVisual : WorldObject
    {
        private const string SoundPath = "Sound/sDarkCritical.wav";
        private static readonly Vector3 CastLight = new(1f, 0.6f, 0.3f);
        private readonly PlayerObject _caster;
        private bool _completed;

        public ClassicFxDarkLordCriticalCastVisual(PlayerObject caster)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f, -250f, -70f),
                new Vector3(250f, 250f, 320f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (_completed || World == null || _caster.World == null ||
                !ReferenceEquals(_caster.World, World) ||
                _caster.Status != GameControlStatus.Ready)
            {
                RemoveSelf();
                return;
            }

            ClassicFxRuntime fx = World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
            {
                SpawnLegacyFallback();
                RemoveSelf();
                return;
            }

            // Same hand attachment convention already validated in
            // DarkLordCriticalCastEffect. Snapshot once: native CreateEffect
            // releases these unowned effect models into world space.
            if (!_caster.TryGetHandWorldMatrix(true, out Matrix left) ||
                !_caster.TryGetHandWorldMatrix(false, out Matrix right))
            {
                // Skeleton not ready yet. Preserve the known-good renderer.
                SpawnLegacyFallback();
                RemoveSelf();
                return;
            }

            ClassicFxHandle leftFx = fx.CreateEffect(
                ClassicFxEffectType.DarkLordSkill,
                left.Translation, _caster.TotalAngle, CastLight,
                ClassicFxOwner.None, subType: 0);
            ClassicFxHandle rightFx = fx.CreateEffect(
                ClassicFxEffectType.DarkLordSkill,
                right.Translation, _caster.TotalAngle, CastLight,
                ClassicFxOwner.None, subType: 1);

            if (!leftFx.IsValid || !rightFx.IsValid)
            {
                // No half-rendered cast if the fixed Effect pool is full.
                if (leftFx.IsValid) fx.ReleaseEffect(leftFx);
                if (rightFx.IsValid) fx.ReleaseEffect(rightFx);
                SpawnLegacyFallback();
                Console.WriteLine("[ClassicFX][DL Critical] Fallback: no room for both BMD effects.");
                RemoveSelf();
                return;
            }

            _completed = true;
            PlayCastSound();
            Console.WriteLine("[ClassicFX][DL Critical] Created 2/2 MODEL_DARKLORD_SKILL, subtypes 0/1.");

            // The two pooled BMDs remain alive for 10 original 25-FPS ticks.
            RemoveSelf();
        }

        private void SpawnLegacyFallback()
        {
            if (World == null) return;
            var effect = new DarkLordCriticalCastEffect(_caster);
            World.Objects.Add(effect);
            _ = effect.Load();
        }

        private void PlayCastSound()
        {
            Vector3 source = _caster.WorldPosition.Translation;
            Vector3 listener = source;
            if (MuGame.Instance?.ActiveScene is GameScene scene && scene.Hero != null)
                listener = scene.Hero.WorldPosition.Translation;
            SoundController.Instance.PlayBufferWithAttenuation(
                SoundPath, source, listener, maxDistance: 2500f);
        }

        private void RemoveSelf()
        {
            if (Parent != null) Parent.Children.Remove(this);
            else World?.Objects.Remove(this);
            Dispose();
        }
    }
}
