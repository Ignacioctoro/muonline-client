#nullable enable

using Client.Main;
using Client.Main.Helpers;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Objects.Player;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Body part whose classic +15 linked effect should control an attachment.
    /// </summary>
    public enum Grade15BodyPart
    {
        Helm,
        Armor,
        Pants,
        Gloves,
        Boots
    }

    /// <summary>
    /// Classic MU Online +15 armor effect.
    ///
    /// This reproduces the original NextGradeObjectRender() +
    /// RenderPartObjectEffect() rendering path:
    ///
    /// 1) class15_*.bmd linked to the original character bone.
    ///
    /// 2) The BMD is rendered three times:
    ///
    ///    - class15_effect_main with animated brightness.
    ///    - rgb_mix with animated U coordinate.
    ///    - Chrome02 with classic CHROME4 generated coordinates.
    ///
    /// 3) The three classic sprites are drawn over it:
    ///
    ///    BITMAP_MAGIC
    ///        -> Effect/Magic_Ground1
    ///
    ///    BITMAP_SHINY + 5
    ///        -> Effect/Shiny04
    ///
    ///    BITMAP_PIN_LIGHT
    ///        -> Effect/pin_lights
    ///
    /// Visibility depends on the actual equipped body part ItemLevel,
    /// so it works for both local and remote players.
    /// </summary>
    public sealed class Grade15ArmorEffect : ModelObject
    {
        // --------------------------------------------------------------------
        // Classic resources
        // --------------------------------------------------------------------

        private const string RgbMixTexturePath =
            "Item/rgb_mix.jpg";

        private const string ChromeTexturePath =
            "Effect/Chrome02.jpg";

        private const string Grade15EffectAssetName =
            "Grade15Effect";


        // --------------------------------------------------------------------
        // Classic MU additive blending.
        //
        // The black background of the effect textures contributes zero light,
        // so it disappears instead of being rendered as a black rectangle.
        // --------------------------------------------------------------------

        private static readonly BlendState MuBrightAdditive =
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


        // --------------------------------------------------------------------
        // Effect configuration
        // --------------------------------------------------------------------

        private readonly Grade15BodyPart _bodyPart;

        private readonly string _modelPath;


        // --------------------------------------------------------------------
        // Classic sprites
        // --------------------------------------------------------------------

        private readonly Grade15Sprite _magicSprite;

        private readonly Grade15Sprite _shinySprite;

        private readonly Grade15Sprite _pinLightSprite;


        // --------------------------------------------------------------------
        // Custom +15 renderer
        //
        // ModelObject keeps its normal internal buffers private, so the +15
        // renderer owns a tiny additional set of buffers for these linked BMDs.
        //
        // class15 models are tiny and static, so these are built once.
        // --------------------------------------------------------------------

        private DynamicVertexBuffer[]?
            _grade15VertexBuffers;

        private DynamicIndexBuffer[]?
            _grade15IndexBuffers;

        private Texture2D[]?
            _grade15BaseTextures;


        private Texture2D?
            _rgbMixTexture;

        private Texture2D?
            _chromeTexture;


        private Effect?
            _grade15Effect;

        private bool
            _effectLoadAttempted;

        private bool
            _customBuffersReady;


        // --------------------------------------------------------------------
        // ModelObject configuration
        // --------------------------------------------------------------------

        public override bool IsStaticForCaching =>
            false;

        protected override bool AllowDynamicLightingShader =>
            false;

        protected override bool AllowLightingUpdates =>
            false;


        // --------------------------------------------------------------------
        // Constructor
        // --------------------------------------------------------------------

        public Grade15ArmorEffect(
            Grade15BodyPart bodyPart,
            string modelPath,
            int parentBone,
            Vector3 localPosition,
            Vector3 localRotationDegrees)
        {
            _bodyPart =
                bodyPart;

            _modelPath =
                modelPath;


            // Original NextGradeObjectRender() bone.
            ParentBoneLink =
                parentBone;


            // Original RenderLinkObject() transformation.
            Position =
                localPosition;

            Angle =
                new Vector3(
                    MathHelper.ToRadians(
                        localRotationDegrees.X),

                    MathHelper.ToRadians(
                        localRotationDegrees.Y),

                    MathHelper.ToRadians(
                        localRotationDegrees.Z));


            Scale =
                1.0f;


            // ----------------------------------------------------------------
            // General effect-object settings
            // ----------------------------------------------------------------

            RenderShadow =
                false;

            Interactive =
                false;

            IsTransparent =
                true;

            AffectedByTransparency =
                true;


            // class15 models are effect meshes.
            BlendMesh =
                -2;

            BlendState =
                MuBrightAdditive;

            BlendMeshState =
                MuBrightAdditive;

            BlendMeshLight =
                1.0f;

            DepthState =
                DepthStencilState.DepthRead;


            // No terrain/sun lighting.
            LightEnabled =
                false;

            Light =
                Vector3.One;

            Color =
                Color.White;

            UseSunLight =
                false;


            // Do not involve generic item material rendering.
            ItemLevel =
                0;

            IsExcellentItem =
                false;

            IsAncientItem =
                false;


            Hidden =
                true;


            // ----------------------------------------------------------------
            // BITMAP_MAGIC
            //
            // Original:
            //
            // fLight2 =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.01f));
            //
            // Light =
            // {
            //     0.2 * fLight2,
            //     0.4 * fLight2,
            //     1.0 * fLight2
            // };
            //
            // Scale = 0.12
            // ----------------------------------------------------------------

            _magicSprite =
                new Grade15Sprite(
                    "Effect/Magic_Ground1.jpg",
                    0.12f,
                    Vector3.Zero);


            // ----------------------------------------------------------------
            // BITMAP_SHINY + 5
            //
            // Light = (0.4, 0.7, 1.0)
            // Scale = 0.40
            // ----------------------------------------------------------------

            _shinySprite =
            new Grade15Sprite(
                "Effect/shiny04.jpg",
                0.40f,
                new Vector3(
                    0.4f,
                    0.7f,
                    1.0f));


            // ----------------------------------------------------------------
            // BITMAP_PIN_LIGHT
            //
            // Light = (0.1, 0.3, 1.0)
            // Scale = 0.60
            // Rotation = 90 degrees
            // ----------------------------------------------------------------

            _pinLightSprite =
            new Grade15Sprite(
                "Effect/pin_lights.jpg",
                0.60f,
                new Vector3(
                    0.1f,
                    0.3f,
                    1.0f))
            {
                SpriteRotation =
                    MathHelper.ToRadians(
                        90.0f)
            };


            Children.Add(
                _magicSprite);

            Children.Add(
                _shinySprite);

            Children.Add(
                _pinLightSprite);
        }


        // --------------------------------------------------------------------
        // Never let the generic +7/+15 ItemMaterial shader touch class15 BMD.
        // --------------------------------------------------------------------

        protected override bool ShouldApplyItemMaterial(
            int meshIndex)
        {
            return false;
        }


        // --------------------------------------------------------------------
        // Load
        // --------------------------------------------------------------------

        public override async Task Load()
        {
            // class15 BMDs are in Data/Item.
            //
            // Their own mesh references class15_effect_main.
            Model =
                await BMDLoader.Instance.Prepare(
                    _modelPath);


            // Let ModelObject build its normal state/bones.
            await base.Load();


            if (Model == null ||
                Model.Meshes == null ||
                Model.Meshes.Length == 0)
            {
                return;
            }


            // ---------------------------------------------------------------
            // Prepare the two textures used by the extra classic passes.
            // ---------------------------------------------------------------

            await TextureLoader.Instance.Prepare(
                RgbMixTexturePath);

            await TextureLoader.Instance.Prepare(
                ChromeTexturePath);


            _rgbMixTexture =
                TextureLoader.Instance.GetTexture2D(
                    RgbMixTexturePath);

            _chromeTexture =
                TextureLoader.Instance.GetTexture2D(
                    ChromeTexturePath);


            // ---------------------------------------------------------------
            // Obtain the original class15 texture for every BMD mesh.
            // Usually these models contain only mesh 0.
            // ---------------------------------------------------------------

            int meshCount =
                Model.Meshes.Length;

            _grade15BaseTextures =
                new Texture2D[meshCount];


            for (int meshIndex = 0;
                meshIndex < meshCount;
                meshIndex++)
            {
                string texturePath =
                    BMDLoader.Instance.GetTexturePath(
                        Model,
                        Model.Meshes[
                            meshIndex]
                            .TexturePath);

                if (string.IsNullOrEmpty(
                        texturePath))
                {
                    continue;
                }


                // It should already have been prepared by
                // ModelObject.LoadContent(), but Prepare is cached and safe.
                await TextureLoader.Instance.Prepare(
                    texturePath);

                _grade15BaseTextures[
                    meshIndex] =
                    TextureLoader.Instance.GetTexture2D(
                        texturePath);
            }


            BuildGrade15Buffers();
        }


        // --------------------------------------------------------------------
        // Build the tiny custom buffers used by the three classic passes.
        // --------------------------------------------------------------------

        private void BuildGrade15Buffers()
        {
            _customBuffersReady =
                false;


            if (Model == null ||
                Model.Meshes == null ||
                Model.Meshes.Length == 0)
            {
                return;
            }


            Matrix[]?
                modelBones =
                    GetBoneTransforms();


            // Most class15 models have a normal root bone.
            // Keep an identity fallback for models without explicit bones.
            if (modelBones == null ||
                modelBones.Length == 0)
            {
                int fallbackBoneCount =
                    Math.Max(
                        1,
                        Model.Bones?.Length ?? 0);

                modelBones =
                    new Matrix[
                        fallbackBoneCount];

                for (int i = 0;
                    i < modelBones.Length;
                    i++)
                {
                    modelBones[i] =
                        Matrix.Identity;
                }
            }


            int meshCount =
                Model.Meshes.Length;


            _grade15VertexBuffers =
                new DynamicVertexBuffer[
                    meshCount];

            _grade15IndexBuffers =
                new DynamicIndexBuffer[
                    meshCount];


            bool foundRenderableMesh =
                false;


            for (int meshIndex = 0;
                meshIndex < meshCount;
                meshIndex++)
            {
                DynamicVertexBuffer?
                    vertexBuffer =
                        null;

                DynamicIndexBuffer?
                    indexBuffer =
                        null;


                BMDLoader.Instance.GetModelBuffers(
                    Model,
                    meshIndex,
                    Color.White,
                    modelBones,
                    ref vertexBuffer,
                    ref indexBuffer,

                    // This is a dedicated +15 render copy,
                    // so don't reuse BMDLoader's global buffer cache.
                    skipCache: true,
                    vertexDeformer: null);


                _grade15VertexBuffers[
                    meshIndex] =
                    vertexBuffer;

                _grade15IndexBuffers[
                    meshIndex] =
                    indexBuffer;


                if (vertexBuffer != null &&
                    indexBuffer != null)
                {
                    foundRenderableMesh =
                        true;
                }
            }


            _customBuffersReady =
                foundRenderableMesh;
        }


        // --------------------------------------------------------------------
        // Load Grade15Effect lazily during drawing.
        //
        // This avoids loading an Effect from an async content-loading
        // continuation and keeps ContentManager access on the render thread.
        // --------------------------------------------------------------------

        private void EnsureGrade15Effect()
        {
            if (_effectLoadAttempted)
            {
                return;
            }


            _effectLoadAttempted =
                true;


            try
            {
                _grade15Effect =
                    MuGame.Instance.Content.Load<Effect>(
                        Grade15EffectAssetName);
            }
            catch (Exception ex)
            {
                _grade15Effect =
                    null;

                _logger?.LogWarning(
                    ex,
                    "[Grade15] No se pudo cargar Grade15Effect. " +
                    "Se usará el renderer anterior.");
            }
        }


        // --------------------------------------------------------------------
        // Update
        // --------------------------------------------------------------------

        public override void Update(
            GameTime gameTime)
        {
            if (Parent is not PlayerObject player)
            {
                Hidden =
                    true;

                return;
            }


            Matrix[]?
                playerBones =
                    player.GetBoneTransforms();


            bool hasParentBone =
                playerBones != null &&
                ParentBoneLink >= 0 &&
                ParentBoneLink <
                    playerBones.Length;


            bool enabled =
                hasParentBone &&
                GetEquippedLevel(
                    player) >= 15;


            if (!enabled)
            {
                Hidden =
                    true;

                return;
            }


            Hidden =
                false;


            base.Update(
                gameTime);


            // ---------------------------------------------------------------
            // The original client places the sprites at bone 0 of the linked
            // class15 model.
            // ---------------------------------------------------------------

            Vector3 spriteLocalPosition =
                Vector3.Zero;


            Matrix[]?
                effectBones =
                    GetBoneTransforms();


            if (effectBones != null &&
                effectBones.Length > 0)
            {
                spriteLocalPosition =
                    effectBones[0]
                        .Translation;
            }


            _magicSprite.Position =
                spriteLocalPosition;

            _shinySprite.Position =
                spriteLocalPosition;

            _pinLightSprite.Position =
                spriteLocalPosition;


            // ---------------------------------------------------------------
            // Original:
            //
            // WorldTime = milliseconds
            //
            // abs(
            //     sin(
            //         WorldTime *
            //         0.01f))
            //
            // MonoGame time is seconds:
            //
            // abs(
            //     sin(
            //         seconds *
            //         10.0f))
            // ---------------------------------------------------------------

            float timeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;


            float fLight2 =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        10.0f));


            _magicSprite.Light =
                new Vector3(
                    0.2f * fLight2,
                    0.4f * fLight2,
                    1.0f * fLight2);
        }


        // --------------------------------------------------------------------
        // CUSTOM CLASS15 RENDERER
        //
        // ModelObject.Draw() calls DrawModel(false), followed by its children.
        //
        // That is exactly what we want:
        //
        //      class15 BMD
        //          ↓
        //      Magic / Shiny / PinLight
        //
        // We therefore render the BMD in the first invocation and skip it in
        // DrawAfter(), preventing the effect from being drawn twice.
        // --------------------------------------------------------------------

        public override void DrawModel(
            bool isAfterDraw)
        {
            // The class15 geometry is already rendered additively with
            // DepthRead, so don't render it a second time in DrawAfter().
            if (isAfterDraw)
            {
                return;
            }


            EnsureGrade15Effect();


            // If anything required by the new renderer failed,
            // preserve the previous working implementation.
            if (_grade15Effect == null ||
                !_customBuffersReady ||
                _grade15VertexBuffers == null ||
                _grade15IndexBuffers == null ||
                _grade15BaseTextures == null)
            {
                base.DrawModel(
                    isAfterDraw);

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


            try
            {
                // -----------------------------------------------------------
                // Classic +15 effect state.
                // -----------------------------------------------------------

                gd.BlendState =
                    MuBrightAdditive;

                gd.DepthStencilState =
                    DepthStencilState.DepthRead;

                gd.RasterizerState =
                    RasterizerState.CullNone;


                Effect effect =
                    _grade15Effect;


                float timeSeconds =
                    (float)
                    MuGame.Instance
                        .GameTime
                        .TotalGameTime
                        .TotalSeconds;


                // -----------------------------------------------------------
                // Original:
                //
                // fLight =
                //     0.8 -
                //     abs(
                //         sin(
                //             WorldTime *
                //             0.0018) *
                //         0.5);
                //
                // milliseconds -> seconds:
                //
                // 0.0018 * 1000 = 1.8
                // -----------------------------------------------------------

                float fLight =
                    0.8f -
                    MathF.Abs(
                        MathF.Sin(
                            timeSeconds *
                            1.8f) *
                        0.5f);


                float baseBrightness =
                    MathF.Max(
                        0.0f,
                        fLight -
                        0.1f);


                float rgbMixBrightness =
                    MathF.Max(
                        0.0f,
                        fLight -
                        0.3f);


                // -----------------------------------------------------------
                // Original:
                //
                // texCoordU =
                //     abs(
                //         sin(
                //             WorldTime *
                //             0.0005));
                //
                // milliseconds -> seconds:
                //
                // 0.0005 * 1000 = 0.5
                // -----------------------------------------------------------

                float texCoordU =
                    MathF.Abs(
                        MathF.Sin(
                            timeSeconds *
                            0.5f));


                Matrix world =
                    WorldPosition;


                Matrix worldViewProjection =
                    world *
                    Camera.Instance.View *
                    Camera.Instance.Projection;


                effect.Parameters[
                        "World"]
                    ?.SetValue(
                        world);


                effect.Parameters[
                        "WorldViewProjection"]
                    ?.SetValue(
                        worldViewProjection);


                effect.Parameters[
                        "Time"]
                    ?.SetValue(
                        timeSeconds);


                effect.Parameters[
                        "Alpha"]
                    ?.SetValue(
                        TotalAlpha);


                effect.Parameters[
                        "TexCoordUOffset"]
                    ?.SetValue(
                        texCoordU);


                if (_rgbMixTexture != null)
                {
                    effect.Parameters[
                            "RgbMixTexture"]
                        ?.SetValue(
                            _rgbMixTexture);
                }


                if (_chromeTexture != null)
                {
                    effect.Parameters[
                            "ChromeTexture"]
                        ?.SetValue(
                            _chromeTexture);
                }


                int meshCount =
                    Math.Min(
                        _grade15VertexBuffers.Length,
                        _grade15IndexBuffers.Length);


                meshCount =
                    Math.Min(
                        meshCount,
                        _grade15BaseTextures.Length);


                for (int meshIndex = 0;
                    meshIndex < meshCount;
                    meshIndex++)
                {
                    DynamicVertexBuffer?
                        vertexBuffer =
                            _grade15VertexBuffers[
                                meshIndex];

                    DynamicIndexBuffer?
                        indexBuffer =
                            _grade15IndexBuffers[
                                meshIndex];

                    Texture2D?
                        baseTexture =
                            _grade15BaseTextures[
                                meshIndex];


                    if (vertexBuffer == null ||
                        indexBuffer == null ||
                        vertexBuffer.IsDisposed ||
                        indexBuffer.IsDisposed)
                    {
                        continue;
                    }


                    gd.SetVertexBuffer(
                        vertexBuffer);

                    gd.Indices =
                        indexBuffer;


                    int primitiveCount =
                        indexBuffer.IndexCount /
                        3;


                    // =======================================================
                    // PASS 1
                    //
                    // class15_effect_main
                    //
                    // RENDER_TEXTURE |
                    // RENDER_BRIGHT
                    // =======================================================

                    if (baseTexture != null)
                    {
                        effect.Parameters[
                                "DiffuseTexture"]
                            ?.SetValue(
                                baseTexture);


                        effect.Parameters[
                                "Brightness"]
                            ?.SetValue(
                                baseBrightness);


                        EffectTechnique?
                            technique =
                                effect.Techniques[
                                    "Grade15Base"];


                        if (technique != null)
                        {
                            effect.CurrentTechnique =
                                technique;


                            foreach (
                                EffectPass pass
                                in technique.Passes)
                            {
                                pass.Apply();

                                gd.DrawIndexedPrimitives(
                                    PrimitiveType.TriangleList,
                                    0,
                                    0,
                                    primitiveCount);
                            }
                        }
                    }


                    // =======================================================
                    // PASS 2
                    //
                    // rgb_mix.OZJ
                    //
                    // The texture coordinates move horizontally using:
                    //
                    // abs(sin(Time * 0.5))
                    // =======================================================

                    if (_rgbMixTexture != null &&
                        rgbMixBrightness > 0.001f)
                    {
                        effect.Parameters[
                                "Brightness"]
                            ?.SetValue(
                                rgbMixBrightness);


                        EffectTechnique?
                            technique =
                                effect.Techniques[
                                    "Grade15RgbMix"];


                        if (technique != null)
                        {
                            effect.CurrentTechnique =
                                technique;


                            foreach (
                                EffectPass pass
                                in technique.Passes)
                            {
                                pass.Apply();

                                gd.DrawIndexedPrimitives(
                                    PrimitiveType.TriangleList,
                                    0,
                                    0,
                                    primitiveCount);
                            }
                        }
                    }


                    // =======================================================
                    // PASS 3
                    //
                    // Classic RENDER_CHROME4
                    //
                    // Uses Chrome02 and generates texture coordinates from
                    // the BMD normal + classic time values.
                    // =======================================================

                    if (_chromeTexture != null)
                    {
                        effect.Parameters[
                                "Brightness"]
                            ?.SetValue(
                                1.0f);


                        EffectTechnique?
                            technique =
                                effect.Techniques[
                                    "Grade15Chrome4"];


                        if (technique != null)
                        {
                            effect.CurrentTechnique =
                                technique;


                            foreach (
                                EffectPass pass
                                in technique.Passes)
                            {
                                pass.Apply();

                                gd.DrawIndexedPrimitives(
                                    PrimitiveType.TriangleList,
                                    0,
                                    0,
                                    primitiveCount);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(
                    ex,
                    "[Grade15] Error renderizando efecto +15 clásico.");
            }
            finally
            {
                // Always restore the shared device state.
                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;

                gd.RasterizerState =
                    previousRasterizer;
            }
        }


        // --------------------------------------------------------------------
        // Equipment level
        // --------------------------------------------------------------------

        private int GetEquippedLevel(
            PlayerObject player)
        {
            return _bodyPart switch
            {
                Grade15BodyPart.Helm =>
                    player.Helm?.ItemLevel ?? 0,

                Grade15BodyPart.Armor =>
                    player.Armor?.ItemLevel ?? 0,

                Grade15BodyPart.Pants =>
                    player.Pants?.ItemLevel ?? 0,

                Grade15BodyPart.Gloves =>
                    player.Gloves?.ItemLevel ?? 0,

                Grade15BodyPart.Boots =>
                    player.Boots?.ItemLevel ?? 0,

                _ =>
                    0
            };
        }


        // --------------------------------------------------------------------
        // Dispose
        // --------------------------------------------------------------------

        public override void Dispose()
        {
            if (_grade15VertexBuffers != null)
            {
                for (int i = 0;
                    i < _grade15VertexBuffers.Length;
                    i++)
                {
                    DynamicVertexBuffer?
                        buffer =
                            _grade15VertexBuffers[i];

                    if (buffer != null &&
                        !buffer.IsDisposed)
                    {
                        DynamicBufferPool
                            .ReturnVertexBuffer(
                                buffer);
                    }

                    _grade15VertexBuffers[i] =
                        null!;
                }
            }


            if (_grade15IndexBuffers != null)
            {
                for (int i = 0;
                    i < _grade15IndexBuffers.Length;
                    i++)
                {
                    DynamicIndexBuffer?
                        buffer =
                            _grade15IndexBuffers[i];

                    if (buffer != null &&
                        !buffer.IsDisposed)
                    {
                        DynamicBufferPool
                            .ReturnIndexBuffer(
                                buffer);
                    }

                    _grade15IndexBuffers[i] =
                        null!;
                }
            }


            _grade15VertexBuffers =
                null;

            _grade15IndexBuffers =
                null;

            _grade15BaseTextures =
                null;

            _rgbMixTexture =
                null;

            _chromeTexture =
                null;

            // Grade15Effect belongs to ContentManager.
            // Do not dispose it here because Content.Load caches assets.
            _grade15Effect =
                null;


            base.Dispose();
        }


        // --------------------------------------------------------------------
        // Persistent classic billboard
        // --------------------------------------------------------------------

        // --------------------------------------------------------------------
        // Persistent classic billboard
        //
        // MU original CreateSprite():
        //
        // Width  = texture.Width  * Scale
        // Height = texture.Height * Scale
        //
        // The normal Neffis SpriteObject renderer applies its own
        // distance-based scaling, which makes these small classic +15 sprites
        // almost disappear.
        //
        // This renderer converts the original camera-space scale to screen
        // pixels exactly like the renderer already used by
        // Grade15WeaponEffect.
        // --------------------------------------------------------------------

        private sealed class Grade15Sprite :
            SpriteObject
        {
            private readonly string
                _texturePath;


            public override string TexturePath =>
                _texturePath;


            /// <summary>
            /// Explicit billboard rotation.
            ///
            /// Classic CreateSprite() receives Rotation separately from the
            /// model/object rotation, so it must not use WorldObject.Angle.
            /// </summary>
            public float SpriteRotation
            {
                get;
                set;
            }


            public Grade15Sprite(
                string texturePath,
                float scale,
                Vector3 light)
            {
                _texturePath =
                    texturePath;


                Scale =
                    scale;


                Light =
                    light;


                LightEnabled =
                    true;


                IsTransparent =
                    true;


                AffectedByTransparency =
                    true;


                BlendState =
                    MuBrightAdditive;


                DepthState =
                    DepthStencilState.DepthRead;


                Interactive =
                    false;
            }


            public override void Draw(
                GameTime gameTime)
            {
                if (!Visible ||
                    SpriteTexture == null)
                {
                    return;
                }


                // ------------------------------------------------------------
                // WorldPosition already contains:
                //
                // class15 local bone position
                //      *
                // class15 attachment transform
                //      *
                // player bone/world transform
                //
                // Therefore we only need its final translation here.
                // ------------------------------------------------------------

                Vector3 worldPosition =
                    WorldPosition.Translation;


                Matrix view =
                    Camera.Instance.View;


                Matrix projection =
                    Camera.Instance.Projection;


                Vector3 cameraPosition =
                    Vector3.Transform(
                        worldPosition,
                        view);


                // XNA's CreateLookAt is right-handed.
                // Visible geometry lies on negative camera Z.
                float cameraDepth =
                    -cameraPosition.Z;


                if (cameraDepth <=
                    Camera.Instance.ViewNear)
                {
                    return;
                }


                var viewport =
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


                // ------------------------------------------------------------
                // MU original:
                //
                // Width  = texture.Width  * Scale;
                // Height = texture.Height * Scale;
                //
                // Those dimensions are effectively camera-space dimensions.
                //
                // Convert one camera-space unit into pixels at the current
                // camera depth.
                //
                // This is the same correction already used successfully by
                // Grade15WeaponEffect.
                // ------------------------------------------------------------

                float pixelsPerCameraUnit =
                    viewport.Height *
                    MathF.Abs(
                        projection.M22) /
                    (2.0f *
                    cameraDepth);


                float spriteScale =
                    Scale *
                    pixelsPerCameraUnit;


                if (!float.IsFinite(
                        spriteScale) ||
                    spriteScale <= 0.0f)
                {
                    return;
                }


                Color color =
                    LightEnabled
                        ? new Color(
                            Light) *
                        TotalAlpha
                        : Color.White *
                        TotalAlpha;


                float depth =
                    MathHelper.Clamp(
                        projected.Z,
                        0.0f,
                        1.0f);


                Vector2 origin =
                    new Vector2(
                        SpriteTexture.Width *
                        0.5f,

                        SpriteTexture.Height *
                        0.5f);


                // MU's sprite effects are filtered linearly.
                using (
                    new SpriteBatchScope(
                        SpriteBatch,
                        SpriteSortMode.Deferred,
                        BlendState,
                        SamplerState.LinearClamp,
                        DepthState,
                        RasterizerState.CullNone))
                {
                    SpriteBatch.Draw(
                        SpriteTexture,

                        new Vector2(
                            projected.X,
                            projected.Y),

                        null,

                        color,

                        SpriteRotation,

                        origin,

                        spriteScale,

                        SpriteEffects.None,

                        depth);
                }
            }
        }
    }
}