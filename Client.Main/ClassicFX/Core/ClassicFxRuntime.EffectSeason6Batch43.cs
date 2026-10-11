// BroyalMU ClassicFX Season 6 — Batch 43.
// Native pinned Main: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp CreateEffect(), MoveHandlers.cpp and ZzzEffectJoint.cpp.
// No additional BMD or effect renderer: existing pooled primitives only.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch43LogicalType(ClassicFxEffectType t, int sub) =>
            t switch
            {
                ClassicFxEffectType.FirePlusOneEmitter => sub == 0,
                ClassicFxEffectType.DragonLoreLava => sub == 0,
                ClassicFxEffectType.HolyArrowJointCarrier => sub is 0 or 1 or 2,
                _ => false
            };

        private bool ValidateS6Batch43Owner(
            ClassicFxEffectType t, ClassicFxOwner owner)
        {
            if (t == ClassicFxEffectType.FirePlusOneEmitter)
                return owner.WorldObject == null ||
                    ReferenceEquals(owner.WorldObject.World, World);
            if (owner.WorldObject is not ModelObject model ||
                !ReferenceEquals(model.World, World) ||
                model.Status != GameControlStatus.Ready)
                return false;

            if (t != ClassicFxEffectType.DragonLoreLava)
                return true;

            // BITMAP_LAVA is tied to the Dragon Lore action, not a free
            // particle source which may run indefinitely after a cast.
            if (model is not PlayerObject player ||
                player.CurrentAction != PlayerAction.PlayerSkillDragonlore)
                return false;
            int action = (int)PlayerAction.PlayerSkillDragonlore;
            return player.Model?.Actions != null &&
                (uint)action < (uint)player.Model.Actions.Length &&
                player.Model.Actions[action] != null &&
                HasS6Batch43DragonLoreBones(player);
        }

        private static bool HasS6Batch43DragonLoreBones(ModelObject model)
        {
            Matrix[] bones = model.GetBoneTransforms();
            return bones != null && bones.Length > 36;
        }

        private void InitializeS6Batch43Logical(
            ClassicFxEffectType type, ClassicFxOwner owner,
            ref Vector3 position, Vector3 angle, ref float scale,
            ref float velocity, ref Vector3 direction, out float life)
        {
            life = type switch
            {
                ClassicFxEffectType.FirePlusOneEmitter => 10f,
                ClassicFxEffectType.DragonLoreLava => 999f,
                _ => 30f // MODEL_ARROW_HOLY
            };
            switch (type)
            {
                case ClassicFxEffectType.FirePlusOneEmitter:
                    // CreateEffect(BITMAP_FIRE + 1) displaces once.
                    position += ClassicMath.VectorRotate(
                        new Vector3(0f, -60f, 0f),
                        ClassicMath.AngleMatrix(angle)) * Clock.FrameFactor;
                    position.Z += 130f * Clock.FrameFactor;
                    break;

                case ClassicFxEffectType.DragonLoreLava:
                    // Original play-speed is read from the casting player BMD.
                    PlayerObject player = (PlayerObject)owner.WorldObject;
                    int action = (int)PlayerAction.PlayerSkillDragonlore;
                    velocity = player.Model.Actions[action].PlaySpeed;
                    break;

                case ClassicFxEffectType.HolyArrowJointCarrier:
                    scale = 1f;
                    direction = new Vector3(0f, -60f, 0f);
                    position.Z += 130f * Clock.FrameFactor;
                    break;
            }
        }

        private static Vector3 GetS6Batch43HolyStart(Vector3 pos, Vector3 angle)
        {
            // Exact MODEL_ARROW_HOLY StartPosition offset.
            return pos + ClassicMath.VectorRotate(
                new Vector3(-10f, -100f, 15f),
                ClassicMath.AngleMatrix(angle));
        }

        private void EmitS6Batch43OnCreate(
            ClassicFxEffectType type, ClassicFxHandle handle,
            ref EffectState e)
        {
            if (type != ClassicFxEffectType.HolyArrowJointCarrier)
                return;

            // MODEL_ARROW_HOLY's own model is NOT rendered in Main.
            // The 3/4 flare joints are its visible projectile trails.
            int jointSub = e.SubType == 1 ? 25 : 11;
            Vector3 jointAngle = new Vector3(0f, 0f, e.Angle.Z);
            ClassicFxOwner fxOwner = ClassicFxOwner.FromClassicFx(handle);
            int count = e.SubType == 1 ? 3 : 4;
            for (int i = 0; i < count; i++)
                CreateJoint(ClassicTextureIds.BitmapFlare,
                    e.StartPosition, e.StartPosition, jointAngle,
                    jointSub, fxOwner, 50f, pkKey: (short)(i == 1 ? 1 : -1));
        }

        private bool MoveS6Batch43Logical(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.FirePlusOneEmitter:
                    // MoveEffects(BITMAP_FIRE+1) is an emitter, not a plane.
                    if (Clock.AdvancedReferenceFrame)
                        CreateParticle(ClassicTextureIds.BitmapFire + 1,
                            e.Position, e.Angle, Vector3.One, 1);
                    return true;

                case ClassicFxEffectType.DragonLoreLava:
                    return MoveS6Batch43DragonLore(ref e);

                case ClassicFxEffectType.HolyArrowJointCarrier:
                    e.Angle.Y += (e.SubType == 1 ? 60f : 30f) * f;
                    // Native piercing child at remaining tick 13. One
                    // emission per logical carrier, independent of FPS.
                    if (Clock.AdvancedReferenceFrame &&
                        e.SubType != 0 && e.LifeTime <= 13f &&
                        (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        CreateEffect(ClassicFxEffectType.Piercing,
                            e.Position, e.Angle, e.Light, e.Owner,
                            subType: e.SubType == 1 ? 3 : 0);
                    }
                    // CheckClientArrow / collision and network gameplay
                    // intentionally remain in the caller's projectile layer.
                    return e.Owner.WorldObject != null &&
                        ReferenceEquals(e.Owner.WorldObject.World, World) &&
                        e.Owner.WorldObject.Status == GameControlStatus.Ready;
            }
            return false;
        }

        private bool MoveS6Batch43DragonLore(ref EffectState e)
        {
            if (e.Owner.WorldObject is not PlayerObject player ||
                !ReferenceEquals(player.World, World) ||
                player.Status != GameControlStatus.Ready ||
                player.CurrentAction != PlayerAction.PlayerSkillDragonlore ||
                !HasS6Batch43DragonLoreBones(player))
                return false;

            int action = (int)PlayerAction.PlayerSkillDragonlore;
            if (player.Model?.Actions == null ||
                (uint)action >= (uint)player.Model.Actions.Length ||
                player.Model.Actions[action] == null ||
                player.CurrentFrame > player.Model.Actions[action].NumAnimationKeys)
                return false;

            if (!Clock.AdvancedReferenceFrame) return true;

            float frame = player.CurrentFrame;
            if (frame > 2f && frame < 8f)
            {
                // Main re-samples historical animation frames. MonoGame
                // currently exposes the live bone pose only; use two real
                // anchors rather than manufacturing duplicate old poses.
                if (TryGetOwnerBonePosition(e.Owner, 36, out Vector3 from))
                    CreateJoint(ClassicTextureIds.BitmapLava,
                        from, from, player.Angle, 0, e.Owner, 20f);
                if (TryGetOwnerBonePosition(e.Owner, 28, out Vector3 to))
                    CreateJoint(ClassicTextureIds.BitmapLava,
                        to, to, player.Angle, 7, e.Owner, 20f);
            }

            if (frame <= 5f) return true;

            Vector3 ownerPos = player.WorldPosition.Translation;
            Vector3 ownerAngle = player.Angle;
            if (e.LifeTime > 100f)
            {
                CreateEffect(ClassicFxEffectType.Shockwave01,
                    ownerPos, ownerAngle, new Vector3(1f, 0.12f, 0f),
                    e.Owner, subType: 1);
                e.LifeTime = 95f;
            }
            if (e.LifeTime > 80f)
            {
                CreateEffect(ClassicFxEffectType.ShockWaveGround01,
                    e.Position, e.Angle, new Vector3(1f, 0.1f, 0f),
                    e.Owner, subType: 1);
                e.LifeTime = 80f;
            }

            Vector3 color = Vector3.One;
            float size = e.Scale * (Random.Modulo(5) + 18) * 0.1f;
            Vector3 p = e.Position;
            p.Z += (80f - e.LifeTime) * 7f;
            switch (Random.Modulo(3))
            {
                case 0:
                    CreateParticle(ClassicTextureIds.BitmapFireHik1,
                        p, e.Angle, color, 0, size);
                    break;
                case 1:
                    CreateParticle(ClassicTextureIds.BitmapFireCursedLich,
                        p, e.Angle, color, 4, size);
                    break;
                default:
                    CreateParticle(ClassicTextureIds.BitmapFireHik3,
                        p, e.Angle, color, 0, size);
                    break;
            }
            float initialHeight = p.Z;
            for (int i = 1; i < 8; i += 2)
            {
                Vector3 offset = new Vector3(
                    Random.Modulo(100) - 50,
                    Random.Modulo(100) - 50, 0f);
                Vector3 roll = new Vector3(Random.Modulo(90), 0f, 45f * i);
                p += ClassicMath.VectorRotate(
                    offset, ClassicMath.AngleMatrix(roll));
                p.Z = initialHeight;
                CreateParticle(ClassicTextureIds.BitmapFireHik1,
                    p, e.Angle, new Vector3(0.5f), 5, size);
                Vector3 sparkPos = p;
                sparkPos.Z = 250f;
                CreateParticle(ClassicTextureIds.BitmapSpark,
                    sparkPos, e.Angle, e.Light, 12, 1.5f);
            }
            return true;
        }
    }
}
