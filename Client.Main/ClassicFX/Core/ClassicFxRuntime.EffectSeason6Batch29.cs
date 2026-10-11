// ClassicFX Batch 29 — arrows, siege stones and ranged character effects.
// Reference MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Port of native CreateEffect/MoveEffect with explicitly deferred owner/game hooks.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch29ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.ArrowBasicModel &&
            type <= ClassicFxEffectType.DungeonStoneModel;

        private static bool IsS6Batch29Arrow(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.ArrowBasicModel or
                ClassicFxEffectType.ArrowDarkStingerModel or
                ClassicFxEffectType.LaceArrowModel;

        private static bool IsS6Batch29Ground(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.GroundStoneModel or
                ClassicFxEffectType.GroundStone2Model;

        private static bool TryGetS6Batch29ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch29ModelType(type)) return false;
            if (IsS6Batch29Arrow(type) && (subType < 0 || subType > 5))
                return false;
            if (type == ClassicFxEffectType.SkullModel &&
                subType is not (0 or 1)) return false;
            if (type == ClassicFxEffectType.WoosiStoneModel &&
                subType is not (0 or 1)) return false;
            if (type != ClassicFxEffectType.SkullModel &&
                type != ClassicFxEffectType.WoosiStoneModel &&
                !IsS6Batch29Arrow(type) && subType != 0)
                return false;
            // Death Spike subtype 1 copies the *native effect* owner's
            // lifetime; the WorldObject owner adapter cannot provide it.
            if (type == ClassicFxEffectType.DeathSpiSkillModel &&
                subType != 0) return false;

            string path = type switch
            {
                ClassicFxEffectType.ArrowBasicModel => "Skill/Arrow01.bmd",
                ClassicFxEffectType.ArrowDarkStingerModel =>
                    "Skill/sketbows_arrows.bmd",
                ClassicFxEffectType.LaceArrowModel => "Skill/LaceArrow.bmd",
                ClassicFxEffectType.GroundStoneModel => "Skill/GroundStone.bmd",
                ClassicFxEffectType.GroundStone2Model => "Skill/GroundStone2.bmd",
                ClassicFxEffectType.SkullModel => "Skill/skull.bmd",
                ClassicFxEffectType.ProtectGuildModel => "Skill/protectguild.bmd",
                ClassicFxEffectType.DeathSpiSkillModel => "Skill/deathsp_eff.bmd",
                ClassicFxEffectType.WoosiStoneModel => "Skill/woositone.bmd",
                // Native DungeonStone01 is loaded by Dungeon map objects,
                // not through ZzzOpenData's shared Skill model list.
                ClassicFxEffectType.DungeonStoneModel =>
                    "Object2/DungeonStone01.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = type switch
            {
                ClassicFxEffectType.ArrowBasicModel when
                    subType is 3 or 4 => 40f,
                ClassicFxEffectType.ArrowBasicModel or
                    ClassicFxEffectType.ArrowDarkStingerModel or
                    ClassicFxEffectType.LaceArrowModel => 30f,
                ClassicFxEffectType.GroundStoneModel or
                    ClassicFxEffectType.GroundStone2Model => 40f,
                ClassicFxEffectType.SkullModel => subType == 0 ? 1f : 49f,
                ClassicFxEffectType.ProtectGuildModel => 130f,
                ClassicFxEffectType.DeathSpiSkillModel => 30f,
                ClassicFxEffectType.WoosiStoneModel => subType == 1 ? 35f : 20f,
                _ => 39f
            };
            float scale = type switch
            {
                ClassicFxEffectType.ArrowBasicModel when
                    subType is 3 or 4 => 1.5f,
                ClassicFxEffectType.ArrowBasicModel or
                    ClassicFxEffectType.ArrowDarkStingerModel or
                    ClassicFxEffectType.LaceArrowModel => 0.8f,
                ClassicFxEffectType.SkullModel => subType == 0 ? 2.3f : 2f,
                ClassicFxEffectType.ProtectGuildModel => 3.2f,
                ClassicFxEffectType.DeathSpiSkillModel => 0.3f,
                _ => 1f
            };
            definition = new Season6ModelDefinition(
                path, life, scale,
                needsOwner: type == ClassicFxEffectType.ProtectGuildModel ||
                    type == ClassicFxEffectType.DeathSpiSkillModel ||
                    (type == ClassicFxEffectType.SkullModel && subType == 1));
            return true;
        }

        private void ConfigureS6Batch29ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType)
        {
            if (type == ClassicFxEffectType.ArrowBasicModel)
                view.BlendMesh = 1;
            if (type == ClassicFxEffectType.SkullModel && subType == 1)
                view.BlendMesh = -2;
            if (type == ClassicFxEffectType.DeathSpiSkillModel)
                view.HiddenMesh = 1;
        }

        private bool InitializeS6Batch29Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            Vector3 inputLight, ref Vector3 position, ref Vector3 start,
            ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref float alpha,
            ref float mesh, ref Vector3 direction,
            ref Vector3 headAngle, ref float gravity,
            ref float velocity)
        {
            if (IsS6Batch29Arrow(type))
            {
                life = type == ClassicFxEffectType.ArrowBasicModel &&
                    subType is 3 or 4 ? 40f : 30f;
                velocity = 1f;
                Matrix rotation = Matrix.CreateFromYawPitchRoll(
                    angle.Y, angle.X, angle.Z);
                position += Vector3.TransformNormal(
                    new Vector3(-10f, -60f, 135f), rotation);
                direction = new Vector3(0f, -70f, 0f);
                scale = type == ClassicFxEffectType.ArrowBasicModel &&
                    subType is 3 or 4 ? 1.5f : 0.8f;
                if (type == ClassicFxEffectType.ArrowBasicModel &&
                    subType is 3 or 4)
                    gravity = (Random.Modulo(100) + 50f) / 15f;
                return true;
            }
            if (IsS6Batch29Ground(type))
            {
                // Terrain-wall flags (NOMOVE/NOGROUND/WATER) are currently
                // absent from this adapter; caller must validate tiles.
                life = 40f;
                scale = (type == ClassicFxEffectType.GroundStoneModel
                    ? 1.2f : 1f) + Random.Modulo(30) / 100f;
                velocity = 0.3f;
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                return true;
            }
            if (type == ClassicFxEffectType.SkullModel)
            {
                life = subType == 0 ? 1f : 49f;
                scale = subType == 0 ? 2.3f : 2f;
                if (subType == 1)
                {
                    mesh = 0.5f;
                    direction = new Vector3(0f, -45f, 0f);
                    start = position;
                }
                return true;
            }
            if (type == ClassicFxEffectType.ProtectGuildModel)
            {
                if (owner.WorldObject == null) return false;
                life = 130f;
                scale = 3.2f;
                alpha = 0f;
                angle.Z = MathHelper.ToRadians(45f);
                light = Vector3.One;
                if (TryGetOwnerBonePosition(owner, 20, out Vector3 bone))
                    position = bone + new Vector3(0f, 0f, 60f);
                start = position;
                return true;
            }
            if (type == ClassicFxEffectType.DeathSpiSkillModel)
            {
                if (owner.WorldObject == null) return false;
                life = 30f;
                gravity = 1f;
                velocity = 10f;
                scale = 0.3f;
                alpha = 0.8f;
                mesh = 1f;
                start = inputLight; // native target-relative offset
                headAngle = angle;
                angle = new Vector3(0f, 0f, angle.Z);
                light = Vector3.One;
                direction = new Vector3(0f, -26f, 0f);
                return true;
            }
            if (type == ClassicFxEffectType.WoosiStoneModel)
            {
                if (subType == 1)
                {
                    life = 20f + Random.Modulo(16);
                    scale = (Random.Modulo(13) + 3f) * 0.04f;
                    gravity = 3f + Random.Modulo(3);
                    angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                    headAngle = Vector3.TransformNormal(
                        new Vector3(0f,
                            (Random.Modulo(128) + 64f) * 0.1f, 0f),
                        Matrix.CreateRotationZ(angle.Z));
                    headAngle.Z += 15f * Clock.FrameFactor;
                }
                else
                {
                    position += Vector3.TransformNormal(
                        new Vector3(0f, -60f, 150f),
                        Matrix.CreateFromYawPitchRoll(
                            angle.Y, angle.X, angle.Z))
                        * Clock.FrameFactor;
                    life = 20f;
                    scale = 1f;
                    direction = new Vector3(0f, -40f, 10f);
                }
                return true;
            }
            if (type == ClassicFxEffectType.DungeonStoneModel)
            {
                life = 24f + Random.Modulo(16);
                scale = (Random.Modulo(8) + 6f) * 0.1f;
                gravity = -Random.Modulo(4);
                position += Vector3.TransformNormal(
                    new Vector3(Random.Modulo(64) - 32f,
                        -Random.Modulo(32) - 50f,
                        Random.Modulo(128) + 200f),
                    Matrix.CreateFromYawPitchRoll(
                        angle.Y, angle.X, angle.Z))
                    * Clock.FrameFactor;
                return true;
            }
            return false;
        }

        private bool MoveS6Batch29Model(ref EffectState e, float f)
        {
            if (IsS6Batch29Arrow(e.Type))
                return MoveS6Batch29Arrow(ref e, f);
            if (IsS6Batch29Ground(e.Type))
                return MoveS6Batch29Ground(ref e, f);
            switch (e.Type)
            {
                case ClassicFxEffectType.SkullModel:
                    return MoveS6Batch29Skull(ref e, f);
                case ClassicFxEffectType.ProtectGuildModel:
                    return MoveS6Batch29Guild(ref e, f);
                case ClassicFxEffectType.DeathSpiSkillModel:
                    return MoveS6Batch29DeathSpi(ref e, f);
                case ClassicFxEffectType.WoosiStoneModel:
                    // Original has no specialized per-model move handler;
                    // its shared MoveParticle rotates the direction vector.
                    e.Position += Vector3.TransformNormal(e.Direction,
                        Matrix.CreateFromYawPitchRoll(
                            e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;
                    return true;
                case ClassicFxEffectType.DungeonStoneModel:
                    e.Position.Z += e.Gravity * f;
                    e.Gravity -= 1f * f;
                    float terrain = RequestTerrainHeight(e.Position.X,
                        e.Position.Y);
                    if (e.Position.Z < terrain)
                    {
                        e.Position.Z = terrain;
                        e.Gravity = -e.Gravity * 0.4f;
                        e.LifeTime -= 4f * f;
                        e.Direction.Y -= 2f * f;
                    }
                    return true;
                default:
                    return false;
            }
        }

        private bool MoveS6Batch29Arrow(ref EffectState e, float f)
        {
            // The native shared tail calls MoveParticle(true).
            if (e.Type == ClassicFxEffectType.LaceArrowModel)
                e.Angle.Y += MathHelper.ToRadians(60f * f);
            if (e.Type == ClassicFxEffectType.ArrowBasicModel &&
                e.SubType == 3)
                e.Angle.X = MathF.Min(
                    MathHelper.ToRadians(50f),
                    e.Angle.X + MathHelper.ToRadians(e.Gravity * f));
            e.Position += Vector3.TransformNormal(e.Direction,
                Matrix.CreateFromYawPitchRoll(
                    e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;
            if (!Clock.AdvancedReferenceFrame) return true;
            var spriteOwner = e.ModelView == null
                ? ClassicFxOwner.None
                : ClassicFxOwner.FromWorldObject(e.ModelView);
            if (e.Type == ClassicFxEffectType.ArrowDarkStingerModel)
            {
                if (TryGetOwnerBonePosition(spriteOwner, 0,
                    out Vector3 tip))
                {
                    Vector3 color = new Vector3(0.6f, 0.7f, 0.9f);
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        tip, 3f, color, spriteOwner);
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        tip, 2f, color, spriteOwner);
                }
                int tick = (int)MathF.Ceiling(e.LifeTime);
                if (tick is 30 or 28 or 26 or 24 &&
                    TryGetOwnerBonePosition(spriteOwner, 1,
                        out Vector3 feathers))
                {
                    int count = Random.Modulo(3);
                    for (int j = 0; j < count; j++)
                    {
                        for (int variant = 0; variant < 2; variant++)
                            CreateEffect(ClassicFxEffectType.Feather,
                                feathers, e.Angle,
                                new Vector3(0.6f, 0.7f, 0.9f),
                                ClassicFxOwner.None,
                                subType: variant, scale: 0.6f);
                    }
                }
                if (tick == 30 && (e.TriggerMask & 1) == 0)
                {
                    e.TriggerMask |= 1;
                    CreateJoint(ClassicTextureIds.BitmapFlare + 1,
                        e.Position, e.Position, e.Angle, 18,
                        spriteOwner, 90f);
                }
            }
            else if (e.Type == ClassicFxEffectType.LaceArrowModel)
            {
                if ((int)MathF.Ceiling(e.LifeTime) % 2 == 0)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(32) - 16f,
                            Random.Modulo(64) - 32f,
                            Random.Modulo(32) - 16f);
                        CreateParticle(ClassicTextureIds.BitmapFlare,
                            p, e.Angle, new Vector3(0.4f, 0.2f, 1f),
                            5, 0.2f);
                    }
                }
                // Native emits MODEL_WAVES subtype 3 each reference tick.
                CreateEffect(ClassicFxEffectType.Waves,
                    e.Position, e.Angle, e.Light,
                    ClassicFxOwner.None, subType: 3);
            }
            else if (e.Type == ClassicFxEffectType.ArrowBasicModel)
            {
                CreateParticle(ClassicTextureIds.BitmapFire,
                    e.Position, e.Angle, e.Light,
                    e.SubType is 3 or 4 ? 5 : 0);
            }
            // CheckClientArrow: hit tests, safe-zone flags, skill effects
            // and attack serials are gameplay-side; never fake them here.
            return true;
        }

        private bool MoveS6Batch29Ground(ref EffectState e, float f)
        {
            if (Clock.AdvancedReferenceFrame &&
                e.LifeTime > 32f && e.LifeTime < 37f)
            {
                Vector3 at = e.Position + new Vector3(60f, -60f, 50f);
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    at, e.Angle, new Vector3(1f, 0.8f, 0.6f),
                    11, 2f);
                CreateEffect(Random.Modulo(2) == 0
                    ? ClassicFxEffectType.Stone1 :
                    ClassicFxEffectType.Stone2, at, e.Angle,
                    e.Light, ClassicFxOwner.None, subType: 10);
            }
            if (e.LifeTime < 8f)
            {
                e.Alpha *= MathF.Pow(1f / 1.3f, f);
                e.BlendMeshLight *= MathF.Pow(0.5f, f);
            }
            return true;
        }

        private bool MoveS6Batch29Skull(ref EffectState e, float f)
        {
            if (e.SubType == 0) return true;
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;
            if (e.ModelView != null && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                ClassicFxOwner modelOwner =
                    ClassicFxOwner.FromWorldObject(e.ModelView);
                CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                    e.Position, e.Position, e.Angle, 10,
                    modelOwner, 30f);
                CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                    e.Position, e.Position, e.Angle, 11,
                    modelOwner, 30f);
            }
            Vector3 destination = owner.WorldPosition.Translation +
                new Vector3(0f, 0f, 200f * f);
            Vector3 delta = destination - e.Position;
            float dist = delta.Length();
            if (dist > 0.01f)
                e.Position += delta / dist * MathF.Min(dist, 10f * f);
            if (e.LifeTime < 10f)
            {
                e.Alpha *= MathF.Pow(1f / 1.1f, f);
                e.BlendMeshLight *= MathF.Pow(1f / 1.2f, f);
            }
            if (Clock.AdvancedReferenceFrame)
                CreateParticle(ClassicTextureIds.BitmapFire,
                    e.Position, e.Angle, Vector3.One, 5, 1.2f);
            return true;
        }

        private bool MoveS6Batch29Guild(ref EffectState e, float f)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;
            if (TryGetOwnerBonePosition(e.Owner, 20, out Vector3 at))
            {
                at.Z += 60f;
                e.Position.X = at.X;
                e.Position.Y = at.Y;
                float smoothing = at.Z < e.Position.Z ? 0.1f : 0.5f;
                e.Position.Z += (at.Z - e.Position.Z) * smoothing * f;
            }
            if ((e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                for (int n = 0; n < 10; n++)
                {
                    float a = MathHelper.ToRadians(Random.Modulo(360));
                    float radius = 15f + Random.Modulo(20);
                    Vector3 p = e.Position + new Vector3(
                        MathF.Sin(a) * radius, MathF.Cos(a) * radius, 0f);
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        p, e.Angle, e.Light, 4, 0.6f);
                }
            }
            if (e.Alpha >= 1f) e.Angle.Z += MathHelper.ToRadians(5f * f);
            if (e.LifeTime < 50f)
                e.Alpha = MathF.Max(0f, e.Alpha - 0.1f * f);
            else if (e.LifeTime < 120f && e.LifeTime > 95f)
                e.Alpha = MathF.Min(1f, e.Alpha + 0.4f * f);
            if (Clock.AdvancedReferenceFrame &&
                e.LifeTime > 120f && e.LifeTime < 125f)
            {
                for (int n = 0; n < 5; n++)
                    CreateParticle(ClassicTextureIds.BitmapSpark,
                        e.Position, e.Angle, e.Light, 6, 1.5f);
            }
            return true;
        }

        private bool MoveS6Batch29DeathSpi(ref EffectState e, float f)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World))
                return false;
            // Native's MoveHumming uses both the effect's heading and the
            // caster's target position. Preserve position/target here;
            // full homing/tail/sound waits for the action/target bridge.
            if (Clock.AdvancedReferenceFrame)
                e.Gravity += 0.1f;
            return true;
        }
    }
}
