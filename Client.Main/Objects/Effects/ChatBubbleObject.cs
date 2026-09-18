using System;
using System.Text;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Controls.UI;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Networking;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Chat bubble displayed above a player or NPC.
    /// </summary>
    public class ChatBubbleObject : EffectObject
    {
        private const float DefaultLifetime = 5f;
        private const float OffsetZ = 60f;
        private const int PixelGap = 8;
        private const int MaxBubbleWidth = 200;

        private string _text;
        private readonly string _playerName;
        private readonly ushort _targetId;

        private float _lifetime;
        private readonly float _originalLifetime;
        private float _elapsed;

        private LabelControl _nameLabel;
        private LabelControl _guildLabel;
        private LabelControl _textLabel;

        private SpriteFont _font;

        /// <summary>
        /// Indica si la burbuja actual debe mostrar
        /// la segunda línea con Guild + rango.
        ///
        /// No usamos directamente _guildLabel.Visible
        /// durante el cálculo porque el control podría
        /// hacerse visible antes de haber recibido X/Y,
        /// provocando que aparezca un frame en (0,0).
        /// </summary>
        private bool _showGuildInfo;

        /// <summary>
        /// Creates a new chat bubble.
        /// </summary>
        /// <param name="text">Message text to display.</param>
        /// <param name="targetId">Network id of the player.</param>
        /// <param name="playerName">Name of the player.</param>
        /// <param name="lifetime">Optional lifetime in seconds.</param>
        public ChatBubbleObject(
            string text,
            ushort targetId,
            string playerName,
            float lifetime = DefaultLifetime)
        {
            _text =
                text ?? string.Empty;

            _playerName =
                playerName ?? string.Empty;

            _targetId =
                targetId;

            _lifetime =
                lifetime;

            _originalLifetime =
                lifetime;

            IsTransparent =
                true;

            AffectedByTransparency =
                false;
        }

        public override async Task Load()
        {
            _font =
                GraphicsManager.Instance.Font;

            // =========================================================
            // NOMBRE
            // =========================================================

            _nameLabel =
                new LabelControl
                {
                    Text =
                        $"{_playerName}:",

                    FontSize =
                        10f,

                    TextColor =
                        Color.Yellow,

                    HasShadow =
                        true,

                    ShadowColor =
                        Color.Black,

                    ShadowOpacity =
                        0.8f,

                    BackgroundColor =
                        new Color(
                            30,
                            35,
                            50,
                            145),

                    Padding =
                        new Margin
                        {
                            Left = 4,
                            Right = 4,
                            Top = 2,
                            Bottom = 1
                        },

                    UseManualPosition =
                        true,

                    UseControlSizeBackground =
                        true,

                    Visible =
                        false
                };

            // =========================================================
            // GUILD + RANGO
            // =========================================================

            _guildLabel =
                new LabelControl
                {
                    Text =
                        string.Empty,

                    FontSize =
                        10f,

                    TextColor =
                        new Color(
                            255,
                            225,
                            80),

                    HasShadow =
                        true,

                    ShadowColor =
                        Color.Black,

                    ShadowOpacity =
                        0.8f,

                    BackgroundColor =
                        new Color(
                            30,
                            35,
                            50,
                            145),

                    Padding =
                        new Margin
                        {
                            Left = 4,
                            Right = 4,
                            Top = 1,
                            Bottom = 2
                        },

                    UseManualPosition =
                        true,

                    UseControlSizeBackground =
                        true,

                    Visible =
                        false
                };

            // =========================================================
            // MENSAJE
            // =========================================================

            _textLabel =
                new LabelControl
                {
                    Text =
                        _text,

                    FontSize =
                        10f,

                    TextColor =
                        Color.White,

                    HasShadow =
                        true,

                    ShadowColor =
                        Color.Black,

                    ShadowOpacity =
                        0.8f,

                    // Más oscuro y transparente que el encabezado.
                    BackgroundColor =
                        new Color(
                            15,
                            18,
                            25,
                            100),

                    Padding =
                        new Margin
                        {
                            Left = 4,
                            Right = 4,
                            Top = 2,
                            Bottom = 2
                        },

                    UseManualPosition =
                        true,

                    UseControlSizeBackground =
                        true,

                    Visible =
                        false
                };

            _textLabel.Text =
                WrapText(
                    _textLabel.Text,
                    _textLabel.FontSize,
                    MaxBubbleWidth);

            // =========================================================
            // AGREGAR LABELS A LA ESCENA
            // =========================================================

            if (World?.Scene != null)
            {
                World.Scene.Controls.Add(
                    _nameLabel);

                World.Scene.Controls.Add(
                    _guildLabel);

                World.Scene.Controls.Add(
                    _textLabel);

                await _nameLabel.Load();

                await _guildLabel.Load();

                await _textLabel.Load();
            }

            Status =
                GameControlStatus.Ready;
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);

            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }

            _elapsed +=
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            // =========================================================
            // EXPIRACIÓN
            // =========================================================

            if (_elapsed >=
                _lifetime)
            {
                World?.Scene?.Controls.Remove(
                    _nameLabel);

                World?.Scene?.Controls.Remove(
                    _guildLabel);

                World?.Scene?.Controls.Remove(
                    _textLabel);

                _nameLabel?.Dispose();
                _guildLabel?.Dispose();
                _textLabel?.Dispose();

                World?.RemoveObject(
                    this);

                Dispose();

                return;
            }

            // =========================================================
            // BUSCAR JUGADOR / NPC
            // =========================================================

            WalkerObject target =
                ResolveTarget();

            if (target == null ||
                target.Hidden ||
                target.Status !=
                GameControlStatus.Ready)
            {
                HideLabels();
                return;
            }

            // Primero recopilamos la información.
            // No hacemos visible _guildLabel todavía.
            UpdateGuildInformation(
                target);

            // Después calculamos X/Y y finalmente
            // hacemos visibles los controles.
            UpdateLabelPosition(
                target);
        }

        public override void Draw(
            GameTime gameTime)
        {
            // El contenido visual real son controles UI.
        }

        // =============================================================
        // TARGET
        // =============================================================

        private WalkerObject ResolveTarget()
        {
            if (World == null)
            {
                return null;
            }

            return World.TryGetWalkerById(
                _targetId,
                out var walker)
                    ? walker
                    : null;
        }

        // =============================================================
        // GUILD
        // =============================================================

        private void UpdateGuildInformation(
            WalkerObject target)
        {
            _showGuildInfo =
                false;

            // NPC u otro tipo de Walker:
            // usar burbuja estándar.
            if (target is not
                PlayerObject player)
            {
                ApplyBubbleColors(
                    sameGuild: false,
                    hostileGuild: false);

                return;
            }

            // ---------------------------------------------------------
            // GUILD DEL JUGADOR
            // ---------------------------------------------------------

            if (!GuildInfoCache
                    .TryGetPlayerGuildId(
                        player.NetworkId,
                        out uint guildId) ||
                guildId == 0)
            {
                ApplyBubbleColors(
                    sameGuild: false,
                    hostileGuild: false);

                return;
            }

            if (!GuildInfoCache
                    .TryGetGuild(
                        guildId,
                        out GuildInfoData guild))
            {
                ApplyBubbleColors(
                    sameGuild: false,
                    hostileGuild: false);

                return;
            }

            // ---------------------------------------------------------
            // RANGO
            // ---------------------------------------------------------

            string roleName =
                string.Empty;

            if (GuildInfoCache
                .TryGetPlayerGuildRole(
                    player.NetworkId,
                    out byte role))
            {
                roleName =
                    role switch
                    {
                        0x80 =>
                            "Guild Master",

                        0x40 =>
                            "Assistant Master",

                        0x20 =>
                            "Battle Master",

                        0x00 =>
                            "Member",

                        _ =>
                            string.Empty
                    };
            }

            _guildLabel.Text =
                string.IsNullOrEmpty(
                    roleName)
                    ? $"Guild: {guild.GuildName}"
                    : $"Guild: {guild.GuildName} | {roleName}";

            // ---------------------------------------------------------
            // GUILD DEL JUGADOR LOCAL
            // ---------------------------------------------------------

            uint localGuildId =
                0;

            if (World?.Scene
                    is GameScene gameScene &&
                gameScene.Hero != null)
            {
                GuildInfoCache
                    .TryGetPlayerGuildId(
                        gameScene.Hero.NetworkId,
                        out localGuildId);
            }

            bool sameGuild =
                localGuildId != 0 &&
                localGuildId ==
                guildId;

            // ---------------------------------------------------------
            // HOSTILIDAD
            //
            // Por ahora usamos RivalGuildName del roster.
            // Cuando implementemos Guild Alliances / Hostility,
            // esto se sustituirá por la relación formal.
            // ---------------------------------------------------------

            bool hostileGuild =
                false;

            GuildRosterData roster =
                GuildInfoCache.CurrentRoster;

            if (localGuildId != 0 &&
                roster != null &&
                roster.IsInGuild &&
                !string.IsNullOrWhiteSpace(
                    roster.RivalGuildName) &&
                string.Equals(
                    roster.RivalGuildName,
                    guild.GuildName,
                    StringComparison.OrdinalIgnoreCase))
            {
                hostileGuild =
                    true;
            }

            ApplyBubbleColors(
                sameGuild,
                hostileGuild);

            // IMPORTANTE:
            // todavía NO hacemos Visible = true.
            //
            // UpdateLabelPosition() primero establecerá
            // X/Y y después mostrará el control.
            _showGuildInfo =
                true;
        }

        // =============================================================
        // COLORES
        // =============================================================

        private void ApplyBubbleColors(
            bool sameGuild,
            bool hostileGuild)
        {
            Color headerBackground;
            Color messageBackground;

            // Blanco azulado suave para TODO el encabezado.
            // No cambia según Guild.
            Color headerTextColor =
                new Color(
                    210,
                    225,
                    232);

            // Texto del mensaje también neutro.
            Color messageTextColor =
                new Color(
                    235,
                    235,
                    240);

            if (hostileGuild)
            {
                // Encabezado rojo oscuro y suave
                headerBackground =
                    new Color(
                        30,
                        8,
                        8,
                        90);

                // Cuerpo todavía más oscuro/transparente
                messageBackground =
                    new Color(
                        12,
                        5,
                        5,
                        90);
            }
            else if (sameGuild)
            {
                // Este es exactamente el aspecto que
                // ahora tiene el cuerpo y que te gustó.
                headerBackground =
                    new Color(
                        28,
                        32,
                        12,
                        90);

                // Cuerpo más oscuro y transparente
                messageBackground =
                    new Color(
                        10,
                        13,
                        7,
                        90);
            }
            else
            {
                // Neutral
                headerBackground =
                    new Color(
                        15,
                        18,
                        25,
                        90);

                messageBackground =
                    new Color(
                        8,
                        10,
                        14,
                        90);
            }

            // =========================================================
            // ENCABEZADO
            // =========================================================

            if (_nameLabel != null)
            {
                _nameLabel.BackgroundColor =
                    headerBackground;

                _nameLabel.TextColor =
                    headerTextColor;
            }

            if (_guildLabel != null)
            {
                _guildLabel.BackgroundColor =
                    headerBackground;

                _guildLabel.TextColor =
                    headerTextColor;
            }

            // =========================================================
            // MENSAJE
            // =========================================================

            if (_textLabel != null)
            {
                _textLabel.BackgroundColor =
                    messageBackground;

                _textLabel.TextColor =
                    messageTextColor;
            }
        }

        // =============================================================
        // POSICIÓN DE LA BURBUJA
        // =============================================================

        private void UpdateLabelPosition(
            WalkerObject target)
        {
            if (_nameLabel == null ||
                _guildLabel == null ||
                _textLabel == null)
            {
                return;
            }

            // ---------------------------------------------------------
            // PUNTO 3D SOBRE LA CABEZA
            // ---------------------------------------------------------

            Vector3 anchor =
                new Vector3(
                    (target.BoundingBoxWorld.Min.X +
                     target.BoundingBoxWorld.Max.X) *
                    0.5f,

                    (target.BoundingBoxWorld.Min.Y +
                     target.BoundingBoxWorld.Max.Y) *
                    0.5f,

                    target.BoundingBoxWorld.Max.Z +
                    OffsetZ);

            Vector3 screen =
                GraphicsDevice.Viewport.Project(
                    anchor,
                    Camera.Instance.Projection,
                    Camera.Instance.View,
                    Matrix.Identity);

            if (screen.Z < 0f ||
                screen.Z > 1f)
            {
                HideLabels();
                return;
            }

            // ---------------------------------------------------------
            // SCREEN -> UI VIRTUAL
            // ---------------------------------------------------------

            Point screenPoint =
                new Point(
                    (int)screen.X,
                    (int)screen.Y);

            Point virtualPos =
                UiScaler.ToVirtual(
                    screenPoint);

            // ---------------------------------------------------------
            // MEDIDAS
            // ---------------------------------------------------------

            Vector2 nameSize =
                MeasureLabelSize(
                    _nameLabel);

            Vector2 guildSize =
                _showGuildInfo
                    ? MeasureLabelSize(
                        _guildLabel)
                    : Vector2.Zero;

            Vector2 textSize =
                MeasureLabelSize(
                    _textLabel);

            // ---------------------------------------------------------
            // NOMBRE
            // ---------------------------------------------------------

            int nameWidth =
                (int)nameSize.X +
                _nameLabel.Padding.Left +
                _nameLabel.Padding.Right;

            int nameHeight =
                (int)nameSize.Y +
                _nameLabel.Padding.Top +
                _nameLabel.Padding.Bottom;

            // ---------------------------------------------------------
            // GUILD
            // ---------------------------------------------------------

            int guildWidth =
                _showGuildInfo
                    ? (int)guildSize.X +
                      _guildLabel.Padding.Left +
                      _guildLabel.Padding.Right
                    : 0;

            int guildHeight =
                _showGuildInfo
                    ? (int)guildSize.Y +
                      _guildLabel.Padding.Top +
                      _guildLabel.Padding.Bottom
                    : 0;

            // ---------------------------------------------------------
            // MENSAJE
            // ---------------------------------------------------------

            int textWidth =
                (int)textSize.X +
                _textLabel.Padding.Left +
                _textLabel.Padding.Right;

            int textHeight =
                (int)textSize.Y +
                _textLabel.Padding.Top +
                _textLabel.Padding.Bottom;

            // ---------------------------------------------------------
            // ANCHO TOTAL
            // ---------------------------------------------------------

            int bubbleWidth =
                Math.Max(
                    nameWidth,
                    Math.Max(
                        guildWidth,
                        textWidth));

            int bubbleHeight =
                nameHeight +
                guildHeight +
                textHeight;

            int bubbleX =
                (int)(
                    virtualPos.X -
                    bubbleWidth /
                    2f);

            int bubbleY =
                (int)(
                    virtualPos.Y -
                    bubbleHeight -
                    PixelGap);

            // =========================================================
            // NOMBRE
            // =========================================================

            _nameLabel.ControlSize =
                new Point(
                    Math.Max(
                        1,
                        bubbleWidth -
                        _nameLabel.Padding.Left -
                        _nameLabel.Padding.Right),

                    Math.Max(
                        1,
                        (int)nameSize.Y));

            _nameLabel.X =
                bubbleX;

            _nameLabel.Y =
                bubbleY;

            // =========================================================
            // GUILD
            // =========================================================

            if (_showGuildInfo)
            {
                _guildLabel.ControlSize =
                    new Point(
                        Math.Max(
                            1,
                            bubbleWidth -
                            _guildLabel.Padding.Left -
                            _guildLabel.Padding.Right),

                        Math.Max(
                            1,
                            (int)guildSize.Y));

                _guildLabel.X =
                    bubbleX;

                _guildLabel.Y =
                    _nameLabel.Y +
                    nameHeight;
            }

            // =========================================================
            // MENSAJE
            // =========================================================

            _textLabel.ControlSize =
                new Point(
                    Math.Max(
                        1,
                        bubbleWidth -
                        _textLabel.Padding.Left -
                        _textLabel.Padding.Right),

                    Math.Max(
                        1,
                        (int)textSize.Y));

            _textLabel.X =
                bubbleX;

            if (_showGuildInfo)
            {
                _textLabel.Y =
                    _guildLabel.Y +
                    guildHeight;
            }
            else
            {
                _textLabel.Y =
                    _nameLabel.Y +
                    nameHeight;
            }

            // =========================================================
            // MOSTRAR
            //
            // MUY IMPORTANTE:
            // primero calculamos X/Y.
            // Recién ahora hacemos visibles los labels.
            // =========================================================

            _nameLabel.Visible =
                true;

            _guildLabel.Visible =
                _showGuildInfo;

            _textLabel.Visible =
                true;
        }

        // =============================================================
        // OCULTAR LABELS
        // =============================================================

        private void HideLabels()
        {
            if (_nameLabel != null)
            {
                _nameLabel.Visible =
                    false;
            }

            if (_guildLabel != null)
            {
                _guildLabel.Visible =
                    false;
            }

            if (_textLabel != null)
            {
                _textLabel.Visible =
                    false;
            }
        }

        // =============================================================
        // MEDICIÓN
        // =============================================================

        private Vector2 MeasureLabelSize(
            LabelControl label)
        {
            if (_font == null ||
                label == null)
            {
                return Vector2.Zero;
            }

            float scale =
                label.FontSize /
                Constants.BASE_FONT_SIZE;

            Vector2 size =
                _font.MeasureString(
                    label.Text ?? string.Empty) *
                scale;

            if (label.HasShadow)
            {
                size.X +=
                    (float)Math.Ceiling(
                        Math.Abs(
                            label.ShadowOffset.X));

                size.Y +=
                    (float)Math.Ceiling(
                        Math.Abs(
                            label.ShadowOffset.Y));
            }

            if (label.IsBold)
            {
                size.X +=
                    (float)Math.Ceiling(
                        label.BoldStrength *
                        2);

                size.Y +=
                    (float)Math.Ceiling(
                        label.BoldStrength *
                        2);
            }

            return size;
        }

        // =============================================================
        // WRAP DEL MENSAJE
        // =============================================================

        private string WrapText(
            string rawText,
            float fontSize,
            int maxWidth)
        {
            if (_font == null ||
                string.IsNullOrEmpty(
                    rawText))
            {
                return rawText;
            }

            float scale =
                fontSize /
                Constants.BASE_FONT_SIZE;

            string[] words =
                rawText.Split(' ');

            var result =
                new StringBuilder();

            var currentLine =
                new StringBuilder();

            foreach (string word
                     in words)
            {
                string test =
                    currentLine.Length == 0
                        ? word
                        : currentLine +
                          " " +
                          word;

                float width =
                    _font
                        .MeasureString(
                            test)
                        .X *
                    scale;

                if (width <=
                    maxWidth)
                {
                    currentLine.Clear();

                    currentLine.Append(
                        test);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        if (result.Length > 0)
                        {
                            result.Append(
                                '\n');
                        }

                        result.Append(
                            currentLine);

                        currentLine.Clear();
                    }

                    currentLine.Append(
                        word);
                }
            }

            if (currentLine.Length > 0)
            {
                if (result.Length > 0)
                {
                    result.Append(
                        '\n');
                }

                result.Append(
                    currentLine);
            }

            return result.ToString();
        }

        // =============================================================
        // MENSAJES NUEVOS EN LA MISMA BURBUJA
        // =============================================================

        public void AppendMessage(
            string newMessage)
        {
            if (string.IsNullOrEmpty(
                    newMessage))
            {
                return;
            }

            _text =
                newMessage +
                "\n" +
                _text;

            _lifetime =
                _elapsed +
                _originalLifetime;

            if (_textLabel != null)
            {
                _textLabel.Text =
                    WrapText(
                        _text,
                        _textLabel.FontSize,
                        MaxBubbleWidth);
            }
        }

        // =============================================================
        // PROPERTIES
        // =============================================================

        public ushort TargetId =>
            _targetId;

        // =============================================================
        // DISPOSE
        // =============================================================

        public override void Dispose()
        {
            if (_nameLabel != null)
            {
                _nameLabel.Parent
                    ?.Controls
                    .Remove(
                        _nameLabel);

                _nameLabel.Dispose();
            }

            if (_guildLabel != null)
            {
                _guildLabel.Parent
                    ?.Controls
                    .Remove(
                        _guildLabel);

                _guildLabel.Dispose();
            }

            if (_textLabel != null)
            {
                _textLabel.Parent
                    ?.Controls
                    .Remove(
                        _textLabel);

                _textLabel.Dispose();
            }

            base.Dispose();
        }
    }
}