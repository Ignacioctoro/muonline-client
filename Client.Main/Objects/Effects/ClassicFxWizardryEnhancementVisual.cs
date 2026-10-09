using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Persistent visual emitted by MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF
    /// in the original Season 6 client (ZzzEffect.cpp, RenderEffects).
    ///
    /// Does not pretend to port MODEL_SWELL_OF_MAGICPOWER's 45-frame
    /// casting model or MODEL_ARROWSRE06 hand models. Those still require
    /// the classic Effect/Model bridge. All rendered glow sprites here
    /// use the shared ClassicFX sprite pool and billboard renderer.
    /// </summary>
    public sealed class ClassicFxWizardryEnhancementVisual : WorldObject
    {
        private readonly PlayerObject _owner;
        private double _nextArrowsPulseMilliseconds;

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

        // This is a CAST event, never an effect of attaching an active buff.
        // In particular, map/scope reloads must not replay the casting model.
        public static bool IsWizardryEnhancementSkill(ushort skillId) =>
            skillId == 233 || skillId == 380 || skillId == 383;

        public static void TriggerCast(PlayerObject caster)
        {
            if (caster == null ||
                caster.Status != GameControlStatus.Ready ||
                caster.World == null)
                return;

            ClassicFxRuntime fx = caster.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return;

            ClassicFxOwner effectOwner = ClassicFxOwner.FromWorldObject(caster);
            var cast = fx.CreateEffect(
                ClassicFxEffectType.SwellOfMagicPower,
                caster.WorldPosition.Translation,
                caster.TotalAngle,
                new Vector3(0.4f, 0.3f, 0.9f), effectOwner);

            if (cast.IsValid)
                Console.WriteLine("[ClassicFX] Wizardry Enhancement: cast triggered by skill animation");
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (_owner.Status != GameControlStatus.Ready ||
                _owner.World == null || World == null ||
                !ReferenceEquals(_owner.World, World))
            {
                Hidden = true;
                return;
            }

            Position = _owner.WorldPosition.Translation;
            Hidden = _owner.Hidden || _owner.IsDead;
            if (Hidden)
                return;

            ClassicFxRuntime fx = World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return;

            // MonoGame bone transforms already follow the current player
            // animation. A single fetch is shared by all per-bone sprites.
            Matrix[] bones = _owner.GetBoneTransforms();
            if (bones == null || bones.Length == 0)
                return;

            // The persistent aura is attached by buff state and must NOT
            // initiate a casting animation (e.g. when changing maps).
            // Only real skill casts call TriggerCast().
            ClassicFxOwner effectOwner = ClassicFxOwner.FromWorldObject(_owner);
            double nowMillis = fx.Clock.WorldTimeMilliseconds;
            if (_nextArrowsPulseMilliseconds <= 0.0)
                _nextArrowsPulseMilliseconds = nowMillis + 6000.0;

            if (nowMillis >= _nextArrowsPulseMilliseconds)
            {
                // Original persistent MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF:
                // hand pulse every six seconds on bones 28/37.
                Vector3 purple = new Vector3(0.2f, 0.2f, 0.9f);
                foreach (int bone in new[] { 28, 37 })
                {
                    if ((uint)bone >= (uint)bones.Length) continue;
                    Vector3 hand = (bones[bone] * _owner.WorldPosition).Translation;
                    fx.CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        hand, _owner.TotalAngle, purple, effectOwner,
                        subType: 1, boneIndex: bone);
                }
                _nextArrowsPulseMilliseconds = nowMillis + 6000.0;
            }

            // Native RenderEffects(MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF, 0):
            // fLumi = (abs(sin(WorldTime * .001f)) + .2f) * .5f;
            // vDLight = (fLumi * .7f, fLumi * .3f, fLumi * .9f);
            // for each model bone: CreateSprite(BITMAP_LIGHT, bone, 1.8f)
            float time = (float)fx.Clock.WorldTimeMilliseconds;
            float luminance = (MathF.Abs(MathF.Sin(time * 0.001f)) + 0.2f) * 0.5f;
            Vector3 light = new Vector3(
                luminance * 0.7f,
                luminance * 0.3f,
                luminance * 0.9f);

            Matrix worldTransform = _owner.WorldPosition;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_owner);

            for (int i = 0; i < bones.Length; i++)
            {
                Matrix boneWorld = bones[i] * worldTransform;
                Vector3 position = boneWorld.Translation;
                fx.CreateSprite(
                    ClassicTextureIds.BitmapLight,
                    position,
                    1.8f,
                    light,
                    owner);
            }
        }
    }
}