using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Controllers;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// CreateParticle() bloque B: BITMAP_SMOKE ... BITMAP_EXPLOTION+1.
        /// </summary>
        private void InitializeParticleCreateB(
            ref ClassicParticle particle,
            in Vector3 sourcePosition,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale)
        {
            float ff = Clock.FrameFactor;
            float worldTime = (float)Clock.WorldTimeMilliseconds;

            switch (particle.Type)
            {
                case ClassicTextureIds.BitmapSmoke:
                    InitializeSmoke(ref particle, sourceLight, sourceScale, ff, worldTime);
                    break;

                case ClassicTextureIds.BitmapSmoke + 1:
                case ClassicTextureIds.BitmapSmoke + 4:
                    InitializeSmokeVariant(ref particle, sourceAngle, sourceScale, ff);
                    break;

                case ClassicTextureIds.BitmapSmoke + 2:
                {
                    particle.LifeTime = 50f;
                    particle.Scale = (R(32) + 64) * 0.01f;
                    particle.Angle = new Vector3(0f, 0f, R(360));
                    Vector3 p = new Vector3(0f, R(64), R(16) + 16);
                    particle.StartPosition = Rotate(p, particle.Angle);
                    particle.Angle = Vector3.Zero;
                    break;
                }

                case ClassicTextureIds.BitmapSmoke + 3:
                    InitializeSmoke3(ref particle, ff, worldTime);
                    break;

                case ClassicTextureIds.BitmapSmokeLine1:
                case ClassicTextureIds.BitmapSmokeLine2:
                case ClassicTextureIds.BitmapSmokeLine3:
                    InitializeSmokeLine(ref particle, sourceLight, sourceScale, ff);
                    break;

                case ClassicTextureIds.BitmapLightningMega1:
                case ClassicTextureIds.BitmapLightningMega2:
                case ClassicTextureIds.BitmapLightningMega3:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime = 5f;
                        particle.Rotation = R(360);
                        particle.Gravity = 0f;
                        particle.Alpha = 1f;
                        particle.TurningForce = sourceLight;
                        particle.Light = Vector3.Zero;
                    }
                    break;

                case ClassicTextureIds.BitmapFireHik1:
                case ClassicTextureIds.BitmapFireHik1Mono:
                    InitializeFireHik1(ref particle, sourceLight, sourceScale);
                    break;

                case ClassicTextureIds.BitmapFireHik3:
                case ClassicTextureIds.BitmapFireHik3Mono:
                    InitializeFireHik3(ref particle, sourceLight, sourceScale);
                    break;

                case ClassicTextureIds.BitmapLight + 1:
                    particle.LifeTime = 20f + R(8);
                    if (particle.SubType == 1)
                    {
                        particle.LifeTime = 5f;
                        particle.Angle.X = -2f + R(4);
                    }
                    else if (particle.SubType == 2)
                    {
                        particle.LifeTime = 19f;
                        particle.Position.Y += (-30f + R(60)) * ff;
                    }
                    else if (particle.SubType == 3)
                    {
                        particle.LifeTime = 7f;
                        particle.TexType = ClassicTextureIds.BitmapShiny + 1;
                    }
                    else if (particle.SubType == 4)
                    {
                        particle.LifeTime = 4f;
                        particle.Rotation = -360f + R(180);
                        particle.TexType = ClassicTextureIds.BitmapShiny + 1;
                    }
                    else if (particle.SubType == 5)
                    {
                        particle.LifeTime = 3f;
                    }
                    break;

                case ClassicTextureIds.BitmapSpark:
                    InitializeSpark(ref particle, sourceScale);
                    break;

                case ClassicTextureIds.BitmapSpark + 1:
                    InitializeSpark1(ref particle, sourceAngle, sourceLight, sourceScale, ff);
                    break;

                case ClassicTextureIds.BitmapSpark + 2:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime = 16f;
                        particle.Scale = sourceScale + (R(10) - 5) * 0.03f;
                        particle.Position.X += (R(10) - 5) * ff;
                        particle.Position.Y += (R(10) - 5) * ff;
                        particle.Position.Z += (R(10) - 5) * ff;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime = 12f;
                    }
                    else if (particle.SubType == 2 || particle.SubType == 3)
                    {
                        particle.Scale = 0.4f;
                        particle.LifeTime = 16f;
                    }
                    break;

                case ClassicTextureIds.BitmapExplotionMono:
                case ClassicTextureIds.BitmapExplotion:
                    particle.LifeTime = 20f;
                    particle.Scale = sourceScale;
                    if (particle.SubType == 2)
                    {
                        particle.LifeTime = 30f;
                    }

                    if (particle.SubType == 10)
                    {
                        SoundController.Instance.PlayBuffer(
                            "Sound/SE_Ev_rabbit_Explosion.wav");
                    }
                    else if (World?.Scene is not LoginScene)
                    {
                        SoundController.Instance.PlayBuffer(
                            "Sound/eExplosion.wav");
                    }
                    break;

                case ClassicTextureIds.BitmapSummonSahamuttExplosion:
                    particle.LifeTime = 16f;
                    particle.Scale = sourceScale;
                    break;

                case ClassicTextureIds.BitmapSpotWater:
                    particle.LifeTime = 32f;
                    particle.Scale = (R(170) + 1) * 0.01f;
                    break;

                case ClassicTextureIds.BitmapFlareRed:
                    particle.LifeTime = 18f;
                    TryApplyClassicHandPosition(ref particle);
                    break;

                case ClassicTextureIds.BitmapExplotion + 1:
                    particle.LifeTime = 12f;
                    particle.Scale = sourceScale;
                    SoundController.Instance.PlayBuffer(
                        "Sound/eExplosion.wav");
                    break;
            }
        }

        private void InitializeSmoke(
            ref ClassicParticle p,
            in Vector3 sourceLight,
            float sourceScale,
            float ff,
            float worldTime)
        {
            switch (p.SubType)
            {
                case 0:
                case 61:
                case 4:
                case 9:
                case 23:
                    p.LifeTime = 16f;
                    // La condición MODEL_SLAUGHTERER del Main es inalcanzable
                    // dentro del case BITMAP_SMOKE; conservamos su rama efectiva.
                    p.Scale = (R(32) + 48) * 0.01f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 42:
                    p.LifeTime = 16f;
                    p.Scale = (R(32) + 48) * 0.006f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 17:
                    p.TurningForce = p.Light;
                    p.LifeTime = 12f;
                    p.Scale = sourceScale * (R(32) + 8) * 0.01f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 36:
                case 37:
                    if (p.SubType == 37)
                    {
                        p.TurningForce = p.Light;
                        p.Position.Z -= 15f * p.TurningForce.Z * ff;
                        p.Position.Y -= 15f * p.TurningForce.Y * ff;
                        p.Light = Vector3.One;
                    }
                    else
                    {
                        p.Light = new Vector3(1f, 0.3f, 0f);
                    }
                    p.LifeTime = 25f;
                    p.Rotation = R(360);
                    p.Angle.X = R(360);
                    p.StartPosition = new Vector3(
                        R(50) - 25,
                        R(50) - 25,
                        -R(50));
                    break;

                case 38:
                    p.TexType = ClassicTextureIds.BitmapWaterfall2;
                    p.LifeTime = 35f;
                    p.Rotation = R(360);
                    break;

                case 34:
                case 35:
                    p.Light = new Vector3(1f, 0.2f, 0.2f);
                    p.LifeTime = 30f;
                    p.Rotation = R(360);
                    p.Velocity = p.SubType == 35
                        ? new Vector3(0f, -5f, 0f)
                        : Vector3.Zero;
                    break;

                case 33:
                    p.LifeTime = R(150);
                    p.Scale = sourceScale + 0.1f;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -R(8), 0f);
                    break;

                case 32:
                    p.LifeTime = 10f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    break;

                case 10:
                    p.LifeTime = 16f;
                    break;

                case 12:
                case 13:
                case 18:
                    p.LifeTime = 20f;
                    p.Scale = (R(64) + 64) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 3:
                    p.LifeTime = 10f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    break;

                case 11:
                case 14:
                    p.LifeTime = 50f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Position.Z += (R(64) + 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Rotation = R(360);
                    p.TurningForce = p.Light;
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    break;

                case 15:
                    p.LifeTime = 80f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 53:
                case 56:
                case 1:
                    p.LifeTime = 50f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Position.Z += (R(64) + 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    break;

                case 2:
                case 16:
                    p.LifeTime = 50f;
                    p.Scale = (R(64) + 64) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 5:
                    p.LifeTime = 20f;
                    p.Scale = (R(64) + 98) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 6:
                    p.LifeTime = 30f;
                    p.Gravity = R(1000);
                    p.Scale = (R(20) + 180) * 0.01f;
                    p.Position.X += (R(200) - 100) * sourceScale * ff;
                    p.Position.Y += (R(200) - 100) * sourceScale * ff;
                    p.Position.Z += (R(20) + 20) * ff;
                    p.Rotation = p.Position.Z;
                    p.Velocity = Vector3.Zero;
                    break;

                case 7:
                    p.LifeTime = 30f;
                    p.Velocity = new Vector3(
                        0f,
                        (R(4) + 6) * p.Scale,
                        0f);
                    p.Gravity = R(200) / 200f;
                    p.Scale *= (R(20) + 120) * 0.01f;
                    p.Rotation = R(360);
                    break;

                case 8:
                    p.LifeTime = 24f;
                    p.Velocity = new Vector3(0f, -(R(8) + 32) * 0.3f, 0f);
                    p.Scale *= 0.8f;
                    p.Rotation = R(360);
                    break;

                case 19:
                    p.TurningForce = p.Light;
                    p.LifeTime = 40f;
                    p.Scale = sourceScale * (R(32) + 8) * 0.01f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 20:
                    p.LifeTime = 40f;
                    p.Scale = sourceScale * (R(32) + 8) * 0.01f;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 21:
                    p.LifeTime = 80f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 22:
                    p.LifeTime = 60f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 24:
                case 57:
                    p.LifeTime = 32f;
                    p.Scale = sourceScale + (R(32) + 48) * 0.01f * sourceScale;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 25:
                case 26:
                    p.LifeTime = 25f;
                    p.Scale = (R(64) + 64) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 27:
                    p.LifeTime = 30f;
                    p.Rotation = R(360);
                    p.TexType = ClassicTextureIds.BitmapChrome3;
                    break;

                case 28:
                    p.LifeTime = 12f;
                    p.Scale = 2.3f;
                    p.Rotation = R(360);
                    p.Gravity = 33f;
                    p.Position.Z = RequestTerrainHeight(p.Position.X, p.Position.Y) + 15f;
                    p.TexType = ClassicTextureIds.BitmapPoundingBall;
                    break;

                case 29:
                    p.LifeTime = 12f;
                    p.Scale = 2.3f;
                    p.Rotation = R(360);
                    p.Gravity = 33f;
                    p.Position.Z = RequestTerrainHeight(p.Position.X, p.Position.Y) + 15f;
                    break;

                case 30:
                    p.LifeTime = 4f;
                    p.Scale = 1.8f;
                    p.Gravity = 15f;
                    p.Rotation = R(360);
                    p.Position.Z = RequestTerrainHeight(p.Position.X, p.Position.Y) + 15f;
                    p.Position.X += MathF.Sin(worldTime) * 5f * ff;
                    p.Position.Y += MathF.Sin(worldTime) * 5f * ff;
                    break;

                case 31:
                    p.LifeTime = 15f;
                    p.Scale = sourceScale;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 40:
                    p.LifeTime = 50f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Position.Z += (R(64) + 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    break;

                case 41:
                    p.LifeTime = 50f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Position.Z += (R(64) + 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    p.Gravity = (R(100) - 50) * 0.1f;
                    break;

                case 43:
                    p.TexType = ClassicTextureIds.BitmapClud64;
                    p.LifeTime = 40f;
                    p.Rotation = R(360);
                    p.Angle.X = 90f;
                    p.Velocity = new Vector3(R(20) / 10f - 1f, 2f, 15f);
                    break;

                case 44:
                    p.LifeTime = 60f;
                    p.Scale += R(10) / 2f * ff;
                    p.Position.X += (R(500) - 250f) * ff;
                    p.Position.Y += (R(700) - 350f) * ff;
                    p.Position.Z += (R(100) + 150f) * ff;
                    p.Rotation = R(360);
                    p.Gravity = R(50) - 25f;
                    p.Light = new Vector3(0.1f, 0.2f, 0.3f);
                    break;

                case 45:
                    p.TexType = ClassicTextureIds.BitmapLight + 2;
                    p.LifeTime = 15f;
                    p.Scale += R(10) / 20f * ff;
                    p.Position.X += (R(40) - 20f) * ff;
                    p.Position.Y += (R(40) - 20f) * ff;
                    p.Position.Z += (R(40) - 20f) * ff;
                    break;

                case 46:
                    p.LifeTime = 60f + (int)(p.Scale * 10f);
                    p.Scale += R(50) * 0.01f * ff;
                    p.Rotation = R(360);
                    p.Gravity = (R(30) + 50) * 0.05f;
                    break;

                case 47:
                    p.TexType = ClassicTextureIds.BitmapMagic + 1;
                    p.LifeTime = 5f;
                    p.Scale += R(50) * 0.01f * ff;
                    p.Rotation = R(360);
                    break;

                case 59:
                case 48:
                    p.LifeTime = 40f;
                    p.Scale = (R(64) + 64) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 49:
                    p.TexType = ClassicTextureIds.BitmapCloud;
                    p.LifeTime = R(50) + 100;
                    p.Rotation = R(360);
                    p.Scale = sourceScale * 0.4f;
                    p.Angle.X = Random.FpsCheck(2, Clock) ? 1f : -1f;
                    break;

                case 50:
                    p.LifeTime = 20f;
                    p.Scale = (R(32) + 32) * 0.01f * sourceScale;
                    p.Rotation = R(360);
                    p.Gravity = (R(16) + 16) * 0.1f;
                    p.Alpha = 0f;
                    p.TurningForce = sourceLight;
                    p.Light = Vector3.Zero;
                    break;

                case 51:
                    p.LifeTime = 25f;
                    p.Scale = (R(64) + 64) * 0.01f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    p.TurningForce = sourceLight;
                    break;

                case 52:
                {
                    p.LifeTime = 40f;
                    p.Scale = sourceScale + (R(10) + 10) * 0.05f;
                    p.Rotation = R(360);
                    Vector3 local = new Vector3(0f, 10f, 0f);
                    float angle = p.Angle.Z + (R(100) - 50) + 150f;
                    p.Velocity = Rotate(local, new Vector3(0f, 0f, angle));
                    break;
                }

                case 54:
                    p.LifeTime = (int)(p.Scale * 8f);
                    p.Scale += R(50) * 0.01f * ff;
                    p.Rotation = R(360);
                    p.Gravity = (R(30) + 50) * 0.05f;
                    break;

                case 55:
                    p.LifeTime = 30f;
                    p.Scale = sourceScale + (R(32) + 48) * 0.01f * sourceScale;
                    p.Angle.X = R(360);
                    p.Rotation = (int)worldTime % 360;
                    break;

                case 58:
                    p.LifeTime = 50f;
                    p.Scale = (R(32) + 80) * 0.01f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Position.Z += (R(64) + 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -(R(8) + 40), 0f);
                    p.StartPosition = p.Light;
                    break;

                case 60:
                    p.Light = new Vector3(0.4f);
                    p.LifeTime = 60f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 62:
                    p.LifeTime = 50f;
                    p.Position.X += (R(64) - 32) * ff;
                    p.Position.Y += (R(64) - 32) * ff;
                    p.Angle.X = R(90) - 45;
                    p.Angle.Z = R(360);
                    p.Velocity = new Vector3(0f, -R(8), 0f);
                    break;

                case 63:
                    p.TexType = ClassicTextureIds.BitmapClud64;
                    p.LifeTime = 10f;
                    p.Rotation = R(360);
                    p.Angle.X = 90f;
                    p.Velocity = new Vector3(0f, 30f, 15f);
                    break;

                case 64:
                    p.LifeTime = 30f;
                    p.Scale *= 0.3f;
                    p.Velocity = new Vector3(0f, 10f, 0f);
                    p.Angle.X += R(20) + 10;
                    break;

                case 65:
                    p.LifeTime = 45f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(10) + 18) * 0.1f;
                    break;

                case 66:
                {
                    p.LifeTime = (int)(p.Scale * 30f);
                    p.Scale += R(50) * 0.01f * ff;
                    p.Rotation = R(360);
                    p.Gravity = (R(30) + 50) * 0.01f;
                    int iAngle = R(360);
                    p.TurningForce.X = MathF.Sin(iAngle / MathF.PI * 180f);
                    p.TurningForce.Y = MathF.Cos(iAngle / MathF.PI * 180f);
                    break;
                }

                case 67:
                    p.LifeTime = 45f;
                    p.Alpha = 1f;
                    p.Rotation = R(360);
                    p.Gravity = (R(32) + 60) * 0.1f;
                    break;

                case 68:
                {
                    p.LifeTime = 45f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(10) + 18) * 0.1f;
                    p.Alpha = 1f;
                    Vector3 direction = Rotate(Vector3.UnitZ, p.Angle);
                    p.Velocity = direction * 0.6f;
                    break;
                }

                case 69:
                    p.LifeTime = 60f;
                    p.Scale *= (R(64) + 64) * 0.005f;
                    p.Rotation = R(360);
                    p.Gravity = (R(16) + 30) * 0.1f;
                    p.Angle = sourceLight;
                    break;
            }
        }

        private void InitializeSmokeVariant(
            ref ClassicParticle p,
            in Vector3 sourceAngle,
            float sourceScale,
            float ff)
        {
            p.LifeTime = 32f;
            p.Position.X += (R(16) - 8) * ff;
            p.Position.Y += (R(16) - 8) * ff;

            if (p.Type == ClassicTextureIds.BitmapSmoke + 4)
            {
                p.Scale = (R(32) + 32) * 0.01f;
                p.Scale *= sourceScale;
            }
            else
            {
                p.Scale = (R(32) + 32) * 0.01f * sourceScale;
            }

            Vector3 local;
            Vector3 rotationAngle;
            if (p.SubType == 0)
            {
                rotationAngle = sourceAngle;
                local = new Vector3(0f, 3f, 0f);
            }
            else
            {
                p.Angle.Z = R(360);
                rotationAngle = p.Angle;
                local = new Vector3(0f, 15f, 0f);
            }

            p.Velocity = Rotate(local, rotationAngle);
            p.Angle.X = R(360);

            if (p.SubType == 1 || p.SubType == 6)
            {
                p.LifeTime = 40f;
                p.Velocity = Vector3.Zero;
                p.Position.Z += (R(16) - 8) * ff;
            }
        }

        private void InitializeSmoke3(
            ref ClassicParticle p,
            float ff,
            float worldTime)
        {
            switch (p.SubType)
            {
                case 0:
                case 2:
                    p.TurningForce = p.Light;
                    p.LifeTime = 55f;
                    p.Angle.X = -2f + R(4);
                    p.Rotation = (int)worldTime % 360;
                    break;
                case 1:
                    p.TurningForce = p.Light;
                    p.LifeTime = 30f;
                    p.Angle.X = -2f + R(4);
                    p.Rotation = (int)worldTime % 360;
                    break;
                case 3:
                    p.TexType = ClassicTextureIds.BitmapClud64;
                    p.LifeTime = 60f;
                    p.Scale *= (R(64) + 64) * 0.02f;
                    p.Rotation = R(360);
                    p.Position.X += (R(250) - 125) * ff;
                    p.Position.Y += (R(250) - 125) * ff;
                    p.Position.Z -= R(45) * ff;
                    p.Gravity = (R(100) - 50) * 0.1f;
                    p.Light = Vector3.One;
                    break;
                case 4:
                    p.TexType = ClassicTextureIds.BitmapWaterfall3;
                    p.LifeTime = 30f;
                    p.Scale *= (R(64) + 64) * 0.02f;
                    p.Rotation = R(360);
                    p.Position.X += (R(250) - 125) * ff;
                    p.Position.Y += (R(250) - 125) * ff;
                    p.Position.Z -= R(45) * ff;
                    p.Gravity = (R(100) - 50) * 0.1f;
                    p.Light = Vector3.One;
                    break;
            }
        }

        private void InitializeSmokeLine(
            ref ClassicParticle p,
            in Vector3 sourceLight,
            float sourceScale,
            float ff)
        {
            if (p.SubType == 0 || p.SubType == 5)
            {
                p.LifeTime = World?.WorldIndex == 63 ? 25f : 35f;
                p.Scale = (R(96) + 96) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(16) + 12) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
                if (p.SubType == 5)
                {
                    CopyTargetPositionToStart(ref p);
                }
            }
            else if (p.SubType == 1)
            {
                p.LifeTime = 25f;
                p.Scale = (R(50) + 55) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(4) + 35) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
                CopyTargetPositionToStart(ref p);
            }
            else if (p.SubType == 2 || p.SubType == 3)
            {
                p.LifeTime = 45f;
                p.Scale = (R(96) + 96) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(16) + 12) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 4)
            {
                p.Position.X += (R(20) - 10) * 2f * ff;
                p.Position.Y += (R(20) - 10) * 2f * ff;
                p.Position.Z += (R(20) - 10) * 2f * ff;
                p.Scale += (R(20) - 10) * (p.Scale * 0.03f);
                p.LifeTime = 45f;
                p.Alpha = 1f;
                p.Gravity = 0.2f;
                p.Rotation = R(360);
            }
        }

        private void InitializeFireHik1(
            ref ClassicParticle p,
            in Vector3 sourceLight,
            float sourceScale)
        {
            if (p.SubType == 0 || p.SubType == 6)
            {
                p.LifeTime = R(5) + 27;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 64) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 1)
            {
                p.LifeTime = R(5) + 54;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 74) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 2)
            {
                p.LifeTime = R(5) + 27;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 100) * 0.1f;
                float fRand = R(50) * 0.03f;
                p.Velocity = new Vector3(2.5f + fRand, -7f - fRand, 0f);
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 3)
            {
                p.LifeTime = R(5) + 27;
                p.Scale = (R(72) + 52) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(14) + 44) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 4)
            {
                p.LifeTime = 8f;
                p.Scale = (R(42) + 12) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(14) + 44) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
                CopyTargetPositionToStart(ref p);
            }
            else if (p.SubType == 5)
            {
                p.LifeTime = R(5) + 27;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 64) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }

            if (p.SubType == 10)
            {
                p.LifeTime = R(5) + 47;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 64) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
        }

        private void InitializeFireHik3(
            ref ClassicParticle p,
            in Vector3 sourceLight,
            float sourceScale)
        {
            if (p.SubType == 0 || p.SubType == 6)
            {
                p.LifeTime = R(5) + 17;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 64) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 1)
            {
                p.LifeTime = R(5) + 34;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 74) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 2)
            {
                p.LifeTime = R(5) + 17;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(24) + 100) * 0.1f;
                float fRand = R(50) * 0.03f;
                p.Velocity = new Vector3(2.5f + fRand, -5f - fRand, 0f);
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 3)
            {
                p.LifeTime = R(5) + 17;
                p.Scale = (R(72) + 52) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(14) + 44) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 4)
            {
                p.LifeTime = R(5) + 34;
                p.Scale = (R(72) + 72) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(10) + 40) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 5)
            {
                p.LifeTime = 8f;
                p.Scale = (R(42) + 12) * 0.01f * sourceScale;
                p.Rotation = R(360);
                p.Gravity = (R(14) + 44) * 0.1f;
                p.Alpha = 0f;
                p.TurningForce = sourceLight;
                p.Light = Vector3.Zero;
                CopyTargetPositionToStart(ref p);
            }
        }

        private void InitializeSpark(
            ref ClassicParticle p,
            float sourceScale)
        {
            p.Scale = (R(4) + 4) * 0.1f;

            if (p.SubType == 0 || p.SubType == 2 || p.SubType == 3 ||
                p.SubType == 4 || p.SubType == 6 || p.SubType == 11 ||
                p.SubType == 13)
            {
                p.LifeTime = R(16) + 24;
                p.Angle.Z = R(360);
                p.Gravity = R(16) + 6;
                Vector3 local = new Vector3(0f, (R(20) + 20) * 0.1f, 0f);

                if (p.SubType == 2)
                {
                    p.Scale *= 2f;
                    local *= 3f;
                }
                else if (p.SubType == 4)
                {
                    p.Scale *= 3f;
                    local *= 10f;
                }
                else if (p.SubType == 6)
                {
                    p.LifeTime = R(4) + 4;
                    p.Scale = sourceScale;
                    p.Gravity = 0f;
                    p.TexType = ClassicTextureIds.BitmapSpark + 1;
                    local = new Vector3(0f, (R(20) + 20) * 0.2f, 0f);
                }
                else if (p.SubType == 11)
                {
                    p.Light = new Vector3(1f, 0.3f, 0.3f);
                }
                else if (p.SubType == 13)
                {
                    p.Scale *= 1.6f;
                    local *= 1.5f;
                    p.TexType = ClassicTextureIds.BitmapSpark + 1;
                }

                p.Velocity = Rotate(local, p.Angle);
            }
            else if (p.SubType == 7)
            {
                p.LifeTime = R(12) + 12;
            }
            else if (p.SubType == 8 || p.SubType == 10)
            {
                p.TexType = p.SubType == 10
                    ? ClassicTextureIds.BitmapClud64
                    : ClassicTextureIds.BitmapSpark + 1;
                p.LifeTime = R(16) + 24;
                p.Angle.Z = R(360);
                p.Gravity = R(16) + 6;
                Vector3 local = new Vector3(0f, (R(60) - 30) * 0.1f, 0f);
                p.Scale *= 1.5f;
                local *= 3f;
                p.Velocity = Rotate(local, p.Angle);
            }
            else if (p.SubType == 9)
            {
                p.Scale = sourceScale * 1.2f + (R(4) + 4) * 0.1f;
                p.LifeTime = R(16) + 50;
                p.Angle.Z = R(360);
                p.Gravity = R(2);
                Vector3 local = new Vector3(0f, (R(10) + 10) * 0.05f, 0f);
                p.Velocity = Rotate(local, p.Angle);
            }
            else if (p.SubType == 12)
            {
                p.Scale = sourceScale + (R(4) + 4) * 0.1f;
                p.LifeTime = R(16) + 24;
                p.Angle.Z = R(360);
                p.Gravity = R(16) + 6;
                Vector3 local = new Vector3(0f, (R(20) + 20) * 0.1f, 0f);
                p.Velocity = Rotate(local, p.Angle);
            }
            else
            {
                if (p.SubType == 5)
                {
                    CopyTargetPositionToStart(ref p);
                    p.Scale = sourceScale;
                    p.LifeTime = R(4) + 12;
                    p.Gravity = 0f;
                    p.TexType = ClassicTextureIds.BitmapSpark + 1;
                }
                else
                {
                    p.LifeTime = R(8) + 8;
                }
            }
        }

        private void InitializeSpark1(
            ref ClassicParticle p,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale,
            float ff)
        {
            switch (p.SubType)
            {
                case 1:
                    p.Angle = new Vector3(R(360), R(360), R(360));
                    p.Velocity = Rotate(new Vector3(0f, -50f, 0f), p.Angle);
                    break;

                case 2:
                    p.LifeTime = 15f;
                    p.Angle = Vector3.Zero;
                    p.StartPosition = p.Position;
                    SetVelocityTowardTarget(ref p, 10f);
                    break;

                case 3:
                    p.LifeTime = 5f;
                    break;

                case 4:
                    p.LifeTime = 10f;
                    p.Alpha = 0.1f;
                    p.Angle = Vector3.Zero;
                    p.StartPosition = p.Position;
                    SetVelocityTowardTarget(ref p, 6f);
                    break;

                case 5:
                    p.LifeTime = 10f;
                    p.Angle = new Vector3(0f, 0f, sourceAngle.Z);
                    p.Velocity = Rotate(new Vector3(-(R(2) + 2f), 0f, 0f), p.Angle);
                    break;

                case 6:
                    if (Random.FpsCheck(50, Clock))
                    {
                        p.LifeTime = 50f;
                        p.Gravity = 1f + R(20) / 5f;
                        p.Scale = R(5) / 10f + 1f;
                    }
                    else
                    {
                        p.LifeTime = 0f;
                    }
                    break;

                case 7:
                    p.Alpha = 1f;
                    p.Position.X += (-30f + R(60)) * ff;
                    p.Position.Y += (-30f + R(60)) * ff;
                    p.Position.Z += (-30f + R(60)) * ff;
                    p.LifeTime = 30f;
                    p.Gravity = 1.3f;
                    p.Scale = R(5) / 10f + 1f;
                    break;

                case 8:
                    p.LifeTime = 10f;
                    break;

                case 9:
                {
                    p.LifeTime = 30f;
                    p.Angle = new Vector3(0f, R(360), 0f);
                    Vector3 offset = Rotate(new Vector3(0f, 0f, R(10) + 160f), p.Angle);
                    p.Position += offset;
                    p.Velocity = new Vector3(12f, 0f, -2f);
                    break;
                }

                case 10:
                    p.Alpha = 1f;
                    p.Velocity = new Vector3(-4f + R(8), 0f, -4f + R(8));
                    p.LifeTime = 20f;
                    p.Scale = R(5) / 10f + 1f;
                    break;

                case 13:
                case 11:
                    p.Alpha = 1f;
                    p.Velocity = new Vector3(-4f + R(8), R(4), R(4));
                    p.LifeTime = 30f;
                    p.Scale = R(5) / 10f + 1f;
                    break;

                case 12:
                {
                    p.TexType = ClassicTextureIds.BitmapShiny;
                    p.Alpha = 1f;
                    Vector3 offset = Rotate(new Vector3(0f, -100f, 0f), p.Angle);
                    p.Position += offset * ff;
                    p.Position.X += (-8f + R(16)) * ff;
                    p.Position.Y += (-8f + R(16)) * ff;
                    p.Position.Z += 150f * ff;
                    p.Velocity = new Vector3(
                        -1f + R(20) / 10f,
                        -1f + R(20) / 10f,
                        -R(3) - 3f);
                    p.LifeTime = 40f;
                    p.Scale += -R(15) / 10f * ff;
                    break;
                }

                case 14:
                    if (Random.FpsCheck(2, Clock))
                    {
                        p.Alpha = 1f;
                        p.Velocity = new Vector3(-4f + R(4), R(4), R(4));
                        p.LifeTime = 30f;
                        p.Scale = R(5) / 10f + 0.4f;
                    }
                    break;

                case 15:
                    if (Random.FpsCheck(3, Clock))
                    {
                        p.Alpha = 1f;
                        p.LifeTime = 35f;
                        p.Velocity.Z = -(R(5) + 5) * 0.5f;
                    }
                    break;

                case 16:
                case 18:
                {
                    p.Alpha = 1f;
                    p.LifeTime = p.SubType == 18 ? 80f : 100f;
                    p.Frame = 77;
                    p.Scale = -R(3) / 3f + sourceScale + 0.01f;
                    p.Velocity.X = R(50) / 10f - 2.5f;
                    p.Velocity.Y = R(4) / 10f - 0.2f;
                    if (p.SubType == 18)
                    {
                        p.Velocity.Y = R(50) / 10f - 2.5f;
                    }
                    p.Velocity.Z = R(50) / 10f - 2.5f;
                    Vector3 local = new Vector3(
                        p.Velocity.X * 10f,
                        p.Velocity.Y * 10f,
                        p.Velocity.Z * 10f + 100f);
                    p.Position += Rotate(local, p.Angle) * ff;
                    p.Gravity = 3.5f;
                    break;
                }

                case 17:
                    p.LifeTime = 200f;
                    p.Scale += R(3) * 0.1f * ff;
                    p.Gravity = R(5) * 0.3f + 2f;
                    p.Rotation = R(20) * 0.0001f + 0.002f;
                    p.Alpha = R(10) * 0.1f + 0.5f;
                    p.StartPosition = p.Position;
                    break;

                case 19:
                    p.LifeTime = 50f;
                    p.Gravity = 1f + R(20) * 0.1f;
                    p.Position.Z = RequestTerrainHeight(p.Position.X, p.Position.Y) - 5f;
                    p.Scale = R(10) * 0.08f + 0.8f;
                    p.Light = sourceLight;
                    break;

                case 20:
                {
                    p.Scale = sourceScale + R(10) * 0.02f;
                    p.LifeTime = R(10) + 60;
                    p.Angle.Z = R(360);
                    p.Gravity = R(10) + 15;
                    Vector3 local = new Vector3((R(40) - 20) * 0.1f, (R(40) - 20) * 0.1f, 0f) * 2.5f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }

                case 21:
                    p.Velocity = RandomNormalizedVector(12f);
                    p.LifeTime = R(10) + 20;
                    p.Scale = R(20) / 20f + 1f;
                    break;

                case 22:
                {
                    // El Main genera primero una dirección normalizada y la
                    // sobreescribe después con el segundo VectorRotate.
                    _ = RandomNormalizedVector(14f);
                    p.Scale = R(20) / 20f + 1f;
                    p.LifeTime = R(10) + 20;
                    p.Angle.Z = R(360);
                    p.Gravity = 20f;
                    Vector3 local = new Vector3((R(40) - 20) * 0.1f, (R(40) - 20) * 0.1f, 0f) * 2.5f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }

                case 23:
                    p.Alpha = 1f;
                    p.Velocity = new Vector3(
                        (-1 + R(3)) * 1.5f,
                        (-1 + R(3)) * 1.5f,
                        (-1 + R(3)) * 1.5f);
                    p.LifeTime = 20f;
                    p.Rotation = 0f;
                    p.Scale = R(5) / 10f + 0.8f;
                    break;

                case 24:
                    p.Alpha = 1f;
                    p.Velocity = new Vector3(
                        (-1 + R(3)) * 2f,
                        (-1 + R(3)) * 2f,
                        (-1 + R(3)) * 2f);
                    p.LifeTime = 16f;
                    p.Rotation = 0f;
                    p.Scale = R(5) / 10f + 0.4f;
                    break;

                case 25:
                    p.Alpha = 1f;
                    p.Velocity = new Vector3(-2f + R(6), R(4), R(4));
                    p.LifeTime = 20f;
                    break;

                case 26:
                    p.LifeTime = 20f;
                    p.Angle = Vector3.Zero;
                    p.StartPosition = p.Position;
                    SetVelocityTowardTarget(ref p, 20f);
                    break;

                case 27:
                    p.Velocity = RandomNormalizedVector(12f);
                    p.LifeTime = R(10) + 10;
                    p.Scale = R(20) / 20f + 1f;
                    break;

                case 28:
                {
                    p.Scale = R(20) / 20f + 1f;
                    p.LifeTime = R(10) + 20;
                    p.Angle.Z = R(360);
                    p.Gravity = 20f;
                    Vector3 local = new Vector3((R(40) - 20) * 0.1f, (R(40) - 20) * 0.1f, 0f) * 2.5f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }

                case 29:
                {
                    p.Scale = R(20) / 20f + 0.5f;
                    p.LifeTime = R(10) + 10;
                    p.Angle.Z = R(360);
                    p.Gravity = -20f;
                    Vector3 local = new Vector3((R(40) - 20) * 0.1f, (R(40) - 20) * 0.1f, 0f) * 2.5f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }

                case 30:
                    p.LifeTime = R(5) + 50;
                    p.Scale = R(10) / 10f + 0.2f;
                    p.Velocity = new Vector3(-2f + R(5), -2f + R(5), -2f + R(5));
                    p.Gravity = 0.5f;
                    break;

                case 31:
                {
                    p.Scale = (R(4) + 4) * 0.1f;
                    p.LifeTime = R(16) + 24;
                    p.Angle.Z = R(360);
                    p.Gravity = R(16) + 6;
                    Vector3 local = new Vector3(0f, (R(20) + 20) * 0.1f, 0f) * 3f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }
            }
        }

        private void SetVelocityTowardTarget(
            ref ClassicParticle p,
            float divisor)
        {
            if (TryGetOwnerPosition(p.Target, out Vector3 targetPosition))
            {
                p.Velocity = (targetPosition - p.Position) / divisor;
            }
        }

        private Vector3 RandomNormalizedVector(float magnitude)
        {
            Vector3 v = new Vector3(
                -8f + R(16),
                -8f + R(16),
                -8f + R(16));
            ClassicMath.VectorNormalize(ref v);
            return v * magnitude;
        }
    }
}
