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
    ///             weaponPosition,
    ///             ...,
    ///             subtype: 1);
    ///     }
    /// }
    ///
    /// BITMAP_FLARE_FORCE subtype 1 creates three joints:
    ///
    ///     subtype 5
    ///     subtype 6
    ///     subtype 7
    ///
    /// Those joints use:
    ///
    ///     BITMAP_FIRECRACKER
    ///
    /// which is:
    ///
    ///     Data/Effect/Fire04
    ///
    /// They remain attached to the weapon link bone and generate
    /// three short spiralling trails around the weapon/hand.
    /// </summary>
    public sealed class ClassicCriticalDamageAuraEffect
        : WorldObject
    {
        private const string TexturePath =
            "Effect/Fire04.OZJ";

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
        // BITMAP_FLARE_FORCE subtype 5/6/7:
        //
        // LifeTime = 15
        //
        private const float ActiveFrames =
            15.0f;

        private const int TrailsPerHand =
            3;

        private const int HandCount =
            2;

        //
        // During a 15-frame lifetime the classic joint gradually
        // increases MaxTails.
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

        //
        // CreateJoint(... Scale = 20)
        //
        private const float TrailWidth =
            20.0f;

        //
        // The original uses full (1, .8, 1) light.
        //
        // MonoGame's additive renderer is visually stronger,
        // so this is only a renderer-space compensation.
        //
        private const float Intensity =
            0.70f;

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

        private readonly PlayerObject _owner;

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

        private int _vertexCount;

        private int _indexCount;

        private float _elapsed;

        public PlayerObject Owner =>
            _owner;

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
                        -250f,
                        -250f,
                        -120f),
                    new Vector3(
                        250f,
                        250f,
                        320f));

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

        private void BuildGeometry()
        {
            _vertexCount =
                0;

            _indexCount =
                0;

            //
            // Convert MonoGame real time back to classic
            // effect frames.
            //
            float absoluteFrame =
                _elapsed *
                ClassicReferenceFps;

            float pulseFrame =
                absoluteFrame %
                PulseFrames;

            //
            // Original joint lives for 15 frames.
            //
            if (pulseFrame >=
                ActiveFrames)
            {
                return;
            }

            //
            // MaxTails starts small and grows every frame.
            //
            int sampleCount =
                Math.Clamp(
                    1 +
                    (int)MathF.Floor(
                        pulseFrame),
                    1,
                    MaxSamplesPerTrail);

            //
            // A single sample cannot form a ribbon yet.
            //
            if (sampleCount < 2)
            {
                return;
            }

            //
            // Original:
            //
            // if (LifeTime < 7)
            //     Light /= 1.5
            //
            // Life starts at 15, therefore fading begins
            // around classic frame 8.
            //
            float fadeFrames =
                MathF.Max(
                    0f,
                    pulseFrame -
                    8f);

            float fade =
                MathF.Pow(
                    1f / 1.5f,
                    fadeFrames);

            Vector3 light =
                new Vector3(
                    1.0f,
                    0.8f,
                    1.0f) *
                fade *
                Intensity;

            BuildHand(
                isLeftHand: true,
                _owner.Weapon1,
                sampleCount,
                light);

            BuildHand(
                isLeftHand: false,
                _owner.Weapon2,
                sampleCount,
                light);
        }

        private void BuildHand(
            bool isLeftHand,
            WeaponObject? weapon,
            int sampleCount,
            Vector3 light)
        {
            if (weapon == null ||
                weapon.Model == null)
            {
                return;
            }

            // =========================================================
            // CLASSIC WEAPON FILTERS
            // =========================================================

            if (isLeftHand)
            {
                //
                // Original:
                //
                // Weapon[0].Type != MODEL_BOW + 15
                //
                if (weapon.ItemGroup == 4 &&
                    weapon.ItemNumber == 15)
                {
                    return;
                }
            }
            else
            {
                //
                // Original:
                //
                // Weapon[1].Type != MODEL_BOW + 7
                //
                if (weapon.ItemGroup == 4 &&
                    weapon.ItemNumber == 7)
                {
                    return;
                }

                //
                // Shields do not receive the critical aura.
                //
                if (weapon.ItemGroup == 6)
                {
                    return;
                }
            }

            if (!_owner.TryGetHandWorldMatrix(
                    isLeftHand,
                    out Matrix handWorld))
            {
                return;
            }

            bool isBow =
                weapon.ItemGroup == 4;

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

            float halfWidth =
                TrailWidth *
                0.5f *
                _owner.TotalScale;

            widthAxis1 *=
                halfWidth;

            widthAxis2 *=
                halfWidth;

            //
            // BITMAP_FLARE_FORCE subtype 1 creates:
            //
            //     5
            //     6
            //     7
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
                    isBow,
                    sampleCount,
                    light);
            }
        }

        private void BuildTrail(
            Matrix handWorld,
            Vector3 widthAxis1,
            Vector3 widthAxis2,
            int subType,
            bool isBow,
            int sampleCount,
            Vector3 light)
        {
            int vertexStart =
                _vertexCount;

            float directionY =
                isBow
                    ? 0f
                    : -4.0f;

            float accumulatedY =
                0f;

            //
            // Original:
            //
            // TargetPosition[2] = SubType * 90
            //
            float baseAngle =
                subType *
                90.0f;

            //
            // subtype 5 / 7 -> +40 deg
            // subtype 6     -> -40 deg
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
                if (sample > 0 &&
                    !isBow)
                {
                    accumulatedY +=
                        directionY;

                    //
                    // Original:
                    //
                    // Direction[1] -= .1
                    //
                    directionY -=
                        0.1f;
                }

                float radius =
                    30.0f -
                    sample *
                    0.15f;

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
                // around Angle.X.
                //
                Vector3 local =
                    new Vector3(
                        0f,

                        20.0f +
                        accumulatedY -
                        MathF.Sin(angle) *
                        radius,

                        MathF.Cos(angle) *
                        radius);

                Vector3 point =
                    Vector3.Transform(
                        local,
                        handWorld);

                int v =
                    _vertexCount;

                _vertices[v + 0] =
                    new VertexPositionColorTexture(
                        point -
                            widthAxis1,
                        color,
                        new Vector2(
                            sample /
                            (float)
                            Math.Max(
                                1,
                                sampleCount - 1),
                            0f));

                _vertices[v + 1] =
                    new VertexPositionColorTexture(
                        point +
                            widthAxis1,
                        color,
                        new Vector2(
                            sample /
                            (float)
                            Math.Max(
                                1,
                                sampleCount - 1),
                            1f));

                _vertices[v + 2] =
                    new VertexPositionColorTexture(
                        point -
                            widthAxis2,
                        color,
                        new Vector2(
                            sample /
                            (float)
                            Math.Max(
                                1,
                                sampleCount - 1),
                            0f));

                _vertices[v + 3] =
                    new VertexPositionColorTexture(
                        point +
                            widthAxis2,
                        color,
                        new Vector2(
                            sample /
                            (float)
                            Math.Max(
                                1,
                                sampleCount - 1),
                            1f));

                _vertexCount +=
                    VerticesPerSample;
            }

            for (int segment = 0;
                 segment <
                    sampleCount - 1;
                 segment++)
            {
                int current =
                    vertexStart +
                    segment *
                    VerticesPerSample;

                int next =
                    current +
                    VerticesPerSample;

                // Face 1
                AddTriangle(
                    current + 0,
                    current + 1,
                    next + 1);

                AddTriangle(
                    current + 0,
                    next + 1,
                    next + 0);

                // Face 2
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

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(
                gameTime);

            if (!Visible ||
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

                gd.BlendState =
                    ClassicAdditive;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

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
                    _indexCount / 3;

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
            // TextureLoader owns this texture.
            //
            _texture =
                null;

            base.Dispose();
        }
    }
}