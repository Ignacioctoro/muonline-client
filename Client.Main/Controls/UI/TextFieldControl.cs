using Client.Main.Content;
using TextCopy;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Client.Main.Controls.UI
{
    public enum TextFieldSkin
    {
        Flat,
        NineSlice
    }

    public class TextFieldControl : UIControl, IUiTexturePreloadable
    {
        public static Type ControlType = typeof(TextFieldControl);

        public static TextFieldControl Create()
        {
            return (TextFieldControl)Activator.CreateInstance(ControlType, true);
        }

        protected readonly StringBuilder _inputText = new();
        private double _cursorBlinkTimer;
        private bool _showCursor;
        private float _scrollOffset;
        private static readonly RasterizerState ScissorRasterizer = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        // Real caret/selection state.
        private int _cursorIndex;
        private int _selectionAnchor = -1;
        private bool _mouseSelecting;

        private const int TextMargin = 5;
        private const int CursorBlinkInterval = 500;

        private Texture2D[] _nineSlice = new Texture2D[9];
        private static readonly string[] s_nineSliceSuffixes =
        {
            "01", "02", "03", "04", "05", "06", "07", "08", "09"
        };

        private static readonly ILogger _logger = MuGame.AppLoggerFactory?.CreateLogger<TextFieldControl>();

        // Desktop-only TextInput subscription is done via reflection.
        // Client.Main is shared with Android, while the Android MonoGame
        // GameWindow runtime does not expose the same TextInput event accessors.
        // Keeping a direct GameWindow.TextInput += / -= reference here causes
        // MissingMethodException on Android.
        private EventInfo _desktopTextInputEvent;
        private Delegate _desktopTextInputHandler;
        private object _desktopTextInputWindow;

        public TextFieldSkin Skin { get; set; } = TextFieldSkin.Flat;
        public Color TextColor { get; set; } = Color.White;
        public float FontSize { get; set; } = 12f;
        public bool IsFocused { get; private set; }
        public string Label { get; set; }
        public string Placeholder { get; set; }

        public int MaxLength { get; set; } = int.MaxValue;

        public string Value
        {
            get => _inputText.ToString();
            set
            {
                _inputText.Clear();

                string newValue = value ?? string.Empty;

                if (newValue.Length > MaxLength)
                    newValue = newValue.Substring(0, MaxLength);

                _inputText.Append(newValue);

                _cursorIndex = _inputText.Length;
                ClearSelection();
                UpdateScrollOffset();
                ResetCursorBlink();
            }
        }

        public bool MaskValue { get; set; }
        public event EventHandler ValueChanged;
        public event EventHandler EnterKeyPressed;

        protected TextFieldControl()
        {
            AutoViewSize = false;
            ViewSize = new Point(176, 14);
            Interactive = true;
            IsFocused = false;
            _cursorIndex = 0;
        }

        public IEnumerable<string> GetPreloadTexturePaths()
        {
            if (Skin != TextFieldSkin.NineSlice)
                yield break;

            for (int i = 0; i < s_nineSliceSuffixes.Length; i++)
            {
                yield return $"Interface/GFx/textbg{s_nineSliceSuffixes[i]}.ozd";
            }
        }

        public override async Task Load()
        {
            await base.Load();

            if (Skin == TextFieldSkin.NineSlice)
            {
                for (int i = 0; i < s_nineSliceSuffixes.Length; i++)
                {
                    _nineSlice[i] = await TextureLoader.Instance.PrepareAndGetTexture(
                        $"Interface/GFx/textbg{s_nineSliceSuffixes[i]}.ozd");
                }
            }
        }

        public override void OnFocus()
        {
            if (IsFocused) return;

            base.OnFocus();
            IsFocused = true;
            ResetCursorBlink();

            if (Scene != null)
                Scene.FocusControl = this;

            _logger?.LogDebug("TextFieldControl: OnFocus called.");

            SubscribeDesktopTextInput();
        }

        public override void OnBlur()
        {
            if (!IsFocused) return;

            UnsubscribeDesktopTextInput();

            base.OnBlur();

            IsFocused = false;
            _showCursor = false;
            _cursorBlinkTimer = 0;
            _mouseSelecting = false;

            _logger?.LogDebug("TextFieldControl: OnBlur called.");
        }

        private void SubscribeDesktopTextInput()
        {
            // Android/iOS use their platform-specific TextFieldControl subclass.
            // Do not touch GameWindow.TextInput there.
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
                return;

            var window = MuGame.Instance?.GameWindow;
            if (window == null)
                return;

            try
            {
                var eventInfo = window.GetType().GetEvent(
                    "TextInput",
                    BindingFlags.Instance | BindingFlags.Public);

                if (eventInfo == null || eventInfo.EventHandlerType == null)
                    return;

                // Clear a previous subscription first, if there is one.
                UnsubscribeDesktopTextInput();

                var handler = Delegate.CreateDelegate(
                    eventInfo.EventHandlerType,
                    this,
                    nameof(OnDesktopTextInput));

                eventInfo.AddEventHandler(window, handler);

                _desktopTextInputEvent = eventInfo;
                _desktopTextInputHandler = handler;
                _desktopTextInputWindow = window;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Could not subscribe to desktop GameWindow.TextInput.");
            }
        }

        private void UnsubscribeDesktopTextInput()
        {
            if (_desktopTextInputEvent == null ||
                _desktopTextInputHandler == null ||
                _desktopTextInputWindow == null)
            {
                return;
            }

            try
            {
                _desktopTextInputEvent.RemoveEventHandler(
                    _desktopTextInputWindow,
                    _desktopTextInputHandler);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Could not unsubscribe from desktop GameWindow.TextInput.");
            }
            finally
            {
                _desktopTextInputEvent = null;
                _desktopTextInputHandler = null;
                _desktopTextInputWindow = null;
            }
        }

        public new void Focus() => OnFocus();
        public new void Blur() => OnBlur();

        public void MoveCursorToEnd()
        {
            _cursorIndex = _inputText.Length;
            ClearSelection();
            UpdateScrollOffset();
            ResetCursorBlink();
        }

        private bool HasSelection =>
            _selectionAnchor >= 0 && _selectionAnchor != _cursorIndex;

        private int SelectionStart =>
            HasSelection ? Math.Min(_selectionAnchor, _cursorIndex) : _cursorIndex;

        private int SelectionEnd =>
            HasSelection ? Math.Max(_selectionAnchor, _cursorIndex) : _cursorIndex;

        private int SelectionLength => SelectionEnd - SelectionStart;

        private void ClearSelection()
        {
            _selectionAnchor = -1;
        }

        private void ResetCursorBlink()
        {
            _showCursor = true;
            _cursorBlinkTimer = 0;
        }

        private void BeginKeyboardSelectionIfNeeded()
        {
            if (_selectionAnchor < 0)
                _selectionAnchor = _cursorIndex;
        }

        private void SetCursorIndex(int index, bool extendSelection)
        {
            int oldCursor = _cursorIndex;

            if (extendSelection)
            {
                if (_selectionAnchor < 0)
                    _selectionAnchor = oldCursor;
            }
            else
            {
                ClearSelection();
            }

            _cursorIndex = Math.Clamp(index, 0, _inputText.Length);
            UpdateScrollOffset();
            ResetCursorBlink();
        }

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            int start = SelectionStart;
            int length = SelectionLength;

            _inputText.Remove(start, length);
            _cursorIndex = start;
            ClearSelection();
            return true;
        }

        private void InsertTextAtCursor(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Si hay texto seleccionado, ese espacio volverá a quedar disponible.
            int selectionLength = HasSelection ? SelectionLength : 0;
            int lengthAfterRemovingSelection = _inputText.Length - selectionLength;

            int available = MaxLength - lengthAfterRemovingSelection;

            // Ya alcanzamos el máximo.
            if (available <= 0)
                return;

            // Si escribimos o pegamos más de lo disponible,
            // solamente aceptamos lo que quepa.
            if (text.Length > available)
                text = text.Substring(0, available);

            DeleteSelection();

            _inputText.Insert(_cursorIndex, text);
            _cursorIndex += text.Length;

            UpdateScrollOffset();
            ResetCursorBlink();
            OnValueChanged();
        }
        private string GetDisplayText()
        {
            return MaskValue
                ? new string('*', _inputText.Length)
                : _inputText.ToString();
        }

        private float MeasureTextToIndex(int index)
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null || index <= 0)
                return 0f;

            string displayText = GetDisplayText();
            index = Math.Clamp(index, 0, displayText.Length);

            float scale = FontSize / Constants.BASE_FONT_SIZE;
            return font.MeasureString(displayText.Substring(0, index)).X * scale;
        }

        protected void UpdateScrollOffset()
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null) return;

            string displayText = GetDisplayText();
            float scale = FontSize / Constants.BASE_FONT_SIZE;
            float totalWidth = font.MeasureString(displayText).X * scale;
            float maxVisibleWidth = Math.Max(0f, DisplayRectangle.Width - TextMargin * 2f);
            float caretX = MeasureTextToIndex(_cursorIndex);

            if (caretX - _scrollOffset > maxVisibleWidth)
                _scrollOffset = caretX - maxVisibleWidth;
            else if (caretX - _scrollOffset < 0)
                _scrollOffset = caretX;

            float maxScroll = Math.Max(0f, totalWidth - maxVisibleWidth);
            _scrollOffset = Math.Clamp(_scrollOffset, 0f, maxScroll);
        }

        protected void OnEnterKeyPressed()
        {
            EnterKeyPressed?.Invoke(this, EventArgs.Empty);
        }

        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private string GetSelectedText()
        {
            if (!HasSelection || MaskValue)
                return string.Empty;

            return _inputText.ToString(SelectionStart, SelectionLength);
        }

        private void CopySelection()
        {
            string selected = GetSelectedText();
            if (string.IsNullOrEmpty(selected))
                return;

            try
            {
                ClipboardService.SetText(selected);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo copiar texto al portapapeles.");
            }
        }

        private void CutSelection()
        {
            if (!HasSelection || MaskValue)
                return;

            CopySelection();

            if (DeleteSelection())
            {
                UpdateScrollOffset();
                ResetCursorBlink();
                OnValueChanged();
            }
        }

        private void PasteClipboard()
        {
            try
            {
                string clipboardText = ClipboardService.GetText();
                if (string.IsNullOrEmpty(clipboardText))
                    return;

                // The MU chat field is single-line.
                clipboardText = clipboardText
                    .Replace("\r\n", " ")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ');

                InsertTextAtCursor(clipboardText);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "No se pudo pegar texto desde el portapapeles.");
            }
        }

        private void SelectAll()
        {
            if (_inputText.Length == 0)
                return;

            _selectionAnchor = 0;
            _cursorIndex = _inputText.Length;
            UpdateScrollOffset();
            ResetCursorBlink();
        }

        private bool HandleControlKey(Keys key, bool shift, bool ctrl)
        {
            // Standard clipboard shortcuts.
            if (ctrl)
            {
                switch (key)
                {
                    case Keys.A:
                        SelectAll();
                        return true;

                    case Keys.C:
                        CopySelection();
                        return true;

                    case Keys.X:
                        CutSelection();
                        return true;

                    case Keys.V:
                        PasteClipboard();
                        return true;
                }
            }

            switch (key)
            {
                case Keys.Left:
                    if (!shift && HasSelection)
                    {
                        SetCursorIndex(SelectionStart, false);
                    }
                    else
                    {
                        SetCursorIndex(_cursorIndex - 1, shift);
                    }
                    return true;

                case Keys.Right:
                    if (!shift && HasSelection)
                    {
                        SetCursorIndex(SelectionEnd, false);
                    }
                    else
                    {
                        SetCursorIndex(_cursorIndex + 1, shift);
                    }
                    return true;

                case Keys.Home:
                    SetCursorIndex(0, shift);
                    return true;

                case Keys.End:
                    SetCursorIndex(_inputText.Length, shift);
                    return true;

                case Keys.Back:
                    if (DeleteSelection())
                    {
                        UpdateScrollOffset();
                        ResetCursorBlink();
                        OnValueChanged();
                    }
                    else if (_cursorIndex > 0)
                    {
                        _inputText.Remove(_cursorIndex - 1, 1);
                        _cursorIndex--;
                        UpdateScrollOffset();
                        ResetCursorBlink();
                        OnValueChanged();
                    }
                    return true;

                case Keys.Delete:
                    if (DeleteSelection())
                    {
                        UpdateScrollOffset();
                        ResetCursorBlink();
                        OnValueChanged();
                    }
                    else if (_cursorIndex < _inputText.Length)
                    {
                        _inputText.Remove(_cursorIndex, 1);
                        UpdateScrollOffset();
                        ResetCursorBlink();
                        OnValueChanged();
                    }
                    return true;

                case Keys.Enter:
                    OnEnterKeyPressed();
                    OnValueChanged();
                    return true;
            }

            return false;
        }

        private int GetCharacterIndexFromMouseX(int mouseX)
        {
            var font = GraphicsManager.Instance?.Font;
            if (font == null || _inputText.Length == 0)
                return 0;

            string displayText = GetDisplayText();
            float scale = FontSize / Constants.BASE_FONT_SIZE;
            float localX = mouseX - (DisplayRectangle.X + TextMargin) + _scrollOffset;

            if (localX <= 0)
                return 0;

            float previousWidth = 0f;
            for (int i = 1; i <= displayText.Length; i++)
            {
                float currentWidth = font.MeasureString(displayText.Substring(0, i)).X * scale;
                float midpoint = previousWidth + (currentWidth - previousWidth) * 0.5f;

                if (localX < midpoint)
                    return i - 1;

                previousWidth = currentWidth;
            }

            return displayText.Length;
        }

        private void HandleMouseSelection()
        {
            var mouse = CurrentMouseState;
            var previousMouse = PreviousMouseState;
            bool inside = DisplayRectangle.Contains(mouse.Position);

            if (mouse.LeftButton == ButtonState.Pressed &&
                previousMouse.LeftButton == ButtonState.Released &&
                inside)
            {
                int index = GetCharacterIndexFromMouseX(mouse.X);

                bool shift = MuGame.Instance.Keyboard.IsKeyDown(Keys.LeftShift) ||
                             MuGame.Instance.Keyboard.IsKeyDown(Keys.RightShift);

                if (shift)
                {
                    BeginKeyboardSelectionIfNeeded();
                    _cursorIndex = index;
                }
                else
                {
                    _cursorIndex = index;
                    _selectionAnchor = index;
                }

                _mouseSelecting = true;
                UpdateScrollOffset();
                ResetCursorBlink();
            }
            else if (_mouseSelecting && mouse.LeftButton == ButtonState.Pressed)
            {
                _cursorIndex = GetCharacterIndexFromMouseX(mouse.X);
                UpdateScrollOffset();
                ResetCursorBlink();
            }
            else if (_mouseSelecting && mouse.LeftButton == ButtonState.Released)
            {
                _mouseSelecting = false;

                if (_selectionAnchor == _cursorIndex)
                    ClearSelection();
            }
        }

        private void OnDesktopTextInput(object sender, Microsoft.Xna.Framework.TextInputEventArgs e)
        {
            if (!IsFocused || !Visible)
                return;

            // Ctrl combinations are handled in Update(). Do not let Ctrl+C,
            // Ctrl+V, etc. accidentally insert a printable C/V into the field.
            var keyboard = MuGame.Instance.Keyboard;
            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) ||
                        keyboard.IsKeyDown(Keys.RightControl);

            if (ctrl)
                return;

            char character = e.Character;

            if (character == '\0' || char.IsControl(character))
                return;

            InsertTextAtCursor(character.ToString());
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Mouse selection must run after base.Update(), because base.Update()
            // updates focus and mouse hit-testing first.
            if (Visible)
                HandleMouseSelection();

            if (!IsFocused || !Visible)
                return;

