#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Power Slash visual projectile.
    ///
    /// The Magic02 model is loaded but hidden.
    /// The visible effect is built from Shiny02 + flare01
    /// sprites travelling in the Power Slash direction.
    /// </summary>
    public sealed class PowerSlashEffect : ModelObject
    {
        private const string ModelPath =
            "Skill/Magic02.bmd";

        private const string ShinyTexturePath =
            "Effect/Shiny02.OZJ";

        private const string LightTexturePath =
            "Effect/flare01.OZJ";

        private const float MoveSpeed = 1800f;
        private const float MaxLifeTime = 0.30f;

        private readonly WalkerObject _caster;
        private readonly float _angleOffsetDegrees;

        private SpriteBatch? _spriteBatch;
        private Texture2D? _shinyTexture;
        private Texture2D? _lightTexture;

        private Vector3 _direction;

        private float _lifeTime;
        private bool _initialized;

        public PowerSlashEffect(
            WalkerObject caster,
            float angleOffsetDegrees = 0f)
        {
            _caster =
                caster ??
                throw new ArgumentNullException(
                    nameof(caster));

            _angleOffsetDegrees =
                angleOffsetDegrees;

            //
            // Hide Magic02 completely.
            // The visible effect is created with sprites.
            //
            HiddenMesh = -2;

            IsTransparent = true;
            AffectedByTransparency = true;

            BlendState =
                BlendState.Additive;

            DepthState =
                DepthStencilState.DepthRead;

            RenderShadow = false;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -250f,
                        -250f,
                        -100f),
                    new Vector3(
                        250f,
                        250f,
                        250f));
        }

        public override async Task Load()
        {
            Model =
                await BMDLoader.Instance.Prepare(
                    ModelPath);

            await base.Load();
        }

        public override async Task LoadContent()
        {
            await TextureLoader.Instance.Prepare(
                ShinyTexturePath);

            await TextureLoader.Instance.Prepare(
                LightTexturePath);

            _shinyTexture =
                TextureLoader.Instance.GetTexture2D(
                    ShinyTexturePath);

            _lightTexture =
                TextureLoader.Instance.GetTexture2D(
                    LightTexturePath);

            _shinyTexture ??=
                GraphicsManager.Instance.Pixel;

            _lightTexture ??=
                GraphicsManager.Instance.Pixel;

            _spriteBatch =
                GraphicsManager.Instance.Sprite;
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_initialized)
            {
                InitializeEffect();
                _initialized = true;
            }

            float deltaTime =
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            _lifeTime += deltaTime;

            //
            // Projectile movement.
            //
            Position +=
                _direction *
                MoveSpeed *
                deltaTime;

            if (_lifeTime >= MaxLifeTime)
            {
                RemoveSelf();
            }
        }

        private void InitializeEffect()
        {
            Vector3 casterPosition =
                _caster.WorldPosition.Translation;

            float angle =
                _caster.Angle.Z +
                MathHelper.ToRadians(
                    _angleOffsetDegrees);

            Angle =
                new Vector3(
                    _caster.Angle.X,
                    _caster.Angle.Y,
                    angle);

            _direction =
                new Vector3(
                    MathF.Sin(angle),
                    -MathF.Cos(angle),
                    0f);

            if (_direction.LengthSquared() >
                0.0001f)
            {
                _direction.Normalize();
            }

            //
            // Start slightly in front of the character.
            //
            Position =
                casterPosition +
                new Vector3(
                    0f,
                    0f,
                    45f);

            Position +=
                _direction * 45f;
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                _spriteBatch == null ||
                _shinyTexture == null ||
                _lightTexture == null)
            {
                return;
            }

            if (!SpriteBatchScope.BatchIsBegun)
            {
                using (
                    new SpriteBatchScope(
                        _spriteBatch,
                        SpriteSortMode.Deferred,
                        BlendState.Additive,
                        SamplerState.LinearClamp,
                        DepthState))
                {
                    DrawPowerSlashOrbs();
                }
            }
            else
            {
                DrawPowerSlashOrbs();
            }
        }

        private void DrawPowerSlashOrbs()
        {
            //
            // Large main orb.
            //
            DrawOrb(
                Position +
                new Vector3(
                    0f,
                    0f,
                    55f),
                alpha: 1.00f,
                scaleMultiplier: 1.55f);

            //
            // Larger trailing orbs.
            //
            DrawOrb(
                Position -
                _direction * 55f +
                new Vector3(
                    0f,
                    0f,
                    55f),
                alpha: 0.90f,
                scaleMultiplier: 1.35f);

            DrawOrb(
                Position -
                _direction * 110f +
                new Vector3(
                    0f,
                    0f,
                    55f),
                alpha: 0.75f,
                scaleMultiplier: 1.18f);

            DrawOrb(
                Position -
                _direction * 165f +
                new Vector3(
                    0f,
                    0f,
                    55f),
                alpha: 0.58f,
                scaleMultiplier: 1.00f);

            DrawOrb(
                Position -
                _direction * 220f +
                new Vector3(
                    0f,
                    0f,
                    55f),
                alpha: 0.38f,
                scaleMultiplier: 0.85f);
        }

        private void DrawOrb(
            Vector3 worldPosition,
            float alpha,
            float scaleMultiplier)
        {
            if (_spriteBatch == null ||
                _shinyTexture == null ||
                _lightTexture == null)
            {
                return;
            }

            var viewport =
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

            float screenScale =
                1f /
                (
                    MathF.Max(
                        distance,
                        0.1f)
                    /
                    Constants.TERRAIN_SIZE
                );

            screenScale *=
                Constants.RENDER_SCALE;

            Vector2 screenPosition =
                new Vector2(
                    projected.X,
                    projected.Y);

            float depth =
                MathHelper.Clamp(
                    projected.Z,
                    0f,
                    1f);

            float lifeRatio =
                MathHelper.Clamp(
                    1f -
                    (_lifeTime / MaxLifeTime),
                    0f,
                    1f);

            alpha *=
                MathHelper.Lerp(
                    0.35f,
                    1f,
                    lifeRatio);

            //
            // Main shiny layer.
            //
            Vector2 shinyOrigin =
                new Vector2(
                    _shinyTexture.Width * 0.5f,
                    _shinyTexture.Height * 0.5f);

            float shinyScale = 11.4f *
                scaleMultiplier *
                screenScale;

            _spriteBatch.Draw(
                _shinyTexture,
                screenPosition,
                null,
                Color.White * alpha,
                _lifeTime * 8f,
                shinyOrigin,
                shinyScale,
                SpriteEffects.None,
                depth);

            //
            // Second shiny layer.
            //
            _spriteBatch.Draw(
                _shinyTexture,
                screenPosition,
                null,
                Color.White *
                (alpha * 0.75f),
                -_lifeTime * 10f,
                shinyOrigin,
                shinyScale * 0.82f,
                SpriteEffects.None,
                depth);

            //
            // Large halo.
            //
            Vector2 lightOrigin =
                new Vector2(
                    _lightTexture.Width * 0.5f,
                    _lightTexture.Height * 0.5f);

            Color haloColor =
                new Color(
                    185,
                    220,
                    255);

            float lightScale =
                14.8f *
                scaleMultiplier *
                screenScale;

            _spriteBatch.Draw(
                _lightTexture,
                screenPosition,
                null,
                haloColor *
                (alpha * 0.70f),
                0f,
                lightOrigin,
                lightScale,
                SpriteEffects.None,
                depth);
        }

        private void RemoveSelf()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(this);
            }
            else
            {
                World?.RemoveObject(this);
            }

            Dispose();
        }
    }
}