#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Client.Main.Core.Client;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Barra clásica de selección de skills de MU.
    ///
    /// - Usa los skillbox originales de 32x38.
    /// - La lista crece alrededor del skill activo.
    /// - Con muchas skills crea una segunda fila.
    /// - No utiliza ventana, fondo ni título.
    /// - Click en una skill: la selecciona y cierra la barra.
    /// - Hover puede consultarse directamente para CTRL + 1...0.
    /// </summary>
    public sealed class ClassicSkillBarControl : UIControl
    {
        private readonly CharacterState
            _characterState;

        private readonly List<SkillSlotControl>
            _skillSlots =
                new();

        private readonly List<ushort>
            _loadedSkillIds =
                new();

        private SkillEntryState?
            _selectedSkill;

        /// <summary>
        /// Se dispara cuando se selecciona una skill
        /// desde la barra clásica.
        /// </summary>
        public event Action<SkillEntryState>?
            SkillSelected;

        /// <summary>
        /// Devuelve directamente la skill que está
        /// bajo el mouse/touch en este frame.
        ///
        /// No guarda estado de hover, por lo que
        /// funciona correctamente aunque la barra
        /// se cierre y vuelva a abrir.
        /// </summary>
        public SkillEntryState? HoveredSkill
        {
            get
            {
                if (!Visible)
                {
                    return null;
                }

                for (int i = 0;
                     i < _skillSlots.Count;
                     i++)
                {
                    SkillSlotControl slot =
                        _skillSlots[i];

                    if (slot.IsMouseOver &&
                        slot.Skill != null)
                    {
                        return slot.Skill;
                    }
                }

                return null;
            }
        }

        public ClassicSkillBarControl(
            CharacterState characterState)
        {
            _characterState =
                characterState;

            AutoViewSize =
                false;

            // Este control actúa solamente como
            // contenedor/ancla.
            //
            // Los slots pueden tener coordenadas X/Y
            // negativas y salir de este rectángulo.
            ControlSize =
                new Point(
                    1,
                    1);

            ViewSize =
                ControlSize;

            Interactive =
                false;

            BackgroundColor =
                Color.Transparent;

            BorderThickness =
                0;

            Visible =
                false;

            RefreshSkills();
        }

        // =============================================================
        // OPEN / CLOSE
        // =============================================================

        public void Open()
        {
            RefreshSkills();

            if (_skillSlots.Count == 0)
            {
                Visible =
                    false;

                return;
            }

            Visible =
                true;

            BringToFront();
        }

        public void Close()
        {
            Visible =
                false;
        }

        public void Toggle()
        {
            if (Visible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        // =============================================================
        // CURRENT SKILL
        // =============================================================

        public void SetSelectedSkill(
            SkillEntryState? skill)
        {
            _selectedSkill =
                skill;

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0;
                 i < _skillSlots.Count;
                 i++)
            {
                SkillSlotControl slot =
                    _skillSlots[i];

                slot.IsSelected =
                    _selectedSkill != null &&
                    slot.Skill != null &&
                    slot.Skill.SkillId ==
                    _selectedSkill.SkillId;
            }
        }

        // =============================================================
        // SKILL LIST
        // =============================================================

        private void RefreshSkills()
        {
            List<SkillEntryState> skills =
                _characterState
                    .GetSkills()
                    .Where(
                        skill =>
                            skill.SkillId != 0)
                    .OrderBy(
                        skill =>
                            skill.SkillId)
                    .ToList();

            bool sameSkillList =
                skills.Count ==
                _loadedSkillIds.Count;

            if (sameSkillList)
            {
                for (int i = 0;
                     i < skills.Count;
                     i++)
                {
                    if (_loadedSkillIds[i] !=
                        skills[i].SkillId)
                    {
                        sameSkillList =
                            false;

                        break;
                    }
                }
            }

            // Si no cambió la lista, solamente
            // refrescar las referencias.
            if (sameSkillList)
            {
                for (int i = 0;
                     i < skills.Count;
                     i++)
                {
                    _skillSlots[i].Skill =
                        skills[i];
                }

                RefreshSelection();

                return;
            }

            RebuildSkills(
                skills);
        }

        private void RebuildSkills(
            IReadOnlyList<SkillEntryState> skills)
        {
            ClearSkills();

            for (int i = 0;
                 i < skills.Count;
                 i++)
            {
                SkillEntryState skill =
                    skills[i];

                Point position =
                    GetClassicPosition(
                        i);

                var slot =
                    new SkillSlotControl
                    {
                        Skill =
                            skill,

                        X =
                            position.X,

                        Y =
                            position.Y,

                        // Tamaño clásico:
                        // 32 x 38.
                        VisualScale =
                            1.0f,

                        IsSelected =
                            _selectedSkill != null &&
                            _selectedSkill.SkillId ==
                            skill.SkillId,

                        // Dejamos tooltip activo en la
                        // barra clásica.
                        IsTooltipEnabled =
                            true
                    };

                slot.Click +=
                    (_, _) =>
                    {
                        SelectSkill(
                            skill);
                    };

                _skillSlots.Add(
                    slot);

                _loadedSkillIds.Add(
                    skill.SkillId);

                Controls.Add(
                    slot);
            }
        }

        private void ClearSkills()
        {
            // Dispose() ya elimina cada slot
            // de Controls, por eso recorremos
            // la lista al revés.
            for (int i =
                     _skillSlots.Count - 1;
                 i >= 0;
                 i--)
            {
                _skillSlots[i]
                    .Dispose();
            }

            _skillSlots.Clear();

            _loadedSkillIds.Clear();
        }

        // =============================================================
        // CLASSIC MU LAYOUT
        // =============================================================

        /// <summary>
        /// Distribución basada en CNewUISkillList
        /// del cliente clásico.
        ///
        /// Primera parte:
        ///
        /// 13 11 9 7 5 3 1 [0] 2 4 6 8 10 12
        ///
        /// Después continúa hacia la izquierda.
        /// A partir de la skill número 19 aparece
        /// una segunda fila por encima.
        ///
        /// El punto 0,0 corresponde al primer
        /// skill de la lista.
        /// </summary>
        private static Point GetClassicPosition(
            int index)
        {
            const int width =
                SkillSlotControl.SLOT_WIDTH;

            const int height =
                SkillSlotControl.SLOT_HEIGHT;

            int x;
            int y =
                0;

            // Skills 0-13:
            // alternan derecha / izquierda.
            if (index < 14)
            {
                int remainder =
                    index % 2;

                int quotient =
                    index / 2;

                if (remainder == 0)
                {
                    x =
                        quotient *
                        width;
                }
                else
                {
                    x =
                        -(quotient + 1) *
                        width;
                }
            }

            // Skills 14-17:
            // continúan extendiendo la fila
            // hacia la izquierda.
            else if (index < 18)
            {
                x =
                    -(8 * width)
                    -
                    ((index - 14) *
                     width);
            }

            // Skill 18 en adelante:
            // segunda fila.
            else
            {
                y =
                    -height;

                x =
                    -(12 * width)
                    +
                    ((index - 17) *
                     width);
            }

            return new Point(
                x,
                y);
        }

        // =============================================================
        // SELECT
        // =============================================================

        private void SelectSkill(
            SkillEntryState skill)
        {
            SetSelectedSkill(
                skill);

            SkillSelected?
                .Invoke(
                    skill);

            // En combate:
            // elegir skill y cerrar.
            Close();
        }
    }
}