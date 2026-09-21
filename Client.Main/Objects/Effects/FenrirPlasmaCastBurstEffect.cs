#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Electrical burst that surrounds the Fenrir when
    /// Plasma Storm is cast.
    ///
    /// Based on the six BITMAP_FLARE_FORCE joints created
    /// by the original MU client.
    /// </summary>
    public sealed class FenrirPlasmaCastBurstEffect : EffectObject
    {
        private const string ThunderTexturePath =
            "Effect/JointThunder01.OZJ";

        private const int ArcCount = 6;
        private const int SegmentCount = 10;

        private const float Duration = 0.48f;

        private readonly Func<Matrix> _vehicleMatrixProvider;

        private readonly Vector2[,] _screenPoints =
            new Vector2[ArcCount, SegmentCount + 1];

        private readonly float[,] _screenDepths =
            new float[ArcCount, SegmentCount + 1];

        private readonly float[,] _jitter =
            new float[ArcCount, SegmentCount + 1];

        private readonly float[] _baseAngles =
            new float[ArcCount];

        private readonly float[] _verticalOffsets =
            new float[ArcCount];

        private readonly Quaternion[] _planes =
            new Quaternion[ArcCount];

        private readonly Vector3 _lightColor;

        private Texture2D? _texture;
        private SpriteBatch? _spriteBatch;

        private float _remaining = Duration;
        private float _time;
        private float _reshapeTimer;

        public FenrirPlasmaCastBurstEffect(
            Func<Matrix> vehicleMatrixProvider,
            short fenrirItemIndex)
        {
            _vehicleMatrixProvider =
                vehicleMatrixProvider ??
                throw new ArgumentNullException(
                    nameof(vehicleMatrixProvider));

            _lightColor =
                GetFenrirBurstColor(
                    fenrirItemIndex);

            IsTransparent = true;
            AffectedByTransparency = true;

            BlendState =
                BlendState.Additive;

            DepthState =
                DepthStencilState.DepthRead;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(-180f),
                    new Vector3(180f));

            InitializeArcs();
        }

        private void InitializeArcs()
        {
            for (int arc = 0;
                 arc < ArcCount;
                 arc++)
            {
                //
                // Original:
                //
                // 10 + (rand() % 40 - 20)
                //
                // = -10 .. +29
                //
                _verticalOffsets[arc] =
                    Random.Shared.Next(40) - 10f;

                _baseAngles[arc] =
                    Random.Shared.NextSingle()
                    * MathHelper.TwoPi;

                //
                // Original joints receive random XYZ angles.
                // Give every electrical arc its own plane.
                //
                float pitch =
                    MathHelper.ToRadians(
                        Random.Shared.Next(
                            -50,
                            51));

                float yaw =
                    MathHelper.ToRadians(
                        Random.Shared.Next(
                            0,
                            360));

                float roll =
                    MathHelper.ToRadians(
                        Random.Shared.Next(
                            0,
                            360));

                _planes[arc] =
                    Quaternion.CreateFromYawPitchRoll(
                        yaw,
                        pitch,
                        roll);

                for (int i = 0;
                     i <= SegmentCount;
                     i++)
                {
                    _jitter[arc, i] =
                        Random.Shared.NextSingle()
                        * 2f - 1f;
                }
            }
        }

        private static Vector3 GetFenrirBurstColor(
            short itemIndex)
        {
            return itemIndex switch
            {
                // Black
                11 or 15 =>
                    new Vector3(
                        0.7f,
                        1.0f,
                        0.7f),

                // Blue
                12 or 16 =>
                    new Vector3(
                        0.7f,
                        0.7f,
                        1.0f),

                // Gold
                13 or 17 =>
                    new Vector3(
                        1.0f,
                        0.85f,
                        0.3f),

                // Red
                14 or 18 =>
                    new Vector3(
                        1.0f,
                        0.6f,
                        0.6f),

                _ =>
                    Vector3.One
            };
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await TextureLoader.Instance.Prepare(
                ThunderTexturePath);

            _texture =
                TextureLoader.Instance.GetTexture2D(
                    ThunderTexturePath);

            _texture ??=
                GraphicsManager.Instance.Pixel;

            _spriteBatch =
                GraphicsManager.Instance.Sprite;
        }

        public override void Update(
            GameTime gameTime)
        {
            if (Status ==
                GameControlStatus.NonInitialized)
            {
                _ = Load();
            }

            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }

            float dt =
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            _remaining -= dt;
            _time += dt;

            if (_remaining <= 0f)
            {
                World?.RemoveObject(this);

                Dispose();

                return;
            }

            //
            // Electric arcs constantly change shape.
            //
            _reshapeTimer -= dt;

            if (_reshapeTimer <= 0f)
            {
                Reshape();

                _reshapeTimer =
                    0.035f +
                    Random.Shared.NextSingle()
                    * 0.025f;
            }

            //
            // Keep the object's center approximately
            // on the Fenrir for culling.
            //
            Matrix vehicleMatrix =
                _vehicleMatrixProvider();

            Position =
                Vector3.Transform(
                    new Vector3(
                        0f,
                        0f,
                        130f),
                    vehicleMatrix);

            base.Update(gameTime);
        }

        private void Reshape()
        {
            for (int arc = 0;
                 arc < ArcCount;
                 arc++)
            {
                for (int i = 1;
                     i < SegmentCount;
                     i++)
                {
                    _jitter[arc, i] =
                        Random.Shared.NextSingle()
                        * 2f - 1f;
                }
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                _spriteBatch == null ||
                _texture == null)
            {
                return;
            }

            Matrix vehicleMatrix =
                _vehicleMatrixProvider();

            for (int arc = 0;
                 arc < ArcCount;
                 arc++)
            {
                BuildArc(
                    arc,
                    vehicleMatrix);
            }

            float life =
                MathHelper.Clamp(
                    _remaining / Duration,
                    0f,
                    1f);

            float fade;

            if (life > 0.78f)
            {
                //
                // Very short fade-in.
                //
                fade =
                    (1f - life) / 0.22f;
            }
            else if (life < 0.30f)
            {
                fade =
                    life / 0.30f;
            }
            else
            {
                fade = 1f;
            }

            fade =
                MathHelper.Clamp(
                    fade,
                    0f,
                    1f);

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
                    DrawArcs(fade);
                }
            }
            else
            {
                DrawArcs(fade);
            }
        }

        private void BuildArc(
            int arc,
            Matrix vehicleMatrix)
        {
            var viewport =
                GraphicsDevice.Viewport;

            //
            // Original effect begins roughly around:
            //
            // CalcAddPosition(
            //     o,
            //     0,
            //     random Y,
            //     130,
            //     Position);
            //
            Vector3 localCenter =
                new Vector3(
                    0f,
                    _verticalOffsets[arc],
                    130f);

            //
            // The classic joint begins around radius 80
            // and progressively collapses inward.
            //
            const float startRadius = 80f;
            const float endRadius = 46f;

            //
            // Roughly 200 degrees of curved electrical trail.
            //
            const float arcLength =
                MathHelper.Pi * 1.15f;

            //
            // Whole structure spins rapidly during cast.
            //
            float spin =
                _time * 7.5f;

            for (int i = 0;
                 i <= SegmentCount;
                 i++)
            {
                float t =
                    (float)i /
                    SegmentCount;

                float angle =
                    _baseAngles[arc] +
                    spin +
                    arcLength * t;

                float radius =
                    MathHelper.Lerp(
                        startRadius,
                        endRadius,
                        t);

                //
                // Small electrical deformation.
                //
                radius +=
                    _jitter[arc, i]
                    * 9f;

                Vector3 radial =
                    new Vector3(
                        MathF.Cos(angle)
                            * radius,

                        MathF.Sin(angle)
                            * radius,

                        MathF.Sin(
                            angle * 1.7f)
                            * 18f);

                radial =
                    Vector3.Transform(
                        radial,
                        _planes[arc]);

                Vector3 localPosition =
                    localCenter +
                    radial;

                Vector3 worldPosition =
                    Vector3.Transform(
                        localPosition,
                        vehicleMatrix);

                Vector3 projected =
                    viewport.Project(
                        worldPosition,
                        Camera.Instance.Projection,
                        Camera.Instance.View,
                        Matrix.Identity);

                _screenPoints[arc, i] =
                    new Vector2(
                        projected.X,
                        projected.Y);

                _screenDepths[arc, i] =
                    projected.Z;
            }
        }

        private void DrawArcs(
            float fade)
        {
            if (_spriteBatch == null ||
                _texture == null)
            {
                return;
            }

            Color outerColor =
                new Color(_lightColor)
                * (fade * 0.80f);

            Color innerColor =
                Color.White
                * (fade * 0.65f);

            for (int arc = 0;
                 arc < ArcCount;
                 arc++)
            {
                DrawSingleArc(
                    arc,
                    outerColor,
                    3.6f);

                DrawSingleArc(
                    arc,
                    innerColor,
                    1.15f);
            }
        }

        private void DrawSingleArc(
            int arc,
            Color color,
            float thickness)
        {
            if (_spriteBatch == null ||
                _texture == null)
            {
                return;
            }

            float inverseTextureWidth =
                _texture.Width > 0
                    ? 1f / _texture.Width
                    : 1f;

            Vector2 origin =
                new Vector2(
                    0f,
                    _texture.Height * 0.5f);

            for (int i = 0;
                 i < SegmentCount;
                 i++)
            {
                float depthA =
                    _screenDepths[arc, i];

                float depthB =
                    _screenDepths[arc, i + 1];

                if (depthA < 0f ||
                    depthA > 1f ||
                    depthB < 0f ||
                    depthB > 1f)
                {
                    continue;
                }

                Vector2 p0 =
                    _screenPoints[arc, i];

                Vector2 p1 =
                    _screenPoints[
                        arc,
                        i + 1];

                Vector2 delta =
                    p1 - p0;

                float length =
                    delta.Length();

                if (length < 0.5f ||
                    !float.IsFinite(length))
                {
                    continue;
                }

                float rotation =
                    MathF.Atan2(
                        delta.Y,
                        delta.X);

                float depth =
                    MathHelper.Clamp(
                        (depthA + depthB)
                        * 0.5f,
                        0f,
                        1f);

                //
                // Fade trail towards the end.
                //
                float tailFade =
                    1f -
                    (float)i /
                    SegmentCount
                    * 0.55f;

                Color segmentColor =
                    color *
                    tailFade;

                Vector2 scale =
                    new Vector2(
                        length *
                        inverseTextureWidth *
                        1.08f,

                        thickness);

                _spriteBatch.Draw(
                    _texture,
                    p0,
                    null,
                    segmentColor,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    depth);
            }
        }
    }
}