using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Main.Objects.Wings
{
    /// <summary>
    /// Classic MU Cape of Emperor cloth simulation.
    ///
    /// The original MODEL_CAPE_OF_EMPEROR does NOT deform
    /// DarkLordRobe02.bmd.
    ///
    /// Instead it creates three independent CPhysicsCloth pieces:
    ///
    /// 1) Main cape:
    ///
    ///    bone      = 19
    ///    offset    = (0, 8, 10)
    ///    grid      = 10 x 10
    ///    size      = 180 x 180
    ///    texture   = dl_redwings02
    ///    style     = CURVED | SHORT_SHOULDER | HEAVY | ALPHA
    ///
    /// 2) Right ribbon:
    ///
    ///    bone      = 19
    ///    offset    = (30, 15, 10)
    ///    grid      = 2 x 5
    ///    size      = 12 x 200
    ///    texture   = dl_redwings03
    ///    style     = FLAT | COTTON | ALPHA
    ///
    /// 3) Left ribbon:
    ///
    ///    bone      = 19
    ///    offset    = (-30, 20, 10)
    ///    grid      = 2 x 5
    ///    size      = 12 x 200
    ///    texture   = dl_redwings03
    ///
    /// This ports only the subset of CPhysicsCloth needed
    /// by Cape of Emperor.
    /// </summary>
    internal sealed class EmperorCapeCloth : EffectObject
    {
        // ============================================================
        // CLASSIC ASSETS
        // ============================================================

        private const string MainCapeTexturePath =
            "Item/dl_redwings02.OZT";

        private const string RibbonTexturePath =
            "Item/dl_redwings03.OZT";


        // ============================================================
        // CLASSIC PHYSICS CONSTANTS
        // ============================================================

        //
        // MU PhysicsManager.cpp:
        //
        // RATE_SHORT_SHOULDER = 0.6f
        //
        private const float ShortShoulderRate =
            0.6f;


        //
        // Original CPhysicsVertex:
        //
        // s_Gravity    = 9.8
        // s_fMass      = 0.0025
        // s_fInvOfMass = 400
        //
        private const float Gravity =
            9.8f;

        private const float VertexMass =
            0.0025f;

        private const float InverseVertexMass =
            400.0f;


        //
        // Classic MU reference rate.
        //
        // The random wind changes at this cadence,
        // but the actual cloth physics now runs every frame.
        //
        private const float ClassicFrameTime =
            1.0f / 25.0f;


        //
        // Original:
        //
        // Move2(0.005f, 5)
        //
        private const int ClassicSubstepCount =
            5;


        //
        // Original simulation advances:
        //
        // 0.005 * 5 = 0.025 sec
        //
        // for each 0.040 sec reference MU frame.
        //
        // 0.025 / 0.040 = 0.625
        //
        private const float ClassicSimulationTimeRatio =
            0.625f;


        // ============================================================
        // OWNER
        // ============================================================

        private readonly WingObject _owner;


        // ============================================================
        // TEXTURES
        // ============================================================

        private Texture2D _mainCapeTexture;

        private Texture2D _ribbonTexture;


        // ============================================================
        // CLOTH PIECES
        // ============================================================

        private readonly ClothPiece _mainCape;

        private readonly ClothPiece _rightRibbon;

        private readonly ClothPiece _leftRibbon;


        // ============================================================
        // SIMULATION STATE
        // ============================================================

        private bool _simulationInitialized;

        private bool _wasActive;

        private float _windAccumulator;

        private float _windStrength;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public EmperorCapeCloth(
            WingObject owner)
        {
            _owner =
                owner ??
                throw new ArgumentNullException(
                    nameof(owner));


            IsTransparent = true;

            AffectedByTransparency = true;

            BlendState =
                BlendState.AlphaBlend;

            DepthState =
                DepthStencilState.Default;


            //
            // Cloth geometry is generated directly in world space.
            //
            BoundingBoxLocal =
                new BoundingBox(
                    Vector3.Zero,
                    Vector3.Zero);


            // ========================================================
            // MAIN CAPE
            // ========================================================

            _mainCape =
                new ClothPiece(
                    bone: 19,

                    offset:
                        new Vector3(
                            0.0f,
                            8.0f,
                            10.0f),

                    columns: 10,
                    rows: 10,

                    width: 180.0f,
                    height: 180.0f,

                    curved: true,
                    shortShoulder: true,
                    heavy: true,

                    collisions:
                        new[]
                        {
                            new CollisionSphereDefinition(
                                bone: 17,
                                localCenter:
                                    new Vector3(
                                        -10.0f,
                                        -10.0f,
                                        -10.0f),
                                radius: 25.0f),

                            new CollisionSphereDefinition(
                                bone: 17,
                                localCenter:
                                    new Vector3(
                                        10.0f,
                                        -10.0f,
                                        -10.0f),
                                radius: 25.0f),

                            new CollisionSphereDefinition(
                                bone: 17,
                                localCenter:
                                    new Vector3(
                                        -10.0f,
                                        -10.0f,
                                        20.0f),
                                radius: 27.0f),

                            new CollisionSphereDefinition(
                                bone: 17,
                                localCenter:
                                    new Vector3(
                                        10.0f,
                                        -10.0f,
                                        20.0f),
                                radius: 27.0f)
                        });


            // ========================================================
            // RIGHT RIBBON
            // ========================================================

            _rightRibbon =
                new ClothPiece(
                    bone: 19,

                    offset:
                        new Vector3(
                            30.0f,
                            15.0f,
                            10.0f),

                    columns: 2,
                    rows: 5,

                    width: 12.0f,
                    height: 200.0f,

                    curved: false,
                    shortShoulder: false,
                    heavy: false,

                    collisions:
                        CreateRibbonCollisions());


            // ========================================================
            // LEFT RIBBON
            // ========================================================

            _leftRibbon =
                new ClothPiece(
                    bone: 19,

                    offset:
                        new Vector3(
                            -30.0f,
                            20.0f,
                            10.0f),

                    columns: 2,
                    rows: 5,

                    width: 12.0f,
                    height: 200.0f,

                    curved: false,
                    shortShoulder: false,
                    heavy: false,

                    collisions:
                        CreateRibbonCollisions());
        }


        private static CollisionSphereDefinition[]
            CreateRibbonCollisions()
        {
            return
                new[]
                {
                    new CollisionSphereDefinition(
                        bone: 2,
                        localCenter:
                            new Vector3(
                                0.0f,
                                -15.0f,
                                -20.0f),
                        radius: 30.0f),

                    new CollisionSphereDefinition(
                        bone: 17,
                        localCenter:
                            Vector3.Zero,
                        radius: 35.0f)
                };
        }


        // ============================================================
        // LOAD
        // ============================================================

        public override async Task LoadContent()
        {
            await Task.WhenAll(
                TextureLoader.Instance.Prepare(
                    MainCapeTexturePath),

                TextureLoader.Instance.Prepare(
                    RibbonTexturePath));


            _mainCapeTexture =
                TextureLoader.Instance.GetTexture2D(
                    MainCapeTexturePath);


            _ribbonTexture =
                TextureLoader.Instance.GetTexture2D(
                    RibbonTexturePath);


            _mainCape.Texture =
                _mainCapeTexture;


            _rightRibbon.Texture =
                _ribbonTexture;


            _leftRibbon.Texture =
                _ribbonTexture;


            await base.LoadContent();
        }


        // ============================================================
        // UPDATE
        // ============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);


            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }


            bool active =
                TryGetActivePlayer(
                    out ModelObject player);


            if (!active)
            {
                Hidden = true;


                if (_wasActive)
                {
                    ResetSimulation();
                }


                _wasActive = false;

                return;
            }


            Hidden = false;

            _wasActive = true;


            // ========================================================
            // INITIALIZE
            // ========================================================

            if (!_simulationInitialized)
            {
                if (!InitializeSimulation(
                        player))
                {
                    Hidden = true;

                    return;
                }
            }


            // ========================================================
            // PIN TOP ROW TO CURRENT PLAYER BONE
            // ========================================================

            //
            // This happens every rendered frame.
            //
            // That keeps the top of the cape synchronized with bone 19
            // instead of following it only at the classic 25 FPS rate.
            //
            _mainCape.SyncPinnedVertices(
                player);

            _rightRibbon.SyncPinnedVertices(
                player);

            _leftRibbon.SyncPinnedVertices(
                player);


            float dt =
                (float)gameTime
                    .ElapsedGameTime
                    .TotalSeconds;


            if (!float.IsFinite(dt) ||
                dt <= 0.0f)
            {
                return;
            }


            //
            // Prevent a debugger pause / loading stall from creating
            // a gigantic physics step.
            //
            dt =
                MathF.Min(
                    dt,
                    0.12f);


            // ========================================================
            // CLASSIC WIND
            //
            // Wind randomization stays at the original 25 FPS cadence.
            // ========================================================

            _windAccumulator +=
                dt;


            while (_windAccumulator >=
                   ClassicFrameTime)
            {
                UpdateClassicWind(
                    player);


                _windAccumulator -=
                    ClassicFrameTime;
            }


            Vector3 wind =
                BuildWindVector(
                    player);


            float worldTimeSeconds =
                (float)gameTime
                    .TotalGameTime
                    .TotalSeconds;


            // ========================================================
            // SMOOTH CLASSIC PHYSICS
            //
            // Original:
            //
            // Move2(0.005f, 5)
            //
            // = 0.025 sec simulation per 0.040 sec MU frame.
            //
            // Ratio = 0.625
            //
            // Instead of waiting for a 25 FPS tick, distribute the
            // same simulation speed over every actual frame.
            // ========================================================

            float simulatedTime =
                dt *
                ClassicSimulationTimeRatio;


            float substepTime =
                simulatedTime /
                ClassicSubstepCount;


            bool mainOk =
                _mainCape.StepClassic(
                    player,
                    wind,
                    worldTimeSeconds,
                    substepTime);


            bool rightOk =
                _rightRibbon.StepClassic(
                    player,
                    wind,
                    worldTimeSeconds,
                    substepTime);


            bool leftOk =
                _leftRibbon.StepClassic(
                    player,
                    wind,
                    worldTimeSeconds,
                    substepTime);


            if (!mainOk ||
                !rightOk ||
                !leftOk)
            {
                ResetSimulation();


                if (!InitializeSimulation(
                        player))
                {
                    Hidden = true;
                }
            }
        }


        private bool TryGetActivePlayer(
            out ModelObject player)
        {
            player = null;


            if (!_owner.IsEmperorCapeClothActive ||
                _owner.Hidden)
            {
                return false;
            }


            if (_owner.Parent is not
                ModelObject parentModel)
            {
                return false;
            }


            Matrix[] bones =
                parentModel.GetBoneTransforms();


            if (bones == null ||
                bones.Length <= 19)
            {
                return false;
            }


            player =
                parentModel;


            return true;
        }


        private bool InitializeSimulation(
            ModelObject player)
        {
            bool mainOk =
                _mainCape.Initialize(
                    player);


            bool rightOk =
                _rightRibbon.Initialize(
                    player);


            bool leftOk =
                _leftRibbon.Initialize(
                    player);


            _simulationInitialized =
                mainOk &&
                rightOk &&
                leftOk;


            _windAccumulator =
                0.0f;


            return
                _simulationInitialized;
        }


        private void ResetSimulation()
        {
            _simulationInitialized =
                false;


            _windAccumulator =
                0.0f;


            _windStrength =
                0.0f;


            _mainCape.Reset();

            _rightRibbon.Reset();

            _leftRibbon.Reset();
        }


        // ============================================================
        // CLASSIC WIND
        // ============================================================

        private void UpdateClassicWind(
            ModelObject player)
        {
            //
            // Original:
            //
            // float fPlus =
            //     ((rand() % 200) - 100) * 0.001f;
            //
            // s_fWind += fPlus;
            // clamp(-0.2, 1.0)
            //
            float randomDelta =
                (MuGame.Random.Next(
                    0,
                    200) -
                 100)
                *
                0.001f;


            _windStrength +=
                randomDelta;


            _windStrength =
                MathHelper.Clamp(
                    _windStrength,
                    -0.2f,
                    1.0f);
        }


        private Vector3 BuildWindVector(
            ModelObject player)
        {
            //
            // Original:
            //
            // wind.x =
            //   windStrength *
            //   sin((180 + AngleZ) degrees)
            //
            // wind.y =
            //  -windStrength *
            //   cos((180 + AngleZ) degrees)
            //
            float angle =
                player.TotalAngle.Z;


            return
                new Vector3(
                    -MathF.Sin(angle) *
                        _windStrength,

                    MathF.Cos(angle) *
                        _windStrength,

                    0.0f);
        }


        // ============================================================
        // DRAW
        // ============================================================

        public override void Draw(
            GameTime gameTime)
        {
            if (!Visible ||
                !_simulationInitialized)
            {
                base.Draw(
                    gameTime);

                return;
            }


            if (_mainCapeTexture == null ||
                _mainCapeTexture.IsDisposed ||
                _ribbonTexture == null ||
                _ribbonTexture.IsDisposed)
            {
                base.Draw(
                    gameTime);

                return;
            }


            GraphicsDevice device =
                GraphicsManager
                    .Instance
                    .GraphicsDevice;


            AlphaTestEffect effect =
                GraphicsManager
                    .Instance
                    .AlphaTestEffect3D;


            if (device == null ||
                effect == null)
            {
                base.Draw(
                    gameTime);

                return;
            }


            // ========================================================
            // SAVE GRAPHICS STATE
            // ========================================================

            BlendState oldBlend =
                device.BlendState;


            DepthStencilState oldDepth =
                device.DepthStencilState;


            RasterizerState oldRasterizer =
                device.RasterizerState;


            SamplerState oldSampler =
                device.SamplerStates[0];


            Texture2D oldTexture =
                effect.Texture;


            Matrix oldWorld =
                effect.World;


            Matrix oldView =
                effect.View;


            Matrix oldProjection =
                effect.Projection;


            Vector3 oldDiffuse =
                effect.DiffuseColor;


            float oldAlpha =
                effect.Alpha;


            bool oldVertexColor =
                effect.VertexColorEnabled;


            int oldReferenceAlpha =
                effect.ReferenceAlpha;


            CompareFunction oldAlphaFunction =
                effect.AlphaFunction;


            try
            {
                // ====================================================
                // CLASSIC PCT_MASK_ALPHA
                // ====================================================

                device.BlendState =
                    BlendState.AlphaBlend;


                device.DepthStencilState =
                    DepthStencilState.Default;


                //
                // Classic EnableAlphaTest disables culling.
                //
                device.RasterizerState =
                    RasterizerState.CullNone;


                device.SamplerStates[0] =
                    SamplerState.LinearClamp;


                //
                // Our cloth vertices are already in WORLD coordinates.
                //
                effect.World =
                    Matrix.Identity;


                effect.View =
                    Camera.Instance.View;


                effect.Projection =
                    Camera.Instance.Projection;


                effect.VertexColorEnabled =
                    true;


                effect.DiffuseColor =
                    Vector3.One;


                effect.Alpha =
                    _owner.TotalAlpha;


                effect.AlphaFunction =
                    CompareFunction.Greater;


                effect.ReferenceAlpha =
                    2;


                DrawPiece(
                    device,
                    effect,
                    _mainCape);


                DrawPiece(
                    device,
                    effect,
                    _rightRibbon);


                DrawPiece(
                    device,
                    effect,
                    _leftRibbon);
            }
            finally
            {
                // ====================================================
                // RESTORE EFFECT
                // ====================================================

                effect.Texture =
                    oldTexture;


                effect.World =
                    oldWorld;


                effect.View =
                    oldView;


                effect.Projection =
                    oldProjection;


                effect.DiffuseColor =
                    oldDiffuse;


                effect.Alpha =
                    oldAlpha;


                effect.VertexColorEnabled =
                    oldVertexColor;


                effect.ReferenceAlpha =
                    oldReferenceAlpha;


                effect.AlphaFunction =
                    oldAlphaFunction;


                // ====================================================
                // RESTORE DEVICE
                // ====================================================

                device.BlendState =
                    oldBlend;


                device.DepthStencilState =
                    oldDepth;


                device.RasterizerState =
                    oldRasterizer;


                device.SamplerStates[0] =
                    oldSampler;
            }


            base.Draw(
                gameTime);
        }


        private static void DrawPiece(
            GraphicsDevice device,
            AlphaTestEffect effect,
            ClothPiece piece)
        {
            if (!piece.Initialized ||
                piece.Texture == null ||
                piece.Texture.IsDisposed)
            {
                return;
            }


            piece.BuildRenderVertices();


            effect.Texture =
                piece.Texture;


            foreach (
                EffectPass pass
                in effect
                    .CurrentTechnique
                    .Passes)
            {
                pass.Apply();


                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,

                    piece.RenderVertices,
                    0,
                    piece.RenderVertices.Length,

                    piece.RenderIndices,
                    0,
                    piece.PrimitiveCount);
            }
        }


        // ============================================================
        // CLASSIC AXIS CONVERSION
        // ============================================================

        /// <summary>
        /// Original CPhysicsCloth conversion:
        ///
        /// newX = oldZ
        /// newY = -oldY
        /// newZ = oldX
        /// </summary>
        private static Vector3 ClassicAttachmentLocalToClient(
            Vector3 value)
        {
            return
                new Vector3(
                    value.Z,
                    -value.Y,
                    value.X);
        }


        private static bool TryGetBoneWorldMatrix(
            ModelObject player,
            int bone,
            out Matrix result)
        {
            result =
                Matrix.Identity;


            if (player == null)
            {
                return false;
            }


            Matrix[] bones =
                player.GetBoneTransforms();


            if (bones == null ||
                bone < 0 ||
                bone >= bones.Length)
            {
                return false;
            }


            result =
                bones[bone] *
                player.WorldPosition;


            return true;
        }


        // ============================================================
        // COLLISION DEFINITION
        // ============================================================

        private readonly struct CollisionSphereDefinition
        {
            public readonly int Bone;

            public readonly Vector3 LocalCenter;

            public readonly float Radius;


            public CollisionSphereDefinition(
                int bone,
                Vector3 localCenter,
                float radius)
            {
                Bone =
                    bone;

                LocalCenter =
                    localCenter;

                Radius =
                    radius;
            }
        }


        // ============================================================
        // LINK STYLE
        // ============================================================

        [Flags]
        private enum LinkStyle : byte
        {
            None =
                0,

            LooseDistance =
                1 << 0,

            Spring =
                1 << 1,

            StrictDistance =
                1 << 2
        }


        // ============================================================
        // PHYSICS LINK
        // ============================================================

        private readonly struct ClothLink
        {
            public readonly int Vertex1;

            public readonly int Vertex2;

            public readonly float MinDistance;

            public readonly float MaxDistance;

            public readonly LinkStyle Style;


            public ClothLink(
                int vertex1,
                int vertex2,
                float minDistance,
                float maxDistance,
                LinkStyle style)
            {
                Vertex1 =
                    vertex1;

                Vertex2 =
                    vertex2;

                MinDistance =
                    minDistance;

                MaxDistance =
                    maxDistance;

                Style =
                    style;
            }
        }


        // ============================================================
        // PHYSICS VERTEX
        // ============================================================

        private struct ClothVertex
        {
            public Vector3 Position;

            public Vector3 Velocity;

            public Vector3 Force;

            public bool Fixed;


            public Vector3 PendingMove;

            public int PendingMoveCount;


            public void Reset(
                Vector3 position,
                bool fixedPosition)
            {
                Position =
                    position;

                Velocity =
                    Vector3.Zero;

                Force =
                    Vector3.Zero;

                Fixed =
                    fixedPosition;

                PendingMove =
                    Vector3.Zero;

                PendingMoveCount =
                    0;
            }


            public void SetFixedPosition(
                Vector3 position)
            {
                Position =
                    position;

                Velocity =
                    Vector3.Zero;

                Force =
                    Vector3.Zero;

                Fixed =
                    true;

                PendingMove =
                    Vector3.Zero;

                PendingMoveCount =
                    0;
            }


            public void AddPendingMove(
                Vector3 movement)
            {
                PendingMove +=
                    movement;


                PendingMoveCount++;
            }


            public void ApplyPendingMove()
            {
                if (Fixed)
                {
                    PendingMove =
                        Vector3.Zero;

                    PendingMoveCount =
                        0;

                    return;
                }


                if (PendingMoveCount > 0)
                {
                    Position +=
                        PendingMove /
                        PendingMoveCount;
                }


                PendingMove =
                    Vector3.Zero;


                PendingMoveCount =
                    0;
            }
        }


        // ============================================================
        // CLOTH PIECE
        // ============================================================

        private sealed class ClothPiece
        {
            private readonly int _bone;

            private readonly Vector3 _offset;

            private readonly int _columns;

            private readonly int _rows;

            private readonly float _width;

            private readonly float _height;

            private readonly bool _curved;

            private readonly bool _shortShoulder;

            private readonly bool _heavy;


            private readonly CollisionSphereDefinition[]
                _collisions;


            private readonly ClothVertex[]
                _vertices;


            private ClothLink[]
                _links =
                    Array.Empty<ClothLink>();


            private readonly VertexPositionColorTexture[]
                _renderVertices;


            private readonly short[]
                _renderIndices;


            public Texture2D Texture { get; set; }


            public bool Initialized { get; private set; }


            public VertexPositionColorTexture[]
                RenderVertices =>
                    _renderVertices;


            public short[]
                RenderIndices =>
                    _renderIndices;


            public int PrimitiveCount =>
                _renderIndices.Length /
                3;


            // ========================================================
            // CONSTRUCTOR
            // ========================================================

            public ClothPiece(
                int bone,
                Vector3 offset,
                int columns,
                int rows,
                float width,
                float height,
                bool curved,
                bool shortShoulder,
                bool heavy,
                CollisionSphereDefinition[] collisions)
            {
                _bone =
                    bone;

                _offset =
                    offset;

                _columns =
                    columns;

                _rows =
                    rows;

                _width =
                    width;

                _height =
                    height;

                _curved =
                    curved;

                _shortShoulder =
                    shortShoulder;

                _heavy =
                    heavy;


                _collisions =
                    collisions ??
                    Array.Empty<
                        CollisionSphereDefinition>();


                _vertices =
                    new ClothVertex[
                        columns *
                        rows];


                _renderVertices =
                    new VertexPositionColorTexture[
                        columns *
                        rows];


                _renderIndices =
                    BuildRenderIndices(
                        columns,
                        rows);
            }


            // ========================================================
            // INITIALIZATION
            // ========================================================

            public bool Initialize(
                ModelObject player)
            {
                if (!TryGetBoneWorldMatrix(
                        player,
                        _bone,
                        out Matrix boneWorld))
                {
                    Initialized =
                        false;

                    return false;
                }


                Vector3 anchor =
                    boneWorld.Translation;


                float unitHeight =
                    _height /
                    (_rows - 1);


                for (int row = 0;
                     row < _rows;
                     row++)
                {
                    float rowRatio =
                        (float)row /
                        (_rows - 1);


                    float rowWidth =
                        _width;


                    if (_shortShoulder)
                    {
                        rowWidth *=
                            ShortShoulderRate +
                            (1.0f -
                             ShortShoulderRate)
                            *
                            rowRatio;
                    }


                    float unitWidth =
                        rowWidth /
                        (_columns - 1);


                    for (int column = 0;
                         column < _columns;
                         column++)
                    {
                        int index =
                            row *
                            _columns +
                            column;


                        float x =
                            unitWidth *
                            column -
                            rowWidth *
                            0.5f;


                        float y =
                            20.0f;


                        float z =
                            -unitHeight *
                            row;


                        if (_curved)
                        {
                            float move =
                                2.0f *
                                MathF.Abs(
                                    (float)column /
                                    (_columns - 1)
                                    -
                                    0.5f);


                            y -=
                                10.0f *
                                move *
                                move;
                        }


                        Vector3 local =
                            new Vector3(
                                x,
                                y,
                                z);


                        Vector3 world =
                            anchor +
                            Vector3.TransformNormal(
                                local,
                                player.WorldPosition);


                        _vertices[index].Reset(
                            world,
                            fixedPosition:
                                false);
                    }
                }


                //
                // Original link lengths are calculated before the
                // fixed top row is repositioned.
                //
                BuildLinks();


                if (!SyncPinnedVertices(
                        player))
                {
                    Initialized =
                        false;

                    return false;
                }


                Initialized =
                    true;


                return true;
            }


            public void Reset()
            {
                Initialized =
                    false;


                _links =
                    Array.Empty<
                        ClothLink>();
            }


            // ========================================================
            // FIXED TOP ROW
            // ========================================================

            public bool SyncPinnedVertices(
                ModelObject player)
            {
                if (!TryGetBoneWorldMatrix(
                        player,
                        _bone,
                        out Matrix boneWorld))
                {
                    return false;
                }


                float topWidth =
                    _width;


                if (_shortShoulder)
                {
                    topWidth *=
                        ShortShoulderRate;
                }


                float unitWidth =
                    topWidth /
                    (_columns - 1);


                for (int column = 0;
                     column < _columns;
                     column++)
                {
                    float x =
                        unitWidth *
                        column -
                        topWidth *
                        0.5f;


                    x +=
                        _offset.X;


                    Vector3 classicLocal =
                        new Vector3(
                            x,
                            _offset.Y,
                            _offset.Z);


                    Vector3 clientLocal =
                        ClassicAttachmentLocalToClient(
                            classicLocal);


                    Vector3 worldPosition =
                        Vector3.Transform(
                            clientLocal,
                            boneWorld);


                    _vertices[column]
                        .SetFixedPosition(
                            worldPosition);
                }


                return true;
            }


            // ========================================================
            // BUILD CLASSIC LINKS
            // ========================================================

            private void BuildLinks()
            {
                List<ClothLink> links =
                    new List<ClothLink>(
                        2 *
                        (
                            (_columns - 1) *
                            _rows
                            +
                            _columns *
                            (_rows - 1)
                        ));


                for (int row = 0;
                     row < _rows;
                     row++)
                {
                    for (int column = 0;
                         column < _columns;
                         column++)
                    {
                        int vertex =
                            row *
                            _columns +
                            column;


                        // =============================================
                        // VERTICAL
                        // =============================================

                        if (row <
                            _rows - 1)
                        {
                            int other =
                                vertex +
                                _columns;


                            AddLink(
                                links,
                                vertex,
                                other,

                                LinkStyle.Spring |
                                LinkStyle.StrictDistance);
                        }


                        // =============================================
                        // HORIZONTAL
                        // =============================================

                        if (column <
                            _columns - 1)
                        {
                            int other =
                                vertex +
                                1;


                            AddLink(
                                links,
                                vertex,
                                other,

                                LinkStyle.Spring |
                                LinkStyle.LooseDistance);


                            // =========================================
                            // DIAGONAL DOWN
                            // =========================================

                            if (row <
                                _rows - 1)
                            {
                                int diagonal =
                                    vertex +
                                    1 +
                                    _columns;


                                AddLink(
                                    links,
                                    vertex,
                                    diagonal,

                                    LinkStyle.LooseDistance);
                            }


                            // =========================================
                            // DIAGONAL UP
                            // =========================================

                            if (row > 1)
                            {
                                int diagonal =
                                    vertex +
                                    1 -
                                    _columns;


                                AddLink(
                                    links,
                                    vertex,
                                    diagonal,

                                    LinkStyle.LooseDistance);
                            }
                        }
                    }
                }


                _links =
                    links.ToArray();
            }


            private void AddLink(
                List<ClothLink> links,
                int vertex1,
                int vertex2,
                LinkStyle style)
            {
                float distance =
                    Vector3.Distance(
                        _vertices[vertex1]
                            .Position,

                        _vertices[vertex2]
                            .Position);


                links.Add(
                    new ClothLink(
                        vertex1,
                        vertex2,

                        distance *
                        0.8f,

                        distance,

                        style));
            }


            // ========================================================
            // CLASSIC MOVE2
            // ========================================================

            public bool StepClassic(
                ModelObject player,
                Vector3 wind,
                float worldTimeSeconds,
                float substepTime)
            {
                if (!Initialized)
                {
                    return false;
                }


                if (!float.IsFinite(
                        substepTime) ||
                    substepTime <= 0.0f)
                {
                    return true;
                }


                for (int i = 0;
                     i < ClassicSubstepCount;
                     i++)
                {
                    InitializeForces(
                        wind,
                        worldTimeSeconds);


                    ApplySpringForces();


                    if (!SyncPinnedVertices(
                            player))
                    {
                        return false;
                    }


                    IntegrateVertices(
                        substepTime);


                    if (!PreventFromStretching(
                            player))
                    {
                        return false;
                    }
                }


                return true;
            }


            // ========================================================
            // FORCE INITIALIZATION
            // ========================================================

            private void InitializeForces(
                Vector3 wind,
                float worldTimeSeconds)
            {
                int vertexCount =
                    _vertices.Length;


                int seed =
                    (
                        (int)(
                            worldTimeSeconds /
                            0.4f)
                        *
                        101
                    )
                    %
                    vertexCount;


                int seedColumn =
                    seed %
                    _columns;


                int seedRow =
                    seed /
                    _columns;


                for (int index = 0;
                     index < vertexCount;
                     index++)
                {
                    ref ClothVertex vertex =
                        ref _vertices[index];


                    if (vertex.Fixed)
                    {
                        vertex.Force =
                            Vector3.Zero;

                        continue;
                    }


                    int column =
                        index %
                        _columns;


                    int row =
                        index /
                        _columns;


                    int key =
                        Math.Abs(
                            seedColumn -
                            column)
                        +
                        Math.Abs(
                            seedRow -
                            row);


                    int temp =
                        Math.Clamp(
                            5 - key,
                            0,
                            4);


                    float randomForce =
                        temp == 0
                            ? 0.0f
                            : temp + 2.0f;


                    vertex.Force =
                        wind *
                        randomForce
                        -
                        vertex.Velocity *
                        0.01f;


                    float weight =
                        _heavy
                            ? 180.0f
                            : 100.0f;


                    vertex.Force.Z -=
                        Gravity *
                        VertexMass *
                        weight;
                }
            }


            // ========================================================
            // SPRINGS
            // ========================================================

            private void ApplySpringForces()
            {
                for (int i = 0;
                     i < _links.Length;
                     i++)
                {
                    ClothLink link =
                        _links[i];


                    if ((link.Style &
                         LinkStyle.Spring)
                        == 0)
                    {
                        continue;
                    }


                    Vector3 difference =
                        _vertices[
                            link.Vertex1]
                        .Position
                        -
                        _vertices[
                            link.Vertex2]
                        .Position;


                    float distance =
                        difference.Length();


                    if (distance <
                        0.001f)
                    {
                        distance =
                            0.001f;
                    }


                    if (distance >
                        link.MaxDistance +
                        0.01f)
                    {
                        Vector3 force =
                            difference
                            *
                            (
                                (
                                    distance -
                                    link.MaxDistance
                                )
                                /
                                distance
                            );


                        _vertices[
                            link.Vertex1]
                            .Force
                            -=
                            force;


                        _vertices[
                            link.Vertex2]
                            .Force
                            +=
                            force;
                    }
                }
            }


            // ========================================================
            // INTEGRATION
            // ========================================================

            private void IntegrateVertices(
                float dt)
            {
                for (int i = 0;
                     i < _vertices.Length;
                     i++)
                {
                    ref ClothVertex vertex =
                        ref _vertices[i];


                    if (vertex.Fixed)
                    {
                        continue;
                    }


                    vertex.Velocity +=
                        vertex.Force *
                        InverseVertexMass *
                        dt;


                    vertex.Position +=
                        vertex.Velocity *
                        dt;
                }
            }


            // ========================================================
            // CONSTRAINTS
            // ========================================================

            private bool PreventFromStretching(
                ModelObject player)
            {
                ProcessCollisions(
                    player);


                // ====================================================
                // LOOSE DISTANCE
                // ====================================================

                for (int i = 0;
                     i < _links.Length;
                     i++)
                {
                    ClothLink link =
                        _links[i];


                    if ((link.Style &
                         LinkStyle.LooseDistance)
                        == 0)
                    {
                        continue;
                    }


                    AddOneTimeMoveToKeepLength(
                        link.Vertex1,
                        link.Vertex2,
                        link.MaxDistance);
                }


                for (int i = 0;
                     i < _vertices.Length;
                     i++)
                {
                    _vertices[i]
                        .ApplyPendingMove();
                }


                // ====================================================
                // STRICT VERTICAL LINKS
                // ====================================================

                for (int i = 0;
                     i < _links.Length;
                     i++)
                {
                    ClothLink link =
                        _links[i];


                    if ((link.Style &
                         LinkStyle.StrictDistance)
                        == 0)
                    {
                        continue;
                    }


                    if (link.Vertex2 <
                        _columns)
                    {
                        continue;
                    }


                    if (!KeepLength(
                            link.Vertex2,
                            link.Vertex1,

                            link.MinDistance,
                            link.MaxDistance))
                    {
                        return false;
                    }
                }


                return true;
            }


            private void AddOneTimeMoveToKeepLength(
                int vertex1Index,
                int vertex2Index,
                float length)
            {
                ref ClothVertex vertex1 =
                    ref _vertices[
                        vertex1Index];


                ref ClothVertex vertex2 =
                    ref _vertices[
                        vertex2Index];


                Vector3 difference =
                    vertex1.Position -
                    vertex2.Position;


                float distance =
                    difference.Length();


                if (distance <
                    0.001f)
                {
                    distance =
                        0.001f;
                }


                Vector3 movement =
                    difference
                    *
                    (
                        (
                            distance -
                            length
                        )
                        *
                        0.5f
                        /
                        distance
                    );


                vertex1.AddPendingMove(
                    -movement);


                vertex2.AddPendingMove(
                    movement);
            }


            private bool KeepLength(
                int movingVertexIndex,
                int referenceVertexIndex,
                float minDistance,
                float maxDistance)
            {
                ref ClothVertex moving =
                    ref _vertices[
                        movingVertexIndex];


                if (moving.Fixed)
                {
                    return true;
                }


                ClothVertex reference =
                    _vertices[
                        referenceVertexIndex];


                Vector3 difference =
                    moving.Position -
                    reference.Position;


                float distance =
                    difference.Length();


                if (distance <
                    0.001f)
                {
                    distance =
                        0.001f;
                }


                if (distance >
                    maxDistance *
                    20.0f)
                {
                    return false;
                }


                if (distance >
                    maxDistance)
                {
                    difference *=
                        (
                            distance -
                            maxDistance
                        )
                        /
                        distance;


                    moving.Position -=
                        difference;
                }
                else if (
                    distance <
                    minDistance)
                {
                    difference *=
                        (
                            distance -
                            minDistance
                        )
                        /
                        distance;


                    moving.Position -=
                        difference;
                }


                return true;
            }


            // ========================================================
            // COLLISIONS
            // ========================================================

            private void ProcessCollisions(
                ModelObject player)
            {
                for (int collisionIndex = 0;
                     collisionIndex <
                     _collisions.Length;
                     collisionIndex++)
                {
                    CollisionSphereDefinition collision =
                        _collisions[
                            collisionIndex];


                    if (!TryGetBoneWorldMatrix(
                            player,
                            collision.Bone,
                            out Matrix boneWorld))
                    {
                        continue;
                    }


                    Vector3 local =
                        ClassicAttachmentLocalToClient(
                            collision.LocalCenter);


                    Vector3 center =
                        Vector3.Transform(
                            local,
                            boneWorld);


                    for (int vertexIndex = 0;
                         vertexIndex <
                         _vertices.Length;
                         vertexIndex++)
                    {
                        ref ClothVertex vertex =
                            ref _vertices[
                                vertexIndex];


                        Vector3 difference =
                            vertex.Position -
                            center;


                        float distance =
                            difference.Length();


                        if (distance <
                            0.01f)
                        {
                            distance =
                                0.01f;


                            difference =
                                new Vector3(
                                    distance,
                                    0.0f,
                                    0.0f);
                        }


                        if (distance <
                            collision.Radius)
                        {
                            Vector3 movement =
                                difference
                                *
                                (
                                    (
                                        collision.Radius -
                                        distance
                                    )
                                    /
                                    distance
                                );


                            vertex.AddPendingMove(
                                movement);
                        }
                    }
                }
            }


            // ========================================================
            // RENDER GEOMETRY
            // ========================================================

            public void BuildRenderVertices()
            {
                for (int row = 0;
                     row < _rows;
                     row++)
                {
                    float v =
                        MathF.Min(
                            0.99f,

                            (float)row /
                            (_rows - 1));


                    for (int column = 0;
                         column < _columns;
                         column++)
                    {
                        int index =
                            row *
                            _columns +
                            column;


                        float u =
                            (float)column /
                            (_columns - 1);


                        _renderVertices[index] =
                            new VertexPositionColorTexture(
                                _vertices[index]
                                    .Position,

                                Color.White,

                                new Vector2(
                                    u,
                                    v));
                    }
                }
            }


            private static short[]
                BuildRenderIndices(
                    int columns,
                    int rows)
            {
                int quadCount =
                    (columns - 1) *
                    (rows - 1);


                short[] indices =
                    new short[
                        quadCount *
                        6];


                int indexOffset =
                    0;


                for (int row = 0;
                     row < rows - 1;
                     row++)
                {
                    for (int column = 0;
                         column < columns - 1;
                         column++)
                    {
                        short v0 =
                            (short)(
                                row *
                                columns +
                                column);


                        short v1 =
                            (short)(
                                v0 +
                                1);


                        short v3 =
                            (short)(
                                v0 +
                                columns);


                        short v2 =
                            (short)(
                                v3 +
                                1);


                        indices[
                            indexOffset++] =
                            v0;


                        indices[
                            indexOffset++] =
                            v1;


                        indices[
                            indexOffset++] =
                            v2;


                        indices[
                            indexOffset++] =
                            v0;


                        indices[
                            indexOffset++] =
                            v2;


                        indices[
                            indexOffset++] =
                            v3;
                    }
                }


                return indices;
            }
        }
    }
}