using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// CreateParticle() — bloque D.
        ///
        /// Corresponde a las familias finales del switch original:
        ///
        /// WATERFALL_5 .. DAMAGE2.
        /// </summary>
        private bool InitializeParticleCreateD(
            ref ClassicParticle particle,
            in Vector3 sourcePosition,
            in Vector3 sourceAngle,
            in Vector3 sourceLight,
            float sourceScale)
        {
            float frameFactor =
                Clock.FrameFactor;

            switch (particle.Type)
            {
                case ClassicTextureIds.BitmapWaterfall5:
                {
                    switch (particle.SubType)
                    {
                        case 0:
                            particle.LifeTime = 30f;
                            particle.Rotation = R(360);
                            particle.Scale = 1.6f;
                            particle.Velocity.Z = -(R(5) + 7f);
                            particle.Light = new Vector3(0.2f);
                            break;

                        case 1:
                            particle.LifeTime = 20f;
                            particle.Rotation = R(360);
                            particle.Scale = 1f;
                            particle.Position.Z += 50f * frameFactor;
                            break;

                        case 2:
                            particle.LifeTime = 20f;
                            particle.Rotation = R(360);
                            particle.Scale = 0.5f;
                            particle.Position.Z += 50f * frameFactor;
                            particle.Velocity.Z = R(5) + 10f;
                            break;

                        case 3:
                            particle.LifeTime = 20f;
                            particle.Rotation = R(360);
                            particle.Scale = 1.6f;
                            particle.Velocity.Z = -(R(5) + 7f);
                            break;

                        case 4:
                            particle.LifeTime = 6f;
                            particle.Gravity =
                                (R(100) / 100f) * 4f +
                                particle.Angle.X * 1.2f;
                            particle.Scale =
                                sourceScale +
                                R(6) * 0.20f;
                            particle.Rotation = R(360);
                            particle.StartPosition =
                                particle.Position;
                            break;

                        case 5:
                            particle.LifeTime = 30f;
                            particle.Rotation = R(360);
                            particle.Scale =
                                1.6f *
                                sourceScale;
                            particle.Velocity.Z =
                                -(R(5) + 10f);
                            particle.Light =
                                new Vector3(0.2f);
                            break;

                        case 6:
                            particle.LifeTime = 30f;
                            particle.Rotation = R(360);
                            particle.Scale =
                                sourceScale +
                                0.6f;
                            particle.Velocity.Z =
                                -(R(5) + 12f);
                            particle.Light =
                                new Vector3(0.2f);
                            break;

                        case 7:
                            particle.TexType =
                                Random.FpsCheck(2, Clock)
                                    ? ClassicTextureIds.BitmapWaterfall4
                                    : ClassicTextureIds.BitmapWaterfall5;

                            particle.LifeTime = 30f;
                            particle.Rotation = R(360);
                            particle.Scale =
                                R(50) * 0.05f +
                                sourceScale;

                            particle.Velocity =
                                new Vector3(
                                    -(R(2) + 1f),
                                    -(R(2) + 3f),
                                    R(3) + 3f);
                            break;

                        case 8:
                            particle.LifeTime = 30f;
                            particle.Velocity.Z =
                                R(3) + 1f;
                            particle.Scale +=
                                (R(5) + 5f) *
                                0.05f *
                                frameFactor;
                            break;

                        case 9:
                            particle.LifeTime = 30f;
                            particle.Rotation = R(360);
                            particle.Scale =
                                0.6f +
                                sourceScale;
                            particle.Velocity.Z =
                                -(R(5) + 7f);
                            particle.Light =
                                new Vector3(0.2f);
                            break;
                    }

                    return true;
                }

                case ClassicTextureIds.BitmapPlus:
                {
                    particle.LifeTime =
                        20f;

                    particle.Scale =
                        sourceScale;

                    particle.Position.X +=
                        (R(30) - 15f) *
                        frameFactor;

                    particle.Position.Y +=
                        (R(30) - 15f) *
                        frameFactor;

                    particle.Position.Z +=
                        240f *
                        frameFactor;

                    return true;
                }

                case ClassicTextureIds.BitmapWaterfall2:
                {
                    particle.LifeTime =
                        30f;

                    particle.Rotation =
                        R(360);

                    if (particle.SubType == 2)
                    {
                        particle.LifeTime =
                            40f;

                        particle.Scale =
                            (R(6) + 6f) *
                            0.1f +
                            sourceScale;
                    }
                    else if (particle.SubType == 6)
                    {
                        particle.LifeTime =
                            R(50) +
                            20f;

                        particle.Gravity =
                            R(20) +
                            10f;

                        particle.Scale *=
                            0.2f;

                        particle.StartPosition =
                            particle.Position;
                    }
                    else
                    {
                        particle.Scale =
                            (R(6) + 6f) *
                            0.1f;
                    }

                    particle.Velocity.Z =
                        -(R(3) + 3f);

                    // Main:
                    //
                    // SceneFlag == CHARACTER_SCENE
                    //
                    // CHARACTER_SCENE corresponde a la escena de selección.
                    if (World?.Scene is SelectCharacterScene)
                    {
                        particle.Light =
                            new Vector3(
                                0.25f);
                    }
                    else
                    {
                        particle.Light =
                            new Vector3(
                                0.4f);
                    }

                    particle.Position.X +=
                        (R(20) - 10f) *
                        frameFactor;

                    particle.Position.Y +=
                        (R(20) - 10f) *
                        frameFactor;

                    particle.Position.Z +=
                        (R(40) - 20f) *
                        frameFactor;

                    if (particle.SubType == 1)
                    {
                        particle.Light =
                            new Vector3(
                                0f,
                                0.4f,
                                0.4f);

                        particle.LifeTime =
                            50f;

                        particle.Velocity.Z =
                            -(R(3) + 3f);
                    }

                    if (particle.SubType == 3)
                    {
                        particle.TexType =
                            ClassicTextureIds.BitmapLight +
                            2;

                        particle.Light =
                            sourceLight;

                        particle.LifeTime =
                            12f;

                        particle.Rotation =
                            R(360);

                        particle.Scale =
                            (R(3) + 3f) *
                            0.18f;

                        particle.Velocity.Z =
                            2f;

                        particle.Position.X +=
                            (R(20) - 10f) *
                            frameFactor;

                        particle.Position.Y +=
                            (R(20) - 10f) *
                            frameFactor;

                        particle.Position.Z +=
                            (R(40) - 20f) *
                            frameFactor;
                    }

                    if (particle.SubType == 4)
                    {
                        particle.LifeTime =
                            70f;

                        particle.Scale =
                            (R(6) + 6f) *
                            0.1f +
                            sourceScale;
                    }

                    if (particle.SubType == 5)
                    {
                        particle.LifeTime =
                            40f;

                        particle.Rotation =
                            R(360);

                        particle.Gravity =
                            R(2) +
                            2f;

                        particle.Scale =
                            (R(6) + 6f) *
                            0.1f +
                            sourceScale;

                        particle.Velocity.X =
                            -(R(2) + 2f);

                        particle.Velocity.Y =
                            -(R(2) + 2f);

                        particle.Velocity.Z =
                            R(2) +
                            1f;

                        particle.Position.X +=
                            (R(60) - 30f) *
                            frameFactor;

                        particle.Position.Y +=
                            (R(60) - 30f) *
                            frameFactor;

                        particle.Position.Z +=
                            R(10) *
                            frameFactor;

                        return true;
                    }

                    if (particle.SubType == 11)
                    {
                        particle.LifeTime =
                            30f;

                        particle.Velocity.Z =
                            R(5) +
                            5f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.05f *
                            sourceScale;
                    }

                    return true;
                }

                case ClassicTextureIds.BitmapWaterfall3:
                case ClassicTextureIds.BitmapWaterfall4:
                {
                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Velocity.Z =
                            R(5) +
                            2f;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime =
                            10f;

                        particle.Velocity.Z =
                            R(2) +
                            2f;
                    }
                    else if (particle.SubType == 2)
                    {
                        particle.LifeTime =
                            6f;

                        particle.Gravity =
                            (R(100) / 100f) *
                            4f +
                            particle.Angle.X *
                            1.2f;

                        particle.Scale =
                            sourceScale +
                            R(6) *
                            0.10f;

                        particle.Rotation =
                            R(360);

                        particle.StartPosition =
                            particle.Position;

                        return true;
                    }
                    else if (particle.SubType == 3)
                    {
                        particle.LifeTime =
                            60f;

                        particle.Velocity.Z =
                            -1f;

                        particle.Scale *=
                            (R(10) + 15f) *
                            0.02f;

                        particle.Rotation =
                            R(360);

                        particle.Position.X +=
                            (R(40) - 20f) *
                            frameFactor;

                        particle.Position.Z +=
                            (R(25) - 10f) *
                            frameFactor;

                        return true;
                    }
                    else if (particle.SubType == 12)
                    {
                        particle.Rotation =
                            R(360);

                        particle.LifeTime =
                            (R(2) - 1f) +
                            5f;

                        float intervalScale =
                            sourceScale *
                            0.3f;

                        particle.Scale +=
                            (
                                (R(20) - 10f) /
                                2f
                            ) *
                            (
                                intervalScale /
                                2f
                            ) *
                            frameFactor;

                        particle.Position.X +=
                            (R(20) - 10f) *
                            frameFactor;

                        particle.Position.Y +=
                            (R(20) - 10f) *
                            frameFactor;

                        particle.Position.Z +=
                            (R(20) - 10f) *
                            frameFactor;

                        return true;
                    }
                    else if (particle.SubType == 16)
                    {
                        // ASG_ADD_MAP_KARUTAN
                        particle.LifeTime =
                            30f;

                        particle.Rotation =
                            R(360);

                        particle.Scale =
                            0.6f +
                            sourceScale;

                        particle.Velocity.Z =
                            R(3) +
                            5f;

                        return true;
                    }

                    particle.Rotation =
                        R(360);

                    particle.Scale =
                        (R(10) + 10f) *
                        0.02f;

                    if (particle.SubType == 4)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Velocity.Z =
                            R(5) +
                            2f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.02f +
                            sourceScale;
                    }
                    else if (particle.SubType == 5 ||
                             particle.SubType == 6)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Gravity =
                            (R(100) / 100f) *
                            2f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.02f +
                            sourceScale;

                        particle.Rotation =
                            R(360);

                        particle.Position.X +=
                            (R(40) - 20f) *
                            frameFactor;

                        particle.Position.Y +=
                            (R(40) - 20f) *
                            frameFactor;

                        particle.Position.Z -=
                            50f *
                            frameFactor;

                        return true;
                    }
                    else if (particle.SubType == 7)
                    {
                        particle.LifeTime =
                            60f;

                        particle.Velocity.Z =
                            R(4) *
                            0.1f +
                            0.8f;

                        particle.Scale *=
                            (R(10) + 15f) *
                            0.02f;

                        particle.Rotation =
                            R(360);

                        particle.Position.X +=
                            (R(18) - 9f) *
                            frameFactor;

                        particle.Position.Z +=
                            (R(18) - 9f) *
                            frameFactor;

                        return true;
                    }
                    else if (particle.SubType == 8)
                    {
                        particle.LifeTime =
                            30f;

                        particle.Velocity.Z =
                            R(5) +
                            5f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.05f;
                    }
                    else if (particle.SubType == 9)
                    {
                        particle.LifeTime =
                            30f;

                        particle.Velocity.Z =
                            R(5) +
                            2f;

                        particle.Scale =
                            (R(5) + 5f) *
                            0.05f +
                            sourceScale;
                    }
                    else if (particle.SubType == 10)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Gravity =
                            (R(200) / 100f) +
                            2f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.02f +
                            sourceScale;

                        particle.Rotation =
                            R(360);

                        particle.Velocity.X =
                            -(R(2) + 1f);

                        particle.Velocity.Y =
                            -(R(2) + 2f);
                    }
                    else if (particle.SubType == 11)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Scale =
                            sourceScale +
                            (R(10) - 5f) *
                            0.05f;

                        particle.Rotation =
                            R(360);

                        Vector3 local =
                            new(
                                0f,
                                5f,
                                0f);

                        float directionAngle =
                            particle.Angle.Z +
                            (R(90) - 45f) +
                            150f;

                        particle.Velocity =
                            Rotate(
                                local,
                                new Vector3(
                                    0f,
                                    0f,
                                    directionAngle));

                        return true;
                    }
                    else if (particle.SubType == 13)
                    {
                        particle.LifeTime =
                            30f;

                        particle.Scale =
                            sourceScale;

                        particle.Rotation =
                            R(360);

                        particle.Gravity =
                            2.5f +
                            R(10) *
                            0.1f;
                    }
                    else if (particle.SubType == 14)
                    {
                        particle.LifeTime =
                            30f;

                        particle.Velocity.Z =
                            R(5) +
                            5f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.05f *
                            sourceScale;
                    }
                    else if (particle.SubType == 15)
                    {
                        particle.LifeTime =
                            20f;

                        particle.Gravity =
                            (R(100) / 100f) *
                            2f;

                        particle.Scale =
                            (R(10) + 10f) *
                            0.02f +
                            sourceScale;

                        particle.Rotation =
                            R(360);

                        particle.Position.X +=
                            (R(40) - 20f) *
                            frameFactor;

                        particle.Position.Y +=
                            (R(40) - 20f) *
                            frameFactor;

                        particle.Position.Z -=
                            50f *
                            frameFactor;

                        return true;
                    }

                    particle.Position.X +=
                        (R(40) - 20f) *
                        frameFactor;

                    particle.Position.Y +=
                        (R(40) - 20f) *
                        frameFactor;

                    particle.Position.Z +=
                        (R(20) - 10f) *
                        frameFactor;

                    return true;
                }

                case ClassicTextureIds.BitmapShockWave:
                {
                    if (particle.SubType == 3)
                    {
                        particle.LifeTime =
                            7f;
                    }
                    else if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            7f;

                        particle.Scale =
                            sourceScale;

                        particle.Light =
                            sourceLight;
                    }

                    if (particle.SubType == 4)
                    {
                        particle.Alpha =
                            1f;

                        particle.LifeTime =
                            7f;

                        particle.Scale =
                            sourceScale;

                        particle.Gravity =
                            6f;

                        particle.Light =
                            sourceLight;
                    }

                    return true;
                }

                case ClassicTextureIds.BitmapGmAurora:
                {
                    particle.LifeTime =
                        20f;

                    return true;
                }

                case ClassicTextureIds.BitmapCursedTempleEffectMasker:
                {
                    particle.LifeTime =
                        30f;

                    return true;
                }

                case ClassicTextureIds.BitmapRaklionClouds:
                {
                    particle.Alpha =
                        1f;

                    particle.LifeTime =
                        32f;

                    particle.Rotation =
                        R(360);

                    return true;
                }

                case ClassicTextureIds.BitmapChrome2:
                {
                    particle.LifeTime =
                        R(5) +
                        5f;

                    particle.Rotation =
                        R(360);

                    particle.StartPosition =
                        sourcePosition;

                    particle.Scale *=
                        1f +
                        R(10) *
                        0.03f;

                    return true;
                }

                case ClassicTextureIds.BitmapAgAdditionEffect:
                {
                    float computedScale =
                        particle.Scale;

                    if (particle.SubType == 0)
                    {
                        particle.LifeTime =
                            33f +
                            R(5);

                        particle.Rotation =
                            R(90) +
                            270f;

                        computedScale =
                            (R(20) + 20f) /
                            50f *
                            0.5f;
                    }
                    else if (particle.SubType == 1)
                    {
                        particle.LifeTime =
                            27f +
                            R(5);

                        particle.Rotation =
                            R(90);

                        computedScale =
                            (R(20) + 20f) /
                            50f *
                            1.5f;
                    }
                    else if (particle.SubType == 2)
                    {
                        particle.LifeTime =
                            38f +
                            R(5);

                        particle.Rotation =
                            R(90) +
                            135f;

                        computedScale =
                            (R(20) + 20f) /
                            50f;
                    }

                    particle.Scale =
                        computedScale;

                    particle.Gravity =
                        (R(16) + 12f) *
                        0.1f;

                    particle.Alpha =
                        0f;

                    particle.TurningForce =
                        new Vector3(
                            1f,
                            0f,
                            0.6f);

                    particle.Light =
                        Vector3.Zero;

                    return true;
                }

                case ClassicTextureIds.BitmapSbumb:
                {
                    particle.LifeTime =
                        4f;

                    particle.Scale =
                        sourceScale;

                    return true;
                }

                case ClassicTextureIds.BitmapDamage1:
                {
                    particle.LifeTime =
                        5f;

                    particle.Scale =
                        sourceScale;

                    return true;
                }

                case ClassicTextureIds.BitmapSwordEffectMono:
                {
                    particle.LifeTime =
                        20f;

                    particle.Scale =
                        sourceScale;

                    return true;
                }

                case ClassicTextureIds.BitmapDamage2:
                {
                    particle.LifeTime =
                        15f;

                    particle.Scale =
                        sourceScale *
                        0.9f +
                        (R(2) + 2f) *
                        0.1f;

                    particle.Position.Z +=
                        (80f + R(20)) *
                        frameFactor;

                    return true;
                }
            }

            return false;
        }
    }
}
