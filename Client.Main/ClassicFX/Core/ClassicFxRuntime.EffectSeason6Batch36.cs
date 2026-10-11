// BroyalMU ClassicFX Season 6 Batch 36 — 10 native EffectTypes.
// MuMain SHA 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Shared pools, existing ModelObject BMD and terrain-quad renderer only.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // Native 14-bone marks. Shared arrays avoid 25 Hz per-effect allocations.
        private static readonly int[] S6Batch36MarkBones =
            { 20, 20, 19, 18, 17, 2, 35, 26, 36, 27, 37, 28, 39, 30 };
        private static readonly float[] S6Batch36MarkScales =
            { 1.5f, 1.5f, 0.6f, 1.1f, 0.9f, 0.8f, 0.6f,
              0.6f, 0.8f, 0.8f, 0.8f, 0.8f, 0.7f, 0.7f };
        private static bool IsS6Batch36ModelType(ClassicFxEffectType t) =>
            t >= ClassicFxEffectType.BlizzardModel &&
            t <= ClassicFxEffectType.CursedTempleRestraintSkill;

        private static bool IsS6Batch36GroundType(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.EventCloudEffect or
                ClassicFxEffectType.TargetPositionEffect1 or
                ClassicFxEffectType.TargetPositionEffect2 or
                ClassicFxEffectType.RingOfGradationEffect or
                ClassicFxEffectType.OurInfluenceGroundEffect or
                ClassicFxEffectType.EnemyInfluenceGroundEffect;

        private static bool IsS6Batch36LogicalType(ClassicFxEffectType t, int subType) =>
            ((IsS6Batch36GroundType(t) || t == ClassicFxEffectType.LightMarksEffect)
                && (subType == 0 || (t == ClassicFxEffectType.EventCloudEffect && subType == 1)));

        private static bool IsS6Batch36OwnerRequired(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.OurInfluenceGroundEffect or
                ClassicFxEffectType.EnemyInfluenceGroundEffect or
                ClassicFxEffectType.LightMarksEffect;

        private static bool TryGetS6Batch36ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch36ModelType(type)) return false;
            if (type == ClassicFxEffectType.BlizzardModel)
            {
                if (subType is < 0 or > 2) return false;
                definition = new Season6ModelDefinition(
                    "Skill/blizzard.bmd", 20f, 0.5f);
                return true;
            }
            if (subType != 0) return false;
            definition = type == ClassicFxEffectType.CursedTempleProtectionSkill
                ? new Season6ModelDefinition("Skill/eventshild.bmd",
                    9999999f, 1f, alpha: 0.3f, needsOwner: true)
                : new Season6ModelDefinition("Skill/eventroofe.bmd",
                    9999999f, 1f, alpha: 0.6f, needsOwner: true);
            return true;
        }

        private static void ConfigureS6Batch36ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (type == ClassicFxEffectType.BlizzardModel)
                view.BlendMesh = -2;
        }

        private void InitializeS6Batch36Model(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 light, ref float scale,
            ref float life, ref float gravity, ref float velocity,
            ref float alpha)
        {
            if (type == ClassicFxEffectType.BlizzardModel)
            {
                if (subType == 1)
                {
                    life = 20f;
                    velocity = 0f;
                    return;
                }
                life = 15f + Random.Modulo(15);
                scale = 0.5f;
                gravity = -20f - (Random.Modulo(30) + 10) * Clock.FrameFactor;
                velocity = Random.Modulo(360);
                light = Vector3.Zero;
                position.X += Random.Modulo(300) - 150f + 100f * Clock.FrameFactor;
                position.Y += Random.Modulo(300) - 150f;
                position.Z += 600f;
            }
            else
            {
                life = 9999999f;
                scale = 1f;
                alpha = type == ClassicFxEffectType.CursedTempleProtectionSkill
                    ? 0.3f : 0.6f;
            }
        }

        private bool MoveS6Batch36Model(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.BlizzardModel)
            {
                if (e.SubType == 1) return true;
                // Physical movement is frame-factor-scaled at display Hz;
                // only randomized native emissions are gated at 25 Hz.
                e.Position.Z += e.Gravity * f;
                e.StartPosition.X -= 10f * f;
                e.Light += new Vector3(0.1f * f);
                if (Clock.AdvancedReferenceFrame)
                {
                    e.Position.X = e.StartPosition.X +
                        MathF.Sin(Random.Modulo(1000) * 0.01f) * 10f;
                    e.Position.Y = e.StartPosition.Y +
                        MathF.Sin(Random.Modulo(1000) * 0.01f) * 10f;
                    e.Gravity -= Random.Modulo(5);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, Vector3.One, 0, 1.5f);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        e.Position, (4f + Random.Modulo(4)) * 0.2f,
                        e.Light, e.Owner, Random.Modulo(360));
                }
                if (e.Position.Z < RequestTerrainHeight(e.Position.X, e.Position.Y))
                    return false; // Native collision emits secondary smoke.
                return true;
            }
            var o = e.Owner.WorldObject;
            if (o is not ModelObject model ||
                !ReferenceEquals(o.World, World) ||
                o.Status != GameControlStatus.Ready)
                return false;
            if (e.LifeTime < 10f) e.LifeTime = 999999f;
            if (e.Type == ClassicFxEffectType.CursedTempleProtectionSkill)
            {
                Matrix[] bones = model.GetBoneTransforms();
                if (bones == null || bones.Length <= 2) return false;
                // Native TransformPosition(bone 2, RelativePos(-23,0,0)).
                e.Position = Vector3.Transform(new Vector3(-23f, 0f, 0f),
                    bones[2] * model.WorldPosition);
                e.Alpha = 0.3f;
                if (Clock.AdvancedReferenceFrame)
                    CreateEffect(ClassicFxEffectType.ShockWave,
                        o.WorldPosition.Translation, e.Angle, Vector3.One,
                        e.Owner, subType: 10);
            }
            else
            {
                e.Position = o.WorldPosition.Translation;
                e.Alpha = 0.6f;
            }
            e.Angle = model.Angle;
            e.Scale = 1f;
            e.Light = Vector3.One;
            return true;
        }

        private void InitializeS6Batch36Logical(
            ClassicFxEffectType type, out float life, ref float scale,
            ref float alpha, ref Vector3 light, float callerScale)
        {
            // Always assign the native lifetime for every accepted logical type.
            // The CreateEffect caller can now use an out parameter safely.
            life = 0f;
            switch (type)
            {
                case ClassicFxEffectType.EventCloudEffect:
                    life = 30f; scale = callerScale; break;
                case ClassicFxEffectType.TargetPositionEffect1:
                    life = 20f; scale = 1.2f; break;
                case ClassicFxEffectType.TargetPositionEffect2:
                    life = 30f; scale = 1.8f; break;
                case ClassicFxEffectType.RingOfGradationEffect:
                    life = 20f; break;
                case ClassicFxEffectType.OurInfluenceGroundEffect:
                case ClassicFxEffectType.EnemyInfluenceGroundEffect:
                    life = 50f; scale = 0.6f; alpha = 1f; break;
                case ClassicFxEffectType.LightMarksEffect:
                    life = 65f; break;
            }
        }

        private bool MoveS6Batch36Logical(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.EventCloudEffect:
                    e.Scale = MathF.Min(e.SubType == 1 ? 5f : 3f,
                        e.Scale * MathF.Pow(1.08f, f));
                    e.Angle.X = -(int)Clock.WorldTimeMilliseconds * 0.5f;
                    e.Light *= MathF.Pow(0.9f, f);
                    return true;
                case ClassicFxEffectType.TargetPositionEffect1:
                    e.Scale -= 0.04f * f;
                    if (e.Scale <= 0.2f) return false;
                    if (e.LifeTime <= 10f)
                    {
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
                        e.Light *= MathF.Pow(e.Alpha, f);
                    }
                    return true;
                case ClassicFxEffectType.TargetPositionEffect2:
                    if (e.NativeAnimationFrame == 0 && e.Scale <= 0.8f)
                        e.NativeAnimationFrame = 1;
                    else if (e.NativeAnimationFrame == 1 && e.Scale >= 1.8f)
                        e.NativeAnimationFrame = 0;
                    e.Scale += (e.NativeAnimationFrame == 0 ? -0.15f : 0.15f) * f;
                    if (e.LifeTime <= 10f)
                    {
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
                        e.Light *= MathF.Pow(e.Alpha, f);
                    }
                    return true;
                case ClassicFxEffectType.RingOfGradationEffect:
                    e.Scale += 0.1f * f;
                    e.Light *= MathF.Pow(1f / 1.1f, f);
                    e.Alpha *= MathF.Pow(1f / 1.1f, f);
                    return true;
                case ClassicFxEffectType.LightMarksEffect:
                    return EmitS6Batch36LightMarks(ref e, f);
                default:
                    return MoveS6Batch36InfluenceGround(ref e, f);
            }
        }

        private bool EmitS6Batch36LightMarks(ref EffectState e, float f)
        {
            if (e.Owner.WorldObject is not ModelObject model ||
                !ReferenceEquals(model.World, World) ||
                model.Status != GameControlStatus.Ready)
                return false;
            e.Position = model.WorldPosition.Translation;
            e.Light *= MathF.Pow(e.LifeTime >= 35f ?
                1f / 1.035f : 1.035f, f);
            if (!Clock.AdvancedReferenceFrame) return true;
            Matrix[] bones = model.GetBoneTransforms();
            if (bones == null) return true;
            Matrix world = model.WorldPosition;
            for (int i = 0; i < S6Batch36MarkBones.Length; ++i)
            {
                int bone = S6Batch36MarkBones[i];
                if ((uint)bone >= (uint)bones.Length) continue;
                Vector3 p = (bones[bone] * world).Translation;
                CreateSprite(ClassicTextureIds.BitmapLightMarks,
                    p, e.Scale * S6Batch36MarkScales[i], e.Light, e.Owner);
            }
            return true;
        }

        private bool MoveS6Batch36InfluenceGround(ref EffectState e, float f)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World) ||
                owner.Status != GameControlStatus.Ready)
                return false;
            e.Position = owner.WorldPosition.Translation;
            e.Alpha -= 0.02f * f;
            e.Scale += 0.01f * f;
            if (e.Alpha < 0f) { e.Alpha = 1f; e.Scale = 0.6f; }
            if (e.LifeTime < 25f) e.Phase -= 0.02f * f;
            else e.Phase = MathF.Min(1f, e.Phase + 0.02f * f);
            e.Phase = MathF.Max(0f, e.Phase);
            e.LifeTime = 50f;
            return true;
        }

        private void RenderS6Batch36Ground(ref EffectState e)
        {
            int texture = e.Type switch
            {
                ClassicFxEffectType.EventCloudEffect => ClassicTextureIds.BitmapEventCloud,
                ClassicFxEffectType.TargetPositionEffect1 => ClassicTextureIds.BitmapTargetPositionEffect1,
                ClassicFxEffectType.TargetPositionEffect2 => ClassicTextureIds.BitmapTargetPositionEffect2,
                ClassicFxEffectType.RingOfGradationEffect => ClassicTextureIds.BitmapRingOfGradation,
                ClassicFxEffectType.OurInfluenceGroundEffect => ClassicTextureIds.BitmapOurInfluenceGround,
                ClassicFxEffectType.EnemyInfluenceGroundEffect => ClassicTextureIds.BitmapEnemyInfluenceGround,
                _ => -1
            };
            if (texture < 0 || e.Scale <= 0f ||
                !Textures.TryGet(texture, out ClassicTextureResource tex))
                return;
            if (e.Type == ClassicFxEffectType.OurInfluenceGroundEffect ||
                e.Type == ClassicFxEffectType.EnemyInfluenceGroundEffect)
            {
                bool ally = e.Type == ClassicFxEffectType.OurInfluenceGroundEffect;
                Vector3 color = ally
                    ? new Vector3(0.6f,0.9f,1f)
                    : new Vector3(1f,0.3f,0.2f);
                QueueTerrainEffect(ref e, tex,
                    scaleOverride: ally ? e.Scale : e.Scale * 1.6f,
                    lightOverride: color * e.Alpha, angleZOverride: ally ? 45f : 0f);
                QueueTerrainEffect(ref e, tex,
                    scaleOverride: ally ? 0.8f : 1.15f,
                    lightOverride: color * e.Phase, angleZOverride: ally ? 45f : 0f);
                if (Textures.TryGet(ClassicTextureIds.BitmapLight,
                    out ClassicTextureResource flare))
                    QueueTerrainEffect(ref e, flare, scaleOverride: 2f,
                        lightOverride: color * e.Phase);
                return;
            }
            QueueTerrainEffect(ref e, tex,
                angleZOverride: e.Type == ClassicFxEffectType.EventCloudEffect
                    ? e.Angle.X : null);
        }
    }
}