#if !(ANDROID || IOS)
            var keyboard = MuGame.Instance.Keyboard;
            var previousKeyboard = MuGame.Instance.PrevKeyboard;

            bool shift = keyboard.IsKeyDown(Keys.LeftShift) ||
                         keyboard.IsKeyDown(Keys.RightShift);

            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) ||
                        keyboard.IsKeyDown(Keys.RightControl);

            // Printable characters still come from GameWindow.TextInput so the
            // active Spanish/English/etc. OS keyboard layout remains respected.
            // KeyboardState is used for editing/navigation/clipboard shortcuts.
            foreach (var key in keyboard.GetPressedKeys())
            {
                if (previousKeyboard.IsKeyUp(key))
                {
                    HandleControlKey(key, shift, ctrl);
                }
            }

#elif IOS
            var keyboard = MuGame.Instance.Keyboard;
            var previousKeyboard = MuGame.Instance.PrevKeyboard;
            var keysPressed = keyboard.GetPressedKeys();

            bool shift = keyboard.IsKeyDown(Keys.LeftShift) ||
                         keyboard.IsKeyDown(Keys.RightShift);

            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) ||
                        keyboard.IsKeyDown(Keys.RightControl);

            bool capsLock =
                System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                    ? Console.CapsLock
                    : false;

            foreach (var key in keysPressed)
            {
                if (previousKeyboard.IsKeyUp(key))
                {
                    if (HandleControlKey(key, shift, ctrl))
                        continue;

                    char character = KeyToChar(key, shift, capsLock);
                    if (character != '\0')
                        InsertTextAtCursor(character.ToString());
                }
            }
