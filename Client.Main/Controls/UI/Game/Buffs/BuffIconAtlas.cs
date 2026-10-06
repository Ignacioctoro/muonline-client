#nullable enable

using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Buffs
{
    /// <summary>
    /// Región de una de las texturas originales de iconos
    /// de buffs de MU.
    /// </summary>
    internal readonly struct BuffIconFrame
    {
        public BuffIconFrame(
            string texturePath,
            Rectangle sourceRectangle)
        {
            TexturePath = texturePath;
            SourceRectangle = sourceRectangle;
        }

        public string TexturePath { get; }

        public Rectangle SourceRectangle { get; }
    }

    /// <summary>
    /// Mapeo clásico de iconos de buffs/debuffs de MU Season 6.
    ///
    /// Basado en:
    /// CNewUIBuffWindow::RenderBuffIcon()
    ///
    /// Los EffectId 1..80 utilizan newui_statusicon.
    /// Desde 81 utilizan newui_statusicon2.
    /// </summary>
    internal static class BuffIconAtlas
    {
        public const int IconWidth = 20;
        public const int IconHeight = 28;

        public const string StatusTexturePath =
            "Interface/newui_statusicon.jpg";

        public const string Status2TexturePath =
            "Interface/newui_statusicon2.jpg";

        public static readonly string[] TexturePaths =
        {
            StatusTexturePath,
            Status2TexturePath
        };

        private const int AtlasSize = 256;

        // SourceMain:
        //
        // iWidthIndex =
        //     (eBuffType - 1) % 10;
        //
        private const int IconsPerRow = 10;

        /// <summary>
        /// Buffs que el cliente clásico considera debuffs.
        /// Se usan también para ordenar la barra:
        /// buffs primero, debuffs después.
        /// </summary>
        public static bool IsDebuff(
            byte effectId)
        {
            return effectId switch
            {
                >= 55 and <= 65 => true,

                >= 72 and <= 77 => true,

                >= 83 and <= 86 => true,

                120 => true,

                186 => true,

                _ => false
            };
        }

        /// <summary>
        /// Algunos estados existen internamente, pero SourceMain
        /// no los muestra como icono en la barra.
        /// </summary>
        public static bool ShouldRender(
            byte effectId)
        {
            return effectId switch
            {
                // eDeBuff_FlameStrikeDamage
                83 => false,

                // eDeBuff_GiganticStormDamage
                84 => false,

                // eDeBuff_LightningShockDamage
                85 => false,

                // eDeBuff_Discharge_Stamina
                120 => false,

                _ => true
            };
        }

        /// <summary>
        /// Obtiene textura + región del atlas para un EffectId.
        /// </summary>
        public static bool TryResolve(
            byte effectId,
            out BuffIconFrame frame)
        {
            frame = default;

            if (effectId == 0 ||
                !ShouldRender(effectId))
            {
                return false;
            }

            int iconIndex;
            string texturePath;

            // =====================================================
            // ORIGINAL MU
            // =====================================================
            //
            // if (eBuffType < 81)
            // {
            //     index = eBuffType - 1;
            // }
            // else
            // {
            //     index = eBuffType - 81;
            // }
            //
            // =====================================================

            if (effectId < 81)
            {
                iconIndex =
                    effectId - 1;

                texturePath =
                    StatusTexturePath;
            }
            else
            {
                iconIndex =
                    effectId - 81;

                texturePath =
                    Status2TexturePath;
            }

            int column =
                iconIndex %
                IconsPerRow;

            int row =
                iconIndex /
                IconsPerRow;

            int x =
                column *
                IconWidth;

            int y =
                row *
                IconHeight;

            // Evita leer fuera del atlas de 256x256.
            if (x < 0 ||
                y < 0 ||
                x + IconWidth > AtlasSize ||
                y + IconHeight > AtlasSize)
            {
                return false;
            }

            frame =
                new BuffIconFrame(
                    texturePath,
                    new Rectangle(
                        x,
                        y,
                        IconWidth,
                        IconHeight));

            return true;
        }
    }
}