using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Backend compartido para cualquier primitiva ClassicFX
    /// que termine dibujándose mediante RenderSprite().
    ///
    /// Lo utilizan:
    ///
    /// - ClassicSpriteRenderer
    /// - ClassicParticleRenderer
    ///
    /// y posteriormente cualquier otra familia clásica que
    /// necesite billboards.
    /// </summary>
    internal sealed class ClassicBillboardRenderer :
        IDisposable
    {
        /// <summary>
        /// Particle es actualmente el consumidor más grande:
        ///
        /// MAX_PARTICLES = 3000
        ///
        /// 3000 * 4 = 12000 vértices,
        /// todavía dentro de ushort.
        ///
        /// Algunas partículas generan más de un quad. Cuando el buffer
        /// se llena, Flush() lo envía y continúa sin perder orden.
        /// </summary>
        private const int MaxQuads =
            ClassicFxPools.MaxParticles;

        private const int VerticesPerQuad =
            4;

        private const int IndicesPerQuad =
            6;

        private readonly GraphicsDevice
            _graphicsDevice;

        private readonly BasicEffect
            _basicEffect;

        private readonly AlphaTestEffect
            _alphaTestEffect;

        private readonly DynamicVertexBuffer
            _vertexBuffer;

        private readonly IndexBuffer
            _indexBuffer;

        private readonly
            VertexPositionColorTexture[]
            _vertices =
                new VertexPositionColorTexture[
                    MaxQuads *
                    VerticesPerQuad
                ];

        private ClassicTextureResource
            _batchTexture;

        private ClassicBlendMode
            _batchBlendMode;

        private ClassicDepthMode
            _batchDepthMode;

        private int
            _quadCount;

        private bool
            _disposed;

        public ClassicBillboardRenderer(
            GraphicsDevice graphicsDevice)
        {
            _graphicsDevice =
                graphicsDevice ??
                throw new ArgumentNullException(
                    nameof(graphicsDevice));

            _basicEffect =
                new BasicEffect(
                    graphicsDevice)
                {
                    TextureEnabled =
                        true,

                    VertexColorEnabled =
                        true,

                    LightingEnabled =
                        false,

                    FogEnabled =
                        false,

                    World =
                        Matrix.Identity,

                    View =
                        Matrix.Identity
                };

            _alphaTestEffect =
                new AlphaTestEffect(
                    graphicsDevice)
                {
                    VertexColorEnabled =
                        true,

                    AlphaFunction =
                        CompareFunction.Greater,

                    ReferenceAlpha =
                        2,

                    World =
                        Matrix.Identity,

                    View =
                        Matrix.Identity
                };

            _vertexBuffer =
                new DynamicVertexBuffer(
                    graphicsDevice,
                    VertexPositionColorTexture
                        .VertexDeclaration,

                    MaxQuads *
                    VerticesPerQuad,

                    BufferUsage.WriteOnly);

            ushort[] indices =
                new ushort[
                    MaxQuads *
                    IndicesPerQuad
                ];

            for (int i = 0;
                 i < MaxQuads;
                 i++)
            {
                int vertex =
                    i *
                    VerticesPerQuad;

                int index =
                    i *
                    IndicesPerQuad;

                indices[index + 0] =
                    (ushort)(vertex + 0);

                indices[index + 1] =
                    (ushort)(vertex + 1);

                indices[index + 2] =
                    (ushort)(vertex + 2);

                indices[index + 3] =
                    (ushort)(vertex + 0);

                indices[index + 4] =
                    (ushort)(vertex + 2);

                indices[index + 5] =
                    (ushort)(vertex + 3);
            }

            _indexBuffer =
                new IndexBuffer(
                    graphicsDevice,
                    IndexElementSize.SixteenBits,
                    indices.Length,
                    BufferUsage.WriteOnly);

            _indexBuffer.SetData(
                indices);
        }

        public void Begin()
        {
            if (_disposed)
            {
                return;
            }

            _quadCount =
                0;

            _batchTexture =
                null;

            _batchBlendMode =
                ClassicBlendMode.Glow;

            _batchDepthMode =
                ClassicDepthMode.ReadOnly;
        }

        /// <summary>
        /// Equivalente genérico a RenderSprite().
        ///
        /// Position sigue estando en coordenadas mundiales MU.
        /// Width/Height ya vienen resueltos por el primitive.
        ///
        /// alphaOverride existe por reglas del RenderSprite original
        /// donde ciertas texturas RGBA fuerzan alpha = 1.0.
        /// Particle lo necesita para BITMAP_BLOOD + 1.
        /// </summary>
        public void Queue(
            ClassicTextureResource texture,
            Vector3 position,
            float width,
            float height,
            Vector3 light,
            float rotation,
            ClassicBlendMode blendMode,
            ClassicDepthMode depthMode,
            float u = 0f,
            float v = 0f,
            float uWidth = 1f,
            float vHeight = 1f,
            float? alphaOverride = null)
        {
            if (_disposed ||
                texture == null ||
                !texture.IsReady)
            {
                return;
            }

            Vector3 cameraPosition =
                Vector3.Transform(
                    position,
                    Camera.Instance.View);

            // Main RenderSprite():
            //
            // if (p2[2] >= -1.0f)
            //     return;
            //
            if (cameraPosition.Z >=
                -1f)
            {
                return;
            }

            if (_quadCount > 0 &&
                (
                    !ReferenceEquals(
                        _batchTexture,
                        texture) ||

                    _batchBlendMode !=
                        blendMode ||

                    _batchDepthMode !=
                        depthMode
                ))
            {
                Flush();
            }

            if (_quadCount >=
                MaxQuads)
            {
                Flush();
            }

            _batchTexture =
                texture;

            _batchBlendMode =
                blendMode;

            _batchDepthMode =
                depthMode;

            float halfWidth =
                width *
                0.5f;

            float halfHeight =
                height *
                0.5f;

            Vector3 p0;
            Vector3 p1;
            Vector3 p2;
            Vector3 p3;

            if (MathF.Abs(
                    rotation) <
                0.0001f)
            {
                p0 =
                    new Vector3(
                        cameraPosition.X -
                            halfWidth,

                        cameraPosition.Y -
                            halfHeight,

                        cameraPosition.Z);

                p1 =
                    new Vector3(
                        cameraPosition.X +
                            halfWidth,

                        cameraPosition.Y -
                            halfHeight,

                        cameraPosition.Z);

                p2 =
                    new Vector3(
                        cameraPosition.X +
                            halfWidth,

                        cameraPosition.Y +
                            halfHeight,

                        cameraPosition.Z);

                p3 =
                    new Vector3(
                        cameraPosition.X -
                            halfWidth,

                        cameraPosition.Y +
                            halfHeight,

                        cameraPosition.Z);
            }
            else
            {
                float radians =
                    MathHelper.ToRadians(
                        rotation);

                float sin =
                    MathF.Sin(
                        radians);

                float cos =
                    MathF.Cos(
                        radians);

                RotateCorner(
                    -halfWidth,
                    -halfHeight,
                    sin,
                    cos,
                    cameraPosition,
                    out p0);

                RotateCorner(
                    halfWidth,
                    -halfHeight,
                    sin,
                    cos,
                    cameraPosition,
                    out p1);

                RotateCorner(
                    halfWidth,
                    halfHeight,
                    sin,
                    cos,
                    cameraPosition,
                    out p2);

                RotateCorner(
                    -halfWidth,
                    halfHeight,
                    sin,
                    cos,
                    cameraPosition,
                    out p3);
            }

            Color color =
                BuildColor(
                    light,
                    texture.Data.Components,
                    alphaOverride);

            int baseVertex =
                _quadCount *
                VerticesPerQuad;

            _vertices[
                baseVertex + 0
            ] =
                new VertexPositionColorTexture(
                    p0,
                    color,
                    new Vector2(
                        u,
                        v + vHeight));

            _vertices[
                baseVertex + 1
            ] =
                new VertexPositionColorTexture(
                    p1,
                    color,
                    new Vector2(
                        u + uWidth,
                        v + vHeight));

            _vertices[
                baseVertex + 2
            ] =
                new VertexPositionColorTexture(
                    p2,
                    color,
                    new Vector2(
                        u + uWidth,
                        v));

            _vertices[
                baseVertex + 3
            ] =
                new VertexPositionColorTexture(
                    p3,
                    color,
                    new Vector2(
                        u,
                        v));

            _quadCount++;
        }

        public void End()
        {
            if (_disposed)
            {
                return;
            }

            Flush();

            _graphicsDevice
                .SetVertexBuffer(
                    null);

            _graphicsDevice.Indices =
                null;

            _graphicsDevice.BlendState =
                BlendState.Opaque;

            _graphicsDevice.DepthStencilState =
                DepthStencilState.Default;

            _graphicsDevice.RasterizerState =
                RasterizerState.CullCounterClockwise;
        }

        private void Flush()
        {
            if (_quadCount <= 0 ||
                _batchTexture == null ||
                !_batchTexture.IsReady)
            {
                _quadCount =
                    0;

                return;
            }

            int vertexCount =
                _quadCount *
                VerticesPerQuad;

            _vertexBuffer.SetData(
                _vertices,
                0,
                vertexCount,
                SetDataOptions.Discard);

            _graphicsDevice
                .SetVertexBuffer(
                    _vertexBuffer);

            _graphicsDevice.Indices =
                _indexBuffer;

            _graphicsDevice.BlendState =
                ClassicRenderStates
                    .GetBlendState(
                        _batchBlendMode);

            _graphicsDevice.DepthStencilState =
                ClassicRenderStates
                    .GetDepthState(
                        _batchDepthMode);

            _graphicsDevice.RasterizerState =
                RasterizerState.CullNone;

            _graphicsDevice
                .SamplerStates[0] =
                    _batchTexture
                        .SamplerState;

            if (_batchBlendMode ==
                ClassicBlendMode.AlphaTest)
            {
                _alphaTestEffect.Texture =
                    _batchTexture.Texture;

                _alphaTestEffect.Projection =
                    Camera.Instance.Projection;

                DrawEffect(
                    _alphaTestEffect);
            }
            else
            {
                _basicEffect.Texture =
                    _batchTexture.Texture;

                _basicEffect.Projection =
                    Camera.Instance.Projection;

                DrawEffect(
                    _basicEffect);
            }

            _quadCount =
                0;
        }

        private void DrawEffect(
            Effect effect)
        {
            foreach (EffectPass pass in
                     effect
                         .CurrentTechnique
                         .Passes)
            {
                pass.Apply();

                _graphicsDevice
                    .DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        _quadCount *
                        2);
            }
        }

        private static void RotateCorner(
            float dx,
            float dy,
            float sin,
            float cos,
            Vector3 center,
            out Vector3 result)
        {
            result =
                new Vector3(
                    center.X +
                    dx * cos -
                    dy * sin,

                    center.Y +
                    dx * sin +
                    dy * cos,

                    center.Z);
        }

        private static Color BuildColor(
            Vector3 light,
            int components,
            float? alphaOverride)
        {
            float r =
                MathHelper.Clamp(
                    light.X,
                    0f,
                    1f);

            float g =
                MathHelper.Clamp(
                    light.Y,
                    0f,
                    1f);

            float b =
                MathHelper.Clamp(
                    light.Z,
                    0f,
                    1f);

            // Main RenderSprite:
            //
            // RGB:
            //     alpha = 1
            //
            // RGBA:
            //     alpha = Light[0]
            //
            // Excepciones como BITMAP_BLOOD + 1:
            //     alpha = 1
            float alpha =
                alphaOverride.HasValue
                    ? MathHelper.Clamp(
                        alphaOverride.Value,
                        0f,
                        1f)
                    : components == 3
                        ? 1f
                        : r;

            return new Color(
                r,
                g,
                b,
                alpha);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _vertexBuffer?
                .Dispose();

            _indexBuffer?
                .Dispose();

            _basicEffect?
                .Dispose();

            _alphaTestEffect?
                .Dispose();

            _disposed =
                true;
        }
    }
}
