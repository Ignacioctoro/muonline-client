using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game.Messenger;

/// <summary>Shared cached NewUI frame for the friend list, letters and conversations.</summary>
public abstract class MessengerWindow : UIControl
{
    protected static class Theme
    {
        public static readonly Color Darkest = new(8, 8, 7), Dark = new(17, 16, 13), Mid = new(27, 26, 22),
            Light = new(42, 39, 30), Lighter = new(60, 53, 38), Gold = new(232, 188, 72);
    }
    private Texture2D _atlas, _chatHeader;
    private RenderTarget2D _surface;
    private readonly BlendState _cacheBlend = new()
    { ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha };
    private readonly Dictionary<(string, int, float), string> _labels = new();
    protected SpriteFont Font => GraphicsManager.Instance.Font;
    protected string Caption;
    protected virtual string FramePath => "Interface/GFx/FriendMenu_I1.ozd";
    protected virtual Rectangle FrameBody => new(0, 0, 530, 295);
    protected virtual Rectangle FrameHeader => new(0, 300, 530, 78);
    public override bool BlocksPointerInput => true;
    protected MessengerWindow(string caption, int width, int height)
    {
        Caption = caption; AutoViewSize = false; ViewSize = ControlSize = new(width, height);
        Align = ControlAlign.HorizontalCenter | ControlAlign.VerticalCenter;
        BackgroundColor = Color.Transparent; BorderThickness = 0; Interactive = true;
        AddButton("X", width - 48, 29, 25, CloseWindow);
    }
    protected ButtonControl AddButton(string text, int x, int y, int width, Action action)
    {
        var button = new MessengerButton { Text = text, X = x, Y = y, AutoViewSize = false,
            ViewSize = new(width, 28), ControlSize = new(width, 28), FontSize = 11,
            BackgroundColor = Color.Transparent, TextColor = Theme.Gold, BorderThickness = 0 };
        button.Click += (_, _) => { Scene?.SetMouseInputConsumed(); action(); };
        Controls.Add(button); return button;
    }
    protected TextFieldControl AddInput(int x, int y, int width, int max, string placeholder)
    {
        var input = TextFieldControl.Create(); input.X = x; input.Y = y;
        input.ViewSize = input.ControlSize = new(width, 25); input.MaxLength = max;
        input.Placeholder = placeholder; input.FontSize = 12; input.BackgroundColor = Theme.Darkest;
        input.BorderColor = Theme.Lighter; input.BorderThickness = 1;
        Controls.Add(input); return input;
    }
    protected void BlurInput(TextFieldControl field)
    {
        field.Blur();
        if (Scene?.FocusControl == field) Scene.FocusControl = null;
    }
    public virtual void CloseWindow()
    {
        foreach (var child in Controls) if (child is TextFieldControl field) BlurInput(field);
        Visible = false; Scene?.SetMouseInputConsumed();
    }
    public override async Task Load()
    {
        _atlas = await TextureLoader.Instance.PrepareAndGetTexture(FramePath);
        if (ViewSize.X == 420) _chatHeader = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/FriendWindow_I1.ozd");
        await base.Load();
    }
    public override void Update(GameTime time)
    {
        base.Update(time);
        if (!Visible || Status != GameControlStatus.Ready) return;
        if (_surface == null && _atlas != null)
        {
            var gd = GraphicsManager.Instance.GraphicsDevice; var sb = GraphicsManager.Instance.Sprite;
            _surface = new RenderTarget2D(gd, ViewSize.X, ViewSize.Y);
            var previous = gd.GetRenderTargets(); gd.SetRenderTarget(_surface); gd.Clear(Color.Transparent);
            using (new SpriteBatchScope(sb, blend: _cacheBlend))
            {
                sb.Draw(_atlas, new Rectangle(0, 76, ViewSize.X, ViewSize.Y - 76), FrameBody, Color.White);
                sb.Draw(_chatHeader ?? _atlas, new Rectangle(0, 0, ViewSize.X, 78),
                    _chatHeader != null ? new Rectangle(0, 0, 530, 76) : FrameHeader, Color.White);
            }
            gd.SetRenderTargets(previous);
        }
        if (PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position) &&
            MuGame.Instance.Keyboard.IsKeyDown(Keys.Escape) && MuGame.Instance.PrevKeyboard.IsKeyUp(Keys.Escape))
        { Scene?.ConsumeKeyboardEscape(); CloseWindow(); }
    }
    protected Rectangle Screen(Rectangle r) => new(r.X + DisplayPosition.X, r.Y + DisplayPosition.Y, r.Width, r.Height);
    protected void Text(SpriteBatch sb, string text, int x, int y, int maxWidth = 480, Color? color = null, float size = 11)
    {
        if (Font == null || string.IsNullOrEmpty(text)) return;
        float scale = size / Constants.BASE_FONT_SIZE;
        // Scissor-free bounded labels; wrapping is cached by the caller for long messages.
        var key = (text, maxWidth, size);
        if (!_labels.TryGetValue(key, out string shown))
        {
            shown = text;
            if (Font.MeasureString(shown).X * scale > maxWidth)
            {
                int n = shown.Length;
                while (n > 0 && Font.MeasureString(shown[..n] + "...").X * scale > maxWidth) n--;
                shown = shown[..n] + "...";
            }
            if (_labels.Count > 1024) _labels.Clear();
            _labels[key] = shown;
        }
        sb.DrawString(Font, shown, new Vector2(DisplayPosition.X + x, DisplayPosition.Y + y), color ?? Color.LightGray,
            0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
    protected void Fill(SpriteBatch sb, Rectangle r, Color color) => sb.Draw(GraphicsManager.Instance.Pixel, Screen(r), color);
    protected bool ClickAt(out Point local)
    {
        var mouse = MuGame.Instance.UiMouseState; local = mouse.Position - DisplayPosition;
        return Visible && PersonalStoreLayout.IsTopmostAtPointer(this, mouse.Position) &&
            mouse.LeftButton == ButtonState.Pressed && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released;
    }
    protected int Wheel => PersonalStoreLayout.IsTopmostAtPointer(this, MuGame.Instance.UiMouseState.Position)
        ? Math.Sign(MuGame.Instance.UiMouseState.ScrollWheelValue - MuGame.Instance.PrevUiMouseState.ScrollWheelValue) : 0;
    protected abstract void DrawContent(SpriteBatch sb);
    public override void Draw(GameTime time)
    {
        if (!Visible || Status != GameControlStatus.Ready || _surface == null || Font == null) return;
        var sb = GraphicsManager.Instance.Sprite;
        using var scope = new SpriteBatchScope(sb, blend: BlendState.AlphaBlend, transform: UiScaler.SpriteTransform);
        sb.Draw(_surface, DisplayRectangle, Color.White);
        Text(sb, Caption, Math.Max(28, (ViewSize.X - (int)(Font.MeasureString(Caption).X * 14 / Constants.BASE_FONT_SIZE)) / 2), 38, ViewSize.X - 84, Theme.Gold, 14);
        DrawContent(sb);
        foreach (var child in Controls) child.Draw(time);
    }
    public override void Dispose() { _surface?.Dispose(); _cacheBlend.Dispose(); base.Dispose(); }
    private sealed class MessengerButton : ButtonControl
    {
        private Texture2D _normal, _hover;
        public override async Task Load()
        {
            _normal = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_l01.ozd");
            _hover = await TextureLoader.Instance.PrepareAndGetTexture("Interface/GFx/btn_l02.ozd");
            await base.Load(); SetTexture(_normal);
        }
        public override void Draw(GameTime time)
        {
            if (!Visible) return;
            SetTexture(Enabled && (IsMouseOver || IsMousePressed) ? _hover ?? _normal : _normal);
            using var scope = new SpriteBatchScope(GraphicsManager.Instance.Sprite, blend: BlendState.NonPremultiplied, transform: UiScaler.SpriteTransform);
            base.Draw(time);
        }
    }
}
