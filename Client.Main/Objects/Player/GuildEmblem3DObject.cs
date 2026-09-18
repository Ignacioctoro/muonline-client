using System;
using System.Threading.Tasks;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Player
{
    /// <summary>
    /// Emblema Guild 3D adherido al UpperArm izquierdo.
    ///
    /// Etapa actual:
    /// - Solo se muestra en el jugador local.
    /// - Usa la imagen real 8x8 de la Guild.
    /// - Usa un BasicEffect propio.
    /// - No modifica efectos compartidos del renderer.
    /// </summary>
    public sealed class GuildEmblem3DObject : WorldObject
    {
        private const int SourceSize = 8;
        private const int TextureScale = 4;
        private const int TextureSize = SourceSize * TextureScale;

        private const int Segments = 6;

        private const float Height = 17.0f;
        private const float Radius = 13.0f;
        private const float ArcDegrees = 75.0f;

        private VertexPositionTexture[] _vertices;
        private short[] _indices;

        private BasicEffect _effect;
        private Texture2D _emblemTexture;

        private uint _currentGuildId;

        public Matrix ArmWorldMatrix { get; private set; } =
            Matrix.Identity;

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
            new Color(180, 120, 60)         // 15
        };

        public GuildEmblem3DObject()
        {
            Interactive = false;

            Hidden = true;
        }

        public override Task LoadContent()
        {
            CreateMesh();

            _effect =
                new BasicEffect(GraphicsDevice)
                {
                    VertexColorEnabled = false,
                    TextureEnabled = true,
                    LightingEnabled = false
                };

            return Task.CompletedTask;
        }

        private void CreateMesh()
        {
            int columns =
                Segments + 1;

            _vertices =
                new VertexPositionTexture[
                    columns * 2];

            float halfArc =
                MathHelper.ToRadians(
                    ArcDegrees * 0.5f);

            float halfHeight =
                Height * 0.5f;

            for (int i = 0;
                 i <= Segments;
                 i++)
            {
                float u =
                    i / (float)Segments;

                float angle =
                    MathHelper.Lerp(
                        -halfArc,
                        halfArc,
                        u);

                float x =
                    Radius -
                    MathF.Cos(angle) *
                    Radius;

                float y =
                    MathF.Sin(angle) *
                    Radius;

                int vertex =
                    i * 2;

                // Inferior
                _vertices[vertex] =
                    new VertexPositionTexture(
                        new Vector3(
                            x,
                            y,
                            -halfHeight),
                        new Vector2(
                            u,
                            1f));

                // Superior
                _vertices[vertex + 1] =
                    new VertexPositionTexture(
                        new Vector3(
                            x,
                            y,
                            halfHeight),
                        new Vector2(
                            u,
                            0f));
            }

            _indices =
                new short[
                    Segments * 6];

            int index = 0;

            for (int i = 0;
                 i < Segments;
                 i++)
            {
                short bottomLeft =
                    (short)(i * 2);

                short topLeft =
                    (short)(
                        bottomLeft + 1);

                short bottomRight =
                    (short)(
                        bottomLeft + 2);

                short topRight =
                    (short)(
                        bottomLeft + 3);

                _indices[index++] =
                    bottomLeft;

                _indices[index++] =
                    topLeft;

                _indices[index++] =
                    bottomRight;

                _indices[index++] =
                    bottomRight;

                _indices[index++] =
                    topLeft;

                _indices[index++] =
                    topRight;
            }
        }

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (Parent is not PlayerObject player)
            {
                Hidden = true;
                return;
            }

            // Por ahora solamente nuestro personaje.
            if (!player.IsMainWalker)
            {
                Hidden = true;
                return;
            }

            UpdateGuildInformation(
                player);

            if (_emblemTexture == null)
            {
                Hidden = true;
                return;
            }

            if (!player.TryGetLeftUpperArmWorldMatrix(
                    out Matrix armWorld))
            {
                Hidden = true;
                return;
            }

            ArmWorldMatrix =
                armWorld;

            // Volvemos a la posición que sí habíamos
            // comprobado que quedaba sobre el UpperArm.
            //
            // TODAVÍA NO intentamos moverla al tríceps.
            Matrix localOffset =
                Matrix.CreateTranslation(
                    new Vector3(
                        6.0f,
                        0.0f,
                        1.5f));

            WorldPosition =
                localOffset *
                armWorld;

            Hidden = false;
        }

        private void UpdateGuildInformation(
            PlayerObject player)
        {
            ushort playerId =
                player.NetworkId;

            if (!GuildInfoCache.TryGetPlayerGuildId(
                    playerId,
                    out uint guildId))
            {
                _currentGuildId = 0;

                Hidden = true;

                return;
            }

            if (!GuildInfoCache.TryGetGuild(
                    guildId,
                    out GuildInfoData guild))
            {
                Hidden = true;

                return;
            }

            if (guild.Logo == null ||
                guild.Logo.Length != 32)
            {
                Hidden = true;

                return;
            }

            if (_currentGuildId != guildId ||
                _emblemTexture == null)
            {
                _currentGuildId =
                    guildId;

                CreateEmblemTexture(
                    guild.Logo);
            }
        }

        private void CreateEmblemTexture(
            byte[] emblem)
        {
            if (emblem == null ||
                emblem.Length != 32)
            {
                return;
            }

            Color[] pixels =
                new Color[
                    TextureSize *
                    TextureSize];

            for (int y = 0;
                 y < SourceSize;
                 y++)
            {
                for (int x = 0;
                     x < SourceSize;
                     x++)
                {
                    int byteIndex =
                        y * 4 +
                        (x / 2);

                    byte packed =
                        emblem[
                            byteIndex];

                    int colorIndex;

                    // Cada byte guarda 2 píxeles.
                    //
                    // Pixel par = nibble alto.
                    // Pixel impar = nibble bajo.
                    if ((x & 1) == 0)
                    {
                        colorIndex =
                            (packed >> 4) &
                            0x0F;
                    }
                    else
                    {
                        colorIndex =
                            packed &
                            0x0F;
                    }

                    Color color =
                        _palette[
                            colorIndex];

                    int startX =
                        x *
                        TextureScale;

                    int startY =
                        y *
                        TextureScale;

                    for (int py = 0;
                         py < TextureScale;
                         py++)
                    {
                        for (int px = 0;
                             px < TextureScale;
                             px++)
                        {
                            int destinationX =
                                startX + px;

                            int destinationY =
                                startY + py;

                            pixels[
                                destinationY *
                                TextureSize +
                                destinationX] =
                                color;
                        }
                    }
                }
            }

            _emblemTexture?.Dispose();

            _emblemTexture =
                new Texture2D(
                    GraphicsDevice,
                    TextureSize,
                    TextureSize);

            _emblemTexture.SetData(
                pixels);
        }

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible ||
                _vertices == null ||
                _indices == null ||
                _effect == null ||
                _emblemTexture == null)
            {
                return;
            }

            GraphicsDevice gd =
                GraphicsDevice;

            DepthStencilState previousDepth =
                gd.DepthStencilState;

            BlendState previousBlend =
                gd.BlendState;

            RasterizerState previousRasterizer =
                gd.RasterizerState;

            SamplerState previousSampler =
                gd.SamplerStates[0];

            try
            {
                gd.DepthStencilState =
                    DepthStencilState.Default;

                // La Guild tiene colores sólidos.
                // Para esta primera prueba usamos AlphaBlend
                // porque el índice 0 del emblema es transparente.
                gd.BlendState =
                    BlendState.AlphaBlend;

                // Ver ambas caras durante la calibración.
                gd.RasterizerState =
                    RasterizerState.CullNone;

                // Mantiene el aspecto pixel-art.
                gd.SamplerStates[0] =
                    SamplerState.PointClamp;

                _effect.World =
                    WorldPosition;

                _effect.View =
                    Camera.Instance.View;

                _effect.Projection =
                    Camera.Instance.Projection;

                _effect.Texture =
                    _emblemTexture;

                _effect.DiffuseColor =
                    Vector3.One;

                _effect.Alpha =
                    1.0f;

                foreach (EffectPass pass
                         in _effect
                             .CurrentTechnique
                             .Passes)
                {
                    pass.Apply();

                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        _vertices,
                        0,
                        _vertices.Length,
                        _indices,
                        0,
                        _indices.Length / 3);
                }
            }
            finally
            {
                gd.DepthStencilState =
                    previousDepth;

                gd.BlendState =
                    previousBlend;

                gd.RasterizerState =
                    previousRasterizer;

                gd.SamplerStates[0] =
                    previousSampler;
            }
        }

        public override void Dispose()
        {
            _vertices = null;
            _indices = null;

            _emblemTexture?.Dispose();
            _emblemTexture = null;

            _effect?.Dispose();
            _effect = null;

            base.Dispose();
        }
    }
}