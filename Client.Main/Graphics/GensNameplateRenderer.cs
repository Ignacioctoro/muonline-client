using System;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Networking;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Graphics;

/// <summary>Native 50x69 Gens marks, fitted to the existing world-name scale.</summary>
public static class GensNameplateRenderer
{
    public static bool TryDraw(PlayerObject player)
    {
        if (!Constants.SHOW_NAMES_ON_HOVER || !GensCombatRules.TryGetRelation(player, out var relation)) return false;
        bool hovered = player.IsMouseHover;
        if (!hovered && !GensCombatRules.IsBattleZone) return false;
        var manager = GraphicsManager.Instance;
        var texture = GensControl.Instance.GetMarkTexture(relation.Family);
        var font = manager.Font;
        if (texture == null || texture.IsDisposed || font == null || manager.Pixel == null) return false;
        var box = player.BoundingBoxWorld;
        var anchor = new Vector3((box.Min.X + box.Max.X) * .5f, (box.Min.Y + box.Max.Y) * .5f, box.Max.Z + 20);
        var screen = manager.GraphicsDevice.Viewport.Project(anchor, Camera.Instance.Projection, Camera.Instance.View, Matrix.Identity);
        if (screen.Z < 0 || screen.Z > 1) return true;
        float renderScale = Constants.RENDER_SCALE;
        int markWidth = Math.Max(1, (int)MathF.Round(25 * renderScale)), markHeight = Math.Max(1, (int)MathF.Round(34.5f * renderScale));
        uint rank = relation.Rank is >= 1 and <= 14 ? relation.Rank : 14;
        int tile = 14 - (int)rank;
        var source = new Rectangle(tile % 5 * 50, tile / 5 * 69, 50, 69);
        var batch = manager.Sprite;
        using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        if (!hovered)
        {
            // In Vulcanus a small mark remains visible without expanding every player's name panel.
            batch.Draw(texture, new Rectangle((int)screen.X - markWidth / 2, (int)screen.Y - markHeight, markWidth, markHeight), source, Color.White);
            return true;
        }
        bool enemy = GensCombatRules.IsEnemy(player);
        // F8 05 sends a capped contribution for this native concealment rule, never for the own info window.
        bool concealName = enemy && relation.Contribution == 0;
        string name = concealName ? relation.Family == 1 ? "Duprian" : "Vanert" : player.DisplayName;
        if (string.IsNullOrEmpty(name)) return true;
        string guildLine = "";
        if (!concealName && GuildInfoCache.TryGetPlayerGuildId(player.NetworkId, out uint guildId) && guildId != 0
            && GuildInfoCache.TryGetGuild(guildId, out var guild))
        {
            string role = GuildInfoCache.TryGetPlayerGuildRole(player.NetworkId, out byte status) ? status switch
            { 0x80 => "Guild Master", 0x40 => "Assistant Master", 0x20 => "Battle Master", _ => "Member" } : "";
            guildLine = string.IsNullOrEmpty(role) ? "Guild: " + guild.GuildName : "Guild: " + guild.GuildName + " | " + role;
        }
        float textScale = .4f * renderScale, gap = Math.Max(1, renderScale), padding = 4 * renderScale;
        var nameSize = font.MeasureString(name) * textScale;
        var guildSize = font.MeasureString(guildLine) * textScale;
        float textWidth = Math.Max(nameSize.X, guildSize.X);
        float textHeight = nameSize.Y + (guildLine.Length == 0 ? 0 : gap + guildSize.Y);
        float height = Math.Max(markHeight, textHeight) + padding * 2;
        float width = markWidth + gap + textWidth + padding * 2;
        var background = new Rectangle((int)(screen.X - width / 2), (int)(screen.Y - height), Math.Max(1, (int)width), Math.Max(1, (int)height));
        batch.Draw(manager.Pixel, background, enemy ? new Color(65, 10, 10, 175) : new Color(14, 18, 24, 175));
        batch.Draw(texture, new Rectangle(background.X + (int)padding, background.Center.Y - markHeight / 2, markWidth, markHeight), source, Color.White);
        var position = new Vector2(background.X + padding + markWidth + gap, background.Center.Y - textHeight / 2);
        var color = enemy ? new Color(255, 110, 95) : new Color(190, 225, 255);
        batch.DrawString(font, name, position + Vector2.One, Color.Black, 0, Vector2.Zero, textScale, SpriteEffects.None, 0);
        batch.DrawString(font, name, position, color, 0, Vector2.Zero, textScale, SpriteEffects.None, 0);
        if (guildLine.Length != 0)
            batch.DrawString(font, guildLine, position + new Vector2(0, nameSize.Y + gap), Color.White, 0, Vector2.Zero, textScale, SpriteEffects.None, 0);
        return true;
    }
}
