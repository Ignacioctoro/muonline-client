using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Common
{
    /// <summary>
    /// Resolves blocking windows before controls update, so lower buttons cannot
    /// react first. Retains the owner through release, including a closing window.
    /// </summary>
    public sealed class PointerInputBarrier
    {
        private GameControl _parent;
        private GameControl _blocker;
        private bool _captured;

        public bool HasBlocker => _blocker != null;

        public void Update(GameControl parent, MouseState current, MouseState previous)
        {
            _parent = parent;
            if (_captured && _blocker?.Parent == parent && (IsPressed(current) || IsPressed(previous)))
                return;

            _blocker = null;
            _captured = false;
            for (int i = parent.Controls.Count - 1; i >= 0; i--)
            {
                var control = parent.Controls[i];
                if (control.Visible && control.BlocksPointerInput &&
                    control.DisplayRectangle.Contains(current.Position))
                {
                    _blocker = control;
                    _captured = IsPressed(current);
                    break;
                }
            }
        }

        public bool Blocks(GameControl control)
        {
            if (_blocker == null || control == _parent) return false;
            // A window's children receive input as part of their top-level window.
            while (control != null && control.Parent != _parent)
                control = control.Parent;
            if (control == null || control == _blocker) return false;
            for (int i = _parent.Controls.Count - 1; i >= 0; i--)
            {
                if (_parent.Controls[i] == control) return false;
                if (_parent.Controls[i] == _blocker) return true;
            }
            return false;
        }

        private static bool IsPressed(MouseState mouse) =>
            mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed ||
            mouse.MiddleButton == ButtonState.Pressed;
    }
}
