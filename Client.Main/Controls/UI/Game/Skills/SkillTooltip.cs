#nullable enable
using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>Cached skill text, drawn only in the scene's final tooltip pass.</summary>
    internal sealed class SkillTooltip
    {
        private const string SiegeNotice = "Use in Castle Siege";
        private readonly List<string> _lines = new();
        private ushort _skillId;
        private Rectangle _anchor;
        private bool _visible;
        private bool _siegeOnly;

        public void SetSkill(SkillEntryState? skill, Rectangle anchor)
        {
            _anchor = anchor;
            _visible = skill != null;
            if (skill == null || (_skillId == skill.SkillId && _lines.Count > 0)) return;
            _skillId = skill.SkillId;
            _siegeOnly = CastleSiegeSkillRules.IsCastleSiegeOnly(_skillId);
            _lines.Clear();
            _lines.Add(SkillDatabase.GetSkillName(_skillId));
            if (DarkRavenCommands.IsCommandSkill(_skillId))
            {
                _lines.Add(DarkRavenCommands.GetDescription(_skillId));
                return;
            }

            var definition = SkillDatabase.GetSkillDefinition(_skillId);
            if (definition?.Damage > 0) _lines.Add($"Skill damage: {definition.Damage}");
            if (definition?.Distance > 0) _lines.Add($"Range: {definition.Distance}");
            ushort mana = SkillDatabase.GetSkillManaCost(_skillId);
            ushort ability = SkillDatabase.GetSkillAGCost(_skillId);
            if (mana > 0) _lines.Add($"Mana: {mana}");
            if (ability > 0) _lines.Add($"Stamina Use: {ability}");
        }

        public bool Draw(SpriteBatch sprite)
        {
            var graphics = GraphicsManager.Instance;
            var font = graphics?.Font;
            if (!_visible || font == null || _lines.Count == 0) return false;

            float scale = 10f / Constants.BASE_FONT_SIZE;
            int lineHeight = (int)MathF.Ceiling(font.LineSpacing * scale) + 2;
            int width = 160;
            for (int i = 0; i < _lines.Count; i++)
                width = Math.Max(width, (int)MathF.Ceiling(font.MeasureString(_lines[i]).X * scale) + 16);
            if (_siegeOnly)
                width = Math.Max(width, (int)MathF.Ceiling(font.MeasureString(SiegeNotice).X * scale) + 16);
            int height = 12 + _lines.Count * lineHeight + (_siegeOnly ? lineHeight + 6 : 0);
            int x = Math.Clamp(_anchor.Center.X - width / 2, 4, Math.Max(4, UiScaler.VirtualSize.X - width - 4));
            int y = _anchor.Top - height - 6;
            if (y < 4) y = _anchor.Bottom + 6;
            y = Math.Clamp(y, 4, Math.Max(4, UiScaler.VirtualSize.Y - height - 4));
            var bounds = new Rectangle(x, y, width, height);
            var pixel = graphics!.Pixel;
            sprite.Draw(pixel, bounds, new Color(0, 0, 0, 235));
            Color border = new(75, 70, 62);
            sprite.Draw(pixel, new Rectangle(x, y, width, 1), border);
            sprite.Draw(pixel, new Rectangle(x, bounds.Bottom - 1, width, 1), border);
            sprite.Draw(pixel, new Rectangle(x, y, 1, height), border);
            sprite.Draw(pixel, new Rectangle(bounds.Right - 1, y, 1, height), border);

            for (int i = 0; i < _lines.Count; i++)
                DrawLine(sprite, font, _lines[i], bounds, y + 6 + i * lineHeight,
                    scale, i == 0 ? new Color(130, 180, 240) : Color.White);
            if (_siegeOnly)
            {
                int bannerY = y + 8 + _lines.Count * lineHeight;
                sprite.Draw(pixel, new Rectangle(x + 4, bannerY, width - 8, lineHeight + 2),
                    new Color(170, 0, 0));
                DrawLine(sprite, font, SiegeNotice, bounds, bannerY + 1, scale, Color.White);
            }
            return true;
        }

        private static void DrawLine(SpriteBatch sprite, SpriteFont font, string text,
            Rectangle bounds, int y, float scale, Color color)
        {
            float x = bounds.X + (bounds.Width - font.MeasureString(text).X * scale) / 2f;
            sprite.DrawString(font, text, new Vector2(x + 1, y + 1), Color.Black,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sprite.DrawString(font, text, new Vector2(x, y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
