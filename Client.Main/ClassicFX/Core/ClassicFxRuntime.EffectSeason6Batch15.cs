// ClassicFX S6 Batch 15: twelve BMD effect primitives plus supporting logical FX.
// Main: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect/MoveEffects, MoveHandlers.cpp, ZzzOpenData.cpp.
// Uses existing ClassicFxRuntime Effect pool + MonoGame ModelObject renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch15ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.BrokenIce0 and <= ClassicFxEffectType.FlameStrike;

        private static bool IsS6Batch15Ice(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.BrokenIce0 and <= ClassicFxEffectType.BrokenIce3;

        private static bool TryGetS6Batch15ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch15ModelType(type)) return false;
            string path;
            float life;
            bool owner = false;

            if (IsS6Batch15Ice(type))
            {
                if (subType is < 0 or > 2) return false;
                int num = (int)type - (int)ClassicFxEffectType.BrokenIce0;
                path = "Effect/ice_stone0" + num + ".bmd";
                life = subType == 0 ? 50f : subType == 1 ? 40f : 100f;
            }
            else
            {
                switch (type)
                {
                    case ClassicFxEffectType.CursedStatue1:
                    case ClassicFxEffectType.CursedStatue2:
                        if (subType != 0) return false;
                        path = type == ClassicFxEffectType.CursedStatue1
                            ? "NPC/songck1.bmd" : "NPC/songck2.bmd";
                        life = 220f;
                        break;
                    case ClassicFxEffectType.SnowmanHead:
                    case ClassicFxEffectType.SnowmanBody:
                        if (subType != 0) return false;
                        path = type == ClassicFxEffectType.SnowmanHead
                            ? "Item/xmas/snowman_die_head_model.bmd"
                            : "Item/xmas/snowman_die_body.bmd";
                        life = 50f; owner = true;
                        break;
                    case ClassicFxEffectType.Feather:
                    case ClassicFxEffectType.FeatherForeign:
                        if (type == ClassicFxEffectType.Feather && subType is < 0 or > 3)
                            return false;
                        if (type == ClassicFxEffectType.FeatherForeign && subType != 4)
                            return false;
                        path = "Skill/darkwing_hetachi.bmd";
                        life = subType is 2 or 3 ? 100f : 40f;
                        break;
                    case ClassicFxEffectType.SapitresAttack1:
                        if (subType != 0) return false;
                        path = "Effect/Sapiatttres.bmd";
                        life = 17f; owner = true;
                        break;
                    case ClassicFxEffectType.SapitresAttack2:
                        if (subType is not (0 or 1 or 13 or 14)) return false;
                        path = "Effect/Sapiatttres2.bmd";
                        life = subType == 14 ? 20f : subType == 13 ? 115f : 48f;
                        break;
                    case ClassicFxEffectType.FlameStrike:
                        if (subType != 0) return false;
                        path = "Effect/FlameStrike.bmd";
                        life = 35f; owner = true;
                        break;
                    default:
                        return false;
                }
            }
            definition = new Season6ModelDefinition(
                path, life, 1f, needsOwner: owner, useCallerScale: true);
            return true;
        }

        private bool InitializeS6Batch15Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref Vector3 direction, ref float gravity,
            ref Vector3 headAngle, ref float velocity, ref float meshLight)
        {
            float f = Clock.FrameFactor;
            if (IsS6Batch15Ice(type))
            {
                switch (subType)
                {
                    case 0:
                        life = 35f + Random.Modulo(16);
                        scale = (3f + Random.Modulo(13)) * 0.2f;
                        gravity = 3f + Random.Modulo(3);
                        angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                        headAngle = Vector3.TransformNormal(
                            new Vector3(0f, (64f + Random.Modulo(128)) * 0.1f, 0f),
                            Matrix.CreateRotationZ(angle.Z));
                        headAngle.Z += 25f * f;
                        break;
                    case 1:
                        life = 40f;
                        scale += (15f + Random.Modulo(8)) * 0.1f;
                        direction = new Vector3(0f, 0f, -60f);
                        headAngle = new Vector3(0f, 30f, 0f);
                        break;
                    case 2:
                        life = 100f;
                        gravity = 20f + Random.Modulo(20) * 0.5f;
                        break;
                }
                return true;
            }

            switch (type)
            {
                case ClassicFxEffectType.CursedStatue1:
                case ClassicFxEffectType.CursedStatue2:
                    life = 180f + Random.Modulo(40);
                    scale = 0.1f + Random.Modulo(6) * 0.1f;
                    velocity = 0f;
                    gravity = 2.5f;
                    direction = new Vector3(Random.Modulo(30) - 15f,
                        Random.Modulo(30) - 15f, 0f);
                    angle = new Vector3(0f, 0f,
                        MathHelper.ToRadians(Random.Modulo(360)));
                    light = new Vector3(0.5f + Random.Modulo(6) * 0.1f);
                    break;
                case ClassicFxEffectType.SnowmanHead:
                {
                    if (!TryGetOwnerSnapshot(owner, out ClassicFxOwnerSnapshot caster))
                        return false;
                    life = 50f;
                    scale = 1.3f;
                    angle.Z = caster.Angle.Z;
                    gravity = 5f;
                    direction = Vector3.TransformNormal(new Vector3(
                        (Random.Modulo(10) - 5f) * 0.13f,
                        (Random.Modulo(60) - 40f) * 0.13f, 0f),
                        Matrix.CreateRotationZ(angle.Z));
                    break;
                }
                case ClassicFxEffectType.SnowmanBody:
                {
                    if (!TryGetOwnerSnapshot(owner, out ClassicFxOwnerSnapshot caster))
                        return false;
                    life = 50f; scale = 1.3f; velocity = 1.2f;
                    direction = Vector3.Zero; // owner animation owns motion
                    angle = caster.Angle;
                    break;
                }
                case ClassicFxEffectType.Feather:
                case ClassicFxEffectType.FeatherForeign:
                {
                    Vector3 offset = new Vector3(
                        (Random.Modulo(20) - 10f) * 4f,
                        (Random.Modulo(20) - 10f) * 4f,
                        (Random.Modulo(20) - 10f) * 4f) * f;
                    position += offset;
                    if (offset.LengthSquared() > 0.001f)
                        direction = Vector3.Normalize(-offset);
                    float directionalDrift = (Random.Modulo(10) - 5) * 0.08f;
                    direction += new Vector3(directionalDrift * f);
                    scale += (Random.Modulo(20) - 10f) * (scale * 0.03f);
                    life = type == ClassicFxEffectType.FeatherForeign
                        ? 25f : subType is 2 or 3
                            ? 100f : 20f + Random.Modulo(20);
                    alpha = type == ClassicFxEffectType.FeatherForeign
                        ? 0.3f : 0.6f + Random.Modulo(10) * 0.02f;
                    gravity = subType is 2 or 3 ? 0f : 0.1f;
                    angle = new Vector3(
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)));
                    // Native EyeRight is random angular velocity; store in
                    // HeadAngle, which has no competing role for feathers.
                    headAngle = new Vector3(
                        MathHelper.ToRadians(Random.Modulo(10) - 5f),
                        MathHelper.ToRadians(Random.Modulo(10) - 5f),
                        MathHelper.ToRadians(Random.Modulo(10) - 5f));
                    break;
                }
                case ClassicFxEffectType.SapitresAttack1:
                {
                    if (!TryGetOwnerSnapshot(owner, out ClassicFxOwnerSnapshot target))
                        return false;
                    life = 17f; scale = 1.1f; meshLight = 1f;
                    position.Z += 100f * f;
                    direction = target.Position - position;
                    if (direction.LengthSquared() > 0.0001f)
                        direction.Normalize();
                    angle.Z = MathF.Atan2(target.Position.Y - position.Y,
                        target.Position.X - position.X);
                    break;
                }
                case ClassicFxEffectType.SapitresAttack2:
                {
                    meshLight = 1f;
                    position.Z += 50f * f;
                    if (subType == 14)
                    {
                        life = 15f + Random.Modulo(5);
                        angle = new Vector3(
                            MathHelper.ToRadians(Random.Modulo(60) - 30f),
                            MathHelper.ToRadians(Random.Modulo(60) - 30f),
                            MathHelper.ToRadians(Random.Modulo(360)));
                        direction = Vector3.TransformNormal(
                            new Vector3(0f, (64f + Random.Modulo(256)) * 0.1f, 0f),
                            Matrix.CreateFromYawPitchRoll(
                                angle.Y, angle.X, angle.Z));
                        if (direction.LengthSquared() > 0.0001f)
                            direction.Normalize();
                        gravity = 0f;
                    }
                    else
                    {
                        life = subType == 13 ? 100f + Random.Modulo(16) :
                            32f + Random.Modulo(16);
                        scale = (8f + Random.Modulo(4)) * 0.1f;
                        angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                        direction = Vector3.TransformNormal(
                            new Vector3(0f, (64f + Random.Modulo(256)) * 0.1f, 0f),
                            Matrix.CreateRotationZ(angle.Z));
                        gravity = 8f + Random.Modulo(16);
                    }
                    break;
                }
                case ClassicFxEffectType.FlameStrike:
                    life = 35f; alpha = 0f; velocity = 1f;
                    // Owner-linked original blur is not synthesized.
                    break;
            }
            return true;
        }

        private static void ConfigureS6Batch15ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            if (type is ClassicFxEffectType.SapitresAttack1 or
                ClassicFxEffectType.SapitresAttack2)
                view.BlendMesh = 0;
        }

        private bool MoveS6Batch15Model(ref EffectState e, float f)
        {
            if (IsS6Batch15Ice(e.Type))
                return MoveS6Batch15Ice(ref e, f);

            switch (e.Type)
            {
                case ClassicFxEffectType.CursedStatue1:
                case ClassicFxEffectType.CursedStatue2:
                    if (e.Position.Z > 290f)
                    {
                        e.Direction.Z -= e.Gravity * f;
                        e.Angle.Z -= MathHelper.ToRadians(
                            Random.Modulo(10) * f);
                        e.Position += e.Direction * f;
                    }
                    else if (e.Position.Z < 290f)
                    {
                        e.Position.Z = 290f;
                        e.Direction = Vector3.Zero;
                        // Camera EarthQuake is not visual-model logic.
                    }
                    else
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.01f * f);
                    return true;

                case ClassicFxEffectType.SnowmanHead:
                    if (e.LifeTime < 45f)
                    {
                        e.Position += e.Direction * (2.2f * f);
                        e.Position.Z += e.Gravity * 1.5f * f;
                        e.Gravity -= 1.5f * f;
                        float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                        if (e.Position.Z < ground)
                        {
                            e.Position.Z = ground;
                            e.Gravity = -e.Gravity * 0.3f;
                            e.LifeTime -= 2f * f;
                        }
                    }
                    return true;
                case ClassicFxEffectType.SnowmanBody:
                    // MonoGame ModelObject handles body animation, no new renderer.
                    return true;

                case ClassicFxEffectType.Feather:
                case ClassicFxEffectType.FeatherForeign:
                    float fade = MathF.Pow(0.97f, f);
                    e.Light *= fade;
                    e.Alpha *= fade;
                    if (e.Type == ClassicFxEffectType.FeatherForeign ||
                        e.SubType is 1 or 2 or 3)
                        e.Scale *= MathF.Pow(e.SubType == 1 ? 0.99f : 0.97f, f);
                    e.Angle += e.HeadAngle * f;
                    return true;

                case ClassicFxEffectType.SapitresAttack1:
                {
                    e.Position += e.Direction * (40f * f);
                    if (Clock.AdvancedReferenceFrame)
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            e.Position, 3f, new Vector3(0.1f, 0.3f, 1f));
                    if (e.LifeTime <= 1f && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        for (int i = 0; i < 3; ++i)
                            CreateEffect(ClassicFxEffectType.SapitresAttack2,
                                e.Position, e.Angle, new Vector3(0.1f, 0.3f, 1f),
                                ClassicFxOwner.None, subType: 0);
                    }
                    // Hit/collision handling stays with gameplay.
                    return true;
                }
                case ClassicFxEffectType.SapitresAttack2:
                {
                    if (e.SubType == 14)
                    {
                        e.Position -= e.Direction * ((25f + Random.Modulo(5)) * f);
                        if (e.LifeTime <= 10f)
                            e.BlendMeshLight -= 0.1f * f;
                    }
                    else
                    {
                        e.Position += e.Direction * f;
                        e.Direction *= MathF.Pow(0.9f, f);
                        e.Position.Z += e.Gravity * f;
                        if (e.SubType is 0 or 13)
                        {
                            e.Gravity -= 3f * f;
                            float ground = RequestTerrainHeight(
                                e.Position.X, e.Position.Y);
                            if (e.Position.Z < ground)
                            {
                                e.Position.Z = ground;
                                e.Gravity = -e.Gravity * 0.5f;
                                e.LifeTime -= 4f * f;
                            }
                        }
                    }
                    return true;
                }
                case ClassicFxEffectType.FlameStrike:
                {
                    // Native two-stage fade; owner animation and bones drive
                    // three additive object blurs through another subsystem.
                    if (!TryGetOwnerSnapshot(e.Owner, out _))
                        return false;
                    if (e.LifeTime < 20f)
                    {
                        e.Alpha -= 0.1f * f;
                        if (e.Alpha < 0.1f)
                            return false;
                    }
                    else if (e.Alpha < 1f)
                        e.Alpha = MathF.Min(1f, e.Alpha + 0.1f * f);
                    return true;
                }
                default:
                    return false;
            }
        }

        private bool MoveS6Batch15Ice(ref EffectState e, float f)
        {
            if (e.SubType == 0)
            {
                e.HeadAngle.Z -= e.Gravity * f;
                e.Position += e.HeadAngle * f;
                float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                e.Angle.X += MathHelper.ToRadians(0.5f * e.LifeTime * f);
                e.Angle.Y += MathHelper.ToRadians(0.5f * e.LifeTime * f);
                if (e.Position.Z + e.Direction.Z <= ground)
                {
                    e.Position.Z = ground;
                    e.HeadAngle.X *= MathF.Pow(0.6f, f);
                    e.HeadAngle.Y *= MathF.Pow(0.6f, f);
                    e.HeadAngle.Z += e.LifeTime * f;
                    if (e.HeadAngle.Z < 0.5f)
                        e.HeadAngle.Z = 0f;
                    e.Alpha = MathF.Max(0f, e.Alpha - 0.1f * f);
                }
                if (Clock.AdvancedReferenceFrame &&
                    Random.FpsCheck(30, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Light);
                return true;
            }

            if (e.SubType == 1)
            {
                // Native Set Angle = HeadAngle, then terrain collision
                // creates Inferno2 + Smoke11 + Ice chips + ExplosionMono.
                e.Angle = e.HeadAngle;
                if (e.Position.Z >= RequestTerrainHeight(
                    e.Position.X, e.Position.Y))
                    return true;

                Vector3 impact = e.Position;
                impact.Z = RequestTerrainHeight(impact.X, impact.Y);
                Vector3 blue = new Vector3(0.2f, 0.4f, 0.8f);
                CreateEffect(ClassicFxEffectType.SkillInferno,
                    impact, Vector3.Zero, new Vector3(0f, 0.6f, 1f),
                    ClassicFxOwner.None, subType: 2,
                    boneIndex: 0, scale: 1f);
                for (int i = 0; i < 8; ++i)
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        impact + new Vector3(Random.Modulo(80) - 40f,
                            Random.Modulo(80) - 40f, 50f),
                        e.Angle, blue, subType: 11,
                        scale: (60f + Random.Modulo(40)) * 0.025f);
                for (int i = 0; i < 6; ++i)
                {
                    var child = (ClassicFxEffectType)(
                        (int)ClassicFxEffectType.BrokenIce0 + Random.Modulo(3));
                    CreateEffect(child, impact + new Vector3(0f, 0f, 50f),
                        e.Angle, blue, ClassicFxOwner.None);
                }
                CreateParticle(ClassicTextureIds.BitmapExplotionMono,
                    impact + new Vector3(0f, 0f, 100f),
                    e.Angle, new Vector3(0.6f, 0.6f, 1f),
                    subType: 1, scale: 1.5f);
                return false;
            }

            e.Position.Z -= e.Gravity * f;
            if (e.Position.Z < RequestTerrainHeight(e.Position.X, e.Position.Y))
            {
                CreateEffect(e.Type, e.Position, e.Angle,
                    e.Light, ClassicFxOwner.None, subType: 0);
                return false;
            }
            return true;
        }

        private void MoveS6StarShine(ref EffectState e, float f)
        {
            if (e.LifeTime <= 10f)
            {
                e.Scale *= MathF.Pow(0.9f, f);
                e.Alpha *= MathF.Pow(0.9f, f);
                e.Light *= MathF.Pow(0.9f, f);
            }
            else if (e.LifeTime >= 20f)
            {
                e.Scale *= MathF.Pow(1.1f, f);
                e.Alpha *= MathF.Pow(1.1f, f);
                e.Light *= MathF.Pow(1.1f, f);
            }
            if (Clock.AdvancedReferenceFrame)
            {
                CreateSprite(ClassicTextureIds.BitmapShiny,
                    e.Position, e.Scale, e.Light,
                    rotation: e.Angle.X);
            }
        }

        private void MoveS6SapitresCarrier(ref EffectState e, float f)
        {
            // Native MODEL_EFFECT_SAPITRES_ATTACK emits guided projectile
            // every six frames. Support a bounded pool and world owner.
            if (!Clock.AdvancedReferenceFrame || !e.Owner.HasOwner)
                return;
            int tick = (int)e.LifeTime;
            if (tick % 6 != 0 || tick == e.LastChildNativeTick)
                return;
            e.LastChildNativeTick = tick;
            Vector3 position = e.Position + new Vector3(
                Random.Modulo(120) - 60f,
                Random.Modulo(120) - 60f, 0f);
            CreateEffect(ClassicFxEffectType.SapitresAttack1,
                position, e.Angle, e.Light, e.Owner);
        }
    }
}
