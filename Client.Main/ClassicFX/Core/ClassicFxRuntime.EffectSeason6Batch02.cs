// Season 6 BATCH 02 — shared BMD primitives (not ad-hoc skill sprites).
// Reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect(MODEL_STONE1/2), EffectTypes.json
// Behaviors/MoveHandlers.cpp Move_MODEL_CIRCLE_LIGHT / Move_MODEL_ICE_SMALL.
// All geometry uses the existing ModelObject renderer and Effect pool.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch02ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.CircleLight or
                ClassicFxEffectType.Stone1 or ClassicFxEffectType.Stone2;

        private static bool TryGetS6Batch02ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.CircleLight:
                    // Native MODEL_CIRCLE_LIGHT, default 40-frame ring and
                    // variants: 1 dark, 2 standard, 3 teleport, 4 shorter.
                    if (subType < 0 || subType > 4)
                        return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Circle02.bmd",
                        subType == 3 ? 250f : subType == 4 ? 20f : 40f,
                        1f, useCallerScale: subType <= 1);
                    return true;

                case ClassicFxEffectType.Stone1:
                case ClassicFxEffectType.Stone2:
                    // Batch 02 implements the shared original subtype 0
                    // thrown fragments. Other subtype physics are separate.
                    if (subType != 0)
                        return false;
                    definition = new Season6ModelDefinition(
                        type == ClassicFxEffectType.Stone1
                            ? "Skill/Stone01.bmd" : "Skill/Stone02.bmd",
                        40f, 1f);
                    return true;
                default:
                    return false;
            }
        }

        private bool MoveS6Batch02Model(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.CircleLight)
                return MoveCircleLight(ref e);
            if (e.Type is ClassicFxEffectType.Stone1 or
                ClassicFxEffectType.Stone2)
                return MoveStoneFragment(ref e, f);
            return false;
        }

        private bool MoveCircleLight(ref EffectState e)
        {
            // Subtype 0/1/2: native 40-frame Hellfire ring.
            // Subtype 3/4: bright teleport particles + blue flare joints.
            if (e.SubType <= 2)
            {
                e.BlendMeshLight = e.LifeTime >= 30f
                    ? (40f - e.LifeTime) * 0.1f
                    : e.LifeTime * 0.1f;
                if (Clock.AdvancedReferenceFrame && Random.Modulo(4) == 0)
                {
                    float distance = Random.Modulo(300);
                    float theta = MathHelper.ToRadians(Random.Modulo(360));
                    Vector3 fragment = e.Position + new Vector3(
                        -MathF.Sin(theta) * distance,
                        MathF.Cos(theta) * distance, 0f);
                    ClassicFxEffectType stone = Random.Modulo(2) == 0
                        ? ClassicFxEffectType.Stone1 : ClassicFxEffectType.Stone2;
                    CreateEffect(stone, fragment, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: 0);
                }
                AddClassicTerrainLight(e.Position.X, e.Position.Y,
                    new Vector3(1f, 0.8f, 0.2f), 4f);
                return true;
            }

            e.BlendMeshLight = e.SubType == 3
                ? MathF.Min(0.5f, e.LifeTime >= 240f
                    ? (250f - e.LifeTime) * 0.1f
                    : e.LifeTime * 0.1f)
                : MathF.Min(0.5f, e.LifeTime >= 10f
                    ? (20f - e.LifeTime) * 0.1f
                    : e.LifeTime * 0.1f);

            if (Clock.AdvancedReferenceFrame &&
                (e.SubType != 3 || e.LifeTime > 30f) &&
                (e.SubType != 4 || e.LifeTime > 5f) &&
                Random.Modulo(e.SubType == 3 ? 2 : 5) == 0)
            {
                float distance = Random.Modulo(e.SubType == 3 ? 200 : 100);
                float theta = MathHelper.ToRadians(Random.Modulo(360));
                Vector3 point = e.Position + new Vector3(
                    -MathF.Sin(theta) * distance,
                    MathF.Cos(theta) * distance, 0f);
                CreateParticle(ClassicTextureIds.BitmapFlareBlue,
                    point, e.Angle, e.Light, 0);
                if (e.SubType == 4 || e.LifeTime > 40f)
                {
                    point.Z += 600f;
                    CreateJoint(ClassicTextureIds.BitmapFlareBlue,
                        point, point, new Vector3(0f, 0f, 45f),
                        subType: 19, scale: 40f);
                }
            }
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(MathF.Max(0f, e.BlendMeshLight)), 4f);
            return true;
        }

        private bool MoveStoneFragment(ref EffectState e, float f)
        {
            // Source: MODEL_STONE1/2 subtype 0 inherits native
            // Move_MODEL_ICE_SMALL. Direction decays, gravity bounces on
            // real terrain, and the object rotates during its descent.
            Vector3 movement = Vector3.TransformNormal(e.Direction,
                Matrix.CreateRotationZ(e.Angle.Z));
            e.Position += movement * f;
            e.Direction *= MathF.Pow(0.9f, f);
            e.Position.Z += e.Gravity * f;
            e.Gravity -= 3f * f;

            if (World?.Terrain != null)
            {
                float ground = World.Terrain.RequestTerrainRenderHeight(
                    e.Position.X, e.Position.Y);
                if (e.Position.Z < ground)
                {
                    e.Position.Z = ground;
                    e.Gravity = -e.Gravity * 0.5f;
                    e.LifeTime -= 4f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
                }
                else
                {
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 32f * f);
                }
            }

            if (Clock.AdvancedReferenceFrame && Random.Modulo(10) == 0)
                CreateParticle(ClassicTextureIds.BitmapFire, e.Position,
                    e.Angle, e.Light, 1 + Random.Modulo(3));
            return true;
        }
    }
}
