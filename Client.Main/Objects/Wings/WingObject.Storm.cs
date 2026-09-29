using Client.Main;
using Client.Main.Graphics;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Wings
{
    public partial class WingObject
    {
        // ================================================================
        // WING OF STORM
        // Item group 12, index 36
        //
        // Classic model:
        //     Wing08.bmd
        //
        // Classic client special rendering:
        //
        // Mesh 2:
        //     RENDER_TEXTURE
        //
        // Mesh 0:
        //     RENDER_TEXTURE | RENDER_BRIGHT
        //
        // Mesh 1:
        //     RENDER_TEXTURE | RENDER_BRIGHT
        //     animated U coordinate:
        //
        //         0.00
        //         0.25
        //         0.50
        //         0.75
        //
        // Bone effects:
        //
        //     25 x BITMAP_CLUD64
        //     22 x BITMAP_LIGHT
        //
        // Thunder is intentionally NOT implemented yet.
        // ================================================================

        private const short StormWingItemIndex =
            36;


        private const string StormCloudTexturePath =
            "Effect/clud64.jpg";


        private const string StormLightTexturePath =
            "Effect/flare01.jpg";


        private const string StormEffectAssetName =
            "WingS3";


        // ================================================================
        // ORIGINAL CLASSIC MU BONE TABLES
        // ================================================================

        private static readonly int[] StormCloudBones =
        {
            9,
            20,
            19,
            10,
            18,

            28,
            27,
            36,
            35,
            38,

            37,
            53,
            48,
            62,
            70,

            72,
            71,
            78,
            79,
            80,

            87,
            90,
            91,
            106,
            102
        };


        private static readonly int[] StormLightBones =
        {
            64,
            61,
            69,
            77,
            86,

            98,
            97,
            99,
            104,
            103,

            105,
            12,
            8,
            17,
            26,

            34,
            52,
            44,
            51,
            50,

            49,
            45
        };


        private static readonly int[] StormThunderBones =
        {
            11,
            21,
            29,
            63,
            81,
            89
        };


        // ================================================================
        // CLASSIC GL_ONE + GL_ONE
        // ================================================================

        private static readonly BlendState StormBrightAdditive =
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


        // Classic CreateSprite(..., SubType = 1):
        // glBlendFunc(GL_ZERO, GL_ONE_MINUS_SRC_COLOR)
        private static readonly BlendState StormCloudBlendMinus =
            new BlendState
            {
                ColorBlendFunction =
                    BlendFunction.Add,

                ColorSourceBlend =
                    Blend.Zero,

                ColorDestinationBlend =
                    Blend.InverseSourceColor,

                AlphaBlendFunction =
                    BlendFunction.Add,

                AlphaSourceBlend =
                    Blend.Zero,

                AlphaDestinationBlend =
                    Blend.One
            };


        // ================================================================
        // RESOURCES
        // ================================================================

        private Texture2D _stormCloudTexture;

        private Texture2D _stormLightTexture;

        private bool _stormTexturesPrepared;


        private Effect _stormEffect;

        private bool _stormEffectLoadAttempted;


        // GameTime saved by WingObject.Draw().
        //
        // DrawModel() itself does not receive GameTime, so we store the
        // current render time immediately before base.Draw().
        private float _stormRenderTimeSeconds;


        private const float StormClassicEffectTickRate =
            25.0f;

        private const int StormThunderPoolSize =
            8;

        private float _stormThunderAccumulator;

        private readonly StormThunderEffect[] _stormThunderPool =
            new StormThunderEffect[StormThunderPoolSize];

        private int _stormThunderPoolIndex;

        private bool _stormThunderPoolInitialized;


        private bool IsStormWing =>
            ItemIndex ==
            StormWingItemIndex;


        // ================================================================
        // GPU / CPU RENDERING
        // ================================================================

        /// <summary>
        /// Wing of Storm uses its special classic multipass renderer.
        ///
        /// Keeping the normal dynamic-light shader disabled for Storm also
        /// keeps the classic CPU-rendered buffers available to our renderer.
        /// </summary>
        protected override bool AllowDynamicLightingShader =>
            !IsStormWing;


        // ================================================================
        // FRAME TIME
        // ================================================================

        private void UpdateStormRenderTime(
            GameTime gameTime)
        {
            _stormRenderTimeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;
        }


        // ================================================================
        // ASSET LOADING
        // ================================================================

        private async Task PrepareStormClassicAssetsAsync()
        {
            EnsureStormThunderPool();

            if (_stormTexturesPrepared)
            {
                return;
            }


            await Task.WhenAll(
                TextureLoader.Instance.Prepare(
                    StormCloudTexturePath),

                TextureLoader.Instance.Prepare(
                    StormLightTexturePath));


            _stormTexturesPrepared =
                true;
        }


        private void EnsureStormTextures()
        {
            if (!_stormTexturesPrepared)
            {
                return;
            }


            if (_stormCloudTexture == null ||
                _stormCloudTexture.IsDisposed)
            {
                _stormCloudTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            StormCloudTexturePath);
            }


            if (_stormLightTexture == null ||
                _stormLightTexture.IsDisposed)
            {
                _stormLightTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            StormLightTexturePath);
            }
        }


        // ================================================================
        // SHADER
        // ================================================================

        /// <summary>
        /// Load WingS3.fx lazily on the render thread.
        ///
        /// This follows the same strategy currently used by Grade15Effect.
        /// </summary>
        private void EnsureStormEffect()
        {
            if (_stormEffectLoadAttempted)
            {
                return;
            }


            _stormEffectLoadAttempted =
                true;


            try
            {
                _stormEffect =
                    MuGame.Instance.Content
                        .Load<Effect>(
                            StormEffectAssetName);
            }
            catch
            {
                _stormEffect =
                    null;
            }
        }


        // ================================================================
        // CLASSIC MODEL RENDERER
        // ================================================================

        /// <summary>
        /// Wing of Storm has its own mesh recipe in the original client.
        ///
        /// We therefore bypass ModelObject's normal per-mesh rendering only
        /// for item 12,36.
        /// </summary>
        public override void DrawModel(
            bool isAfterDraw)
        {
            // ============================================================
            // WING OF STORM
            // ============================================================

            if (IsStormWing)
            {
                if (isAfterDraw)
                {
                    return;
                }

                if (!TryDrawStormModel())
                {
                    base.DrawModel(
                        false);
                }

                return;
            }


            // ============================================================
            // RESTO DE ALAS
            // Eternal incluida: usa renderer normal de Neffis
            // ============================================================

            base.DrawModel(
                isAfterDraw);
        }


        private bool TryDrawStormModel()
        {
            if (Model?.Meshes == null ||
                Model.Meshes.Length < 3)
            {
                return false;
            }


            EnsureStormEffect();


            if (_stormEffect == null)
            {
                return false;
            }


            // Check all three meshes BEFORE drawing anything.
            //
            // This prevents one half of a frame from using the classic
            // renderer while another half falls back to ModelObject.
            if (!TryGetDerivedMeshRenderData(
                    0,
                    out _,
                    out _,
                    out _) ||

                !TryGetDerivedMeshRenderData(
                    1,
                    out _,
                    out _,
                    out _) ||

                !TryGetDerivedMeshRenderData(
                    2,
                    out _,
                    out _,
                    out _))
            {
                return false;
            }


            GraphicsDevice gd =
                GraphicsDevice;


            BlendState previousBlend =
                gd.BlendState;


            DepthStencilState previousDepth =
                gd.DepthStencilState;


            RasterizerState previousRasterizer =
                gd.RasterizerState;


            try
            {
                gd.RasterizerState =
                    RasterizerState.CullNone;


                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;


                // ========================================================
                // MESH 2
                //
                // Original:
                //
                // Vector(
                //     1.0f,
                //     0.7f,
                //     0.5f,
                //     BodyLight);
                //
                // RenderMesh(
                //     2,
                //     RENDER_TEXTURE);
                // ========================================================

                gd.BlendState =
                    BlendState.AlphaBlend;


                DrawStormMesh(
                    2,

                    new Vector3(
                        1.0f,
                        0.7f,
                        0.5f),

                    Vector2.Zero);


                // ========================================================
                // MESH 0
                //
                // Original:
                //
                // RenderMesh(
                //     0,
                //     RENDER_TEXTURE |
                //     RENDER_BRIGHT);
                // ========================================================

                gd.BlendState =
                    StormBrightAdditive;


                DrawStormMesh(
                    0,

                    new Vector3(
                        1.0f,
                        0.7f,
                        0.5f),

                    Vector2.Zero);


                // ========================================================
                // MESH 1
                //
                // Original:
                //
                // static int s_iTexAni;
                //
                // s_iTexAni++;
                //
                // if (s_iTexAni > 15)
                //     s_iTexAni = 0;
                //
                // fU =
                //     (s_iTexAni / 4)
                //     * 0.25f;
                //
                // Vector(
                //     0.9f,
                //     0.6f,
                //     0.3f,
                //     BodyLight);
                //
                // RenderMesh(
                //     1,
                //     RENDER_TEXTURE |
                //     RENDER_BRIGHT,
                //     ...,
                //     fU);
                //
                //
                // We make this TIME BASED rather than monitor-refresh based.
                // Otherwise 120/144 Hz would make the old frame counter run
                // absurdly fast.
                // ========================================================

                int animationFrame =
                    (int)(
                        _stormRenderTimeSeconds *
                        25.0f)
                    %
                    16;


                float texCoordU =
                    (
                        animationFrame /
                        4
                    )
                    *
                    0.25f;


                DrawStormMesh(
                    1,

                    new Vector3(
                        0.9f,
                        0.6f,
                        0.3f),

                    new Vector2(
                        texCoordU,
                        0.0f));


                return true;
            }
            finally
            {
                gd.BlendState =
                    previousBlend;


                gd.DepthStencilState =
                    previousDepth;


                gd.RasterizerState =
                    previousRasterizer;
            }
        }


        private void DrawStormMesh(
            int mesh,
            Vector3 tint,
            Vector2 texCoordOffset)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D texture))
            {
                return;
            }


            Effect effect =
                _stormEffect;


            if (effect == null)
            {
                return;
            }


            Matrix worldViewProjection =
                WorldPosition
                *
                Camera.Instance.View
                *
                Camera.Instance.Projection;


            effect.Parameters[
                    "WorldViewProjection"]
                ?.SetValue(
                    worldViewProjection);


            effect.Parameters[
                    "DiffuseTexture"]
                ?.SetValue(
                    texture);


            effect.Parameters[
                    "TexCoordOffset"]
                ?.SetValue(
                    texCoordOffset);


            effect.Parameters[
                    "Tint"]
                ?.SetValue(
                    tint);


            effect.Parameters[
                    "Alpha"]
                ?.SetValue(
                    TotalAlpha);


            GraphicsDevice.SetVertexBuffer(
                vertexBuffer);


            GraphicsDevice.Indices =
                indexBuffer;


            EffectPass pass =
                effect.CurrentTechnique
                    .Passes[0];


            pass.Apply();


            GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                indexBuffer.IndexCount / 3);
        }


        // ================================================================
        // CLASSIC EXTERNAL BONE EFFECTS
        // ================================================================

        private void DrawStormClassicEffects(
            GameTime gameTime)
        {
            if (!Visible ||
                !IsStormWing ||
                Model == null ||
                BoneTransform == null)
            {
                return;
            }


            EnsureStormTextures();


            if (_stormCloudTexture == null ||
                _stormCloudTexture.IsDisposed ||
                _stormLightTexture == null ||
                _stormLightTexture.IsDisposed)
            {
                return;
            }


            float timeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;


            UpdateStormThunder(
                gameTime);


            // ============================================================
            // CLOUDS
            //
            // Original:
            //
            // fLuminosity =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.0004f))
            //     *
            //     0.4f;
            //
            // Light =
            // {
            //     0.5 + fLuminosity,
            //     0.5 + fLuminosity,
            //     0.5 + fLuminosity
            // };
            //
            // CreateSprite(
            //     BITMAP_CLUD64,
            //     Position,
            //     0.5f,
            //     Light,
            //     o,
            //     WorldTime * 0.01f,
            //     1);
            // ============================================================

            float cloudLuminosity =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        0.4f))
                *
                0.4f;


            Vector3 cloudLight =
                new Vector3(
                    0.5f +
                    cloudLuminosity,

                    0.5f +
                    cloudLuminosity,

                    0.5f +
                    cloudLuminosity);


            // Original WorldTime was milliseconds:
            //
            // WorldTime * 0.01
            //
            // becomes approximately 10 degrees per second.
            float cloudRotation =
                MathHelper.ToRadians(
                    timeSeconds *
                    10.0f);


            // ============================================================
            // LIGHTS
            //
            // Original:
            //
            // fScale =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.003f))
            //     *
            //     0.2f;
            // ============================================================

            float lightPulse =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        3.0f))
                *
                0.2f;


            GraphicsDevice gd =
                GraphicsDevice;


            BlendState previousBlend =
                gd.BlendState;


            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;


            // ============================================================
            // CLOUDS
            // ============================================================

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    StormCloudBlendMinus,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                    i < StormCloudBones.Length;
                    i++)
                {
                    int bone =
                        StormCloudBones[i];

                    if (!TryGetStormBoneWorldPosition(
                            bone,
                            out Vector3 position))
                    {
                        continue;
                    }

                    DrawStormBillboard(
                        spriteBatch,
                        _stormCloudTexture,
                        position,
                        0.50f,
                        cloudLight,
                        cloudRotation);
                }
            }


            // ============================================================
            // LIGHTS
            // ============================================================

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    StormBrightAdditive,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                    i < StormLightBones.Length;
                    i++)
                {
                    int bone =
                        StormLightBones[i];

                    if (!TryGetStormBoneWorldPosition(
                            bone,
                            out Vector3 position))
                    {
                        continue;
                    }

                    bool isLargeRed =
                        bone == 12 ||
                        bone == 64 ||
                        bone == 98 ||
                        bone == 52;

                    Vector3 light;
                    float scale;

                    if (isLargeRed)
                    {
                        light =
                            new Vector3(
                                0.9f,
                                0.0f,
                                0.0f);

                        scale =
                            lightPulse +
                            1.4f;
                    }
                    else
                    {
                        light =
                            new Vector3(
                                0.8f,
                                0.5f,
                                0.2f);

                        scale =
                            lightPulse +
                            0.3f;
                    }

                    DrawStormBillboard(
                        spriteBatch,
                        _stormLightTexture,
                        position,
                        scale,
                        light,
                        0.0f);
                }
            }


            // SpriteBatchScope restores most graphics state, but restore the
            // blend state explicitly because subsequent model drawing depends
            // on it.
            gd.BlendState =
                previousBlend;
        }


        // ================================================================
        // RANDOM CLASSIC THUNDER
        // ================================================================

        private void EnsureStormThunderPool()
        {
            if (_stormThunderPoolInitialized)
            {
                return;
            }

            _stormThunderPoolInitialized =
                true;

            for (int i = 0;
                i < StormThunderPoolSize;
                i++)
            {
                var thunder =
                    new StormThunderEffect
                    {
                        Hidden =
                            true
                    };

                _stormThunderPool[i] =
                    thunder;

                Children.Add(
                    thunder);
            }
        }


        private void UpdateStormThunder(
            GameTime gameTime)
        {
            if (!IsStormWing ||
                BoneTransform == null)
            {
                return;
            }

            EnsureStormThunderPool();

            _stormThunderAccumulator +=
                (float)
                gameTime
                    .ElapsedGameTime
                    .TotalSeconds
                *
                StormClassicEffectTickRate;

            while (_stormThunderAccumulator >= 1.0f)
            {
                _stormThunderAccumulator -=
                    1.0f;

                if (Random.Shared.Next(2) != 0)
                {
                    continue;
                }

                for (int i = 0;
                    i < StormThunderBones.Length;
                    i++)
                {
                    if (Random.Shared.Next(20) != 0)
                    {
                        continue;
                    }

                    int bone =
                        StormThunderBones[i];

                    if (!TryGetStormBoneLocalPosition(
                            bone,
                            out Vector3 localPosition))
                    {
                        continue;
                    }

                    SpawnStormThunder(
                        localPosition);
                }
            }
        }


        private bool TryGetStormBoneLocalPosition(
            int bone,
            out Vector3 position)
        {
            position =
                Vector3.Zero;

            if (BoneTransform == null ||
                bone < 0 ||
                bone >= BoneTransform.Length)
            {
                return false;
            }

            position =
                BoneTransform[bone]
                    .Translation;

            return true;
        }


        private void SpawnStormThunder(
            Vector3 bonePosition)
        {
            EnsureStormThunderPool();

            StormThunderEffect thunder =
                _stormThunderPool[
                    _stormThunderPoolIndex];

            _stormThunderPoolIndex++;

            if (_stormThunderPoolIndex >=
                _stormThunderPool.Length)
            {
                _stormThunderPoolIndex =
                    0;
            }

            Vector3 randomOffset =
                new Vector3(
                    Random.Shared.Next(-20, 20),
                    Random.Shared.Next(-20, 20),
                    Random.Shared.Next(-20, 20));

            float scale =
                0.3f
                +
                Random.Shared.Next(100)
                *
                0.002f;

            Vector3 angle =
                new Vector3(
                    MathHelper.ToRadians(Random.Shared.Next(360)),
                    MathHelper.ToRadians(Random.Shared.Next(360)),
                    MathHelper.ToRadians(Random.Shared.Next(360)));

            thunder.Activate(
                bonePosition +
                randomOffset,
                angle,
                scale);
        }


        // ================================================================
        // BONE -> WORLD
        // ================================================================

        private bool TryGetStormBoneWorldPosition(
            int bone,
            out Vector3 position)
        {
            position =
                Vector3.Zero;


            if (BoneTransform == null ||
                bone < 0 ||
                bone >= BoneTransform.Length)
            {
                return false;
            }


            // BoneTransform is in wing-model local space.
            //
            // Transform it through WingObject.WorldPosition so it follows:
            //
            // player position
            // player rotation
            // wing scale
            // parent bone attachment
            position =
                Vector3.Transform(
                    BoneTransform[bone]
                        .Translation,

                    WorldPosition);


            return true;
        }


        // ================================================================
        // CLASSIC CREATE-SPRITE PROJECTION
        // ================================================================

        /// <summary>
        /// Draws an old MU-style billboard.
        ///
        /// This intentionally does NOT use SpriteObject's normal scale system.
        ///
        /// Classic MU effectively sizes the sprite in world/camera units:
        ///
        ///     width  = texture.Width  * Scale
        ///     height = texture.Height * Scale
        ///
        /// We convert that size to screen pixels according to camera depth.
        /// This is the same principle used by our corrected +15 sprites.
        /// </summary>
        private void DrawStormBillboard(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector3 worldPosition,
            float classicScale,
            Vector3 light,
            float rotation)
        {
            if (texture == null ||
                texture.IsDisposed)
            {
                return;
            }


            Matrix view =
                Camera.Instance.View;


            Matrix projection =
                Camera.Instance.Projection;


            // World position expressed in camera/view space.
            Vector3 cameraPosition =
                Vector3.Transform(
                    worldPosition,
                    view);


            // MonoGame's camera looks down -Z in view space.
            float cameraDepth =
                -cameraPosition.Z;


            if (cameraDepth <=
                0.001f)
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


            // Perspective conversion:
            //
            // pixels =
            //     worldUnits *
            //     viewportHeight *
            //     projection.M22 /
            //     (2 * depth)
            float pixelsPerCameraUnit =
                viewport.Height
                *
                MathF.Abs(
                    projection.M22)
                /
                (
                    2.0f
                    *
                    cameraDepth
                );


            float spriteScale =
                classicScale
                *
                pixelsPerCameraUnit;


            if (!float.IsFinite(
                    spriteScale) ||
                spriteScale <= 0.0f)
            {
                return;
            }


            Vector3 clampedLight =
                Vector3.Clamp(
                    light,
                    Vector3.Zero,
                    Vector3.One);


            Color color =
                new Color(
                    clampedLight)
                *
                TotalAlpha;


            Vector2 origin =
                new Vector2(
                    texture.Width *
                    0.5f,

                    texture.Height *
                    0.5f);


            spriteBatch.Draw(
                texture,

                new Vector2(
                    projected.X,
                    projected.Y),

                null,

                color,

                rotation,

                origin,

                spriteScale,

                SpriteEffects.None,

                MathHelper.Clamp(
                    projected.Z,
                    0.0f,
                    1.0f));
        }
    }


    internal sealed class StormThunderEffect
        : ModelObject
    {
        private const string ModelPath =
            "Effect/lightning_type01.bmd";

        private const float LifetimeSeconds =
            0.20f;

        private float _life;


        public StormThunderEffect()
        {
            IsTransparent =
                true;

            AffectedByTransparency =
                true;

            RenderShadow =
                false;

            LightEnabled =
                false;

            BlendState =
                BlendState.Additive;

            BlendMesh =
                -1;

            BlendMeshState =
                BlendState.Additive;

            Alpha =
                1.0f;
        }


        public override async Task Load()
        {
            Model =
                await BMDLoader.Instance
                    .Prepare(
                        ModelPath);

            await base.Load();
        }


        public void Activate(
            Vector3 position,
            Vector3 angle,
            float scale)
        {
            Position =
                position;

            Angle =
                angle;

            Scale =
                scale;

            Alpha =
                1.0f;

            _life =
                LifetimeSeconds;

            Hidden =
                false;
        }


        public override void Update(
            GameTime gameTime)
        {
            if (Hidden)
            {
                base.Update(
                    gameTime);

                return;
            }

            float dt =
                (float)
                gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            _life -=
                dt;

            if (_life <= 0.0f)
            {
                Hidden =
                    true;

                Alpha =
                    0.0f;

                base.Update(
                    gameTime);

                return;
            }

            Alpha =
                Math.Clamp(
                    _life /
                    LifetimeSeconds,
                    0.0f,
                    1.0f);

            const float rotationSpeed =
                3.75f;

            Angle =
                new Vector3(
                    Angle.X + rotationSpeed * dt,
                    Angle.Y + rotationSpeed * dt,
                    Angle.Z + rotationSpeed * dt);

            base.Update(
                gameTime);
        }

    }
}
