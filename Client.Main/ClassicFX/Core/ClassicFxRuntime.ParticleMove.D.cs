using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain / ZzzEffectParticle.cpp / MoveParticles():
        // BITMAP_SMOKE, todos los case SubType 0..69 presentes en el Main.
        // La ausencia de SubType 39 es deliberada: no existe en este switch.
        // El movimiento común (EnableMove) y LifeTime -= ff ocurren antes.
        private bool MoveParticleD(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.Type != ClassicTextureIds.BitmapSmoke)
                return false;

            MoveClassicSmoke(ref p, ff, ref keepAlive);
            return true;
        }

        private void MoveClassicSmoke(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float l;
            float worldTime = (float)Clock.WorldTimeMilliseconds;

            switch (p.SubType)
            {
                case 0:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l);
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 36:
                    if (p.LifeTime < 23f)
                    {
                        p.Gravity += 0.1f * p.Scale * ff;
                        p.Position.Z += p.Gravity * ff;
                        p.Position.Y += p.Gravity * ff;
                        p.Scale -= 0.08f * ff;
                    }
                    else if (TryGetOwnerPosition(p.Target, out Vector3 owner36))
                        p.Position = owner36 + p.StartPosition;
                    else
                        keepAlive = false;
                    p.Rotation += 0.01f * ff;
                    break;

                case 37:
                    p.Position.Z -= p.TurningForce.Z * 0.8f * ff;
                    p.Position.Y -= p.TurningForce.Y * 0.8f * ff;
                    p.Scale += 0.08f * ff;
                    p.Rotation += 0.01f * ff;
                    p.Light *= MathF.Pow(1f / 1.03f, ff);
                    break;

                case 38:
                    p.Scale += 0.09f * ff;
                    p.Light *= MathF.Pow(1f / 1.04f, ff);
                    break;

                case 33:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l * 0.4f, l * 0.4f, l);
                    p.Velocity *= 0.001f;
                    p.Scale += 0.02f * ff;
                    p.Position.Z += p.Scale * ff;
                    break;

                case 32:
                case 3:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l * 0.8f, l * 0.8f, l);
                    p.Velocity *= 0.4f;
                    p.Scale += 0.1f * ff;
                    break;

                case 23:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l * 0.1f, l, l * 0.6f);
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 11:
                case 14:
                    l = p.LifeTime / 50f;
                    p.Light = p.TurningForce * l;
                    p.Velocity *= 0.4f;
                    p.Scale += 0.05f * ff;
                    p.Position.Z -= ff;
                    break;

                case 17:
                    l = p.LifeTime / 8f;
                    if (l > 1f) l = 1f - l; // Main: no es Clamp.
                    p.Light = p.TurningForce * l;
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 15:
                case 19:
                case 20:
                    MoveSmokeDrifting(ref p, ff, p.SubType == 20);
                    break;

                case 1:
                case 40:
                case 41:
                case 53:
                case 56:
                case 58:
                    l = p.LifeTime / 50f;
                    switch (p.SubType)
                    {
                        case 1:
                        case 41: p.Light = new Vector3(l * 0.5f, l, l * 0.8f); break;
                        case 40: p.Light = new Vector3(l * 0.5f, l * 0.5f, l); break;
                        case 53: p.Light = new Vector3(l); break;
                        case 56: p.Light = new Vector3(l * 0.5f, l * 0.1f, l * 0.8f); break;
                        case 58: p.Light = p.StartPosition * l; break;
                    }
                    p.Velocity *= 0.4f;
                    p.Scale += 0.05f * ff;
                    if (p.SubType == 41)
                    {
                        p.Gravity += 0.2f * ff;
                        p.Position.Z += p.Gravity * ff;
                    }
                    break;

                case 2:
                case 16:
                case 21:
                    l = p.LifeTime / 50f;
                    p.Light = new Vector3(l);
                    p.Gravity -= 0.1f * ff;
                    p.Position.X -= p.Gravity * 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.01f * ff;
                    break;

                case 12:
                case 13:
                case 18:
                case 25:
                case 59:
                case 48:
                    MoveSmokePlainScale(ref p, ff);
                    break;

                case 4:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l * 120f / 255f, l * 100.7f / 255f, l * 80f / 255f);
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 5:
                    l = p.LifeTime / 10f;
                    p.Light = new Vector3(l);
                    p.Gravity -= 0.1f * ff;
                    p.Position.X -= p.Gravity * (0.2f * (R(2) + 1)) * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale -= 0.01f * ff;
                    break;

                case 6:
                    // Main: este subtype fija LifeTime=10 cada movimiento.
                    p.LifeTime = 10f;
                    p.Position.Z = p.Rotation + MathF.Sin((worldTime + p.Gravity) / 5000f) * 20f;
                    float angle = ((int)(p.Gravity + worldTime) % 1800) * 0.1f * (MathF.PI / 180f);
                    p.Scale = MathF.Sin(angle) * 0.5f + 1.8f;
                    p.Light = new Vector3(0.6f * 0.6f, 0.6f * 0.5f, 0.6f * 0.4f);
                    break;

                case 7:
                    l = 1f;
                    p.Scale += 0.03f * ff;
                    p.Gravity += ff;
                    p.Velocity.Y -= 0.1f * ff;
                    p.Position.Z -= p.Gravity * ff;
                    if (p.LifeTime < 5f)
                    {
                        l = p.LifeTime / 8f;
                        p.Scale -= 0.1f * ff;
                    }
                    p.Light = new Vector3(l * 0.725f, l * 0.572f, l * 0.333f);
                    break;

                case 8:
                    p.Gravity += 0.02f * ff;
                    if (p.LifeTime > 5f)
                    {
                        p.Scale += p.Gravity * ff;
                        p.Position.Z += p.Gravity * 20f * ff;
                        p.Velocity *= 1.05f;
                        l = p.LifeTime / 24f;
                        p.Light = new Vector3(l);
                        p.Velocity *= 0.4f;
                    }
                    else
                    {
                        p.Light *= 0.5f;
                        p.Velocity = Vector3.Zero;
                    }
                    break;

                case 9:
                    l = p.LifeTime / 10f;
                    p.Light = new Vector3(l * 250f / 255f, l * 156.7f / 255f, 0f);
                    p.Gravity += 0.5f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.1f * ff;
                    break;

                case 10:
                    l = p.LifeTime / 16f;
                    p.Light = new Vector3(l, l * 0.1f, l * 0.1f);
                    p.Scale += 0.05f * ff;
                    break;

                case 22:
                case 60:
                    l = p.LifeTime / 50f;
                    p.Light = p.SubType == 22
                        ? new Vector3(l * 0.9f, l * 0.5f, l * 0.5f)
                        : new Vector3(l * 0.4f);
                    p.Gravity -= 0.1f * ff;
                    p.Position.X += p.Gravity * MathF.Sin(worldTime) * 0.05f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.04f * ff;
                    break;

                case 24:
                    l = p.LifeTime / 32f;
                    p.Light = new Vector3(l * 0.2f, l * 0.5f, l * 0.35f);
                    p.Gravity += 0.1f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 35:
                case 34:
                    p.Light *= MathF.Pow(1f / 1.04f, ff);
                    p.Scale += 0.06f * ff;
                    break;

                case 26:
                    l = p.LifeTime / 20f;
                    p.Light *= l;
                    p.Scale += 0.15f * ff;
                    break;

                case 27:
                    p.Light *= 0.92f; // Factor directo, sin pow ni ff.
                    p.Scale -= 0.05f * ff;
                    break;

                case 28:
                case 29:
                    p.Position.Z += p.Gravity * ff;
                    if (p.LifeTime >= 9f) p.Scale -= 0.4f * ff;
                    else p.Scale *= MathF.Pow(1.2f, ff);
                    if (p.SubType == 28) p.Light *= 0.92f;
                    else p.Light = Vector3.One;
                    break;

                case 30:
                    p.Scale += 0.15f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Light = new Vector3(1f, 3f, 1f);
                    break;

                case 31:
                    p.Scale -= 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    if (p.Scale <= 0f && p.Light.Z <= 0f)
                        keepAlive = false;
                    break;

                case 42:
                    l = p.LifeTime / 8f;
                    p.Light = new Vector3(l * 127f / 255f, l, l * 200f / 255f);
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 43:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    p.Scale += 0.08f * ff;
                    break;

                case 44:
                    p.Light *= MathF.Pow(p.LifeTime >= 30f ? 1.07f : 1f / 1.07f, ff);
                    p.Scale += 0.02f * ff;
                    p.Position.Z += 0.8f * ff;
                    p.Rotation += p.Gravity / 50f * ff;
                    break;

                case 45:
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    break;

                case 46:
                case 54:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    if (p.Light.X <= 0.05f)
                        p.LifeTime = 0f; // Main: muere al entrar al próximo Move.
                    p.Gravity -= 0.05f * ff;
                    p.Position.Z += (p.Scale + p.Gravity) * 1.5f * ff;
                    p.Scale += p.Scale * ff / 100f;
                    break;

                case 47:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    if (p.Light.X <= 0.05f)
                        p.LifeTime = 0f;
                    p.Scale += ff;
                    break;

                case 49:
                    p.Light *= MathF.Pow(p.LifeTime < 100f ? 0.97f : 1.03f, ff);
                    if (p.Light.X <= 0.01f)
                        p.LifeTime = 0f;
                    p.Velocity *= 0.001f;
                    p.Scale += (R(15) + 4) * 0.001f * ff;
                    p.Position.Z += p.Scale * 6f * ff;
                    p.Rotation += (R(10) + 10) * 0.1f * p.Angle.X * ff;
                    break;

                case 50:
                    if (p.LifeTime < 10f)
                        p.Alpha -= 0.1f * ff;
                    else if (p.Alpha < 0.5f)
                        p.Alpha += (R(3) + 2) * 0.1f * ff;
                    else
                        p.Alpha = 0.5f;
                    if (p.Alpha < 0.1f) keepAlive = false;
                    p.Light = p.TurningForce * p.Alpha;
                    p.Scale += 0.02f * ff;
                    p.Position.Z += p.Gravity * ff;
                    break;

                case 51:
                    l = p.LifeTime / 20f;
                    p.Light = p.TurningForce * l;
                    p.Scale += 0.09f * ff;
                    break;

                case 52:
                    p.Position += p.Velocity * ff;
                    p.Scale += 0.06f * ff;
                    p.Light *= MathF.Pow(1f / 1.07f, ff);
                    break;

                case 55:
                    p.Gravity += 0.1f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.08f, ff);
                    break;

                case 57:
                    l = p.LifeTime / 32f;
                    p.Light = new Vector3(l * 0.5f, l * 0.1f, l * 0.8f);
                    p.Gravity += 0.1f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 61:
                    // Original calcula Luminosity, pero no modifica Light.
                    p.Gravity += 0.2f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.05f * ff;
                    break;

                case 62:
                    l = p.LifeTime / 50f;
                    p.Light = new Vector3(l * 0.9f);
                    p.Velocity *= 0.001f;
                    p.Scale += 0.03f * ff;
                    break;

                case 63:
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    p.Scale += 0.5f * ff;
                    p.Velocity.Y += 1.5f * ff;
                    break;

                case 64:
                    l = p.LifeTime / 24f;
                    p.Light = new Vector3(l * 0.1f, l * 0.7f, l * 0.4f);
                    // El Main mueve esta partícula dos veces:
                    // al comienzo de MoveParticles() y dentro de SubType 64.
                    p.Position = ClassicMath.MovePosition(p.Position, p.Angle, p.Velocity, ff);
                    p.Rotation += 0.05f * ff;
                    p.Gravity -= 0.1f * ff;
                    p.Position.X += p.Gravity * MathF.Sin(worldTime) * 0.05f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += (R(15) + 15) * 0.01f * ff;
                    if (p.LifeTime < 16f) p.Alpha -= 0.1f * ff;
                    break;

                case 65:
                    l = p.LifeTime / 40f;
                    p.Light = new Vector3(l * 0.4f);
                    p.Gravity -= 0.05f * ff;
                    p.Position.X += p.Gravity * MathF.Sin(worldTime) * 0.05f * ff;
                    p.Position.Z += p.Gravity * ff;
                    break;

                case 66:
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    if (p.Light.X <= 0.05f)
                        p.LifeTime = 0f;
                    p.Gravity -= 0.05f * ff;
                    p.Position.X += p.TurningForce.X * (p.Scale + p.Gravity) * ff;
                    p.Position.Y += p.TurningForce.Y * (p.Scale + p.Gravity) * ff;
                    p.Scale += p.Scale * ff / 100f;
                    break;

                case 67:
                    p.Alpha -= 0.01f * ff;
                    if (p.Alpha < 0.1f) keepAlive = false;
                    p.Light *= MathF.Pow(p.Alpha, ff);
                    p.Scale += 0.05f * ff;
                    p.Rotation += 0.01f * ff;
                    break;

                case 68:
                    l = p.LifeTime / 40f;
                    p.Light = new Vector3(l * 0.4f);
                    p.Velocity += p.Velocity * (0.03f * ff);
                    // Segundo MovePosition explícito del Main.
                    p.Position = ClassicMath.MovePosition(p.Position, p.Angle, p.Velocity, ff);
                    if (p.LifeTime < 30f) p.Alpha -= 0.02f * ff;
                    break;

                case 69:
                    // Presente en MuMain bajo ASG_ADD_MAP_KARUTAN.
                    l = p.LifeTime / 50f;
                    p.Light = p.Angle * l;
                    p.Gravity -= 0.04f * ff;
                    p.Position.X += p.Gravity * MathF.Sin(worldTime) * 0.05f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Scale += 0.04f * ff;
                    break;
            }
        }

        private void MoveSmokeDrifting(ref ClassicParticle p, float ff, bool white)
        {
            float l = p.LifeTime / 40f;
            p.Light = new Vector3(l * (white ? 1f : 0.5f));
            p.Gravity += R(20) / 500f * ff;
            p.Position.X += (R(100) - 20) / 100f * ff;
            p.Position.Y += (R(100) - 20) / 100f * ff;
            p.Position.Z += p.Gravity * ff;
            p.Scale += R(10) * ff / 1000f;
            p.Rotation = p.Scale;
        }

        private static void MoveSmokePlainScale(ref ClassicParticle p, float ff)
        {
            switch (p.SubType)
            {
                case 12:
                    p.Light = new Vector3(p.LifeTime / 40f);
                    p.Scale += 0.05f * ff;
                    break;
                case 13:
                    float l13 = p.LifeTime / 20f;
                    p.Light = new Vector3(l13, l13 * 0.5f, l13 * 0.1f);
                    p.Scale += 0.09f * ff;
                    break;
                case 18:
                case 25:
                    p.Light = new Vector3(p.LifeTime / 20f);
                    p.Scale += 0.09f * ff;
                    break;
                case 59:
                    p.Light = new Vector3(p.LifeTime / 40f * 0.9f);
                    p.Scale += 0.09f * ff;
                    break;
                case 48:
                    p.Light = new Vector3(p.LifeTime / 40f * 0.4f);
                    p.Scale += 0.09f * ff;
                    break;
            }
        }
    }
}