#endif

            _cursorBlinkTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_cursorBlinkTimer >= CursorBlinkInterval)
            {
                _showCursor = !_showCursor;
                _cursorBlinkTimer = 0;
            }
        }

#if IOS
        private char KeyToChar(Keys key, bool shift, bool capsLock)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                bool isUpper = capsLock ^ shift;
                char letter = (char)('A' + (key - Keys.A));
                return isUpper ? letter : char.ToLower(letter);
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                char digit = (char)('0' + (key - Keys.D0));
                if (shift)
                {
                    return key switch
                    {
                        Keys.D1 => '!',
                        Keys.D2 => '@',
                        Keys.D3 => '#',
                        Keys.D4 => '$',
                        Keys.D5 => '%',
                        Keys.D6 => '^',
                        Keys.D7 => '&',
                        Keys.D8 => '*',
                        Keys.D9 => '(',
                        Keys.D0 => ')',
                        _ => digit,
                    };
                }
                return digit;
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (char)('0' + (key - Keys.NumPad0));
            }

            return key switch
            {
                Keys.Space => ' ',
                Keys.OemComma => ',',
                Keys.OemPeriod => '.',
                Keys.OemMinus => shift ? '_' : '-',
                Keys.OemPlus => shift ? '+' : '=',
                Keys.OemQuestion => shift ? '?' : '/',
                Keys.OemOpenBrackets => shift ? '{' : '[',
                Keys.OemCloseBrackets => shift ? '}' : ']',
                Keys.OemPipe => shift ? '|' : '\\',
                Keys.OemTilde => shift ? '~' : '`',
                Keys.OemQuotes => shift ? '"' : '\'',
                Keys.OemSemicolon => shift ? ':' : ';',
                _ => '\0'
            };
        }
