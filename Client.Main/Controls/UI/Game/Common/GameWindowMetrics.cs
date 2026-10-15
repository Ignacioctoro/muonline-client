namespace Client.Main.Controls.UI.Game.Common
{
    /// <summary>
    /// Logical UI pixels before UiScaler. Keep item rendering and hit testing
    /// on the same cell size. Compact dialogs share the secondary width but
    /// may use a shorter height when they do not contain an item grid.
    /// </summary>
    public static class GameWindowMetrics
    {
        public const int InventoryWidth = 420;
        public const int InventoryHeight = 690;
        public const int InventoryCell = 32;
        public const int SecondaryWidth = 348;
        public const int SecondaryHeight = 640;
        public const int SecondaryCell = 29;
        public const int ContentMargin = 18;
        public const int SectionGap = 10;
        public const int ActionHeight = 48;
    }
}
