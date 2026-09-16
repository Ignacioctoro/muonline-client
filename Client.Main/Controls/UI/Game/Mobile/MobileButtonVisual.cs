using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Mobile
{
    /// <summary>
    /// Sprite utilizado por los botones móviles.
    /// Permite oscurecer la textura cuando el botón está presionado
    /// sin afectar la transparencia original del PNG.
    /// </summary>
    public sealed class MobileButtonVisual : SpriteControl
    {
        public bool Pressed { get; set; }

        public Color NormalTint { get; set; } = Color.White;

        // Cuanto más bajo, más oscuro.
        // 170 = oscurecimiento suave.
        public Color PressedTint { get; set; } = new Color(170, 170, 170);

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready ||
                !Visible ||
                Texture == null)
            {
                return;
            }

            Color tint = Pressed
                ? PressedTint
                : NormalTint;

            var sprite = GraphicsManager.Instance.Sprite;
            var blend = BlendState ?? BlendState.NonPremultiplied;

            if (SpriteBatchScope.BatchIsBegun)
            {
                sprite.Draw(
                    Texture,
                    DisplayRectangle,
                    SourceRectangle,
                    tint * Alpha);
            }
            else
            {
                using (new SpriteBatchScope(
                    sprite,
                    SpriteSortMode.Deferred,
                    blend,
                    SamplerState.PointClamp))
                {
                    sprite.Draw(
                        Texture,
                        DisplayRectangle,
                        SourceRectangle,
                        tint * Alpha);
                }
            }
        }
    }
}