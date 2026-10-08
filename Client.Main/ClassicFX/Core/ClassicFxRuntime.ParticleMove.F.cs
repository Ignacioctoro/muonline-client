using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(): lines 6092-6557.
        // FIRE_HIK1/MONO, FIRE_HIK3/MONO, LIGHT+1, BLOOD/BLOOD+1.
        // Original Type/SubType behavior; no skill-specific rendering.
        private bool MoveParticleF(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapFireHik1:
                case ClassicTextureIds.BitmapFireHik1Mono:
                    MoveClassicFireHik1(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapFireHik3:
                case ClassicTextureIds.BitmapFireHik3Mono:
                    MoveClassicFireHik3(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapLight + 1:
                    MoveClassicLightPlusOne(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapBlood:
                case ClassicTextureIds.BitmapBlood + 1:
                    p.Frame = (int)((12f - p.LifeTime) / 3f);
                    // Main VectorScale(..., 0.95f): intentionally not FPS-adjusted.
                    p.Velocity *= 0.95f;
                    p.Position += p.Velocity * ff;
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicFireHik1(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                case 6:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 5, false, ref keepAlive);
                    if (p.SubType == 6)
                    {
                        p.Rotation = 0f;
                        p.Position.Z += p.Gravity * 1.2f * ff;
                    }
                    return;

                case 1:
                    UpdateClassicHikAlpha(ref p, ff, 20f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 2, 4, false, ref keepAlive);
                    return;

                case 2:
                    UpdateClassicHikAlpha(ref p, ff, 10f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 7, true, ref keepAlive);
                    return;

                case 10:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 5, false, ref keepAlive);
                    return;

                case 3:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 2, false, ref keepAlive);
                    return;

                case 4:
                    UpdateClassicHikAlpha(ref p, ff, 5f, 0.1f, ref keepAlive);
                    MoveClassicHikAttached(ref p, ff, ref keepAlive);
                    return;

                case 5:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 5, false, ref keepAlive);
                    return;
            }
        }

        private void MoveClassicFireHik3(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                case 6:
                    UpdateClassicHikAlpha(ref p, ff, 10f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 7, false, ref keepAlive);
                    if (p.SubType == 6)
                    {
                        p.Rotation = 0f;
                        p.Position.Z += p.Gravity * 1.2f * ff;
                    }
                    return;

                case 1:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 2, 6, false, ref keepAlive);
                    return;

                case 2:
                    UpdateClassicHikAlpha(ref p, ff, 10f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 7, true, ref keepAlive);
                    return;

                case 3:
                    UpdateClassicHikAlpha(ref p, ff, 10f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 3, 7, false, ref keepAlive);
                    return;

                case 4:
                    UpdateClassicHikAlpha(ref p, ff, 15f, 0.2f, ref keepAlive);
                    MoveClassicHikShrinking(ref p, ff, 4, 5, false, ref keepAlive);
                    return;

                case 5:
                    UpdateClassicHikAlpha(ref p, ff, 4f, 0.1f, ref keepAlive);
                    MoveClassicHikAttached(ref p, ff, ref keepAlive);
                    return;
            }
        }

        // Original: fade-in while LifeTime >= threshold and Alpha < 1;
        // otherwise fades out below threshold. RGB uses TurningForce * Alpha.
        private void UpdateClassicHikAlpha(
            ref ClassicParticle p,
            float ff,
            float fadeStart,
            float fadeStep,
            ref bool keepAlive)
        {
            if (p.LifeTime < fadeStart)
                p.Alpha -= fadeStep * ff;
            else if (p.Alpha < 1f)
                p.Alpha += (R(2) + 2) * 0.1f * ff;
            else
                p.Alpha = 1f;

            if (p.Alpha < 0.1f)
                keepAlive = false;
            p.Light = p.TurningForce * p.Alpha;
        }

        // Used by the shrinking subtypes of both classic HIK families.
        // Random and position modifications match the source call order.
        private void MoveClassicHikShrinking(
            ref ClassicParticle p,
            float ff,
            int randomRange,
            int randomBase,
            bool moveXY,
            ref bool keepAlive)
        {
            if (p.Scale > 0f)
                p.Scale -= (R(randomRange) + randomBase) * 0.01f * ff;
            else
                keepAlive = false;

            if (moveXY)
            {
                p.Position.X += p.Velocity.X * ff;
                p.Position.Y += p.Velocity.Y * ff;
            }

            p.Position.Z += p.Gravity * ff;
            p.Rotation += 3f * ff;
        }

        // HIK1 subtype 4 / HIK3 subtype 5.
        // Retains the original double Target->Position addition exactly:
        //   Position = Position - StartPosition + TargetPosition;
        //   StartPosition = TargetPosition;
        //   Position = Position + TargetPosition;
        // This is not the standard FollowClassicParticleOwner behavior.
        private void MoveClassicHikAttached(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            p.Scale += (R(3) + 8) * 0.01f * ff;
            if (p.LifeTime < 3f)
                p.Scale += (10f - p.LifeTime) * 0.02f * ff;

            // Main rotates (0, -1, 0) by Angle and moves by 8 per update;
            // intentionally not multiplied by FPS_ANIMATION_FACTOR.
            p.Position += Rotate(new Vector3(0f, -1f, 0f), p.Angle) * 8f;

            if (TryGetOwnerPosition(p.Target, out Vector3 targetPosition))
            {
                p.Position = (p.Position - p.StartPosition) + targetPosition;
                p.StartPosition = targetPosition;
                p.Position += targetPosition;
            }
            else
            {
                // Main dereferences a live Target; no stale owner references.
                keepAlive = false;
            }

            p.Rotation += 5f * ff;
        }

        private void MoveClassicLightPlusOne(
            ref ClassicParticle p,
            float ff)
        {
            switch (p.SubType)
            {
                case 2:
                    p.Scale *= MathF.Pow(0.92f, ff);
                    return;
                case 3:
                    p.Scale *= MathF.Pow(1.3f, ff);
                    p.Light *= MathF.Pow(0.9f, ff);
                    return;
                case 5:
                    // Main deliberately refreshes LifeTime to 5 every update.
                    p.LifeTime = 5f;
                    p.Light *= MathF.Pow(0.7f, ff);
                    return;
                case 4:
                    return;
                default:
                    p.Position.Z -= p.Gravity * ff;
                    p.Position.Y -= 1f * ff;
                    p.Gravity += 1f * ff;
                    p.Scale *= MathF.Pow(0.95f, ff);
                    return;
            }
        }
    }
}
