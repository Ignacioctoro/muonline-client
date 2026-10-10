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
            float scale = 1f)
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
                s6Batch19Logical ||
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
                // Translation from native Effect state to MonoGame BMD presentation.
                view.ApplyNativeRenderState(effectScale, effectAlpha, effectMeshLight, light);
            }
            _effects[handle.Index] = new EffectState
            {
                Type = type,
                SubType = subType,
                Owner = owner,
                Position = effectPosition,
                StartPosition = (type == ClassicFxEffectType.PierPart && subType == 0) ||
                    (type == ClassicFxEffectType.BlowOfDestruction && subType == 0)
                    ? inputLight : effectPosition,
                HeadAngle = effectHeading,
                Angle = angle,
                Light = light,
                BaseLight = light,
                Scale = effectScale,
                Direction = effectDirection,
                Velocity = effectVelocity,
                Gravity = effectGravity,
                Alpha = terrain ? 0f : effectAlpha,
                BlendMeshLight = effectMeshLight,
                LifeTime = life,
                BoneIndex = boneIndex,
                FirstMove = true,
                Phase = furyStrike ? Random.Modulo(100) : 0f,
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
                        IsS6Batch12ModelType(e.Type))
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
