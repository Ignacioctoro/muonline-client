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
    /// CreateEffect(
    ///     BITMAP_LIGHT,
    ///     target.Position,
    ///     target.Angle,
    ///     target.Light,
    ///     1,
    ///     target);
    ///
    /// While eBuff_Life is active:
    ///
    /// Vector(1.0f, 0.5f, 0.1f, Light);
    ///
    /// Index = rand() % 7;
    ///
    /// g_byUpperBoneLocation[7] =
    /// {
    ///     25, 26, 27,
    ///     20,
    ///     34, 35, 36
    /// };
    ///
    /// CreateParticle(
    ///     BITMAP_LIGHT,
    ///     ...,
    ///     subtype 4,
    ///     boneIndex,
    ///     owner);
    ///
    /// Then creates the mirrored entry:
    ///
    /// g_byUpperBoneLocation[6 - Index]
    ///
    /// Bone 20 corresponds to the head area, which produces
    /// the characteristic yellow/orange hair/head glow.
    /// </summary>
    public sealed class ClassicGreaterFortitudeGlowEffect
        : WorldObject
    {
        private const string TexturePath =
            "Effect/flare01.jpg";

        private static readonly int[] UpperBones =
        {
            25,
            26,
            27,
            20,
            34,
            35,
            36
        };

        private const float ClassicReferenceFps =
            25.0f;

        private const float SpawnInterval =
            1.0f /
            ClassicReferenceFps;

        private const float ParticleLifetime =
            10.0f /
            ClassicReferenceFps;

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

            public float Scale;

            public float UpOffset;

            public float Rotation;
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
                        -160f,
                        -160f,
                        -60f),
                    new Vector3(
                        160f,
                        160f,
                        300f));

            Interactive = false;

            IsTransparent = true;

            AffectedByTransparency = true;

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
            // Original Main continuously spawns
            // upper-body light particles while
            // eBuff_Life remains active.
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

            int bone1 =
                UpperBones[index];

            int bone2 =
                UpperBones[
                    UpperBones.Length -
                    1 -
                    index];

            SpawnParticle(
                bone1);

            SpawnParticle(
                bone2);

            //
            // Bone 20 is the classic head anchor.
            //
            // Original Main reaches this bone randomly.
            // With Neffis' different visual scale the
            // head glow can become too subtle, so we
            // reinforce it occasionally while preserving
            // the original upper-body distribution.
            //
            if (MuGame.Random.Next(
                    0,
                    3) == 0)
            {
                SpawnParticle(
                    20);
            }
        }

        private void SpawnParticle(
            int boneIndex)
        {
            if (!_owner.TryGetBoneWorldMatrix(
                    boneIndex,
                    out _))
            {
                return;
            }

            //
            // Original BITMAP_LIGHT particle subtype 4:
            //
            // LifeTime = 10
            // Gravity = 0
            // Scale = 2
            //
            _particles.Add(
                new GlowParticle
                {
                    BoneIndex =
                        boneIndex,

                    Age =
                        0f,

                    Lifetime =
                        ParticleLifetime,

                    Scale =
                        2.0f,

                    UpOffset =
                        0f,

                    Rotation =
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

                float normalized =
                    MathHelper.Clamp(
                        particle.Age /
                        particle.Lifetime,
                        0f,
                        1f);

                //
                // Classic subtype 4:
                //
                // particle rises,
                // scale shrinks,
                // light fades quickly.
                //
                particle.UpOffset =
                    normalized *
                    normalized *
                    52f;

                particle.Scale =
                    MathHelper.Lerp(
                        2.20f,
                        0.28f,
                        normalized);

                particle.Rotation +=
                    dt *
                    2.5f;

                if (particle.Age >=
                        particle.Lifetime ||
                    particle.Scale <=
                        0.1f)
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

            Vector3 worldPosition =
                boneWorld.Translation;

            worldPosition.Z +=
                particle.UpOffset;

            float normalized =
                MathHelper.Clamp(
                    particle.Age /
                    particle.Lifetime,
                    0f,
                    1f);

            //
            // Original persistent Inner light:
            //
            // (1.0, 0.5, 0.1)
            //
            float luminosity =
                1f -
                normalized;

            luminosity *=
                luminosity;

            Vector3 light =
                new Vector3(
                    1.0f,
                    0.50f,
                    0.10f) *
                luminosity;

            float finalScale =
                particle.Scale;

            //
            // Slight head emphasis.
            //
            // Bone 20 is the classic head anchor,
            // so make it a little stronger without
            // changing the color or visual family.
            //
            if (particle.BoneIndex ==
                20)
            {
                finalScale *=
                    1.18f;
            }

            DrawWorldSprite(
                spriteBatch,
                worldPosition,
                light,
                finalScale,
                particle.Rotation);
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

            Matrix view =
                Camera.Instance.View;

            Matrix projection =
                Camera.Instance.Projection;

            Viewport viewport =
                GraphicsDevice.Viewport;

            Vector3 projected =
                viewport.Project(
                    worldPosition,
                    projection,
                    view,
                    Matrix.Identity);

            if (projected.Z < 0f ||
                projected.Z > 1f)
            {
                return;
            }

            //
            // IMPORTANT:
            //
            // Use the same screen-scale conversion
            // already used by Neffis skill effects
            // such as Power Slash / Fire Slash / Nova.
            //
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

            float spriteScale =
                classicScale *
                neffisScale;

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
            // Texture is owned by TextureLoader cache.
            //
            _texture = null;

            base.Dispose();
        }
    }
}