// ClassicFX S6 Batch 27: Infinity Arrow, Blade Skill and Rage Fighter BMDs.
// Source: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// EffectTypes.json; ZzzOpenData.cpp; ZzzEffect.cpp; MoveHandlers.cpp.
// Reuses the existing effect pool, ModelObject BMD renderer and owner/bone bridge.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch27ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.InfinityArrowCore &&
            type <= ClassicFxEffectType.DragonKickDummy;

        private static bool IsS6Batch27Infinity(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.InfinityArrowCore &&
            type <= ClassicFxEffectType.InfinityArrow4;

        private static bool IsS6Batch27Wolf(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.WolfHeadEffect or
                    ClassicFxEffectType.WolfHeadEffect2;

        private static bool TryGetS6Batch27ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch27ModelType(type)) return false;

            // MODEL_INFINITY_ARROW subtype 0 is a native spawning emitter,
            // not the visible BMD. It must be bridged as its own caller.
            if (type == ClassicFxEffectType.InfinityArrowCore && subType != 1)
                return false;
            if (type >= ClassicFxEffectType.InfinityArrow1 &&
                type <= ClassicFxEffectType.InfinityArrow3 &&
                (subType < 0 || subType > 3)) return false;
            if ((type is ClassicFxEffectType.InfinityArrow4 or
                 ClassicFxEffectType.DownAttackDummyL or
                 ClassicFxEffectType.DownAttackDummyR or
                 ClassicFxEffectType.DragonKickDummy) && subType != 0)
                return false;
            if (type == ClassicFxEffectType.BladeSkillModel &&
                (subType < 0 || subType > 1)) return false;
            if (type == ClassicFxEffectType.WolfHeadEffect &&
                (subType < 0 || subType > 2)) return false;
            if (type == ClassicFxEffectType.WolfHeadEffect2 &&
                (subType < 3 || subType > 6)) return false;

            string path = type switch
            {
                ClassicFxEffectType.InfinityArrowCore => "Skill/arrowsre01.bmd",
                ClassicFxEffectType.InfinityArrow1 => "Skill/arrowsre02.bmd",
                ClassicFxEffectType.InfinityArrow2 => "Skill/arrowsre03.bmd",
                ClassicFxEffectType.InfinityArrow3 => "Skill/arrowsre04.bmd",
                ClassicFxEffectType.InfinityArrow4 => "Skill/arrowsre05.bmd",
                ClassicFxEffectType.BladeSkillModel => "Effect/bladetonedo.bmd",
                ClassicFxEffectType.WolfHeadEffect => "Effect/wolf_head_effect.bmd",
                ClassicFxEffectType.WolfHeadEffect2 => "Effect/wolf_head_effect2.bmd",
                // ZzzOpenData has L/R inverted relative to file names.
                ClassicFxEffectType.DownAttackDummyL => "Effect/down_right_punch.bmd",
                ClassicFxEffectType.DownAttackDummyR => "Effect/down_left_punch.bmd",
                ClassicFxEffectType.DragonKickDummy => "Effect/dragon_kick_dummy.bmd",
                _ => null
            };
            if (path == null) return false;

            float life = IsS6Batch27Infinity(type)
                ? type == ClassicFxEffectType.InfinityArrow4 ? 15f :
                  type == ClassicFxEffectType.InfinityArrowCore ||
                  subType == 0 ? 40f : 60f
                : type == ClassicFxEffectType.BladeSkillModel
                    ? subType == 1 ? 14f : 10f
                : type == ClassicFxEffectType.DownAttackDummyL ||
                  type == ClassicFxEffectType.DownAttackDummyR ? 100f
                : type == ClassicFxEffectType.DragonKickDummy ? 200f : 10f;
            float scale = type == ClassicFxEffectType.BladeSkillModel
                ? subType == 0 ? 1.5f : 1f : 1f;
            definition = new Season6ModelDefinition(
                path, life, scale,
                needsOwner: type != ClassicFxEffectType.BladeSkillModel ||
                            subType == 1);
            return true;
        }

        private void ConfigureS6Batch27ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType, float nativeAnimationSpeed)
        {
            // Main copies the owner's stamp/dragon-kick PlaySpeed into
            // the dummy BMD action. ModelObject expects frames per second.
            if (type is ClassicFxEffectType.DownAttackDummyL or
                ClassicFxEffectType.DownAttackDummyR or
                ClassicFxEffectType.DragonKickDummy)
                view.AnimationSpeed = nativeAnimationSpeed * 25f;
            if (type == ClassicFxEffectType.BladeSkillModel &&
                subType == 1)
                view.BlendMesh = -2;
        }

        private bool InitializeS6Batch27Model(
            ClassicFxEffectType type, int subType, ClassicFxOwner owner,
            float nativeAnimationSpeed, ref Vector3 position,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref float mesh,
            ref Vector3 direction, ref float velocity)
        {
            if (IsS6Batch27Infinity(type))
            {
                if (owner.WorldObject == null) return false;
                light = Vector3.One;
                direction = light;
                life = type == ClassicFxEffectType.InfinityArrow4 ? 15f :
                    type == ClassicFxEffectType.InfinityArrowCore ||
                    subType == 0 ? 40f : 60f;
                scale = 1f;
                if (type == ClassicFxEffectType.InfinityArrowCore &&
                    TryGetOwnerBonePosition(owner, 29, out Vector3 arrowBone))
                {
                    position = arrowBone;
                    angle = owner.WorldObject.Angle;
                }
                return true;
            }
            if (type == ClassicFxEffectType.BladeSkillModel)
            {
                life = subType == 1 ? 14f : 10f;
                scale = subType == 1 ? 1f : 1.5f;
                return true;
            }
            if (IsS6Batch27Wolf(type))
            {
                if (owner.WorldObject == null) return false;
                bool source = type == ClassicFxEffectType.WolfHeadEffect
                    ? subType == 0 : subType == 3 || subType == 5;
                // These lifetimes explicitly depend on the owner's action
                // PlaySpeed. Do not invent an animation speed.
                if (source && (!float.IsFinite(nativeAnimationSpeed) ||
                    nativeAnimationSpeed <= 0f)) return false;
                int bone = type == ClassicFxEffectType.WolfHeadEffect2 &&
                    (subType == 3 || subType == 5) ? 5 : 27;
                if (TryGetOwnerBonePosition(owner, bone, out Vector3 at))
                    position = at;
                if (type == ClassicFxEffectType.WolfHeadEffect)
                {
                    if (subType == 0)
                    {
                        life = 10f / nativeAnimationSpeed;
                        scale = 1f;
                    }
                    else if (subType == 1)
                    {
                        life = 2f + Random.Modulo(2);
                        scale = 1.2f + Random.Modulo(5) * 0.1f;
                        direction = new Vector3(0f, -50f, 0f);
                    }
                    else
                    {
                        life = 3f + Random.Modulo(3);
                        scale = 0.8f + Random.Modulo(3) * 0.1f;
                        direction = new Vector3(0f, -40f, 0f);
                        angle.Z += MathHelper.ToRadians(
                            Random.Modulo(10) - 5f) * Clock.FrameFactor;
                    }
                }
                else
                {
                    light = new Vector3(0.4f, 0.4f, 0.6f);
                    if (subType == 3 || subType == 5)
                    {
                        life = 4f / nativeAnimationSpeed;
                        scale = subType == 5 ? 1.5f : 1f;
                    }
                    else
                    {
                        life = subType == 4
                            ? 5f + Random.Modulo(2)
                            : 2f + Random.Modulo(2);
                        scale = subType == 4
                            ? 0.7f + Random.Modulo(3) * 0.1f
                            : 1.2f + Random.Modulo(5) * 0.1f;
                        direction = new Vector3(0f,
                            subType == 4 ? -40f : -50f, 0f);
                    }
                }
                velocity = source ? 0f : 0.1f;
                return true;
            }
            if (type is ClassicFxEffectType.DownAttackDummyL or
                ClassicFxEffectType.DownAttackDummyR or
                ClassicFxEffectType.DragonKickDummy)
            {
                if (owner.WorldObject == null ||
                    !float.IsFinite(nativeAnimationSpeed) ||
                    nativeAnimationSpeed <= 0f)
                    return false;
                position = owner.WorldObject.WorldPosition.Translation;
                life = type == ClassicFxEffectType.DragonKickDummy
                    ? 200f : 100f;
                velocity = nativeAnimationSpeed * 2f;
                return true;
            }
            return false;
        }

        private bool MoveS6Batch27Model(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.BladeSkillModel)
            {
                if (e.SubType == 0)
                    e.Scale = MathF.Max(0.8f, e.Scale - 0.1f * f);
                else
                {
                    e.BlendMeshLight = e.LifeTime / 14f;
                    e.Alpha = e.BlendMeshLight;
                    // Native also moves the caster and emits six shiny
                    // sprites / 3 spark joints on alternate ticks. That
                    // target/caster mutation remains a callsite concern.
                }
                return true;
            }
            if (IsS6Batch27Infinity(e.Type))
            {
                var owner = e.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World))
                    return false;
                if (e.Type == ClassicFxEffectType.InfinityArrowCore)
                {
                    e.Light *= MathF.Pow(0.95f, f);
                    if (TryGetOwnerBonePosition(e.Owner, 29, out Vector3 at))
                        e.Position = at;
                    e.Angle = owner.Angle;
                    return true;
                }
                if (e.Type == ClassicFxEffectType.InfinityArrow4)
                    return true; // No native Move handler for this BMD.
                e.Light *= MathF.Pow(
                    e.SubType == 0 ? 0.95f : 0.98f, f);
                // Main copies the owner position for types 1/2/3.
                e.Position = owner.WorldPosition.Translation;
                if (e.SubType == 1 && Clock.AdvancedReferenceFrame)
                {
                    int tick = (int)MathF.Ceiling(e.LifeTime);
                    int bit = tick == 40 ? 1 : tick == 20 ? 2 : 0;
                    if (bit != 0 && (e.TriggerMask & bit) == 0)
                    {
                        e.TriggerMask |= (byte)bit;
                        CreateEffect(ClassicFxEffectType.InfinityArrow3,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.FromWorldObject(e.ModelView),
                            subType: bit == 1 ? 2 : 3);
                    }
                }
                return true;
            }
            if (IsS6Batch27Wolf(e.Type))
            {
                var owner = e.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World))
                    return false;
                // Native: wolf 0 tracks bone 27; wolf2 subtype 3 moves
                // with bone 4 and subtype 5 moves with bone 27. Remaining
                // children keep the positions captured when created.
                int bone = e.Type == ClassicFxEffectType.WolfHeadEffect
                    ? e.SubType == 0 ? 27 : -1
                    : e.SubType == 3 ? 4 :
                      e.SubType == 5 ? 27 : -1;
                if (bone >= 0 &&
                    TryGetOwnerBonePosition(e.Owner, bone, out Vector3 at))
                    e.Position = at;
                if (e.Type == ClassicFxEffectType.WolfHeadEffect2 &&
                    (e.SubType == 3 || e.SubType == 4 || e.SubType == 5))
                    e.Angle = owner.Angle;
                bool wolfSource = e.Type == ClassicFxEffectType.WolfHeadEffect
                    ? e.SubType == 0 : e.SubType == 3;
                if (wolfSource)
                {
                    e.BlendMeshLight = e.LifeTime / 20f;
                    e.Alpha = e.BlendMeshLight;
                    if (Clock.AdvancedReferenceFrame && e.LifeTime >= 3f)
                    {
                        // Native creates two linked model pieces per
                        // source-frame. Owner action mutations and the
                        // source's pin-light joints remain pending.
                        ClassicFxEffectType t = e.Type;
                        int a = t == ClassicFxEffectType.WolfHeadEffect ? 1 : 4;
                        int b = t == ClassicFxEffectType.WolfHeadEffect ? 2 : 6;
                        CreateEffect(t, e.Position, e.Angle, e.Light,
                            e.Owner, subType: a);
                        CreateEffect(t, e.Position, e.Angle, e.Light,
                            e.Owner, subType: b);
                    }
                }
                // Native child subtypes accelerate Owner->Velocity. Do not
                // mutate gameplay movement from this visual-only runtime.
                return true;
            }
            if (e.Type is ClassicFxEffectType.DownAttackDummyL or
                ClassicFxEffectType.DownAttackDummyR or
                ClassicFxEffectType.DragonKickDummy)
            {
                var owner = e.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World))
                    return false;
                e.Position = owner.WorldPosition.Translation;
                e.Angle = owner.Angle;
                if (e.Type == ClassicFxEffectType.DragonKickDummy)
                    return true; // Owner action/keyframe bridge still needed.
                if (Clock.AdvancedReferenceFrame && e.ModelView != null &&
                    TryGetOwnerBonePosition(
                        ClassicFxOwner.FromWorldObject(e.ModelView),
                        0, out Vector3 jointAt))
                {
                    var source = ClassicFxOwner.FromWorldObject(e.ModelView);
                    CreateJoint(ClassicTextureIds.BitmapForcePillar,
                        jointAt, jointAt, e.Angle,
                        subType: e.Type == ClassicFxEffectType.DownAttackDummyL
                            ? 0 : 1,
                        target: source, scale: 50f);
                    CreateSprite(ClassicTextureIds.BitmapFlare, jointAt,
                        5f, new Vector3(0.47f, 0.36f, 0.24f), source);
                }
                return true;
            }
            return false;
        }
    }
}
