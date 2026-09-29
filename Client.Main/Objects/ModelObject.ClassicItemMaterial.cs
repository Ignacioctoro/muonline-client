using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Controls;
using Client.Main.Graphics;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Main.Objects
{
    /// <summary>
    /// Classic MU item level material renderer.
    ///
    /// Port of the material progression used by RenderPartObjectEffect():
    ///
    /// +7 / +8
    ///     base 0.8
    ///     + CHROME
    ///
    /// +9 / +10
    ///     base 0.9
    ///     + CHROME
    ///     + METAL
    ///
    /// +11 / +12
    ///     base 0.9
    ///     + CHROME2
    ///     + METAL
    ///     + CHROME
    ///
    /// +13 / +14 / +15
    ///     base 0.9
    ///     + CHROME4
    ///     + METAL
    ///     + CHROME
    ///
    /// Excellent and Ancient are intentionally NOT implemented here yet.
    /// They will be layered on top after the level rendering is validated.
    /// </summary>
    public abstract partial class ModelObject
    {
        // --------------------------------------------------------------------
        // Original MU EnableAlphaBlend():
        //
        // glBlendFunc(
        //     GL_ONE,
        //     GL_ONE);
        //
        // XNA/MonoGame BlendState.Additive is NOT exactly the same,
        // because its source blend normally uses SourceAlpha.
        // --------------------------------------------------------------------

        private static readonly BlendState
            _classicItemBrightAdditive =
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


        // --------------------------------------------------------------------
        // Classic MU material bitmaps.
        //
        // TextureLoader accepts the original .jpg name and resolves it to
        // the corresponding .OZJ inside Data.
        // --------------------------------------------------------------------

        private const string
            ClassicChrome01Path =
                "Effect/Chrome01.jpg";

        private const string
            ClassicChrome02Path =
                "Effect/Chrome02.jpg";

        private const string
            ClassicShiny01Path =
                "Effect/Shiny01.jpg";


        private static readonly object
            _classicItemTextureLock =
                new object();


        private static Texture2D
            _classicChrome01;

        private static Texture2D
            _classicChrome02;

        private static Texture2D
            _classicShiny01;


        private static bool
            _classicItemTextureLoadAttempted;


        // ====================================================================
        // TEXTURE LOADING
        // ====================================================================

        private static bool
            IsClassicTextureReady(
                Texture2D texture)
        {
            return
                texture != null &&
                !texture.IsDisposed;
        }


        private static Texture2D
            LoadClassicItemTexture(
                string path)
        {
            try
            {
                // Prepare() performs the file/decode work.
                //
                // We intentionally create Texture2D afterwards on the render
                // thread through GetTexture2D().

                TextureLoader.Instance
                    .Prepare(
                        path)
                    .GetAwaiter()
                    .GetResult();


                return
                    TextureLoader.Instance
                        .GetTexture2D(
                            path);
            }
            catch
            {
                return null;
            }
        }


        private static bool
            EnsureClassicItemTextures()
        {
            if (IsClassicTextureReady(
                    _classicChrome01) &&
                IsClassicTextureReady(
                    _classicChrome02) &&
                IsClassicTextureReady(
                    _classicShiny01))
            {
                return true;
            }


            lock (_classicItemTextureLock)
            {
                if (IsClassicTextureReady(
                        _classicChrome01) &&
                    IsClassicTextureReady(
                        _classicChrome02) &&
                    IsClassicTextureReady(
                        _classicShiny01))
                {
                    return true;
                }


                // Do not hammer the filesystem every frame if the Data is
                // incomplete or one file is missing.

                if (_classicItemTextureLoadAttempted)
                {
                    return false;
                }


                _classicItemTextureLoadAttempted =
                    true;


                _classicChrome01 =
                    LoadClassicItemTexture(
                        ClassicChrome01Path);


                _classicChrome02 =
                    LoadClassicItemTexture(
                        ClassicChrome02Path);


                _classicShiny01 =
                    LoadClassicItemTexture(
                        ClassicShiny01Path);


                return
                    IsClassicTextureReady(
                        _classicChrome01) &&
                    IsClassicTextureReady(
                        _classicChrome02) &&
                    IsClassicTextureReady(
                        _classicShiny01);
            }
        }


        // ====================================================================
        // CLASSIC ITEM COLOR TABLE
        //
        // Port of PartObjectColor().
        //
        // ItemGroup:
        //
        // 0  Sword
        // 1  Axe
        // 2  Mace / Scepter
        // 3  Spear
        // 4  Bow / Crossbow
        // 5  Staff
        // 6  Shield
        // 7  Helm
        // 8  Armor
        // 9  Pants
        // 10 Gloves
        // 11 Boots
        // ====================================================================

        private Vector3
            GetClassicSecondaryItemColor(
                Vector3 bodyLight)
        {
            int colorCode =
                0;


            if (ItemGroup >= 7 &&
                ItemGroup <= 11)
            {
                colorCode =
                    ItemNumber switch
                    {
                        4 => 1,

                        14 => 1,
                        15 => 1,
                        17 => 1,

                        18 => 2,

                        21 => 3,

                        39 => 1,
                        40 => 1,
                        41 => 1,
                        42 => 1,

                        43 => 2,
                        44 => 3,

                        _ => 0
                    };
            }
            else
            {
                if (ItemGroup == 4 &&
                    (ItemNumber == 5 ||
                    ItemNumber == 13))
                {
                    colorCode =
                        2;
                }
                else if (
                    ItemGroup == 0 &&
                    ItemNumber == 14)
                {
                    colorCode =
                        2;
                }
                else if (
                    ItemGroup == 5 &&
                    ItemNumber == 5)
                {
                    colorCode =
                        2;
                }
            }


            // ------------------------------------------------------------
            // Exact behavior of PartObjectColor2().
            //
            // IMPORTANT:
            //
            // Unlike PartObjectColor(), this does NOT normally replace
            // BodyLight with a completely new color.
            //
            // It filters the BodyLight that was already set by the base
            // item pass.
            // ------------------------------------------------------------

            switch (colorCode)
            {
                default:
                case 0:

                    return bodyLight;


                case 1:

                    return new Vector3(
                        bodyLight.X,
                        bodyLight.Y *
                        0.5f,
                        0.0f);


                case 2:

                    return new Vector3(
                        0.0f,
                        bodyLight.Y *
                        0.5f,
                        bodyLight.Z);


                case 3:

                    // Original case 3 explicitly sets white.
                    return Vector3.One;
            }
        }



        private Vector3
            GetClassicPrimaryItemColor()
        {
            int colorCode =
                0;


            // ---------------------------------------------------------------
            // Armor sets
            // ---------------------------------------------------------------

            if (ItemGroup >= 7 &&
                ItemGroup <= 11)
            {
                colorCode =
                    ItemNumber switch
                    {
                        1 => 1,
                        3 => 3,
                        4 => 5,
                        6 => 6,

                        // Sphinx
                        7 => 44,

                        9 => 2,
                        12 => 2,
                        13 => 4,
                        14 => 5,
                        15 => 7,
                        16 => 10,
                        17 => 9,
                        18 => 5,
                        19 => 9,
                        20 => 9,

                        21 => 16,
                        22 => 17,
                        23 => 11,
                        24 => 16,
                        25 => 11,
                        26 => 12,
                        27 => 10,
                        28 => 15,

                        29 => 18,
                        30 => 19,
                        31 => 20,
                        32 => 21,
                        33 => 22,

                        34 => 24,
                        35 => 25,
                        36 => 26,
                        37 => 27,
                        38 => 28,

                        39 => 29,
                        40 => 30,
                        41 => 31,
                        42 => 32,
                        43 => 33,
                        44 => 34,

                        45 => 36,
                        46 => 42,
                        47 => 37,
                        48 => 1,
                        49 => 35,
                        50 => 39,
                        51 => 40,
                        52 => 36,
                        53 => 41,

                        59 => 16,
                        60 => 42,
                        61 => 18,

                        _ => 0
                    };
            }
            else
            {
                // -----------------------------------------------------------
                // Weapons / shields
                // -----------------------------------------------------------

                switch (ItemGroup)
                {
                    // Sword
                    case 0:
                        colorCode =
                            ItemNumber switch
                            {
                                14 => 2,

                                20 => 10,
                                21 => 5,
                                22 => 18,
                                23 => 23,
                                24 => 24,
                                25 => 27,

                                28 => 8,
                                31 => 10,

                                _ => 0
                            };
                        break;


                    // Axe
                    case 1:
                        colorCode =
                            0;
                        break;


                    // Mace / Scepter
                    case 2:
                        colorCode =
                            ItemNumber switch
                            {
                                8 => 9,
                                9 => 10,
                                10 => 12,

                                12 => 16,
                                14 => 22,
                                15 => 28,

                                17 => 40,
                                18 => 5,

                                _ => 0
                            };
                        break;


                    // Spear
                    case 3:
                        colorCode =
                            ItemNumber switch
                            {
                                9 => 1,
                                10 => 9,
                                11 => 20,

                                _ => 0
                            };
                        break;


                    // Bow / Crossbow
                    case 4:
                        colorCode =
                            ItemNumber switch
                            {
                                5 => 5,
                                13 => 5,

                                17 => 9,
                                18 => 10,
                                19 => 9,

                                20 => 16,
                                21 => 20,
                                22 => 26,
                                23 => 35,
                                24 => 36,

                                _ => 0
                            };
                        break;


                    // Staff
                    case 5:
                        colorCode =
                            ItemNumber switch
                            {
                                5 => 2,
                                9 => 5,

                                11 => 17,
                                12 => 19,
                                13 => 25,
                                14 => 24,
                                15 => 15,
                                16 => 1,
                                17 => 3,
                                18 => 30,
                                19 => 21,
                                20 => 34,
                                22 => 1,

                                30 => 1,
                                31 => 19,
                                33 => 43,
                                34 => 5,

                                _ => 0
                            };
                        break;


                    // Shield
                    case 6:
                        colorCode =
                            ItemNumber switch
                            {
                                16 => 6,

                                19 => 29,
                                20 => 36,
                                21 => 30,

                                _ => 0
                            };
                        break;
                }
            }


            return
                GetClassicPrimaryColorByCode(
                    colorCode);
        }


        private static Vector3
            GetClassicPrimaryColorByCode(
                int colorCode)
        {
            return
                colorCode switch
                {
                    0 =>
                        new Vector3(
                            1.00f,
                            0.50f,
                            0.00f),

                    1 =>
                        new Vector3(
                            1.00f,
                            0.20f,
                            0.00f),

                    2 =>
                        new Vector3(
                            0.00f,
                            0.50f,
                            1.00f),

                    3 =>
                        new Vector3(
                            0.00f,
                            0.50f,
                            1.00f),

                    4 =>
                        new Vector3(
                            0.00f,
                            0.80f,
                            0.40f),

                    5 =>
                        new Vector3(
                            1.00f,
                            1.00f,
                            1.00f),

                    6 =>
                        new Vector3(
                            0.60f,
                            0.80f,
                            0.40f),

                    7 =>
                        new Vector3(
                            0.90f,
                            0.80f,
                            1.00f),

                    8 =>
                        new Vector3(
                            0.80f,
                            0.80f,
                            1.00f),

                    9 =>
                        new Vector3(
                            0.50f,
                            0.50f,
                            0.80f),

                    10 =>
                        new Vector3(
                            0.75f,
                            0.65f,
                            0.50f),

                    11 =>
                        new Vector3(
                            0.35f,
                            0.35f,
                            0.60f),

                    12 =>
                        new Vector3(
                            0.47f,
                            0.67f,
                            0.60f),

                    13 =>
                        new Vector3(
                            0.00f,
                            0.30f,
                            0.60f),

                    14 =>
                        new Vector3(
                            0.65f,
                            0.65f,
                            0.55f),

                    15 =>
                        new Vector3(
                            0.20f,
                            0.30f,
                            0.60f),

                    16 =>
                        new Vector3(
                            0.80f,
                            0.46f,
                            0.25f),

                    17 =>
                        new Vector3(
                            0.65f,
                            0.45f,
                            0.30f),

                    18 =>
                        new Vector3(
                            0.50f,
                            0.40f,
                            0.30f),

                    19 =>
                        new Vector3(
                            0.37f,
                            0.37f,
                            1.00f),

                    20 =>
                        new Vector3(
                            0.30f,
                            0.70f,
                            0.30f),

                    21 =>
                        new Vector3(
                            0.50f,
                            0.40f,
                            1.00f),

                    22 =>
                        new Vector3(
                            0.45f,
                            0.45f,
                            0.23f),

                    23 =>
                        new Vector3(
                            0.30f,
                            0.30f,
                            0.45f),

                    24 =>
                        new Vector3(
                            0.60f,
                            0.50f,
                            0.20f),

                    25 =>
                        new Vector3(
                            0.60f,
                            0.60f,
                            0.60f),

                    26 =>
                        new Vector3(
                            0.30f,
                            0.70f,
                            0.30f),

                    27 =>
                        new Vector3(
                            0.50f,
                            0.60f,
                            0.70f),

                    28 =>
                        new Vector3(
                            0.45f,
                            0.45f,
                            0.23f),

                    29 =>
                        new Vector3(
                            0.20f,
                            0.70f,
                            0.30f),

                    30 =>
                        new Vector3(
                            0.70f,
                            0.30f,
                            0.30f),

                    31 =>
                        new Vector3(
                            0.70f,
                            0.50f,
                            0.30f),

                    32 =>
                        new Vector3(
                            0.50f,
                            0.20f,
                            0.70f),

                    33 =>
                        new Vector3(
                            0.80f,
                            0.40f,
                            0.60f),

                    34 =>
                        new Vector3(
                            0.60f,
                            0.40f,
                            0.80f),

                    35 =>
                        new Vector3(
                            0.70f,
                            0.40f,
                            0.40f),

                    36 =>
                        new Vector3(
                            0.50f,
                            0.50f,
                            0.70f),

                    37 =>
                        new Vector3(
                            0.70f,
                            0.50f,
                            0.70f),

                    38 =>
                        new Vector3(
                            0.20f,
                            0.40f,
                            0.70f),

                    39 =>
                        new Vector3(
                            0.30f,
                            0.60f,
                            0.40f),

                    40 =>
                        new Vector3(
                            0.70f,
                            0.20f,
                            0.20f),

                    41 =>
                        new Vector3(
                            0.70f,
                            0.20f,
                            0.70f),

                    42 =>
                        new Vector3(
                            0.80f,
                            0.40f,
                            0.00f),

                    43 =>
                        new Vector3(
                            0.80f,
                            0.60f,
                            0.20f),

                    44 =>
                        new Vector3(
                            0.80f,
                            0.70f,
                            0.40f),

                    _ =>
                        new Vector3(
                            1.00f,
                            0.50f,
                            0.00f)
                };
        }


        // ====================================================================
        // SECONDARY CLASSIC COLOR TABLE
        //
        // Port of PartObjectColor2().
        //
        // Used by:
        //
        // CHROME2
        // CHROME4
        // ====================================================================
        private Vector3
            GetClassicLevelBodyLight()
        {
            Vector3 light =
                Light;


            if (LightEnabled &&
                World?.Terrain != null)
            {
                Vector3 position =
                    WorldPosition.Translation;


                light =
                    World.Terrain
                        .EvaluateTerrainLight(
                            position.X,
                            position.Y)
                    +
                    Light;
            }


            // +9 onward in the original:
            //
            // Vector(
            //     Light[0] * 0.9f,
            //     Light[1] * 0.9f,
            //     Light[2] * 0.9f,
            //     b->BodyLight);
            //
            // Chrome2/Chrome4 then starts from this BodyLight.

            light *=
                0.90f;


            return Vector3.Clamp(
                light,
                Vector3.Zero,
                Vector3.One);
        }

        // ====================================================================
        // CLASSIC MULTIPASS RENDERER
        // ====================================================================

        private void
            DrawMeshWithClassicItemMaterial(
                int mesh)
        {
            if (Model?.Meshes == null ||
                mesh < 0 ||
                mesh >= Model.Meshes.Length)
            {
                return;
            }


            if (_boneVertexBuffers == null ||
                _boneIndexBuffers == null ||
                _boneTextures == null ||
                mesh >= _boneVertexBuffers.Length ||
                mesh >= _boneIndexBuffers.Length ||
                mesh >= _boneTextures.Length ||
                _boneVertexBuffers[mesh] == null ||
                _boneIndexBuffers[mesh] == null ||
                _boneTextures[mesh] == null ||
                IsHiddenMesh(
                    mesh))
            {
                return;
            }


            Effect effect =
                GraphicsManager.Instance
                    .ItemMaterialEffect;


            if (effect == null)
            {
                return;
            }


            GraphicsDevice gd =
                GraphicsDevice;


            VertexBuffer vertexBuffer =
                _boneVertexBuffers[mesh];


            IndexBuffer indexBuffer =
                _boneIndexBuffers[mesh];


            Texture2D diffuseTexture =
                _boneTextures[mesh];


            bool isBlendMesh =
                IsBlendMesh(
                    mesh);


            bool isTwoSided =
                IsMeshTwoSided(
                    mesh,
                    isBlendMesh);


            BlendState baseBlendState =
                GetMeshBlendState(
                    mesh,
                    isBlendMesh);


            RasterizerState previousRasterizer =
                gd.RasterizerState;


            BlendState previousBlendState =
                gd.BlendState;


            DepthStencilState previousDepthState =
                gd.DepthStencilState;


            try
            {
                effect.CurrentTechnique =
                    effect.Techniques[0];


                GraphicsManager.Instance
                    .ShadowMapRenderer
                    ?.ApplyShadowParameters(
                        effect);


                // ------------------------------------------------------------
                // Common matrices
                // ------------------------------------------------------------

                effect.Parameters[
                        "World"]
                    ?.SetValue(
                        WorldPosition);


                effect.Parameters[
                        "View"]
                    ?.SetValue(
                        Camera.Instance.View);


                effect.Parameters[
                        "Projection"]
                    ?.SetValue(
                        Camera.Instance.Projection);


                effect.Parameters[
                        "DiffuseTexture"]
                    ?.SetValue(
                        diffuseTexture);


                effect.Parameters[
                        "Time"]
                    ?.SetValue(
                        GetShaderTimeSeconds());


                effect.Parameters[
                        "Alpha"]
                    ?.SetValue(
                        TotalAlpha);


                // ------------------------------------------------------------
                // Shadow strength
                // ------------------------------------------------------------

                bool worldAllowsSun =
                    World is WorldControl wc
                        ? wc.IsSunWorld
                        : true;


                bool sunEnabled =
                    Constants.SUN_ENABLED &&
                    worldAllowsSun &&
                    UseSunLight &&
                    !HasWalkerAncestor();


                effect.Parameters[
                        "ShadowStrength"]
                    ?.SetValue(
                        sunEnabled
                            ? SunCycleManager
                                .GetEffectiveShadowStrength()
                            : 0.0f);


                // ------------------------------------------------------------
                // Buffers
                // ------------------------------------------------------------

                gd.SetVertexBuffer(
                    vertexBuffer);


                gd.Indices =
                    indexBuffer;


                int primitiveCount =
                    indexBuffer.IndexCount /
                    3;


                EffectPass pass =
                    effect.CurrentTechnique
                        .Passes[0];


                // ============================================================
                // BASE PASS
                // ============================================================

                int level =
                    Math.Clamp(
                        ItemLevel,
                        0,
                        15);


                float baseLightScale =
                    level < 9
                        ? 0.80f
                        : 0.90f;


                effect.Parameters[
                        "PassMode"]
                    ?.SetValue(
                        0);


                effect.Parameters[
                        "BaseLightScale"]
                    ?.SetValue(
                        baseLightScale);


                effect.Parameters[
                        "MaterialIntensity"]
                    ?.SetValue(
                        1.0f);


                effect.Parameters[
                        "MaterialColor"]
                    ?.SetValue(
                        Vector3.One);


                gd.RasterizerState =
                    isTwoSided
                        ? _cullNone
                        : _cullClockwise;


                gd.BlendState =
                    baseBlendState;


                gd.DepthStencilState =
                    isBlendMesh
                        ? GraphicsManager
                            .ReadOnlyDepth
                        : previousDepthState;


                pass.Apply();


                gd.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    primitiveCount);


                // Nothing else below +7.
                //
                // At the moment the item shader is only selected from +7,
                // but keeping this guard makes the method self-contained.

                if (level < 7)
                {
                    return;
                }


                // If one of the original material textures is unavailable,
                // keep the correctly lit base item instead of making the
                // entire item disappear.

                if (!EnsureClassicItemTextures())
                {
                    return;
                }


                // ============================================================
                // CLASSIC BRIGHT PASSES
                //
                // Original EnableAlphaBlend():
                //
                // GL_ONE + GL_ONE
                //
                // No depth writes.
                // No culling.
                // ============================================================

                gd.BlendState =
                    _classicItemBrightAdditive;


                gd.DepthStencilState =
                    GraphicsManager
                        .ReadOnlyDepth;


                gd.RasterizerState =
                    _cullNone;


                Vector3 primaryColor =
                    GetClassicPrimaryItemColor();


                Vector3 classicBodyLight =
                    GetClassicLevelBodyLight();


                Vector3 secondaryColor =
                    GetClassicSecondaryItemColor(
                        classicBodyLight);


                void DrawMaterialPass(
                    int passMode,
                    Texture2D materialTexture,
                    Vector3 materialColor)
                {
                    if (materialTexture == null ||
                        materialTexture.IsDisposed)
                    {
                        return;
                    }


                    effect.Parameters[
                            "PassMode"]
                        ?.SetValue(
                            passMode);


                    effect.Parameters[
                            "MaterialTexture"]
                        ?.SetValue(
                            materialTexture);


                    effect.Parameters[
                            "MaterialColor"]
                        ?.SetValue(
                            materialColor);


                    effect.Parameters[
                            "MaterialIntensity"]
                        ?.SetValue(
                            1.0f);


                    pass.Apply();


                    gd.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        primitiveCount);
                }


                // ============================================================
                // +7 / +8
                //
                // base
                // CHROME
                // ============================================================

                if (level <= 8)
                {
                    DrawMaterialPass(
                        1,
                        _classicChrome01,
                        primaryColor);
                }
                else if (level <= 10)
                {
                    // ========================================================
                    // +9 / +10
                    //
                    // base
                    // CHROME
                    // METAL
                    // ========================================================

                    DrawMaterialPass(
                        1,
                        _classicChrome01,
                        primaryColor);


                    DrawMaterialPass(
                        4,
                        _classicShiny01,
                        primaryColor);
                }
                else if (level <= 12)
                {
                    // ========================================================
                    // +11 / +12
                    //
                    // base
                    // CHROME2
                    // METAL
                    // CHROME
                    // ========================================================

                    DrawMaterialPass(
                        2,
                        _classicChrome02,
                        secondaryColor);


                    DrawMaterialPass(
                        4,
                        _classicShiny01,
                        primaryColor);


                    DrawMaterialPass(
                        1,
                        _classicChrome01,
                        primaryColor);
                }
                else
                {
                    // ========================================================
                    // +13 / +14 / +15
                    //
                    // base
                    // CHROME4
                    // METAL
                    // CHROME
                    // ========================================================

                    DrawMaterialPass(
                        3,
                        _classicChrome02,
                        secondaryColor);


                    DrawMaterialPass(
                        4,
                        _classicShiny01,
                        primaryColor);


                    DrawMaterialPass(
                        1,
                        _classicChrome01,
                        primaryColor);
                }


                // ============================================================
                // CLASSIC EXCELLENT PASS
                //
                // This is a SEPARATE pass in the original client, executed
                // after the level material rendering.
                //
                // Luminosity =
                //     sin(WorldTime * 0.002f) * 0.5f + 0.5f;
                //
                // BodyLight =
                //     (Luminosity,
                //      Luminosity * 0.3f,
                //      1.0f - Luminosity);
                //
                // RenderBody(RENDER_TEXTURE | RENDER_BRIGHT)
                // ============================================================

                if (IsExcellentItem)
                {
                    float excellentLuminosity =
                        MathF.Sin(
                            GetShaderTimeSeconds() *
                            2.0f)
                        *
                        0.5f
                        +
                        0.5f;


                    Vector3 excellentColor =
                        new Vector3(
                            excellentLuminosity,

                            excellentLuminosity *
                            0.30f,

                            1.0f -
                            excellentLuminosity);


                    DrawMaterialPass(
                        5,
                        diffuseTexture,
                        excellentColor);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(
                    "Classic item material render failed on mesh {Mesh}: {Message}",
                    mesh,
                    ex.Message);
            }
            finally
            {
                // Leave the shared ItemMaterial Effect in a deterministic base state.
                // UI previews and other renderers reuse the same Effect instance.

                effect.Parameters["PassMode"]
                    ?.SetValue(
                        0);

                effect.Parameters["BaseLightScale"]
                    ?.SetValue(
                        1.0f);

                effect.Parameters["MaterialColor"]
                    ?.SetValue(
                        Vector3.One);

                effect.Parameters["MaterialIntensity"]
                    ?.SetValue(
                        1.0f);
                        
                gd.RasterizerState =
                    previousRasterizer;


                gd.BlendState =
                    previousBlendState;


                gd.DepthStencilState =
                    previousDepthState;
            }
        }
    }
}