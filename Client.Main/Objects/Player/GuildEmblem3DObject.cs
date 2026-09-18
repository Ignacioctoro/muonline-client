using System;
using System.Threading.Tasks;
using Client.Main.Graphics;
using Client.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Player
{
    /// <summary>
    /// Emblema 3D de Guild anclado al hombro/brazo izquierdo.
    ///
    /// La posición se controla con valores intuitivos:
    ///
    /// DownFromShoulder = hombro -> codo
    /// OutwardOffset    = pegado -> afuera de la armadura
    /// TricepOffset     = bíceps <-> tríceps
    ///
    /// La orientación vertical se calcula automáticamente
    /// siguiendo la dirección real del brazo.
    /// </summary>
    public sealed class GuildEmblem3DObject : WorldObject
    {
        // =========================================================
        // TEXTURA GUILD
        // =========================================================

        private const int SourceSize = 8;
        private const int TextureScale = 4;
        private const int TextureSize =
            SourceSize * TextureScale;


        // =========================================================
        // GEOMETRÍA DEL BRAZALETE
        // =========================================================

        private const int Segments = 6;

        private const float Height = 17.0f;
        private const float Radius = 13.0f;
        private const float ArcDegrees = 75.0f;


        // =========================================================
        // AJUSTES DE POSICIÓN
        //
        // ESTOS SON LOS VALORES QUE DEBES MODIFICAR.
        // =========================================================

        /// <summary>
        /// Distancia desde el hombro hacia el codo.
        ///
        /// 0  = hombro
        /// 3  = parte alta del brazo
        /// 8  = mitad del brazo
        /// 15 = cerca del codo
        /// </summary>
        private const float DownFromShoulder = 3.0f;

        /// <summary>
        /// Separación respecto al cuerpo/armadura.
        ///
        /// Más alto = más afuera.
        /// Más bajo = más pegado.
        /// </summary>
        private const float OutwardOffset = 2.5f;

        /// <summary>
        /// Mueve el emblema alrededor del brazo.
        ///
        /// Un signo será tríceps y el otro bíceps.
        /// Si -3 queda en el lado incorrecto,
        /// simplemente usa +3.
        /// </summary>
        private const float TricepOffset = -3.0f;


        // =========================================================
        // RECURSOS
        // =========================================================

        private VertexPositionTexture[] _vertices;
        private short[] _indices;

        private BasicEffect _effect;
        private Texture2D _emblemTexture;

        private uint _currentGuildId;


        // =========================================================
        // DEBUG / REFERENCIA
        // =========================================================

        public Matrix ArmWorldMatrix { get; private set; } =
            Matrix.Identity;


        // =========================================================
        // PALETA GUILD MU
        // =========================================================

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


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public GuildEmblem3DObject()
        {
            Interactive = false;
            Hidden = true;
        }


        // =========================================================
        // LOAD
        // =========================================================

        public override Task LoadContent()
        {
            CreateMesh();

            // BasicEffect exclusivo del emblema.
            // No usamos efectos compartidos del renderer.
            _effect =
                new BasicEffect(GraphicsDevice)
                {
                    VertexColorEnabled = false,
                    TextureEnabled = true,
                    LightingEnabled = false
                };

            return Task.CompletedTask;
        }


        // =========================================================
        // CREAR MALLA CURVA
        // =========================================================

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


                // Curvatura cilíndrica.
                //
                // X = profundidad / salida del parche
                // Y = recorrido alrededor del brazo
                // Z = altura del emblema
                float x =
                    Radius -
                    MathF.Cos(angle) *
                    Radius;

                float y =
                    MathF.Sin(angle) *
                    Radius;

                int vertex =
                    i * 2;


                // Parte inferior
                _vertices[vertex] =
                    new VertexPositionTexture(
                        new Vector3(
                            x,
                            y,
                            -halfHeight),
                        new Vector2(
                            1f - u,
                            1f));


                // Parte superior
                _vertices[vertex + 1] =
                    new VertexPositionTexture(
                        new Vector3(
                            x,
                            y,
                            halfHeight),
                        new Vector2(
                            1f - u,
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


                // Triángulo 1
                _indices[index++] =
                    bottomLeft;

                _indices[index++] =
                    topLeft;

                _indices[index++] =
                    bottomRight;


                // Triángulo 2
                _indices[index++] =
                    bottomRight;

                _indices[index++] =
                    topLeft;

                _indices[index++] =
                    topRight;
            }
        }


        // =========================================================
        // UPDATE
        // =========================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);


            if (Parent is not PlayerObject player)
            {
                Hidden = true;
                return;
            }


            // Por ahora solamente el personaje local.
            if (!player.IsMainWalker)
            {
                Hidden = true;
                return;
            }


            // -----------------------------------------------------
            // ACTUALIZAR INFORMACIÓN DE GUILD
            // -----------------------------------------------------

            UpdateGuildInformation(
                player);


            if (_emblemTexture == null)
            {
                Hidden = true;
                return;
            }


            // -----------------------------------------------------
            // MATRIZ DEL HOMBRO
            //
            // Este será nuestro punto de anclaje principal.
            // -----------------------------------------------------

            if (!player.TryGetLeftShoulderWorldMatrix(
                    out Matrix shoulderWorld))
            {
                Hidden = true;
                return;
            }


            // -----------------------------------------------------
            // MATRIZ DE LA MANO
            //
            // Solo la usamos para descubrir hacia dónde
            // apunta realmente el brazo.
            // -----------------------------------------------------

            if (!player.TryGetHandWorldMatrix(
                    true,
                    out Matrix handWorld))
            {
                Hidden = true;
                return;
            }


            Vector3 shoulderPosition =
                shoulderWorld.Translation;

            Vector3 handPosition =
                handWorld.Translation;


            // =====================================================
            // DIRECCIÓN HOMBRO -> MANO
            // =====================================================

            Vector3 armDown =
                handPosition -
                shoulderPosition;


            if (armDown.LengthSquared() <
                0.001f)
            {
                Hidden = true;
                return;
            }


            armDown.Normalize();


            // Dirección contraria:
            //
            // mano -> hombro
            //
            // Será nuestro eje vertical.
            Vector3 armUp =
                -armDown;


            // =====================================================
            // DIRECCIÓN HACIA AFUERA DEL CUERPO
            // =====================================================

            Vector3 bodyPosition =
                player.WorldPosition.Translation;


            Vector3 outward =
                shoulderPosition -
                bodyPosition;


            // Eliminamos cualquier componente que esté
            // apuntando a lo largo del brazo.
            //
            // Queremos solamente "salir" del brazo/cuerpo.
            outward -=
                armDown *
                Vector3.Dot(
                    outward,
                    armDown);


            if (outward.LengthSquared() <
                0.001f)
            {
                outward =
                    Vector3.UnitX;
            }
            else
            {
                outward.Normalize();
            }


            // =====================================================
            // DIRECCIÓN ALREDEDOR DEL BRAZO
            // =====================================================

            Vector3 around =
                Vector3.Cross(
                    armUp,
                    outward);


            if (around.LengthSquared() <
                0.001f)
            {
                around =
                    Vector3.UnitY;
            }
            else
            {
                around.Normalize();
            }


            // =====================================================
            // POSICIÓN FINAL
            // =====================================================

            Vector3 targetPosition =
                shoulderPosition

                // Hombro -> codo
                + armDown *
                  DownFromShoulder

                // Separación respecto a la armadura
                + outward *
                  OutwardOffset

                // Bíceps <-> tríceps
                + around *
                  TricepOffset;


            // =====================================================
            // ORIENTACIÓN FINAL
            //
            // Ya no usamos:
            //
            // RotationX(105)
            // RotationZ(-48)
            //
            // Construimos una orientación directamente
            // a partir del brazo.
            //
            // Local X = hacia afuera
            // Local Y = alrededor del brazo
            // Local Z = hacia el hombro
            //
            // Como la malla tiene su altura en Z,
            // esto hace que la T quede vertical
            // siguiendo el brazo.
            // =====================================================

            Matrix finalMatrix =
            new Matrix(
                // Local X
                outward.X,
                outward.Y,
                outward.Z,
                0f,

                // Local Y
                around.X,
                around.Y,
                around.Z,
                0f,

                // Local Z
                armUp.X,
                armUp.Y,
                armUp.Z,
                0f,

                // Posición
                targetPosition.X,
                targetPosition.Y,
                targetPosition.Z,
                1f);


            ArmWorldMatrix =
                shoulderWorld;

            WorldPosition =
                finalMatrix;


            Hidden = false;
        }


        // =========================================================
        // INFORMACIÓN DE GUILD
        // =========================================================

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


        // =========================================================
        // CREAR TEXTURA DEL EMBLEMA
        // =========================================================

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


                    // Pixel par:
                    // nibble alto.
                    if ((x & 1) == 0)
                    {
                        colorIndex =
                            (packed >> 4) &
                            0x0F;
                    }

                    // Pixel impar:
                    // nibble bajo.
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


        // =========================================================
        // DRAW
        // =========================================================

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


            // Guardamos solamente los estados
            // que vamos a modificar.
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
                // Profundidad 3D real.
                gd.DepthStencilState =
                    DepthStencilState.Default;


                // Necesario porque el color 0
                // de la Guild es transparente.
                gd.BlendState =
                    BlendState.AlphaBlend;


                // Durante la calibración queremos
                // ver ambas caras del parche.
                gd.RasterizerState =
                    RasterizerState.CullNone;


                // Pixel-art sin suavizado.
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
                // Restauramos estados.
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


        // =========================================================
        // DISPOSE
        // =========================================================

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