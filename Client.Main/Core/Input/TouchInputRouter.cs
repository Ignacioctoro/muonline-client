using Client.Main.Controllers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace Client.Main.Core.Input
{
    /// <summary>
    /// Separa el dedo utilizado por el joystick del dedo utilizado
    /// como puntero/UI.
    ///
    /// Esto permite:
    /// dedo izquierdo -> joystick
    /// dedo derecho   -> botones / UI / mundo
    /// </summary>
    public static class TouchInputRouter
    {
        private static int? _joystickTouchId;
        private static int? _pointerTouchId;

        private static bool _joystickEnabled;
        private static Vector2 _joystickCenterVirtual;
        private static float _joystickCaptureRadiusVirtual;

        private static TouchLocation _joystickTouch;
        private static TouchLocation _pointerTouch;

        private static bool _hasJoystickTouch;
        private static bool _hasPointerTouch;

        public static bool HasJoystickTouch => _hasJoystickTouch;
        public static bool HasPointerTouch => _hasPointerTouch;

        public static bool IsJoystickActive =>
            _hasJoystickTouch &&
            IsTouchDown(_joystickTouch.State);

        public static void ConfigureJoystick(
            Vector2 centerVirtual,
            float captureRadiusVirtual,
            bool enabled)
        {
            _joystickCenterVirtual = centerVirtual;
            _joystickCaptureRadiusVirtual = captureRadiusVirtual;
            _joystickEnabled = enabled;

            if (!enabled)
            {
                _joystickTouchId = null;
                _hasJoystickTouch = false;
            }
        }

        public static void Update(TouchCollection touches)
        {
            _hasJoystickTouch = false;
            _hasPointerTouch = false;

            UpdateJoystickTouch(touches);
            UpdatePointerTouch(touches);
        }

        public static bool TryGetJoystickTouch(out TouchLocation touch)
        {
            touch = _joystickTouch;
            return _hasJoystickTouch;
        }

        public static bool TryGetPointerTouch(out TouchLocation touch)
        {
            touch = _pointerTouch;
            return _hasPointerTouch;
        }

        private static void UpdateJoystickTouch(TouchCollection touches)
        {
            if (!_joystickEnabled)
            {
                _joystickTouchId = null;
                return;
            }

            // Si ya tenemos un dedo capturado por el joystick,
            // mantener exactamente ese mismo ID.
            if (_joystickTouchId.HasValue)
            {
                if (TryFindTouch(
                        touches,
                        _joystickTouchId.Value,
                        out var capturedTouch))
                {
                    if (IsTouchDown(capturedTouch.State))
                    {
                        _joystickTouch = capturedTouch;
                        _hasJoystickTouch = true;
                        return;
                    }

                    // Released / Invalid
                    _joystickTouchId = null;
                }
                else
                {
                    _joystickTouchId = null;
                }
            }

            // Buscar un nuevo dedo que haya comenzado dentro
            // de la zona circular del joystick.
            foreach (var touch in touches)
            {
                if (!IsTouchDown(touch.State))
                    continue;

                // No robar un dedo que ya pertenece al puntero/UI.
                if (_pointerTouchId.HasValue &&
                    touch.Id == _pointerTouchId.Value)
                {
                    continue;
                }

                if (!IsInsideJoystick(touch.Position))
                    continue;

                _joystickTouchId = touch.Id;
                _joystickTouch = touch;
                _hasJoystickTouch = true;
                return;
            }
        }

        private static void UpdatePointerTouch(TouchCollection touches)
        {
            // Mantener el mismo dedo como puntero mientras siga existiendo.
            if (_pointerTouchId.HasValue)
            {
                if (TryFindTouch(
                        touches,
                        _pointerTouchId.Value,
                        out var capturedTouch))
                {
                    // Nunca utilizar el dedo del joystick como puntero.
                    if (!_joystickTouchId.HasValue ||
                        capturedTouch.Id != _joystickTouchId.Value)
                    {
                        _pointerTouch = capturedTouch;
                        _hasPointerTouch = true;

                        // Dejamos pasar Released durante este frame
                        // para que ButtonControl reciba correctamente
                        // el mouse-up/click.
                        if (capturedTouch.State ==
                                TouchLocationState.Released ||
                            capturedTouch.State ==
                                TouchLocationState.Invalid)
                        {
                            _pointerTouchId = null;
                        }

                        return;
                    }
                }

                _pointerTouchId = null;
            }

            // Buscar un dedo libre para UI / mundo.
            foreach (var touch in touches)
            {
                if (!IsTouchDown(touch.State))
                    continue;

                if (_joystickTouchId.HasValue &&
                    touch.Id == _joystickTouchId.Value)
                {
                    continue;
                }

                // Un segundo dedo dentro de la zona del joystick
                // tampoco debe provocar clicks en el mapa.
                if (_joystickEnabled &&
                    IsInsideJoystick(touch.Position))
                {
                    continue;
                }

                _pointerTouchId = touch.Id;
                _pointerTouch = touch;
                _hasPointerTouch = true;
                return;
            }
        }

        private static bool TryFindTouch(
            TouchCollection touches,
            int id,
            out TouchLocation result)
        {
            foreach (var touch in touches)
            {
                if (touch.Id == id)
                {
                    result = touch;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static bool IsInsideJoystick(Vector2 physicalPosition)
        {
            var virtualPoint = UiScaler.ToVirtual(
                new Point(
                    (int)physicalPosition.X,
                    (int)physicalPosition.Y));

            var virtualPosition =
                new Vector2(
                    virtualPoint.X,
                    virtualPoint.Y);

            return Vector2.DistanceSquared(
                       virtualPosition,
                       _joystickCenterVirtual)
                   <=
                   _joystickCaptureRadiusVirtual *
                   _joystickCaptureRadiusVirtual;
        }

        private static bool IsTouchDown(
            TouchLocationState state)
        {
            return state == TouchLocationState.Pressed ||
                   state == TouchLocationState.Moved;
        }
        public static void Reset()
        {
            _joystickTouchId = null;
            _pointerTouchId = null;

            _joystickEnabled = false;

            _joystickCenterVirtual = Vector2.Zero;
            _joystickCaptureRadiusVirtual = 0f;

            _joystickTouch = default;
            _pointerTouch = default;

            _hasJoystickTouch = false;
            _hasPointerTouch = false;
        }
    }
}