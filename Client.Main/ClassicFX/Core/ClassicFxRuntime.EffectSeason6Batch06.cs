// BroyalMU ClassicFX Season 6 - Batch 06: shared projectile Effect models.
// Source pinned: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp, Behaviors/MoveHandlers.cpp, ZzzOpenData.cpp.
// Visual-only. No CheckClientArrow(), damage, hit detection or network sends.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch06ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Piercing or
                ClassicFxEffectType.ArrowBomb or
                ClassicFxEffectType.ArrowNature or
                ClassicFxEffectType.ArrowDouble or
                ClassicFxEffectType.ArrowWing;

        private static bool TryGetS6Batch06ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.Piercing:
                    // MODEL_PIERCING's owner is mandatory in native movement.
                    if (subType is < 0 or > 3) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Piercing.bmd", subType == 2 ? 10f : 100f,
                        1f, needsOwner: true);
                    return true;

                case ClassicFxEffectType.ArrowBomb:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/ArrowBomb01.bmd", 40f, 1f);
                    return true;

                case ClassicFxEffectType.ArrowNature:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/ArrowNature01.bmd", 30f, 0.8f);
                    return true;

                case ClassicFxEffectType.ArrowDouble:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/ArrowDouble01.bmd", 30f, 1f);
                    return true;

                case ClassicFxEffectType.ArrowWing:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/ArrowWing01.bmd", 30f, 1.8f);
                    return true;
                default:
                    return false;
            }
        }

        private void InitializeS6Batch06Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref Vector3 direction,
            ref float velocity, ref float gravity)
        {
            if (type == ClassicFxEffectType.Piercing)
            {
                // Native MODEL_PIERCING sets a large scale only while it
                // creates its four owned flare joints; the BMD scale is 1.
                if (subType == 3)
                    light = new Vector3(0.9f, 0.4f, 0.6f);
                return;
            }

            if (type == ClassicFxEffectType.ArrowDouble)
            {
                position.Z += 130f * Clock.FrameFactor;
                direction = new Vector3(0f, -70f, 0f);
                return;
            }

            // Native arrow launch offset from AngleMatrix, preserving the
            // existing MonoGame angular convention (radians, not degrees).
            Matrix facing = Matrix.CreateFromYawPitchRoll(
                angle.Y, angle.X, angle.Z);
            position += Vector3.TransformNormal(
                new Vector3(-10f, -60f, 135f), facing) *
                (type == ClassicFxEffectType.ArrowBomb ? Clock.FrameFactor : 1f);
            velocity = 1f;
            if (type == ClassicFxEffectType.ArrowBomb)
            {
                direction = new Vector3(0f, -30f, 0f);
                gravity = -10f;
            }
            else if (type == ClassicFxEffectType.ArrowWing)
            {
                direction = new Vector3(0f, -50f, 0f);
                gravity = -10f;
            }
            else
            {
                direction = new Vector3(0f, -70f, 0f);
            }
        }

        private static void ConfigureS6Batch06ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            switch (type)
            {
                case ClassicFxEffectType.Piercing:
                    view.BlendMesh = 0;
                    if (subType is 1 or 2) view.HiddenMesh = 0;
                    break;
                case ClassicFxEffectType.ArrowBomb:
                case ClassicFxEffectType.ArrowWing:
                    view.BlendMesh = 0;
                    break;
                case ClassicFxEffectType.ArrowNature:
                case ClassicFxEffectType.ArrowDouble:
                    view.BlendMesh = -2;
                    break;
            }
        }

        private bool MoveS6Batch06Model(ref EffectState e, float f)
        {
            if (e.Type != ClassicFxEffectType.Piercing &&
                e.Type != ClassicFxEffectType.ArrowBomb)
                SpawnS6PiercingChild(ref e);
            switch (e.Type)
            {
                case ClassicFxEffectType.Piercing:
                    return MoveS6Piercing(ref e, f);
                case ClassicFxEffectType.ArrowBomb:
                    return MoveS6ArrowBomb(ref e, f);
                case ClassicFxEffectType.ArrowNature:
                    return MoveS6ArrowNature(ref e, f);
                case ClassicFxEffectType.ArrowDouble:
                    return MoveS6ArrowDouble(ref e, f);
                case ClassicFxEffectType.ArrowWing:
                    return MoveS6ArrowWing(ref e, f);
                default:
                    return false;
            }
        }

        private void SpawnS6PiercingChild(ref EffectState e)
        {
            // Native ArrowNature / ArrowWing subtype 2, ArrowDouble any
            // nonzero subtype: parent projectile owns MODEL_PIERCING(0).
            bool needsChild = (e.Type == ClassicFxEffectType.ArrowDouble &&
                    e.SubType != 0) ||
                ((e.Type == ClassicFxEffectType.ArrowNature ||
                  e.Type == ClassicFxEffectType.ArrowWing) && e.SubType == 2);
            if (!needsChild || (e.TriggerMask & 2) != 0 ||
                e.ModelView == null ||
                e.ModelView.Status != GameControlStatus.Ready)
                return;
            ClassicFxHandle created = CreateEffect(
                ClassicFxEffectType.Piercing,
                e.Position, e.Angle, e.Light,
                ClassicFxOwner.FromWorldObject(e.ModelView), subType: 0);
            if (created.IsValid)
                e.TriggerMask |= 2;
        }

        private bool MoveS6Piercing(ref EffectState e, float f)
        {
            if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot owner) ||
                owner.WorldObject == null ||
                !ReferenceEquals(owner.WorldObject.World, World))
                return false;

            // Native four flare joints use a parent effect owner. FirstMove
            // avoids repeating the CreateEffect initialiser every tick.
            if (e.FirstMove && e.ModelView != null &&
                e.ModelView.Status == GameControlStatus.Ready)
            {
                e.FirstMove = false;
                float jointScale = e.SubType switch
                {
                    0 => 12f,
                    1 => 24f,
                    2 => 12f,
                    _ => 10f
                };
                ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                for (int i = 0; i < 4; i++)
                    CreateJoint(ClassicTextureIds.BitmapFlare + 1,
                        e.Position, e.Position, e.Angle, i,
                        source, jointScale);
            }

            e.Gravity += 90f * f;
            e.Position = owner.Position;
            e.Angle = owner.Angle;
            if (e.SubType == 1 && Clock.AdvancedReferenceFrame)
            {
                CreateJoint(ClassicTextureIds.BitmapJointThunder,
                    e.Position, e.Position, e.Angle, 3,
                    ClassicFxOwner.None, 20f);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, (8f + Random.Modulo(8)) * 0.2f,
                    Vector3.One, ClassicFxOwner.FromWorldObject(e.ModelView));
            }
            return true;
        }

        // This moves visual projectiles only. Native CheckClientArrow and
        // AttackCharacterRange MUST stay in gameplay / server protocol code.
        private static void AdvanceS6Arrow(ref EffectState e, float f)
        {
            Matrix orientation = Matrix.CreateFromYawPitchRoll(
                e.Angle.Y, e.Angle.X, e.Angle.Z);
            e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;
        }

        private bool MoveS6ArrowNature(ref EffectState e, float f)
        {
            if (e.FirstMove && e.ModelView != null &&
                e.ModelView.Status == GameControlStatus.Ready)
            {
                e.FirstMove = false;
                if (e.SubType == 1)
                    CreateJoint(ClassicTextureIds.BitmapFlare + 1,
                        e.Position, e.Position, e.Angle, 13,
                        ClassicFxOwner.FromWorldObject(e.ModelView), 20f);
            }
            AdvanceS6Arrow(ref e, f);
            e.Angle.Y += MathHelper.ToRadians(e.SubType == 1 ? 60f : 30f) * f;
            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            Vector3 tint = e.SubType == 1
                ? new Vector3(0.1f, 0.4f, 0.1f)
                : e.Light;
            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
            CreateSprite(ClassicTextureIds.BitmapLightning + 1,
                e.Position, e.SubType == 1 ? 0.3f : 0.5f, tint, source);
            CreateSprite(ClassicTextureIds.BitmapLightning + 1,
                e.Position, e.SubType == 1 ? 0.7f : 1f, tint, source);

            if (Clock.AdvancedReferenceFrame)
            {
                if (e.SubType == 1)
                {
                    // Native: early green flare dust, every second tick.
                    if (e.LifeTime > 25f && (int)e.LifeTime % 2 == 0)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 p = e.Position + new Vector3(
                                Random.Modulo(32) - 16f,
                                Random.Modulo(64) - 32f,
                                Random.Modulo(32) - 16f);
                            CreateParticle(ClassicTextureIds.BitmapFlare, p,
                                e.Angle, new Vector3(0.4f, 1f, 0.2f), 5, 0.2f);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 p = e.Position + new Vector3(
                            Random.Modulo(32) - 16f,
                            Random.Modulo(64) - 32f,
                            Random.Modulo(32) - 16f);
                        CreateParticle(ClassicTextureIds.BitmapFlower01 + Random.Modulo(3),
                            p, e.Angle, e.Light);
                    }
                }
            }
            Vector3 lamp = e.SubType == 1
                ? new Vector3(0.2f, 0.8f, 0.2f) * luminosity
                : new Vector3(0.6f, 0.8f, 0.8f) * luminosity;
            AddClassicTerrainLight(e.Position.X, e.Position.Y, lamp, 2f);
            return true;
        }

        private bool MoveS6ArrowWing(ref EffectState e, float f)
        {
            AdvanceS6Arrow(ref e, f);
            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
            CreateSprite(ClassicTextureIds.BitmapLightning + 1,
                e.Position, 0.5f, e.Light, source);
            CreateSprite(ClassicTextureIds.BitmapLightning + 1,
                e.Position, 1f, e.Light, source);
            if (Clock.AdvancedReferenceFrame)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(16) - 8f,
                        Random.Modulo(16) - 8f,
                        Random.Modulo(16) - 8f);
                    CreateParticle(ClassicTextureIds.BitmapBubble,
                        p, e.Angle, e.Light, 1);
                }
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    e.Position, e.Angle, e.Light, 0);
            }
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(0.6f, 0.8f, 0.8f), 2f);
            return true;
        }

        private bool MoveS6ArrowDouble(ref EffectState e, float f)
        {
            if (e.FirstMove && e.ModelView != null &&
                e.ModelView.Status == GameControlStatus.Ready)
            {
                e.FirstMove = false;
                CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                    e.Position, e.Position, e.Angle, 5,
                    ClassicFxOwner.FromWorldObject(e.ModelView), 100f);
            }
            AdvanceS6Arrow(ref e, f);
            e.Angle.Y += MathHelper.ToRadians(30f) * f;
            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(0.2f, 0.4f, 1f) * luminosity, 2f);
            return true;
        }

        private bool MoveS6ArrowBomb(ref EffectState e, float f)
        {
            AdvanceS6Arrow(ref e, f);
            // Native MoveJump adds vertical gravity after the arrow's
            // forward direction; exact CheckClientArrow collision is absent.
            e.Position.Z += e.Gravity * f;
            e.Gravity -= 0.5f * f;
            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
            Vector3 orange = new Vector3(1f, 0.6f, 0.4f);
            CreateSprite(ClassicTextureIds.BitmapLight,
                e.Position, 1f, orange, source);
            CreateSprite(ClassicTextureIds.BitmapLight,
                e.Position, 2f, orange, source);
            if (Clock.AdvancedReferenceFrame)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(16) - 8f,
                        Random.Modulo(16) - 8f,
                        Random.Modulo(16) - 8f);
                    CreateParticle(ClassicTextureIds.BitmapBubble,
                        p, e.Angle, e.Light, 1);
                }
            }
            if (e.LifeTime <= 1f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                // Visual approximation of native CreateBomb(); no damage.
                CreateParticle(ClassicTextureIds.BitmapExplotion,
                    e.Position, e.Angle, Vector3.One);
                CreateParticle(ClassicTextureIds.BitmapExplotion,
                    e.Position, e.Angle, orange);
            }
            float luminosity = (7f + Random.Modulo(4)) * 0.1f;
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(0.6f, 0.8f, 0.8f) * luminosity, 2f);
            return true;
        }
    }
}
