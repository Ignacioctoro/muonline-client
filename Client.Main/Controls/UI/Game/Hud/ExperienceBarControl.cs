#nullable enable
using System;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Controls.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// NewUI experience gauge. Its original main_IE atlas slice supplies the
    /// frame; the original 4x8 EXP fill and 1024x16 cover are its only overlays.
    /// All positions are virtual HUD coordinates and use the existing UiScaler.
    /// </summary>
    public sealed class ExperienceBarControl : TextureControl
    {
        private readonly CharacterState _state;
        private readonly TextureControl _fill;
        private readonly TextureControl _cover;

        public ExperienceBarControl(CharacterState state)
        {
            _state = state;
            AutoViewSize = false;
            Interactive = false;
            TexturePath = "Interface/GFx/main_IE.ozd";
            TextureRectangle = new Rectangle(218, 90, 741, 14);
            BlendState = BlendState.NonPremultiplied;

            // Stretch the tiny original EXP gauge across the available track.
            // The cover draws the ten divisions on top, preserving NewUI artwork.
            _fill = new TextureControl
            {
                Name = "ExperienceFill",
                AutoViewSize = false,
                Interactive = false,
                TexturePath = "Interface/GFx/guege_exp.ozd",
                TextureRectangle = new Rectangle(0, 0, 4, 8),
                BlendState = BlendState.NonPremultiplied,
                ViewSize = Point.Zero
            };

            _cover = new TextureControl
            {
                Name = "ExperienceCover",
                AutoViewSize = false,
                Interactive = false,
                TexturePath = "Interface/GFx/guege_exp_cover.ozd",
                TextureRectangle = new Rectangle(0, 0, 1024, 16),
                BlendState = BlendState.NonPremultiplied,
                ViewSize = Point.Zero
            };

            Controls.Add(_fill);
            Controls.Add(_cover);
        }

        public override void Update(GameTime gameTime)
        {
            // Keep the original layout rectangle untouched. Only the clipped
            // length of the fill changes with server-authoritative experience.
            const int insetX = 4;
            int usableWidth = Math.Max(0, ViewSize.X - insetX * 2);
            int usableHeight = Math.Max(0, ViewSize.Y - 6);
            float fraction = ExperienceProgressCalculator.GetNormalLevelFraction(
                _state.Level, _state.Experience, _state.ExperienceForNextLevel);

            _fill.X = insetX;
            _fill.Y = (ViewSize.Y - usableHeight) / 2;
            _fill.ViewSize = new Point(
                (int)Math.Round(usableWidth * fraction),
                usableHeight);
            _fill.Visible = fraction > 0f && usableHeight > 0;

            // The overlay owns the ornamentation and all ten tick marks.
            _cover.ViewSize = ViewSize;
            base.Update(gameTime);
        }
    }
}
