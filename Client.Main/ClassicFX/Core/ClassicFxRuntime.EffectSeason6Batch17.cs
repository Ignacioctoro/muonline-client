// ClassicFX Season 6 Batch 17: original Halloween, Moon Harvest, Change Up
// and bow-effect model families from pinned sven-n/MuMain:
// 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// BMDs validated against Ignacioctoro/Data_Broyal.
// Uses the existing Effects pool, ModelObject, Sprite/Particle/Joint pools.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch17ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.HalloweenCandyBlue and <=
                ClassicFxEffectType.ArrowRing;

        private static bool IsS6Batch17Candy(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.HalloweenCandyBlue and <=
                ClassicFxEffectType.HalloweenCandyStar;

        private static bool IsS6Batch17HarvestFruit(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.MoonHarvestGam and <=
                ClassicFxEffectType.MoonHarvestSongpuen2;

        private static bool IsS6Batch17Arrow(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.ArrowBestCrossbow and <=
                ClassicFxEffectType.ArrowRing;

        private static bool TryGetS6Batch17ModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch17ModelType(type) || subtype < 0) return false;
            if (IsS6Batch17Candy(type) && subtype > 1) return false;
            if (IsS6Batch17HarvestFruit(type) && subtype != 0) return false;
            if (type == ClassicFxEffectType.MoonHarvestMoon && subtype > 2) return false;
            if (type == ClassicFxEffectType.ChangeUpNasa && subtype > 3) return false;
            if (type is ClassicFxEffectType.ChangeUpEffect or
                ClassicFxEffectType.ChangeUpCylinder && subtype > 2) return false;
            if (type == ClassicFxEffectType.ArrowDrill && subtype is not (0 or 2))
                return false;
            if (IsS6Batch17Arrow(type) && subtype > 2) return false;

            string path = type switch
            {
                ClassicFxEffectType.HalloweenCandyBlue => "Skill/hcandyblue.bmd",
                ClassicFxEffectType.HalloweenCandyOrange => "Skill/hcandyorange.bmd",
                ClassicFxEffectType.HalloweenCandyYellow => "Skill/hcandyyellow.bmd",
                ClassicFxEffectType.HalloweenCandyRed => "Skill/hcandyred.bmd",
                ClassicFxEffectType.HalloweenCandyHobak => "Skill/hhobak.bmd",
                ClassicFxEffectType.HalloweenCandyStar => "Skill/hstar.bmd",
                ClassicFxEffectType.MoonHarvestGam => "Effect/chusukgam.bmd",
                ClassicFxEffectType.MoonHarvestSongpuen1 => "Effect/chusukseung1.bmd",
                ClassicFxEffectType.MoonHarvestSongpuen2 => "Effect/chusukseung2.bmd",
                ClassicFxEffectType.MoonHarvestMoon => "Effect/chysukmoon.bmd",
                ClassicFxEffectType.ChangeUpEffect => "Effect/Change_Up_Eff.bmd",
                ClassicFxEffectType.ChangeUpNasa => "Effect/changup_nasa.bmd",
                ClassicFxEffectType.ChangeUpCylinder => "Effect/clinderlight.bmd",
                ClassicFxEffectType.ArrowBestCrossbow => "Skill/kcross.bmd",
                ClassicFxEffectType.ArrowDrill => "Skill/Carow.bmd",
                ClassicFxEffectType.ArrowRing => "Skill/CW_Bow_Skill.bmd",
                _ => null
            };
            if (path == null) return false;
            bool owner = subtype == 2 && type is
                ClassicFxEffectType.ChangeUpEffect or
                ClassicFxEffectType.ChangeUpCylinder;
            definition = new Season6ModelDefinition(path, 50f, 1f,
                needsOwner: owner, useCallerScale: true);
            return true;
        }

        private bool InitializeS6Batch17Model(
            ClassicFxEffectType type, int subtype, ClassicFxOwner owner,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref Vector3 direction, ref float gravity,
            ref Vector3 headAngle, ref float velocity, ref float meshLight)
        {
            float f = Clock.FrameFactor;
            if (IsS6Batch17Candy(type) || IsS6Batch17HarvestFruit(type))
            {
                bool candy = IsS6Batch17Candy(type);
                if (candy && subtype == 1)
                    scale = 2f + (Random.Modulo(10) - 5f) * 0.02f;
                else if (candy)
                {
                    bool big = type is ClassicFxEffectType.HalloweenCandyHobak or
                        ClassicFxEffectType.HalloweenCandyStar;
                    scale = (big ? 2f : 0.6f) +
                        (Random.Modulo(10) - 5f) * 0.02f;
                }
                else
                    scale = (type == ClassicFxEffectType.MoonHarvestGam
                        ? 0.5f : 0.8f) +
                        (Random.Modulo(10) - 5f) * 0.02f;

                life = (candy && subtype == 1 ? 40f : 50f) + Random.Modulo(10);
                angle = new Vector3(
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)),
                    MathHelper.ToRadians(Random.Modulo(360)));
                gravity = 10f + Random.Modulo(10);
                float dx = (Random.Modulo(candy ? 60 : 10) - (candy ? 30f : 5f)) * 0.1f;
                float dy = (Random.Modulo(60) - (candy ? 30f : 30f)) * 0.1f;
                float dz = candy && subtype == 1 ? -1f : 0f;
                direction = Vector3.TransformNormal(
                    new Vector3(dx, dy, dz) * (candy
                        ? subtype == 1 ? 1.5f : 2f : 1.2f),
                    Matrix.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z));
                // Native m_iAnimation chooses which angular axis rotates.
                headAngle.X = candy ? Random.Modulo(3) : 0f;
                return true;
            }

            switch (type)
            {
                case ClassicFxEffectType.MoonHarvestMoon:
                    if (subtype == 0) { life = 70f; alpha = 0.6f; }
                    else if (subtype == 1)
                    {
                        life = 50f;
                        direction = angle; // native copy: direction=call Angle
                        angle = Vector3.Zero;
                    }
                    else life = 1f;
                    break;

                case ClassicFxEffectType.ChangeUpEffect:
                    life = subtype == 1 ? 10f : 100f;
                    scale = subtype == 1 ? 0.4f : 0.7f;
                    meshLight = subtype == 1 ? 0.7f : 1f;
                    position.Z += 22f * f;
                    break;

                case ClassicFxEffectType.ChangeUpNasa:
                    life = subtype == 0 ? 100f : 80f;
                    scale = 0.9f;
                    position.Z += 12f * f;
                    break;

                case ClassicFxEffectType.ChangeUpCylinder:
                    life = subtype == 1 ? 10f : 100f;
                    scale = subtype == 1 ? 0.1f : 0.9f;
                    if (subtype == 1) light = Vector3.Zero;
                    direction = Vector3.UnitZ;
                    break;

                case ClassicFxEffectType.ArrowBestCrossbow:
                case ClassicFxEffectType.ArrowDrill:
                    life = 30f;
                    scale = 1f;
                    position.Z += 130f * f;
                    direction = new Vector3(0f, -70f, 0f);
                    if (type == ClassicFxEffectType.ArrowDrill)
                        gravity = -10f;
                    break;

                case ClassicFxEffectType.ArrowRing:
                    life = 30f;
                    scale = 1f;
                    velocity = 1f;
                    position += Vector3.TransformNormal(
                        new Vector3(-10f, -60f, 135f),
                        Matrix.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z));
                    direction = new Vector3(0f, -70f, 0f);
                    angle.Y = MathHelper.ToRadians(Random.Modulo(360));
                    break;
            }

            if ((type is ClassicFxEffectType.ChangeUpEffect or
                ClassicFxEffectType.ChangeUpCylinder) && subtype == 2 &&
                !TryGetOwnerSnapshot(owner, out _))
                return false;
            return true;
        }

        private static void ConfigureS6Batch17ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (type is ClassicFxEffectType.ChangeUpEffect or
                ClassicFxEffectType.ChangeUpNasa or
                ClassicFxEffectType.ChangeUpCylinder or
                ClassicFxEffectType.ArrowBestCrossbow or
                ClassicFxEffectType.ArrowDrill)
                view.BlendMesh = -2;
            else if (type == ClassicFxEffectType.ArrowRing)
                view.BlendMesh = 0;
        }

        private void EmitS6HalloweenEx(
            Vector3 position, Vector3 angle, Vector3 light)
        {
            // Native MODEL_HALLOWEEN_EX: exact 24 child attempts and
            // weighted random distribution (pumpkin and star each 2/8).
            for (int i = 0; i < 24; i++)
            {
                ClassicFxEffectType type = Random.Modulo(8) switch
                {
                    0 => ClassicFxEffectType.HalloweenCandyBlue,
                    1 => ClassicFxEffectType.HalloweenCandyOrange,
                    2 => ClassicFxEffectType.HalloweenCandyYellow,
                    3 => ClassicFxEffectType.HalloweenCandyRed,
                    4 or 6 => ClassicFxEffectType.HalloweenCandyHobak,
                    _ => ClassicFxEffectType.HalloweenCandyStar
                };
                CreateEffect(type, position, angle, light,
                    ClassicFxOwner.None);
            }
        }

        private bool MoveS6Batch17Model(ref EffectState e, float f)
        {
            if (IsS6Batch17Candy(e.Type))
                return MoveS6Batch17Candy(ref e, f);
            if (IsS6Batch17HarvestFruit(e.Type))
                return MoveS6Batch17HarvestFruit(ref e, f);
            switch (e.Type)
            {
                case ClassicFxEffectType.MoonHarvestMoon:
                    return MoveS6Batch17HarvestMoon(ref e, f);
                case ClassicFxEffectType.ChangeUpEffect:
                case ClassicFxEffectType.ChangeUpNasa:
                case ClassicFxEffectType.ChangeUpCylinder:
                    return MoveS6Batch17ChangeUp(ref e, f);
                case ClassicFxEffectType.ArrowBestCrossbow:
                case ClassicFxEffectType.ArrowDrill:
                case ClassicFxEffectType.ArrowRing:
                    return MoveS6Batch17Arrow(ref e, f);
                default:
                    return false;
            }
        }

        private bool MoveS6Batch17Candy(ref EffectState e, float f)
        {
            // Native rotation is degrees per tick, stored in radians here.
            float spin = MathHelper.ToRadians(20f) * f;
            switch ((int)e.HeadAngle.X)
            {
                case 0: e.Angle.X += spin; break;
                case 1: e.Angle.Y += spin; break;
                default: e.Angle.Z += spin; break;
            }
            e.Position.Z += e.Gravity * (0.5f * f);
            e.Gravity -= 1.5f * f;
            float floor = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z < floor)
            {
                e.Position.Z = floor;
                e.Gravity = -e.Gravity * 0.3f;
                e.LifeTime -= 2f * f;
            }
            e.Position += e.Direction * f;
            if (e.Type == ClassicFxEffectType.HalloweenCandyHobak &&
                Clock.AdvancedReferenceFrame && Random.Modulo(3) == 0)
                CreateParticle(ClassicTextureIds.BitmapFire,
                    e.Position, e.Angle, e.Light, 5,
                    0.4f + Random.Modulo(10) * 0.01f);
            return true;
        }

        private bool MoveS6Batch17HarvestFruit(ref EffectState e, float f)
        {
            e.Angle.X += MathHelper.ToRadians(10f) * f;
            e.Position.X += e.Direction.X * 2.2f * f;
            e.Position.Y += e.Direction.Y * 2.2f * f;
            e.Position.Z += e.Gravity * 1.5f * f;
            e.Gravity -= 1.5f * f;
            float floor = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z < floor)
            {
                e.Position.Z = floor;
                e.Gravity = -e.Gravity * 0.3f;
                e.LifeTime -= 2f * f;
            }
            return true;
        }

        private bool MoveS6Batch17HarvestMoon(ref EffectState e, float f)
        {
            if (e.SubType == 0)
            {
                e.Angle.Z += MathHelper.ToRadians(5f) * f;
                if (e.LifeTime < 25f)
                {
                    e.Light.X -= 0.05f * f;
                    e.Light.Y -= 0.06f * f;
                    e.Light.Z -= 0.05f * f;
                }
                if (Clock.AdvancedReferenceFrame)
                {
                    e.Phase += 3f;
                    CreateSprite(ClassicTextureIds.BitmapShockWave,
                        e.Position, 0.8f, new Vector3(0.6f, 0.8f, 0.6f),
                        rotation: -e.Phase);
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        e.Position, 5f, new Vector3(0.8f, 0.6f, 0f));
                }
            }
            else if (e.SubType == 1)
            {
                e.Position += e.Direction * f;
                if (Clock.AdvancedReferenceFrame)
                {
                    CreateParticle(ClassicTextureIds.BitmapSmokeLine1 +
                        Random.Modulo(3), e.Position, e.Angle, e.Light);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Light, 11);
                    if (Random.Modulo(2) == 0)
                    {
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            e.Position, 5f, e.Light);
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            e.Position, 3f, e.Light);
                    }
                    CreateSprite(ClassicTextureIds.BitmapShiny + 6,
                        e.Position, 2f, e.Light);
                }
            }
            else if (e.SubType == 2)
                e.Scale += 0.01f * f;
            return true;
        }

        private bool MoveS6Batch17ChangeUp(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.ChangeUpNasa)
            {
                if (e.SubType == 0)
                {
                    // Native triggers when remaining life is 100 / 70 / 40.
                    int trigger = e.LifeTime > 70f ? 0 :
                        e.LifeTime > 40f ? 1 : 2;
                    int mask = 1 << trigger;
                    if ((e.TriggerMask & mask) == 0 &&
                        Clock.AdvancedReferenceFrame)
                    {
                        e.TriggerMask |= (byte)mask;
                        ClassicFxOwner owner = e.ModelView != null &&
                            e.ModelView.Status == GameControlStatus.Ready
                            ? ClassicFxOwner.FromWorldObject(e.ModelView)
                            : ClassicFxOwner.None;
                        CreateEffect(ClassicFxEffectType.ChangeUpNasa,
                            e.Position, e.Angle, e.Light, owner,
                            subType: trigger + 1);
                    }
                }
                else
                    e.Scale += 0.02f * f;
                e.BlendMeshLight = e.LifeTime *
                    (e.SubType == 0 ? 0.01f : 0.005f);
                return true;
            }

            if (e.SubType == 2)
            {
                if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot owner))
                    return false;
                e.Position = owner.Position;
                e.Position.Z = RequestTerrainHeight(
                    e.Position.X, e.Position.Y) +
                    (e.Type == ClassicFxEffectType.ChangeUpEffect ? 22f : 0f);
            }

            if (e.Type == ClassicFxEffectType.ChangeUpCylinder)
            {
                if (e.SubType == 1)
                {
                    e.BlendMeshLight = e.LifeTime * 0.015f;
                    e.Scale += 0.08f * f;
                }
                return true;
            }

            // MODEL_CHANGE_UP_EFF: original produces FLARE(50) joints.
            // Its BITMAP_MAGIC subtype 4 visual has no registered ClassicFX
            // handler, so don't invent an unrelated bitmap as a substitute.
            if (Clock.AdvancedReferenceFrame && Random.Modulo(2) == 0)
            {
                Vector3 pos = e.Position + new Vector3(
                    Random.Modulo(200) - 100f,
                    Random.Modulo(200) - 100f,
                    -200f);
                if (e.SubType is 0 or 2)
                    CreateJoint(ClassicTextureIds.BitmapFlare,
                        pos, pos, e.Angle, 50,
                        ClassicFxOwner.None, 40f);
            }
            // Native SetPlayerStop() is gameplay and must not be duplicated.
            return true;
        }

        private bool MoveS6Batch17Arrow(ref EffectState e, float f)
        {
            if (e.FirstMove && e.ModelView != null &&
                e.ModelView.Status == GameControlStatus.Ready)
            {
                e.FirstMove = false;
                ClassicFxOwner carrier =
                    ClassicFxOwner.FromWorldObject(e.ModelView);
                if (e.Type == ClassicFxEffectType.ArrowRing)
                    CreateJoint(ClassicTextureIds.BitmapFlare + 1,
                        e.Position, e.Position, e.Angle, 15,
                        carrier, 50f);
                else
                    CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                        e.Position, e.Position, e.Angle, 5,
                        carrier, 100f);
                if (e.SubType != 0 &&
                    e.Type != ClassicFxEffectType.ArrowRing)
                    CreateEffect(ClassicFxEffectType.Piercing,
                        e.Position, e.Angle, e.Light, carrier);
            }

            // CheckClientArrow and server hit/skill damage are intentionally
            // not copied into this pooled visual runtime.
            AdvanceS6Arrow(ref e, f);
            if (e.Type == ClassicFxEffectType.ArrowBestCrossbow)
            {
                e.BlendMeshLight = e.LifeTime > 25f
                    ? (30f - e.LifeTime) / 40f : 1f;
                e.Angle.Y += MathHelper.ToRadians(
                    30f + Random.Modulo(60)) * f;
            }
            else if (e.Type == ClassicFxEffectType.ArrowDrill)
            {
                e.Angle.Y += MathHelper.ToRadians(30f) * f;
                if (Clock.AdvancedReferenceFrame)
                {
                    for (int i = 0; i < 3; ++i)
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position, e.Angle, Vector3.One, 13);
                }
            }
            else if (e.Type == ClassicFxEffectType.ArrowRing &&
                     Clock.AdvancedReferenceFrame)
            {
                Vector3 purple = new Vector3(0f, 1f, 0.1f);
                CreateEffect(ClassicFxEffectType.Waves,
                    e.Position, e.Angle, purple, ClassicFxOwner.None,
                    subType: 4, boneIndex: 20);
                AddClassicTerrainLight(e.Position.X, e.Position.Y,
                    new Vector3(0.6f, 0.2f, 0.8f), 2f);
            }
            if (e.Type != ClassicFxEffectType.ArrowRing)
                AddClassicTerrainLight(e.Position.X, e.Position.Y,
                    new Vector3(1f, 0.4f, 0.2f), 2f);
            return true;
        }
    }
}
