using System;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>Server-authoritative SD/AG gauges using the native GFx fills.</summary>
    public sealed class ResourceBarControl : TextureControl
    {
        private readonly CharacterState _state;
        private readonly bool _shield;
        private readonly LabelControl _tooltip;
        private uint _shownCurrent = uint.MaxValue;
        private uint _shownMaximum = uint.MaxValue;

        public ResourceBarControl(CharacterState state, bool shield)
        {
            _state = state;
            _shield = shield;
            Name = shield ? "Bar1" : "Bar2";
            AutoViewSize = false;
            Interactive = true;
            TexturePath = shield ? "Interface/GFx/sd_yellow.ozd" : "Interface/GFx/ag_purple.ozd";
            BlendState = BlendState.NonPremultiplied;
            _tooltip = new LabelControl
            {
                FontSize = 11f,
                TextColor = new Color(245, 218, 153),
                BackgroundColor = new Color(14, 10, 9, 230),
                Padding = new Margin { Left = 6, Right = 6, Top = 4, Bottom = 4 },
                ShadowOpacity = 0.7f
            };
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible || Texture == null) return;
            uint current = _shield ? _state.CurrentShield : _state.CurrentAbility;
            uint maximum = _shield ? _state.MaximumShield : _state.MaximumAbility;
            float fraction = maximum == 0 ? 0 : Math.Clamp((float)current / maximum, 0f, 1f);
            var bounds = DisplayRectangle;
            var sprite = GraphicsManager.Instance.Sprite;
            // Cover the colored empty trough baked into main_IE. No new texture or
            // render target is allocated: only the fill width changes each frame.
            sprite.Draw(GraphicsManager.Instance.Pixel, bounds, new Color(19, 13, 17));
            int width = (int)MathF.Round(bounds.Width * fraction);
            if (width <= 0) return;
            var fill = new Rectangle(_shield ? bounds.X : bounds.Right - width,
                bounds.Y, width, bounds.Height);
            sprite.Draw(Texture, fill, SourceRectangle, Color.White);
        }

        public void DrawTooltip(GameTime gameTime)
        {
            if (!Visible || !IsMouseOver || Status != GameControlStatus.Ready) return;
            uint current = _shield ? _state.CurrentShield : _state.CurrentAbility;
            uint maximum = _shield ? _state.MaximumShield : _state.MaximumAbility;
            if (current != _shownCurrent || maximum != _shownMaximum)
            {
                _tooltip.Text = $"{(_shield ? "SD (Shield)" : "AG (Ability)")}: {current:N0} / {maximum:N0}";
                _shownCurrent = current;
                _shownMaximum = maximum;
            }
            var bounds = DisplayRectangle;
            _tooltip.X = Math.Clamp(bounds.Center.X - _tooltip.ControlSize.X / 2,
                0, Math.Max(0, UiScaler.VirtualSize.X - _tooltip.ControlSize.X));
            _tooltip.Y = Math.Max(0, bounds.Y - _tooltip.ControlSize.Y - 6);
            _tooltip.Draw(gameTime);
        }
    }
}
