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
using Client.Main.Controls;
namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Fenrir Plasma Storm.
    ///
    /// Recreates the four lightning trails used by the
    /// original MU client for MODEL_FENRIR_SKILL_THUNDER.
    /// </summary>
    public sealed class FenrirPlasmaStormEffect : EffectObject
    {
        private const string ThunderTexturePath =
            "Effect/JointThunder01.OZJ";
        private const string FlashTexturePath =
            "Effect/Flashing.OZJ";

        private const int BoltCount = 4;
        private const int MaxSegments = 14;

        private readonly Func<Vector3> _sourceProvider;
        private readonly Func<Vector3> _targetProvider;

        private readonly Vector2[,] _pathPoints =
            new Vector2[BoltCount, MaxSegments + 1];

        private readonly float[,] _pathDepths =
            new float[BoltCount, MaxSegments + 1];

        private readonly float[,] _offsets =
            new float[BoltCount, MaxSegments + 1];

        private readonly Vector3 _colorA;
        private readonly Vector3 _colorB;

        private Texture2D? _thunderTexture;
        private Texture2D? _flashTexture;
        private SpriteBatch? _spriteBatch;

        private readonly Vector3 _firstLayerColor;
        private readonly Vector3 _secondLayerColor;
        private readonly bool _secondLayerUsesFlash;

        //
        // Dynamic terrain light.
        //
        // Original MU calls AddTerrainLight while
        // MODEL_FENRIR_SKILL_THUNDER travels toward its target.
        //
        private readonly DynamicLight _travelLight;
        private bool _lightAdded;

        private float _remaining;
        private float _reshapeTimer;
        private float _time;

        private const float Duration = 0.65f;

        public FenrirPlasmaStormEffect(
            Func<Vector3> sourceProvider,
            Func<Vector3> targetProvider,
            short fenrirItemIndex,
            bool isSecondaryTarget = false)
        {
            _sourceProvider =
                sourceProvider ??
                throw new ArgumentNullException(
                    nameof(sourceProvider));

            _targetProvider =
                targetProvider ??
                throw new ArgumentNullException(
                    nameof(targetProvider));

            _remaining = Duration;

            IsTransparent = true;
            AffectedByTransparency = true;

            BlendState = BlendState.Additive;

            DepthState =
                DepthStencilState.DepthRead;

            BoundingBoxLocal =
                new BoundingBox(
                    Vector3.Zero,
                    Vector3.Zero);

            GetFenrirSkillLayers(
                fenrirItemIndex,
                isSecondaryTarget,
                out _firstLayerColor,
                out _secondLayerColor,
                out _secondLayerUsesFlash);


            //
            // Original MODEL_FENRIR_SKILL_THUNDER uses
            // a relatively small neutral terrain light.
            //
            // We keep it mostly white so it illuminates
            // the terrain instead of painting it strongly.
            //
            _travelLight =
                new DynamicLight
                {
                    Owner = this,

                    Color =
                        new Vector3(
                            0.85f,
                            0.85f,
                            0.85f),

                    Radius = 260f,

                    Intensity = 0f
                };


            InitializeOffsets();
        }

        private void InitializeOffsets()
        {
            for (int bolt = 0;
                 bolt < BoltCount;
                 bolt++)
            {
                _offsets[bolt, 0] = 0f;

                _offsets[
                    bolt,
                    MaxSegments] = 0f;

                for (int i = 1;
                     i < MaxSegments;
                     i++)
                {
                    _offsets[bolt, i] =
                        Random.Shared.NextSingle()
                        * 2f - 1f;
                }
            }
        }

        private static void GetFenrirSkillLayers(
            short itemIndex,
            bool isSecondaryTarget,
            out Vector3 firstColor,
            out Vector3 secondColor,
            out bool secondUsesFlash)
        {
            int fenrirType =
                itemIndex switch
                {
                    // Black
                    11 or 15 => 0,

                    // Red
                    14 or 18 => 1,

                    // Blue
                    12 or 16 => 2,

                    // Gold
                    13 or 17 => 3,

                    _ => 0
                };

            //
            // Original:
            //
            // main target:
            //   0 + fenrirType
            //   3 + fenrirType
            //
            // secondary targets:
            //   0 + fenrirType
            //   4 + fenrirType
            //
            int firstSubtype =
                fenrirType;

            int secondSubtype =
                isSecondaryTarget
                    ? 4 + fenrirType
                    : 3 + fenrirType;

            firstColor =
                GetFenrirSubtypeColor(
                    firstSubtype);

            secondColor =
                GetFenrirSubtypeColor(
                    secondSubtype);

            secondUsesFlash =
                secondSubtype >= 4 &&
                secondSubtype <= 7;
        }

        private static Vector3 GetFenrirSubtypeColor(
            int subtype)
        {
            return subtype switch
            {
                //
                // JointThunder variants.
                //
                0 => new Vector3(
                    0.7f,
                    1.0f,
                    0.7f),

                1 => new Vector3(
                    1.0f,
                    0.6f,
                    0.6f),

                2 => new Vector3(
                    0.7f,
                    0.7f,
                    1.0f),

                3 => new Vector3(
                    0.9f,
                    0.9f,
                    0.3f),

                //
                // Flashing variants.
                //
                4 => new Vector3(
                    0.1f,
                    0.8f,
                    0.1f),

                5 => new Vector3(
                    1.0f,
                    0.3f,
                    0.2f),

                6 => new Vector3(
                    0.2f,
                    0.3f,
                    1.0f),

                7 => new Vector3(
                    0.8f,
                    0.8f,
                    0.1f),

                _ => Vector3.One
            };
        }

        public override async Task LoadContent()
        {
            await TextureLoader.Instance.Prepare(
                ThunderTexturePath);

            await TextureLoader.Instance.Prepare(
                FlashTexturePath);

            _thunderTexture =
                TextureLoader.Instance.GetTexture2D(
                    ThunderTexturePath);

            _flashTexture =
                TextureLoader.Instance.GetTexture2D(
                    FlashTexturePath);

            _thunderTexture ??=
                GraphicsManager.Instance.Pixel;

            _flashTexture ??=
                GraphicsManager.Instance.Pixel;

            _spriteBatch =
                GraphicsManager.Instance.Sprite;


            //
            // Register the moving Plasma Storm light.
            //
            if (World?.Terrain != null &&
                !_lightAdded)
            {
                World.Terrain.AddDynamicLight(
                    _travelLight);

                _lightAdded = true;
            }
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

            _reshapeTimer -= dt;

            if (_reshapeTimer <= 0f)
            {
                Reshape();

                _reshapeTimer =
                    0.035f +
                    Random.Shared.NextSingle()
                    * 0.025f;
            }

            Vector3 source =
                _sourceProvider();

            Vector3 target =
                _targetProvider();

            UpdateBounds(
                source,
                target);

            UpdateDynamicLight(
                source,
                target);

            base.Update(gameTime);
        }

        private void Reshape()
        {
            for (int bolt = 0;
                 bolt < BoltCount;
                 bolt++)
            {
                for (int i = 1;
                     i < MaxSegments;
                     i++)
                {
                    _offsets[bolt, i] =
                        Random.Shared.NextSingle()
                        * 2f - 1f;
                }
            }
        }
        private void UpdateDynamicLight(
            Vector3 source,
            Vector3 target)
        {
            //
            // 1.0 = effect just spawned
            // 0.0 = effect finished
            //
            float life =
                MathHelper.Clamp(
                    _remaining / Duration,
                    0f,
                    1f);


            //
            // MODEL_FENRIR_SKILL_THUNDER in the original
            // physically moves toward the target.
            //
            float progress =
                1f - life;

            //
            // Slight acceleration gives it a less mechanical
            // movement and matches the fast arrival of the
            // original Fenrir thunder.
            //
            progress =
                MathHelper.Clamp(
                    progress * 1.20f,
                    0f,
                    1f);

            progress =
                progress *
                progress *
                (3f - 2f * progress);


            //
            // Move the terrain light along the bolt.
            //
            Vector3 lightPosition =
                Vector3.Lerp(
                    source,
                    target,
                    progress);

            //
            // Keep the light slightly above terrain/object
            // center so the projected illumination is visible.
            //
            lightPosition.Z += 10f;

            _travelLight.Position =
                lightPosition;


            //
            // Fast electrical flicker.
            //
            float flicker =
                0.78f +
                MathF.Sin(
                    _time * 70f)
                * 0.14f;


            //
            // Add a tiny random component so multiple targets
            // don't all pulse at exactly the same intensity.
            //
            flicker +=
                Random.Shared.NextSingle()
                * 0.08f;


            //
            // Fade near the end.
            //
            float fade = 1f;

            if (life < 0.25f)
            {
                fade =
                    life / 0.25f;
            }


            _travelLight.Intensity =
                MathHelper.Clamp(
                    flicker * fade,
                    0f,
                    1.15f);
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                _spriteBatch == null ||
                _thunderTexture == null ||
                _flashTexture == null)
            {
                return;
            }

            Vector3 source =
                _sourceProvider();

            Vector3 target =
                _targetProvider();

            var viewport =
                GraphicsDevice.Viewport;

            Vector3 projectedSource =
                viewport.Project(
                    source,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            Vector3 projectedTarget =
                viewport.Project(
                    target,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            if (projectedSource.Z < 0f ||
                projectedSource.Z > 1f ||
                projectedTarget.Z < 0f ||
                projectedTarget.Z > 1f)
            {
                return;
            }

            Vector2 start =
                new Vector2(
                    projectedSource.X,
                    projectedSource.Y);

            Vector2 end =
                new Vector2(
                    projectedTarget.X,
                    projectedTarget.Y);

            Vector2 difference =
                end - start;

            float totalLength =
                difference.Length();

            if (totalLength < 5f ||
                !float.IsFinite(totalLength))
            {
                return;
            }

            int segments =
                Math.Clamp(
                    (int)(totalLength / 38f),
                    5,
                    MaxSegments);

            Vector2 direction =
                difference / totalLength;

            Vector2 perpendicular =
                new Vector2(
                    -direction.Y,
                    direction.X);

            float step =
                totalLength / segments;

            for (int bolt = 0;
                 bolt < BoltCount;
                 bolt++)
            {
                BuildPath(
                    bolt,
                    start,
                    direction,
                    perpendicular,
                    step,
                    segments,
                    projectedSource.Z,
                    projectedTarget.Z);
            }

            float life =
                MathHelper.Clamp(
                    _remaining / Duration,
                    0f,
                    1f);

            float fade = 1f;

            if (life < 0.25f)
            {
                fade =
                    life / 0.25f;
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
                    DrawBolts(
                        segments,
                        fade);
                }
            }
            else
            {
                DrawBolts(
                    segments,
                    fade);
            }
        }

        private void BuildPath(
            int bolt,
            Vector2 start,
            Vector2 direction,
            Vector2 perpendicular,
            float step,
            int segments,
            float sourceDepth,
            float targetDepth)
        {
            //
            // The original joint randomly perturbs
            // Direction every update.
            //
            // Give each of the four bolts a slightly
            // different amplitude.
            //
            float amplitude =
                24f +
                bolt * 7f;

            for (int i = 0;
                 i <= segments;
                 i++)
            {
                float t =
                    (float)i / segments;

                Vector2 basePosition =
                    start +
                    direction *
                    (i * step);

                float offset =
                    _offsets[bolt, i]
                    * amplitude;

                //
                // Make the ends converge exactly
                // on Fenrir and target.
                //
                float endFade =
                    MathF.Sin(
                        t *
                        MathHelper.Pi);

                offset *= endFade;

                _pathPoints[bolt, i] =
                    basePosition +
                    perpendicular * offset;

                _pathDepths[bolt, i] =
                    MathHelper.Lerp(
                        sourceDepth,
                        targetDepth,
                        t);
            }
        }

        private void DrawBolts(
            int segments,
            float fade)
        {
            if (_thunderTexture == null ||
                _flashTexture == null)
            {
                return;
            }

            //
            // Original Plasma Storm creates the pair twice:
            //
            // Joint 1
            // Joint 2
            //
            // Joint 1
            // Joint 2
            //
            // So we reproduce two copies of each layer.
            //


            // =============================================
            // FIRST LAYER
            // MODEL_FENRIR_SKILL_THUNDER
            // subtype = fenrirType
            //
            // Always JointThunder.
            // =============================================

            DrawBolt(
                0,
                segments,
                _thunderTexture,
                _firstLayerColor,
                3.4f,
                fade);

            DrawBolt(
                2,
                segments,
                _thunderTexture,
                _firstLayerColor,
                2.8f,
                fade * 0.90f);


            // =============================================
            // SECOND LAYER
            //
            // Depending on subtype this becomes either:
            //
            // BITMAP_JOINT_THUNDER
            // or
            // BITMAP_FLASH -> Flashing.jpg
            //
            // =============================================

            Texture2D secondTexture =
                _secondLayerUsesFlash
                    ? _flashTexture
                    : _thunderTexture;

            float secondThickness =
                _secondLayerUsesFlash
                    ? 2.8f
                    : 2.6f;


            DrawBolt(
                1,
                segments,
                secondTexture,
                _secondLayerColor,
                secondThickness,
                fade);

            DrawBolt(
                3,
                segments,
                secondTexture,
                _secondLayerColor,
                secondThickness * 0.82f,
                fade * 0.88f);
        }

        private void DrawBolt(
            int bolt,
            int segments,
            Texture2D texture,
            Vector3 lightColor,
            float thickness,
            float alpha)
        {
            if (_spriteBatch == null ||
                texture == null)
            {
                return;
            }

            Color color =
                new Color(lightColor)
                * alpha;

            Vector2 origin =
                new Vector2(
                    0f,
                    texture.Height * 0.5f);

            float invWidth =
                texture.Width > 0
                    ? 1f / texture.Width
                    : 1f;

            for (int i = 0;
                 i < segments;
                 i++)
            {
                Vector2 p0 =
                    _pathPoints[bolt, i];

                Vector2 p1 =
                    _pathPoints[bolt, i + 1];

                Vector2 delta =
                    p1 - p0;

                float length =
                    delta.Length();

                if (length < 0.5f)
                    continue;

                float rotation =
                    MathF.Atan2(
                        delta.Y,
                        delta.X);

                float depth =
                    MathHelper.Clamp(
                        (
                            _pathDepths[bolt, i] +
                            _pathDepths[
                                bolt,
                                i + 1]
                        ) * 0.5f,
                        0f,
                        1f);

                Vector2 scale =
                    new Vector2(
                        length *
                        invWidth *
                        1.08f,

                        thickness);

                _spriteBatch.Draw(
                    texture,
                    p0,
                    null,
                    color,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    depth);
            }
        }

        private void UpdateBounds(
            Vector3 source,
            Vector3 target)
        {
            Vector3 min =
                Vector3.Min(
                    source,
                    target);

            Vector3 max =
                Vector3.Max(
                    source,
                    target);

            Vector3 padding =
                new Vector3(
                    120f,
                    120f,
                    120f);

            min -= padding;
            max += padding;

            Vector3 center =
                (min + max)
                * 0.5f;

            Position =
                center;

            BoundingBoxLocal =
                new BoundingBox(
                    min - center,
                    max - center);
        }
        public override void Dispose()
        {
            if (_lightAdded &&
                World?.Terrain != null)
            {
                World.Terrain.RemoveDynamicLight(
                    _travelLight);

                _lightAdded = false;
            }

            base.Dispose();
        }
    }
}