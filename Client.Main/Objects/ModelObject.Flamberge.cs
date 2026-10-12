// Original MU MODEL_FLAMBERGE (Sword_27.bmd): shared renderer for equipped,
 // previewed and ClassicFX weapon echoes. Keep the four-frame flamestani
 // texture and native per-mesh BRIGHT/CHROME sequence in one place.
using System;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects
{
    public abstract partial class ModelObject
    {
        private bool IsNativeFlambergeEffectMesh(int mesh) =>
            ItemGroup == 0 && ItemNumber == 26 &&
            Model?.Meshes != null && Model.Meshes.Length > 5 &&
            mesh is 1 or 3 or 4 or 5;

        private void DrawNativeFlambergeEffectMesh(int mesh)
        {
            // Main ItemRenderStyles.cpp RenderFlamberge():
            // mesh 1 = BRIGHT red diffuse + white CHROME;
            // meshes 3,4 = BRIGHT white diffuse;
            // mesh 5 = BRIGHT with four vertical atlas frames.
            if (mesh == 1)
            {
                DrawClassicBrightDiffuseWithChrome(mesh,
                    new Vector3(1f, 0f, 0.2f));
            }
            else if (mesh == 3 || mesh == 4)
                DrawNativeFlambergeBrightMesh(mesh);
            else if (mesh == 5)
                DrawNativeFlambergeAnimatedFlame(mesh);
        }

private void DrawNativeFlambergeBrightMesh(int mesh)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D texture))
            {
                return;
            }

            var effect =
                GraphicsManager.Instance.ItemMaterialEffect;

            if (effect == null)
            {
                return;
            }

            var gd =
                GraphicsDevice;

            var previousRasterizer =
                gd.RasterizerState;

            var previousBlend =
                gd.BlendState;

            var previousDepth =
                gd.DepthStencilState;

            try
            {
                effect.CurrentTechnique =
                    effect.Techniques[0];

                GraphicsManager.Instance
                    .ShadowMapRenderer
                    ?.ApplyShadowParameters(effect);

                effect.Parameters["World"]
                    ?.SetValue(WorldPosition);

                effect.Parameters["View"]
                    ?.SetValue(Camera.Instance.View);

                effect.Parameters["Projection"]
                    ?.SetValue(Camera.Instance.Projection);

                effect.Parameters["DiffuseTexture"]
                    ?.SetValue(texture);

                effect.Parameters["Time"]
                    ?.SetValue(Environment.TickCount * 0.001f);

                effect.Parameters["Alpha"]
                    ?.SetValue(TotalAlpha);

                // PassMode 5 is our direct diffuse BRIGHT pass.
                effect.Parameters["PassMode"]
                    ?.SetValue(5);

                // The original leaves BodyLight white after the
                // second Flamberge mesh-1 pass.
                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["BaseLightScale"]
                    ?.SetValue(1.0f);

                effect.Parameters["ShadowStrength"]
                    ?.SetValue(0.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                gd.SetVertexBuffer(
                    vertexBuffer);

                gd.Indices =
                    indexBuffer;

                // Original MU:
                // EnableAlphaBlend()
                // glBlendFunc(GL_ONE, GL_ONE)
                gd.BlendState =
                    _classicItemBrightAdditive;

                // Bright/effect geometry must not write depth.
                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                int primitiveCount =
                    indexBuffer.IndexCount / 3;

                foreach (EffectPass pass
                    in effect.CurrentTechnique.Passes)
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
                effect.Parameters["PassMode"]
                    ?.SetValue(0);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                gd.RasterizerState =
                    previousRasterizer;

                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;
            }
        }

private void DrawNativeFlambergeAnimatedFlame(int mesh)
        {
            if (!TryGetDerivedMeshRenderData(
                    mesh,
                    out VertexBuffer vertexBuffer,
                    out IndexBuffer indexBuffer,
                    out Texture2D texture))
            {
                return;
            }

            var effect =
                GraphicsManager.Instance.ItemMaterialEffect;

            if (effect == null)
            {
                return;
            }

            var gd = GraphicsDevice;

            var previousRasterizer = gd.RasterizerState;
            var previousBlend = gd.BlendState;
            var previousDepth = gd.DepthStencilState;

            try
            {
                // 4-frame vertical atlas:
                // V = 0.00 / 0.25 / 0.50 / 0.75
                //
                // Ajusta el 80 si lo quieres más rápido o más lento.
                int frame =
                    (Environment.TickCount / 80) & 3;

                float vOffset =
                    frame * 0.25f;

                effect.CurrentTechnique =
                    effect.Techniques[0];

                GraphicsManager.Instance
                    .ShadowMapRenderer
                    ?.ApplyShadowParameters(effect);

                effect.Parameters["World"]
                    ?.SetValue(WorldPosition);

                effect.Parameters["View"]
                    ?.SetValue(Camera.Instance.View);

                effect.Parameters["Projection"]
                    ?.SetValue(Camera.Instance.Projection);

                effect.Parameters["DiffuseTexture"]
                    ?.SetValue(texture);

                effect.Parameters["Time"]
                    ?.SetValue(Environment.TickCount * 0.001f);

                effect.Parameters["Alpha"]
                    ?.SetValue(TotalAlpha);

                effect.Parameters["PassMode"]
                    ?.SetValue(6);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(new Vector2(0.0f, vOffset));

                effect.Parameters["BaseLightScale"]
                    ?.SetValue(1.0f);

                effect.Parameters["ShadowStrength"]
                    ?.SetValue(0.0f);

                gd.SetVertexBuffer(vertexBuffer);
                gd.Indices = indexBuffer;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                gd.BlendState =
                _classicItemBrightAdditive;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                int primitiveCount =
                    indexBuffer.IndexCount / 3;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
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
                effect.Parameters["PassMode"]
                    ?.SetValue(0);

                effect.Parameters["DiffuseUVOffset"]
                    ?.SetValue(Vector2.Zero);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(1.0f);

                gd.RasterizerState = previousRasterizer;
                gd.BlendState = previousBlend;
                gd.DepthStencilState = previousDepth;
            }
        }
    }
}
