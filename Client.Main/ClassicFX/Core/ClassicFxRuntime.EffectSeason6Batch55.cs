// ClassicFX Season 6 — full BK/MG Twisting Slash visual sequence.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect / RenderWheelWeapon;
// Behaviors/MoveHandlers.cpp Move_MODEL_SKILL_WHEEL1/2.
// Five native wheel copies, model-based weapon rendering, smoke,
// JOINT_SPARK trails, SPARK particles, BITMAP_LIGHT and terrain lights.
// This contains no collision, damage, input, or packet handling.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private bool MoveS6SkillWheel(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.SkillWheel1)
            {
                // MODEL_SKILL_WHEEL1 has a five-tick lifetime and creates
                // MODEL_SKILL_WHEEL2 with native subtype -1,0,1,2,3.
                // The last emitted native frame is stored per carrier.
                // Catching crossed 25-Hz ticks avoids missing weapon copies
                // when the MonoGame frame rate drops below the native rate.
                if (Clock.AdvancedReferenceFrame)
                {
                    int index = Math.Clamp(
                        5 - (int)MathF.Ceiling(e.LifeTime), 0, 4);
                    for (int tick = e.LastChildNativeTick + 1;
                         tick <= index; tick++)
                    {
                        CreateEffect(ClassicFxEffectType.SkillWheel2,
                            e.Position, e.Angle, e.Light, e.Owner,
                            subType: tick - 1);
                    }
                    e.LastChildNativeTick = index;
                }
                return true;
            }

            if (e.Owner.WorldObject is not PlayerObject fighter ||
                !TryGetOwnerSnapshot(e.Owner,
                    out ClassicFxOwnerSnapshot caster) ||
                e.ModelView == null)
                return false;

            // Native rotation always uses the weapon captured at cast time;
            // the equipped-model echo carries that BMD and its materials.
            bool spear = fighter.Weapon1?.Model != null
                ? fighter.Weapon1.ItemGroup == 3
                : fighter.Weapon2?.ItemGroup == 3;
            float rearOffset = spear ? -180f : -150f;
            Vector3 offset = Vector3.TransformNormal(
                new Vector3(0f, rearOffset, 0f),
                Matrix.CreateFromYawPitchRoll(
                    e.Angle.Y, e.Angle.X, e.Angle.Z));
            e.Position = caster.Position + offset;
            e.Angle.Z -= MathHelper.ToRadians(18f) * f;
            e.Alpha = e.SubType switch
            {
                1 => 0.6f,
                2 => 0.5f,
                3 => 0.4f,
                4 => 0.3f,
                _ => 1f
            };

            // RenderWheelWeapon updates Direction.Z, then adds it to the
            // rendered weapon angle. Acceleration must accumulate across
            // frames; using only Direction.Z skips nearly all its spin.
            e.Direction.Z -= MathHelper.ToRadians(30f) * f;
            e.HeadAngle.Z += e.Direction.Z;
            e.ModelView.Position = e.Position +
                new Vector3(0f, 0f, 100f * f);
            e.ModelView.Angle = new Vector3(
                e.Angle.X, MathHelper.PiOver2,
                e.Angle.Z + e.HeadAngle.Z);
            e.ModelView.Light = RequestTerrainLight(e.Position.X,
                e.Position.Y) + e.Light;
            e.ModelView.ApplyNativeRenderState(
                e.Scale, e.Alpha, e.BlendMeshLight, e.Light);

            // Native MoveEffect() adds a single warm BITMAP_LIGHT sprite
            // EVERY update. At 60 FPS this must not be gated by the 25-Hz
            // particle/joint reference tick or the visible light will flicker.
            // Native InHellas(): Kalima 1-6 (24..29), plus Kalima 7
            // (special index 36). Its water branch uses the light sprite
            // sixty units above the wheel, not twenty.
            bool kalima = World != null &&
                (World.MapId is >= 24 and <= 30 or 36);
            CreateSprite(ClassicTextureIds.BitmapLight,
                e.Position + new Vector3(0f, 0f, kalima ? 60f : 20f),
                2f, new Vector3(1f, 0.8f, 0.6f));

            // Smoke, sparks, joints and terrain light share the 25-Hz
            // source clock; we do not spawn four uncontrolled bursts at
            // every display refresh on mobile devices.
            if (!Clock.AdvancedReferenceFrame)
                return true;

            float luminosity = (Random.Modulo(4) + 7) * 0.1f;
            if (e.LifeTime < 5f)
                luminosity = MathF.Max(0f,
                    luminosity - (5f - e.LifeTime) * 0.2f);

            CreateParticle(ClassicTextureIds.BitmapSmoke,
                e.Position, e.Angle, e.Light, subType: 3);
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(luminosity * 0.3f), 3f);

            if (kalima)
            {
                // Main's water-map branch replaces the four JOINT_SPARK
                // chains with exactly one WATERFALL_5 particle subtype 3.
                // No invented joints are rendered above Kalima water.
                Vector3 spray = new Vector3(
                    MathHelper.ToDegrees(e.Angle.X) +
                        Random.Modulo(60) - 30f,
                    MathHelper.ToDegrees(e.Angle.Y),
                    MathHelper.ToDegrees(e.Angle.Z) +
                        Random.Modulo(30) + 90f);
                Vector3 waterPosition = e.Position + new Vector3(
                    Random.Modulo(20) - 10f,
                    Random.Modulo(20) - 10f, 120f);
                CreateParticle(ClassicTextureIds.BitmapWaterfall5,
                    waterPosition, spray, new Vector3(0.5f),
                    subType: 3);
                return true;
            }

            for (int j = 0; j < 4; j++)
            {
                // MuMain joint angles are degrees. EffectState angles
                // are MonoGame radians. Do not mix the two systems.
                Vector3 spin = new Vector3(
                    MathHelper.ToDegrees(e.Angle.X) +
                        Random.Modulo(60) - 30f,
                    MathHelper.ToDegrees(e.Angle.Y),
                    MathHelper.ToDegrees(e.Angle.Z) +
                        Random.Modulo(30) + 90f);
                Vector3 pos = e.Position + new Vector3(
                    Random.Modulo(20) - 10f,
                    Random.Modulo(20) - 10f, 0f);
                CreateJoint(ClassicTextureIds.BitmapJointSpark,
                    pos, pos, spin);
                if (Random.FpsCheck(4, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSpark,
                        pos, spin, Vector3.One);
            }
            return true;
        }
    }
}
