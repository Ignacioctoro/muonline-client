#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Classic BK Greater Fortitude / Swell Life / Inner cast.
    ///
    /// Port basado en el comportamiento original:
    ///
    /// 36 x BITMAP_JOINT_SPIRIT subtype 2
    ///     Angle.X = -10
    ///     Angle.Y = 0
    ///     Angle.Z = i * 10
    ///     Position.Z += 100
    ///     Velocity = 50
    ///     LifeTime = 20
    ///     Scale = 60
    ///     MaxTails = 3
    ///     RenderType = ALPHA_BLEND / GLOW (ONE + ONE)
    ///     RenderFace = TWO
    ///     Light = (0.5, 0.5, 0.5)
    ///
    /// Cada joint crea un BITMAP_LIGHT en su cabeza:
    ///
    ///     Scale = 4 + (20 - LifeTime) / 5
    ///     Light = (1.0, 0.5, 0.1)
    ///
    /// Durante el comienzo del joint se crea además:
    ///
    ///     BITMAP_FLARE subtype 2
    ///     Scale = 40
    ///     MaxTails = 20
    ///     Direction.Z = 35..54
    ///     LifeTime = 25..74
    ///
    /// Y el cast crea exactamente dos:
    ///
    ///     BITMAP_MAGIC + 1 subtype 4
    ///     i = 0   -> Angle.Z = 0
    ///     i = 20  -> Angle.Z = 200
    ///
    /// El efecto persistente que queda en pelo/torso mientras dura
    /// Greater Fortitude vive en ClassicGreaterFortitudeGlowEffect.
    /// </summary>
    public sealed class SwellLifeCastEffect : EffectObject
    {
        private const string SpiritTexturePath =
            "Effect/JointSpirit01.jpg";

        private const string MagicTexturePath =
            "Effect/Magic_Ground2.jpg";

        private const string LightTexturePath =
            "Effect/flare01.jpg";

        private const string FlareTexturePath =
            "Effect/Flare.OZJ";
        private static readonly Vector3 WarmOuterFlareLight =
            new Vector3(
                1.00f,
                0.62f,
                0.16f);

        private static readonly Vector3 WarmSpiritHeadLight =
            new Vector3(
                1.00f,
                0.50f,
                0.10f);

        // ============================================================
        // CLASSIC TIMING
        // ============================================================

        private const float ClassicReferenceFps =
            25.0f;

        //
        // La primera traducción se percibía algo más lenta que Main
        // dentro de la cámara/animación de Neffis.
        //
        // Mantenemos una compensación pequeña, pero toda la geometría,
        // tail count y escalas de las texturas siguen la lógica original.
        //
        private const float SpiritPlaybackRate =
            1.25f;

        // ============================================================
        // SPIRIT JOINT
        // ============================================================

        private const int SpiritCount =
            36;

        //
        // Main:
        //     MaxTails = 3
        //
        // CreateTail limita NumTails a MaxTails - 1.
        //
        // Por lo tanto hay:
        //     3 estados de tail
        //     2 segmentos dibujados
        //
        // NO 3 segmentos.
        //
        private const int SpiritTailStateCount =
            3;

        private const int SpiritSegmentCount =
            2;

        private const int SpiritVerticesPerState =
            2;

        private const int SpiritVerticesPerJoint =
            SpiritTailStateCount *
            SpiritVerticesPerState;

        private const int SpiritTotalVertices =
            SpiritCount *
            SpiritVerticesPerJoint;

        private const int SpiritIndicesPerSegment =
            6;

        private const int SpiritTotalIndices =
            SpiritCount *
            SpiritSegmentCount *
            SpiritIndicesPerSegment;

        //
        // Main:
        //     Scale = 60
        //
        private const float SpiritWidth =
            60.0f;

        //
        // Conversión de la distancia de movimiento a la escala visual
        // que está usando este cliente MonoGame.
        //
        // La geometría y ancho permanecen clásicos; solo la distancia
        // de desplazamiento requiere esta conversión.
        //
        private const float SpiritMotionScale =
            0.30f;

        private const float SpiritDuration =
            20.0f /
            (
                ClassicReferenceFps *
                SpiritPlaybackRate
            );

        // ============================================================
        // BITMAP_FLARE subtype 2
        // ============================================================

        private const int FlareCount =
            36;

        //
        // Main:
        //     MaxTails = 20
        //
        // Igual que arriba:
        //     20 estados
        //     19 segmentos máximos
        //
        private const int FlareTailStateCount =
            20;

        private const int FlareSegmentCount =
            FlareTailStateCount - 1;

        private const int FlareVerticesPerState =
            4;

        private const int FlareVerticesPerJoint =
            FlareTailStateCount *
            FlareVerticesPerState;

        private const int FlareTotalVertices =
            FlareCount *
            FlareVerticesPerJoint;

        private const int FlareIndicesPerSegment =
            12;

        private const int FlareTotalIndices =
            FlareCount *
            FlareSegmentCount *
            FlareIndicesPerSegment;

        private const float FlareWidth =
            40.0f;

        private const float FlareMotionScale =
            0.22f;

        private const float FlareHorizontalScale =
            0.62f;

        private const float FlareStartBelowCenter =
            145.0f;

        //
        // En Main el FLARE se crea durante el primer movimiento
        // del SPIRIT, no exactamente en el mismo instante de creación.
        //
        private const float FlareSpawnDelayFrames =
            1.0f;

        // ============================================================
        // MAGIC_GROUND2
        // ============================================================

        private const float GroundDuration =
            40.0f /
            ClassicReferenceFps;

        //
        // CRÍTICO:
        //
        // RenderTerrainAlphaBitmap recibe Size en tiles.
        //
        // TERRAIN_SCALE clásico = 100.
        //
        // Nuestro quad local va desde -1 a +1, por lo que tiene ancho 2.
        // Para obtener:
        //
        //     ancho final = Size * 100
        //
        // necesitamos:
        //
        //     2 * (Size * 50)
        //
        // La versión anterior usaba 82 y hacía el halo del suelo
        // aproximadamente 64% más grande de lo debido.
        //
        private const float GroundHalfTerrainScale =
            50.0f;

        // ============================================================
        // CLASSIC GLOW BLEND
        // ============================================================

        /// <summary>
        /// EnableAlphaBlend() de Main usa Glow:
        ///
        ///     GL_ONE, GL_ONE
        ///
        /// No es el BlendState.Additive estándar de MonoGame.
        /// </summary>
        private static readonly BlendState ClassicGlowBlend =
            new BlendState
            {
                ColorBlendFunction =
                    BlendFunction.Add,

                ColorSourceBlend =
                    Blend.One,

                ColorDestinationBlend =
                    Blend.One,

                AlphaBlendFunction =
                    BlendFunction.Add,

                AlphaSourceBlend =
                    Blend.One,

                AlphaDestinationBlend =
                    Blend.One
            };

        // ============================================================
        // OWNER
        // ============================================================

        private readonly WalkerObject _caster;

        // ============================================================
        // SPIRIT DATA
        // ============================================================

        private readonly VertexPositionColorTexture[] _spiritVertices =
            new VertexPositionColorTexture[
                SpiritTotalVertices];

        private readonly short[] _spiritIndices =
            new short[
                SpiritTotalIndices];

        private readonly Vector3[] _spiritHeads =
            new Vector3[
                SpiritCount];

        private readonly float[] _spiritHeadRotations =
            new float[
                SpiritCount];

        // ============================================================
        // FLARE DATA
        // ============================================================

        private readonly VertexPositionColorTexture[] _flareVertices =
            new VertexPositionColorTexture[
                FlareTotalVertices];

        private readonly short[] _flareIndices =
            new short[
                FlareTotalIndices];

        private readonly FlareState[] _flares =
            new FlareState[
                FlareCount];

        private struct FlareState
        {
            public Vector2 Offset;

            public float InitialLife;

            public float InitialVelocity;
        }

        // ============================================================
        // GRAPHICS
        // ============================================================

        private Texture2D? _spiritTexture;

        private Texture2D? _magicTexture;

        private Texture2D? _lightTexture;

        private Texture2D? _flareTexture;

        private SpriteBatch? _spriteBatch;

        private BasicEffect? _jointEffect;

        // ============================================================
        // STATE
        // ============================================================

        private float _time;

        private float _totalDuration;

        //
        // BITMAP_MAGIC+1 subtype 4:
        //
        // Scale =
        // ((rand()%50)+50)/100.f*4.f
        //
        private readonly float _groundScale1;

        private readonly float _groundScale2;

        //
        // Los dos MAGIC se crean cuando:
        //
        //     i == 0
        //     i == 20
        //
        // por eso sus ángulos son:
        //
        //     0°
        //     200°
        //
        // RenderTerrainAlphaBitmap usa -Angle.Z.
        //
        private const float GroundRotation1 =
            0.0f;

        private static readonly float GroundRotation2 =
            MathHelper.ToRadians(
                -200.0f);

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public SwellLifeCastEffect(
            WalkerObject caster)
        {
            _caster =
                caster ??
                throw new ArgumentNullException(
                    nameof(caster));

            IsTransparent =
                true;

            AffectedByTransparency =
                true;

            BlendState =
                ClassicGlowBlend;

            DepthState =
                GraphicsManager.ReadOnlyDepth;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -1000f,
                        -1000f,
                        -300f),
                    new Vector3(
                        1000f,
                        1000f,
                        1000f));

            _groundScale1 =
                2.0f +
                (float)
                MuGame.Random.NextDouble() *
                1.96f;

            _groundScale2 =
                2.0f +
                (float)
                MuGame.Random.NextDouble() *
                1.96f;

            for (int i = 0;
                 i < SpiritCount;
                 i++)
            {
                _spiritHeadRotations[i] =
                    MathHelper.ToRadians(
                        MuGame.Random.Next(
                            0,
                            360));
            }

            float longestFlareLife =
                0f;

            for (int i = 0;
                 i < FlareCount;
                 i++)
            {
                float initialLife =
                    MuGame.Random.Next(
                        25,
                        75);

                _flares[i] =
                    new FlareState
                    {
                        Offset =
                            new Vector2(
                                MuGame.Random.Next(
                                    -100,
                                    100) *
                                FlareHorizontalScale,

                                MuGame.Random.Next(
                                    -100,
                                    100) *
                                FlareHorizontalScale),

                        InitialLife =
                            initialLife,

                        InitialVelocity =
                            MuGame.Random.Next(
                                35,
                                55)
                    };

                longestFlareLife =
                    MathF.Max(
                        longestFlareLife,
                        initialLife);
            }

            _totalDuration =
                MathF.Max(
                    GroundDuration,
                    (
                        longestFlareLife +
                        FlareSpawnDelayFrames
                    ) /
                    ClassicReferenceFps);

            BuildSpiritIndices();

            BuildFlareIndices();
        }

        // ============================================================
        // LOAD
        // ============================================================

        public override async Task LoadContent()
        {
            await base.LoadContent();

            await TextureLoader.Instance.Prepare(
                SpiritTexturePath);

            await TextureLoader.Instance.Prepare(
                MagicTexturePath);

            await TextureLoader.Instance.Prepare(
                LightTexturePath);

            await TextureLoader.Instance.Prepare(
                FlareTexturePath);

            _spiritTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        SpiritTexturePath);

            _magicTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        MagicTexturePath);

            _lightTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        LightTexturePath);

            _flareTexture =
                TextureLoader.Instance
                    .GetTexture2D(
                        FlareTexturePath);

            _spiritTexture ??=
                GraphicsManager.Instance.Pixel;

            _magicTexture ??=
                GraphicsManager.Instance.Pixel;

            _lightTexture ??=
                GraphicsManager.Instance.Pixel;

            //
            // Si Flare.OZJ no puede cargarse,
            // flare01 es un fallback visual mucho mejor
            // que un cuadrado blanco.
            //
            _flareTexture ??=
                _lightTexture;

            _spriteBatch =
                GraphicsManager.Instance.Sprite;

            _jointEffect =
                new BasicEffect(
                    GraphicsDevice)
                {
                    TextureEnabled =
                        true,

                    VertexColorEnabled =
                        true,

                    LightingEnabled =
                        false,

                    FogEnabled =
                        false
                };
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(gameTime);

            if (_caster.Status ==
                    GameControlStatus.Disposed ||
                _caster.World == null)
            {
                RemoveSelf();

                return;
            }

            Position =
                _caster.WorldPosition
                    .Translation;

            _time +=
                (float)
                gameTime.ElapsedGameTime
                    .TotalSeconds;

            if (_time <=
                SpiritDuration)
            {
                BuildSpiritGeometry();
            }

            BuildFlareGeometry();

            if (_time >=
                _totalDuration)
            {
                RemoveSelf();
            }
        }

        // ============================================================
        // SPIRIT GEOMETRY
        // ============================================================

        private void BuildSpiritGeometry()
        {
            Vector3 center =
                GetClassicSpiritCenter();

            float currentFrame =
                MathHelper.Clamp(
                    _time *
                    ClassicReferenceFps *
                    SpiritPlaybackRate,
                    0f,
                    20f);

            float spiritLuminosity =
                CalculateSpiritLuminosity(
                    currentFrame);

            Color spiritColor =
                ToColor(
                    new Vector3(
                        0.5f,
                        0.5f,
                        0.5f) *
                    spiritLuminosity);

            Color invisible =
                new Color(
                    0,
                    0,
                    0,
                    0);

            for (int joint = 0;
                 joint < SpiritCount;
                 joint++)
            {
                float yaw =
                    MathHelper.ToRadians(
                        joint *
                        10f);

                Vector3 direction =
                    CalculateSpiritDirection(
                        yaw);

                //
                // Esta es exactamente la orientación de
                // local X después de AngleMatrix con Y=0.
                //
                // El movimiento radial es perpendicular
                // a esta dirección, igual que CreateTail().
                //
                Vector3 tangent =
                    new Vector3(
                        MathF.Cos(yaw),
                        MathF.Sin(yaw),
                        0f);

                if (tangent.LengthSquared() >
                    0.0001f)
                {
                    tangent.Normalize();
                }

                int vertexStart =
                    joint *
                    SpiritVerticesPerJoint;

                for (int state = 0;
                     state < SpiritTailStateCount;
                     state++)
                {
                    float sampleFrame =
                        currentFrame -
                        state;

                    bool valid =
                        sampleFrame >=
                        0f;

                    sampleFrame =
                        MathF.Max(
                            sampleFrame,
                            0f);

                    float distance =
                        CalculateSpiritDistance(
                            sampleFrame);

                    Vector3 point =
                        center +
                        direction *
                        distance;

                    Vector3 widthOffset =
                        tangent *
                        (
                            SpiritWidth *
                            0.5f
                        );

                    //
                    // Main UV:
                    //
                    // MaxTails = 3
                    //
                    // head/actual = 1.0
                    // anterior    = 0.5
                    // anterior    = 0.0
                    //
                    float u =
                        1f -
                        state /
                        (float)
                        (
                            SpiritTailStateCount -
                            1
                        );

                    int v =
                        vertexStart +
                        state *
                        SpiritVerticesPerState;

                    Color color =
                        valid
                            ? spiritColor
                            : invisible;

                    _spiritVertices[v + 0] =
                        new VertexPositionColorTexture(
                            point -
                            widthOffset,
                            color,
                            new Vector2(
                                u,
                                0f));

                    _spiritVertices[v + 1] =
                        new VertexPositionColorTexture(
                            point +
                            widthOffset,
                            color,
                            new Vector2(
                                u,
                                1f));
                }

                float headDistance =
                    CalculateSpiritDistance(
                        currentFrame);

                _spiritHeads[joint] =
                    center +
                    direction *
                    headDistance;
            }
        }

        /// <summary>
        /// Main:
        ///
        ///     Velocity = 50
        ///     Velocity += 5 each classic frame
        ///
        /// Se conserva la curva clásica y solo se convierte
        /// la distancia final a la escala espacial del port.
        /// </summary>
        private static float CalculateSpiritDistance(
            float frame)
        {
            frame =
                MathHelper.Clamp(
                    frame,
                    0f,
                    20f);

            float accelerationFrames =
                MathF.Max(
                    frame -
                    1f,
                    0f);

            float distance =
                50f *
                    frame +
                2.5f *
                    frame *
                    accelerationFrames;

            return
                distance *
                SpiritMotionScale;
        }

        /// <summary>
        /// Angle.X = -10
        /// Angle.Y = 0
        /// Angle.Z = yaw
        ///
        /// Main mueve un vector local:
        ///
        ///     (0, -Velocity, 0)
        ///
        /// después de AngleMatrix.
        /// </summary>
        private static Vector3 CalculateSpiritDirection(
            float yaw)
        {
            float pitch =
                MathHelper.ToRadians(
                    10f);

            float horizontal =
                MathF.Cos(
                    pitch);

            return new Vector3(
                MathF.Sin(yaw) *
                    horizontal,

                -MathF.Cos(yaw) *
                    horizontal,

                MathF.Sin(
                    pitch));
        }

        /// <summary>
        /// Main mantiene Light=(0.5,0.5,0.5) hasta
        /// que LifeTime baja de 10.
        ///
        /// Desde ahí multiplica por 1/1.2 cada frame.
        /// </summary>
        private static float CalculateSpiritLuminosity(
            float elapsedFrame)
        {
            if (elapsedFrame <=
                10f)
            {
                return 1f;
            }

            return
                MathF.Pow(
                    1f / 1.2f,
                    elapsedFrame -
                    10f);
        }

        // ============================================================
        // FLARE GEOMETRY
        // ============================================================

        private void BuildFlareGeometry()
        {
            Vector3 center =
                GetClassicSpiritCenter();

            float currentFrame =
                _time *
                ClassicReferenceFps -
                FlareSpawnDelayFrames;

            Color invisible =
                new Color(
                    0,
                    0,
                    0,
                    0);

            for (int flareIndex = 0;
                 flareIndex < FlareCount;
                 flareIndex++)
            {
                FlareState flare =
                    _flares[
                        flareIndex];

                int vertexStart =
                    flareIndex *
                    FlareVerticesPerJoint;

                for (int state = 0;
                     state < FlareTailStateCount;
                     state++)
                {
                    float historyFrame =
                        currentFrame -
                        state;

                    bool valid =
                        historyFrame >=
                            0f &&
                        historyFrame <=
                            flare.InitialLife;

                    float safeFrame =
                        MathF.Max(
                            historyFrame,
                            0f);

                    Vector3 point =
                        CalculateFlarePosition(
                            center,
                            flare,
                            safeFrame);

                    float life =
                        flare.InitialLife -
                        safeFrame;

                    float fade =
                        valid
                            ? CalculateFlareFade(
                                life)
                            : 0f;

                    Color color =
                            valid
                                ? ToColor(
                                    WarmOuterFlareLight *
                                    fade *
                                    0.90f)
                                : invisible;

                    float halfWidth =
                        FlareWidth *
                        0.5f;

                    Vector3 xOffset =
                        Vector3.UnitX *
                        halfWidth;

                    Vector3 yOffset =
                        Vector3.UnitY *
                        halfWidth;

                    float u =
                        1f -
                        state /
                        (float)
                        (
                            FlareTailStateCount -
                            1
                        );

                    int v =
                        vertexStart +
                        state *
                        FlareVerticesPerState;

                    _flareVertices[v + 0] =
                        new VertexPositionColorTexture(
                            point -
                            xOffset,
                            color,
                            new Vector2(
                                u,
                                0f));

                    _flareVertices[v + 1] =
                        new VertexPositionColorTexture(
                            point +
                            xOffset,
                            color,
                            new Vector2(
                                u,
                                1f));

                    _flareVertices[v + 2] =
                        new VertexPositionColorTexture(
                            point -
                            yOffset,
                            color,
                            new Vector2(
                                u,
                                0f));

                    _flareVertices[v + 3] =
                        new VertexPositionColorTexture(
                            point +
                            yOffset,
                            color,
                            new Vector2(
                                u,
                                1f));
                }
            }
        }

        private static Vector3 CalculateFlarePosition(
            Vector3 center,
            FlareState flare,
            float elapsedFrame)
        {
            Vector3 position =
                center +
                new Vector3(
                    flare.Offset.X,
                    flare.Offset.Y,
                    -FlareStartBelowCenter);

            //
            // Main:
            //
            // LifeTime inicial = 25..74.
            //
            // Solo empieza a subir cuando LifeTime <= 25.
            //
            float waitingFrames =
                flare.InitialLife -
                25f;

            float movingFrames =
                elapsedFrame -
                waitingFrames;

            movingFrames =
                MathHelper.Clamp(
                    movingFrames,
                    0f,
                    25f);

            if (movingFrames <=
                0f)
            {
                return position;
            }

            //
            // Main:
            //
            // Direction.Z += 5
            // Position.Z += Direction.Z
            //
            float verticalDistance =
                flare.InitialVelocity *
                    movingFrames +
                5f *
                    movingFrames *
                    (
                        movingFrames +
                        1f
                    ) *
                    0.5f;

            position.Z +=
                verticalDistance *
                FlareMotionScale;

            return position;
        }

        private static float CalculateFlareFade(
            float life)
        {
            if (life <=
                0f)
            {
                return 0f;
            }

            //
            // Conservamos la caída final que ya nos estaba
            // funcionando visualmente con los pilares.
            //
            if (life <
                5f)
            {
                return
                    MathF.Pow(
                        1f / 1.3f,
                        5f -
                        life);
            }

            return 1f;
        }

        // ============================================================
        // INDEX BUFFERS
        // ============================================================

        private void BuildSpiritIndices()
        {
            int output =
                0;

            for (int joint = 0;
                 joint < SpiritCount;
                 joint++)
            {
                int start =
                    joint *
                    SpiritVerticesPerJoint;

                for (int segment = 0;
                     segment < SpiritSegmentCount;
                     segment++)
                {
                    int current =
                        start +
                        segment *
                        SpiritVerticesPerState;

                    int next =
                        current +
                        SpiritVerticesPerState;

                    _spiritIndices[output++] =
                        (short)(current + 0);

                    _spiritIndices[output++] =
                        (short)(current + 1);

                    _spiritIndices[output++] =
                        (short)(next + 1);

                    _spiritIndices[output++] =
                        (short)(current + 0);

                    _spiritIndices[output++] =
                        (short)(next + 1);

                    _spiritIndices[output++] =
                        (short)(next + 0);
                }
            }
        }

        private void BuildFlareIndices()
        {
            int output =
                0;

            for (int joint = 0;
                 joint < FlareCount;
                 joint++)
            {
                int start =
                    joint *
                    FlareVerticesPerJoint;

                for (int segment = 0;
                     segment < FlareSegmentCount;
                     segment++)
                {
                    int current =
                        start +
                        segment *
                        FlareVerticesPerState;

                    int next =
                        current +
                        FlareVerticesPerState;

                    //
                    // FACE ONE.
                    //
                    _flareIndices[output++] =
                        (short)(current + 0);

                    _flareIndices[output++] =
                        (short)(current + 1);

                    _flareIndices[output++] =
                        (short)(next + 1);

                    _flareIndices[output++] =
                        (short)(current + 0);

                    _flareIndices[output++] =
                        (short)(next + 1);

                    _flareIndices[output++] =
                        (short)(next + 0);

                    //
                    // FACE TWO.
                    //
                    _flareIndices[output++] =
                        (short)(current + 2);

                    _flareIndices[output++] =
                        (short)(current + 3);

                    _flareIndices[output++] =
                        (short)(next + 3);

                    _flareIndices[output++] =
                        (short)(current + 2);

                    _flareIndices[output++] =
                        (short)(next + 3);

                    _flareIndices[output++] =
                        (short)(next + 2);
                }
            }
        }

        // ============================================================
        // DRAW
        // ============================================================

        public override void Draw(
            GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!Visible)
            {
                return;
            }

            //
            // No hay "fake overbright" ni flares gigantes inventados.
            //
            // La explosión sale de:
            //
            // 36 JointSpirit
            // + 36 BITMAP_LIGHT
            // + Flare subtype 2
            // + 2 Magic_Ground2
            //
            if (_time <=
                SpiritDuration)
            {
                DrawSpiritRibbons();

                DrawSpiritHeadLights();
            }

            DrawFlareRibbons();

            DrawMagicGroundEffects();
        }

        // ============================================================
        // SPIRIT RIBBON
        // ============================================================

        private void DrawSpiritRibbons()
        {
            if (_spiritTexture == null)
            {
                return;
            }

            DrawRibbonGeometry(
                _spiritTexture,
                _spiritVertices,
                SpiritTotalVertices,
                _spiritIndices,
                SpiritTotalIndices);
        }

        // ============================================================
        // FLARE RIBBON
        // ============================================================

        private void DrawFlareRibbons()
        {
            if (_flareTexture == null)
            {
                return;
            }

            DrawRibbonGeometry(
                _flareTexture,
                _flareVertices,
                FlareTotalVertices,
                _flareIndices,
                FlareTotalIndices);
        }

        private void DrawRibbonGeometry(
            Texture2D texture,
            VertexPositionColorTexture[] vertices,
            int vertexCount,
            short[] indices,
            int indexCount)
        {
            if (_jointEffect == null)
            {
                return;
            }

            GraphicsDevice gd =
                GraphicsDevice;

            BlendState oldBlend =
                gd.BlendState;

            DepthStencilState oldDepth =
                gd.DepthStencilState;

            RasterizerState oldRasterizer =
                gd.RasterizerState;

            try
            {
                gd.BlendState =
                    ClassicGlowBlend;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                _jointEffect.World =
                    Matrix.Identity;

                _jointEffect.View =
                    Camera.Instance.View;

                _jointEffect.Projection =
                    Camera.Instance.Projection;

                _jointEffect.Texture =
                    texture;

                _jointEffect.Alpha =
                    TotalAlpha;

                foreach (
                    EffectPass pass
                    in _jointEffect
                        .CurrentTechnique
                        .Passes)
                {
                    pass.Apply();

                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        vertices,
                        0,
                        vertexCount,
                        indices,
                        0,
                        indexCount / 3);
                }
            }
            finally
            {
                gd.BlendState =
                    oldBlend;

                gd.DepthStencilState =
                    oldDepth;

                gd.RasterizerState =
                    oldRasterizer;
            }
        }

        // ============================================================
        // BITMAP_LIGHT AT SPIRIT HEAD
        // ============================================================

        private void DrawSpiritHeadLights()
        {
            if (_spriteBatch == null ||
                _lightTexture == null)
            {
                return;
            }

            float frame =
                MathHelper.Clamp(
                    _time *
                    ClassicReferenceFps *
                    SpiritPlaybackRate,
                    0f,
                    20f);

            float lifeTime =
                20f -
                frame;

            //
            // Main:
            //
            // Scale =
            // 4 + (20 - LifeTime) / 5
            //
            float classicScale =
                4f +
                (
                    20f -
                    lifeTime
                ) /
                5f;

            //
            // Este fue uno de los errores principales de la
            // implementación anterior.
            //
            // flare01 en TU Data_Broyal es 64x64.
            //
            // CreateSprite de Main hace:
            //
            //     Width  = texture.Width  * Scale
            //     Height = texture.Height * Scale
            //
            // Por lo tanto el BITMAP_LIGHT original mide:
            //
            //     64 * 4 = 256
            // hasta
            //     64 * 8 = 512
            //
            // unidades de mundo.
            //
            // Eso hace que las 36 luces se SUPERPONGAN y formen
            // un halo continuo de fuego.
            //
            // No son 36 pelotitas pequeñas.
            //
            float worldSize =
                _lightTexture.Width *
                classicScale;

            Vector3 light;

            if (lifeTime >=
                10f)
            {
                light =
                    WarmSpiritHeadLight;
            }
            else
            {
                float fade =
                    CalculateSpiritLuminosity(
                        frame);

                light =
                    WarmSpiritHeadLight *
                    fade;
            }
            
            using (
                new SpriteBatchScope(
                    _spriteBatch,
                    SpriteSortMode.Deferred,
                    ClassicGlowBlend,
                    SamplerState.LinearClamp,
                    GraphicsManager.ReadOnlyDepth,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                     i < SpiritCount;
                     i++)
                {
                    DrawWorldSprite(
                        _spriteBatch,
                        _lightTexture,
                        _spiritHeads[i],
                        light,
                        worldSize,
                        _spiritHeadRotations[i]);
                }
            }
        }

        // ============================================================
        // MAGIC_GROUND2
        // ============================================================

        private void DrawMagicGroundEffects()
        {
            if (_magicTexture == null ||
                _time >=
                GroundDuration)
            {
                return;
            }

            float frame =
                MathHelper.Clamp(
                    _time *
                    ClassicReferenceFps,
                    0f,
                    40f);

            float lifeTime =
                40f -
                frame;

            float luminosity =
                1f;

            if (lifeTime <
                5f)
            {
                luminosity -=
                    (
                        5f -
                        lifeTime
                    ) *
                    0.2f;
            }
            else
            {
                //
                // Main subtype 4:
                //
                // sin((60-LifeTime)*0.05)+0.5
                //
                luminosity =
                    MathF.Sin(
                        (
                            60f -
                            lifeTime
                        ) *
                        0.05f) +
                    0.5f;
            }

            luminosity =
                MathF.Max(
                    luminosity,
                    0f);

            Vector3 light =
                new Vector3(
                    1.0f,
                    0.50f,
                    0.10f) *
                luminosity;

            Vector3 position =
                _caster.WorldPosition
                    .Translation;

            position.Z +=
                5f;

            //
            // EXACTAMENTE DOS.
            //
            // Nada de copias extra para "reforzar" el halo.
            //
            DrawGroundQuad(
                _magicTexture,
                position,
                light,
                _groundScale1,
                GroundRotation1);

            DrawGroundQuad(
                _magicTexture,
                position,
                light,
                _groundScale2,
                GroundRotation2);
        }

        // ============================================================
        // WORLD SPRITE
        // ============================================================

        private void DrawWorldSprite(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector3 worldPosition,
            Vector3 light,
            float worldSize,
            float rotation)
        {
            Matrix view =
                Camera.Instance.View;

            Matrix projection =
                Camera.Instance.Projection;

            Vector3 cameraPosition =
                Vector3.Transform(
                    worldPosition,
                    view);

            float cameraDepth =
                -cameraPosition.Z;

            if (cameraDepth <=
                Camera.Instance.ViewNear)
            {
                return;
            }

            Viewport viewport =
                GraphicsDevice.Viewport;

            Vector3 projected =
                viewport.Project(
                    worldPosition,
                    projection,
                    view,
                    Matrix.Identity);

            if (projected.Z <
                    0f ||
                projected.Z >
                    1f)
            {
                return;
            }

            float pixelsPerWorldUnit =
                viewport.Height *
                MathF.Abs(
                    projection.M22) /
                (
                    2f *
                    cameraDepth
                );

            float pixelSize =
                worldSize *
                pixelsPerWorldUnit;

            float spriteScale =
                pixelSize /
                MathF.Max(
                    texture.Width,
                    1);

            if (!float.IsFinite(
                    spriteScale) ||
                spriteScale <=
                    0f)
            {
                return;
            }

            Vector2 origin =
                new Vector2(
                    texture.Width *
                        0.5f,
                    texture.Height *
                        0.5f);

            //
            // Glow ONE+ONE.
            //
            // El color contiene la energía; no necesitamos
            // convertirlo en humo mediante SourceAlpha.
            //
            Color color =
                ToColor(
                    light *
                    TotalAlpha);

            spriteBatch.Draw(
                texture,
                new Vector2(
                    projected.X,
                    projected.Y),
                null,
                color,
                rotation,
                origin,
                spriteScale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    projected.Z,
                    0f,
                    1f));
        }

        // ============================================================
        // TERRAIN MAGIC QUAD
        // ============================================================

        private void DrawGroundQuad(
            Texture2D texture,
            Vector3 position,
            Vector3 light,
            float scale,
            float rotation)
        {
            GraphicsDevice gd =
                GraphicsManager.Instance
                    .GraphicsDevice;

            AlphaTestEffect effect =
                GraphicsManager.Instance
                    .AlphaTestEffect3D;

            BlendState oldBlend =
                gd.BlendState;

            DepthStencilState oldDepth =
                gd.DepthStencilState;

            RasterizerState oldRasterizer =
                gd.RasterizerState;

            Texture2D oldTexture =
                effect.Texture;

            Vector3 oldDiffuse =
                effect.DiffuseColor;

            float oldAlpha =
                effect.Alpha;

            bool oldVertexColor =
                effect.VertexColorEnabled;

            try
            {
                //
                // CRÍTICO:
                //
                // Antes:
                //
                //     scale * 82
                //
                // Ahora:
                //
                //     scale * 50
                //
                // porque el quad local ya mide 2 unidades
                // de extremo a extremo.
                //
                effect.World =
                    Matrix.CreateScale(
                        scale *
                        GroundHalfTerrainScale) *
                    Matrix.CreateRotationX(
                        -MathHelper.PiOver2) *
                    Matrix.CreateRotationZ(
                        rotation) *
                    Matrix.CreateTranslation(
                        position);

                effect.View =
                    Camera.Instance.View;

                effect.Projection =
                    Camera.Instance.Projection;

                effect.Texture =
                    texture;

                effect.VertexColorEnabled =
                    false;

                //
                // No clampamos artificialmente luminosity.
                // Main permite que subtype 4 alcance > 1.
                //
                effect.DiffuseColor =
                    light *
                    TotalAlpha;

                effect.Alpha =
                    1.0f;

                gd.BlendState =
                    ClassicGlowBlend;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.RasterizerState =
                    RasterizerState.CullNone;

                VertexPositionTexture[] vertices =
                {
                    new(
                        new Vector3(
                            -1f,
                            0f,
                            -1f),
                        new Vector2(
                            0f,
                            0f)),

                    new(
                        new Vector3(
                            1f,
                            0f,
                            -1f),
                        new Vector2(
                            1f,
                            0f)),

                    new(
                        new Vector3(
                            -1f,
                            0f,
                            1f),
                        new Vector2(
                            0f,
                            1f)),

                    new(
                        new Vector3(
                            1f,
                            0f,
                            1f),
                        new Vector2(
                            1f,
                            1f))
                };

                short[] indices =
                {
                    0, 1, 2,
                    2, 1, 3
                };

                foreach (
                    EffectPass pass
                    in effect
                        .CurrentTechnique
                        .Passes)
                {
                    pass.Apply();

                    gd.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        vertices,
                        0,
                        4,
                        indices,
                        0,
                        2);
                }
            }
            finally
            {
                effect.Texture =
                    oldTexture;

                effect.DiffuseColor =
                    oldDiffuse;

                effect.Alpha =
                    oldAlpha;

                effect.VertexColorEnabled =
                    oldVertexColor;

                gd.BlendState =
                    oldBlend;

                gd.DepthStencilState =
                    oldDepth;

                gd.RasterizerState =
                    oldRasterizer;
            }
        }

        // ============================================================
        // UTILS
        // ============================================================

        private Vector3 GetClassicSpiritCenter()
        {
            Vector3 center =
                _caster.WorldPosition
                    .Translation;

            //
            // Main:
            //
            // Position.Z += 100
            //
            center.Z +=
                100f;

            return center;
        }

        private static Color ToColor(
            Vector3 light)
        {
            return new Color(
                MathHelper.Clamp(
                    light.X,
                    0f,
                    1f),

                MathHelper.Clamp(
                    light.Y,
                    0f,
                    1f),

                MathHelper.Clamp(
                    light.Z,
                    0f,
                    1f),

                1f);
        }

        // ============================================================
        // REMOVE / DISPOSE
        // ============================================================

        private void RemoveSelf()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(
                    this);

                Dispose();

                return;
            }

            if (World != null)
            {
                World.Objects.Remove(
                    this);

                Dispose();

                return;
            }

            Dispose();
        }

        public override void Dispose()
        {
            _jointEffect?.Dispose();

            _jointEffect =
                null;

            //
            // TextureLoader mantiene ownership/cache.
            //
            _spiritTexture =
                null;

            _magicTexture =
                null;

            _lightTexture =
                null;

            _flareTexture =
                null;

            base.Dispose();
        }
    }
}
