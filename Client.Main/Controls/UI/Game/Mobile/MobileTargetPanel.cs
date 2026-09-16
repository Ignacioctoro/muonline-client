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

            // Tamaño visual más grande.
            ControlSize = new Point(150, 90);
            ViewSize = ControlSize;

            // Más a la izquierda para alinearlo con los botones.
            X = UiScaler.VirtualSize.X - 175;

            // Más cerca del botón Cambiar Objetivo.
            Y = UiScaler.VirtualSize.Y - 395;

            BackgroundColor = Color.Transparent;

            TexturePath = "Mobile/TargetPanel.png";

            _targetLabel = new LabelControl
            {
                Text = "Sin objetivo",

                // Posición ajustada al panel más grande.
                X = 50,
                Y = 43,

                FontSize = 12f,
                TextColor = new Color(180, 180, 180)
            };

            Controls.Add(_targetLabel);
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