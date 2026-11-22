#nullable enable
using System;
using Client.Main.Core.Client;
using Client.Main.Controllers;
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
        private readonly TextureControl _masterFill;
        private readonly TextureControl _cover;
        private readonly LabelControl _hoverTooltip;
        private ulong _shownExperience = ulong.MaxValue;
        private ulong _shownNextExperience = ulong.MaxValue;
        private bool _shownMasterMode;

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

            // As in the original main frame, the same gauge switches to the Master EXP texture.
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

            _masterFill = new TextureControl
            {
                Name = "MasterExperienceFill",
                AutoViewSize = false,
                Interactive = false,
                TexturePath = "Interface/Exbar_Master.OZJ",
                TextureRectangle = new Rectangle(0, 0, 6, 4),
                BlendState = BlendState.NonPremultiplied,
                ViewSize = Point.Zero,
                Visible = false
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
            Controls.Add(_masterFill);
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

            bool master = _state.UsesMasterExperience;
            ulong current = master ? _state.MasterExperience : _state.Experience;
            ulong next = master ? _state.MasterExperienceForNextLevel : _state.ExperienceForNextLevel;
            if (current != _shownExperience || next != _shownNextExperience || master != _shownMasterMode)
            {
                _hoverTooltip.Text = $"{(master ? "Master EXP" : "EXP")}: {current:N0} / {next:N0}";
                _shownMasterMode = master;
                _shownExperience = current;
                _shownNextExperience = next;
            }

            var bar = DisplayRectangle;
            // The quick-skill row extends upwards from the EXP bar. Place
            // the hint above those icons instead of inside their rectangles.
            // Keep it in virtual coordinates; UiScaler handles the DPI.
            const int skillRowClearance = 60;
            _hoverTooltip.X = Math.Clamp(
                bar.Center.X - _hoverTooltip.ControlSize.X / 2,
                0,
                Math.Max(0, UiScaler.VirtualSize.X - _hoverTooltip.ControlSize.X));
            _hoverTooltip.Y = Math.Max(
                0, bar.Y - _hoverTooltip.ControlSize.Y - skillRowClearance);
            _hoverTooltip.Draw(gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            // Keep the original layout rectangle untouched. Only the clipped
            // length of the fill changes with server-authoritative experience.
            const int insetX = 4;
            int usableWidth = Math.Max(0, ViewSize.X - insetX * 2);
            int usableHeight = Math.Max(0, ViewSize.Y - 6);
            ExperienceProgressCalculator.GetCharacterDecile(_state, out int digit, out float fraction);
            CurrentDecileDigit = digit;

            _fill.X = insetX;
            _fill.Y = (ViewSize.Y - usableHeight) / 2;
            _fill.ViewSize = new Point(
                (int)Math.Round(usableWidth * fraction),
                usableHeight);
            bool master = _state.UsesMasterExperience;
            _fill.Visible = !master && fraction > 0f && usableHeight > 0;
            _masterFill.X = _fill.X;
            _masterFill.Y = _fill.Y;
            _masterFill.ViewSize = _fill.ViewSize;
            _masterFill.Visible = master && fraction > 0f && usableHeight > 0;

            // The overlay owns the ornamentation and all ten tick marks.
            _cover.ViewSize = ViewSize;
            base.Update(gameTime);
        }
    }
}
