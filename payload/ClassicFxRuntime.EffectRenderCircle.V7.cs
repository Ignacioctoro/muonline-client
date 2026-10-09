// ClassicFX Effect V7 — RenderCircle / BITMAP_MAGIC+2.
// Original: MuMain ZzzEffectMagicSkill.cpp RenderCircle() and
//           ZzzEffect.cpp CreateEffect(BITMAP_MAGIC+2) / RenderEffects().
// Shared, native 12-quad circular mantle. Reuses the ClassicBillboardRenderer
// gradient batching pipeline (already used by Blur) — no new GPU buffers.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Port of native RenderCircle(int Type, vec3_t ObjectPosition,
        /// ScaleBottom, ScaleTop, Height, Rotation, LightTop, TextureV).
        /// Must be called between _billboardRenderer.Begin() and End().
        /// Two light levels: white at bottom, LightTop at the upper edge.
        /// Input world coordinates follow MU's X/Y-horizontal, Z-up convention.
        /// </summary>
        private void QueueClassicRenderCircle(
            ClassicTextureResource texture,
            Vector3 center,
            float scaleBottom,
            float scaleTop,
            float height,
            float rotationDegrees = 0f,
            float lightTop = 1f,
            float textureV = 0f)
        {
            if (_billboardRenderer == null || texture == null ||
                !texture.IsReady || scaleBottom <= 0f || scaleTop <= 0f)
                return;

            // MuMain: Num=12, x=0..11, angle=x*30+Rotation.
            // Native AngleIMatrix(angleZ) applied to (0,r,z) yields
            // (sin(angleZ)*r, cos(angleZ)*r, z), hence positive sin here.
            const int segments = 12;
            const float stepDegrees = 360f / segments;
            Vector3 bottomLight = Vector3.One;
            Vector3 upperLight = new Vector3(MathHelper.Clamp(lightTop, 0f, 1f));

            for (int segment = 0; segment < segments; ++segment)
            {
                float angle0 = MathHelper.ToRadians(
                    segment * stepDegrees + rotationDegrees);
                float angle1 = MathHelper.ToRadians(
                    (segment + 1) * stepDegrees + rotationDegrees);

                float sin0 = MathF.Sin(angle0), cos0 = MathF.Cos(angle0);
                float sin1 = MathF.Sin(angle1), cos1 = MathF.Cos(angle1);

                Vector3 p0 = center + new Vector3(
                    sin0 * scaleBottom, cos0 * scaleBottom, 0f);
                Vector3 p1 = center + new Vector3(
                    sin1 * scaleBottom, cos1 * scaleBottom, 0f);
                Vector3 p2 = center + new Vector3(
                    sin1 * scaleTop, cos1 * scaleTop, height);
                Vector3 p3 = center + new Vector3(
                    sin0 * scaleTop, cos0 * scaleTop, height);

                float u0 = (float)segment / segments;
                float u1 = (float)(segment + 1) / segments;
                _billboardRenderer.QueueWorldQuadGradient(
                    texture, p0, p1, p2, p3,
                    new Vector2(u0, 1f + textureV),
                    new Vector2(u1, 1f + textureV),
                    new Vector2(u1, textureV),
                    new Vector2(u0, textureV),
                    bottomLight, upperLight,
                    ClassicBlendMode.Glow, ClassicDepthMode.ReadOnly);
            }
        }

        /// <summary>
        /// Native BITMAP_MAGIC+2. Shares its creation table with MAGIC+1
        /// (see TryGetMagicGround2Definition in V6), but renders differently.
        /// The original has no standalone MoveHandlers entry for +2.
        /// </summary>
        private void RenderMagicCircleGround(ref EffectState effect)
        {
            // Native starts by drawing two opposing cylinders for every
            // subtype except 2. Each has bottomRadius=90, topRadius=130,
            // height=200, lightTop=0. Both are world-space 3D.
            if (effect.SubType != 2 &&
                Textures.TryGet(ClassicTextureIds.BitmapMagic + 2,
                    out ClassicTextureResource ringTexture))
            {
                // Native: (int)WorldTime % 3600 / 10.f — degrees.
                float rotation = (float)(
                    ((long)Clock.WorldTimeMilliseconds % 3600L) / 10.0);
                QueueClassicRenderCircle(ringTexture, effect.Position,
                    90f, 130f, 200f, rotation, lightTop: 0f);
                QueueClassicRenderCircle(ringTexture, effect.Position,
                    90f, 130f, 200f, -rotation, lightTop: 0f);
            }

            // The native effect also draws a BITMAP_MAGIC+1 floor layer.
            // This stays independent: missing the ring texture must never
            // suppress the ground visual (and vice versa).
            if (!Textures.TryGet(ClassicTextureIds.BitmapMagic + 1,
                    out ClassicTextureResource groundTexture))
                return;

            float lum = 1f;
            float size;
            if (effect.SubType == 2)
            {
                if (effect.LifeTime > 10f)
                    size = (20f - effect.LifeTime) * 0.55f;
                else
                {
                    lum -= (10f - effect.LifeTime) * 0.1f;
                    // Native leaves the local Scale uninitialized in the
                    // last 10 frames. Hold the last intended size at 5.5;
                    // avoids undefined data, preserving the fade phase.
                    size = 5.5f;
                }
            }
            else
            {
                if (effect.LifeTime < 5f)
                    lum -= (5f - effect.LifeTime) * 0.2f;
                size = (20f - effect.LifeTime) * 0.15f;
            }

            if (size <= 0f || lum <= 0f)
                return;

            // Native: Vector(Luminosity, Luminosity*0.4, Luminosity*0.2)
            Vector3 light = new Vector3(lum, lum * 0.4f, lum * 0.2f);
            QueueTerrainEffect(ref effect, groundTexture,
                scaleOverride: size, lightOverride: light);
        }
    }
}
