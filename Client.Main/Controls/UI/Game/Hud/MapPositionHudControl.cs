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

        private const int TOTAL_HEIGHT =
            MAP_FRAME_HEIGHT;

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

            if (!Visible ||
                Scene == null)
            {
                return;
            }

            Point mouse =
                MuGame.Instance
                    .UiMouseState
                    .Position;

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