#endif

        public override void Draw(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready || !Visible)
                return;

            using (new SpriteBatchScope(
            GraphicsManager.Instance.Sprite,
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            raster: ScissorRasterizer,
            transform: UiScaler.SpriteTransform))
            {
                var spriteBatch = GraphicsManager.Instance.Sprite;

                if (Skin == TextFieldSkin.NineSlice && _nineSlice[0] != null)
                    DrawNineSliceBackground(spriteBatch);
                else
                    DrawFlatBackground(spriteBatch);

                DrawTextSelectionAndCursor(spriteBatch);
            }

            base.Draw(gameTime);
        }

        private void DrawFlatBackground(SpriteBatch spriteBatch)
        {
            DrawBackground();
            DrawBorder();
        }

        private void DrawNineSliceBackground(SpriteBatch spriteBatch)
        {
            var r = DisplayRectangle;

            var TL = _nineSlice[0];
            var T = _nineSlice[1];
            var TR = _nineSlice[2];
            var L = _nineSlice[3];
            var C = _nineSlice[4];
            var R = _nineSlice[5];
            var BL = _nineSlice[6];
            var B = _nineSlice[7];
            var BR = _nineSlice[8];

            spriteBatch.Draw(TL, new Rectangle(r.X, r.Y, TL.Width, TL.Height), Color.White);
            spriteBatch.Draw(TR, new Rectangle(r.Right - TR.Width, r.Y, TR.Width, TR.Height), Color.White);
            spriteBatch.Draw(BL, new Rectangle(r.X, r.Bottom - BL.Height, BL.Width, BL.Height), Color.White);
            spriteBatch.Draw(BR, new Rectangle(r.Right - BR.Width, r.Bottom - BR.Height, BR.Width, BR.Height), Color.White);

            spriteBatch.Draw(T, new Rectangle(r.X + TL.Width, r.Y, r.Width - TL.Width - TR.Width, T.Height), Color.White);
            spriteBatch.Draw(B, new Rectangle(r.X + BL.Width, r.Bottom - B.Height, r.Width - BL.Width - BR.Width, B.Height), Color.White);
            spriteBatch.Draw(L, new Rectangle(r.X, r.Y + TL.Height, L.Width, r.Height - TL.Height - BL.Height), Color.White);
            spriteBatch.Draw(R, new Rectangle(r.Right - R.Width, r.Y + TR.Height, R.Width, r.Height - TR.Height - BR.Height), Color.White);

            spriteBatch.Draw(C, new Rectangle(r.X + L.Width, r.Y + T.Height, r.Width - L.Width - R.Width, r.Height - T.Height - B.Height), Color.White);
        }

        private void DrawTextSelectionAndCursor(SpriteBatch spriteBatch)
        {
            var font = GraphicsManager.Instance.Font;
            if (font == null) return;

            var gd = GraphicsManager.Instance.GraphicsDevice;
            var originalScissorRect = gd.ScissorRectangle;

            var area = new Rectangle(
                DisplayRectangle.X + TextMargin,
                DisplayRectangle.Y,
                Math.Max(0, DisplayRectangle.Width - TextMargin * 2),
                DisplayRectangle.Height
            );

            // El SpriteBatch usa UiScaler.SpriteTransform,
            // pero ScissorRectangle trabaja en coordenadas reales de pantalla.
            // Por eso transformamos el rectángulo antes de usarlo.
            var transform = UiScaler.SpriteTransform;

            Vector2 topLeft = Vector2.Transform(
                new Vector2(area.Left, area.Top),
                transform);

            Vector2 bottomRight = Vector2.Transform(
                new Vector2(area.Right, area.Bottom),
                transform);

            var screenArea = new Rectangle(
                (int)Math.Floor(topLeft.X),
                (int)Math.Floor(topLeft.Y),
                Math.Max(0, (int)Math.Ceiling(bottomRight.X - topLeft.X)),
                Math.Max(0, (int)Math.Ceiling(bottomRight.Y - topLeft.Y))
            );

            screenArea = Rectangle.Intersect(
                screenArea,
                GraphicsManager.Instance.GraphicsDevice.Viewport.Bounds);

            gd.ScissorRectangle = Rectangle.Intersect(
                originalScissorRect,
                screenArea);

            float scale = FontSize / Constants.BASE_FONT_SIZE;
            string text = GetDisplayText();

            Vector2 textPos = new Vector2(
                DisplayRectangle.X + TextMargin - _scrollOffset,
                DisplayRectangle.Y +
                (DisplayRectangle.Height - font.MeasureString("A").Y * scale) / 2f
            );

            // Draw selection highlight behind the text.
            if (HasSelection && !MaskValue)
            {
                float selectionX1 = textPos.X + MeasureTextToIndex(SelectionStart);
                float selectionX2 = textPos.X + MeasureTextToIndex(SelectionEnd);
                int selectionHeight = Math.Max(1, (int)Math.Ceiling(font.LineSpacing * scale));

                var selectionRect = new Rectangle(
                    (int)Math.Floor(selectionX1),
                    (int)Math.Floor(textPos.Y),
                    Math.Max(1, (int)Math.Ceiling(selectionX2 - selectionX1)),
                    selectionHeight
                );

                spriteBatch.Draw(
                    GraphicsManager.Instance.Pixel,
                    selectionRect,
                    new Color(70, 110, 180, 150));
            }

            spriteBatch.DrawString(
                font,
                text,
                textPos,
                TextColor,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);

            if (IsFocused && _showCursor)
            {
                float cursorWidth = MeasureTextToIndex(_cursorIndex);
                var cursorPos = textPos + new Vector2(cursorWidth, 0);

                if (cursorPos.X >= area.Left && cursorPos.X <= area.Right)
                {
                    spriteBatch.DrawString(
                        font,
                        "|",
                        cursorPos,
                        TextColor,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        0f);
                }
            }

            gd.ScissorRectangle = originalScissorRect;
        }
    }
}
