// BroyalMU ClassicFX Season 6 — Batch 14.
// Exact native type/subtype family from MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// MODEL_FENRIR_FOOT_THUNDER, MODEL_TWINTAIL_EFFECT, BITMAP_CLOUD.
// ZzzEffect.cpp CreateEffect/RenderEffects; MoveHandlers.cpp; ZzzOpenData.cpp.
// Only logical terrain effects. Drawn by ClassicFX's existing terrain batcher.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch14TerrainType(
            ClassicFxEffectType type, int subType) =>
            (type == ClassicFxEffectType.FenrirFootThunder &&
             subType is >= 0 and <= 4) ||
            (type == ClassicFxEffectType.TwinTail &&
             subType is >= 0 and <= 2) ||
            (type == ClassicFxEffectType.CloudGround && subType == 0);

        private void InitializeS6Batch14Terrain(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle,
            ref float scale, ref float alpha, out float life)
        {
            alpha = 1f;

            if (type == ClassicFxEffectType.FenrirFootThunder)
            {
                // MODEL_FENRIR_FOOT_THUNDER CreateEffect():
                // LifeTime=200, initial frame=0, Position.Z=0.
                life = 200f;
                position.Z = 0f;
                return;
            }

            if (type == ClassicFxEffectType.TwinTail)
            {
                // MODEL_TWINTAIL_EFFECT:
                // 0: pulsating Spark+1, 200 ticks;
                // 1,2: rotating Cloud plane, 50 ticks, Scale=3.5.
                life = subType == 0 ? 200f : 50f;
                position.Z = 0f;
                if (subType is 1 or 2)
                {
                    scale = 3.5f;
                    angle.X = 0f;
                }
                return;
            }

            // BITMAP_CLOUD subType 0: fades over 60 ticks while expanding.
            life = 60f;
            angle.Z = Random.Modulo(360); // native terrain angle: degrees
        }

        private bool MoveS6Batch14Terrain(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.FenrirFootThunder:
                {
                    // Original light channel fades are subtype-specific.
                    e.Angle.X += MathHelper.ToRadians(0.1f * f);
                    float fade = 0.05f * f;
                    switch (e.SubType)
                    {
                        case 1: e.Light.Y -= fade; e.Light.Z -= fade; break;
                        case 2: e.Light.X -= fade; e.Light.Y -= fade; break;
                        case 3: e.Light.X -= fade; e.Light.Z -= fade; break;
                        case 4: e.Light.Z -= fade; break;
                    }

                    e.Alpha -= fade;
                    if (e.Alpha <= 0f)
                        return false;

                    // Native timed bitmap advances every ~200 ms,
                    // and retires after frame index exceeds 4.
                    e.Phase += f;
                    while (e.Phase >= 5f)
                    {
                        e.Phase -= 5f;
                        ++e.NativeAnimationFrame;
                        if (e.NativeAnimationFrame > 4)
                            return false;
                    }
                    return true;
                }

                case ClassicFxEffectType.TwinTail:
                {
                    e.Alpha -= 0.01f * f;
                    if (e.Alpha <= 0f)
                        return false;

                    float decay = e.SubType == 0 ? 1.02f : 1.04f;
                    e.Light *= MathF.Pow(1f / decay, f);
                    if (e.SubType == 0)
                    {
                        // Main toggles shrink/expand every 1000 ms.
                        e.Phase += f;
                        while (e.Phase >= 25f)
                        {
                            e.Phase -= 25f;
                            e.NativeAnimationFrame ^= 1;
                        }
                        if (e.NativeAnimationFrame == 0)
                            e.Scale = MathF.Max(0f, e.Scale - 0.015f * f);
                        else
                            e.Scale = MathF.Min(1.2f, e.Scale + 0.02f * f);
                    }
                    else
                    {
                        // Native o->Angle[0] is DEGREES of ground rotation,
                        // and the renderer passes it to RenderTerrainAlphaBitmap.
                        float speed = e.SubType == 1 ? 0.3f : 0.1f;
                        e.Angle.X = -(float)(Clock.WorldTimeMilliseconds * speed);
                        e.Scale -= 0.02f * f;
                    }
                    return true;
                }

                case ClassicFxEffectType.CloudGround:
                    e.Light *= MathF.Pow(1f / 1.05f, f);
                    e.Scale += 0.03f * f;
                    return true;

                default:
                    return false;
            }
        }

        private void RenderS6Batch14Terrain(ref EffectState e)
        {
            int textureId = e.Type switch
            {
                ClassicFxEffectType.FenrirFootThunder =>
                    ClassicTextureIds.BitmapFenrirFootThunder1 +
                    (e.NativeAnimationFrame % 5),
                ClassicFxEffectType.TwinTail =>
                    e.SubType == 0
                        ? ClassicTextureIds.BitmapSpark + 1
                        : ClassicTextureIds.BitmapCloud,
                ClassicFxEffectType.CloudGround =>
                    ClassicTextureIds.BitmapCloud,
                _ => -1
            };

            if (textureId < 0 ||
                !Textures.TryGet(textureId,
                    out ClassicTextureResource texture))
                return;

            if (e.Type == ClassicFxEffectType.FenrirFootThunder)
            {
                // Native frame atlas is five independent textures, not
                // five new model objects. Native alpha is not a render arg
                // for this type; only Light tints the plane.
                QueueTerrainEffect(ref e, texture, scaleOverride: 0.6f,
                    angleZOverride: 0f);
            }
            else if (e.Type == ClassicFxEffectType.TwinTail)
            {
                // Original RenderTerrainAlphaBitmap passes alpha directly;
                // existing renderer takes the resulting premultiplied light.
                QueueTerrainEffect(ref e, texture,
                    lightOverride: e.Light * MathHelper.Clamp(e.Alpha, 0f, 1f),
                    angleZOverride: e.SubType == 0 ? 0f : e.Angle.X);
            }
            else
            {
                QueueTerrainEffect(ref e, texture,
                    angleZOverride: e.Angle.Z);
            }
        }
    }
}
