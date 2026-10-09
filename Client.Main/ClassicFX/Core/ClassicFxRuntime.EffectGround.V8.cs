// ClassicFX V8 — native BITMAP_SHOCK_WAVE subtypes 0..13 and
// BITMAP_TWLIGHT subtypes 0..2. Existing Wizardry (14 / 3) untouched.
// MuMain: ZzzEffect.cpp CreateEffect, MoveHandlers.cpp
// Move_BITMAP_SHOCK_WAVE / Move_BITMAP_TWLIGHT.
using System;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct V8GroundEffectDefinition
        {
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly int RandomScaleCount;
            public readonly float RandomScaleOffset;
            public readonly float RandomScaleDivisor;
            public readonly bool UseCallerScale;
            public readonly bool RequiresOwner;
            public readonly float LightMultiplier;

            public V8GroundEffectDefinition(float lifeTime, float scale,
                int randomScaleCount = 0, float randomScaleOffset = 0f,
                float randomScaleDivisor = 1f, bool useCallerScale = false,
                bool requiresOwner = false, float lightMultiplier = 1f)
            {
                LifeTime = lifeTime;
                Scale = scale;
                RandomScaleCount = randomScaleCount;
                RandomScaleOffset = randomScaleOffset;
                RandomScaleDivisor = randomScaleDivisor;
                UseCallerScale = useCallerScale;
                RequiresOwner = requiresOwner;
                LightMultiplier = lightMultiplier;
            }
        }

        private static bool IsV8GroundEffectType(ClassicFxEffectType type, int subType) =>
            (type == ClassicFxEffectType.ShockWave && subType >= 0 && subType <= 13) ||
            (type == ClassicFxEffectType.Twlight && subType >= 0 && subType <= 2);

        private static bool TryGetV8GroundEffectDefinition(
            ClassicFxEffectType type, int subType,
            out V8GroundEffectDefinition definition)
        {
            definition = default;
            if (type == ClassicFxEffectType.Twlight)
            {
                if (subType < 0 || subType > 2)
                    return false;
                definition = new V8GroundEffectDefinition(80f, 8f);
                return true;
            }
            if (type != ClassicFxEffectType.ShockWave)
                return false;

            switch (subType)
            {
                case 0: definition = new(30f, 20f); break;
                case 1: definition = new(20f, 0f, 10, 10f, 10f); break;
                case 2: definition = new(20f, 0f, 10, 10f, 10f); break;
                case 3: definition = new(15f, 1f); break;
                case 4: definition = new(10f, 0f, 6, 6f, 10f); break;
                case 5: definition = new(20f, 9f); break;
                case 6: definition = new(50f, 9f); break;
                case 7: definition = new(10f, 1f); break;
                case 8: definition = new(10f, 0f, 10, 10f, 5f); break;
                case 9: definition = new(5f, 5f, requiresOwner: true); break;
                case 10: definition = new(1f, 2f,
                    requiresOwner: true, lightMultiplier: 2.3f); break;
                case 11: definition = new(20f, 0f, 10, 10f, 10f); break;
                case 12: definition = new(10f, 1f, useCallerScale: true); break;
                case 13: definition = new(40f, 0f, 10, 20f, 7f); break;
                default: return false; // Existing ShockWave 14 is handled by V2.
            }
            return true;
        }

        private float InitializeV8GroundScale(
            in V8GroundEffectDefinition definition, float callerScale)
        {
            if (definition.RandomScaleCount > 0)
                return (Random.Modulo(definition.RandomScaleCount) +
                    definition.RandomScaleOffset) / definition.RandomScaleDivisor;
            return definition.UseCallerScale ? callerScale : definition.Scale;
        }

        // Return false only if the original subtype requires an owner that
        // no longer exists. No fabricated positions or bone indices.
        private bool MoveV8GroundEffect(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.Twlight)
            {
                // MuMain: 0/1/2 have identical scale motion, different yaw.
                e.Scale -= 0.1f * f;
                e.Angle.Z += (e.SubType == 0 ? 10f :
                              e.SubType == 1 ? 5f : 15f) * f;
                return true;
            }
            if (e.Type != ClassicFxEffectType.ShockWave)
                return false;

            switch (e.SubType)
            {
                case 0:
                    e.Scale -= 1f * f;
                    break;
                case 1:
                    e.Scale += Random.Modulo(5) * 0.1f * f;
                    e.Position.X += (Random.Modulo(8) - 4f) * f;
                    e.Position.Y += (Random.Modulo(8) - 4f) * f;
                    break;
                case 2:
                    e.Scale += Random.Modulo(5) * 0.025f * f;
                    e.Position.X += (Random.Modulo(8) - 4f) * f;
                    e.Position.Y += (Random.Modulo(8) - 4f) * f;
                    break;
                case 3:
                    e.Scale += 2f * f;
                    break;
                case 4:
                    e.Scale += 0.3f * f;
                    break;
                case 5:
                    e.Scale -= 0.4f * f;
                    break;
                case 6:
                {
                    // MuMain checks int(LifeTime) % 8 and then FPS-checks
                    // CreateEffect. Once per native lifetime tick prevents
                    // duplicates on 60/120 Hz updates.
                    int nativeTick = (int)e.LifeTime;
                    if (nativeTick >= 0 && nativeTick % 8 == 0 &&
                        nativeTick != e.LastChildNativeTick)
                    {
                        e.LastChildNativeTick = nativeTick;
                        CreateEffect(ClassicFxEffectType.ShockWave,
                            e.Position, e.Angle, e.Light, ClassicFxOwner.None,
                            subType: 5);
                    }
                    return true; // The native handler skips common fade.
                }
                case 7:
                    e.Scale += 2.5f * f;
                    break;
                case 8:
                    e.Scale += 1f * f;
                    break;
                case 9:
                    e.Scale += 0.8f * f;
                    if (!TryV8GroundOwnerPosition(e.Owner, out e.Position))
                        return false;
                    break;
                case 10:
                    e.Scale -= 0.02f * f;
                    if (!TryV8GroundOwnerPosition(e.Owner, out e.Position))
                        return false;
                    break;
                case 11:
                    e.Scale += Random.Modulo(5) * 0.1f * f;
                    e.Position.X += (Random.Modulo(8) - 4f) * f;
                    e.Position.Y += (Random.Modulo(8) - 4f) * f;
                    break;
                case 12:
                    if (e.LifeTime > 4f)
                        e.Scale += (e.LifeTime - 4f) * 0.25f * f;
                    break;
                case 13:
                    e.Scale += 0.08f * f;
                    break;
                default:
                    return false;
            }

            e.Scale = MathF.Max(0f, e.Scale);
            if (e.SubType <= 3)
            {
                float luminosity = e.LifeTime <= 20f
                    ? e.LifeTime / 20f
                    : (40f - e.LifeTime) / 20f;
                e.Light = new Vector3(luminosity);
            }
            else if (e.LifeTime < 6f)
            {
                e.Light *= MathF.Pow(1f / 1.3f, f);
            }
            return true;
        }

        private bool TryV8GroundOwnerPosition(
            ClassicFxOwner owner, out Vector3 position)
        {
            position = default;
            WorldObject target = owner.WorldObject;
            if (target == null || !ReferenceEquals(target.World, World) ||
                target.Status == GameControlStatus.Disposed ||
                target.Status == GameControlStatus.Error)
                return false;
            position = target.WorldPosition.Translation;
            return true;
        }
    }
}
