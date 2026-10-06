using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Renderer de OBJECT Sprites[MAX_SPRITES].
    ///
    /// Reproduce RenderSprite() trabajando en camera-space,
    /// igual que el cliente clásico:
    ///
    /// world Position
    ///      ->
    /// Camera.View
    ///      ->
    /// quad construido sobre XY de la cámara
    ///      ->
    /// Projection
    ///
    /// Esto evita el sistema SpriteObject de Neffis,
    /// que proyecta a pantalla y luego reconstruye manualmente
    /// una escala dependiente de distancia.
    /// </summary>
    internal sealed class ClassicSpriteRenderer :
        IDisposable
    {
        private const int
            MaxSprites =
                ClassicFxPools.MaxSprites;

        private const int
            VerticesPerSprite =
                4;

        private const int
            IndicesPerSprite =
                6;

        private readonly
            GraphicsDevice
            _graphicsDevice;

        private readonly
            ClassicTextureRepository
            _textures;

        private readonly
            BasicEffect
            _basicEffect;

        private readonly
            AlphaTestEffect
            _alphaTestEffect;

        private readonly
            DynamicVertexBuffer
            _vertexBuffer;

        private readonly
            IndexBuffer
            _indexBuffer;

        /// <summary>
        /// Scratch CPU fijo.
        ///
        /// No hay allocations por frame.
        /// </summary>
        private readonly
            VertexPositionColorTexture[]
            _vertices =
                new VertexPositionColorTexture[
                    MaxSprites *
                    VerticesPerSprite
                ];

        private ClassicTextureResource
            _batchTexture;

        private ClassicSpriteBlendMode
            _batchBlendMode;

        private int
            _quadCount;

        private bool
            _disposed;

        // =============================================================
        // CLASSIC FX TEMPORARY DEBUG
        //
        // Se eliminan después de validar completamente
        // ClassicSpriteRenderer.
        // =============================================================

        private bool
            _debugFirstQueueLogged;

        private bool
            _debugFirstFlushLogged;

        public ClassicSpriteRenderer(
            GraphicsDevice graphicsDevice,
            ClassicTextureRepository textures)
        {
            _graphicsDevice =
                graphicsDevice ??
                throw new ArgumentNullException(
                    nameof(graphicsDevice));

            _textures =
                textures ??
                throw new ArgumentNullException(
                    nameof(textures));

            _basicEffect =
                new BasicEffect(
                    _graphicsDevice)
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
                    _graphicsDevice)
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
                    _graphicsDevice,
                    VertexPositionColorTexture
                        .VertexDeclaration,

                    MaxSprites *
                    VerticesPerSprite,

                    BufferUsage.WriteOnly);

            ushort[] indices =
                new ushort[
                    MaxSprites *
                    IndicesPerSprite
                ];

            for (int i = 0;
                 i < MaxSprites;
                 i++)
            {
                int vertex =
                    i *
                    VerticesPerSprite;

                int index =
                    i *
                    IndicesPerSprite;

                indices[
                    index + 0
                ] =
                    (ushort)(
                        vertex + 0);

                indices[
                    index + 1
                ] =
                    (ushort)(
                        vertex + 1);

                indices[
                    index + 2
                ] =
                    (ushort)(
                        vertex + 2);

                indices[
                    index + 3
                ] =
                    (ushort)(
                        vertex + 0);

                indices[
                    index + 4
                ] =
                    (ushort)(
                        vertex + 2);

                indices[
                    index + 5
                ] =
                    (ushort)(
                        vertex + 3);
            }

            _indexBuffer =
                new IndexBuffer(
                    _graphicsDevice,
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
                ClassicSpriteBlendMode.Glow;
        }

        /// <summary>
        /// Equivalente a RenderSprite(OBJECT*).
        ///
        /// No hace un drawcall inmediatamente.
        /// Agrega el quad al batch actual preservando el orden.
        /// </summary>
        public void QueueSprite(
            in ClassicSprite sprite)
        {
            if (_disposed)
            {
                return;
            }

            if (!_textures.TryGet(
                    sprite.Type,
                    out ClassicTextureResource resource))
            {
                return;
            }

            Vector3 cameraPosition =
                Vector3.Transform(
                    sprite.Position,
                    Camera.Instance.View);

            // =========================================================
            // DEBUG TEMPORAL
            // =========================================================

            if (!_debugFirstQueueLogged)
            {
                Vector3 projected =
                    _graphicsDevice
                        .Viewport
                        .Project(
                            sprite.Position,
                            Camera.Instance.Projection,
                            Camera.Instance.View,
                            Matrix.Identity);

                Console.WriteLine(
                    $"[ClassicFX][Sprite] QUEUE " +
                    $"Type={sprite.Type}, " +
                    $"World={sprite.Position}, " +
                    $"Camera={cameraPosition}, " +
                    $"Projected={projected}, " +
                    $"Texture={resource.Data.Width}x{resource.Data.Height}, " +
                    $"Components={resource.Data.Components}, " +
                    $"Viewport=" +
                    $"{_graphicsDevice.Viewport.Width}x" +
                    $"{_graphicsDevice.Viewport.Height}");

                _debugFirstQueueLogged =
                    true;
            }

            // Main:
            //
            // if (z >= -1.0f)
            //     return;
            //
            if (cameraPosition.Z >=
                -1f)
            {
                return;
            }

            ClassicSpriteBlendMode blendMode =
                GetBlendMode(
                    sprite.Type,
                    sprite.SubType);

            if (_quadCount > 0 &&
                (
                    !ReferenceEquals(
                        _batchTexture,
                        resource) ||

                    _batchBlendMode !=
                        blendMode
                ))
            {
                Flush();
            }

            if (_quadCount >=
                MaxSprites)
            {
                Flush();
            }

            _batchTexture =
                resource;

            _batchBlendMode =
                blendMode;

            float width;
            float height;

            float u =
                0f;

            float v =
                0f;

            float uWidth =
                1f;

            float vHeight =
                1f;

            if (sprite.Type ==
                ClassicTextureIds.BitmapFormationMark)
            {
                width =
                    64f;

                height =
                    64f;

                GetFormationMarkUv(
                    sprite.SubType,
                    out u,
                    out v,
                    out uWidth,
                    out vHeight);
            }
            else
            {
                float renderScale =
                    sprite.AnimationFrame *
                    sprite.Scale;

                // Main:
                //
                // Width =
                //     bitmap.Width *
                //     AnimationFrame *
                //     Scale;
                //
                // Height =
                //     bitmap.Height *
                //     AnimationFrame *
                //     Scale;
                //
                width =
                    resource.Data.Width *
                    renderScale;

                height =
                    resource.Data.Height *
                    renderScale;
            }

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
                    sprite.Rotation) <
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
                        sprite.Rotation);

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
                    sprite.Light,
                    resource.Data.Components);

            int baseVertex =
                _quadCount *
                VerticesPerSprite;

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

            // Dejamos estados neutros para el pipeline
            // que continúa después de ClassicFX.

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

            // =========================================================
            // DEBUG TEMPORAL
            // =========================================================

            if (!_debugFirstFlushLogged)
            {
                Console.WriteLine(
                    $"[ClassicFX][Sprite] FLUSH " +
                    $"Quads={_quadCount}, " +
                    $"Texture={_batchTexture.Type}, " +
                    $"Path={_batchTexture.Path}, " +
                    $"Blend={_batchBlendMode}");

                _debugFirstFlushLogged =
                    true;
            }

            int vertexCount =
                _quadCount *
                VerticesPerSprite;

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

            // =========================================================
            // TEMPORAL:
            //
            // Estamos comparando el BlendState.Additive conocido
            // de MonoGame contra ClassicRenderStates.Glow.
            //
            // DepthRead se mantiene porque ya comprobamos que
            // el depth test funciona correctamente.
            //
            // Si el fondo negro desaparece con este estado,
            // entonces la siguiente corrección estará en
            // ClassicRenderStates.Glow, no aquí.
            // =========================================================

            _graphicsDevice.BlendState =
                ClassicRenderStates
                    .GetBlendState(
                        _batchBlendMode);

            _graphicsDevice.DepthStencilState =
                ClassicRenderStates
                    .GetDepthState(
                        _batchBlendMode);

            _graphicsDevice.RasterizerState =
                RasterizerState.CullNone;

            _graphicsDevice
                .SamplerStates[0] =
                    _batchTexture
                        .SamplerState;

            if (_batchBlendMode ==
                ClassicSpriteBlendMode.AlphaTest)
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
                        _quadCount * 2);
            }
        }

        private static
            ClassicSpriteBlendMode
            GetBlendMode(
                int type,
                int subType)
        {
            if (type ==
                ClassicTextureIds.BitmapFormationMark)
            {
                return
                    ClassicSpriteBlendMode.AlphaTest;
            }

            // zzzeffectsprite.cpp:
            //
            // subtype 0 -> EnableAlphaBlend()
            // subtype 1 -> EnableAlphaBlendMinus()
            // subtype 2 -> EnableAlphaTest()
            // subtype 3 -> EnableAlphaBlend2()
            //
            return subType switch
            {
                0 =>
                    ClassicSpriteBlendMode.Glow,

                1 =>
                    ClassicSpriteBlendMode.Subtract,

                2 =>
                    ClassicSpriteBlendMode.AlphaTest,

                3 =>
                    ClassicSpriteBlendMode.Luminance,

                _ =>
                    ClassicSpriteBlendMode.Glow
            };
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
            int components)
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
            float alpha =
                components == 3
                    ? 1f
                    : r;

            return
                new Color(
                    r,
                    g,
                    b,
                    alpha);
        }

        private static void GetFormationMarkUv(
            int subType,
            out float u,
            out float v,
            out float uWidth,
            out float vHeight)
        {
            u =
                0f;

            v =
                0f;

            uWidth =
                0.33f;

            vHeight =
                0.33f;

            switch (subType)
            {
                case 0:
                    u =
                        0f;

                    v =
                        0f;

                    break;

                case 1:
                    u =
                        0.33f;

                    v =
                        0f;

                    break;

                case 2:
                    u =
                        0.66f;

                    v =
                        0f;

                    break;

                case 3:
                    u =
                        0f;

                    v =
                        0.33f;

                    break;

                case 4:
                    u =
                        0.33f;

                    v =
                        0.33f;

                    break;

                case 5:
                    u =
                        0.66f;

                    v =
                        0.33f;

                    break;

                case 6:
                    u =
                        0f;

                    v =
                        0.66f;

                    break;

                case 7:
                    u =
                        0.33f;

                    v =
                        0.66f;

                    break;
            }
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