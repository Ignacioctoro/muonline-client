// BroyalMU ClassicFX S6 Batch 38: ten native BITMAP_* effect roots.
// MuMain reference 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Uses existing Effect/Particle/Sprite/Joint pools; no BMD placeholders.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch38LogicalType(ClassicFxEffectType type, int sub) =>
            type switch
            {
                ClassicFxEffectType.FlameEmitter => sub is 0 or 1 or 2 or 3 or 5 or 6,
                ClassicFxEffectType.CursedLichFireEmitter => sub is 1 or 12,
                ClassicFxEffectType.SparkFountainEmitter => sub == 0,
                ClassicFxEffectType.SparkOwnerEmitter => sub == 0,
                ClassicFxEffectType.EnergyEmitter => sub == 0,
                ClassicFxEffectType.ShinyRingEmitter => sub == 0,
                ClassicFxEffectType.ShinyScatterEmitter => sub == 0,
                ClassicFxEffectType.LightningTerrain2Emitter => sub is 0 or 1,
                ClassicFxEffectType.SwordMonoEmitter => sub is 0 or 1 or 2,
                ClassicFxEffectType.ImpactEmitter => sub == 0,
                _ => false
            };

        private static bool S6Batch38NeedsOwner(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.SparkOwnerEmitter or
                ClassicFxEffectType.ImpactEmitter;

        private static bool IsS6Batch38TerrainType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.FlameEmitter or
                ClassicFxEffectType.LightningTerrain2Emitter;

        private void InitializeS6Batch38Logical(
            ClassicFxEffectType type, int sub,
            ref Vector3 position, ref Vector3 angle,
            ref float scale, ref float alpha, ref Vector3 direction,
            out float life)
        {
            life = 30f;
            switch (type)
            {
                case ClassicFxEffectType.FlameEmitter:
                    life = sub switch
                    {
                        0 or 6 => 40f,
                        1 or 2 => 10f,
                        3 => 15f,
                        5 => 20f,
                        _ => 30f
                    };
                    if (sub is 1 or 2) angle = Vector3.Zero;
                    break;
                case ClassicFxEffectType.CursedLichFireEmitter:
                    life = sub == 1 ? 50f : 20f;
                    break;
                case ClassicFxEffectType.SparkFountainEmitter:
                    life = 10f;
                    break;
                case ClassicFxEffectType.SparkOwnerEmitter:
                    life = 100f;
                    break;
                case ClassicFxEffectType.EnergyEmitter:
                    life = 20f;
                    direction = new Vector3(0f, -60f, 0f);
                    position.Z += 100f * Clock.FrameFactor;
                    break;
                case ClassicFxEffectType.ShinyRingEmitter:
                    life = 16f;
                    break;
                case ClassicFxEffectType.ShinyScatterEmitter:
                    life = 24f;
                    break;
                case ClassicFxEffectType.LightningTerrain2Emitter:
                    life = sub == 0 ? 10f : 50f;
                    if (sub == 0) scale = 1.5f;
                    else alpha = 0.01f;
                    break;
                case ClassicFxEffectType.SwordMonoEmitter:
                    life = 15f;
                    break;
                case ClassicFxEffectType.ImpactEmitter:
                    life = 80f;
                    scale = 0f;
                    break;
            }
        }

        private bool MoveS6Batch38Logical(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.FlameEmitter:
                    if (Clock.AdvancedReferenceFrame)
                        EmitS6Batch38Flame(ref e);
                    return true;

                case ClassicFxEffectType.CursedLichFireEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(e.SubType == 12 ? 50 : 100) -
                                (e.SubType == 12 ? 25 : 50),
                            Random.Modulo(e.SubType == 12 ? 50 : 100) -
                                (e.SubType == 12 ? 25 : 50),
                            5 + Random.Modulo(10));
                        float scale = e.SubType == 12
                            ? (13 + Random.Modulo(5)) * 0.05f * e.Scale
                            : (13 + Random.Modulo(5)) * 0.1f;
                        int tex = Random.Modulo(3) switch
                        {
                            0 => ClassicTextureIds.BitmapFireHik1,
                            1 => ClassicTextureIds.BitmapFireCursedLich,
                            _ => ClassicTextureIds.BitmapFireHik3
                        };
                        CreateParticle(tex, p, e.Angle, e.Light,
                            tex == ClassicTextureIds.BitmapFireCursedLich
                                ? 5 : 1, scale);
                    }
                    return true;

                case ClassicFxEffectType.SparkFountainEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 p = e.Position;
                        Vector3 light = new Vector3(e.LifeTime * 0.1f);
                        for (int j = 0; j < 18; j++)
                        {
                            p.Z += 24f;
                            CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                                p, e.Angle, light, 1, j == 0 ? 12f : 6f);
                        }
                    }
                    return true;

                case ClassicFxEffectType.SparkOwnerEmitter:
                    if (!IsS6Batch38OwnerAlive(e.Owner))
                        return false;
                    e.LifeTime = 100f;
                    if (Clock.AdvancedReferenceFrame &&
                        Random.Modulo(60) == 0)
                        CreateParticle(ClassicTextureIds.BitmapSpark + 2,
                            e.Position, e.Angle, e.Light, e.SubType,
                            0.5f, e.Owner);
                    return true;

                case ClassicFxEffectType.EnergyEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 light = new Vector3(e.LifeTime * 0.2f);
                        CreateParticle(ClassicTextureIds.BitmapEnergy,
                            e.Position, e.Angle, light);
                        CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                            e.Position, e.Angle, light, 0, 4f);
                    }
                    // Native also requests AddTerrainLight and target checks.
                    return true;

                case ClassicFxEffectType.ShinyRingEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(100) - 40,
                            Random.Modulo(100) - 40,
                            Random.Modulo(100) + 50);
                        CreateParticle(ClassicTextureIds.BitmapShiny + 4,
                            p, e.Angle, e.Light, 2, e.Scale);
                    }
                    return true;

                case ClassicFxEffectType.ShinyScatterEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(500) - 250,
                            Random.Modulo(500) - 250,
                            150f - Random.Modulo(100));
                        CreateParticle(ClassicTextureIds.BitmapShiny + 6,
                            p, e.Angle, e.Light, 0, e.Scale);
                    }
                    return true;

                case ClassicFxEffectType.LightningTerrain2Emitter:
                    e.Scale += 0.2f * f;
                    return true;

                case ClassicFxEffectType.SwordMonoEmitter:
                    if (Clock.AdvancedReferenceFrame)
                    {
                        int nativeTick = (int)e.LifeTime;
                        if (nativeTick % 3 != 0)
                            CreateParticle(ClassicTextureIds.BitmapSwordEffectMono,
                                e.Position, e.Angle, e.Light, 0,
                                e.Scale, e.Owner);
                        if (nativeTick <= 14 && (e.TriggerMask & 1) == 0)
                        {
                            e.TriggerMask |= 1;
                            Vector3 light = e.SubType switch
                            {
                                1 => new Vector3(0.7f, 0.5f, 1f),
                                2 => new Vector3(1f, 0.12f, 0f),
                                _ => new Vector3(0.4f, 0.4f, 1f)
                            };
                            CreateEffect(ClassicFxEffectType.Shockwave01,
                                e.Position, e.Angle, light, e.Owner,
                                subType: 2, scale: 1f);
                        }
                    }
                    return true;

                case ClassicFxEffectType.ImpactEmitter:
                    return MoveS6Batch38Impact(ref e, f);
            }
            return false;
        }

        private bool IsS6Batch38OwnerAlive(ClassicFxOwner owner) =>
            owner.WorldObject != null &&
            ReferenceEquals(owner.WorldObject.World, World) &&
            owner.WorldObject.Status == GameControlStatus.Ready;

        private bool MoveS6Batch38Impact(ref EffectState e, float f)
        {
            if (!IsS6Batch38OwnerAlive(e.Owner))
                return false;

            float renderScale;
            if (e.Scale < 2f)
            {
                e.Scale += 0.1f * f;
                renderScale = e.Scale;
            }
            else
            {
                if (e.Scale < 2.4f) e.Scale += 0.02f * f;
                else e.Scale = 2f;
                renderScale = e.Scale >= 2.2f
                    ? 2.2f - (e.Scale - 2.2f) : e.Scale;
            }
            if (e.LifeTime <= 40f)
            {
                e.Alpha = MathF.Max(0f, e.Alpha - 0.1f * f);
                e.BlendMeshLight = MathF.Max(0f,
                    e.BlendMeshLight - 0.1f * f);
            }
            var obj = e.Owner.WorldObject;
            Vector3 local = new Vector3(-10f, -30f, 0f);
            e.Position = obj.WorldPosition.Translation +
                Vector3.TransformNormal(local,
                    Matrix.CreateRotationZ(obj.Angle.Z));
            e.Position.Z += 130f * f;
            if (!Clock.AdvancedReferenceFrame) return true;
            Vector3 light = e.Light * e.Alpha;
            float spin = (float)Clock.WorldTimeMilliseconds * 0.08f;
            CreateSprite(ClassicTextureIds.BitmapImpact,
                e.Position, renderScale, light, e.Owner);
            CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                e.Position, renderScale, light, e.Owner, -spin);
            if (e.Scale > 1f)
                CreateSprite(ClassicTextureIds.BitmapOrora,
                    e.Position, renderScale, light, e.Owner, -spin);
            if (e.Scale > 2f)
                CreateSprite(ClassicTextureIds.BitmapOrora,
                    e.Position, renderScale, light, e.Owner, spin);
            return true;
        }

        private void EmitS6Batch38Flame(ref EffectState e)
        {
            int sub = e.SubType;
            if (sub == 0)
            {
                for (int j = 0; j < 6; j++)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(50) - 25, Random.Modulo(50) - 25, 0f);
                    CreateParticle(ClassicTextureIds.BitmapFlame,
                        p, e.Angle, Vector3.One);
                }
                if (Random.Modulo(8) == 0)
                    CreateEffect(Random.Modulo(2) == 0
                            ? ClassicFxEffectType.Stone1
                            : ClassicFxEffectType.Stone2,
                        e.Position, e.Angle, e.Light, e.Owner);
            }
            else if (sub is 1 or 2)
            {
                for (int j = 0; j < 18; j++)
                {
                    float angle = MathHelper.ToRadians(j * 20f);
                    Vector3 p = e.Position + new Vector3(
                        -250f * MathF.Sin(angle) + Random.Modulo(64) - 32,
                        250f * MathF.Cos(angle) + Random.Modulo(64) - 32,
                        0f);
                    CreateParticle(sub == 1
                            ? ClassicTextureIds.BitmapFlame
                            : ClassicTextureIds.BitmapFire + 3,
                        p, e.Angle, Vector3.One, sub == 1 ? 0 : 13,
                        sub == 1 ? 1.2f : 2.5f);
                }
            }
            else if (sub == 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    CreateParticle(ClassicTextureIds.BitmapFlame,
                        e.Position, e.Angle, Vector3.One, 6);
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(10) - 5, Random.Modulo(10) - 5, 40f);
                    CreateParticle(ClassicTextureIds.BitmapTrueFire,
                        p, e.Angle, Vector3.One, 0, 2.8f);
                }
                Vector3 smoke = e.Position + new Vector3(
                    Random.Modulo(10) - 5,
                    Random.Modulo(10) - 5, -40f);
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    smoke, e.Angle, Vector3.One, 21, 0.8f);
            }
            else if (sub is 5 or 6)
            {
                Vector3 p = e.Position + new Vector3(
                    Random.Modulo(32) - 16,
                    Random.Modulo(32) - 16, 0f);
                CreateParticle(ClassicTextureIds.BitmapFlame,
                    p, e.Angle, sub == 5 ? Vector3.One : e.Light,
                    sub == 5 ? 0 : 12, e.Scale);
            }
        }

        private void RenderS6Batch38Terrain(ref EffectState e)
        {
            int texture = e.Type == ClassicFxEffectType.FlameEmitter
                ? ClassicTextureIds.BitmapFlame
                : ClassicTextureIds.BitmapLightning + 1;
            if (e.Type == ClassicFxEffectType.FlameEmitter &&
                e.SubType is 3 or 6)
                return;
            if (!Textures.TryGet(texture, out ClassicTextureResource res))
                return;
            float size = e.Type == ClassicFxEffectType.FlameEmitter
                ? 2f : e.Scale;
            Vector3 light = e.Type == ClassicFxEffectType.FlameEmitter
                ? new Vector3(0.8f) : new Vector3(e.LifeTime * 0.1f);
            QueueTerrainEffect(ref e, res,
                scaleOverride: size, lightOverride: light);
        }
    }
}
