#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Client.Data.BMD;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Main skill management window.
    ///
    /// Current behavior:
    /// - Shows learned skills only.
    /// - Clicking a skill selects it.
    /// - Shows skill information at the top.
    /// - Allows assigning the selected skill to quick slots 1-5.
    /// - Supports multiple pages for characters with many skills.
    ///
    /// Future:
    /// - Connect quick slots to the actual HUD hotkeys.
    /// - Show unlearned class skills using disabled icon atlases.
    /// - Optional drag & drop.
    /// </summary>
    public class SkillSelectionPanel : UIControl
    {
        private const int PANEL_WIDTH = 350;
        private const int PANEL_HEIGHT = 365;

        private const int PADDING = 12;

        private const int TITLE_HEIGHT = 34;

        private const int INFO_Y = 42;
        private const int INFO_HEIGHT = 72;

        private const int GRID_Y = 126;

        private const int COLUMNS = 6;
        private const int ROWS = 3;
        private const int PAGE_SIZE = COLUMNS * ROWS;
        private const float SKILL_GRID_SCALE = 1.30f;

        private const int GRID_GAP_X = 10;
        private const int GRID_GAP_Y = 8;

        private const int QUICK_AREA_Y = 275;
        private const int QUICK_SLOT_GAP = 14;

        private readonly List<SkillSlotControl> _skillSlots = new();

        private const int QUICK_SLOT_COUNT = 10;
        private const int QUICK_VISIBLE_COUNT = 5;

        private readonly SkillSlotControl[] _quickSlots = new SkillSlotControl[QUICK_VISIBLE_COUNT];

        private readonly LabelControl[] _quickSlotLabels = new LabelControl[QUICK_VISIBLE_COUNT];

        private readonly SkillEntryState?[] _quickSlotSkills = new SkillEntryState?[QUICK_SLOT_COUNT];

        private int _quickSlotBank;

        private SkillEntryState? _hoveredSkill;

        private List<SkillEntryState> _allSkills =
            new();

        private SkillEntryState? _selectedSkill;

        private readonly LabelControl _titleLabel;
        private readonly ButtonControl _closeButton;

        private readonly UIControl _infoPanel;
        private readonly LabelControl _skillNameLabel;
        private readonly LabelControl _skillTypeLabel;
        private readonly LabelControl _skillCostLabel;
        private readonly LabelControl _skillStatsLabel;

        private readonly ButtonControl _previousPageButton;
        private readonly ButtonControl _nextPageButton;
        private readonly LabelControl _pageLabel;

        private readonly LabelControl _quickTitleLabel;
        private readonly ButtonControl _quickBankButton;

        private int _currentPage;
        private int _pageCount = 1;

        private sealed class PanelControl : UIControl
        {
        }

        /// <summary>
        /// Fired whenever a skill is selected.
        ///
        /// SkillQuickSlot already listens to this event,
        /// so selecting a skill here also updates the
        /// current active skill.
        /// </summary>
        public event Action<SkillEntryState>? SkillSelected;

        public SkillEntryState? HoveredSkill => _hoveredSkill;

        /// <summary>
        /// Fired whenever one of the five quick slots changes.
        ///
        /// Parameters:
        /// slot index: 0-4
        /// skill: assigned skill
        /// </summary>
        public event Action<int, SkillEntryState?>? QuickSlotAssigned;

        public SkillSelectionPanel()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    PANEL_WIDTH,
                    PANEL_HEIGHT);

            ViewSize =
                ControlSize;

            Align =
                ControlAlign.HorizontalCenter |
                ControlAlign.VerticalCenter;

            Interactive = true;

            BackgroundColor =
                new Color(
                    12,
                    12,
                    16) * 0.97f;

            BorderColor =
                new Color(
                    135,
                    105,
                    45);

            BorderThickness = 2;

            Visible = false;

            // =========================================================
            // TITLE
            // =========================================================

            _titleLabel =
                new LabelControl
                {
                    Text = "SKILL",

                    TextColor =
                        new Color(
                            225,
                            205,
                            150),

                    FontSize = 16f,

                    X = 0,
                    Y = 8,

                    ViewSize =
                        new Point(
                            PANEL_WIDTH,
                            24),

                    Align =
                        ControlAlign.HorizontalCenter
                };

            Controls.Add(
                _titleLabel);

            _closeButton =
                new ButtonControl
                {
                    Text = "X",

                    FontSize = 11f,

                    X =
                        PANEL_WIDTH -
                        34,

                    Y = 6,

                    AutoViewSize = false,

                    ControlSize =
                        new Point(
                            26,
                            24),

                    ViewSize =
                        new Point(
                            26,
                            24),

                    BackgroundColor =
                        new Color(
                            40,
                            30,
                            25),

                    HoverBackgroundColor =
                        new Color(
                            90,
                            45,
                            30),

                    PressedBackgroundColor =
                        new Color(
                            110,
                            50,
                            30),

                    TextColor =
                        Color.Silver,

                    HoverTextColor =
                        Color.White
                };

            _closeButton.Click +=
                (_, _) =>
                {
                    Close();
                };

            Controls.Add(
                _closeButton);

            // =========================================================
            // SKILL INFORMATION
            // =========================================================

            _infoPanel =
                new PanelControl
                {
                    AutoViewSize = false,

                    X = PADDING,
                    Y = INFO_Y,

                    ControlSize =
                        new Point(
                            PANEL_WIDTH -
                            PADDING * 2,
                            INFO_HEIGHT),

                    ViewSize =
                        new Point(
                            PANEL_WIDTH -
                            PADDING * 2,
                            INFO_HEIGHT),

                    BackgroundColor =
                        new Color(
                            18,
                            18,
                            20) * 0.96f,

                    BorderColor =
                        new Color(
                            95,
                            80,
                            50),

                    BorderThickness = 1,

                    Interactive = false
                };

            Controls.Add(
                _infoPanel);

            _skillNameLabel =
                new LabelControl
                {
                    Text =
                        "Selecciona una skill",

                    TextColor =
                        Color.White,

                    FontSize = 13f,

                    X = 10,
                    Y = 7,

                    ViewSize =
                        new Point(
                            300,
                            20)
                };

            _infoPanel.Controls.Add(
                _skillNameLabel);

            _skillTypeLabel =
                new LabelControl
                {
                    Text =
                        string.Empty,

                    TextColor =
                        new Color(
                            215,
                            180,
                            80),

                    FontSize = 9f,

                    X = 10,
                    Y = 28,

                    ViewSize =
                        new Point(
                            145,
                            16)
                };

            _infoPanel.Controls.Add(
                _skillTypeLabel);

            _skillCostLabel =
                new LabelControl
                {
                    Text =
                        string.Empty,

                    TextColor =
                        Color.Silver,

                    FontSize = 9f,

                    X = 10,
                    Y = 47,

                    ViewSize =
                        new Point(
                            150,
                            16)
                };

            _infoPanel.Controls.Add(
                _skillCostLabel);

            _skillStatsLabel =
                new LabelControl
                {
                    Text =
                        string.Empty,

                    TextColor =
                        Color.Silver,

                    FontSize = 9f,

                    X = 165,
                    Y = 47,

                    ViewSize =
                        new Point(
                            145,
                            16)
                };

            _infoPanel.Controls.Add(
                _skillStatsLabel);

            // =========================================================
            // PAGINATION
            // =========================================================

            _previousPageButton =
                new ButtonControl
                {
                    Text = "<",

                    FontSize = 11f,

                    AutoViewSize = false,

                    ControlSize =
                        new Point(
                            28,
                            22),

                    ViewSize =
                        new Point(
                            28,
                            22),

                    X = 105,
                    Y = 242,

                    BackgroundColor =
                        new Color(
                            30,
                            28,
                            24),

                    HoverBackgroundColor =
                        new Color(
                            70,
                            55,
                            30),

                    TextColor =
                        Color.Silver,

                    HoverTextColor =
                        Color.White
                };

            _previousPageButton.Click +=
                (_, _) =>
                {
                    ChangePage(
                        _currentPage - 1);
                };

            Controls.Add(
                _previousPageButton);

            _nextPageButton =
                new ButtonControl
                {
                    Text = ">",

                    FontSize = 11f,

                    AutoViewSize = false,

                    ControlSize =
                        new Point(
                            28,
                            22),

                    ViewSize =
                        new Point(
                            28,
                            22),

                    X = 217,
                    Y = 242,

                    BackgroundColor =
                        new Color(
                            30,
                            28,
                            24),

                    HoverBackgroundColor =
                        new Color(
                            70,
                            55,
                            30),

                    TextColor =
                        Color.Silver,

                    HoverTextColor =
                        Color.White
                };

            _nextPageButton.Click +=
                (_, _) =>
                {
                    ChangePage(
                        _currentPage + 1);
                };

            Controls.Add(
                _nextPageButton);

            _pageLabel =
                new LabelControl
                {
                    Text = "1 / 1",

                    TextColor =
                        new Color(
                            190,
                            165,
                            95),

                    FontSize = 9f,

                    X = 155,
                    Y = 246,

                    ViewSize =
                        new Point(
                            50,
                            16),

                    Align =
                        ControlAlign.HorizontalCenter
                };

            Controls.Add(
                _pageLabel);

            // =========================================================
            // QUICK SLOTS
            // =========================================================

            _quickTitleLabel =
                new LabelControl
                {
                    Text =
                        "ACCESOS RAPIDOS",

                    TextColor =
                        new Color(
                            185,
                            165,
                            110),

                    FontSize = 9f,

                    X = PADDING,
                    Y = QUICK_AREA_Y,

                    ViewSize =
                        new Point(
                            PANEL_WIDTH -
                            PADDING * 2,
                            18),

                    Align =
                        ControlAlign.HorizontalCenter
                };

            Controls.Add(
                _quickTitleLabel);

            CreateQuickSlots();
            _quickBankButton =
            new ButtonControl
            {
                Text = ">",

                FontSize = 11f,

                AutoViewSize = false,

                ControlSize =
                    new Point(
                        26,
                        30),

                ViewSize =
                    new Point(
                        26,
                        30),

                X =
                    PANEL_WIDTH - 38,

                Y =
                    QUICK_AREA_Y + 27,

                BackgroundColor =
                    new Color(
                        30,
                        28,
                        24),

                HoverBackgroundColor =
                    new Color(
                        70,
                        55,
                        30),

                PressedBackgroundColor =
                    new Color(
                        90,
                        65,
                        30),

                TextColor =
                    Color.Silver,

                HoverTextColor =
                    Color.White
            };

        _quickBankButton.Click +=
            (_, _) =>
            {
                ToggleQuickSlotBank();
            };

        Controls.Add(
            _quickBankButton);
        }
        private int GetQuickSlotRealIndex(
            int visualIndex)
        {
            return
                (_quickSlotBank *
                QUICK_VISIBLE_COUNT)
                +
                visualIndex;
        }

        private string GetQuickSlotLabel(
            int visualIndex)
        {
            int realIndex =
                GetQuickSlotRealIndex(
                    visualIndex);

            return realIndex switch
            {
                0 => "1",
                1 => "2",
                2 => "3",
                3 => "4",
                4 => "5",
                5 => "6",
                6 => "7",
                7 => "8",
                8 => "9",
                9 => "0",
                _ => string.Empty
            };
        }
        private void RefreshQuickSlotBank()
        {
            for (int visualIndex = 0;
                visualIndex < QUICK_VISIBLE_COUNT;
                visualIndex++)
            {
                int realIndex =
                    GetQuickSlotRealIndex(
                        visualIndex);

                _quickSlots[visualIndex].Skill =
                    _quickSlotSkills[
                        realIndex];

                _quickSlotLabels[visualIndex].Text =
                    GetQuickSlotLabel(
                        visualIndex);
            }
        }
        public void ToggleQuickSlotBank()
        {
            _quickSlotBank =
                _quickSlotBank == 0
                    ? 1
                    : 0;

            RefreshQuickSlotBank();

            // Evita que el botón quede visualmente "pegado"
            // después de cambiar de banco.
            if (_quickBankButton != null)
            {
                _quickBankButton.IsMousePressed = false;
                _quickBankButton.IsMouseOver = false;

                _quickBankButton.Text =
                    _quickSlotBank == 0
                        ? ">"
                        : "<";
            }
        }

        public void SetQuickSlotBank(
            int bank)
        {
            _quickSlotBank =
                bank <= 0
                    ? 0
                    : 1;

            RefreshQuickSlotBank();

            if (_quickBankButton != null)
            {
                _quickBankButton.IsMousePressed = false;
                _quickBankButton.IsMouseOver = false;

                _quickBankButton.Text =
                    _quickSlotBank == 0
                        ? ">"
                        : "<";
            }
        }
        // =============================================================
        // OPEN / CLOSE
        // =============================================================

        public void Open(
            CharacterState characterState)
        {
            if (characterState == null)
                return;

            _allSkills =
                characterState
                    .GetSkills()
                    .OrderBy(
                        skill =>
                            skill.SkillId)
                    .ToList();

            _pageCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        _allSkills.Count /
                        (float)PAGE_SIZE));

            _currentPage =
                Math.Clamp(
                    _currentPage,
                    0,
                    _pageCount - 1);

            // If no skill has been selected yet,
            // use the first learned skill.
            if (_selectedSkill == null)
            {
                _selectedSkill =
                    _allSkills
                        .FirstOrDefault();
            }
            else
            {
                // Refresh the reference in case
                // the server updated the skill list.
                _selectedSkill =
                    _allSkills
                        .FirstOrDefault(
                            skill =>
                                skill.SkillId ==
                                _selectedSkill.SkillId)
                    ??
                    _allSkills.FirstOrDefault();
            }

            // Open on the page containing
            // the currently selected skill.
            if (_selectedSkill != null)
            {
                int selectedIndex =
                    _allSkills.FindIndex(
                        skill =>
                            skill.SkillId ==
                            _selectedSkill.SkillId);

                if (selectedIndex >= 0)
                {
                    _currentPage =
                        selectedIndex /
                        PAGE_SIZE;
                }
            }

            RebuildSkillGrid();

            UpdateSkillInfo(
                _selectedSkill);

            Visible = true;
            BringToFront();
        }

        public void Close()
        {
            Visible = false;
        }

        // =============================================================
        // SKILL GRID
        // =============================================================

        private void RebuildSkillGrid()
        {
            ClearSkillGrid();

            List<SkillEntryState> pageSkills =
                _allSkills
                    .Skip(
                        _currentPage *
                        PAGE_SIZE)
                    .Take(
                        PAGE_SIZE)
                    .ToList();

            int scaledSlotWidth =
                (int)(
                    SkillSlotControl.SLOT_WIDTH *
                    SKILL_GRID_SCALE);

            int gridWidth =
                COLUMNS *
                scaledSlotWidth
                +
                (COLUMNS - 1) *
                GRID_GAP_X;

            int startX =
                (PANEL_WIDTH -
                 gridWidth) / 2;

            for (int i = 0;
                 i < pageSkills.Count;
                 i++)
            {
                int row =
                    i / COLUMNS;

                int column =
                    i % COLUMNS;

                SkillEntryState skill =
                    pageSkills[i];

                var slot = new SkillSlotControl
                    {
                        Skill = skill,

                        VisualScale = SKILL_GRID_SCALE,

                        X =
                            startX +
                            column *
                            (
                                (int)(
                                    SkillSlotControl.SLOT_WIDTH *
                                    SKILL_GRID_SCALE)
                                +
                                GRID_GAP_X
                            ),

                        Y =
                            GRID_Y +
                            row *
                            (
                                (int)(
                                    SkillSlotControl.SLOT_HEIGHT *
                                    SKILL_GRID_SCALE)
                                +
                                GRID_GAP_Y
                            ),

                        IsTooltipEnabled =
                            false,

                        IsSelected =
                            _selectedSkill != null &&
                            _selectedSkill.SkillId ==
                            skill.SkillId
                    };

                slot.Click +=
                    (_, _) =>
                    {
                        SelectSkill(
                            skill);
                    };

                slot.HoverChanged +=
                    OnSkillHover;

                _skillSlots.Add(
                    slot);

                Controls.Add(
                    slot);
            }

            bool multiplePages =
                _pageCount > 1;

            _previousPageButton.Visible =
                multiplePages;

            _nextPageButton.Visible =
                multiplePages;

            _pageLabel.Visible =
                multiplePages;

            _previousPageButton.Enabled =
                _currentPage > 0;

            _nextPageButton.Enabled =
                _currentPage <
                _pageCount - 1;

            _pageLabel.Text =
                $"{_currentPage + 1} / {_pageCount}";
        }

        private void ClearSkillGrid()
        {
            foreach (
                SkillSlotControl slot
                in _skillSlots)
            {
                slot.HoverChanged -=
                    OnSkillHover;

                Controls.Remove(
                    slot);
            }

            _skillSlots.Clear();
        }

        private void SelectSkill(
            SkillEntryState skill)
        {
            _selectedSkill =
                skill;

            foreach (
                SkillSlotControl slot
                in _skillSlots)
            {
                slot.IsSelected =
                    slot.Skill?.SkillId ==
                    skill.SkillId;
            }

            UpdateSkillInfo(
                skill);

            // Keep current active skill behavior.
            SkillSelected?.Invoke(
                skill);
        }

        private void OnSkillHover(
            SkillEntryState? skill)
        {
            _hoveredSkill = skill;

            if (skill != null)
            {
                UpdateSkillInfo(skill);
                return;
            }

            UpdateSkillInfo(_selectedSkill);
        }

        // =============================================================
        // SKILL INFO
        // =============================================================

        private void UpdateSkillInfo(
            SkillEntryState? skill)
        {
            if (skill == null)
            {
                _skillNameLabel.Text =
                    "Sin skills";

                _skillTypeLabel.Text =
                    string.Empty;

                _skillCostLabel.Text =
                    string.Empty;

                _skillStatsLabel.Text =
                    string.Empty;

                return;
            }

            SkillBMD? definition =
                SkillDatabase
                    .GetSkillDefinition(
                        skill.SkillId);

            SkillType type =
                SkillDatabase
                    .GetSkillType(
                        skill.SkillId);

            string typeText =
                type switch
                {
                    SkillType.Area =>
                        "Area",

                    SkillType.Self =>
                        "Self",

                    _ =>
                        "Target"
                };

            _skillNameLabel.Text =
                SkillDatabase.GetSkillName(
                    skill.SkillId);

            _skillTypeLabel.Text =
                typeText;

            if (definition == null)
            {
                _skillCostLabel.Text =
                    string.Empty;

                _skillStatsLabel.Text =
                    string.Empty;

                return;
            }

            string costText =
                string.Empty;

            if (definition.ManaCost > 0)
            {
                costText =
                    $"Mana: {definition.ManaCost}";
            }

            if (definition.AbilityGaugeCost > 0)
            {
                if (!string.IsNullOrEmpty(
                    costText))
                {
                    costText +=
                        "   ";
                }

                costText +=
                    $"AG: {definition.AbilityGaugeCost}";
            }

            _skillCostLabel.Text =
                costText;

            string statsText =
                string.Empty;

            if (definition.Damage > 0)
            {
                statsText =
                    $"Daño: {definition.Damage}";
            }

            if (definition.Distance > 0)
            {
                if (!string.IsNullOrEmpty(
                    statsText))
                {
                    statsText +=
                        "   ";
                }

                statsText +=
                    $"Rango: {definition.Distance}";
            }

            _skillStatsLabel.Text =
                statsText;
        }

        // =============================================================
        // QUICK SLOTS
        // =============================================================

        private void CreateQuickSlots()
        {
            int totalWidth =
                QUICK_VISIBLE_COUNT *
                SkillSlotControl.SLOT_WIDTH
                +
                (QUICK_VISIBLE_COUNT - 1) *
                QUICK_SLOT_GAP;

            int startX =
                (PANEL_WIDTH -
                totalWidth) / 2;

            for (int i = 0;
                i < QUICK_VISIBLE_COUNT;
                i++)
            {
                int visualSlotIndex =
                    i;

                var quickSlot =
                    new SkillSlotControl
                    {
                        X =
                            startX +
                            i *
                            (
                                SkillSlotControl.SLOT_WIDTH +
                                QUICK_SLOT_GAP
                            ),

                        Y =
                            QUICK_AREA_Y +
                            24,

                        Skill = null,

                        IsSelected = false,

                        IsTooltipEnabled = false
                    };

                quickSlot.Click +=
                    (_, _) =>
                    {
                        int realSlotIndex =
                            GetQuickSlotRealIndex(
                                visualSlotIndex);

                        AssignSelectedSkillToQuickSlot(
                            realSlotIndex);
                    };

                _quickSlots[i] =
                    quickSlot;

                Controls.Add(
                    quickSlot);

                var numberLabel =
                new LabelControl
                {
                    Text =
                        GetQuickSlotLabel(i),

                    TextColor =
                        new Color(
                            220,
                            195,
                            120),

                    FontSize = 10f,

                    X =
                        quickSlot.X,

                    Y =
                        quickSlot.Y +
                        SkillSlotControl.SLOT_HEIGHT +
                        1,

                    ViewSize =
                        new Point(
                            SkillSlotControl.SLOT_WIDTH,
                            14),

                    TextAlign =
                        HorizontalAlign.Center
                };

                _quickSlotLabels[i] =
                    numberLabel;

                Controls.Add(
                    numberLabel);
            }

            RefreshQuickSlotBank();
        }

        private void AssignSelectedSkillToQuickSlot(
            int slotIndex)
        {
            if (_selectedSkill == null)
                return;

            AssignSkillToQuickSlot(
                slotIndex,
                _selectedSkill);
        }
        public void AssignSkillToQuickSlot(
            int slotIndex,
            SkillEntryState skill)
        {
            if (skill == null)
                return;

            if (slotIndex < 0 ||
                slotIndex >= QUICK_SLOT_COUNT)
            {
                return;
            }

            // ---------------------------------------------------------
            // Remove the same skill from any previous hotkey.
            // ---------------------------------------------------------

            for (int i = 0;
                i < QUICK_SLOT_COUNT;
                i++)
            {
                if (i == slotIndex)
                    continue;

                if (_quickSlotSkills[i]?.SkillId ==
                    skill.SkillId)
                {
                    _quickSlotSkills[i] = null;

                    // IMPORTANT:
                    // Tell external controls that this old slot
                    // has been cleared.
                    QuickSlotAssigned?.Invoke(
                        i,
                        null);
                }
            }

            // ---------------------------------------------------------
            // Assign to new hotkey.
            // ---------------------------------------------------------

            _quickSlotSkills[slotIndex] =
                skill;

            RefreshQuickSlotBank();

            // Notify HUD / external controls.
            QuickSlotAssigned?.Invoke(
                slotIndex,
                skill);
        }

        /// <summary>
        /// Allows another control to initialize or refresh
        /// one of the five quick slots later.
        /// </summary>
        public void SetQuickSlot(
            int slotIndex,
            SkillEntryState? skill)
        {
            if (slotIndex < 0 ||
                slotIndex >= QUICK_SLOT_COUNT)
            {
                return;
            }

            _quickSlotSkills[slotIndex] =
                skill;

            RefreshQuickSlotBank();
        }

        // =============================================================
        // PAGE MANAGEMENT
        // =============================================================

        private void ChangePage(
            int newPage)
        {
            newPage =
                Math.Clamp(
                    newPage,
                    0,
                    _pageCount - 1);

            if (newPage ==
                _currentPage)
            {
                return;
            }

            _currentPage =
                newPage;

            RebuildSkillGrid();
        }
        // =============================================================
        // EXISTING API
        // =============================================================

        public void HighlightSkill(
            ushort skillId)
        {
            SkillEntryState? skill =
                _allSkills
                    .FirstOrDefault(
                        entry =>
                            entry.SkillId ==
                            skillId);

            if (skill == null)
            {
                // The panel may not have been opened yet.
                // Keep only the visual update for existing slots.
                foreach (
                    SkillSlotControl slot
                    in _skillSlots)
                {
                    slot.IsSelected =
                        slot.Skill?.SkillId ==
                        skillId;
                }

                return;
            }

            _selectedSkill =
                skill;

            foreach (
                SkillSlotControl slot
                in _skillSlots)
            {
                slot.IsSelected =
                    slot.Skill?.SkillId ==
                    skillId;
            }

            UpdateSkillInfo(
                skill);
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);
        }
    }
}