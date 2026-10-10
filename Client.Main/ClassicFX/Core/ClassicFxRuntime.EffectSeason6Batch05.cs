// BroyalMU ClassicFX - Season 6 Batch 05.
// Native source pinned: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp CreateEffect / MoveEffect, Behaviors/MoveHandlers.cpp,
// EffectTypes.json, ZzzOpenData.cpp. All BMDs use existing ModelObject;
// all children use existing fixed Sprite / Particle / Joint / Effect pools.
// Strictly visual: gameplay, damage, inputs and server packets stay elsewhere.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch05ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Storm or
                ClassicFxEffectType.Summon or
                ClassicFxEffectType.Tail or
                ClassicFxEffectType.WaveForce;

        private static bool TryGetS6Batch05ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.Storm:
                    // Native 0..8 have different lifetimes, directions and
                    // mesh visibility. 3..7 are smoke-only child emitters.
                    if (subType is < 0 or > 8)
                        return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Storm01.bmd",
                        subType == 1 ? 30f :
                        subType is >= 3 and <= 7 ? 60f :
                        subType == 8 ? 100f : 59f,
                        subType == 8 ? 2f : 1f);
                    return true;

                case ClassicFxEffectType.Summon:
                    if (subType is not (0 or 1)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/nightmaresum.bmd", 60f,
                        subType == 0 ? 0.7f : 1.2f);
                    return true;

                case ClassicFxEffectType.Tail:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/tail.bmd", 6f, 1f);
                    return true;

                case ClassicFxEffectType.WaveForce:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/WaveForce.bmd", 12f, 1f,
                        meshLight: 0.1f, useCallerScale: true);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Native MODEL_WAVE_FORCE uses PKKey / 100 as its initial scale.
        /// This helper names the native input, instead of overloading a
        /// bone or skill parameter. Combat is never issued by the effect.
        /// </summary>
        public ClassicFxHandle CreateWaveForce(
            Vector3 position, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, int nativePkKey)
        {
            if (nativePkKey <= 0 || nativePkKey > 32000)
                return ClassicFxHandle.Invalid;
            return CreateEffect(ClassicFxEffectType.WaveForce,
                position, angle, light, owner, 0,
                scale: nativePkKey / 100f);
        }

        private void InitializeS6Batch05Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle,
            ref Vector3 light, ref float scale,
            ref Vector3 direction, ref float velocity,
            ref float gravity, ref float meshLight)
        {
            switch (type)
            {
                case ClassicFxEffectType.Storm:
                    switch (subType)
                    {
                        case 0:
                            direction = new Vector3(0f, -10f, 0f);
                            position.Z = RequestTerrainHeight(position.X, position.Y);
                            break;
                        case 1:
                            direction = Vector3.Zero;
                            break;
                        case 2:
                            direction = new Vector3(0f, -10f, 0f);
                            light = Vector3.One;
                            break;
                        case 3:
                            direction = new Vector3(0f, -12f, 0f);
                            break;
                        case 4:
                            direction = new Vector3(7f, -6f, 0f);
                            break;
                        case 5:
                            direction = new Vector3(-7f, -6f, 0f);
                            break;
                        case 6:
                            direction = new Vector3(4f, 5f, 0f);
                            break;
                        case 7:
                            direction = new Vector3(-4f, 5f, 0f);
                            break;
                    }
                    break;

                case ClassicFxEffectType.Summon:
                    angle.Z += MathHelper.ToRadians(Random.Modulo(360));
                    break;

                case ClassicFxEffectType.Tail:
                    gravity = 80f;
                    light = new Vector3(0.5f);
                    angle = new Vector3(0f, 0f, MathHelper.ToRadians(45f));
                    break;

                case ClassicFxEffectType.WaveForce:
                    // The native initializer sets PKKey/100, velocity 0.5,
                    // and mesh light 0.1; no motion is added in Move handler.
                    velocity = 0.5f;
                    meshLight = 0.1f;
                    break;
            }
        }

        private static void ConfigureS6Batch05ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            switch (type)
            {
                case ClassicFxEffectType.Storm:
                    view.BlendMesh = 0;
                    if (subType is >= 1 and <= 7)
                        view.HiddenMesh = -2;
                    break;
                case ClassicFxEffectType.Summon:
                    view.BlendMesh = 0;
                    break;
                case ClassicFxEffectType.Tail:
                case ClassicFxEffectType.WaveForce:
                    view.BlendMesh = -2;
                    break;
            }
        }

        private bool MoveS6Batch05Model(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.Storm:
                    return MoveS6Storm(ref e, f);
                case ClassicFxEffectType.Summon:
                    return MoveS6Summon(ref e, f);
                case ClassicFxEffectType.Tail:
                    return MoveS6Tail(ref e, f);
                case ClassicFxEffectType.WaveForce:
                    return MoveS6WaveForce(ref e);
                default:
                    return false;
            }
        }

        private bool MoveS6Storm(ref EffectState e, float f)
        {
            // The native shared MoveParticle() runs after the per-type
            // handler. It rotates Direction by the effect Angle every tick.
            // MonoGame stores model angles in radians, not native degrees.
            if (e.Direction != Vector3.Zero)
            {
                Matrix orientation = Matrix.CreateFromYawPitchRoll(
                    e.Angle.Y, e.Angle.X, e.Angle.Z);
                e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;
            }

            e.BlendMeshLight = e.SubType switch
            {
                1 => e.LifeTime * 0.01f,
                0 or 2 or 8 => e.LifeTime * 0.1f,
                _ => e.BlendMeshLight
            };

            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            ClassicFxOwner source = e.ModelView != null
                ? ClassicFxOwner.FromWorldObject(e.ModelView)
                : ClassicFxOwner.None;

            switch (e.SubType)
            {
                case 0:
                    e.Position.Z = RequestTerrainHeight(e.Position.X, e.Position.Y);
                    if (Clock.AdvancedReferenceFrame)
                    {
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position, e.Angle, e.Light, 3);
                        Vector3 thunderAngle = new Vector3(
                            MathHelper.ToRadians(90f), 0f, e.Angle.Z);
                        if (Random.Modulo(2) == 0)
                            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                                e.Position + new Vector3(-200f, 0f, 700f),
                                e.Position, thunderAngle, 0, source, 10f);
                        if (Random.Modulo(2) == 0)
                            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                                e.Position + new Vector3(200f, 0f, 700f),
                                e.Position, thunderAngle, 0, source, 10f);
                        if (Random.Modulo(4) == 0)
                            EmitS6StormStone(ref e);
                    }
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        new Vector3(-luminosity * 0.4f,
                            -luminosity * 0.3f, -luminosity * 0.2f), 5f);
                    // Original also calls AttackCharacterRange every 15
                    // ticks locally. OpenMU owns combat: never duplicate it.
                    break;

                case 1:
                    e.Angle.Z += MathHelper.ToRadians(
                        (30f + Random.Modulo(30)) * f);
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 smoke = e.Position + new Vector3(0f, 0f, 100f);
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            smoke, e.Angle, e.Light, 3);
                        CreateParticle(ClassicTextureIds.BitmapBubble,
                            smoke, e.Angle, e.Light, 3, 0.1f);
                    }
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        e.Light, 5f);
                    break;

                case 2:
                    e.Gravity = Random.Modulo(360);
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 smoke = e.Position + new Vector3(0f, 0f, 100f);
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            smoke, e.Angle, e.Light, 3);
                        CreateParticle(ClassicTextureIds.BitmapBubble,
                            smoke, e.Angle, e.Light, 3, 0.1f);
                    }
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        e.Light, 5f);
                    break;

                case >= 3 and <= 7:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        for (int subType = 28; subType <= 30; subType++)
                            CreateParticle(ClassicTextureIds.BitmapSmoke,
                                e.Position, e.Angle, e.Light, subType);
                        if (Random.Modulo(2) == 0)
                            EmitS6StormStone(ref e);
                    }
                    // Native EarthQuake is camera gameplay state, deliberately
                    // not mutated inside the platform-neutral FX runtime.
                    break;

                case 8:
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        e.Light, 5f);
                    break;
            }
            return true;
        }

        private void EmitS6StormStone(ref EffectState e)
        {
            // Shared, bounded child family. Storm originally spawns
            // MODEL_STONE1/2 subtype 2 (not the subtype-0 ground fragment).
            CreateEffect(Random.Modulo(2) == 0
                    ? ClassicFxEffectType.Stone1
                    : ClassicFxEffectType.Stone2,
                e.Position, e.Angle, e.Light,
                ClassicFxOwner.None, subType: 2);
        }

        private bool MoveS6Summon(ref EffectState e, float f)
        {
            // MODEL_SUMMON: 30 frames of brightening then 30 of fade,
            // continuous slow yaw. No particles/children in native handler.
            e.Light *= MathF.Pow(e.LifeTime >= 30f ? 1.03f : 1f / 1.08f, f);
            e.Angle.Z += MathHelper.ToRadians(1f) * f;
            return true;
        }

        private static bool MoveS6Tail(ref EffectState e, float f)
        {
            e.Position.Z -= e.Gravity * f;
            e.Gravity += 60f * f;
            e.BlendMeshLight = e.LifeTime / 20f;
            return true;
        }

        private bool MoveS6WaveForce(ref EffectState e)
        {
            e.BlendMeshLight = e.LifeTime / 20f;
            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(-luminosity * 0.5f), 5f);
            return true;
        }

        private bool MoveS6Batch05StoneSubType10Or12(ref EffectState e, float f)
        {
            // Native subtype 10/12 starts with direction already rotated at
            // creation; Move_MODEL_ICE_SMALL advances it WITHOUT a second
            // AngleMatrix transform. Both bounce and lose four life ticks.
            e.Position += e.Direction * f;
            e.Direction *= MathF.Pow(0.9f, f);
            e.Position.Z += e.Gravity * f;
            e.Gravity -= 3f * f;
            float terrainZ = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z < terrainZ)
            {
                e.Position.Z = terrainZ;
                e.Gravity = -e.Gravity * 0.5f;
                e.LifeTime -= 4f * f;
                e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f) * f;
            }
            else
            {
                e.Angle.X -= MathHelper.ToRadians(e.Scale * 32f) * f;
            }
            if (e.SubType == 12 && Clock.AdvancedReferenceFrame &&
                Random.Modulo(10) == 0)
                CreateParticle(ClassicTextureIds.BitmapFire,
                    e.Position, e.Angle, e.Light, 1 + Random.Modulo(3));
            return true;
        }

        private static bool MoveS6Batch05StoneSubType2(ref EffectState e, float f)
        {
            // Native subtype 2 takes the non-bouncing Move_MODEL_ICE_SMALL
            // branch: rotated direction, 0.9 damping, +0.5 gravity,
            // +20 degrees yaw. (No unrequested subtype-0 fire trail.)
            Matrix orientation = Matrix.CreateFromYawPitchRoll(
                e.Angle.Y, e.Angle.X, e.Angle.Z);
            e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;
            e.Direction *= MathF.Pow(0.9f, f);
            e.Position.Z += e.Gravity * f;
            e.Gravity += 0.5f * f;
            e.Angle.Z += MathHelper.ToRadians(20f) * f;
            return true;
        }
    }
}
