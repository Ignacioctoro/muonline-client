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
        private const int FrameWidth = 57;
        private const int FrameHeight = 23;
        private const int RowPitch = 24;
        private const int BarWidth = 49;
        private const float NameTextScale = 0.34f;

        private readonly CharacterState _state;
        private readonly PetEntry[] _entries = new PetEntry[2];
        private int _entryCount;
        private Texture2D _frame;
        private Texture2D _bar;

        public PetStatusHudControl(CharacterState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            Name = "PetStatusHud";
            Interactive = false;
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
                        _ => null
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
                        _entries[_entryCount++] = new PetEntry(name, health);
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
                    _entries[_entryCount++] = new PetEntry("Dark Raven", health);
                }
            }

            base.Update(gameTime);
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
                        new Rectangle(x + 2, y + 2, 53, 13),
                        new Color(0, 0, 0, 179));
                    sprite.Draw(_frame, new Rectangle(x, y, FrameWidth, FrameHeight),
                        new Rectangle(0, 0, FrameWidth, FrameHeight), Color.White);

                    // Source is cropped as well as destination: never stretch 49 texels.
                    int width = Math.Clamp(_entries[i].Health * BarWidth / 255, 0, BarWidth);
                    if (width > 0)
                    {
                        sprite.Draw(_bar, new Rectangle(x + 4, y + 16, width, 3),
                            new Rectangle(0, 0, width, 3), Color.White);
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
                    float nameY = origin.Y + i * RowPitch + 5f;
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

            public PetEntry(string name, byte health)
            {
                Name = name;
                Health = health;
            }
        }
    }
}
