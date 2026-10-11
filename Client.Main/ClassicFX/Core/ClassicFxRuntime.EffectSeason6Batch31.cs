// BroyalMU ClassicFX S6 Batch 31 — Kanturu storms, warp models and ambient skills.
// Pinned native: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// Shared ClassicFX pool and MonoGame BMD renderer; no secondary renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch31ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.KanturuStorm2 &&
            type <= ClassicFxEffectType.Warp5;

        private static bool IsS6Batch31Warp(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Warp1 or
                ClassicFxEffectType.Warp2 or
                ClassicFxEffectType.Warp4 or
                ClassicFxEffectType.Warp5;

        private static bool TryGetS6Batch31ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch31ModelType(type)) return false;
            if (type == ClassicFxEffectType.KanturuStorm2 &&
                (subType < 0 || subType > 2)) return false;
            if (type == ClassicFxEffectType.LaserSkillModel &&
                (subType < 0 || subType > 3)) return false;
            if (type == ClassicFxEffectType.ButterflyModel &&
                (subType < 0 || subType > 3)) return false;
            if (IsS6Batch31Warp(type) && subType is not (0 or 1))
                return false;
            if (type is ClassicFxEffectType.KanturuStorm3 or
                ClassicFxEffectType.AuroraModel or
                ClassicFxEffectType.RidingSpearModel)
            {
                if (subType != 0) return false;
            }

            string path = type switch
            {
                ClassicFxEffectType.KanturuStorm2 => "Skill/boswind.bmd",
                ClassicFxEffectType.KanturuStorm3 => "Skill/mayatonedo.bmd",
                ClassicFxEffectType.AuroraModel => "Skill/Aurora.bmd",
                ClassicFxEffectType.ButterflyModel => "Object1/Butterfly01.bmd",
                ClassicFxEffectType.LaserSkillModel => "Skill/Laser01.bmd",
                ClassicFxEffectType.RidingSpearModel => "Skill/RidingSpear01.bmd",
                ClassicFxEffectType.Warp1 or ClassicFxEffectType.Warp4 =>
                    "NPC/warp01.bmd",
                ClassicFxEffectType.Warp2 or ClassicFxEffectType.Warp5 =>
                    "NPC/warp02.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = type switch
            {
                ClassicFxEffectType.KanturuStorm2 when subType == 0 => 35f,
                ClassicFxEffectType.KanturuStorm2 when subType == 1 => 60f,
                ClassicFxEffectType.KanturuStorm2 => 100f,
                ClassicFxEffectType.KanturuStorm3 => 50f,
                ClassicFxEffectType.AuroraModel => 100f,
                ClassicFxEffectType.ButterflyModel => 250f,
                ClassicFxEffectType.LaserSkillModel when subType is 0 or 3 => 1f,
                ClassicFxEffectType.LaserSkillModel => 30f,
                ClassicFxEffectType.RidingSpearModel => 20f,
                _ => 16777215f
            };
            float scale = type switch
            {
                ClassicFxEffectType.KanturuStorm2 when subType == 0 => 0.5f,
                ClassicFxEffectType.KanturuStorm2 when subType == 2 => 1f,
                ClassicFxEffectType.KanturuStorm3 => 3.5f,
                ClassicFxEffectType.LaserSkillModel => 1.3f,
                ClassicFxEffectType.RidingSpearModel => 1.5f,
                ClassicFxEffectType.ButterflyModel when subType == 3 => 0.9f,
                ClassicFxEffectType.ButterflyModel => 0.25f,
                _ => 1f
            };
            definition = new Season6ModelDefinition(path, life, scale,
                needsOwner: type is ClassicFxEffectType.ButterflyModel or
                    ClassicFxEffectType.AuroraModel or
                    ClassicFxEffectType.KanturuStorm3);
            return true;
        }

        private static void ConfigureS6Batch31ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (IsS6Batch31Warp(type))
                view.BlendMesh = -2;
            if (type == ClassicFxEffectType.AuroraModel)
            {
                // Native is visible exclusively during active Castle Siege.
                // Until an event-state bridge is supplied, fail closed.
                view.HiddenMesh = -2;
            }
            if (type is ClassicFxEffectType.KanturuStorm2 or
                ClassicFxEffectType.KanturuStorm3 or
                ClassicFxEffectType.AuroraModel)
                view.BlendMesh = 0;
        }

        private bool InitializeS6Batch31Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            int nativePkKey, ref Vector3 position, ref Vector3 start,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref float mesh,
            ref Vector3 direction, ref float gravity, ref float velocity)
        {
            if (type == ClassicFxEffectType.KanturuStorm2)
            {
                if (subType == 0)
                {
                    angle.X = MathHelper.ToRadians(90f);
                    life = 35f;
                    scale = 0.5f + Random.Modulo(10) / 100f;
                }
                else if (subType == 1)
                {
                    life = 60f;
                    gravity = 30f + Random.Modulo(10);
                }
                else
                {
                    life = 100f;
                    angle.X = MathHelper.ToRadians(90f);
                    scale = 1f;
                }
                return true;
            }
            if (type == ClassicFxEffectType.KanturuStorm3)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                life = 50f;
                angle.X = MathHelper.ToRadians(90f);
                scale = 3.5f;
                start = owner.WorldObject.WorldPosition.Translation;
                return true;
            }
            if (type == ClassicFxEffectType.AuroraModel)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                life = 100f;
                scale = nativePkKey / 100f;
                mesh = 0f;
                return nativePkKey > 0;
            }
            if (type == ClassicFxEffectType.ButterflyModel)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                life = 250f;
                velocity = 0.3f;
                scale = subType == 3 ? 0.9f : 0.25f;
                direction = new Vector3(0f, -5f, 0f);
                gravity = Random.Modulo(2); // Native Kind = rand()%2.
                return true;
            }
            if (type == ClassicFxEffectType.LaserSkillModel)
            {
                life = subType is 0 or 3 ? 1f : 30f;
                mesh = subType is 0 or 3 ? light.X : 1f;
                scale = 1.3f;
                if (subType is not (0 or 3))
                {
                    velocity = 1f;
                    position.Z += 150f * Clock.FrameFactor;
                    direction = new Vector3(0f, -1f, 0f);
                    light = new Vector3(1f, 0f, 0f);
                    angle = new Vector3(
                        MathHelper.ToRadians(30f), 0f, angle.Z);
                    // JointForce child is created when BMD view exists.
                }
                return true;
            }
            if (type == ClassicFxEffectType.RidingSpearModel)
            {
                life = 20f;
                scale = 1.5f;
                direction = new Vector3(5f * MathF.Sin(angle.Z),
                    -5f * MathF.Cos(angle.Z), 0f);
                return true;
            }
            if (IsS6Batch31Warp(type))
            {
                life = 16777215f;
                scale = 1.3f + Random.Modulo(50) / 100f;
                gravity = Random.Modulo(80) / 10f;
                velocity = Random.Modulo(100) / 1000f + 0.01f;
                return true;
            }
            return false;
        }

        private bool MoveS6Batch31Model(ref EffectState e, float f)
        {
            if (IsS6Batch31Warp(e.Type))
            {
                float t = (float)Clock.WorldTimeMilliseconds;
                if (e.SubType == 0)
                {
                    e.Light = new Vector3(
                        MathF.Sin(t * 0.0011f) * 0.2f + 0.01f,
                        MathF.Sin(t * 0.0017f) * 0.2f + 0.01f,
                        MathF.Sin(t * 0.0013f) * 0.2f + 0.01f);
                    e.Angle.Y += MathHelper.ToRadians(4f + e.Gravity) * f;
                }
                else
                {
                    float wave = MathF.Sin(t * 0.0011f) * 0.05f;
                    e.Light = new Vector3(wave, 0.2f + wave, 0.1f + wave);
                    e.Angle.Y += MathHelper.ToRadians(2f + e.Gravity) * f;
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.KanturuStorm2)
            {
                if (e.SubType is 0 or 2)
                {
                    e.Angle.Y += MathHelper.ToRadians(40f) * f;
                    if (e.LifeTime <= 5f)
                        e.Light *= MathF.Pow(1f / 1.01f, f);
                    if (e.SubType == 2 && Clock.AdvancedReferenceFrame)
                        CreateS6Batch31Stone(e.Position, e.Angle, e.Light);
                }
                else
                {
                    if (Clock.AdvancedReferenceFrame)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 turn = new Vector3(e.Angle.X, e.Angle.Y,
                                e.Angle.Z / 3f +
                                MathHelper.ToRadians(180f * i));
                            Vector3 at = e.Position +
                                Vector3.TransformNormal(new Vector3(0f, 200f, 0f),
                                    Matrix.CreateFromYawPitchRoll(
                                        turn.Y, turn.X, turn.Z));
                            CreateParticle(ClassicTextureIds.BitmapClud64,
                                at, e.Angle,
                                new Vector3(0.3f, 0.5f, 0.6f), 1, 1f);
                        }
                        Vector3 from = e.Position + new Vector3(
                            Random.Modulo(600) - 300f,
                            Random.Modulo(600) - 300f, 0f);
                        Vector3 to = from + new Vector3(
                            Random.Modulo(100) - 50f,
                            Random.Modulo(100) - 50f, 0f);
                        CreateJoint(ClassicTextureIds.BitmapJointThunder,
                            from, to, new Vector3(MathHelper.PiOver2, 0f, 0f),
                            20, ClassicFxOwner.None, 20f);
                        CreateS6Batch31Stone(to, e.Angle,
                            new Vector3(0.3f, 0.5f, 0.6f));
                    }
                    e.Angle.Z += MathHelper.ToRadians(e.Gravity) * f;
                    e.Light *= MathF.Pow(1f / 1.02f, f);
                    e.Alpha += 0.01f * f;
                    if (e.Light.X <= 0.05f) return false;
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.KanturuStorm3)
            {
                if (e.LifeTime <= 30f)
                    e.Light *= MathF.Pow(1f / 1.1f, f);
                e.Angle.Y += MathHelper.ToRadians(30f) * f;
                if (Clock.AdvancedReferenceFrame)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (!Random.FpsCheck(2, Clock)) continue;
                        Vector3 from = e.StartPosition + new Vector3(
                            Random.Modulo(2000) - 1000f,
                            Random.Modulo(2000) - 1000f, 0f);
                        // Native leaves Pos2[2] uninitialized. Use source Z.
                        Vector3 to = new Vector3(
                            from.X + Random.Modulo(200) - 100f,
                            from.Y + Random.Modulo(200) - 100f,
                            from.Z);
                        CreateJoint(ClassicTextureIds.BitmapJointThunder,
                            from, to, new Vector3(
                                MathHelper.PiOver2, 0f, e.Angle.Z),
                            20, ClassicFxOwner.None, 20f);
                    }
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.AuroraModel)
            {
                if (e.Owner.WorldObject == null ||
                    !ReferenceEquals(e.Owner.WorldObject.World, World))
                    return false;
                e.LifeTime = 2f;
                e.BlendMeshLight = MathF.Sin(
                    (float)Clock.WorldTimeMilliseconds * 0.001f) * 0.1f + 0.2f;
                // BattleCastle.IsBattleCastleStart and UV scroll must be
                // bridged before activating the BMD. Hidden by default.
                return true;
            }
            if (e.Type == ClassicFxEffectType.ButterflyModel)
            {
                if (e.Owner.WorldObject == null ||
                    !ReferenceEquals(e.Owner.WorldObject.World, World))
                    return false;
                if (Clock.AdvancedReferenceFrame)
                {
                    float turn = MathHelper.ToRadians(Random.Modulo(10)) * f;
                    e.Angle.Z += e.Gravity > 0f ? turn : -turn;
                    if (Random.FpsCheck(32, Clock))
                        e.Direction.Z = Random.Modulo(15) - 7f;
                }
                e.Direction.Z += (Random.Modulo(15) - 7f) * 0.2f * f;
                float ground = RequestTerrainHeight(e.Position.X,
                    e.Position.Y);
                if (e.Position.Z < ground + 50f)
                {
                    e.Direction.Z *= MathF.Pow(0.8f, f);
                    e.Direction.Z += f;
                }
                if (e.Position.Z > ground + 150f)
                {
                    e.Direction.Z *= MathF.Pow(0.8f, f);
                    e.Direction.Z -= f;
                }
                e.Position.Z += (Random.Modulo(15) - 7f) * 0.3f * f;
                e.Position += e.Direction * f;
                if (Clock.AdvancedReferenceFrame)
                {
                    float brightness = (Random.Modulo(32) + 64f) * 0.01f;
                    Vector3 color = e.SubType switch
                    {
                        0 => new Vector3(0.4f, 0.8f, 0.6f),
                        1 => new Vector3(0.4f, 0.6f, 0.8f),
                        2 => new Vector3(0.6f, 0.8f, 0.4f),
                        _ => new Vector3(0.7f, 0.9f, 0.5f)
                    };
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        e.Position, 1f, color * brightness, e.Owner);
                    if (e.SubType == 3 && Random.FpsCheck(2, Clock))
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(16) - 8f,
                            Random.Modulo(16) - 8f,
                            Random.Modulo(16) - 8f);
                        CreateParticle(ClassicTextureIds.BitmapSpark,
                            p, e.Angle, e.Light, 7);
                    }
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.LaserSkillModel)
            {
                if (e.SubType is not (0 or 3))
                {
                    e.Direction.Y -= e.Velocity * f;
                    e.Velocity += f;
                    if (e.ModelView != null && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        ClassicFxOwner view =
                            ClassicFxOwner.FromWorldObject(e.ModelView);
                        CreateJoint(ClassicTextureIds.BitmapJointForce,
                            e.Position, e.Position, e.Angle, 1, view, 180f);
                    }
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.RidingSpearModel)
                return true; // Native Move_MODEL_SPEARSKILL is empty.
            return false;
        }

        private void CreateS6Batch31Stone(Vector3 position, Vector3 angle,
            Vector3 light)
        {
            CreateEffect(Random.Modulo(2) == 0
                ? ClassicFxEffectType.Stone1 :
                ClassicFxEffectType.Stone2,
                position, angle, light, ClassicFxOwner.None,
                subType: 2);
        }
    }
}
