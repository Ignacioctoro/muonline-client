using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MUnique.OpenMU.Network.Packets;

namespace Client.Main.Controls.UI.Game
{
    public sealed class BloodCastleEnterControl : UIControl, IUiTexturePreloadable
    {
        private const int Width = 190;
        private const int Height = 429;
        private const int GateCount = 8;
        private const int MasterGate = GateCount - 1;
        // The original sends 0xFF so the server finds and validates the ticket.
        private const byte AutomaticTicketSlot = 0xFF;

        private static readonly Rectangle ExitButton = new(13, 392, 36, 29);
        private static readonly Rectangle CloseCorner = new(166, 5, 20, 22);
        private static readonly (int Min, int Max)[] NormalRanges =
        {
            (15, 80), (81, 130), (131, 180), (181, 230), (231, 280), (281, 330), (331, 400),
        };
        private static readonly (int Min, int Max)[] SpecialRanges =
        {
            (10, 60), (61, 110), (111, 160), (161, 210), (211, 260), (261, 310), (311, 400),
        };
        private static readonly string[] TexturePaths =
        {
            "Interface/newui_msgbox_back.OZJ",
            "Interface/newui_item_back01.OZT",
            "Interface/newui_item_back02-L.OZT",
            "Interface/newui_item_back02-R.OZT",
            "Interface/newui_item_back03.OZT",
            "Interface/newui_exit_00.OZT",
            "Interface/newui_btn_empty_big.OZT",
        };

        public static BloodCastleEnterControl Instance { get; } = new();
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private Texture2D[] _textures = new Texture2D[TexturePaths.Length];
        private (int Min, int Max)[] _ranges = NormalRanges;
        private int _activeGate;
        private Rectangle _pressedButton;

        private BloodCastleEnterControl()
        {
            ControlSize = ViewSize = new Point(Width, Height);
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
        }

        public IEnumerable<string> GetPreloadTexturePaths() => TexturePaths;

        public override async Task Load()
        {
            await base.Load();
            var tasks = Array.ConvertAll(TexturePaths, path => TextureLoader.Instance.PrepareAndGetTexture(path));
            _textures = await Task.WhenAll(tasks);
        }

        internal byte GetEventGateLevel()
        {
            RefreshGate();
            return (byte)(_activeGate + 1);
        }

        public void ShowWindow()
        {
            RefreshGate();
            _pressedButton = Rectangle.Empty;
            Visible = true;
            PositionWindow();
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            SendCloseNpcRequest();
        }

        public void CloseWindow()
        {
            if (!Visible) return;
            HideWindow();
            SendCloseNpcRequest();
        }

        private void HideWindow()
        {
            Visible = false;
            _pressedButton = Rectangle.Empty;
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            PositionWindow();
            base.Update(gameTime);

            if (Scene?.IsPointerInputBlockedFor(this) == true)
            {
                _pressedButton = Rectangle.Empty;
                return;
            }

            var game = MuGame.Instance;
            if (HasFocus && game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                CloseWindow();
                return;
            }

            var mouse = game.UiMouseState;
            var previous = game.PrevUiMouseState;
            var point = ToLocal(mouse.Position);
            var gateButton = GateButton(_activeGate);
            if (mouse.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released &&
                DisplayRectangle.Contains(mouse.Position))
            {
                Scene?.SetMouseInputConsumed();
                _pressedButton = CloseCorner.Contains(point) ? CloseCorner :
                    ExitButton.Contains(point) ? ExitButton :
                    gateButton.Contains(point) ? gateButton : Rectangle.Empty;
            }

            if (mouse.LeftButton == ButtonState.Released && previous.LeftButton == ButtonState.Pressed &&
                _pressedButton != Rectangle.Empty)
            {
                var pressed = _pressedButton;
                _pressedButton = Rectangle.Empty;
                Scene?.SetMouseInputConsumed();
                if (!pressed.Contains(point)) return;
                if (pressed == gateButton)
                    EnterCastle();
                else
                    CloseWindow();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var graphics = GraphicsManager.Instance;
            var batch = graphics.Sprite;
            var font = graphics.Font;
            if (graphics.Pixel == null || font == null) return;

            var transform = Matrix.CreateScale(Scale, Scale, 1f) *
                Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0f) * UiScaler.SpriteTransform;
            var mouse = ToLocal(MuGame.Instance.UiMouseState.Position);
            using var scope = new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                GraphicsManager.GetQualityLinearSamplerState(), transform: transform);

