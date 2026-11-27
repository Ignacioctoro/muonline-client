using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Core.Client;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Client.Main.Controls.UI.Game;

/// <summary>Gens information and the two steward dialogs, using server acknowledgements and the native NewUI atlas.</summary>
public sealed class GensControl : UIControl, IUiTexturePreloadable
{
    private const int Width = 332, Height = 490;
    private static class Theme
    {
        public static readonly Color Background = new(13, 11, 9, 240), Inset = new(0, 0, 0, 160),
            Surface = new(22, 20, 17), Hover = new(45, 36, 20), Border = new(91, 70, 38),
            Gold = new(221, 184, 83), Text = new(222, 217, 203), Muted = new(145, 140, 130);
    }
    private static readonly string[] Paths = { "Interface/GFx/GensRanking_I3.ozd", "Interface/GFx/Popups_I1.ozd",
        "Interface/Gens_mark_D_new.OZT", "Interface/Gens_mark_V_new.OZT" };
    private static readonly string[] Ranks = { "", "Grand Duke", "Duke", "Marquis", "Count", "Viscount", "Baron",
        "Knight Commander", "Superior Knight", "Knight", "Guard Prefect", "Officer", "Lieutenant", "Sergeant", "Private" };
    private static GensControl _instance;
    public static GensControl Instance => _instance ??= new();
    public static bool IsOpen => _instance?.Visible == true;
    public bool IsNpcDialog => Visible && _npc != 0;
    public int DialogRevision => Volatile.Read(ref _dialogRevision);
    public Texture2D GetMarkTexture(byte family) => family == 1 ? _textures[2] : family == 2 ? _textures[3] : null;
    public override bool NonDisposable => true;
    public override bool BlocksPointerInput => true;
    private readonly Texture2D[] _textures = new Texture2D[4];
    private RenderTarget2D _frame;
    private BlendState _frameBlend;
    private CharacterState _state;
    private int _session, _dialogRevision, _infoRevision, _map;
    private ushort _npc;
    private byte _pending;
    private bool _waitingInfo, _confirmLeave;
    private long _deadline;
    private string _message = "";
    private Point _mouse;
    private Point? _click;

