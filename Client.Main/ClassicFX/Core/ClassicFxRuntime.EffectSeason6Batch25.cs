// ClassicFX Batch 25 — remaining native Cursed Temple, Doppelganger,
// Raklion, Shockwave, Wind Force and SD Aura 3D models.
// Original: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2,
// ZzzEffect.cpp + Behaviors/MoveHandlers.cpp + ZzzOpenData.cpp.
// Does not duplicate BrokenIce, CursedStatue, RaklionBossCrack or Casting.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch25ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.CursedTempleHolyItem &&
            type <= ClassicFxEffectType.SdAura;

        private static bool IsS6Batch25CursedTemple(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.CursedTempleHolyItem &&
            type <= ClassicFxEffectType.CursedTempleRestraint;

        private static bool IsS6Batch25Shockwave(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.Shockwave01 &&
            type <= ClassicFxEffectType.ShockwaveSpin01;

        private static bool TryGetS6Batch25ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch25ModelType(type))
                return false;
            if (IsS6Batch25CursedTemple(type) &&
                subType != 0) return false;
            if (type == ClassicFxEffectType.DoppelgangerSlimeChip &&
                (subType < 0 || subType > 1)) return false;
            if (type == ClassicFxEffectType.RaklionBossMagic &&
                subType != 0) return false;
            if (type == ClassicFxEffectType.Shockwave01 &&
                (subType < 1 || subType > 4)) return false;
            if (type == ClassicFxEffectType.Shockwave02 &&
                subType != 0) return false;
            if (type == ClassicFxEffectType.ShockwaveSpin01 &&
                (subType < 0 || subType > 1)) return false;
            // WindForce subtype 1 is a buff-driven persistent effect:
            // requires g_isCharacterBuff(), not yet bridged. Do not
            // make a permanently-visible fake effect.
            if (type == ClassicFxEffectType.WindForce &&
                (subType < 0 || subType > 5 || subType == 1)) return false;
            if (type == ClassicFxEffectType.SdAura && subType != 0)
                return false;

            string path = type switch
            {
                ClassicFxEffectType.CursedTempleHolyItem => "Skill/eventsungmul.bmd",
                ClassicFxEffectType.CursedTempleProtection => "Skill/eventshild.bmd",
                ClassicFxEffectType.CursedTempleRestraint => "Skill/eventroofe.bmd",
                ClassicFxEffectType.DoppelgangerSlimeChip => "Effect/slime_chip.bmd",
                ClassicFxEffectType.RaklionBossMagic => "Effect/serufan_magic.bmd",
                ClassicFxEffectType.Shockwave01 => "Effect/shockwave01.bmd",
                ClassicFxEffectType.Shockwave02 => "Effect/shockwave02.bmd",
                ClassicFxEffectType.ShockwaveSpin01 => "Effect/shockwave_spin01.bmd",
                ClassicFxEffectType.WindForce => "Effect/wind_foce.bmd",
                ClassicFxEffectType.SdAura => "Effect/shield_up.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = IsS6Batch25CursedTemple(type) ? 9999999f :
                type == ClassicFxEffectType.RaklionBossMagic ? 35f :
                type == ClassicFxEffectType.SdAura ? 1000f :
                type == ClassicFxEffectType.DoppelgangerSlimeChip ? 40f :
                type == ClassicFxEffectType.WindForce ?
                    (subType == 2 || subType == 3 ? 70f : 50f) : 20f;
            float modelScale = type == ClassicFxEffectType.SdAura ? 1f :
                type == ClassicFxEffectType.DoppelgangerSlimeChip ? 0.5f : 1f;
            definition = new Season6ModelDefinition(path, life, modelScale,
                needsOwner: IsS6Batch25CursedTemple(type) ||
                    type == ClassicFxEffectType.SdAura ||
                    IsS6Batch25Shockwave(type),
                useCallerScale: IsS6Batch25Shockwave(type) ||
                    type == ClassicFxEffectType.RaklionBossMagic ||
                    type == ClassicFxEffectType.WindForce);
            return true;
        }

        private bool InitializeS6Batch25Model(
            ClassicFxEffectType type, ClassicFxOwner owner, ref int subType,
            float nativeAnimationSpeed, ref Vector3 position,
            ref Vector3 savedStart, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref float velocity, ref float gravity,
            ref Vector3 headAngle)
        {
            if (IsS6Batch25CursedTemple(type))
            {
                if (owner.WorldObject == null) return false;
                life = 9999999f;
                return true;
            }
            if (type == ClassicFxEffectType.SdAura)
            {
                if (owner.WorldObject == null) return false;
                life = 1000f;
                scale = 1f;
                return true;
            }
            if (type == ClassicFxEffectType.RaklionBossMagic)
            {
                life = 35f;
                return true;
            }
            if (type == ClassicFxEffectType.DoppelgangerSlimeChip)
            {
                life = 30f + Random.Modulo(10);
                scale = 0.1f + Random.Modulo(7) * 0.1f;
                velocity = 0f;
                gravity = 4f;
                angle = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)));
                float zAngle = angle.Z;
                float p = (48f + Random.Modulo(64)) * 0.1f;
                headAngle = new Vector3(-MathF.Sin(zAngle) * p,
                    MathF.Cos(zAngle) * p, 35f + Random.Modulo(5));
                subType = Random.Modulo(2);
                return true;
            }
            if (type == ClassicFxEffectType.WindForce)
            {
                life = subType == 2 || subType == 3 ? 70f : 50f;
                return true;
            }
            if (IsS6Batch25Shockwave(type))
            {
                if (owner.WorldObject == null) return false;
                if (!float.IsFinite(nativeAnimationSpeed) ||
                    nativeAnimationSpeed < 0f)
                    return false;
                if (type == ClassicFxEffectType.Shockwave01 && subType == 2)
                {
                    velocity = 0.3f;
                    life = 5f / velocity;
                }
                else
                {
                    // MuMain uses Owner->BMD.Actions[skill].PlaySpeed.
                    // The bridge cannot invent this per-action source.
                    if (nativeAnimationSpeed <= 0f) return false;
                    velocity = nativeAnimationSpeed;
                    if (type == ClassicFxEffectType.Shockwave01)
                    {
                        if (subType == 3) velocity *= 2f;
                        else if (subType == 4) velocity = 0.6f;
                        life = subType == 4 ? 12f : 5f / velocity;
                    }
                    else if (type == ClassicFxEffectType.Shockwave02)
                    {
                        velocity *= 2f;
                        life = 14f / velocity;
                    }
                    else
                    {
                        life = 8f / velocity;
                        if (subType == 0) velocity *= 2f;
                        else velocity *= 3f;
                    }
                }
                if (type == ClassicFxEffectType.Shockwave01 &&
                    subType >= 3)
                {
                    // Original subtype 3/4 references owner StartPosition;
                    // current bridge can only access owner world position.
                    Vector3 delta = position -
                        owner.WorldObject.WorldPosition.Translation;
                    delta.Z = 0f;
                    if (delta.LengthSquared() > 0.0001f)
                    {
                        delta.Normalize();
                        savedStart = delta * 30f;
                    }
                    else savedStart = Vector3.Zero;
                    if (subType == 3)
                        position += savedStart * 5f * Clock.FrameFactor;
                }
                else if (type == ClassicFxEffectType.Shockwave02)
                {
                    Vector3 delta = position -
                        owner.WorldObject.WorldPosition.Translation;
                    delta.Z = 0f;
                    if (delta.LengthSquared() > 0.0001f)
                    {
                        delta.Normalize();
                        savedStart = delta * 25f;
                    }
                    else savedStart = Vector3.Zero;
                }
                return true;
            }
            return false;
        }

        private bool MoveS6Batch25Model(ref EffectState e, float f)
        {
            if (IsS6Batch25CursedTemple(e.Type) ||
                e.Type == ClassicFxEffectType.SdAura)
                return MoveS6Batch25OwnerAttached(ref e);
            if (e.Type == ClassicFxEffectType.DoppelgangerSlimeChip)
                return MoveS6Batch25Slime(ref e, f);
            if (e.Type == ClassicFxEffectType.RaklionBossMagic)
            {
                e.Alpha = MathF.Max(0f, e.Alpha - 0.03f * f);
                return true;
            }
            if (IsS6Batch25Shockwave(e.Type))
                return MoveS6Batch25Shockwave(ref e, f);
            if (e.Type == ClassicFxEffectType.WindForce)
                return MoveS6Batch25WindForce(ref e, f);
            return true;
        }

        private bool MoveS6Batch25OwnerAttached(ref EffectState e)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;

            if (e.Type == ClassicFxEffectType.SdAura)
            {
                if (TryGetOwnerBonePosition(e.Owner, 18, out Vector3 at))
                {
                    at.Z -= 30f;
                    e.Position = at;
                }
                e.LifeTime = 100f;
                return true;
            }

            if (e.LifeTime < 10f) e.LifeTime = 999999f;
            if (e.Type == ClassicFxEffectType.CursedTempleHolyItem)
            {
                if (TryGetOwnerBonePosition(e.Owner, 20, out Vector3 holy))
                    e.Position = holy;
                e.Light = Vector3.One;
            }
            else if (e.Type == ClassicFxEffectType.CursedTempleProtection)
            {
                if (TryGetOwnerBonePosition(e.Owner, 2, out Vector3 shield))
                    e.Position = shield;
                e.Angle = owner.Angle;
                e.Light = Vector3.One;
                e.Alpha = 0.3f;
                e.Scale = 1f;
                // Native also spawns BITMAP_SHOCK_WAVE subtype 10;
                // its temporary owned sprite is a separate pipeline.
            }
            else
            {
                e.Position = owner.WorldPosition.Translation;
                e.Angle = owner.Angle;
                e.Light = Vector3.One;
                e.Alpha = 0.6f;
                e.Scale = 1f;
            }
            return true;
        }

        private bool MoveS6Batch25Slime(ref EffectState e, float f)
        {
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float terrain = RequestTerrainHeight(e.Position.X, e.Position.Y) + 20f;
            if (e.LifeTime < 10f)
                e.Light *= MathF.Pow(0.8f, f);
            if (e.Position.Z + e.Direction.Z <= terrain)
            {
                e.Position.Z = terrain;
                e.HeadAngle.X *= MathF.Pow(0.8f, f);
                e.HeadAngle.Y *= MathF.Pow(0.8f, f);
                e.HeadAngle.Z += 1.6f * e.LifeTime * f;
                if (e.HeadAngle.Z < 5f) e.HeadAngle.Z = 0f;
            }
            else
                e.Scale += MathF.Sin((float)Clock.WorldTimeMilliseconds *
                    0.015f) * 0.1f * f;
            float angleStep = MathHelper.ToRadians(0.35f * e.LifeTime * f);
            e.Angle.X += e.SubType == 0 ? angleStep : -angleStep;
            e.Angle.Y += e.SubType == 0 ? angleStep : -angleStep;
            return true;
        }

        private bool MoveS6Batch25Shockwave(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.Shockwave01)
            {
                if (e.SubType == 1 || e.SubType == 2)
                {
                    e.Alpha = 1f;
                    e.Scale *= MathF.Pow(1.24f, f);
                    e.Position.Z *= MathF.Pow(1.19f, f);
                    e.Light *= MathF.Pow(0.45f, f);
                    if (e.Light.X < 0.001f) e.Light = Vector3.Zero;
                }
                else if (e.SubType == 3 || e.SubType == 4)
                {
                    e.Scale *= MathF.Pow(1.15f, f);
                    e.StartPosition *= MathF.Pow(
                        e.SubType == 3 ? 0.99f : 1.04f, f);
                    e.Position -= e.StartPosition * f;
                    e.Light *= MathF.Pow(
                        e.SubType == 3 ? 0.7f : 1.5f, f);
                    if (e.Light.X > 0.6f) e.SubType = 5;
                    e.Position.Z = e.SubType == 3 ?
                        180f + e.Scale * 80f : 160f + e.Scale * 10f;
                }
                else if (e.SubType == 5)
                {
                    e.Scale *= MathF.Pow(1.15f, f);
                    e.StartPosition *= MathF.Pow(1.04f, f);
                    e.Position -= e.StartPosition * f;
                    e.Light *= MathF.Pow(0.5f, f);
                    e.Position.Z = 160f + e.Scale * 10f;
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.Shockwave02)
            {
                e.Alpha = 1f;
                e.Light *= MathF.Pow(0.8f, f);
                e.Scale += 0.1f * f;
                e.StartPosition *= MathF.Pow(0.95f, f);
                e.Position -= e.StartPosition * f;
                return true;
            }
            e.Scale *= MathF.Pow(1.01f, f);
            e.Angle.Y += MathHelper.ToRadians(
                (e.SubType == 1 ? 10f : -10f) * f);
            e.Light *= MathF.Pow(0.9f, f);
            e.Alpha = 1f;
            return true;
        }

        private bool MoveS6Batch25WindForce(ref EffectState e, float f)
        {
            float rotationSpeed = e.SubType == 2 || e.SubType == 3 ?
                -0.9f : -0.3f;
            e.Angle.Z = MathHelper.ToRadians(
                (int)Clock.WorldTimeMilliseconds * rotationSpeed);
            e.Scale = MathF.Min(3f, e.Scale + 0.1f * f);
            e.Alpha *= MathF.Pow(0.92f, f);
            e.Light *= MathF.Pow(
                e.SubType == 2 || e.SubType == 3 ? 0.85f : 0.9f, f);

            if (!Clock.AdvancedReferenceFrame ||
                e.SubType == 3)
                return true;

            int tick = (int)MathF.Ceiling(e.LifeTime);
            if (tick == 40 || tick == 30 ||
                (e.SubType == 2 && (tick == 60 || tick == 50)))
            {
                int bit = tick == 40 || tick == 60 ? 1 : 2;
                if ((e.TriggerMask & bit) == 0)
                {
                    e.TriggerMask |= (byte)bit;
                    Vector3 color = e.SubType == 2 ?
                        new Vector3(1f, 0.5f, 0f) :
                        e.SubType == 4 ?
                        new Vector3(0.85f, 0.2f, 1f) :
                        e.SubType == 5 ?
                        new Vector3(1f, 0.2f, 0f) :
                        new Vector3(0.5f, 0.55f, 1f);
                    CreateEffect(ClassicFxEffectType.WindForce,
                        e.Position, e.Angle, color,
                        ClassicFxOwner.None, subType: 3);
                }
            }
            return true;
        }
    }
}
