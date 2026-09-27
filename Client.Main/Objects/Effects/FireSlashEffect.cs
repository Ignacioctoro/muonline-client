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
    public sealed class FireSlashEffect : WorldObject
    {
        private const string SlashTexturePath =
            "Effect/motion_blur_r2.OZJ";

        private const string FireTexturePath =
            "Effect/Fire01.OZJ";

        private const string FlareTexturePath =
            "Effect/Flare.OZJ";

        private const float LifeTime = 0.38f;
        private const float StartDelay = 0.04f;

        private readonly WalkerObject _caster;

        private SpriteBatch? _spriteBatch;

        private Texture2D? _slashTexture;
        private Texture2D? _fireTexture;
        private Texture2D? _flareTexture;

        private float _time;

        public FireSlashEffect(
            WalkerObject caster)
        {
            _caster =
                caster ??
                throw new ArgumentNullException(
                    nameof(caster));
        }

        public override async Task LoadContent()
        {
            await TextureLoader.Instance.Prepare(
                SlashTexturePath);

            await TextureLoader.Instance.Prepare(
                FireTexturePath);

            await TextureLoader.Instance.Prepare(
                FlareTexturePath);

            _slashTexture =
                TextureLoader.Instance.GetTexture2D(
                    SlashTexturePath);

            _fireTexture =
                TextureLoader.Instance.GetTexture2D(
                    FireTexturePath);

            _flareTexture =
                TextureLoader.Instance.GetTexture2D(
                    FlareTexturePath);

            _spriteBatch =
                GraphicsManager.Instance.Sprite;
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            _time +=
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            Position =
                _caster.WorldPosition.Translation;

            if (_time >= LifeTime)
            {
                RemoveSelf();
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (_time < StartDelay ||
                _spriteBatch == null ||
                _slashTexture == null ||
                _fireTexture == null ||
                _flareTexture == null)
            {
                return;
            }

            using (
                new SpriteBatchScope(
                    _spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.Additive,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead))
            {
                DrawSlash();
            }
        }

        private void DrawSlash()
        {
            float progress =
                MathHelper.Clamp(
                    (_time - StartDelay) /
                    (LifeTime - StartDelay),
                    0f,
                    1f);

            float fade =
                1f - progress;

            float angle =
                _caster.Angle.Z +
                MathHelper.Lerp(
                    -1.8f,
                    1.8f,
                    progress);

            //
            // Several points along a circular arc.
            //
            const int segments = 11;
            const float radius = 105f;

            for (int i = 0;
                 i < segments;
                 i++)
            {
                float t =
                    i /
                    (float)(segments - 1);

                float segmentAngle =
                    angle -
                    MathHelper.Lerp(
                        1.35f,
                        0f,
                        t);

                Vector3 worldPosition =
                    Position +
                    new Vector3(
                        MathF.Sin(segmentAngle) *
                        radius,

                        -MathF.Cos(segmentAngle) *
                        radius,

                        55f);

                float alpha =
                    fade *
                    MathHelper.Lerp(
                        0.20f,
                        1f,
                        t);

                float scale =
                    MathHelper.Lerp(
                        0.70f,
                        1.30f,
                        t);

                DrawSprite(
                    _slashTexture!,
                    worldPosition,
                    Color.White,
                    alpha,
                    scale * 3.2f,
                    segmentAngle);

                DrawSprite(
                    _fireTexture!,
                    worldPosition,
                    new Color(
                        255,
                        110,
                        25),
                    alpha * 0.85f,
                    scale * 2.6f,
                    segmentAngle);

                if (i >= segments - 3)
                {
                    DrawSprite(
                        _flareTexture!,
                        worldPosition,
                        new Color(
                            255,
                            220,
                            120),
                        alpha,
                        scale * 3.0f,
                        0f);
                }
            }
        }

        private void DrawSprite(
            Texture2D texture,
            Vector3 worldPosition,
            Color color,
            float alpha,
            float size,
            float rotation)
        {
            if (_spriteBatch == null)
                return;

            Vector3 projected =
                GraphicsDevice.Viewport.Project(
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
                MathF.Max(
                    distance /
                    Constants.TERRAIN_SIZE,
                    0.1f);

            screenScale *=
                Constants.RENDER_SCALE;

            Vector2 origin =
                new Vector2(
                    texture.Width * 0.5f,
                    texture.Height * 0.5f);

            _spriteBatch.Draw(
                texture,
                new Vector2(
                    projected.X,
                    projected.Y),
                null,
                color *
                MathHelper.Clamp(
                    alpha,
                    0f,
                    1f),
                rotation,
                origin,
                size * screenScale,
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