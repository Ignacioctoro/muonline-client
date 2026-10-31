using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>main.ozg SkillHotKey: 26x36 icon and 36x45 selection frame.</summary>
    internal static class NewUiSkillSkin
    {
        public const string AtlasPath = "Interface/GFx/main_IE.ozd";
        public const int Width = 36;
        public const int Height = 45;
        public const int IconWidth = 26;
        public const int IconHeight = 36;
        public const int IconX = 5;
        public const int IconY = 5;

        // Empty skill socket from the existing HUD panel; selection subimage 151.
        public static readonly Rectangle NormalFrame = new(531, 32, 30, 42);
        public static readonly Rectangle SelectedFrame = new(48, 174, 36, 45);
    }
}
