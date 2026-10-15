namespace Client.Main.Controls.UI.Game.Common
{
    /// <summary>
    /// Logical pixels before UiScaler. Inventory, Vault and NPC Shop share
    /// the same full-height GFx footprint and 32px item-cell pitch. Character,
    /// Chaos Mix and Pet Trainer keep their independent compact dialog size.
    /// </summary>
    public static class GameWindowMetrics
    {
        public const int FullHeightWindowWidth = 360;
        public const int FullHeightWindowHeight = 690;
        public const int InventoryWidth = FullHeightWindowWidth;
        public const int InventoryHeight = FullHeightWindowHeight;
        public const int InventoryCell = 32;
        public const int SecondaryWidth = 348;
        public const int SecondaryHeight = 640;
        public const int SecondaryCell = 29;
        public const int ContentMargin = 18;
        public const int SectionGap = 10;
        public const int ActionHeight = 48;
    }
}