    private GensControl()
    {
        ControlSize = ViewSize = new Point(Width, Height);
        AutoViewSize = false; Interactive = true; Visible = false;
    }
    public IEnumerable<string> GetPreloadTexturePaths() => Paths;
    public override async Task Load()
    {
        await base.Load();
        for (int i = 0; i < Paths.Length; i++) _textures[i] = await TextureLoader.Instance.PrepareAndGetTexture(Paths[i]);
    }
    public void Toggle()
    {
        if (Visible) { CloseWindow(); return; }
        Show(0);
    }
    public void ShowNpc(ushort npc)
    {
        if (npc is 543 or 544) Show(npc);
    }
    private void Show(ushort npc)
    {
        var state = MuGame.Network?.GetCharacterState();
        if (state?.IsInGame != true || Scene is not GameScene) return;
        HideWithoutServerRequest(); _state = state; _session = state.SkillSessionRevision; _npc = npc; _map = state.MapId;
        _infoRevision = state.GensRevision;
        Visible = true; PositionWindow(); BringToFront();
        if (Scene != null) { Scene.FocusControl = this; Scene.Cursor.BringToFront(); }
        (Scene as GameScene)?.StopMuHelper();
        RefreshInfo();
    }
    public void HideWithoutServerRequest()
    {
        Interlocked.Increment(ref _dialogRevision);
        Visible = false; _npc = 0; _pending = 0; _waitingInfo = _confirmLeave = false; _click = null;
        if (Scene?.FocusControl == this) Scene.FocusControl = null;
    }
    public void CloseWindow()
    {
        bool closeNpc = IsNpcDialog && OwnsCharacter();
        HideWithoutServerRequest();
        if (closeNpc) _ = MuGame.Network.GetCharacterService().SendCloseNpcRequestAsync();
    }
    private bool OwnsCharacter() => _state?.IsInGame == true && _state.SkillSessionRevision == _session
        && ReferenceEquals(_state, MuGame.Network?.GetCharacterState());
    private byte NpcFamily => _npc == 543 ? (byte)1 : _npc == 544 ? (byte)2 : (byte)0;
    private void RefreshInfo()
    {
        if (!OwnsCharacter() || _pending != 0) return;
        _waitingInfo = true; _deadline = Environment.TickCount64 + 10000; _message = "Consultando al servidor...";
        _ = SendAsync(MuGame.Network.GetCharacterService().RequestGensInfoAsync(), DialogRevision);
    }
    private async Task SendAsync(Task<bool> request, int dialog)
    {
        bool sent = await request;
        if (sent) return;
        MuGame.ScheduleOnMainThread(() =>
        {
            if (!Visible || DialogRevision != dialog || !OwnsCharacter()) return;
            _waitingInfo = false; _pending = 0; _message = "No se pudo enviar la solicitud.";
        });
    }
    private void Request(byte operation)
    {
        if (!OwnsCharacter() || !IsNpcDialog || _pending != 0 || _waitingInfo || !_state.Gens.Received) return;
        _confirmLeave = false; _pending = operation; _deadline = Environment.TickCount64 + 10000;
        _message = "Esperando respuesta del servidor...";
        var service = MuGame.Network.GetCharacterService();
        _ = SendAsync(operation == 1 ? service.JoinGensAsync(NpcFamily) : operation == 3 ? service.LeaveGensAsync()
            : service.RequestGensRewardAsync(NpcFamily), DialogRevision);
    }
    public void ReceiveResult(byte operation, byte result, int dialog)
    {
        if (!Visible || !OwnsCharacter() || DialogRevision != dialog || _pending != operation) return;
        _pending = 0;
        _message = ResultText(operation, result);
        if (result == 0 && operation is 1 or 3)
        {
            // The following F8 07 supplies the actual membership, rank and points.
            _waitingInfo = true; _deadline = Environment.TickCount64 + 10000;
            _ = SendAsync(MuGame.Network.GetCharacterService().RequestGensInfoAsync(), DialogRevision);
        }
    }
    private static string ResultText(byte operation, byte result) => operation switch
    {
        1 => result switch { 0 => "Afiliacion aceptada.", 1 => "Ya perteneces a una familia Gens.",
            2 => "Debes esperar antes de volver a afiliarte.", 3 => "No tienes el nivel requerido.",
            4 => "Debes salir de la guild para afiliarte.", 5 => "El Guild Master no puede afiliarse en este estado.",
            6 => "Debes salir de la party para afiliarte.", 7 => "Tu guild pertenece a una alianza.", _ => "El servidor rechazo la afiliacion." },
        3 => result switch { 0 => "Has abandonado la familia Gens.", 1 => "No perteneces a una familia Gens.",
            2 => "El Guild Master no puede abandonar Gens.", 3 => "Habla con el representante de tu familia.", _ => "El servidor rechazo la salida." },
        _ => result switch { 0 => "Recompensa entregada en el inventario.", 1 => "Fuera del periodo de recompensas.",
            2 => "Tu rango no tiene una recompensa disponible.", 3 => "No hay espacio suficiente en el inventario.",
            4 => "Ya reclamaste la recompensa de este mes.", 5 => "Habla con el representante de tu familia.",
            6 => "No perteneces a una familia Gens.", _ => "El servidor rechazo la recompensa." }
    };
    private void PositionWindow()
    {
        var size = Parent?.DisplaySize ?? UiScaler.VirtualSize;
        Scale = MathHelper.Clamp(Math.Min((size.X - 20f) / Width, (size.Y - 20f) / Height), .1f, 1f);
        X = (size.X - DisplaySize.X) / 2; Y = (size.Y - DisplaySize.Y) / 2;
    }
    public override void Update(GameTime gameTime)
    {
        if (!Visible) return;
        if (!OwnsCharacter()) { HideWithoutServerRequest(); return; }
        if (IsNpcDialog && _state.MapId != _map) { HideWithoutServerRequest(); return; }
        PositionWindow(); base.Update(gameTime);
        if (_infoRevision != _state.GensRevision)
        {
            _infoRevision = _state.GensRevision; _waitingInfo = false;
            if (_pending == 0 && _message == "Consultando al servidor...") _message = "";
        }
        if ((_pending != 0 || _waitingInfo) && Environment.TickCount64 > _deadline)
        {
            _pending = 0; _waitingInfo = false;
            _message = "Sin respuesta. Actualiza la informacion.\nComprueba que Gens este habilitado.";
        }
        var game = MuGame.Instance;
        if (Scene?.IsPointerInputBlockedFor(this) == true) return;
        if (game.Keyboard.IsKeyDown(Keys.Escape) && game.PrevKeyboard.IsKeyUp(Keys.Escape))
        { Scene?.ConsumeKeyboardEscape(); if (_confirmLeave) _confirmLeave = false; else CloseWindow(); return; }
        _mouse = new((int)((game.UiMouseState.X - DisplayPosition.X) / Scale), (int)((game.UiMouseState.Y - DisplayPosition.Y) / Scale));
        if (game.UiMouseState.LeftButton != ButtonState.Pressed || game.PrevUiMouseState.LeftButton != ButtonState.Released
            || !DisplayRectangle.Contains(game.UiMouseState.Position)) return;
        Scene?.SetMouseInputConsumed(); _click = _mouse; Build(null); _click = null;
    }
    private void Build(SpriteBatch batch)
    {
        var info = _state.Gens;
        bool busy = _pending != 0 || _waitingInfo;
        Text(batch, "Gens", new(35, 37, 254, 22), Theme.Gold, 12, true);
        Button(batch, new(292, 35, 20, 20), "X", CloseWindow, true, false);
        byte family = info.Received ? info.Family : (byte)0;
        if (batch != null && family is 1 or 2 && info.Rank is >= 1 and <= 14)
        {
            int tile = 14 - (int)info.Rank;
            var texture = _textures[family == 1 ? 2 : 3];
            if (texture != null) batch.Draw(texture, new Rectangle(48, 93, 50, 69), new Rectangle(tile % 5 * 50, tile / 5 * 69, 50, 69), Color.White);
        }
        Text(batch, "Familia", new(119, 93, 180, 20), Theme.Muted, 10);
        Text(batch, !info.Received ? "Consultando..." : family == 1 ? "Duprian" : family == 2 ? "Vanert" : "Sin afiliacion", new(119, 115, 180, 24), Theme.Gold, 13);
        Text(batch, family == 0 ? "" : info.Rank is >= 1 and <= 14 ? Ranks[(int)info.Rank] : "Sin rango", new(119, 142, 180, 20), Theme.Text, 10);
        Row(batch, 179, "Posicion", info.Received && info.Position > 0 ? info.Position.ToString("N0") : "--");
        Row(batch, 207, "Contribucion", info.Received ? info.Contribution.ToString("N0") : "--");
        Row(batch, 235, "Puntos restantes", family != 0 && info.NextContribution > 0 ? info.NextContribution.ToString("N0") : "--");
        Row(batch, 263, "Rango", family != 0 ? info.Rank + " / 14" : "--");
        string description = _confirmLeave ? "Al abandonar perderas tu contribucion.\nQuieres abandonar tu familia Gens?"
            : !IsNpcDialog ? "Habla con Duprian o Vanert para afiliarte,\nretirarte o solicitar una recompensa."
            : family == 0 ? "Representante: " + (NpcFamily == 1 ? "Duprian" : "Vanert") + ".\nEl servidor comprueba los requisitos."
            : family != NpcFamily ? "Este representante pertenece a otra familia.\nHabla con el de tu propia familia."
            : "Solicita aqui tu recompensa por rango.\nEl servidor determina el periodo y el premio.";
        Text(batch, description, new(29, 301, 274, 43), Theme.Text, 10, true);
        if (IsNpcDialog)
        {
            if (_confirmLeave)
            {
                Button(batch, new(32, 352, 126, 31), "Abandonar", () => Request(3), !busy);
                Button(batch, new(174, 352, 126, 31), "Cancelar", () => _confirmLeave = false, !busy);
            }
            else
            {
                Button(batch, new(32, 352, 126, 31), family == 0 ? "Unirse" : "Retirarse",
                    () => { if (family == 0) Request(1); else _confirmLeave = true; },
                    !busy && info.Received && (family == 0 || family == NpcFamily));
                Button(batch, new(174, 352, 126, 31), "Recompensa", () => Request(9), !busy && family == NpcFamily);
            }
        }
        Text(batch, _message, new(27, 390, 278, 39), Theme.Gold, 9, true);
        Button(batch, new(32, 439, 126, 29), "Actualizar", RefreshInfo, !busy && !_confirmLeave);
        Button(batch, new(174, 439, 126, 29), "Cerrar", CloseWindow);
    }
    private void Row(SpriteBatch batch, int y, string label, string value)
    {
        Text(batch, label, new(34, y, 145, 22), Theme.Muted, 10);
        Text(batch, value, new(180, y, 116, 22), Theme.Text, 10, true);
    }
    private void Button(SpriteBatch batch, Rectangle rect, string label, Action action, bool enabled = true, bool skin = true)
    {
        if (batch != null)
        {
            bool hover = enabled && rect.Contains(_mouse);
            if (skin && _textures[1] != null) batch.Draw(_textures[1], rect, new Rectangle(hover ? 0 : 78, 0, 74, 36), enabled ? Color.White : Color.Gray);
            Text(batch, label, rect, enabled ? hover ? Theme.Gold : Theme.Text : Theme.Muted, 10, true);
        }
        if (enabled && _click is { } point && rect.Contains(point)) { _click = null; action(); }
    }
    private static void Text(SpriteBatch batch, string text, Rectangle rect, Color color, float size, bool centered = false)
    {
        if (batch == null || string.IsNullOrEmpty(text)) return;
        var font = GraphicsManager.Instance.Font; var measure = font.MeasureString(text);
        float scale = Math.Min(size / Constants.BASE_FONT_SIZE, rect.Width / Math.Max(1, measure.X));
        batch.DrawString(font, text, new Vector2(centered ? rect.Center.X - measure.X * scale / 2 : rect.X,
            rect.Center.Y - measure.Y * scale / 2), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
    private void EnsureFrame(SpriteBatch batch)
    {
        if (_frame != null && !_frame.IsDisposed) return;
        var device = GraphicsManager.Instance.GraphicsDevice;
        _frameBlend ??= new BlendState { ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.InverseSourceAlpha };
        _frame = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
        var targets = device.GetRenderTargets(); var viewport = device.Viewport;
        try
        {
            using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, _frameBlend, SamplerState.LinearClamp))
            {
                device.SetRenderTarget(_frame); device.Clear(Color.Transparent);
                batch.Draw(GraphicsManager.Instance.Pixel, new Rectangle(20, 73, Width - 40, Height - 91), Theme.Background);
                if (_textures[0] != null)
                {
                    NewUiFrameRenderer.DrawNineSlice(batch, _textures[0], new(0, 0, 320, 380), new(6, 23, 320, Height - 23), 14, 14, 14, 14);
                    batch.Draw(_textures[0], new Rectangle(0, 0, 332, 74), new Rectangle(648, 0, 332, 74), Color.White);
                }
                var inset = new Rectangle(27, 172, 278, 115);
                batch.Draw(GraphicsManager.Instance.Pixel, inset, Theme.Inset);
                Border(batch, inset, Theme.Border);
            }
        }
        finally { device.SetRenderTargets(targets); device.Viewport = viewport; }
    }
    private static void Border(SpriteBatch batch, Rectangle rect, Color color)
    {
        var p = GraphicsManager.Instance.Pixel;
        batch.Draw(p, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        batch.Draw(p, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        batch.Draw(p, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        batch.Draw(p, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
    public override void Draw(GameTime gameTime)
    {
        if (!Visible || Status != GameControlStatus.Ready || _state == null || GraphicsManager.Instance.Font == null) return;
        var batch = GraphicsManager.Instance.Sprite; EnsureFrame(batch);
        var transform = Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(DisplayPosition.X, DisplayPosition.Y, 0) * UiScaler.SpriteTransform;
        using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, transform: transform))
            batch.Draw(_frame, new Rectangle(0, 0, Width, Height), Color.White);
        using (new SpriteBatchScope(batch, SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, transform: transform)) Build(batch);
    }
    public override void Dispose()
    {
        HideWithoutServerRequest(); _state = null;
        _frame?.Dispose(); _frame = null; _frameBlend?.Dispose(); _frameBlend = null;
        base.Dispose();
    }
}
