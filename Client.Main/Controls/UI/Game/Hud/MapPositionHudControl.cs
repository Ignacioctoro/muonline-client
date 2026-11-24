using Client.Main.Content;
using Client.Main.Core.Utilities;
using Client.Main.Data;
using System.Reflection;
using Client.Main.Controllers;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Threading.Tasks;

namespace Client.Main.Controls.UI.Game.Hud
{
    public class MapPositionHudControl : UIControl
    {
        // =============================================================
        // TEXTURE
        // =============================================================

        private const string HUD_TEXTURE_PATH =
            "Interface/GFx/TopMenu_I3.ozd";

        // =============================================================
        // SOURCE RECTS - TOPMENU_I3
        // =============================================================

        // Marco izquierdo:
        // nombre del mapa + coordenadas
        private static readonly Rectangle MAP_FRAME_SOURCE =
            new Rectangle(
                0,
                0,
                185,
                37);

        // Marco derecho:
        // base donde van Hunting Log / Settings / Start-Stop
        private static readonly Rectangle HELPER_FRAME_SOURCE =
            new Rectangle(
                187,
                0,
                185,
                29);

        // =============================================================
        // DESTINATION DIMENSIONS
        // =============================================================
        private const int FRAME_OVERLAP = 28;
        private const int MAP_FRAME_WIDTH = 185;
        private const int MAP_FRAME_HEIGHT = 37;

        private const int HELPER_FRAME_WIDTH = 185;
        private const int HELPER_FRAME_HEIGHT = 29;

        // Los dos marcos se dibujan pegados.
        private const int TOTAL_WIDTH =
            MAP_FRAME_WIDTH +
            HELPER_FRAME_WIDTH -
            FRAME_OVERLAP;

        private const int TOTAL_HEIGHT = 40;

        // =============================================================
        // TEXT
        // =============================================================

        private const float TEXT_SCALE = 0.36f;

        private const int TEXT_LEFT_PADDING = 35;
        private const int TEXT_RIGHT_PADDING = 35;
        private const int TEXT_Y = 7;

        private const int TEXT_SHADOW_OFFSET_X = 1;
        private const int TEXT_SHADOW_OFFSET_Y = 1;

        // =============================================================
        // STATE
        // =============================================================

        private Texture2D _hudTexture;
        private bool _helperHovered;
        private bool _helperRunHovered;
        private bool _helperLogHovered;
        // TopMenu_I3 stores the native circular faces in 30px cells with a 1px gutter.
        private Rectangle HelperLogArea => new(DisplayRectangle.X + 178, DisplayRectangle.Y + 10, 30, 30);
        private Rectangle HelperSetupArea => new(DisplayRectangle.X + 209, DisplayRectangle.Y + 10, 30, 30);
        private Rectangle HelperRunArea => new(DisplayRectangle.X + 240, DisplayRectangle.Y + 10, 30, 30);

        private string _mapName = string.Empty;

        private int _mapX;
        private int _mapY;

        private bool _mapHovered;

        // =============================================================
        // EVENTS
        // =============================================================

        public event EventHandler MapNameClicked;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public MapPositionHudControl()
        {
            Name = "MapPositionHud";

            Interactive = true;
            Visible = true;

            ControlSize =
                new Point(
                    TOTAL_WIDTH,
                    TOTAL_HEIGHT);

            ViewSize =
                ControlSize;
        }

        // =============================================================
        // LOAD
        // =============================================================

        public override async Task Load()
        {
            _hudTexture =
                await TextureLoader.Instance
                    .PrepareAndGetTexture(
                        HUD_TEXTURE_PATH);


            await base.Load();
        }

        // =============================================================
        // UPDATE
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateMapInformation();
            UpdateInteraction();
        }

        // =============================================================
        // MAP DATA
        // =============================================================

        private void UpdateMapInformation()
        {
            if (Scene is not GameScene gameScene)
                return;

            if (gameScene.World
                is not WalkableWorldControl walkableWorld)
            {
                return;
            }

            if (walkableWorld.Walker
                is not PlayerObject player)
            {
                return;
            }

            _mapX =
                (int)player.Location.X;

            _mapY =
                (int)player.Location.Y;

            _mapName =
            GetCurrentMapDisplayName(
                walkableWorld);
        }

        // =============================================================
        // INPUT
        // =============================================================

