using Client.Main.Controls.UI.Game.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>
    /// Shared seller/buyer layout: a compact name field, the Season 6 8x4 grid
    /// and a bottom action row, using only the existing GFx NewUI skin.
    /// </summary>
    internal static class PersonalStoreLayout
    {
        public const int Width = GameWindowMetrics.InventoryWidth;
        public const int Height = GameWindowMetrics.InventoryHeight;
        public const int FooterY = Height - 52;
        public static readonly Rectangle Name = new(52, 96, 256, 28);
        public static readonly Rectangle Grid = new(52, 150, 8 * GameWindowMetrics.InventoryCell, 4 * GameWindowMetrics.InventoryCell);
        public static readonly Rectangle Close = new(Width - 44, 35, 25, 25);
        public static readonly Rectangle Exit = new(24, FooterY, 44, 32);
        public static readonly Rectangle Open = new(84, FooterY, 78, 32);
        public static readonly Rectangle CloseStore = new(178, FooterY, 78, 32);
        public static readonly Rectangle State = new(28, FooterY - 62, Width - 56, 24);
        public static readonly Rectangle Feedback = new(24, FooterY - 34, Width - 48, 24);

        public static void DrawNameField(SpriteBatch sprite, Texture2D pixel, Rectangle rect, float alpha)
        {
            // Match the warm NewUI trim, keeping a visible frame in both read-only views.
            sprite.Draw(pixel, rect, new Color(37, 28, 20) * alpha);
            Color border = new Color(159, 121, 70) * alpha;
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), new Color(209, 165, 99) * alpha);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), border);
            sprite.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), border);
            sprite.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), border);
        }

        public static bool IsTopmostAtPointer(GameControl window, Point pointer)
        {
            if (!window.DisplayRectangle.Contains(pointer) || window.Parent == null)
                return false;
            var siblings = window.Parent.Controls;
            for (int i = siblings.Count - 1; i >= 0; i--)
            {
                var control = siblings[i];
                // The cursor is intentionally non-interactive and always follows the pointer.
                if (control.Visible && control.Interactive && control.DisplayRectangle.Contains(pointer))
                    return ReferenceEquals(control, window);
            }
            return false;
        }
    }
}
