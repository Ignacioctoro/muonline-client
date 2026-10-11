// ClassicFX Effect bridge v1. Native references: ZzzEffect.cpp CreateEffect,
// MoveEffects and RenderEffects for MODEL_SWELL_OF_MAGICPOWER / MODEL_ARROWSRE06.
// MonoGame's existing ModelObject is the only BMD renderer.
using System;
using System.Threading.Tasks;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.ClassicFX.Core
{
    // A named model type; not a fabricated original Main numeric MODEL_* ID.
    // Future types belong here and in the model catalogue below.
    public enum ClassicFxEffectType
    {
        SwellOfMagicPower = 1,
        ArrowsRe06 = 2,
        ShockWave = 3,
        Twlight = 4,
        AirForce = 5,
        SummonerCasting1 = 6,
        SummonerCasting11 = 7,
        SummonerCasting111 = 8,
        SummonerCasting2 = 9,
        SummonerCasting22 = 10,
        SummonerCasting222 = 11,
        SummonerCasting4 = 12,
        SwordForce = 13,
        MagicCircle1 = 14,
        Magic1 = 15,
        MagicCapsule2 = 16,
        Poison = 17,
        MagicZin = 18,
        LightningGround = 19,
        MagicGround = 20,
        MagicGround2 = 21,
        MagicCircleGround = 22,
        DarkLordSkill = 23,
        AliceBuffSkillEffect = 24,
        AliceBuffSkillEffect2 = 25,
        ShockWaveGround01 = 26,
        Wave = 27,
        CircleLight = 28,
        Stone1 = 29,
        Stone2 = 30,
        AliceDrainLife = 31,
        KnightPlancrackA = 32,
        Damage01Mono = 33,
        LightningShock = 34,
        SkillBlast = 35,
        SkillInferno = 36,
        Circle = 37,
        Storm = 38,
        Summon = 39,
        Tail = 40,
        WaveForce = 41,
        Piercing = 42,
        ArrowBomb = 43,
        ArrowNature = 44,
        ArrowDouble = 45,
        ArrowWing = 46,
        Fire = 47,
        Ice = 48,
        IceSmall = 49,
        Blizzard = 50,
        Snow1 = 51,
        Snow2 = 52,
        Snow3 = 53,
        DarkScream = 54,
        DarkScreamFire = 55,
        ManaRune = 56,
        Javelin = 57,
        ArrowImpact = 58,
        SkinShell = 59,
        StunStone = 60,
        Crater = 61,
        Waves = 62,
        Piercing2 = 63,
        PierPart = 64,
        BlowOfDestruction = 65,
        NightWater01 = 66,
        KnightPlancrackB = 67,
        RaklionBossCrack = 68,
        LightningOrb = 69,
        FenrirThunder = 70,
        Magic2 = 71,
        FuryStrike = 72,
        FuryQuake1 = 73,
        FuryQuake2 = 74,
        FuryQuake3 = 75,
        FuryQuake4 = 76,
        FuryQuake5 = 77,
        FuryQuake6 = 78,
        FuryQuake7 = 79,
        FuryQuake8 = 80,
        FenrirFootThunder = 81,
        TwinTail = 82,
        CloudGround = 83,
        BrokenIce0 = 84,
        BrokenIce1 = 85,
        BrokenIce2 = 86,
        BrokenIce3 = 87,
        CursedStatue1 = 88,
        CursedStatue2 = 89,
        SnowmanHead = 90,
        SnowmanBody = 91,
        Feather = 92,
        FeatherForeign = 93,
        SapitresAttack1 = 94,
        SapitresAttack2 = 95,
        FlameStrike = 96,
        StarShine = 97,
        SapitresAttackCarrier = 98,
        // S6 Batch 16: native model visuals and shared physical debris.
        MultiShot1 = 99,
        MultiShot2 = 100,
        MultiShot3 = 101,
        BigStonePart1 = 102,
        BigStonePart2 = 103,
        WallPart1 = 104,
        WallPart2 = 105,
        GatePart1 = 106,
        GatePart2 = 107,
        GatePart3 = 108,
        GolemStone = 109,
        ArrowSteel = 110,
        ArrowThunder = 111,
        ArrowLaser = 112,
        ArrowV = 113,
        ArrowSaw = 114,
        ArrowSpark = 115,
        ArrowGamble = 116,
        // Season 6 Batch 17: Halloween, Moon Harvest, Change Up and arrows.
        HalloweenCandyBlue = 117,
        HalloweenCandyOrange = 118,
        HalloweenCandyYellow = 119,
        HalloweenCandyRed = 120,
        HalloweenCandyHobak = 121,
        HalloweenCandyStar = 122,
        MoonHarvestGam = 123,
        MoonHarvestSongpuen1 = 124,
        MoonHarvestSongpuen2 = 125,
        MoonHarvestMoon = 126,
        ChangeUpEffect = 127,
        ChangeUpNasa = 128,
        ChangeUpCylinder = 129,
        ArrowBestCrossbow = 130,
        ArrowDrill = 131,
        ArrowRing = 132,
        HalloweenEx = 133,
        // S6 Batch 18: native Christmas and New Year's Day event models.
        XmasEventBox = 134,
        XmasEventCandy = 135,
        XmasEventTree = 136,
        XmasEventSocks = 137,
        XmasEventIceHeart = 138,
        NewYearsDayBeksulki = 139,
        NewYearsDayCandy = 140,
        NewYearsDayMoney = 141,
        NewYearsDayHotPepperGreen = 142,
        NewYearsDayHotPepperRed = 143,
        NewYearsDayPig = 144,
        NewYearsDayYut = 145,
        // S6 Batch 19: Imperial Guardian door/statue breakage.
        DoorCrushPiece01 = 146,
        DoorCrushPiece02 = 147,
        DoorCrushPiece03 = 148,
        DoorCrushPiece04 = 149,
        DoorCrushPiece05 = 150,
        DoorCrushPiece06 = 151,
        DoorCrushPiece07 = 152,
        DoorCrushPiece08 = 153,
        DoorCrushPiece09 = 154,
        DoorCrushPiece10 = 155,
        DoorCrushPiece11 = 156,
        DoorCrushPiece12 = 157,
        DoorCrushPiece13 = 158,
        StatueCrushPiece01 = 159,
        StatueCrushPiece02 = 160,
        StatueCrushPiece03 = 161,
        StatueCrushPiece04 = 162,
        DoorCrushCarrier = 163,
        StatueCrushCarrier = 164,
        // S6 Batch 20: Karutan Condra/NarCondra fragments and stones.
        CondraArmL = 165,
        CondraArmL2 = 166,
        CondraShoulder = 167,
        CondraArmR = 168,
        CondraArmR2 = 169,
        CondraConeL = 170,
        CondraConeR = 171,
        CondraPelvis = 172,
        CondraStomach = 173,
        CondraNeck = 174,
        NarCondraArmL = 175,
        NarCondraArmL2 = 176,
        NarCondraShoulderL = 177,
        NarCondraShoulderR = 178,
        NarCondraArmR = 179,
        NarCondraArmR2 = 180,
        NarCondraArmR3 = 181,
        NarCondraCone1 = 182,
        NarCondraCone2 = 183,
        NarCondraCone3 = 184,
        NarCondraCone4 = 185,
        NarCondraCone5 = 186,
        NarCondraCone6 = 187,
        NarCondraPelvis = 188,
        NarCondraStomach = 189,
        NarCondraNeck = 190,
        CondraStone = 191,
        CondraStone1 = 192,
        CondraStone2 = 193,
        CondraStone3 = 194,
        CondraStone4 = 195,
        CondraStone5 = 196,
        NarCondraStone = 197,
        NarCondraStone1 = 198,
        NarCondraStone2 = 199,
        NarCondraStone3 = 200,
        // S6 Batch 21: Swamp of Quiet Shadow Pawn/Knight/Rook debris.
        ShadowPawnAnkleLeft = 201,
        ShadowPawnAnkleRight = 202,
        ShadowPawnBelt = 203,
        ShadowPawnChest = 204,
        ShadowPawnHelmet = 205,
        ShadowPawnKneeLeft = 206,
        ShadowPawnKneeRight = 207,
        ShadowPawnWristLeft = 208,
        ShadowPawnWristRight = 209,
        ShadowKnightAnkleLeft = 210,
        ShadowKnightAnkleRight = 211,
        ShadowKnightBelt = 212,
        ShadowKnightChest = 213,
        ShadowKnightHelmet = 214,
        ShadowKnightKneeLeft = 215,
        ShadowKnightKneeRight = 216,
        ShadowKnightWristLeft = 217,
        ShadowKnightWristRight = 218,
        ShadowRookAnkleLeft = 219,
        ShadowRookAnkleRight = 220,
        ShadowRookBelt = 221,
        ShadowRookChest = 222,
        ShadowRookHelmet = 223,
        ShadowRookKneeLeft = 224,
        ShadowRookKneeRight = 225,
        ShadowRookWristLeft = 226,
        ShadowRookWristRight = 227,
        // S6 Batch 22: Totem Golem, Ice Giant, Bone and BigStone.
        TotemGolemPart1 = 228,
        TotemGolemPart2 = 229,
        TotemGolemPart3 = 230,
        TotemGolemPart4 = 231,
        TotemGolemPart5 = 232,
        TotemGolemPart6 = 233,
        IceGiantPart1 = 234,
        IceGiantPart2 = 235,
        IceGiantPart3 = 236,
        IceGiantPart4 = 237,
        IceGiantPart5 = 238,
        IceGiantPart6 = 239,
        Bone1 = 240,
        Bone2 = 241,
        BigStone1 = 242,
        BigStone2 = 243,
        // S6 Batch 23: Kundun fragments and Kanturu Maya visual effects.
        KundunPart1 = 244,
        KundunPart2 = 245,
        KundunPart3 = 246,
        KundunPart4 = 247,
        KundunPart5 = 248,
        KundunPart6 = 249,
        KundunPart7 = 250,
        KundunPart8 = 251,
        MayaStone1 = 252,
        MayaStone2 = 253,
        MayaStone3 = 254,
        MayaStone4 = 255,
        MayaStone5 = 256,
        MayaStoneFire = 257,
        MayaHandSkill = 258,
        MayaStar = 259,
        // S6 Batch 24: missing Summoner BMD families (casting 7 already exist).
        SummonerWristRing = 260,
        SummonerHeadSahamutt = 261,
        SummonerHeadNeil = 262,
        SummonerHeadLagul = 263,
        SummonerSahamutt = 264,
        SummonerNeil = 265,
        SummonerLagul = 266,
        SummonerNeilKnife1 = 267,
        SummonerNeilKnife2 = 268,
        SummonerNeilKnife3 = 269,
        SummonerNeilGround1 = 270,
        SummonerNeilGround2 = 271,
        SummonerNeilGround3 = 272,
        // S6 Batch 25: Cursed Temple, Doppelganger, Raklion, Shockwave, Wind Force, SD.
        CursedTempleHolyItem = 273,
        CursedTempleProtection = 274,
        CursedTempleRestraint = 275,
        DoppelgangerSlimeChip = 276,
        RaklionBossMagic = 277,
        Shockwave01 = 278,
        Shockwave02 = 279,
        ShockwaveSpin01 = 280,
        WindForce = 281,
        SdAura = 282,
        // S6 Batch 26: physical combat effects and projectiles.
        ShieldCrashModel = 283,
        ShieldCrashRing = 284,
        ComboModel = 285,
        FissureModel = 286,
        FissureLight = 287,
        WaterWaveModel = 288,
        IronRiderArrowModel = 289,
        KentaurosArrowModel = 290,
        DragonLowerDummy = 291,
        BalgasSkillModel = 292,
        DarkElfSkillModel = 293,
        ArrowAutoLoadModel = 294,
        // S6 Batch 27: Infinity Arrow BMD chain, Blade and Rage Fighter models.
        InfinityArrowCore = 295,
        InfinityArrow1 = 296,
        InfinityArrow2 = 297,
        InfinityArrow3 = 298,
        InfinityArrow4 = 299,
        BladeSkillModel = 300,
        WolfHeadEffect = 301,
        WolfHeadEffect2 = 302,
        DownAttackDummyL = 303,
        DownAttackDummyR = 304,
        DragonKickDummy = 305,
        // Batch 28 — volcanic, event and combat BMD models.
        PhoenixShotModel = 306,
        WindSpin02Model = 307,
        WindSpin03Model = 308,
        VolcanoOfMonkModel = 309,
        VolcanoStoneModel = 310,
        MoveTargetPositionModel = 311,
        SakuraEventItemModel = 312,
        UmbrellaGoldModel = 313,
        EmpireGuardianFrameStrikeModel = 314,
        // S6 Batch 29 — ranged and siege effect BMD families.
        ArrowBasicModel = 315,
        ArrowDarkStingerModel = 316,
        LaceArrowModel = 317,
        GroundStoneModel = 318,
        GroundStone2Model = 319,
        SkullModel = 320,
        ProtectGuildModel = 321,
        DeathSpiSkillModel = 322,
        WoosiStoneModel = 323,
        DungeonStoneModel = 324,
        // S6 Batch 30: Cursed Temple, PK field and physical stone debris.
        CursedTempleStatuePart1 = 325,
        CursedTempleStatuePart2 = 326,
        PkFieldAssassinGreenHead = 327,
        PkFieldAssassinRedHead = 328,
        PkFieldAssassinGreenBody = 329,
        PkFieldAssassinRedBody = 330,
        StoneCoffin1 = 331,
        StoneCoffin2 = 332,
        FlyBigStone1 = 333,
        FlyBigStone2 = 334,
        FallStoneEffect = 335,
        // S6 Batch 31: Kanturu storms, ambient models and skill visual BMDs.
        KanturuStorm2 = 336,
        KanturuStorm3 = 337,
        AuroraModel = 338,
        ButterflyModel = 339,
        LaserSkillModel = 340,
        RidingSpearModel = 341,
        Warp1 = 342,
        Warp2 = 343,
        Warp4 = 344,
        Warp5 = 345,
        // S6 Batch 32: Kundun/Kalima, Aida and Imperial Guardian models.
        KundunDragonHead = 346,
        KundunPhoenix = 347,
        KundunGhost = 348,
        DeasulerBoomerang = 349,
        ImperialProjectile = 350,
        SawSkillModel = 351,
        TreeAttackModel = 352,
        DesairModel = 353,
        // S6 Batch 33: Swamp EX01 Shadow Master fragments and Warp03.
        Ex01ShadowMasterAnkleLeft = 354,
        Ex01ShadowMasterAnkleRight = 355,
        Ex01ShadowMasterBelt = 356,
        Ex01ShadowMasterChest = 357,
        Ex01ShadowMasterHelmet = 358,
        Ex01ShadowMasterKneeLeft = 359,
        Ex01ShadowMasterKneeRight = 360,
        Ex01ShadowMasterWristLeft = 361,
        Ex01ShadowMasterWristRight = 362,
        Warp3 = 363,
        Warp6 = 364,
        // S6 Batch 34: battlefield, support, siege and gate BMD effects.
        MagicCircle1Model = 365,
        ProtectModel = 366,
        TowerGatePlaneModel = 367,
        WarcraftModel = 368,
        ShieldCrash2Model = 369,
        GateDebris1 = 370,
        GateDebris2 = 371,
        // Batch 35: mirror BMD and genuine model-less particle/joint carriers.
        WindForceMirror = 372,
        ChainLightning = 373,
        TargetMonEffect = 374,
        // Batch 36: native Cursed Temple/Blizzard BMDs and authentic bitmap effects.
        BlizzardModel = 375,
        CursedTempleProtectionSkill = 376,
        CursedTempleRestraintSkill = 377,
        EventCloudEffect = 378,
        TargetPositionEffect1 = 379,
        TargetPositionEffect2 = 380,
        RingOfGradationEffect = 381,
        OurInfluenceGroundEffect = 382,
        EnemyInfluenceGroundEffect = 383,
        LightMarksEffect = 384,
        // Batch 37: original BITMAP_* effect objects (no fake BMD).
        FirecrackerRise = 385,
        FirecrackerBurst = 386,
        FirecrackerSequence = 387,
        FirecrackerExplosion = 388,
        FirecrackerFlash = 389,
        CloudEffect = 390,
        OroraEffect = 391,
        GatheringEffect = 392,
        FireHik2MonoEffect = 393,
        PinLightEffect = 394,
        // Batch 38: ten additional native BITMAP_* Effect roots.
        FlameEmitter = 395,
        CursedLichFireEmitter = 396,
        SparkFountainEmitter = 397,
        SparkOwnerEmitter = 398,
        EnergyEmitter = 399,
        ShinyRingEmitter = 400,
        ShinyScatterEmitter = 401,
        LightningTerrain2Emitter = 402,
        SwordMonoEmitter = 403,
        ImpactEmitter = 404,
        // Batch 39: native BITMAP roots and variants, never source bitmap IDs.
        BossLaserBlue = 405,
        BossLaserRed = 406,
        BossLaserShort = 407,
        FireTrail02 = 408,
        LightProjectile = 409,
        MagicGroundBase = 410,
        ShotgunJointBurst = 411,
        FlareForceJointBurst = 412,
        LightRedGround = 413,
        ChromeEnergyGround = 414,
        // Batch 40: verified Main roots / effect carriers.
        SkullEffect = 415,
        FlareParticleEffect = 416,
        SwordEffCarrier = 417,
        JointForceCarrier = 418,
        SbumbImpactEmitter = 419,
        Damage1ImpactEmitter = 420,
        IceBreathCloudCarrier = 421,
        LavaGiantFootprintRed = 422,
        LavaGiantFootprintViolet = 423,
        FireHik3MonoCarrier = 424,
        // Season 6 Batch 41: real native carrier roots / distinct variants.
        TraceEnergyJointCarrier = 425,
        UmbrellaDeathRingCarrier = 426,
        GuardianDefenderAttackCarrier = 427,
        StreamBreathFireCarrier = 428,
        ThunderNapinCore = 429,
        ThunderNapinScatter = 430,
        SkillFissureCarrier = 431,
        SakuraItemEffectModel = 432,
        FenrirDamageRed = 433,
        FenrirDamageBlue = 434,
        FenrirDamageGreen = 435,
        WaterfallOrbit = 436,
        FirePlusOneEmitter = 437,
        DragonLoreLava = 438,
        HolyArrowJointCarrier = 439,
        // S6 Batch 44: genuine native logical emitters (no BMD stand-ins).
        KundunSkillCarrier = 440,
        ShineFlareCarrier = 441,
        SpearHealingCarrier = 442,
        ThunderPlusOneCarrier = 443,
    }

    public sealed partial class ClassicFxRuntime
    {
        private struct EffectState
        {
            public ClassicFxEffectType Type;
            public int SubType;
            public ClassicFxOwner Owner;
            public Vector3 Position;
            public Vector3 Angle;
            public Vector3 Light;
            public float Scale;
            public Vector3 Direction;
            public float Velocity;
            public float Gravity;
            public WorldObject TargetWorldObject;
            public float Alpha;
            public float LifeTime;
            public float BlendMeshLight;
            public int BoneIndex;
            public bool FirstMove;
            // Native EyeRight and PKKey drive ShockWave(14)/Twlight(3)
            // luminous fade independently of the model's Alpha.
            public Vector3 BaseLight;
            public Vector3 StartPosition; // native MODEL_BLIZZARD StartPosition
            public Vector3 HeadAngle; // native projectile heading (Javelin)
            // Original Kundun fragment timing and ground offset (visual only).
            public int NativePkKey;
            public int NativeSkillIndex;
            public float Phase;
            // Native timed-bitmap texture frame. Used only by the timed
            // Fenrir foot / TwinTail logical terrain effects.
            public int NativeAnimationFrame;
            public byte TriggerMask;
            // Last native tick emitting children for BITMAP_MAGIC+1 subtypes 6/8.
            public int LastChildNativeTick;
            public ClassicFxEffectModelObject ModelView;
        }

        private readonly EffectState[] _effects =
            new EffectState[ClassicFxPools.MaxEffects];

        public int ActiveEffectCount => Pools.Effects.ActiveCount;

        /// <summary>
        /// CreateEffect() bridge. Only accepted original type/subtype pairs
        /// are allocated. Unknown cases do NOT claim to render anything.
        /// This pool owns the state and the corresponding NeffisDev model.
        /// </summary>
        public ClassicFxHandle CreateEffect(
            ClassicFxEffectType type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            ClassicFxOwner owner,
            int subType = 0,
            int boneIndex = -1,
            float scale = 1f,
            int nativePkKey = 0,
            int nativeSkillIndex = 0,
            float nativeAnimationSpeed = 0f,
            WorldObject nativeTarget = null)
        {
            if (_disposed || !Enabled)
                return ClassicFxHandle.Invalid;

            // Main CreateEffect flips the red/green New Year's pepper
            // before loading its model. Preserve the chosen BMD identity.
            if ((type is ClassicFxEffectType.NewYearsDayHotPepperGreen or
                 ClassicFxEffectType.NewYearsDayHotPepperRed) &&
                Random.FpsCheck(2, Clock))
                type = type == ClassicFxEffectType.NewYearsDayHotPepperGreen
                    ? ClassicFxEffectType.NewYearsDayHotPepperRed
                    : ClassicFxEffectType.NewYearsDayHotPepperGreen;

            bool additionalModel = TryGetAdditionalModelDefinition(
                type, subType, out AdditionalEffectModelDefinition modelDefinition);
            bool batch2Model = TryGetBatch2EffectModelDefinition(
                type, subType, out Batch2EffectModelDefinition batch2Definition);
            bool season6Model = TryGetSeason6ModelDefinition(
                type, subType, out Season6ModelDefinition season6Definition);
            if (IsS6Batch22HeavyModel(type) && subType == 5 &&
                owner.WorldObject == null)
                return ClassicFxHandle.Invalid;
            bool additionalTerrain = TryGetV5TerrainDefinition(type, subType,
                out V5TerrainEffectDefinition terrainDefinition);
            MagicGround2Definition magicGround2Definition = default;
            bool magicGround2 = type == ClassicFxEffectType.MagicGround2 &&
                TryGetMagicGround2Definition(subType, out magicGround2Definition);
            // Both BITMAP_MAGIC+1 and BITMAP_MAGIC+2 share native CreateEffect().
            MagicGround2Definition magicCircleDefinition = default;
            bool magicCircleGround = type == ClassicFxEffectType.MagicCircleGround &&
                TryGetMagicGround2Definition(subType, out magicCircleDefinition);
            bool nativeGroundV8 = TryGetV8GroundEffectDefinition(type, subType,
                out V8GroundEffectDefinition groundV8Definition);
            bool damage01Mono = type == ClassicFxEffectType.Damage01Mono &&
                (subType == 0 || subType == 1);
            bool crater = type == ClassicFxEffectType.Crater &&
                (subType is >= 0 and <= 2);
            // Native BLOW_OF_DESTRUCTION is a logical Effect with terrain
            // and sprite render paths, not another BMD renderer.
            bool blowOfDestruction = type == ClassicFxEffectType.BlowOfDestruction &&
                (subType is 0 or 1);
            bool lightningOrb = type == ClassicFxEffectType.LightningOrb &&
                (subType is 0 or 1);
            // Kind 0 / 2 / 3 is carried in subtype for the logical root.
            // The original random subtype for radial placement lives in Phase.
            bool furyStrike = type == ClassicFxEffectType.FuryStrike &&
                (subType is 0 or 2 or 3);
            bool s6TimedTerrain = IsS6Batch14TerrainType(type, subType);
            bool s6Batch15Logical =
                (type == ClassicFxEffectType.StarShine && subType == 0) ||
                (type == ClassicFxEffectType.SapitresAttackCarrier && subType == 0);
            bool s6Batch17Logical =
                type == ClassicFxEffectType.HalloweenEx && subType == 0;
            bool s6Batch19Logical = IsS6Batch19CrushCarrier(type, subType);
            bool s6Batch35Logical = IsS6Batch35LogicalType(type, subType);
            bool s6Batch36Logical = IsS6Batch36LogicalType(type, subType);
            bool s6Batch37Logical = IsS6Batch37LogicalType(type, subType);
            bool s6Batch38Logical = IsS6Batch38LogicalType(type, subType);
            bool s6Batch39Logical = IsS6Batch39LogicalType(type, subType);
            bool s6Batch40Logical = IsS6Batch40LogicalType(type, subType);
            bool s6Batch41Logical = IsS6Batch41LogicalType(type, subType);
            bool s6Batch42Logical = IsS6Batch42LogicalType(type, subType);
            bool s6Batch43Logical = IsS6Batch43LogicalType(type, subType);
            bool s6Batch44Logical = IsS6Batch44LogicalType(type, subType);
            if (s6Batch43Logical &&
                !ValidateS6Batch43Owner(type, owner))
                return ClassicFxHandle.Invalid;
            if (s6Batch42Logical &&
                (owner.WorldObject is not ModelObject ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready ||
                 nativePkKey < 0 ||
                 !TryGetOwnerBonePosition(owner, nativePkKey, out _)))
                return ClassicFxHandle.Invalid;
            if ((s6Batch41Logical || IsS6Batch41ModelType(type)) &&
                S6Batch41NeedsOwner(type) &&
                (owner.WorldObject == null ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            if ((s6Batch41Logical || IsS6Batch41ModelType(type)) &&
                S6Batch41NeedsModelOwner(type) &&
                owner.WorldObject is not ModelObject)
                return ClassicFxHandle.Invalid;
            if (s6Batch40Logical &&
                ((S6Batch40NeedsOwner(type, subType) &&
                  (owner.WorldObject == null ||
                   !ReferenceEquals(owner.WorldObject.World, World) ||
                   owner.WorldObject.Status != GameControlStatus.Ready)) ||
                 (S6Batch40NeedsModelOwner(type) &&
                  owner.WorldObject is not ModelObject) ||
                 (S6Batch40NeedsTarget(type) &&
                  (nativeTarget == null ||
                   !ReferenceEquals(nativeTarget.World, World) ||
                   nativeTarget.Status != GameControlStatus.Ready))))
                return ClassicFxHandle.Invalid;
            if (s6Batch39Logical && S6Batch39NeedsOwner(type, subType) &&
                (owner.WorldObject == null ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            if (s6Batch38Logical && S6Batch38NeedsOwner(type) &&
                (owner.WorldObject == null ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            if (s6Batch37Logical && S6Batch37NeedsOwner(type, subType) &&
                (owner.WorldObject == null ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            if (type == ClassicFxEffectType.PinLightEffect &&
                (subType is 1 or 2 or 4) &&
                owner.WorldObject is not ModelObject)
                return ClassicFxHandle.Invalid;
            if (s6Batch36Logical && IsS6Batch36OwnerRequired(type) &&
                (owner.WorldObject == null ||
                 !ReferenceEquals(owner.WorldObject.World, World) ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            if (type == ClassicFxEffectType.LightMarksEffect &&
                owner.WorldObject is not ModelObject)
                return ClassicFxHandle.Invalid;
            // Chain lightning references two actual client objects, not a BMD.
            if (type == ClassicFxEffectType.ChainLightning &&
                (nativeTarget == null ||
                 !ReferenceEquals(nativeTarget.World, World)))
                return ClassicFxHandle.Invalid;
            if (s6Batch35Logical &&
                (owner.WorldObject is not ModelObject ||
                 owner.WorldObject.Status != GameControlStatus.Ready))
                return ClassicFxHandle.Invalid;
            // Source model is present in Main, but BMD is missing in Data_Broyal.
            if (type == ClassicFxEffectType.DoorCrushPiece09)
                return ClassicFxHandle.Invalid;
            // Native SkillIndex and PKKey are mandatory for Inferno 2/6/8/10.
            if (type == ClassicFxEffectType.SkillInferno &&
                (subType is 2 or 6 or 8 or 10) &&
                (boneIndex < 0 || !float.IsFinite(scale) || scale <= 0f))
                return ClassicFxHandle.Invalid;
            if (type == ClassicFxEffectType.WaveForce &&
                (!float.IsFinite(scale) || scale <= 0f))
                return ClassicFxHandle.Invalid;
            // Preserve already-ported Wizardry subtypes ShockWave 14 / Twlight 3.
            bool terrain = (type == ClassicFxEffectType.ShockWave && subType == 14) ||
                           (type == ClassicFxEffectType.Twlight && subType == 3);
            if (magicGround2 && subType == 7 && owner.WorldObject == null)
                return ClassicFxHandle.Invalid; // Native subtype 7 follows Owner.
            if (nativeGroundV8 && groundV8Definition.RequiresOwner &&
                owner.WorldObject == null)
                return ClassicFxHandle.Invalid;
            if (nativeGroundV8 || crater || blowOfDestruction || lightningOrb || furyStrike ||
                s6TimedTerrain || s6Batch15Logical || s6Batch17Logical ||
                s6Batch19Logical || s6Batch35Logical || s6Batch36Logical ||
                s6Batch37Logical || s6Batch38Logical || s6Batch39Logical ||
                s6Batch40Logical || s6Batch41Logical ||
                s6Batch42Logical || s6Batch43Logical || s6Batch44Logical ||
                magicGround2 || magicCircleGround ||
                additionalTerrain || damage01Mono)
            {
                // Original terrain bitmaps can be spawned with a null Owner.
                // If present, Owner still must belong to this world.
                if (owner.WorldObject != null &&
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
            }
            else if (terrain)
            {
                // Native these are Effect objects with texture terrain render,
                // NOT standalone sprites/billboards nor BMD model objects.
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
            }
            else if (additionalModel || batch2Model || season6Model)
            {
                // Native model Effects can be unowned. Owner-required
                // variants are checked against their original creation rules.
                if (owner.WorldObject != null &&
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return ClassicFxHandle.Invalid;
                if (type == ClassicFxEffectType.AirForce && owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
                if (batch2Model && batch2Definition.RequiresOwner &&
                    owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
                if (season6Model && season6Definition.NeedsOwner &&
                    owner.WorldObject == null)
                    return ClassicFxHandle.Invalid;
            }
            else if (owner.WorldObject is not PlayerObject player ||
                     !ReferenceEquals(player.World, World) ||
                     player.Status != GameControlStatus.Ready)
            {
                return ClassicFxHandle.Invalid;
            }

            if (furyStrike && owner.WorldObject == null)
                return ClassicFxHandle.Invalid;
            // Some native MODEL_PIER_PART variants store the input Light
            // as a target-relative StartPosition before changing Light.
            Vector3 inputLight = light;
            string modelPath = null;
            float life;
            float effectScale = scale;
            float effectMeshLight = 1f;
            float effectAlpha = 1f;
            int effectBlendMesh = 0;
            int effectHiddenMesh = -1;
            Vector3 effectDirection = Vector3.Zero;
            Vector3 effectHeading = angle;
            float effectVelocity = 0f;
            float effectGravity = 0f;
            Vector3 effectPosition = position;
            Vector3 s6Batch22StoredPosition = position;
            Vector3 s6Batch23StoredPosition = position;
            Vector3 s6Batch24StoredPosition = position;
            Vector3 s6Batch25StoredPosition = position;
            Vector3 s6Batch26StoredPosition = position;
            Vector3 s6Batch29StoredPosition = position;
            Vector3 s6Batch31StoredPosition = position;
            Vector3 s6Batch32StoredPosition = position;
            float s6Batch32Phase = 0f;
            if (type == ClassicFxEffectType.SwellOfMagicPower && subType == 0)
            {
                modelPath = "Effect/magic_powerup.bmd";
                life = 45f;
            }
            else if (type == ClassicFxEffectType.ArrowsRe06 && subType == 1 && boneIndex >= 0)
            {
                modelPath = "Effect/arrowsre06.bmd";
                life = 40f;
            }
            else if (additionalModel)
            {
                modelPath = modelDefinition.Path;
                life = modelDefinition.LifeTime;
                effectScale = modelDefinition.Scale;
                effectMeshLight = modelDefinition.BlendMeshLight;
            }
            else if (batch2Model)
            {
                modelPath = batch2Definition.Path;
                life = batch2Definition.LifeTime;
                effectScale = batch2Definition.UseCallerScale
                    ? scale : batch2Definition.Scale;
                effectMeshLight = batch2Definition.BlendMeshLight;
                effectBlendMesh = batch2Definition.BlendMesh;
                effectHiddenMesh = batch2Definition.HiddenMesh;
                effectDirection = batch2Definition.Direction;
                effectVelocity = batch2Definition.Velocity;
                effectPosition.Z += batch2Definition.SpawnZ * Clock.FrameFactor;
                if (type == ClassicFxEffectType.DarkLordSkill)
                    angle = new Vector3(
                        MathHelper.ToRadians(45f),
                        MathHelper.ToRadians(subType == 0 ? 45f : -45f),
                        0f);
            }
            else if (season6Model)
            {
                modelPath = season6Definition.Path;
                life = season6Definition.LifeTime;
                effectScale = season6Definition.UseCallerScale ? scale : season6Definition.Scale;
                effectAlpha = season6Definition.Alpha;
                effectMeshLight = season6Definition.MeshLight;
                effectPosition.Z += season6Definition.OffsetZ * Clock.FrameFactor;
                if (season6Definition.WhiteLight) light = Vector3.One;
                // MuMain EffectTypes: ALICE rings reset the yaw to 0 on spawn.
                if (type == ClassicFxEffectType.AliceBuffSkillEffect2 ||
                    (type == ClassicFxEffectType.AliceBuffSkillEffect && subType <= 2))
                    angle.Z = 0f;
                // The native MODEL_STONE1/2 init has a separate HeadAngle
                // trajectory for subtypes 13/14 (Lightning Shock debris).
                if (type is ClassicFxEffectType.Stone1 or ClassicFxEffectType.Stone2)
                {
                    angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                    if (subType == 10 || subType == 12)
                    {
                        life = 32f + Random.Modulo(16);
                        effectScale = (15f + Random.Modulo(4)) *
                            (subType == 10 ? 0.05f : 0.1f);
                        float speed = (64f + Random.Modulo(256)) *
                            (subType == 10 ? 0.2f : 0.1f);
                        effectDirection = Vector3.TransformNormal(
                            new Vector3(0f, speed, 0f),
                            Matrix.CreateRotationZ(angle.Z));
                        effectGravity = (subType == 10 ? 28f : 8f) + Random.Modulo(16);
                    }
                    else if (subType == 13 || subType == 14)
                    {
                        life = 20f + Random.Modulo(16);
                        effectScale = (3f + Random.Modulo(13)) * 0.08f * scale;
                        effectGravity = 3f + Random.Modulo(3);
                        float speed = (64f + Random.Modulo(128)) * 0.1f;
                        effectDirection = Vector3.TransformNormal(
                            new Vector3(0f, speed, 0f),
                            Matrix.CreateRotationZ(angle.Z));
                        effectDirection.Z += 15f * Clock.FrameFactor;
                    }
                    else
                    {
                        life = 32f + Random.Modulo(16);
                        effectScale = (8f + Random.Modulo(4)) * 0.1f;
                        float speed = (64f + Random.Modulo(256)) * 0.1f;
                        // Move_MODEL_ICE_SMALL rotates the unrotated
                        // direction each frame; avoid applying yaw twice.
                        effectDirection = new Vector3(0f, speed, 0f);
                        effectGravity = 8f + Random.Modulo(16);
                    }
                }
                if (type == ClassicFxEffectType.KnightPlancrackA)
                {
                    angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                    effectScale = scale + Random.Modulo(10) * 0.05f;
                    effectPosition.Z += 10f * Clock.FrameFactor;
                }
                if (IsS6Batch04ModelType(type))
                    InitializeS6Batch04Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref effectDirection,
                        ref effectVelocity, ref effectGravity,
                        ref effectMeshLight);
                if (IsS6Batch05ModelType(type))
                    InitializeS6Batch05Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref effectDirection,
                        ref effectVelocity, ref effectGravity,
                        ref effectMeshLight);
                if (IsS6Batch06ModelType(type))
                    InitializeS6Batch06Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref effectDirection,
                        ref effectVelocity, ref effectGravity);
                if (IsS6Batch07ModelType(type))
                    InitializeS6Batch07Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectDirection,
                        ref effectVelocity, ref effectGravity, ref effectMeshLight);
                if (IsS6Batch08ModelType(type))
                    InitializeS6Batch08Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref effectDirection,
                        ref effectVelocity, ref effectGravity);
                if (IsS6Batch09ModelType(type))
                    InitializeS6Batch09Spawn(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectVelocity, ref effectGravity, ref effectHeading);
                if (IsS6Batch10ModelType(type))
                    InitializeS6Batch10Spawn(type, subType, boneIndex,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectVelocity, ref effectGravity, ref effectHeading);
                if (type == ClassicFxEffectType.PierPart && subType == 1)
                {
                    life = GetS6ParentEffectLifetime(owner);
                    effectAlpha = (20f - life) / 5f;
                }
                if (IsS6Batch11ModelType(type))
                {
                    InitializeS6Batch11Model(type, ref angle);
                    if (type == ClassicFxEffectType.RaklionBossCrack)
                        effectScale = scale + 1f;
                }
                if (IsS6Batch12ModelType(type) &&
                    !InitializeS6Batch12Model(type, subType, owner,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectDirection))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch15ModelType(type) &&
                    !InitializeS6Batch15Model(type, subType, owner,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading, ref effectVelocity,
                        ref effectMeshLight))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch16ModelType(type))
                    InitializeS6Batch16Model(type, ref subType,
                        ref effectPosition, ref angle, ref effectScale,
                        ref life, ref effectAlpha, ref effectDirection,
                        ref effectGravity, ref effectVelocity,
                        ref effectMeshLight);
                if (IsS6Batch17ModelType(type) &&
                    !InitializeS6Batch17Model(type, subType, owner,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading, ref effectVelocity,
                        ref effectMeshLight))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch18ModelType(type))
                    InitializeS6Batch18Model(type, ref angle,
                        ref effectScale, ref life, ref effectDirection,
                        ref effectGravity, ref effectHeading);
                if (IsS6Batch19CrushModel(type))
                    InitializeS6Batch19CrushModel(type, ref subType,
                        ref angle, ref light, ref life, ref effectHeading,
                        ref effectDirection, ref effectVelocity, ref effectGravity);
                if (IsS6Batch20AvailableModel(type))
                    InitializeS6Batch20Model(type, ref subType,
                        ref angle, ref light, ref effectScale, ref life,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading, ref effectVelocity);
                if (IsS6Batch21ModelType(type))
                    InitializeS6Batch21Model(ref subType, ref angle,
                        ref light, ref effectScale, ref life,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading, ref effectVelocity,
                        type);
                if (IsS6Batch22ModelType(type) &&
                    !InitializeS6Batch22Model(type, ref subType, owner,
                        ref effectPosition, ref s6Batch22StoredPosition,
                        ref angle, ref light, ref effectScale, ref life,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading, ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch23ModelType(type))
                    InitializeS6Batch23Model(type, subType, scale,
                        nativePkKey, ref effectPosition,
                        ref s6Batch23StoredPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectDirection, ref effectGravity,
                        ref effectHeading);
                if (IsS6Batch24ModelType(type))
                {
                    if (!InitializeS6Batch24Model(type, subType, owner,
                            inputLight, ref effectPosition,
                            ref s6Batch24StoredPosition, ref effectScale,
                            ref life, ref effectAlpha, ref effectMeshLight,
                            ref effectVelocity, ref light))
                        return ClassicFxHandle.Invalid;
                }
                if (IsS6Batch25ModelType(type) &&
                    !InitializeS6Batch25Model(type, owner, ref subType,
                        nativeAnimationSpeed, ref effectPosition,
                        ref s6Batch25StoredPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectVelocity, ref effectGravity,
                        ref effectHeading))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch26ModelType(type) &&
                    !InitializeS6Batch26Model(type, subType, owner,
                        inputLight, ref effectPosition,
                        ref s6Batch26StoredPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectHeading, ref effectVelocity,
                        ref effectGravity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch27ModelType(type) &&
                    !InitializeS6Batch27Model(type, subType, owner,
                        nativeAnimationSpeed, ref effectPosition,
                        ref angle, ref light, ref effectScale,
                        ref life, ref effectAlpha, ref effectMeshLight,
                        ref effectDirection, ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch28ModelType(type) &&
                    !InitializeS6Batch28Model(type, subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectHeading, ref effectGravity,
                        ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch29ModelType(type) &&
                    !InitializeS6Batch29Model(type, subType, owner,
                        inputLight, ref effectPosition,
                        ref s6Batch29StoredPosition, ref angle,
                        ref light, ref effectScale, ref life,
                        ref effectAlpha, ref effectMeshLight,
                        ref effectDirection, ref effectHeading,
                        ref effectGravity, ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch30ModelType(type) &&
                    !InitializeS6Batch30Model(type, ref subType,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectDirection, ref effectHeading,
                        ref effectGravity, ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch31ModelType(type) &&
                    !InitializeS6Batch31Model(type, subType, owner,
                        nativePkKey, ref effectPosition,
                        ref s6Batch31StoredPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectGravity, ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch32ModelType(type) &&
                    !InitializeS6Batch32Model(type, owner,
                        inputLight, ref effectPosition,
                        ref s6Batch32StoredPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectAlpha,
                        ref effectMeshLight, ref effectDirection,
                        ref effectHeading, ref effectGravity,
                        ref effectVelocity, ref s6Batch32Phase))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch34ModelType(type) &&
                    !InitializeS6Batch34Model(type, ref subType, inputLight,
                        ref effectPosition, ref angle, ref light,
                        ref effectScale, ref life, ref effectMeshLight,
                        ref effectDirection, ref effectGravity,
                        ref effectVelocity))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch35ModelType(type) &&
                    !InitializeS6Batch35Model(owner))
                    return ClassicFxHandle.Invalid;
                if (IsS6Batch36ModelType(type))
                    InitializeS6Batch36Model(type, subType, ref effectPosition,
                        ref light, ref effectScale, ref life, ref effectGravity,
                        ref effectVelocity, ref effectAlpha);
            }
            else if (additionalTerrain)
            {
                life = terrainDefinition.LifeTime;
                effectScale = terrainDefinition.UseCallerScale
                    ? scale * terrainDefinition.Scale : terrainDefinition.Scale;
                effectAlpha = terrainDefinition.Alpha;
            }
            else if (magicGround2)
            {
                life = magicGround2Definition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicGround2Definition, scale);
                if (magicGround2Definition.RandomAngle)
                    angle.Z = Random.Modulo(360); // degrees: native BITMAP_MAGIC+1:7
            }
            else if (magicCircleGround)
            {
                // Original CreateEffect(BITMAP_MAGIC+2) shares init with +1.
                life = magicCircleDefinition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicCircleDefinition, scale);
                if (magicCircleDefinition.RandomAngle)
                    angle.Z = Random.Modulo(360);
            }
            else if (nativeGroundV8)
            {
                life = groundV8Definition.LifeTime;
                effectScale = InitializeV8GroundScale(in groundV8Definition, scale);
                light *= groundV8Definition.LightMultiplier;
            }
            else if (crater)
            {
                life = subType == 0 ? 60f : subType == 1 ? 30f : 40f;
                effectScale = subType == 0 ? 4.5f :
                              subType == 1 ? 2.5f : 3f;
                light = Vector3.One;
            }
            else if (blowOfDestruction)
            {
                // Native both visible impact phases run for forty ticks.
                life = 40f;
                if (subType == 0)
                {
                    if (!TryGetOwnerSnapshot(owner,
                            out ClassicFxOwnerSnapshot caster))
                        return ClassicFxHandle.Invalid;
                    Vector3 offset = Vector3.TransformNormal(
                        new Vector3(-20f, -100f, 0f),
                        Matrix.CreateFromYawPitchRoll(
                            caster.Angle.Y, caster.Angle.X, caster.Angle.Z));
                    effectPosition += offset;
                }
                else
                {
                    effectPosition.Z = 150f;
                    effectScale = 5f;
                }
                light = new Vector3(1.2f);
            }
            else if (lightningOrb)
            {
                life = subType == 0 ? 20f : 18f;
                if (subType == 0)
                {
                    effectDirection = new Vector3(0f, -60f, 0f);
                    effectPosition.Z += 100f * Clock.FrameFactor;
                }
            }
            else if (s6TimedTerrain)
            {
                InitializeS6Batch14Terrain(type, subType,
                    ref effectPosition, ref angle, ref effectScale,
                    ref effectAlpha, out life);
            }
            else if (s6Batch17Logical)
            {
                // MODEL_HALLOWEEN_EX is a zero-lifetime 24-candy carrier.
                life = 0f;
            }
            else if (s6Batch19Logical)
            {
                // Native crush roots emit only their BMD children.
                life = 0f;
            }
            else if (s6Batch15Logical)
            {
                life = type == ClassicFxEffectType.StarShine ? 30f : 20f;
                if (type == ClassicFxEffectType.StarShine)
                {
                    effectAlpha = 0.2f;
                    angle.X = MathHelper.ToRadians(Random.Modulo(360));
                }
                else if (owner.WorldObject == null)
                {
                    return ClassicFxHandle.Invalid;
                }
            }
            else if (furyStrike)
            {
                // Logical 20-tick carrier. Native RenderFuryStrike() draws
                // the equipped item; a fake stand-alone skill BMD is wrong.
                life = 20f;
                InitializeS6FuryStrike(ref angle,
                    ref effectHeading, ref effectGravity);
            }
            else if (s6Batch35Logical)
            {
                // No ModelView is created for these native emitters.
                life = type == ClassicFxEffectType.ChainLightning ? 20f : 100f;
                if (type == ClassicFxEffectType.TargetMonEffect)
                    effectScale = scale;
            }
            else if (s6Batch36Logical)
            {
                InitializeS6Batch36Logical(type, out life, ref effectScale,
                    ref effectAlpha, ref light, scale);
            }
            else if (s6Batch37Logical)
            {
                InitializeS6Batch37Logical(type, subType,
                    ref effectPosition, ref angle, ref light,
                    ref effectScale, ref effectAlpha,
                    ref effectDirection, out life);
            }
            else if (s6Batch38Logical)
            {
                InitializeS6Batch38Logical(type, subType,
                    ref effectPosition, ref angle,
                    ref effectScale, ref effectAlpha,
                    ref effectDirection, out life);
            }
            else if (s6Batch39Logical)
            {
                InitializeS6Batch39Logical(type, subType, ref effectPosition,
                    ref angle, ref light, ref effectScale,
                    ref effectDirection, out life);
            }
            else if (s6Batch40Logical)
            {
                InitializeS6Batch40Logical(type, subType, owner,
                    ref effectPosition, ref angle, ref light,
                    ref effectScale, ref effectDirection,
                    ref effectVelocity, ref effectAlpha, out life);
            }
            else if (s6Batch41Logical)
            {
                InitializeS6Batch41Logical(type, ref effectScale, out life);
            }
            else if (s6Batch42Logical)
            {
                InitializeS6Batch42Logical(ref angle, ref effectScale,
                    ref effectVelocity, out life);
            }
            else if (s6Batch43Logical)
            {
                InitializeS6Batch43Logical(type, owner,
                    ref effectPosition, angle, ref effectScale,
                    ref effectVelocity, ref effectDirection, out life);
            }
            else if (s6Batch44Logical)
            {
                InitializeS6Batch44Logical(type, owner, ref effectPosition,
                    ref angle, subType, ref nativePkKey, out life);
            }
            else if (damage01Mono)
            {
                // BITMAP_DAMAGE_01_MONO native 0 / 1 initializers.
                life = subType == 0 ? 20f : 10f;
                effectScale = subType == 0 ? scale : 0.1f;
            }
            else if (terrain)
            {
                life = 30f; // source CreateEffect(), ShockWave 14 / Twlight 3
            }
            else
                return ClassicFxHandle.Invalid;

            if (!Pools.Effects.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            ClassicFxEffectModelObject view = null;
            if (modelPath != null)
            {
                view = new ClassicFxEffectModelObject(modelPath, type);
                view.Position = effectPosition;
                view.Angle = angle;
                view.Scale = effectScale;
                if (additionalModel || batch2Model || season6Model)
                    view.Color = new Color(Vector3.Clamp(light, Vector3.Zero, Vector3.One));
                if (batch2Model)
                {
                    view.BlendMesh = effectBlendMesh;
                    view.HiddenMesh = effectHiddenMesh;
                }
                if (IsS6Batch04ModelType(type))
                {
                    view.BlendMesh = type == ClassicFxEffectType.SkillInferno ? -2 : 0;
                    if (type == ClassicFxEffectType.SkillInferno)
                    {
                        view.HiddenMesh = subType == 4 ? 1 :
                            (subType is 2 or 6 or 8 or 10) ? boneIndex : -1;
                    }
                }
                if (IsS6Batch05ModelType(type))
                    ConfigureS6Batch05ModelView(view, type, subType);
                if (IsS6Batch06ModelType(type))
                    ConfigureS6Batch06ModelView(view, type, subType);
                if (IsS6Batch07ModelType(type))
                    ConfigureS6Batch07ModelView(view, type, subType);
                if (IsS6Batch08ModelType(type))
                    ConfigureS6Batch08ModelView(view, type, subType);
                if (IsS6Batch09ModelType(type))
                    ConfigureS6Batch09ModelView(view, type, subType);
                if (IsS6Batch10ModelType(type))
                    ConfigureS6Batch10ModelView(view, type, subType);
                if (IsS6Batch12ModelType(type))
                    ConfigureS6Batch12ModelView(view, type, subType);
                if (IsS6Batch13QuakeType(type))
                    ConfigureS6Batch13ModelView(view, type, subType);
                if (IsS6Batch15ModelType(type))
                    ConfigureS6Batch15ModelView(view, type, subType);
                if (IsS6Batch16ModelType(type))
                    ConfigureS6Batch16ModelView(view, type);
                if (IsS6Batch17ModelType(type))
                    ConfigureS6Batch17ModelView(view, type);
                if (IsS6Batch26ModelType(type))
                    ConfigureS6Batch26ModelView(view, type, subType);
                if (IsS6Batch27ModelType(type))
                    ConfigureS6Batch27ModelView(view, type, subType,
                        nativeAnimationSpeed);
                if (IsS6Batch28ModelType(type))
                    ConfigureS6Batch28ModelView(view, type);
                if (IsS6Batch29ModelType(type))
                    ConfigureS6Batch29ModelView(view, type, subType);
                if (IsS6Batch30ModelType(type))
                    ConfigureS6Batch30ModelView(view, type);
                if (IsS6Batch31ModelType(type))
                    ConfigureS6Batch31ModelView(view, type);
                if (IsS6Batch32ModelType(type))
                    ConfigureS6Batch32ModelView(view, type);
                if (IsS6Batch33ModelType(type))
                    ConfigureS6Batch33ModelView(view, type);
                if (IsS6Batch34ModelType(type))
                    ConfigureS6Batch34ModelView(view, type, subType);
                if (IsS6Batch36ModelType(type))
                    ConfigureS6Batch36ModelView(view, type);
                if (IsS6Batch24ModelType(type))
                    ConfigureS6Batch24ModelView(view, type);
                // Translation from native Effect state to MonoGame BMD presentation.
                view.ApplyNativeRenderState(effectScale, effectAlpha, effectMeshLight, light);
            }
            _effects[handle.Index] = new EffectState
            {
                Type = type,
                SubType = subType,
                Owner = owner,
                TargetWorldObject = nativeTarget,
                Position = effectPosition,
                StartPosition = type == ClassicFxEffectType.HolyArrowJointCarrier
                    ? GetS6Batch43HolyStart(effectPosition, angle)
                    : type is
                    ClassicFxEffectType.DeasulerBoomerang or
                    ClassicFxEffectType.ImperialProjectile
                    ? s6Batch32StoredPosition
                    : type == ClassicFxEffectType.BlizzardModel
                    ? effectPosition
                    : type == ClassicFxEffectType.KanturuStorm3
                    ? s6Batch31StoredPosition
                    : type is ClassicFxEffectType.DeathSpiSkillModel or
                      ClassicFxEffectType.ProtectGuildModel
                    ? s6Batch29StoredPosition
                    : IsS6Batch26ModelType(type)
                    ? s6Batch26StoredPosition
                    : IsS6Batch25ModelType(type)
                    ? s6Batch25StoredPosition
                    : IsS6Batch24ModelType(type)
                    ? s6Batch24StoredPosition
                    : IsS6Batch23Kundun(type) &&
                        subType is 2 or 3 or 4
                    ? s6Batch23StoredPosition
                    : type == ClassicFxEffectType.MayaHandSkill
                        ? inputLight
                    : IsS6Batch22HeavyModel(type) && subType == 5
                        ? s6Batch22StoredPosition
                    : (type == ClassicFxEffectType.PierPart && subType == 0) ||
                      (type == ClassicFxEffectType.BlowOfDestruction && subType == 0)
                        ? inputLight : effectPosition,
                HeadAngle = effectHeading,
                Angle = angle,
                Light = light,
                BaseLight = type == ClassicFxEffectType.DeasulerBoomerang
                    ? inputLight : light,
                Scale = effectScale,
                Direction = effectDirection,
                Velocity = effectVelocity,
                Gravity = effectGravity,
                Alpha = terrain ? 0f : effectAlpha,
                BlendMeshLight = effectMeshLight,
                LifeTime = life,
                BoneIndex = boneIndex,
                FirstMove = true,
                NativePkKey = nativePkKey,
                NativeSkillIndex = nativeSkillIndex,
                Phase = type == ClassicFxEffectType.DeasulerBoomerang
                    ? s6Batch32Phase
                    : type is ClassicFxEffectType.OurInfluenceGroundEffect or
                      ClassicFxEffectType.EnemyInfluenceGroundEffect ? 0.75f
                    : type == ClassicFxEffectType.WaterfallOrbit
                    ? Random.Modulo(360)
                    : furyStrike ? Random.Modulo(100) : 0f,
                LastChildNativeTick = -1,
                ModelView = view
            };
            try
            {
                // BMD effects are drawn by the existing WorldControl/ModelObject.
                // Terrain Effects are drawn in ClassicFxRuntime.RenderEffects().
                if (view != null)
                {
                    World.Objects.Add(view);
                    _ = view.Load();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClassicFX] CreateEffect {type} failed: {ex.Message}");
                ReleaseEffect(handle);
                return ClassicFxHandle.Invalid;
            }
            // Native CreateEffect immediately spawns phase 1.
            if (blowOfDestruction && subType == 0)
                CreateEffect(ClassicFxEffectType.BlowOfDestruction,
                    inputLight, angle, light, owner, subType: 1);
            if (type == ClassicFxEffectType.FenrirThunder)
                StartS6FenrirThunderSprite(ref _effects[handle.Index]);
            if (type == ClassicFxEffectType.ShieldCrashModel &&
                subType == 0 && owner.WorldObject != null)
            {
                // Original MODEL_SHIELD_CRASH subtype 0 spawns the model
                // MODEL_SHIELD_CRASH2 once at creation, same WorldObject.
                CreateEffect(ClassicFxEffectType.ShieldCrashRing,
                    owner.WorldObject.WorldPosition.Translation,
                    owner.WorldObject.Angle, new Vector3(0.5f, 0.5f, 1f),
                    owner, subType: 0);
            }
            if (type == ClassicFxEffectType.SapitresAttackCarrier)
            {
                for (int n = 0; n < 10; n++)
                    CreateEffect(ClassicFxEffectType.SapitresAttack2,
                        effectPosition, angle, light,
                        ClassicFxOwner.None, subType: 14);
            }
            if (s6Batch17Logical)
                EmitS6HalloweenEx(effectPosition, angle, light);
            if (s6Batch19Logical)
                EmitS6Batch19Crush(type, subType, effectPosition,
                    angle, light, owner);
            if (s6Batch37Logical)
                EmitS6Batch37OnCreate(type, effectPosition,
                    angle, light, owner, subType);
            if (s6Batch39Logical)
                EmitS6Batch39OnCreate(type, effectPosition,
                    angle, light, owner, subType);
            if (s6Batch40Logical)
                EmitS6Batch40OnCreate(type, subType,
                    effectPosition, angle, light, owner, effectScale);
            if (s6Batch41Logical)
                EmitS6Batch41OnCreate(type, handle, effectPosition,
                    angle, light, owner, effectScale);
            if (s6Batch43Logical)
                EmitS6Batch43OnCreate(type, handle, ref _effects[handle.Index]);
            if (s6Batch44Logical)
                EmitS6Batch44OnCreate(handle, ref _effects[handle.Index]);
            return handle;
        }

        public bool IsEffectAlive(ClassicFxHandle handle) =>
            Pools.Effects.IsAlive(handle);

        public bool ReleaseEffect(ClassicFxHandle handle)
        {
            if (!Pools.Effects.IsAlive(handle))
                return false;
            ReleaseEffectAt(handle.Index);
            return true;
        }

        private void ReleaseEffectAt(int index)
        {
            var view = _effects[index].ModelView;
            _effects[index] = default;
            ClassicFxHandle handle = Pools.Effects.GetHandle(index);
            if (handle.IsValid)
                Pools.Effects.Release(handle);
            if (view != null)
            {
                World?.RemoveObject(view);
                view.Dispose();
            }
        }

        // Called by ClassicFxRuntime.Update() before MoveParticles/MoveJoints.
        // Original 25-FPS lifetime semantics remain FPS-independent.
        private void MoveEffects()
        {
            if (_disposed || !Enabled)
                return;
            float f = Clock.FrameFactor;
            if (f <= 0f)
                return;
            for (int i = 0; i < _effects.Length; i++)
            {
                if (!Pools.Effects.IsActive(i))
                    continue;
                ref EffectState e = ref _effects[i];
                bool terrain = e.Type == ClassicFxEffectType.ShockWave ||
                               e.Type == ClassicFxEffectType.Twlight;
                if (e.Type == ClassicFxEffectType.AliceDrainLife)
                {
                    if (!MoveAliceDrainLife(ref e))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.LightningShock)
                {
                    if (!MoveLightningShock(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.Crater)
                {
                    MoveS6Crater(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.BlowOfDestruction)
                {
                    MoveS6BlowOfDestruction(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.LightningOrb)
                {
                    MoveS6LightningOrb(ref e, i, f);
                }
                else if (e.Type == ClassicFxEffectType.FuryStrike)
                {
                    if (!MoveS6FuryStrike(ref e, i, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.StarShine)
                {
                    MoveS6StarShine(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.SapitresAttackCarrier)
                {
                    MoveS6SapitresCarrier(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.HalloweenEx ||
                         e.Type == ClassicFxEffectType.DoorCrushCarrier ||
                         e.Type == ClassicFxEffectType.StatueCrushCarrier)
                {
                    // Logical carriers emit their children only at creation.
                    ReleaseEffectAt(i);
                    continue;
                }
                else if (IsS6Batch35LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch35Logical(ref e))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch36LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch36Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch37LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch37Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch38LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch38Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch39LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch39Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch40LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch40Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch41LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch41Logical(ref e, f, Pools.Effects.GetHandle(i)))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch42LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch42Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch43LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch43Logical(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch44LogicalType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch44Logical(ref e))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (IsS6Batch14TerrainType(e.Type, e.SubType))
                {
                    if (!MoveS6Batch14Terrain(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.Damage01Mono)
                {
                    MoveDamage01Mono(ref e, f);
                }
                else if (IsV5TerrainEffectType(e.Type))
                {
                    if (!MoveV5TerrainEffect(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    MoveMagicGround2(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    // MuMain +2 has no Move handler: the shared pool ages it.
                }
                else if (IsV8GroundEffectType(e.Type, e.SubType))
                {
                    if (!MoveV8GroundEffect(ref e, f))
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }
                }
                else if (terrain)
                {
                    // Terrain effects can outlive their BMD owner: preserve
                    // last known ground position if owner has disappeared.
                    MoveTerrainEffect(ref e, f);
                }
                else
                {
                    if (e.ModelView == null ||
                        e.ModelView.Status == GameControlStatus.Disposed ||
                        e.ModelView.Status == GameControlStatus.Error)
                    {
                        ReleaseEffectAt(i);
                        continue;
                    }

                    if (IsAdditionalModelEffectType(e.Type))
                    {
                        if (!MoveAdditionalModelEffect(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else if (IsBatch2EffectModelType(e.Type))
                    {
                        if (!MoveBatch2EffectModel(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else if (IsSeason6ModelType(e.Type))
                    {
                        if (!MoveSeason6Model(ref e, f))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        if (e.Owner.WorldObject is not PlayerObject player ||
                            player.Status != GameControlStatus.Ready ||
                            player.IsDead ||
                            !ReferenceEquals(player.World, World))
                        {
                            ReleaseEffectAt(i);
                            continue;
                        }

                        e.Position = player.WorldPosition.Translation;
                        if (e.Type == ClassicFxEffectType.SwellOfMagicPower)
                        {
                            MoveSwellOfMagicPower(ref e, player);
                        }
                        else if (e.Type == ClassicFxEffectType.ArrowsRe06)
                        {
                            if (!TryPlayerBonePosition(player, e.BoneIndex, out Vector3 pos))
                                continue;
                            e.Position = pos;
                            if (e.LifeTime >= 15f)
                                e.Scale *= MathF.Pow(1.05f, f);
                            else
                                e.Scale *= MathF.Pow(0.95f, f);
                            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                            CreateSprite(ClassicTextureIds.BitmapLight, pos,
                                e.Scale, e.Light, source);
                            CreateSprite(ClassicTextureIds.BitmapLight, pos,
                                e.Scale * 0.8f, e.Light, source);
                            if (e.LifeTime <= 10f)
                                e.Alpha *= MathF.Pow(0.95f, f);
                        }
                    }
                    e.ModelView.Position = e.Position;
                    e.ModelView.Angle = e.Angle;
                    e.ModelView.ApplyNativeRenderState(e.Scale, e.Alpha, e.BlendMeshLight, e.Light);
                    if (IsS6Batch05ModelType(e.Type) ||
                        IsS6Batch07ModelType(e.Type) ||
                        IsS6Batch08ModelType(e.Type) ||
                        IsS6Batch09ModelType(e.Type) ||
                        IsS6Batch10ModelType(e.Type) ||
                        IsS6Batch12ModelType(e.Type) ||
                        IsS6Batch35ModelType(e.Type) ||
                        IsS6Batch36ModelType(e.Type))
                        e.ModelView.Color = new Color(Vector3.Clamp(
                            e.Light, Vector3.Zero, Vector3.One));
                }
                e.LifeTime -= f;
                if (e.LifeTime <= 0f)
                    ReleaseEffectAt(i);
            }
        }

        private void MoveSwellOfMagicPower(ref EffectState e, PlayerObject owner)
        {
            // Native invokes these three FX pulses at LifeTime 45, 35, 25.
            // Track consumed thresholds: a 60-FPS frame can visit the same
            // integer LifeTime more than once, so avoid duplicate pulses.
            if (e.LifeTime <= 45f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                EmitWizardryGroundPulse(ref e);
            }
            if (e.LifeTime <= 35f && (e.TriggerMask & 2) == 0)
            {
                e.TriggerMask |= 2;
                EmitWizardryGroundPulse(ref e);
            }
            if (e.LifeTime <= 25f && (e.TriggerMask & 4) == 0)
            {
                e.TriggerMask |= 4;
                EmitWizardryGroundPulse(ref e);
            }

            // Main MoveEffects(MODEL_SWELL_OF_MAGICPOWER), subtype 0:
            // 45 frames; hand models at 45; purple 2LINE_GHOST during >=30;
            // body sprites in final 20 frames; mesh fade in final 20.
            if (e.FirstMove)
            {
                e.FirstMove = false;
                var source = ClassicFxOwner.FromWorldObject(owner);
                Vector3 purple = new Vector3(0.2f, 0.2f, 0.9f);
                if (TryPlayerBonePosition(owner, 28, out Vector3 right))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        right, e.Angle, purple, source, 1, 28);
                if (TryPlayerBonePosition(owner, 37, out Vector3 left))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        left, e.Angle, purple, source, 1, 37);
                Console.WriteLine("[ClassicFX] Effect MODEL_SWELL_OF_MAGICPOWER: 45 frames");
            }

            if (e.LifeTime >= 30f)
            {
                // Main creates two per frame, each FPS checked.
                ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                Vector3 purple = new Vector3(0.3f, 0.2f, 0.9f);
                for (int j = 0; j < 2; j++)
                {
                    CreateJointFpsChecked(ClassicTextureIds.Bitmap2LineGhost,
                        e.Position, e.Position, e.Angle, 1, source,
                        20f + System.Random.Shared.Next(10), priorColor: purple);
                }
            }
            if (e.LifeTime <= 20f)
            {
                Vector3 light = new Vector3(0.7f, 0.3f, 0.9f) *
                                (e.LifeTime * 0.05f);
                Matrix[] bones = owner.GetBoneTransforms();
                if (bones != null)
                {
                    Matrix world = owner.WorldPosition;
                    ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
                    for (int b = 0; b < bones.Length; b++)
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            (bones[b] * world).Translation, 1.5f, light, source);
                }
                e.BlendMeshLight *= MathF.Pow(0.86f, Clock.FrameFactor);
            }
        }

        private void EmitWizardryGroundPulse(ref EffectState source)
        {
            // Source ZzzEffect.cpp: 2x ShockWave 14 at Scale=5,
            // 1x Twlight 3 at Scale=6. Their native EyeRight = Light.
            if (source.ModelView == null)
                return;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(source.ModelView);
            Vector3 light = new Vector3(0.4f, 0.3f, 0.9f);
            // MonoGame character angles use radians; Effect terrain rotation
            // uses classic degrees.
            Vector3 angle = new Vector3(0f, 0f,
                MathHelper.ToDegrees(source.Angle.Z));
            int createdShockWaves = 0;
            for (int n = 0; n < 2; n++)
            {
                if (CreateEffect(ClassicFxEffectType.ShockWave,
                    source.Position, angle, light, owner, subType: 14, scale: 5f).IsValid)
                    createdShockWaves++;
            }
            bool createdTwlight = CreateEffect(ClassicFxEffectType.Twlight,
                source.Position, angle, light, owner, subType: 3, scale: 6f).IsValid;

            // Three messages per cast at most. Distinguishes failed creation
            // from a missing texture; avoids per-frame console spam.
            Console.WriteLine(
                $"[ClassicFX] Wizardry pulse: ShockWave {createdShockWaves}/2 " +
                $"(texture={Textures.TryGet(ClassicTextureIds.BitmapShockWave, out _)}), " +
                $"Twlight {(createdTwlight ? 1 : 0)}/1 " +
                $"(texture={Textures.TryGet(ClassicTextureIds.BitmapTwlight, out _)}).");
        }

        private void MoveTerrainEffect(ref EffectState e, float f)
        {
            // Source MoveHandlers.cpp: Move_BITMAP_SHOCK_WAVE/Move_BITMAP_TWLIGHT.
            // Both of Wizardry's subtypes use the same scale/fade logic.
            WorldObject owner = e.Owner.WorldObject;
            if (owner != null && ReferenceEquals(owner.World, World) &&
                owner.Status != GameControlStatus.Disposed)
                e.Position = owner.WorldPosition.Translation;

            e.Scale = MathF.Max(0f, e.Scale - 0.15f * f);
            if (e.Type == ClassicFxEffectType.Twlight)
                e.Angle.Z += 10f * f;

            if (e.LifeTime >= 20f)
            {
                e.Alpha += 0.1f * f;
                e.Phase += f;
                e.Light = e.BaseLight * (e.Phase * 0.1f);
            }
            else if (e.LifeTime <= 10f)
            {
                e.Phase -= f;
                e.Alpha -= 0.1f * f;
                e.Light = e.BaseLight * (e.Phase * 0.1f);
            }
        }

        /// <summary>
        /// Native RenderTerrainAlphaBitmap for ShockWave 14 and Twlight 3.
        /// Uses one shared GPU-batched billboard pipeline; geometry is a
        /// tessellated XY plane following the existing MonoGame terrain.
        /// Mesh BMD effects continue through WorldControl.RenderObjects().
        /// </summary>
        public void RenderEffects()
        {
            if (_disposed || !Enabled || _billboardRenderer == null ||
                World?.Terrain == null)
                return;

            _billboardRenderer.Begin();
            for (int i = 0; i < _effects.Length; i++)
            {
                if (!Pools.Effects.IsActive(i)) continue;
                ref EffectState e = ref _effects[i];
                if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    RenderMagicGround2(ref e);
                    continue;
                }
                if (e.Type == ClassicFxEffectType.BlowOfDestruction)
                {
                    if (e.LifeTime <= 24f &&
                        Textures.TryGet(ClassicTextureIds.BitmapFlareBlue,
                            out ClassicTextureResource flare))
                        QueueTerrainEffect(ref e, flare,
                            scaleOverride: e.SubType == 0 ? 4f : 6f);
                    continue;
                }
                if (IsS6Batch14TerrainType(e.Type, e.SubType))
                {
                    RenderS6Batch14Terrain(ref e);
                    continue;
                }
                if (IsS6Batch36GroundType(e.Type))
                {
                    RenderS6Batch36Ground(ref e);
                    continue;
                }
                if (IsS6Batch37GroundType(e.Type))
                {
                    RenderS6Batch37Ground(ref e);
                    continue;
                }
                if (IsS6Batch38TerrainType(e.Type))
                {
                    RenderS6Batch38Terrain(ref e);
                    continue;
                }
                if (IsS6Batch39TerrainType(e.Type))
                {
                    RenderS6Batch39Terrain(ref e);
                    continue;
                }
                if (IsS6Batch40TerrainType(e.Type))
                {
                    RenderS6Batch40Terrain(ref e);
                    continue;
                }
                if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    RenderMagicCircleGround(ref e);
                    continue;
                }
                if (IsV5TerrainEffectType(e.Type))
                {
                    RenderV5TerrainEffect(ref e);
                    continue;
                }
                int textureId = e.Type switch
                {
                    ClassicFxEffectType.ShockWave => ClassicTextureIds.BitmapShockWave,
                    ClassicFxEffectType.Twlight => ClassicTextureIds.BitmapTwlight,
                    ClassicFxEffectType.Damage01Mono => ClassicTextureIds.BitmapDamage01Mono,
                    ClassicFxEffectType.Crater => ClassicTextureIds.BitmapCrater,
                    _ => -1
                };
                if (textureId < 0 || e.Scale <= 0f ||
                    !Textures.TryGet(textureId, out ClassicTextureResource tex))
                    continue;

                QueueTerrainEffect(ref e, tex,
                    blendOverride: e.Type == ClassicFxEffectType.Crater
                        ? ClassicBlendMode.AlphaTest
                        : ClassicBlendMode.Glow);
            }
            _billboardRenderer.End();
        }

        private void QueueTerrainEffect(ref EffectState e, ClassicTextureResource texture,
            float? scaleOverride = null, Vector3? lightOverride = null,
            float? angleZOverride = null,
            ClassicBlendMode blendOverride = ClassicBlendMode.Glow)
        {
            // Main: RenderTerrainAlphaBitmap(), ZzzLodTerrain.cpp.
            // Each quad sits on an ACTUAL 100-unit tile; its four Z values
            // match TerrainRenderer's visual mesh, including TWFlags.Height.
            // Reuses the existing batched ClassicBillboardRenderer on Android.
            float tileScale = Constants.TERRAIN_SCALE;
            float fx = e.Position.X / tileScale;
            float fy = e.Position.Y / tileScale;
            int cellX = (int)fx;
            int cellY = (int)fy;
            float size = scaleOverride ?? e.Scale;
            if (size <= 0f) return;

            // Faithful original tile bounds and texcoord derivation.
            int extent = (int)size + 1;
            float texU = (cellX - fx) + 0.5f * size;
            float texV = (cellY - fy) + 0.5f * size;
            float invSize = 1f / size;
            float radians = MathHelper.ToRadians(-(angleZOverride ?? e.Angle.Z));
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            int lastTile = Constants.TERRAIN_SIZE - 1;

            for (int dy = -extent; dy <= extent; dy++)
            {
                int tileY = cellY + dy;
                if (tileY < 0 || tileY >= lastTile) continue;

                for (int dx = -extent; dx <= extent; dx++)
                {
                    int tileX = cellX + dx;
                    if (tileX < 0 || tileX >= lastTile) continue;

                    Vector3 p0 = EffectTerrainTilePoint(tileX, tileY);
                    Vector3 p1 = EffectTerrainTilePoint(tileX + 1, tileY);
                    Vector3 p2 = EffectTerrainTilePoint(tileX + 1, tileY + 1);
                    Vector3 p3 = EffectTerrainTilePoint(tileX, tileY + 1);

                    float u0 = texU + dx;
                    float v0 = texV + dy;
                    Vector2 uv0 = EffectTerrainTileUv(u0, v0, invSize, cos, sin);
                    Vector2 uv1 = EffectTerrainTileUv(u0 + 1f, v0, invSize, cos, sin);
                    Vector2 uv2 = EffectTerrainTileUv(u0 + 1f, v0 + 1f, invSize, cos, sin);
                    Vector2 uv3 = EffectTerrainTileUv(u0, v0 + 1f, invSize, cos, sin);

                    _billboardRenderer.QueueWorldQuad(texture,
                        p0, p1, p2, p3, uv0, uv1, uv2, uv3,
                        lightOverride ?? e.Light, blendOverride, ClassicDepthMode.ReadOnly);
                }
            }
        }

        private Vector3 EffectTerrainTilePoint(int tileX, int tileY)
        {
            float x = tileX * Constants.TERRAIN_SCALE;
            float y = tileY * Constants.TERRAIN_SCALE;
            // DepthRead + 2 world units prevents z fighting on flat tiles.
            float z = World.Terrain.RequestTerrainRenderHeight(x, y) + 2f;
            return new Vector3(x, y, z);
        }

        private static Vector2 EffectTerrainTileUv(
            float u, float v, float invSize, float cos, float sin)
        {
            // Original: rotate texcoords around center (0.5, 0.5).
            float x = u * invSize - 0.5f;
            float y = v * invSize - 0.5f;
            return new Vector2(
                x * cos - y * sin + 0.5f,
                x * sin + y * cos + 0.5f);
        }

        private static bool TryPlayerBonePosition(PlayerObject owner,
            int boneIndex, out Vector3 position)
        {
            Matrix[] bones = owner.GetBoneTransforms();
            if (bones == null || (uint)boneIndex >= (uint)bones.Length)
            {
                position = Vector3.Zero;
                return false;
            }
            position = (bones[boneIndex] * owner.WorldPosition).Translation;
            return true;
        }

        private void ClearEffectStorage()
        {
            for (int i = 0; i < _effects.Length; i++)
            {
                var view = _effects[i].ModelView;
                _effects[i] = default;
                if (view == null)
                    continue;
                World?.RemoveObject(view);
                view.Dispose();
            }
        }
    }

    /// <summary>
    /// No second BMD renderer: meshes, animation and blend go through
    /// NeffisDev's existing ModelObject and WorldControl passes.
    /// </summary>
    internal sealed class ClassicFxEffectModelObject : ModelObject
    {
        private readonly string _bmdPath;
        // This is a model-asset renderer profile, not skill-logic scale tuning.
        // Values are grounded in the previous DarkLordCriticalHandEffect renderer.
        // Disable for a side-by-side A/B comparison of the generic model bridge.
        private const bool EnableDarkLordModelProfile = true;
        private readonly bool _useDarkLordModelProfile;
        private const float DarkLordRenderMaxScale = 0.65f;
        private const float DarkLordRenderOpacity = 0.48f;
        private const float DarkLordRenderBlendIntensity = 0.70f;

        public ClassicFxEffectModelObject(string bmdPath, ClassicFxEffectType type)
        {
            _bmdPath = bmdPath;
            _useDarkLordModelProfile = EnableDarkLordModelProfile &&
                type == ClassicFxEffectType.DarkLordSkill;
            IsTransparent = true;
            AffectedByTransparency = true;
            BlendState = BlendState.Additive;
            BlendMesh = 0;
            BlendMeshLight = 1f;
            DepthState = DepthStencilState.DepthRead;
            RenderShadow = false;
            LightEnabled = false;
            UseSunLight = false;
            ContinuousAnimation = !_useDarkLordModelProfile;
            AnimationSpeed = 25f;
            Color = type == ClassicFxEffectType.SwellOfMagicPower
                ? new Color(0.7f, 0.4f, 0.9f)
                : new Color(0.2f, 0.2f, 0.9f);
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f, -250f, -200f),
                new Vector3(250f, 250f, 350f));
            Interactive = false;
        }

        // Classic spell BMDs may require a different shader/material path
        // than world actors. The first measured profile concerns DarkLordSkill.
        protected override bool AllowDynamicLightingShader =>
            !_useDarkLordModelProfile;

        // Keep original EffectState math unmodified. Translation happens only
        // when state enters the MonoGame renderer. All non-profiled models
        // retain the previous ClassicFX presentation, exactly as before.
        public void ApplyNativeRenderState(float nativeScale,
            float nativeAlpha, float nativeBlendMeshLight, Vector3 nativeLight)
        {
            if (_useDarkLordModelProfile)
            {
                // The prior ModelObject cast uses Light=(1,.6,.3), NOT just Color.
                // When the dynamic shader is disabled, CPU skinning obtains
                // vertex illumination from ModelObject.Light. Zero = black.
                // Keep the native source light and do not double-tint it.
                Light = nativeLight;
                Color = Microsoft.Xna.Framework.Color.White;
                // Previous functioning DarkLordCriticalHandEffect parameters.
                // This is a temporary renderer calibration for this BMD asset,
                // not a change to native lifespan or growth physics.
                Scale = MathF.Min(nativeScale, DarkLordRenderMaxScale);
                Alpha = MathHelper.Clamp(
                    nativeAlpha * DarkLordRenderOpacity, 0f, 1f);
                BlendMeshLight = nativeBlendMeshLight * DarkLordRenderBlendIntensity;
                BlendMesh = -1; // Previous ModelObject default, not mesh 0.
            }
            else
            {
                Scale = nativeScale;
                Alpha = MathHelper.Clamp(nativeAlpha, 0f, 1f);
                BlendMeshLight = nativeBlendMeshLight;
            }
        }

        public override async Task Load()
        {
            try
            {
                Model = await BMDLoader.Instance.Prepare(_bmdPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClassicFX] BMD {_bmdPath}: {ex.Message}");
                Status = GameControlStatus.Error;
                return;
            }
            await base.Load();
        }
    }
}
