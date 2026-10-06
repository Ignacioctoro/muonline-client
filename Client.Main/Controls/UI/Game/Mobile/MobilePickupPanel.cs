using System;
using Client.Main.Controls.UI.Common;
using Client.Main.Controllers;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Mobile
{
    /// <summary>
    /// Barra móvil para seleccionar y recoger objetos del suelo.
    ///
    /// Visual:
    ///
    /// [ CursorGet ] Recoger | Nombre del objeto | 1/3 | Cambiar [>]
    ///
    /// La lógica de red NO vive aquí.
    /// GameScene decide qué objetos hay disponibles
    /// y realiza realmente el pickup.
    /// </summary>
    public sealed class MobilePickupPanel : UIControl
    {
        private const int PanelWidth = 340;
        private const int PanelHeight = 42;

        private readonly MobileButtonVisual _pickupIcon;
        private readonly MobileButtonVisual _nextIcon;

        private readonly ButtonControl _pickupButton;
        private readonly ButtonControl _nextButton;

        private readonly LabelControl _pickupLabel;
        private readonly LabelControl _itemLabel;
        private readonly LabelControl _counterLabel;
        private readonly LabelControl _nextLabel;

        private int _candidateCount;
        private float _opacity = 1f;

        public ushort? SelectedRawId { get; private set; }

        public event Action<ushort> PickupRequested;

        public event EventHandler NextItemRequested;

        public MobilePickupPanel()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    PanelWidth,
                    PanelHeight);

            ViewSize = ControlSize;

            Reposition();

            Visible = false;
            Interactive = true;

            // Fondo negro translúcido,
            // inspirado en los paneles clásicos de MU.
            BackgroundColor =
                new Color(
                    0,
                    0,
                    0,
                    175);

            // Borde extremadamente fino.
            BorderThickness = 1;

            BorderColor =
                new Color(
                    125,
                    100,
                    70,
                    175);


            // =====================================================
            // BOTÓN RECOGER
            // =====================================================

            _pickupButton =
                new ButtonControl
                {
                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            82,
                            PanelHeight),

                    ViewSize =
                        new Point(
                            82,
                            PanelHeight),

                    AutoViewSize = false,

                    Text = string.Empty,

                    BackgroundColor =
                        Color.Transparent,

                    HoverBackgroundColor =
                        new Color(
                            255,
                            255,
                            255,
                            18),

                    PressedBackgroundColor =
                        new Color(
                            255,
                            255,
                            255,
                            42),

                    Interactive = true
                };


            // Cursor clásico de MU para recoger objetos.
            //
            // Es exactamente el mismo que ya utiliza
            // CursorControl cuando pasas sobre DroppedItemObject.
            _pickupIcon =
                new MobileButtonVisual
                {
                    TexturePath =
                        "Interface/CursorGet.ozt",

                    TileWidth = 32,
                    TileHeight = 32,

                    X = 7,
                    Y = 9,

                    ControlSize =
                        new Point(
                            24,
                            24),

                    ViewSize =
                        new Point(
                            24,
                            24),

                    AutoViewSize = false,

                    Interactive = false,

                    BackgroundColor =
                        Color.Transparent,

                    PressedTint =
                        new Color(
                            170,
                            170,
                            170)
                };


            _pickupLabel =
                new LabelControl
                {
                    Text = "Recoger",

                    X = 34,
                    Y = 13,

                    FontSize = 9f,

                    TextColor =
                        new Color(
                            230,
                            220,
                            195),

                    HasShadow = true
                };


            // =====================================================
            // NOMBRE DEL OBJETO
            // =====================================================

            _itemLabel =
                new LabelControl
                {
                    Text = string.Empty,

                    X = 91,
                    Y = 13,

                    FontSize = 9f,

                    TextColor =
                        new Color(
                            235,
                            235,
                            235),

                    HasShadow = true
                };


            // =====================================================
            // CONTADOR 1/3
            // =====================================================

            _counterLabel =
                new LabelControl
                {
                    Text = string.Empty,

                    X = 236,
                    Y = 13,

                    FontSize = 8f,

                    TextColor =
                        new Color(
                            175,
                            175,
                            175),

                    HasShadow = true
                };


            // =====================================================
            // BOTÓN CAMBIAR
            // =====================================================

            _nextButton =
                new ButtonControl
                {
                    X = 260,
                    Y = 0,

                    ControlSize =
                        new Point(
                            80,
                            PanelHeight),

                    ViewSize =
                        new Point(
                            80,
                            PanelHeight),

                    AutoViewSize = false,

                    Text = string.Empty,

                    BackgroundColor =
                        Color.Transparent,

                    HoverBackgroundColor =
                        new Color(
                            255,
                            255,
                            255,
                            18),

                    PressedBackgroundColor =
                        new Color(
                            255,
                            255,
                            255,
                            42),

                    Interactive = true
                };


            _nextLabel =
                new LabelControl
                {
                    Text = "Cambiar",

                    X = 267,
                    Y = 13,

                    FontSize = 8f,

                    TextColor =
                        new Color(
                            210,
                            200,
                            180),

                    HasShadow = true
                };


            // Flecha original de MU.
            _nextIcon =
                new MobileButtonVisual
                {
                    TexturePath =
                        "Interface/newui_arrow(R).tga",

                    TileWidth = 6,
                    TileHeight = 9,

                    X = 320,
                    Y = 12,

                    ControlSize =
                        new Point(
                            12,
                            18),

                    ViewSize =
                        new Point(
                            12,
                            18),

                    AutoViewSize = false,

                    Interactive = false,

                    BackgroundColor =
                        Color.Transparent,

                    PressedTint =
                        new Color(
                            170,
                            170,
                            170)
                };


            // El ButtonControl va primero para que
            // el fondo de pulsación quede debajo
            // de los iconos y textos.
            Controls.Add(_pickupButton);
            Controls.Add(_nextButton);

            Controls.Add(_pickupIcon);
            Controls.Add(_pickupLabel);

            Controls.Add(_itemLabel);
            Controls.Add(_counterLabel);

            Controls.Add(_nextLabel);
            Controls.Add(_nextIcon);


            _pickupButton.Click +=
                OnPickupClicked;

            _nextButton.Click +=
                OnNextClicked;
        }


        // =========================================================
        // API PÚBLICA
        // =========================================================

        public void SetSelection(
            ushort rawId,
            string itemName,
            int index,
            int count)
        {
            SelectedRawId = rawId;

            _candidateCount =
                Math.Max(
                    1,
                    count);

            _itemLabel.Text =
                ShortenName(
                    itemName,
                    21);

            _counterLabel.Text =
                _candidateCount > 1
                    ? $"{index + 1}/{_candidateCount}"
                    : string.Empty;

            Visible = true;

            ApplyNextButtonVisualState();
        }


        public void ClearSelection()
        {
            SelectedRawId = null;

            _candidateCount = 0;

            _itemLabel.Text =
                string.Empty;

            _counterLabel.Text =
                string.Empty;

            Visible = false;

            CancelPress();

            ApplyNextButtonVisualState();
        }


        public void SetOpacity(
            float opacity)
        {
            _opacity =
                MathHelper.Clamp(
                    opacity,
                    0f,
                    1f);

            Alpha = _opacity;

            _pickupButton.Alpha =
                _opacity;

            _pickupIcon.Alpha =
                _opacity;

            _pickupLabel.Alpha =
                _opacity;

            _itemLabel.Alpha =
                _opacity;

            _counterLabel.Alpha =
                _opacity;

            _nextButton.Alpha =
                _opacity;

            ApplyNextButtonVisualState();
        }


        public void CancelPress()
        {
            _pickupButton.IsMousePressed =
                false;

            _nextButton.IsMousePressed =
                false;

            _pickupIcon.Pressed =
                false;

            _nextIcon.Pressed =
                false;
        }


        // =========================================================
        // UPDATE
        // =========================================================

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

            bool pickupEnabled =
                !coveredByWindow &&
                SelectedRawId.HasValue;

            bool nextEnabled =
                !coveredByWindow &&
                _candidateCount > 1;


            _pickupButton.Enabled =
                pickupEnabled;

            _pickupButton.Interactive =
                pickupEnabled;


            _nextButton.Enabled =
                nextEnabled;

            _nextButton.Interactive =
                nextEnabled;


            if (coveredByWindow)
            {
                CancelPress();
            }

            base.Update(
                gameTime);


            _pickupIcon.Pressed =
                _pickupButton.IsMousePressed;

            _nextIcon.Pressed =
                _nextButton.IsMousePressed;
        }


        // =========================================================
        // EVENTOS
        // =========================================================

        private void OnPickupClicked(
            object sender,
            EventArgs e)
        {
            if (!SelectedRawId.HasValue)
            {
                return;
            }

            PickupRequested?.Invoke(
                SelectedRawId.Value);
        }


        private void OnNextClicked(
            object sender,
            EventArgs e)
        {
            if (_candidateCount <= 1)
            {
                return;
            }

            NextItemRequested?.Invoke(
                this,
                EventArgs.Empty);
        }


        // =========================================================
        // VISUAL
        // =========================================================

        private void ApplyNextButtonVisualState()
        {
            bool enabled =
                _candidateCount > 1;

            float alpha =
                enabled
                    ? _opacity
                    : _opacity * 0.30f;

            _nextLabel.Alpha =
                alpha;

            _nextIcon.Alpha =
                alpha;

            _counterLabel.Alpha =
                _opacity;
        }


        private static string ShortenName(
            string name,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(
                    name))
            {
                return "Item";
            }

            string trimmed =
                name.Trim();

            if (trimmed.Length <=
                maxLength)
            {
                return trimmed;
            }

            return
                trimmed.Substring(
                    0,
                    maxLength - 3) +
                "...";
        }


        private void Reposition()
        {
            X =
                (UiScaler.VirtualSize.X -
                 PanelWidth) / 2;

            // 720p virtual:
            //
            // Y ≈ 577.
            //
            // Coincide aproximadamente
            // con la zona roja que marcaste.
            Y =
                UiScaler.VirtualSize.Y -
                143;
        }


        protected override void OnScreenSizeChanged()
        {
            base.OnScreenSizeChanged();

            Reposition();
        }
    }
}