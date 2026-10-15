using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Common
{
    /// <summary>
    /// Shared NewUI GFx window skin. The source rectangles come from
    /// Data_Broyal/Interface/GFx/PersonalShop_I1.ozd (1024x1024 atlas).
    /// Use it for compact or full-height dialogs without stretching the
    /// ornate border corners or importing adjacent atlas sprites.
    /// </summary>
    public static class NewUiFrameRenderer
    {
        public const string PersonalShopAtlasPath = "Interface/GFx/PersonalShop_I1.ozd";
        public const int PersonalShopHeaderHeight = 96;

        // The original Personal Shop has a large dark textured frame on the
        // atlas's left. The independent upper-right panel is its inset skin.
        // Keep each source entirely inside its sprite, away from the gutters.
        public static readonly Rectangle PersonalShopFrameSource = new(0, 0, 575, 638);
        public static readonly Rectangle PersonalShopInsetSource = new(590, 1, 312, 376);
        // The upper-right sprite starts outside the atlas: its top border is absent.
        // The panel immediately below it supplies a complete matching top rail.
        private static readonly Rectangle PersonalShopInsetTopSource = new(590, 386, 312, 12);

        public static void DrawPersonalShopWindow(SpriteBatch spriteBatch,
            Texture2D atlas, Rectangle destination)
        {
            DrawNineSlice(spriteBatch, atlas, PersonalShopFrameSource,
                destination, 16, PersonalShopHeaderHeight, 16, 39);
        }

        public static void DrawPersonalShopInset(SpriteBatch spriteBatch,
            Texture2D atlas, Rectangle destination)
        {
            DrawNineSlice(spriteBatch, atlas, PersonalShopInsetSource,
                destination, 12, 12, 12, 12);
            if (atlas == null || atlas.IsDisposed || destination.Width <= 24 || destination.Height <= 24)
                return;

            // Keep corner widths intact instead of stretching the rail as one sprite.
            spriteBatch.Draw(atlas, new Rectangle(destination.X, destination.Y, 12, 12),
                new Rectangle(PersonalShopInsetTopSource.X, PersonalShopInsetTopSource.Y, 12, 12), Color.White);
            spriteBatch.Draw(atlas, new Rectangle(destination.X + 12, destination.Y, destination.Width - 24, 12),
                new Rectangle(PersonalShopInsetTopSource.X + 12, PersonalShopInsetTopSource.Y, 288, 12), Color.White);
            spriteBatch.Draw(atlas, new Rectangle(destination.Right - 12, destination.Y, 12, 12),
                new Rectangle(PersonalShopInsetTopSource.Right - 12, PersonalShopInsetTopSource.Y, 12, 12), Color.White);
        }

        /// <summary>
        /// Render nine atlas regions with independent source/destination
        /// dimensions, preserving corners, title ornament and footer pixels.
        /// </summary>
        public static void DrawNineSlice(SpriteBatch sb, Texture2D atlas,
            Rectangle source, Rectangle target, int left, int top, int right, int bottom)
        {
            if (sb == null || atlas == null || atlas.IsDisposed ||
                target.Width <= left + right || target.Height <= top + bottom ||
                source.Width <= left + right || source.Height <= top + bottom)
            {
                return;
            }

            int[] srcX = { source.Left, source.Left + left, source.Right - right, source.Right };
            int[] srcY = { source.Top, source.Top + top, source.Bottom - bottom, source.Bottom };
            int[] dstX = { target.Left, target.Left + left, target.Right - right, target.Right };
            int[] dstY = { target.Top, target.Top + top, target.Bottom - bottom, target.Bottom };

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    sb.Draw(atlas,
                        new Rectangle(dstX[col], dstY[row],
                            dstX[col + 1] - dstX[col],
                            dstY[row + 1] - dstY[row]),
                        new Rectangle(srcX[col], srcY[row],
                            srcX[col + 1] - srcX[col],
                            srcY[row + 1] - srcY[row]),
                        Color.White);
                }
            }
        }
    }
}
