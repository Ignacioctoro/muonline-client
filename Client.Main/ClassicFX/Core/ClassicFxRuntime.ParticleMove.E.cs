using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles():
        // BITMAP_SMOKE + 2 ... BITMAP_LIGHTNING_MEGA3 (5892-6091).
        // Port por Type/SubType, independiente de los skills.
        private bool MoveParticleE(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapSmoke + 2:
                    MoveClassicSmokePlusTwo(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapSmoke + 3:
                    MoveClassicSmokePlusThree(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapSmokeLine1:
                case ClassicTextureIds.BitmapSmokeLine2:
                case ClassicTextureIds.BitmapSmokeLine3:
                    MoveClassicSmokeLine(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapLightningMega1:
                case ClassicTextureIds.BitmapLightningMega2:
                case ClassicTextureIds.BitmapLightningMega3:
                    if (p.SubType == 0)
                    {
                        p.Alpha -= 0.15f * ff;
                        if (p.Alpha < 0.1f)
                            keepAlive = false;
                        p.Light = p.TurningForce * p.Alpha;
                    }
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicSmokePlusTwo(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float luminosity = 1f;
            if (p.LifeTime < 10f)
                luminosity -= (10f - p.LifeTime) * 0.1f;

            p.Light = new Vector3(luminosity);

            // Main: AngleMatrix / VectorRotate(StartPosition) / Target->Position.
            // La posición inicial funciona como offset que rota con Angle.
            Vector3 rotatedOffset = Rotate(p.StartPosition, p.Angle);
            if (TryGetOwnerPosition(p.Target, out Vector3 targetPosition))
                p.Position = targetPosition + rotatedOffset;
            else
                keepAlive = false;

            p.Angle.Y += 5f * ff;
            p.Scale -= 0.01f * ff;
        }

        private void MoveClassicSmokePlusThree(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float luminosity;
            switch (p.SubType)
            {
                case 0:
                    if (p.TurningForce.X > 0.1f)
                        p.TurningForce.X -= 0.015f * ff;
                    // Se conserva el original: comprueba Y, pero reduce X.
                    if (p.TurningForce.Y > 0.1f)
                        p.TurningForce.X -= 0.005f * ff;
                    luminosity = p.LifeTime / 55f;
                    p.Light = p.TurningForce * luminosity;
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.03f * ff;
                    p.Rotation += p.Angle.X * ff;
                    return;

                case 1:
                    luminosity = p.LifeTime / 55f;
                    p.Light = p.TurningForce * luminosity;
                    p.Gravity += 0.1f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.03f * ff;
                    p.Rotation += p.Angle.X * ff;
                    return;

                case 2:
                    if (p.TurningForce.Y > 0.1f)
                        p.TurningForce.Y -= 0.005f * ff;
                    if (p.TurningForce.Z > 0.1f)
                        p.TurningForce.Z -= 0.015f * ff;
                    luminosity = p.LifeTime / 55f;
                    p.Light = p.TurningForce * luminosity;
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.03f * ff;
                    p.Rotation += p.Angle.X * ff;
                    return;

                case 3:
                case 4:
                    // Original evalúa el umbral ANTES de atenuar RGB.
                    if (p.Light.X <= 0.05f)
                        keepAlive = false;
                    p.Light *= MathF.Pow(1f / 1.012f, ff);
                    p.Scale += 0.01f * ff;
                    p.Rotation += (p.Gravity / 2f) * ff;
                    if (p.Gravity <= 0f)
                        p.Gravity = -p.Gravity;
                    p.Position.Z += p.Gravity * ff;
                    return;
            }
        }

        private void MoveClassicSmokeLine(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                case 5:
                    UpdateClassicSmokeLineAlpha(
                        ref p, ff, 0.1f, 0.7f, ref keepAlive);
                    p.Light = p.TurningForce * p.Alpha;
                    p.Scale += (R(3) + 6) * 0.01f * ff;
                    p.Position.Z += p.Gravity * ff;
                    if (p.SubType == 5)
                        FollowClassicParticleOwner(ref p, ref keepAlive);
                    return;

                case 1:
                    UpdateClassicSmokeLineAlpha(
                        ref p, ff, 0.2f, 1f, ref keepAlive);
                    p.Light = p.TurningForce * p.Alpha;
                    if (p.Scale > 0f)
                        p.Scale -= (R(10) + 10) * 0.001f * ff;
                    else
                        keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    FollowClassicParticleOwner(ref p, ref keepAlive);
                    return;

                case 2:
                case 3:
                    UpdateClassicSmokeLineAlpha(
                        ref p, ff, 0.1f, 0.7f, ref keepAlive);
                    p.Light = p.TurningForce * p.Alpha;
                    p.Scale += (R(3) + 6) * 0.01f * ff;
                    p.Position.Z += p.Gravity * ff;
                    return;

                case 4:
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 0.8f * ff;
                    p.Scale += 0.01f * ff;
                    p.Alpha -= 0.01f * ff;
                    if (p.Alpha < 0.1f)
                        keepAlive = false;
                    // Igual que powf(alpha, FPS_ANIMATION_FACTOR) original.
                    p.Light *= MathF.Pow(p.Alpha, ff);
                    return;
            }
        }

        private void UpdateClassicSmokeLineAlpha(
            ref ClassicParticle p,
            float ff,
            float fadeStep,
            float targetAlpha,
            ref bool keepAlive)
        {
            if (p.LifeTime < 10f)
                p.Alpha -= fadeStep * ff;
            else if (p.Alpha < targetAlpha)
                p.Alpha += (R(5) + 2) * 0.1f * ff;
            else
                p.Alpha = targetAlpha;

            if (p.Alpha < 0.1f)
                keepAlive = false;
        }
    }
}
