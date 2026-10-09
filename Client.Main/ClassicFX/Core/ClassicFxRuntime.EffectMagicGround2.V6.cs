// ClassicFX Effect V6: MuMain BITMAP_MAGIC + 1 (NOT BITMAP_MAGIC + 2).
// References (sven-n/MuMain):
//   ZzzEffect.cpp CreateEffect() case BITMAP_MAGIC + 1/+2
//   ZzzEffect.cpp RenderEffects() case BITMAP_MAGIC + 1
//   ZzzOpenData.cpp BITMAP_MAGIC+1 -> Effect/Magic_Ground2.jpg
// Frame-based child emissions are dispatched once per crossed native tick,
// preventing duplicate children at 60/120Hz while preserving native cadence.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Client.Main.Controllers;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct MagicGround2Definition
        {
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly bool UseCallerScale;
            public readonly bool RandomScale;
            public readonly bool RandomAngle;

            public MagicGround2Definition(float lifeTime, float scale = 1f,
                bool useCallerScale = true, bool randomScale = false,
                bool randomAngle = false)
            {
                LifeTime = lifeTime;
                Scale = scale;
                UseCallerScale = useCallerScale;
                RandomScale = randomScale;
                RandomAngle = randomAngle;
            }
        }

        private static bool TryGetMagicGround2Definition(int subType,
            out MagicGround2Definition definition)
        {
            definition = default;
            // Source explicitly initializes BITMAP_MAGIC+1 and +2 together.
            // This block deliberately supports +1 only: +2 uses RenderCircle,
            // a separate 3D geometry primitive that is not terrain tessellation.
            switch (subType)
            {
                case 0: case 1: case 2: case 3: case 5:
                case 8: case 11: case 12:
                    definition = new MagicGround2Definition(20f);
                    return true;
                case 4: case 10:
                    definition = new MagicGround2Definition(40f, randomScale: true);
                    return true;
                case 6:
                    definition = new MagicGround2Definition(60f, randomScale: true);
                    return true;
                case 7:
                    definition = new MagicGround2Definition(40f, randomAngle: true);
                    return true;
                case 9:
                    definition = new MagicGround2Definition(10f, 0.1f, useCallerScale: false);
                    return true;
                case 13:
                    definition = new MagicGround2Definition(40f);
                    return true;
                default:
                    return false;
            }
        }

        // Used only at CreateEffect(), where random initializers belong.
        private float InitializeMagicGround2Scale(in MagicGround2Definition definition,
            float callerScale)
        {
            if (definition.RandomScale)
                return (Random.Modulo(50) + 50) * 0.01f * 4f;
            return definition.UseCallerScale
                ? callerScale * definition.Scale
                : definition.Scale;
        }

        private void MoveMagicGround2(ref EffectState effect, float f)
        {
            // Native MoveHandlers has no dedicated BITMAP_MAGIC+1 case.
            // Subtype 7 follows Owner and rotates in native RenderEffects();
            // child spawns and color/scale calculations are also in Render.
            // The shared pool accounts for LifeTime on every 25-FPS frame.
        }

        private void RenderMagicGround2(ref EffectState effect)
        {
            // In native RenderEffects() type +1 spawns subtype 7/9 children
            // rather than drawing for subtype 6/8. Emissions must remain
            // independent of the texture load state.
            if (effect.SubType == 6 || effect.SubType == 8)
            {
                int nativeTick = (int)effect.LifeTime;
                bool shouldSpawn = effect.SubType == 6
                    ? nativeTick % 25 == 0
                    : nativeTick % 2 == 0;
                if (nativeTick >= 0 && shouldSpawn &&
                    nativeTick != effect.LastChildNativeTick)
                {
                    effect.LastChildNativeTick = nativeTick;
                    CreateEffect(ClassicFxEffectType.MagicGround2,
                        effect.Position, effect.Angle, effect.Light, effect.Owner,
                        subType: effect.SubType == 6 ? 7 : 9);
                }
                return;
            }

            if (effect.SubType == 5)
                return; // Native branch has no drawing or child emissions.

            if (!Textures.TryGet(ClassicTextureIds.BitmapMagic + 1,
                    out ClassicTextureResource texture))
                return;

            float lum = 1f;
            if (effect.LifeTime < 5f)
                lum -= (5f - effect.LifeTime) * 0.2f;
            else if (effect.SubType == 7 && effect.LifeTime > 30f)
                lum = (40f - effect.LifeTime) * 0.1f;

            float size;
            if (effect.SubType == 4 || effect.SubType == 10)
            {
                size = effect.Scale;
                if (lum == 1f)
                    lum = MathF.Sin((60f - effect.LifeTime) * 0.05f) + 0.5f;
            }
            else if (effect.SubType == 7)
                size = effect.LifeTime * 0.07f;
            else if (effect.SubType == 9 || effect.SubType == 13)
                size = effect.Scale;
            else
                size = (20f - effect.LifeTime) * 0.15f;

            if (effect.SubType == 7)
            {
                // Original BITMAP_MAGIC+1:7 reads Owner->Position every render.
                var owner = effect.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World) ||
                    owner.Status == GameControlStatus.Disposed ||
                    owner.Status == GameControlStatus.Error)
                {
                    effect.LifeTime = 0f; // shared Update() releases next tick
                    return;
                }
                effect.Position = owner.WorldPosition.Translation;
                effect.Angle.Z += 10f * Clock.FrameFactor; // original degrees
            }

            // Type 13 modifies persistent Light during rendering in MuMain.
            if (effect.SubType == 13)
                effect.Light *= MathF.Pow(1f / 1.05f, Clock.FrameFactor);

            Vector3 light;
            switch (effect.SubType)
            {
                case 0: case 1:
                    light = new Vector3(0.4f, 0.6f, 1f) * lum;
                    break;
                case 2:
                    light = new Vector3(0.4f, 1f, 0.6f) * lum;
                    break;
                case 3:
                    light = new Vector3(1f, 0.6f, 0.4f) * lum;
                    break;
                case 4:
                    light = new Vector3(1f, 0.5f, 0.1f) * lum;
                    break;
                case 7: case 9:
                    light = new Vector3(0.9f, 0.4f, 0.2f) * lum;
                    break;
                case 10:
                    light = new Vector3(0.1f, 0.5f, 1f) * lum;
                    break;
                case 11: case 12:
                    light = effect.Light * lum;
                    break;
                case 13:
                    light = effect.Light;
                    break;
                default:
                    return;
            }

            if (size <= 0f)
                return;

            QueueTerrainEffect(ref effect, texture,
                scaleOverride: size, lightOverride: light,
                blendOverride: effect.SubType == 12
                    ? ClassicBlendMode.Subtract : ClassicBlendMode.Glow);
        }
    }
}
