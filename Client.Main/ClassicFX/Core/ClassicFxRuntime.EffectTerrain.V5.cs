// ClassicFX terrain effect families V5 (MuMain native CreateEffect/MoveEffect/RenderEffects).
// BITMAP_MAGIC_ZIN (0/1/2), BITMAP_LIGHTNING+1 (0/1),
// BITMAP_MAGIC (1/8/12/13/14).
// Uses the shared terrain tessellator in ClassicFxRuntime.Effects.cs.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct V5TerrainEffectDefinition
        {
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly bool UseCallerScale;
            public readonly float Alpha;

            public V5TerrainEffectDefinition(float life, float size,
                bool callerScale = false, float alpha = 1f)
            {
                LifeTime = life;
                Scale = size;
                UseCallerScale = callerScale;
                Alpha = alpha;
            }
        }

        private static bool IsV5TerrainEffectType(ClassicFxEffectType type) =>
            type == ClassicFxEffectType.MagicZin ||
            type == ClassicFxEffectType.LightningGround ||
            type == ClassicFxEffectType.MagicGround;

        private static bool TryGetV5TerrainDefinition(
            ClassicFxEffectType type, int subType,
            out V5TerrainEffectDefinition definition)
        {
            definition = default;
            // MuMain EffectTypes.json + ZzzEffect.cpp CreateEffect().
            switch (type)
            {
                case ClassicFxEffectType.MagicZin:
                    switch (subType)
                    {
                        case 0: definition = new(50f, 1f, callerScale: true); return true;
                        case 1: definition = new(40f, 1f, callerScale: true, alpha: 0f); return true;
                        case 2: definition = new(30f, 1f, callerScale: true); return true;
                    }
                    break;
                case ClassicFxEffectType.LightningGround:
                    switch (subType)
                    {
                        case 0: definition = new(10f, 1.5f); return true;
                        case 1: definition = new(50f, 1f, callerScale: true, alpha: 0.01f); return true;
                    }
                    break;
                case ClassicFxEffectType.MagicGround:
                    switch (subType)
                    {
                        case 1: definition = new(20f, 0.5f); return true;
                        case 8: definition = new(30f, 1f); return true;
                        case 12: definition = new(20f, 0.1f, callerScale: true); return true;
                        case 13:
                        case 14: definition = new(30f, 1f); return true;
                    }
                    break;
            }
            return false;
        }

        // Return false only for a condition that kills the native effect.
        private bool MoveV5TerrainEffect(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.MagicZin:
                    switch (e.SubType)
                    {
                        case 0:
                            if (e.LifeTime < 20f) e.Alpha -= 0.05f * f;
                            else if (e.Alpha < 1f) e.Alpha += 0.05f * f;
                            break;
                        case 1:
                            if (e.LifeTime < 20f) e.Alpha -= 0.03f * f;
                            else if (e.Alpha < 0.7f) e.Alpha += 0.06f * f;
                            break;
                        case 2:
                            if (e.Scale < 3.5f) e.Scale += 0.1f * f;
                            if (e.LifeTime < 20f) e.Alpha -= 0.05f * f;
                            else if (e.Alpha < 1f) e.Alpha += 0.05f * f;
                            break;
                    }
                    break;

                case ClassicFxEffectType.LightningGround:
                    if (e.SubType == 0)
                    {
                        float lum = e.LifeTime * 0.2f;
                        Vector3 light = new Vector3(lum * 0.2f, lum * 0.5f, lum);
                        AddClassicTerrainLight(e.Position.X, e.Position.Y, light, 2f);
                    }
                    else if (e.SubType == 1)
                        e.Alpha = MathF.Min(1f, e.Alpha + 0.01f * f);
                    break;

                case ClassicFxEffectType.MagicGround:
                    switch (e.SubType)
                    {
                        // Subtype 1 mutates on the *render* side in native RenderEffects.
                        case 8:
                            e.Scale += 1.8f * f;
                            break;
                        case 12:
                            e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
                            e.Scale += e.Alpha * f;
                            if (e.Scale > 5f)
                                e.Light *= MathF.Pow(0.5f, f);
                            break;
                        case 13:
                        case 14:
                            e.Scale *= MathF.Pow(1.1f, f);
                            // Render is a separate pass, but the original movement
                            // multiplies Light by 0.95 on each reference frame.
                            e.Light *= MathF.Pow(0.95f, f);
                            if (e.Scale > 8f)
                                return false;
                            if (e.Scale > 4f)
                                e.Light *= MathF.Pow(0.5f, f);
                            break;
                    }
                    break;
            }
            return true;
        }

        private void RenderV5TerrainEffect(ref EffectState e)
        {
            int bitmap = e.Type switch
            {
                ClassicFxEffectType.MagicZin => ClassicTextureIds.BitmapMagicZin,
                ClassicFxEffectType.LightningGround => ClassicTextureIds.BitmapLightning + 1,
                ClassicFxEffectType.MagicGround => ClassicTextureIds.BitmapMagic,
                _ => -1
            };
            if (bitmap < 0 || e.Scale <= 0f ||
                !Textures.TryGet(bitmap, out ClassicTextureResource texture))
                return;

            switch (e.Type)
            {
                case ClassicFxEffectType.MagicZin:
                {
                    float coefficient = e.SubType switch
                    {
                        0 => 2f,
                        1 => 1f / 2.5f,
                        _ => 1f
                    };
                    // Native uses HeadAngle[1], not the effect's Angle[2].
                    // None of the three native CreateEffect variants assigns
                    // HeadAngle; their native initial rotation is zero.
                    QueueTerrainEffect(ref e, texture,
                        lightOverride: e.Light * e.Alpha * coefficient,
                        angleZOverride: 0f);
                    break;
                }
                case ClassicFxEffectType.LightningGround:
                {
                    // Native RenderEffects increments Scale immediately before drawing.
                    e.Scale += 0.2f * Clock.FrameFactor;
                    float lum = e.LifeTime * 0.1f;
                    QueueTerrainEffect(ref e, texture,
                        lightOverride: new Vector3(lum));
                    break;
                }
                case ClassicFxEffectType.MagicGround:
                {
                    if (e.SubType == 8)
                    {
                        QueueTerrainEffect(ref e, texture);
                        QueueTerrainEffect(ref e, texture, scaleOverride: e.Scale * 0.8f);
                        QueueTerrainEffect(ref e, texture, scaleOverride: e.Scale * 1.2f);
                    }
                    else if (e.SubType == 12 || e.SubType == 13 || e.SubType == 14)
                    {
                        QueueTerrainEffect(ref e, texture);
                        QueueTerrainEffect(ref e, texture);
                    }
                    else if (e.SubType == 1)
                    {
                        QueueTerrainEffect(ref e, texture);
                        // Native mutates this subtype in RenderEffects, not MoveEffect.
                        float f = Clock.FrameFactor;
                        e.Light *= MathF.Pow(1f / 1.1f, f);
                        e.Scale += 0.05f * f;
                    }
                    break;
                }
            }
        }
    }
}
