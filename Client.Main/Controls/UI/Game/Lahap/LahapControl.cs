using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Client;
using Client.Main.Core.Items;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game
{
    /// <summary>Lahap's original popup flow, backed by the server's NPC session.</summary>
    public sealed class LahapControl : UIControl, IUiTexturePreloadable
    {
        private const int WindowWidth = 300;
        private const int BundlesPerPage = 4;
        private const string ButtonAtlasPath = "Interface/GFx/Popups_I1.ozd";
        private const string DividerPath = "Interface/GFx/popup_line_m.ozd";
        // Verified against Data_Broyal Popups_I1: 74x36 button states.
        private static readonly Rectangle ButtonNormal = new(78, 0, 74, 36);
        private static readonly Rectangle ButtonHover = new(0, 0, 74, 36);
        private static readonly Rectangle ButtonPressed = new(156, 0, 74, 36);
        private static readonly Color Gold = new(224, 189, 35);
        private static readonly Color Body = new(185, 185, 185);
        private static readonly Color DialogBackground = new(30, 22, 12, 180);

        private enum Page { Menu, Jewels, Quantities, Bundles, Confirm, Waiting, Result }
        private readonly record struct Button(int Action, Rectangle Bounds, string Text, bool Enabled = true, bool Row = false);
        private sealed record Pending(bool Dismantle, LahapJewels.Jewel Jewel, int Quantity,
            int Singles, int Packs, uint Zen, uint Fee);

        private static LahapControl _instance;
        public static LahapControl Instance => _instance ??= new LahapControl();
        public static bool IsOpen => _instance?.Visible == true;
        public override bool NonDisposable => true;
        public override bool BlocksPointerInput => true;

        private readonly Texture2D[] _frameTiles = new Texture2D[9];
        private Texture2D _buttonAtlas, _divider;
        private RenderTarget2D _surface;
        private BlendState _frameBlend;
        private readonly List<Button> _buttons = new();
        private readonly List<LahapJewels.InventoryEntry> _bundles = new();
        private List<LahapJewels.InventoryEntry> _inventory = new();
        private CharacterState _state;
        private Page _page, _returnPage;
        private LahapJewels.Jewel _jewel;
        private LahapJewels.InventoryEntry _confirmedBundle;
        private int _quantity = 10, _bundlePage, _selectedBundleSlot = -1;
        private int _hovered = -1, _pressed = -1, _inventoryDirty = 1, _session;
        private bool _dismantle, _closeOnResult;
        private Pending _pending;
        private double _pendingSeconds;
        private string _message = string.Empty;
        private string[] _messageLines = Array.Empty<string>();

        private LahapControl()
        {
            ControlSize = new Point(WindowWidth, 230);
            ViewSize = ControlSize;
            AutoViewSize = false;
            Interactive = true;
            Visible = false;
            Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            for (int i = 1; i <= 9; i++) yield return $"Interface/GFx/popupbg{i:00}.ozd";
            yield return ButtonAtlasPath;
            yield return DividerPath;
        }

        public override async Task Load()
        {
            await base.Load();
            var tasks = new List<Task<Texture2D>>();
            foreach (string path in GetPreloadTexturePaths())
                tasks.Add(TextureLoader.Instance.PrepareAndGetTexture(path));
            var textures = await Task.WhenAll(tasks);
            for (int i = 0; i < 9; i++) _frameTiles[i] = textures[i];
            _buttonAtlas = textures[9];
            _divider = textures[10];
        }

        public void ShowWindow()
        {
            HideWithoutServerRequest();
            _state = MuGame.Network?.GetCharacterState();
            if (_state == null) return;
            _state.InventoryChanged += MarkInventoryDirty;
            _state.MoneyChanged += MarkInventoryDirty;
            _pending = null;
            _bundlePage = 0;
            _selectedBundleSlot = -1;
            RefreshInventory();
            SetPage(Page.Menu);
            Visible = true;
            BringToFront();
            if (Scene != null) Scene.FocusControl = this;
            SoundController.Instance.PlayBuffer("Sound/iCreateWindow.wav");
        }

        public void HideWithoutServerRequest()
        {
            _session++;
            if (_state != null)
            {
                _state.InventoryChanged -= MarkInventoryDirty;
                _state.MoneyChanged -= MarkInventoryDirty;
            }
            _state = null;
            _pending = null;
            _pressed = _hovered = -1;
            Visible = false;
            if (Scene?.FocusControl == this) Scene.FocusControl = null;
        }

        public void CloseWindow()
        {
            if (!Visible) return;
            HideWithoutServerRequest();
            Scene?.SetMouseInputConsumed();
            var service = MuGame.Network?.GetCharacterService();
            if (service != null) _ = service.SendCloseNpcRequestAsync();
        }

        private void MarkInventoryDirty() => Interlocked.Exchange(ref _inventoryDirty, 1);

        private void RefreshInventory()
        {
            if (_state == null) return;
            _inventory = LahapJewels.ReadInventory(_state.GetInventoryItems(), _state.InventoryExpansionState);
            _bundles.Clear();
            foreach (var item in _inventory)
                if (LahapJewels.GetPackType(item) != null) _bundles.Add(item);
            _bundlePage = Math.Clamp(_bundlePage, 0, Math.Max(0, (_bundles.Count - 1) / BundlesPerPage));
            if (SelectedBundle() == null)
                _selectedBundleSlot = _bundles.Count > 0 ? _bundles[0].Slot : -1;
            if (_page == Page.Bundles) BuildButtons();
        }

        private LahapJewels.InventoryEntry SelectedBundle()
        {
            for (int i = 0; i < _bundles.Count; i++)
                if (_bundles[i].Slot == _selectedBundleSlot) return _bundles[i];
            return null;
        }

        private void SetPage(Page page)
        {
            _page = page;
            _pressed = _hovered = -1;
            int height = page is Page.Jewels or Page.Quantities ? 400
                : page is Page.Confirm or Page.Waiting or Page.Result ? 185 : page == Page.Menu ? 230 : 270;
            ControlSize = new Point(WindowWidth, height);
            ViewSize = ControlSize;
            AlignControl();
            BuildButtons();
            BuildMessageLines();
        }

        private void BuildButtons()
        {
            _buttons.Clear();
            switch (_page)
            {
                case Page.Menu:
                    _buttons.Add(new(0, new(88, 96, 124, 32), "Jewel combination"));
                    _buttons.Add(new(1, new(88, 136, 124, 32), "Dismantle jewel"));
                    break;
                case Page.Jewels:
                    for (int i = 0; i < LahapJewels.Types.Count; i++)
                        _buttons.Add(new(10 + i, new(i % 2 == 0 ? 44 : 168, 108 + i / 2 * 42, 92, 32), LahapJewels.Types[i].Name));
                    break;
                case Page.Quantities:
                    for (int i = 0; i < 3; i++)
                        _buttons.Add(new(20 + i, new(104, 92 + i * 67, 92, 32), $"{(i + 1) * 10} pieces combine"));
                    break;
                case Page.Bundles:
                    int start = _bundlePage * BundlesPerPage;
                    bool selectionVisible = false;
                    for (int i = start; i < Math.Min(start + BundlesPerPage, _bundles.Count); i++)
                        selectionVisible |= _bundles[i].Slot == _selectedBundleSlot;
                    if (!selectionVisible && start < _bundles.Count) _selectedBundleSlot = _bundles[start].Slot;
                    for (int i = start; i < Math.Min(start + BundlesPerPage, _bundles.Count); i++)
                    {
                        var bundle = _bundles[i];
                        var type = LahapJewels.GetPackType(bundle);
                        _buttons.Add(new(100 + i, new(20, 88 + (i - start) * 22, 260, 22),
                            $"{type.Name} Bundle,  {(bundle.Level + 1) * 10}", true, true));
                    }
                    _buttons.Add(new(30, new(104, 176, 92, 32), "Disband", SelectedBundle() != null));
                    if (_bundles.Count > BundlesPerPage)
                    {
                        _buttons.Add(new(31, new(32, 184, 40, 24), "<", _bundlePage > 0));
                        _buttons.Add(new(32, new(228, 184, 40, 24), ">", start + BundlesPerPage < _bundles.Count));
                    }
                    break;
                case Page.Confirm:
                    _buttons.Add(new(80, new(76, 137, 70, 32), "OK"));
                    _buttons.Add(new(81, new(154, 137, 70, 32), "Cancel"));
                    break;
                case Page.Result:
                    _buttons.Add(new(82, new(115, 137, 70, 32), "OK"));
                    break;
            }
            if (_page is Page.Menu or Page.Jewels or Page.Quantities or Page.Bundles)
                _buttons.Add(new(90, new(118, ControlSize.Y - 44, 64, 28), "Close"));
        }

        private void Activate(int action)
        {
            if (_pending != null) return;
            SoundController.Instance.PlayBuffer("Sound/iButton.wav");
            switch (action)
            {
                case 0: _dismantle = false; SetPage(Page.Jewels); break;
                case 1: _dismantle = true; RefreshInventory(); SetPage(Page.Bundles); break;
                case >= 10 and < 20:
                    _jewel = LahapJewels.Types[action - 10]; SetPage(Page.Quantities); break;
                case >= 20 and < 23:
                    _quantity = (action - 19) * 10;
                    _returnPage = Page.Quantities;
                    _message = $"Are you sure to combine {_jewel.Name} x {_quantity}?\nCombination cost: {LahapJewels.CombineFee(_quantity)} Zen";
                    SetPage(Page.Confirm);
                    break;
                case 30:
                    var bundle = SelectedBundle();
                    if (bundle == null) return;
                    _confirmedBundle = bundle;
                    _jewel = LahapJewels.GetPackType(bundle);
                    _quantity = (bundle.Level + 1) * 10;
                    _returnPage = Page.Bundles;
                    _message = $"Are you sure to dismantle {_jewel.Name} Bundle x {_quantity}?\nDismantle cost: {LahapJewels.DismantleFee} Zen";
                    SetPage(Page.Confirm);
                    break;
                case 31: _bundlePage--; BuildButtons(); break;
                case 32: _bundlePage++; BuildButtons(); break;
                case 80: _ = SubmitAsync(); break;
                case 81: SetPage(_returnPage); break;
                case 82:
                    if (_closeOnResult) CloseWindow();
                    else SetPage(_returnPage);
                    break;
                case 90: CloseWindow(); break;
                case >= 100:
                    int index = action - 100;
                    if (index < _bundles.Count) _selectedBundleSlot = _bundles[index].Slot;
                    break;
            }
        }

        private async Task SubmitAsync()
        {
            if (_pending != null || _state == null || _jewel == null) return;
            RefreshInventory(); // Revalidate after the confirmation was displayed.
            uint fee = _dismantle ? LahapJewels.DismantleFee : LahapJewels.CombineFee(_quantity);
            if (InventoryControl.Instance._pickedItemRenderer.Item != null || ExtendedInventoryControl.PickedItem != null ||
                _state.PendingMoveFromSlot.HasValue || _state.HasPendingTradeMove)
            { ShowResult("Put down the item before using Lahap."); return; }
            if (_state.InventoryZen < fee)
            { ShowResult("You do not have enough Zen."); return; }
            if (!_dismantle && LahapJewels.CountSingles(_inventory, _jewel) < _quantity)
            { ShowResult("You do not have enough jewels."); return; }
            LahapJewels.InventoryEntry bundle = null;
            for (int i = 0; i < _inventory.Count; i++)
                if (_inventory[i] == _confirmedBundle) bundle = _inventory[i];
            if (_dismantle && (bundle == null || LahapJewels.GetPackType(bundle)?.MixId != _jewel.MixId ||
                (bundle.Level + 1) * 10 != _quantity))
            { ShowResult("The selected bundle is no longer available."); return; }
            // OpenMU checks free cells BEFORE removing the bundle.
            if (_dismantle && LahapJewels.FreeCells(_inventory, _state.InventoryExpansionState) < _quantity)
            { ShowResult("There is not enough space in your inventory."); return; }
            var service = MuGame.Network?.GetCharacterService();
            if (service == null) { ShowResult("The connection is not available.", true); return; }
            _pending = new(_dismantle, _jewel, _quantity, LahapJewels.CountSingles(_inventory, _jewel),
                LahapJewels.CountPacks(_inventory, _jewel, _quantity / 10 - 1), _state.InventoryZen, fee);
            _pendingSeconds = 0;
            int session = _session;
            byte slot = _dismantle ? bundle.Slot : (byte)0;
            _message = "Waiting for the server...";
            SetPage(Page.Waiting);
            bool sent = await service.SendLahapJewelRequestAsync(_dismantle, _jewel.MixId, (byte)(_quantity / 10 - 1), slot);
            if (!sent) MuGame.ScheduleOnMainThread(() =>
            {
                if (Visible && session == _session && _pending != null)
                    ShowResult("The request could not be confirmed.\nClose and reopen Lahap before retrying.", true);
            });
        }

        public void ReceiveServerMessage(string message)
        {
            // OpenMU reports Lahap validation failures as localized blue messages.
            if (Visible && _pending != null && !string.IsNullOrWhiteSpace(message)) ShowResult(message);
        }

        private void CheckCompletion()
        {
            var request = _pending;
            if (request == null || _state == null) return;
            int singles = LahapJewels.CountSingles(_inventory, request.Jewel);
            int packs = LahapJewels.CountPacks(_inventory, request.Jewel, request.Quantity / 10 - 1);
            bool transformed = request.Dismantle
                ? singles == request.Singles + request.Quantity && packs == request.Packs - 1
                : singles == request.Singles - request.Quantity && packs == request.Packs + 1;
            if (transformed && _state.InventoryZen == request.Zen - request.Fee)
            {
                _returnPage = request.Dismantle ? Page.Bundles : Page.Menu;
                ShowResult(request.Dismantle
                    ? "Congratulations! You have successfully\ndismantled the jewels."
                    : "Congratulations! You have successfully\ncombined the jewels.");
            }
        }

        private void ShowResult(string message, bool closeSession = false)
        {
            _pending = null;
            _message = message;
            _closeOnResult = closeSession;
            SetPage(Page.Result);
        }

        private void BuildMessageLines()
        {
            string text = _page switch
            {
                Page.Menu => "You can combine or dissolve\nvarious jewels.",
                Page.Jewels => "Select a jewel to combine.",
                Page.Quantities => "Choose a number button to combine.",
                Page.Bundles => _bundles.Count == 0 ? "There are no jewel bundles in your inventory." : string.Empty,
                _ => _message
            };
            var font = GraphicsManager.Instance?.Font;
            if (font == null) { _messageLines = text.Split('\n'); return; }
            var lines = new List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                string line = string.Empty;
                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string next = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && font.MeasureString(next).X * 0.36f > WindowWidth - 32)
                    { lines.Add(line); line = word; }
                    else line = next;
                }
                lines.Add(line);
            }
            _messageLines = lines.ToArray();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (Interlocked.Exchange(ref _inventoryDirty, 0) != 0)
            {
                RefreshInventory();
                CheckCompletion();
            }
            if (_pending != null)
            {
                _pendingSeconds += gameTime.ElapsedGameTime.TotalSeconds;
                if (_pendingSeconds >= 20)
                    ShowResult("No confirmation was received.\nClose and reopen Lahap before retrying.", true);
            }
            if (MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
            {
                Scene?.ConsumeKeyboardEscape();
                if (_pending == null) CloseWindow();
                return;
            }
            var mouse = MuGame.Instance.UiMouseState;
            var previous = MuGame.Instance.PrevUiMouseState;
            bool down = mouse.LeftButton == ButtonState.Pressed;
            bool inside = DisplayRectangle.Contains(mouse.Position);
            _hovered = -1;
            if (inside)
            {
                Scene?.SetMouseInputConsumed();
                for (int i = 0; i < _buttons.Count; i++)
                    if (_buttons[i].Enabled && Screen(_buttons[i].Bounds).Contains(mouse.Position)) _hovered = i;
            }
            if (down && previous.LeftButton == ButtonState.Released) _pressed = _hovered;
            if (!down && previous.LeftButton == ButtonState.Pressed)
            {
                int pressed = _pressed;
                _pressed = -1;
                if (pressed >= 0 && pressed == _hovered) Activate(_buttons[pressed].Action);
            }
        }

        private Rectangle Screen(Rectangle rect)
        {
            var origin = DisplayRectangle.Location;
            int left = origin.X + (int)MathF.Round(rect.Left * Scale);
            int top = origin.Y + (int)MathF.Round(rect.Top * Scale);
            int right = origin.X + (int)MathF.Round(rect.Right * Scale);
            int bottom = origin.Y + (int)MathF.Round(rect.Bottom * Scale);
            return new(left, top, right - left, bottom - top);
        }

        private void EnsureSurface(SpriteBatch sb)
        {
            var gd = MuGame.Instance.GraphicsDevice;
            _frameBlend ??= new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.InverseSourceAlpha
            };
            if (_surface != null && !_surface.IsDisposed && _surface.Height == ControlSize.Y) return;
            _surface?.Dispose();
            _surface = new RenderTarget2D(gd, WindowWidth, ControlSize.Y, false, SurfaceFormat.Color, DepthFormat.None);
            var targets = gd.GetRenderTargets();
            var viewport = gd.Viewport;
            try
            {
                // End the outer batch before changing the target, and flush this
                // batch before restoring it. Cached pixels use premultiplied alpha.
                using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, _frameBlend, SamplerState.PointClamp))
                {
                    gd.SetRenderTarget(_surface);
                    gd.Clear(Color.Transparent);
                    int[] xs = { 0, 16, WindowWidth - 16, WindowWidth };
                    int[] ys = { 0, 16, ControlSize.Y - 16, ControlSize.Y };
                    for (int y = 0; y < 3; y++)
                    for (int x = 0; x < 3; x++)
                    {
                        var texture = _frameTiles[y * 3 + x];
                        var target = new Rectangle(xs[x], ys[y], xs[x + 1] - xs[x], ys[y + 1] - ys[y]);
                        if (y == 1 && x == 1)
                        {
                            // A black texture cannot be tinted brown by multiplication.
                            // Draw a warm, darker backdrop while keeping the original border.
                            sb.Draw(GraphicsManager.Instance.Pixel, target, DialogBackground);
                        }
                        else if (texture != null)
                        {
                            sb.Draw(texture, target, Color.White);
                        }
                    }
                    if (_divider != null) sb.Draw(_divider, new Rectangle(16, 37, 268, 9), Color.White);
                }
            }
            finally
            {
                gd.SetRenderTargets(targets);
                gd.Viewport = viewport;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || Status != GameControlStatus.Ready) return;
            var manager = GraphicsManager.Instance;
            var sb = manager?.Sprite;
            var font = manager?.Font;
            if (sb == null || font == null) return;
            EnsureSurface(sb);
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, transform: UiScaler.SpriteTransform))
                sb.Draw(_surface, DisplayRectangle, Color.White * Alpha);
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp, transform: UiScaler.SpriteTransform))
            {
                for (int i = 0; i < _buttons.Count; i++)
                {
                    var button = _buttons[i];
                    if (button.Row || _buttonAtlas == null) continue;
                    var source = i == _pressed && i == _hovered ? ButtonPressed : i == _hovered ? ButtonHover : ButtonNormal;
                    NewUiFrameRenderer.DrawNineSlice(sb, _buttonAtlas, source, Screen(button.Bounds), 6, 6, 6, 6);
                }
            }
            using (new SpriteBatchScope(sb, SpriteSortMode.Deferred, BlendState.AlphaBlend,
                GraphicsManager.GetQualityLinearSamplerState(), transform: UiScaler.SpriteTransform))
            {
                if (_page is not (Page.Confirm or Page.Waiting or Page.Result))
                    DrawText(sb, font, "Jewel combination", new Rectangle(12, 17, 276, 22), Gold, 0.38f);
                for (int i = 0; i < _messageLines.Length; i++)
                    DrawText(sb, font, _messageLines[i], new Rectangle(16, 53 + i * 16, 268, 18), Body, 0.36f);
                if (_page == Page.Quantities)
                    for (int i = 0; i < 3; i++)
                        DrawText(sb, font, $"{LahapJewels.CombineFee((i + 1) * 10).ToString("N0", CultureInfo.InvariantCulture)} Zen required",
                            new Rectangle(16, 127 + i * 67, 268, 22), Gold, 0.36f);
                for (int i = 0; i < _buttons.Count; i++)
                {
                    var button = _buttons[i];
                    bool selected = button.Row && button.Action - 100 < _bundles.Count &&
                        _bundles[button.Action - 100].Slot == _selectedBundleSlot;
                    bool gold = selected || i == _hovered || _page == Page.Jewels ||
                        _page == Page.Quantities || button.Action == 30;
                    Color color = !button.Enabled ? Color.Gray : gold ? Gold : Body;
                    DrawText(sb, font, button.Text, button.Bounds, color, 0.36f, button.Row);
                }
            }
        }

        private void DrawText(SpriteBatch sb, SpriteFont font, string text, Rectangle local, Color color,
            float scale, bool left = false)
        {
            var rect = Screen(local);
            Vector2 measured = font.MeasureString(text);
            float fitted = Math.Min(scale * Scale, (rect.Width - 8) / Math.Max(1, measured.X));
            Vector2 size = measured * fitted;
            Vector2 position = new(left ? rect.X + 3 : rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2);
            sb.DrawString(font, text, position + Vector2.One, Color.Black * Alpha, 0, Vector2.Zero, fitted, SpriteEffects.None, 0);
            sb.DrawString(font, text, position, color * Alpha, 0, Vector2.Zero, fitted, SpriteEffects.None, 0);
        }

        public override void Dispose()
        {
            HideWithoutServerRequest();
            _surface?.Dispose();
            _surface = null;
            _frameBlend?.Dispose();
            _frameBlend = null;
            base.Dispose();
        }
    }
}
