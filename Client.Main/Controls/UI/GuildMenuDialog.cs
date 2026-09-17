using Client.Main.Models;
using Microsoft.Xna.Framework;
using System;

namespace Client.Main.Controls.UI
{
    public class GuildMenuDialog : PopupFieldDialog
    {
        private readonly LabelButton _disbandButton;
        private readonly LabelButtonSmall _closeButton;

        public event EventHandler DisbandRequested;

        public GuildMenuDialog()
        {
            Interactive = true;
            ControlSize = new Point(300, 180);

            Controls.Add(new LabelControl
            {
                Text = "Guild Menu",
                Align = ControlAlign.HorizontalCenter,
                Y = 20,
                FontSize = 14
            });

            Controls.Add(new LabelControl
            {
                Text = "Guild management",
                Align = ControlAlign.HorizontalCenter,
                Y = 55,
                FontSize = 11,
                TextColor = new Color(200, 200, 200)
            });

            _disbandButton = new LabelButton
            {
                X = 60,
                Y = 90,
                Label = new LabelControl
                {
                    Text = "Disband Guild",
                    Align = ControlAlign.HorizontalCenter |
                            ControlAlign.VerticalCenter,
                    FontSize = 11
                }
            };

            _disbandButton.Click += (sender, args) =>
            {
                DisbandRequested?.Invoke(this, EventArgs.Empty);
            };

            Controls.Add(_disbandButton);

            _closeButton = new LabelButtonSmall
            {
                X = 118,
                Y = 135,
                Label = new LabelControl
                {
                    Text = "Close",
                    Align = ControlAlign.HorizontalCenter |
                            ControlAlign.VerticalCenter,
                    FontSize = 11
                }
            };

            _closeButton.Click += (sender, args) =>
            {
                Close();
            };

            Controls.Add(_closeButton);
        }
    }
}