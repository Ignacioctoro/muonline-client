// Season 6 Batch 30: Cursed Temple, PK field and physical stone models.
// Native pinned MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Existing ClassicFX pool and BMD ModelObject; no parallel renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch30ModelType(ClassicFxEffectType t) =>
            t >= ClassicFxEffectType.CursedTempleStatuePart1 &&
            t <= ClassicFxEffectType.FallStoneEffect;
        private static bool IsS6Batch30Statue(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.CursedTempleStatuePart1 or
                ClassicFxEffectType.CursedTempleStatuePart2;
        private static bool IsS6Batch30Head(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.PkFieldAssassinGreenHead or
                ClassicFxEffectType.PkFieldAssassinRedHead;
        private static bool IsS6Batch30Body(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.PkFieldAssassinGreenBody or
                ClassicFxEffectType.PkFieldAssassinRedBody;
        private static bool IsS6Batch30Coffin(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.StoneCoffin1 or
                ClassicFxEffectType.StoneCoffin2;
        private static bool IsS6Batch30FlyStone(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.FlyBigStone1 or
                ClassicFxEffectType.FlyBigStone2;

        private static bool TryGetS6Batch30ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch30ModelType(type)) return false;
            // Catapult subtypes involve hit testing, server messages and
            // Hero camera state. Only native free debris subtype 2 is safe.
            if (IsS6Batch30FlyStone(type) && subType != 2) return false;
            if (type == ClassicFxEffectType.FallStoneEffect &&
                (subType < 0 || subType > 3)) return false;
            if (IsS6Batch30Coffin(type) && subType is not (0 or 1))
                return false;
            if (!IsS6Batch30Coffin(type) &&
                !IsS6Batch30FlyStone(type) &&
                type != ClassicFxEffectType.FallStoneEffect && subType != 0)
                return false;

            string path = type switch
            {
                ClassicFxEffectType.CursedTempleStatuePart1 => "NPC/songck1.bmd",
                ClassicFxEffectType.CursedTempleStatuePart2 => "NPC/songck2.bmd",
                ClassicFxEffectType.PkFieldAssassinGreenHead => "Monster/pk_manhead_green.bmd",
                ClassicFxEffectType.PkFieldAssassinRedHead => "Monster/pk_manhead_red.bmd",
                ClassicFxEffectType.PkFieldAssassinGreenBody => "Monster/assassin_dieg.bmd",
                ClassicFxEffectType.PkFieldAssassinRedBody => "Monster/assassin_dier.bmd",
                ClassicFxEffectType.StoneCoffin1 => "Object12/StoneCoffin01.bmd",
                ClassicFxEffectType.StoneCoffin2 => "Object12/StoneCoffin02.bmd",
                ClassicFxEffectType.FlyBigStone1 => "Skill/Flybigstone1.bmd",
                ClassicFxEffectType.FlyBigStone2 => "Skill/Flybigstone2.bmd",
                ClassicFxEffectType.FallStoneEffect => "Object47/Stoneeffec.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = IsS6Batch30Statue(type) ? 180f :
                IsS6Batch30Head(type) ? 50f :
                IsS6Batch30Body(type) ? 40f :
                IsS6Batch30Coffin(type) ? 32f :
                IsS6Batch30FlyStone(type) ? 50f :
                subType == 3 ? 15f : 100f;
            definition = new Season6ModelDefinition(path, life, 1f,
                useCallerScale: type == ClassicFxEffectType.FallStoneEffect);
            return true;
        }

        private static void ConfigureS6Batch30ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (IsS6Batch30Body(type))
                view.AnimationSpeed = 10f; // Native 0.4 frames/tick at 25Hz.
        }

        private bool InitializeS6Batch30Model(
            ClassicFxEffectType type, ref int subType, ref Vector3 position,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref Vector3 direction,
            ref Vector3 heading, ref float gravity, ref float velocity)
        {
            if (IsS6Batch30Statue(type))
            {
                life = 180f + Random.Modulo(40);
                scale = 0.1f + 0.1f * Random.Modulo(6);
                velocity = 0f;
                gravity = 2.5f;
                direction = new Vector3(Random.Modulo(30) - 15f,
                    Random.Modulo(30) - 15f, 0f);
                angle = new Vector3(0f, 0f,
                    MathHelper.ToRadians(Random.Modulo(360)));
                light = Vector3.One * (0.5f + 0.1f * Random.Modulo(6));
                return true;
            }
            if (IsS6Batch30Head(type))
            {
                life = 50f + Random.Modulo(30);
                scale = 1f;
                velocity = 1f;
                gravity = 3.5f;
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                heading = Vector3.TransformNormal(
                    new Vector3(0f, (Random.Modulo(64) + 25f) * 0.2f, 0f),
                    Matrix.CreateRotationZ(angle.Z));
                heading.Z = 25f;
                subType = Random.Modulo(2);
                direction = Vector3.Zero;
                return true;
            }
            if (IsS6Batch30Body(type))
            {
                scale = 1f;
                life = 40f + Random.Modulo(30);
                velocity = 0.4f;
                return true;
            }
            if (IsS6Batch30Coffin(type))
            {
                position.Z += 50f * Clock.FrameFactor;
                life = 32f + Random.Modulo(16);
                scale = (Random.Modulo(4) + 8f) * 0.1f;
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                direction = Vector3.TransformNormal(
                    new Vector3(0f, (Random.Modulo(128) + 32f) * 0.1f, 0f),
                    Matrix.CreateRotationZ(angle.Z));
                gravity = 2f + Random.Modulo(5);
                if (type == ClassicFxEffectType.StoneCoffin2 && subType == 0)
                {
                    subType = 1;
                    gravity += Random.Modulo(5) * Clock.FrameFactor;
                }
                angle = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)));
                return true;
            }
            if (IsS6Batch30FlyStone(type))
            {
                life = 50f;
                scale = 0.7f + Random.Modulo(8) / 20f;
                gravity = 10f;
                angle.X = MathHelper.ToRadians(Random.Modulo(360));
                direction = Vector3.Zero;
                heading = new Vector3(0f, 0f,
                    MathHelper.ToRadians(Random.Modulo(360)));
                return true;
            }
            if (type == ClassicFxEffectType.FallStoneEffect)
            {
                if (subType is 0 or 1)
                {
                    life = 100f + Random.Modulo(5);
                    scale = subType == 0
                        ? (Random.Modulo(20) + 5f) * 0.005f + scale * 0.05f
                        : (Random.Modulo(10) + 5f) * 0.02f + scale * 0.05f;
                    position.X += (Random.Modulo(100) - 50f) * Clock.FrameFactor;
                    position.Y += (Random.Modulo(100) - 50f) * Clock.FrameFactor;
                    gravity = (Random.Modulo(10) + 10f) * 0.5f;
                }
                else if (subType == 2)
                {
                    life = 100f;
                    scale += (Random.Modulo(20) + 5f) * 0.05f;
                    gravity = (Random.Modulo(20) + 40f) * 0.5f + scale * 2f;
                }
                else
                {
                    life = 15f + Random.Modulo(5);
                    gravity = 3f + Random.Modulo(3);
                    angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                    heading = Vector3.TransformNormal(
                        new Vector3(0f, (Random.Modulo(60) + 30f) * 0.1f, 0f),
                        Matrix.CreateRotationZ(angle.Z));
                    heading.Z += 25f * Clock.FrameFactor;
                    direction = Vector3.Zero;
                    return true;
                }
                angle = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)));
                return true;
            }
            return false;
        }

        private bool MoveS6Batch30Model(ref EffectState e, float f)
        {
            if (IsS6Batch30Statue(e.Type))
            {
                // Native Cursed Temple ground plane is Z=290.
                e.Position += e.Direction * f;
                if (e.Position.Z > 290f)
                {
                    e.Direction.Z -= e.Gravity * f;
                    if (Clock.AdvancedReferenceFrame)
                        e.Angle.Z -= MathHelper.ToRadians(Random.Modulo(10)) * f;
                }
                else if (e.Position.Z < 290f)
                {
                    e.Position.Z = 290f;
                    e.Direction = Vector3.Zero;
                    // No global EarthQuake camera mutation in visual runtime.
                }
                else e.Alpha -= 0.01f * f;
                return true;
            }
            if (IsS6Batch30Head(e.Type))
            {
                e.HeadAngle.Z -= e.Gravity * f;
                e.Position += e.HeadAngle * f;
                float height = RequestTerrainHeight(e.Position.X, e.Position.Y) + 20f;
                if (e.Position.Z + e.Direction.Z <= height)
                {
                    e.Position.Z = height;
                    e.HeadAngle.X *= MathF.Pow(0.8f, f);
                    e.HeadAngle.Y *= MathF.Pow(0.8f, f);
                    e.HeadAngle.Z += 0.6f * e.LifeTime * f;
                    if (e.HeadAngle.Z < 5f) e.HeadAngle.Z = 0f;
                    e.Alpha -= 0.05f * f;
                }
                else
                {
                    float turn = MathHelper.ToRadians(0.15f * e.LifeTime * f);
                    e.Angle.X += e.SubType == 0 ? turn : -turn;
                    e.Angle.Y += e.SubType == 0 ? turn : -turn;
                }
                // Native BITMAP_FIRE_CURSEDLICH child Effect needs a proper
                // bitmap-effect bridge, not a substitute generic particle.
                return true;
            }
            if (IsS6Batch30Body(e.Type))
            {
                if (e.LifeTime < 20f) e.Alpha = 0.1f * e.LifeTime;
                return true;
            }
            if (IsS6Batch30Coffin(e.Type))
            {
                e.Position += e.Direction * f;
                e.Direction *= MathF.Pow(e.SubType == 1 ? 0.99f : 0.9f, f);
                e.Position.Z += e.Gravity * f;
                e.Gravity -= 3f * f;
                float height = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < height)
                {
                    e.Position.Z = height;
                    e.Gravity = -e.Gravity * 0.2f;
                    e.LifeTime -= 5f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
                }
                else e.Angle.X -= MathHelper.ToRadians(e.Scale * 16f * f);
                e.Alpha = e.LifeTime / 10f;
                if (Clock.AdvancedReferenceFrame && Random.FpsCheck(10, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke + 1,
                        e.Position, e.Angle, e.Light);
                return true;
            }
            if (IsS6Batch30FlyStone(e.Type))
            {
                e.Position.Z -= e.Gravity * f;
                e.Gravity += 1.9f * f;
                if (Clock.AdvancedReferenceFrame)
                {
                    e.Angle.X += MathHelper.ToRadians(Random.Modulo(20) + 20f) * f;
                    e.Angle.Y += MathHelper.ToRadians(Random.Modulo(5)) * f;
                }
                float height = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < height)
                {
                    e.Position.Z = height + 50f;
                    e.Gravity = -e.Gravity * 0.2f;
                    e.LifeTime -= 5f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
                    e.Direction = new Vector3(0f, 5f, 0f);
                }
                else e.Angle.X -= MathHelper.ToRadians(e.Scale * 16f * f);
                e.Position += Vector3.TransformNormal(e.Direction,
                    Matrix.CreateRotationZ(e.HeadAngle.Z)) * f;
                if (e.LifeTime < 10f)
                    e.Alpha *= MathF.Pow(1f / 1.5f, f);
                if (Clock.AdvancedReferenceFrame &&
                    (int)MathF.Ceiling(e.LifeTime) % 3 == 0)
                    CreateParticle(ClassicTextureIds.BitmapAdvSmoke + 1,
                        e.Position, e.Angle,
                        new Vector3(1f, 0.6f, 0.2f), 1, 0.2f);
                return true;
            }
            if (e.Type == ClassicFxEffectType.FallStoneEffect)
                return MoveS6Batch30FallStone(ref e, f);
            return false;
        }

        private bool MoveS6Batch30FallStone(ref EffectState e, float f)
        {
            if (e.SubType <= 2)
            {
                e.Position.Z -= e.Gravity * f;
                e.Gravity += 0.1f * f;
                if (e.SubType <= 1)
                {
                    float turn = MathHelper.ToRadians(0.5f * f);
                    e.Angle += new Vector3(turn, turn, turn);
                }
                else if (e.Position.Z <
                    RequestTerrainHeight(e.Position.X, e.Position.Y))
                {
                    if (Clock.AdvancedReferenceFrame)
                    {
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position, e.Angle, new Vector3(0.5f),
                            11, (Random.Modulo(20) + 30f) * 0.02f);
                        int count = Random.Modulo(2) + 2;
                        for (int i = 0; i < count; ++i)
                            CreateEffect(ClassicFxEffectType.FallStoneEffect,
                                e.Position, e.Angle, e.Light,
                                ClassicFxOwner.None, subType: 3,
                                scale: 0.03f + Random.Modulo(10) / 40f +
                                    e.Scale * 0.3f);
                    }
                    return false; // Native o->Live=false on impact.
                }
                return true;
            }
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float height = RequestTerrainHeight(e.Position.X, e.Position.Y);
            e.Angle.X += MathHelper.ToRadians(0.5f * e.LifeTime * f);
            e.Angle.Y += MathHelper.ToRadians(0.5f * e.LifeTime * f);
            if (e.Position.Z + e.Direction.Z <= height)
            {
                e.Position.Z = height;
                e.HeadAngle.X *= MathF.Pow(0.6f, f);
                e.HeadAngle.Y *= MathF.Pow(0.6f, f);
                e.HeadAngle.Z += e.LifeTime * f;
                if (e.HeadAngle.Z < 0.5f) e.HeadAngle.Z = 0f;
                e.Alpha -= 0.15f * f;
                e.Light *= MathF.Pow(1f / 1.08f, f);
            }
            return true;
        }
    }
}
