#nullable enable

using System;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Core.Client;
using Client.Main.Core.Utilities;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI.Game.Hud
{
    /// <summary>
    /// Draws the assigned skill hotkeys over the five physical
    /// skill slots which already exist in the HUD artwork.
    ///
    /// There are 10 logical hotkeys:
    ///
    /// Bank 0:
    /// 1 2 3 4 5
    ///
    /// Bank 1:
    /// 6 7 8 9 0
    ///
    /// Only five are rendered at a time.
    /// </summary>
    public class SkillHotkeyHudControl : ExtendedUIControl
    {
        // Total logical hotkeys.
        private const int TOTAL_SLOT_COUNT = 10;

        // Physical slots visible in the HUD.
        private const int VISIBLE_SLOT_COUNT = 5;

        // Visual size inside our HUD.
        private const int ICON_WIDTH = 24;
        private const int ICON_HEIGHT = 34;

        // Positions are relative to this control.
        // These are the five physical HUD positions.
        private static readonly Point[] SlotPositions =
        {
            new(0,   0),
            new(35,  0),
            new(71,  0),
            new(104, 0),
            new(140, 0)
        };

        // 0-4 = keys 1-5
        // 5-9 = keys 6-0
        private readonly SkillEntryState?[] _skills =
            new SkillEntryState?[TOTAL_SLOT_COUNT];

        // 0 = 1-5
        // 1 = 6-0
        private int _visibleBank;

        /// <summary>
        /// Returns the currently visible bank.
        ///
        /// 0 = 1-5
        /// 1 = 6-0
        /// </summary>
        public int VisibleBank =>
            _visibleBank;

        /// <summary>
        /// Fired when the user clicks/touches a skill.
        ///
        /// The slot index is the real logical index:
        ///
        /// 0 = 1
        /// 1 = 2
        /// ...
        /// 4 = 5
        /// 5 = 6
        /// ...
        /// 8 = 9
        /// 9 = 0
        /// </summary>
        public event Action<int, SkillEntryState>? SkillClicked;

        public SkillHotkeyHudControl()
        {
            AutoViewSize = false;

            ViewSize = new Point(
                165,
                ICON_HEIGHT);

            ControlSize = ViewSize;

            Interactive = true;

            _visibleBank = 0;
        }

        // =============================================================
        // BANK
        // =============================================================

        public void SetVisibleBank(
            int bank)
        {
            _visibleBank =
                bank <= 0
                    ? 0
                    : 1;
        }

        public void ToggleBank()
        {
            _visibleBank =
                _visibleBank == 0
                    ? 1
                    : 0;
        }

        // =============================================================
        // SKILLS
        // =============================================================

        public void SetSkill(
            int slotIndex,
            SkillEntryState? skill)
        {
            if (slotIndex < 0 ||
                slotIndex >= TOTAL_SLOT_COUNT)
            {
                return;
            }

            _skills[slotIndex] =
                skill;
        }

        public SkillEntryState? GetSkill(
            int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >= TOTAL_SLOT_COUNT)
            {
                return null;
            }

            return _skills[slotIndex];
        }

        // =============================================================
        // INDEX HELPERS
        // =============================================================

        private int GetRealSlotIndex(
            int visualSlotIndex)
        {
            return
                (_visibleBank *
                 VISIBLE_SLOT_COUNT)
                +
                visualSlotIndex;
        }

        // =============================================================
        // INPUT
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (!Visible ||
                !Interactive)
            {
                return;
            }

            var mouse =
                CurrentMouseState;

            var previousMouse =
                PreviousMouseState;

            if (mouse.LeftButton !=
                    Microsoft.Xna.Framework.Input.ButtonState.Pressed ||
                previousMouse.LeftButton !=
                    Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                return;
            }

            Point mousePosition =
                mouse.Position;

            Rectangle controlRect =
                DisplayRectangle;

            for (int visualSlot = 0;
                 visualSlot < VISIBLE_SLOT_COUNT;
                 visualSlot++)
            {
                Point pos =
                    SlotPositions[visualSlot];

                Rectangle slotRectangle =
                    new Rectangle(
                        controlRect.X +
                        pos.X,

                        controlRect.Y +
                        pos.Y,

                        ICON_WIDTH,
                        ICON_HEIGHT);

                if (!slotRectangle.Contains(
                        mousePosition))
                {
                    continue;
                }

                int realSlot =
                    GetRealSlotIndex(
                        visualSlot);

                SkillEntryState? skill =
                    _skills[realSlot];

                if (skill == null)
                {
                    return;
                }

                Scene?
                    .SetMouseInputConsumed();

                SkillClicked?
                    .Invoke(
                        realSlot,
                        skill);

                return;
            }
        }

        // =============================================================
        // DRAW
        // =============================================================

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible ||
                Status != GameControlStatus.Ready ||
                GraphicsManager.Instance == null)
            {
                return;
            }

            var spriteBatch =
                GraphicsManager
                    .Instance
                    .Sprite;

            Rectangle controlRect =
                DisplayRectangle;

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    transform:
                        UiScaler.SpriteTransform))
            {
                for (int visualSlot = 0;
                     visualSlot < VISIBLE_SLOT_COUNT;
                     visualSlot++)
                {
                    int realSlot =
                        GetRealSlotIndex(
                            visualSlot);

                    SkillEntryState? skill =
                        _skills[realSlot];

                    if (skill == null)
                    {
                        continue;
                    }

                    SkillIconInfo? iconInfo =
                        SkillIconDatabase.GetIcon(
                            skill.SkillId);

                    if (!iconInfo.HasValue)
                    {
                        continue;
                    }

                    SkillIconInfo icon =
                        iconInfo.Value;

                    Texture2D texture =
                        TextureLoader.Instance
                            .GetTexture2D(
                                icon.TexturePath);

                    if (texture == null)
                    {
                        continue;
                    }

                    Point pos =
                        SlotPositions[
                            visualSlot];

                    Rectangle destination =
                        new Rectangle(
                            controlRect.X +
                            pos.X,

                            controlRect.Y +
                            pos.Y,

                            ICON_WIDTH,
                            ICON_HEIGHT);

                    spriteBatch.Draw(
                        texture,
                        destination,
                        icon.SourceRectangle,
                        Color.White);
                }
            }
        }
    }
}