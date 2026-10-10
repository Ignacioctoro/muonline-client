// ClassicFX S6 Batch 24: Summoner missing models; existing Casting1/11/111/2/22/222/4 untouched.
// Reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// EffectTypes.json, ZzzEffect.cpp and Behaviors/MoveHandlers.cpp.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch24ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.SummonerWristRing and <=
                ClassicFxEffectType.SummonerNeilGround3;

        private static bool IsS6Batch24Head(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.SummonerHeadSahamutt and <=
                ClassicFxEffectType.SummonerHeadLagul;

        private static bool IsS6Batch24NeilKnife(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.SummonerNeilKnife1 and <=
                ClassicFxEffectType.SummonerNeilKnife3;

        private static bool IsS6Batch24NeilGround(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.SummonerNeilGround1 and <=
                ClassicFxEffectType.SummonerNeilGround3;

        private static bool TryGetS6Batch24ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch24ModelType(type))
                return false;

            // Owner-based subtype 1 head "aura only" is not a BMD render.
            // Lagul subtype 1 is owned by a JOINT*, not a WorldObject.
            // Reject rather than silently display a fake object.
            if (IsS6Batch24Head(type) || type == ClassicFxEffectType.SummonerWristRing ||
                type == ClassicFxEffectType.SummonerLagul)
            {
                if (subType != 0) return false;
            }
            else if (type is ClassicFxEffectType.SummonerNeil or
                     ClassicFxEffectType.SummonerSahamutt ||
                     ClassicFxEffectType.SummonerNeilKnife1 or
                     ClassicFxEffectType.SummonerNeilKnife2 or
                     ClassicFxEffectType.SummonerNeilKnife3 or
                     ClassicFxEffectType.SummonerNeilGround1 or
                     ClassicFxEffectType.SummonerNeilGround2 or
                     ClassicFxEffectType.SummonerNeilGround3)
            {
                if (subType is < 0 or > 2) return false;
            }
            else return false;

            string path = type switch
            {
                ClassicFxEffectType.SummonerWristRing => "Effect/ringtyperout.bmd",
                ClassicFxEffectType.SummonerHeadSahamutt => "Skill/sahatail.bmd",
                ClassicFxEffectType.SummonerHeadNeil => "Skill/nillsohwanz.bmd",
                ClassicFxEffectType.SummonerHeadLagul => "Skill/lagul_head.bmd",
                ClassicFxEffectType.SummonerSahamutt => "Skill/summon_sahamutt.bmd",
                ClassicFxEffectType.SummonerNeil => "Skill/summon_neil.bmd",
                ClassicFxEffectType.SummonerLagul => "Skill/summon_lagul.bmd",
                ClassicFxEffectType.SummonerNeilKnife1 => "Skill/nelleff_nife01.bmd",
                ClassicFxEffectType.SummonerNeilKnife2 => "Skill/nelleff_nife02.bmd",
                ClassicFxEffectType.SummonerNeilKnife3 => "Skill/nelleff_nife03.bmd",
                ClassicFxEffectType.SummonerNeilGround1 => "Skill/nell_nifegrund01.bmd",
                ClassicFxEffectType.SummonerNeilGround2 => "Skill/nell_nifegrund02.bmd",
                ClassicFxEffectType.SummonerNeilGround3 => "Skill/nell_nifegrund03.bmd",
                _ => null
            };
            if (path == null) return false;

            float life = type == ClassicFxEffectType.SummonerWristRing ||
                         IsS6Batch24Head(type) ? 100f :
                         type == ClassicFxEffectType.SummonerLagul ? 160f :
                         IsS6Batch24NeilGround(type) || IsS6Batch24NeilKnife(type)
                            ? 50f : 80f;
            float scale = type == ClassicFxEffectType.SummonerWristRing ? 0.7f :
                          IsS6Batch24Head(type) ? 0.8f : 1f;
            float alpha = IsS6Batch24Head(type) ||
                          IsS6Batch24NeilGround(type) ||
                          type is ClassicFxEffectType.SummonerNeil or
                              ClassicFxEffectType.SummonerSahamutt
                              ? 0f : 1f;
            definition = new Season6ModelDefinition(path, life, scale,
                alpha: alpha, meshLight:
                    type == ClassicFxEffectType.SummonerWristRing ? 1f : 1f,
                needsOwner: IsS6Batch24Head(type) ||
                    type == ClassicFxEffectType.SummonerWristRing);
            return true;
        }

        private void ConfigureS6Batch24ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (type == ClassicFxEffectType.SummonerWristRing)
                view.BlendMesh = -2;
        }

        private bool InitializeS6Batch24Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            Vector3 inputLight, ref Vector3 position, ref Vector3 savedStart,
            ref float scale, ref float life, ref float alpha,
            ref float meshLight, ref float velocity, ref Vector3 light)
        {
            if (IsS6Batch24Head(type) ||
                type == ClassicFxEffectType.SummonerWristRing)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
            }

            if (type == ClassicFxEffectType.SummonerWristRing)
            {
                life = 100f;
                scale = 0.7f;
                return true;
            }
            if (IsS6Batch24Head(type))
            {
                life = 100f;
                scale = 0.8f;
                alpha = 0f;
                double t = Clock.WorldTimeMilliseconds;
                Vector3 center = owner.WorldObject.WorldPosition.Translation;
                position = center + new Vector3(
                    (float)Math.Cos(t * 0.003) * 40f,
                    (float)Math.Sin(t * 0.003) * 40f,
                    ((float)Math.Sin(t * 0.001) + 2f) * 80f - 60f);
                savedStart = position - center; // native StartPosition offset
                return true;
            }
            if (type == ClassicFxEffectType.SummonerSahamutt)
            {
                life = 80f;
                scale = subType == 0 ? 0.35f : subType == 1 ? 0.5f : 0.7f;
                velocity = 0.5f;
                alpha = 0f;
                savedStart = inputLight; // native HeadTargetAngle = call Light
                light = Vector3.One;
                return true;
            }
            if (type == ClassicFxEffectType.SummonerNeil)
            {
                life = 80f;
                scale = 1f;
                velocity = 0.35f;
                alpha = 0f;
                position.Z += 10f * Clock.FrameFactor;
                savedStart = inputLight;
                light = Vector3.One;
                return true;
            }
            if (type == ClassicFxEffectType.SummonerLagul)
            {
                life = 160f;
                scale = 1f;
                velocity = 0.5f;
                savedStart = position; // native HeadTargetAngle
                return true;
            }

            // The six Neil children are real BMDs with explicit descriptors,
            // not sprites, and retain the native 50-tick lifetime.
            if (IsS6Batch24NeilKnife(type) || IsS6Batch24NeilGround(type))
            {
                life = 50f;
                scale = 1f;
                alpha = IsS6Batch24NeilGround(type) ? 0f : 1f;
            }
            return true;
        }

        private bool MoveS6Batch24Model(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.SummonerWristRing)
            {
                if (e.Owner.WorldObject == null ||
                    !ReferenceEquals(e.Owner.WorldObject.World, World))
                    return false;
                // MuMain owner bone 37.
                if (TryGetOwnerBonePosition(e.Owner, 37, out Vector3 ring))
                    e.Position = ring;
                e.LifeTime = 100f;
                return true;
            }

            if (IsS6Batch24Head(e.Type))
                return MoveS6Batch24OrbitHead(ref e, f);
            if (e.Type == ClassicFxEffectType.SummonerNeil)
                return MoveS6Batch24Neil(ref e, f);
            if (e.Type == ClassicFxEffectType.SummonerSahamutt)
                return MoveS6Batch24Sahamutt(ref e, f);
            if (e.Type == ClassicFxEffectType.SummonerLagul)
                return MoveS6Batch24Lagul(ref e, f);
            // Native Neil knife/ground models have no independent Move
            // branch at the pinned source; ModelObject owns BMD animation.
            return true;
        }

        private bool MoveS6Batch24OrbitHead(ref EffectState e, float f)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;

            float ms = (float)Clock.WorldTimeMilliseconds;
            float offset = e.NativeSkillIndex * 0.024f;
            Vector3 center = owner.WorldPosition.Translation;
            Vector3 relative = new Vector3(
                MathF.Cos(ms * 0.003f + offset) * 60f,
                MathF.Sin(ms * 0.003f + offset) * 60f,
                (MathF.Sin(ms * 0.001f + offset) + 2f) * 80f - 60f);
            e.Position = center + relative;
            e.Angle.Z = MathF.Atan2(relative.Y - e.StartPosition.Y,
                                  relative.X - e.StartPosition.X);
            e.StartPosition = relative;

            // Main fades in until a safe-zone/oscillation trigger begins
            // permanent fade-out. Hero.SafeZone is not yet bridged here.
            if (e.Alpha >= 0.8f &&
                MathF.Sin(ms * 0.0004f + offset) < 0.3f)
                e.TriggerMask |= 1;

            if ((e.TriggerMask & 1) != 0)
            {
                e.Alpha = MathF.Max(0f, e.Alpha - 0.03f * f);
                if (e.Alpha <= 0f) return false;
            }
            else
                e.Alpha = MathF.Min(1f, e.Alpha + 0.03f * f);

            e.LifeTime = 100f;
            if (!Clock.AdvancedReferenceFrame) return true;

            if (e.Type == ClassicFxEffectType.SummonerHeadNeil)
            {
                if (Random.FpsCheck(2, Clock))
                    CreateParticle(ClassicTextureIds.BitmapLight + 2,
                        e.Position, e.Angle, e.Light, 3, 0.3f);
            }
            else if (e.Type == ClassicFxEffectType.SummonerHeadSahamutt)
            {
                if (Random.FpsCheck(1, Clock))
                    CreateParticle(ClassicTextureIds.BitmapFireCursedLich,
                        e.Position, e.Angle, new Vector3(e.Alpha * 0.3f),
                        1, 1f);
            }
            else if (Random.FpsCheck(1, Clock))
                CreateParticle(ClassicTextureIds.BitmapClud64,
                    e.Position, e.Angle,
                    new Vector3(e.Alpha * 0.7f, e.Alpha * 0.3f, e.Alpha),
                    10, 1f);
            return true;
        }

        private bool MoveS6Batch24Neil(ref EffectState e, float f)
        {
            // The Main keys these children to BMD AnimationFrame 8/10.
            // Existing MonoGame BMD view doesn't expose the native Action
            // frame to EffectState. Phase is an explicit velocity-based proxy.
            e.Phase += e.Velocity * f;
            if (e.LifeTime < 20f)
                e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
            else if (e.Alpha < 0.7f)
                e.Alpha = MathF.Min(0.7f, e.Alpha + 0.04f * f);

            if (e.Phase > 8f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                CreateEffect(ClassicFxEffectType.SummonerNeilKnife1,
                    e.StartPosition, e.Angle, e.Light,
                    ClassicFxOwner.None, subType: e.SubType);
                if (e.SubType >= 1)
                    CreateEffect(ClassicFxEffectType.SummonerNeilKnife2,
                        e.StartPosition, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: e.SubType);
                if (e.SubType >= 2)
                    CreateEffect(ClassicFxEffectType.SummonerNeilKnife3,
                        e.StartPosition, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: e.SubType);
            }
            if (e.Phase > 10f && (e.TriggerMask & 2) == 0)
            {
                e.TriggerMask |= 2;
                Vector3 near = e.Position + Vector3.TransformNormal(
                    new Vector3(0f, -60f, 0f),
                    Matrix.CreateRotationZ(e.Angle.Z));
                CreateEffect(ClassicFxEffectType.SummonerNeilGround1,
                    near, e.Angle, e.Light,
                    ClassicFxOwner.None, subType: e.SubType);
                CreateEffect(ClassicFxEffectType.SummonerNeilGround1,
                    e.StartPosition, e.Angle, e.Light,
                    ClassicFxOwner.None, subType: e.SubType);
                if (e.SubType >= 1)
                    CreateEffect(ClassicFxEffectType.SummonerNeilGround2,
                        e.StartPosition, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: e.SubType);
                if (e.SubType >= 2)
                    CreateEffect(ClassicFxEffectType.SummonerNeilGround3,
                        e.StartPosition, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: e.SubType);
            }
            return true;
        }

        private bool MoveS6Batch24Sahamutt(ref EffectState e, float f)
        {
            // Approximate BMD action stages until native AnimationFrame
            // can be exposed by ModelObject. Model position and fade use
            // native ground snap and trajectory parameters.
            e.Phase += e.Velocity * f;
            if (e.LifeTime < 20f)
                e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
            else if (e.Phase < 4f)
            {
                e.Alpha = MathF.Min(0.3f, e.Alpha + 0.05f * f);
                Vector3 delta = e.StartPosition - e.Position;
                e.Angle.Z = MathF.Atan2(delta.Y, delta.X);
            }
            else
            {
                if (e.Phase >= 11f)
                    e.Alpha = MathF.Max(0f, e.Alpha - 0.3f * f);
                else if (e.Alpha < 0.7f)
                    e.Alpha = MathF.Min(0.7f, e.Alpha + 0.05f * f);

                if (e.Phase is > 4f and < 12f)
                {
                    float distance = Vector2.Distance(
                        new Vector2(e.Position.X, e.Position.Y),
                        new Vector2(e.StartPosition.X, e.StartPosition.Y));
                    float divisor = e.Phase < 10f ? 13f : 45f;
                    e.Position += Vector3.TransformNormal(
                        new Vector3(0f, -distance / divisor, 0f),
                        Matrix.CreateRotationZ(e.Angle.Z)) * f;
                }
            }
            e.Position.Z = RequestTerrainHeight(e.Position.X, e.Position.Y);
            // Native CreateBomb3 and per-bone particle cascade are distinct
            // engine features, not fabricated in this model-only batch.
            return true;
        }

        private bool MoveS6Batch24Lagul(ref EffectState e, float f)
        {
            // Native subtype 0 emits particles distributed near target.
            int chance = e.NativePkKey == 1 ? 4 :
                e.NativePkKey == 2 ? 3 : 5;
            if (!Clock.AdvancedReferenceFrame ||
                !Random.FpsCheck(chance, Clock))
                return true;
            Vector3 target = e.StartPosition;
            Vector3 pos = target + new Vector3(
                Random.Modulo(500) - 250f,
                Random.Modulo(500) - 250f, 0f);
            CreateParticle(ClassicTextureIds.BitmapSmoke,
                pos, e.Angle, Vector3.One, 57, 3.5f);
            // Native also emits BITMAP_CLOUD and BITMAP_TWINTAIL_WATER,
            // not replicated here as different texture IDs.
            return true;
        }
    }
}
