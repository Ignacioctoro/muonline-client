// ClassicFX Season 6 Batch 12: Lightning Orb / Fenrir Thunder / Magic2.
// Main: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// Sources: ZzzEffect.cpp, MoveHandlers.cpp, ZzzOpenData.cpp, EffectTypes.json.
// Shared Effect/Sprite/Particle pools; original BMD via MonoGame ModelObject.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch12ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.FenrirThunder or
                ClassicFxEffectType.Magic2;

        private static bool TryGetS6Batch12ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.FenrirThunder:
                    if (subType is < 0 or > 3) return false;
                    definition = new Season6ModelDefinition(
                        "Effect/lightning_type01.bmd",
                        subType >= 2 ? 4f : 100f, 1f,
                        needsOwner: subType == 0);
                    return true;
                case ClassicFxEffectType.Magic2:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Magic02.bmd", 20f, 1f,
                        useCallerScale: true);
                    return true;
                default:
                    return false;
            }
        }

        private bool InitializeS6Batch12Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref Vector3 direction)
        {
            if (type == ClassicFxEffectType.Magic2)
            {
                life = 20f;
                direction = new Vector3(0f, -60f, 0f);
                return true;
            }

            if (type != ClassicFxEffectType.FenrirThunder)
                return false;

            float tickFactor = Clock.FrameFactor;
            life = subType >= 2 ? 4f : 100f;
            scale = (subType == 3 ? 0.5f :
                     subType == 2 ? 0.1f : 0.3f) +
                Random.Modulo(100) * 0.002f;
            alpha = subType == 0 ? 0.7f : 1f;
            angle = new Vector3(
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)));

            if (subType == 0)
            {
                // Native random bone bindings: 10, 14, 2, 50/51, 53.
                // Reject dead/unavailable owner geometry, not fake offset.
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                int roll = Random.Modulo(30);
                int bone = roll switch
                {
                    1 or 2 => 10,
                    3 => 14,
                    4 or 5 => 2,
                    6 or 7 => Random.FpsCheck(2, Clock) ? 50 : 51,
                    8 => 53,
                    _ => -1
                };
                if (roll is 9 or 10)
                    return false;
                if (bone >= 0)
                {
                    if (!TryGetOwnerBonePosition(owner, bone,
                            out Vector3 bonePosition))
                        return false;
                    position = bonePosition;
                    if (roll is 3 or 6 or 7 or 8)
                        scale -= 0.2f * tickFactor;
                }
                else
                {
                    scale += 0.1f * tickFactor;
                    position += new Vector3(
                        (Random.Modulo(240) - 120f) * tickFactor,
                        (Random.Modulo(10) - 5f) * tickFactor,
                        110f * tickFactor);
                }
            }
            else
            {
                float spread = subType == 3 ? 160f : 40f;
                float half = spread * 0.5f;
                position += new Vector3(
                    (Random.Modulo((int)spread) - half) * tickFactor,
                    (Random.Modulo((int)spread) - half) * tickFactor,
                    (Random.Modulo((int)spread) - half) * tickFactor);
            }
            return true;
        }

        private static void ConfigureS6Batch12ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType)
        {
            if (type == ClassicFxEffectType.Magic2)
            {
                view.BlendMesh = 0;
                if (subType == 2)
                    view.HiddenMesh = 0;
            }
        }

        private void StartS6FenrirThunderSprite(ref EffectState effect)
        {
            CreateSprite(ClassicTextureIds.BitmapLight,
                effect.Position, 2f,
                effect.Light - new Vector3(0.3f),
                effect.ModelView != null
                    ? ClassicFxOwner.FromWorldObject(effect.ModelView)
                    : ClassicFxOwner.None);
        }

        private bool MoveS6Batch12Model(ref EffectState effect, float f)
        {
            switch (effect.Type)
            {
                case ClassicFxEffectType.FenrirThunder:
                    return MoveS6FenrirThunder(ref effect, f);
                case ClassicFxEffectType.Magic2:
                    return MoveS6Magic2(ref effect, f);
                default:
                    return false;
            }
        }

        private bool MoveS6FenrirThunder(ref EffectState e, float f)
        {
            // Subtypes 2/3 stay unanimated and expire after four ticks.
            if (e.SubType >= 2)
                return true;

            if (e.Phase == 0f)
            {
                e.Alpha += 0.3f * f;
                if (e.Alpha >= 1f)
                {
                    e.Alpha = 1f;
                    e.Phase = 1f;
                }
            }
            else
            {
                e.Alpha -= 0.3f * f;
                if (e.Alpha <= 0f)
                    return false;
            }

            float delta = MathHelper.ToRadians(0.15f * f);
            e.Angle += new Vector3(delta);
            return true;
        }

        private bool MoveS6Magic2(ref EffectState e, float f)
        {
            // Native MODEL_MAGIC2 has 20 ticks and no position translation.
            e.BlendMeshLight = e.LifeTime * 0.1f;
            if (!Clock.AdvancedReferenceFrame)
                return true;

            for (int j = 0; j < 4; j++)
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    e.Position, e.Angle, e.Light, subType: 3);

            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(0.3f, 0.6f, 1f), 3f);

            if (e.SubType == 2)
            {
                if (e.ModelView != null)
                    e.ModelView.HiddenMesh = 0;
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    e.Position, e.Angle, new Vector3(0.3f, 0.6f, 1f),
                    subType: 11,
                    scale: (Random.Modulo(32) + 80f) * 0.015f);
                Vector3 at = e.Position + new Vector3(0f, 0f, 50f);
                for (int j = 0; j < 2; j++)
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        at, 1.5f, Vector3.One,
                        MathHelper.ToRadians(Random.Modulo(360)));
                CreateSprite(ClassicTextureIds.BitmapLight,
                    at, 3.5f, new Vector3(0.3f, 0.6f, 1f),
                    MathHelper.ToRadians(Random.Modulo(360)));
            }
            return true;
        }

        private void MoveS6LightningOrb(ref EffectState e,
            int effectIndex, float f)
        {
            // Original MODEL_LIGHTNING_ORB is Sprite/Particle-only.
            if (!Clock.AdvancedReferenceFrame)
                return;
            float rotation = MathHelper.ToRadians(
                (float)(Clock.WorldTimeMilliseconds * 0.0006 * 360.0));

            if (e.SubType == 0)
            {
                Vector3 shiny = new Vector3(0.1f, 0.7f, 1.5f);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, 4f, shiny, rotation);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, 3f, shiny, -rotation);
                Vector3 magic = new Vector3(0.1f, 0.1f, 1.5f);
                CreateSprite(ClassicTextureIds.BitmapMagic,
                    e.Position, 1f, magic, rotation);
                CreateSprite(ClassicTextureIds.BitmapMagic,
                    e.Position, 0.5f, magic, -rotation);
                for (int j = 0; j < 2; j++)
                    CreateSprite(ClassicTextureIds.BitmapPinLight,
                        e.Position, 2f, new Vector3(0.5f, 0.5f, 1.5f),
                        MathHelper.ToRadians(Random.Modulo(360)));
                CreateParticle(ClassicTextureIds.BitmapMagic,
                    e.Position, e.Angle,
                    new Vector3(0.4f, 0.4f, 1.5f), subType: 0);
                for (int j = 0; j < 3; j++)
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        e.Position, e.Angle, e.Light, subType: 13);
                // CheckTargetRange belongs to gameplay and OpenMU.
                return;
            }

            if (e.LifeTime >= 5f)
            {
                Vector3 shiny = new Vector3(0.1f, 0.5f, 1.5f);
                CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                    e.Position, 3f, shiny, rotation);
                CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                    e.Position, 2f, shiny, -rotation);
                Vector3 pin = new Vector3(
                    e.Light.X * 0.3f, e.Light.Y * 0.3f, e.Light.Z);
                for (int j = 0; j < 2; j++)
                    CreateSprite(ClassicTextureIds.BitmapPinLight,
                        e.Position, 4f, pin,
                        MathHelper.ToRadians(Random.Modulo(360)));
                CreateSprite(ClassicTextureIds.BitmapEnergy,
                    e.Position, 4f, e.Light, rotation);
            }

            if (e.LifeTime >= 15f)
                for (int j = 0; j < 5; j++)
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        e.Position, e.Angle, e.Light, subType: 20);
            if (e.LifeTime >= 14f)
                for (int j = 0; j < 2; j++)
                    CreateParticle(ClassicTextureIds.BitmapShockWave,
                        e.Position, e.Angle, new Vector3(0.4f, 0.3f, 1f),
                        subType: 0, scale: 0.3f);

            ClassicFxOwner parent = ClassicFxOwner.FromClassicFx(
                Pools.Effects.GetHandle(effectIndex));
            for (int j = 0; j < 2; j++)
                CreateEffect(ClassicFxEffectType.FenrirThunder,
                    e.Position, e.Angle, new Vector3(0.2f, 0.2f, 1f),
                    parent, subType: 3);
            if (e.LifeTime <= 5f)
                for (int j = 0; j < 2; j++)
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Light, subType: 40);
            e.Light *= MathF.Pow(1f / 1.08f, f);
        }
    }
}
