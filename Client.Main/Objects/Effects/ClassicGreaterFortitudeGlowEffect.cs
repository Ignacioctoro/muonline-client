#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Persistent classic Greater Fortitude / Swell Life visual.
    ///
    /// Original Main:
    ///
    /// BITMAP_LIGHT subtype 1 acts as a controller while
    /// eBuff_Life is active.
    ///
    /// Every classic frame it selects two mirrored upper-body bones:
    ///
    ///     25, 26, 27, 20, 34, 35, 36
    ///
    /// and creates BITMAP_LIGHT particle subtype 4.
    ///
    /// Subtype 4:
    ///
    ///     LifeTime = 10
    ///     Gravity = 0
    ///     Scale = 2
    ///
    /// It stays attached to the selected player bone,
    /// rises vertically, shrinks slightly and fades.
    ///
    /// Bone 20 is responsible for the characteristic
    /// yellow/orange Greater Fortitude head/hair glow.
    /// </summary>
    public sealed class ClassicGreaterFortitudeGlowEffect
        : WorldObject
    {
        private const string TexturePath =
            "Effect/flare01.jpg";

        private const float ClassicReferenceFps =
            25.0f;

        private const float SpawnInterval =
            1.0f / ClassicReferenceFps;

        private const float ParticleLifetime =
            10.0f / ClassicReferenceFps;

        /// <summary>
        /// MU's BITMAP_LIGHT scale isn't equivalent to MonoGame
        /// SpriteBatch scale.
        ///
        /// Neffis' screen projection makes the original Scale=2
        /// considerably smaller, so this converts classic particle
        /// scale to the visual size expected by the MU camera.
        /// </summary>
        private const float ClassicSpriteScaleMultiplier =
            3.25f;
        //
        // Original MU world/effect coordinates are much larger
        // vertically than Neffis/MonoGame player coordinates.
        //
        // Without this conversion, BITMAP_LIGHT subtype 4
        // rises several hundred world units and goes far above
        // the character's wings.
        //
        // Greater Fortitude should remain concentrated around
        // head / shoulders / upper torso.
        //
        private const float ClassicVerticalMotionScale =
            0.20f;

        //
        // Safety ceiling. The original effect does not turn
        // into a tall vertical column above the character.
        //
        private const float MaxVerticalOffset =
            95.0f;

        private const int HeadBone =
            20;

        private static readonly int[] UpperBones =
        {
            25,
            26,
            27,
            HeadBone,
            34,
            35,
            36
        };

        private static readonly BlendState ClassicAdditive =
            new BlendState
            {
                ColorBlendFunction =
                    BlendFunction.Add,

                ColorSourceBlend =
                    Blend.One,

                ColorDestinationBlend =
                    Blend.One,

                AlphaBlendFunction =
                    BlendFunction.Add,

                AlphaSourceBlend =
                    Blend.One,

                AlphaDestinationBlend =
                    Blend.One
            };

        private sealed class GlowParticle
        {
            public int BoneIndex;

            public float Age;

            public float Lifetime;

            /// <summary>
            /// Original:
            ///
            /// Scale -=
            ///     (rand() % 400 + 400) /
            ///     10000.f
            ///
            /// each classic frame.
            /// </summary>
            public float ScaleLossPerFrame;

            /// <summary>
            /// Original:
            ///
            /// Gravity +=
            ///     ((rand() % 40 + 60) /
            ///      100.f * 9.5f)
            ///
            /// each classic frame.
            /// </summary>
            public float GravityStepPerFrame;

            public float SpriteRotation;
        }

        private readonly PlayerObject _owner;

        private readonly List<GlowParticle> _particles =
            new();

        private Texture2D? _texture;

        private float _spawnAccumulator;

        public PlayerObject Owner =>
            _owner;

        public ClassicGreaterFortitudeGlowEffect(
            PlayerObject owner)
        {
            _owner =
                owner ??
                throw new ArgumentNullException(
                    nameof(owner));

            Position =
                _owner.WorldPosition
                    .Translation;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -220f,
                        -220f,
                        -80f),
                    new Vector3(
                        220f,
                        220f,
                        650f));

            Interactive =
                false;

            IsTransparent =
                true;

            AffectedByTransparency =
                true;

            BlendState =
                ClassicAdditive;

            DepthState =
                GraphicsManager.ReadOnlyDepth;
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await TextureLoader.Instance.Prepare(
                TexturePath);

            _texture =
                TextureLoader.Instance
                    .GetTexture2D(
                        TexturePath);

            if (_texture == null)
            {
                Status =
                    GameControlStatus.Error;
            }
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }

            if (_owner.Status ==
                    GameControlStatus.Disposed ||
                _owner.World == null)
            {
                RemoveSelf();

                return;
            }

            Position =
                _owner.WorldPosition
                    .Translation;

            bool hide =
                _owner.Hidden ||
                _owner.IsDead ||
                _owner.Status !=
                    GameControlStatus.Ready;

            Hidden =
                hide;

            float dt =
                (float)
                gameTime.ElapsedGameTime
                    .TotalSeconds;

            UpdateParticles(
                dt);

            if (hide)
            {
                return;
            }

            _spawnAccumulator +=
                dt;

            //
            // Original Main:
            //
            // BITMAP_LIGHT subtype 1 continuously creates
            // two subtype-4 particles while eBuff_Life
            // remains active.
            //
            while (_spawnAccumulator >=
                   SpawnInterval)
            {
                _spawnAccumulator -=
                    SpawnInterval;

                SpawnClassicPair();
            }
        }

        private void SpawnClassicPair()
        {
            int index =
                MuGame.Random.Next(
                    0,
                    UpperBones.Length);

            int mirrorIndex =
                UpperBones.Length -
                1 -
                index;

            int bone1 =
                UpperBones[index];

            int bone2 =
                UpperBones[mirrorIndex];

            SpawnParticle(
                bone1);

            SpawnParticle(
                bone2);
        }

        private void SpawnParticle(
            int boneIndex)
        {
            //
            // Don't create the particle when that bone
            // isn't available in the current player model.
            //
            if (!_owner.TryGetBoneWorldMatrix(
                    boneIndex,
                    out _))
            {
                return;
            }

            float scaleLoss =
                MuGame.Random.Next(
                    400,
                    800) /
                10000.0f;

            float gravityStep =
                (
                    MuGame.Random.Next(
                        60,
                        100) /
                    100.0f
                ) *
                9.5f;

            _particles.Add(
                new GlowParticle
                {
                    BoneIndex =
                        boneIndex,

                    Age =
                        0f,

                    Lifetime =
                        ParticleLifetime,

                    ScaleLossPerFrame =
                        scaleLoss,

                    GravityStepPerFrame =
                        gravityStep,

                    SpriteRotation =
                        MathHelper.ToRadians(
                            MuGame.Random.Next(
                                0,
                                360))
                });
        }

        private void UpdateParticles(
            float dt)
        {
            for (int i =
                     _particles.Count - 1;
                 i >= 0;
                 i--)
            {
                GlowParticle particle =
                    _particles[i];

                particle.Age +=
                    dt;

                if (particle.Age >=
                    particle.Lifetime)
                {
                    _particles.RemoveAt(
                        i);

                    continue;
                }

                _particles[i] =
                    particle;
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                Hidden ||
                _texture == null ||
                _particles.Count == 0)
            {
                return;
            }

            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    ClassicAdditive,
                    SamplerState.LinearClamp,
                    GraphicsManager.ReadOnlyDepth,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                     i < _particles.Count;
                     i++)
                {
                    DrawParticle(
                        spriteBatch,
                        _particles[i]);
                }
            }
        }

        private void DrawParticle(
            SpriteBatch spriteBatch,
            GlowParticle particle)
        {
            if (_texture == null)
            {
                return;
            }

            if (!_owner.TryGetBoneWorldMatrix(
                    particle.BoneIndex,
                    out Matrix boneWorld))
            {
                return;
            }

            //
            // Convert elapsed real time back to the
            // original client's 25 FPS effect time.
            //
            float classicFrame =
                particle.Age *
                ClassicReferenceFps;

            classicFrame =
                MathHelper.Clamp(
                    classicFrame,
                    0f,
                    10f);

            //
            // Original subtype 4 starts with:
            //
            //     Scale = 2
            //
            // and only decreases around 0.04 - 0.08
            // per classic frame.
            //
            float classicScale =
                2.0f -
                (
                    particle.ScaleLossPerFrame *
                    classicFrame
                );

            classicScale =
                MathF.Max(
                    classicScale,
                    0.1f);

            //
            // Original:
            //
            // Gravity starts at zero and increases every
            // frame. Position.Z is then increased by that
            // accumulated gravity.
            //
            // Sum:
            //
            // G * (n * (n + 1) / 2)
            //
            float upOffset =
                particle.GravityStepPerFrame *
                (
                    classicFrame *
                    (
                        classicFrame +
                        1f
                    ) *
                    0.5f
                );

            //
            // MU Main uses a different effect/world scale.
            //
            // Raw classic gravity would send these particles
            // 300-500+ Neffis world units upward, which is why
            // they were flying above the wings.
            //
            // Compress the vertical component so the effect remains
            // around the head, shoulders and upper torso.
            //
            upOffset *=
                ClassicVerticalMotionScale;

            upOffset =
                MathF.Min(
                    upOffset,
                    MaxVerticalOffset);

            Vector3 worldPosition =
                boneWorld.Translation;

            worldPosition.Z +=
                upOffset;

            //
            // Bone 20 starts inside/very close to the
            // head geometry.
            //
            // A tiny initial lift prevents the additive
            // sprite from disappearing inside the head
            // because of the depth test.
            //
            if (particle.BoneIndex ==
                HeadBone)
            {
                worldPosition.Z +=
                    5f;
            }

            //
            // Original subtype 4:
            //
            // Light *= 1 / 1.35 each frame.
            //
            float luminosity =
                MathF.Pow(
                    1.0f / 1.35f,
                    classicFrame);

            Vector3 light =
                new Vector3(
                    1.0f,
                    0.50f,
                    0.10f) *
                luminosity;

            //
            // The head particle is the part that gives
            // Greater Fortitude its characteristic
            // yellow/orange hair glow.
            //
            // We keep the same particle family, only
            // compensate slightly for Neffis' projection.
            //
            float headMultiplier =
            particle.BoneIndex ==
            HeadBone
                ? 1.38f
                : 0.92f;

            DrawWorldSprite(
                spriteBatch,
                worldPosition,
                light,
                classicScale *
                headMultiplier,
                particle.SpriteRotation);
        }

        private void DrawWorldSprite(
            SpriteBatch spriteBatch,
            Vector3 worldPosition,
            Vector3 light,
            float classicScale,
            float rotation)
        {
            if (_texture == null)
            {
                return;
            }

            Viewport viewport =
                GraphicsDevice.Viewport;

            Vector3 projected =
                viewport.Project(
                    worldPosition,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            if (projected.Z < 0f ||
                projected.Z > 1f)
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    Camera.Instance.Position,
                    worldPosition);

            float neffisScale =
                1f /
                MathF.Max(
                    distance /
                    Constants.TERRAIN_SIZE,
                    0.1f);

            neffisScale *=
                Constants.RENDER_SCALE;

            //
            // Important:
            //
            // Classic BITMAP_LIGHT Scale=2 is not equal
            // to SpriteBatch scale=2.
            //
            // This conversion is the piece our previous
            // implementation was missing.
            //
            float spriteScale =
                classicScale *
                neffisScale *
                ClassicSpriteScaleMultiplier;

            if (!float.IsFinite(
                    spriteScale) ||
                spriteScale <=
                    0f)
            {
                return;
            }

            Vector2 origin =
                new Vector2(
                    _texture.Width *
                        0.5f,
                    _texture.Height *
                        0.5f);

            spriteBatch.Draw(
                _texture,
                new Vector2(
                    projected.X,
                    projected.Y),
                null,
                new Color(light) *
                    TotalAlpha,
                rotation,
                origin,
                spriteScale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    projected.Z,
                    0f,
                    1f));
        }

        private void RemoveSelf()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(
                    this);

                Dispose();

                return;
            }

            if (World != null)
            {
                World.Objects.Remove(
                    this);

                Dispose();

                return;
            }

            Dispose();
        }

        public override void Dispose()
        {
            _particles.Clear();

            //
            // Texture belongs to TextureLoader cache.
            //
            _texture = null;

            base.Dispose();
        }
    }
}