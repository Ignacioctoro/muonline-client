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
    /// Draws the assigned skill hotkeys over the 1-5 slots
    /// which already exist in the HUD artwork.
    ///
    /// The original MU client uses 20x28 skill icons inside
    /// 32x38 hotkey cells. We keep the source dimensions and
    /// enlarge them slightly to fit the custom B Royal HUD.
    /// </summary>
    public class SkillHotkeyHudControl : ExtendedUIControl
    {
        private const int SLOT_COUNT = 5;

        // Visual size inside our HUD.
        private const int ICON_WIDTH = 24;
        private const int ICON_HEIGHT = 34;

        // Positions are relative to this control.
        //
        // These follow the existing 1-5 slots in MainLayout.json.
        private static readonly Point[] SlotPositions =
        {
            new(0,   0), // 1
            new(35,  0), // 2
            new(71,  0), // 3
            new(104, 0), // 4
            new(140, 0), // 5
        };

        private readonly SkillEntryState?[] _skills =
            new SkillEntryState?[SLOT_COUNT];

        public event Action<int, SkillEntryState>? SkillClicked;

        public SkillHotkeyHudControl()
        {
            AutoViewSize = false;

            ViewSize = new Point(
                165,
                ICON_HEIGHT);

            ControlSize = ViewSize;

            Interactive = true;
        }

        public void SetSkill(
            int slotIndex,
            SkillEntryState? skill)
        {
            if (slotIndex < 0 ||
                slotIndex >= SLOT_COUNT)
            {
                return;
            }

            _skills[slotIndex] = skill;
        }

        public SkillEntryState? GetSkill(
            int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >= SLOT_COUNT)
            {
                return null;
            }

            return _skills[slotIndex];
        }

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

            for (int i = 0;
                 i < SLOT_COUNT;
                 i++)
            {
                Point pos =
                    SlotPositions[i];

                Rectangle slotRectangle =
                    new Rectangle(
                        controlRect.X + pos.X,
                        controlRect.Y + pos.Y,
                        ICON_WIDTH,
                        ICON_HEIGHT);

                if (!slotRectangle.Contains(
                        mousePosition))
                {
                    continue;
                }

                SkillEntryState? skill =
                    _skills[i];

                if (skill == null)
                {
                    return;
                }

                Scene?.SetMouseInputConsumed();

                SkillClicked?.Invoke(
                    i,
                    skill);

                return;
            }
        }

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
                GraphicsManager.Instance.Sprite;

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
                for (int i = 0;
                     i < SLOT_COUNT;
                     i++)
                {
                    SkillEntryState? skill =
                        _skills[i];

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
                        SlotPositions[i];

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