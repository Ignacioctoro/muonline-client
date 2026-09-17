using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Client.Main.Controllers;

namespace Client.Main.Controls.UI
{
    public class GuildEmblemEditorControl : UIControl
    {
        private const int GridSize = 8;
        private const int CellSize = 18;
        private const int PaletteCellSize = 16;
        private const int PaletteColumns = 8;

        private readonly byte[,] _pixels = new byte[GridSize, GridSize];

        private readonly Color[] _palette = new Color[]
        {
            new Color(0, 0, 0),         // 0
            new Color(255, 255, 255),   // 1
            new Color(255, 0, 0),       // 2
            new Color(0, 255, 0),       // 3
            new Color(0, 0, 255),       // 4
            new Color(255, 255, 0),     // 5
            new Color(255, 128, 0),     // 6
            new Color(128, 0, 255),     // 7
            new Color(0, 255, 255),     // 8
            new Color(255, 0, 255),     // 9
            new Color(128, 128, 128),   // 10
            new Color(192, 192, 192),   // 11
            new Color(128, 0, 0),       // 12
            new Color(0, 128, 0),       // 13
            new Color(0, 0, 128),       // 14
            new Color(180, 120, 60),    // 15
        };

        public int SelectedColorIndex { get; private set; } = 1;

        public GuildEmblemEditorControl()
        {
            Interactive = true;
            AutoViewSize = false;
            ViewSize = new Point(220, 220);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var sb = GraphicsManager.Instance.Sprite;

            DrawGrid(sb);
            DrawPalette(sb);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseOver)
                return;

            var mouse = MuGame.Instance.UiMouseState;
            if (mouse.LeftButton != ButtonState.Pressed)
                return;

            Point local = new Point(
                MuGame.Instance.Mouse.X - DisplayRectangle.X,
                MuGame.Instance.Mouse.Y - DisplayRectangle.Y);

            if (TryHandleGridClick(local))
                return;

            TryHandlePaletteClick(local);
        }

        public void Clear()
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    _pixels[x, y] = 0;
                }
            }
        }

        public byte[] GetEmblemBytes()
        {
            // 8x8 pixels, 4 bits each = 64 nibbles = 32 bytes
            byte[] result = new byte[32];

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    int byteIndex = (y * 4) + (x / 2);
                    byte color = (byte)(_pixels[x, y] & 0x0F);

                    if (x % 2 == 0)
                    {
                        result[byteIndex] |= (byte)(color << 4);
                    }
                    else
                    {
                        result[byteIndex] |= color;
                    }
                }
            }

            return result;
        }

        private bool TryHandleGridClick(Point local)
        {
            int gridX = 0;
            int gridY = 0;
            int gridWidth = GridSize * CellSize;
            int gridHeight = GridSize * CellSize;

            if (local.X < gridX || local.Y < gridY ||
                local.X >= gridX + gridWidth || local.Y >= gridY + gridHeight)
            {
                return false;
            }

            int x = local.X / CellSize;
            int y = local.Y / CellSize;

            if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
            {
                _pixels[x, y] = (byte)SelectedColorIndex;
                return true;
            }

            return false;
        }

        private bool TryHandlePaletteClick(Point local)
        {
            int paletteStartY = (GridSize * CellSize) + 15;

            for (int i = 0; i < _palette.Length; i++)
            {
                int col = i % PaletteColumns;
                int row = i / PaletteColumns;

                Rectangle rect = new Rectangle(
                    col * (PaletteCellSize + 4),
                    paletteStartY + row * (PaletteCellSize + 4),
                    PaletteCellSize,
                    PaletteCellSize);

                if (rect.Contains(local))
                {
                    SelectedColorIndex = i;
                    return true;
                }
            }

            return false;
        }

        private void DrawGrid(SpriteBatch sb)
        {
            int gridWidth = GridSize * CellSize;
            int gridHeight = GridSize * CellSize;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Rectangle rect = new Rectangle(
                        DisplayRectangle.X + x * CellSize,
                        DisplayRectangle.Y + y * CellSize,
                        CellSize - 1,
                        CellSize - 1);

                    DrawFilledRect(sb, rect, _palette[_pixels[x, y]]);
                }
            }

            // borde exterior
            DrawRectOutline(sb, new Rectangle(
                DisplayRectangle.X,
                DisplayRectangle.Y,
                gridWidth,
                gridHeight), Color.White);
        }

        private void DrawPalette(SpriteBatch sb)
        {
            int paletteStartY = DisplayRectangle.Y + (GridSize * CellSize) + 15;

            for (int i = 0; i < _palette.Length; i++)
            {
                int col = i % PaletteColumns;
                int row = i / PaletteColumns;

                Rectangle rect = new Rectangle(
                    DisplayRectangle.X + col * (PaletteCellSize + 4),
                    paletteStartY + row * (PaletteCellSize + 4),
                    PaletteCellSize,
                    PaletteCellSize);

                DrawFilledRect(sb, rect, _palette[i]);

                DrawRectOutline(
                    sb,
                    rect,
                    i == SelectedColorIndex ? Color.Yellow : Color.White);
            }
        }

        private void DrawFilledRect(SpriteBatch sb, Rectangle rect, Color color)
        {
            sb.Draw(
                GraphicsManager.Instance.Pixel,
                rect,
                color);
        }

        private void DrawRectOutline(SpriteBatch sb, Rectangle rect, Color color)
        {
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), color);
            sb.Draw(GraphicsManager.Instance.Pixel, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), color);
        }
    }
}