// BroyalMU ClassicFX Season 6 Batch 32 — Kundun, Aida and Imperial visuals.
// Pinned MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Shared ClassicFX pool and existing MonoGame BMD renderer only.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch32ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.KundunDragonHead &&
            type <= ClassicFxEffectType.DesairModel;

        private static bool TryGetS6Batch32ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch32ModelType(type)) return false;
            // Native only exposes the subType 0 visual path in this set.
            if (subType != 0) return false;

            string path = type switch
            {
                ClassicFxEffectType.KundunDragonHead => "Skill/dragonhead.bmd",
                ClassicFxEffectType.KundunPhoenix => "Skill/phoenix.bmd",
                ClassicFxEffectType.KundunGhost => "Monster/cundun_gone.bmd",
                ClassicFxEffectType.DeasulerBoomerang => "Monster/deasther_boomerang.bmd",
                ClassicFxEffectType.ImperialProjectile => "Effect/choarms_06.bmd",
                ClassicFxEffectType.SawSkillModel => "Skill/Saw01.bmd",
                ClassicFxEffectType.TreeAttackModel => "Object34/tree_eff.bmd",
                ClassicFxEffectType.DesairModel => "Skill/desair.bmd",
                _ => null
            };
            if (path == null) return false;

            // Models lacking an explicit CreateEffect lifespan are given
            // finite, documented bridge-only values until the real caller
            // supplies its lifetime; this is not native lifespan parity.
            float life = type switch
            {
                ClassicFxEffectType.KundunDragonHead => 30f,
                ClassicFxEffectType.KundunPhoenix => 20f,
                ClassicFxEffectType.DeasulerBoomerang => 55f,
                ClassicFxEffectType.SawSkillModel => 10f,
                ClassicFxEffectType.ImperialProjectile => 45f,
                ClassicFxEffectType.TreeAttackModel => 30f,
                ClassicFxEffectType.KundunGhost => 60f,
                _ => 30f
            };
            float scale = type switch
            {
                ClassicFxEffectType.KundunPhoenix => 0.7f,
                _ => 1f
            };
            definition = new Season6ModelDefinition(path, life, scale,
                needsOwner: type is ClassicFxEffectType.DeasulerBoomerang or
                    ClassicFxEffectType.ImperialProjectile);
            return true;
        }

        private static void ConfigureS6Batch32ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (type is ClassicFxEffectType.KundunDragonHead or
                ClassicFxEffectType.KundunPhoenix)
                view.BlendMesh = -2;
            if (type == ClassicFxEffectType.DeasulerBoomerang)
                view.HiddenMesh = 1; // Native hides mesh 1.
        }

        private bool InitializeS6Batch32Model(
            ClassicFxEffectType type, ClassicFxOwner owner,
            Vector3 inputLight, ref Vector3 position, ref Vector3 start,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref float mesh,
            ref Vector3 direction, ref Vector3 heading,
            ref float gravity, ref float velocity, ref float phase)
        {
            if (type == ClassicFxEffectType.KundunDragonHead)
            {
                life = 30f;
                scale = 1f;
                position.Z += 100f * Clock.FrameFactor;
                light = new Vector3(0.5f);
                alpha = 0f;
                angle = new Vector3(MathHelper.ToRadians(-30f), 0f,
                    MathHelper.ToRadians(Random.Modulo(360)));
                return true;
            }
            if (type == ClassicFxEffectType.KundunPhoenix)
            {
                life = 20f;
                velocity = 0.34f;
                scale = 0.7f;
                light = new Vector3(0.5f);
                alpha = 0f;
                return true;
            }
            if (type == ClassicFxEffectType.KundunGhost)
            {
                // The native model has no CreateEffect initializer.
                // Owner->PKKey/animation frame logic needs an Effect-owner
                // and animated BMD bridge. Do not invent an animation state.
                life = 60f; // Explicit provisional finite expiry.
                return true;
            }
            if (type == ClassicFxEffectType.DeasulerBoomerang)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World) ||
                    !float.IsFinite(inputLight.X) ||
                    !float.IsFinite(inputLight.Y) ||
                    !float.IsFinite(inputLight.Z))
                    return false;
                Vector3 hero = owner.WorldObject.WorldPosition.Translation;
                Vector3 planar = new Vector3(inputLight.X - hero.X,
                    inputLight.Y - hero.Y, 0f);
                if (planar.LengthSquared() < 0.0001f) return false;

                // C++ uses the input Light as a destination, not an RGB color.
                life = 55f;
                alpha = 1f;
                gravity = 0f;
                heading = angle;
                direction = Vector3.Normalize(planar);
                float distance = Vector3.Distance(inputLight, position);
                phase = MathHelper.Clamp(distance * 0.3f / 1700f,
                    0.0001f, 0.99f);
                start = new Vector3(position.X, position.Y, inputLight.Z + 100f);
                position = start;
                // Use neutral model RGB: inputLight is preserved in BaseLight.
                light = Vector3.One;
                return true;
            }
            if (type == ClassicFxEffectType.ImperialProjectile)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                life = 45f; // Native caller lifetime not registered.
                start = inputLight; // Native effect destination adapter.
                gravity = 0f;
                velocity = 0.3f;
                direction = Vector3.Zero;
                light = Vector3.One;
                return true;
            }
            if (type == ClassicFxEffectType.SawSkillModel)
            {
                life = 10f;
                position.Z += 130f * Clock.FrameFactor;
                direction = Vector3.TransformNormal(
                    new Vector3(0f, -60f, 0f),
                    Matrix.CreateFromYawPitchRoll(
                        angle.Y, angle.X, angle.Z));
                return true;
            }
            if (type == ClassicFxEffectType.TreeAttackModel)
            {
                life = 30f; // No native type-specific CreateEffect lifespan.
                return true;
            }
            if (type == ClassicFxEffectType.DesairModel)
            {
                life = 30f; // Registered BMD only; no native Move handler.
                return true;
            }
            return false;
        }

        private bool MoveS6Batch32Model(ref EffectState e, float f)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.KundunDragonHead:
                    if (Clock.AdvancedReferenceFrame)
                        e.Angle.Z += MathHelper.ToRadians(
                            10f + Random.Modulo(10)) * f;
                    if (e.Position.Z > 300f && e.Position.Z < 600f &&
                        Clock.AdvancedReferenceFrame &&
                        Random.FpsCheck(2, Clock))
                    {
                        Vector3 p = new Vector3(
                            e.Position.X, e.Position.Y, 350f);
                        CreateJoint(ClassicTextureIds.BitmapJointSpirit2,
                            p, p, new Vector3(0f, 0f,
                                MathHelper.ToRadians(
                                    Random.Modulo(24) * 30f)),
                            14, ClassicFxOwner.None, 100f);
                    }
                    if (e.ModelView != null && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        // Native spawns an owner-bound Spirit2 joint
                        // when the dragon BMD is created.
                        var modelOwner =
                            ClassicFxOwner.FromWorldObject(e.ModelView);
                        Vector3 p = e.Position +
                            new Vector3(0f, 0f, -100f);
                        CreateJoint(ClassicTextureIds.BitmapJointSpirit2,
                            p, p, e.Angle, 18, modelOwner, 100f);
                    }
                    return true;

                case ClassicFxEffectType.KundunPhoenix:
                    if (e.ModelView != null && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        var modelOwner =
                            ClassicFxOwner.FromWorldObject(e.ModelView);
                        Vector3 a = new Vector3(0f, 0f, e.Angle.Z);
                        CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                            e.Position, e.Position, a, 15,
                            modelOwner, 100f);
                    }
                    if (e.LifeTime < 5f)
                        e.Alpha = e.LifeTime * 0.2f;
                    return true;

                case ClassicFxEffectType.KundunGhost:
                    // Native movement needs an Effect owner PKKey and actual
                    // BMD AnimationFrame, not exposed by this owner adapter.
                    // Register the BMD without inventing its attack phase.
                    if (e.Owner.WorldObject != null &&
                        !ReferenceEquals(e.Owner.WorldObject.World, World))
                        return false;
                    return true;

                case ClassicFxEffectType.DeasulerBoomerang:
                    return MoveS6Batch32Deasuler(ref e);

                case ClassicFxEffectType.ImperialProjectile:
                    return MoveS6Batch32Projectile(ref e, f);

                case ClassicFxEffectType.SawSkillModel:
                    e.Angle.Z -= MathHelper.ToRadians(30f * f);
                    e.Position += e.Direction * f;
                    return true;

                case ClassicFxEffectType.TreeAttackModel:
                    e.Scale += 0.1f * f;
                    if (e.Scale >= 1f)
                    {
                        e.Scale = 1f;
                        e.Alpha -= 0.07f * f;
                    }
                    return true;

                case ClassicFxEffectType.DesairModel:
                    // C++ has no specialized movement routine.
                    return true;

                default:
                    return false;
            }
        }

        private bool MoveS6Batch32Deasuler(ref EffectState e)
        {
            // Native CInterpolateContainer's two linear position/angle
            // segments evaluated directly; avoids per-effect heap lists.
            if (e.Owner.WorldObject == null ||
                !ReferenceEquals(e.Owner.WorldObject.World, World))
                return false;

            const float total = 55f;
            float rate = MathHelper.Clamp(1f - e.LifeTime / total, 0f, 1f);
            float first = e.Phase;
            // Phase already encodes the native first-distance ratio
            // computed before moving the Z start plane at creation.
            Vector3 firstPosition = e.StartPosition +
                e.Direction * (1700f * first);
            Vector3 finalPosition = e.StartPosition + e.Direction * 1700f;

            Vector3 relativeAngle;
            if (rate <= first)
            {
                float t = rate / MathF.Max(first, 0.0001f);
                e.Position = Vector3.Lerp(e.StartPosition, firstPosition, t);
                relativeAngle = new Vector3(0f,
                    MathHelper.ToRadians(90f * t), 0f);
            }
            else
            {
                float t = MathHelper.Clamp(
                    (rate - first) / MathF.Max(1.01f - first, 0.0001f),
                    0f, 1f);
                e.Position = Vector3.Lerp(firstPosition, finalPosition, t);
                relativeAngle = new Vector3(0f,
                    MathHelper.ToRadians(90f),
                    MathHelper.ToRadians(2560f * t));
            }
            e.Angle = e.HeadAngle + relativeAngle;
            // Native samples bone 3D blur along the interpolated path.
            // Full BMD blur history must be added via shared Blur runtime.
            return true;
        }

        private bool MoveS6Batch32Projectile(ref EffectState e, float f)
        {
            if (e.Owner.WorldObject == null ||
                !ReferenceEquals(e.Owner.WorldObject.World, World))
                return false;

            if (e.LifeTime > 38f)
            {
                if (TryGetOwnerBonePosition(e.Owner, 9, out Vector3 at))
                    e.Position = at;
                return true;
            }
            if (e.Gravity == 0f)
            {
                e.Gravity = 0.1f;
                Vector3 d = e.Position - e.StartPosition;
                if (d.LengthSquared() > 0.0001f)
                    e.Direction = Vector3.Normalize(d);
            }
            e.Angle.X += MathHelper.ToRadians(0.5f) * f;
            e.Velocity += 0.2f * f;
            e.Position.X += e.Direction.X * e.Velocity * f;
            e.Position.Y += e.Direction.Y * e.Velocity * f;
            e.Position.Z += e.Direction.Z * f;
            return true;
        }
    }
}
