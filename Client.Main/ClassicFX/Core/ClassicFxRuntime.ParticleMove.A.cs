using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Primer tramo de MoveParticles() de sven-n/MuMain:
        /// ZzzEffectParticle.cpp, desde BITMAP_EFFECT hasta BITMAP_CHROME_ENERGY2.
        /// BITMAP_FLARE_BLUE también se maneja aquí; no quedan fallbacks.
        /// true = este Type fue procesado; false = continuar al siguiente bloque.
        /// </summary>
        private bool MoveParticleA(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            float luminosity;
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapEffect:
                {
                    float fade = p.SubType == 2
                        ? MathF.Pow(1f / 1.03f, ff)
                        : MathF.Pow(p.LifeTime >= 10f ? 1.16f : 1f / 1.16f, ff);
                    p.Light *= fade;
                    p.Position.Z += p.Gravity * ff;
                    return true;
                }

                case ClassicTextureIds.BitmapFlower01:
                case ClassicTextureIds.BitmapFlower01 + 1:
                case ClassicTextureIds.BitmapFlower01 + 2:
                {
                    if (p.Frame == 0)
                    {
                        if (p.SubType == 0)
                        {
                            p.Velocity.X += (R(32) - 16) * 0.1f * ff;
                            p.Velocity.Y += (R(32) - 16) * 0.1f * ff;
                            p.Velocity.Z += (R(32) - 16) * 0.1f * ff;
                        }
                        else if (p.SubType == 1)
                        {
                            p.Velocity.X += (R(8) - 4) * 0.05f * ff;
                            p.Velocity.Y += (R(8) - 4) * 0.05f * ff;
                            p.Velocity.Z -= R(8) * 0.05f * ff;
                        }
                        p.Position += p.Velocity * ff;
                    }
                    float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
                    if (p.Position.Z < height)
                    {
                        p.Position.Z = height;
                        p.Velocity = Vector3.Zero;
                        p.Frame = 1;
                    }
                    p.Rotation += R(16) * ff;
                    return true;
                }

                // BITMAP_FLARE_BLUE was previously a provisional fallback in
                // ClassicFxRuntime.Particles.cs. Keep all movement Type cases in A-M.
                case ClassicTextureIds.BitmapFlareBlue:
                {
                    if (p.SubType == 0)
                    {
                        p.Scale = 0.2f;
                        p.Velocity.Z += 0.4f * ff;
                        p.Velocity.Z = MathF.Min(8f * ff, p.Velocity.Z);
                        if (p.LifeTime < 5f)
                        {
                            // Main: 0.98f + (0.02f * 1 - FPS_ANIMATION_FACTOR).
                            p.Light *= 0.98f + (0.02f - ff);
                        }
                    }
                    else if (p.SubType == 1)
                    {
                        p.Light.X *= 0.99f + (0.01f - ff);
                        float fade = MathF.Pow(1f / 1.1f, ff);
                        p.Light.Y *= fade;
                        p.Light.Z *= fade;
                        p.Scale += 1.5f * ff;
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapFlare + 1:
                {
                    if (p.SubType == 0)
                    {
                        float count = (p.Velocity.X + p.LifeTime) * 0.05f;
                        p.Position.X = p.StartPosition.X +
                            MathF.Sin(count) * (105f + p.Scale * -250f);
                        p.Position.Y = p.StartPosition.Y -
                            MathF.Cos(count) * (105f + p.Scale * -250f);
                        p.Position.Z += p.Gravity * ff;
                        p.Scale -= 0.0008f * ff;
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapBlueBlur:
                {
                    if (p.SubType == 0 || p.SubType == 1)
                    {
                        luminosity = p.LifeTime / 20f;
                        p.Light = new Vector3(luminosity);
                        if (p.SubType == 1)
                        {
                            p.Scale += 0.19f * ff;
                            p.Position.Z += 5f * ff;
                        }
                        else
                        {
                            p.Scale += 0.05f * ff;
                        }
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapLight + 2:
                {
                    MoveClassicLight2(ref p, ff, ref keepAlive);
                    return true;
                }

                case ClassicTextureIds.BitmapGmAurora:
                {
                    luminosity = p.LifeTime / 8f * 0.04f;
                    p.Light -= new Vector3(luminosity * ff);
                    return true;
                }

                case ClassicTextureIds.BitmapMagic + 1:
                {
                    p.Light *= MathF.Pow(0.9f, ff);
                    p.Scale += 0.1f * ff;
                    return true;
                }

                case ClassicTextureIds.BitmapBubble:
                {
                    switch (p.SubType)
                    {
                        case 0:
                            p.Frame++;
                            p.Position.X += (R(20) - 10) * 2.5f * p.Scale * ff;
                            p.Position.Y += (R(20) - 10) * 2.5f * p.Scale * ff;
                            p.Position.Z += (R(20) + 10) * 2.5f * p.Scale * ff;
                            break;
                        case 1:
                            p.Frame++;
                            p.Position.X += (R(30) - 15) * p.Scale * ff;
                            p.Position.Y += (R(30) - 15) * p.Scale * ff;
                            p.Position.Z += (R(20) + 10) * 0.3f * p.Scale * ff;
                            p.Scale += 0.002f * ff;
                            break;
                        case 2:
                            p.Position.Z += (R(20) + 10) * 0.3f * ff;
                            p.Scale += 0.005f * ff;
                            break;
                        case 3:
                            p.Frame++;
                            p.Position.X += (R(20) - 10) * 2.5f * p.Gravity * ff;
                            p.Position.Y += (R(20) - 10) * 2.5f * p.Gravity * ff;
                            p.Position.Z += (R(20) + 10) * 2.5f * p.Gravity * ff;
                            break;
                        case 4:
                            p.Position.Z += (R(20) + 10) * 0.3f * ff;
                            p.Scale += 0.005f * ff;
                            if (p.Position.Z > 350f)
                            {
                                p.Position.Z = 350f;
                                p.LifeTime = 0f;
                                // El Main marca Live=false al comienzo del siguiente Move.
                            }
                            break;
                        case 5:
                            p.Frame++;
                            p.Position.X += (R(20) - 10) * 2.5f * p.Scale * ff;
                            p.Position.Y += (R(20) - 10) * 2.5f * p.Scale * ff;
                            p.Position.Z += (R(20) + 10) * 2.5f * p.Scale * ff;
                            if (p.Position.Z > 550f)
                                keepAlive = false;
                            break;
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapExplotionMono:
                case ClassicTextureIds.BitmapExplotion:
                {
                    p.Frame = (int)((20f - p.LifeTime) / 2f);
                    if (p.SubType == 2)
                    {
                        float life = p.LifeTime > 10f
                            ? (p.LifeTime - 10f) * ff
                            : 1f;
                        p.Frame = (int)((20f - life) / 2f);
                        if ((int)p.LifeTime == 20)
                        {
                            p.LifeTime = 19.9f;
                            EmitClassicExplosionRing(ref p, 200f, Vector3.One);
                        }
                        else if ((int)p.LifeTime == 10)
                        {
                            p.LifeTime = 9.9f;
                            EmitClassicExplosionRing(ref p, 400f, new Vector3(0.6f));
                        }
                        else if ((int)p.LifeTime == 1)
                        {
                            p.LifeTime = 0.99f;
                            EmitClassicExplosionRing(ref p, 600f, new Vector3(0.3f));
                        }
                    }
                    if (p.SubType != 1)
                    {
                        luminosity = p.LifeTime / 20f;
                        Vector3 light = new Vector3(
                            luminosity * 0.5f,
                            luminosity * 0.3f,
                            luminosity * 0.1f);
                        AddClassicTerrainLight(p.Position.X, p.Position.Y, light, 4f);
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapSummonSahamutExplosion:
                    p.Frame = (int)(16f - p.LifeTime);
                    return true;

                case ClassicTextureIds.BitmapSpotWater:
                    p.Frame = (int)((32f - p.LifeTime) / 4f);
                    return true;

                case ClassicTextureIds.BitmapExplotion + 1:
                    p.Frame = (int)((12f - p.LifeTime) / 3f);
                    return true;

                case ClassicTextureIds.BitmapLightning + 1:
                {
                    float life = Math.Max(0, (int)p.LifeTime) * ff;
                    p.Rotation = ((int)Clock.WorldTimeMilliseconds % 1000) * 0.001f;
                    luminosity = life / 10f;
                    Vector3 light = new Vector3(
                        luminosity * 0.5f,
                        luminosity,
                        luminosity * 0.8f);
                    AddClassicTerrainLight(p.Position.X, p.Position.Y, light, 3f);
                    if (p.SubType == 2)
                    {
                        p.Scale += 0.1f * ff;
                        p.Light = light;
                        if (TryGetOwnerPosition(p.Target, out Vector3 targetPosition))
                        {
                            p.Position = targetPosition;
                            p.Position.Z += 80f * ff;
                        }
                        else
                        {
                            // El Main desreferencia Target aquí; un handle inválido
                            // no debe provocar un crash ni conservar la partícula.
                            keepAlive = false;
                        }
                    }
                    return true;
                }

                case ClassicTextureIds.BitmapLightning:
                {
                    p.Frame = R(4);
                    luminosity = p.LifeTime / 5f;
                    p.Light = new Vector3(luminosity);
                    AddClassicTerrainLight(
                        p.Position.X, p.Position.Y,
                        new Vector3(-luminosity * 0.6f), 6f);
                    AddClassicTerrainLight(
                        p.Position.X, p.Position.Y,
                        new Vector3(luminosity * 0.2f,
                            luminosity * 0.4f, luminosity), 4f);
                    return true;
                }

                case ClassicTextureIds.BitmapChromeEnergy2:
                    p.Gravity = 0f;
                    p.Scale -= 0.04f * ff;
                    p.Rotation += 5f * ff;
                    p.Frame = (int)((23f - p.LifeTime) / 6f);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// MoveParticle(BITMAP_LIGHT+2), subtypes 3/5/6/7 y default.
        /// No convierte los valores originales de tamaño, color o duración.
        /// </summary>
        private void MoveClassicLight2(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            if (p.SubType == 3)
            {
                p.Gravity += 0.2f * ff;
                p.Scale -= 0.03f * ff;
                if (p.Scale < 0f)
                    keepAlive = false;
                FollowClassicParticleOwner(ref p, ref keepAlive);
                return;
            }

            if (p.SubType == 5 || p.SubType == 6 || p.SubType == 7)
            {
                p.TurningForce.Z -= p.Gravity * ff;
                p.Position += p.TurningForce * ff;
                p.Angle.X += (0.5f * p.LifeTime) * ff;
                p.Angle.Y += (0.5f * p.LifeTime) * ff;
                p.Scale += 0.04f * ff;

                if (p.SubType == 7)
                    p.TexType = ClassicTextureIds.BitmapLight;

                if (p.LifeTime < 10f)
                    p.Alpha -= 0.1f * ff;

                if (p.LifeTime < 20f)
                {
                    float damping = MathF.Pow(0.6f, ff);
                    p.TurningForce.X *= damping;
                    p.TurningForce.Y *= damping;
                }

                if (p.SubType == 5)
                    p.Light = new Vector3(p.Alpha, 0f, p.Alpha * 0.6f);
                else if (p.SubType == 6)
                    p.Light = new Vector3(p.Alpha);
                else
                    p.Light *= p.Alpha;

                if (p.Alpha < 0f)
                    keepAlive = false;

                if (p.SubType == 5)
                    FollowClassicParticleOwner(ref p, ref keepAlive);

                p.Velocity.X += 0.05f * ff;
                p.Velocity.Y += 0.05f * ff;
                p.Rotation += (p.SubType == 5 ? 1f : 2f) * ff;

                if (p.SubType == 7)
                {
                    float luminosity = p.LifeTime / 8f * 0.02f;
                    p.Light += new Vector3(luminosity * ff);
                }
                return;
            }

            float lightGrowth = p.LifeTime / 8f * 0.02f * ff;
            p.Light += new Vector3(lightGrowth);
            p.Gravity += 0.2f * ff;
            p.Position.Z += p.Gravity * ff;
            p.Scale -= 0.1f * ff;
        }

        /// <summary>
        /// Mantiene offset respecto al owner, como VectorSubtract/Copy/Add
        /// del Main. En el WorldBridge actual, World != null es la señal
        /// disponible de que un WorldObject sigue conectado al mundo.
        /// </summary>
        private void FollowClassicParticleOwner(
            ref ClassicParticle p,
            ref bool keepAlive)
        {
            if (!TryGetOwnerPosition(p.Target, out Vector3 targetPosition))
            {
                keepAlive = false;
                return;
            }

            p.Position = (p.Position - p.StartPosition) + targetPosition;
            p.StartPosition = targetPosition;
        }

        /// <summary>
        /// SubType 2 de BITMAP_EXPLOTION: 18 ángulos a intervalos
        /// de 20 grados, en radios originales de 200/400/600 unidades.
        /// Crea humo 35 y explosión 1 por cada dirección.
        /// </summary>
        private void EmitClassicExplosionRing(
            ref ClassicParticle parent,
            float radius,
            Vector3 light)
        {
            for (int j = 0; j < 18; j++)
            {
                Vector3 direction = new Vector3(0f, 0f, j * 20f);
                Vector3 position = ClassicMath.RotateAndAdd(
                    parent.Position, new Vector3(0f, radius, 0f), direction);

                CreateParticle(
                    ClassicTextureIds.BitmapSmoke,
                    position, parent.Angle, light, 35, 2.5f);
                CreateParticle(
                    ClassicTextureIds.BitmapExplotion,
                    position, parent.Angle, light, 1, 1f);
            }
        }
    }
}
