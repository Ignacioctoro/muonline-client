using System;
using System.IO;

namespace Client.Main.Controls.UI.Game.Inventory
{
    public static partial class BmdPreviewRenderer
    {
        /// <summary>
        /// MuMain RenderItem3D calls RenderPartObject with HideSkin=true.
        /// CGMModelManager maps these filename prefixes to SKIN, HAIR and HIDE
        /// textures; ZzzBMD skips their meshes in the item preview. Keep every
        /// other shell/ornament mesh, instead of guessing one shell mesh index.
        /// This only filters the preview; shared BMD models stay untouched.
        /// </summary>
        private static bool ShouldDrawInventoryMesh(ItemDefinition definition, string texturePath)
        {
            if (definition.Group < 7 || definition.Group > 11 || string.IsNullOrEmpty(texturePath)) return true;
            string name = Path.GetFileName(texturePath.Replace('\\', '/'));
            // Modern Data also embeds class faces/hair without the classic
            // "skin_" prefix. Keep SkinClass*_headhelmet: that is the actual
            // helmet shell, despite sharing the face texture's name prefix.
            if (definition.Group == 7 && name.StartsWith("skinclass", StringComparison.OrdinalIgnoreCase))
                return name.Contains("_headhelmet", StringComparison.OrdinalIgnoreCase);
            return !(name.StartsWith("skin_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("level", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("hid", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("hair", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("HQskin", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("HQlevel_", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("HQhair", StringComparison.OrdinalIgnoreCase));
        }
    }
}
