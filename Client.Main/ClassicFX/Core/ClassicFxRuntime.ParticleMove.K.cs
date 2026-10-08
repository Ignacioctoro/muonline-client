using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(), lines 8219-8477.
        // Advanced smoke, true fire/blue, hole, waterfall 1/5 and plus.
        // The original bitmap Type/SubType behavior is shared by all skills.
        private bool MoveParticleK(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapAdvSmoke:
                    MoveClassicAdvSmokeK(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapAdvSmoke + 1:
                    MoveClassicAdvSmokePlusOneK(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapTrueFire:
                case ClassicTextureIds.BitmapTrueBlue:
                    MoveClassicTrueFireK(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapHole:
                    p.Rotation += 5f * ff;
                    if (p.LifeTime < 20f)
                    {
                        p.Scale -= 0.08f * ff;
                        if (p.LifeTime < 10f)
                            p.Light *= MathF.Pow(1f / 1.3f, ff);
                    }
                    else if (p.LifeTime < 30f)
                    {
                        p.Light *= MathF.Pow(1.1f, ff);
                    }
                    return true;

                case ClassicTextureIds.BitmapWaterfall1:
                    MoveClassicWaterfallOneK(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapWaterfall5:
                    MoveClassicWaterfallFiveK(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapPlus:
                    p.Scale -= 0.01f * ff;
                    p.Position.X += (R(2) - 1) * ff;
                    p.Position.Y += (R(2) - 1) * ff;
                    p.Position.Z += 2f * ff;
                    p.Light = new Vector3(p.LifeTime / 20f);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicAdvSmokeK(ref ClassicParticle p, float ff)
        {
            p.Position += p.Velocity * ff;
            float damping = MathF.Pow(0.95f, ff);
            p.Velocity.X *= damping;
            p.Velocity.Y *= damping;
            p.Light = new Vector3(p.LifeTime / 10f);

            switch (p.SubType)
            {
                case 0:
                case 2:
                    p.Velocity.Z += 0.3f * ff;
                    p.Scale += 0.07f * ff;
                    break;
                case 3:
                    p.Velocity.Y += 0.1f * ff;
                    p.Scale += 0.05f * ff;
                    p.Alpha -= 0.2f * ff;
                    break;
                default:
                    p.Velocity.Z += 0.3f * ff;
                    p.Scale += 0.09f * ff;
                    break;
            }
        }

        private void MoveClassicAdvSmokePlusOneK(ref ClassicParticle p, float ff)
        {
            p.Position += p.Velocity * ff;
            float damping = MathF.Pow(0.95f, ff);
            p.Velocity.X *= damping;
            p.Velocity.Y *= damping;
            p.Velocity.Z += 0.6f * ff;
            p.Position.X += (R(4) - 2) * ff;
            p.Position.Y += (R(4) - 2) * ff;
            p.Position.Z += (R(4) - 2) * 0.8f * ff;
            p.Scale += 0.05f * ff;
            // The original subtype 2 uses (LifeTime / 25)*2 - 1.
            float brightness = p.LifeTime / 25f;
            if (p.SubType == 2)
                brightness = brightness * 2f - 1f;
            p.Light = new Vector3(brightness);
            p.Rotation += (1f + R(2)) * ff;
        }

        private void MoveClassicTrueFireK(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            // Original: CurrentAction == 1 shortens the flame by two
            // additional reference frames for subtypes 1 and 2.
            if ((p.SubType == 1 || p.SubType == 2) &&
                p.Target.HasOwner &&
                TryGetOwnerCurrentAction(p.Target, out int action) &&
                action == 1)
            {
                p.LifeTime -= 2f * ff;
            }

            if ((p.SubType == 3 || p.SubType == 4) && p.Target.HasOwner)
            {
                string boneName = p.SubType == 3
                    ? "Monster82_LHand" : "Monster82_RHand";

                if (TryGetOwnerNamedBonePosition(
                        p.Target, boneName, Vector3.Zero,
                        out Vector3 handPosition))
                {
                    float distance = Vector3.Distance(p.Position, handPosition);
                    p.Scale -= distance * 0.003f * ff;
                    p.Light -= new Vector3(distance * 0.08f * ff);
                }
                // Main: no mutation if the named bone does not exist.
            }

            if (p.SubType == 8)
            {
                // Main transforms bone 20 with offset (6,6,0).
                // Unlike the native uninitialised vPos path, a missing owner
                // safely ends the particle rather than generating NaNs.
                if (TryTransformOwnerBonePosition(
                        p.Target, 20, new Vector3(6f, 6f, 0f),
                        out Vector3 bonePosition))
                {
                    p.Position.X = bonePosition.X;
                    p.Position.Y = bonePosition.Y;
                }
                else
                {
                    keepAlive = false;
                }
            }

            if (p.SubType == 7)
            {
                p.Scale -= 0.1f * ff;
                p.Position.Z += 2f * ff;
            }
            else
            {
                p.Scale -= 0.02f * ff;
            }

            if (p.Scale < 0f)
                p.Scale = 0f;

            float damping = MathF.Pow(0.95f, ff);
            p.Velocity.X *= damping;
            p.Velocity.Y *= damping;

            // The original checks `o->Type == 6` inside a switch whose only
            // types are BITMAP_TRUE_FIRE and BITMAP_TRUE_BLUE: unreachable.
            p.Position.Z += 1f * ff;
            p.Light = new Vector3(p.LifeTime / 25f);
        }

        private void MoveClassicWaterfallOneK(ref ClassicParticle p, float ff)
        {
            p.Scale -= 0.005f * ff;
            if (p.LifeTime < 5f)
                p.Light *= MathF.Pow(1f / 1.2f, ff);
            else if (p.LifeTime > 20f)
                p.Light *= MathF.Pow(1.1f, ff);

            p.Velocity.Z += 0.1f * ff;
            if (p.SubType == 1)
            {
                // All three channels get capped together in the source.
                if (p.Light.X > 0.5f)
                    p.Light = new Vector3(0.5f);
                p.Rotation += 1f; // Main ++ without FPS factor.
                p.Scale += 0.01f * ff;
                p.Velocity.Z -= 0.4f * ff;
            }
        }

        private void MoveClassicWaterfallFiveK(ref ClassicParticle p, float ff)
        {
            switch (p.SubType)
            {
                case 0:
                case 5:
                case 9:
                    p.Scale -= 0.005f * ff;
                    p.Velocity.Z += 0.1f * ff;
                    break;

                case 1:
                    p.Scale += 0.1f * ff;
                    p.Position.X += (R(10) - 5f) * ff;
                    p.Position.Y += (R(10) - 5f) * ff;
                    break;

                case 2:
                    if (p.Scale < 1f)
                        p.Scale += 0.1f * ff;
                    p.Position.X += (R(10) - 5f) * ff;
                    p.Position.Y += (R(10) - 5f) * ff;
                    p.Velocity.Z -= 1f * ff;
                    break;

                case 3:
                    p.Scale -= 0.005f * ff;
                    p.Rotation += 4f * ff;
                    p.Velocity.Z += 0.1f * ff;
                    break;

                case 4:
                    // Angle.Z is passed directly to cos/sin (radians in Main).
                    p.Position.X += MathF.Cos(p.Angle.Z) * 20f * ff;
                    p.Position.Y += MathF.Sin(p.Angle.Z) * 20f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += ff;
                    break;

                case 7:
                    p.Scale -= 0.005f * ff;
                    p.Rotation += ff;
                    p.Position += p.Velocity * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    break;

                case 8:
                    p.Scale += 0.05f * ff;
                    p.Velocity.Z -= 0.6f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return; // C++ break inside this branch skips tail fade.
            }

            if (p.LifeTime < 8f)
                p.Light *= MathF.Pow(1f / 1.2f, ff);
            else if (p.LifeTime > 20f)
                p.Light *= MathF.Pow(1.1f, ff);
        }
    }
}
