using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Wings
{
    public partial class WingObject
    {
        // ================================================================
        // WING OF ILLUSION
        //
        // Item:
        //     12,38
        //
        // Model:
        //     Item/Wing10.bmd
        //
        // Classic client:
        //
        // 12 x BITMAP_FLARE
        //     red / pink
        //
        // 8 x BITMAP_LIGHT
        //     green
        //
        // 1 x BITMAP_LIGHT
        //     small yellow highlight
        //
        // Wing10 itself does NOT have a special classic multipass renderer.
        // The normal Neffis renderer remains responsible for the model.
        // ================================================================

        private const short IllusionWingItemIndex =
            38;


        // Classic:
        //
        // BITMAP_FLARE
        //     Effect/Flare.jpg
        //
        // Data_Broyal:
        //     Effect/Flare.OZJ
        //
        // Keep the capital F because Android/Linux paths are case-sensitive.
        private const string IllusionFlareTexturePath =
            "Effect/Flare.jpg";


        // Classic:
        //
        // BITMAP_LIGHT
        //     Effect/flare01.jpg
        //
        // Data_Broyal:
        //     Effect/flare01.OZJ
        private const string IllusionLightTexturePath =
            "Effect/flare01.jpg";


        private bool IsIllusionWing =>
            ItemIndex ==
            IllusionWingItemIndex;


        // ================================================================
        // CLASSIC BONE TABLES
        // ================================================================

        // Original:
        //
        // int iRedFlarePos[] =
        // {
        //     5, 6, 7, 8,
        //     18, 19,
        //     23, 24, 25, 27,
        //     37, 38
        // };

        private static readonly int[]
            IllusionRedFlareBones =
        {
            5,
            6,
            7,
            8,
            18,
            19,
            23,
            24,
            25,
            27,
            37,
            38
        };


        // Original:
        //
        // int iGreenFlarePos[] =
        // {
        //     4, 9, 13, 14,
        //     26, 32, 31, 33
        // };

        private static readonly int[]
            IllusionGreenLightBones =
        {
            4,
            9,
            13,
            14,
            26,
            32,
            31,
            33
        };


        // ================================================================
        // RESOURCES
        // ================================================================

        private Texture2D
            _illusionFlareTexture;


        private Texture2D
            _illusionLightTexture;


        private bool
            _illusionTexturesPrepared;


        private async Task
            PrepareIllusionClassicAssetsAsync()
        {
            if (_illusionTexturesPrepared)
            {
                return;
            }


            await Task.WhenAll(
                TextureLoader.Instance.Prepare(
                    IllusionFlareTexturePath),

                TextureLoader.Instance.Prepare(
                    IllusionLightTexturePath));


            _illusionTexturesPrepared =
                true;
        }


        private void EnsureIllusionTextures()
        {
            if (!_illusionTexturesPrepared)
            {
                return;
            }


            if (_illusionFlareTexture == null ||
                _illusionFlareTexture.IsDisposed)
            {
                _illusionFlareTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            IllusionFlareTexturePath);
            }


            if (_illusionLightTexture == null ||
                _illusionLightTexture.IsDisposed)
            {
                _illusionLightTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            IllusionLightTexturePath);
            }
        }


        // ================================================================
        // CLASSIC EXTERNAL EFFECTS
        // ================================================================

        private void DrawIllusionClassicEffects(
            GameTime gameTime)
        {
            if (!Visible ||
                !IsIllusionWing ||
                Model == null ||
                BoneTransform == null)
            {
                return;
            }


            EnsureIllusionTextures();


            if (_illusionFlareTexture == null ||
                _illusionFlareTexture.IsDisposed ||
                _illusionLightTexture == null ||
                _illusionLightTexture.IsDisposed)
            {
                return;
            }


            float timeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;


            // ============================================================
            // Original:
            //
            // Scale =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.002f))
            //     *
            //     0.2f;
            //
            // WorldTime was milliseconds.
            // Therefore:
            //
            //     seconds * 2.0
            // ============================================================

            float scalePulse =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        2.0f))
                *
                0.2f;


            // ============================================================
            // Original:
            //
            // Luminosity =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.002f))
            //     *
            //     0.4f;
            // ============================================================

            float luminosity =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        2.0f))
                *
                0.4f;


            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;


            GraphicsDevice gd =
                GraphicsDevice;


            BlendState oldBlend =
                gd.BlendState;


            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    StormBrightAdditive,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead,
                    RasterizerState.CullNone))
            {
                // ========================================================
                // RED / PINK FLARES
                //
                // Classic:
                //
                // Vector(
                //     0.5 + Luminosity,
                //     Luminosity,
                //     Luminosity,
                //     Light);
                //
                // CreateSprite(
                //     BITMAP_FLARE,
                //     Position,
                //     Scale + 0.6,
                //     Light,
                //     o);
                // ========================================================

                Vector3 redLight =
                    new Vector3(
                        0.5f + luminosity,
                        luminosity,
                        luminosity);


                for (int i = 0;
                    i < IllusionRedFlareBones.Length;
                    i++)
                {
                    if (!TryGetIllusionBoneWorldPosition(
                            IllusionRedFlareBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _illusionFlareTexture,
                        position,
                        scalePulse + 0.6f,
                        redLight,
                        0.0f);
                }


                // ========================================================
                // GREEN LIGHTS
                //
                // Classic:
                //
                // Vector(
                //     Luminosity,
                //     0.5 + Luminosity,
                //     Luminosity,
                //     Light);
                //
                // Important:
                // scale is FIXED at 1.3.
                // ========================================================

                Vector3 greenLight =
                    new Vector3(
                        luminosity,
                        0.5f + luminosity,
                        luminosity);


                for (int i = 0;
                    i < IllusionGreenLightBones.Length;
                    i++)
                {
                    if (!TryGetIllusionBoneWorldPosition(
                            IllusionGreenLightBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _illusionLightTexture,
                        position,
                        1.3f,
                        greenLight,
                        0.0f);
                }


                // ========================================================
                // SMALL YELLOW HIGHLIGHT
                //
                // The original code does NOT calculate another Position
                // before this CreateSprite().
                //
                // Therefore it reuses the last position from the green
                // loop, which is bone 33.
                //
                // We make bone 33 explicit here.
                // ========================================================

                if (TryGetIllusionBoneWorldPosition(
                        33,
                        out Vector3 highlightPosition))
                {
                    // Original:
                    //
                    // fLumi =
                    //     (
                    //         sin(
                    //             WorldTime *
                    //             0.004f)
                    //         +
                    //         1.0f
                    //     )
                    //     *
                    //     0.05f;

                    float highlightLuminosity =
                        (
                            MathF.Sin(
                                timeSeconds *
                                4.0f)
                            +
                            1.0f
                        )
                        *
                        0.05f;


                    Vector3 highlightLight =
                        new Vector3(
                            0.8f +
                            highlightLuminosity,

                            0.8f +
                            highlightLuminosity,

                            0.3f +
                            highlightLuminosity);


                    // Classic CreateSprite rotation is expressed in degrees.
                    //
                    // The original passes 0.5f.
                    // SpriteBatch uses radians.
                    float rotation =
                        MathHelper.ToRadians(
                            0.5f);


                    DrawStormBillboard(
                        spriteBatch,
                        _illusionLightTexture,
                        highlightPosition,
                        0.4f,
                        highlightLight,
                        rotation);
                }
            }


            gd.BlendState =
                oldBlend;
        }


        // ================================================================
        // BONE -> WORLD
        // ================================================================

        private bool TryGetIllusionBoneWorldPosition(
            int bone,
            out Vector3 position)
        {
            position =
                Vector3.Zero;


            if (BoneTransform == null ||
                bone < 0 ||
                bone >= BoneTransform.Length)
            {
                return false;
            }


            position =
                Vector3.Transform(
                    BoneTransform[bone]
                        .Translation,

                    WorldPosition);


            return true;
        }
    }
}