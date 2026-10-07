using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Modos de blending utilizados por el Main clásico.
    /// </summary>
    internal enum ClassicBlendMode : byte
    {
        Glow = 0,

        Subtract,

        AlphaTest,

        Luminance,

        Alpha
    }

    /// <summary>
    /// Separamos profundidad de blending porque en el Main
    /// son estados independientes.
    /// </summary>
    internal enum ClassicDepthMode : byte
    {
        ReadOnly = 0,

        ReadWrite,

        Disabled
    }

    internal static class ClassicRenderStates
    {
        /// <summary>
        /// EnableAlphaBlend()
        ///
        /// GL_ONE,
        /// GL_ONE
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
        /// GL_ZERO,
        /// GL_ONE_MINUS_SRC_COLOR
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
        /// GL_ONE_MINUS_SRC_COLOR,
        /// GL_ONE
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
        /// Blend alpha clásico.
        ///
        /// GL_SRC_ALPHA,
        /// GL_ONE_MINUS_SRC_ALPHA
        /// </summary>
        public static readonly BlendState Alpha =
            BlendState.NonPremultiplied;

        public static BlendState GetBlendState(
            ClassicBlendMode mode)
        {
            return mode switch
            {
                ClassicBlendMode.Glow =>
                    Glow,

                ClassicBlendMode.Subtract =>
                    Subtract,

                ClassicBlendMode.Luminance =>
                    Luminance,

                ClassicBlendMode.Alpha =>
                    Alpha,

                ClassicBlendMode.AlphaTest =>
                    Alpha,

                _ =>
                    Glow
            };
        }

        public static DepthStencilState GetDepthState(
            ClassicDepthMode mode)
        {
            return mode switch
            {
                ClassicDepthMode.ReadOnly =>
                    DepthStencilState.DepthRead,

                ClassicDepthMode.ReadWrite =>
                    DepthStencilState.Default,

                ClassicDepthMode.Disabled =>
                    DepthStencilState.None,

                _ =>
                    DepthStencilState.DepthRead
            };
        }
    }
}