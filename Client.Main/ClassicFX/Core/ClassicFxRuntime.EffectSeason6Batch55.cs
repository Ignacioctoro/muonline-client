// ClassicFX Season 6 Batch 55 — native Twisting Slash wheel carrier.
// Main ZzzEffect.cpp CreateEffect(MODEL_SKILL_WHEEL1/2),
// Behaviors/MoveHandlers.cpp Move_MODEL_SKILL_WHEEL1/2,
// RenderWheelWeapon() — equipped BMD, not a fabricated slash texture.
// Model views are owned by the shared Effect pool. No gameplay hit changes.
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
                // Five 25-Hz native pulses, 4-LifeTime is the literal
                // original subtype passed to MODEL_SKILL_WHEEL2.
                if (Clock.AdvancedReferenceFrame)
                    CreateEffect(ClassicFxEffectType.SkillWheel2,
                        e.Position, e.Angle, e.Light, e.Owner,
                        subType: 4 - (int)System.MathF.Ceiling(e.LifeTime));
                return true;
            }

            if (e.Owner.WorldObject is not PlayerObject fighter ||
                !TryGetOwnerSnapshot(e.Owner,
                    out ClassicFxOwnerSnapshot caster))
                return false;

            // MODEL_SKILL_WHEEL2 moves 180 behind a spear and 150
            // behind other equipped weapon models.
            bool spear = fighter.Weapon1?.Model != null
                ? fighter.Weapon1.ItemGroup == 3
                : fighter.Weapon2?.ItemGroup == 3;
            float distance = spear ? -180f : -150f;
            Vector3 offset = Vector3.TransformNormal(
                new Vector3(0f, distance, 0f),
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

            // RenderWheelWeapon(): temporary weapon pitch of 90 degrees
            // and native -30-degree accelerating spin. No owner mutation.
            e.Direction.Z -= MathHelper.ToRadians(30f) * f;
            if (e.ModelView != null)
            {
                e.ModelView.Position = e.Position +
                    new Vector3(0f, 0f, 100f * f);
                e.ModelView.Angle = new Vector3(
                    e.Angle.X, MathHelper.PiOver2,
                    e.Angle.Z + e.Direction.Z);
                e.ModelView.ApplyNativeRenderState(
                    e.Scale, e.Alpha, e.BlendMeshLight, e.Light);
            }

            if (!Clock.AdvancedReferenceFrame)
                return true;

            // Native wheel smoke, joint sparks, occasional spark
            // particles, ground light, and one orange tip light.
            CreateParticle(ClassicTextureIds.BitmapSmoke,
                e.Position, e.Angle, e.Light, subType: 3);
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(0.3f), 3f);
            for (int j = 0; j < 4; j++)
            {
                Vector3 angle = new Vector3(
                    MathHelper.ToDegrees(e.Angle.X) +
                        Random.Modulo(60) - 30f,
                    MathHelper.ToDegrees(e.Angle.Y),
                    MathHelper.ToDegrees(e.Angle.Z) +
                        Random.Modulo(30) + 90f);
                Vector3 position = e.Position + new Vector3(
                    Random.Modulo(20) - 10f,
                    Random.Modulo(20) - 10f, 0f);
                CreateJointFpsChecked(
                    ClassicTextureIds.BitmapJointSpark,
                    position, position, angle);
                if (Random.FpsCheck(4, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSpark,
                        position, angle, Vector3.One);
            }

            CreateSprite(ClassicTextureIds.BitmapLight,
                e.Position + new Vector3(0f, 0f, 20f), 2f,
                new Vector3(1f, 0.8f, 0.6f));
            return true;
        }
    }
}
