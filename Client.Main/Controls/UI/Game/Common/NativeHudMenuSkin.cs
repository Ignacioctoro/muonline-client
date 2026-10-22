using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Common
{
    /// <summary>
    /// Native Scaleform main.ozg skins, reconstructed from its DefineShape and
    /// DefineSubImage tags. Coordinates refer to main_IE, not a generic window.
    /// </summary>
    internal static class NativeHudMenuSkin
    {
        public const string AtlasPath = "Interface/GFx/main_IE.ozd";

        // SubMenuScrollingList (sprite 18, shape 17): subimages 16, 15, 14.
        private static readonly Rectangle MenuTop = new(0, 229, 145, 34);
        private static readonly Rectangle MenuMiddle = new(299, 163, 145, 8);
        private static readonly Rectangle MenuBottom = new(152, 163, 145, 9);

        // TooltipMainButton (sprite 199, shape 195): subimages 192, 194, 193.
        private static readonly Rectangle HelpLeft = new(211, 229, 26, 28);
        private static readonly Rectangle HelpMiddle = new(1016, 0, 8, 28);
        private static readonly Rectangle HelpRight = new(183, 229, 26, 28);

        public static void DrawCommunity(SpriteBatch sprite, Texture2D atlas, Rectangle bounds)
        {
            if (atlas == null || atlas.IsDisposed || bounds.Height < 43) return;
            sprite.Draw(atlas, new Rectangle(bounds.X, bounds.Y, bounds.Width, 34), MenuTop, Color.White);
            sprite.Draw(atlas, new Rectangle(bounds.X, bounds.Y + 34, bounds.Width, bounds.Height - 43),
                MenuMiddle, Color.White);
            sprite.Draw(atlas, new Rectangle(bounds.X, bounds.Bottom - 9, bounds.Width, 9), MenuBottom, Color.White);
        }

        public static void DrawHelp(SpriteBatch sprite, Texture2D atlas, Rectangle bounds)
        {
            if (atlas == null || atlas.IsDisposed || bounds.Width < 52) return;
            sprite.Draw(atlas, new Rectangle(bounds.X, bounds.Y, 26, bounds.Height), HelpLeft, Color.White);
            sprite.Draw(atlas, new Rectangle(bounds.X + 26, bounds.Y, bounds.Width - 52, bounds.Height),
                HelpMiddle, Color.White);
            sprite.Draw(atlas, new Rectangle(bounds.Right - 26, bounds.Y, 26, bounds.Height), HelpRight, Color.White);
        }
    }
}
