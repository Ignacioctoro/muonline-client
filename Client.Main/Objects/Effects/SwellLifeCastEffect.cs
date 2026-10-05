#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Classic Greater Fortitude / Swell Life cast visual.
    ///
    /// Original Main:
    ///
    /// 36 x BITMAP_JOINT_SPIRIT subtype 2
    /// Angle.Z = i * 10
    /// Angle.X = -10
    /// Position.Z += 100
    /// Scale = 60
    ///
    /// plus exactly two:
    /// BITMAP_MAGIC + 1 subtype 4
    ///
    /// Color for SPIRIT subtype 2:
    /// (1.0, 0.5, 0.1)
    /// </summary>
    public sealed class SwellLifeCastEffect
        : EffectObject
    {
        private const string SpiritTexturePath =
            "Effect/JointSpirit01.jpg";

        private const string MagicTexturePath =
            "Effect/Magic_Ground2.jpg";

        private const string FlareTexturePath =
            "Effect/flare01.jpg";

        private const int RayCount =
            36;

        //
        // The effect is short and explosive.
        //
        private const float TotalDuration =
            0.82f;

        private const float BurstStart =
            0.16f;

        private readonly WalkerObject _caster;

        private Texture2D? _spiritTexture;

        private Texture2D? _magicTexture;

        private Texture2D? _flareTexture;

        private SpriteBatch? _spriteBatch;

        private float _time;

        private readonly RayState[] _rays =
            new RayState[RayCount];

        private struct RayState
        {
            public float Angle;

            public float RandomScale;

            public float Rotation;
        }

        public SwellLifeCastEffect(
            WalkerObject caster)
        {
            _caster =
                caster ??
                throw new ArgumentNullException(
                    nameof(caster));

            IsTransparent = true;

            AffectedByTransparency = true;

            BlendState =
                BlendState.Additive;

            DepthState =
                GraphicsManager.ReadOnlyDepth;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -450f,
                        -450f,
                        -100f),
                    new Vector3(
                        450f,
                        450f,
                        420f));

            for (int i = 0;
                 i < RayCount;
                 i++)
            {
                _rays[i] =
                    new RayState
                    {
                        Angle =
                            MathHelper.ToRadians(
                                i * 10f),

                        RandomScale =
                            0.90f +
                            (float)
                            MuGame.Random
                                .NextDouble() *
                            0.20f,

                        Rotation =
                            MathHelper.ToRadians(
                                MuGame.Random.Next(
                                    0,
                                    360))
                    };
            }
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await TextureLoader.Instance.Prepare(
                SpiritTexturePath);

            await TextureLoader.Instance.Prepare(
                MagicTexturePath);

            await TextureLoader.Instance.Prepare(
                FlareTexturePath);

            _spiritTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        SpiritTexturePath);

            _magicTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        MagicTexturePath);

            _flareTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        FlareTexturePath);

            _spiritTexture ??=
                GraphicsManager.Instance.Pixel;

            _magicTexture ??=
                GraphicsManager.Instance.Pixel;

            _flareTexture ??=
                GraphicsManager.Instance.Pixel;

            _spriteBatch =
                GraphicsManager.Instance.Sprite;
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (_caster.Status ==
                    GameControlStatus.Disposed ||
                _caster.World == null)
            {
                RemoveSelf();
                return;
            }

            Position =
                _caster.WorldPosition
                    .Translation;

            _time +=
                (float)
                gameTime.ElapsedGameTime
                    .TotalSeconds;

            if (_time >=
                TotalDuration)
            {
                RemoveSelf();
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                _spriteBatch == null)
            {
                return;
            }

            using (
                new SpriteBatchScope(
                    _spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.Additive,
                    SamplerState.LinearClamp,
                    GraphicsManager.ReadOnlyDepth,
                    RasterizerState.CullNone))
            {
                DrawCastVisual();
            }
        }

        private void DrawCastVisual()
        {
            //
            // Short initial charge.
            //
            if (_time <
                BurstStart)
            {
                DrawCharge();

                return;
            }

            float burstTime =
                _time -
                BurstStart;

            float burstDuration =
                TotalDuration -
                BurstStart;

            float progress =
                MathHelper.Clamp(
                    burstTime /
                    burstDuration,
                    0f,
                    1f);

            DrawMagicPulse(
                progress,
                0f);

            //
            // Original code creates its second
            // BITMAP_MAGIC+1 when i == 20.
            //
            DrawMagicPulse(
                progress,
                0.12f);

            DrawCentralBlast(
                progress);

            DrawSpiritExplosion(
                progress);
        }

        private void DrawCharge()
        {
            if (_flareTexture == null)
            {
                return;
            }

            float progress =
                MathHelper.Clamp(
                    _time /
                    BurstStart,
                    0f,
                    1f);

            Vector3 center =
                _caster.WorldPosition
                    .Translation;

            center.Z +=
                100f;

            //
            // Pulling light inward before the release.
            //
            for (int i = 0;
                 i < 8;
                 i++)
            {
                float angle =
                    MathHelper.TwoPi *
                    i /
                    8f;

                float radius =
                    MathHelper.Lerp(
                        95f,
                        10f,
                        progress);

                Vector3 pos =
                    center +
                    new Vector3(
                        MathF.Sin(angle) *
                            radius,
                        MathF.Cos(angle) *
                            radius,
                        0f);

                DrawBillboard(
                    _flareTexture,
                    pos,
                    new Vector3(
                        1f,
                        0.48f,
                        0.08f),
                    0.42f *
                        progress,
                    0.65f,
                    angle);
            }

            DrawBillboard(
                _flareTexture,
                center,
                new Vector3(
                    1f,
                    0.6f,
                    0.12f),
                progress *
                    0.85f,
                MathHelper.Lerp(
                    0.7f,
                    1.55f,
                    progress),
                0f);
        }

        private void DrawSpiritExplosion(
            float progress)
        {
            if (_spiritTexture == null)
            {
                return;
            }

            Vector3 center =
                _caster.WorldPosition
                    .Translation;

            center.Z +=
                100f;

            //
            // Original joint subtype 2 has Velocity = 50,
            // Scale = 60 and only a few trail segments.
            //
            // We reproduce that appearance as a short,
            // rapidly expanding radial streak.
            //
            float radius =
            EaseOut(
                progress) *
            420f;

            float fade =
                1f -
                progress;

            fade =
                MathF.Sqrt(
                    MathHelper.Clamp(
                        fade,
                        0f,
                        1f));

            for (int i = 0;
                 i < RayCount;
                 i++)
            {
                RayState ray =
                    _rays[i];

                Vector3 dir =
                    new Vector3(
                        MathF.Sin(
                            ray.Angle),

                        MathF.Cos(
                            ray.Angle),

                        0f);

                Vector3 start =
                    center +
                    dir *
                    MathF.Max(
                        0f,
                        radius -
                        185f);

                Vector3 end =
                    center +
                    dir *
                    (
                        radius +
                        55f
                    );

                //
                // Original Angle.X = -10 degrees.
                //
                // Slight rise gives the same outward-up
                // perspective instead of a perfectly flat ring.
                //
                start.Z +=
                    progress *
                    15f;

                end.Z +=
                    30f +
                    progress *
                    45f;

                float width =
                    46f *
                    ray.RandomScale *
                    MathHelper.Lerp(
                        1.20f,
                        0.58f,
                        progress);

                Vector3 outer =
                    new Vector3(
                        1f,
                        0.36f,
                        0.04f);

                Vector3 core =
                    new Vector3(
                        1f,
                        0.78f,
                        0.30f);

                DrawTrail(
                    start,
                    end,
                    outer,
                    fade *
                        0.85f,
                    width);

                DrawTrail(
                    start,
                    end,
                    core,
                    fade *
                        0.95f,
                    width *
                        0.35f);

                DrawBillboard(
                    _flareTexture!,
                    end,
                    core,
                    fade *
                        0.75f,
                    0.55f *
                        ray.RandomScale,
                    ray.Rotation +
                        progress *
                        2f);
            }
        }

        private void DrawCentralBlast(
            float progress)
        {
            if (_flareTexture == null)
            {
                return;
            }

            Vector3 center =
                _caster.WorldPosition
                    .Translation;

            center.Z +=
                95f;

            //
            // Strong flash at the instant all 36
            // SPIRIT joints are released.
            //
            float early =
                MathHelper.Clamp(
                    progress /
                    0.30f,
                    0f,
                    1f);

            float alpha =
                1f -
                early;

            alpha *= alpha;

            DrawBillboard(
                _flareTexture,
                center,
                new Vector3(
                    1f,
                    0.72f,
                    0.20f),
                alpha,
                MathHelper.Lerp(
                    3.2f,
                    6.0f,
                    early),
                progress *
                    3f);

            DrawBillboard(
                _flareTexture,
                center,
                Vector3.One,
                alpha *
                    0.60f,
                MathHelper.Lerp(
                    2.5f,
                    5.0f,
                    early),
                -progress *
                    4f);
        }

        private void DrawMagicPulse(
            float progress,
            float delay)
        {
            if (_magicTexture == null)
            {
                return;
            }

            float local =
                MathHelper.Clamp(
                    (
                        progress -
                        delay
                    ) /
                    MathF.Max(
                        1f -
                        delay,
                        0.001f),
                    0f,
                    1f);

            if (local <= 0f)
            {
                return;
            }

            float alpha =
                MathF.Sin(
                    local *
                    MathHelper.Pi);

            float scale =
                MathHelper.Lerp(
                    1.6f,
                    6.2f,
                    EaseOut(
                        local));

            Vector3 center =
                _caster.WorldPosition
                    .Translation;

            center.Z +=
                4f;

            DrawGroundQuad(
                _magicTexture,
                center,
                new Vector3(
                    1f,
                    0.5f,
                    0.1f),
                alpha *
                    0.70f,
                scale,
                local *
                    MathHelper.TwoPi);
        }

        private void DrawTrail(
            Vector3 start,
            Vector3 end,
            Vector3 light,
            float alpha,
            float width)
        {
            if (_spriteBatch == null ||
                _spiritTexture == null)
            {
                return;
            }

            Viewport viewport =
                GraphicsDevice.Viewport;

            Vector3 s0 =
                viewport.Project(
                    start,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            Vector3 s1 =
                viewport.Project(
                    end,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            if (s0.Z < 0f ||
                s0.Z > 1f ||
                s1.Z < 0f ||
                s1.Z > 1f)
            {
                return;
            }

            Vector2 p0 =
                new Vector2(
                    s0.X,
                    s0.Y);

            Vector2 p1 =
                new Vector2(
                    s1.X,
                    s1.Y);

            Vector2 delta =
                p1 -
                p0;

            float length =
                delta.Length();

            if (length <
                0.5f)
            {
                return;
            }

            float rotation =
                MathF.Atan2(
                    delta.Y,
                    delta.X);

            float screenScale =
                ComputeScreenScale(
                    start);

            Vector2 scale =
                new Vector2(
                    length /
                    MathF.Max(
                        _spiritTexture.Width,
                        1),

                    width *
                    screenScale /
                    MathF.Max(
                        _spiritTexture.Height,
                        1));

            _spriteBatch.Draw(
                _spiritTexture,
                p0,
                null,
                new Color(light) *
                    alpha,
                rotation,
                new Vector2(
                    0f,
                    _spiritTexture.Height *
                        0.5f),
                scale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    s0.Z,
                    0f,
                    1f));
        }

        private void DrawBillboard(
            Texture2D texture,
            Vector3 worldPosition,
            Vector3 light,
            float alpha,
            float scale,
            float rotation)
        {
            if (_spriteBatch == null ||
                alpha <= 0.01f)
            {
                return;
            }

            Vector3 projected =
                GraphicsDevice.Viewport
                    .Project(
                        worldPosition,
                        Camera.Instance
                            .Projection,
                        Camera.Instance.View,
                        Matrix.Identity);

            if (projected.Z < 0f ||
                projected.Z > 1f)
            {
                return;
            }

            float screenScale =
                ComputeScreenScale(
                    worldPosition);

            Vector2 origin =
                new Vector2(
                    texture.Width *
                        0.5f,
                    texture.Height *
                        0.5f);

            _spriteBatch.Draw(
                texture,
                new Vector2(
                    projected.X,
                    projected.Y),
                null,
                new Color(light) *
                    MathHelper.Clamp(
                        alpha,
                        0f,
                        1f),
                rotation,
                origin,
                scale *
                    screenScale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    projected.Z,
                    0f,
                    1f));
        }

        private void DrawGroundQuad(
            Texture2D texture,
            Vector3 position,
            Vector3 light,
            float alpha,
            float scale,
            float rotation)
        {
            if (alpha <= 0.01f)
            {
                return;
            }

            GraphicsDevice gd =
                GraphicsManager.Instance
                    .GraphicsDevice;

            AlphaTestEffect effect =
                GraphicsManager.Instance
                    .AlphaTestEffect3D;

            BlendState oldBlend =
                gd.BlendState;

            DepthStencilState oldDepth =
                gd.DepthStencilState;

            Texture2D oldTexture =
                effect.Texture;

            Vector3 oldDiffuse =
                effect.DiffuseColor;

            float oldAlpha =
                effect.Alpha;

            bool oldVertexColor =
                effect.VertexColorEnabled;

            try
            {
                effect.World =
                    Matrix.CreateScale(
                        scale *
                        82f) *
                    Matrix.CreateRotationX(
                        -MathHelper.PiOver2) *
                    Matrix.CreateRotationZ(
                        rotation) *
                    Matrix.CreateTranslation(
                        position);

                effect.View =
                    Camera.Instance.View;

                effect.Projection =
                    Camera.Instance
                        .Projection;

                effect.Texture =
                    texture;

                effect.VertexColorEnabled =
                    false;

                effect.DiffuseColor =
                    light;

                effect.Alpha =
                    alpha;

                gd.BlendState =
                    BlendState.Additive;

                gd.DepthStencilState =
                    GraphicsManager
                        .ReadOnlyDepth;

                VertexPositionTexture[] vertices =
                {
                    new(
                        new Vector3(
                            -1f,
                            0f,
                            -1f),
                        new Vector2(
                            0f,
                            0f)),

                    new(
                        new Vector3(
                            1f,
                            0f,
                            -1f),
                        new Vector2(
                            1f,
                            0f)),

                    new(
                        new Vector3(
                            -1f,
                            0f,
                            1f),
                        new Vector2(
                            0f,
                            1f)),

                    new(
                        new Vector3(
                            1f,
                            0f,
                            1f),
                        new Vector2(
                            1f,
                            1f))
                };

                short[] indices =
                {
                    0, 1, 2,
                    2, 1, 3
                };

                foreach (
                    EffectPass pass
                    in effect
                        .CurrentTechnique
                        .Passes)
                {
                    pass.Apply();

                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        vertices,
                        0,
                        4,
                        indices,
                        0,
                        2);
                }
            }
            finally
            {
                effect.Texture =
                    oldTexture;

                effect.DiffuseColor =
                    oldDiffuse;

                effect.Alpha =
                    oldAlpha;

                effect.VertexColorEnabled =
                    oldVertexColor;

                gd.BlendState =
                    oldBlend;

                gd.DepthStencilState =
                    oldDepth;
            }
        }

        private static float EaseOut(
            float value)
        {
            value =
                MathHelper.Clamp(
                    value,
                    0f,
                    1f);

            float inv =
                1f -
                value;

            return 1f -
                inv *
                inv *
                inv;
        }

        private static float ComputeScreenScale(
            Vector3 worldPosition)
        {
            float distance =
                Vector3.Distance(
                    Camera.Instance.Position,
                    worldPosition);

            float scale =
                1f /
                MathF.Max(
                    distance /
                    Constants.TERRAIN_SIZE,
                    0.1f);

            return
                scale *
                Constants.RENDER_SCALE;
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
            //
            // Textures belong to TextureLoader.
            //
            _spiritTexture = null;
            _magicTexture = null;
            _flareTexture = null;

            base.Dispose();
        }
    }
}