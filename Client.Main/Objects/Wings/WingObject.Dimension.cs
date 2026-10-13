using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Wings
{
    public partial class WingObject
    {
        // MU Season 6: group 12, index 43 -> Item/Wing44.bmd.
        // Do not confuse this S3 wing with Wings of Despair (12,42 / Wing43.bmd).
        private const short DimensionWingItemIndex = 43;

        private bool IsDimensionWing =>
            ItemIndex == DimensionWingItemIndex;

        /// <summary>
        /// Original Main (RenderWingOfDimension):
        ///     RenderBody(RENDER_TEXTURE, ...);
        ///     RenderMesh(1, RENDER_BRIGHT | RENDER_CHROME, ...);
        ///
        /// The normal Neffis mesh pass already handles RenderBody.
        /// Add only the native Chrome01 bright pass on mesh 1, AFTER
        /// both the solid and translucent base-model passes have drawn.
        /// Reuse ModelObject's normal-derived chrome UV shader and
        /// read-only depth state rather than inventing a glow texture
        /// or placing stationary lights on unverified BMD bones.
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
    }
}
