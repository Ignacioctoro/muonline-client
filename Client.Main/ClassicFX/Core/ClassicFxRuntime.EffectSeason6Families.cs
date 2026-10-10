// Season 6 Effect families ported from MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// EffectTypes.json, ZzzOpenData.cpp, Behaviors/MoveHandlers.cpp.
// Uses the pre-existing Effect pool, BMD ModelObject, texture repository,
// native AngleMatrix, joints, sprites, particles and world/bone bridges.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct Season6ModelDefinition
        {
            public readonly string Path;
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly float Alpha;
            public readonly float MeshLight;
            public readonly float OffsetZ;
            public readonly bool NeedsOwner;
            public readonly bool UseCallerScale;
            public readonly bool WhiteLight;

            public Season6ModelDefinition(string path, float lifeTime,
                float scale, float alpha = 1f, float meshLight = 1f,
                float offsetZ = 0f, bool needsOwner = false,
                bool useCallerScale = false, bool whiteLight = false)
            {
                Path = path;
                LifeTime = lifeTime;
                Scale = scale;
                Alpha = alpha;
                MeshLight = meshLight;
                OffsetZ = offsetZ;
                NeedsOwner = needsOwner;
                UseCallerScale = useCallerScale;
                WhiteLight = whiteLight;
            }
        }

        private static bool IsSeason6ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.AliceBuffSkillEffect or
                ClassicFxEffectType.AliceBuffSkillEffect2 or
                ClassicFxEffectType.ShockWaveGround01 or
                ClassicFxEffectType.Wave;

        private static bool TryGetSeason6ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.AliceBuffSkillEffect:
                    if (subType is >= 0 and <= 2)
                    {
                        definition = new Season6ModelDefinition(
                            "Effect/elshildring.bmd", 34f, 0.1f,
                            alpha: 0f, meshLight: 0f,
                            offsetZ: 100f, needsOwner: true);
                        return true;
                    }
                    if (subType == 3 || subType == 4)
                    {
                        definition = new Season6ModelDefinition(
                            "Effect/elshildring.bmd", 100f,
                            subType == 3 ? 1.5f : 1f, needsOwner: true);
                        return true;
                    }
                    return false;

                case ClassicFxEffectType.AliceBuffSkillEffect2:
                    // MuMain target casts also pass subtypes 1 and 2;
                    // all three share EffectTypes.json initialization.
                    if (subType < 0 || subType > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Effect/elshildring2.bmd", 35f, 0.15f,
                        alpha: 0f, meshLight: 0f,
                        offsetZ: 100f, needsOwner: true);
                    return true;

                case ClassicFxEffectType.ShockWaveGround01:
                    if (subType < 0 || subType > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Effect/shockwave_ground01.bmd",
                        subType == 0 ? 20f : subType == 2 ? 10f : 50f,
                        1f, useCallerScale: true);
                    return true;

                case ClassicFxEffectType.Wave:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/flashing.bmd", 15f, 0.5f, meshLight: 1.5f,
                        offsetZ: -15f, whiteLight: true);
                    return true;

                default:
                    return false;
            }
        }

        private bool MoveSeason6Model(ref EffectState effect, float f)
        {
            switch (effect.Type)
            {
                case ClassicFxEffectType.AliceBuffSkillEffect:
                case ClassicFxEffectType.AliceBuffSkillEffect2:
                    return MoveAliceBuffModel(ref effect, f);

                case ClassicFxEffectType.ShockWaveGround01:
                    // Move_MODEL_SHOCKWAVE_GROUND01: fade differs by subtype.
                    effect.Scale *= MathF.Pow(1.2f, f);
                    float fade = effect.SubType == 1 ? 0.9f :
                                 effect.SubType == 2 ? 0.7f : 0.8f;
                    effect.Light *= MathF.Pow(fade, f);
                    return true;

                case ClassicFxEffectType.Wave:
                    // Move_MODEL_WAVE: transition from rapidly growing wave
                    // to drifting/fading wave after scale passes two.
                    bool grown = effect.Scale > 2f;
                    effect.Scale += (grown ? 0.1f : 1.2f) * f;
                    effect.Position.X -= (grown ? 1f : 1.2f) * f;
                    effect.Position.Z -= (grown ? 1.5f : 1.8f) * f;
                    if (grown)
                        effect.BlendMeshLight = effect.LifeTime / 30f;
                    return true;
                default:
                    return false;
            }
        }

        private bool MoveAliceBuffModel(ref EffectState e, float f)
        {
            if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot owner))
                return false;

            if (e.SubType == 3 || e.SubType == 4)
            {
                // Owner-bound persistent buff: lifespan is refreshed until
                // owner goes away. Bone source is the actual owner model.
                e.LifeTime = 100f;
                if (!Clock.AdvancedReferenceFrame ||
                    owner.WorldObject is not ModelObject model)
                    return true;

                Matrix[] bones = model.GetBoneTransforms();
                if (bones == null || bones.Length == 0)
                    return true;
                int bone = Random.Modulo(bones.Length);
                if (!TryGetOwnerBonePosition(e.Owner, bone, out Vector3 location))
                    return true;

                if (e.SubType == 3)
                {
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapLight + 2,
                        location, e.Angle, e.Light, 6, e.Scale);
                    bone = Random.Modulo(bones.Length);
                    if (TryGetOwnerBonePosition(e.Owner, bone, out location))
                        CreateParticleFpsChecked(ClassicTextureIds.BitmapLight + 2,
                            location, e.Angle, e.Light, 6, e.Scale);
                }
                else if (Random.FpsCheck(2, Clock))
                {
                    location.Z -= 20f;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapTwinTailWater,
                        location, e.Angle, e.Light, 2);
                }
                return true;
            }
            if (e.SubType is < 0 or > 2) return false;

            // Three native colored ring subtypes, with no equipment
            // requirement and without guessing bone indexes.
            e.Position = owner.Position + new Vector3(0f, 0f, 100f * f);
            if (e.LifeTime > 20f)
            {
                e.Alpha += 0.05f * f;
                e.BlendMeshLight += 0.05f * f;
            }
            else
            {
                e.Alpha -= 0.05f * f;
                e.BlendMeshLight -= 0.05f * f;
                if (e.Alpha < 0f) return false;
            }
            e.Angle.Z += MathHelper.ToRadians(
                (e.Type == ClassicFxEffectType.AliceBuffSkillEffect ? 8f : -8f) * f);
            e.Scale += 0.035f * f;

            // Native: 2 flare sprites, 2 shiny sprites and 3 healing
            // joints every 25-FPS tick. Avoid high-refresh sprite floods.
            if (!Clock.AdvancedReferenceFrame) return true;

            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
            Vector3 flare = e.SubType switch
            {
                0 => new Vector3(0.8f, 0.1f, 0.9f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            flare *= MathF.Max(0f, e.Alpha);
            int spriteSubtype = e.SubType == 1 ? 1 : 0;
            for (int n = 0; n < 2; n++)
                CreateSprite(ClassicTextureIds.BitmapLight, e.Position, 5f,
                    flare, source, subType: spriteSubtype);

            Vector3 shiny = e.SubType switch
            {
                0 => new Vector3(0.7f, 0.6f, 0.9f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            shiny *= MathF.Max(0f, e.Alpha);
            float spin = (float)(Clock.WorldTimeMilliseconds * 0.0006) * 360f;
            CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                e.Position, 2f, shiny, source, spin, spriteSubtype);
            CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                e.Position, 1f, shiny, source, -spin, spriteSubtype);

            Vector3 jointLight = e.SubType switch
            {
                0 => new Vector3(0.7f, 0.5f, 0.7f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            for (int n = 0; n < 3; n++)
            {
                Vector3 angle = new Vector3(Random.Modulo(90), 0f, Random.Modulo(360));
                Vector3 radial = ClassicMath.VectorRotate(
                    new Vector3(0f, -200f, 0f), ClassicMath.AngleMatrix(angle));
                CreateJointFpsChecked(ClassicTextureIds.BitmapJointHealing,
                    e.Position - radial, e.Position, angle,
                    subType: spriteSubtype == 1 ? 16 : 15,
                    target: source, scale: 5f, priorColor: jointLight);
            }
            return true;
        }
    }
}
