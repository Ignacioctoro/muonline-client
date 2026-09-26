using Client.Main.Controllers;
using Client.Main.Controls.UI;
using Client.Main.Controls.UI.Game.Hud;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Client;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Main.Controls.UI.Game
{
    public class MainControl : DynamicLayoutControl
    {
        // =============================================================
        // RESOURCES
        // =============================================================

        protected override string LayoutJsonResource =>
            "Client.Main.Controls.UI.Game.Layouts.MainLayout.json";

        protected override string TextureRectJsonResource =>
            "Client.Main.Controls.UI.Game.Layouts.MainRect.json";

        protected override string DefaultTexturePath =>
            "Interface/GFx/main_IE.ozd";

        // =============================================================
        // UI COMPONENTS
        // =============================================================

        private readonly MainHPControl _hp;
        private readonly MainMPControl _mp;

        private readonly ItemHotkeyHudControl _itemHotkeys;
        private readonly SkillHotkeyHudControl _skillHotkeys;
        private readonly MapPositionHudControl _mapPositionHud;

        // Yellow highlight taken from ActiveSkill_1.
        private TextureControl _skillHotkeyHighlight;

        // Current visible bank:
        //
        // 0 = 1 2 3 4 5
        // 1 = 6 7 8 9 0
        private int _skillHotkeyBank;

        // Real logical selected hotkey:
        //
        // 0 = 1
        // 1 = 2
        // ...
        // 4 = 5
        // 5 = 6
        // ...
        // 8 = 9
        // 9 = 0
        //
        // -1 = none
        private int _selectedSkillHotkey = -1;

        public SkillHotkeyHudControl SkillHotkeys =>
            _skillHotkeys;

        public int SkillHotkeyBank =>
            _skillHotkeyBank;

        private int _hoveredHudElements;

        private readonly List<DrawEntry> _drawEntries =
            new();

        private IReadOnlyList<GameControl> _lastControlsSnapshot =
            Array.Empty<GameControl>();

        private int _lastControlsRenderOrderHash = 0;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public MainControl(CharacterState state)
        {
            foreach (var key in new[]
            {
                "ActiveSkill_1",
                "ActiveSkill_2",
                "0",
                "1",
                "2",
                "3",
                "4",
                "5"
            })
            {
                AlphaOverrides[key] = 0.7f;
            }

            // =========================================================
            // HP
            // =========================================================

            _hp = new MainHPControl
            {
                Name = "HP_1"
            };

            _hp.Tag = new LayoutInfo
            {
                ScreenX = 292,
                ScreenY = 604,
                Width = 100,
                Height = 100,
                Z = -5
            };

            if (_hp is ExtendedUIControl extHp &&
                _hp.Tag is LayoutInfo lhp)
            {
                extHp.RenderOrder = lhp.Z;
            }

            // =========================================================
            // MP
            // =========================================================

            _mp = new MainMPControl
            {
                Name = "MP_1"
            };

            _mp.Tag = new LayoutInfo
            {
                ScreenX = 860,
                ScreenY = 604,
                Width = 100,
                Height = 100,
                Z = -5
            };

            if (_mp is ExtendedUIControl extMp &&
                _mp.Tag is LayoutInfo lmp)
            {
                extMp.RenderOrder = lmp.Z;
            }

            // =========================================================
            // ITEM HOTKEYS Q/W/E/R
            // =========================================================

            _itemHotkeys =
                new ItemHotkeyHudControl
                {
                    Name = "ItemHotkeys",
                    X = 435,
                    Y = 665,
                    RenderOrder = 7
                };

            // =========================================================
            // SKILL HOTKEYS
            // =========================================================

            _skillHotkeys =
                new SkillHotkeyHudControl
                {
                    Name = "SkillHotkeys",

                    // Your already adjusted position.
                    X = 657,
                    Y = 661,

                    RenderOrder = 8
                };
            // =========================================================
            // MAP / COORDINATES HUD
            // =========================================================

            _mapPositionHud =
                new MapPositionHudControl
                {
                    Name = "MapPositionHud",
                    X = 0,
                    Y = 0
                };

            _mapPositionHud.MapNameClicked +=
                (_, _) =>
                {
                    if (Scene is not GameScene gameScene)
                        return;

                    var moveWindow =
                        gameScene.MoveCommandWindow;

                    if (moveWindow == null)
                        return;

                    moveWindow.Show();

                    SoundController.Instance?
                        .PlayBuffer(
                            "Sound/iButtonClick.wav");
                };

            // =========================================================
            // INITIAL VALUES
            // =========================================================

            _hp.SetValues(
                (int)state.CurrentHp,
                (int)state.MaxHp);

            _mp.SetValues(
                (int)state.CurrentMana,
                (int)state.MaxMp);

            state.HealthChanged +=
                (cur, max) =>
                    _hp.SetValues(
                        (int)cur,
                        (int)max);

            state.ManaChanged +=
                (cur, max) =>
                    _mp.SetValues(
                        (int)cur,
                        (int)max);

            // =========================================================
            // CONTROLS
            // =========================================================

            Controls.Clear();

            Controls.Add(_mp);
            Controls.Add(_hp);
            Controls.Add(_itemHotkeys);
            Controls.Add(_skillHotkeys);
            Controls.Add(_mapPositionHud);

            ControlFactories["InventoryButton"] =
                CreateHudButton;

            ControlFactories["SettingsButton"] =
                CreateHudButton;

            // The HUD arrow now becomes a real button.
            ControlFactories["arrow_right_2"] =
                CreateHudButton;

            CreateControls();
            UpdateLayout();

            // =========================================================
            // HOTKEY HIGHLIGHT
            // =========================================================

            _skillHotkeyHighlight =
                Controls.FirstOrDefault(
                    c => c.Name == "ActiveSkill_1")
                as TextureControl;

            if (_skillHotkeyHighlight != null)
            {
                _skillHotkeyHighlight.Visible =
                    false;
            }

            // Start with 1-5.
            _skillHotkeyBank = 0;

            _skillHotkeys.SetVisibleBank(
                _skillHotkeyBank);

            UpdateSkillHotkeyNumbers();

            // Clicking/tapping one of the five visible skills.
            _skillHotkeys.SkillClicked +=
                (slotIndex, skill) =>
                {
                    SetSelectedSkillHotkey(
                        slotIndex);
                };

            // =========================================================
            // HOTKEY BANK ARROW
            // =========================================================

            if (Controls.FirstOrDefault(
                    c => c.Name == "arrow_right_2")
                is TextureControl arrowButton)
            {
                arrowButton.Interactive = true;
                arrowButton.BringToFront();

                arrowButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();

                    ToggleSkillHotkeyBank();

                    SoundController.Instance?
                        .PlayBuffer(
                            "Sound/iButtonClick.wav");
                };
            }

            // =========================================================
            // INVENTORY BUTTON
            // =========================================================

            if (Controls.FirstOrDefault(
                    c => c.Name == "InventoryButton")
                is TextureControl inventoryButton)
            {
                inventoryButton.Interactive = true;
                inventoryButton.BringToFront();

                inventoryButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();

                    var inventory =
                        (Scene as GameScene)?
                        .InventoryControl;

                    if (inventory == null)
                        return;

                    if (inventory.Visible)
                    {
                        inventory.Hide();
                    }
                    else
                    {
                        inventory.Show();
                    }

                    SoundController.Instance?
                        .PlayBuffer(
                            "Sound/iButtonClick.wav");
                };
            }

            // =========================================================
            // SETTINGS BUTTON
            // =========================================================

            if (Controls.FirstOrDefault(
                    c => c.Name == "SettingsButton")
                is TextureControl settingsButton)
            {
                settingsButton.Interactive = true;
                settingsButton.BringToFront();

                settingsButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();

                    var pauseMenu =
                        (Scene as GameScene)?
                        .PauseMenu;

                    if (pauseMenu == null)
                        return;

                    bool show =
                        !pauseMenu.Visible;

                    pauseMenu.Visible =
                        show;

                    if (show)
                    {
                        pauseMenu.BringToFront();

                        if (Scene != null)
                        {
                            Scene.FocusControl =
                                pauseMenu;
                        }
                    }
                    else if (
                        Scene?.FocusControl ==
                        pauseMenu)
                    {
                        Scene.FocusControl =
                            null;
                    }

                    SoundController.Instance?
                        .PlayBuffer(
                            "Sound/iButtonClick.wav");
                };
            }
        }

        // =============================================================
        // HOTKEY BANK
        // =============================================================

        public void ToggleSkillHotkeyBank()
        {
            SetSkillHotkeyBank(
                _skillHotkeyBank == 0
                    ? 1
                    : 0);
        }

        public void SetSkillHotkeyBank(
            int bank)
        {
            _skillHotkeyBank =
                bank <= 0
                    ? 0
                    : 1;

            // Change which five logical skills are drawn.
            _skillHotkeys.SetVisibleBank(
                _skillHotkeyBank);

            // Change 1-5 into 6-0 or vice versa.
            UpdateSkillHotkeyNumbers();

            // Show/hide/reposition highlight.
            UpdateSelectedSkillHighlight();
        }

        // =============================================================
        // HUD NUMBERS
        // =============================================================

        private void UpdateSkillHotkeyNumbers()
        {
            int[] digits =
                _skillHotkeyBank == 0
                    ? new[]
                    {
                        1, 2, 3, 4, 5
                    }
                    : new[]
                    {
                        6, 7, 8, 9, 0
                    };

            for (int visualSlot = 0;
                 visualSlot < 5;
                 visualSlot++)
            {
                // These controls represent the five physical positions.
                //
                // Their names remain 1-5 internally even when the
                // displayed graphics are 6-0.
                string controlName =
                    (visualSlot + 1)
                    .ToString();

                var control =
                    Controls.FirstOrDefault(
                        c =>
                            c.Name ==
                            controlName)
                    as TextureControl;

                if (control == null)
                {
                    continue;
                }

                control.TextureRectangle =
                    GetHotkeyDigitRectangle(
                        digits[
                            visualSlot]);
            }
        }

        private static Rectangle GetHotkeyDigitRectangle(
            int digit)
        {
            return digit switch
            {
                // Coordinates from main_IE.ozd atlas.

                0 => new Rectangle(
                    959,
                    68,
                    14,
                    18),

                1 => new Rectangle(
                    1002,
                    68,
                    12,
                    18),

                2 => new Rectangle(
                    973,
                    68,
                    13,
                    18),

                3 => new Rectangle(
                    930,
                    68,
                    15,
                    18),

                4 => new Rectangle(
                    986,
                    68,
                    16,
                    18),

                5 => new Rectangle(
                    945,
                    68,
                    14,
                    18),

                6 => new Rectangle(
                    916,
                    68,
                    14,
                    18),

                7 => new Rectangle(
                    902,
                    68,
                    14,
                    18),

                8 => new Rectangle(
                    888,
                    68,
                    14,
                    18),

                9 => new Rectangle(
                    874,
                    68,
                    14,
                    18),

                _ => Rectangle.Empty
            };
        }

        // =============================================================
        // SELECTED SKILL HOTKEY
        // =============================================================

        public void SetSelectedSkillHotkey(
            int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >= 10)
            {
                ClearSelectedSkillHotkey();
                return;
            }

            _selectedSkillHotkey =
                slotIndex;

            // Automatically show the bank containing the selected key.
            //
            // 0-4 -> bank 1-5
            // 5-9 -> bank 6-0
            int requiredBank =
                slotIndex < 5
                    ? 0
                    : 1;

            if (_skillHotkeyBank !=
                requiredBank)
            {
                SetSkillHotkeyBank(
                    requiredBank);

                return;
            }

            UpdateSelectedSkillHighlight();
        }

        private void UpdateSelectedSkillHighlight()
        {
            if (_skillHotkeyHighlight == null)
            {
                return;
            }

            if (_selectedSkillHotkey < 0 ||
                _selectedSkillHotkey >= 10)
            {
                _skillHotkeyHighlight.Visible =
                    false;

                return;
            }

            int selectedBank =
                _selectedSkillHotkey < 5
                    ? 0
                    : 1;

            // The selected skill belongs to the other page.
            // Hide the highlight until that page is visible again.
            if (selectedBank !=
                _skillHotkeyBank)
            {
                _skillHotkeyHighlight.Visible =
                    false;

                return;
            }

            // Convert logical slot to one of the five physical positions.
            //
            // 0 -> position 1
            // 1 -> position 2
            // ...
            // 5 -> position 1
            // 6 -> position 2
            // ...
            int visualSlot =
                _selectedSkillHotkey %
                5;

            string numberControlName =
                (visualSlot + 1)
                .ToString();

            var numberControl =
                Controls.FirstOrDefault(
                    c =>
                        c.Name ==
                        numberControlName);

            if (numberControl == null)
            {
                _skillHotkeyHighlight.Visible =
                    false;

                return;
            }

            _skillHotkeyHighlight.X =
                numberControl.X - 13;

            _skillHotkeyHighlight.Y =
                numberControl.Y + 8;

            _skillHotkeyHighlight.Visible =
                true;
        }

        public void ClearSelectedSkillHotkey()
        {
            _selectedSkillHotkey =
                -1;

            if (_skillHotkeyHighlight != null)
            {
                _skillHotkeyHighlight.Visible =
                    false;
            }
        }

        // =============================================================
        // UPDATE
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            _hoveredHudElements = 0;

            base.Update(gameTime);

            if (_hoveredHudElements > 0)
            {
                IsMouseOver = true;
            }
            else if (IsMouseOver)
            {
                IsMouseOver = false;
            }
        }

        // =============================================================
        // RENDER ORDER
        // =============================================================

        private static int GetRenderOrder(
            GameControl control)
        {
            return
                (control as ExtendedUIControl)?
                    .RenderOrder
                ?? 0;
        }

        private void EnsureDrawEntries(
            IReadOnlyList<GameControl> snapshot)
        {
            int hash = 17;

            for (int i = 0;
                 i < snapshot.Count;
                 i++)
            {
                var c =
                    snapshot[i];

                if (c == null)
                    continue;

                hash =
                    unchecked(
                        hash * 31 +
                        GetRenderOrder(c));
            }

            if (ReferenceEquals(
                    snapshot,
                    _lastControlsSnapshot) &&
                hash ==
                _lastControlsRenderOrderHash)
            {
                return;
            }

            _drawEntries.Clear();

            if (_drawEntries.Capacity <
                snapshot.Count)
            {
                _drawEntries.Capacity =
                    snapshot.Count;
            }

            for (int i = 0;
                 i < snapshot.Count;
                 i++)
            {
                var c =
                    snapshot[i];

                if (c == null)
                    continue;

                _drawEntries.Add(
                    new DrawEntry(
                        c,
                        GetRenderOrder(c),
                        i));
            }

            _drawEntries.Sort(
                DrawEntryComparer.Instance);

            _lastControlsSnapshot =
                snapshot;

            _lastControlsRenderOrderHash =
                hash;
        }

        // =============================================================
        // DRAW
        // =============================================================

        public override void Draw(
            GameTime gameTime)
        {
            if (Status !=
                    GameControlStatus.Ready ||
                !Visible)
            {
                return;
            }

            var sb =
                GraphicsManager
                    .Instance
                    .Sprite;

            var snapshot =
                Controls.GetSnapshot();

            EnsureDrawEntries(
                snapshot);

            SpriteBatchScope? textureScope =
                null;

            try
            {
                for (int i = 0;
                     i < _drawEntries.Count;
                     i++)
                {
                    var entry =
                        _drawEntries[i];

                    var c =
                        entry.Control;

                    if (entry.IsTexture)
                    {
                        textureScope ??=
                            new SpriteBatchScope(
                                sb,
                                SpriteSortMode.Deferred,
                                BlendState.NonPremultiplied,
                                SamplerState.PointClamp,
                                transform:
                                    UiScaler.SpriteTransform);

                        c.Draw(
                            gameTime);
                    }
                    else
                    {
                        textureScope?.Dispose();
                        textureScope = null;

                        c.Draw(
                            gameTime);
                    }
                }
            }
            finally
            {
                textureScope?.Dispose();
            }

            using (
                new SpriteBatchScope(
                    sb,
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    transform:
                        UiScaler.SpriteTransform))
            {
                _hp.DrawLabel(
                    gameTime);

                _mp.DrawLabel(
                    gameTime);
            }
        }

        // =============================================================
        // DRAW ENTRY
        // =============================================================

        private readonly struct DrawEntry
        {
            public GameControl Control { get; }

            public int RenderOrder { get; }

            public int Index { get; }

            public bool IsTexture { get; }

            public DrawEntry(
                GameControl control,
                int renderOrder,
                int index)
            {
                Control = control;
                RenderOrder = renderOrder;
                Index = index;

                IsTexture =
                    control is TextureControl;
            }
        }

        private sealed class DrawEntryComparer :
            IComparer<DrawEntry>
        {
            public static readonly
                DrawEntryComparer Instance =
                    new();

            public int Compare(
                DrawEntry x,
                DrawEntry y)
            {
                int cmp =
                    x.RenderOrder.CompareTo(
                        y.RenderOrder);

                if (cmp != 0)
                    return cmp;

                return
                    x.Index.CompareTo(
                        y.Index);
            }
        }

        // =============================================================
        // HUD BUTTON FACTORY
        // =============================================================

        private UIControl CreateHudButton(
            LayoutInfo info)
        {
            var button =
                new HudTextureButton
                {
                    Name =
                        info.Name,

                    AutoViewSize =
                        false,

                    TexturePath =
                        DefaultTexturePath,

                    BlendState =
                        BlendState.NonPremultiplied
                };

            var texRect =
                TextureRectDatas
                    .FirstOrDefault(
                        t =>
                            t.Name ==
                            info.Name);

            button.TextureRectangle =
                texRect != null
                    ? new Rectangle(
                        texRect.X,
                        texRect.Y,
                        texRect.Width,
                        texRect.Height)
                    : new Rectangle(
                        0,
                        0,
                        info.Width,
                        info.Height);

            return button;
        }

        // =============================================================
        // HUD BUTTON
        // =============================================================

        private sealed class HudTextureButton :
            TextureControl
        {
            public HudTextureButton()
            {
                Interactive =
                    true;
            }

            public override void Update(
                GameTime gameTime)
            {
                base.Update(
                    gameTime);

                if (!Visible ||
                    !Interactive ||
                    Scene == null)
                {
                    return;
                }

                if (Parent is MainControl main &&
                    main.Scene is BaseScene scene)
                {
                    if (IsMouseOver)
                    {
                        main._hoveredHudElements++;

                        if (scene.MouseHoverControl ==
                                null ||
                            scene.MouseHoverControl ==
                                scene.World)
                        {
                            scene.MouseHoverControl =
                                main;
                        }

                        if (scene.MouseControl ==
                                null ||
                            scene.MouseControl ==
                                scene.World)
                        {
                            scene.MouseControl =
                                main;
                        }
                    }
                }

                var mouse =
                    CurrentMouseState;

                var prevMouse =
                    PreviousMouseState;

                if (IsMouseOver &&
                    mouse.LeftButton ==
                        ButtonState.Pressed &&
                    prevMouse.LeftButton ==
                        ButtonState.Released)
                {
                    Scene.SetMouseInputConsumed();
                }
            }
        }
    }
}