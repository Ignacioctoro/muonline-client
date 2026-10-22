// Season 6 inventory preview for MODEL_FLAMBERGE (Group 0, ID 26).
// Ordered mesh sequence from MuMain ItemRenderStyles.cpp::RenderFlamberge.
// World/equipped Flamberge remains in ModelObject.Flamberge.
using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Inventory
{
    public static partial class BmdPreviewRenderer
    {
        private static bool TryRenderNativeFlambergePreview(
            GraphicsDevice gd,
            BasicEffect baseEffect,
            BMD bmd,
            Matrix[] bones,
            Matrix world,
            Matrix view,
            Matrix projection,
            float seconds)
        {
            // Never apply the six-mesh Flamberge effect to unrelated models.
            var shader = GraphicsManager.Instance.ItemMaterialEffect;
            if (shader == null || bmd?.Meshes == null || bmd.Meshes.Length < 6)
                return false;

            // Native opaque meshes first: blade and hilt.
            baseEffect.World = world;
            RenderMeshWithBlendState(gd, baseEffect, bmd, 0, bones);
            RenderMeshWithBlendState(gd, baseEffect, bmd, 2, bones);

            BlendState oldBlend = gd.BlendState;
            DepthStencilState oldDepth = gd.DepthStencilState;
            RasterizerState oldRasterizer = gd.RasterizerState;
            try
            {
                shader.CurrentTechnique = shader.Techniques[0];
                shader.Parameters["World"]?.SetValue(world);
                shader.Parameters["View"]?.SetValue(view);
                shader.Parameters["Projection"]?.SetValue(projection);
                shader.Parameters["Time"]?.SetValue(seconds);
                shader.Parameters["Alpha"]?.SetValue(1f);
                shader.Parameters["ShadowStrength"]?.SetValue(0f);
                shader.Parameters["BaseLightScale"]?.SetValue(1f);
                shader.Parameters["MaterialIntensity"]?.SetValue(1f);

                gd.BlendState = _classicPreviewBrightAdditive;
                gd.DepthStencilState = DepthStencilState.DepthRead;
                gd.RasterizerState = RasterizerState.CullNone;

                // Mesh 1: red BRIGHT diffuse then white CHROME.
                DrawFlambergePreviewPass(gd, shader, bmd, bones, 1, 5,
                    new Vector3(1f, 0f, 0.2f), Vector2.Zero, null);
                if (EnsureClassicPreviewTextures() && _previewChrome01 != null)
                    DrawFlambergePreviewPass(gd, shader, bmd, bones, 1, 1,
                        Vector3.One, Vector2.Zero, _previewChrome01);

                // Meshes 3 and 4: white BRIGHT.
                DrawFlambergePreviewPass(gd, shader, bmd, bones, 3, 5,
                    Vector3.One, Vector2.Zero, null);
                DrawFlambergePreviewPass(gd, shader, bmd, bones, 4, 5,
                    Vector3.One, Vector2.Zero, null);

                // WorldTime in original code is milliseconds.
                // fV = (((int)(WorldTime * 0.05) % 16) / 4) * 0.25f.
                float frameV = (((int)(seconds * 50f) % 16) / 4) * 0.25f;
                DrawFlambergePreviewPass(gd, shader, bmd, bones, 5, 6,
                    Vector3.One, new Vector2(0f, frameV), null);
            }
            finally
            {
                shader.Parameters["PassMode"]?.SetValue(0);
                shader.Parameters["DiffuseUVOffset"]?.SetValue(Vector2.Zero);
                shader.Parameters["MaterialColor"]?.SetValue(Vector3.One);
                shader.Parameters["MaterialIntensity"]?.SetValue(1f);
                gd.BlendState = oldBlend;
                gd.DepthStencilState = oldDepth;
                gd.RasterizerState = oldRasterizer;
            }

            return true;
        }

        private static void DrawFlambergePreviewPass(
            GraphicsDevice gd,
            Effect shader,
            BMD bmd,
            Matrix[] bones,
            int meshIndex,
            int passMode,
            Vector3 color,
            Vector2 uvOffset,
            Texture2D chrome,
            Texture2D diffuseOverride = null)
        {
            DynamicVertexBuffer vertices = null;
            DynamicIndexBuffer indices = null;
            BMDLoader.Instance.GetModelBuffers(
                bmd, meshIndex, Color.White, bones, ref vertices, ref indices, false);
            if (vertices == null || indices == null)
                return;

            var mesh = bmd.Meshes[meshIndex];
            string path = BMDLoader.Instance.GetTexturePath(bmd, mesh.TexturePath);
            if (string.IsNullOrWhiteSpace(path))
                return;
            Texture2D diffuse = diffuseOverride ?? TextureLoader.Instance.GetTexture2D(path);
            if (diffuse == null)
                return;

            shader.Parameters["DiffuseTexture"]?.SetValue(diffuse);
            if (chrome != null)
                shader.Parameters["MaterialTexture"]?.SetValue(chrome);
            shader.Parameters["PassMode"]?.SetValue(passMode);
            shader.Parameters["MaterialColor"]?.SetValue(color);
            shader.Parameters["DiffuseUVOffset"]?.SetValue(uvOffset);

            gd.SetVertexBuffer(vertices);
            gd.Indices = indices;
            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, indices.IndexCount / 3);
            }
        }
    }
}
