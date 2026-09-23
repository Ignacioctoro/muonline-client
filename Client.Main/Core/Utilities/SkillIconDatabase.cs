#nullable enable

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Utilities
{
    public readonly struct SkillIconInfo
    {
        public string TexturePath { get; }
        public Rectangle SourceRectangle { get; }

        public SkillIconInfo(
            string texturePath,
            Rectangle sourceRectangle)
        {
            TexturePath = texturePath;
            SourceRectangle = sourceRectangle;
        }
    }

    public static class SkillIconDatabase
    {
        public const int IconWidth = 20;
        public const int IconHeight = 28;

        private const ushort SpiralSlash = 57;
        private const ushort PlasmaStorm = 76;
        private const ushort KillingBlow = 260;
        private const ushort MasterSkillFirst = 300;

        private static readonly Dictionary<ushort, Point>
            Sheet2SpecialCells =
            new()
            {
                { 214, new Point(0, 3) },
                { 215, new Point(1, 3) },
                { 216, new Point(2, 3) },
                { 217, new Point(3, 3) },
                { 219, new Point(4, 3) },
                { 220, new Point(5, 3) },
                { 223, new Point(6, 3) },
                { 224, new Point(7, 3) },
                { 221, new Point(8, 3) },
                { 222, new Point(9, 3) },
                { 218, new Point(10, 3) },
                { 225, new Point(11, 3) },

                { 230, new Point(2, 3) },

                { 232, new Point(7, 2) },
                { 233, new Point(8, 2) },
                { 234, new Point(9, 2) },

                { 235, new Point(0, 8) },
                { 236, new Point(1, 8) },
                { 237, new Point(2, 8) },
                { 238, new Point(3, 8) }
            };

        public static SkillIconInfo? GetIcon(
            ushort skillId,
            bool disabled = false)
        {
            if (skillId == 0 ||
                skillId >= MasterSkillFirst)
            {
                return null;
            }

            string skill1 =
                disabled
                    ? "Interface/newui_non_skill.OZJ"
                    : "Interface/newui_skill.OZJ";

            string skill2 =
                disabled
                    ? "Interface/newui_non_skill2.OZJ"
                    : "Interface/newui_skill2.OZJ";

            string skill3 =
                disabled
                    ? "Interface/newui_non_skill3.OZJ"
                    : "Interface/newui_skill3.OZJ";

            string command =
                disabled
                    ? "Interface/newui_non_command.OZJ"
                    : "Interface/newui_command.OZJ";

            // Fenrir Plasma Storm
            if (skillId == PlasmaStorm)
            {
                return Create(
                    command,
                    4,
                    0);
            }

            // Special Season 6 skills
            if (Sheet2SpecialCells.TryGetValue(
                    skillId,
                    out Point special))
            {
                return Create(
                    skill2,
                    special.X,
                    special.Y);
            }

            // Rage Fighter
            if (skillId >= KillingBlow)
            {
                int index =
                    skillId - KillingBlow;

                return Create(
                    skill3,
                    index % 12,
                    index / 12);
            }

            // Second skill sheet
            if (skillId >= SpiralSlash)
            {
                int index =
                    skillId - SpiralSlash;

                return Create(
                    skill2,
                    index % 8,
                    index / 8);
            }

            // Main skill sheet
            int firstIndex =
                skillId - 1;

            return Create(
                skill1,
                firstIndex % 8,
                firstIndex / 8);
        }

        private static SkillIconInfo Create(
            string texturePath,
            int column,
            int row)
        {
            return new SkillIconInfo(
                texturePath,
                new Rectangle(
                    column * IconWidth,
                    row * IconHeight,
                    IconWidth,
                    IconHeight));
        }
    }
}