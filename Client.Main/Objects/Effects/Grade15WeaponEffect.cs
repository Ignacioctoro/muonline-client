#nullable enable

using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Classic external effects for +15 weapons.
    ///
    /// Ported from NextGradeObjectRender():
    /// - 3x BITMAP_LIGHT / flare01
    /// - 1x BITMAP_MAGIC / Magic_Ground1
    /// - 1x BITMAP_FLARE_RED / flareRed
    /// - BITMAP_LIGHTNING_MEGA1..3 particles
    ///
    /// Important compatibility detail:
    /// the effect object remains a child of WeaponObject so PlayerObject does
    /// not need extra references/fields. However, its render anchor is taken
    /// from PlayerObject's hand bone, not from WeaponObject.WorldPosition.
    /// This reproduces the classic behavior when this client visually holsters
    /// the weapon on the back in a safe zone: the +15 external effect remains
    /// on the hand.
    ///
    /// This class does NOT modify the weapon material/glow.
    /// </summary>
    public sealed class Grade15WeaponEffect : WorldObject
    {
        // MU's old particle logic uses a 25 FPS reference cadence.
        private const float ClassicReferenceFps = 25.0f;
        private const float LightningSpawnInterval = 1.0f / ClassicReferenceFps;

        // Original lightning particle: LifeTime = 5 reference frames.
        private const float LightningLifetime = 5.0f / ClassicReferenceFps;

        private static readonly BlendState MuBrightAdditive =
            new BlendState
            {
                ColorBlendFunction = BlendFunction.Add,
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Add,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.One
            };

        // Persistent classic sprites.
        private readonly ClassicWeaponSprite _light0;
        private readonly ClassicWeaponSprite _light10;
        private readonly ClassicWeaponSprite _light20;
        private readonly ClassicWeaponSprite _magic;
        private readonly ClassicWeaponSprite _flareRed;

        // lightning_mega particle pool.
        private const int LightningPoolSize = 15;

        private readonly ClassicLightningSprite[] _lightningPool =
            new ClassicLightningSprite[LightningPoolSize];

        private float _lightningSpawnAccumulator;

        // Current PLAYER hand transform. Deliberately independent from the
        // visual WeaponObject transform so safe-zone holstering does not move
        // the effect to the back.
        private Matrix _handWorld = Matrix.Identity;
        private bool _hasHandWorld;

        /// <summary>
        /// Parameterless on purpose. PlayerObject can keep:
        ///
        /// Weapon1.Children.Add(new Grade15WeaponEffect());
        /// Weapon2.Children.Add(new Grade15WeaponEffect());
        ///
        /// The hand side is inferred from which WeaponObject owns this effect.
        /// </summary>
        public Grade15WeaponEffect()
        {
            Hidden = true;
            Interactive = false;

            // ------------------------------------------------------------
            // 3x BITMAP_LIGHT / flare01
            // Classic offsets: X = 0, 10, 20
            // Light = (1.0, 0.6, 0.0)
            // Scale = 0.6
            // ------------------------------------------------------------
            Vector3 orange = new Vector3(1.0f, 0.6f, 0.0f);

            _light0 =
                new ClassicWeaponSprite(
                    "Effect/flare01.jpg",
                    0.60f,
                    orange)
                {
                    Position = new Vector3(0.0f, 0.0f, 0.0f)
                };

            _light10 =
                new ClassicWeaponSprite(
                    "Effect/flare01.jpg",
                    0.60f,
                    orange)
                {
                    Position = new Vector3(10.0f, 0.0f, 0.0f)
                };

            _light20 =
                new ClassicWeaponSprite(
                    "Effect/flare01.jpg",
                    0.60f,
                    orange)
                {
                    Position = new Vector3(20.0f, 0.0f, 0.0f)
                };

            // ------------------------------------------------------------
            // BITMAP_MAGIC / Magic_Ground1
            // Classic offset: (10, 0, 0)
            // Scale = 0.35
            // Color is animated in Update().
            // ------------------------------------------------------------
            _magic =
                new ClassicWeaponSprite(
                    "Effect/Magic_Ground1.jpg",
                    0.35f,
                    new Vector3(0.3f, 0.1f, 0.0f))
                {
                    Position = new Vector3(10.0f, 0.0f, 0.0f)
                };

            // ------------------------------------------------------------
            // BITMAP_FLARE_RED / flareRed
            // Classic offset: (10, 0, 0)
            // Scale changes every frame.
            // ------------------------------------------------------------
            _flareRed =
                new ClassicWeaponSprite(
                    "Effect/flareRed.jpg",
                    0.40f,
                    Vector3.One)
                {
                    Position = new Vector3(10.0f, 0.0f, 0.0f)
                };

            Children.Add(_light0);
            Children.Add(_light10);
            Children.Add(_light20);
            Children.Add(_magic);
            Children.Add(_flareRed);

            // ------------------------------------------------------------
            // BITMAP_LIGHTNING_MEGA1 + rand()%3
            // Reusable pool instead of allocating every frame.
            // ------------------------------------------------------------
            for (int i = 0; i < LightningPoolSize; i++)
            {
                int textureIndex = i % 3;

                string path =
                    textureIndex switch
                    {
                        0 => "Effect/lighting_mega01.jpg",
                        1 => "Effect/lighting_mega02.jpg",
                        _ => "Effect/lighting_mega03.jpg"
                    };

                var lightning =
                    new ClassicLightningSprite(path)
                    {
                        Hidden = true
                    };

                _lightningPool[i] = lightning;
                Children.Add(lightning);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // This object remains a child of the actual WeaponObject.
            if (Parent is not WeaponObject weapon ||
                weapon.Parent is not PlayerObject player)
            {
                DisableEffect();
                base.Update(gameTime);
                return;
            }

            bool enabled =
                weapon.Model != null &&
                weapon.ItemLevel >= 15;

            if (!enabled)
            {
                DisableEffect();
                base.Update(gameTime);
                return;
            }

            // Identify which equipment slot owns this effect.
            bool isLeftHand;

            if (ReferenceEquals(player.Weapon1, weapon))
            {
                isLeftHand = true;
            }
            else if (ReferenceEquals(player.Weapon2, weapon))
            {
                isLeftHand = false;
            }
            else
            {
                DisableEffect();
                base.Update(gameTime);
                return;
            }

            // ORIGINAL-BEHAVIOR FIX:
            // use the PLAYER hand transform rather than the current visual
            // WeaponObject transform. This keeps the +15 external effect on
            // the hands even when WeaponObject is moved to the back.
            _hasHandWorld =
                player.TryGetHandWorldMatrix(
                    isLeftHand,
                    out _handWorld);

            if (!_hasHandWorld)
            {
                DisableEffect();
                base.Update(gameTime);
                return;
            }

            Hidden = false;

            // Persistent sprites are transformed from the player hand.
            _light0.AnchorMatrix = _handWorld;
            _light10.AnchorMatrix = _handWorld;
            _light20.AnchorMatrix = _handWorld;
            _magic.AnchorMatrix = _handWorld;
            _flareRed.AnchorMatrix = _handWorld;

            float timeSeconds =
                (float)gameTime.TotalGameTime.TotalSeconds;

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Original:
            // abs(sin(WorldTime * 0.002f))
            // WorldTime is milliseconds -> seconds multiplier = 2.0.
            float fLight2 =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds * 2.0f));

            _magic.Light =
                new Vector3(
                    0.7f * fLight2 + 0.3f,
                    0.1f * fLight2 + 0.1f,
                    0.0f);

            // Original:
            // fScale = (rand() % 60) * 0.01f;
            // flare scale = 0.6f * fScale + 0.4f;
            float flareRandom =
                MuGame.Random.Next(60) * 0.01f;

            _flareRed.Scale =
                0.6f * flareRandom + 0.4f;

            // Original uses CreateParticleFpsChecked(). Reproduce the classic
            // 25 Hz reference cadence explicitly.
            _lightningSpawnAccumulator += dt;

            int safety = 0;

            while (_lightningSpawnAccumulator >= LightningSpawnInterval &&
                   safety < 3)
            {
                _lightningSpawnAccumulator -= LightningSpawnInterval;
                SpawnClassicLightning();
                safety++;
            }

            base.Update(gameTime);
        }

        private void DisableEffect()
        {
            Hidden = true;
            _hasHandWorld = false;
            _lightningSpawnAccumulator = 0.0f;

            for (int i = 0; i < _lightningPool.Length; i++)
            {
                _lightningPool[i].Stop();
            }
        }

        // ================================================================
        // Lightning
        // ================================================================

        private void SpawnClassicLightning()
        {
            if (!_hasHandWorld)
            {
                return;
            }

            // Original:
            // (rand() % 80 + 10) * 0.01f -> 0.10 .. 0.89
            float scale =
                MuGame.Random.Next(10, 90) * 0.01f;

            float rotation =
                (float)MuGame.Random.NextDouble() * MathHelper.TwoPi;

            // BITMAP_LIGHTNING_MEGA1 + rand()%3
            int wantedTexture =
                MuGame.Random.Next(3);

            ClassicLightningSprite? candidate = null;

            for (int i = 0; i < _lightningPool.Length; i++)
            {
                var particle = _lightningPool[i];

                if (particle.TextureVariant != wantedTexture)
                {
                    continue;
                }

                if (!particle.IsActive)
                {
                    candidate = particle;
                    break;
                }
            }

            // All matching pool slots are active: recycle one of that variant.
            if (candidate == null)
            {
                int variantSlot =
                    MuGame.Random.Next(
                        LightningPoolSize / 3);

                candidate =
                    _lightningPool[
                        wantedTexture +
                        3 * variantSlot];
            }

            // Classic particle is born from the secondary weapon-effect point
            // and then lives in WORLD space for its short lifetime.
            Vector3 spawnWorldPosition =
                Vector3.Transform(
                    new Vector3(10.0f, 0.0f, 0.0f),
                    _handWorld);

            candidate.Trigger(
                spawnWorldPosition,
                scale,
                rotation);
        }

        // ================================================================
        // Classic sprite renderer
        // ================================================================

        private class ClassicWeaponSprite : SpriteObject
        {
            private readonly string _texturePath;

            public override string TexturePath =>
                _texturePath;

            /// <summary>
            /// Hand-space anchor used for persistent +15 weapon sprites.
            /// </summary>
            public Matrix AnchorMatrix
            {
                get;
                set;
            } = Matrix.Identity;

            /// <summary>
            /// Explicit billboard rotation. Classic CreateSprite() does not
            /// inherit the model's Z rotation.
            /// </summary>
            public float SpriteRotation
            {
                get;
                set;
            }

            public ClassicWeaponSprite(
                string texturePath,
                float scale,
                Vector3 light)
            {
                _texturePath = texturePath;

                Scale = scale;
                Light = light;
                LightEnabled = true;
                IsTransparent = true;
                AffectedByTransparency = true;
                BlendState = MuBrightAdditive;
                DepthState = DepthStencilState.DepthRead;
                Interactive = false;
            }

            protected virtual Vector3 GetRenderWorldPosition()
            {
                return Vector3.Transform(
                    Position,
                    AnchorMatrix);
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible ||
                    SpriteTexture == null)
                {
                    return;
                }

                Vector3 worldPosition =
                    GetRenderWorldPosition();

                Matrix view =
                    Camera.Instance.View;

                Matrix projection =
                    Camera.Instance.Projection;

                Vector3 cameraPosition =
                    Vector3.Transform(
                        worldPosition,
                        view);

                // XNA CreateLookAt is right-handed; visible geometry is along
                // negative camera Z.
                float cameraDepth =
                    -cameraPosition.Z;

                if (cameraDepth <= Camera.Instance.ViewNear)
                {
                    return;
                }

                var viewport =
                    GraphicsDevice.Viewport;

                Vector3 projected =
                    viewport.Project(
                        worldPosition,
                        projection,
                        view,
                        Matrix.Identity);

                if (projected.Z < 0.0f ||
                    projected.Z > 1.0f)
                {
                    return;
                }

                // Classic MU uses:
                // Width  = texture.Width  * Scale
                // Height = texture.Height * Scale
                // in camera space. Convert one camera-space unit to pixels at
                // the current depth so its visual size matches that renderer.
                float pixelsPerCameraUnit =
                    viewport.Height *
                    MathF.Abs(projection.M22) /
                    (2.0f * cameraDepth);

                float spriteScale =
                    Scale * pixelsPerCameraUnit;

                if (!float.IsFinite(spriteScale) ||
                    spriteScale <= 0.0f)
                {
                    return;
                }

                Color color =
                    LightEnabled
                        ? new Color(Light) * TotalAlpha
                        : Color.White * TotalAlpha;

                float depth =
                    MathHelper.Clamp(
                        projected.Z,
                        0.0f,
                        1.0f);

                Vector2 origin =
                    new Vector2(
                        SpriteTexture.Width * 0.5f,
                        SpriteTexture.Height * 0.5f);

                // Original MU uses linear texture filtering for these sprites.
                using (
                    new SpriteBatchScope(
                        SpriteBatch,
                        SpriteSortMode.Deferred,
                        BlendState,
                        SamplerState.LinearClamp,
                        DepthState,
                        RasterizerState.CullNone))
                {
                    SpriteBatch.Draw(
                        SpriteTexture,
                        new Vector2(
                            projected.X,
                            projected.Y),
                        null,
                        color,
                        SpriteRotation,
                        origin,
                        spriteScale,
                        SpriteEffects.None,
                        depth);
                }
            }
        }

        // ================================================================
        // Classic BITMAP_LIGHTNING_MEGA particle
        // ================================================================

        private sealed class ClassicLightningSprite : ClassicWeaponSprite
        {
            private readonly Vector3 _baseLight =
                new Vector3(
                    1.0f,
                    0.2f,
                    0.0f);

            private Vector3 _spawnWorldPosition;
            private float _life;

            public bool IsActive =>
                !Hidden;

            public int TextureVariant
            {
                get;
            }

            public ClassicLightningSprite(
                string texturePath)
                : base(
                    texturePath,
                    0.10f,
                    Vector3.Zero)
            {
                if (texturePath.Contains(
                        "mega01",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TextureVariant = 0;
                }
                else if (texturePath.Contains(
                             "mega02",
                             StringComparison.OrdinalIgnoreCase))
                {
                    TextureVariant = 1;
                }
                else
                {
                    TextureVariant = 2;
                }

                Hidden = true;
            }

            public void Trigger(
                Vector3 worldPosition,
                float scale,
                float rotation)
            {
                _spawnWorldPosition =
                    worldPosition;

                _life =
                    LightningLifetime;

                Scale =
                    scale;

                SpriteRotation =
                    rotation;

                Light =
                    _baseLight;

                Alpha =
                    1.0f;

                Hidden =
                    false;
            }

            public void Stop()
            {
                _life = 0.0f;
                Hidden = true;
            }

            protected override Vector3 GetRenderWorldPosition()
            {
                return _spawnWorldPosition;
            }

            public override void Update(GameTime gameTime)
            {
                if (Hidden)
                {
                    return;
                }

                float dt =
                    (float)gameTime.ElapsedGameTime.TotalSeconds;

                _life -= dt;

                if (_life <= 0.0f)
                {
                    Stop();
                    return;
                }

                float progress =
                    1.0f -
                    _life /
                    LightningLifetime;

                // Original subtype 0 starts at alpha 1 and subtracts roughly
                // 0.15 per reference frame. Over five frames this ends near
                // 0.25 before disappearing.
                float luminosity =
                    1.0f -
                    progress * 0.75f;

                luminosity =
                    MathHelper.Clamp(
                        luminosity,
                        0.25f,
                        1.0f);

                Light =
                    _baseLight * luminosity;

                base.Update(gameTime);
            }
        }
    }
}
