using Client.Main;
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
        // WING OF ETERNAL
        //
        // Item:
        //     12,37
        //
        // Model:
        //     Item/Wing09.bmd
        //
        // Classic visual effects:
        //
        //     6 x BITMAP_LIGHT
        //         pale blue / white
        //
        //     18 x BITMAP_LIGHT
        //         deep blue
        //
        //     4 x BITMAP_LIGHT
        //         smaller deep blue
        // ================================================================
        protected override float GetDynamicLightingAlphaCutoff()
        {
            if (IsEternalWing)
            {
                return 0.27f;
            }

            return base.GetDynamicLightingAlphaCutoff();
        }

        private const short EternalWingItemIndex =
            37;


        private const string EternalLightTexturePath =
            "Effect/flare01.jpg";


        private bool IsEternalWing =>
            ItemIndex ==
            EternalWingItemIndex;


        // ================================================================
        // CLASSIC BONE TABLES
        // ================================================================

        // Original:
        //
        // int iRedFlarePos[] =
        // {
        //     24, 31, 15, 8, 53, 35
        // };

        private static readonly int[]
            EternalMainLightBones =
        {
            24,
            31,
            15,
            8,
            53,
            35
        };


        // Original:
        //
        // int iGreenFlarePos[] =
        // {
        //     22,23,25,29,30,28,32,13,16,
        //     14,12,9,7,6,57,58,40,39
        // };

        private static readonly int[]
            EternalBlueLightBones =
        {
            22,
            23,
            25,
            29,
            30,
            28,
            32,
            13,
            16,
            14,
            12,
            9,
            7,
            6,
            57,
            58,
            40,
            39
        };


        // Original:
        //
        // int iGreenFlarePos2[] =
        // {
        //     56, 38, 51, 45
        // };

        private static readonly int[]
            EternalSmallLightBones =
        {
            56,
            38,
            51,
            45
        };


        // ================================================================
        // RESOURCES
        // ================================================================

        private Texture2D
            _eternalLightTexture;


        private bool
            _eternalTexturePrepared;


        private async Task
            PrepareEternalClassicAssetsAsync()
        {
            if (_eternalTexturePrepared)
            {
                return;
            }


            await TextureLoader.Instance.Prepare(
                EternalLightTexturePath);


            _eternalTexturePrepared =
                true;
        }


        private void EnsureEternalTexture()
        {
            if (!_eternalTexturePrepared)
            {
                return;
            }


            if (_eternalLightTexture == null ||
                _eternalLightTexture.IsDisposed)
            {
                _eternalLightTexture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            EternalLightTexturePath);
            }
        }


        // ================================================================
        // MODEL RENDERER
        // ================================================================

        // Eternal still uses the normal Neffis model renderer for now.
        //
        // We intentionally DO NOT intercept DrawModel() yet.
        // This means:
        //
        //     base.DrawModel(false)
        //     base.DrawModel(true)
        //
        // remain responsible for Wing09.bmd.
        //
        // We'll replace this only after inspecting the exact mesh recipe,
        // which will also let us fix the white texture fringe properly.

        private bool TryDrawEternalModel()
        {
            return false;
        }


        // ================================================================
        // CLASSIC EXTERNAL EFFECTS
        // ================================================================

        private void DrawEternalClassicEffects(
            GameTime gameTime)
        {
            if (!Visible ||
                !IsEternalWing ||
                Model == null ||
                BoneTransform == null)
            {
                return;
            }


            EnsureEternalTexture();


            if (_eternalLightTexture == null ||
                _eternalLightTexture.IsDisposed)
            {
                return;
            }


            float timeSeconds =
                (float)
                gameTime
                    .TotalGameTime
                    .TotalSeconds;


            // ------------------------------------------------------------
            // Original:
            //
            // Scale =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.003f))
            //     *
            //     0.2f;
            //
            // WorldTime in classic MU is milliseconds.
            // ------------------------------------------------------------

            float scalePulse =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        3.0f))
                *
                0.2f;


            // ------------------------------------------------------------
            // Original:
            //
            // Luminosity =
            //     abs(
            //         sin(
            //             WorldTime *
            //             0.003f))
            //     *
            //     0.3f;
            // ------------------------------------------------------------

            float luminosity =
                MathF.Abs(
                    MathF.Sin(
                        timeSeconds *
                        3.0f))
                *
                0.3f;


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
                // GROUP 1
                //
                // Original:
                //
                // Vector(
                //     0.5 + Luminosity,
                //     0.5 + Luminosity,
                //     0.6 + Luminosity,
                //     Light);
                //
                // CreateSprite(
                //     BITMAP_LIGHT,
                //     Position,
                //     Scale + 1.3,
                //     Light,
                //     o);
                // ========================================================

                Vector3 mainLight =
                    new Vector3(
                        0.5f + luminosity,
                        0.5f + luminosity,
                        0.6f + luminosity);


                for (int i = 0;
                    i < EternalMainLightBones.Length;
                    i++)
                {
                    if (!TryGetEternalBoneWorldPosition(
                            EternalMainLightBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _eternalLightTexture,
                        position,
                        scalePulse + 1.3f,
                        mainLight,
                        0.0f);
                }


                // ========================================================
                // GROUP 2
                //
                // Original:
                //
                // Vector(
                //     0.1,
                //     0.1,
                //     0.9,
                //     Light);
                //
                // CreateSprite(
                //     BITMAP_LIGHT,
                //     Position,
                //     Scale + 1.5,
                //     Light,
                //     o);
                // ========================================================

                Vector3 blueLight =
                    new Vector3(
                        0.1f,
                        0.1f,
                        0.9f);


                for (int i = 0;
                    i < EternalBlueLightBones.Length;
                    i++)
                {
                    if (!TryGetEternalBoneWorldPosition(
                            EternalBlueLightBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _eternalLightTexture,
                        position,
                        scalePulse + 1.5f,
                        blueLight,
                        0.0f);
                }


                // ========================================================
                // GROUP 3
                //
                // Same blue color, but much smaller.
                //
                // Original:
                //
                // CreateSprite(
                //     BITMAP_LIGHT,
                //     Position,
                //     Scale + 0.5,
                //     Light,
                //     o);
                // ========================================================

                for (int i = 0;
                    i < EternalSmallLightBones.Length;
                    i++)
                {
                    if (!TryGetEternalBoneWorldPosition(
                            EternalSmallLightBones[i],
                            out Vector3 position))
                    {
                        continue;
                    }


                    DrawStormBillboard(
                        spriteBatch,
                        _eternalLightTexture,
                        position,
                        scalePulse + 0.5f,
                        blueLight,
                        0.0f);
                }
            }


            gd.BlendState =
                oldBlend;
        }


        // ================================================================
        // BONE -> WORLD
        // ================================================================

        private bool TryGetEternalBoneWorldPosition(
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