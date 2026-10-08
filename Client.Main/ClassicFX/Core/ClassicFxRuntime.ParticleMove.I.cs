using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles(), lines 7446-7942.
        // Weather, Death Stab, torches, ghost clouds and all BITMAP_CLOUD subtypes.
        private bool MoveParticleI(ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapSnowEffect1:
                case ClassicTextureIds.BitmapSnowEffect2:
                    p.Rotation += 20f * ff;
                    p.Position.Z += p.Gravity * 0.5f * ff;
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    p.Gravity -= 1.5f * ff;
                    p.Scale += (p.LifeTime < 10f ? 0.01f : -0.01f) * ff;
                    p.Position += p.Velocity * ff;
                    return true;

                case ClassicTextureIds.BitmapDsEffect:
                    p.Rotation += 10f * ff;
                    // Main only checks CurrentAction when Target != NULL.
                    if (p.Target.HasOwner && !IsOwnerSanta2Action(p.Target))
                        keepAlive = false;
                    return true;

                case ClassicTextureIds.BitmapFirecracker:
                    p.Velocity.Z -= 0.5f * ff;
                    return true;

                case ClassicTextureIds.BitmapSwordForce:
                    p.Gravity += 0.01f * ff;
                    p.Scale += p.Gravity * ff;
                    p.Light = new Vector3(p.LifeTime / 10f);
                    return true;

                case ClassicTextureIds.BitmapTorchFire:
                    if (p.LifeTime <= 0f)
                        keepAlive = false;
                    else
                    {
                        p.Position.Z += p.Gravity * ff;
                        if (p.Position.Z >= 500f)
                            keepAlive = false;
                        if (p.LifeTime >= 20f)
                        {
                            p.Scale -= 0.02f * ff;
                            if (p.Scale <= 0.02f)
                                keepAlive = false;
                        }
                    }
                    return true;

                case ClassicTextureIds.BitmapGhostCloud1:
                case ClassicTextureIds.BitmapGhostCloud2:
                    MoveClassicGhostCloudI(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapCloud:
                    MoveClassicCloudI(ref p, ff, ref keepAlive);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicGhostCloudI(ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            if (p.SubType != 0)
                return;

            p.Position += p.Velocity * ff;
            float distance = (p.Position - p.StartPosition).Length();
            float rotation = (0.2f + R(4) * 0.1f) * ff;
            p.Rotation += MathF.Sin((float)Clock.WorldTimeMilliseconds * 0.001f) > 0f
                ? rotation : -rotation;

            if (distance <= 20f)
                p.Light = ClassicCloudMinLightI(p.TurningForce, 0.1f * (500f - p.LifeTime));
            else
                p.Light *= MathF.Pow(1f / 1.02f, ff);

            if (distance > 500f)
                keepAlive = false;
        }

        private void MoveClassicCloudI(ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            float worldTime = (float)Clock.WorldTimeMilliseconds;
            float distance;
            switch (p.SubType)
            {
                case 6:
                case 14:
                    p.Position += p.Velocity * ff;
                    float damping = MathF.Pow(0.95f, ff);
                    p.Velocity.X *= damping;
                    p.Velocity.Y *= damping;
                    p.Velocity.Z += 0.6f * ff;
                    p.Position.X += (R(4) - 2) * ff;
                    p.Position.Y += (R(4) - 2) * ff;
                    p.Position.Z += (R(4) - 2) * 0.8f * ff;
                    p.Scale += 0.05f * ff;
                    p.Light = new Vector3(p.LifeTime / 50f);
                    p.Rotation += (1f + R(2)) * ff;
                    break;

                case 8:
                case 9:
                case 20:
                    MoveClassicCloudIAlpha(ref p, ff, 150f, 0.04f, 0.025f,
                        1f, -0.5f, 0.1f, ref keepAlive);
                    break;

                case 10:
                    p.Position += p.Velocity * ff;
                    p.TurningForce.X = 1.5f;
                    distance = (p.Position - p.StartPosition).Length();
                    if (distance <= 300f)
                    {
                        p.Light += new Vector3(0.01f * ff);
                        if (p.Light.X >= 0.15f)
                            p.Light = new Vector3(0.15f);
                    }
                    else if (distance <= 500f)
                        p.Light *= MathF.Pow(1f / 1.05f, ff);
                    else
                        keepAlive = false;
                    break;

                case 11:
                    p.Position += p.Velocity * ff;
                    p.TurningForce.X = 1.5f;
                    p.Rotation += (0.5f + R(2)) * ff;
                    if ((p.Position - p.StartPosition).Length() > 500f)
                        keepAlive = false;
                    break;

                case 13:
                    p.Rotation += p.Gravity * 1.5f * ff;
                    break;

                case 15:
                    if (p.LifeTime <= 10f)
                        keepAlive = false;
                    p.Position += p.Velocity * ff;
                    distance = (p.Position - p.StartPosition).Length();
                    p.Rotation += (0.5f + R(2)) * ff;
                    if (distance <= 50f)
                        p.Light = ClassicCloudMinLightI(p.TurningForce,
                            0.1f * (500f - p.LifeTime));
                    else
                    {
                        p.Light *= MathF.Pow(1f / 1.02f, ff);
                        if (p.Light.X < 0.001f)
                            keepAlive = false;
                    }
                    if (distance > 500f)
                        keepAlive = false;
                    break;

                case 16:
                    if (p.LifeTime <= 10f)
                        keepAlive = false;
                    p.Position += p.Velocity * ff;
                    distance = (p.Position - p.StartPosition).Length();
                    p.Rotation += (0.5f + R(2)) * ff;
                    if (distance <= 50f)
                    {
                        p.Light += new Vector3(0.01f * ff);
                        if (p.Light.X >= 0.2f)
                            p.Light = new Vector3(0.2f);
                    }
                    else
                    {
                        p.Light *= MathF.Pow(1f / 1.05f, ff);
                        if (p.Light.X < 0.001f)
                            keepAlive = false;
                    }
                    if (distance >= 100f)
                        keepAlive = false;
                    break;

                case 17:
                    if (p.LifeTime <= 10f)
                        keepAlive = false;
                    p.Position += p.Velocity * ff;
                    distance = (p.Position - p.StartPosition).Length();
                    p.Rotation += (0.08f + R(2) * 0.15f) * ff;
                    if (p.LifeTime >= 400f)
                        p.Light = ClassicCloudMinLightI(p.TurningForce,
                            0.02f * (500f - p.LifeTime));
                    else
                    {
                        p.Light *= MathF.Pow(1f / 1.02f, ff);
                        if (p.Light.X < 0.015f)
                            keepAlive = false;
                    }
                    if (distance > 500f)
                        keepAlive = false;
                    break;

                case 18:
                    if (p.LifeTime > 90f)
                        p.Light += new Vector3(0.002f, 0.002f, 0.0017f) * ff;
                    else if (p.LifeTime < 30f)
                        p.Light -= new Vector3(0.004f, 0.004f, 0.0033f) * ff;
                    if (p.Scale > 0f)
                        p.Scale -= 0.001f * ff;
                    if (p.TurningForce.Y > 0f)
                        p.TurningForce.Y -= 0.5f * ff;
                    if (TryGetOwnerPosition(p.Target, out Vector3 target18))
                    {
                        float a = (worldTime + p.Rotation) * p.TurningForce.X;
                        p.Position.X = target18.X + p.StartPosition.X +
                            MathF.Cos(a) * p.TurningForce.Y;
                        p.Position.Y = target18.Y + p.StartPosition.Y +
                            MathF.Sin(a) * p.TurningForce.Y;
                    }
                    else
                        keepAlive = false;
                    // Main rand()%10/10.0f: ten possible steps, not integer division.
                    p.Position.Z += (R(10) / 10f) * ff;
                    if (p.LifeTime <= 0f)
                    {
                        RestoreClassicCloudHiddenMeshI(p.Target);
                        keepAlive = false;
                    }
                    break;

                case 19:
                    if (p.LifeTime > 30f)
                    {
                        p.Light += new Vector3(0.01f * ff);
                        p.Scale += 0.01f * ff;
                    }
                    else
                        p.Light -= new Vector3(0.01f * ff);
                    p.Position.Y += (8f + R(200) * 0.1f) * ff;
                    if (p.LifeTime <= 0f)
                    {
                        RestoreClassicCloudHiddenMeshI(p.Target);
                        keepAlive = false;
                    }
                    break;

                case 21:
                    MoveClassicCloudIAlpha(ref p, ff, 50f, 0.04f, 0.005f,
                        2f, -1f, 0.1f, ref keepAlive);
                    break;

                case 22:
                    MoveClassicCloudIAlpha(ref p, ff, 40f, 0.1f, 0.025f,
                        0.5f, -0.5f, 0f, ref keepAlive);
                    p.Scale += 0.001f * ff;
                    break;

                case 23:
                    if (p.LifeTime <= 0f)
                        keepAlive = false;
                    else if (p.LifeTime > 40f)
                    {
                        if (p.Alpha < 1f)
                            p.Alpha += 0.2f * ff;
                        p.Scale += 0.003f * ff;
                    }
                    else
                    {
                        float factor = MathF.Pow(0.7f, ff);
                        p.Velocity.X *= factor;
                        p.Velocity.Y *= factor;
                        if (p.Alpha > 0f)
                            p.Alpha -= R(10) * 0.01f * ff;
                        else
                            keepAlive = false;
                        p.Position.Z += 0.2f * ff;
                    }
                    p.Rotation += p.TurningForce.X * 5f * ff;
                    break;

                default:
                    // Source's final else branch: owner visibility extends lifetime.
                    // Missing/stale owners are not dereferenced or fabricated.
                    if (TryGetOwnerWorldObject(p.Target, out WorldObject owner))
                    {
                        if (owner.Visible)
                            p.LifeTime = 50f;
                    }
                    else
                        keepAlive = false;
                    if (p.LifeTime <= 0f)
                    {
                        RestoreClassicCloudHiddenMeshI(p.Target);
                        keepAlive = false;
                    }
                    p.Position.Z = p.StartPosition.Z +
                        MathF.Sin((worldTime + p.Gravity) / 5000f) * 20f;
                    break;
            }

            // Final original switch, after the subtype's branch.
            switch (p.SubType)
            {
                case 1:
                case 4:
                    p.Rotation = worldTime * 0.02f * p.TurningForce.X + p.StartPosition.Y;
                    break;
                case 2:
                case 5:
                    p.Rotation = worldTime * -0.02f * p.TurningForce.X + p.StartPosition.Y;
                    break;
            }
        }

        private static Vector3 ClassicCloudMinLightI(in Vector3 original, float luminosity)
        {
            return Vector3.Min(original * luminosity, original);
        }

        private static void MoveClassicCloudIAlpha(
            ref ClassicParticle p, float ff, float threshold, float fadeIn,
            float fadeOut, float rise, float fall, float fadeFloor, ref bool keepAlive)
        {
            if (p.LifeTime <= 0f)
                keepAlive = false;
            else if (p.LifeTime > threshold)
            {
                if (p.Alpha < 1f)
                    p.Alpha += fadeIn * ff;
                p.Position.Z += rise * ff;
            }
            else
            {
                if (p.Alpha > fadeFloor)
                    p.Alpha -= fadeOut * ff;
                else if (fadeFloor <= 0f)
                    keepAlive = false;
                p.Position.Z += fall * ff;
            }
        }

        private void RestoreClassicCloudHiddenMeshI(in ClassicFxOwner owner)
        {
            // The classic Main writes Target->HiddenMesh = 0 on expiration.
            if (TryGetOwnerWorldObject(owner, out WorldObject worldObject) &&
                worldObject is ModelObject model)
            {
                model.HiddenMesh = 0;
            }
        }
    }
}
