#nullable enable

using Microsoft.Xna.Framework;

namespace Client.Main.Core.Utilities
{
    public readonly struct SkillIconInfo
    {
        public string TexturePath { get; }
        public Rectangle SourceRectangle { get; }

        public SkillIconInfo(string texturePath, Rectangle sourceRectangle)
        {
            TexturePath = texturePath;
            SourceRectangle = sourceRectangle;
        }
    }

    public static class SkillIconDatabase
    {
        public const int IconWidth = 20;
        public const int IconHeight = 28;
        public const string NewUiAtlas = "Interface/newui_skill00.OZJ";
        public const string NewUiDisabledAtlas = "Interface/newui_skill00_Non_Icon.OZJ";

        public static SkillIconInfo? GetIcon(ushort skillId, bool disabled = false)
        {
            if (skillId == 0 || skillId >= 300) return null;

            // S6 pet commands have a separate sheet and are not regular skills.
            if (DarkRavenCommands.IsCommandSkill(skillId))
                return Create(disabled ? "Interface/newui_non_command.OZJ" : "Interface/newui_command.OZJ",
                    skillId - DarkRavenCommands.Normal, 0);

            if (skillId == 76)
                return Create(disabled ? "Interface/newui_non_command.OZJ" : "Interface/newui_command.OZJ", 4, 0);

            // Data_Broyal NewUI: 25 cells per row, indexed by protocol skill ID.
            // The zero cell is reserved; unlike S6 sheets, do not subtract one.
            int iconId = skillId == 230 ? 216 : skillId; // S6 Lightning Shock alias.
            return Create(disabled ? NewUiDisabledAtlas : NewUiAtlas, iconId % 25, iconId / 25);
        }

        private static SkillIconInfo Create(string path, int column, int row) =>
            new(path, new Rectangle(column * IconWidth, row * IconHeight, IconWidth, IconHeight));
    }
}
