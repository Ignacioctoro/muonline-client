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
    /// Supports independent visual scaling so menus can use
    /// larger icons without affecting quick-slot sizes.
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

        private float _visualScale = 1f;

        // =============================================================
        // ORIGINAL MU SIZES
        // =============================================================

        public const int SLOT_WIDTH = 32;
        public const int SLOT_HEIGHT = 38;

        private const int ICON_WIDTH = 20;
        private const int ICON_HEIGHT = 28;

        private const int ICON_OFFSET_X = 6;
        private const int ICON_OFFSET_Y = 6;

        // =============================================================
        // PROPERTIES
        // =============================================================

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

        /// <summary>
        /// Scales the actual frame + skill icon.
        ///
        /// 1.0 = original MU size.
        /// 1.25 = 25% larger.
        ///
        /// This is intentionally separate from GameControl.Scale,
        /// because normal Scale only changes the outer control size.
        /// </summary>
        public float VisualScale
        {
            get => _visualScale;

            set
            {
                _visualScale =
                    Math.Max(
                        0.5f,
                        value);

                ApplyVisualScale();
            }
        }

        public bool IsTooltipEnabled { get; set; } = true;

        public event Action<SkillEntryState?>? HoverChanged;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public SkillSlotControl()
        {
            AutoViewSize = false;

            ControlSize =
                new Point(
                    SLOT_WIDTH,
                    SLOT_HEIGHT);

            ViewSize =
                ControlSize;

            Interactive = true;

            BackgroundColor =
                Color.Transparent;

            BorderThickness = 0;

            // =========================================================
            // SLOT FRAME
            // =========================================================

            _slotFrame =
                new TextureControl
                {
                    TexturePath =
                        SLOT_TEXTURE_NORMAL,

                    AutoViewSize = false,

                    X = 0,
                    Y = 0,

                    ControlSize =
                        new Point(
                            SLOT_WIDTH,
                            SLOT_HEIGHT),

                    ViewSize =
                        new Point(
                            SLOT_WIDTH,
                            SLOT_HEIGHT),

                    Interactive = false
                };

            Controls.Add(
                _slotFrame);

            // =========================================================
            // SKILL ICON
            // =========================================================

            _skillIcon =
                new TextureControl
                {
                    AutoViewSize = false,

                    X =
                        ICON_OFFSET_X,

                    Y =
                        ICON_OFFSET_Y,

                    ControlSize =
                        new Point(
                            ICON_WIDTH,
                            ICON_HEIGHT),

                    ViewSize =
                        new Point(
                            ICON_WIDTH,
                            ICON_HEIGHT),

                    Visible = false,
                    Interactive = false
                };

            Controls.Add(
                _skillIcon);

            // =========================================================
            // FALLBACK
            // =========================================================

            _fallbackLabel =
                new LabelControl
                {
                    Text = "?",

                    TextColor =
                        Color.White,

                    FontSize = 10f,

                    X = 0,
                    Y = 10,

                    ViewSize =
                        new Point(
                            SLOT_WIDTH,
                            16),

                    TextAlign =
                        HorizontalAlign.Center,

                    Visible = false
                };

            Controls.Add(
                _fallbackLabel);

            // =========================================================
            // TOOLTIP
            // =========================================================

            _tooltipLabel =
                new LabelControl
                {
                    Text =
                        string.Empty,

                    TextColor =
                        Color.White,

                    BackgroundColor =
                        new Color(
                            0,
                            0,
                            0) * 0.92f,

                    BorderColor =
                        new Color(
                            170,
                            140,
                            60),

                    BorderThickness = 1,

                    X =
                        SLOT_WIDTH + 6,

                    Y = 0,

                    ViewSize =
                        new Point(
                            210,
                            90),

                    Visible = false,

                    FontSize = 10f
                };

            Controls.Add(
                _tooltipLabel);

            ApplyVisualScale();
            UpdateDisplay();
        }

        // =============================================================
        // VISUAL SCALE
        // =============================================================

        private void ApplyVisualScale()
        {
            int slotWidth =
                (int)MathF.Round(
                    SLOT_WIDTH *
                    _visualScale);

            int slotHeight =
                (int)MathF.Round(
                    SLOT_HEIGHT *
                    _visualScale);

            int iconWidth =
                (int)MathF.Round(
                    ICON_WIDTH *
                    _visualScale);

            int iconHeight =
                (int)MathF.Round(
                    ICON_HEIGHT *
                    _visualScale);

            int iconOffsetX =
                (int)MathF.Round(
                    ICON_OFFSET_X *
                    _visualScale);

            int iconOffsetY =
                (int)MathF.Round(
                    ICON_OFFSET_Y *
                    _visualScale);

            // Outer clickable area
            ControlSize =
                new Point(
                    slotWidth,
                    slotHeight);

            ViewSize =
                ControlSize;

            // Frame
            _slotFrame.ControlSize =
                new Point(
                    slotWidth,
                    slotHeight);

            _slotFrame.ViewSize =
                _slotFrame.ControlSize;

            // Actual skill icon
            _skillIcon.X =
                iconOffsetX;

            _skillIcon.Y =
                iconOffsetY;

            _skillIcon.ControlSize =
                new Point(
                    iconWidth,
                    iconHeight);

            _skillIcon.ViewSize =
                _skillIcon.ControlSize;

            // Fallback label
            _fallbackLabel.X = 0;

            _fallbackLabel.Y =
                Math.Max(
                    0,
                    (slotHeight - 16) / 2);

            _fallbackLabel.ViewSize =
                new Point(
                    slotWidth,
                    16);

            // Tooltip starts after enlarged slot
            _tooltipLabel.X =
                slotWidth + 6;
        }

        // =============================================================
        // DISPLAY
        // =============================================================

        private void UpdateDisplay()
        {
            if (_skill == null)
            {
                _skillIcon.Visible =
                    false;

                _fallbackLabel.Visible =
                    false;

                _tooltipLabel.Visible =
                    false;

                UpdateVisualState();

                return;
            }

            var iconInfo =
                SkillIconDatabase.GetIcon(
                    _skill.SkillId);

            if (iconInfo.HasValue)
            {
                var icon =
                    iconInfo.Value;

                _skillIcon.TexturePath =
                    icon.TexturePath;

                _skillIcon.TextureRectangle =
                    icon.SourceRectangle;

                _skillIcon.Visible =
                    true;

                _fallbackLabel.Visible =
                    false;
            }
            else
            {
                _skillIcon.Visible =
                    false;

                _fallbackLabel.Text =
                    _skill.SkillId
                        .ToString();

                _fallbackLabel.Visible =
                    true;
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

        // =============================================================
        // UPDATE / HOVER
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);

            bool isHovered =
                IsMouseOver;

            if (!IsTooltipEnabled)
            {
                _tooltipLabel.Visible =
                    false;
            }
            else if (
                isHovered &&
                _skill != null)
            {
                RenderTooltip();
            }
            else
            {
                _tooltipLabel.Visible =
                    false;
            }

            if (_wasHovered !=
                isHovered)
            {
                _wasHovered =
                    isHovered;

                HoverChanged?.Invoke(
                    isHovered
                        ? _skill
                        : null);
            }
        }

        // =============================================================
        // TOOLTIP
        // =============================================================

        private void RenderTooltip()
        {
            if (_skill == null)
            {
                _tooltipLabel.Visible =
                    false;

                return;
            }

            var skillDef =
                SkillDatabase
                    .GetSkillDefinition(
                        _skill.SkillId);

            string skillName =
                SkillDatabase
                    .GetSkillName(
                        _skill.SkillId);

            ushort manaCost =
                SkillDatabase
                    .GetSkillManaCost(
                        _skill.SkillId);

            ushort agCost =
                SkillDatabase
                    .GetSkillAGCost(
                        _skill.SkillId);

            var skillType =
                SkillDatabase
                    .GetSkillType(
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

            string tooltip =
                skillName;

            if (!string.IsNullOrEmpty(
                typeText))
            {
                tooltip +=
                    $"\nType: {typeText}";
            }

            if (manaCost > 0)
            {
                tooltip +=
                    $"\nMana: {manaCost}";
            }

            if (agCost > 0)
            {
                tooltip +=
                    $"  AG: {agCost}";
            }

            if (skillDef != null)
            {
                if (skillDef.Damage > 0)
                {
                    tooltip +=
                        $"\nDamage: {skillDef.Damage}";
                }

                if (skillDef.Distance > 0)
                {
                    tooltip +=
                        $"  Range: {skillDef.Distance}";
                }
            }

            _tooltipLabel.Text =
                tooltip;

            _tooltipLabel.Visible =
                true;
        }
    }
}