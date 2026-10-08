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