// BroyalMU ClassicFX — Season 6 Batch 04.
// Native baseline: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp CreateEffect/MoveEffect, Behaviors/MoveHandlers.cpp,
// ZzzOpenData.cpp. The existing MonoGame BMD renderer and fixed pools are reused.
// No combat packets, damage, legacy skill adapters or input are changed.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch04ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.SkillBlast or
                ClassicFxEffectType.SkillInferno or
                ClassicFxEffectType.Circle;

        // Only native subtypes whose required arguments and movement are
        // represented by the current bridge are accepted. The others fail
        // without allocating an Effect pool slot or drawing a fake graphic.
        private static bool TryGetS6Batch04ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.SkillBlast:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Blast01.bmd", 30f, 1f);
                    return true;

                case ClassicFxEffectType.SkillInferno:
                    // Subtype 3 reads the C++ Owner->Owner pointer and
                    // cannot be safely represented as a single owner.
                    if (subType is not (0 or 1 or 2 or 4 or 5 or 6 or 8 or 9 or 10))
                        return false;
                    float lifetime = subType switch
                    {
                        0 or 5 => 15f,
                        1 or 4 => 35f,
                        2 or 6 or 8 or 10 => 12f,
                        9 => 13f,
                        _ => 0f
                    };
                    if (subType == 6) lifetime = 5f;
                    definition = new Season6ModelDefinition(
                        "Skill/Inferno01.bmd", lifetime,
                        subType switch
                        {
                            4 => 0.1f,
                            9 => 0.45f,
                            _ => 0.9f
                        },
                        meshLight: subType is 2 or 6 or 8 or 9 or 10
                            ? 0.1f : 1f,
                        needsOwner: subType == 4,
                        useCallerScale: subType is 2 or 6 or 8 or 10);
                    return true;

                case ClassicFxEffectType.Circle:
                    // Native 1/4 depend on Owner->m_bySkillCount and emit
                    // 36x joint fans. Until skill-count is bridged, reject.
                    if (subType is not (0 or 2 or 3)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Circle01.bmd",
                        subType == 2 ? 250f : subType == 3 ? 30f : 45f,
                        1f, useCallerScale: subType == 0);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Native Inferno 2/6/8/10 carry PKKey (divided by 100 for scale)
        /// and SkillIndex (the hidden-mesh index) in CreateEffect().
        /// The general bridge maps these inputs to scale and boneIndex;
        /// this named API prevents callers from confusing their meaning.
        /// </summary>
        public ClassicFxHandle CreateParameterizedSkillInferno(
            Vector3 position, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, int subType,
            int nativePkKey, int hiddenMesh)
        {
            if (subType is not (2 or 6 or 8 or 10) ||
                nativePkKey <= 0 || hiddenMesh < 0)
                return ClassicFxHandle.Invalid;
            return CreateEffect(ClassicFxEffectType.SkillInferno,
                position, angle, light, owner, subType,
                boneIndex: hiddenMesh, scale: nativePkKey / 100f);
        }

        private void InitializeS6Batch04Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle,
            ref Vector3 light, ref float scale,
            ref Vector3 direction, ref float velocity,
            ref float gravity, ref float meshLight)
        {
            if (type == ClassicFxEffectType.SkillBlast)
            {
                // Source MODEL_SKILL_BLAST: randomized airborne fragments.
                scale = (10f + Random.Modulo(8)) * 0.1f;
                position.X += 200f + Random.Modulo(100);
                position.Y += Random.Modulo(100) - 50f;
                position.Z += 300f + Random.Modulo(500);
                direction = new Vector3(0f, 0f, -50f - Random.Modulo(50));
                angle = new Vector3(0f, MathHelper.ToRadians(20f), 0f);
                return;
            }

            if (type == ClassicFxEffectType.SkillInferno)
            {
                velocity = subType == 8 ? 2f : subType == 6 ? 0.1f : 0.5f;
                if (subType == 9)
                {
                    light = new Vector3(0.1f, 1f, 0.2f);
                    gravity = 2f;
                }
                else if (subType == 1)
                {
                    light = new Vector3(1f, 0.5f, 0.2f);
                }
                else if (subType is 0 or 4 or 5)
                {
                    light = new Vector3(0.8f, 0.8f, 0.8f);
                }
                if (subType is 2 or 6 or 8 or 10)
                    meshLight = 0.1f;
            }
        }

        private bool MoveS6Batch04Model(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.SkillBlast:
                    return MoveSkillBlastModel(ref e, f);
                case ClassicFxEffectType.SkillInferno:
                    return MoveSkillInfernoModel(ref e, f);
                case ClassicFxEffectType.Circle:
                    return MoveCircleModel(ref e);
                default:
                    return false;
            }
        }

        private bool MoveSkillBlastModel(ref EffectState e, float f)
        {
            // Original creates BITMAP_JOINT_ENERGY/5 once at spawn. The
            // WorldObject becomes available on the first native move.
            if (e.FirstMove && e.ModelView != null)
            {
                e.FirstMove = false;
                CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                    e.Position, e.Position, e.Angle,
                    subType: 5,
                    target: ClassicFxOwner.FromWorldObject(e.ModelView),
                    scale: 100f);
            }

            // Native MoveParticle rotates the falling vector by AngleMatrix.
            Matrix orientation = Matrix.CreateFromYawPitchRoll(
                e.Angle.Y, e.Angle.X, e.Angle.Z);
            e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;

            float height = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z <= height)
            {
                e.Position.Z = height;
                // Visual children only. AttackCharacterRange/PacketSerial are
                // SERVER/gameplay concerns and must not run in ClassicFX.
                Vector3 impact = e.Position + new Vector3(0f, 0f, 80f);
                for (int j = 0; j < 6; j++)
                {
                    if (Random.Modulo(2) != 0) continue;
                    CreateEffect(Random.Modulo(2) == 0
                            ? ClassicFxEffectType.Stone1
                            : ClassicFxEffectType.Stone2,
                        e.Position, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: 0);
                }
                CreateParticle(ClassicTextureIds.BitmapShiny + 4,
                    impact, e.Angle, Vector3.One);
                CreateParticle(ClassicTextureIds.BitmapExplotion,
                    impact, e.Angle, Vector3.One);
                return false; // Return to fixed pool on impact.
            }

            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(luminosity * 0.2f,
                    luminosity * 0.4f, luminosity), 2f);
            return true;
        }

        private bool MoveSkillInfernoModel(ref EffectState e, float f)
        {
            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            switch (e.SubType)
            {
                case 2:
                    e.Scale += 0.04f * f;
                    break;
                case 8:
                    e.Scale += 0.04f * f;
                    if (e.LifeTime < 10f)
                        e.Light *= MathF.Pow(0.8f, f);
                    break;
                case 4:
                    if (!TryGetOwnerPosition(e.Owner, out Vector3 ownerPosition))
                        return false;
                    e.Position = ownerPosition;
                    e.Scale += 0.003f * f;
                    e.Gravity += 0.8f * e.Scale * 30f * f;
                    e.Position.Z += e.Gravity * f;
                    break;
                case 5:
                    e.Position.Z += 2f * f;
                    e.Angle.Z += MathHelper.ToRadians(20f) * f;
                    e.BlendMeshLight = e.LifeTime / 20f;
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        new Vector3(luminosity * 0.1f,
                            luminosity * 0.3f, luminosity * 0.8f), 5f);
                    break;
                case 6:
                    e.Scale += 0.01f * f;
                    e.BlendMeshLight = e.LifeTime / 50f;
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        new Vector3(luminosity * 0.8f,
                            luminosity * 0.3f, luminosity * 0.1f), 2f);
                    break;
                case 9:
                    e.BlendMeshLight = e.LifeTime / 80f;
                    e.Position.Z += e.Gravity * f;
                    float fade = MathF.Pow(1f / 1.04f, f);
                    e.Light.X *= fade;
                    e.Light.Z *= fade;
                    e.Alpha = MathF.Max(0f, e.Alpha - 0.01f * f);
                    break;
                case 10:
                    e.Scale += 0.04f * f;
                    break;
            }

            if (e.SubType is 0 or 1 or 2)
            {
                e.BlendMeshLight = e.LifeTime / 20f;
                if (e.SubType != 2)
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        new Vector3(-luminosity * 0.5f), 5f);
            }
            else if (e.SubType == 8)
            {
                e.BlendMeshLight = e.LifeTime / 20f;
            }
            return true;
        }

        private static bool MoveCircleModel(ref EffectState e)
        {
            // Native MODEL_CIRCLE/0 has a local AttackCharacterRange branch;
            // this visual-only client never sends a second gameplay attack.
            e.BlendMeshLight = e.LifeTime * 0.1f;
            if (e.SubType == 2)
                e.BlendMeshLight = e.LifeTime > 240f
                    ? (250f - e.LifeTime) * 0.1f
                    : e.LifeTime * 0.1f;
            else if (e.SubType == 3)
                e.BlendMeshLight = e.LifeTime > 10f
                    ? (30f - e.LifeTime) * 0.1f
                    : e.LifeTime * 0.1f;
            return true;
        }
    }
}
