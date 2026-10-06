#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Mobile
{
    /// <summary>
    /// Hotkey móvil de skill.
    ///
    /// slotIndex:
    /// 0 = tecla 1
    /// 1 = tecla 2
    /// 2 = tecla 3
    ///
    /// Tap corto:
    /// ejecuta la skill asignada.
    ///
    /// Long press:
    /// abre el selector de skills.
    /// </summary>
    public sealed class MobileSkillButton : UIControl
    {
        private const double LongPressMilliseconds =
            500.0;
        // El atlas original de MU usa iconos muy pequeños.
        // No conviene ocupar casi todo el botón porque
        // terminamos ampliándolos demasiado.
        private const int SkillIconDisplaySize =
            32;

        private readonly int _slotIndex;
        private readonly int _visualSize;
        private readonly int _touchSize;

        private readonly ButtonControl _hitButton;
        private readonly LabelControl _hotkeyLabel;
        private readonly LabelControl _plusLabel;

        private Texture2D? _frameTexture;
        private Texture2D? _circularIconTexture;

        private SkillEntryState? _skill;

        private ushort _cachedSkillId;

        private bool _circularBuildFailed;

        private bool _pressTracking;
        private bool _coveredByWindow;
        private bool _longPressTriggered;

        private TimeSpan _pressStartTime;

        public event Action<int>? SkillPressed;

        public event Action<int>? AssignmentRequested;

        public int SlotIndex =>
            _slotIndex;

        public SkillEntryState? Skill =>
            _skill;

        public MobileSkillButton(
            int slotIndex,
            int visualSize = 58,
            int touchSize = 72)
        {
            if (slotIndex < 0 ||
                slotIndex > 9)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex));
            }

            _slotIndex =
                slotIndex;

            _visualSize =
                visualSize;

            _touchSize =
                Math.Max(
                    touchSize,
                    visualSize);

            AutoViewSize =
                false;

            ControlSize =
                new Point(
                    _touchSize,
                    _touchSize);

            ViewSize =
                ControlSize;

            BackgroundColor =
                Color.Transparent;

            // =========================================================
            // SENSOR
            // =========================================================

            _hitButton =
                new ButtonControl
                {
                    Text =
                        string.Empty,

                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            _touchSize,
                            _touchSize),

                    ViewSize =
                        new Point(
                            _touchSize,
                            _touchSize),

                    AutoViewSize =
                        false,

                    Interactive =
                        true,

                    BackgroundColor =
                        Color.Transparent,

                    HoverBackgroundColor =
                        Color.Transparent,

                    PressedBackgroundColor =
                        Color.Transparent
                };

            // =========================================================
            // NÚMERO 1 / 2 / 3...
            // =========================================================

            _hotkeyLabel =
                new LabelControl
                {
                    Text =
                        (_slotIndex + 1)
                        .ToString(),

                    X =
                        (_touchSize / 2) - 4,

                    Y =
                        _touchSize - 21,

                    FontSize =
                        10f,

                    IsBold =
                        true,

                    TextColor =
                        Color.White,

                    ShadowOpacity =
                        0.9f,

                    Interactive =
                        false
                };

            // =========================================================
            // + SIN SKILL
            // =========================================================

            _plusLabel =
                new LabelControl
                {
                    Text =
                        "+",

                    X =
                        (_touchSize / 2) - 6,

                    Y =
                        (_touchSize / 2) - 13,

                    FontSize =
                        18f,

                    IsBold =
                        true,

                    TextColor =
                        new Color(
                            170,
                            170,
                            170),

                    ShadowOpacity =
                        0.9f,

                    Interactive =
                        false
                };

            Controls.Add(
                _hitButton);

            Controls.Add(
                _hotkeyLabel);

            Controls.Add(
                _plusLabel);
        }

        public override async Task Load()
        {
            _frameTexture =
                CreateCircleTexture(
                    _visualSize);

            PrepareSkillTexture();

            await base.Load();
        }

        // =============================================================
        // SKILL
        // =============================================================

        public void SetSkill(
            SkillEntryState? skill)
        {
            ushort newSkillId =
                skill?.SkillId ??
                0;

            if (_skill?.SkillId ==
                    newSkillId &&
                _cachedSkillId ==
                    newSkillId)
            {
                _skill =
                    skill;

                _plusLabel.Text =
                    skill == null
                        ? "+"
                        : string.Empty;

                return;
            }

            _skill =
                skill;

            DisposeCircularIcon();

            _cachedSkillId =
                0;

            _circularBuildFailed =
                false;

            _plusLabel.Text =
                skill == null
                    ? "+"
                    : string.Empty;

            PrepareSkillTexture();
        }

        private void PrepareSkillTexture()
        {
            if (_skill == null)
            {
                return;
            }

            SkillIconInfo? iconInfo =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId);

            if (!iconInfo.HasValue)
            {
                return;
            }

            _ =
                TextureLoader.Instance
                    .Prepare(
                        iconInfo.Value
                            .TexturePath);
        }

        // =============================================================
        // PRESS
        // =============================================================

        public void CancelPress()
        {
            _pressTracking =
                false;

            _longPressTriggered =
                false;
        }

        public override void Update(
            GameTime gameTime)
        {
            _coveredByWindow =
                Scene is GameScene gameScene &&
                gameScene.IsMobileControlCovered(
                    this);

            if (_coveredByWindow)
            {
                CancelPress();

                _hitButton.Interactive =
                    false;
            }
            else
            {
                _hitButton.Interactive =
                    true;
            }

            base.Update(
                gameTime);

            if (_coveredByWindow)
            {
                return;
            }

            UpdatePressState(
                gameTime);
        }

        private void UpdatePressState(
            GameTime gameTime)
        {
            bool pressed =
                _hitButton
                    .IsMousePressed;

            if (pressed)
            {
                if (!_pressTracking)
                {
                    _pressTracking =
                        true;

                    _longPressTriggered =
                        false;

                    _pressStartTime =
                        gameTime.TotalGameTime;

                    return;
                }

                if (!_longPressTriggered)
                {
                    double heldMilliseconds =
                        (
                            gameTime.TotalGameTime -
                            _pressStartTime
                        )
                        .TotalMilliseconds;

                    if (heldMilliseconds >=
                        LongPressMilliseconds)
                    {
                        _longPressTriggered =
                            true;

                        AssignmentRequested?
                            .Invoke(
                                _slotIndex);
                    }
                }

                return;
            }

            if (!_pressTracking)
            {
                return;
            }

            bool released =
                MuGame.Instance
                    .UiMouseState
                    .LeftButton ==
                ButtonState.Released;

            if (released &&
                !_longPressTriggered)
            {
                SkillPressed?
                    .Invoke(
                        _slotIndex);
            }

            _pressTracking =
                false;

            _longPressTriggered =
                false;
        }

        // =============================================================
        // OPACITY
        // =============================================================

        public void SetOpacity(
            float opacity)
        {
            Alpha =
                MathHelper.Clamp(
                    opacity,
                    0f,
                    1f);

            _hotkeyLabel.Alpha =
                Alpha;

            _plusLabel.Alpha =
                Alpha;
        }

        // =============================================================
        // DRAW
        // =============================================================

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible ||
                Status !=
                    GameControlStatus.Ready ||
                GraphicsManager.Instance ==
                    null ||
                _frameTexture ==
                    null)
            {
                return;
            }

            Rectangle controlRect =
                DisplayRectangle;

            int offset =
                (
                    _touchSize -
                    _visualSize
                ) / 2;

            Rectangle frameRectangle =
                new Rectangle(
                    controlRect.X +
                        offset,

                    controlRect.Y +
                        offset,

                    _visualSize,
                    _visualSize);

            int iconX =
                frameRectangle.X +
                (
                    _visualSize -
                    SkillIconDisplaySize
                ) / 2;

            int iconY =
                frameRectangle.Y +
                (
                    _visualSize -
                    SkillIconDisplaySize
                ) / 2;

            Rectangle iconRectangle =
                new Rectangle(
                    iconX,
                    iconY,
                    SkillIconDisplaySize,
                    SkillIconDisplaySize);

            var sprite =
                GraphicsManager.Instance
                    .Sprite;

            if (SpriteBatchScope.BatchIsBegun)
            {
                DrawButton(
                    sprite,
                    frameRectangle,
                    iconRectangle);
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
                    DrawButton(
                        sprite,
                        frameRectangle,
                        iconRectangle);
                }
            }

            base.Draw(
                gameTime);
        }

        private void DrawButton(
            SpriteBatch sprite,
            Rectangle frameRectangle,
            Rectangle iconRectangle)
        {
            Color frameColor =
                Color.White;

            if (_hitButton
                .IsMousePressed)
            {
                frameColor =
                    new Color(
                        165,
                        165,
                        165);
            }
            else if (_skill == null)
            {
                frameColor =
                    new Color(
                        190,
                        190,
                        190);
            }

            sprite.Draw(
                _frameTexture,
                frameRectangle,
                frameColor * Alpha);

            if (_skill == null)
            {
                return;
            }

            Texture2D? circular =
                GetCircularSkillIcon();

            if (circular != null)
            {
                sprite.Draw(
                    circular,
                    iconRectangle,
                    Color.White * Alpha);

                return;
            }

            // Fallback:
            // si el dispositivo no permite GetData()
            // sobre la textura, mostramos el icono
            // normal. No rompe el botón.
            SkillIconInfo? info =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId);

            if (!info.HasValue)
            {
                return;
            }

            Texture2D? atlas =
                TextureLoader.Instance
                    .GetTexture2D(
                        info.Value
                            .TexturePath);

            if (atlas == null)
            {
                return;
            }

            sprite.Draw(
                atlas,
                iconRectangle,
                info.Value
                    .SourceRectangle,
                Color.White * Alpha);
        }

        // =============================================================
        // CORTE CIRCULAR DEL ICONO
        // =============================================================

        private Texture2D?
            GetCircularSkillIcon()
        {
            if (_skill == null)
            {
                return null;
            }

            if (_circularIconTexture !=
                    null &&
                !_circularIconTexture
                    .IsDisposed &&
                _cachedSkillId ==
                    _skill.SkillId)
            {
                return
                    _circularIconTexture;
            }

            if (_circularBuildFailed)
            {
                return null;
            }

            SkillIconInfo? iconInfo =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId);

            if (!iconInfo.HasValue)
            {
                return null;
            }

            Texture2D? atlas =
                TextureLoader.Instance
                    .GetTexture2D(
                        iconInfo.Value
                            .TexturePath);

            // Todavía se está cargando.
            if (atlas == null)
            {
                return null;
            }

            try
            {
                Rectangle original =
                    iconInfo.Value
                        .SourceRectangle;

                // El icono clásico mide 20x28.
                // Recortamos el centro a cuadrado
                // antes de convertirlo en círculo,
                // así no deformamos la imagen.
                int squareSize =
                    Math.Min(
                        original.Width,
                        original.Height);

                Rectangle source =
                    new Rectangle(
                        original.X +
                            (
                                original.Width -
                                squareSize
                            ) / 2,

                        original.Y +
                            (
                                original.Height -
                                squareSize
                            ) / 2,

                        squareSize,
                        squareSize);

                Color[] sourcePixels =
                    new Color[
                        squareSize *
                        squareSize];

                atlas.GetData(
                    0,
                    source,
                    sourcePixels,
                    0,
                    sourcePixels.Length);

                // IMPORTANTE:
                // Conservamos la resolución NATIVA del icono.
                //
                // El código anterior convertía primero el pequeño
                // icono MU a una textura mucho más grande usando
                // nearest-neighbour. Eso hacía visibles los pixels.
                //
                // Ahora solamente aplicamos la máscara circular
                // sobre la textura original.
                //
                // El escalado 20 -> 32 lo hará SpriteBatch después
                // con LinearClamp.

                int diameter =
                    squareSize;

                Color[] output =
                    new Color[
                        diameter *
                        diameter];

                float radius =
                    (diameter - 1) /
                    2f;

                Vector2 center =
                    new Vector2(
                        radius,
                        radius);

                for (int y = 0;
                    y < diameter;
                    y++)
                {
                    for (int x = 0;
                        x < diameter;
                        x++)
                    {
                        int index =
                            y * diameter +
                            x;

                        Vector2 delta =
                            new Vector2(
                                x,
                                y) -
                            center;

                        float distance =
                            delta.Length();

                        // Pequeño antialias de 1 pixel
                        // alrededor del corte circular.
                        float coverage =
                            MathHelper.Clamp(
                                radius +
                                0.5f -
                                distance,
                                0f,
                                1f);

                        if (coverage <= 0f)
                        {
                            output[index] =
                                Color.Transparent;

                            continue;
                        }

                        output[index] =
                            sourcePixels[index] *
                            coverage;
                    }
                }

                _circularIconTexture =
                    new Texture2D(
                        GraphicsDevice,
                        diameter,
                        diameter);

                _circularIconTexture
                    .SetData(
                        output);

                _cachedSkillId =
                    _skill.SkillId;

                return
                    _circularIconTexture;
            }
            catch
            {
                // Algunos backends/dispositivos
                // pueden impedir leer una Texture2D.
                // En ese caso DrawButton usa
                // automáticamente el icono normal.
                _circularBuildFailed =
                    true;

                return null;
            }
        }

        // =============================================================
        // FRAME
        // =============================================================

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
                    diameter *
                    diameter];

            float radius =
                diameter / 2f;

            float outerRadiusSquared =
                radius *
                radius;

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
                        y * diameter +
                        x;

                    if (distanceSquared >
                        outerRadiusSquared)
                    {
                        pixels[index] =
                            Color.Transparent;
                    }
                    else if (
                        distanceSquared >=
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

        private void DisposeCircularIcon()
        {
            _circularIconTexture?
                .Dispose();

            _circularIconTexture =
                null;
        }

        public override void Dispose()
        {
            DisposeCircularIcon();

            _frameTexture?
                .Dispose();

            _frameTexture =
                null;

            base.Dispose();
        }
    }
}