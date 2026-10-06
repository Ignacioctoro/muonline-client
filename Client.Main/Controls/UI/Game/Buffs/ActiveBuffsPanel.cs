#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Buffs
{
    /// <summary>
    /// Barra clásica de buffs de MU.
    ///
    /// - Centrada arriba.
    /// - Iconos 20x28.
    /// - 5 px entre iconos.
    /// - Máximo 8 iconos por fila.
    /// - Buffs antes que debuffs.
    /// - Sin fondo ni marcos.
    /// </summary>
    public class ActiveBuffsPanel : UIControl
    {
        private readonly CharacterState
            _characterState;

        private readonly List<BuffSlotControl>
            _buffSlots =
                new();

        // SourceMain:
        //
        // BUFF_MAX_LINE_COUNT = 8
        //
        private const int
            BUFFS_PER_ROW = 8;

        // SourceMain:
        //
        // BUFF_IMG_SPACE = 5
        //
        private const int
            BUFF_SPACING = 5;

        // Suficiente para Season 6 y para varias filas.
        private const int
            MAX_VISIBLE_BUFFS = 32;

        // SourceMain crea CNewUIBuffWindow en Y = 15.
        private const int
            TOP_MARGIN = 15;

        private int
            _visibleBuffCount;

        private Point
            _lastVirtualSize =
                Point.Zero;

        public ActiveBuffsPanel(
            CharacterState characterState)
        {
            _characterState =
                characterState;

            AutoViewSize = false;

            Interactive = false;

            BackgroundColor =
                Color.Transparent;

            BorderColor =
                Color.Transparent;

            BorderThickness = 0;

            // =====================================================
            // CREAR SLOTS
            // =====================================================

            for (int i = 0;
                 i < MAX_VISIBLE_BUFFS;
                 i++)
            {
                var slot =
                    new BuffSlotControl
                    {
                        X = 0,
                        Y = 0,
                        Visible = false
                    };

                _buffSlots.Add(
                    slot);

                Controls.Add(
                    slot);
            }

            // =====================================================
            // EVENTO DE BUFF
            // =====================================================

            _characterState.ActiveBuffsChanged +=
                OnActiveBuffsChanged;

            UpdateBuffDisplay();

            UpdateAnchorPosition();
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);

            // Esto permite mantenerlo centrado incluso
            // al cambiar resolución, fullscreen, Android, etc.

            Point virtualSize =
                UiScaler.VirtualSize;

            if (virtualSize !=
                _lastVirtualSize)
            {
                _lastVirtualSize =
                    virtualSize;

                UpdateAnchorPosition();
            }
        }

        private void OnActiveBuffsChanged()
        {
            // El evento puede llegar desde el hilo de red.

            MuGame.ScheduleOnMainThread(
                UpdateBuffDisplay);
        }

        private void UpdateBuffDisplay()
        {
            // =====================================================
            // FILTRAR
            // =====================================================

            var buffs =
                _characterState
                    .GetActiveBuffs()
                    .Where(
                        buff =>
                            BuffIconAtlas
                                .ShouldRender(
                                    buff.EffectId))
                    .ToList();

            // =====================================================
            // ORDEN ORIGINAL
            // =====================================================
            //
            // SourceMain:
            //
            // Buff:
            //     push_front
            //
            // Debuff:
            //     push_back
            //
            // =====================================================

            var orderedBuffs =
                OrderLikeClassicClient(
                    buffs)
                    .Take(
                        MAX_VISIBLE_BUFFS)
                    .ToList();

            _visibleBuffCount =
                orderedBuffs.Count;

            // =====================================================
            // ACTUALIZAR SLOTS
            // =====================================================

            for (int i = 0;
                 i < _buffSlots.Count;
                 i++)
            {
                BuffSlotControl slot =
                    _buffSlots[i];

                if (i >=
                    _visibleBuffCount)
                {
                    slot.Buff =
                        null;

                    slot.Visible =
                        false;

                    continue;
                }

                int row =
                    i /
                    BUFFS_PER_ROW;

                int column =
                    i %
                    BUFFS_PER_ROW;

                slot.X =
                    column *
                    (
                        BuffSlotControl
                            .SLOT_WIDTH +
                        BUFF_SPACING
                    );

                slot.Y =
                    row *
                    (
                        BuffSlotControl
                            .SLOT_HEIGHT +
                        BUFF_SPACING
                    );

                slot.Buff =
                    orderedBuffs[i];

                slot.Visible =
                    true;
            }

            // =====================================================
            // TAMAÑO DEL PANEL
            // =====================================================

            if (_visibleBuffCount <= 0)
            {
                ControlSize =
                    new Point(
                        1,
                        1);

                ViewSize =
                    ControlSize;

                UpdateAnchorPosition();

                return;
            }

            int rows =
                (
                    _visibleBuffCount +
                    BUFFS_PER_ROW -
                    1
                ) /
                BUFFS_PER_ROW;

            int columns =
                Math.Min(
                    _visibleBuffCount,
                    BUFFS_PER_ROW);

            int width =
                columns *
                BuffSlotControl.SLOT_WIDTH;

            if (columns > 1)
            {
                width +=
                    (
                        columns -
                        1
                    ) *
                    BUFF_SPACING;
            }

            int height =
                rows *
                BuffSlotControl.SLOT_HEIGHT;

            if (rows > 1)
            {
                height +=
                    (
                        rows -
                        1
                    ) *
                    BUFF_SPACING;
            }

            ControlSize =
                new Point(
                    Math.Max(
                        1,
                        width),
                    Math.Max(
                        1,
                        height));

            ViewSize =
                ControlSize;

            UpdateAnchorPosition();
        }

        /// <summary>
        /// Centra horizontalmente toda la barra de buffs
        /// en la resolución virtual actual.
        /// </summary>
        private void UpdateAnchorPosition()
        {
            Point virtualSize =
                UiScaler.VirtualSize;

            int centeredX =
                (
                    virtualSize.X -
                    ViewSize.X
                ) /
                2;

            X =
                Math.Max(
                    0,
                    centeredX);

            Y =
                TOP_MARGIN;
        }

        /// <summary>
        /// Emula el orden del CNewUIBuffWindow clásico:
        ///
        /// buffs delante,
        /// debuffs al final.
        /// </summary>
        private static IEnumerable<ActiveBuffState>
            OrderLikeClassicClient(
                IReadOnlyList<ActiveBuffState>
                    buffs)
        {
            var ordered =
                new LinkedList<ActiveBuffState>();

            foreach (
                ActiveBuffState buff
                in buffs)
            {
                if (BuffIconAtlas.IsDebuff(
                        buff.EffectId))
                {
                    ordered.AddLast(
                        buff);
                }
                else
                {
                    ordered.AddFirst(
                        buff);
                }
            }

            return ordered;
        }

        public override void Dispose()
        {
            _characterState.ActiveBuffsChanged -=
                OnActiveBuffsChanged;

            base.Dispose();
        }
    }
}