using System;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Inventory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Client.Main.Helpers;

namespace Client.Main.Controls.UI.Game.Hud
{
    public class ItemHotkeyHudControl : ExtendedUIControl
    {
        private readonly LabelControl[] _countLabels;

        private static readonly Keys[] Hotkeys =
        {
            Keys.Q,
            Keys.W,
            Keys.E,
            Keys.R
        };

        private const int SlotSpacing = 43;

        // Tamaño del icono dentro de la casilla del HUD.
        private const int IconSize = 32;
        private static readonly int[] SlotOffsetX =
        {
            -7, // Q
            -4, // W
            -2, // E
            0  // R
        };

        public ItemHotkeyHudControl()
        {
            AutoViewSize = false;
            ViewSize = new Point(175, 36);

            Interactive = false;

            _countLabels = new LabelControl[4];

            for (int i = 0; i < Hotkeys.Length; i++)
            {
                int x = i * SlotSpacing;

                _countLabels[i] = new LabelControl
                {
                    Text = "0",
                    X = x + 2 + SlotOffsetX[i],
                    Y = 21,
                    FontSize = 9,
                    IsBold = true,
                    ShadowOpacity = 0.8f
                };

                Controls.Add(_countLabels[i]);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var inventory = InventoryControl.Instance;

            if (inventory == null)
            {
                return;
            }

            for (int i = 0; i < Hotkeys.Length; i++)
            {
                Keys key = Hotkeys[i];

                if (inventory.TryGetItemHotkey(
                    key,
                    out _,
                    out _,
                    out _,
                    out _))
                {
                    _countLabels[i].Text =
                        inventory.GetItemHotkeyTotalCount(key).ToString();
                }
                else
                {
                    _countLabels[i].Text = "0";
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible ||
                Status != Models.GameControlStatus.Ready ||
                GraphicsManager.Instance == null)
            {
                return;
            }

            var inventory = InventoryControl.Instance;
            var spriteBatch = GraphicsManager.Instance.Sprite;

            if (inventory != null)
            {
                // IMPORTANTE:
                // Obtenemos/generamos las previews antes de abrir el SpriteBatch,
                // porque los modelos BMD utilizan RenderTarget internamente.
                Texture2D[] textures = new Texture2D[Hotkeys.Length];

                for (int i = 0; i < Hotkeys.Length; i++)
                {
                    if (!inventory.TryGetItemHotkeyItem(
                        Hotkeys[i],
                        out InventoryItem item))
                    {
                        continue;
                    }

                    string texturePath = item.Definition?.TexturePath;

                    if (string.IsNullOrWhiteSpace(texturePath))
                    {
                        continue;
                    }

                    if (texturePath.EndsWith(
                        ".bmd",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        textures[i] =
                            BmdPreviewRenderer.GetPreview(
                                item,
                                IconSize,
                                IconSize);
                    }
                    else
                    {
                        textures[i] =
                            TextureLoader.Instance.GetTexture2D(texturePath);
                    }
                }

                using (new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    transform: UiScaler.SpriteTransform))
                {
                    Rectangle controlRect = DisplayRectangle;

                    for (int i = 0; i < textures.Length; i++)
                    {
                        Texture2D texture = textures[i];

                        if (texture == null)
                        {
                            continue;
                        }

                        int slotX =
                            controlRect.X +
                            (i * SlotSpacing) +
                            SlotOffsetX[i];

                        int slotY =
                            controlRect.Y;

                        var destination = new Rectangle(
                            slotX,
                            slotY,
                            IconSize,
                            IconSize);

                        spriteBatch.Draw(
                            texture,
                            destination,
                            Color.White);
                    }
                }
            }

            // Dibuja después los contadores para que queden sobre el icono.
            base.Draw(gameTime);
        }
    }
}