            var panel = new Rectangle(0, 0, Width, Height);
            batch.Draw(graphics.Pixel, panel, new Color(27, 20, 16));
            if (_textures[0] != null) batch.Draw(_textures[0], panel, Color.White);
            DrawSlice(batch, 1, new Rectangle(0, 0, Width, 64), new Rectangle(0, 0, Width, 64));
            DrawSlice(batch, 2, new Rectangle(0, 64, 21, 320), new Rectangle(0, 0, 21, 320));
            DrawSlice(batch, 3, new Rectangle(169, 64, 21, 320), new Rectangle(0, 0, 21, 320));
            DrawSlice(batch, 4, new Rectangle(0, 384, Width, 45), new Rectangle(0, 0, Width, 45));

            DrawText(batch, font, "Messenger of Archangel", new Rectangle(18, 9, 154, 17), Color.White, 10f);
            DrawText(batch, font, "Will you help the Archangel\nretrieve his weapon?", new Rectangle(8, 49, 174, 47), Color.White, 10f);
            for (int i = 0; i < GateCount; i++)
            {
                var button = GateButton(i);
                bool active = i == _activeGate;
                int frame = active ? (_pressedButton == button && button.Contains(mouse) ? 2 : button.Contains(mouse) ? 1 : 0) : 2;
                DrawSlice(batch, 6, button, new Rectangle(0, 29 * frame, 180, 29));
                string label = i == MasterGate ? "Castle 8 (Master Level)" :
                    $"Castle {i + 1} (Level {_ranges[i].Min} - {_ranges[i].Max})";
                DrawText(batch, font, label, button, active ? Color.White : new Color(150, 150, 150), 10f);
            }

            DrawSlice(batch, 5, ExitButton, new Rectangle(0, ExitButton.Contains(mouse) ? 29 : 0, 36, 29));
        }

        private void RefreshGate()
        {
            var state = MuGame.Network?.GetCharacterState();
            _ranges = (state?.Class) is CharacterClassNumber.MagicGladiator or CharacterClassNumber.DuelMaster
                or CharacterClassNumber.DarkLord or CharacterClassNumber.LordEmperor
                or CharacterClassNumber.RageFighter or CharacterClassNumber.FistMaster ? SpecialRanges : NormalRanges;
            _activeGate = 0;
            if (state == null) return;

            bool isMasterClass = state.Class is CharacterClassNumber.GrandMaster or CharacterClassNumber.BladeMaster
                or CharacterClassNumber.HighElf or CharacterClassNumber.DuelMaster
                or CharacterClassNumber.LordEmperor or CharacterClassNumber.DimensionMaster or CharacterClassNumber.FistMaster;
            if (isMasterClass && (_ranges == SpecialRanges || state.Level >= NormalRanges[^1].Min))
            {
                _activeGate = MasterGate;
                return;
            }

            for (int i = 0; i < _ranges.Length; i++)
            {
                if (state.Level < _ranges[i].Min || state.Level > _ranges[i].Max) continue;
                _activeGate = i;
                break;
            }
        }

        private void EnterCastle()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) return;
            _ = service.SendBloodCastleEnterRequestAsync((byte)(_activeGate + 1), AutomaticTicketSlot);
            CloseWindow();
        }

        private static void SendCloseNpcRequest()
        {
            var service = MuGame.Network?.GetCharacterService();
            if (service != null) _ = service.SendCloseNpcRequestAsync();
        }

        private void PositionWindow()
        {
            var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
            Scale = MathHelper.Clamp(Math.Min((size.X - 12f) / Width, (size.Y - 12f) / Height), 0.1f, 1.45f);
            X = size.X - DisplaySize.X - 6;
            Y = 6;
        }

        private static Rectangle GateButton(int index) => new(6, 125 + index * 33, 180, 29);
        private Point ToLocal(Point point) => new((int)Math.Floor((point.X - DisplayPosition.X) / Scale),
            (int)Math.Floor((point.Y - DisplayPosition.Y) / Scale));

        private void DrawSlice(SpriteBatch batch, int index, Rectangle bounds, Rectangle source)
        {
            if (_textures[index] != null) batch.Draw(_textures[index], bounds, source, Color.White);
        }

        private static void DrawText(SpriteBatch batch, SpriteFont font, string text, Rectangle bounds, Color color, float size)
        {
            float scale = size / Constants.BASE_FONT_SIZE;
            var center = new Vector2(bounds.Center.X, bounds.Center.Y);
            var position = center - font.MeasureString(text) * scale / 2f;
            batch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
