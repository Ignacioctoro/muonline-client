using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Adaptador PARTICLE -> ClassicBillboardRenderer.
    ///
    /// Esta primera versión implementa el camino genérico de:
    ///
    /// RenderParticles():
    ///
    /// Width  = Bitmap.Width  * Scale
    /// Height = Bitmap.Height * Scale
    ///
    /// RGB  -> EnableAlphaBlend()
    /// RGBA -> EnableAlphaTest(false)
    ///
    /// Los casos atlas/especiales se agregarán según el
    /// switch original de RenderParticles().
    /// </summary>
    internal sealed class ClassicParticleRenderer
    {
        private readonly
            ClassicBillboardRenderer
            _billboards;

        private readonly
            ClassicTextureRepository
            _textures;

        public ClassicParticleRenderer(
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

        public void QueueParticle(
            in ClassicParticle particle)
        {
            if (!_textures.TryGet(
                    particle.TexType,
                    out ClassicTextureResource resource))
            {
                return;
            }

            float width =
                resource.Data.Width *
                particle.Scale;

            float height =
                resource.Data.Height *
                particle.Scale;

            ClassicBlendMode blendMode;

            ClassicDepthMode depthMode;

            // RenderParticles() original:
            //
            // if (pBitmap->Components == 3)
            //     EnableAlphaBlend();
            // else
            //     EnableAlphaTest(false);
            //
            if (resource.Data.Components ==
                3)
            {
                blendMode =
                    ClassicBlendMode.Glow;

                depthMode =
                    ClassicDepthMode.ReadOnly;
            }
            else
            {
                blendMode =
                    ClassicBlendMode.AlphaTest;

                // EnableAlphaTest(false) no fuerza escritura de depth.
                //
                // Para esta primera etapa lo mantenemos read-only.
                // Los casos que alteran DepthTest se portarán
                // explícitamente desde RenderParticles().
                depthMode =
                    ClassicDepthMode.ReadOnly;
            }

            _billboards.Queue(
                resource,
                particle.Position,
                width,
                height,
                particle.Light,
                particle.Rotation,
                blendMode,
                depthMode);
        }

        public void End()
        {
            _billboards.End();
        }
    }
}