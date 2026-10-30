using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Input;
using Microsoft.Xna.Framework.Input;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>Mobile HUD entry point, governed by the existing mobile settings.</summary>
    public sealed class MobileCommandsLauncher : TextureControl, IUiOcclusionAware
    {
        private readonly LabelControl _label;
        private readonly UiOcclusionGate _occlusion = new();
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

        public void CapturePointerFrame(MouseState current, MouseState previous)
        {
            bool covered = false;
            if (Parent != null)
            {
                // Hidden controls do not run their normal layout update.
                AlignControl();
                bool aboveLauncher = false;
                var bounds = DisplayRectangle;
                for (int i = 0; i < Parent.Controls.Count; i++)
                {
                    var control = Parent.Controls[i];
                    if (ReferenceEquals(control, this)) { aboveLauncher = true; continue; }
                    if (aboveLauncher && control.Visible && IsOccludingMenu(control) &&
                        control.DisplayRectangle.Intersects(bounds))
                    {
                        covered = true;
                        break;
                    }
                }
            }
            _occlusion.Update(covered, current.LeftButton == ButtonState.Pressed,
                previous.LeftButton == ButtonState.Pressed);
        }

        private static bool IsOccludingMenu(GameControl control)
        {
            // HUD indicators/launchers are not windows, even if they own pointer input.
            if (control is Party.PartyMatchingLauncher or Hud.PetStatusHudControl)
                return false;
            return control.BlocksPointerInput ||
                control is InventoryControl or Character.CharacterInfoWindowControl or
                    NpcShopControl or MoveCommandWindow or PetTrainerControl or
                    PauseMenu.PauseMenuControl or Skills.SkillSelectionPanel or
                    Map.MapListControl or DevilSquareEnterControl or BloodCastleEnterControl or
                    BloodCastleTimeControl or BloodCastleResultControl or DialogControl;
        }

        public override void Update(GameTime time)
        {
            bool enabled = Constants.SHOW_MOBILE_CONTROLS;
            bool inventoryOpen = InventoryControl.Instance.Visible;
            bool paused = (Scene as GameScene)?.PauseMenu?.Visible == true;
            Visible = enabled && !inventoryOpen && !paused;
            Interactive = !_occlusion.IsSuppressed;
            Alpha = Constants.MOBILE_CONTROLS_OPACITY;
            _label.Alpha = Alpha;
            if ((!enabled || inventoryOpen) && (Scene as GameScene)?.CommandWindow?.Visible == true)
                ((GameScene)Scene).CommandWindow.Close();
            base.Update(time);
        }

        public override bool OnClick()
        {
            if (!Visible || !Constants.SHOW_MOBILE_CONTROLS || _occlusion.IsSuppressed ||
                InventoryControl.Instance.Visible || Scene?.IsPointerInputBlockedFor(this) == true)
                return false;
            return base.OnClick();
        }
    }
}
