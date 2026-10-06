using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    /// <summary>
    /// Botón táctil compacto para abrir el chat.
    ///
    /// Visualmente muestra solamente el icono clásico de MU.
    /// El sensor táctil es mayor que el icono.
    /// </summary>
    public sealed class MobileChatButton : UIControl
    {
        private const int TouchWidth = 46;
        private const int TouchHeight = 38;

        private const int IconWidth = 27;
        private const int IconHeight = 26;

        private const int IconX = 9;
        private const int IconY = 6;

        private readonly MobileButtonVisual _visual;
        private readonly ButtonControl _button;

        public event EventHandler ChatClicked;

        public MobileChatButton()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    TouchWidth,
                    TouchHeight);

            ViewSize =
                ControlSize;

            // Sin fondo.
            BackgroundColor =
                Color.Transparent;

            // Sin borde.
            BorderThickness = 0;


            // =====================================================
            // SENSOR TÁCTIL
            //
            // Invisible.
            // Es más grande que el icono para facilitar el tap
            // en Android.
            // =====================================================

            _button =
                new ButtonControl
                {
                    Text = string.Empty,

                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            TouchWidth,
                            TouchHeight),

                    ViewSize =
                        new Point(
                            TouchWidth,
                            TouchHeight),

                    AutoViewSize = false,

                    Interactive = true,

                    // IMPORTANTE:
                    // absolutamente transparente en todos
                    // los estados.
                    BackgroundColor =
                        Color.Transparent,

                    HoverBackgroundColor =
                        Color.Transparent,

                    PressedBackgroundColor =
                        Color.Transparent
                };


            // =====================================================
            // ICONO
            // =====================================================

            _visual =
                new MobileButtonVisual
                {
                    TexturePath =
                        "Interface/newui_chat_chat_on.jpg",

                    TileWidth =
                        IconWidth,

                    TileHeight =
                        IconHeight,

                    X =
                        IconX,

                    Y =
                        IconY,

                    ControlSize =
                        new Point(
                            IconWidth,
                            IconHeight),

                    ViewSize =
                        new Point(
                            IconWidth,
                            IconHeight),

                    AutoViewSize = false,

                    Interactive = false,

                    BackgroundColor =
                        Color.Transparent,

                    // Estado normal:
                    NormalTint =
                        Color.White,

                    // No existe una textura "pressed"
                    // separada en el Data clásico.
                    //
                    // La oscurecemos al tocarla para dar
                    // feedback visual.
                    PressedTint =
                        new Color(
                            145,
                            145,
                            145)
                };


            // Primero el sensor invisible.
            // Después el icono encima.
            Controls.Add(
                _button);

            Controls.Add(
                _visual);


            _button.Click +=
                OnChatClicked;
        }


        public void SetOpacity(
            float opacity)
        {
            opacity =
                MathHelper.Clamp(
                    opacity,
                    0f,
                    1f);

            Alpha =
                opacity;

            _visual.Alpha =
                opacity;
        }


        public void CancelPress()
        {
            _button.IsMousePressed =
                false;

            _visual.Pressed =
                false;

            // Recuperar posición normal.
            _visual.X =
                IconX;

            _visual.Y =
                IconY;
        }


        public override void Update(
            GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }


            bool coveredByWindow =
                Scene is GameScene gameScene &&
                gameScene.IsMobileControlCovered(
                    this);


            _button.Interactive =
                !coveredByWindow;


            if (coveredByWindow)
            {
                CancelPress();
            }


            base.Update(
                gameTime);


            bool pressed =
                _button.IsMousePressed;


            _visual.Pressed =
                pressed;


            // Pequeño desplazamiento físico al presionar.
            //
            // Da sensación de botón real sin necesitar
            // otra textura.
            _visual.X =
                IconX +
                (pressed ? 1 : 0);

            _visual.Y =
                IconY +
                (pressed ? 1 : 0);
        }


        private void OnChatClicked(
            object sender,
            EventArgs e)
        {
            ChatClicked?.Invoke(
                this,
                EventArgs.Empty);
        }
    }
}