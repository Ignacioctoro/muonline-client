using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Controllers;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// CreateParticle() — bloque A.
        ///
        /// Port estructural de las primeras 30 familias consecutivas del
        /// switch original de ZzzEffectParticle.cpp:
        /// BITMAP_EFFECT ... BITMAP_TWINTAIL_WATER.
        ///
        /// No es lógica específica de skills. Es inicialización del primitive
        /// Particle compartida por todo el cliente clásico.
        /// </summary>
        private void InitializeParticleCreateA(
            ref ClassicParticle particle,
            in Vector3 sourcePosition,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale)
        {
            float frameFactor =
                Clock.FrameFactor;

            float worldTime =
                (float)Clock.WorldTimeMilliseconds;

            switch (particle.Type)
            {
                case ClassicTextureIds.BitmapEffect:
                    InitializeBitmapEffect(
                        ref particle,
                        sourcePosition,
                        frameFactor);
                    break;

                case ClassicTextureIds.BitmapFlower01:
                case ClassicTextureIds.BitmapFlower01 + 1:
                case ClassicTextureIds.BitmapFlower01 + 2:
                    particle.LifeTime =
                        15f + R(10);
                    particle.Scale =
                        (R(8) + 4) * 0.03f;
                    particle.Light =
                        Vector3.One;
                    particle.Velocity =
                        new Vector3(
                            (R(32) - 16) * 0.1f,
                            (R(32) - 16) * 0.1f,
                            (R(32) - 32) * 0.1f);
                    break;

                case ClassicTextureIds.BitmapFlareBlue:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            30f + R(10);
                        particle.Light =
                            Vector3.One;
                        particle.Velocity =
                            new Vector3(
                                0f,
                                0f,
                                R(100) / 50f);
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime =
                            26f + R(2);
                        particle.Gravity =
                            0f;
                        particle.Velocity.X =
                            0f;
                        particle.Scale =
                            sourceScale +
                            R(6) * 0.1f;
                        particle.Rotation =
                            R(360);
                    }
                    break;

                case ClassicTextureIds.BitmapFlare + 1:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            110f + R(10);
                        particle.Gravity =
                            (R(100) / 100f) * 2f + 1f;
                        particle.Velocity.X =
                            R(100) - 50;
                        particle.Scale =
                            sourceScale +
                            R(2) * 0.01f;
                        particle.Rotation =
                            R(360);
                        particle.Angle =
                            Vector3.Zero;
                        particle.StartPosition =
                            particle.Position;
                    }
                    break;

                case ClassicTextureIds.BitmapBubble:
                    InitializeBubble(
                        ref particle,
                        sourceScale);
                    break;

                case ClassicTextureIds.BitmapLightning + 1:
                    switch (particle.SubType)
                    {
                        case 0:
                            particle.Scale = 0.15f;
                            break;
                        case 4:
                            particle.Scale = 0.25f;
                            break;
                        case 1:
                            particle.LifeTime = 10f;
                            particle.Scale = 0f;
                            break;
                        case 2:
                            particle.LifeTime = 20f;
                            particle.Scale = 1f;
                            break;
                        case 3:
                            particle.LifeTime = 1f;
                            particle.Scale = sourceScale;
                            break;
                        case 5:
                            particle.Scale = sourceScale;
                            break;
                    }

                    if (particle.SubType == 4 ||
                        particle.SubType == 5)
                    {
                        particle.Light =
                            sourceLight;
                    }
                    else
                    {
                        particle.Light =
                            sourceLight +
                            new Vector3(0.5f);
                    }
                    break;

                case ClassicTextureIds.BitmapLightning:
                    particle.LifeTime =
                        10f;
                    particle.Scale =
                        1.8f;
                    particle.Angle.X =
                        30f;
                    particle.Light =
                        new Vector3(
                            (R(4) + 6) * 0.1f,
                            (R(4) + 6) * 0.1f,
                            (R(4) + 6) * 0.1f);
                    particle.Position.Z +=
                        260f * frameFactor;
                    break;

                case ClassicTextureIds.BitmapChromeEnergy2:
                    particle.LifeTime =
                        8f;
                    particle.Velocity =
                        Vector3.Zero;
                    particle.Scale =
                        sourceScale *
                        (R(64) + 128) * 0.01f;
                    particle.Rotation =
                        R(360);
                    break;

                case ClassicTextureIds.BitmapFireCursedLich:
                case ClassicTextureIds.BitmapFireHik2Mono:
                    InitializeFireCursedLich(
                        ref particle,
                        sourceLight,
                        sourceScale,
                        frameFactor);
                    break;

                case ClassicTextureIds.BitmapLeafTotemGolem:
                    particle.LifeTime =
                        R(10) + 40;
                    particle.Velocity =
                        Vector3.Zero;
                    particle.Scale =
                        (R(20) + 10) * 0.04f;
                    particle.Rotation =
                        R(360);
                    particle.Gravity =
                        (R(40) + 30) * -0.05f;
                    particle.Velocity.X =
                        R(10) - 5;
                    particle.Velocity.Y =
                        R(10) - 5;
                    particle.Velocity.Z =
                        R(5) + 10;
                    particle.Alpha =
                        1f;
                    break;

                case ClassicTextureIds.BitmapFire:
                case ClassicTextureIds.BitmapFire + 2:
                case ClassicTextureIds.BitmapFire + 3:
                    InitializeFireFamily(
                        ref particle,
                        sourceScale);
                    break;

                case ClassicTextureIds.BitmapFire + 1:
                    InitializeFire2(
                        ref particle,
                        sourceScale,
                        frameFactor);
                    break;

                case ClassicTextureIds.BitmapFlame:
                    InitializeFlame(
                        ref particle,
                        sourceLight,
                        sourceScale,
                        frameFactor,
                        worldTime);
                    break;

                case ClassicTextureIds.BitmapFireRed:
                    particle.LifeTime =
                        20f;
                    particle.Velocity =
                        new Vector3(
                            0f,
                            0f,
                            (R(100) + 100) * 0.15f);
                    particle.Scale =
                        sourceScale *
                        (R(64) + 64) * 0.01f;
                    break;

                case ClassicTextureIds.BitmapRainCircle:
                case ClassicTextureIds.BitmapRainCircle + 1:
                    particle.LifeTime =
                        20f;
                    particle.Scale =
                        (R(6) + 8) * 0.1f;

                    if (particle.Type ==
                            ClassicTextureIds.BitmapRainCircle + 1 ||
                        particle.SubType == 1)
                    {
                        particle.Position.X +=
                            (R(10) - 5) * frameFactor;
                        particle.Position.Y +=
                            (R(10) - 5) * frameFactor;
                        particle.Position.Z +=
                            (R(10) - 5) * frameFactor;
                    }

                    if (particle.SubType == 1)
                    {
                        particle.Scale =
                            sourceScale;
                        particle.Gravity =
                            R(100) / 1000f;
                        particle.Velocity.Y =
                            1f;
                    }
                    break;

                case ClassicTextureIds.BitmapEnergy:
                    particle.Scale =
                        sourceScale *
                        (R(8) + 6) * 0.1f;
                    particle.Rotation =
                        R(360);
                    particle.Gravity =
                        20f;

                    if (particle.SubType == 1)
                    {
                        particle.LifeTime = 10f;
                        particle.Light = new Vector3(0.5f);
                    }
                    else if (particle.SubType == 2)
                    {
                        particle.TexType =
                            ClassicTextureIds.BitmapMagic + 1;
                        particle.LifeTime = 10f;
                        particle.Scale =
                            0.3f +
                            sourceScale *
                            (R(8) + 6) * 0.1f;
                        particle.Rotation = 0f;
                        particle.Gravity = 0f;
                    }
                    else if (particle.SubType == 3 ||
                             particle.SubType == 4 ||
                             particle.SubType == 5)
                    {
                        particle.LifeTime = 25f;
                    }
                    else if (particle.SubType == 6)
                    {
                        particle.LifeTime = 20f;
                    }
                    else if (particle.SubType == 7)
                    {
                        particle.LifeTime = 5f;
                    }
                    break;

                case ClassicTextureIds.BitmapMagic:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime = 10f;
                        particle.Scale = sourceScale;
                    }
                    break;

                case ClassicTextureIds.BitmapFlare:
                    InitializeFlare(
                        ref particle,
                        sourceScale);
                    break;

                case ClassicTextureIds.BitmapLight + 2:
                    InitializeLight2(
                        ref particle,
                        sourceLight,
                        sourceScale,
                        frameFactor,
                        worldTime);
                    break;

                case ClassicTextureIds.BitmapMagic + 1:
                    particle.LifeTime = 10f;
                    particle.Scale +=
                        (R(32) + 48) *
                        0.001f *
                        frameFactor;
                    particle.Angle.X = R(360);
                    particle.Rotation =
                        (int)worldTime % 360;
                    break;

                case ClassicTextureIds.BitmapBlueBlur:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime = 30f;
                        particle.Scale =
                            (R(64) + 64) * 0.01f;
                        particle.Rotation = R(360);
                        particle.Gravity =
                            (R(32) + 60) * 0.1f;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime = 30f;
                        particle.Scale =
                            (R(64) + 64) * 0.01f;
                        particle.Rotation = R(360);
                        particle.Gravity =
                            (R(32) + 60) * 0.1f;
                        particle.Position.Z -=
                            45f * frameFactor;
                        particle.TexType =
                            ClassicTextureIds.BitmapPoundingBall;
                    }
                    break;

                case ClassicTextureIds.BitmapClud64:
                    InitializeClud64(
                        ref particle,
                        sourceLight,
                        sourceScale,
                        frameFactor);
                    break;

                case ClassicTextureIds.BitmapLight + 3:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            R(10) + 140;
                        particle.Gravity =
                            R(10) + 5f;
                        particle.Scale =
                            (R(20) + 20f) /
                            100f *
                            sourceScale;
                        particle.Position.X +=
                            (R(100) - 50) * frameFactor;
                        particle.Position.Y +=
                            (R(100) - 50) * frameFactor;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.Scale = 0.1f;
                        particle.StartPosition.X =
                            particle.Scale;
                        particle.LifeTime =
                            R(10) + 20;
                        particle.Position.X +=
                            (R(50) - 25) * frameFactor;
                        particle.Position.Y +=
                            (R(50) - 25) * frameFactor;
                        particle.Position.Z +=
                            (R(50) - 25) * frameFactor;
                        particle.Gravity = 1f;
                        particle.Angle =
                            new Vector3(
                                R(360),
                                R(360),
                                R(360));

                        Vector3 p =
                            new Vector3(0.5f) * 0.8f;
                        particle.Velocity =
                            Rotate(
                                p,
                                particle.Angle);
                        particle.Rotation =
                            R(20) - 10;
                    }
                    break;

                case ClassicTextureIds.BitmapTwinTailWater:
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            50f + R(10);
                        particle.Scale =
                            (R(32) + 140) * 0.01f;
                        particle.Position.X +=
                            (R(120) - 60) * frameFactor;
                        particle.Position.Y +=
                            (R(100) - 50) * frameFactor;
                        particle.Position.Z +=
                            R(100) * frameFactor;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime =
                            20f + R(10);
                        particle.Scale =
                            (R(32) + 50) * 0.01f;
                        particle.Position.X +=
                            (R(80) - 40) * frameFactor;
                        particle.Position.Y +=
                            (R(80) - 40) * frameFactor;
                        particle.Position.Z +=
                            R(80) * frameFactor;
                    }
                    else if (particle.SubType == 2)
                    {
                        particle.LifeTime =
                            60f + R(10);
                        particle.Scale =
                            (R(32) + 50) * 0.02f;
                        particle.Position.X +=
                            (R(40) - 20) * frameFactor;
                        particle.Position.Y +=
                            (R(40) - 20) * frameFactor;
                        particle.Position.Z +=
                            R(40) * frameFactor;
                    }

                    particle.Angle.X = R(360);
                    particle.Angle.Z = R(360);
                    particle.Gravity =
                        (R(10) + 20) * 0.1f;
                    break;
            }
        }

        private void InitializeBitmapEffect(
            ref ClassicParticle particle,
            in Vector3 sourcePosition,
            float frameFactor)
        {
            if (particle.SubType == 1)
            {
                particle.TexType =
                    ClassicTextureIds.BitmapExtLoginImpact;
                particle.Scale =
                    (R(10) + 20) * 0.01f;
                particle.Gravity =
                    (R(10) + 20) * 0.05f;
            }
            else if (particle.SubType == 2)
            {
                particle.Gravity = 0f;
                particle.Scale =
                    (R(5) + 12) * 0.1f;
            }
            else if (particle.SubType == 3)
            {
                particle.TexType =
                    ClassicTextureIds.BitmapClud64;
                particle.Scale =
                    (R(5) + 10) * 0.01f;
                particle.Gravity =
                    (R(10) + 20) * 0.05f;
            }
            else if (particle.SubType == 0)
            {
                particle.Gravity = 0f;
                particle.Scale =
                    (R(5) + 20) * 0.1f;
            }

            particle.LifeTime =
                20f + R(3);

            if (particle.SubType == 2)
            {
                particle.LifeTime =
                    40f + R(3);
            }

            particle.Position.X +=
                R(50) - 25;
            particle.Position.Y +=
                R(50) - 25;
            particle.Position.Z +=
                R(200) - 100 + 250f;

            if (particle.SubType == 4)
            {
                particle.Gravity = 0f;

                // El Main calcula estas dos asignaciones y luego las pisa.
                // Conservamos también las llamadas RNG para mantener la
                // secuencia clásica reproducible.
                _ = (R(5) + 20) * 0.1f * 1.3f;
                _ = sourcePosition.X +
                    (R(80) - 40) * 1.3f;

                particle.Scale =
                    (R(5) + 20) * 0.1f;
                particle.Position.X =
                    sourcePosition.X +
                    (R(80) - 40);
                particle.Position.Z -=
                    100f * frameFactor;
            }
            else if (particle.SubType == 5)
            {
                particle.TexType =
                    ClassicTextureIds.BitmapExtLoginImpact;
                particle.Scale =
                    (R(10) + 20) * 0.01f * 1.3f;
                particle.Gravity =
                    (R(10) + 20) * 0.05f * 1.3f;
                particle.Position.Z -=
                    100f * frameFactor;
            }
            else if (particle.SubType == 6)
            {
                particle.Gravity =
                    (R(20) + 80) * 0.1f;
                particle.Scale =
                    (R(5) + 20) * 0.1f;
                particle.Position.Z -=
                    200f * frameFactor;
            }
            else if (particle.SubType == 7)
            {
                particle.TexType =
                    ClassicTextureIds.BitmapExtLoginImpact;
                particle.Scale =
                    (R(10) + 20) * 0.01f;
                particle.Gravity =
                    (R(20) + 80) * 0.1f;
                particle.Position.Z -=
                    100f * frameFactor;
            }
        }

        private void InitializeBubble(
            ref ClassicParticle particle,
            float sourceScale)
        {
            switch (particle.SubType)
            {
                case 0:
                case 1:
                    particle.LifeTime = 30f + R(10);
                    particle.Scale = (R(6) + 4) * 0.03f;
                    particle.Light = Vector3.One;
                    break;

                case 2:
                    particle.LifeTime = 30f + R(10);
                    particle.Scale = (R(6) + 4) * 0.03f;
                    particle.Light = Vector3.One;
                    SoundController.Instance.PlayBuffer(
                        "Sound/mDeathBubble.wav");
                    break;

                case 3:
                    particle.LifeTime = 30f + R(10);
                    particle.Scale =
                        (R(6) + 4) * 0.03f +
                        sourceScale;
                    particle.Gravity =
                        (R(6) + 4) * 0.03f;
                    particle.Light = Vector3.One;
                    break;

                case 4:
                    particle.LifeTime = 30f + R(10);
                    particle.Scale = (R(6) + 4) * 0.03f;
                    particle.Light = Vector3.One;
                    break;

                case 5:
                    particle.LifeTime = 30f + R(10);
                    particle.Scale = (R(6) + 4) * 0.04f;
                    particle.Light = Vector3.One;
                    break;
            }
        }

        private void InitializeFireCursedLich(
            ref ClassicParticle particle,
            in Vector3 sourceLight,
            float sourceScale,
            float frameFactor)
        {
            if (particle.SubType == 0)
            {
                particle.LifeTime = R(12) + 8;
                particle.Velocity = Vector3.Zero;
                particle.Scale = (R(30) + 20) * 0.01f;
                particle.Rotation = R(360);
                particle.Gravity = (R(15) + 15) * 0.01f;
            }
            else if (particle.SubType == 1)
            {
                particle.Position.X += (R(10) - 5) * 0.2f * frameFactor;
                particle.Position.Y += (R(10) - 5) * 0.2f * frameFactor;
                particle.Position.Z += (R(10) - 5) * 0.2f * frameFactor;
                particle.LifeTime = R(28) + 4;
                particle.Velocity = Vector3.Zero;
                particle.Scale = (R(5) + 50) * 0.016f;
                particle.Rotation = R(360);
                particle.Gravity = (R(5) + 10) * 0.01f;
                CopyTargetPositionToStart(ref particle);
            }
            else if (particle.SubType == 2)
            {
                particle.LifeTime = R(12) + 8;
                particle.Velocity = Vector3.Zero;
                particle.Scale =
                    (R(30) + 20) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(15) + 15) * 0.01f;
            }
            else if (particle.SubType == 3)
            {
                particle.LifeTime = R(12) + 16;
                particle.Velocity = Vector3.Zero;
                particle.Scale = (R(30) + 20) * 0.02f;
                particle.Rotation = R(360);
                particle.Gravity = (R(15) + 15) * 0.02f;
                CopyTargetPositionToStart(ref particle);
            }
            else if (particle.SubType == 4 ||
                     particle.SubType == 9)
            {
                particle.LifeTime = R(5) + 12;
                particle.Scale =
                    (R(72) + 72) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(24) + 64) * 0.1f;
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
            }
            else if (particle.SubType == 5)
            {
                particle.LifeTime = R(5) + 24;
                particle.Scale =
                    (R(72) + 72) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(24) + 74) * 0.1f;
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
            }
            else if (particle.SubType == 6)
            {
                particle.LifeTime = R(5) + 24;
                particle.Scale =
                    (R(72) + 72) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(24) + 100) * 0.1f;

                // El VectorCopy(Angle, Velocity) del Main se pisa justo
                // después; no cambia el resultado final.
                float fRand = R(50) * 0.03f;
                particle.Velocity =
                    new Vector3(
                        2.5f + fRand,
                        -5f - fRand,
                        0f);
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
            }
            else if (particle.SubType == 7)
            {
                particle.LifeTime = R(5) + 12;
                particle.Scale =
                    (R(72) + 52) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(14) + 44) * 0.1f;
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
            }
            else if (particle.SubType == 8)
            {
                particle.LifeTime = 8f;
                particle.Scale =
                    (R(42) + 12) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(14) + 44) * 0.1f;
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
                CopyTargetPositionToStart(ref particle);
            }
        }

        private void InitializeFireFamily(
            ref ClassicParticle particle,
            float sourceScale)
        {
            switch (particle.SubType)
            {
                case 0:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    particle.Scale = (R(64) + 128) * 0.01f;
                    particle.Rotation = R(360);
                    break;

                case 1:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    particle.Scale = (R(4) + 10) * 0.01f;
                    particle.Rotation = R(360);
                    break;

                case 17:
                case 5:
                    particle.LifeTime =
                        particle.SubType == 17 ? 10f : 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    particle.Scale =
                        sourceScale * (R(64) + 128) * 0.01f;
                    particle.Rotation = R(360);
                    break;

                case 7:
                    particle.LifeTime = 24f;
                    // Primera asignación del Main, inmediatamente pisada.
                    _ = R(16);
                    particle.Velocity =
                        new Vector3(0f, -(R(32) - 16) * 0.1f, 0f);
                    particle.Scale =
                        (R(64) + 128) * 0.008f + sourceScale;
                    particle.Rotation = R(360);
                    break;

                case 9:
                {
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    int range = R(60) - 30;
                    particle.StartPosition =
                        new Vector3(
                            range,
                            range,
                            190f - Math.Abs(range) * 1.5f);
                    particle.Rotation = R(360);
                    break;
                }

                case 10:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    particle.Rotation = R(360);
                    break;

                case 11:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(32) - 16) * 0.1f, 0f);
                    particle.Scale =
                        (R(64) + 128) * 0.008f + sourceScale;
                    particle.Rotation = R(360);
                    break;

                case 12:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(16) + 32) * 0.1f, 0f);
                    particle.Rotation = R(360);
                    particle.Scale =
                        sourceScale * (R(16) + 150) * 0.012f;
                    break;

                case 13:
                    particle.LifeTime = 20f;
                    particle.Velocity =
                        new Vector3(0f, 0f, (R(128) + 128) * 0.15f);
                    particle.Rotation = R(360);
                    particle.Scale = (R(16) + 150) * 0.012f;
                    break;

                case 14:
                    particle.LifeTime = 24f;
                    particle.Scale = 1.5f;
                    particle.TexType = ClassicTextureIds.BitmapClud64;
                    particle.Velocity = Vector3.Zero;
                    particle.Rotation = R(360);
                    break;

                case 15:
                    particle.LifeTime = 10f;
                    particle.Scale = (R(64) + 128) * 0.01f;
                    particle.TexType = ClassicTextureIds.BitmapClud64;
                    particle.Velocity = Vector3.Zero;
                    particle.Rotation = R(360);
                    break;

                case 16:
                    particle.LifeTime = 4f;
                    break;

                case 18:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(32) - 16) * 0.1f, 0f);
                    particle.Rotation = R(360);
                    particle.TexType = ClassicTextureIds.BitmapGroundSmoke;
                    particle.Light = Vector3.One;
                    break;

                default:
                    particle.LifeTime = 24f;
                    particle.Velocity =
                        new Vector3(0f, -(R(32) - 16) * 0.1f, 0f);
                    particle.Rotation = R(360);
                    break;
            }
        }

        private void InitializeFire2(
            ref ClassicParticle particle,
            float sourceScale,
            float frameFactor)
        {
            if (particle.SubType == 1)
            {
                particle.LifeTime = 3f;
                particle.Velocity =
                    new Vector3(0f, -(R(8) + 32) * 0.3f, 0f);
            }
            else if (particle.SubType == 2)
            {
                particle.LifeTime = 5f;
                particle.Velocity =
                    new Vector3(0f, -(R(8) + 32) * 0.1f, 0f);
            }
            else if (particle.SubType == 3)
            {
                particle.LifeTime = 7f;
                particle.Velocity =
                    new Vector3(0f, -(R(8) + 32) * 0.1f, 0f);
            }
            else if (particle.SubType == 4)
            {
                particle.LifeTime = 100f;
                particle.Gravity = (R(100) / 100f) * 4f + 1f;
                particle.Velocity.X = R(300) - 150;
                particle.Scale = sourceScale + R(6) * 0.15f;
                particle.Rotation = R(360);
                particle.Angle = Vector3.Zero;
                particle.StartPosition = particle.Position;
            }
            else if (particle.SubType == 5)
            {
                particle.LifeTime = 6f;
                particle.Gravity =
                    (R(100) / 100f) * 4f +
                    particle.Angle.X * 1.2f;
                particle.Scale = sourceScale + R(6) * 0.20f;
                particle.Rotation = R(360);
                particle.StartPosition = particle.Position;
            }
            else if (particle.SubType == 6)
            {
                particle.LifeTime = 6f;
                particle.Gravity = (R(100) / 100f) * 4f + 5f;
                particle.Scale = sourceScale + R(6) * 0.10f;
                particle.Rotation = R(360);
                particle.StartPosition = particle.Position;
            }
            else if (particle.SubType == 7)
            {
                particle.LifeTime = 10f;
            }
            else if (particle.SubType == 8)
            {
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z += (R(20) - 10) * frameFactor;
                particle.LifeTime = 32f;
                particle.Rotation = R(360);
                particle.Gravity = (R(5) + 5) / 5f;
            }
            else if (particle.SubType == 9)
            {
                particle.TexType = ClassicTextureIds.BitmapLight + 2;
                particle.LifeTime = 32f;
                particle.Rotation = R(360);
                particle.Gravity = (R(5) + 5) / 10f;
            }
            else if (particle.SubType == 0)
            {
                particle.LifeTime = 12f;
                particle.Velocity =
                    new Vector3(0f, -(R(8) + 32) * 0.3f, 0f);
                particle.Scale = 0.8f;
            }

            // Main lo vuelve a asignar al final para todos los subtypes.
            particle.Rotation = R(360);
        }

        private void InitializeFlame(
            ref ClassicParticle particle,
            in Vector3 sourceLight,
            float sourceScale,
            float frameFactor,
            float worldTime)
        {
            switch (particle.SubType)
            {
                case 0:
                    particle.LifeTime = 20f;
                    particle.Velocity =
                        new Vector3(0f, 0f, (R(128) + 128) * 0.15f);
                    particle.Scale =
                        sourceScale * (R(64) + 64) * 0.01f;
                    break;

                case 6:
                    particle.LifeTime = 10f;
                    particle.Velocity =
                        new Vector3(0f, 0f, (R(128) + 256) * 0.12f);
                    particle.Scale =
                        sourceScale * (R(64) + 64) * 0.01f;
                    break;

                case 8:
                case 7:
                {
                    particle.LifeTime = 33f;
                    particle.Scale += -R(20) / 100f;
                    particle.Rotation = R(360) - 180f;
                    particle.StartPosition.X = particle.Position.X;
                    particle.Position.X +=
                        (R(2) / 2f) *
                        particle.Scale *
                        frameFactor;
                    particle.Gravity =
                        (R(100) / 100f + 1.8f) *
                        particle.Scale;

                    float fTemp = 0f;
                    if (R(10) >= 3)
                    {
                        fTemp =
                            (R(20) / 10f - 1f) *
                            particle.Scale;
                    }

                    particle.Velocity =
                        new Vector3(0f, fTemp, 0f);
                    particle.Position.Y +=
                        (particle.Position.X -
                         particle.StartPosition.X) *
                        frameFactor;
                    break;
                }

                case 1:
                    particle.LifeTime = 15f;
                    particle.Scale +=
                        (R(32) + 32) * 0.01f * frameFactor;
                    particle.Velocity =
                        new Vector3(0f, (R(4) + 4) * 0.15f, 0f);
                    break;

                case 5:
                    particle.LifeTime = 4f;
                    particle.Velocity =
                        new Vector3(0f, (R(4) + 4) * 0.15f, 0f);
                    break;

                case 2:
                {
                    particle.LifeTime = 10f;
                    particle.Scale +=
                        (R(32) + 32) * 0.01f * frameFactor;

                    float inter =
                        sourceLight.X * R(80) / 100f;
                    particle.Velocity =
                        new Vector3(0f, inter, 0f);
                    particle.Position =
                        ClassicMath.MovePosition(
                            particle.Position,
                            particle.Angle,
                            particle.Velocity,
                            frameFactor);

                    inter =
                        (sourceLight.X - inter) / 15f;
                    particle.Velocity =
                        new Vector3(0f, inter, 0f);

                    float luminosity =
                        MathF.Sin(worldTime * 0.002f) * 0.3f + 0.7f;
                    particle.Light =
                        new Vector3(
                            luminosity,
                            luminosity * 0.5f,
                            luminosity * 0.5f);
                    break;
                }

                case 3:
                {
                    particle.LifeTime = 10f;
                    particle.Scale +=
                        (R(32) + 32) * 0.01f * frameFactor;
                    particle.Velocity = Vector3.Zero;

                    float luminosity =
                        MathF.Sin(worldTime * 0.002f) * 0.3f + 0.7f;
                    particle.Light =
                        new Vector3(
                            luminosity,
                            luminosity * 0.5f,
                            luminosity * 0.5f);
                    break;
                }

                case 4:
                {
                    particle.LifeTime = 5f;
                    particle.Scale +=
                        (R(32) + 32) * 0.01f * frameFactor;
                    particle.Gravity = 10f;
                    CopyTargetPositionToStart(ref particle);

                    float luminosity =
                        MathF.Sin(worldTime * 0.002f) * 0.3f + 0.7f;
                    particle.Light =
                        new Vector3(
                            luminosity,
                            luminosity * 0.5f,
                            luminosity * 0.5f);
                    break;
                }

                case 9:
                {
                    particle.LifeTime = 40f;
                    particle.Scale += -R(20) / 100f * frameFactor;
                    particle.Rotation = R(360) - 180f;
                    particle.StartPosition.X = particle.Position.X;
                    particle.Position.X +=
                        (R(2) / 2f) * particle.Scale * frameFactor;
                    particle.Gravity =
                        (R(100) / 100f + 1.8f) *
                        particle.Scale +
                        2f;

                    float fTemp = 0f;
                    if (R(10) >= 3)
                    {
                        fTemp =
                            (R(20) / 10f - 1f) *
                            particle.Scale;
                    }

                    particle.Velocity =
                        new Vector3(0f, fTemp * 2f, 0f);
                    particle.Position.Y +=
                        (particle.Position.X -
                         particle.StartPosition.X) *
                        frameFactor;
                    break;
                }

                case 10:
                    particle.LifeTime = 20f + R(5);
                    particle.Rotation = 0f;
                    particle.Scale =
                        sourceScale * 0.5f + R(10) * 0.02f;
                    particle.Velocity =
                        new Vector3(
                            (R(10) + 5) * 0.4f,
                            (R(10) - 5) * 0.4f,
                            (R(10) + 5) * 0.2f);
                    break;

                case 11:
                    particle.LifeTime = 20f;
                    particle.Rotation = R(360);
                    particle.TexType = ClassicTextureIds.BitmapFireHik2Mono;
                    particle.Velocity =
                        new Vector3(0f, 0f, (R(128) + 128) * 0.15f);
                    particle.Scale =
                        sourceScale * (R(64) + 64) * 0.01f;
                    break;

                case 12:
                    particle.LifeTime = 10f;
                    particle.Velocity =
                        new Vector3(0f, 0f, (R(128) + 128) * 0.15f);
                    particle.Scale =
                        sourceScale * (R(64) + 64) * 0.01f;
                    break;
            }
        }

        private void InitializeFlare(
            ref ClassicParticle particle,
            float sourceScale)
        {
            particle.LifeTime = 60f;

            if (particle.SubType == 0 ||
                particle.SubType == 3 ||
                particle.SubType == 6 ||
                particle.SubType == 10)
            {
                particle.Gravity =
                    (R(100) / 100f) * 4f + 1f;
            }
            else if (particle.SubType == 2)
            {
                particle.Gravity =
                    (R(100) / 100f) * 5f + 5f;
            }
            else if (particle.SubType == 4 ||
                     particle.SubType == 5)
            {
                particle.LifeTime = 40f;
                particle.Gravity =
                    (R(100) / 100f) * 5f + 1f;
            }

            particle.Velocity.X =
                R(300) - 150;
            particle.Scale =
                sourceScale + R(6) * 0.01f;
            particle.Rotation =
                R(360);
            particle.Angle =
                Vector3.Zero;
            particle.StartPosition =
                particle.Position;

            if (particle.SubType == 10)
            {
                float count =
                    (particle.Velocity.X +
                     particle.LifeTime) *
                    0.1f;
                particle.StartPosition.X +=
                    MathF.Sin(count) * 40f;
                particle.StartPosition.Y -=
                    MathF.Cos(count) * 40f;
            }
            else if (particle.SubType == 11)
            {
                particle.LifeTime = 26f + R(2);
                particle.Gravity = 0f;
                particle.Velocity.X = 0f;
                particle.Scale =
                    sourceScale + R(6) * 0.1f;
                particle.Rotation = R(360);
            }
            else if (particle.SubType == 12)
            {
                particle.LifeTime = 80f;
                particle.Gravity =
                    (R(100) / 100f) * 4f + 1f;
                particle.Light =
                    new Vector3(0.2f, 0.6f, 1f);
            }
        }

        private void InitializeLight2(
            ref ClassicParticle particle,
            in Vector3 sourceLight,
            float sourceScale,
            float frameFactor,
            float worldTime)
        {
            if (particle.SubType == 0)
            {
                particle.LifeTime = 16f;
                particle.Scale +=
                    (R(32) + 48) * 0.01f * frameFactor;
                particle.Angle.X = R(360);
                particle.Rotation = (int)worldTime % 360;
            }
            else if (particle.SubType == 1)
            {
                particle.TexType = ClassicTextureIds.BitmapSmoke;
                particle.LifeTime = 20f;
                particle.Scale +=
                    (R(10) + 10) * 0.01f * frameFactor;
                particle.Angle.X = R(360);
                particle.Rotation = (int)worldTime % 360;
                particle.Gravity = (R(10) + 40) * 0.04f;
                particle.Position.Y += 10f * frameFactor;
            }
            else if (particle.SubType == 2)
            {
                particle.LifeTime = 10f;
                particle.Scale -=
                    (R(40) + 48) * 0.008f * frameFactor;
                particle.Angle.X = R(360);
                particle.Rotation = (int)worldTime % 360;
                particle.Gravity = 0f;
            }
            else if (particle.SubType == 3)
            {
                particle.LifeTime = 20f;
                particle.Scale +=
                    (R(32) + 48) * 0.008f * frameFactor;
                particle.Angle.X = R(360);
                particle.Rotation = (int)worldTime % 360;
                CopyTargetPositionToStart(ref particle);
            }
            else if (particle.SubType == 4)
            {
                particle.LifeTime = 16f;
                particle.Scale +=
                    (R(32) + 48) * 0.01f * frameFactor;
                particle.Angle.X = R(360);
                particle.Rotation = (int)worldTime % 360;
            }
            else if (particle.SubType == 5)
            {
                particle.TexType = ClassicTextureIds.BitmapAdvSmoke;

                TrySetPositionToRandomTargetBone(
                    ref particle);

                particle.Velocity = Vector3.Zero;
                particle.LifeTime = 21f;
                particle.Scale =
                    (R(4) + 8) * 0.01f * frameFactor;
                particle.Gravity =
                    (R(1) + 4) * 0.1f;
                particle.Angle.Z = R(360);

                Vector3 p =
                    new Vector3(
                        0f,
                        (R(2) + 60) * 0.1f,
                        0f);
                particle.TurningForce =
                    Rotate(p, particle.Angle);
                particle.TurningForce.Z +=
                    4f * frameFactor;
                particle.Rotation =
                    (int)worldTime % 360;
                particle.Alpha = 1f;
            }
            else if (particle.SubType == 6 ||
                     particle.SubType == 7)
            {
                particle.LifeTime =
                    particle.SubType == 6 ? 21f : 18f;
                particle.Scale =
                    sourceScale +
                    (R(4) + 8) * 0.01f;
                particle.Gravity =
                    (R(1) + 1) * 0.1f;
                particle.Angle.Z = R(360);

                Vector3 p =
                    new Vector3(
                        0f,
                        (R(2) + 60) * 0.1f,
                        0f);
                particle.TurningForce =
                    Rotate(p, particle.Angle);
                particle.TurningForce.Z +=
                    2f * frameFactor;
                particle.Rotation =
                    (int)worldTime % 360;
                particle.Alpha = 1f;
                particle.Velocity = Vector3.Zero;
            }
        }

        private void InitializeClud64(
            ref ClassicParticle particle,
            in Vector3 sourceLight,
            float sourceScale,
            float frameFactor)
        {
            if (particle.SubType == 0 ||
                particle.SubType == 2)
            {
                if (R(4) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 40f;
                particle.Scale = (R(20) + 250) * 0.01f;
                particle.Rotation = R(360);
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z -= 20f * frameFactor;
                particle.Gravity = (R(10) + 5) * 0.1f;
            }
            else if (particle.SubType == 1)
            {
                particle.LifeTime = 12f;
                particle.Scale += R(30) * 0.01f * frameFactor;
                particle.Rotation = R(360);
                particle.Position.Z += 30f * frameFactor;
            }
            else if (particle.SubType == 3)
            {
                if (R(2) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 50f;
                particle.Scale = sourceScale + (R(10) + 10) * 0.01f;
                particle.Rotation = R(360);
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z -= 20f * frameFactor;
                particle.Gravity = (R(10) + 5) * 0.1f;
            }
            else if (particle.SubType == 4)
            {
                if (R(2) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 4f;
                particle.Scale = sourceScale + (R(10) + 10) * 0.01f;
                particle.Rotation = R(360);
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z -= 20f * frameFactor;
                particle.Gravity = (R(10) + 5) * 0.1f;
            }
            else if (particle.SubType == 5)
            {
                if (R(2) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 50f;
                particle.Scale = sourceScale + (R(10) + 10) * 0.01f;
                particle.Rotation = R(360);
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z -= 20f * frameFactor;
                particle.Gravity = (R(10) + 5) * 0.1f;
            }
            else if (particle.SubType == 6)
            {
                particle.LifeTime = 25f;
                particle.Scale =
                    (R(8) + 50) * 0.01f * sourceScale;
                particle.Rotation = R(360);
                particle.Gravity = (R(10) + 35) * 0.1f;
                particle.Alpha = 0f;
                particle.TurningForce = sourceLight;
                particle.Light = Vector3.Zero;
                CopyTargetPositionToStart(ref particle);
            }
            else if (particle.SubType == 7)
            {
                particle.LifeTime = 40f;
                particle.Velocity.X = (R(20) - 10) * 0.1f;
                particle.Velocity.Y = (R(20) - 10) * 0.1f;
                particle.Position.Z += 80f * frameFactor;
                particle.Gravity = 0f;
                particle.Alpha = 0.5f;
            }
            else if (particle.SubType == 8)
            {
                particle.LifeTime = 20f;
                particle.Velocity.X = (R(20) - 10) * 0.1f;
                particle.Velocity.Y = (R(20) - 10) * 0.1f;
                particle.Position.Z += 80f * frameFactor;
                particle.Gravity = 0f;
                particle.Alpha = 0.5f;
            }
            else if (particle.SubType == 9)
            {
                if (R(2) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 40f;
                particle.Alpha = 0.5f;
                float intervalScale = sourceScale * 0.1f;
                particle.Scale +=
                    ((R(20) - 10) / 2f) *
                    (intervalScale / 2f) *
                    frameFactor;
                particle.Rotation = R(360);
                particle.Velocity.X = (R(20) - 10) * 0.03f;
                particle.Velocity.Y = (R(20) - 10) * 0.03f;
                particle.Velocity.Z =
                    -(1.2f + (R(20) - 10) * 0.025f);
                particle.Gravity =
                    2f + (R(20) - 10) * 0.05f;
            }
            else if (particle.SubType == 10)
            {
                particle.Position.X += (R(10) - 5) * 0.2f * frameFactor;
                particle.Position.Y += (R(10) - 5) * 0.2f * frameFactor;
                particle.Position.Z += (R(10) - 5) * 0.2f * frameFactor;
                particle.LifeTime = R(28) + 4;
                particle.Velocity = Vector3.Zero;
                particle.Scale = (R(5) + 70) * 0.016f;
                particle.Rotation = R(360);
                particle.Gravity = (R(5) + 10) * 0.01f;
                CopyTargetPositionToStart(ref particle);
            }
            else if (particle.SubType == 11)
            {
                if (R(4) != 0)
                {
                    particle.TexType = ClassicTextureIds.BitmapSmoke;
                }

                particle.LifeTime = 30f;
                particle.Scale = sourceScale;
                particle.Rotation = R(360);
                particle.Position.X += (R(20) - 10) * frameFactor;
                particle.Position.Y += (R(20) - 10) * frameFactor;
                particle.Position.Z += (R(20) - 10) * frameFactor;
                particle.Gravity = -1.5f;
            }
        }

        private void CopyTargetPositionToStart(
            ref ClassicParticle particle)
        {
            if (TryGetOwnerPosition(
                    particle.Target,
                    out Vector3 targetPosition))
            {
                particle.StartPosition =
                    targetPosition;
            }
        }

        private void TrySetPositionToRandomTargetBone(
            ref ClassicParticle particle)
        {
            if (!TryGetOwnerWorldObject(
                    particle.Target,
                    out WorldObject worldObject) ||
                worldObject is not ModelObject modelObject)
            {
                return;
            }

            Matrix[] bones =
                modelObject.GetBoneTransforms();

            if (bones == null ||
                bones.Length == 0)
            {
                return;
            }

            int boneIndex =
                R(bones.Length);

            Matrix worldMatrix =
                bones[boneIndex] *
                modelObject.WorldPosition;

            particle.Position =
                worldMatrix.Translation;
        }

        /// <summary>
        /// Port de HandPosition(PARTICLE*).
        /// Se usa en el bloque posterior para FLARE_RED / SHINY.
        /// Queda aquí para que CreateParticle mantenga una sola implementación.
        /// </summary>
        private bool TryApplyClassicHandPosition(
            ref ClassicParticle particle)
        {
            if (!TryGetOwnerWorldObject(
                    particle.Target,
                    out WorldObject worldObject) ||
                worldObject is not PlayerObject player)
            {
                return false;
            }

            bool isLeftHand =
                (particle.SubType & 1) == 0;

            if (!player.TryGetHandWorldMatrix(
                    isLeftHand,
                    out Matrix handWorld))
            {
                return false;
            }

            Vector3 localOffset =
                particle.Type == ClassicTextureIds.BitmapFlareRed ||
                particle.Type == ClassicTextureIds.BitmapShiny + 2
                    ? new Vector3(0f, -120f, 0f)
                    : Vector3.Zero;

            particle.Position =
                Vector3.Transform(
                    localOffset,
                    handWorld);

            return true;
        }

        private Vector3 Rotate(
            in Vector3 value,
            in Vector3 angle)
        {
            ClassicMatrix3x4 matrix =
                ClassicMath.AngleMatrix(
                    angle);

            return ClassicMath.VectorRotate(
                value,
                matrix);
        }

        private int R(
            int exclusiveMaximum)
        {
            return Random.Modulo(
                exclusiveMaximum);
        }
    }
}
