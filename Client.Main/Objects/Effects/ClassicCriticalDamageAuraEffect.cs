#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Persistent classic Dark Lord Increase Critical Damage visual.
    ///
    /// Original Main:
    ///
    /// if (g_isCharacterBuff(
    ///         o,
    ///         eBuff_AddCriticalDamage))
    /// {
    ///     if ((MoveSceneFrame % 30) == 0)
    ///     {
    ///         CreateEffect(
    ///             BITMAP_FLARE_FORCE,
    ///             hand/weapon position,
    ///             ...,
    ///             subtype: 1);
    ///     }
    /// }
    ///
    /// BITMAP_FLARE_FORCE subtype 1 creates:
    ///
    ///     subtype 5
    ///     subtype 6
    ///     subtype 7
    ///
    /// Those joints use:
    ///
    ///     BITMAP_FIRECRACKER
    ///
    /// which corresponds to:
    ///
    ///     Data/Effect/Fire04
    ///
    /// Classic implementation attaches them to the weapon LinkBone.
    ///
    /// MonoGame adaptation:
    ///
    /// We attach directly to the actual player hand bones so both
    /// hands retain the aura while the buff is active, regardless
    /// of whether a weapon is equipped.
    ///
    /// The original geometry is preserved, but compensated in size
    /// because the same numeric dimensions render substantially
    /// larger in this client.
    /// </summary>
    public sealed class ClassicCriticalDamageAuraEffect
        : WorldObject
    {
        private const string TexturePath =
            "Effect/Fire04.OZJ";

        // =============================================================
        // CLASSIC TIMING
        // =============================================================

        private const float ClassicReferenceFps =
            25.0f;

        //
        // Original:
        //
        // MoveSceneFrame % 30
        //
        private const float PulseFrames =
            30.0f;

        //
        // BITMAP_FLARE_FORCE subtype 5 / 6 / 7:
        //
        // LifeTime = 15
        //
        private const float ActiveFrames =
            15.0f;

        // =============================================================
        // TRAILS
        // =============================================================

        private const int TrailsPerHand =
            3;

        private const int HandCount =
            2;

        //
        // Original joint gradually increases MaxTails while alive.
        //
        private const int MaxSamplesPerTrail =
            15;

        private const int VerticesPerSample =
            4;

        private const int MaxTrails =
            HandCount *
            TrailsPerHand;

        private const int MaxVertices =
            MaxTrails *
            MaxSamplesPerTrail *
            VerticesPerSample;

        private const int MaxSegmentsPerTrail =
            MaxSamplesPerTrail - 1;

        private const int IndicesPerSegment =
            12;

        private const int MaxIndices =
            MaxTrails *
            MaxSegmentsPerTrail *
            IndicesPerSegment;

        // =============================================================
        // MONOGAME VISUAL COMPENSATION
        // =============================================================

        //
        // The original values:
        //
        //     joint Scale   = 20
        //     radius        = 30
        //     local Y       = 20
        //     Direction.Y   = -4
        //
        // are visually too large when applied literally in this
        // MonoGame renderer.
        //
        // Keep all proportions, but shrink the entire local effect
        // uniformly around the hand.
        //
        private const float PersistentVisualScale =
            0.55f;

        //
        // Original:
        //
        // CreateJoint(... Scale = 20)
        //
        private const float TrailWidth =
            20.0f *
            PersistentVisualScale;

        //
        // Original light:
        //
        //     (1.0, 0.8, 1.0)
        //
        // Additive blending in MonoGame is visually stronger, so
        // this acts only as renderer-space brightness compensation.
        //
        private const float Intensity =
            0.55f;

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

        // =============================================================
        // OWNER
        // =============================================================

        private readonly PlayerObject
            _owner;

        // =============================================================
        // GEOMETRY BUFFERS
        // =============================================================

        private readonly VertexPositionColorTexture[]
            _vertices =
                new VertexPositionColorTexture[
                    MaxVertices];

        private readonly ushort[]
            _indices =
                new ushort[
                    MaxIndices];

        private Texture2D? _texture;

        private BasicEffect? _effect;

        private DynamicVertexBuffer? _vertexBuffer;

        private DynamicIndexBuffer? _indexBuffer;

        private int
            _vertexCount;

        private int
            _indexCount;

        private float
            _elapsed;

        public PlayerObject Owner =>
            _owner;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public ClassicCriticalDamageAuraEffect(
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
                        -100f),
                    new Vector3(
                        160f,
                        160f,
                        260f));

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

        // =============================================================
        // LOAD
        // =============================================================

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

                return;
            }

            _effect =
                new BasicEffect(
                    GraphicsDevice)
                {
                    TextureEnabled =
                        true,

                    VertexColorEnabled =
                        true,

                    LightingEnabled =
                        false,

                    FogEnabled =
                        false
                };

            _vertexBuffer =
                new DynamicVertexBuffer(
                    GraphicsDevice,
                    typeof(
                        VertexPositionColorTexture),
                    MaxVertices,
                    BufferUsage.WriteOnly);

            _indexBuffer =
                new DynamicIndexBuffer(
                    GraphicsDevice,
                    IndexElementSize.SixteenBits,
                    MaxIndices,
                    BufferUsage.WriteOnly);
        }

        // =============================================================
        // UPDATE
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);

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

            Hidden =
                _owner.Hidden ||
                _owner.IsDead ||
                _owner.Status !=
                    GameControlStatus.Ready;

            if (Hidden)
            {
                _vertexCount =
                    0;

                _indexCount =
                    0;

                return;
            }

            _elapsed +=
                (float)
                gameTime.ElapsedGameTime
                    .TotalSeconds;

            BuildGeometry();

            if (_vertexCount > 0 &&
                _indexCount > 0)
            {
                _vertexBuffer?.SetData(
                    _vertices,
                    0,
                    _vertexCount,
                    SetDataOptions.Discard);

                _indexBuffer?.SetData(
                    _indices,
                    0,
                    _indexCount,
                    SetDataOptions.Discard);
            }
        }

        // =============================================================
        // BUILD COMPLETE EFFECT
        // =============================================================

        private void BuildGeometry()
        {
            _vertexCount =
                0;

            _indexCount =
                0;

            //
            // Convert real-time MonoGame time back into the original
            // 25 FPS effect timing.
            //
            float absoluteFrame =
                _elapsed *
                ClassicReferenceFps;

            float pulseFrame =
                absoluteFrame %
                PulseFrames;

            //
            // Original joint only lives for 15 of the 30-frame
            // recurring pulse.
            //
            if (pulseFrame >=
                ActiveFrames)
            {
                return;
            }

            //
            // Classic MaxTails grows throughout the joint lifetime.
            //
            int sampleCount =
                Math.Clamp(
                    1 +
                    (int)MathF.Floor(
                        pulseFrame),
                    1,
                    MaxSamplesPerTrail);

            //
            // Need at least two samples to make one ribbon segment.
            //
            if (sampleCount < 2)
            {
                return;
            }

            // =========================================================
            // CLASSIC FADE
            // =============================================================
            //
            // Original:
            //
            // if (LifeTime < 7)
            //
            //     Light /= 1.5;
            //
            // Life starts at 15, therefore fade starts at roughly
            // classic frame 8.
            //
            float fadeFrames =
                MathF.Max(
                    0.0f,
                    pulseFrame -
                    8.0f);

            float fade =
                MathF.Pow(
                    1.0f / 1.5f,
                    fadeFrames);

            Vector3 light =
                new Vector3(
                    1.0f,
                    0.8f,
                    1.0f) *
                fade *
                Intensity;

            // =========================================================
            // BOTH HANDS
            // =============================================================
            //
            // IMPORTANT:
            //
            // Do not check Weapon1 or Weapon2 here.
            //
            // This aura belongs to the hand/buff, not to the weapon
            // model. It therefore remains visible:
            //
            //     weapon + weapon
            //     weapon + empty hand
            //     empty hand + weapon
            //     empty hand + empty hand
            //
            BuildHand(
                isLeftHand: true,
                sampleCount,
                light);

            BuildHand(
                isLeftHand: false,
                sampleCount,
                light);
        }

        // =============================================================
        // HAND
        // =============================================================

        private void BuildHand(
            bool isLeftHand,
            int sampleCount,
            Vector3 light)
        {
            //
            // PlayerObject exposes the actual animated hand bones.
            //
            // Left:
            //
            //     bone 33
            //
            // Right:
            //
            //     bone 42
            //
            if (!_owner.TryGetHandWorldMatrix(
                    isLeftHand,
                    out Matrix handWorld))
            {
                return;
            }

            Vector3 widthAxis1 =
                Vector3.TransformNormal(
                    Vector3.UnitX,
                    handWorld);

            Vector3 widthAxis2 =
                Vector3.TransformNormal(
                    Vector3.UnitZ,
                    handWorld);

            NormalizeSafe(
                ref widthAxis1,
                Vector3.UnitX);

            NormalizeSafe(
                ref widthAxis2,
                Vector3.UnitZ);

            //
            // Classic joint Scale = 20.
            //
            // Our TrailWidth already contains the MonoGame size
            // compensation.
            //
            float halfWidth =
                TrailWidth *
                0.5f *
                _owner.TotalScale;

            widthAxis1 *=
                halfWidth;

            widthAxis2 *=
                halfWidth;

            //
            // BITMAP_FLARE_FORCE subtype 1 creates exactly:
            //
            //     subtype 5
            //     subtype 6
            //     subtype 7
            //
            for (int trail = 0;
                 trail < TrailsPerHand;
                 trail++)
            {
                int subType =
                    5 + trail;

                BuildTrail(
                    handWorld,
                    widthAxis1,
                    widthAxis2,
                    subType,
                    sampleCount,
                    light);
            }
        }

        // =============================================================
        // TRAIL
        // =============================================================

        private void BuildTrail(
            Matrix handWorld,
            Vector3 widthAxis1,
            Vector3 widthAxis2,
            int subType,
            int sampleCount,
            Vector3 light)
        {
            int vertexStart =
                _vertexCount;

            //
            // Original non-bow value:
            //
            // Direction = (0, -4, 0)
            //
            // Critical Damage is a Dark Lord skill, therefore the
            // standard non-bow trajectory is the one we reproduce.
            //
            float directionY =
                -4.0f *
                PersistentVisualScale;

            float accumulatedY =
                0.0f;

            //
            // Original:
            //
            // TargetPosition[2] =
            //     SubType * 90
            //
            float baseAngle =
                subType *
                90.0f;

            //
            // subtype 5:
            //
            //     +40 degrees per sample
            //
            // subtype 6:
            //
            //     -40 degrees per sample
            //
            // subtype 7:
            //
            //     +40 degrees per sample
            //
            float rotationDirection =
                (subType & 1) != 0
                    ? 1.0f
                    : -1.0f;

            Color color =
                new Color(
                    Vector3.Clamp(
                        light,
                        Vector3.Zero,
                        Vector3.One));

            for (int sample = 0;
                 sample < sampleCount;
                 sample++)
            {
                if (sample > 0)
                {
                    accumulatedY +=
                        directionY;

                    //
                    // Original:
                    //
                    // Direction[1] -= 0.1
                    //
                    directionY -=
                        0.1f *
                        PersistentVisualScale;
                }

                //
                // Original:
                //
                // TargetPosition[0] = 30
                //
                // then:
                //
                // TargetPosition[0] -= 0.15
                //
                // per generated point.
                //
                float radius =
                    (
                        30.0f -
                        sample *
                        0.15f
                    ) *
                    PersistentVisualScale;

                float angleDegrees =
                    baseAngle +
                    rotationDirection *
                    sample *
                    40.0f;

                float angle =
                    MathHelper.ToRadians(
                        angleDegrees);

                //
                // Original starts from:
                //
                //     local (0, 20, 0)
                //
                // and rotates:
                //
                //     (0, 0, radius)
                //
                // around the hand.
                //
                Vector3 local =
                    new Vector3(
                        0.0f,

                        20.0f *
                        PersistentVisualScale +
                        accumulatedY -
                        MathF.Sin(
                            angle) *
                        radius,

                        MathF.Cos(
                            angle) *
                        radius);

                //
                // CRITICAL:
                //
                // Transform using the actual animated hand bone.
                //
                // This is what makes the aura follow the hand instead
                // of the weapon model.
                //
                Vector3 point =
                    Vector3.Transform(
                        local,
                        handWorld);

                int v =
                    _vertexCount;

                //
                // Safety.
                //
                if (v +
                        VerticesPerSample >
                    MaxVertices)
                {
                    break;
                }

                float u =
                    sample /
                    (float)
                    Math.Max(
                        1,
                        sampleCount - 1);

                // =====================================================
                // FACE 1
                // =====================================================

                _vertices[v + 0] =
                    new VertexPositionColorTexture(
                        point -
                            widthAxis1,
                        color,
                        new Vector2(
                            u,
                            0.0f));

                _vertices[v + 1] =
                    new VertexPositionColorTexture(
                        point +
                            widthAxis1,
                        color,
                        new Vector2(
                            u,
                            1.0f));

                // =====================================================
                // FACE 2
                // =====================================================
                //
                // Second crossed ribbon allows the trail to retain
                // volume from different camera angles, matching the
                // classic joint renderer more closely.
                //
                _vertices[v + 2] =
                    new VertexPositionColorTexture(
                        point -
                            widthAxis2,
                        color,
                        new Vector2(
                            u,
                            0.0f));

                _vertices[v + 3] =
                    new VertexPositionColorTexture(
                        point +
                            widthAxis2,
                        color,
                        new Vector2(
                            u,
                            1.0f));

                _vertexCount +=
                    VerticesPerSample;
            }

            //
            // The effective number of samples may theoretically be
            // lower if we reached our safety capacity.
            //
            int generatedSamples =
                (
                    _vertexCount -
                    vertexStart
                ) /
                VerticesPerSample;

            for (int segment = 0;
                 segment <
                    generatedSamples - 1;
                 segment++)
            {
                int current =
                    vertexStart +
                    segment *
                    VerticesPerSample;

                int next =
                    current +
                    VerticesPerSample;

                // =====================================================
                // FACE 1
                // =====================================================

                AddTriangle(
                    current + 0,
                    current + 1,
                    next + 1);

                AddTriangle(
                    current + 0,
                    next + 1,
                    next + 0);

                // =====================================================
                // FACE 2
                // =====================================================

                AddTriangle(
                    current + 2,
                    current + 3,
                    next + 3);

                AddTriangle(
                    current + 2,
                    next + 3,
                    next + 2);
            }
        }

        // =============================================================
        // INDEX BUFFER HELPERS
        // =============================================================

        private void AddTriangle(
            int a,
            int b,
            int c)
        {
            if (_indexCount + 3 >
                MaxIndices)
            {
                return;
            }

            _indices[_indexCount++] =
                (ushort)a;

            _indices[_indexCount++] =
                (ushort)b;

            _indices[_indexCount++] =
                (ushort)c;
        }

        private static void NormalizeSafe(
            ref Vector3 value,
            Vector3 fallback)
        {
            if (value.LengthSquared() <
                0.0001f)
            {
                value =
                    fallback;

                return;
            }

            value.Normalize();
        }

        // =============================================================
        // DRAW
        // =============================================================

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(
                gameTime);

            if (!Visible ||
                Hidden ||
                _indexCount <= 0 ||
                _texture == null ||
                _effect == null ||
                _vertexBuffer == null ||
                _indexBuffer == null)
            {
                return;
            }

            GraphicsDevice gd =
                GraphicsDevice;

            BlendState previousBlend =
                gd.BlendState;

            DepthStencilState previousDepth =
                gd.DepthStencilState;

            RasterizerState previousRasterizer =
                gd.RasterizerState;

            IndexBuffer? previousIndices =
                gd.Indices;

            try
            {
                gd.SetVertexBuffer(
                    _vertexBuffer);

                gd.Indices =
                    _indexBuffer;

                // =====================================================
                // ADDITIVE
                // =====================================================
                //
                // Fire04 contains dark/black texels around the effect.
                //
                // Additive blending makes those pixels contribute
                // nothing instead of drawing a dark rectangle around
                // the aura.
                //
                gd.BlendState =
                    ClassicAdditive;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                //
                // Vertex positions are already world-space because
                // BuildTrail transformed them through handWorld.
                //
                _effect.World =
                    Matrix.Identity;

                _effect.View =
                    Camera.Instance.View;

                _effect.Projection =
                    Camera.Instance.Projection;

                _effect.Texture =
                    _texture;

                _effect.Alpha =
                    TotalAlpha;

                int primitiveCount =
                    _indexCount /
                    3;

                foreach (
                    EffectPass pass
                    in _effect
                        .CurrentTechnique
                        .Passes)
                {
                    pass.Apply();

                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        primitiveCount);
                }
            }
            finally
            {
                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;

                gd.RasterizerState =
                    previousRasterizer;

                gd.Indices =
                    previousIndices;

                gd.SetVertexBuffer(
                    null);
            }
        }

        // =============================================================
        // REMOVE
        // =============================================================

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

        // =============================================================
        // DISPOSE
        // =============================================================

        public override void Dispose()
        {
            _vertexBuffer?.Dispose();

            _vertexBuffer =
                null;

            _indexBuffer?.Dispose();

            _indexBuffer =
                null;

            _effect?.Dispose();

            _effect =
                null;

            //
            // TextureLoader owns the actual texture.
            //
            _texture =
                null;

            base.Dispose();
        }
    }
}