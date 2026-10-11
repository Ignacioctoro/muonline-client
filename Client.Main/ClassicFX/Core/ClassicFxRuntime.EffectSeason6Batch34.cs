// BroyalMU ClassicFX Season 6 Batch 34: support, combat and siege models.
// Reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Source: EffectTypes.json, ZzzEffect.cpp, MoveHandlers.cpp, ZzzOpenData.cpp.
// Uses the shared effect pool, world/terrain bridge, existing BMD renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch34ModelType(ClassicFxEffectType t) =>
            t >= ClassicFxEffectType.MagicCircle1Model &&
            t <= ClassicFxEffectType.GateDebris2;

        private static bool IsS6Batch34Gate(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.GateDebris1 or ClassicFxEffectType.GateDebris2;

        private static bool TryGetS6Batch34ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch34ModelType(type)) return false;

            // Only native callers represented by the shared owner adapter
            // are accepted. Gate debris has native subtypes 0/1/2.
            if (type == ClassicFxEffectType.MagicCircle1Model)
            {
                if (subType is < 0 or > 2) return false;
            }
            else if (IsS6Batch34Gate(type))
            {
                if (subType is < 0 or > 2) return false;
            }
            else if (type == ClassicFxEffectType.WarcraftModel)
            {
                if (subType is not (0 or 1)) return false;
            }
            else if (subType != 0) return false;

            string path = type switch
            {
                ClassicFxEffectType.MagicCircle1Model => "Skill/MagicCircle01.bmd",
                ClassicFxEffectType.ProtectModel => "Skill/Protect01.bmd",
                ClassicFxEffectType.TowerGatePlaneModel => "Skill/TowerGateplane.bmd",
                ClassicFxEffectType.WarcraftModel => "Skill/hellgate.bmd",
                ClassicFxEffectType.ShieldCrash2Model => "Effect/atshild2.bmd",
                ClassicFxEffectType.GateDebris1 => "Object12/Gate01.bmd",
                ClassicFxEffectType.GateDebris2 => "Object12/Gate02.bmd",
                _ => null
            };
            if (path == null) return false;

            // Fallbacks for Gate are overwritten by native random creation.
            float life = type switch
            {
                ClassicFxEffectType.MagicCircle1Model =>
                    subType == 1 ? 20f : subType == 2 ? 15f : 30f,
                ClassicFxEffectType.ProtectModel => 10000f,
                ClassicFxEffectType.TowerGatePlaneModel => 100f,
                ClassicFxEffectType.WarcraftModel => 50f,
                ClassicFxEffectType.ShieldCrash2Model => 24f,
                _ => 32f
            };
            float scale = type switch
            {
                ClassicFxEffectType.MagicCircle1Model => 0.7f,
                ClassicFxEffectType.WarcraftModel => 0.7f,
                ClassicFxEffectType.ShieldCrash2Model => 1.1f,
                _ => 1f
            };

            bool needsOwner = type is
                ClassicFxEffectType.ProtectModel or
                ClassicFxEffectType.WarcraftModel or
                ClassicFxEffectType.ShieldCrash2Model ||
                (type == ClassicFxEffectType.MagicCircle1Model &&
                    subType != 1);

            definition = new Season6ModelDefinition(
                path, life, scale,
                meshLight: type == ClassicFxEffectType.TowerGatePlaneModel
                    ? 0.3f : 1f,
                needsOwner: needsOwner);
            return true;
        }

        private static void ConfigureS6Batch34ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            if (type == ClassicFxEffectType.MagicCircle1Model)
            {
                view.BlendMesh = -2;
                if (subType == 1) view.HiddenMesh = 0;
            }
            else if (type == ClassicFxEffectType.ProtectModel)
                view.BlendMesh = 0;
            else if (type == ClassicFxEffectType.WarcraftModel)
            {
                view.BlendMesh = subType == 0 ? -3 : -4;
                // Native HellGate Actions[0].PlaySpeed = 0.15 per 25Hz tick.
                view.AnimationSpeed = 25f * 0.15f;
            }
        }

        private bool InitializeS6Batch34Model(
            ClassicFxEffectType type, ref int subType,
            Vector3 inputLight, ref Vector3 position,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float mesh, ref Vector3 direction,
            ref float gravity, ref float velocity)
        {
            switch (type)
            {
                case ClassicFxEffectType.MagicCircle1Model:
                    life = subType == 1 ? 20f : subType == 2 ? 15f : 30f;
                    scale = 0.7f;
                    velocity = subType == 2 ? 0.3f : 0.1f;
                    return true;

                case ClassicFxEffectType.ProtectModel:
                    life = 10000f;
                    velocity = 0.3f;
                    return true;

                case ClassicFxEffectType.TowerGatePlaneModel:
                    life = 100f;
                    mesh = 0.3f;
                    // EffectState.StartPosition captures position at spawn.
                    return true;

                case ClassicFxEffectType.WarcraftModel:
                    life = 50f;
                    scale = 0.7f;
                    angle = new Vector3(0f, 0f, MathHelper.ToRadians(45f));
                    return true;

                case ClassicFxEffectType.ShieldCrash2Model:
                    life = 24f;
                    scale = 1.1f;
                    gravity = 0.3f;
                    // Native CreateParams copy Direction from caller Light
                    // before setting effect RGB to (0.5, 0.5, 1).
                    direction = inputLight;
                    light = new Vector3(0.5f, 0.5f, 1f);
                    return true;

                case ClassicFxEffectType.GateDebris1:
                case ClassicFxEffectType.GateDebris2:
                    position.Z += 50f * Clock.FrameFactor;
                    life = 32f + Random.Modulo(16);
                    scale = (8f + Random.Modulo(4)) * 0.1f;
                    float yaw = MathHelper.ToRadians(Random.Modulo(360));
                    direction = Vector3.TransformNormal(
                        new Vector3(0f, (64f + Random.Modulo(128)) * 0.1f, 0f),
                        Matrix.CreateRotationZ(yaw));
                    gravity = 2f + Random.Modulo(5);
                    if (type == ClassicFxEffectType.GateDebris1 &&
                        subType == 0)
                    {
                        subType = 1;
                        gravity += Random.Modulo(5) * Clock.FrameFactor;
                    }
                    else if (subType == 2)
                        scale *= 0.6f;
                    angle = new Vector3(
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)));
                    return true;

                default:
                    return false;
            }
        }

        private bool MoveS6Batch34Model(ref EffectState e, float f)
        {
            if (IsS6Batch34Gate(e.Type))
            {
                // Native gate debris (not Batch 16's GatePart1/2/3):
                // rotation, friction, ground bounce and short smoke bursts.
                e.Position += e.Direction * f;
                e.Direction *= MathF.Pow(e.SubType == 1 ? 0.99f : 0.9f, f);
                e.Position.Z += e.Gravity * f;
                e.Gravity -= 4f * f;
                float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < ground)
                {
                    e.Position.Z = ground;
                    e.Gravity = -e.Gravity * 0.5f;
                    e.LifeTime -= 5f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f) * f;
                }
                else
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 16f) * f;

                if (Clock.AdvancedReferenceFrame &&
                    Random.FpsCheck(10, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke + 1,
                        e.Position, e.Angle, e.Light);
                return true;
            }

            if (e.Type == ClassicFxEffectType.TowerGatePlaneModel)
            {
                // Native refreshes the 100-tick life in Move.
                e.LifeTime = 100f;
                if (e.Owner.WorldObject != null)
                {
                    if (!ReferenceEquals(e.Owner.WorldObject.World, World))
                        return false;
                    e.Position.Z = e.StartPosition.Z +
                        MathF.Sin((float)Clock.WorldTimeMilliseconds * 0.001f)
                        * 200f + 200f;
                }
                // Native renders two mirrored BMD passes; the second model
                // pass is not available in our single-view bridge yet.
                return true;
            }

            if (e.Type == ClassicFxEffectType.MagicCircle1Model)
            {
                if (e.SubType != 1)
                {
                    if (!TryS6Batch34OwnerPosition(e.Owner, out var p))
                        return false;
                    e.Position = p;
                }
                e.Scale += (e.SubType == 2 ? 0.015f : 0.01f) * f;
                if (e.SubType == 2)
                    e.Light = new Vector3(0.1f, 0f, 0f);
                // Native subtype 1 also writes a dynamic terrain light.
                // Do not invent a MonoGame terrain-light substitute.
                return true;
            }

            if (!TryS6Batch34OwnerPosition(e.Owner, out Vector3 ownerPos))
                return false;

            switch (e.Type)
            {
                case ClassicFxEffectType.ProtectModel:
                    if (!e.Owner.WorldObject.Visible)
                        return false;
                    e.Position = ownerPos;
                    e.BlendMeshLight = 1.3f;
                    e.Angle.Z += MathHelper.ToRadians(10f) * f;
                    return true;

                case ClassicFxEffectType.WarcraftModel:
                    e.Position = ownerPos;
                    return true;

                case ClassicFxEffectType.ShieldCrash2Model:
                    if (e.LifeTime >= 0f && e.LifeTime < 8f)
                        e.Light = e.Direction * (e.LifeTime / 8f);
                    else if (e.LifeTime >= 8f && e.LifeTime < 24f)
                        e.Light = e.Direction *
                            (1f - (e.LifeTime - 24f) / 16f);
                    e.Position = ownerPos;
                    return true;
            }
            return false;
        }

        private bool TryS6Batch34OwnerPosition(
            ClassicFxOwner owner, out Vector3 position)
        {
            if (owner.WorldObject == null ||
                !ReferenceEquals(owner.WorldObject.World, World))
            {
                position = Vector3.Zero;
                return false;
            }
            position = owner.WorldObject.WorldPosition.Translation;
            return true;
        }
    }
}
