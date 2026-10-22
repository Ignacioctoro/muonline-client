// Per-mesh Season 6 wing inventory styles from MuMain ItemRenderStyles.cpp.
// Bone-attached sprites and particles still require a ClassicFX preview bridge.
using System;
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
        private static Texture2D _previewRuinLayer;

        private static bool TryRenderNativeWingPreview(
            GraphicsDevice gd, BasicEffect baseEffect, int wingId, BMD bmd,
            Matrix[] bones, Matrix world, Matrix view, Matrix projection, float seconds)
        {
            int minMeshes = wingId switch { 6 => 1, 36 => 3, 39 => 2, 43 => 2, _ => 0 };
            var shader = GraphicsManager.Instance.ItemMaterialEffect;
            if (minMeshes == 0 || shader == null ||
                bmd?.Meshes == null || bmd.Meshes.Length < minMeshes)
                return false;

            Vector3 oldDiffuse = baseEffect.DiffuseColor;
            BlendState oldBlend = gd.BlendState;
            DepthStencilState oldDepth = gd.DepthStencilState;
            RasterizerState oldRasterizer = gd.RasterizerState;
            try
            {
                baseEffect.World = world;
                shader.CurrentTechnique = shader.Techniques[0];
                shader.Parameters["World"]?.SetValue(world);
                shader.Parameters["View"]?.SetValue(view);
                shader.Parameters["Projection"]?.SetValue(projection);
                shader.Parameters["Time"]?.SetValue(seconds);
                shader.Parameters["Alpha"]?.SetValue(1f);
                shader.Parameters["BaseLightScale"]?.SetValue(1f);
                shader.Parameters["ShadowStrength"]?.SetValue(0f);
                shader.Parameters["MaterialIntensity"]?.SetValue(1f);

                void BeginBright()
                {
                    gd.BlendState = _classicPreviewBrightAdditive;
                    gd.DepthStencilState = DepthStencilState.DepthRead;
                    gd.RasterizerState = RasterizerState.CullNone;
                }

                if (wingId == 36) // Wing of Storm: 2 texture, 0 bright, 1 animated bright
                {
                    baseEffect.DiffuseColor = new Vector3(1f, 0.7f, 0.5f);
                    RenderMeshWithBlendState(gd, baseEffect, bmd, 2, bones);
                    BeginBright();
                    DrawFlambergePreviewPass(gd, shader, bmd, bones, 0, 5,
                        new Vector3(1f, 0.7f, 0.5f), Vector2.Zero, null);
                    float frameU = (((int)(seconds * 25f) % 16) / 4) * 0.25f;
                    DrawFlambergePreviewPass(gd, shader, bmd, bones, 1, 6,
                        new Vector3(0.9f, 0.6f, 0.3f), new Vector2(frameU, 0f), null);
                }
                else if (wingId == 39) // Wing of Ruin
                {
                    RenderMeshWithBlendState(gd, baseEffect, bmd, 1, bones);
                    BeginBright();
                    float pulse0 = 0.1f + MathF.Abs(MathF.Sin(seconds)) * 0.3f;
                    DrawFlambergePreviewPass(gd, shader, bmd, bones, 0, 5,
                        new Vector3(pulse0), Vector2.Zero, null);
                    // MuMain ZzzOpenData.cpp: BITMAP_3RDWING_LAYER = Item/msword01_r.jpg.
                    if (_previewRuinLayer == null || _previewRuinLayer.IsDisposed)
                    {
                        try
                        {
                            const string layer = "Item/msword01_r.jpg";
                            _ = TextureLoader.Instance.Prepare(layer);
                            _previewRuinLayer = TextureLoader.Instance.GetTexture2D(layer);
                        }
                        catch (Exception) { /* Asset load retries on a later frame. */ }
                    }
                    if (_previewRuinLayer != null && !_previewRuinLayer.IsDisposed)
                    {
                        float pulse1 = MathF.Abs(MathF.Sin(seconds)) * 0.8f;
                        DrawFlambergePreviewPass(gd, shader, bmd, bones, 1, 5,
                            new Vector3(pulse1), Vector2.Zero, null, _previewRuinLayer);
                    }
                }
                else if (wingId == 6) // Wings of Darkness: tinted CHROME+1 then base
                {
                    if (EnsureClassicPreviewTextures() && _previewChrome02 != null)
                    {
                        BeginBright();
                        for (int i = 0; i < bmd.Meshes.Length; i++)
                            DrawFlambergePreviewPass(gd, shader, bmd, bones, i, 1,
                                new Vector3(0.4f, 0.3f, 0.5f), Vector2.Zero, _previewChrome02);
                    }
                    gd.BlendState = oldBlend;
                    gd.DepthStencilState = oldDepth;
                    for (int i = 0; i < bmd.Meshes.Length; i++)
                        RenderMeshWithBlendState(gd, baseEffect, bmd, i, bones);
                }
                else if (wingId == 43) // Wing of Dimension: base then mesh-1 chrome
                {
                    for (int i = 0; i < bmd.Meshes.Length; i++)
                        RenderMeshWithBlendState(gd, baseEffect, bmd, i, bones);
                    if (EnsureClassicPreviewTextures() && _previewChrome01 != null)
                    {
                        BeginBright();
                        DrawFlambergePreviewPass(gd, shader, bmd, bones, 1, 1,
                            Vector3.One, Vector2.Zero, _previewChrome01);
                    }
                }
            }
            finally
            {
                baseEffect.DiffuseColor = oldDiffuse;
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
    }
}
