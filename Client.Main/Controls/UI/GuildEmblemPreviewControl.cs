using System;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Networking;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Controls.UI
{
    public class GuildEmblemPreviewControl : UIControl
    {
        private const int SourceSize = 8;
        private const int PixelScale = 4;
        private const int PreviewSize = SourceSize * PixelScale;

        private PlayerObject _targetPlayer;
        private Texture2D _emblemTexture;

        private uint _currentGuildId;

        private float _drawRotation;
        private Vector2 _drawScale = Vector2.One;

        private readonly Color[] _palette =
        {
            Color.Transparent,              // 0
            new Color(255, 255, 255),       // 1
            new Color(255, 0, 0),           // 2
            new Color(0, 255, 0),           // 3
            new Color(0, 0, 255),           // 4
            new Color(255, 255, 0),         // 5
            new Color(255, 128, 0),         // 6
            new Color(128, 0, 255),         // 7
            new Color(0, 255, 255),         // 8
            new Color(255, 0, 255),         // 9
            new Color(128, 128, 128),       // 10
            new Color(192, 192, 192),       // 11
            new Color(128, 0, 0),           // 12
            new Color(0, 128, 0),           // 13
            new Color(0, 0, 128),           // 14
            new Color(180, 120, 60),        // 15
        };

        public GuildEmblemPreviewControl()
        {
            Interactive = false;
            AutoViewSize = false;

            ControlSize = new Point(PreviewSize, PreviewSize);
            ViewSize = ControlSize;

            Visible = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            FindGuildPlayer();

            if (_targetPlayer == null || _emblemTexture == null)
            {
                Visible = false;
                return;
            }

            if (!TryGetArmScreenPlacement(
                    _targetPlayer,
                    out Vector2 screenCenter,
                    out float rotation,
                    out Vector2 scale))
            {
                Visible = false;
                return;
            }

            _drawRotation = rotation;
            _drawScale = scale;

            X = (int)(screenCenter.X - PreviewSize * 0.5f);
            Y = (int)(screenCenter.Y - PreviewSize * 0.5f);

            Visible = true;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || _emblemTexture == null)
                return;

            var spriteBatch = GraphicsManager.Instance.Sprite;

            Vector2 drawCenter = new Vector2(
                DisplayRectangle.X + PreviewSize * 0.5f,
                DisplayRectangle.Y + PreviewSize * 0.5f);

            spriteBatch.Draw(
                _emblemTexture,
                drawCenter,
                null,
                Color.White,
                _drawRotation,
                new Vector2(
                    PreviewSize * 0.5f,
                    PreviewSize * 0.5f),
                _drawScale,
                SpriteEffects.None,
                0f);
        }

        private void FindGuildPlayer()
        {
            if (Scene is not GameScene gameScene)
            {
                _targetPlayer = null;
                return;
            }

            // 1) Personaje local primero
            if (TrySelectGuildPlayer(gameScene.Hero))
                return;

            // 2) Luego jugadores remotos
            if (gameScene.World is not WalkableWorldControl world)
            {
                _targetPlayer = null;
                return;
            }

            var players = world.Players;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerObject player = players[i];

                if (player == null || player == gameScene.Hero)
                    continue;

                if (TrySelectGuildPlayer(player))
                    return;
            }

            _targetPlayer = null;
        }

        private bool TrySelectGuildPlayer(PlayerObject player)
        {
            if (player == null)
                return false;

            ushort playerId = player.NetworkId;

            if (!GuildInfoCache.TryGetPlayerGuildId(playerId, out uint guildId))
                return false;

            if (!GuildInfoCache.TryGetGuild(guildId, out GuildInfoData guild))
                return false;

            _targetPlayer = player;

            if (_currentGuildId != guildId || _emblemTexture == null)
            {
                _currentGuildId = guildId;
                CreateEmblemTexture(guild.Logo);
            }

            return true;
        }

        private void CreateEmblemTexture(byte[] emblem)
        {
            if (emblem == null || emblem.Length != 32)
                return;

            Color[] pixels = new Color[PreviewSize * PreviewSize];

            for (int y = 0; y < SourceSize; y++)
            {
                for (int x = 0; x < SourceSize; x++)
                {
                    int byteIndex = y * 4 + (x / 2);
                    byte packed = emblem[byteIndex];

                    int colorIndex;
                    if ((x & 1) == 0)
                    {
                        colorIndex = (packed >> 4) & 0x0F;
                    }
                    else
                    {
                        colorIndex = packed & 0x0F;
                    }

                    Color color = _palette[colorIndex];

                    int startX = x * PixelScale;
                    int startY = y * PixelScale;

                    for (int py = 0; py < PixelScale; py++)
                    {
                        for (int px = 0; px < PixelScale; px++)
                        {
                            int destinationX = startX + px;
                            int destinationY = startY + py;

                            pixels[destinationY * PreviewSize + destinationX] = color;
                        }
                    }
                }
            }

            _emblemTexture?.Dispose();
            _emblemTexture = new Texture2D(GraphicsDevice, PreviewSize, PreviewSize);
            _emblemTexture.SetData(pixels);
        }

        private static bool TryGetArmScreenPlacement(
            PlayerObject player,
            out Vector2 screenCenter,
            out float rotation,
            out Vector2 scale)
        {
            screenCenter = Vector2.Zero;
            rotation = 0f;
            scale = Vector2.One;

            if (player == null)
                return false;

            // Usar hombro/clavícula en vez de brazo medio
            if (!player.TryGetLeftShoulderWorldMatrix(
                    out Matrix shoulderMatrix))
            {
                return false;
            }

            // ============================================================
            // AJUSTE FINO
            //
            // localCenter:
            //   centro del emblema, más cerca del hombro.
            //
            // localHalfWidth:
            //   ancho del emblema sobre el hombro.
            //
            // localHalfHeight:
            //   alto del emblema.
            // ============================================================

            Vector3 localCenter =
                new Vector3(
                    0f,
                    3.2f,
                    1.8f);

            Vector3 localHalfWidth =
                new Vector3(
                    0f,
                    -5.2f,
                    0f);

            Vector3 localHalfHeight =
                new Vector3(
                    0f,
                    0f,
                    5.8f);

            Vector3 worldCenter =
                Vector3.Transform(
                    localCenter,
                    shoulderMatrix);

            Vector3 worldRight =
                Vector3.Transform(
                    localCenter + localHalfWidth,
                    shoulderMatrix);

            Vector3 worldUp =
                Vector3.Transform(
                    localCenter + localHalfHeight,
                    shoulderMatrix);

            if (!TryProjectWorldToScreen(
                    worldCenter,
                    out Vector2 center2D))
            {
                return false;
            }

            if (!TryProjectWorldToScreen(
                    worldRight,
                    out Vector2 right2D))
            {
                return false;
            }

            if (!TryProjectWorldToScreen(
                    worldUp,
                    out Vector2 up2D))
            {
                return false;
            }

            Vector2 widthVector =
                right2D - center2D;

            Vector2 heightVector =
                up2D - center2D;

            // ------------------------------------------------------------
            // Evitar que el emblema se invierta cuando cambia la orientación.
            // Queremos que el "arriba" del emblema siga apuntando hacia
            // arriba en pantalla.
            // ------------------------------------------------------------
            if (heightVector.Y > 0f)
            {
                widthVector = -widthVector;
                heightVector = -heightVector;
            }

            float widthPixels =
                widthVector.Length() * 2f;

            float heightPixels =
                heightVector.Length() * 2f;

            if (widthPixels < 2f ||
                heightPixels < 2f)
            {
                return false;
            }

            rotation =
                MathF.Atan2(
                    widthVector.Y,
                    widthVector.X);

            // Un pequeño "aplastado" horizontal ayuda a que
            // parezca un parche curvado sobre el hombro.
            scale = new Vector2(
                (widthPixels / PreviewSize) * 0.92f,
                heightPixels / PreviewSize);

            screenCenter =
                center2D;

            return true;
        }

        private static bool TryProjectWorldToScreen(
            Vector3 worldPosition,
            out Vector2 screenPosition)
        {
            screenPosition = Vector2.Zero;

            Matrix viewProjection =
                Camera.Instance.View *
                Camera.Instance.Projection;

            Vector4 clip =
                Vector4.Transform(
                    new Vector4(worldPosition, 1f),
                    viewProjection);

            if (clip.W <= 0f)
                return false;

            float inverseW = 1f / clip.W;

            float ndcX = clip.X * inverseW;
            float ndcY = clip.Y * inverseW;
            float ndcZ = clip.Z * inverseW;

            if (ndcZ < 0f || ndcZ > 1f)
                return false;

            float x =
                (ndcX + 1f) *
                0.5f *
                UiScaler.VirtualSize.X;

            float y =
                (1f - ndcY) *
                0.5f *
                UiScaler.VirtualSize.Y;

            screenPosition = new Vector2(x, y);
            return true;
        }

        public override void Dispose()
        {
            _emblemTexture?.Dispose();
            _emblemTexture = null;
            _targetPlayer = null;

            base.Dispose();
        }
    }
}