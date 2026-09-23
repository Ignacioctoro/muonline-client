#nullable enable
using System.Linq;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Main skill quick slot displayed in the center of the screen.
    /// Shows the currently selected skill and opens the skill selection panel on click.
    /// </summary>
    public class SkillQuickSlot : UIControl
    {
        private readonly CharacterState _characterState;
        private SkillSelectionPanel? _selectionPanel;
        private readonly SkillSlotControl _currentSkillSlot;
        // private readonly LabelControl _hintLabel;

        private const int SLOT_SIZE = 52; // Compact size

        public SkillEntryState? SelectedSkill { get; private set; }

        public SkillQuickSlot(CharacterState characterState)
        {
            _characterState = characterState;

            // Position at bottom-center of screen - closer to edge
            Align = ControlAlign.HorizontalCenter | ControlAlign.Bottom;
            Margin = new Margin { Bottom = 30 }; // Much closer to bottom

            ViewSize = new Point(SLOT_SIZE + 8, SLOT_SIZE + 22); // Compact
            Interactive = true;

            // Current skill display
            float slotScale =
                SLOT_SIZE /
                (float)SkillSlotControl.SLOT_HEIGHT;

            int scaledWidth =
                (int)(
                    SkillSlotControl.SLOT_WIDTH *
                    slotScale);

            int scaledHeight =
                (int)(
                    SkillSlotControl.SLOT_HEIGHT *
                    slotScale);

            // Fine tuning for the center hole of the HUD.
            // Negative X = move left
            // Positive Y = move down
            const int HORIZONTAL_NUDGE = -20;
            const int VERTICAL_NUDGE = 20;

            _currentSkillSlot = new SkillSlotControl
            {
                IsSelected = false,
                Skill = null,
                Scale = slotScale
            };

            _currentSkillSlot.X =
                ((ViewSize.X - scaledWidth) / 2) +
                HORIZONTAL_NUDGE;

            _currentSkillSlot.Y =
                ((SLOT_SIZE - scaledHeight) / 2) +
                VERTICAL_NUDGE;

            Controls.Add(_currentSkillSlot);

            // Hint label - compact
            // _hintLabel = new LabelControl
            // {
            //     Text = "",
            //     TextColor = Color.Gray,
            //     X = 0,
            //     Y = SLOT_SIZE + 4,
            //     ViewSize = new Point(SLOT_SIZE + 8, 16),
            //     Scale = 0.65f
            // };
            // Controls.Add(_hintLabel);

            // Visual feedback - minimal background
            BackgroundColor = Color.Transparent;
            BorderColor = Color.Transparent;
            BorderThickness = 0;

            var defaultSkill = _characterState.GetSkills().FirstOrDefault();
            if (defaultSkill != null)
            {
                OnSkillSelectedFromPanel(defaultSkill);
            }
        }

        /// <summary>
        /// Connects the selection panel (must be called from parent scene).
        /// </summary>
        public void SetSelectionPanel(SkillSelectionPanel panel)
        {
            if (_selectionPanel != null)
            {
                _selectionPanel.SkillSelected -= OnSkillSelectedFromPanel;
            }

            _selectionPanel = panel;
            _selectionPanel.SkillSelected += OnSkillSelectedFromPanel;

            if (SelectedSkill != null)
            {
                _selectionPanel.HighlightSkill(SelectedSkill.SkillId);
            }
        }

        private void OnSkillSelectedFromPanel(SkillEntryState skill)
        {
            SelectedSkill = skill;

            // IMPORTANT: Update the skill slot's Skill property!
            _currentSkillSlot.Skill = skill;

            // Force display update
            _currentSkillSlot.IsSelected = false;

            if (skill != null)
            {
                string skillName = SkillDatabase.GetSkillName(skill.SkillId);
                // _hintLabel.Text = $"{skillName} Lv{skill.SkillLevel}";
                // _hintLabel.TextColor = Color.Gold;
                _selectionPanel?.HighlightSkill(skill.SkillId);
            }
            else
            {
                // _hintLabel.Text = "";
                // _hintLabel.TextColor = Color.Gray;
            }
        }
        public void SelectSkill(
            SkillEntryState skill)
        {
            if (skill == null)
                return;

            OnSkillSelectedFromPanel(
                skill);
        }

        public override bool OnClick()
        {
            base.OnClick();

            // Toggle panel (only if panel is connected)
            if (_selectionPanel != null)
            {
                if (_selectionPanel.Visible)
                {
                    _selectionPanel.Close();
                }
                else
                {
                    _selectionPanel.Open(_characterState);
                }
            }

            return true; // Handled
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Hover effect on hint only
            bool panelVisible = _selectionPanel?.Visible ?? false;

            if (IsMouseOver && !panelVisible)
            {
                if (SelectedSkill != null)
                {
                    // _hintLabel.TextColor = Color.Yellow;
                }
            }
            else
            {
                if (SelectedSkill != null)
                {
                    // _hintLabel.TextColor = Color.Gold;
                }
            }
        }
    }
}
