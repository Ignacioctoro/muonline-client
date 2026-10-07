using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Rendering
{
    /// <summary>
    /// Port del RenderParticles() clásico.
    ///
    /// Responsabilidades:
    ///
    /// - resolver Width / Height desde TexType;
    /// - conservar el estado clásico de blend/depth durante el loop;
    /// - aplicar atlas y UV originales;
    /// - respetar usos especiales de Type vs TexType;
    /// - permitir múltiples RenderSprite() por una sola PARTICLE;
    /// - conservar mutaciones que el Main realiza durante render
    ///   (por ejemplo Rotation y Light).
    ///
    /// La simulación CreateParticle()/MoveParticles() sigue viviendo
    /// en ClassicFxRuntime.Particles.cs.
    /// </summary>
    internal sealed class ClassicParticleRenderer
    {
        private readonly
            ClassicBillboardRenderer
            _billboards;

        private readonly
            ClassicTextureRepository
            _textures;

        // RenderParticles() modifica DepthTest y DepthMask como estados
        // independientes. Los conservamos explícitamente durante el loop.
        private bool
            _depthTestEnabled;

        private bool
            _depthWriteEnabled;

        public ClassicParticleRenderer(
            ClassicBillboardRenderer billboards,
            ClassicTextureRepository textures)
        {
            _billboards =
                billboards ??
                throw new ArgumentNullException(
                    nameof(billboards));

            _textures =
                textures ??
                throw new ArgumentNullException(
                    nameof(textures));
        }

        public void Begin()
        {
            _billboards.Begin();

            // WorldControl/ClassicBillboardRenderer dejan Default
            // antes de comenzar este bloque.
            //
            // Equivalente inicial:
            //
            // DepthTest = true
            // DepthMask = true
            _depthTestEnabled =
                true;

            _depthWriteEnabled =
                true;
        }

        public void QueueParticle(
            ref ClassicParticle particle,
            int particleIndex,
            float frameFactor,
            float worldTimeMilliseconds)
        {
            if (!_textures.TryGet(
                    particle.TexType,
                    out ClassicTextureResource resource))
            {
                return;
            }

            float width =
                resource.Data.Width *
                particle.Scale;

            float height =
                resource.Data.Height *
                particle.Scale;

            // ---------------------------------------------------------
            // Estado base de RenderParticles().
            // ---------------------------------------------------------

            ClassicBlendMode blendMode;

            if (resource.Data.Components ==
                3)
            {
                // EnableAlphaBlend()
                blendMode =
                    ClassicBlendMode.Glow;

                ApplyBlendSideEffects(
                    blendMode);
            }
            else
            {
                // EnableAlphaTest(false)
                //
                // No fuerza DepthMask.
                blendMode =
                    ClassicBlendMode.AlphaTest;
            }

            // Main:
            //
            // if (o->Type == BITMAP_LIGHT &&
            //     o->SubType == 6)
            //     EnableDepthTest();
            if (particle.Type ==
                    ClassicTextureIds.BitmapLight &&
                particle.SubType ==
                    6)
            {
                _depthTestEnabled =
                    true;
            }

            // Main:
            //
            // if (o->Type == BITMAP_EXPLOTION &&
            //     o->SubType == 5)
            //     DisableDepthTest();
            if (particle.Type ==
                    ClassicTextureIds.BitmapExplotion &&
                particle.SubType ==
                    5)
            {
                _depthTestEnabled =
                    false;
            }

            switch (particle.Type)
            {
                case ClassicTextureIds.BitmapWaterfall1:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapBubble:
                {
                    int frame =
                        particle.Frame %
                        9;

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        0f,
                        blendMode,
                        frame % 3 * 0.25f + 0.005f,
                        frame / 3 * 0.25f + 0.005f,
                        0.25f - 0.01f,
                        0.25f - 0.01f);

                    break;
                }

                case ClassicTextureIds.BitmapSpotWater:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height *
                            0.125f,
                        particle.Light,
                        particle.Angle.X,
                        blendMode,
                        0f,
                        particle.Frame % 8 * 0.125f,
                        1f,
                        0.125f);

                    break;
                }

                case ClassicTextureIds.BitmapSpark + 2:
                {
                    if (particle.SubType ==
                            0 ||
                        particle.SubType ==
                            2 ||
                        particle.SubType ==
                            3)
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            0f,
                            blendMode,
                            particle.Frame % 2 * 0.5f,
                            particle.Frame / 2 * 0.5f,
                            0.5f,
                            0.5f);
                    }

                    break;
                }

                case ClassicTextureIds.BitmapExplotionMono:
                case ClassicTextureIds.BitmapExplotion:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        0f,
                        blendMode,
                        particle.Frame % 4 * 0.25f + 0.005f,
                        particle.Frame / 4 * 0.25f + 0.005f,
                        0.25f - 0.01f,
                        0.25f - 0.01f);

                    break;
                }

                case ClassicTextureIds.BitmapExplotion + 1:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width *
                            0.25f,
                        height,
                        particle.Light,
                        particle.Angle.X,
                        blendMode,
                        particle.Frame % 4 * 0.25f,
                        0f,
                        0.25f,
                        1f);

                    break;
                }

                case ClassicTextureIds.BitmapSummonSahamutExplosion:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        0f,
                        blendMode,
                        particle.Frame % 4 * 0.25f + 0.005f,
                        particle.Frame / 4 * 0.25f + 0.005f,
                        0.25f - 0.01f,
                        0.25f - 0.01f);

                    break;
                }

                case ClassicTextureIds.BitmapClud64:
                {
                    if (particle.SubType ==
                            0 ||
                        particle.SubType ==
                            5 ||
                        particle.SubType ==
                            11)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapTorchFire:
                {
                    Vector3 position =
                        particle.Position;

                    for (int i = 0;
                         i < 3;
                         i++)
                    {
                        QueueByTextureType(
                            particle.Type,
                            position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);

                        position.Z -=
                            10f *
                            frameFactor;
                    }

                    break;
                }

                case ClassicTextureIds.BitmapGhostCloud1:
                case ClassicTextureIds.BitmapGhostCloud2:
                case ClassicTextureIds.BitmapLight + 3:
                {
                    QueueByTextureType(
                        particle.Type,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapTwinTailWater:
                {
                    SetBlendMode(
                        ref blendMode,
                        ClassicBlendMode.Glow);

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSmoke:
                {
                    if (particle.SubType ==
                            2 ||
                        particle.SubType ==
                            5 ||
                        particle.SubType ==
                            12 ||
                        particle.SubType ==
                            14 ||
                        particle.SubType ==
                            15 ||
                        particle.SubType ==
                            20 ||
                        particle.SubType ==
                            21 ||
                        particle.SubType ==
                            29 ||
                        particle.SubType ==
                            37 ||
                        particle.SubType ==
                            38 ||
                        particle.SubType ==
                            59)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }

                    float rotation =
                        particle.SubType ==
                            6
                            ? 0f
                            : particle.Rotation;

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSmoke + 1:
                case ClassicTextureIds.BitmapSmoke + 4:
                {
                    // EnableAlphaBlend3()
                    SetBlendMode(
                        ref blendMode,
                        ClassicBlendMode.Alpha);

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapAdvSmoke + 1:
                {
                    // Main, SubType 2:
                    //
                    // SetTexEnv(..., GL_ADD)
                    // EnableAlphaBlend3()
                    //
                    // El blend framebuffer sí se porta aquí.
                    //
                    // GL_ADD es una operación de texture-combine previa
                    // al blending y necesita un shader dedicado en MonoGame.
                    // No la sustituimos por Glow/Subtract/Luminance porque
                    // serían operaciones distintas.
                    SetBlendMode(
                        ref blendMode,
                        ClassicBlendMode.Alpha);

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSmoke + 3:
                {
                    if (particle.SubType ==
                            3 ||
                        particle.SubType ==
                            4)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }
                    else
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Alpha);
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapLightning:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width *
                            0.25f,
                        height,
                        particle.Light,
                        particle.Angle.X,
                        blendMode,
                        particle.Frame % 4 * 0.25f,
                        0f,
                        0.25f,
                        1f);

                    break;
                }

                case ClassicTextureIds.BitmapBlood + 1:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        0f,
                        blendMode,
                        particle.Frame % 2 * 0.5f,
                        particle.Frame / 2 * 0.5f,
                        0.5f,
                        0.5f,
                        alphaOverride:
                            1f);

                    break;
                }

                case ClassicTextureIds.BitmapChromeEnergy2:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width *
                            0.25f,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode,
                        particle.Frame % 4 * 0.25f,
                        0f,
                        0.25f,
                        1f);

                    break;
                }

                case ClassicTextureIds.BitmapFireCursedLich:
                case ClassicTextureIds.BitmapFireHik2Mono:
                case ClassicTextureIds.BitmapLeafTotemGolem:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapFire:
                case ClassicTextureIds.BitmapFire + 2:
                case ClassicTextureIds.BitmapFire + 3:
                {
                    if (particle.SubType ==
                            17 ||
                        particle.SubType ==
                            5 ||
                        particle.SubType ==
                            7 ||
                        particle.SubType ==
                            8 ||
                        particle.SubType ==
                            11 ||
                        particle.SubType ==
                            12 ||
                        particle.SubType ==
                            13)
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width *
                                0.25f,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode,
                            particle.Frame % 4 * 0.25f,
                            0f,
                            0.25f,
                            1f);
                    }
                    else if (particle.SubType ==
                             18)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Alpha);

                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }
                    else if (particle.SubType ==
                                14 ||
                             particle.SubType ==
                                15)
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }
                    else
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width *
                                0.25f,
                            height,
                            particle.Light,
                            particle.Angle.X,
                            blendMode,
                            particle.Frame % 4 * 0.25f,
                            0f,
                            0.25f,
                            1f);
                    }

                    break;
                }

                case ClassicTextureIds.BitmapFirecracker:
                {
                    RenderFirecracker(
                        resource,
                        ref particle,
                        particleIndex,
                        width,
                        height,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapFlare:
                {
                    if (particle.SubType ==
                        11)
                    {
                        float flareWidth =
                            resource.Data.Width *
                            0.5f *
                            particle.Scale;

                        float flareHeight =
                            resource.Data.Height *
                            0.4f;

                        Queue(
                            resource,
                            particle.Position,
                            flareWidth,
                            flareHeight,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }
                    else if (particle.SubType !=
                             4)
                    {
                        if (particle.LifeTime !=
                            60f)
                        {
                            Queue(
                                resource,
                                particle.Position,
                                width,
                                height,
                                particle.Light,
                                particle.Rotation,
                                blendMode);
                        }
                    }
                    else
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }

                    break;
                }

                case ClassicTextureIds.BitmapFlareBlue:
                {
                    if (particle.SubType ==
                        0)
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }
                    else if (particle.SubType ==
                             1)
                    {
                        float flareWidth =
                            resource.Data.Width *
                            0.2f *
                            particle.Scale;

                        float flareHeight =
                            resource.Data.Height *
                            0.3f;

                        Queue(
                            resource,
                            particle.Position,
                            flareWidth,
                            flareHeight,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }

                    break;
                }

                case ClassicTextureIds.BitmapFlare + 1:
                {
                    if (particle.SubType ==
                        0)
                    {
                        Queue(
                            resource,
                            particle.Position,
                            width,
                            height,
                            particle.Light,
                            particle.Rotation,
                            blendMode);
                    }

                    break;
                }

                case ClassicTextureIds.BitmapLight + 2:
                {
                    if (particle.SubType ==
                            3 ||
                        particle.SubType ==
                            4 ||
                        particle.SubType ==
                            6)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapMagic + 1:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapCloud:
                {
                    switch (particle.SubType)
                    {
                        case 10:
                        case 12:
                        case 7:
                        case 14:
                        case 16:
                        {
                            SetBlendMode(
                                ref blendMode,
                                ClassicBlendMode.Subtract);

                            break;
                        }

                        case 0:
                        case 8:
                        case 3:
                        case 18:
                        {
                            float direction =
                                (particleIndex %
                                 2) ==
                                0
                                    ? 1f
                                    : -1f;

                            particle.Rotation =
                                (
                                    worldTimeMilliseconds *
                                    0.02f *
                                    direction *
                                    particle.TurningForce.X
                                ) +
                                particle.StartPosition.Y;

                            break;
                        }
                    }

                    Vector3 light =
                        particle.Light;

                    if (particle.SubType ==
                            8 ||
                        particle.SubType ==
                            9 ||
                        particle.SubType ==
                            20 ||
                        particle.SubType ==
                            21)
                    {
                        light *=
                            particle.Alpha;
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSpark:
                {
                    if (particle.SubType ==
                        10)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapFlame:
                case ClassicTextureIds.BitmapCursedTempleEffectMasker:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapShiny + 6:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    QueueByTextureType(
                        ClassicTextureIds.BitmapLight,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSmokeLine2:
                {
                    if (particle.SubType ==
                        3)
                    {
                        SetBlendMode(
                            ref blendMode,
                            ClassicBlendMode.Subtract);
                    }

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSbumb:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width *
                            0.25f,
                        height,
                        particle.Light,
                        0f,
                        blendMode,
                        particle.Frame % 4 * 0.25f + 0.005f,
                        0f,
                        0.25f - 0.01f,
                        1f);

                    break;
                }

                case ClassicTextureIds.BitmapDamage1:
                {
                    // Main muta o->Light dentro de RenderParticles().
                    particle.Light *=
                        2f;

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapSwordEffectMono:
                {
                    Vector3 position =
                        particle.Position;

                    position.Z +=
                        31f *
                        particle.Scale *
                        frameFactor;

                    Queue(
                        resource,
                        position,
                        width *
                            0.9f,
                        height *
                            1.1f,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapDamage2:
                {
                    Vector3 light =
                        particle.Light *
                        1.4f;

                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        light,
                        particle.Rotation,
                        blendMode);

                    break;
                }

                case ClassicTextureIds.BitmapTrueFire:
                default:
                {
                    Queue(
                        resource,
                        particle.Position,
                        width,
                        height,
                        particle.Light,
                        particle.Rotation,
                        blendMode);

                    break;
                }
            }

            // Main hace el clamp al final de RenderParticles().
            if (particle.LifeTime <
                0f)
            {
                particle.LifeTime =
                    0f;
            }
        }

        public void End()
        {
            _billboards.End();
        }

        private void RenderFirecracker(
            ClassicTextureResource resource,
            ref ClassicParticle particle,
            int particleIndex,
            float width,
            float height,
            ClassicBlendMode blendMode)
        {
            int count =
                particleIndex %
                8 +
                22;

            int temp =
                (int)(
                    particle.LifeTime /
                    4f +
                    particle.SubType);

            int colorIndex =
                temp /
                10;

            int colorChange =
                temp %
                10;

            int lifeFactor =
                Math.Min(
                    (int)particle.LifeTime,
                    10);

            for (int j = count;
                 j >= 0;
                 j--)
            {
                Vector3 position =
                    particle.Position -
                    particle.Velocity *
                    (
                        j *
                        0.1f
                    );

                int distanceFactor =
                    Math.Min(
                        count -
                        j,
                        10);

                float commonFactor =
                    distanceFactor *
                    lifeFactor *
                    0.001f;

                Vector3 light =
                    new(
                        InterpolateFirecrackerChannel(
                            particle.Light,
                            0,
                            colorIndex,
                            colorChange) *
                        commonFactor,

                        InterpolateFirecrackerChannel(
                            particle.Light,
                            1,
                            colorIndex,
                            colorChange) *
                        commonFactor,

                        InterpolateFirecrackerChannel(
                            particle.Light,
                            2,
                            colorIndex,
                            colorChange) *
                        commonFactor);

                Queue(
                    resource,
                    position,
                    width,
                    height,
                    light,
                    particle.Rotation,
                    blendMode);
            }
        }

        private static float
            InterpolateFirecrackerChannel(
                Vector3 source,
                int channel,
                int colorIndex,
                int colorChange)
        {
            float current =
                GetComponent(
                    source,
                    (
                        channel +
                        colorIndex
                    ) %
                    3);

            float next =
                GetComponent(
                    source,
                    (
                        channel +
                        colorIndex +
                        1
                    ) %
                    3);

            return
                current *
                (
                    10 -
                    colorChange
                ) +
                next *
                colorChange;
        }

        private static float GetComponent(
            Vector3 value,
            int index)
        {
            return index switch
            {
                0 =>
                    value.X,

                1 =>
                    value.Y,

                _ =>
                    value.Z
            };
        }

        private void SetBlendMode(
            ref ClassicBlendMode current,
            ClassicBlendMode requested)
        {
            current =
                requested;

            ApplyBlendSideEffects(
                requested);
        }

        /// <summary>
        /// EnableAlphaBlend / Minus / Blend2 / Blend3
        /// llaman DisableDepthMask().
        ///
        /// EnableAlphaTest(false) NO lo hace.
        /// </summary>
        private void ApplyBlendSideEffects(
            ClassicBlendMode blendMode)
        {
            if (blendMode !=
                ClassicBlendMode.AlphaTest)
            {
                _depthWriteEnabled =
                    false;
            }
        }

        private ClassicDepthMode
            ResolveDepthMode()
        {
            if (!_depthTestEnabled)
            {
                return
                    ClassicDepthMode.Disabled;
            }

            return
                _depthWriteEnabled
                    ? ClassicDepthMode.ReadWrite
                    : ClassicDepthMode.ReadOnly;
        }

        private void Queue(
            ClassicTextureResource resource,
            Vector3 position,
            float width,
            float height,
            Vector3 light,
            float rotation,
            ClassicBlendMode blendMode,
            float u = 0f,
            float v = 0f,
            float uWidth = 1f,
            float vHeight = 1f,
            float? alphaOverride = null)
        {
            _billboards.Queue(
                resource,
                position,
                width,
                height,
                light,
                rotation,
                blendMode,
                ResolveDepthMode(),
                u,
                v,
                uWidth,
                vHeight,
                alphaOverride);
        }

        private void QueueByTextureType(
            int textureType,
            Vector3 position,
            float width,
            float height,
            Vector3 light,
            float rotation,
            ClassicBlendMode blendMode,
            float u = 0f,
            float v = 0f,
            float uWidth = 1f,
            float vHeight = 1f,
            float? alphaOverride = null)
        {
            if (!_textures.TryGet(
                    textureType,
                    out ClassicTextureResource resource))
            {
                return;
            }

            Queue(
                resource,
                position,
                width,
                height,
                light,
                rotation,
                blendMode,
                u,
                v,
                uWidth,
                vHeight,
                alphaOverride);
        }
    }
}
