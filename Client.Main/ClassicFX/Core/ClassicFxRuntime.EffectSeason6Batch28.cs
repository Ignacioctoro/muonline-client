// BroyalMU ClassicFX S6 Batch 28 — late combat, event and volcanic models.
// Pinned MuMain: 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzOpenData.cpp / ZzzEffect.cpp / Behaviors/MoveHandlers.cpp.
// All 3D models use the existing ClassicFxEffectModelObject / shared pool.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch28ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.PhoenixShotModel &&
            type <= ClassicFxEffectType.EmpireGuardianFrameStrikeModel;

        private static bool TryGetS6Batch28ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch28ModelType(type)) return false;

            // No fictitious wind_spin01 or shockwave03 assets:
            // neither BMD is available in the pinned Data_Broyal.
            if (type == ClassicFxEffectType.VolcanoOfMonkModel &&
                subType != 1) return false; // 2/3 are unrendered emitters.
            if (type == ClassicFxEffectType.SakuraEventItemModel &&
                subType is not (0 or 1)) return false;
            if (type == ClassicFxEffectType.EmpireGuardianFrameStrikeModel &&
                subType != 0) return false; // subtype 3 needs interpolator.
            if (type != ClassicFxEffectType.VolcanoOfMonkModel &&
                type != ClassicFxEffectType.SakuraEventItemModel &&
                subType != 0) return false;

            string path = type switch
            {
                ClassicFxEffectType.PhoenixShotModel =>
                    "Effect/phoenix_shot_effect.bmd",
                ClassicFxEffectType.WindSpin02Model =>
                    "Effect/wind_spin02.bmd",
                ClassicFxEffectType.WindSpin03Model =>
                    "Effect/wind_spin03.bmd",
                ClassicFxEffectType.VolcanoOfMonkModel =>
                    "Effect/volcano_of_monk.bmd",
                ClassicFxEffectType.VolcanoStoneModel =>
                    "Effect/volcano_stone.bmd",
                ClassicFxEffectType.MoveTargetPositionModel =>
                    "Effect/MoveTargetPosEffect.bmd",
                ClassicFxEffectType.SakuraEventItemModel =>
                    "Effect/cherryblossom/Skura_iteam_event.bmd",
                ClassicFxEffectType.UmbrellaGoldModel =>
                    "Effect/japan_gold01.bmd",
                ClassicFxEffectType.EmpireGuardianFrameStrikeModel =>
                    "Effect/Karanebos_sword_framestrike.bmd",
                _ => null
            };
            if (path == null) return false;

            float life = type switch
            {
                ClassicFxEffectType.VolcanoOfMonkModel => 100f,
                ClassicFxEffectType.VolcanoStoneModel => 35f,
                ClassicFxEffectType.MoveTargetPositionModel => 30f,
                ClassicFxEffectType.SakuraEventItemModel => 52f,
                ClassicFxEffectType.UmbrellaGoldModel => 55f,
                ClassicFxEffectType.EmpireGuardianFrameStrikeModel => 2f,
                // Native has no explicit CreateEffect lifespan for these
                // three BMDs: finite provisional lifetime only.
                _ => 20f
            };
            definition = new Season6ModelDefinition(
                path, life, type == ClassicFxEffectType.VolcanoOfMonkModel
                    ? 1f : type == ClassicFxEffectType.UmbrellaGoldModel
                    ? 2f : 1f,
                needsOwner: type == ClassicFxEffectType.SakuraEventItemModel,
                useCallerScale: type is
                    ClassicFxEffectType.VolcanoOfMonkModel or
                    ClassicFxEffectType.VolcanoStoneModel);
            return true;
        }

        private void ConfigureS6Batch28ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (type == ClassicFxEffectType.MoveTargetPositionModel)
                view.BlendMesh = 0;
            if (type == ClassicFxEffectType.VolcanoOfMonkModel)
                view.AnimationSpeed = 2.5f; // native 0.1 key/tick at 25fps
        }

        private bool InitializeS6Batch28Model(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref float mesh, ref Vector3 direction,
            ref Vector3 headAngle, ref float gravity,
            ref float velocity)
        {
            if (type == ClassicFxEffectType.VolcanoOfMonkModel)
            {
                life = 100f;
                position.Z = RequestTerrainHeight(position.X, position.Y);
                velocity = 0.6f;
                return true;
            }
            if (type == ClassicFxEffectType.VolcanoStoneModel)
            {
                scale += (Random.Modulo(20) + 5f) * 0.06f;
                life = Random.Modulo(10) + 30f;
                gravity = 2f + Random.Modulo(2);
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                float speed = (Random.Modulo(128) + 64f) * 0.1f;
                headAngle = Vector3.TransformNormal(
                    new Vector3(0f, speed, 0f),
                    Matrix.CreateRotationZ(angle.Z));
                headAngle.Z += 25f * Clock.FrameFactor;
                direction = Vector3.Zero;
                return true;
            }
            if (type == ClassicFxEffectType.MoveTargetPositionModel)
            {
                life = 30f;
                mesh = 1f;
                return true;
            }
            if (type == ClassicFxEffectType.SakuraEventItemModel)
            {
                life = 52f;
                light = new Vector3(1f, 0.6f, 0.8f);
                return true;
            }
            if (type == ClassicFxEffectType.UmbrellaGoldModel)
            {
                life = Random.Modulo(10) + 50f;
                scale = 2f + (Random.Modulo(10) - 5f) * 0.2f;
                angle = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)));
                gravity = Random.Modulo(10) + 10f;
                Vector3 p = new Vector3(
                    (Random.Modulo(10) - 5f) * 0.12f,
                    (Random.Modulo(60) - 30f) * 0.12f, 0f);
                direction = Vector3.TransformNormal(p,
                    Matrix.CreateFromYawPitchRoll(
                        angle.Y, angle.X, angle.Z));
                position.Z = RequestTerrainHeight(position.X,
                    position.Y) + 100f;
                return true;
            }
            if (type == ClassicFxEffectType.EmpireGuardianFrameStrikeModel)
            {
                // Native subtype 0 only: short-lived chrome BMD.
                // ChromeEnable is not a complete renderer bridge yet.
                life = 2f;
                return true;
            }
            // No native CreateEffect custom initialization in the pinned
            // source for PhoenixShot, WindSpin02 and WindSpin03.
            return true;
        }

        private bool MoveS6Batch28Model(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.VolcanoOfMonkModel:
                    return MoveS6Batch28Volcano(ref e, f);
                case ClassicFxEffectType.VolcanoStoneModel:
                    return MoveS6Batch28Stone(ref e, f);
                case ClassicFxEffectType.MoveTargetPositionModel:
                    if (!Clock.AdvancedReferenceFrame) return true;
                    Vector3 at = e.Position + new Vector3(0f, 0f, 110f);
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        at, e.Angle, new Vector3(1f, 0.7f, 0.3f),
                        24, 1f);
                    if (e.LifeTime <= 10f)
                    {
                        e.BlendMeshLight = MathF.Max(0f,
                            e.BlendMeshLight - 0.05f * f);
                        e.Light *= e.BlendMeshLight;
                    }
                    return true;
                case ClassicFxEffectType.SakuraEventItemModel:
                    return MoveS6Batch28Sakura(ref e);
                case ClassicFxEffectType.UmbrellaGoldModel:
                    return MoveS6Batch28Gold(ref e, f);
                // These are native visible BMDs without a corresponding
                // Move handler in the pinned Main. Their bounded lifecycle
                // is managed by the common effect pool.
                case ClassicFxEffectType.PhoenixShotModel:
                case ClassicFxEffectType.WindSpin02Model:
                case ClassicFxEffectType.WindSpin03Model:
                case ClassicFxEffectType.EmpireGuardianFrameStrikeModel:
                    return true;
                default:
                    return false;
            }
        }

        private bool MoveS6Batch28Volcano(ref EffectState e, float f)
        {
            if ((e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                // Native subtype 1 immediately spawns four stones.
                for (int j = 0; j < 4; j++)
                    CreateEffect(ClassicFxEffectType.VolcanoStoneModel,
                        e.Position, e.Angle, Vector3.One,
                        ClassicFxOwner.None, scale: 1f);
                // BITMAP_LIGHT_RED subtype 3 is a separate native effect
                // and is intentionally not replaced by a fake BMD.
            }
            if (!Clock.AdvancedReferenceFrame) return true;

            Vector3 vPos = e.Position;
            vPos.Z += (80f - e.LifeTime) *
                (0.9f + Random.Modulo(5) * 0.1f);
            vPos.Z += e.SubType * 20f;
            if (vPos.Z <= 400f)
            {
                float fireScale = e.Scale *
                    (Random.Modulo(5) + 8f) * 0.19f;
                int n = Random.Modulo(3);
                int texture = n == 0 ? ClassicTextureIds.BitmapFireHik1 :
                    n == 1 ? ClassicTextureIds.BitmapFireCursedLich :
                    ClassicTextureIds.BitmapFireHik3;
                CreateParticle(texture, vPos, e.Angle, Vector3.One,
                    n == 1 ? 9 : 6, fireScale);
            }
            if (e.LifeTime <= 10f && (e.TriggerMask & 2) == 0)
            {
                e.TriggerMask |= 2;
                for (int j = 0; j < 4; j++)
                    CreateEffect(ClassicFxEffectType.VolcanoStoneModel,
                        e.Position, e.Angle, Vector3.One,
                        ClassicFxOwner.None, scale: 1f);
                CreateEffect(ClassicFxEffectType.ShockWaveGround01,
                    e.Position, e.Angle, new Vector3(1f, 0.1f, 0f),
                    ClassicFxOwner.None, subType: 2, scale: 1f);
            }
            if (e.LifeTime < 20f)
                e.Alpha = e.LifeTime * 0.1f;
            if (e.LifeTime < 30f)
                e.BlendMeshLight = e.LifeTime * 0.03f;
            // Native terrain luminosity uses its own terrain-light bridge.
            return true;
        }

        private bool MoveS6Batch28Stone(ref EffectState e, float f)
        {
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float height = RequestTerrainHeight(e.Position.X,
                e.Position.Y);
            e.Angle.X += MathHelper.ToRadians(
                0.3f * e.LifeTime * f);
            e.Angle.Y += MathHelper.ToRadians(
                0.3f * e.LifeTime * f);

            if (e.Position.Z + e.Direction.Z <= height)
            {
                e.Position.Z = height;
                e.HeadAngle.X *= MathF.Pow(0.6f, f);
                e.HeadAngle.Y *= MathF.Pow(0.6f, f);
                e.HeadAngle.Z += e.LifeTime * f;
                if (e.HeadAngle.Z < 0.5f) e.HeadAngle.Z = 0f;
                e.Alpha -= 0.05f * f;
            }
            e.Scale -= 0.03f * f;
            if (e.Scale <= 0f || e.Alpha <= 0f) return false;

            if (Clock.AdvancedReferenceFrame)
            {
                int n = Random.Modulo(3);
                int texture = n == 0 ? ClassicTextureIds.BitmapFireHik1 :
                    n == 1 ? ClassicTextureIds.BitmapFireCursedLich :
                    ClassicTextureIds.BitmapFireHik3;
                float s = e.Scale *
                    (Random.Modulo(5) + 5f) * 0.1f;
                CreateParticle(texture, e.Position, e.Angle,
                    Vector3.One, n == 1 ? 4 : 0, s);
            }
            return true;
        }

        private bool MoveS6Batch28Sakura(ref EffectState e)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;
            e.Position = owner.WorldPosition.Translation;
            e.Light = new Vector3(1f, 0.6f, 0.8f);

            if (!Clock.AdvancedReferenceFrame || e.ModelView == null)
                return true;
            ClassicFxOwner modelSource =
                ClassicFxOwner.FromWorldObject(e.ModelView);
            for (int bone = 1; bone <= 2; bone++)
            {
                if (!TryGetOwnerBonePosition(modelSource, bone,
                    out Vector3 p))
                    continue;
                float degrees = (float)(Clock.WorldTimeMilliseconds *
                    (bone == 1 ? 0.08 : -0.08));
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    p, 1.8f, e.Light, modelSource, degrees);
                // Native creates seven shiny and seven petals per
                // model bone on each 25-FPS reference tick.
                for (int j = 0; j < 7; j++)
                {
                    CreateParticle(ClassicTextureIds.BitmapShiny + 1,
                        p, e.Angle, e.Light, 5, 0.8f);
                    Vector3 petalLight = Random.Modulo(7) == 3
                        ? new Vector3(1f, 0.6f, 0.8f)
                        : new Vector3(0.3f, 0.3f, 0.3f);
                    CreateParticle(
                        ClassicTextureIds.BitmapCherryBlossomEventPetal,
                        p, e.Angle, petalLight, 0, 0.5f);
                }
            }
            // Native also emits periodic 70-particle bursts at LifeTimes
            // 30, 15 and 4, derived from owner bone 20. Pending.
            return true;
        }

        private bool MoveS6Batch28Gold(ref EffectState e, float f)
        {
            e.Angle.X += MathHelper.ToRadians(10f * f);
            e.Position.X += e.Direction.X * 2.2f * f;
            e.Position.Y += e.Direction.Y * 2.2f * f;
            e.Position.Z += e.Gravity * 1.5f * f;
            e.Gravity -= 1.5f * f;
            float height = RequestTerrainHeight(e.Position.X,
                e.Position.Y);
            if (e.Position.Z < height)
            {
                e.Position.Z = height;
                e.Gravity = -e.Gravity * 0.3f;
                e.LifeTime -= 2f * f;
            }
            return true;
        }
    }
}
