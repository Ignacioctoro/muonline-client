using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
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
        // WING OF RUIN
        //
        // Item:
        //     12,39
        //
        // Model:
        //     Item/Wing11.bmd
        //
        // Classic model recipe:
        //
        // Mesh 1:
        //     RENDER_TEXTURE
        //
        // Mesh 0:
        //     RENDER_TEXTURE | RENDER_BRIGHT
        //     pulsating grey light
        //
        // Mesh 1:
        //     RENDER_TEXTURE | RENDER_BRIGHT
        //     BITMAP_3RDWING_LAYER
        //
        // BITMAP_3RDWING_LAYER:
        //     Item/msword01_r.jpg
        //
        // External effects:
        //     6 x BITMAP_LIGHT
        //     bones 6,15,24,56,47,38
        // ================================================================

        private const short RuinWingItemIndex =
            39;


        private const string RuinLayerTexturePath =
            "Item/msword01_r.OZJ";


        private const string RuinLightTexturePath =
            "Effect/flare01.OZJ";


        private bool IsRuinWing =>
            ItemIndex ==
            RuinWingItemIndex;


        // ================================================================
        // CLASSIC BONE TABLE
        // ================================================================

        private static readonly int[] RuinLightBones =
        {
            6,
            15,
            24,
            56,
            47,
            38
        };


        // ================================================================
        // RESOURCES
        // ================================================================

        private Texture2D
            _ruinLayerTexture;


        private Texture2D
            _ruinLightTexture;


        private bool
            _ruinTexturesPrepared;


        private async Task PrepareRuinClassicAssetsAsync()
        {
            if (_ruinTexturesPrepared)
            {
                return;
            }


            await Task.WhenAll(
                TextureLoader.Instance.Prepare(
                    RuinLayerTexturePath),

                TextureLoader.Instance.Prepare(
                    RuinLightTexturePath));


            _ruinTexturesPrepared =
                true;
        }


        private void EnsureRuinTextures()
        {
            if (!_ruinTexturesPrepared)
            {
                return;
            }


            if (_ruinLayerTexture == null ||
                _ruinLayerTexture.IsDisposed)
            {
                _ruinLayerTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            RuinLayerTexturePath);
            }


            if (_ruinLightTexture == null ||
                _ruinLightTexture.IsDisposed)
            {
                _ruinLightTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            RuinLightTexturePath);
            }
        }


        // ================================================================
        // CLASSIC MODEL RENDERER
        // ================================================================

        private bool TryDrawRuinModel()
        {
            if (Model?.Meshes == null ||
                Model.Meshes.Length < 2)
            {
                return false;
            }


            EnsureStormEffect();
            EnsureRuinTextures();


            if (_stormEffect == null ||
                _ruinLayerTexture == null ||
                _ruinLayerTexture.IsDisposed)
            {
                return false;
            }


            if (!TryGetDerivedMeshRenderData(
                    0,
                    out _,
                    out _,
                    out _) ||

                !TryGetDerivedMeshRenderData(
                    1,
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


                float pulse =
                    MathF.Abs(
                        MathF.Sin(
                            _stormRenderTimeSeconds));


                // ========================================================
                // PASS 1
                // Mesh 1 base: msword01.
                // Contains the dark metallic frame and purple blade body.
                // ========================================================

                gd.BlendState =
                    BlendState.AlphaBlend;


                DrawRuinMesh(
                    1,
                    Vector3.One,
                    null);


                // ========================================================
                // PASS 2A
                // Mesh 0 body.
                //
                // This extra alpha-blended pass keeps more visible body
                // definition in Neffis/MonoGame before the classic bright
                // pass is added.
                // ========================================================

                float mesh0Body =
                    0.55f +
                    pulse *
                    0.25f;


                gd.BlendState =
                    BlendState.AlphaBlend;


                DrawRuinMesh(
                    0,
                    new Vector3(
                        mesh0Body,
                        mesh0Body,
                        mesh0Body),
                    null);


                // ========================================================
                // PASS 2B
                // Mesh 0 bright contribution.
                // ========================================================

                float mesh0Bloom =
                    0.08f +
                    pulse *
                    0.18f;


                gd.BlendState =
                    StormBrightAdditive;


                DrawRuinMesh(
                    0,
                    new Vector3(
                        mesh0Bloom,
                        mesh0Bloom,
                        mesh0Bloom),
                    null);


                // ========================================================
                // PASS 3
                // BITMAP_3RDWING_LAYER = Item/msword01_r.OZJ
                // ========================================================

                float overlayLight =
                    0.20f +
                    pulse *
                    0.45f;


                gd.BlendState =
                    StormBrightAdditive;


                DrawRuinMesh(
                    1,
                    new Vector3(
                        overlayLight,
                        overlayLight,
                        overlayLight),
                    _ruinLayerTexture);


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


        // ================================================================
        // DRAW ONE RUIN PASS
        // ================================================================

        private void DrawRuinMesh(
            int mesh,
            Vector3 tint,
            Texture2D textureOverride)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D modelTexture))
            {
                return;
            }


            Effect effect =
                _stormEffect;


            if (effect == null)
            {
                return;
            }


            Texture2D texture =
                textureOverride ??
                modelTexture;


            if (texture == null ||
                texture.IsDisposed)
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
                    Vector2.Zero);


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
        // CLASSIC EXTERNAL LIGHTS
        // ================================================================

        private void DrawRuinClassicEffects(
            GameTime gameTime)
        {
            if (!Visible ||
                !IsRuinWing ||
                Model == null ||
                BoneTransform == null)
            {
                return;
            }


            EnsureRuinTextures();


            if (_ruinLightTexture == null ||
                _ruinLightTexture.IsDisposed)
            {
                return;
            }


            float timeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;


            // Classic:
            //
            // Scale =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.003))
            //     * 0.2
            //
            // WorldTime = milliseconds
            //
            // -> seconds * 3.0

            float pulse =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds * 3.0f));

            float scale =
                pulse * 0.2f;

            float luminosity =
                pulse * 0.3f;

            Vector3 light =
                new Vector3(
                    0.7f + luminosity,
                    0.5f + luminosity,
                    0.8f + luminosity);


            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;


            GraphicsDevice gd =
                GraphicsDevice;


            BlendState previousBlend =
                gd.BlendState;


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
                    i < RuinLightBones.Length;
                    i++)
                {
                    if (!TryGetRuinBoneWorldPosition(
                            RuinLightBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _ruinLightTexture,
                        position,
                        scale + 1.5f,
                        light,
                        0.0f);
                }
            }


            gd.BlendState =
                previousBlend;
        }


        // ================================================================
        // BONE -> WORLD
        // ================================================================

        private bool TryGetRuinBoneWorldPosition(
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
                Vector3.Transform(
                    BoneTransform[bone]
                        .Translation,

                    WorldPosition);


            return true;
        }
    }
}