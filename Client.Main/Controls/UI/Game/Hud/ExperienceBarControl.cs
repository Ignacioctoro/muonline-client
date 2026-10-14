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
        private readonly LabelControl _hoverTooltip;
        private ulong _shownExperience = ulong.MaxValue;
        private ulong _shownNextExperience = ulong.MaxValue;

        /// <summary>Original HUD EXP digit, 0-9. Updated with the gauge.</summary>
        public int CurrentDecileDigit { get; private set; }

        public ExperienceBarControl(CharacterState state)
        {
            _state = state;
            AutoViewSize = false;
            Interactive = true; // Enables the original EXP current/next hover tooltip.
            TexturePath = "Interface/GFx/main_IE.ozd";
            TextureRectangle = new Rectangle(218, 90, 741, 14);
            BlendState = BlendState.NonPremultiplied;

            // The GFx normal-EXP resource is orange. Data also has a green
            // guege_exp_1 variant; its original switching rule is unverified.
            // Do not invent a color change per decile or activate Master here.
            // The cover draws the original divisions on top.
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

            // As in the original main frame, show exact cumulative EXP on hover.
            // Draw it in MainControl's existing text pass, not in this texture batch.
            _hoverTooltip = new LabelControl
            {
                FontSize = 11f,
                TextColor = new Color(245, 218, 153),
                BackgroundColor = new Color(14, 10, 9, 210),
                Padding = new Client.Main.Models.Margin
                {
                    Left = 6,
                    Right = 6,
                    Top = 4,
                    Bottom = 4
                },
                ShadowOpacity = 0.7f
            };
        }

        public void DrawTooltip(GameTime gameTime)
        {
            if (!IsMouseOver || !Visible || Status != Client.Main.Models.GameControlStatus.Ready)
                return;

            ulong current = _state.Experience;
            ulong next = _state.ExperienceForNextLevel;
            if (current != _shownExperience || next != _shownNextExperience)
            {
                _hoverTooltip.Text = $"EXP: {current:N0} / {next:N0}";
                _shownExperience = current;
                _shownNextExperience = next;
            }

            var bar = DisplayRectangle;
            _hoverTooltip.X = bar.Center.X - _hoverTooltip.ControlSize.X / 2;
            _hoverTooltip.Y = bar.Y - _hoverTooltip.ControlSize.Y - 14;
            _hoverTooltip.Draw(gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            // Keep the original layout rectangle untouched. Only the clipped
            // length of the fill changes with server-authoritative experience.
            const int insetX = 4;
            int usableWidth = Math.Max(0, ViewSize.X - insetX * 2);
            int usableHeight = Math.Max(0, ViewSize.Y - 6);
            ExperienceProgressCalculator.GetNormalLevelDecile(
                _state.Level, _state.Experience, _state.ExperienceForNextLevel,
                out int digit, out float fraction);
            CurrentDecileDigit = digit;

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
