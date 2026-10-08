using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles():
        // BITMAP_LIGHT ... BITMAP_POUNDING_BALL (7943-8218).
        // Tipos y subtipos originales, sin lógica propia de skills.
        private bool MoveParticleJ(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapLight:
                    MoveClassicLight(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapPoundingBall:
                    MoveClassicPoundingBall(ref p, ff, ref keepAlive);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicLight(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float factor;
            switch (p.SubType)
            {
                case 0:
                case 2:
                case 7:
                case 8:
                {
                    factor = p.SubType == 0 || p.SubType == 8 ? 0.5f : 4f;
                    if (p.SubType == 7)
                    {
                        // El Main pasa WorldTime directamente a sinf() (radianes).
                        factor = MathF.Sin((float)Clock.WorldTimeMilliseconds + p.LifeTime) * 5f + 10f;
                        p.Position.Z += factor * ff;
                    }
                    else
                    {
                        // Cada componente consume un rand() independiente.
                        Vector3 randomWind = new Vector3(
                            (R(2001) - 1000) * 0.0004f * ff,
                            (R(2001) - 1000) * 0.0004f * ff,
                            0f);
                        p.Position += (randomWind + _particleWind) * factor;
                        p.Position.Z += 5f * factor * ff;
                    }

                    if (p.SubType == 0 || p.SubType == 8)
                        p.Scale -= 0.05f * ff;
                    else if (p.SubType == 7)
                        p.Rotation += 30f * ff;
                    else
                        p.Scale *= MathF.Pow(0.95f, ff);

                    if (p.SubType == 7)
                    {
                        if (p.LifeTime < 10f)
                            p.Light *= MathF.Pow(1f / 1.2f, ff);
                    }
                    else
                    {
                        p.Light = new Vector3(p.Scale);
                    }

                    if (((p.SubType == 0 || p.SubType == 8) && p.Scale <= 0.1f) ||
                        (p.SubType == 7 && p.Scale <= 0.1f) ||
                        (p.SubType == 2 && p.Scale <= 0.3f))
                    {
                        p.LifeTime = -1f;
                        keepAlive = false;
                    }

                    if (p.SubType == 8)
                        p.Light = new Vector3(p.Scale, 0.5f * p.Scale, 0.3f * p.Scale);
                    return;
                }

                case 6:
                {
                    // En el Main, a diferencia de 0/2/8, NO se suma viento global.
                    Vector3 randomWind = new Vector3(
                        (R(2001) - 1000) * 0.0004f * ff,
                        (R(2001) - 1000) * 0.0004f * ff,
                        0f);
                    p.Position += randomWind * 0.5f;
                    p.Position.Z += 2.5f * ff;
                    p.Scale *= MathF.Pow(0.95f, ff);
                    p.Light *= MathF.Pow(0.95f, ff);
                    if (p.Scale <= 0.1f)
                    {
                        p.LifeTime = -1f;
                        keepAlive = false;
                    }
                    return;
                }

                case 1:
                case 5:
                    // VectorScale(Light,0.9) no utiliza FPS_ANIMATION_FACTOR.
                    p.Light *= 0.9f;
                    p.Scale *= MathF.Pow(0.95f, ff);
                    return;

                case 3:
                    p.Light *= 0.9f;
                    p.Scale *= MathF.Pow(0.85f, ff);
                    p.Gravity += 5f * ff;
                    if (TryGetOwnerPosition(p.Target, out Vector3 ownerPosition))
                        p.Position = ownerPosition;
                    else
                        keepAlive = false;
                    p.Position.X += 2f * ff;
                    p.Position.Y -= 2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    return;

                case 4:
                {
                    // TransformPosition(bone[(int)Rotation], Angle, ..., false)
                    // y suma Owner.Position en el Main. El bridge MonoGame
                    // devuelve la transformación WORLD completa: no volver
                    // a sumar Owner.Position o la mano quedará desplazada.
                    if (TryGetOwnerWorldObject(p.Target, out WorldObject worldObject) &&
                        worldObject is PlayerObject &&
                        TryTransformOwnerBonePosition(
                            p.Target, (int)p.Rotation, p.Angle, out Vector3 bonePosition))
                    {
                        p.Position = bonePosition;
                    }
                    else
                    {
                        keepAlive = false;
                    }
                    p.Gravity += ((R(40) + 60) / 100f * 9.5f) * ff;
                    p.Scale -= ff * (R(400) + 400) / 10000f;
                    p.Position.Z += p.Gravity * ff;
                    p.Light *= MathF.Pow(1f / 1.35f, ff);
                    return;
                }

                case 9:
                    p.Scale -= 0.011f * ff;
                    if (p.Scale <= 0f)
                    {
                        p.LifeTime = 0f;
                        keepAlive = false;
                    }
                    p.Light *= p.LifeTime >= 75f
                        ? MathF.Pow(1.05f, ff)
                        : MathF.Pow(1f / 1.05f, ff);
                    return;

                case 10:
                    p.Rotation -= 10f * ff;
                    // Main: solo valida CurrentAction cuando Target != NULL.
                    if (p.Target.HasOwner && !IsOwnerSanta2Action(p.Target))
                        keepAlive = false;
                    return;

                case 11:
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.0005f * ff;
                    return;

                case 12:
                    p.Scale += (p.LifeTime > 80f ? 0.02f : -0.005f) * ff;
                    return;

                case 13:
                    p.Position.Y -= p.Gravity * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.001f * ff;
                    return;

                case 14:
                    // Main: Light *= 0.98 sin factor FPS.
                    p.Light *= 0.98f;
                    p.Scale *= MathF.Pow(0.95f, ff);
                    return;

                case 15:
                {
                    p.StartPosition.Z += (R(10) + 5) * 0.01f * ff;
                    p.Position.X = p.StartPosition.X + MathF.Sin(p.StartPosition.Z) * p.Gravity * 2f;
                    p.Position.Y = p.StartPosition.Y + MathF.Cos(p.StartPosition.Z) * p.Gravity * 2f;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.001f * ff;
                    if (p.LifeTime > 20f)
                    {
                        p.Alpha += 0.1f * ff;
                        if (p.Alpha > 1f) p.Alpha = 1f;
                    }
                    else
                    {
                        p.Scale -= 0.01f * ff;
                        p.Alpha -= 0.1f * ff;
                        // El Main solo limita el valor superior.
                        if (p.Alpha > 1f) p.Alpha = 1f;
                    }
                    p.Light = p.TurningForce * p.Alpha;
                    return;
                }
            }
        }

        private void MoveClassicPoundingBall(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            // El Main escribe literalmente 'SubType == 0 && SubType == 1'.
            // La condición es imposible, por lo que 0/1 NO hacen nada
            // en este switch. No sustituimos por || sin evidencias.
            if (p.SubType == 2)
            {
                if (TryGetOwnerSnapshot(p.Target, out ClassicFxOwnerSnapshot owner))
                    p.Angle = owner.Angle;
                else
                    keepAlive = false;
                p.Scale -= 0.06f * ff;
                p.Position.Z += p.Gravity * 10f * ff;
                p.Light = new Vector3(p.LifeTime / 10f);
            }
            else if (p.SubType == 3)
            {
                if (p.LifeTime < 15f)
                    p.Alpha -= 0.2f * ff;
                else if (p.Alpha < 1f)
                    p.Alpha += (R(2) + 2) * 0.1f * ff;
                else
                    p.Alpha = 1f;

                if (p.Alpha < 0.1f)
                    keepAlive = false;
                p.Light = p.TurningForce * p.Alpha;
                if (p.Scale > 0f)
                    p.Scale -= (R(3) + 5) * 0.01f * ff;
                else
                    keepAlive = false;
                p.Position.Z += p.Gravity * ff;
                p.Rotation += 3f * ff;
            }
        }
    }
}
