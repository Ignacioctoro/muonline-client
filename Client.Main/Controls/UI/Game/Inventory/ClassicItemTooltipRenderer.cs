using System;
using System.Collections.Generic;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Inventory
{
    /// <summary>
    /// Shared Season 6 item tooltip presentation for Inventory and Personal Store.
    /// Lines come from ClassicItemTooltipBuilder, so item names/options are identical.
    /// Keep the same colors/layout as InventoryControl's original tooltip renderer.
    /// Call inside an active SpriteBatch using UiScaler.SpriteTransform.
    /// </summary>
    internal static class ClassicItemTooltipRenderer
    {
        private static readonly Color TextWhite = new(240, 240, 245);
        private static readonly Color SecondaryBright = new(130, 180, 240);
        private static readonly Color GlowExcellent = new(120, 255, 120, 60);
        private static readonly Color GlowAncient = new(80, 200, 255, 70);
        private static readonly Color Accent = new(212, 175, 85);

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, InventoryItem item,
            List<(string text, Color color)> lines, Rectangle itemRect, Point mousePosition)
        {
            if (spriteBatch == null || font == null || item == null || lines == null || lines.Count == 0)
                return;

            const float titleScale = 0.44f;
            const float bodyScale = 0.40f;
            const int lineSpacing = 4;
            const int paddingX = 14;
            const int paddingY = 12;

            // Calculate tooltip size
            int maxWidth = 0;
            int totalHeight = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                var (text, _) = lines[i];

                float lineScale =
                    i == 0
                        ? titleScale
                        : bodyScale;

                Vector2 sz =
                    font.MeasureString(text) *
                    lineScale;

                maxWidth =
                    Math.Max(
                        maxWidth,
                        (int)MathF.Ceiling(sz.X));

                totalHeight +=
                    (int)MathF.Ceiling(sz.Y) +
                    lineSpacing;
            }

            // Add separator after the first line
            totalHeight += 6;

            int tooltipWidth = maxWidth + paddingX * 2;
            int tooltipHeight = totalHeight + paddingY * 2;

            Rectangle hoveredItemRect = itemRect;

            // Tooltip positioning
            Rectangle tooltipRect = new(mousePosition.X + 16, mousePosition.Y + 16, tooltipWidth, tooltipHeight);
            Rectangle screenBounds = new(0, 0, UiScaler.VirtualSize.X, UiScaler.VirtualSize.Y);

            // Avoid overlapping the item
            if (tooltipRect.Intersects(hoveredItemRect))
            {
                // Try left side
                tooltipRect.X = hoveredItemRect.X - tooltipWidth - 8;
                tooltipRect.Y = hoveredItemRect.Y;

                if (tooltipRect.X < 10 || tooltipRect.Intersects(hoveredItemRect))
                {
                    // Try above
                    tooltipRect.X = hoveredItemRect.X;
                    tooltipRect.Y = hoveredItemRect.Y - tooltipHeight - 8;

                    if (tooltipRect.Y < 10)
                    {
                        // Under the item
                        tooltipRect.X = hoveredItemRect.X;
                        tooltipRect.Y = hoveredItemRect.Bottom + 8;
                    }
                }
            }

            // Clamp to screen bounds
            tooltipRect.X = Math.Clamp(tooltipRect.X, 10, Math.Max(10, screenBounds.Right - tooltipRect.Width - 10));
            tooltipRect.Y = Math.Clamp(tooltipRect.Y, 10, Math.Max(10, screenBounds.Bottom - tooltipRect.Height - 10));

            var pixel = GraphicsManager.Instance.Pixel;
            if (pixel == null) return;

            // ═══════════════════════════════════════════════════════════
            // TOOLTIP BACKGROUND
            // ═══════════════════════════════════════════════════════════

            // Drop shadow
            var shadowRect = new Rectangle(tooltipRect.X + 4, tooltipRect.Y + 4, tooltipRect.Width, tooltipRect.Height);
            spriteBatch.Draw(pixel, shadowRect, Color.Black * 0.5f);

            // Main background
            UiDrawHelper.DrawVerticalGradient(spriteBatch, tooltipRect, new Color(20, 24, 32, 252), new Color(12, 14, 18, 254));

            // =========================================================
            // ANCIENT ITEM NAME BACKGROUND
            //
            // Original MU highlights the item name row, not the Ancient
            // option list.
            // =========================================================

            bool isAncient =
                item.Details.IsAncient;

            if (isAncient &&
                lines.Count > 0)
            {
                Vector2 firstLineSize =
                    font.MeasureString(
                        lines[0].text) *
                    titleScale;

                int titleHeight =
                    (int)MathF.Ceiling(
                        firstLineSize.Y) + 6;

                Rectangle ancientTitleRect =
                    new(
                        tooltipRect.X + 2,
                        tooltipRect.Y + 5,
                        tooltipRect.Width - 4,
                        titleHeight);

                UiDrawHelper.DrawVerticalGradient(
                    spriteBatch,
                    ancientTitleRect,
                    new Color(
                        32,
                        70,
                        155,
                        235),
                    new Color(
                        14,
                        38,
                        105,
                        235));
            }

            // Border color based on item rarity.
            //
            // Fenrir is a special original-client case:
            // - Normal red Fenrir keeps a white tooltip border.
            // - Destroy / Protect / Illusion use the Fenrir title blue.
            // - Fenrir must not inherit the generic Excellent border.
            bool isExcellent =
                item.Details.IsExcellent;

            bool isHighLevel =
                item.Details.Level >= 7;

            bool isFenrir =
                item.Definition != null &&
                item.Definition.Group == 13 &&
                item.Definition.Id == 37;

            byte fenrirVariant =
                (byte)(
                    item.Details.FenrirFlags &
                    0x3F);

            Color borderColor;

            if (isFenrir)
            {
                borderColor =
                    fenrirVariant == 0x00
                        ? TextWhite
                        : lines.Count > 0
                            ? lines[0].color
                            : SecondaryBright;
            }
            else
            {
                borderColor =
                    isExcellent ? GlowExcellent :
                    isAncient ? GlowAncient :
                    isHighLevel ? Accent :
                    TextWhite;
            }

            // Uniform border all around
            const int borderThickness = 2;
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Bottom - borderThickness, tooltipRect.Width, borderThickness), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.X, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);
            spriteBatch.Draw(pixel, new Rectangle(tooltipRect.Right - borderThickness, tooltipRect.Y, borderThickness, tooltipRect.Height), borderColor);

            // ═══════════════════════════════════════════════════════════
            // TOOLTIP TEXT
            // ═══════════════════════════════════════════════════════════

            int textY =
                tooltipRect.Y +
                paddingY;

            bool isFirstLine =
                true;

            for (int i = 0; i < lines.Count; i++)
            {
                var (text, color) =
                    lines[i];

                float lineScale =
                    i == 0
                        ? titleScale
                        : bodyScale;

                Vector2 textSize =
                    font.MeasureString(text) *
                    lineScale;

                int textX =
                    tooltipRect.X +
                    (
                        tooltipRect.Width -
                        (int)textSize.X
                    ) / 2;

                // Shadow
                spriteBatch.DrawString(
                    font,
                    text,
                    new Vector2(
                        textX + 1,
                        textY + 1),
                    Color.Black * 0.7f,
                    0f,
                    Vector2.Zero,
                    lineScale,
                    SpriteEffects.None,
                    0f);

                // Text
                //
                // Generic items continue using the tooltip border color
                // for the title. Fenrir keeps the original blue title
                // independently from its border (the red Fenrir border
                // is white, while its title is still blue).
                Color lineColor =
                    isFirstLine && !isFenrir
                        ? borderColor
                        : color;

                spriteBatch.DrawString(
                    font,
                    text,
                    new Vector2(
                        textX,
                        textY),
                    lineColor,
                    0f,
                    Vector2.Zero,
                    lineScale,
                    SpriteEffects.None,
                    0f);

                textY +=
                    (int)textSize.Y +
                    lineSpacing;

                // Separator after item name
                if (isFirstLine)
                {
                    textY += 2;

                    spriteBatch.Draw(
                        pixel,
                        new Rectangle(
                            tooltipRect.X + 8,
                            textY,
                            tooltipRect.Width - 16,
                            1),
                        borderColor * 0.3f);

                    textY += 4;

                    isFirstLine =
                        false;
                }
            }

        }
    }
}
