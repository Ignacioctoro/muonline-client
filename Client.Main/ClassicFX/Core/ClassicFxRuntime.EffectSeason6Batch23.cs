// ClassicFX S6 Batch 23: Kundun fragments + Kanturu Maya effect models.
// MuMain @ 21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp, Behaviors/MoveHandlers.cpp, MapManager.cpp / ZzzOpenData.cpp.
// Reuses EffectState, ClassicFxEffectModelObject, 25 FPS and the existing pools.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch23ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.KundunPart1 and <=
                ClassicFxEffectType.MayaStar;

        private static bool IsS6Batch23Kundun(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.KundunPart1 and <=
                ClassicFxEffectType.KundunPart8;

        private static bool IsS6Batch23MayaFallingStone(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.MayaStone1 and <=
                ClassicFxEffectType.MayaStone3;

        private static bool TryGetS6Batch23ModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch23ModelType(type))
                return false;
            if (IsS6Batch23Kundun(type) && subtype is < 1 or > 5)
                return false;
            if (type == ClassicFxEffectType.MayaHandSkill &&
                subtype is not (0 or 1))
                return false;
            // MayaStoneFire uses 0,1,2 as ClassicFX symbolic references to
            // the original MODEL_MAYASTONE1/2/3 values in the native SubType.
            if (type == ClassicFxEffectType.MayaStoneFire &&
                subtype is < 0 or > 2)
                return false;
            if (type != ClassicFxEffectType.MayaHandSkill &&
                type != ClassicFxEffectType.MayaStoneFire &&
                !IsS6Batch23Kundun(type) && subtype != 0)
                return false;

            string path = type switch
            {
                ClassicFxEffectType.KundunPart1 => "Monster/cd71a.bmd",
                ClassicFxEffectType.KundunPart2 => "Monster/cd71b.bmd",
                ClassicFxEffectType.KundunPart3 => "Monster/cd71c.bmd",
                ClassicFxEffectType.KundunPart4 => "Monster/cd71d.bmd",
                ClassicFxEffectType.KundunPart5 => "Monster/cd71e.bmd",
                ClassicFxEffectType.KundunPart6 => "Monster/cd71f.bmd",
                ClassicFxEffectType.KundunPart7 => "Monster/cd71g.bmd",
                ClassicFxEffectType.KundunPart8 => "Monster/cd71h.bmd",
                ClassicFxEffectType.MayaStone1 => "Skill/mayastone01.bmd",
                ClassicFxEffectType.MayaStone2 => "Skill/mayastone02.bmd",
                ClassicFxEffectType.MayaStone3 => "Skill/mayastone03.bmd",
                ClassicFxEffectType.MayaStone4 => "Skill/mayastone04.bmd",
                ClassicFxEffectType.MayaStone5 => "Skill/mayastone05.bmd",
                ClassicFxEffectType.MayaStoneFire => "Skill/mayastonebluefire.bmd",
                ClassicFxEffectType.MayaHandSkill => "Skill/hendlight02.bmd",
                ClassicFxEffectType.MayaStar => "Skill/arrowsre05.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = IsS6Batch23Kundun(type) ? 100f :
                type == ClassicFxEffectType.MayaStar ? 50f :
                type == ClassicFxEffectType.MayaHandSkill ? 20f : 40f;
            float scale = type == ClassicFxEffectType.MayaStar ? 50f : 1f;
            definition = new Season6ModelDefinition(path, life, scale,
                useCallerScale: IsS6Batch23Kundun(type) ||
                    type == ClassicFxEffectType.MayaHandSkill ||
                    type == ClassicFxEffectType.MayaStoneFire);
            return true;
        }

        private void InitializeS6Batch23Model(
            ClassicFxEffectType type, int subtype, float callerScale,
            int nativePkKey, ref Vector3 position, ref Vector3 storedPosition,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref Vector3 direction,
            ref float gravity, ref Vector3 headAngle)
        {
            if (IsS6Batch23Kundun(type))
            {
                scale = 2f;
                switch (subtype)
                {
                    case 1:
                        life = 100f;
                        break;
                    case 2:
                    case 3:
                        storedPosition = new Vector3(position.X, position.Y,
                            RequestTerrainHeight(position.X, position.Y) -
                                nativePkKey - 80f);
                        direction = Vector3.Zero;
                        headAngle.X = subtype == 2 ? 0f : Random.Modulo(2);
                        life = 500f;
                        break;
                    case 4:
                        storedPosition = new Vector3(position.X, position.Y,
                            RequestTerrainHeight(position.X, position.Y) -
                                nativePkKey);
                        direction = Vector3.Zero;
                        life = 170f;
                        break;
                    case 5:
                        life = 170f;
                        break;
                }
                return;
            }
            if (IsS6Batch23MayaFallingStone(type))
            {
                life = 40f;
                scale = 6f + (15f + Random.Modulo(8)) * 0.1f;
                direction = new Vector3(0f, 0f, -60f);
                headAngle = new Vector3(0f, MathHelper.ToRadians(30f), 0f);
                return;
            }
            if (type == ClassicFxEffectType.MayaStoneFire)
            {
                scale = callerScale * (subtype == 0 ? 0.8f :
                    subtype == 1 ? 0.6f : 0.5f);
                life = 40f;
                direction = new Vector3(0f, 0f, -60f);
                headAngle = new Vector3(0f, MathHelper.ToRadians(30f), 0f);
            }
            else if (type == ClassicFxEffectType.MayaHandSkill)
            {
                // EffectTypes.json: 20 reference ticks, color target is
                // the CreateEffect input Light, copied to StartPosition.
                life = 20f;
                scale = callerScale;
            }
            else if (type == ClassicFxEffectType.MayaStar)
            {
                life = 50f;
                scale = 50f;
            }
            // MODEL_MAYASTONE4/5 are spawned as fragments by MayaStone1..3.
            // They have a native Move handler but no dedicated CreateEffect
            // setup; their 40-tick fallback remains intentionally explicit.
        }

        private bool MoveS6Batch23Model(ref EffectState e, float f)
        {
            if (IsS6Batch23Kundun(e.Type))
                return MoveS6Batch23Kundun(ref e, f);

            if (IsS6Batch23MayaFallingStone(e.Type))
            {
                // MayaStone1..3: C++ uses the HeadAngle for model orientation,
                // while the shared native directional step moves it downward.
                e.Angle = e.HeadAngle;
                e.Position += e.Direction * f;
                float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z >= ground)
                    return true;

                e.Position.Z = ground;
                e.Direction = Vector3.Zero;
                Vector3 blue = new Vector3(0f, 0.6f, 1f);
                // Main creates an Inferno + 15 Smoke + 6 pieces + explosion.
                // SkillInferno subtype 2 requires its native SkillIndex/PKKey
                // metadata; an Effect owner cannot be represented by
                // ClassicFxOwner. Spawn only the visuals safely supported.
                for (int i = 0; i < 15; ++i)
                {
                    Vector3 where = e.Position + new Vector3(
                        Random.Modulo(160) - 80f,
                        Random.Modulo(160) - 100f, 50f);
                    CreateParticle(ClassicTextureIds.BitmapSmoke, where,
                        e.Angle, new Vector3(0.3f, 0.5f, 1f), 11,
                        (80f + Random.Modulo(32)) * 0.025f);
                }
                for (int i = 0; i < 6; i++)
                    CreateEffect(Random.Modulo(2) == 0
                        ? ClassicFxEffectType.MayaStone4
                        : ClassicFxEffectType.MayaStone5,
                        e.Position, e.Angle, blue, ClassicFxOwner.None);
                CreateParticle(ClassicTextureIds.BitmapExplotion,
                    e.Position + new Vector3(0f, 0f, 50f),
                    e.Angle, blue, 0, 4f);
                return false;
            }
            if (e.Type is ClassicFxEffectType.MayaStone4 or
                ClassicFxEffectType.MayaStone5)
            {
                // Shared original stone fragment: direction damping,
                // gravity, terrain rebound, rotational slowdown.
                e.Position += e.Direction * f;
                e.Direction *= MathF.Pow(0.9f, f);
                e.Position.Z += e.Gravity * f;
                e.Gravity -= 3f * f;
                float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < ground)
                {
                    e.Position.Z = ground;
                    e.Gravity = -e.Gravity * 0.5f;
                    e.LifeTime -= 4f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
                }
                else
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 32f * f);

                if (Clock.AdvancedReferenceFrame &&
                    Random.FpsCheck(10, Clock))
                    CreateParticle(ClassicTextureIds.BitmapFire,
                        e.Position, e.Angle, e.Light, 15);
                return true;
            }
            if (e.Type == ClassicFxEffectType.MayaStoneFire)
            {
                e.Angle = e.HeadAngle;
                e.Position += e.Direction * f;
                return e.Position.Z >= RequestTerrainHeight(
                    e.Position.X, e.Position.Y);
            }
            if (e.Type == ClassicFxEffectType.MayaHandSkill)
            {
                // Original paired hand-light effects fade at 1/1.17 per
                // reference frame, and subtype 0 emits a subtype 1 child.
                e.Light *= MathF.Pow(1f / 1.17f, f);
                if (Clock.AdvancedReferenceFrame)
                {
                    Vector3 smokeAt = e.Position + new Vector3(0f, 0f, 100f);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        smokeAt, e.Angle, e.StartPosition, 17, 10f);
                    if (e.SubType == 0)
                    {
                        Vector3 offset = Vector3.TransformNormal(
                            new Vector3(0f,
                                -(80f + Random.Modulo(30)) * e.Scale, 0f),
                            Matrix.CreateFromYawPitchRoll(
                                e.Angle.Y, e.Angle.X, e.Angle.Z));
                        e.Position += offset * f;
                        CreateEffect(ClassicFxEffectType.MayaHandSkill,
                            e.Position, e.Angle, e.StartPosition,
                            ClassicFxOwner.None, subType: 1,
                            scale: e.Scale + 0.2f);
                    }
                }
                return e.Light.X > 0.05f;
            }
            return true; // Native MayaStar: no separate Move handler.
        }

        private bool MoveS6Batch23Kundun(ref EffectState e, float f)
        {
            switch (e.SubType)
            {
                case 1:
                    e.Alpha = MathF.Max(0f, e.Alpha - 0.01f * f);
                    return true;
                case 2:
                case 3:
                {
                    float threshold = 300f + (34 - e.NativeSkillIndex) * 5f;
                    if (e.LifeTime >= threshold)
                        return true;
                    if (e.Position.Z > 350f)
                    {
                        if (e.Direction == Vector3.Zero)
                            e.Direction = new Vector3(
                                (Random.Modulo(2) - 1f) * 2f,
                                (Random.Modulo(2) - 1f) * 2f, 0f);
                        e.Direction.Z -= 6f * f;
                        e.Position += e.Direction * f;
                        return true;
                    }

                    bool firstContact = (e.TriggerMask & 1) == 0;
                    if (firstContact)
                    {
                        e.TriggerMask |= 1;
                        // Original impact emits 100 waterfall particles and
                        // alters the water tile. Cap to 20 to protect Android;
                        // water-vertex mutation remains world-side pending.
                        Vector3 waterLight = new Vector3(0.3f);
                        for (int i = 0; i < 20; i++)
                        {
                            Vector3 where = e.Position + new Vector3(
                                Random.Modulo(60) - 30f,
                                Random.Modulo(60) - 30f, -30f);
                            CreateParticle(ClassicTextureIds.BitmapWaterfall5,
                                where, e.Angle, waterLight, 2);
                        }
                    }
                    if (Clock.AdvancedReferenceFrame)
                    {
                        Vector3 bubbleAt = e.Position + new Vector3(
                            Random.Modulo(40) - 20f,
                            Random.Modulo(40) - 20f, -50f);
                        CreateParticle(ClassicTextureIds.BitmapBubble,
                            bubbleAt, e.Angle, e.Light, 4);
                    }
                    e.Direction = new Vector3(0f, 0f, -3f);
                    float swing = MathHelper.ToRadians(
                        Random.Modulo(3) + 1f) * f;
                    if (e.HeadAngle.X == 0f)
                    {
                        e.Angle.X -= swing;
                        if (e.Angle.X <= MathHelper.ToRadians(-30f))
                            e.HeadAngle.X = 1f;
                    }
                    else
                    {
                        e.Angle.X += swing;
                        if (e.Angle.X >= MathHelper.ToRadians(30f))
                            e.HeadAngle.X = 0f;
                    }
                    if (e.LifeTime < 100f)
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.1f * f);
                    return true;
                }
                case 4:
                    if (e.LifeTime < 140f)
                    {
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.01f * f);
                        if (e.Position.Z > e.StartPosition.Z)
                        {
                            e.Direction.Z -= 3.5f * f;
                            e.Position += e.Direction * f;
                        }
                        else e.Direction = Vector3.Zero;
                    }
                    return true;
                case 5:
                    if (e.LifeTime < 150f)
                        e.Alpha = MathF.Max(0f, e.Alpha - 0.01f * f);
                    return true;
                default:
                    return false;
            }
        }
    }
}
