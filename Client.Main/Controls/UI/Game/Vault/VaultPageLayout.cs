using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game
{
    public static class VaultPageLayout
    {
        public const int Columns = 8;
        public const int Rows = 15;
        public const int PageSize = Columns * Rows;
        public static int Slot(int page, Point position) => page * PageSize + position.Y * Columns + position.X;
        public static bool Contains(int page, int slot) => slot >= page * PageSize && slot < (page + 1) * PageSize;
        public static bool Fits(int page, Point position, int width, int height, bool extended) =>
            page >= 0 && page <= (extended ? 1 : 0) && position.X >= 0 && position.Y >= 0 &&
            width > 0 && height > 0 && position.X + width <= Columns && position.Y + height <= Rows;
    }
}
