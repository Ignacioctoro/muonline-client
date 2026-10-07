using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Adaptador OBJECT Sprite -> ClassicBillboardRenderer.
    ///
    /// Toda la rasterización común está centralizada en
    /// ClassicBillboardRenderer.
    /// </summary>
    internal sealed class ClassicSpriteRenderer
    {
        private readonly
            ClassicBillboardRenderer
            _billboards;

        private readonly
            ClassicTextureRepository
            _textures;

        public ClassicSpriteRenderer(
            ClassicBillboardRenderer billboards,
            ClassicTextureRepository textures)
        {
            _billboards =
                billboards ??
                throw new ArgumentNullException(
                    nameof(billboards));

            _textures =
                textures ??
                throw new ArgumentNullException(
                    nameof(textures));
        }

        public void Begin()
        {
            _billboards.Begin();
        }

        public void QueueSprite(
            in ClassicSprite sprite)
        {
            if (!_textures.TryGet(
                    sprite.Type,
                    out ClassicTextureResource resource))
            {
                return;
            }

            float width;
            float height;

            float u =
                0f;

            float v =
                0f;

            float uWidth =
                1f;

            float vHeight =
                1f;

            ClassicBlendMode blendMode;

            ClassicDepthMode depthMode;

            if (sprite.Type ==
                ClassicTextureIds.BitmapFormationMark)
            {
                width =
                    64f;

                height =
                    64f;

                GetFormationMarkUv(
                    sprite.SubType,
                    out u,
                    out v,
                    out uWidth,
                    out vHeight);

                blendMode =
                    ClassicBlendMode.AlphaTest;

                depthMode =
                    ClassicDepthMode.ReadWrite;
            }
            else
            {
                float renderScale =
                    sprite.AnimationFrame *
                    sprite.Scale;

                width =
                    resource.Data.Width *
                    renderScale;

                height =
                    resource.Data.Height *
                    renderScale;

                ResolveSpriteState(
                    sprite.SubType,
                    out blendMode,
                    out depthMode);
            }

            _billboards.Queue(
                resource,
                sprite.Position,
                width,
                height,
                sprite.Light,
                sprite.Rotation,
                blendMode,
                depthMode,
                u,
                v,
                uWidth,
                vHeight);
        }

        public void End()
        {
            _billboards.End();
        }

        private static void ResolveSpriteState(
            int subType,
            out ClassicBlendMode blendMode,
            out ClassicDepthMode depthMode)
        {
            switch (subType)
            {
                // EnableAlphaBlend()
                case 0:
                    blendMode =
                        ClassicBlendMode.Glow;

                    depthMode =
                        ClassicDepthMode.ReadOnly;

                    break;

                // EnableAlphaBlendMinus()
                case 1:
                    blendMode =
                        ClassicBlendMode.Subtract;

                    depthMode =
                        ClassicDepthMode.ReadOnly;

                    break;

                // EnableAlphaTest()
                case 2:
                    blendMode =
                        ClassicBlendMode.AlphaTest;

                    depthMode =
                        ClassicDepthMode.ReadWrite;

                    break;

                // EnableAlphaBlend2()
                case 3:
                    blendMode =
                        ClassicBlendMode.Luminance;

                    depthMode =
                        ClassicDepthMode.ReadOnly;

                    break;

                default:
                    blendMode =
                        ClassicBlendMode.Glow;

                    depthMode =
                        ClassicDepthMode.ReadOnly;

                    break;
            }
        }

        private static void GetFormationMarkUv(
            int subType,
            out float u,
            out float v,
            out float uWidth,
            out float vHeight)
        {
            u =
                0f;

            v =
                0f;

            uWidth =
                0.33f;

            vHeight =
                0.33f;

            switch (subType)
            {
                case 0:
                    u = 0f;
                    v = 0f;
                    break;

                case 1:
                    u = 0.33f;
                    v = 0f;
                    break;

                case 2:
                    u = 0.66f;
                    v = 0f;
                    break;

                case 3:
                    u = 0f;
                    v = 0.33f;
                    break;

                case 4:
                    u = 0.33f;
                    v = 0.33f;
                    break;

                case 5:
                    u = 0.66f;
                    v = 0.33f;
                    break;

                case 6:
                    u = 0f;
                    v = 0.66f;
                    break;

                case 7:
                    u = 0.33f;
                    v = 0.66f;
                    break;
            }
        }
    }
}