        private void UpdateInteraction()
        {
            _mapHovered = false;
            _helperHovered = false;
            _helperRunHovered = false;
            _helperLogHovered = false;
            if (Scene?.IsPointerInputBlockedFor(this) == true) return;

            if (!Visible ||
                Scene == null)
            {
                return;
            }

            Point mouse =
                MuGame.Instance
                    .UiMouseState
                    .Position;

            _helperLogHovered = HelperLogArea.Contains(mouse);
            if (_helperLogHovered)
            {
                Scene.SetMouseInputConsumed();
                if (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released
                    && Scene is GameScene logScene) logScene.ShowMuHelperLog();
                return;
            }
            _helperRunHovered = HelperRunArea.Contains(mouse);
            if (_helperRunHovered)
            {
                Scene.SetMouseInputConsumed();
                if (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released
                    && Scene is GameScene gameScene) gameScene.ToggleMuHelper();
                return;
            }
            _helperHovered = HelperSetupArea.Contains(mouse);
            if (_helperHovered)
            {
                Scene.SetMouseInputConsumed();
                if (MuGame.Instance.UiMouseState.LeftButton == ButtonState.Pressed
                    && MuGame.Instance.PrevUiMouseState.LeftButton == ButtonState.Released)
                    MuHelperControl.Instance.Toggle();
                return;
            }

            // Solo el marco izquierdo abre
            // el menú de movimiento/mapas.
            Rectangle mapArea =
                new Rectangle(
                    DisplayRectangle.X,
                    DisplayRectangle.Y,
                    MAP_FRAME_WIDTH,
                    MAP_FRAME_HEIGHT);

            _mapHovered =
                mapArea.Contains(mouse);

            if (!_mapHovered)
                return;

            Scene.SetMouseInputConsumed();

            MouseState currentMouse =
                MuGame.Instance
                    .UiMouseState;

            MouseState previousMouse =
                MuGame.Instance
                    .PrevUiMouseState;

            if (
                currentMouse.LeftButton ==
                    ButtonState.Pressed &&
                previousMouse.LeftButton ==
                    ButtonState.Released)
            {
                MapNameClicked?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        // =============================================================
        // DRAW
        // =============================================================

        public override void Draw(
            GameTime gameTime)
        {
            if (
                Status != GameControlStatus.Ready ||
                !Visible ||
                _hudTexture == null)
            {
                return;
            }

            DrawFrames();
            DrawMapText();
            if (_hudTexture != null)
            {
                var sprite = GraphicsManager.Instance.Sprite;
                using var scope = new SpriteBatchScope(sprite, SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullNone, transform: UiScaler.SpriteTransform);
                bool busy = Scene is GameScene helperScene && helperScene.IsMuHelperBusy;
                sprite.Draw(_hudTexture, HelperLogArea, new Rectangle(_helperLogHovered ? 654 : 592, 0, 30, 30), Color.White);
                sprite.Draw(_hudTexture, HelperSetupArea, new Rectangle(_helperHovered ? 561 : 375, 0, 30, 30), Color.White);
                int runSource = busy ? _helperRunHovered ? 778 : 747 : _helperRunHovered ? 437 : 406;
                sprite.Draw(_hudTexture, HelperRunArea, new Rectangle(runSource, 0, 30, 30), Color.White);
                if (_helperHovered || _helperRunHovered || _helperLogHovered)
                {
                    var rect = new Rectangle(HelperLogArea.Left, HelperLogArea.Bottom + 4, 164, 20);
                    sprite.Draw(GraphicsManager.Instance.Pixel, rect, new Color(12, 10, 8, 235));
                    var font = GraphicsManager.Instance.Font;
                    if (font != null) sprite.DrawString(font, _helperLogHovered ? "Hunting Log" : _helperRunHovered ? busy ? "Stop MU Helper (Home)" : "Start MU Helper (Home)" : "Settings (Z)", new Vector2(rect.X + 5, rect.Y + 3),
                        new Color(221, 184, 83), 0, Vector2.Zero, 9f / Constants.BASE_FONT_SIZE, SpriteEffects.None, 0);
                }
            }
        }

        // =============================================================
        // DRAW FRAMES
        // =============================================================

        private void DrawFrames()
        {
            var spriteBatch =
                GraphicsManager.Instance.Sprite;

            Rectangle rect =
                DisplayRectangle;

            // ---------------------------------------------------------
            // MAP FRAME
            // ---------------------------------------------------------

            Rectangle mapDestination =
                new Rectangle(
                    rect.X,
                    rect.Y,
                    MAP_FRAME_WIDTH,
                    MAP_FRAME_HEIGHT);

            // ---------------------------------------------------------
            // HELPER FRAME
            //
            // Importante:
            // en el atlas comienza en X=187,
            // pero en pantalla lo pegamos justo
            // después del marco izquierdo.
            // ---------------------------------------------------------

            Rectangle helperDestination =
            new Rectangle(
                rect.X +
                MAP_FRAME_WIDTH -
                FRAME_OVERLAP,
                rect.Y,
                HELPER_FRAME_WIDTH,
                HELPER_FRAME_HEIGHT);

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    BlendState.NonPremultiplied,
                    SamplerState.PointClamp,
                    transform:
                        UiScaler.SpriteTransform))
            {
                // Primero el marco derecho
                spriteBatch.Draw(
                    _hudTexture,
                    helperDestination,
                    HELPER_FRAME_SOURCE,
                    Color.White);

                // Después el marco izquierdo para que quede por encima
                spriteBatch.Draw(
                    _hudTexture,
                    mapDestination,
                    MAP_FRAME_SOURCE,
                    Color.White);
            }
        }

