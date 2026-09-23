#nullable enable

using System;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Controls.UI.Game.Skills
{
    /// <summary>
    /// Single skill slot using the original MU skill box art.
    /// </summary>
    public class SkillSlotControl : UIControl
    {
        private const string SLOT_TEXTURE_NORMAL =
            "Interface/newui_skillbox.OZJ";

        private const string SLOT_TEXTURE_SELECTED =
            "Interface/newui_skillbox2.OZJ";

        private SkillEntryState? _skill;

        private readonly TextureControl _slotFrame;
        private readonly TextureControl _skillIcon;
        private readonly LabelControl _fallbackLabel;
        private readonly LabelControl _tooltipLabel;

        private bool _isSelected;
        private bool _wasHovered;

        // Original MU slot size.
        public const int SLOT_WIDTH = 32;
        public const int SLOT_HEIGHT = 38;

        // Original icon size inside the box.
        private const int ICON_WIDTH = 20;
        private const int ICON_HEIGHT = 28;

        // Original client draws around x+6, y+6.
        private const int ICON_OFFSET_X = 6;
        private const int ICON_OFFSET_Y = 6;

        public SkillEntryState? Skill
        {
            get => _skill;
            set
            {
                _skill = value;
                UpdateDisplay();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        public bool IsTooltipEnabled { get; set; } = true;

        public event Action<SkillEntryState?>? HoverChanged;

        public SkillSlotControl()
        {
            AutoViewSize = false;
            ControlSize = new Point(
                SLOT_WIDTH,
                SLOT_HEIGHT);
            ViewSize = ControlSize;

            Interactive = true;
            BackgroundColor = Color.Transparent;
            BorderThickness = 0;

            // ---------------------------------------------------------
            // SLOT FRAME
            // ---------------------------------------------------------
            _slotFrame = new TextureControl
            {
                TexturePath = SLOT_TEXTURE_NORMAL,
                AutoViewSize = false,
                X = 0,
                Y = 0,
                ControlSize = new Point(
                    SLOT_WIDTH,
                    SLOT_HEIGHT),
                ViewSize = new Point(
                    SLOT_WIDTH,
                    SLOT_HEIGHT),
                Interactive = false
            };
            Controls.Add(_slotFrame);

            // ---------------------------------------------------------
            // ICON
            // ---------------------------------------------------------
            _skillIcon = new TextureControl
            {
                AutoViewSize = false,
                X = ICON_OFFSET_X,
                Y = ICON_OFFSET_Y,
                ControlSize = new Point(
                    ICON_WIDTH,
                    ICON_HEIGHT),
                ViewSize = new Point(
                    ICON_WIDTH,
                    ICON_HEIGHT),
                Visible = false,
                Interactive = false
            };
            Controls.Add(_skillIcon);

            // ---------------------------------------------------------
            // FALLBACK TEXT
            // Used only when a skill icon mapping does not exist yet.
            // ---------------------------------------------------------
            _fallbackLabel = new LabelControl
            {
                Text = "?",
                TextColor = Color.White,
                FontSize = 10f,
                X = 0,
                Y = 10,
                ViewSize = new Point(
                    SLOT_WIDTH,
                    16),
                Align = ControlAlign.HorizontalCenter,
                Visible = false
            };
            Controls.Add(_fallbackLabel);

            // ---------------------------------------------------------
            // TOOLTIP
            // ---------------------------------------------------------
            _tooltipLabel = new LabelControl
            {
                Text = string.Empty,
                TextColor = Color.White,
                BackgroundColor = new Color(0, 0, 0) * 0.92f,
                BorderColor = new Color(170, 140, 60),
                BorderThickness = 1,
                X = SLOT_WIDTH + 6,
                Y = 0,
                ViewSize = new Point(210, 90),
                Visible = false,
                FontSize = 10f
            };
            Controls.Add(_tooltipLabel);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_skill == null)
            {
                _skillIcon.Visible = false;
                _fallbackLabel.Visible = false;
                _tooltipLabel.Visible = false;

                UpdateVisualState();
                return;
            }

            var iconInfo =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId);

            if (iconInfo.HasValue)
            {
                var icon = iconInfo.Value;

                _skillIcon.TexturePath =
                    icon.TexturePath;

                _skillIcon.TextureRectangle =
                    icon.SourceRectangle;

                _skillIcon.Visible = true;
                _fallbackLabel.Visible = false;
            }
            else
            {
                _skillIcon.Visible = false;

                _fallbackLabel.Text =
                    _skill.SkillId.ToString();

                _fallbackLabel.Visible = true;
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _slotFrame.TexturePath =
                _isSelected
                    ? SLOT_TEXTURE_SELECTED
                    : SLOT_TEXTURE_NORMAL;
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            bool isHovered = IsMouseOver;

            if (!IsTooltipEnabled)
            {
                _tooltipLabel.Visible = false;
            }
            else if (isHovered && _skill != null)
            {
                RenderTooltip();
            }
            else
            {
                _tooltipLabel.Visible = false;
            }

            if (_wasHovered != isHovered)
            {
                _wasHovered = isHovered;

                HoverChanged?.Invoke(
                    isHovered
                        ? _skill
                        : null);
            }
        }

        private void RenderTooltip()
        {
            if (_skill == null)
            {
                _tooltipLabel.Visible = false;
                return;
            }

            var skillDef =
                SkillDatabase.GetSkillDefinition(
                    _skill.SkillId);

            string skillName =
                SkillDatabase.GetSkillName(
                    _skill.SkillId);

            ushort manaCost =
                SkillDatabase.GetSkillManaCost(
                    _skill.SkillId);

            ushort agCost =
                SkillDatabase.GetSkillAGCost(
                    _skill.SkillId);

            var skillType =
                SkillDatabase.GetSkillType(
                    _skill.SkillId);

            string typeText =
                skillType switch
                {
                    Client.Data.BMD.SkillType.Area =>
                        "Area",

                    Client.Data.BMD.SkillType.Target =>
                        "Target",

                    Client.Data.BMD.SkillType.Self =>
                        "Self",

                    _ =>
                        string.Empty
                };

            string tooltip = skillName;

            if (!string.IsNullOrEmpty(typeText))
            {
                tooltip += $"\nType: {typeText}";
            }

            if (manaCost > 0)
            {
                tooltip += $"\nMana: {manaCost}";
            }

            if (agCost > 0)
            {
                tooltip += $"  AG: {agCost}";
            }

            if (skillDef != null)
            {
                if (skillDef.Damage > 0)
                {
                    tooltip += $"\nDamage: {skillDef.Damage}";
                }

                if (skillDef.Distance > 0)
                {
                    tooltip += $"  Range: {skillDef.Distance}";
                }
            }

            _tooltipLabel.Text = tooltip;
            _tooltipLabel.Visible = true;
        }
    }
}