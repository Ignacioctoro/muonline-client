#nullable enable

using Client.Main.Core.Client;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Buffs
{
    /// <summary>
    /// Un único icono de buff/debuff usando las texturas
    /// originales de MU:
    ///
    /// newui_statusicon
    /// newui_statusicon2
    /// </summary>
    public class BuffSlotControl : UIControl
    {
        private ActiveBuffState? _buff;

        private readonly TextureControl
            _buffIcon;

        public const int SLOT_WIDTH =
            BuffIconAtlas.IconWidth;

        public const int SLOT_HEIGHT =
            BuffIconAtlas.IconHeight;

        public ActiveBuffState? Buff
        {
            get => _buff;

            set
            {
                _buff = value;

                UpdateDisplay();
            }
        }

        public BuffSlotControl()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    SLOT_WIDTH,
                    SLOT_HEIGHT);

            ViewSize =
                ControlSize;

            // Más adelante podemos habilitar esto para
            // tooltip clásico al pasar el mouse.
            Interactive = false;

            BackgroundColor =
                Color.Transparent;

            BorderColor =
                Color.Transparent;

            BorderThickness = 0;

            // =====================================================
            // ICONO ORIGINAL
            // =====================================================

            _buffIcon =
                new TextureControl
                {
                    AutoViewSize = false,

                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            BuffIconAtlas.IconWidth,
                            BuffIconAtlas.IconHeight),

                    ViewSize =
                        new Point(
                            BuffIconAtlas.IconWidth,
                            BuffIconAtlas.IconHeight),

                    Visible = false,

                    Interactive = false
                };

            Controls.Add(
                _buffIcon);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_buff == null)
            {
                _buffIcon.Visible =
                    false;

                return;
            }

            if (!BuffIconAtlas.TryResolve(
                    _buff.EffectId,
                    out BuffIconFrame frame))
            {
                _buffIcon.Visible =
                    false;

                return;
            }

            // TexturePath puede ser:
            //
            // Interface/newui_statusicon.jpg
            //
            // TextureLoader lo resolverá realmente contra
            // newui_statusicon.OZJ en el Data.

            _buffIcon.TexturePath =
                frame.TexturePath;

            _buffIcon.TextureRectangle =
                frame.SourceRectangle;

            _buffIcon.Visible =
                true;
        }
    }
}