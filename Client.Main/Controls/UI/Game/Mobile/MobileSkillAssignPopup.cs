#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Mobile
{
    public sealed class MobileSkillAssignPopup : UIControl
    {
        private const int PopupWidth =
            280;

        private const int HeaderHeight =
            34;

        private const int RowHeight =
            36;

        private const int Padding =
            8;

        // Igual que un pequeño menú móvil.
        // Si hay más skills, aparecen páginas.
        private const int PageSize =
            8;

        private readonly CharacterState
            _characterState;

        private readonly Func<
            int,
            SkillEntryState?>
            _getCurrentAssignment;

        private readonly Action<
            int,
            SkillEntryState>
            _assignSkill;

        private readonly Action<int>
            _clearSkill;

        private readonly PopupPanel
            _panel;

        private readonly LabelControl
            _titleLabel;

        private readonly ButtonControl
            _closeButton;

        private readonly List<ButtonControl>
            _dynamicButtons =
                new();

        private readonly List<SkillIconControl>
            _dynamicIcons =
                new();

        private readonly List<LabelControl>
            _dynamicLabels =
                new();

        private int _slotIndex;

        private int _pageIndex;

        private Rectangle
            _anchorRectangle;

        // =============================================================
        // PANEL
        // =============================================================

        private sealed class PopupPanel :
            UIControl
        {
            public PopupPanel()
            {
                AutoViewSize =
                    false;

                Interactive =
                    true;
            }
        }

        // =============================================================
        // SKILL ICON
        // =============================================================

        private sealed class SkillIconControl :
            UIControl
        {
            private readonly string
                _texturePath;

            private readonly Rectangle
                _sourceRectangle;

            public SkillIconControl(
                string texturePath,
                Rectangle sourceRectangle)
            {
                _texturePath =
                    texturePath;

                _sourceRectangle =
                    sourceRectangle;

                AutoViewSize =
                    false;

                Interactive =
                    false;

                BackgroundColor =
                    Color.Transparent;

                _ =
                    TextureLoader.Instance
                        .Prepare(
                            _texturePath);
            }

            public override void Draw(
                GameTime gameTime)
            {
                if (!Visible ||
                    Status !=
                        GameControlStatus.Ready)
                {
                    return;
                }

                Texture2D? texture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            _texturePath);

                if (texture == null ||
                    GraphicsManager.Instance ==
                        null)
                {
                    return;
                }

                GraphicsManager.Instance
                    .Sprite
                    .Draw(
                        texture,
                        DisplayRectangle,
                        _sourceRectangle,
                        Color.White * Alpha);
            }
        }

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public MobileSkillAssignPopup(
            CharacterState characterState,
            Func<int, SkillEntryState?>
                getCurrentAssignment,
            Action<int, SkillEntryState>
                assignSkill,
            Action<int>
                clearSkill)
        {
            _characterState =
                characterState ??
                throw new ArgumentNullException(
                    nameof(characterState));

            _getCurrentAssignment =
                getCurrentAssignment ??
                throw new ArgumentNullException(
                    nameof(getCurrentAssignment));

            _assignSkill =
                assignSkill ??
                throw new ArgumentNullException(
                    nameof(assignSkill));

            _clearSkill =
                clearSkill ??
                throw new ArgumentNullException(
                    nameof(clearSkill));

            AutoViewSize =
                false;

            ControlSize =
                UiScaler.VirtualSize;

            ViewSize =
                UiScaler.VirtualSize;

            Interactive =
                true;

            BackgroundColor =
                Color.Transparent;

            Visible =
                false;

            // =========================================================
            // PANEL
            // =========================================================

            _panel =
                new PopupPanel
                {
                    ControlSize =
                        new Point(
                            PopupWidth,
                            120),

                    ViewSize =
                        new Point(
                            PopupWidth,
                            120),

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            12,
                            12,
                            18,
                            240),

                    BorderColor =
                        Color.FromNonPremultiplied(
                            130,
                            115,
                            85,
                            230),

                    BorderThickness =
                        1
                };

            Controls.Add(
                _panel);

            // =========================================================
            // TITLE
            // =========================================================

            _titleLabel =
                new LabelControl
                {
                    Text =
                        "Asignar Skill",

                    X = 10,
                    Y = 9,

                    FontSize =
                        11f,

                    IsBold =
                        true,

                    TextColor =
                        new Color(
                            235,
                            215,
                            170),

                    Interactive =
                        false
                };

            _panel.Controls.Add(
                _titleLabel);

            // =========================================================
            // CLOSE
            // =========================================================

            _closeButton =
                new ButtonControl
                {
                    Text =
                        "X",

                    X =
                        PopupWidth - 32,

                    Y = 5,

                    ControlSize =
                        new Point(
                            26,
                            24),

                    ViewSize =
                        new Point(
                            26,
                            24),

                    AutoViewSize =
                        false,

                    FontSize =
                        10f,

                    TextColor =
                        Color.White,

                    HoverTextColor =
                        Color.Yellow,

                    BackgroundColor =
                        Color.FromNonPremultiplied(
                            45,
                            35,
                            35,
                            220),

                    HoverBackgroundColor =
                        Color.FromNonPremultiplied(
                            90,
                            45,
                            45,
                            230),

                    PressedBackgroundColor =
                        Color.FromNonPremultiplied(
                            120,
                            40,
                            40,
                            240)
                };

            _closeButton.Click +=
                (_, _) =>
                {
                    Hide();
                };

            _panel.Controls.Add(
                _closeButton);
        }

        // =============================================================
        // SHOW
        // =============================================================

        public void ShowFor(
            int slotIndex,
            Rectangle anchorRectangle)
        {
            _slotIndex =
                slotIndex;

            _pageIndex =
                0;

            _anchorRectangle =
                anchorRectangle;

            _titleLabel.Text =
                $"Asignar Skill {slotIndex + 1}";

            BuildOptions();

            Visible =
                true;

            BringToFront();
        }

        public void Hide()
        {
            Visible =
                false;
        }

        // =============================================================
        // OPTIONS
        // =============================================================

        private void BuildOptions()
        {
            ClearDynamicControls();

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

            int pageCount =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        skills.Count /
                        (double)PageSize));

            _pageIndex =
                Math.Clamp(
                    _pageIndex,
                    0,
                    pageCount - 1);

            List<SkillEntryState> pageSkills =
                skills
                    .Skip(
                        _pageIndex *
                        PageSize)
                    .Take(
                        PageSize)
                    .ToList();

            SkillEntryState?
                current =
                    _getCurrentAssignment(
                        _slotIndex);

            int y =
                HeaderHeight + 4;

            // =========================================================
            // SKILL ROWS
            // =========================================================

            foreach (
                SkillEntryState skill
                in pageSkills)
            {
                bool isCurrent =
                    current != null &&
                    current.SkillId ==
                        skill.SkillId;

                string name =
                    SkillDatabase.GetSkillName(
                        skill.SkillId);

                string prefix =
                    isCurrent
                        ? "> "
                        : string.Empty;

                var row =
                    new ButtonControl
                    {
                        Text =
                            $"{prefix}{name}",

                        X =
                            Padding,

                        Y =
                            y,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        AutoViewSize =
                            false,

                        FontSize =
                            9.5f,

                        TextColor =
                            isCurrent
                                ? new Color(
                                    255,
                                    220,
                                    140)
                                : Color.White,

                        HoverTextColor =
                            new Color(
                                255,
                                230,
                                150),

                        BackgroundColor =
                            isCurrent
                                ? Color
                                    .FromNonPremultiplied(
                                        65,
                                        52,
                                        28,
                                        225)
                                : Color
                                    .FromNonPremultiplied(
                                        30,
                                        30,
                                        38,
                                        220),

                        HoverBackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    60,
                                    55,
                                    45,
                                    235),

                        PressedBackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    80,
                                    65,
                                    40,
                                    240)
                    };

                SkillEntryState
                    selectedSkill =
                        skill;

                row.Click +=
                    (_, _) =>
                    {
                        _assignSkill(
                            _slotIndex,
                            selectedSkill);

                        Hide();
                    };

                _panel.Controls.Add(
                    row);

                _dynamicButtons.Add(
                    row);

                SkillIconInfo? iconInfo =
                    SkillIconDatabase
                        .GetIcon(
                            skill.SkillId);

                if (iconInfo.HasValue)
                {
                    var icon =
                        new SkillIconControl(
                            iconInfo.Value
                                .TexturePath,

                            iconInfo.Value
                                .SourceRectangle)
                        {
                            X =
                                Padding + 7,

                            Y =
                                y + 4,

                            ControlSize =
                                new Point(
                                    24,
                                    28),

                            ViewSize =
                                new Point(
                                    24,
                                    28)
                        };

                    _panel.Controls.Add(
                        icon);

                    _dynamicIcons.Add(
                        icon);
                }

                y +=
                    RowHeight;
            }

            // =========================================================
            // EMPTY
            // =========================================================

            if (pageSkills.Count == 0)
            {
                var empty =
                    new ButtonControl
                    {
                        Text =
                            "No hay skills aprendidas",

                        X =
                            Padding,

                        Y =
                            y,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        AutoViewSize =
                            false,

                        FontSize =
                            9f,

                        Enabled =
                            false,

                        BackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    25,
                                    25,
                                    30,
                                    210)
                    };

                _panel.Controls.Add(
                    empty);

                _dynamicButtons.Add(
                    empty);

                y +=
                    RowHeight;
            }

            // =========================================================
            // PAGES
            // =========================================================

            if (pageCount > 1)
            {
                const int ButtonWidth =
                    48;

                const int NavHeight =
                    28;

                var previous =
                    new ButtonControl
                    {
                        Text =
                            "<",

                        X =
                            Padding,

                        Y =
                            y,

                        ControlSize =
                            new Point(
                                ButtonWidth,
                                NavHeight),

                        ViewSize =
                            new Point(
                                ButtonWidth,
                                NavHeight),

                        AutoViewSize =
                            false,

                        Enabled =
                            _pageIndex > 0,

                        BackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    35,
                                    35,
                                    42,
                                    220)
                    };

                previous.Click +=
                    (_, _) =>
                    {
                        if (_pageIndex <= 0)
                        {
                            return;
                        }

                        _pageIndex--;

                        BuildOptions();
                    };

                var next =
                    new ButtonControl
                    {
                        Text =
                            ">",

                        X =
                            PopupWidth -
                            Padding -
                            ButtonWidth,

                        Y =
                            y,

                        ControlSize =
                            new Point(
                                ButtonWidth,
                                NavHeight),

                        ViewSize =
                            new Point(
                                ButtonWidth,
                                NavHeight),

                        AutoViewSize =
                            false,

                        Enabled =
                            _pageIndex <
                            pageCount - 1,

                        BackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    35,
                                    35,
                                    42,
                                    220)
                    };

                next.Click +=
                    (_, _) =>
                    {
                        if (_pageIndex >=
                            pageCount - 1)
                        {
                            return;
                        }

                        _pageIndex++;

                        BuildOptions();
                    };

                var pageLabel =
                    new LabelControl
                    {
                        Text =
                            $"{_pageIndex + 1} / {pageCount}",

                        X =
                            60,

                        Y =
                            y + 6,

                        ViewSize =
                            new Point(
                                PopupWidth - 120,
                                18),

                        FontSize =
                            9f,

                        TextAlign =
                            HorizontalAlign.Center,

                        TextColor =
                            new Color(
                                200,
                                200,
                                200),

                        Interactive =
                            false
                    };

                _panel.Controls.Add(
                    previous);

                _panel.Controls.Add(
                    next);

                _panel.Controls.Add(
                    pageLabel);

                _dynamicButtons.Add(
                    previous);

                _dynamicButtons.Add(
                    next);

                _dynamicLabels.Add(
                    pageLabel);

                y +=
                    NavHeight + 4;
            }

            // =========================================================
            // CLEAR
            // =========================================================

            if (current != null)
            {
                var clear =
                    new ButtonControl
                    {
                        Text =
                            $"Quitar asignación { _slotIndex + 1 }",

                        X =
                            Padding,

                        Y =
                            y + 3,

                        ControlSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        ViewSize =
                            new Point(
                                PopupWidth -
                                    Padding * 2,

                                RowHeight - 2),

                        AutoViewSize =
                            false,

                        FontSize =
                            9f,

                        TextColor =
                            new Color(
                                235,
                                170,
                                170),

                        HoverTextColor =
                            new Color(
                                255,
                                200,
                                200),

                        BackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    55,
                                    28,
                                    28,
                                    220),

                        HoverBackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    90,
                                    35,
                                    35,
                                    235),

                        PressedBackgroundColor =
                            Color
                                .FromNonPremultiplied(
                                    120,
                                    35,
                                    35,
                                    240)
                    };

                clear.Click +=
                    (_, _) =>
                    {
                        _clearSkill(
                            _slotIndex);

                        Hide();
                    };

                _panel.Controls.Add(
                    clear);

                _dynamicButtons.Add(
                    clear);

                y +=
                    RowHeight + 3;
            }

            // =========================================================
            // HEIGHT
            // =========================================================

            int panelHeight =
                y + Padding;

            _panel.ControlSize =
                new Point(
                    PopupWidth,
                    panelHeight);

            _panel.ViewSize =
                _panel.ControlSize;

            PositionNextTo(
                _anchorRectangle);
        }

        // =============================================================
        // POSITION
        // =============================================================

        private void PositionNextTo(
            Rectangle anchor)
        {
            const int Gap =
                8;

            const int ScreenMargin =
                8;

            int rightX =
                anchor.Right +
                Gap;

            int leftX =
                anchor.Left -
                PopupWidth -
                Gap;

            if (rightX +
                    PopupWidth <=
                UiScaler.VirtualSize.X -
                    ScreenMargin)
            {
                _panel.X =
                    rightX;
            }
            else
            {
                _panel.X =
                    Math.Max(
                        ScreenMargin,
                        leftX);
            }

            int anchorCenterY =
                anchor.Y +
                anchor.Height / 2;

            int desiredY =
                anchorCenterY -
                _panel.ControlSize.Y / 2;

            int maxY =
                UiScaler.VirtualSize.Y -
                _panel.ControlSize.Y -
                ScreenMargin;

            _panel.Y =
                Math.Max(
                    ScreenMargin,
                    Math.Min(
                        desiredY,
                        Math.Max(
                            ScreenMargin,
                            maxY)));
        }

        // =============================================================
        // UPDATE
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            if (!Visible)
            {
                return;
            }

            base.Update(
                gameTime);

            MouseState mouse =
                MuGame.Instance
                    .UiMouseState;

            MouseState previous =
                MuGame.Instance
                    .PrevUiMouseState;

            bool freshPress =
                mouse.LeftButton ==
                    ButtonState.Pressed &&
                previous.LeftButton ==
                    ButtonState.Released;

            if (freshPress &&
                !_panel
                    .DisplayRectangle
                    .Contains(
                        mouse.Position))
            {
                Hide();
            }
        }

        // =============================================================
        // CLEANUP
        // =============================================================

        private void ClearDynamicControls()
        {
            for (int i =
                     _dynamicButtons.Count - 1;
                 i >= 0;
                 i--)
            {
                _dynamicButtons[i]
                    .Dispose();
            }

            _dynamicButtons.Clear();

            for (int i =
                     _dynamicIcons.Count - 1;
                 i >= 0;
                 i--)
            {
                _dynamicIcons[i]
                    .Dispose();
            }

            _dynamicIcons.Clear();

            for (int i =
                     _dynamicLabels.Count - 1;
                 i >= 0;
                 i--)
            {
                _dynamicLabels[i]
                    .Dispose();
            }

            _dynamicLabels.Clear();
        }
    }
}