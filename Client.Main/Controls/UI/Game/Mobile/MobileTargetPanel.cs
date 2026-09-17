using Client.Main.Controls;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileTargetPanel : SpriteControl
    {
        private readonly LabelControl _targetLabel;

        public MobileTargetPanel()
        {
            AutoViewSize = false;

            ControlSize = new Point(168, 168);
            ViewSize = ControlSize;

            X = UiScaler.VirtualSize.X - 180;
            Y = UiScaler.VirtualSize.Y - 345;

            BackgroundColor = Color.Transparent;

            TexturePath = "Mobile/TargetPanel.png";

            _targetLabel = new LabelControl
            {
                Text = "Sin objetivo",

                X = 50,
                Y = 116,

                FontSize = 12f,
                TextColor = new Color(180, 180, 180)
            };

            Controls.Add(_targetLabel);
        }

        public void SetOpacity(float opacity)
        {
            opacity = MathHelper.Clamp(opacity, 0f, 1f);

            Alpha = opacity;

            // También bajamos la opacidad del texto dinámico.
            _targetLabel.Alpha = opacity;
        }

        public void SetTarget(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ClearTarget();
                return;
            }

            string displayName = name.Trim();

            if (displayName.Length > 17)
            {
                displayName = displayName.Substring(0, 17) + "...";
            }

            _targetLabel.Text = displayName;
            _targetLabel.TextColor = Color.White;
        }

        public void SetTarget(string name, bool isPlayer)
        {
            SetTarget(name);

            if (!string.IsNullOrWhiteSpace(name))
            {
                _targetLabel.TextColor = isPlayer
                    ? new Color(255, 210, 120)
                    : new Color(220, 220, 220);
            }
        }

        public void ClearTarget()
        {
            _targetLabel.Text = "Sin objetivo";
            _targetLabel.TextColor = new Color(180, 180, 180);
        }
    }
}