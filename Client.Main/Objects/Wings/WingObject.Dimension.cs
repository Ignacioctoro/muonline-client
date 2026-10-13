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
        // MU Season 6: group 12, index 43 -> Item/Wing44.bmd.
        // Wings of Despair (12,42 / Wing43.bmd) are second-tier wings.
        private const short DimensionWingItemIndex = 43;

        private const string DimensionFlareTexturePath = "Effect/Flare.jpg";

        // Main C++: ItemEffects.cpp / ApplyWingOfDimension.
        // Flare positions use BMD bone indices, not arbitrary world offsets.
        private static readonly int[] DimensionLargeGoldBones = { 7, 30 };
        private static readonly int[] DimensionSmallGoldBones = { 31, 43, 8, 20 };
        private static readonly int[] DimensionPinkBones = { 29, 38, 42, 19, 15, 6 };

        private Texture2D _dimensionFlareTexture;
        private bool _dimensionFlarePrepared;

        private bool IsDimensionWing =>
            ItemIndex == DimensionWingItemIndex;

        private async Task PrepareDimensionClassicAssetsAsync()
        {
            if (_dimensionFlarePrepared)
                return;

            // BITMAP_FLARE = Effect/Flare.jpg (Effect/Flare.OZJ in MU data).
            // Case is important on Android.
            await TextureLoader.Instance.Prepare(DimensionFlareTexturePath);
            _dimensionFlarePrepared = true;
        }

        private void EnsureDimensionFlareTexture()
        {
            if (!_dimensionFlarePrepared)
                return;

            if (_dimensionFlareTexture == null || _dimensionFlareTexture.IsDisposed)
            {
                _dimensionFlareTexture =
                    TextureLoader.Instance.GetTexture2D(DimensionFlareTexturePath);
            }
        }

        /// <summary>
        /// Original Main / RenderWingOfDimension:
        ///     RenderBody(RENDER_TEXTURE, ...);
        ///     RenderMesh(1, RENDER_BRIGHT | RENDER_CHROME, ...);
        /// The normal Neffis path renders the base mesh. This applies
        /// only the original chrome overlay, not a replacement material.
        /// </summary>
        private void DrawDimensionClassicChrome()
        {
            if (!Visible ||
                !IsDimensionWing ||
                Model?.Meshes == null ||
                Model.Meshes.Length <= 1)
            {
                return;
            }

            DrawClassicChromePass(
                mesh: 1,
                materialColor: Vector3.One);
        }

        /// <summary>
        /// Faithful port of ItemEffects.cpp / ApplyWingOfDimension:
        ///
        /// Scale      = abs(sin(WorldTime * .002)) * .2;
        /// Luminosity = abs(sin(WorldTime * .002)) * .4;
        ///
        /// Two large warm-gold flares (bones 7,30; Scale+2),
        /// four small gold flares (31,43,8,20; Scale+.5),
        /// six large rose/magenta flares (29,38,42,19,15,6; Scale+2).
        ///
        /// The original calls CreateSprite(BITMAP_FLARE, ...) per frame,
        /// anchored to each wing bone, not spawned as traveling particles.
        /// Reuse the existing Season 6 world-to-screen billboard renderer.
        /// </summary>
        private void DrawDimensionClassicEffects(GameTime gameTime)
        {
            if (!Visible ||
                !IsDimensionWing ||
                Model == null ||
                BoneTransform == null)
            {
                return;
            }

            EnsureDimensionFlareTexture();

            if (_dimensionFlareTexture == null || _dimensionFlareTexture.IsDisposed)
                return;

            // Original WorldTime is in milliseconds: .002/ms = 2/sec.
            float timeSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
            float pulse = MathF.Abs(MathF.Sin(timeSeconds * 2.0f));
            float scale = pulse * 0.2f;
            float luminosity = pulse * 0.4f;

            Vector3 largeGoldLight = new Vector3(
                (1.0f + luminosity) / 2.0f,
                (0.7f + luminosity) / 2.0f,
                (0.2f + luminosity) / 2.0f);

            Vector3 smallGoldLight = new Vector3(
                (1.0f + luminosity) / 4.0f,
                (0.7f + luminosity) / 4.0f,
                (0.2f + luminosity) / 4.0f);

            Vector3 pinkLight = new Vector3(
                (0.5f + luminosity) / 2.0f,
                (0.1f + luminosity) / 2.0f,
                (0.4f + luminosity) / 2.0f);

            GraphicsDevice gd = GraphicsDevice;
            BlendState previousBlend = gd.BlendState;
            SpriteBatch spriteBatch = GraphicsManager.Instance.Sprite;

            using (new SpriteBatchScope(
                spriteBatch,
                SpriteSortMode.Deferred,
                StormBrightAdditive,
                SamplerState.LinearClamp,
                DepthStencilState.DepthRead,
                RasterizerState.CullNone))
            {
                DrawDimensionFlareBones(spriteBatch, DimensionLargeGoldBones, scale + 2.0f, largeGoldLight);
                DrawDimensionFlareBones(spriteBatch, DimensionSmallGoldBones, scale + 0.5f, smallGoldLight);
                DrawDimensionFlareBones(spriteBatch, DimensionPinkBones, scale + 2.0f, pinkLight);
            }

            gd.BlendState = previousBlend;
        }

        private void DrawDimensionFlareBones(
            SpriteBatch spriteBatch,
            int[] bones,
            float scale,
            Vector3 light)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                int bone = bones[i];
                if (bone < 0 || bone >= BoneTransform.Length)
                    continue;

                Vector3 position = Vector3.Transform(
                    BoneTransform[bone].Translation,
                    WorldPosition);

                DrawStormBillboard(
                    spriteBatch,
                    _dimensionFlareTexture,
                    position,
                    scale,
                    light,
                    0.0f);
            }
        }
    }
}
