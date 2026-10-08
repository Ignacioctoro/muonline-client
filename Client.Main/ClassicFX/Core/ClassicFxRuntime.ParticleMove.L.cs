using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(), lines 8478-8721.
        // WATERFALL_2, WATERFALL_3/4 and SHOCK_WAVE.
        // Keep source Type/SubType semantics, including non-FPS-adjusted terms
        // and the common tail that only certain waterfall subtypes execute.
        private bool MoveParticleL(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapWaterfall2:
                    MoveClassicWaterfallTwoL(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapWaterfall3:
                case ClassicTextureIds.BitmapWaterfall4:
                    MoveClassicWaterfallThreeFourL(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapShockWave:
                    MoveClassicShockWaveL(ref p, ff, ref keepAlive);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicWaterfallTwoL(ref ClassicParticle p, float ff)
        {
            if (p.SubType == 5)
            {
                p.Scale += 0.03f * ff;
                p.Velocity.Z += 0.1f * ff;
                p.Position.X += p.Velocity.X * ff;
                p.Position.Y += p.Velocity.Y * ff;

                // Main adds Velocity.Z directly (without FPS factor).
                p.Position.Z += p.Gravity * ff + p.Velocity.Z;
                p.Light *= MathF.Pow(1f / 1.1f, ff);
                return;
            }

            p.Scale += 0.03f * ff;
            p.Velocity.X = (R(20) - 10) * 0.1f;
            p.Velocity.Y = (R(20) - 10) * 0.1f;
            p.Velocity.Z += 0.1f * ff;

            if (p.LifeTime < 10f)
                p.Light *= MathF.Pow(1f / 1.1f, ff);

            if (p.SubType == 1)
                p.Light *= MathF.Pow(1f / 1.05f, ff);

            if (p.SubType == 3)
            {
                p.Scale -= 0.038f * ff;
                p.Velocity.Z -= 0.02f * ff;
                p.Light *= MathF.Pow(1f / 1.02f, ff);
            }

            if (p.SubType == 4)
                p.Rotation -= 1.1f * ff;

            if (p.SubType == 6)
            {
                p.Gravity += (R(5) + 1.5f * (p.LifeTime / 5f)) * ff;
                p.Velocity.X += 1f * ff;
                p.Velocity.X -= (100f * (20f / p.LifeTime)) * ff;
                p.Rotation += 50f * ff;

                // Main calculates a rotated Position here but immediately
                // overwrites it with StartPosition before anything reads it.
                // Preserve the final native result without wasting work per frame.
                p.Scale += (1f / p.LifeTime) * 2f * ff;
                p.Light.X *= MathF.Pow(0.8f, ff);
                p.Light.Y *= MathF.Pow(0.77f, ff);
                p.Light.Z *= MathF.Pow(0.77f, ff);

                p.Position = p.StartPosition;
                p.StartPosition.Z += R(5) + 10f;
                p.Position.Z = p.StartPosition.Z + p.Gravity;
            }
        }

        private void MoveClassicWaterfallThreeFourL(ref ClassicParticle p, float ff)
        {
            switch (p.SubType)
            {
                case 2:
                    // Main passes Angle.Z directly to cos/sin (radians).
                    p.Position.X += MathF.Cos(p.Angle.Z) * 20f * ff;
                    p.Position.Y += MathF.Sin(p.Angle.Z) * 20f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 3:
                    p.Scale += 0.005f * ff;
                    p.Velocity.Z -= 0.05f * ff;
                    p.Light *= MathF.Pow(0.97f, ff);
                    return;

                case 5:
                    p.Position.Z -= p.Gravity * ff;
                    p.Gravity -= 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 6:
                    p.Position.Z += p.Gravity * ff;
                    p.Gravity += 0.05f * ff;
                    p.Alpha -= 0.01f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 7:
                    p.Velocity.Z += 0.01f * ff;
                    p.Light *= MathF.Pow(0.97f, ff);
                    return;

                case 8:
                case 14:
                    p.Scale += 0.05f * ff;
                    p.Velocity.Z -= 0.6f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    if (p.SubType == 8)
                        return; // Main's branch 8 has a break; 14 falls through.
                    break;

                case 10:
                    p.Position.X += p.Velocity.X * ff;
                    p.Position.Y += p.Velocity.Y * ff;
                    p.Position.Z -= p.Gravity * ff;
                    p.Gravity -= 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 11:
                    p.Position += p.Velocity * ff;
                    p.Scale += 0.01f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 12:
                    p.Rotation += (((int)Clock.WorldTimeMilliseconds % 360) * 0.01f) * ff;
                    p.Scale *= MathF.Pow(0.92f, ff);
                    p.Light *= MathF.Pow(0.92f, ff);
                    p.Alpha *= MathF.Pow(0.8f, ff);
                    return;

                case 13:
                    p.Position.Z -= p.Gravity * ff;
                    p.Scale += 0.001f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 15:
                    p.Position.Z -= p.Gravity * ff;
                    p.Gravity -= 0.05f * ff;
                    p.Scale -= 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    break; // No break in Main: also execute shared tail.

                case 16:
                    // ASG_ADD_MAP_KARUTAN branch from original Main.
                    p.Scale -= 0.005f * ff;
                    p.Velocity.Z -= 0.1f * ff;
                    if (p.LifeTime < 8f)
                        p.Light *= MathF.Pow(1f / 1.2f, ff);
                    else if (p.LifeTime > 20f)
                        p.Light *= MathF.Pow(1.1f, ff);
                    return;
            }

            // Main executes this tail for unhandled subtypes and 14/15.
            // It does NOT run for subtype 8, despite the shared code above.
            p.Scale += 0.005f * ff;
            p.Velocity.Z -= 2.5f * ff;
            p.Light *= MathF.Pow(1f / 1.1f, ff);
        }

        private static void MoveClassicShockWaveL(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.SubType == 3)
            {
                p.Light *= MathF.Pow(1f / 1.8f, ff);
                p.Scale += 0.3f * ff;
            }
            else if (p.SubType == 0)
            {
                p.Light *= MathF.Pow(1f / 1.5f, ff);
                p.Scale += 0.8f * ff;
            }

            if (p.SubType == 4)
            {
                p.Alpha -= 0.001f * ff;
                if (p.Alpha <= 0f)
                    keepAlive = false;
                p.Light.X *= MathF.Pow(1f / 1.01f, ff);
                p.Light.Y *= MathF.Pow(1f / 1.03f, ff);
                p.Light.Z *= MathF.Pow(1f / 1.03f, ff);
                p.Scale += p.Gravity * 0.015f * ff;
                p.Gravity += 2.4f * ff;
            }
        }
    }
}
