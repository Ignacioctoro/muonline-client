using System;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Reimplementation of the persistent MODEL_SPEARSKILL joint
    /// used by the classic MU client.
    ///
    /// Initial supported subtypes:
    ///
    /// 0 = Soul Barrier
    /// 4 = Greater Defense / Greater Damage / Elf Soldier
    ///
    /// Original behavior reference:
    /// ZzzEffectJoint.cpp
    ///
    /// The old client creates five MODEL_SPEARSKILL joints around
    /// the character. Instead of allocating five WorldObjects, this
    /// MonoGame implementation renders the five trails in one object.
    /// </summary>
    public sealed class ClassicSpearJointEffect : WorldObject
    {
        private const int JointCount = 5;

        private const int MaxTails = 30;

        // 30 segments require 31 points.
        private const int SamplesPerJoint =
            MaxTails + 1;

        // Each sample contains:
        //
        // 0/1 = horizontal ribbon
        // 2/3 = vertical ribbon
        //
        // This reproduces the two crossed faces stored in the
        // original Tails[][4] array.
        private const int VerticesPerSample = 4;

        private const int VerticesPerJoint =
            SamplesPerJoint * VerticesPerSample;

        private const int TotalVertices =
            JointCount * VerticesPerJoint;

        // Two quads per segment.
        // Each quad = two triangles = six indices.
        private const int IndicesPerSegment = 12;

        private const int TotalIndices =
            JointCount *
            MaxTails *
            IndicesPerSegment;

        private const float ClassicFrameMs = 40.0f;

        private const float JointWidth = 20.0f;

        private const string FlareTexturePath =
            "Effect/flareBlue.jpg";

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

        private readonly int _subType;

        private readonly VertexPositionColorTexture[] _vertices =
            new VertexPositionColorTexture[TotalVertices];

        private readonly ushort[] _indices =
            new ushort[TotalIndices];

        private readonly Vector3[] _markerPositions =
            new Vector3[JointCount];

        private readonly Vector3[] _markerLights =
            new Vector3[JointCount];

        private Texture2D _flareTexture;

        private BasicEffect _effect;

        private DynamicVertexBuffer _vertexBuffer;

        private IndexBuffer _indexBuffer;

        public PlayerObject Owner =>
            _owner;

        public int SubType =>
            _subType;

        public ClassicSpearJointEffect(
            PlayerObject owner,
            int subType)
        {
            _owner =
                owner ??
                throw new ArgumentNullException(
                    nameof(owner));
            // The effect renders its vertices directly in world space,
            // but WorldControl still uses this WorldObject's Position /
            // BoundingBoxWorld for frustum culling and depth sorting.
            //
            // Keep this container physically attached to the player.
            Position =
                _owner.WorldPosition.Translation;

            // MODEL_SPEARSKILL reaches well above and around the player.
            // The default WorldObject bounding box is too small for it.
            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -160.0f,
                        -160.0f,
                        -40.0f),
                    new Vector3(
                        160.0f,
                        160.0f,
                        300.0f));

            if (subType != 0 &&
                subType != 4)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(subType),
                    subType,
                    "This first implementation supports only classic SPEARSKILL subtypes 0 and 4.");
            }

            _subType =
                subType;

            Interactive = false;

            IsTransparent = true;

            AffectedByTransparency = true;

            BlendState =
                ClassicAdditive;

            DepthState =
                GraphicsManager.ReadOnlyDepth;

            BuildIndexBufferData();
        }

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await TextureLoader.Instance.Prepare(
                FlareTexturePath);

            _flareTexture =
                TextureLoader.Instance.GetTexture2D(
                    FlareTexturePath);

            if (_flareTexture == null)
            {
                Status =
                    GameControlStatus.Error;

                return;
            }

            _effect =
                new BasicEffect(
                    GraphicsDevice)
                {
                    TextureEnabled = true,
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    FogEnabled = false
                };

            _vertexBuffer =
                new DynamicVertexBuffer(
                    GraphicsDevice,
                    typeof(VertexPositionColorTexture),
                    TotalVertices,
                    BufferUsage.WriteOnly);

            _indexBuffer =
                new IndexBuffer(
                    GraphicsDevice,
                    IndexElementSize.SixteenBits,
                    TotalIndices,
                    BufferUsage.WriteOnly);

            _indexBuffer.SetData(
                _indices);
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }

            if (_owner == null ||
                _owner.Status ==
                    GameControlStatus.Disposed ||
                _owner.World == null)
            {
                RemoveSelf();
                return;
            }
            // IMPORTANT:
            // WorldControl performs frustum culling using this object's
            // BoundingBoxWorld. The actual spear vertices are generated from
            // the owner, but the container itself must follow the player too.
            Position =
                _owner.WorldPosition.Translation;

            bool shouldHide =
                _owner.Hidden ||
                _owner.IsDead ||
                _owner.Status !=
                    GameControlStatus.Ready;

            Hidden =
                shouldHide;

            if (shouldHide)
            {
                return;
            }

            BuildGeometry(
                gameTime.TotalGameTime
                    .TotalMilliseconds);

            _vertexBuffer?.SetData(
                _vertices,
                0,
                TotalVertices,
                SetDataOptions.Discard);
        }

        private void BuildGeometry(
            double totalMilliseconds)
        {
            Vector3 targetPosition =
                _owner.WorldPosition.Translation;

            // SourceMain does:
            //
            // VectorCopy(target->Position, TargetPosition);
            // TargetPosition[2] += 10.f;
            //
            targetPosition.Z += 10.0f;

            float halfWidth =
                JointWidth * 0.5f;

            float yaw =
                _owner.TotalAngle.Z;

            float cosYaw =
                MathF.Cos(yaw);

            float sinYaw =
                MathF.Sin(yaw);

            // Equivalent to rotating local X by the character Z angle.
            Vector3 horizontalAxis =
                new Vector3(
                    cosYaw,
                    sinYaw,
                    0.0f);

            Vector3 verticalAxis =
                Vector3.UnitZ;

            int classicFrame =
                (int)(
                    totalMilliseconds /
                    ClassicFrameMs);

            Vector3 baseLight =
                _subType == 0
                    ? Vector3.One
                    : new Vector3(
                        0.4f,
                        0.8f,
                        0.2f);

            for (int joint = 0;
                 joint < JointCount;
                 joint++)
            {
                int vertexStart =
                    joint *
                    VerticesPerJoint;

                for (int sample = 0;
                     sample < SamplesPerJoint;
                     sample++)
                {
                    // Sample previous classic 40 ms ticks.
                    //
                    // This gives us the same history that SourceMain
                    // stored incrementally in Tails[].
                    int historicalFrame =
                        classicFrame -
                        sample;

                    int frame =
                        ((joint & 1) != 0
                            ? -historicalFrame
                            : historicalFrame)
                        + joint * 53731;

                    Vector3 direction =
                        CalculateClassicDirection(
                            frame);

                    Vector3 point;

                    // ZzzEffectJoint:
                    //
                    // subtype 0 / 4:
                    //
                    // X = targetX + dirX * 80
                    // Y = targetY + dirY * 80
                    // Z = 110 + targetZ + dirZ * 120
                    //
                    point.X =
                        targetPosition.X +
                        direction.X * 80.0f;

                    point.Y =
                        targetPosition.Y +
                        direction.Y * 80.0f;

                    point.Z =
                        targetPosition.Z +
                        110.0f +
                        direction.Z * 120.0f;

                    Vector3 light =
                        CalculateHeightLight(
                            point,
                            targetPosition,
                            baseLight);

                    float u =
                        1.0f -
                        sample /
                        (float)MaxTails;

                    int v =
                        vertexStart +
                        sample *
                        VerticesPerSample;

                    Vector3 horizontal =
                        horizontalAxis *
                        halfWidth;

                    Vector3 vertical =
                        verticalAxis *
                        halfWidth;

                    Color color =
                        ToColor(light);

                    // Horizontal crossed ribbon.
                    _vertices[v + 0] =
                        new VertexPositionColorTexture(
                            point - horizontal,
                            color,
                            new Vector2(
                                u,
                                0.0f));

                    _vertices[v + 1] =
                        new VertexPositionColorTexture(
                            point + horizontal,
                            color,
                            new Vector2(
                                u,
                                1.0f));

                    // Vertical crossed ribbon.
                    _vertices[v + 2] =
                        new VertexPositionColorTexture(
                            point - vertical,
                            color,
                            new Vector2(
                                u,
                                0.0f));

                    _vertices[v + 3] =
                        new VertexPositionColorTexture(
                            point + vertical,
                            color,
                            new Vector2(
                                u,
                                1.0f));

                    // SourceMain creates an extra BITMAP_FLARE_BLUE
                    // sprite at NumTails / 2.
                    if (sample ==
                        MaxTails / 2)
                    {
                        _markerPositions[joint] =
                            point;

                        _markerLights[joint] =
                            light;
                    }
                }
            }
        }

        /// <summary>
        /// Exact direction formula used by MODEL_SPEARSKILL
        /// in ZzzEffectJoint.cpp.
        /// </summary>
        private static Vector3 CalculateClassicDirection(
            int frame)
        {
            const float speed0 =
                0.048f;

            const float speed1 =
                0.0613f;

            const float speed2 =
                0.1113f;

            float first =
                (frame + 55555) *
                speed0;

            float second =
                frame *
                speed1;

            float tempX =
                MathF.Sin(first) *
                MathF.Cos(second);

            float tempY =
                MathF.Sin(first) *
                MathF.Sin(second);

            float tempZ =
                MathF.Cos(first);

            float extra =
                (frame + 11111) *
                speed2;

            float sinAdd =
                MathF.Sin(extra);

            float cosAdd =
                MathF.Cos(extra);

            Vector3 direction;

            direction.Z =
                tempX;

            direction.Y =
                sinAdd *
                    tempY +
                cosAdd *
                    tempZ;

            direction.X =
                cosAdd *
                    tempY -
                sinAdd *
                    tempZ;

            return direction;
        }

        private static Vector3 CalculateHeightLight(
            Vector3 point,
            Vector3 targetPosition,
            Vector3 baseLight)
        {
            // SourceMain:
            //
            // fJointHeight =
            // (tailZ - (targetZ + 50)) * 0.01
            //
            float heightFade =
                (
                    point.Z -
                    (
                        targetPosition.Z +
                        50.0f
                    )
                ) *
                0.01f;

            if (heightFade <= 0.0f)
            {
                return baseLight;
            }

            return new Vector3(
                MathHelper.Clamp(
                    baseLight.X -
                    heightFade,
                    0.0f,
                    1.0f),

                MathHelper.Clamp(
                    baseLight.Y -
                    heightFade,
                    0.0f,
                    1.0f),

                MathHelper.Clamp(
                    baseLight.Z -
                    heightFade,
                    0.0f,
                    1.0f));
        }

        private void BuildIndexBufferData()
        {
            int output =
                0;

            for (int joint = 0;
                 joint < JointCount;
                 joint++)
            {
                int vertexStart =
                    joint *
                    VerticesPerJoint;

                for (int segment = 0;
                     segment < MaxTails;
                     segment++)
                {
                    int current =
                        vertexStart +
                        segment *
                        VerticesPerSample;

                    int next =
                        current +
                        VerticesPerSample;

                    // Horizontal ribbon.
                    _indices[output++] =
                        (ushort)(current + 0);

                    _indices[output++] =
                        (ushort)(current + 1);

                    _indices[output++] =
                        (ushort)(next + 1);

                    _indices[output++] =
                        (ushort)(current + 0);

                    _indices[output++] =
                        (ushort)(next + 1);

                    _indices[output++] =
                        (ushort)(next + 0);

                    // Vertical ribbon.
                    _indices[output++] =
                        (ushort)(current + 2);

                    _indices[output++] =
                        (ushort)(current + 3);

                    _indices[output++] =
                        (ushort)(next + 3);

                    _indices[output++] =
                        (ushort)(current + 2);

                    _indices[output++] =
                        (ushort)(next + 3);

                    _indices[output++] =
                        (ushort)(next + 2);
                }
            }
        }

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible ||
                _flareTexture == null ||
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

            IndexBuffer previousIndexBuffer = gd.Indices;

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
                    _flareTexture;

                _effect.Alpha =
                    TotalAlpha;

                int primitiveCount =
                    TotalIndices / 3;

                foreach (EffectPass pass
                    in _effect.CurrentTechnique.Passes)
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
                    previousIndexBuffer;

                gd.SetVertexBuffer(null);
            }

            DrawClassicMidFlares();
        }

        private void DrawClassicMidFlares()
        {
            if (_flareTexture == null)
            {
                return;
            }

            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    ClassicAdditive,
                    SamplerState.LinearClamp,
                    GraphicsManager.ReadOnlyDepth,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                     i < JointCount;
                     i++)
                {
                    DrawWorldSprite(
                        spriteBatch,
                        _markerPositions[i],
                        _markerLights[i],
                        0.7f);
                }
            }
        }

        private void DrawWorldSprite(
            SpriteBatch spriteBatch,
            Vector3 worldPosition,
            Vector3 light,
            float classicScale)
        {
            Matrix view =
                Camera.Instance.View;

            Matrix projection =
                Camera.Instance.Projection;

            Vector3 cameraPosition =
                Vector3.Transform(
                    worldPosition,
                    view);

            float cameraDepth =
                -cameraPosition.Z;

            if (cameraDepth <=
                Camera.Instance.ViewNear)
            {
                return;
            }

            Viewport viewport =
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

            float pixelsPerCameraUnit =
                viewport.Height *
                MathF.Abs(
                    projection.M22) /
                (
                    2.0f *
                    cameraDepth
                );

            float spriteScale =
                classicScale *
                pixelsPerCameraUnit;

            if (!float.IsFinite(
                    spriteScale) ||
                spriteScale <= 0.0f)
            {
                return;
            }

            Color color =
                ToColor(light) *
                TotalAlpha;

            Vector2 origin =
                new Vector2(
                    _flareTexture.Width *
                        0.5f,
                    _flareTexture.Height *
                        0.5f);

            spriteBatch.Draw(
                _flareTexture,
                new Vector2(
                    projected.X,
                    projected.Y),
                null,
                color,
                0.0f,
                origin,
                spriteScale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    projected.Z,
                    0.0f,
                    1.0f));
        }

        private static Color ToColor(
            Vector3 light)
        {
            return new Color(
                MathHelper.Clamp(
                    light.X,
                    0.0f,
                    1.0f),

                MathHelper.Clamp(
                    light.Y,
                    0.0f,
                    1.0f),

                MathHelper.Clamp(
                    light.Z,
                    0.0f,
                    1.0f),

                1.0f);
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
            _vertexBuffer = null;

            _indexBuffer?.Dispose();
            _indexBuffer = null;

            _effect?.Dispose();
            _effect = null;

            // TextureLoader owns the cached texture.
            _flareTexture = null;

            base.Dispose();
        }
    }
}