using System;
using System.Threading.Tasks;
using Client.Main.Scenes;
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
    public sealed class MobilePotionButton : UIControl
    {
        private const double LongPressMilliseconds = 500.0;

        // El dibujo es pequeño, pero dejamos una zona táctil
        // ligeramente más grande para que sea cómodo en móvil.
        private readonly int _visualSize;
        private readonly int _touchSize;

        private readonly Keys _hotkey;

        private readonly ButtonControl _hitButton;
        private readonly LabelControl _hotkeyLabel;
        private readonly LabelControl _countLabel;
        private readonly LabelControl _plusLabel;

        private Texture2D _frameTexture;
        private Texture2D _cachedIconTexture;

        private InventoryItem _assignedItem;

        private bool _hasAssignment;
        private int _count;

        private bool _pressTracking;
        private bool _coveredByWindow;
        private bool _longPressTriggered;
        private TimeSpan _pressStartTime;

        public event Action<Keys> PotionPressed;
        public event Action<Keys> AssignmentRequested;

        public Keys Hotkey => _hotkey;

        public void CancelPress()
        {
            _pressTracking = false;
            _longPressTriggered = false;
        }

        public MobilePotionButton(
            Keys hotkey,
            int visualSize = 58,
            int touchSize = 72)
        {
            if (hotkey != Keys.Q &&
                hotkey != Keys.W &&
                hotkey != Keys.E &&
                hotkey != Keys.R)
            {
                throw new ArgumentException(
                    "MobilePotionButton solo admite Q, W, E o R.",
                    nameof(hotkey));
            }

            _hotkey = hotkey;

            _visualSize = visualSize;
            _touchSize = Math.Max(touchSize, visualSize);

            AutoViewSize = false;

            ControlSize = new Point(
                _touchSize,
                _touchSize);

            ViewSize = ControlSize;

            BackgroundColor = Color.Transparent;

            // ─────────────────────────────────────────────
            // BOTÓN INVISIBLE / ÁREA TÁCTIL
            // ─────────────────────────────────────────────

            _hitButton = new ButtonControl
            {
                Text = string.Empty,

                X = 0,
                Y = 0,

                ControlSize = new Point(
                    _touchSize,
                    _touchSize),

                ViewSize = new Point(
                    _touchSize,
                    _touchSize),

                AutoViewSize = false,
                Interactive = true,

                BackgroundColor = Color.Transparent,
                HoverBackgroundColor = Color.Transparent,
                PressedBackgroundColor = Color.Transparent
            };

            // ─────────────────────────────────────────────
            // LETRA Q / W / E / R
            // ─────────────────────────────────────────────

            _hotkeyLabel = new LabelControl
            {
                Text = _hotkey.ToString(),

                X = (_touchSize / 2) - 4,
                Y = _touchSize - 21,

                FontSize = 10f,
                IsBold = true,
                TextColor = Color.White,
                ShadowOpacity = 0.9f,

                Interactive = false
            };

            // ─────────────────────────────────────────────
            // CONTADOR
            // ─────────────────────────────────────────────

            _countLabel = new LabelControl
            {
                Text = string.Empty,

                X = _touchSize - 24,
                Y = 8,

                FontSize = 9f,
                IsBold = true,
                TextColor = Color.White,
                ShadowOpacity = 0.9f,

                Interactive = false
            };

            // ─────────────────────────────────────────────
            // "+" CUANDO NO HAY ASIGNACIÓN
            // ─────────────────────────────────────────────

            _plusLabel = new LabelControl
            {
                Text = "+",

                X = (_touchSize / 2) - 6,
                Y = (_touchSize / 2) - 13,

                FontSize = 18f,
                IsBold = true,

                TextColor = new Color(
                    170,
                    170,
                    170),

                ShadowOpacity = 0.9f,

                Interactive = false
            };

            // El botón invisible va primero.
            // Los textos se dibujan encima.
            Controls.Add(_hitButton);
            Controls.Add(_hotkeyLabel);
            Controls.Add(_countLabel);
            Controls.Add(_plusLabel);
        }

        public override async Task Load()
        {
            _frameTexture =
                CreateCircleTexture(
                    _visualSize);

            await base.Load();
        }

        public void SetOpacity(float opacity)
        {
            Alpha = MathHelper.Clamp(
                opacity,
                0f,
                1f);

            _hotkeyLabel.Alpha = Alpha;
            _countLabel.Alpha = Alpha;
            _plusLabel.Alpha = Alpha;
        }

        public override void Update(GameTime gameTime)
        {
            _coveredByWindow =
                Scene is GameScene gameScene &&
                gameScene.IsMobileControlCovered(this);

            if (_coveredByWindow)
            {
                CancelPress();
                _hitButton.Interactive = false;
            }
            else
            {
                _hitButton.Interactive = true;
            }

            base.Update(gameTime);

            RefreshItemState();

            if (_coveredByWindow)
            {
                return;
            }

            UpdatePressState(gameTime);
        }
        private void RefreshItemState()
        {
            var inventory =
                InventoryControl.Instance;

            if (inventory == null)
            {
                _hasAssignment = false;
                _assignedItem = null;
                _count = 0;

                _countLabel.Text = string.Empty;
                _plusLabel.Text = "+";

                return;
            }

            _hasAssignment =
                inventory.TryGetItemHotkey(
                    _hotkey,
                    out _,
                    out _,
                    out _,
                    out _);

            if (!_hasAssignment)
            {
                _assignedItem = null;
                _cachedIconTexture = null;

                _count = 0;

                _countLabel.Text = string.Empty;
                _plusLabel.Text = "+";

                return;
            }

            _count =
                inventory.GetItemHotkeyTotalCount(
                    _hotkey);

            // Importante:
            // asignada + sin stock = "0".
            _countLabel.Text =
                _count.ToString();

            _plusLabel.Text =
                string.Empty;

            if (inventory.TryGetItemHotkeyItem(
                    _hotkey,
                    out InventoryItem item))
            {
                _assignedItem = item;
            }
            else
            {
                // No borramos _cachedIconTexture.
                //
                // Así, si el jugador acaba de gastar
                // la última poción, podemos conservar
                // visualmente el icono y mostrar 0.
                _assignedItem = null;
            }
        }

        private void UpdatePressState(
            GameTime gameTime)
        {
            bool pressed =
                _hitButton.IsMousePressed;

            if (pressed)
            {
                if (!_pressTracking)
                {
                    _pressTracking = true;
                    _longPressTriggered = false;

                    _pressStartTime =
                        gameTime.TotalGameTime;

                    return;
                }

                if (!_longPressTriggered)
                {
                    double heldMilliseconds =
                        (gameTime.TotalGameTime -
                         _pressStartTime)
                        .TotalMilliseconds;

                    if (heldMilliseconds >=
                        LongPressMilliseconds)
                    {
                        _longPressTriggered = true;

                        AssignmentRequested?.Invoke(
                            _hotkey);
                    }
                }

                return;
            }

            if (!_pressTracking)
            {
                return;
            }

            // Si realmente se soltó el dedo/mouse,
            // y NO se activó el long press,
            // lo interpretamos como tap corto.
            bool released =
                MuGame.Instance.UiMouseState.LeftButton ==
                ButtonState.Released;

            if (released &&
                !_longPressTriggered)
            {
                PotionPressed?.Invoke(
                    _hotkey);
            }

            _pressTracking = false;
            _longPressTriggered = false;
        }

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible ||
                Status != GameControlStatus.Ready ||
                GraphicsManager.Instance == null ||
                _frameTexture == null)
            {
                return;
            }

            Texture2D iconTexture =
                GetCurrentIconTexture();

            var sprite =
                GraphicsManager.Instance.Sprite;

            Rectangle controlRect =
                DisplayRectangle;

            int offset =
                (_touchSize - _visualSize) / 2;

            Rectangle frameRectangle =
                new Rectangle(
                    controlRect.X + offset,
                    controlRect.Y + offset,
                    _visualSize,
                    _visualSize);

            int iconInset = 10;

            Rectangle iconRectangle =
                new Rectangle(
                    frameRectangle.X + iconInset,
                    frameRectangle.Y + iconInset,
                    _visualSize - (iconInset * 2),
                    _visualSize - (iconInset * 2));

            if (SpriteBatchScope.BatchIsBegun)
            {
                DrawPotionButton(
                    sprite,
                    frameRectangle,
                    iconRectangle,
                    iconTexture);
            }
            else
            {
                using (
                    new SpriteBatchScope(
                        sprite,
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        transform:
                            UiScaler.SpriteTransform))
                {
                    DrawPotionButton(
                        sprite,
                        frameRectangle,
                        iconRectangle,
                        iconTexture);
                }
            }

            // Labels + hit button.
            base.Draw(gameTime);
        }

        private void DrawPotionButton(
            SpriteBatch sprite,
            Rectangle frameRectangle,
            Rectangle iconRectangle,
            Texture2D iconTexture)
        {
            float buttonAlpha =
                Alpha;

            Color frameColor =
                Color.White;

            if (_hitButton.IsMousePressed)
            {
                frameColor =
                    new Color(
                        165,
                        165,
                        165);
            }
            else if (!_hasAssignment)
            {
                frameColor =
                    new Color(
                        190,
                        190,
                        190);
            }
            else if (_count <= 0)
            {
                frameColor =
                    new Color(
                        120,
                        120,
                        120);
            }

            sprite.Draw(
                _frameTexture,
                frameRectangle,
                frameColor * buttonAlpha);

            if (iconTexture == null ||
                !_hasAssignment)
            {
                return;
            }

            Color iconColor =
                _count > 0
                    ? Color.White
                    : new Color(
                        110,
                        110,
                        110);

            sprite.Draw(
                iconTexture,
                iconRectangle,
                iconColor * buttonAlpha);
        }

        private Texture2D GetCurrentIconTexture()
        {
            if (_assignedItem?.Definition != null)
            {
                string texturePath =
                    _assignedItem.Definition.TexturePath;

                if (!string.IsNullOrWhiteSpace(
                        texturePath))
                {
                    if (texturePath.EndsWith(
                            ".bmd",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedIconTexture =
                            BmdPreviewRenderer.GetPreview(
                                _assignedItem,
                                48,
                                48);
                    }
                    else
                    {
                        _cachedIconTexture =
                            TextureLoader.Instance
                                .GetTexture2D(
                                    texturePath);
                    }
                }
            }

            return _cachedIconTexture;
        }

        private Texture2D CreateCircleTexture(
            int diameter)
        {
            var texture =
                new Texture2D(
                    GraphicsDevice,
                    diameter,
                    diameter);

            var pixels =
                new Color[
                    diameter * diameter];

            float radius =
                diameter / 2f;

            float outerRadiusSquared =
                radius * radius;

            float borderRadius =
                radius - 3f;

            float borderRadiusSquared =
                borderRadius *
                borderRadius;

            Vector2 center =
                new Vector2(
                    radius - 0.5f,
                    radius - 0.5f);

            Color outerBorder =
                Color.FromNonPremultiplied(
                    115,
                    105,
                    90,
                    235);

            Color innerFill =
                Color.FromNonPremultiplied(
                    18,
                    18,
                    22,
                    205);

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
                        (y * diameter) +
                        x;

                    if (distanceSquared >
                        outerRadiusSquared)
                    {
                        pixels[index] =
                            Color.Transparent;
                    }
                    else if (distanceSquared >=
                             borderRadiusSquared)
                    {
                        pixels[index] =
                            outerBorder;
                    }
                    else
                    {
                        pixels[index] =
                            innerFill;
                    }
                }
            }

            texture.SetData(
                pixels);

            return texture;
        }

        public override void Dispose()
        {
            _frameTexture?.Dispose();
            _frameTexture = null;

            // NO hacemos Dispose de _cachedIconTexture:
            // puede pertenecer al TextureLoader
            // o al BmdPreviewRenderer.

            _cachedIconTexture = null;

            base.Dispose();
        }
    }
}