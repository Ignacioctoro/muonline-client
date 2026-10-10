// ClassicFX Season 6 Batch 09: Javelin, Arrow Impact, Skin Shell,
// Stun Stone and its CRATER terrain child.
// Pinned native: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp + Behaviors/MoveHandlers.cpp + ZzzAI.cpp.
// Uses existing BMD, terrain quad, sprite/particle/joint pools.
// No combat, skill packets, server-side hit checks, or legacy duplicates.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch09ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Javelin or
                ClassicFxEffectType.ArrowImpact or
                ClassicFxEffectType.SkinShell or
                ClassicFxEffectType.StunStone;

        private static bool TryGetS6Batch09ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.Javelin:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Javelin.bmd", 35f, 1.2f,
                        needsOwner: true);
                    return true;

                case ClassicFxEffectType.ArrowImpact:
                    // Native subtype 0 transitions into subtype 1 near the
                    // end of its lifetime; subtype 1 is NOT a new cast.
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/ArrowImpact.bmd", 20f, 1.8f,
                        needsOwner: true);
                    return true;

                case ClassicFxEffectType.SkinShell:
                    if (subType is not (0 or 1)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/skinshell.bmd", 40f, 0.4f);
                    return true;

                case ClassicFxEffectType.StunStone:
                    if (subType is not (0 or 1)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/GroundCrystal.bmd",
                        subType == 0 ? 40f : 22f, 1f);
                    return true;

                default:
                    return false;
            }
        }

        private void InitializeS6Batch09Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle,
            ref Vector3 light, ref float scale, ref float life,
            ref float alpha, ref float meshLight,
            ref Vector3 direction, ref float velocity,
            ref float gravity, ref Vector3 headAngle)
        {
            switch (type)
            {
                case ClassicFxEffectType.Javelin:
                    gravity = 2f;
                    velocity = 10f;
                    direction = new Vector3(0f, -5f, 0f);
                    position.Z += 150f * Clock.FrameFactor;
                    // Native HeadAngle starts as the input Angle. The three
                    // visual javelins then receive independent random yaw.
                    float spread = Random.Modulo(80) + 10f;
                    headAngle = angle;
                    headAngle.Z += MathHelper.ToRadians(
                        (subType - 1) * spread * Clock.FrameFactor);
                    break;

                case ClassicFxEffectType.ArrowImpact:
                    velocity = 1f;
                    direction = new Vector3(0f, -30f, 0f);
                    light = new Vector3(0.3f, 0.8f, 1f);
                    position += Vector3.TransformNormal(
                        new Vector3(-10f, -80f, 200f),
                        Matrix.CreateFromYawPitchRoll(
                            angle.Y, angle.X, angle.Z)) * Clock.FrameFactor;
                    angle.X = MathHelper.ToRadians(-30f);
                    break;

                case ClassicFxEffectType.SkinShell:
                {
                    position.Z += 50f * Clock.FrameFactor;
                    life = 32f + Random.Modulo(16);
                    scale = (3f + Random.Modulo(3)) * 0.1f;
                    alpha = 0.5f;
                    float heading = MathHelper.ToRadians(Random.Modulo(360));
                    float speed = (32f + Random.Modulo(128)) * 0.1f;
                    direction = Vector3.TransformNormal(
                        new Vector3(0f, speed, 0f),
                        Matrix.CreateRotationZ(heading));
                    gravity = 2f + Random.Modulo(5) +
                        Random.Modulo(5) * Clock.FrameFactor;
                    angle = new Vector3(
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)),
                        MathHelper.ToRadians(Random.Modulo(360)));
                    light = subType == 0
                        ? new Vector3(0.5f)
                        : new Vector3(0.1f, 0.6f, 1f);
                    break;
                }

                case ClassicFxEffectType.StunStone:
                    if (subType == 0)
                    {
                        life = 40f;
                        scale = 1.1f + Random.Modulo(100) / 100f;
                        gravity = -(Random.Modulo(16) + 10f);
                        position.Z += 600f * Clock.FrameFactor;
                        direction = Vector3.TransformNormal(
                            new Vector3(Random.Modulo(256) / 64f - 2f,
                                -(Random.Modulo(200) + 64f) * 0.1f, 0f),
                            Matrix.CreateFromYawPitchRoll(
                                angle.Y, angle.X, angle.Z));
                        headAngle = new Vector3(0f, 0f, angle.Z);
                        angle = new Vector3(
                            MathHelper.ToRadians(-20f), 0f,
                            MathHelper.ToRadians(Random.Modulo(360)));
                        light = Vector3.One;
                    }
                    else
                    {
                        life = 22f;
                        direction = new Vector3(0f, -25f, 0f);
                        light = new Vector3(-1f);
                    }
                    break;
            }
        }

        private static void ConfigureS6Batch09ModelView(
            ClassicFxEffectModelObject view,
            ClassicFxEffectType type, int subType)
        {
            switch (type)
            {
                case ClassicFxEffectType.ArrowImpact:
                    view.BlendMesh = -2;
                    break;
                case ClassicFxEffectType.StunStone:
                    if (subType == 1)
                        view.HiddenMesh = -2; // native emitter-only phase
                    break;
            }
        }

        private bool MoveS6Batch09Model(ref EffectState effect, float f)
        {
            switch (effect.Type)
            {
                case ClassicFxEffectType.Javelin:
                    return MoveS6Javelin(ref effect, f);
                case ClassicFxEffectType.ArrowImpact:
                    return MoveS6ArrowImpact(ref effect, f);
                case ClassicFxEffectType.SkinShell:
                    return MoveS6SkinShell(ref effect, f);
                case ClassicFxEffectType.StunStone:
                    return MoveS6StunStone(ref effect, f);
                default:
                    return false;
            }
        }

        // 25-Hz stable equivalent of ZzzAI.cpp MoveHumming's TurnAngle2.
        // MonoGame uses radians; the native AngleMatrix/TurnAngle2 uses
        // degrees. Direction=(0,-5,0) is the original Javelin forward axis.
        private static float TurnS6JavelinHeading(
            Vector3 source, Vector3 destination,
            ref Vector3 heading, float degreesPerNativeTick, float f)
        {
            Vector3 delta = destination - source;
            float xy = MathF.Sqrt(delta.X * delta.X +
                delta.Y * delta.Y);
            float yaw = MathF.Atan2(delta.X, -delta.Y);
            float pitch = MathF.Atan2(-delta.Z, xy);
            float limit = MathHelper.ToRadians(
                MathF.Max(0f, degreesPerNativeTick * f));
            heading.Z += Math.Clamp(
                MathHelper.WrapAngle(yaw - heading.Z), -limit, limit);
            heading.X += Math.Clamp(
                MathHelper.WrapAngle(pitch - heading.X), -limit, limit);
            return delta.Length();
        }

        private bool MoveS6Javelin(ref EffectState e, float f)
        {
            if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot target) ||
                target.WorldObject == null ||
                !ReferenceEquals(target.WorldObject.World, World) ||
                target.WorldObject.Status != GameControlStatus.Ready)
                return false;

            // Native Owner is the *target*, not the caster.
            e.StartPosition = target.Position +
                new Vector3(0f, 0f, 150f * Clock.FrameFactor);
            e.Scale += 0.015f * f;

            if (e.LifeTime < 25f)
            {
                if (e.LifeTime < 20f)
                {
                    float distance = TurnS6JavelinHeading(
                        e.Position, e.StartPosition,
                        ref e.HeadAngle, e.Velocity, f);
                    if (distance < 100f)
                    {
                        e.Position = e.StartPosition;
                        e.Scale += 0.04f * f;
                    }

                    // Native emits a pounding ball when approaching the
                    // target; fixed native ticks prevent refresh flood.
                    if (Clock.AdvancedReferenceFrame &&
                        (distance >= 100f || (int)e.LifeTime % 3 == 0))
                        CreateParticle(ClassicTextureIds.BitmapPoundingBall,
                            e.Position, e.Angle, e.Light, 1);
                }
                e.Velocity += 1.5f * f;
                e.Position += Vector3.TransformNormal(
                    e.Direction, Matrix.CreateFromYawPitchRoll(
                        e.HeadAngle.Y, e.HeadAngle.X, e.HeadAngle.Z)) * f;
                if (Clock.AdvancedReferenceFrame)
                    e.Gravity = Random.Modulo(30) + 30f;
                if (e.Direction.Y > -50f)
                    e.Direction.Y -= 8f * f;
            }
            else
            {
                e.Gravity += 10f * f;
                e.Position += Vector3.TransformNormal(
                    e.Direction, Matrix.CreateFromYawPitchRoll(
                        e.HeadAngle.Y, e.HeadAngle.X, e.HeadAngle.Z)) * f;
            }

            e.Angle.Z += MathHelper.ToRadians(e.Gravity * f);
            e.BlendMeshLight = e.LifeTime / 10f;
            e.Alpha = e.BlendMeshLight;
            float height = MathF.Sin(e.LifeTime * 0.1f) * 30f;
            if (e.SubType == 1)
                e.Position.Z = e.StartPosition.Z + height;
            else if (e.SubType == 2)
                e.Position.Z = e.StartPosition.Z - height;

            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(1f, 0.6f, 0.3f), 2f);
            return true;
        }

        private bool MoveS6ArrowImpact(ref EffectState e, float f)
        {
            if (e.SubType == 1)
                return false;
            if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot target) ||
                target.WorldObject == null ||
                !ReferenceEquals(target.WorldObject.World, World))
                return false;

            // Native CreateEffect emits FLASH4 and binds it to the
            // effect model, not to the character. Defer until BMD is ready.
            if (e.FirstMove && e.ModelView != null &&
                e.ModelView.Status == GameControlStatus.Ready)
            {
                ClassicFxHandle joint = CreateJoint(
                    ClassicTextureIds.BitmapFlash,
                    e.Position, e.Position, e.Angle,
                    subType: 4,
                    target: ClassicFxOwner.FromWorldObject(e.ModelView),
                    scale: 40f, pkKey: 50);
                if (joint.IsValid) e.FirstMove = false;
            }

            e.Angle.X -= MathHelper.ToRadians(5f * f);
            e.Direction.Y -= 8f * f;
            e.Position += Vector3.TransformNormal(e.Direction,
                Matrix.CreateFromYawPitchRoll(
                    e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;

            if (e.LifeTime < 2f)
            {
                e.SubType = 1;
                e.Angle.X = MathHelper.ToRadians(90f);
                Vector3 from = target.Position + new Vector3(
                    Random.Modulo(100) - 50f,
                    Random.Modulo(100) - 50f, 1200f);
                ClassicFxOwner fxOwner = e.ModelView != null
                    ? ClassicFxOwner.FromWorldObject(e.ModelView)
                    : ClassicFxOwner.None;
                if (fxOwner.HasOwner)
                {
                    CreateJoint(ClassicTextureIds.BitmapFlash,
                        from, from, e.Angle, subType: 2,
                        target: fxOwner, scale: 50f);
                    CreateJoint(ClassicTextureIds.BitmapFlash,
                        from, from, e.Angle, subType: 3,
                        target: fxOwner, scale: 50f);
                }
            }
            // CheckClientArrow and packet serial are not visual effects.
            return true;
        }

        private bool MoveS6SkinShell(ref EffectState e, float f)
        {
            e.Position += e.Direction * f;
            // Native applies 0.9 and 1.1, in that order, each tick.
            e.Direction *= MathF.Pow(0.99f, f);
            e.Position.Z += e.Gravity * f;
            e.Gravity -= 3f * f;

            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z < ground)
            {
                e.Position.Z = ground;
                e.Gravity = -e.Gravity * 0.2f;
                e.LifeTime -= 5f * f;
                e.Angle.X -= MathHelper.ToRadians(
                    e.Scale * 128f * f);
            }
            else
            {
                e.Angle.X -= MathHelper.ToRadians(
                    e.Scale * 16f * f);
            }
            e.Alpha = e.LifeTime / 10f;
            return true;
        }

        private bool MoveS6StunStone(ref EffectState e, float f)
        {
            if (e.SubType == 1)
            {
                int tick = (int)e.LifeTime;
                if (Clock.AdvancedReferenceFrame && tick >= 0 &&
                    tick % 3 == 0 && tick != e.LastChildNativeTick)
                {
                    e.LastChildNativeTick = tick;
                    CreateEffect(ClassicFxEffectType.StunStone,
                        e.Position, e.Angle, e.Light,
                        ClassicFxOwner.None, subType: 0);
                }
                AddClassicTerrainLight(e.Position.X, e.Position.Y,
                    e.Light, 1f);
                return true;
            }

            e.Position.Z += e.Gravity * f;
            e.Gravity -= 15f * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y)
                + 50f;
            if (e.Position.Z <= ground)
            {
                e.Position.Z = ground;
                if ((e.TriggerMask & 1) == 0)
                {
                    e.TriggerMask |= 1;
                    CreateEffect(ClassicFxEffectType.Crater,
                        e.Position, e.Angle, Vector3.One,
                        ClassicFxOwner.None, subType: 1);
                }
            }
            else
            {
                e.Position += e.Direction * f;
                e.Direction *= MathF.Pow(0.9f, f);
                e.HeadAngle.X -= MathHelper.ToRadians(e.Scale * 32f * f);
                if (Clock.AdvancedReferenceFrame &&
                    (int)e.LifeTime % 3 == 0)
                    CreateParticle(ClassicTextureIds.BitmapAdvSmoke + 1,
                        e.Position, e.Angle, e.Light, 1, 1f);
            }

            float fade = e.LifeTime / 10f;
            e.Light = new Vector3(fade);
            e.Alpha = fade;
            return true;
        }

        private bool MoveS6Crater(ref EffectState e, float f)
        {
            // MuMain Move_BITMAP_CRATER: short fade and negative ground light.
            if (e.LifeTime < 10f)
            {
                float fading = MathF.Max(0f, e.LifeTime / 10f);
                e.Light = new Vector3(fading);
                e.Alpha = fading;
            }
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(-0.5f), MathF.Max(1f, e.Scale - 1f));
            return true;
        }
    }
}
