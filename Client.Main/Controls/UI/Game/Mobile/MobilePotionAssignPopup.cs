using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public sealed class MobilePotionAssignPopup : UIControl
    {
        private const int PopupWidth = 250;
        private const int HeaderHeight = 34;
        private const int RowHeight = 36;
        private const int Padding = 8;
        private const int MaxVisibleOptions = 8;

        private readonly InventoryControl _inventory;

        private readonly PopupPanel _panel;
        private readonly LabelControl _titleLabel;
        private readonly ButtonControl _closeButton;

        private readonly List<ButtonControl> _dynamicRows =
            new();

        private readonly List<ItemIconControl> _dynamicIcons =
            new();

        private Keys _hotkey;


        // ═════════════════════════════════════════════
        // PANEL
        // ═════════════════════════════════════════════

        private sealed class PopupPanel : UIControl
        {
            public PopupPanel()
            {
                AutoViewSize = false;
                Interactive = true;
            }
        }


        // ═════════════════════════════════════════════
        // ICONO DE ITEM
        //
        // Dibujamos directamente la Texture2D.
        // No usamos SpriteControl porque estos controles
        // se crean dinámicamente después de que la escena
        // ya está inicializada.
        // ═════════════════════════════════════════════

        private sealed class ItemIconControl : UIControl
        {
            private readonly Texture2D _texture;

            public ItemIconControl(Texture2D texture)
            {
                _texture = texture;

                AutoViewSize = false;
                Interactive = false;
                BackgroundColor = Color.Transparent;
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible ||
                    Status != GameControlStatus.Ready ||
                    _texture == null)
                {
                    return;
                }

                GraphicsManager.Instance.Sprite.Draw(
                    _texture,
                    DisplayRectangle,
                    Color.White * Alpha);
            }
        }


        // ═════════════════════════════════════════════
        // CONSTRUCTOR
        // ═════════════════════════════════════════════

        public MobilePotionAssignPopup(
            InventoryControl inventory)
        {
            _inventory =
                inventory ??
                throw new ArgumentNullException(
                    nameof(inventory));

            AutoViewSize = false;

            ControlSize =
                UiScaler.VirtualSize;

            ViewSize =
                UiScaler.VirtualSize;

            Interactive = true;
            BackgroundColor = Color.Transparent;

            Visible = false;


            // ─────────────────────────────────────────
            // PANEL
            // ─────────────────────────────────────────

            _panel =
                new PopupPanel
                {
                    ControlSize =
                        new Point(
                            PopupWidth,
                            120),

                    ViewSize =
                        new Point(
                            PopupWidth,
                            120),

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            12,
                            12,
                            18,
                            240),

                    BorderColor =
                        Color.FromNonPremultiplied(
                            130,
                            115,
                            85,
                            230),

                    BorderThickness = 1
                };

            Controls.Add(
                _panel);


            // ─────────────────────────────────────────
            // TÍTULO
            // ─────────────────────────────────────────

            _titleLabel =
                new LabelControl
                {
                    Text = "Asignar",

                    X = 10,
                    Y = 9,

                    FontSize = 11f,
                    IsBold = true,

                    TextColor =
                        new Color(
                            235,
                            215,
                            170),

                    Interactive = false
                };

            _panel.Controls.Add(
                _titleLabel);


            // ─────────────────────────────────────────
            // BOTÓN X
            // ─────────────────────────────────────────

            _closeButton =
                new ButtonControl
                {
                    Text = "X",

                    X = PopupWidth - 32,
                    Y = 5,

                    ControlSize =
                        new Point(
                            26,
                            24),

                    ViewSize =
                        new Point(
                            26,
                            24),

                    AutoViewSize = false,

                    FontSize = 10f,

                    TextColor =
                        Color.White,

                    HoverTextColor =
                        Color.Yellow,

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            45,
                            35,
                            35,
                            220),

                    HoverBackgroundColor =
                        Color.FromNonPremultiplied(
                            90,
                            45,
                            45,
                            230),

                    PressedBackgroundColor =
                        Color.FromNonPremultiplied(
                            120,
                            40,
                            40,
                            240)
                };

            _closeButton.Click +=
                (_, _) =>
                    Hide();

            _panel.Controls.Add(
                _closeButton);
        }


        // ═════════════════════════════════════════════
        // MOSTRAR
        // ═════════════════════════════════════════════

        public void ShowFor(
            Keys hotkey,
            Rectangle anchorRectangle)
        {
            _hotkey =
                hotkey;

            _titleLabel.Text =
                $"Asignar {_hotkey}";


            // Refrescamos el inventario directamente
            // desde el estado del personaje.
            //
            // Así NO es necesario abrir previamente
            // la ventana del inventario.
            _inventory.Preload();


            BuildOptions();

            PositionNextTo(
                anchorRectangle);

            Visible = true;

            BringToFront();
        }


        // ═════════════════════════════════════════════
        // OCULTAR
        // ═════════════════════════════════════════════

        public void Hide()
        {
            Visible = false;
        }


        // ═════════════════════════════════════════════
        // IDENTIFICAR POCIONES
        // ═════════════════════════════════════════════

        private static bool IsPotionOption(
            string itemName)
        {
            if (string.IsNullOrWhiteSpace(
                    itemName))
            {
                return false;
            }

            // Queremos objetos realmente apropiados
            // para los accesos Q / W / E / R.
            //
            // Esto incluye:
            // Healing Potion
            // Mana Potion
            // SD Potion
            // Complex Potion, etc.
            //
            // Apple y Antidote también se comportan
            // como consumibles rápidos clásicos.
            return
                itemName.Contains(
                    "Potion",
                    StringComparison.OrdinalIgnoreCase) ||

                itemName.Contains(
                    "Apple",
                    StringComparison.OrdinalIgnoreCase) ||

                itemName.Contains(
                    "Antidote",
                    StringComparison.OrdinalIgnoreCase);
        }


        // ═════════════════════════════════════════════
        // CONSTRUIR LISTA
        // ═════════════════════════════════════════════

        private void BuildOptions()
        {
            ClearDynamicRows();


            // ─────────────────────────────────────────
            // OBTENER SOLO POCIONES
            // ─────────────────────────────────────────

            var options =
                _inventory
                    .GetAssignableItemHotkeyOptions()
                    .Where(option =>
                        option.Group == 14 &&
                        IsPotionOption(
                            option.Name))
                    .ToList();


            // ─────────────────────────────────────────
            // ASIGNACIÓN ACTUAL
            // ─────────────────────────────────────────

            bool hasCurrent =
                _inventory.TryGetItemHotkey(
                    _hotkey,
                    out int currentGroup,
                    out int currentId,
                    out _,
                    out _);


            int y =
                HeaderHeight + 4;


            int shown =
                Math.Min(
                    options.Count,
                    MaxVisibleOptions);


            // ═════════════════════════════════════════
            // FILAS DE POCIONES
            // ═════════════════════════════════════════

            for (int i = 0;
                 i < shown;
                 i++)
            {
                var option =
                    options[i];


                bool isCurrent =
                    hasCurrent &&
                    option.Group ==
                        currentGroup &&
                    option.Id ==
                        currentId;


                string prefix =
                    isCurrent
                        ? "> "
                        : string.Empty;


                // ─────────────────────────────────────
                // FILA
                // ─────────────────────────────────────

                var row =
                    new ButtonControl
                    {
                        Text =
                            $"{prefix}{option.Name}   x{option.TotalCount}",

                        X = Padding,
                        Y = y,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        AutoViewSize = false,

                        FontSize = 9.5f,

                        TextColor =
                            isCurrent
                                ? new Color(
                                    255,
                                    220,
                                    140)
                                : Color.White,

                        HoverTextColor =
                            new Color(
                                255,
                                230,
                                150),

                        BackgroundColor =
                            isCurrent
                                ? Color.FromNonPremultiplied(
                                    65,
                                    52,
                                    28,
                                    225)
                                : Color.FromNonPremultiplied(
                                    30,
                                    30,
                                    38,
                                    220),

                        HoverBackgroundColor =
                            Color.FromNonPremultiplied(
                                60,
                                55,
                                45,
                                235),

                        PressedBackgroundColor =
                            Color.FromNonPremultiplied(
                                80,
                                65,
                                40,
                                240)
                    };


                var selectedOption =
                    option;


                row.Click +=
                    (_, _) =>
                    {
                        if (_inventory
                            .TryAssignItemHotkey(
                                _hotkey,
                                selectedOption,
                                out _))
                        {
                            Hide();
                        }
                    };


                // Primero agregamos la fila.
                // El icono se agrega después,
                // para quedar dibujado encima.
                _panel.Controls.Add(
                    row);

                _dynamicRows.Add(
                    row);


                // ─────────────────────────────────────
                // ICONO
                // ─────────────────────────────────────

                Texture2D iconTexture =
                    GetItemTexture(
                        option.RepresentativeItem);


                if (iconTexture != null)
                {
                    var icon =
                        new ItemIconControl(
                            iconTexture)
                        {
                            X = Padding + 6,
                            Y = y + 5,

                            ControlSize =
                                new Point(
                                    24,
                                    24),

                            ViewSize =
                                new Point(
                                    24,
                                    24)
                        };


                    _panel.Controls.Add(
                        icon);

                    _dynamicIcons.Add(
                        icon);
                }


                y +=
                    RowHeight;
            }


            // ═════════════════════════════════════════
            // SIN POCIONES
            // ═════════════════════════════════════════

            if (shown == 0)
            {
                var emptyRow =
                    new ButtonControl
                    {
                        Text =
                            "No hay pociones disponibles",

                        X = Padding,
                        Y = y,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        AutoViewSize = false,

                        FontSize = 9f,

                        Enabled = false,

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                25,
                                25,
                                30,
                                210)
                    };


                _panel.Controls.Add(
                    emptyRow);

                _dynamicRows.Add(
                    emptyRow);

                y +=
                    RowHeight;
            }


            // ═════════════════════════════════════════
            // MÁS DE 8 POCIONES
            // ═════════════════════════════════════════

            if (options.Count >
                MaxVisibleOptions)
            {
                int remaining =
                    options.Count -
                    MaxVisibleOptions;


                var moreRow =
                    new ButtonControl
                    {
                        Text =
                            $"+{remaining} pociones más",

                        X = Padding,
                        Y = y,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        AutoViewSize = false,

                        FontSize = 8.5f,

                        Enabled = false,

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                22,
                                22,
                                28,
                                210)
                    };


                _panel.Controls.Add(
                    moreRow);

                _dynamicRows.Add(
                    moreRow);

                y +=
                    RowHeight;
            }


            // ═════════════════════════════════════════
            // QUITAR ASIGNACIÓN
            // ═════════════════════════════════════════

            if (hasCurrent)
            {
                var clearButton =
                    new ButtonControl
                    {
                        Text =
                            $"Quitar asignación {_hotkey}",

                        X = Padding,
                        Y = y + 3,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                Padding * 2,

                                RowHeight - 2),

                        AutoViewSize = false,

                        FontSize = 9f,

                        TextColor =
                            new Color(
                                235,
                                170,
                                170),

                        HoverTextColor =
                            new Color(
                                255,
                                200,
                                200),

                        BackgroundColor =
                            Color.FromNonPremultiplied(
                                55,
                                28,
                                28,
                                220),

                        HoverBackgroundColor =
                            Color.FromNonPremultiplied(
                                90,
                                35,
                                35,
                                235),

                        PressedBackgroundColor =
                            Color.FromNonPremultiplied(
                                120,
                                35,
                                35,
                                240)
                    };


                clearButton.Click +=
                    (_, _) =>
                    {
                        _inventory
                            .ClearItemHotkey(
                                _hotkey);

                        Hide();
                    };


                _panel.Controls.Add(
                    clearButton);

                _dynamicRows.Add(
                    clearButton);


                y +=
                    RowHeight + 3;
            }


            // ═════════════════════════════════════════
            // ALTURA FINAL DEL PANEL
            // ═════════════════════════════════════════

            int panelHeight =
                y + Padding;


            _panel.ControlSize =
                new Point(
                    PopupWidth,
                    panelHeight);


            _panel.ViewSize =
                _panel.ControlSize;
        }


        // ═════════════════════════════════════════════
        // POSICIÓN DEL POPUP
        // ═════════════════════════════════════════════

        private void PositionNextTo(
            Rectangle anchor)
        {
            const int Gap = 8;
            const int ScreenMargin = 8;


            int rightX =
                anchor.Right +
                Gap;


            int leftX =
                anchor.Left -
                PopupWidth -
                Gap;


            // Si cabe a la derecha,
            // abrir hacia la derecha.
            if (rightX +
                PopupWidth <=
                UiScaler.VirtualSize.X -
                ScreenMargin)
            {
                _panel.X =
                    rightX;
            }
            else
            {
                // Si no cabe,
                // abrir hacia la izquierda.
                _panel.X =
                    Math.Max(
                        ScreenMargin,
                        leftX);
            }


            int anchorCenterY =
                anchor.Y +
                anchor.Height / 2;


            int desiredY =
                anchorCenterY -
                _panel.ControlSize.Y / 2;


            int maxY =
                UiScaler.VirtualSize.Y -
                _panel.ControlSize.Y -
                ScreenMargin;


            _panel.Y =
                Math.Max(
                    ScreenMargin,
                    Math.Min(
                        desiredY,
                        Math.Max(
                            ScreenMargin,
                            maxY)));
        }


        // ═════════════════════════════════════════════
        // UPDATE
        // ═════════════════════════════════════════════

        public override void Update(
            GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }


            base.Update(
                gameTime);


            var mouse =
                MuGame.Instance
                    .UiMouseState;


            var previous =
                MuGame.Instance
                    .PrevUiMouseState;


            bool freshPress =
                mouse.LeftButton ==
                    ButtonState.Pressed &&
                previous.LeftButton ==
                    ButtonState.Released;


            // Tocar fuera del panel
            // cierra el menú.
            if (freshPress &&
                !_panel.DisplayRectangle
                    .Contains(
                        mouse.Position))
            {
                Hide();
            }
        }


        // ═════════════════════════════════════════════
        // OBTENER ICONO
        // ═════════════════════════════════════════════

        private Texture2D GetItemTexture(
            InventoryItem item)
        {
            if (item?.Definition == null)
            {
                return null;
            }


            string texturePath =
                item.Definition
                    .TexturePath;


            if (string.IsNullOrWhiteSpace(
                    texturePath))
            {
                return null;
            }


            try
            {
                // Los items 3D de MU normalmente
                // utilizan un modelo BMD.
                if (texturePath.EndsWith(
                        ".bmd",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return BmdPreviewRenderer
                        .GetPreview(
                            item,
                            32,
                            32);
                }


                // Item con textura 2D normal.
                return TextureLoader
                    .Instance
                    .GetTexture2D(
                        texturePath);
            }
            catch
            {
                // Si algún item particular falla,
                // no rompemos todo el popup.
                return null;
            }
        }


        // ═════════════════════════════════════════════
        // LIMPIEZA
        // ═════════════════════════════════════════════

        private void ClearDynamicRows()
        {
            // ─────────────────────────────────────────
            // FILAS
            // ─────────────────────────────────────────

            for (int i =
                     _dynamicRows.Count - 1;
                 i >= 0;
                 i--)
            {
                _dynamicRows[i]
                    .Dispose();
            }


            _dynamicRows.Clear();


            // ─────────────────────────────────────────
            // ICONOS
            // ─────────────────────────────────────────

            for (int i =
                     _dynamicIcons.Count - 1;
                 i >= 0;
                 i--)
            {
                _dynamicIcons[i]
                    .Dispose();
            }


            _dynamicIcons.Clear();
        }
    }
}