        // =============================================================
        // DRAW MAP TEXT
        // =============================================================

        private void DrawMapText()
            {
                var spriteBatch =
                    GraphicsManager.Instance.Sprite;

                var font =
                    GraphicsManager.Instance.Font;

                Rectangle rect =
                    DisplayRectangle;

                string mapText =
                    GetCompactMapName(_mapName);

                string coordinates =
                    $"{_mapX},{_mapY}";

                Vector2 mapTextSize =
                    font.MeasureString(mapText) *
                    TEXT_SCALE;

                Vector2 coordinateSize =
                    font.MeasureString(coordinates) *
                    TEXT_SCALE;

                Vector2 mapTextPosition =
                    new Vector2(
                        rect.X + TEXT_LEFT_PADDING,
                        rect.Y + TEXT_Y);

                Vector2 coordinatePosition =
                    new Vector2(
                        rect.X +
                        MAP_FRAME_WIDTH -
                        TEXT_RIGHT_PADDING -
                        coordinateSize.X,
                        rect.Y + TEXT_Y);

                Vector2 shadowOffset =
                    new Vector2(
                        TEXT_SHADOW_OFFSET_X,
                        TEXT_SHADOW_OFFSET_Y);

                Color mainColor =
                    _mapHovered
                        ? Color.White
                        : new Color(235, 235, 235);

                Color shadowColor =
                    new Color(0, 0, 0, 220);

                using (
                    new SpriteBatchScope(
                        spriteBatch,
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        transform: UiScaler.SpriteTransform))
                {
                    // sombra nombre mapa
                    spriteBatch.DrawString(
                        font,
                        mapText,
                        mapTextPosition + shadowOffset,
                        shadowColor,
                        0f,
                        Vector2.Zero,
                        TEXT_SCALE,
                        SpriteEffects.None,
                        0f);

                    // sombra coordenadas
                    spriteBatch.DrawString(
                        font,
                        coordinates,
                        coordinatePosition + shadowOffset,
                        shadowColor,
                        0f,
                        Vector2.Zero,
                        TEXT_SCALE,
                        SpriteEffects.None,
                        0f);

                    // texto principal nombre mapa
                    spriteBatch.DrawString(
                        font,
                        mapText,
                        mapTextPosition,
                        mainColor,
                        0f,
                        Vector2.Zero,
                        TEXT_SCALE,
                        SpriteEffects.None,
                        0f);

                    // texto principal coordenadas
                    spriteBatch.DrawString(
                        font,
                        coordinates,
                        coordinatePosition,
                        mainColor,
                        0f,
                        Vector2.Zero,
                        TEXT_SCALE,
                        SpriteEffects.None,
                        0f);
                }
            }

        // =============================================================
        // MAP NAME
        // =============================================================

        private static string GetCompactMapName(
            string mapName)
        {
            return mapName switch
            {
                "Valley of Loren" =>
                    "Valley",

                "Land of Trials" =>
                    "Trials",

                "Swamp of Peace" =>
                    "Swamp",

                "Raklion Boss" =>
                    "Raklion",

                "Santa Village" =>
                    "Santa",

                "Kanturu Remain" =>
                    "Remain",

                _ =>
                    mapName
            };
        }
        private static string GetCurrentMapDisplayName(
            WalkableWorldControl world)
        {
            var worldInfo =
                world.GetType()
                    .GetCustomAttribute<
                        WorldInfoAttribute>();

            if (worldInfo == null)
            {
                return $"Map {world.WorldIndex}";
            }

            string worldName =
                worldInfo.DisplayName;

            byte mapId =
                (byte)worldInfo.MapId;

            return MoveCommandDataManager
                .Instance
                .GetDisplayNameForMap(
                    mapId,
                    worldName);
        }
    }
}