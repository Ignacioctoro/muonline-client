using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // ZzzEffectParticle.cpp / MoveParticles():
        // desde BITMAP_FIRE_CURSEDLICH hasta BITMAP_MAGIC (inclusive).
        // Todos los subtipos de estas familias se resuelven aquí.
        // Devuelve false solo si el tipo pertenece a otro bloque.
        private bool MoveParticleB(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapFireCursedLich:
                case ClassicTextureIds.BitmapFireHik2Mono:
                    MoveClassicCursedFire(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapLeafTotemGolem:
                {
                    p.Velocity.Z += p.Gravity * ff;
                    float height = RequestTerrainHeight(p.Position.X, p.Position.Y) + 20f;
                    if (p.Position.Z <= height)
                    {
                        p.Velocity = Vector3.Zero;
                        p.Position.Z = height;
                        p.Alpha -= 0.05f * ff;
                        p.Light = new Vector3(p.Alpha);
                    }
                    else
                    {
                        p.Scale += 0.01f * ff;
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapFire:
                case ClassicTextureIds.BitmapFire + 2:
                case ClassicTextureIds.BitmapFire + 3:
                    MoveClassicFire(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapFire + 1:
                    MoveClassicFirePlusOne(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapFlame:
                    MoveClassicFlame(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapFireRed:
                    // La comprobación LifeTime <= 0 del Main ya se efectúa
                    // en MoveParticles() antes de entrar al switch.
                    return true;

                case ClassicTextureIds.BitmapRainCircle:
                case ClassicTextureIds.BitmapRainCircle + 1:
                    p.Scale += (p.SubType == 1 ? p.Gravity : 0.03f) * ff;
                    if (IsCryWolfFirstStage())
                    {
                        p.Light = new Vector3(1f, 1f, 0.7f);
                    }
                    else
                    {
                        float reduction = p.SubType == 2 ? 0.03f : 0.05f;
                        p.Light -= new Vector3(reduction * ff);
                    }
                    return true;

                case ClassicTextureIds.BitmapEnergy:
                    MoveClassicEnergy(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapMagic:
                    if (p.SubType == 0)
                    {
                        p.Scale -= 0.05f * ff;
                        p.Light -= new Vector3(0.01f * ff);
                    }
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicCursedFire(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                    p.Scale -= (R(20) + 10) * 0.001f * ff;
                    p.Position.Z += p.Gravity * 10f * ff;
                    return;

                case 1:
                    p.Scale -= (R(5) + 15) * 0.0016f * ff;
                    p.Position.Z += p.Gravity * 10f * ff;
                    if (p.Scale < 0f)
                        keepAlive = false;
                    FollowClassicParticleOwner(ref p, ref keepAlive);
                    return;

                case 2:
                    p.Scale -= (R(20) + 10) * 0.004f * ff;
                    p.Position.Z += p.Gravity * 25f * ff;
                    return;

                case 3:
                    p.Scale -= 0.03f * ff;
                    p.Position.Z += p.Gravity * 10f * ff;
                    if (p.Scale < 0f)
                        keepAlive = false;
                    // Main: solo sigue al owner si Target->Live.
                    if (TryGetOwnerPosition(p.Target, out Vector3 position))
                    {
                        p.Position = (p.Position - p.StartPosition) + position;
                        p.StartPosition = position;
                    }
                    return;

                case 4:
                case 9:
                    FadeClassicCursedFire(ref p, ff, 10f, 0.2f, ref keepAlive);
                    if (p.Scale > 0f)
                        p.Scale -= (R(3) + 6) * 0.01f * ff;
                    else
                        keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 3f * ff;
                    if (p.SubType == 9)
                    {
                        p.Rotation = 0f;
                        p.Position.Z += p.Gravity * 1.2f * ff;
                    }
                    return;

                case 5:
                    FadeClassicCursedFire(ref p, ff, 15f, 0.2f, ref keepAlive);
                    if (p.Scale > 0f)
                        p.Scale -= (R(2) + 5) * 0.01f * ff;
                    else
                        keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 3f * ff;
                    return;

                case 6:
                    FadeClassicCursedFire(ref p, ff, 10f, 0.2f, ref keepAlive);
                    if (p.Scale > 0f)
                        p.Scale -= (R(3) + 6) * 0.01f * ff;
                    else
                        keepAlive = false;
                    p.Position.X += p.Velocity.X * ff;
                    p.Position.Y += p.Velocity.Y * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 3f * ff;
                    return;

                case 7:
                    FadeClassicCursedFire(ref p, ff, 10f, 0.2f, ref keepAlive);
                    if (p.Scale > 0f)
                        p.Scale -= (R(3) + 3) * 0.01f * ff;
                    else
                        keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 3f * ff;
                    return;

                case 8:
                    FadeClassicCursedFire(ref p, ff, 5f, 0.1f, ref keepAlive);
                    p.Scale += (R(3) + 8) * 0.01f * ff;
                    if (p.LifeTime < 3f)
                        p.Scale += (10f - p.LifeTime) * 0.02f * ff;

                    // El Main aplica 8 unidades SIN factor FPS a este vector.
                    Vector3 direction = Rotate(new Vector3(0f, -1f, 0f), p.Angle);
                    p.Position += direction * 8f;
                    FollowClassicParticleOwner(ref p, ref keepAlive);
                    p.Rotation += 5f * ff;
                    return;
            }
        }

        private void FadeClassicCursedFire(
            ref ClassicParticle p,
            float ff,
            float fadeStartLifetime,
            float fadeStep,
            ref bool keepAlive)
        {
            if (p.LifeTime < fadeStartLifetime)
                p.Alpha -= fadeStep * ff;
            else if (p.Alpha < 1f)
                p.Alpha += (R(2) + 2) * 0.1f * ff;
            else
                p.Alpha = 1f;

            if (p.Alpha < 0.1f)
                keepAlive = false;
            p.Light = p.TurningForce * p.Alpha;
        }

        private void MoveClassicFire(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            // El Main aplica estos cambios a todos los subtipos salvo 16.
            if (p.SubType == 16)
            {
                p.Frame = (int)(3f - p.LifeTime);
                p.Light *= MathF.Pow(1f / 1.7f, ff);
                p.Scale *= MathF.Pow(1.1f, ff);
                return;
            }

            p.Gravity += 0.004f * ff;
            float luminosity = p.LifeTime / 24f;
            p.Frame = (int)((23f - p.LifeTime) / 6f);

            switch (p.SubType)
            {
                case 0:
                case 7:
                    p.Scale -= 0.04f * ff;
                    break;

                case 17:
                case 5:
                case 6:
                    p.Scale -= 0.04f * ff;
                    p.Rotation += 5f * ff;
                    break;

                case 8:
                    p.Scale *= MathF.Pow(0.95f, ff);
                    p.Rotation += 5f * ff;
                    break;

                case 9:
                    if (TryGetOwnerPosition(p.Target, out Vector3 ownerPosition))
                        p.Position = ownerPosition + p.StartPosition;
                    else
                        keepAlive = false;

                    // Main: división ENTERA del resultado de rand()%60+60.
                    p.Gravity += ((R(60) + 60) / 100) * ff;
                    p.Scale -= p.Gravity * ff / 90f;
                    break;

                case 11:
                    // El Main calcula antes otro Frame pero lo sobrescribe
                    // inmediatamente; prevalece el valor común ya asignado.
                    p.Scale += 0.04f * ff;
                    break;

                case 10:
                    p.Scale *= MathF.Pow(0.95f, ff);
                    break;

                case 12:
                case 13:
                    p.Rotation += (R(10) + 10f) * ff;
                    p.Scale -= 0.04f * ff;
                    p.Light = new Vector3(luminosity);
                    break;

                case 14:
                    p.Rotation += p.Gravity * ff;
                    p.Scale += 0.03f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    if (p.Light.Z <= 0.05f || p.Scale <= 0f)
                        keepAlive = false;
                    break;

                case 15:
                    p.Scale -= 0.04f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    if (p.Light.Z <= 0.05f || p.Scale <= 0f)
                        keepAlive = false;
                    break;

                case 18:
                    p.Scale += p.Gravity * ff;
                    // Main: VectorScale(o->Velocity, 0.98f), sin FPS.
                    p.Velocity *= 0.98f;
                    p.Light *= 0.95f;
                    break;

                default:
                    p.Scale += p.Gravity * ff;
                    p.Velocity *= 0.98f;
                    break;
            }

            p.Position.Z += p.Gravity * 10f * ff;
        }

        private void MoveClassicFirePlusOne(ref ClassicParticle p, float ff)
        {
            float luminosity;
            switch (p.SubType)
            {
                case 1:
                    p.Velocity *= 1.05f;
                    p.Position.Z += p.Gravity * 20f * ff;
                    luminosity = p.LifeTime * 0.2f;
                    p.Light = new Vector3(luminosity);
                    return;
                case 2:
                    luminosity = p.LifeTime * 0.2f;
                    p.Light = new Vector3(luminosity);
                    return;
                case 3:
                case 0:
                    p.Gravity += 0.02f * ff;
                    p.Scale += p.Gravity * ff;
                    p.Velocity *= 1.05f;
                    p.Position.Z += p.Gravity * 20f * ff;
                    luminosity = p.LifeTime * 0.2f;
                    p.Light = new Vector3(luminosity);
                    return;
                case 4:
                {
                    float count = (p.Velocity.X + p.LifeTime) * 0.1f;
                    p.Position.X = p.StartPosition.X + MathF.Sin(count) * 120f;
                    p.Position.Y = p.StartPosition.Y - MathF.Cos(count) * 120f;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.002f * ff;
                    return;
                }
                case 5:
                    // El Main usa sin/cos directamente con Angle[2] en radianes.
                    p.Position.X += MathF.Cos(p.Angle.Z) * 20f * ff;
                    p.Position.Y += MathF.Sin(p.Angle.Z) * 20f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += ff;
                    return;
                case 6:
                    p.Position.Z += p.Gravity * ff;
                    p.Rotation += 4f * ff;
                    return;
                case 7:
                    p.Velocity *= 1.05f;
                    luminosity = p.LifeTime * 0.2f;
                    p.Light = new Vector3(luminosity);
                    return;
                case 8:
                case 9:
                    p.Scale += 0.13f * ff;
                    p.Rotation += p.Gravity * ff;
                    p.Light *= MathF.Pow(1f / 1.13f, ff);
                    return;
            }
        }

        private void MoveClassicFlame(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            // Todos los subtipos se liberan con LifeTime <= 0 por
            // el bloque común de MoveParticles().
            if (p.LifeTime <= 0f)
            {
                keepAlive = false;
                return;
            }

            switch (p.SubType)
            {
                case 1:
                case 5:
                    if ((int)p.LifeTime == 10)
                    {
                        p.Velocity.Y += 6.4f * ff;
                        if (p.SubType == 1)
                            p.Scale -= 0.15f * ff;
                        else
                            p.Scale *= MathF.Pow(0.8f, ff);
                    }
                    p.Rotation = R(360);
                    p.Light -= new Vector3(0.05f * ff);
                    return;

                case 2:
                    p.Rotation = R(360);
                    p.Light -= new Vector3(0.05f * ff);
                    return;

                case 3:
                    p.Rotation = R(360);
                    return;

                case 4:
                    if (TryGetOwnerPosition(p.Target, out Vector3 ownerPosition))
                    {
                        Vector3 delta = ownerPosition - p.StartPosition;
                        p.Position.X += delta.X * ff;
                        p.Position.Y += delta.Y * ff;
                        // Main: Z del owner SIN multiplicar por FPS.
                        p.Position.Z += delta.Z + p.Gravity * ff;
                        p.StartPosition = ownerPosition;
                    }
                    else
                    {
                        keepAlive = false;
                    }
                    p.Gravity += 0.1f * ff;
                    p.Rotation = R(360);
                    p.Light -= new Vector3(0.05f * ff);
                    return;

                case 8:
                case 7:
                case 9:
                {
                    float rotationStep = p.SubType == 9 ? 0.5f : 2f;
                    p.Rotation += (p.StartPosition.X < p.Position.X ? rotationStep : -rotationStep) * ff;
                    float heightFactor = p.SubType == 8 ? 0.5f : p.SubType == 9 ? 1.2f : 1f;
                    float scaleDivisor = p.SubType == 9 ? 98f : 95f;
                    float lightDivisor = p.SubType == 9 ? 1.008f : 1.007f;
                    p.Position.Z += p.Gravity * heightFactor * ff;
                    p.Scale -= p.Gravity * ff / scaleDivisor;
                    if (p.Scale <= 0f)
                        keepAlive = false;
                    p.Light *= MathF.Pow(1f / lightDivisor, ff);
                    return;
                }

                case 10:
                    // Main: MovePosition() común + un segundo desplazamiento.
                    p.Position += p.Velocity * ff;
                    float attenuation = MathF.Pow(0.95f, ff);
                    p.Velocity.X *= attenuation;
                    p.Velocity.Y *= attenuation;
                    p.Light = new Vector3(p.LifeTime / 10f);
                    p.Velocity.Z += 0.3f * ff;
                    p.Scale += 0.07f * ff;
                    return;

                case 11:
                    p.Light *= MathF.Pow(1f / 1.008f, ff);
                    return;
            }
        }

        private void MoveClassicEnergy(ref ClassicParticle p, float ff)
        {
            p.Rotation += p.Gravity * ff;
            switch (p.SubType)
            {
                case 1:
                    p.Light = new Vector3(p.LifeTime / 15f);
                    p.Scale += 0.1f * ff;
                    break;
                case 2:
                    p.Light -= new Vector3(0.01f * ff);
                    break;
                case 3:
                case 4:
                case 5:
                {
                    if (p.SubType == 3 || p.SubType == 4)
                    {
                        int bone = p.SubType == 3 ? 37 : 28;
                        if (TryGetOwnerBonePosition(p.Target, bone, out Vector3 bonePosition))
                            p.Position = bonePosition;
                    }
                    else if (TryGetOwnerPosition(p.Target, out Vector3 ownerPosition))
                    {
                        p.Position = ownerPosition;
                        p.Position.Z += 80f * ff;
                    }
                    p.Light *= MathF.Pow(1f / 1.01f, ff);
                    break;
                }
                case 7:
                    // Main: multiplicación literal, sin FPS factor.
                    p.Light *= 0.97f;
                    break;
            }
        }
    }
}
