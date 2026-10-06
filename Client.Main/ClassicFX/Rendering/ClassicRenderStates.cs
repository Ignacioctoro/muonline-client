using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Rendering
{
    internal enum ClassicSpriteBlendMode : byte
    {
        Glow = 0,

        Subtract,

        AlphaTest,

        Luminance
    }

    /// <summary>
    /// Traducción directa de los estados OpenGL
    /// utilizados por ZzzOpenglUtil.
    ///
    /// No son aproximaciones visuales.
    /// </summary>
    internal static class ClassicRenderStates
    {
        /// <summary>
        /// EnableAlphaBlend()
        ///
        /// Main:
        ///     GL_ONE,
        ///     GL_ONE
        /// </summary>
        public static readonly BlendState Glow =
            new()
            {
                ColorSourceBlend =
                    Blend.One,

                ColorDestinationBlend =
                    Blend.One,

                AlphaSourceBlend =
                    Blend.One,

                AlphaDestinationBlend =
                    Blend.One
            };

        /// <summary>
        /// EnableAlphaBlendMinus()
        ///
        /// Main:
        ///     GL_ZERO,
        ///     GL_ONE_MINUS_SRC_COLOR
        /// </summary>
        public static readonly BlendState Subtract =
            new()
            {
                ColorSourceBlend =
                    Blend.Zero,

                ColorDestinationBlend =
                    Blend.InverseSourceColor,

                AlphaSourceBlend =
                    Blend.Zero,

                AlphaDestinationBlend =
                    Blend.InverseSourceAlpha
            };

        /// <summary>
        /// EnableAlphaBlend2()
        ///
        /// Main:
        ///     GL_ONE_MINUS_SRC_COLOR,
        ///     GL_ONE
        /// </summary>
        public static readonly BlendState Luminance =
            new()
            {
                ColorSourceBlend =
                    Blend.InverseSourceColor,

                ColorDestinationBlend =
                    Blend.One,

                AlphaSourceBlend =
                    Blend.InverseSourceAlpha,

                AlphaDestinationBlend =
                    Blend.One
            };

        /// <summary>
        /// EnableAlphaTest()
        ///
        /// Para color usamos el comportamiento
        /// no-premultiplicado equivalente a:
        ///
        /// SRC_ALPHA,
        /// ONE_MINUS_SRC_ALPHA
        ///
        /// El descarte por alpha lo hará AlphaTestEffect.
        /// </summary>
        public static readonly BlendState AlphaTest =
            BlendState.NonPremultiplied;

        public static readonly DepthStencilState
            DepthReadOnly =
                DepthStencilState.DepthRead;

        public static readonly DepthStencilState
            DepthWrite =
                DepthStencilState.Default;

        public static BlendState GetBlendState(
            ClassicSpriteBlendMode mode)
        {
            return mode switch
            {
                ClassicSpriteBlendMode.Glow =>
                    Glow,

                ClassicSpriteBlendMode.Subtract =>
                    Subtract,

                ClassicSpriteBlendMode.AlphaTest =>
                    AlphaTest,

                ClassicSpriteBlendMode.Luminance =>
                    Luminance,

                _ =>
                    Glow
            };
        }

        public static DepthStencilState GetDepthState(
            ClassicSpriteBlendMode mode)
        {
            return
                mode ==
                ClassicSpriteBlendMode.AlphaTest

                    ? DepthWrite
                    : DepthReadOnly;
        }
    }
}