// BroyalMU ClassicFX S6 Batch 08: Dark Lord Fire Scream / Mana Rune.
// Native pinned reference: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp (arrow-shaped spawn), MoveHandlers.cpp (movement),
// ZzzCharacter.cpp (six Fire Scream casts and Brand of Skill).
// Visual-only: hit testing, animation state, targeting, sound and damage
// remain with the existing client / OpenMU gameplay layer.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch08ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.DarkScream or
                ClassicFxEffectType.DarkScreamFire or
                ClassicFxEffectType.ManaRune;

        private static bool TryGetS6Batch08ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                // Native ZzzEffect.cpp initializes both as a 19-tick
                // arrow-shaped Effect. Subtype 1 is used by EmpireGuardian.
                case ClassicFxEffectType.DarkScream:
                    if (subType is not (0 or 1)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/darkfirescrem02.bmd", 19f, 0.9f);
                    return true;

                case ClassicFxEffectType.DarkScreamFire:
                    if (subType is not (0 or 1)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/darkfirescrem01.bmd", 19f, 2.3f);
                    return true;

                // EffectTypes.json: two distinct Mana Rune phases.
                // The caller's angle is replaced by native 45-degree Z.
                case ClassicFxEffectType.ManaRune:
                    if (subType == 0)
                    {
                        definition = new Season6ModelDefinition(
                            "Skill/ManaRune.bmd", 50f, 0f,
                            alpha: 0.3f, offsetZ: 300f);
                        return true;
                    }
                    if (subType == 1)
                    {
                        definition = new Season6ModelDefinition(
                            "Skill/ManaRune.bmd", 10f, 1.1f,
                            meshLight: 0.4f);
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private void InitializeS6Batch08Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref Vector3 direction,
            ref float velocity, ref float gravity)
        {
            if (type == ClassicFxEffectType.ManaRune)
            {
                gravity = 0.1f;
                if (subType == 0)
                    angle.Z = MathHelper.ToRadians(45f);
                return;
            }

            // ZzzEffect.cpp: native arrow-shaped launch offset. The
            // parent cast leaves the actual character transform intact.
            velocity = 1f;
            direction = new Vector3(0f, -35f, 0f);
            Matrix orientation = Matrix.CreateFromYawPitchRoll(
                angle.Y, angle.X, angle.Z);
            position += Vector3.TransformNormal(
                new Vector3(-10f, -60f, 135f), orientation);

            if (type == ClassicFxEffectType.DarkScream)
            {
                // Native places the shockwave close to terrain, *not*
                // at the z=135 launch height of the fire model.
                position.Z = RequestTerrainHeight(position.X, position.Y)
                    + 23f * Clock.FrameFactor;
            }
        }

        private static void ConfigureS6Batch08ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType)
        {
            if (type != ClassicFxEffectType.ManaRune)
                return;

            if (subType == 0)
            {
                // Native model is initially hidden until the expansion
                // phase (lifetime 43..40).
                view.HiddenMesh = -2;
            }
            else
            {
                view.BlendMesh = -2;
            }
        }

        private bool MoveS6Batch08Model(ref EffectState effect, float f)
        {
            switch (effect.Type)
            {
                case ClassicFxEffectType.DarkScream:
                case ClassicFxEffectType.DarkScreamFire:
                    return MoveS6DarkScream(ref effect, f);
                case ClassicFxEffectType.ManaRune:
                    return MoveS6ManaRune(ref effect, f);
                default:
                    return false;
            }
        }

        private void StartS6DarkScreamChildren(ref EffectState e)
        {
            // Native CreateEffect, not each Move. Wait for BMD load,
            // because both JOINT_FORCE variants require a live Owner.
            if (!e.FirstMove || e.ModelView == null ||
                e.ModelView.Status != GameControlStatus.Ready)
                return;

            e.FirstMove = false;
            if (e.Type != ClassicFxEffectType.DarkScream)
                return;

            ClassicFxOwner model = ClassicFxOwner.FromWorldObject(
                e.ModelView);
            CreateJoint(ClassicTextureIds.BitmapJointForce,
                e.Position, e.Position, e.Angle,
                subType: e.SubType == 0 ? 7 : 20,
                target: model,
                scale: e.SubType == 0 ? 150f : 400f,
                pkKey: 40);

            Vector3 blueBlurPosition = e.Position +
                Vector3.TransformNormal(new Vector3(0f, -20f, 0f),
                    Matrix.CreateFromYawPitchRoll(
                        e.Angle.Y, e.Angle.X, e.Angle.Z));
            CreateParticle(ClassicTextureIds.BitmapBlueBlur,
                blueBlurPosition, e.Angle,
                new Vector3(0f, 1f, 0f), subType: 1, scale: 1f);
        }

        private bool MoveS6DarkScream(ref EffectState e, float f)
        {
            StartS6DarkScreamChildren(ref e);

            // Native Move_MODEL_DARK_SCREAM_FIRE. Children are gated at
            // 25Hz so 60/120Hz rendering does not multiply particles.
            e.Scale = MathF.Max(0f,
                e.Scale - (e.Type == ClassicFxEffectType.DarkScreamFire
                    ? 0.14f : 0.04f) * f);
            e.Position.Z = RequestTerrainHeight(e.Position.X, e.Position.Y)
                + 3f;

            if (Clock.AdvancedReferenceFrame && e.Scale > 0.4f)
                CreateParticle(ClassicTextureIds.BitmapFlame,
                    e.Position, e.Angle, e.Light,
                    subType: 8, scale: (e.Scale - 0.4f) * 3.5f);

            // Native CheckClientArrow and AttackCharacterRange are
            // gameplay/hit-test operations, not visual FX. Never run
            // client-side damage or replicate packets in this engine.
            return true;
        }

        private bool MoveS6ManaRune(ref EffectState e, float f)
        {
            if (e.SubType == 1)
            {
                e.Scale = MathF.Max(1f, e.Scale - 0.02f * f);
                e.Alpha = MathF.Max(0f, e.LifeTime / 20f);
                return true;
            }

            if (e.SubType != 0)
                return false;

            if (e.ModelView != null)
            {
                if (e.LifeTime > 43f)
                    e.ModelView.HiddenMesh = -2;
                else if (e.LifeTime > 40f)
                    e.ModelView.HiddenMesh = 0;
            }

            if (e.LifeTime <= 40f && e.LifeTime > 30f)
            {
                e.Scale += e.Gravity * f;
                e.Gravity += 0.15f * f;

                // Native emits MODEL_MANA_RUNE subtype 1 on reaching
                // unit size. TriggerMask prevents multi-frame duplicates.
                if (e.Scale >= 1f)
                {
                    e.Scale = 1f;
                    e.Gravity = 0.01f;
                    if ((e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        CreateEffect(ClassicFxEffectType.ManaRune,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.None, subType: 1);
                    }
                }
            }
            else if (e.LifeTime < 15f)
            {
                e.Scale = MathF.Max(0f, e.Scale - e.Gravity * f);
                e.Gravity += 0.2f * f;
                e.Alpha = MathF.Max(0f, e.LifeTime / 20f);
                e.Position.Z -= 10f * f;
            }
            return true;
        }
    }
}
