using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Inventory
{
    /// <summary>OpenMU Season 6 inventory addressing; each extension is a separate 8x4 storage.</summary>
    public static class ExtendedInventoryLayout
    {
        public const int FirstSlot = 76;
        public const int Columns = 8;
        public const int Rows = 4;
        public const int SlotsPerExtension = Columns * Rows;
        public const int MaximumExtensions = 4;
        public const int EndSlot = FirstSlot + SlotsPerExtension * MaximumExtensions;
        public const int Cell = 32;
        public static Rectangle Grid(int visibleBlock) => new(52, 178 + visibleBlock * 236, 256, 128);
        public static int Extension(int slot) => (slot - FirstSlot) / SlotsPerExtension;
        public static Point Position(int slot) => new((slot - FirstSlot) % Columns, (slot - FirstSlot) % SlotsPerExtension / Columns);
        public static bool Fits(int slot, int width, int height, int unlocked)
        {
            if (slot < FirstSlot || slot >= EndSlot || Extension(slot) >= unlocked || width < 1 || height < 1) return false;
            var pos = Position(slot);
            return pos.X + width <= Columns && pos.Y + height <= Rows;
        }
        public static int SlotAt(Point point, int page, int unlocked)
        {
            for (int block = 0; block < 2; block++)
            {
                var grid = Grid(block);
                int extension = page * 2 + block;
                if (extension >= MaximumExtensions || extension >= unlocked || !grid.Contains(point)) continue;
                return FirstSlot + extension * SlotsPerExtension + (point.Y - grid.Y) / Cell * Columns + (point.X - grid.X) / Cell;
            }
            return -1;
        }
    }
}
