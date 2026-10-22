using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Content;
using System.Threading.Tasks;
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
        private readonly PetStatusHudControl _petStatusHud;
        private ExperienceBarControl _experienceBar;
        private ResourceBarControl _shieldBar;
        private ResourceBarControl _abilityBar;
        private TextureControl _experienceDigit;
        private int _lastExperienceDigit = -1;

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

            // Original pet endurance frames, repositioned below the modern
            // 37px map panel so they never overlap its left-hand atlas.
            _petStatusHud = new PetStatusHudControl(state)
            {
                X = 8,
                Y = 44
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
            Controls.Add(_petStatusHud);

            ControlFactories["InventoryButton"] =
                CreateHudButton;

            ControlFactories["Button_1"] = CreateHudButton;
            ControlFactories["button2"] = CreateHudButton;
            ControlFactories["button4"] = CreateHudButton;
            ControlFactories["button5"] = CreateHudButton;
            ControlFactories["Bar1"] = _ => _shieldBar = new ResourceBarControl(state, true);
            ControlFactories["Bar2"] = _ => _abilityBar = new ResourceBarControl(state, false);

            ControlFactories["SettingsButton"] =
                CreateHudButton;

            // The HUD arrow now becomes a real button.
            ControlFactories["arrow_right_2"] =
                CreateHudButton;

            // Replace the static atlas slice with its server-driven NewUI gauge.
            // It retains the ExpBar layout and original render order.
            ControlFactories["ExpBar"] =
                _ => _experienceBar = new ExperienceBarControl(state);

            CreateControls();
            UpdateLayout();

            // MainLayout's sprite named "0" is the EXP decile indicator at
            // the far right, not a skill hotkey. Keep its original atlas art.
            _experienceDigit = Controls.FirstOrDefault(
                c => c.Name == "0") as TextureControl;

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

            if (Controls.FirstOrDefault(c => c.Name == "button2") is TextureControl characterButton)
            {
                characterButton.BringToFront();
                characterButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();
                    (Scene as GameScene)?.ToggleCharacterInfoWindow();
                };
            }

            if (Controls.FirstOrDefault(c => c.Name == "button5") is TextureControl friendsButton)
            {
                friendsButton.BringToFront();
                friendsButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();
                    if (Scene is GameScene game) game.CommunityMenu?.Toggle(friendsButton);
                };
            }

            if (Controls.FirstOrDefault(c => c.Name == "button4") is TextureControl questButton)
            {
                questButton.BringToFront();
                questButton.Click += (_, _) =>
                {
                    Scene?.SetMouseInputConsumed();
                    Quest.MyQuestsControl.Instance.Toggle();
                    SoundController.Instance?.PlayBuffer("Sound/iButtonClick.wav");
                };
            }

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

            // MainControl updates child gauges first. Change the original
            // HUD digit sprite only when the active 10% stage changes.
            if (_experienceDigit != null && _experienceBar != null)
            {
                int digit = _experienceBar.CurrentDecileDigit;
                if (digit != _lastExperienceDigit)
                {
                    _experienceDigit.TextureRectangle =
                        GetHotkeyDigitRectangle(digit);
                    _lastExperienceDigit = digit;
                }
            }

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

        // GameScene calls this after its regular UI draw pass, ensuring the
        // EXP tooltip is never covered by the active skill or other HUD art.
        public bool HasExperienceTooltip =>
            Status == GameControlStatus.Ready &&
            Visible &&
            ((_experienceBar?.IsMouseOver == true && _experienceBar.Visible) ||
             (_shieldBar?.IsMouseOver == true && _shieldBar.Visible) ||
             (_abilityBar?.IsMouseOver == true && _abilityBar.Visible) ||
             Controls.Any(c => c is HudTextureButton b && b.ShowHelp));

        public void DrawExperienceTooltip(GameTime gameTime)
        {
            if (!HasExperienceTooltip) return;
            _experienceBar?.DrawTooltip(gameTime);
            _shieldBar?.DrawTooltip(gameTime);
            _abilityBar?.DrawTooltip(gameTime);
            foreach (var control in Controls)
                if (control is HudTextureButton button) button.DrawHelp(gameTime);
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

            button.ConfigureSkin(info.Name);
            return button;
        }

        // =============================================================
        // HUD BUTTON
        // =============================================================

        private sealed class HudTextureButton :
            TextureControl
        {
            private readonly Texture2D[] _states = new Texture2D[4];
            private string _skin;
            private string _helpKey;
            private string _helpText;
            public bool ShowHelp => Visible && IsMouseOver && _helpText != null &&
                Scene?.IsPointerInputBlockedFor(this) != true;

            public void ConfigureSkin(string name)
            {
                var settings = name switch
                {
                    "SettingsButton" => (1, "Esc", "Sistema"),
                    "button5" => (2, "", "Community"),
                    "button4" => (3, "T", "Misiones"),
                    "InventoryButton" => (4, "I", "Inventario"),
                    "button2" => (5, "C", "Personaje"),
                    "Button_1" => (6, "", "Tienda"),
                    _ => (0, "", "Cambiar barra de habilidades")
                };
                if (settings.Item1 != 0)
                    _skin = $"Interface/GFx/button_btn{settings.Item1:00}_";
                _helpKey = settings.Item2;
                _helpText = settings.Item3;
            }

            public override async Task Load()
            {
                await base.Load();
                if (_skin == null) return;
                for (int i = 0; i < _states.Length; i++)
                    _states[i] = await TextureLoader.Instance.PrepareAndGetTexture(_skin + (i + 1) + ".ozd");
            }

            public override void Draw(GameTime gameTime)
            {
                if (!Visible || Status != GameControlStatus.Ready) return;
                int state = !Interactive ? 3 : IsMousePressed ? 2 : IsMouseOver ? 1 : 0;
                var texture = _states[state] ?? _states[0];
                if (texture == null) { base.Draw(gameTime); return; }
                // Whole, transparent GFx sprites include the original illuminated rim.
                GraphicsManager.Instance.Sprite.Draw(texture, DisplayRectangle, Color.White * Alpha);
            }

            public void DrawHelp(GameTime gameTime)
            {
                if (!ShowHelp || Texture == null) return;
                var font = GraphicsManager.Instance.Font;
                if (font == null) return;
                float scale = 12f / Constants.BASE_FONT_SIZE;
                var keySize = font.MeasureString(_helpKey) * scale;
                var textSize = font.MeasureString(_helpText) * scale;
                float textOffset = string.IsNullOrEmpty(_helpKey) ? 10f : Math.Max(30f, keySize.X + 16f);
                int width = Math.Max(72, (int)Math.Ceiling(textOffset + textSize.X + 12));
                var bounds = DisplayRectangle;
                int x = Math.Clamp(bounds.Center.X - width / 2, 0, Math.Max(0, UiScaler.VirtualSize.X - width));
                var box = new Rectangle(x, Math.Max(0, bounds.Top - 30), width, 28);
                var sprite = GraphicsManager.Instance.Sprite;
                NativeHudMenuSkin.DrawHelp(sprite, Texture, box);
                sprite.DrawString(font, _helpKey, new Vector2(box.X + 8, box.Center.Y - keySize.Y / 2),
                    new Color(254, 42, 42), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
                sprite.DrawString(font, _helpText, new Vector2(box.X + textOffset, box.Center.Y - textSize.Y / 2),
                    Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }

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
