using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects
{
    /// <summary>
    /// Generic classic MU RENDER_CHROME pass.
    ///
    /// Reuses:
    ///
    /// - Effect/Chrome01.jpg
    /// - ItemMaterial.fx
    /// - classic normal-based Chrome UV generation
    /// - GL_ONE + GL_ONE equivalent blending
    ///
    /// The small negative depth bias compensates for the fact that
    /// Neffis renders the base model and the Chrome overlay through
    /// different shaders. Without it, tiny floating-point differences
    /// can make coplanar Chrome fragments intermittently fail the
    /// depth test.
    /// </summary>
    public abstract partial class ModelObject
    {
        /// <summary>
        /// Small depth bias used only by the classic Chrome overlay.
        ///
        /// Neffis already uses values in this range for players and
        /// dropped items to prevent coplanar Z-fighting.
        ///
        /// Negative values move the rendered surface very slightly
        /// toward the camera without visually changing its position.
        /// </summary>
        private const float ClassicChromeDepthBias =
            -0.00002f;


        /// <summary>
        /// Draws one original-style:
        ///
        /// RENDER_TEXTURE |
        /// RENDER_BRIGHT |
        /// RENDER_CHROME
        ///
        /// pass over an already rendered mesh.
        /// </summary>
        protected void DrawClassicChromePass(
            int mesh,
            Vector3 materialColor,
            float materialIntensity = 1.0f)
        {
            if (!Visible ||
                Model?.Meshes == null ||
                mesh < 0 ||
                mesh >= Model.Meshes.Length)
            {
                return;
            }


            if (_boneVertexBuffers == null ||
                _boneIndexBuffers == null ||
                _boneTextures == null ||
                mesh >= _boneVertexBuffers.Length ||
                mesh >= _boneIndexBuffers.Length ||
                mesh >= _boneTextures.Length ||
                _boneVertexBuffers[mesh] == null ||
                _boneIndexBuffers[mesh] == null ||
                _boneTextures[mesh] == null ||
                IsHiddenMesh(mesh))
            {
                return;
            }


            if (!EnsureClassicItemTextures())
            {
                return;
            }


            Effect effect =
                GraphicsManager.Instance
                    .ItemMaterialEffect;


            if (effect == null ||
                effect.Techniques.Count == 0)
            {
                return;
            }


            EffectTechnique technique =
                effect.Techniques[0];


            if (technique.Passes.Count == 0)
            {
                return;
            }


            VertexBuffer vertexBuffer =
                _boneVertexBuffers[mesh];


            IndexBuffer indexBuffer =
                _boneIndexBuffers[mesh];


            Texture2D diffuseTexture =
                _boneTextures[mesh];


            GraphicsDevice gd =
                GraphicsDevice;


            RasterizerState previousRasterizer =
                gd.RasterizerState;


            BlendState previousBlendState =
                gd.BlendState;


            DepthStencilState previousDepthState =
                gd.DepthStencilState;


            try
            {
                effect.CurrentTechnique =
                    technique;


                // ========================================================
                // MATRICES
                // ========================================================

                effect.Parameters[
                        "World"]
                    ?.SetValue(
                        WorldPosition);


                effect.Parameters[
                        "View"]
                    ?.SetValue(
                        Camera.Instance.View);


                effect.Parameters[
                        "Projection"]
                    ?.SetValue(
                        Camera.Instance.Projection);


                // ========================================================
                // TEXTURES
                // ========================================================

                effect.Parameters[
                        "DiffuseTexture"]
                    ?.SetValue(
                        diffuseTexture);


                effect.Parameters[
                        "MaterialTexture"]
                    ?.SetValue(
                        _classicChrome01);


                // ========================================================
                // RENDER_CHROME
                // ========================================================

                effect.Parameters[
                        "PassMode"]
                    ?.SetValue(
                        1);


                effect.Parameters[
                        "BaseLightScale"]
                    ?.SetValue(
                        1.0f);


                effect.Parameters[
                        "MaterialColor"]
                    ?.SetValue(
                        materialColor);


                effect.Parameters[
                        "MaterialIntensity"]
                    ?.SetValue(
                        materialIntensity);


                effect.Parameters[
                        "Time"]
                    ?.SetValue(
                        GetShaderTimeSeconds());


                effect.Parameters[
                        "Alpha"]
                    ?.SetValue(
                        TotalAlpha);


                // ========================================================
                // CLASSIC BRIGHT BLEND
                // ========================================================
                //
                // Original EnableAlphaBlend():
                //
                //     GL_ONE
                //     GL_ONE
                //
                //     DisableCullFace()
                //     DisableDepthMask()
                //
                // We additionally apply an extremely small negative
                // depth bias because the Neffis base mesh and this
                // overlay use different vertex shaders.
                //
                // Without it both surfaces are mathematically coplanar,
                // but floating point precision can make individual
                // Chrome pixels alternate between passing/failing depth.
                // ========================================================

                gd.RasterizerState =
                    GraphicsManager
                        .GetCachedRasterizerState(
                            ClassicChromeDepthBias,
                            CullMode.None,
                            previousRasterizer);


                gd.BlendState =
                    _classicItemBrightAdditive;


                gd.DepthStencilState =
                    GraphicsManager
                        .ReadOnlyDepth;


                gd.SetVertexBuffer(
                    vertexBuffer);


                gd.Indices =
                    indexBuffer;


                EffectPass pass =
                    technique.Passes[0];


                pass.Apply();


                int primitiveCount =
                    indexBuffer.IndexCount /
                    3;


                gd.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    primitiveCount);
            }
            finally
            {
                //
                // ItemMaterialEffect is shared with inventory items.
                // Restore predictable defaults.
                //

                effect.Parameters[
                        "PassMode"]
                    ?.SetValue(
                        0);


                effect.Parameters[
                        "BaseLightScale"]
                    ?.SetValue(
                        1.0f);


                effect.Parameters[
                        "MaterialColor"]
                    ?.SetValue(
                        Vector3.One);


                effect.Parameters[
                        "MaterialIntensity"]
                    ?.SetValue(
                        1.0f);


                gd.RasterizerState =
                    previousRasterizer;


                gd.BlendState =
                    previousBlendState;


                gd.DepthStencilState =
                    previousDepthState;
            }
        }
    }
}