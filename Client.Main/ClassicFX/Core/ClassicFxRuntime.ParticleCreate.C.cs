using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// CreateParticle() bloque C: BITMAP_SHINY ... BITMAP_WATERFALL_1.
        /// </summary>
        private void InitializeParticleCreateC(
            ref ClassicParticle p,
            in Vector3 sourcePosition,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale)
        {
            float ff = Clock.FrameFactor;

            switch (p.Type)
            {
                case ClassicTextureIds.BitmapShiny:
                    InitializeShiny(ref p, sourceScale, ff);
                    break;

                case ClassicTextureIds.BitmapCherryBlossomEventPetal:
                    if (p.SubType == 0)
                    {
                        p.Alpha = 1f;
                        p.LifeTime = 30f;
                        p.Scale = R(10) / 20f + 0.5f;
                        p.Rotation = R(360);
                        p.Gravity = -(R(10) + 10f);
                        p.Angle = Vector3.Zero;
                        p.Velocity.Z = -4f + R(8);
                        p.Velocity.Y = R(4);
                        p.Velocity.X = R(4);
                    }
                    else if (p.SubType == 1)
                    {
                        p.Alpha = 1f;
                        p.LifeTime = 70f;
                        p.Velocity.X = -2f + R(6);
                        p.Velocity.Y = R(4);
                        p.Velocity.Z = R(4);
                        p.Rotation = R(360);
                    }
                    break;

                case ClassicTextureIds.BitmapCherryBlossomEventFlower:
                    if (p.SubType == 0)
                    {
                        // El Main genera primero una dirección normalizada, pero
                        // después la pisa con el VectorRotate siguiente.
                        _ = RandomNormalizedVector(14f);
                        p.Alpha = 1f;
                        p.LifeTime = R(30) + 20;
                        p.Angle.Z = R(360);
                        p.Rotation = R(360);
                        p.Gravity = 20f;
                        Vector3 local =
                            new Vector3(
                                (R(40) - 20) * 0.1f,
                                (R(40) - 20) * 0.1f,
                                0f) * 2.5f;
                        p.Velocity = Rotate(local, p.Angle);
                    }
                    break;

                case ClassicTextureIds.BitmapShiny + 1:
                    if (p.SubType == 5)
                    {
                        p.LifeTime = 14f;
                        p.Rotation = R(360);
                        p.Gravity = 0f;
                        p.Velocity = Vector3.Zero;
                    }
                    else
                    {
                        p.LifeTime = 36f;
                        p.Angle.X = 45f;
                        if (p.SubType != 99)
                        {
                            TryApplyClassicHandPosition(ref p);
                        }
                    }
                    break;

                case ClassicTextureIds.BitmapShiny + 2:
                    p.LifeTime = 18f;
                    TryApplyClassicHandPosition(ref p);
                    break;

                case ClassicTextureIds.BitmapShiny + 4:
                    p.LifeTime = 20f;
                    if (p.SubType == 1)
                    {
                        p.LifeTime = 15f;
                        p.Gravity = 0f;
                        p.Rotation = R(360);
                    }
                    else if (p.SubType == 2)
                    {
                        p.LifeTime = 8f;
                    }
                    break;

                case ClassicTextureIds.BitmapShiny + 6:
                    if (p.SubType == 0)
                    {
                        p.Alpha = 1f;
                        p.Position.X += (-5f + R(10)) * ff;
                        p.Position.Y += (-5f + R(10)) * ff;
                        p.Position.Z += (-5f + R(10)) * ff;
                        p.LifeTime = 30f;
                        p.Gravity = 1.3f;
                        p.Scale += R(3) / 10f * ff;
                    }
                    else if (p.SubType == 1)
                    {
                        p.Alpha = 1f;
                        p.LifeTime = R(3) + 10;
                        p.Scale = 0.3f;
                        p.Position.X += (R(50) - 25) * ff;
                        p.Position.Y += (R(50) - 25) * ff;
                        p.Position.Z += (R(50) - 25) * ff;
                        p.Rotation = R(20) - 10;
                    }
                    break;

                case ClassicTextureIds.BitmapPinLight:
                    p.Alpha = 1f;
                    p.Position.X += (-5f + R(10)) * ff;
                    p.Position.Y += (-5f + R(10)) * ff;
                    p.Position.Z += (-5f + R(10)) * ff;
                    p.LifeTime = 30f;
                    p.Gravity = 10f;
                    p.Scale += R(5) / 10f * ff;
                    if (p.SubType == 2)
                    {
                        p.Gravity = -5f;
                    }
                    else if (p.SubType == 3)
                    {
                        p.Gravity = -5f;
                        p.TexType = ClassicTextureIds.BitmapShiny + 6;
                        p.Rotation = R(360);
                    }
                    break;

                case ClassicTextureIds.BitmapOrora:
                    p.Scale = 0.2f;
                    if (p.SubType == 0 || p.SubType == 1)
                    {
                        p.LifeTime = 100f;
                    }
                    else if (p.SubType == 2 || p.SubType == 3)
                    {
                        p.LifeTime = 25f;
                    }
                    break;

                case ClassicTextureIds.BitmapSnowEffect1:
                case ClassicTextureIds.BitmapSnowEffect2:
                {
                    p.Scale = sourceScale + R(10) * 0.02f;
                    p.LifeTime = R(10) + 30;
                    p.Angle.Z = R(360);
                    p.Gravity = R(10) + 20;
                    Vector3 local =
                        new Vector3(
                            (R(40) - 20) * 0.1f,
                            (R(40) - 20) * 0.1f,
                            0f) * 1.5f;
                    p.Velocity = Rotate(local, p.Angle);
                    break;
                }

                case ClassicTextureIds.BitmapDsEffect:
                    p.Scale = sourceScale + R(10) * 0.02f;
                    p.LifeTime = 100f;
                    break;

                case ClassicTextureIds.BitmapBlood:
                case ClassicTextureIds.BitmapBlood + 1:
                {
                    p.LifeTime = 12f;
                    p.Scale = (R(4) + 8) * 0.05f;
                    p.Angle.X = R(360);
                    Vector3 local =
                        new Vector3(
                            0f,
                            -(R(16) + 8),
                            R(6) - 3);
                    p.Velocity = Rotate(local, sourceAngle);
                    if (p.Type == ClassicTextureIds.BitmapBlood + 1)
                    {
                        p.Light = new Vector3(0.1f, 0f, 0f);
                    }
                    break;
                }

                case ClassicTextureIds.BitmapFirecracker:
                    p.Angle = Vector3.Zero;
                    p.Rotation = R(360);
                    if (p.SubType == -1)
                    {
                        p.LifeTime = 2f;
                        p.Scale = 0.25f;
                    }
                    else
                    {
                        int size = 15 + p.SubType;
                        if (size <= 0)
                        {
                            size = 1;
                        }
                        p.Velocity.X = (R(size) - size / 2) * 0.15f;
                        p.Velocity.Y = (R(size) - size / 2) * 0.15f;
                        p.Velocity.Z = (R(size) - size / 2) * 0.15f + 10f;
                        p.LifeTime = 50f;
                        p.Scale = 0.10f;
                    }
                    break;

                case ClassicTextureIds.BitmapSwordForce:
                    p.LifeTime = 10f;
                    p.Gravity = 0.1f;
                    p.Rotation = p.Angle.Z + 135f;
                    break;

                case ClassicTextureIds.BitmapCloud:
                    InitializeCloud(ref p, sourcePosition, sourceScale, ff);
                    break;

                case ClassicTextureIds.BitmapTorchFire:
                    p.LifeTime = R(5) + 60;
                    p.Scale = (R(30) + 30) * 0.02f + sourceScale / 3f;
                    p.Position.X += (R(4) - 2) * 0.25f * ff;
                    p.Position.Y += (R(4) - 2) * 0.25f * ff;
                    p.Position.Z += (R(10) - 5) * ff;
                    p.Gravity = (R(2) + 10) * 0.5f;
                    break;

                case ClassicTextureIds.BitmapGhostCloud1:
                case ClassicTextureIds.BitmapGhostCloud2:
                    if (p.SubType == 0)
                    {
                        p.LifeTime = 500f;
                        p.Position.X += (R(400) - 200) * ff;
                        p.Position.Y += (R(400) - 200) * ff;
                        p.Position.Z += (R(400) - 200) * ff;
                        p.StartPosition = p.Position;
                        p.Scale = (R(15) + 15) / 30f + sourceScale;
                        float temp = (R(10) + 5) * 0.08f;
                        p.Velocity = new Vector3(temp, temp, 0f);
                        p.TurningForce = p.Light;
                        p.Light = Vector3.Zero;
                    }
                    break;

                case ClassicTextureIds.BitmapLight:
                    InitializeLight(ref p, sourcePosition, sourceLight, sourceScale);
                    break;

                case ClassicTextureIds.BitmapPoundingBall:
                    InitializePoundingBall(ref p, sourceLight, sourceScale);
                    break;

                case ClassicTextureIds.BitmapAdvSmoke:
                    p.LifeTime = 20f + R(5);
                    p.Rotation = 0f;
                    if (p.SubType == 0)
                    {
                        p.Scale = 0.5f + R(10) * 0.02f;
                        p.Velocity = new Vector3(
                            (R(10) + 5) * 0.4f,
                            (R(10) - 5) * 0.4f,
                            (R(10) + 5) * 0.2f);
                    }
                    else if (p.SubType == 2)
                    {
                        p.Scale = sourceScale * 0.5f + R(10) * 0.02f;
                        p.Velocity = new Vector3(
                            (R(10) + 5) * 0.4f,
                            (R(10) - 5) * 0.4f,
                            (R(10) + 5) * 0.2f);
                    }
                    else if (p.SubType == 3)
                    {
                        p.Scale = sourceScale * 0.5f + R(10) * 0.02f;
                        p.Alpha = 0.5f;
                        p.Velocity = new Vector3(
                            0f,
                            (R(10) - 5) * 0.4f,
                            0f);
                    }
                    else
                    {
                        p.Scale = 1f + R(10) * 0.1f;
                        p.Velocity = new Vector3(
                            (R(10) + 5) * 0.2f,
                            (R(10) - 5) * 0.2f,
                            (R(10) + 5) * 0.1f);
                    }
                    break;

                case ClassicTextureIds.BitmapAdvSmoke + 1:
                    p.LifeTime = 25f + R(5);
                    p.Rotation = R(360);
                    p.Scale = 0.5f;
                    if (p.SubType == 1)
                    {
                        p.Scale *= sourceScale;
                    }
                    p.Velocity.X = (R(10) + 5) * 0.4f;
                    p.Velocity.Y = 0f;
                    p.Velocity.Z = (R(10) + 5) * 0.2f;
                    break;

                case ClassicTextureIds.BitmapTrueFire:
                case ClassicTextureIds.BitmapTrueBlue:
                    if (p.SubType == 7)
                    {
                        p.Scale = sourceScale + R(30) / 100f;
                        p.LifeTime = 15f;
                        p.Position.X += (R(10) / 10f - 0.5f) * ff;
                        p.Position.Y += (R(10) / 10f - 0.5f) * ff;
                        p.Position.Z += (R(10) / 10f - 0.5f) * ff;
                    }
                    else if (p.SubType == 5)
                    {
                        p.LifeTime = 20f;
                    }
                    else if (p.SubType == 6)
                    {
                        p.LifeTime = 32f;
                    }
                    else if (p.SubType == 8)
                    {
                        p.LifeTime = 15f;
                    }
                    else if (p.SubType == 9)
                    {
                        p.LifeTime = 20f;
                    }
                    else
                    {
                        p.LifeTime = 24f;
                    }

                    if (p.SubType == 8 || p.SubType == 9)
                    {
                        p.Velocity = new Vector3(
                            (R(4) - 2) * 0.4f,
                            0f,
                            (R(4) + 2) * 0.2f);
                    }
                    else
                    {
                        p.Velocity = new Vector3(
                            (R(10) - 5) * 0.4f,
                            0f,
                            (R(10) + 5) * 0.2f);
                    }
                    p.StartPosition = p.Position;
                    break;

                case ClassicTextureIds.BitmapHole:
                    p.LifeTime = 30f;
                    p.Rotation = R(360);
                    p.Light = new Vector3(0.1f);
                    break;

                case ClassicTextureIds.BitmapWaterfall1:
                    p.LifeTime = 30f;
                    p.Rotation = R(360);
                    p.Velocity.Z = -(R(5) + 7);
                    p.Light = new Vector3(0.2f);
                    if (p.SubType == 0)
                    {
                        p.Scale = 1.6f;
                    }
                    else if (p.SubType == 1)
                    {
                        p.Scale = (R(20) + 80) * 0.01f;
                        p.TexType = ClassicTextureIds.BitmapCloud + 2;
                    }
                    else if (p.SubType == 2)
                    {
                        p.Scale = 1.6f + sourceScale;
                    }
                    break;
            }
        }

        private void InitializeShiny(
            ref ClassicParticle p,
            float sourceScale,
            float ff)
        {
            p.LifeTime = 18f;
            p.Angle.X = 45f;

            if (p.SubType == 2)
            {
                p.LifeTime = 25f;
                p.Rotation = R(360);
                p.Velocity = new Vector3(-0.1f, -0.5f, -1f);
            }
            else if (p.SubType == 3)
            {
                p.Alpha = 1f;
                p.Velocity = new Vector3(-4f + R(8), R(4), R(4));
                p.LifeTime = 30f;
                p.Scale = R(10) / 20f + 0.5f;
            }
            else if (p.SubType == 4)
            {
                p.Scale = sourceScale + R(10) * 0.02f;
                p.LifeTime = R(10) + 15;
                p.Angle.Z = R(360);
                p.Gravity = R(10) + 10;
                Vector3 local = new Vector3(
                    (R(40) - 20) * 0.1f,
                    (R(40) - 20) * 0.1f,
                    0f) * 2f;
                p.Velocity = Rotate(local, p.Angle);
            }
            else if (p.SubType == 5)
            {
                p.LifeTime = R(10) + 10;
                p.Scale += R(10) * 0.02f * ff;
                p.Position.X += (R(10) - 5) * ff;
                p.Position.Y += (R(10) - 5) * ff;
                p.Gravity = -R(10) * 0.3f;
            }
            else if (p.SubType == 6 || p.SubType == 9)
            {
                p.Scale = R(5) / 10f + 0.5f;
                p.StartPosition.X = p.Scale;
                p.LifeTime = R(10) + 60;
                p.Gravity = 20f;
                p.Angle = new Vector3(R(360), R(360), R(360));
                p.Velocity = Rotate(new Vector3(8f), p.Angle);
                p.Rotation = R(20) - 10;
            }
            else if (p.SubType == 7)
            {
                p.Scale = 0.1f;
                p.StartPosition.X = p.Scale;
                p.LifeTime = R(10) + 20;
                p.Position.X += (R(50) - 25) * ff;
                p.Position.Y += (R(50) - 25) * ff;
                p.Position.Z += (R(50) - 25) * ff;
                p.Gravity = 1f;
                p.Angle = new Vector3(R(360), R(360), R(360));
                p.Velocity = Rotate(new Vector3(0.4f), p.Angle);
                p.Rotation = R(20) - 10;
            }
            else if (p.SubType == 8)
            {
                p.Scale = R(5) / 10f + 0.5f;
                p.StartPosition.X = p.Scale;
                p.LifeTime = R(10) + 40;
                p.Gravity = 20f;
                p.Velocity = Vector3.UnitZ;
                p.Rotation = R(20) - 10;
            }
        }

        private void InitializeCloud(
            ref ClassicParticle p,
            in Vector3 sourcePosition,
            float sourceScale,
            float ff)
        {
            if (p.SubType == 6 || p.SubType == 14)
            {
                p.LifeTime = 25f + R(5);
                p.Rotation = R(360);
                p.Scale = sourceScale * 0.2f;
                if (p.SubType == 6)
                {
                    p.Scale = 0.2f;
                }
                p.Velocity.X = (R(10) + 5) * 0.4f;
                p.Velocity.Y = 0f;
                p.Velocity.Z = (R(10) + 5) * 0.2f;
            }
            else if (p.SubType == 8)
            {
                p.LifeTime = 300f;
                p.Gravity = R(1000);
                p.Alpha = 0.1f;
                p.Position.X += (R(500) - 250) * ff;
                p.Position.Y += (R(500) - 250) * ff;
                p.Position.Z += (R(20) - 20) * ff;
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale = (R(20) + 180) * 0.01f;
                p.TurningForce.X = sourceScale + R(30) / 100f;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 9 || p.SubType == 20)
            {
                p.LifeTime = 300f;
                p.Gravity = R(1000);
                p.Alpha = 0.1f;
                p.Position.X += (R(500) - 250) * ff;
                p.Position.Y += (R(500) - 250) * ff;
                p.Position.Z += (R(20) - 20) * ff;
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale *= (R(20) + 180) * 0.01f;
                p.TurningForce.X = R(10) / 100f;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 10)
            {
                p.Light = Vector3.Zero;
                p.LifeTime = 500f;
                p.Position.X += (R(400) - 200) * ff;
                p.Position.Y += (R(400) - 200) * ff;
                p.Position.Z += (R(400) - 200) * ff;
                p.StartPosition = p.Position;
                p.Scale = (R(15) + 10) / 15f + sourceScale;
                float temp = (R(10) + 5) * 0.12f;
                p.Velocity = new Vector3(temp, temp, 0f);
            }
            else if (p.SubType == 11)
            {
                p.LifeTime = 500f;
                p.Position.X += (R(400) - 200) * ff;
                p.Position.Y += (R(400) - 200) * ff;
                p.Position.Z += (R(400) - 200) * ff;
                p.StartPosition = p.Position;
                p.Scale = (R(15) + 15) / 30f + sourceScale;
                float temp = (R(10) + 5) * 0.12f;
                p.Velocity = new Vector3(temp, temp, 0f);
            }
            else if (p.SubType == 12)
            {
                p.LifeTime = 30f;
                p.Gravity = R(1000);
                p.Position.X += (R(500) - 250) * ff;
                p.Position.Y += (R(500) - 250) * ff;
                p.Position.Z += (R(20) + 20) * ff;
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale = (R(20) + 180) * 0.01f;
                p.TurningForce.X = sourceScale + R(30) / 100f;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 13)
            {
                p.LifeTime = 30f;
                p.Rotation = R(360);
                p.Gravity = (R(50) - 25) / 10f;
            }
            else if (p.SubType == 15)
            {
                p.LifeTime = 500f;
                p.Position.X += (R(400) - 200) * ff;
                p.Position.Y += (R(400) - 200) * ff;
                p.Position.Z += (R(400) - 200) * ff;
                p.StartPosition = p.Position;
                p.Rotation = R(360);
                p.Scale = (R(15) + 15) / 30f + sourceScale;
                float temp = (R(10) + 5) * 0.08f;
                p.Velocity = new Vector3(temp, temp, 0f);
                p.TurningForce = p.Light;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 16)
            {
                p.Light = Vector3.Zero;
                p.LifeTime = 500f;
                p.Position.X += (R(400) - 200) * ff;
                p.Position.Y += (R(400) - 200) * ff;
                p.Position.Z += (R(400) - 200) * ff;
                p.StartPosition = p.Position;
                p.Rotation = R(360);
                p.Scale = (R(15) + 10) / 15f + sourceScale;
                float temp = (R(10) + 5) * 0.05f;
                p.Velocity = new Vector3(temp, temp, 0f);
            }
            else if (p.SubType == 17)
            {
                p.TexType = ClassicTextureIds.BitmapEventCloud;
                p.LifeTime = 500f;
                p.Position.X += (R(900) - 450) * ff;
                p.Position.Y += (R(900) - 450) * ff;
                p.Position.Z += (R(20) + 50) * ff;
                p.StartPosition = p.Position;
                p.Rotation = R(360);
                p.Scale = (R(20) + 20) / 80f + sourceScale / 3f;
                float temp = (R(10) + 5) * 0.006f;
                p.Velocity = new Vector3(temp, temp, 0f);
                p.TurningForce = p.Light;
                p.Light = Vector3.Zero;
            }
            else if (p.SubType == 18)
            {
                p.TexType = ClassicTextureIds.BitmapChrome + 2;
                p.LifeTime = 160f;
                p.Light = Vector3.Zero;
                p.Gravity = R(1000);
                p.Position = sourcePosition;
                p.Position.Z += R(80) * ff;
                p.Rotation = R(360);
                p.StartPosition.X = (R(200) - 10) / 10f;
                p.StartPosition.Y = (R(200) - 10) / 10f;
                p.Scale = (R(70) + 5) * 0.02f;
                p.TurningForce.X = (R(40) + 10) / 10000f;
                p.TurningForce.Y = R(120) + 80;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 19)
            {
                p.TexType = ClassicTextureIds.BitmapChrome + 2;
                p.LifeTime = 60f;
                p.Light = Vector3.Zero;
                p.Gravity = R(1000);
                p.Position.X += (R(500) - 250) * ff;
                p.Position.Y += (R(500) - 250) * ff;
                p.Position.Z += (R(20) + 20) * ff;
                p.Rotation = R(360);
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale = (R(90) + 220) * 0.02f;
                p.TurningForce.X = sourceScale + R(20) / 100f;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 21)
            {
                p.LifeTime = 100f;
                p.Gravity = R(1000);
                p.Alpha = 0.6f;
                p.Position.X += (R(200) - 100) * ff;
                p.Position.Y += (R(200) - 100) * ff;
                p.Position.Z += (R(20) - 20) * ff;
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale *= (R(20) + 180) * 0.01f;
                p.TurningForce.X = R(10) / 100f;
                p.Velocity = Vector3.Zero;
            }
            else if (p.SubType == 22)
            {
                p.LifeTime = 80f;
                p.Gravity = R(1000);
                p.Alpha = 0.1f;
                p.Position.X += (R(20) - 10) * ff;
                p.Position.Y += (R(20) - 10) * ff;
                p.Position.Z += (R(20) - 20) * ff;
                p.Scale *= (R(20) + 50) * 0.003f;
                p.TurningForce = Vector3.Zero;
                float angle = p.Angle.Z + R(360);
                p.Velocity = Rotate(
                    new Vector3(0f, R(10) * 0.1f + 0.2f, 0f),
                    new Vector3(0f, 0f, angle));
                p.Velocity.Z = 0f;
            }
            else if (p.SubType == 23)
            {
                p.LifeTime = 80f;
                p.Gravity = R(1000);
                p.Alpha = 0.1f;
                p.Position.X += (R(6) - 3) * ff;
                p.Position.Y += (R(6) - 3) * ff;
                p.Position.Z += (R(6) - 3) * ff;
                p.Scale = 0f;
                p.TurningForce = Vector3.Zero;
                p.TurningForce.X = Random.FpsCheck(2, Clock) ? 1f : -1f;
                float angle = p.Angle.Z + 90f + R(40) - 20f;
                p.Velocity = Rotate(
                    new Vector3(0f, R(10) * 0.2f + 1f, 0f),
                    new Vector3(0f, 0f, angle));
                p.Velocity.Z = 0f;
            }
            else
            {
                p.LifeTime = 30f;
                p.Gravity = R(1000);
                p.Position.X += (R(500) - 250) * ff;
                p.Position.Y += (R(500) - 250) * ff;
                p.Position.Z += (R(20) + 20) * ff;
                p.StartPosition.Y = R(100) / 100f;
                p.StartPosition.Z = p.Position.Z;
                p.Scale = (R(20) + 180) * 0.01f;
                p.TurningForce.X = sourceScale + R(30) / 100f;
                p.Velocity = Vector3.Zero;
            }
        }

        private void InitializeLight(
            ref ClassicParticle p,
            in Vector3 sourcePosition,
            in Vector3 sourceLight,
            float sourceScale)
        {
            if (p.SubType == 0 || p.SubType == 8)
            {
                p.LifeTime = R(10) + 10;
                p.Gravity = R(10) + 10f;
                p.Scale = (R(50) + 50f) / 100f * sourceScale;
            }
            else if (p.SubType == 9)
            {
                p.LifeTime = 40f * p.Scale;
            }
            else if (p.SubType == 7)
            {
                p.LifeTime = R(10) + 30;
                p.Gravity = R(10) + 10f;
                p.Scale = (R(15) + 30f) / 100f * sourceScale;
            }
            else if (p.SubType == 6)
            {
                p.LifeTime = R(10) + 10;
                p.Gravity = R(10) + 10f;
                p.Scale = (R(50) + 50f) / 100f * sourceScale;
            }
            else if (p.SubType == 1)
            {
                p.LifeTime = 50f;
            }
            else if (p.SubType == 2)
            {
                p.LifeTime = R(10) + 10;
                p.Gravity = R(10) + 10f;
                p.Scale = (R(50) + 50f) / 100f * sourceScale;
            }
            else if (p.SubType == 3)
            {
                p.LifeTime = 10f;
            }
            else if (p.SubType == 4)
            {
                p.LifeTime = 10f;
                p.Gravity = 0f;
                p.Rotation = sourceScale;
                p.Scale = 2f;
            }
            else if (p.SubType == 5)
            {
                p.LifeTime = 50f;
            }
            else if (p.SubType == 10)
            {
                p.LifeTime = 100f;
                p.Scale = sourceScale + R(10) * 0.02f;
            }
            else if (p.SubType == 11)
            {
                p.LifeTime = R(10) + 130;
                p.Gravity = R(2) + 2f;
                p.Scale = (R(20) + 20f) / 100f * sourceScale;
            }
            else if (p.SubType == 12 || p.SubType == 13)
            {
                p.LifeTime = R(10) + 100;
                p.Gravity = R(2) + 2f;
                p.Scale = (R(20) + 20f) / 100f * sourceScale;
            }
            else if (p.SubType == 14)
            {
                p.LifeTime = 50f;
                p.Scale = sourceScale + R(10) * 0.02f;
            }
            else if (p.SubType == 15)
            {
                p.LifeTime = R(10) + 100;
                p.Gravity = R(3) + 2f;
                p.Scale = (R(20) + 20f) / 100f * sourceScale;
                p.StartPosition = sourcePosition;
                p.StartPosition.Z = 0f;
                p.TurningForce = sourceLight;
                p.Alpha = 0f;
            }
        }

        private void InitializePoundingBall(
            ref ClassicParticle p,
            in Vector3 sourceLight,
            float sourceScale)
        {
            if (p.SubType == 0 || p.SubType == 1)
            {
                p.LifeTime = p.SubType == 0 ? 24f : 15f;
                p.Velocity = new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                p.Scale = (R(128) + 128) * 0.01f;
                p.Rotation = R(360);
                p.Light = new Vector3(0.5f);
            }
            else if (p.SubType == 2)
            {
                p.LifeTime = 20f;
                p.Scale = (R(128) + 80) * 0.009f;
                p.Rotation = R(360);
                p.Gravity = 0f;
                p.Velocity = Vector3.Zero;
                p.Light = new Vector3(0.5f);
            }
            else if (p.SubType == 3)
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
    }
}
