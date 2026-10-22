using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>Mobile HUD entry point, governed by the existing mobile settings.</summary>
    public sealed class MobileCommandsLauncher : TextureControl
    {
        private readonly LabelControl _label;
        public override bool BlocksPointerInput => true;
        public MobileCommandsLauncher()
        {
            TexturePath = "Interface/GFx/btn_l01.ozd";
            AutoViewSize = false;
            ViewSize = ControlSize = new Point(110, 40);
            Align = ControlAlign.Top | ControlAlign.Right;
            Margin = new Margin { Top = 12, Right = 10 };
            Interactive = true;
            _label = new LabelControl
            {
                Text = "Comandos", X = 20, Y = 12, FontSize = 12,
                TextColor = Color.Gold, Interactive = false
            };
            Controls.Add(_label);
            Click += (_, _) =>
            {
                Scene?.SetMouseInputConsumed();
                (Scene as GameScene)?.CommandWindow?.Toggle();
            };
        }

        public override void Update(GameTime time)
        {
            bool enabled = Constants.SHOW_MOBILE_CONTROLS;
            bool inventoryOpen = InventoryControl.Instance.Visible;
            bool paused = (Scene as GameScene)?.PauseMenu?.Visible == true;
            Visible = enabled && !inventoryOpen && !paused;
            Alpha = Constants.MOBILE_CONTROLS_OPACITY;
            _label.Alpha = Alpha;
            if ((!enabled || inventoryOpen) && (Scene as GameScene)?.CommandWindow?.Visible == true)
                ((GameScene)Scene).CommandWindow.Close();
            base.Update(time);
        }

        public override bool OnClick()
        {
            if (!Visible || !Constants.SHOW_MOBILE_CONTROLS ||
                InventoryControl.Instance.Visible || Scene?.IsPointerInputBlockedFor(this) == true)
                return false;
            return base.OnClick();
        }
    }
}
