using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Compact pet endurance display ported from MuMain's CItemEnduranceInfo.
    /// This is not the Pet Info window: the original uses the equipped item
    /// durability (0..255), a 57x23 frame and a clipped 49x3 health strip.
    /// Coordinates are virtual NewUI HUD coordinates (UiScaler transforms them).
    /// </summary>
    public sealed class PetStatusHudControl : UIControl
    {
        private const string FramePath = "Interface/newui_Pet_Back.tga";
        private const string BarPath = "Interface/newui_Pet_HpBar.jpg";
        private const byte RavenSlot = 1; // EQUIPMENT_WEAPON_LEFT
        private const byte HelperSlot = 8; // EQUIPMENT_HELPER
        // Magnification affects only the NewUI destination geometry.
        // All source rectangles continue using the original MU texture pixels.
        private const float DisplayScale = 1.6f;
        private const int SourceFrameWidth = 57;
        private const int SourceFrameHeight = 23;
        private const int SourceRowPitch = 24;
        private const int SourceBarWidth = 49;
        private const int SourceBarHeight = 3;
        // Keep the original compact font size while enlarging the pet frames.
        private const float NameTextScale = 0.34f;

        private static int Scale(int pixels) => (int)MathF.Round(pixels * DisplayScale);

        // Precomputed virtual screen sizes; avoid recalculating scaling each frame.
        private static readonly int FrameWidth = Scale(SourceFrameWidth);
        private static readonly int FrameHeight = Scale(SourceFrameHeight);
        private static readonly int RowPitch = Scale(SourceRowPitch);
        private static readonly int BarWidth = Scale(SourceBarWidth);
        private static readonly int BarHeight = Math.Max(1, Scale(SourceBarHeight));
        private static readonly int InnerX = Scale(2);
        private static readonly int InnerY = Scale(2);
        private static readonly int InnerWidth = Scale(53);
        private static readonly int InnerHeight = Scale(13);
        private static readonly int BarOffsetX = Scale(4);
        private static readonly int BarOffsetY = Scale(16);

        public override bool BlocksPointerInput => _entryCount > 0;

        private readonly CharacterState _state;
        private readonly PetEntry[] _entries = new PetEntry[2];
        private int _entryCount;
        private Texture2D _frame;
        private Texture2D _bar;

        public PetStatusHudControl(CharacterState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            Name = "PetStatusHud";
            Interactive = true;
            Visible = true;
            ControlSize = new Point(FrameWidth, FrameHeight + RowPitch);
            ViewSize = ControlSize;
        }

        public override async Task Load()
        {
            // TextureLoader resolves .tga/.jpg to Data_Broyal's .OZT/.OZJ.
            _frame = await TextureLoader.Instance.PrepareAndGetTexture(FramePath);
            _bar = await TextureLoader.Instance.PrepareAndGetTexture(BarPath);
            await base.Load();
        }

        public override void Update(GameTime gameTime)
        {
            // Two direct dictionary lookups; no inventory snapshot allocations,
            // no additional A9 requests and no polling intervals tied to FPS.
            _entryCount = 0;
            if (_state.IsInGame)
            {
                // Native draw order: equipped helper first, then Dark Raven.
                if (_state.TryGetInventoryItem(HelperSlot, out var helper) &&
                    ItemDatabase.TryGetItemGroupAndNumber(helper, out var group, out var number) &&
                    group == 13)
                {
                    string name = number switch
                    {
                        0 => "Guardian Angel",
                        1 => "Imp",
                        2 => "Uniria",
                        3 => "Dinorant",
                        4 => "Dark Horse",
                        37 => "Fenrir",
                        64 => "Demon",
                        65 => "Spirit of Guardian",
                        67 => "Rudolf",
                        80 => "Panda",
                        106 => "Unicorn",
                        123 => "Skeleton",
                        _ => ItemDatabase.GetItemName(helper)
                    };

                    if (name != null)
                    {
                        byte health = ItemDatabase.GetItemDurability(helper);
                        if (number == 4 &&
                            _state.TryGetPetInfo(HelperSlot, out var info) &&
                            info.PetType == 1)
                        {
                            health = info.Health;
                        }
                        _entries[_entryCount++] = new PetEntry(name, health, HelperSlot);
                    }
                }

                if (_state.HasEquippedDarkRaven &&
                    _state.TryGetInventoryItem(RavenSlot, out var raven))
                {
                    byte health = ItemDatabase.GetItemDurability(raven);
                    if (_state.TryGetPetInfo(RavenSlot, out var info) &&
                        info.PetType == 0)
                    {
                        health = info.Health;
                    }
                    _entries[_entryCount++] = new PetEntry("Dark Raven", health, RavenSlot);
                }
            }

            ViewSize = new Point(FrameWidth, Math.Max(FrameHeight, FrameHeight + (_entryCount - 1) * RowPitch));
            base.Update(gameTime);
            var mouse = MuGame.Instance.UiMouseState;
            if (_entryCount > 0 && DisplayRectangle.Contains(mouse.Position))
            {
                Scene?.SetMouseInputConsumed();
                if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    MuGame.Instance.PrevUiMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
                {
                    int index = (mouse.Position.Y - DisplayRectangle.Y) / RowPitch;
                    if (index < _entryCount) PetInfoControl.Instance.ShowWindow(_entries[index].Slot);
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible ||
                _entryCount == 0 || _frame == null || _bar == null)
                return;

            var sprite = GraphicsManager.Instance.Sprite;
            var origin = DisplayRectangle.Location;
            using (new SpriteBatchScope(
                sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.PointClamp,
                transform: UiScaler.SpriteTransform))
            {
                for (int i = 0; i < _entryCount; i++)
                {
                    int x = origin.X;
                    int y = origin.Y + i * RowPitch;

                    // Original CItemEnduranceInfo's black inner backing.
                    sprite.Draw(GraphicsManager.Instance.Pixel,
                        new Rectangle(x + InnerX, y + InnerY, InnerWidth, InnerHeight),
                        new Color(0, 0, 0, 179));
                    sprite.Draw(_frame, new Rectangle(x, y, FrameWidth, FrameHeight),
                        new Rectangle(0, 0, SourceFrameWidth, SourceFrameHeight),
                        Color.White);

                    // Crop the original 49x3 source strip, then enlarge its
                    // destination. This preserves the native health percentage.
                    int sourceWidth = Math.Clamp(
                        _entries[i].Health * SourceBarWidth / 255, 0, SourceBarWidth);
                    if (sourceWidth > 0)
                    {
                        int destWidth = Math.Min(BarWidth, Scale(sourceWidth));
                        sprite.Draw(_bar,
                            new Rectangle(x + BarOffsetX, y + BarOffsetY, destWidth, BarHeight),
                            new Rectangle(0, 0, sourceWidth, SourceBarHeight),
                            Color.White);
                    }
                }
            }

            var font = GraphicsManager.Instance.Font;
            if (font == null)
                return;

            using (new SpriteBatchScope(
                sprite, SpriteSortMode.Deferred,
                BlendState.NonPremultiplied, SamplerState.LinearClamp,
                transform: UiScaler.SpriteTransform))
            {
                for (int i = 0; i < _entryCount; i++)
                {
                    string name = _entries[i].Name;
                    float textWidth = font.MeasureString(name).X * NameTextScale;
                    float nameX = origin.X + (FrameWidth - textWidth) * 0.5f;
                    float nameY = origin.Y + i * RowPitch + (5f * DisplayScale);
                    sprite.DrawString(font, name, new Vector2(nameX, nameY),
                        Color.White, 0f, Vector2.Zero, NameTextScale,
                        SpriteEffects.None, 0f);
                }
            }
        }

        private readonly struct PetEntry
        {
            public readonly string Name;
            public readonly byte Health;
            public readonly byte Slot;

            public PetEntry(string name, byte health, byte slot)
            {
                Name = name;
                Health = health;
                Slot = slot;
            }
        }
    }
}
