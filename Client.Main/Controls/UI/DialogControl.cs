using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using System;

namespace Client.Main.Controls.UI
{
    public abstract class DialogControl : UIControl
    {
        public event EventHandler Closed;

        public DialogControl()
        {
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
        }

        public void Close()
        {
            var scene = MuGame.Instance?.ActiveScene;

            if (scene != null)
            {
                // Clear any mouse/focus references which still point
                // to this dialog or one of its child controls.
                if (BelongsToDialog(scene.MouseControl))
                {
                    scene.MouseControl = null;
                }

                if (BelongsToDialog(scene.MouseHoverControl))
                {
                    scene.MouseHoverControl = null;
                }

                if (BelongsToDialog(scene.FocusControl))
                {
                    scene.FocusControl = null;
                }

                // Prevent the click which closed the dialog from
                // falling through to the 3D world in the same frame.
                scene.SetMouseInputConsumed();
            }

            Closed?.Invoke(this, EventArgs.Empty);

            Dispose();
        }

        private bool BelongsToDialog(GameControl control)
        {
            if (control == null)
                return false;

            GameControl current = control;

            while (current != null)
            {
                if (ReferenceEquals(current, this))
                    return true;

                current = current.Parent;
            }

            return false;
        }

        public void ShowDialog()
        {
            MuGame.Instance.ActiveScene.Controls.Add(this);
            AlignControl();
        }
    }
}