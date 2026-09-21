using System;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Core.Input;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public sealed class MobileJoystickControl : UIControl
    {
        // Zona total reservada para el joystick.
        // Después podemos usar los alrededores para potas.
        private const int AreaSize = 220;

        private const int BaseDiameter = 160;
        private const int KnobDiameter = 68;

        // Recorrido máximo visual del knob.
        private const float MaxKnobDistance = 58f;

        // Zona muerta para evitar movimientos accidentales.
        private const float DeadZone = 18f;

        // Zona en la que un dedo queda capturado por el joystick.
        private const float CaptureRadius = 86f;

        private Texture2D _baseTexture;
        private Texture2D _knobTexture;

        private Vector2 _knobOffset;
        private bool _touchEnabled = true;

        // Permite probar el joystick con mouse en Windows.
        private bool _mouseCaptured;

        /// <summary>
        /// Vector del joystick.
        ///
        /// X:
        /// -1 izquierda
        /// +1 derecha
        ///
        /// Y:
        /// -1 arriba
        /// +1 abajo
        /// </summary>
        public Vector2 Direction { get; private set; }

        public bool IsActive { get; private set; }
        public bool IsMouseCaptured => _mouseCaptured;

        public bool IsMouseInsideCaptureArea(Point mousePosition)
        {
            Vector2 delta =
                new Vector2(
                    mousePosition.X,
                    mousePosition.Y) -
                GetCenterVirtual();

            return delta.LengthSquared() <=
                CaptureRadius * CaptureRadius;
        }

        public MobileJoystickControl()
        {
            AutoViewSize = false;

            ControlSize = new Point(
                AreaSize,
                AreaSize);

            ViewSize = ControlSize;

            // Posición elegida para tu HUD 1280x720.
            // Queda encima del chat.
            X = 35;
            Y = 305;

            Interactive = false;
            BackgroundColor = Color.Transparent;

            RegisterTouchRegion();
        }

        public override Task Load()
        {
            _baseTexture = CreateCircleTexture(
                BaseDiameter,
                Color.FromNonPremultiplied(
                    20,
                    20,
                    24,
                    145),
                Color.FromNonPremultiplied(
                    150,
                    150,
                    160,
                    190),
                4);

            _knobTexture = CreateCircleTexture(
                KnobDiameter,
                Color.FromNonPremultiplied(
                    75,
                    75,
                    85,
                    210),
                Color.FromNonPremultiplied(
                    190,
                    190,
                    200,
                    230),
                3);

            return Task.CompletedTask;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Mantener actualizada la región táctil.
            RegisterTouchRegion();

            if (!_touchEnabled || !Visible)
            {
                _mouseCaptured = false;
                ResetJoystick();
                return;
            }

            // ─────────────────────────────────────────────
            // 1. TOUCH REAL - Android / iOS
            // ─────────────────────────────────────────────
            if (TouchInputRouter.TryGetJoystickTouch(
                    out TouchLocation touch))
            {
                if (touch.State == TouchLocationState.Released ||
                    touch.State == TouchLocationState.Invalid)
                {
                    ResetJoystick();
                    return;
                }

                var virtualPoint =
                    UiScaler.ToVirtual(
                        new Point(
                            (int)touch.Position.X,
                            (int)touch.Position.Y));

                UpdateJoystickFromPosition(
                    new Vector2(
                        virtualPoint.X,
                        virtualPoint.Y));

                return;
            }

            // ─────────────────────────────────────────────
            // 2. MOUSE - solo para probar en PC
            // ─────────────────────────────────────────────
            if (!OperatingSystem.IsAndroid() &&
                !OperatingSystem.IsIOS())
            {
                var mouse =
                    MuGame.Instance.UiMouseState;

                var previousMouse =
                    MuGame.Instance.PrevUiMouseState;

                Vector2 mousePosition =
                    new Vector2(
                        mouse.X,
                        mouse.Y);

                // El mouse solo captura el joystick si el click
                // COMENZÓ dentro de su círculo.
                if (!_mouseCaptured &&
                    mouse.LeftButton == ButtonState.Pressed &&
                    previousMouse.LeftButton == ButtonState.Released)
                {
                    Vector2 delta =
                        mousePosition -
                        GetCenterVirtual();

                    if (delta.LengthSquared() <=
                        CaptureRadius * CaptureRadius)
                    {
                        _mouseCaptured = true;
                    }
                }

                if (_mouseCaptured)
                {
                    if (mouse.LeftButton == ButtonState.Pressed)
                    {
                        UpdateJoystickFromPosition(
                            mousePosition);

                        return;
                    }

                    // Se soltó el mouse.
                    _mouseCaptured = false;
                    ResetJoystick();
                    return;
                }
            }

            ResetJoystick();
        }
        private void UpdateJoystickFromPosition(
            Vector2 inputPosition)
        {
            IsActive = true;

            Vector2 delta =
                inputPosition -
                GetCenterVirtual();

            float length =
                delta.Length();

            if (length <= 0.001f)
            {
                Direction = Vector2.Zero;
                _knobOffset = Vector2.Zero;
                return;
            }

            Vector2 normalized =
                delta / length;

            // El knob visual nunca sale de la base.
            float visualDistance =
                Math.Min(
                    length,
                    MaxKnobDistance);

            _knobOffset =
                normalized *
                visualDistance;

            // Zona muerta central.
            if (length <= DeadZone)
            {
                Direction = Vector2.Zero;
                return;
            }

            float magnitude =
                MathHelper.Clamp(
                    (length - DeadZone) /
                    (MaxKnobDistance - DeadZone),
                    0f,
                    1f);

            Direction =
                normalized *
                magnitude;
        }

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready ||
                !Visible ||
                _baseTexture == null ||
                _knobTexture == null)
            {
                return;
            }

            var center = GetCenterVirtual();

            var baseRectangle =
                new Rectangle(
                    (int)(center.X -
                          BaseDiameter / 2f),
                    (int)(center.Y -
                          BaseDiameter / 2f),
                    BaseDiameter,
                    BaseDiameter);

            Vector2 knobCenter =
                center + _knobOffset;

            var knobRectangle =
                new Rectangle(
                    (int)(knobCenter.X -
                          KnobDiameter / 2f),
                    (int)(knobCenter.Y -
                          KnobDiameter / 2f),
                    KnobDiameter,
                    KnobDiameter);

            var sprite =
                GraphicsManager.Instance.Sprite;

            if (SpriteBatchScope.BatchIsBegun)
            {
                DrawJoystick(
                    sprite,
                    baseRectangle,
                    knobRectangle);
            }
            else
            {
                using (
                    new SpriteBatchScope(
                        sprite,
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp))
                {
                    DrawJoystick(
                        sprite,
                        baseRectangle,
                        knobRectangle);
                }
            }
        }

        public void SetOpacity(float opacity)
        {
            Alpha =
                MathHelper.Clamp(
                    opacity,
                    0f,
                    1f);
        }

        public void SetTouchEnabled(bool enabled)
        {
            _touchEnabled = enabled;

            if (!enabled)
            {
                ResetJoystick();
            }

            RegisterTouchRegion();
        }

        private void DrawJoystick(
            SpriteBatch sprite,
            Rectangle baseRectangle,
            Rectangle knobRectangle)
        {
            sprite.Draw(
                _baseTexture,
                baseRectangle,
                Color.White * Alpha);

            sprite.Draw(
                _knobTexture,
                knobRectangle,
                Color.White * Alpha);
        }

        private void ResetJoystick()
        {
            Direction = Vector2.Zero;
            _knobOffset = Vector2.Zero;
            IsActive = false;
        }

        private Vector2 GetCenterVirtual()
        {
            var position =
                DisplayPosition;

            return new Vector2(
                position.X +
                ControlSize.X / 2f,
                position.Y +
                ControlSize.Y / 2f);
        }

        private void RegisterTouchRegion()
        {
            TouchInputRouter.ConfigureJoystick(
                GetCenterVirtual(),
                CaptureRadius,
                _touchEnabled && Visible);
        }

        private Texture2D CreateCircleTexture(
            int diameter,
            Color fillColor,
            Color borderColor,
            int borderWidth)
        {
            var texture =
                new Texture2D(
                    GraphicsDevice,
                    diameter,
                    diameter);

            var pixels =
                new Color[
                    diameter *
                    diameter];

            float radius =
                diameter / 2f;

            float radiusSquared =
                radius * radius;

            float innerRadius =
                radius -
                borderWidth;

            float innerRadiusSquared =
                innerRadius *
                innerRadius;

            Vector2 center =
                new Vector2(
                    radius - 0.5f,
                    radius - 0.5f);

            for (int y = 0;
                 y < diameter;
                 y++)
            {
                for (int x = 0;
                     x < diameter;
                     x++)
                {
                    Vector2 delta =
                        new Vector2(
                            x,
                            y) -
                        center;

                    float distanceSquared =
                        delta.LengthSquared();

                    int index =
                        y * diameter +
                        x;

                    if (distanceSquared >
                        radiusSquared)
                    {
                        pixels[index] =
                            Color.Transparent;
                    }
                    else if (distanceSquared >=
                             innerRadiusSquared)
                    {
                        pixels[index] =
                            borderColor;
                    }
                    else
                    {
                        pixels[index] =
                            fillColor;
                    }
                }
            }

            texture.SetData(pixels);

            return texture;
        }

        public override void Dispose()
        {
            TouchInputRouter.ConfigureJoystick(
                GetCenterVirtual(),
                CaptureRadius,
                false);

            _baseTexture?.Dispose();
            _baseTexture = null;

            _knobTexture?.Dispose();
            _knobTexture = null;

            base.Dispose();
        }
    }
}