using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Data.Texture;
using Client.Main.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Data
{
    /// <summary>
    /// Una textura ya resuelta desde el Data clásico.
    /// </summary>
    public sealed class ClassicTextureResource
    {
        public int Type
        {
            get;
        }

        public string Path
        {
            get;
        }

        public TextureData Data
        {
            get;
        }

        public Texture2D Texture
        {
            get;
        }

        public SamplerState SamplerState
        {
            get;
        }

        public bool IsReady =>
            Data != null &&
            Texture != null &&
            !Texture.IsDisposed;

        public ClassicTextureResource(
            int type,
            string path,
            TextureData data,
            Texture2D texture,
            SamplerState samplerState)
        {
            Type =
                type;

            Path =
                path;

            Data =
                data;

            Texture =
                texture;

            SamplerState =
                samplerState;
        }
    }

    internal readonly struct ClassicTextureDefinition
    {
        public int Type
        {
            get;
        }

        public string Path
        {
            get;
        }

        public SamplerState SamplerState
        {
            get;
        }

        public ClassicTextureDefinition(
            int type,
            string path,
            SamplerState samplerState)
        {
            Type =
                type;

            Path =
                path;

            SamplerState =
                samplerState;
        }
    }

    internal readonly struct ClassicTextureLoadResult
    {
        public int Loaded
        {
            get;
        }

        public int Reused
        {
            get;
        }

        public int Failed
        {
            get;
        }

        public ClassicTextureLoadResult(
            int loaded,
            int reused,
            int failed)
        {
            Loaded =
                loaded;

            Reused =
                reused;

            Failed =
                failed;
        }
    }

    /// <summary>
    /// Equivalente del catálogo global Bitmaps[] utilizado por el Main.
    ///
    /// El catálogo se separa por familias ClassicFX para mantener trazabilidad
    /// con el cliente original:
    ///
    /// - CoreDefinitions:
    ///   mínimo compartido usado por Sprite y por validaciones iniciales.
    ///
    /// - ParticleDefinitions:
    ///   los BITMAP_* de Particle cuya textura fue resuelta contra los
    ///   LoadBitmap() del Main y el Data_Broyal actual, más los TexType
    ///   indirectos verificables usados desde CreateParticle().
    ///
    /// Particle tiene 98 tipos lógicos en ParticleTypes.json. Además,
    /// CreateParticle() puede cambiar TexType a slots auxiliares. Quedan
    /// deliberadamente sin mapping estático cuatro casos:
    ///
    /// - BITMAP_SMOKE + 2: sin LoadBitmap verificable.
    /// - BITMAP_SWORD_FORCE: sin LoadBitmap verificable.
    /// - BITMAP_CLOUD + 2: sin LoadBitmap global verificable.
    /// - BITMAP_CHROME + 2: slot reutilizado dinámicamente según el mapa.
    ///
    /// No se les asigna una textura "parecida": quedan visibles como una
    /// diferencia real de la referencia hasta resolver el sistema de slots
    /// dinámicos/map-specific del Main.
    /// </summary>
    public sealed class ClassicTextureRepository
    {
        private static readonly
            ClassicTextureDefinition[]
            CoreDefinitions =
            [
                new(
                    ClassicTextureIds.BitmapLight,
                    "Effect/flare01.jpg",
                    SamplerState.LinearClamp),

                // MuMain ZzzOpenData.cpp: BITMAP_MAGIC_ZIN -> mzine_typer2.jpg.
                // Data_Broyal contains Effect/mzine_typer2.OZJ.
                new(ClassicTextureIds.BitmapMagicZin,
                    "Effect/mzine_typer2.jpg", SamplerState.LinearClamp),

                // Native RenderTerrainAlphaBitmap(BITMAP_TWLIGHT).
                new(ClassicTextureIds.BitmapTwlight,
                    "Skill/twlighthik01.jpg", SamplerState.LinearClamp),

                // Main ZzzOpenData.cpp: BITMAP_SHOCK_WAVE -> Effect/ShockWave.jpg.
                // Physical Data_Broyal asset: Effect/Shockwave.OZJ.
                new(ClassicTextureIds.BitmapShockWave,
                    "Effect/Shockwave.jpg", SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny,
                    "Effect/Shiny01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlare,
                    "Effect/Flare.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareBlue,
                    "Effect/flareBlue.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareForce,
                    "Effect/NSkill.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareRed,
                    "Effect/flareRed.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFormationMark,
                    "Effect/FormationMark.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapPinLight,
                    "Effect/pin_lights.jpg",
                    SamplerState.LinearClamp),

                // MuMain ZzzOpenData.cpp: native Joint textures.
                // All paths were checked against the current Data_Broyal files.
                new(ClassicTextureIds.BitmapScolpionTail,
                    "Effect/ScolTail.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.Bitmap2LineGhost,
                    "Skill/2line_gost.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapDrainLifeGhost,
                    "Effect/gostmark01.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapForcePillar,
                    "Effect/force_Pillar.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapSwordEff,
                    "Effect/!SwordEff.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapGroundWind,
                    "Effect/ground_wind.jpg", SamplerState.LinearClamp),
                // Original lives under Effect/partCharge1 in MuMain;
                // Data_Broyal stores the actual OZJ in Effect/.
                new(ClassicTextureIds.BitmapLuckySealEffect,
                    "Effect/bujuckline.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapInferno,
                    "Effect/inferno.jpg", SamplerState.LinearClamp),

                // MuMain ZzzOpenData.cpp: verified original joint textures.
                // Data_Broyal: matching Effect/*.OZJ assets confirmed.
                new(ClassicTextureIds.BitmapJointSpark,
                    "Effect/Spark01.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapJointThunder,
                    "Effect/JointThunder01.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapJointSpirit,
                    "Effect/JointSpirit01.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapJointSpirit2,
                    "Effect/JointSpirit02.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapJointEnergy,
                    "Effect/JointLaser01.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapJointHealing,
                    "Effect/JointEnergy01.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapJointLaser + 1,
                    "Effect/JointLaser02.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapJointForce,
                    "Effect/motion_blur_r2.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapBlur + 1,
                    "Effect/motion_blur.jpg", SamplerState.PointClamp),
                // Main ZzzOpenData.cpp: native Blur/ObjectBlur textures.
                // Data_Broyal: matching Effect/*.OZJ assets verified.
                new(ClassicTextureIds.BitmapBlur,
                    "Effect/blur01.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 2,
                    "Effect/motion_blur_r.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 3,
                    "Effect/motion_mono.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 6,
                    "Effect/motion_blur_r3.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 7,
                    "Effect/gra.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 8,
                    "Effect/spinmark01.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapBlur + 9,
                    "Effect/flamestani.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapBlur + 10,
                    "Effect/sword_blur.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapBlur + 11,
                    "Effect/joint_sword_red.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapBlur + 12,
                    "Effect/motion_blur_r2.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapBlur + 13,
                    "Effect/motion_blur_r3.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapBlur2,
                    "Effect/blur02.jpg", SamplerState.PointClamp),
                new(ClassicTextureIds.BitmapLava,
                    "Effect/Lava.jpg", SamplerState.LinearClamp),
                new(ClassicTextureIds.BitmapFlare + 1,
                    "Effect/flare02.jpg", SamplerState.LinearWrap),
                new(ClassicTextureIds.BitmapFlash,
                    "Effect/Flashing.jpg", SamplerState.LinearClamp)
            ];

        /// <summary>
        /// Catálogo Particle completo en cuanto a texturas verificadas.
        ///
        /// Samplers traducidos desde LoadBitmap():
        ///
        /// GL_LINEAR  + CLAMP  -> LinearClamp
        /// GL_LINEAR  + REPEAT -> LinearWrap
        /// GL_NEAREST + CLAMP  -> PointClamp
        /// GL_NEAREST + REPEAT -> PointWrap
        ///
        /// Las rutas usan el casing real encontrado en Data_Broyal.
        /// TextureLoader sigue recibiendo la extensión lógica original
        /// (.jpg/.tga) y resuelve los contenedores .OZJ/.OZT.
        /// </summary>
        private static readonly
            ClassicTextureDefinition[]
            ParticleDefinitions =
            [
                // Indirect TexType usados por CreateParticle().
                new(
                    ClassicTextureIds.BitmapExtLoginImpact,
                    "Effect/Impack03.jpg",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapEventCloud,
                    "Effect/clouds2.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapChrome3,
                    "Effect/Chrome03.jpg",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapGroundSmoke,
                    "Effect/ground_smoke.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapAdvSmoke,
                    "Effect/fi01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapAdvSmoke + 1,
                    "Effect/fi02.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapAgAdditionEffect,
                    "Effect/mist01.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapBlood,
                    "Effect/blood01.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapBlood + 1,
                    "Effect/blood.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapBlueBlur,
                    "Skill/SwordEff.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapBubble,
                    "Object8/drop01.jpg",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapCherryBlossomEventFlower,
                    "Effect/cherryblossom/sakuras02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapCherryBlossomEventPetal,
                    "Effect/cherryblossom/sakuras01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapChrome2,
                    "Effect/Chrome02.jpg",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapChromeEnergy2,
                    "Effect/energy02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapCloud,
                    "Effect/clouds.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapClud64,
                    "Effect/clud64.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapCursedTempleEffectMasker,
                    "Effect/masker.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapDamage1,
                    "Effect/Damage1.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapDamage2,
                    "Effect/Damage2.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapDsEffect,
                    "Effect/BowE.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapEffect,
                    "Logo/chasellight.jpg",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapEnergy,
                    "Effect/Thunder01.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapExplotion,
                    "Effect/Explotion01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapExplotion + 1,
                    "Effect/DinoE.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapExplotionMono,
                    "Effect/explotion01mono.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFire,
                    "Effect/Fire01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFire + 1,
                    "Effect/Fire02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFire + 2,
                    "Effect/Fire03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFire + 3,
                    "Effect/Fire05.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireCursedLich,
                    "Effect/firehik02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireHik1,
                    "Effect/firehik01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireHik1Mono,
                    "Effect/firehik_mono01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireHik2Mono,
                    "Effect/firehik_mono02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireHik3,
                    "Effect/firehik03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireHik3Mono,
                    "Effect/firehik_mono03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFireRed,
                    "Effect/firered.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFirecracker,
                    "Effect/Fire04.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlame,
                    "Effect/Flame01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlare,
                    "Effect/Flare.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlare + 1,
                    "Effect/flare02.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapFlareBlue,
                    "Effect/flareBlue.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlareRed,
                    "Effect/flareRed.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapFlower01,
                    "Skill/flower1.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapFlower01 + 1,
                    "Skill/flower2.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapFlower01 + 2,
                    "Skill/flower3.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapGhostCloud1,
                    "Effect/ghosteffect01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapGhostCloud2,
                    "Effect/Ghosteffect02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLight + 2,
                    "Effect/cra_04.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapGmAurora,
                    "Skill/gmmzine.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapHole,
                    "Effect/hole.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapLight + 3,
                    "Effect/impack01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLight + 1,
                    "Object9/Impack03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLeafTotemGolem,
                    "Monster/totemgolem_leaf.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLight,
                    "Effect/flare01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLightning,
                    "Effect/lightning.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLightning + 1,
                    "Effect/lightning2.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapLightningMega1,
                    "Effect/lighting_mega01.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapLightningMega2,
                    "Effect/lighting_mega02.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapLightningMega3,
                    "Effect/lighting_mega03.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapMagic,
                    "Effect/Magic_Ground1.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapMagic + 1,
                    "Effect/Magic_Ground2.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapOrora,
                    "Effect/hikorora.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapPinLight,
                    "Effect/pin_lights.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapPlus,
                    "Effect/Plus.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapPoundingBall,
                    "Effect/PoundingBall.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapRainCircle,
                    "World1/rain02.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapRainCircle + 1,
                    "World10/rain03.tga",
                    SamplerState.PointClamp),

                new(
                    ClassicTextureIds.BitmapRaklionClouds,
                    "Effect/clouds3.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny + 4,
                    "Effect/ring.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSbumb,
                    "Effect/sbumb.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapShiny,
                    "Effect/Shiny01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny + 1,
                    "Effect/Shiny02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny + 2,
                    "Effect/Shiny03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShiny + 6,
                    "Effect/shiny05.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapShockWave,
                    "Effect/Shockwave.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSmoke,
                    "Effect/smoke01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSmoke + 1,
                    "Effect/smoke02.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSmoke + 3,
                    "Effect/smoke04.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSmoke + 4,
                    "Effect/smoke05.tga",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSmokeLine1,
                    "Effect/smokelines01.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapSmokeLine2,
                    "Effect/smokelines02.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapSmokeLine3,
                    "Effect/smokelines03.jpg",
                    SamplerState.LinearWrap),

                new(
                    ClassicTextureIds.BitmapSnowEffect1,
                    "Effect/snowseff01.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSnowEffect2,
                    "Effect/snowseff02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSpark,
                    "Effect/Spark02.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSpark + 1,
                    "Effect/Spark03.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSpark + 2,
                    "Effect/spark.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSpotWater,
                    "Effect/coll.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSummonSahamuttExplosion,
                    "Effect/loungexflow.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapSwordEffectMono,
                    "Effect/Swordeff_mono.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapTorchFire,
                    "Effect/Torchfire.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapTrueBlue,
                    "Effect/fantaB.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapTrueFire,
                    "Effect/fantaF.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapTwinTailWater,
                    "Effect/water.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapWaterfall1,
                    "Effect/waterFall1.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapWaterfall2,
                    "Effect/waterFall2.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapWaterfall3,
                    "Effect/waterFall3.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapWaterfall4,
                    "Effect/waterFall4.jpg",
                    SamplerState.LinearClamp),

                new(
                    ClassicTextureIds.BitmapWaterfall5,
                    "Effect/waterFall5.jpg",
                    SamplerState.LinearClamp)
            ];

        private readonly
            Dictionary<int, ClassicTextureResource>
            _resources =
                new();

        private bool
            _coreLoaded;

        private bool
            _particlesLoaded;

        public int LoadedTextureCount =>
            _resources.Count;

        public int KnownUnresolvedParticleTextureCount =>
            4;

        public async Task LoadCoreAsync()
        {
            if (_coreLoaded)
            {
                return;
            }

            ClassicTextureLoadResult result =
                await LoadDefinitionsAsync(
                    CoreDefinitions);

            _coreLoaded =
                true;

            Console.WriteLine(
                $"[ClassicFX] Core textures: " +
                $"{result.Loaded} loaded, " +
                $"{result.Reused} reused, " +
                $"{result.Failed} failed. " +
                $"Total={_resources.Count}.");
        }

        public async Task LoadParticlesAsync()
        {
            if (_particlesLoaded)
            {
                return;
            }

            // Core primero para que las texturas compartidas se reutilicen
            // y no se preparen dos veces.
            if (!_coreLoaded)
            {
                await LoadCoreAsync();
            }

            ClassicTextureLoadResult result =
                await LoadDefinitionsAsync(
                    ParticleDefinitions);

            _particlesLoaded =
                true;

            Console.WriteLine(
                $"[ClassicFX] Particle textures: " +
                $"{result.Loaded} loaded, " +
                $"{result.Reused} reused, " +
                $"{result.Failed} failed, " +
                $"{KnownUnresolvedParticleTextureCount} known unresolved. " +
                $"Total={_resources.Count}.");

            Console.WriteLine(
                $"[ClassicFX] Particle texture unresolved by reference: " +
                $"BITMAP_SMOKE + 2 " +
                $"({ClassicTextureIds.BitmapSmoke + 2}).");

            Console.WriteLine(
                $"[ClassicFX] Particle texture unresolved by reference: " +
                $"BITMAP_SWORD_FORCE " +
                $"({ClassicTextureIds.BitmapSwordForce}).");

            Console.WriteLine(
                $"[ClassicFX] Particle texture unresolved by reference: " +
                $"BITMAP_CLOUD + 2 " +
                $"({ClassicTextureIds.BitmapCloud + 2}).");

            Console.WriteLine(
                $"[ClassicFX] Particle texture is a map-dependent dynamic slot: " +
                $"BITMAP_CHROME + 2 " +
                $"({ClassicTextureIds.BitmapChrome + 2}).");
        }

        public bool IsKnownUnresolvedParticleTexture(
            int type)
        {
            return
                type ==
                    ClassicTextureIds.BitmapSmoke +
                    2 ||
                type ==
                    ClassicTextureIds.BitmapSwordForce ||
                type ==
                    ClassicTextureIds.BitmapCloud +
                    2 ||
                type ==
                    ClassicTextureIds.BitmapChrome +
                    2;
        }

        public bool TryGet(
            int type,
            out ClassicTextureResource resource)
        {
            if (_resources.TryGetValue(
                    type,
                    out resource))
            {
                return
                    resource != null &&
                    resource.IsReady;
            }

            resource =
                null;

            return false;
        }

        private async Task<ClassicTextureLoadResult>
            LoadDefinitionsAsync(
                ClassicTextureDefinition[] definitions)
        {
            int loaded =
                0;

            int reused =
                0;

            int failed =
                0;

            for (int i = 0;
                 i < definitions.Length;
                 i++)
            {
                ClassicTextureDefinition definition =
                    definitions[i];

                if (_resources.TryGetValue(
                        definition.Type,
                        out ClassicTextureResource existing) &&
                    existing != null &&
                    existing.IsReady)
                {
                    reused++;

                    continue;
                }

                TextureData data =
                    await TextureLoader
                        .Instance
                        .Prepare(
                            definition.Path);

                if (data == null)
                {
                    failed++;

                    Console.WriteLine(
                        $"[ClassicFX] Texture data not found: " +
                        $"{definition.Type} -> {definition.Path}");

                    continue;
                }

                Texture2D texture =
                    TextureLoader
                        .Instance
                        .GetTexture2D(
                            definition.Path);

                if (texture == null)
                {
                    failed++;

                    Console.WriteLine(
                        $"[ClassicFX] Texture2D could not be created: " +
                        $"{definition.Type} -> {definition.Path}");

                    continue;
                }

                _resources[
                    definition.Type
                ] =
                    new ClassicTextureResource(
                        definition.Type,
                        definition.Path,
                        data,
                        texture,
                        definition.SamplerState);

                loaded++;
            }

            return
                new ClassicTextureLoadResult(
                    loaded,
                    reused,
                    failed);
        }
    }
}
