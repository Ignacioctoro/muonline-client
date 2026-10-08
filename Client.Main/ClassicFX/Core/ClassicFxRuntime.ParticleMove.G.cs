using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(), lines 6558-7055.
        // Complete BITMAP_SPARK family: SPARK, SPARK+1, SPARK+2.
        // Parameters are kept in original MU units (and use classic ff).
        private bool MoveParticleG(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapSpark:
                    MoveClassicSpark(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapSpark + 1:
                    MoveClassicSparkPlusOne(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapSpark + 2:
                    MoveClassicSparkPlusTwo(ref p, ff, ref keepAlive);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicSpark(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float luminosity = p.LifeTime / 16f;
            if (p.LifeTime < 0f)
                keepAlive = false;

            if (p.SubType == 11)
                p.Light = new Vector3(luminosity, luminosity * 0.3f, luminosity * 0.3f);
            else if (p.SubType != 8)
                p.Light = new Vector3(luminosity);

            p.Position.Z += p.Gravity * ff;
            if (p.SubType == 5)
                p.Gravity -= 0.3f * ff;
            else if (p.SubType == 6)
            {
                p.Gravity -= 0.3f * ff;
                p.Light = new Vector3(0.9f);
            }
            else if (p.SubType == 8 || p.SubType == 10)
                p.Light *= MathF.Pow(1f / 1.02f, ff);

            if (p.SubType == 7)
                p.Gravity -= 0.6f * ff;
            else if (p.SubType == 9)
                p.Gravity -= 0.4f * ff;
            else
                p.Gravity -= 2f * ff;

            float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
            if (p.Position.Z < height)
            {
                p.Position.Z = height;
                p.Gravity = -p.Gravity * 0.6f;
                p.LifeTime -= 4f * ff;
            }

            p.Position += p.Velocity * ff;
            if (p.SubType == 5 || p.SubType == 6)
                FollowClassicParticleOwner(ref p, ref keepAlive);
        }

        private void MoveClassicSparkPlusOne(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                case 1:
                case 8:
                    p.Scale -= (p.SubType == 1 ? 2f : 0.5f) * ff;
                    if (p.Scale < 0.2f)
                        keepAlive = false;
                    return;

                case 2:
                case 4:
                {
                    if (p.LifeTime <= 0f)
                        keepAlive = false;
                    if (!TryGetOwnerPosition(p.Target, out Vector3 ownerPosition) &&
                        p.LifeTime > 5f)
                        p.LifeTime = 5f;

                    p.Light = new Vector3(p.LifeTime / 5f);
                    p.Scale += (p.SubType == 2 ? 0.05f : 0.08f) * ff;
                    p.Position = p.StartPosition + p.Velocity * ff;
                    p.StartPosition = p.Position;

                    if (p.SubType == 4 &&
                        TryGetClassicSparkTargetStartPosition(p.Target, out Vector3 targetStart))
                    {
                        // Main: Target.Position - Target.StartPosition, then * ff.
                        p.Position += (ownerPosition - targetStart) * ff;
                    }
                    // A WorldObject has no original OBJECT::StartPosition yet;
                    // do not synthesize an offset or invent model movement.
                    return;
                }

                case 3:
                    p.Scale *= MathF.Pow(0.8f, ff);
                    return;

                case 5:
                    p.Scale *= MathF.Pow(0.9f, ff);
                    p.Light *= MathF.Pow(1f / 1.03f, ff);
                    return;

                case 6:
                    p.Position.Z -= p.Gravity * ff;
                    p.Position.Y -= ff;
                    p.Gravity += 0.1f * ff;
                    p.Light.Y = R(10) / 100f + 0.7f;
                    return;

                case 7:
                    p.Scale -= 0.02f * ff;
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    p.Position.Z -= p.Gravity * ff;
                    p.Light = new Vector3(0f, R(50) / 100f + 0.2f, 0f);
                    return;

                case 9:
                    p.Velocity.X -= 1.2f * ff;
                    p.Velocity.Z -= ff;
                    p.Scale -= 0.08f * ff;
                    if (p.LifeTime <= 0f || p.Scale < 0.2f)
                        keepAlive = false;
                    return;

                case 10:
                    p.Light *= MathF.Pow(1f / 1.08f, ff);
                    p.Scale -= 0.03f * ff;
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;

                case 11:
                case 13:
                case 14:
                case 15:
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    if (p.SubType == 11 || p.SubType == 13)
                        p.Scale -= 0.04f * ff;
                    else if (p.SubType == 14)
                        p.Scale -= 0.01f * ff;
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;

                case 12:
                {
                    float light = R(2);
                    p.Light = new Vector3(light - 0.6f, light - 0.6f, light - 0.8f);
                    p.Scale -= 0.04f * ff;
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;
                }

                case 16:
                case 18:
                {
                    float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
                    if (height + 2f >= p.Position.Z)
                        p.Frame = 88;

                    if (p.Frame == 77)
                    {
                        p.Gravity *= MathF.Pow(1.03f, ff);
                        p.Position.Z -= p.Gravity * ff;
                    }
                    else if (p.Frame == 88)
                    {
                        p.Gravity *= MathF.Pow(1f / 1.2f, ff);
                        if (p.Gravity <= 0.1f)
                        {
                            p.Gravity = 0.1f;
                            p.Frame = 99;
                        }
                        p.Position.Z += p.Gravity * ff;
                    }
                    else if (p.Frame == 99)
                    {
                        p.Gravity *= MathF.Pow(1.2f, ff);
                        p.Position.Z -= p.Gravity * ff;
                    }
                    p.Alpha -= 0.007f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;
                }

                case 17:
                    if (p.Position.Z >= p.StartPosition.Z + 350f)
                        p.Light *= MathF.Pow(1f / 1.05f, ff);
                    if (p.Light.X <= 0.05f) keepAlive = false;
                    p.Position.X += MathF.Sin((float)Clock.WorldTimeMilliseconds * p.Rotation) *
                        p.Gravity * 0.3f * ff;
                    p.Position.Z += p.Gravity * ff;
                    return;

                case 19:
                    if (p.LifeTime <= 0f) keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    p.Gravity += 0.1f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    return;

                case 20:
                case 22:
                case 28:
                case 29:
                    MoveClassicSparkGroundBounce(ref p, ff, ref keepAlive);
                    return;

                case 21:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    return;

                case 23:
                    ReverseClassicSparkOnce(ref p);
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    p.Scale -= 0.02f * ff;
                    p.Alpha -= 0.0001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;

                case 24:
                    ReverseClassicSparkOnce(ref p);
                    if (p.LifeTime <= 10f)
                    {
                        p.Alpha -= 0.05f * ff;
                        p.Light *= MathF.Pow(p.Alpha, ff);
                    }
                    p.Scale -= 0.02f * ff;
                    return;

                case 25:
                    if ((int)p.LifeTime == 19)
                        p.Velocity -= new Vector3(ff);
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    p.Scale = MathF.Sin(p.LifeTime * (MathF.PI / 40f));
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;

                case 26:
                    if (p.LifeTime <= 0f) keepAlive = false;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    p.Scale -= 0.05f * ff;
                    p.Position = p.StartPosition + p.Velocity * ff;
                    p.StartPosition = p.Position;
                    return;

                case 27:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    p.Scale *= MathF.Pow(1f / 1.03f, ff);
                    return;

                case 30:
                    if (p.LifeTime <= 45f)
                    {
                        // Two independent calls to rand() in original Main.
                        p.Velocity.X += MathF.Sin((MathF.PI / 180f) * R(360)) * ff;
                        p.Velocity.Y += MathF.Cos((MathF.PI / 180f) * R(360)) * ff;
                    }
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    p.Scale -= 0.01f * ff;
                    p.LifeTime -= ff; // Additional decrement in source.
                    if (p.LifeTime <= 0f) keepAlive = false;
                    return;

                case 31:
                {
                    p.Position.Z += p.Gravity * ff;
                    p.Gravity -= 2f * ff;
                    float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
                    if (p.Position.Z < height)
                    {
                        p.Position.Z = height;
                        p.Gravity = -p.Gravity * 0.6f;
                        p.LifeTime -= 4f * ff;
                    }
                    p.Position += p.Velocity * ff;
                    return;
                }
            }
        }

        private void MoveClassicSparkGroundBounce(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            p.Light *= MathF.Pow(1f / 1.02f, ff);
            bool hasScale = p.SubType == 28 || p.SubType == 29;
            if (hasScale)
                p.Scale *= MathF.Pow(1f / 1.01f, ff);

            if (p.Light.X <= (hasScale ? 0.03f : 0.05f))
                keepAlive = false;
            p.Position.Z += p.Gravity * 0.5f * ff;
            p.Gravity -= 1.5f * ff;
            float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
            if (p.Position.Z < height + 3f)
            {
                p.Position.Z = height + 3f;
                p.Gravity = -p.Gravity * 0.3f;
                p.LifeTime -= 2f * ff;
            }
        }

        private static void ReverseClassicSparkOnce(ref ClassicParticle p)
        {
            if (p.LifeTime < 10f && p.Rotation == 0f)
            {
                p.Velocity *= -1f;
                p.Rotation = 1f;
            }
        }

        // In Main this reads target->StartPosition. The ClassicFX owners
        // currently exposing an actual equivalent are particle handles.
        private bool TryGetClassicSparkTargetStartPosition(
            in ClassicFxOwner owner,
            out Vector3 startPosition)
        {
            if (owner.Kind == ClassicFxOwnerKind.ClassicFx &&
                owner.FxHandle.Kind == ClassicFxPoolKind.Particle &&
                TryGetParticle(owner.FxHandle, out ClassicParticle target))
            {
                startPosition = target.StartPosition;
                return true;
            }

            startPosition = Vector3.Zero;
            return false;
        }

        private void MoveClassicSparkPlusTwo(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.SubType == 0)
            {
                p.Frame = (int)((16f - p.LifeTime) / 4f);
            }
            else if (p.SubType == 1)
            {
                if ((int)p.LifeTime % 3 == 0)
                {
                    Vector3 pos = p.Position + new Vector3(
                        (R(100) - 50) * ff,
                        (R(100) - 50) * ff,
                        (R(100) - 50) * ff);
                    // Effect subsystem bridge: does not fabricate an effect.
                    RequestClassicBomb(pos, true);
                }
            }
            else if (p.SubType == 2 || p.SubType == 3)
            {
                // Main draws this at model bone 37 (left) / 28 (right).
                // These are original MU bone IDs, not MonoGame hand indices.
                if (TryGetOwnerBonePosition(
                        p.Target, p.SubType == 2 ? 37 : 28,
                        out Vector3 bonePosition))
                {
                    p.Frame = (int)((16f - p.LifeTime) / 4f);
                    p.Position = bonePosition;
                }
                else if (!TryGetOwnerPosition(p.Target, out _))
                {
                    keepAlive = false;
                }
            }
        }
    }
}
