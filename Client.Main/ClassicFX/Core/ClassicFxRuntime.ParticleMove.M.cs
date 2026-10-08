using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(), lines 8722-8893.
        // Last Type cases in the original movement switch.
        // eBuff_AG_Addition is 113 in MuMain's Core/Globals/_enum.h.
        private const byte ClassicAgAdditionBuffId = 113;

        private bool MoveParticleM(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapCursedTempleEffectMasker:
                    MoveClassicCursedTempleMaskerM(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapRaklionClouds:
                    MoveClassicRaklionCloudsM(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapChrome2:
                    MoveClassicChromeTwoM(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapAgAdditionEffect:
                    MoveClassicAgAdditionM(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapSbumb:
                    // Main: float LifeTime is assigned to int Frame (truncated).
                    p.Frame = (int)(4f - p.LifeTime);
                    return true;

                case ClassicTextureIds.BitmapDamage1:
                    p.Scale *= MathF.Pow(1.2f, ff);
                    p.Light *= 0.8f; // No FPS factor in original VectorScale.
                    return true;

                case ClassicTextureIds.BitmapSwordEffectMono:
                    MoveClassicSwordEffectMonoM(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapDamage2:
                    p.Scale *= MathF.Pow(1.2f, ff);
                    p.Light *= 0.55f; // No FPS factor in original VectorScale.
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicCursedTempleMaskerM(
            ref ClassicParticle p,
            float ff)
        {
            p.Light *= MathF.Pow(1f / 1.1f, ff);
            p.Scale += 0.02f * ff;

            // Original: rand_fps_check(2), CreateParticle subtype 1 at StartPosition.
            if (p.SubType == 0 && Random.FpsCheck(2, Clock))
            {
                CreateParticle(
                    ClassicTextureIds.BitmapCursedTempleEffectMasker,
                    p.StartPosition,
                    p.Angle,
                    new Vector3(0.8f, 0.3f, 0.3f),
                    subType: 1,
                    scale: 1.3f);
            }
        }

        private void MoveClassicRaklionCloudsM(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            // Important: original position, scale and RGB changes do NOT
            // multiply by FPS_ANIMATION_FACTOR (unlike most move cases).
            Vector3 direction = Rotate(new Vector3(0f, -1f, 0f), p.Angle);
            p.Position += direction * 13f;
            p.Scale += 0.065f;
            p.Light /= 1.6f;

            if (p.Light.X + p.Light.Y + p.Light.Z <= 0.01f)
                keepAlive = false;

            // Native code can still emit the child on its last frame.
            if (Random.FpsCheck(10, Clock))
            {
                float range = 70f * p.Scale;
                Vector3 randomOffset = new Vector3(
                    (R(2000) - 1000) * 0.001f * range,
                    (R(2000) - 1000) * 0.001f * range,
                    (R(2000) - 1000) * 0.001f * range);

                CreateParticle(
                    ClassicTextureIds.BitmapShiny + 6,
                    p.Position + randomOffset,
                    p.Angle,
                    p.Light,
                    subType: 0,
                    scale: 0.5f);
            }
        }

        private void MoveClassicChromeTwoM(ref ClassicParticle p, float ff)
        {
            if (p.Scale > 0f)
                p.Scale -= 0.1f * ff;
            else
                p.Scale = 0f;

            p.Rotation += ff;
            p.Alpha -= 0.05f * ff;

            // Original only updates the position when Target != NULL.
            // StartPosition is used as an offset, not as an absolute origin.
            if (TryGetOwnerPosition(p.Target, out Vector3 ownerPosition))
                p.Position = ownerPosition - p.StartPosition;
        }

        private void MoveClassicAgAdditionM(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.LifeTime < 10f)
                p.Alpha -= 0.1f * ff;
            else
                p.Alpha = 0.7f;

            if (p.Alpha < 0.1f)
                keepAlive = false;

            p.Light = p.TurningForce * p.Alpha;

            // Main: TransformByObjectBone(..., bone 18).
            if (!TryGetOwnerBonePosition(p.Target, 18, out Vector3 bonePosition))
            {
                // No valid model/bone: don't fabricate an attachment.
                keepAlive = false;
                return;
            }

            switch (p.SubType)
            {
                case 0:
                    p.Scale *= MathF.Pow(1.05f, ff);
                    bonePosition.Z -= 10f * ff;
                    break;
                case 1:
                    p.Scale *= MathF.Pow(1.02f, ff);
                    bonePosition.Z += 10f * ff;
                    break;
                case 2:
                    p.Scale *= MathF.Pow(1.03f, ff);
                    bonePosition.Z += 25f * ff;
                    break;
            }

            p.Position = bonePosition;

            // Main: when the current particle dies, maintain the aura while
            // buff 113 (eBuff_AG_Addition) remains active on the target.
            // CreateParticleFpsChecked supplies the original FPS gate.
            if (!keepAlive && IsOwnerBuffActive(p.Target, ClassicAgAdditionBuffId))
            {
                if (p.SubType >= 0 && p.SubType <= 2)
                {
                    CreateParticleFpsChecked(
                        ClassicTextureIds.BitmapAgAdditionEffect,
                        bonePosition,
                        p.Angle,
                        p.Light,
                        p.SubType,
                        1f,
                        p.Target);
                }
            }
        }

        private static void MoveClassicSwordEffectMonoM(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.SubType == 1)
                p.Scale *= MathF.Pow(0.8f, ff);
            else if (p.SubType == 0)
                p.Scale *= MathF.Pow(1.2f, ff);

            if (p.Scale > 6f)
            {
                p.SubType = 1;
                return; // Main breaks before dimming RGB.
            }
            if (p.SubType == 1 && p.Scale < 1f)
            {
                keepAlive = false;
                return; // Main breaks before dimming RGB.
            }

            p.Light *= 0.94f; // Unscaled VectorScale in the original.
        }
    }
}
