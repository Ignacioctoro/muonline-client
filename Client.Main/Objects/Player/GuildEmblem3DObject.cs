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
    /// IMPORTANTE:
    /// La posición del emblema se calcula durante Draw(),
    /// no durante Update().
    ///
    /// Esto evita que el emblema quede un frame atrás
    /// cuando el PlayerObject está caminando.
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
        // MALLA DEL BRAZALETE
        // =========================================================

        private const int Segments = 6;

        private const float Height = 12.0f;
        private const float Radius = 9.0f;
        private const float ArcDegrees = 65.0f;


        // =========================================================
        // AJUSTES VISUALES
        //
        // ESTOS SON LOS 3 VALORES QUE PUEDES AJUSTAR.
        // =========================================================

        /// <summary>
        /// Baja desde el hombro hacia el codo.
        ///
        /// 0 = prácticamente en el hombro.
        /// Más alto = más abajo por el brazo.
        /// </summary>
        private const float DownFromShoulder = 1.9f;


        /// <summary>
        /// Separa el emblema de la armadura.
        ///
        /// Más alto = más afuera.
        /// Más bajo = más pegado.
        /// </summary>
        private const float OutwardOffset = 5.8f;


        /// <summary>
        /// Desplaza alrededor del brazo.
        ///
        /// Un signo apunta hacia tríceps,
        /// el otro hacia bíceps.
        /// </summary>
        private const float TricepOffset = -3.0f;
        private const float ShoulderTiltDegrees = 26.0f;


        // =========================================================
        // RECURSOS
        // =========================================================

        private VertexPositionTexture[] _vertices;
        private short[] _indices;

        private BasicEffect _effect;
        private Texture2D _emblemTexture;

        private uint _currentGuildId;
        // Slot de armadura en el inventario del jugador local.
        private const byte ArmorInventorySlot = 3;

        // Separación adicional cuando el jugador tiene armadura equipada.
        private const float ArmorExtraOutwardOffset = 5.5f;


        // Solo como referencia/debug.
        public Matrix ArmWorldMatrix { get; private set; } =
            Matrix.Identity;


        // =========================================================
        // PALETA DE COLORES MU
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

            // Efecto exclusivo para este objeto.
            // No modificamos efectos compartidos del cliente.
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
        // MALLA CURVA
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


                // Superficie cilíndrica pequeña.
                float x =
                    Radius -
                    MathF.Cos(angle) *
                    Radius;

                float y =
                    MathF.Sin(angle) *
                    Radius;

                int vertex =
                    i * 2;


                // Parte inferior.
                _vertices[vertex] =
                    new VertexPositionTexture(
                        new Vector3(
                            x,
                            y,
                            -halfHeight),
                        new Vector2(
                            u,
                            1f));


                // Parte superior.
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

            // En Update solamente actualizamos
            // los datos de Guild/textura.
            //
            // NO calculamos aquí la posición del hombro.
            //
            // WalkerObject mueve al personaje después
            // de actualizar sus hijos, por lo que hacerlo aquí
            // dejaba el emblema un frame atrás al caminar.
        
            UpdateGuildInformation(
                player);


            if (_emblemTexture == null)
            {
                Hidden = true;
                return;
            }


            Hidden = false;
        }


        // =========================================================
        // CALCULAR MATRIZ DEL BRAZALETE
        //
        // Se llama durante Draw(), cuando el PlayerObject
        // ya terminó de moverse en este frame.
        // =========================================================

        private bool TryBuildAttachmentMatrix(
            PlayerObject player,
            out Matrix finalMatrix)
        {
            finalMatrix =
                Matrix.Identity;


            // -----------------------------------------------------
            // HOMBRO
            // -----------------------------------------------------

            if (!player.TryGetLeftShoulderWorldMatrix(
                    out Matrix shoulderWorld))
            {
                return false;
            }


            // -----------------------------------------------------
            // MANO
            //
            // La usamos para saber hacia dónde apunta
            // actualmente el brazo.
            // -----------------------------------------------------

            if (!player.TryGetHandWorldMatrix(
                    true,
                    out Matrix handWorld))
            {
                return false;
            }


            Vector3 shoulderPosition =
                shoulderWorld.Translation;

            Vector3 handPosition =
                handWorld.Translation;


            // =====================================================
            // EJE HOMBRO -> MANO
            // =====================================================

            Vector3 armDown =
                handPosition -
                shoulderPosition;


            if (armDown.LengthSquared() <
                0.001f)
            {
                return false;
            }


            armDown.Normalize();


            // Dirección mano -> hombro.
            //
            // Esta será la vertical del emblema.
            Vector3 armUp =
                -armDown;


            // =====================================================
            // HACIA AFUERA DEL CUERPO
            // =====================================================

            Vector3 bodyPosition =
                player.WorldPosition.Translation;


            Vector3 outward =
                shoulderPosition -
                bodyPosition;


            // Eliminamos la parte del vector
            // que va a lo largo del brazo.
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
            // ALREDEDOR DEL BRAZO
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

            // =========================================================
            // COMPENSACIÓN POR ARMADURA
            // =========================================================

            float armorExtraOffset =
                HasArmorEquipped(player)
                    ? ArmorExtraOutwardOffset
                    : 0f;


            // =========================================================
            // POSICIÓN FINAL
            // =========================================================

            Vector3 targetPosition =
                shoulderPosition
                + armDown * DownFromShoulder
                + outward *
                (OutwardOffset + armorExtraOffset)
                + around * TricepOffset;


            // =====================================================
            // ORIENTACIÓN
            //
            // Local X = hacia afuera del personaje
            // Local Y = alrededor del brazo
            // Local Z = mano -> hombro
            //
            // Nuestra malla tiene la altura en Z,
            // por eso sigue verticalmente el brazo.
            // =====================================================

            finalMatrix =
                new Matrix(
                    // Local X
                    -outward.X,
                    -outward.Y,
                    -outward.Z,
                    0f,

                    // Local Y
                    -around.X,
                    -around.Y,
                    -around.Z,
                    0f,

                    // Local Z
                    armUp.X,
                    armUp.Y,
                    armUp.Z,
                    0f,

                    // Posición mundial
                    targetPosition.X,
                    targetPosition.Y,
                    targetPosition.Z,
                    1f);
            // Ligera inclinación vertical siguiendo el hombro.
            finalMatrix =
                Matrix.CreateRotationY(
                    MathHelper.ToRadians(ShoulderTiltDegrees))
                *
                finalMatrix;        

            ArmWorldMatrix =
                shoulderWorld;


            return true;
        }

        private static bool HasArmorEquipped(
            PlayerObject player)
        {
            if (player == null)
                return false;

            // =========================================================
            // JUGADOR LOCAL
            // =========================================================

            if (player.IsMainWalker)
            {
                var characterState =
                    MuGame.Network?.GetCharacterState();

                var inventory =
                    characterState?.GetInventoryItems();

                return inventory != null &&
                    inventory.ContainsKey(
                        ArmorInventorySlot);
            }


            // =========================================================
            // JUGADOR REMOTO
            //
            // Los jugadores remotos no tienen acceso al inventario
            // completo del cliente local.
            //
            // Su equipamiento viene codificado en Appearance.
            // =========================================================

            short armorIndex =
                player.Appearance.ArmorItemIndex;

            return armorIndex >= 0 &&
                armorIndex != 0xFF &&
                armorIndex != 0x1FF;
        }
        // =========================================================
        // GUILD INFO
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
        // CREAR TEXTURA
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


            if (Parent is not PlayerObject player)
            {
                return;
            }


            // =====================================================
            // MUY IMPORTANTE:
            //
            // Calculamos la posición AHORA.
            //
            // A esta altura del frame el PlayerObject
            // ya terminó de ejecutar UpdatePosition().
            //
            // Así evitamos que el brazalete se quede atrás
            // cuando el personaje camina.
            // =====================================================

            if (!TryBuildAttachmentMatrix(
                    player,
                    out Matrix attachmentMatrix))
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
                // Profundidad 3D real.
                gd.DepthStencilState =
                    DepthStencilState.Default;


                // Transparencia de la Guild.
                gd.BlendState =
                    BlendState.AlphaBlend;


                // Seguimos mostrando ambas caras
                // durante calibración.
                gd.RasterizerState =
                    RasterizerState.CullNone;


                // Pixel art sin filtrado.
                gd.SamplerStates[0] =
                    SamplerState.PointClamp;


                // IMPORTANTE:
                //
                // Ya no usamos WorldPosition aquí.
                //
                // Usamos la matriz recién calculada
                // con la posición actual del personaje.
                _effect.World =
                    attachmentMatrix;


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