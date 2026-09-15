using Client.Main.Controls;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public class MobileTargetPanel : UIControl
    {
        private readonly LabelControl _titleLabel;
        private readonly LabelControl _targetLabel;

        public MobileTargetPanel()
        {
            AutoViewSize = false;

            // Panel ancho y relativamente bajo.
            ControlSize = new Point(160, 72);
            ViewSize = ControlSize;

            // Misma columna derecha de los botones.
            X = UiScaler.VirtualSize.X - 180;

            // Arriba del botón TARGET.
            Y = UiScaler.VirtualSize.Y - 500;

            BackgroundColor = new Color(20, 25, 30, 210);

            _titleLabel = new LabelControl
            {
                Text = "OBJETIVO",
                X = 10,
                Y = 8,
                FontSize = 12f,
                TextColor = new Color(220, 200, 120)
            };

            _targetLabel = new LabelControl
            {
                Text = "Sin objetivo",
                X = 10,
                Y = 34,
                FontSize = 14f,
                TextColor = Color.White
            };

            Controls.Add(_titleLabel);
            Controls.Add(_targetLabel);
        }

        public void SetTarget(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ClearTarget();
                return;
            }

            // Evita nombres absurdamente largos rompiendo el HUD.
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