// ClassicFX Effect Model Batch 2 — native, reusable Effect behavior families.
// References:
//  MuMain src/bin/Data/Effects/EffectTypes.json (CreateEffect initialization)
//  MuMain src/source/Engine/Object/ZzzOpenData.cpp (original BMD paths)
//  MuMain src/source/Render/Effects/Behaviors/MoveHandlers.cpp
// Types: MODEL_SWORD_FORCE, MODEL_MAGIC_CIRCLE1, MODEL_MAGIC1,
//        MODEL_MAGIC_CAPSULE2, MODEL_POISON.
// These are engine types, NOT individual skill effects.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct Batch2EffectModelDefinition
        {
            public readonly string Path;
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly float BlendMeshLight;
            public readonly int BlendMesh;
            public readonly int HiddenMesh;
            public readonly Vector3 Direction;
            public readonly float Velocity;
            public readonly float SpawnZ;
            public readonly bool RequiresOwner;
            public readonly bool UseCallerScale;

            public Batch2EffectModelDefinition(
                string path, float life, float scale, int blendMesh = 0,
                float blendMeshLight = 1f, int hiddenMesh = -1,
                Vector3 direction = default, float velocity = 0f,
                float spawnZ = 0f, bool requiresOwner = false,
                bool useCallerScale = false)
            {
                Path = path;
                LifeTime = life;
                Scale = scale;
                BlendMesh = blendMesh;
                BlendMeshLight = blendMeshLight;
                HiddenMesh = hiddenMesh;
                Direction = direction;
                Velocity = velocity;
                SpawnZ = spawnZ;
                RequiresOwner = requiresOwner;
                UseCallerScale = useCallerScale;
            }
        }

        private static bool IsBatch2EffectModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.SwordForce or
                ClassicFxEffectType.MagicCircle1 or
                ClassicFxEffectType.Magic1 or
                ClassicFxEffectType.MagicCapsule2 or
                 ClassicFxEffectType.Poison or
                 ClassicFxEffectType.DarkLordSkill;

        private static bool TryGetBatch2EffectModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Batch2EffectModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.DarkLordSkill:
                    // MODEL_DARKLORD_SKILL: both equipped-hand subtypes.
                    if (subtype != 0 && subtype != 1) return false;
                    definition = new Batch2EffectModelDefinition(
                        "Skill/DarkLordSkill.bmd", 10f, 0.2f,
                        velocity: 0.1f);
                    return true;

                case ClassicFxEffectType.SwordForce:
                    // MuMain MODEL_SWORD_FORCE: types 0/2 grow and spawn
                    // copies 1/3, and then create spark/fire impacts.
                    if (subtype == 0 || subtype == 2)
                        definition = new Batch2EffectModelDefinition(
                            "Skill/SwordForce.bmd", 15f, 0f,
                            direction: new Vector3(0f, -10f, 0f),
                            velocity: 0.25f, spawnZ: 100f);
                    else if (subtype == 1 || subtype == 3)
                        definition = new Batch2EffectModelDefinition(
                            "Skill/SwordForce.bmd", 5f, 3.5f,
                            velocity: 0.25f);
                    else
                        return false;
                    return true;

                case ClassicFxEffectType.MagicCircle1:
                    // Base subtype 0, plus original subtype 1/2 overrides.
                    if (subtype < 0 || subtype > 2)
                        return false;
                    definition = new Batch2EffectModelDefinition(
                        "Skill/MagicCircle01.bmd",
                        subtype == 1 ? 20f : subtype == 2 ? 15f : 30f,
                        0.7f, blendMesh: -2,
                        hiddenMesh: subtype == 1 ? 0 : -1,
                        velocity: subtype == 2 ? 0.3f : 0.1f,
                        requiresOwner: subtype != 1);
                    return true;

                case ClassicFxEffectType.Magic1:
                    if (subtype != 0) return false;
                    definition = new Batch2EffectModelDefinition(
                        "Skill/Magic01.bmd", 20f, 1f, useCallerScale: true);
                    return true;

                case ClassicFxEffectType.MagicCapsule2:
                    if (subtype != 0) return false;
                    definition = new Batch2EffectModelDefinition(
                        "Skill/Protect02.bmd", 20f, 1f, useCallerScale: true);
                    return true;

                case ClassicFxEffectType.Poison:
                    if (subtype != 0) return false;
                    definition = new Batch2EffectModelDefinition(
                        "Skill/Poison01.bmd", 40f, 1f, blendMesh: 1);
                    return true;

                default:
                    return false;
            }
        }

        private bool MoveBatch2EffectModel(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.DarkLordSkill:
                    // MuMain common MoveEffects branch, 25-FPS semantics.
                    e.Scale += e.Velocity * f;
                    e.Velocity += 0.02f * f;
                    if (e.LifeTime < 7f)
                        e.BlendMeshLight *= MathF.Pow(1f / 1.8f, f);
                    return true;

                case ClassicFxEffectType.SwordForce:
                    return MoveSwordForceEffect(ref e, f);

                case ClassicFxEffectType.MagicCircle1:
                    if (e.SubType == 2)
                    {
                        e.Scale += 0.015f * f;
                        if (!TryBatch2OwnerPosition(e.Owner, out e.Position))
                            return false;
                        e.Light = new Vector3(0.1f, 0f, 0f);
                    }
                    else if (e.SubType == 1)
                    {
                        e.Scale += 0.01f * f;
                        float lum = (Random.Modulo(4) + 7) * 0.1f;
                        AddClassicTerrainLight(e.Position.X, e.Position.Y,
                            new Vector3(lum, 0f, 0f), 3f);
                    }
                    else
                    {
                        if (!TryBatch2OwnerPosition(e.Owner, out e.Position))
                            return false;
                        e.Scale += 0.01f * f;
                    }
                    return true;

                case ClassicFxEffectType.Poison:
                    e.BlendMeshLight = e.LifeTime * 0.1f;
                    e.Alpha = e.LifeTime * 0.1f;
                    float luminosity = (Random.Modulo(4) + 7) * 0.1f;
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        new Vector3(luminosity * 0.3f,
                            luminosity, luminosity * 0.6f), 2f);
                    return true;

                case ClassicFxEffectType.Magic1:
                case ClassicFxEffectType.MagicCapsule2:
                    // MuMain has no MoveHandlers entry for either type.
                    // Their common Effect pool handles lifetime and BMD rendering.
                    return true;

                default:
                    return false;
            }
        }

        private bool TryBatch2OwnerPosition(
            ClassicFxOwner owner, out Vector3 position)
        {
            position = default;
            var worldObject = owner.WorldObject;
            if (worldObject == null ||
                !ReferenceEquals(worldObject.World, World) ||
                worldObject.Status == GameControlStatus.Disposed ||
                worldObject.Status == GameControlStatus.Error)
                return false;
            position = worldObject.WorldPosition.Translation;
            return true;
        }

        private bool MoveSwordForceEffect(ref EffectState e, float f)
        {
            if (e.SubType == 1 || e.SubType == 3)
            {
                float luminosity = e.LifeTime / 10f;
                e.BlendMeshLight = luminosity;
                e.Alpha = luminosity;
                e.Light = new Vector3(luminosity);
                return true;
            }
            if (e.SubType != 0 && e.SubType != 2)
                return false;

            if (e.LifeTime > 12f)
            {
                e.Scale += 0.9f * f;
                e.Direction.Y -= 2f * f;

                // MuMain CreateEffectFpsChecked(): one child/25-FPS tick
                // on average even on 60/120-Hz MonoGame updates.
                if (Random.FpsCheck(1, Clock))
                {
                    ClassicFxOwner parent = ClassicFxOwner.FromWorldObject(e.ModelView);
                    CreateEffect(ClassicFxEffectType.SwordForce,
                        e.Position, e.Angle, e.Light, parent,
                        subType: e.SubType == 2 ? 3 : 1);
                }
            }
            else
            {
                e.Scale -= 0.05f * f;
                e.BlendMeshLight = e.LifeTime / 18f;
                e.Alpha = e.BlendMeshLight;
                e.Light = new Vector3(e.Alpha);
                e.Direction.Y -= 2f * f;

                Vector3 impact = e.Position + new Vector3(
                    Random.Modulo(30) - 15f,
                    Random.Modulo(30) - 15f, -100f);
                for (int n = 0; n < 4; n++)
                {
                    // Main generates separate angles for every spark.
                    Vector3 angle = new Vector3(
                        Random.Modulo(60) + 150f,
                        0f, MathHelper.ToDegrees(e.Angle.Z));
                    CreateJointFpsChecked(ClassicTextureIds.BitmapJointSpark,
                        impact, impact, angle);
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapFire,
                        impact, angle, e.Light,
                        e.SubType == 2 ? 18 : 2, 1.5f);
                }
            }

            // MuMain: warm light is emitted on both original phases.
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(1f, 0.8f, 0.6f), 1f);
            return true;
        }
    }
}
