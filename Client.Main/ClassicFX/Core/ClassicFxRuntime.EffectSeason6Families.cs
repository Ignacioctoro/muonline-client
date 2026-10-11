// Season 6 Effect families ported from MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// EffectTypes.json, ZzzOpenData.cpp, Behaviors/MoveHandlers.cpp.
// Uses the pre-existing Effect pool, BMD ModelObject, texture repository,
// native AngleMatrix, joints, sprites, particles and world/bone bridges.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Controls;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct Season6ModelDefinition
        {
            public readonly string Path;
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly float Alpha;
            public readonly float MeshLight;
            public readonly float OffsetZ;
            public readonly bool NeedsOwner;
            public readonly bool UseCallerScale;
            public readonly bool WhiteLight;

            public Season6ModelDefinition(string path, float lifeTime,
                float scale, float alpha = 1f, float meshLight = 1f,
                float offsetZ = 0f, bool needsOwner = false,
                bool useCallerScale = false, bool whiteLight = false)
            {
                Path = path;
                LifeTime = lifeTime;
                Scale = scale;
                Alpha = alpha;
                MeshLight = meshLight;
                OffsetZ = offsetZ;
                NeedsOwner = needsOwner;
                UseCallerScale = useCallerScale;
                WhiteLight = whiteLight;
            }
        }

        private static bool IsSeason6ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.AliceBuffSkillEffect or
                ClassicFxEffectType.AliceBuffSkillEffect2 or
                ClassicFxEffectType.ShockWaveGround01 or
                ClassicFxEffectType.Wave || IsS6Batch02ModelType(type) ||
                IsS6Batch04ModelType(type) || IsS6Batch05ModelType(type) ||
                IsS6Batch06ModelType(type) || IsS6Batch07ModelType(type) ||
                IsS6Batch08ModelType(type) ||
                IsS6Batch09ModelType(type) ||
                IsS6Batch10ModelType(type) ||
                IsS6Batch11ModelType(type) ||
                IsS6Batch12ModelType(type) ||
                IsS6Batch13QuakeType(type) ||
                IsS6Batch15ModelType(type) ||
                IsS6Batch16ModelType(type) ||
                IsS6Batch17ModelType(type) ||
                IsS6Batch18ModelType(type) ||
                IsS6Batch19CrushModel(type) ||
                IsS6Batch20AvailableModel(type) ||
                IsS6Batch21ModelType(type) ||
                IsS6Batch22ModelType(type) ||
                IsS6Batch23ModelType(type) ||
                IsS6Batch24ModelType(type) ||
                IsS6Batch25ModelType(type) ||
                IsS6Batch26ModelType(type) ||
                IsS6Batch27ModelType(type) ||
                IsS6Batch28ModelType(type);

        private static bool TryGetSeason6ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.AliceBuffSkillEffect:
                    if (subType is >= 0 and <= 2)
                    {
                        definition = new Season6ModelDefinition(
                            "Effect/elshildring.bmd", 34f, 0.1f,
                            alpha: 0f, meshLight: 0f,
                            offsetZ: 100f, needsOwner: true);
                        return true;
                    }
                    if (subType == 3 || subType == 4)
                    {
                        definition = new Season6ModelDefinition(
                            "Effect/elshildring.bmd", 100f,
                            subType == 3 ? 1.5f : 1f, needsOwner: true);
                        return true;
                    }
                    return false;

                case ClassicFxEffectType.AliceBuffSkillEffect2:
                    // MuMain target casts also pass subtypes 1 and 2;
                    // all three share EffectTypes.json initialization.
                    if (subType < 0 || subType > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Effect/elshildring2.bmd", 35f, 0.15f,
                        alpha: 0f, meshLight: 0f,
                        offsetZ: 100f, needsOwner: true);
                    return true;

                case ClassicFxEffectType.ShockWaveGround01:
                    if (subType < 0 || subType > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Effect/shockwave_ground01.bmd",
                        subType == 0 ? 20f : subType == 2 ? 10f : 50f,
                        1f, useCallerScale: true);
                    return true;

                case ClassicFxEffectType.Wave:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/flashing.bmd", 15f, 0.5f, meshLight: 1.5f,
                        offsetZ: -15f, whiteLight: true);
                    return true;

                default:
                    if (TryGetS6Batch02ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch04ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch05ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch06ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch07ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch08ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch09ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch10ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch11ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch12ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch13ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch15ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch16ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch17ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch18ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch19ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch20ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch21ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch22ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch23ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch24ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch25ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch26ModelDefinition(type, subType,
                            out definition))
                        return true;
                    if (TryGetS6Batch27ModelDefinition(type, subType,
                            out definition))
                        return true;
                    return TryGetS6Batch28ModelDefinition(type, subType,
                        out definition);
            }
        }

        private bool MoveSeason6Model(ref EffectState effect, float f)
        {
            switch (effect.Type)
            {
                case ClassicFxEffectType.AliceBuffSkillEffect:
                case ClassicFxEffectType.AliceBuffSkillEffect2:
                    return MoveAliceBuffModel(ref effect, f);

                case ClassicFxEffectType.ShockWaveGround01:
                    // Move_MODEL_SHOCKWAVE_GROUND01: fade differs by subtype.
                    effect.Scale *= MathF.Pow(1.2f, f);
                    float fade = effect.SubType == 1 ? 0.9f :
                                 effect.SubType == 2 ? 0.7f : 0.8f;
                    effect.Light *= MathF.Pow(fade, f);
                    return true;

                case ClassicFxEffectType.Wave:
                    // Move_MODEL_WAVE: transition from rapidly growing wave
                    // to drifting/fading wave after scale passes two.
                    bool grown = effect.Scale > 2f;
                    effect.Scale += (grown ? 0.1f : 1.2f) * f;
                    effect.Position.X -= (grown ? 1f : 1.2f) * f;
                    effect.Position.Z -= (grown ? 1.5f : 1.8f) * f;
                    if (grown)
                        effect.BlendMeshLight = effect.LifeTime / 30f;
                    return true;
                case ClassicFxEffectType.CircleLight:
                case ClassicFxEffectType.Stone1:
                case ClassicFxEffectType.Stone2:
                case ClassicFxEffectType.KnightPlancrackA:
                    return MoveS6Batch02Model(ref effect, f);
                case ClassicFxEffectType.SkillBlast:
                case ClassicFxEffectType.SkillInferno:
                case ClassicFxEffectType.Circle:
                    return MoveS6Batch04Model(ref effect, f);
                case ClassicFxEffectType.Storm:
                case ClassicFxEffectType.Summon:
                case ClassicFxEffectType.Tail:
                case ClassicFxEffectType.WaveForce:
                    return MoveS6Batch05Model(ref effect, f);
                case ClassicFxEffectType.Piercing:
                case ClassicFxEffectType.ArrowBomb:
                case ClassicFxEffectType.ArrowNature:
                case ClassicFxEffectType.ArrowDouble:
                case ClassicFxEffectType.ArrowWing:
                    return MoveS6Batch06Model(ref effect, f);
                case ClassicFxEffectType.Fire:
                case ClassicFxEffectType.Ice:
                case ClassicFxEffectType.IceSmall:
                case ClassicFxEffectType.Blizzard:
                case ClassicFxEffectType.Snow1:
                case ClassicFxEffectType.Snow2:
                case ClassicFxEffectType.Snow3:
                    return MoveS6Batch07Model(ref effect, f);
                case ClassicFxEffectType.DarkScream:
                case ClassicFxEffectType.DarkScreamFire:
                case ClassicFxEffectType.ManaRune:
                    return MoveS6Batch08Model(ref effect, f);
                case ClassicFxEffectType.Javelin:
                case ClassicFxEffectType.ArrowImpact:
                case ClassicFxEffectType.SkinShell:
                case ClassicFxEffectType.StunStone:
                    return MoveS6Batch09Model(ref effect, f);
                case ClassicFxEffectType.Waves:
                case ClassicFxEffectType.Piercing2:
                case ClassicFxEffectType.PierPart:
                    return MoveS6Batch10Model(ref effect, f);
                case ClassicFxEffectType.NightWater01:
                case ClassicFxEffectType.KnightPlancrackB:
                case ClassicFxEffectType.RaklionBossCrack:
                    return MoveS6Batch11Model(ref effect, f);
                case ClassicFxEffectType.FenrirThunder:
                case ClassicFxEffectType.Magic2:
                    return MoveS6Batch12Model(ref effect, f);
                case ClassicFxEffectType.FuryQuake1:
                case ClassicFxEffectType.FuryQuake2:
                case ClassicFxEffectType.FuryQuake3:
                case ClassicFxEffectType.FuryQuake4:
                case ClassicFxEffectType.FuryQuake5:
                case ClassicFxEffectType.FuryQuake6:
                case ClassicFxEffectType.FuryQuake7:
                case ClassicFxEffectType.FuryQuake8:
                    return MoveS6Batch13Quake(ref effect, f);
                case ClassicFxEffectType.BrokenIce0:
                case ClassicFxEffectType.BrokenIce1:
                case ClassicFxEffectType.BrokenIce2:
                case ClassicFxEffectType.BrokenIce3:
                case ClassicFxEffectType.CursedStatue1:
                case ClassicFxEffectType.CursedStatue2:
                case ClassicFxEffectType.SnowmanHead:
                case ClassicFxEffectType.SnowmanBody:
                case ClassicFxEffectType.Feather:
                case ClassicFxEffectType.FeatherForeign:
                case ClassicFxEffectType.SapitresAttack1:
                case ClassicFxEffectType.SapitresAttack2:
                case ClassicFxEffectType.FlameStrike:
                    return MoveS6Batch15Model(ref effect, f);
                case ClassicFxEffectType.MultiShot1:
                case ClassicFxEffectType.MultiShot2:
                case ClassicFxEffectType.MultiShot3:
                case ClassicFxEffectType.BigStonePart1:
                case ClassicFxEffectType.BigStonePart2:
                case ClassicFxEffectType.WallPart1:
                case ClassicFxEffectType.WallPart2:
                case ClassicFxEffectType.GatePart1:
                case ClassicFxEffectType.GatePart2:
                case ClassicFxEffectType.GatePart3:
                case ClassicFxEffectType.GolemStone:
                case ClassicFxEffectType.ArrowSteel:
                case ClassicFxEffectType.ArrowThunder:
                case ClassicFxEffectType.ArrowLaser:
                case ClassicFxEffectType.ArrowV:
                case ClassicFxEffectType.ArrowSaw:
                case ClassicFxEffectType.ArrowSpark:
                case ClassicFxEffectType.ArrowGamble:
                    return MoveS6Batch16Model(ref effect, f);
                case ClassicFxEffectType.HalloweenCandyBlue:
                case ClassicFxEffectType.HalloweenCandyOrange:
                case ClassicFxEffectType.HalloweenCandyYellow:
                case ClassicFxEffectType.HalloweenCandyRed:
                case ClassicFxEffectType.HalloweenCandyHobak:
                case ClassicFxEffectType.HalloweenCandyStar:
                case ClassicFxEffectType.MoonHarvestGam:
                case ClassicFxEffectType.MoonHarvestSongpuen1:
                case ClassicFxEffectType.MoonHarvestSongpuen2:
                case ClassicFxEffectType.MoonHarvestMoon:
                case ClassicFxEffectType.ChangeUpEffect:
                case ClassicFxEffectType.ChangeUpNasa:
                case ClassicFxEffectType.ChangeUpCylinder:
                case ClassicFxEffectType.ArrowBestCrossbow:
                case ClassicFxEffectType.ArrowDrill:
                case ClassicFxEffectType.ArrowRing:
                    return MoveS6Batch17Model(ref effect, f);
                case ClassicFxEffectType.XmasEventBox:
                case ClassicFxEffectType.XmasEventCandy:
                case ClassicFxEffectType.XmasEventTree:
                case ClassicFxEffectType.XmasEventSocks:
                case ClassicFxEffectType.XmasEventIceHeart:
                case ClassicFxEffectType.NewYearsDayBeksulki:
                case ClassicFxEffectType.NewYearsDayCandy:
                case ClassicFxEffectType.NewYearsDayMoney:
                case ClassicFxEffectType.NewYearsDayHotPepperGreen:
                case ClassicFxEffectType.NewYearsDayHotPepperRed:
                case ClassicFxEffectType.NewYearsDayPig:
                case ClassicFxEffectType.NewYearsDayYut:
                    return MoveS6Batch18Model(ref effect, f);
                case ClassicFxEffectType.DoorCrushPiece01:
                case ClassicFxEffectType.DoorCrushPiece02:
                case ClassicFxEffectType.DoorCrushPiece03:
                case ClassicFxEffectType.DoorCrushPiece04:
                case ClassicFxEffectType.DoorCrushPiece05:
                case ClassicFxEffectType.DoorCrushPiece06:
                case ClassicFxEffectType.DoorCrushPiece07:
                case ClassicFxEffectType.DoorCrushPiece08:
                case ClassicFxEffectType.DoorCrushPiece10:
                case ClassicFxEffectType.DoorCrushPiece11:
                case ClassicFxEffectType.DoorCrushPiece12:
                case ClassicFxEffectType.DoorCrushPiece13:
                case ClassicFxEffectType.StatueCrushPiece01:
                case ClassicFxEffectType.StatueCrushPiece02:
                case ClassicFxEffectType.StatueCrushPiece03:
                case ClassicFxEffectType.StatueCrushPiece04:
                    return MoveS6Batch19CrushModel(ref effect, f);
                case ClassicFxEffectType.CondraArmL:
                case ClassicFxEffectType.CondraArmL2:
                case ClassicFxEffectType.CondraShoulder:
                case ClassicFxEffectType.CondraArmR:
                case ClassicFxEffectType.CondraArmR2:
                case ClassicFxEffectType.CondraConeL:
                case ClassicFxEffectType.CondraConeR:
                case ClassicFxEffectType.NarCondraArmL:
                case ClassicFxEffectType.NarCondraArmL2:
                case ClassicFxEffectType.NarCondraShoulderL:
                case ClassicFxEffectType.NarCondraShoulderR:
                case ClassicFxEffectType.NarCondraArmR:
                case ClassicFxEffectType.NarCondraArmR2:
                case ClassicFxEffectType.NarCondraArmR3:
                case ClassicFxEffectType.NarCondraCone1:
                case ClassicFxEffectType.NarCondraCone2:
                case ClassicFxEffectType.NarCondraCone3:
                case ClassicFxEffectType.NarCondraCone4:
                case ClassicFxEffectType.NarCondraCone5:
                case ClassicFxEffectType.NarCondraCone6:
                case ClassicFxEffectType.NarCondraPelvis:
                case ClassicFxEffectType.NarCondraStomach:
                case ClassicFxEffectType.NarCondraNeck:
                case ClassicFxEffectType.CondraStone:
                case ClassicFxEffectType.CondraStone1:
                case ClassicFxEffectType.CondraStone2:
                case ClassicFxEffectType.CondraStone3:
                case ClassicFxEffectType.CondraStone4:
                case ClassicFxEffectType.CondraStone5:
                case ClassicFxEffectType.NarCondraStone:
                case ClassicFxEffectType.NarCondraStone1:
                case ClassicFxEffectType.NarCondraStone2:
                case ClassicFxEffectType.NarCondraStone3:
                    return MoveS6Batch20Model(ref effect, f);
                case ClassicFxEffectType.ShadowPawnAnkleLeft:
                case ClassicFxEffectType.ShadowPawnAnkleRight:
                case ClassicFxEffectType.ShadowPawnBelt:
                case ClassicFxEffectType.ShadowPawnChest:
                case ClassicFxEffectType.ShadowPawnHelmet:
                case ClassicFxEffectType.ShadowPawnKneeLeft:
                case ClassicFxEffectType.ShadowPawnKneeRight:
                case ClassicFxEffectType.ShadowPawnWristLeft:
                case ClassicFxEffectType.ShadowPawnWristRight:
                case ClassicFxEffectType.ShadowKnightAnkleLeft:
                case ClassicFxEffectType.ShadowKnightAnkleRight:
                case ClassicFxEffectType.ShadowKnightBelt:
                case ClassicFxEffectType.ShadowKnightChest:
                case ClassicFxEffectType.ShadowKnightHelmet:
                case ClassicFxEffectType.ShadowKnightKneeLeft:
                case ClassicFxEffectType.ShadowKnightKneeRight:
                case ClassicFxEffectType.ShadowKnightWristLeft:
                case ClassicFxEffectType.ShadowKnightWristRight:
                case ClassicFxEffectType.ShadowRookAnkleLeft:
                case ClassicFxEffectType.ShadowRookAnkleRight:
                case ClassicFxEffectType.ShadowRookBelt:
                case ClassicFxEffectType.ShadowRookChest:
                case ClassicFxEffectType.ShadowRookHelmet:
                case ClassicFxEffectType.ShadowRookKneeLeft:
                case ClassicFxEffectType.ShadowRookKneeRight:
                case ClassicFxEffectType.ShadowRookWristLeft:
                case ClassicFxEffectType.ShadowRookWristRight:
                    return MoveS6Batch21Model(ref effect, f);
                case ClassicFxEffectType.TotemGolemPart1:
                case ClassicFxEffectType.TotemGolemPart2:
                case ClassicFxEffectType.TotemGolemPart3:
                case ClassicFxEffectType.TotemGolemPart4:
                case ClassicFxEffectType.TotemGolemPart5:
                case ClassicFxEffectType.TotemGolemPart6:
                case ClassicFxEffectType.IceGiantPart1:
                case ClassicFxEffectType.IceGiantPart2:
                case ClassicFxEffectType.IceGiantPart3:
                case ClassicFxEffectType.IceGiantPart4:
                case ClassicFxEffectType.IceGiantPart5:
                case ClassicFxEffectType.IceGiantPart6:
                case ClassicFxEffectType.Bone1:
                case ClassicFxEffectType.Bone2:
                case ClassicFxEffectType.BigStone1:
                case ClassicFxEffectType.BigStone2:
                    return MoveS6Batch22Model(ref effect, f);
                case ClassicFxEffectType.KundunPart1:
                case ClassicFxEffectType.KundunPart2:
                case ClassicFxEffectType.KundunPart3:
                case ClassicFxEffectType.KundunPart4:
                case ClassicFxEffectType.KundunPart5:
                case ClassicFxEffectType.KundunPart6:
                case ClassicFxEffectType.KundunPart7:
                case ClassicFxEffectType.KundunPart8:
                case ClassicFxEffectType.MayaStone1:
                case ClassicFxEffectType.MayaStone2:
                case ClassicFxEffectType.MayaStone3:
                case ClassicFxEffectType.MayaStone4:
                case ClassicFxEffectType.MayaStone5:
                case ClassicFxEffectType.MayaStoneFire:
                case ClassicFxEffectType.MayaHandSkill:
                case ClassicFxEffectType.MayaStar:
                    return MoveS6Batch23Model(ref effect, f);
                case ClassicFxEffectType.SummonerWristRing:
                case ClassicFxEffectType.SummonerHeadSahamutt:
                case ClassicFxEffectType.SummonerHeadNeil:
                case ClassicFxEffectType.SummonerHeadLagul:
                case ClassicFxEffectType.SummonerSahamutt:
                case ClassicFxEffectType.SummonerNeil:
                case ClassicFxEffectType.SummonerLagul:
                case ClassicFxEffectType.SummonerNeilKnife1:
                case ClassicFxEffectType.SummonerNeilKnife2:
                case ClassicFxEffectType.SummonerNeilKnife3:
                case ClassicFxEffectType.SummonerNeilGround1:
                case ClassicFxEffectType.SummonerNeilGround2:
                case ClassicFxEffectType.SummonerNeilGround3:
                    return MoveS6Batch24Model(ref effect, f);
                case ClassicFxEffectType.CursedTempleHolyItem:
                case ClassicFxEffectType.CursedTempleProtection:
                case ClassicFxEffectType.CursedTempleRestraint:
                case ClassicFxEffectType.DoppelgangerSlimeChip:
                case ClassicFxEffectType.RaklionBossMagic:
                case ClassicFxEffectType.Shockwave01:
                case ClassicFxEffectType.Shockwave02:
                case ClassicFxEffectType.ShockwaveSpin01:
                case ClassicFxEffectType.WindForce:
                case ClassicFxEffectType.SdAura:
                    return MoveS6Batch25Model(ref effect, f);
                case ClassicFxEffectType.ShieldCrashModel:
                case ClassicFxEffectType.ShieldCrashRing:
                case ClassicFxEffectType.ComboModel:
                case ClassicFxEffectType.FissureModel:
                case ClassicFxEffectType.FissureLight:
                case ClassicFxEffectType.WaterWaveModel:
                case ClassicFxEffectType.IronRiderArrowModel:
                case ClassicFxEffectType.KentaurosArrowModel:
                case ClassicFxEffectType.DragonLowerDummy:
                case ClassicFxEffectType.BalgasSkillModel:
                case ClassicFxEffectType.DarkElfSkillModel:
                case ClassicFxEffectType.ArrowAutoLoadModel:
                    return MoveS6Batch26Model(ref effect, f);
                case ClassicFxEffectType.InfinityArrowCore:
                case ClassicFxEffectType.InfinityArrow1:
                case ClassicFxEffectType.InfinityArrow2:
                case ClassicFxEffectType.InfinityArrow3:
                case ClassicFxEffectType.InfinityArrow4:
                case ClassicFxEffectType.BladeSkillModel:
                case ClassicFxEffectType.WolfHeadEffect:
                case ClassicFxEffectType.WolfHeadEffect2:
                case ClassicFxEffectType.DownAttackDummyL:
                case ClassicFxEffectType.DownAttackDummyR:
                case ClassicFxEffectType.DragonKickDummy:
                    return MoveS6Batch27Model(ref effect, f);
                case ClassicFxEffectType.PhoenixShotModel:
                case ClassicFxEffectType.WindSpin02Model:
                case ClassicFxEffectType.WindSpin03Model:
                case ClassicFxEffectType.VolcanoOfMonkModel:
                case ClassicFxEffectType.VolcanoStoneModel:
                case ClassicFxEffectType.MoveTargetPositionModel:
                case ClassicFxEffectType.SakuraEventItemModel:
                case ClassicFxEffectType.UmbrellaGoldModel:
                case ClassicFxEffectType.EmpireGuardianFrameStrikeModel:
                    return MoveS6Batch28Model(ref effect, f);
                default:
                    return false;
            }
        }

        private bool MoveAliceBuffModel(ref EffectState e, float f)
        {
            if (!TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot owner) ||
                owner.WorldObject == null ||
                !ReferenceEquals(owner.WorldObject.World, World) ||
                owner.WorldObject.Status != GameControlStatus.Ready)
                return false;

            if (e.SubType == 3 || e.SubType == 4)
            {
                // Owner-bound persistent buff: lifespan is refreshed until
                // owner goes away. Bone source is the actual owner model.
                e.LifeTime = 100f;
                if (!Clock.AdvancedReferenceFrame ||
                    owner.WorldObject is not ModelObject model)
                    return true;

                Matrix[] bones = model.GetBoneTransforms();
                if (bones == null || bones.Length == 0)
                    return true;
                int bone = Random.Modulo(bones.Length);
                if (!TryGetOwnerBonePosition(e.Owner, bone, out Vector3 location))
                    return true;

                if (e.SubType == 3)
                {
                    CreateParticle(ClassicTextureIds.BitmapLight + 2,
                        location, e.Angle, e.Light, 6, e.Scale);
                    bone = Random.Modulo(bones.Length);
                    if (TryGetOwnerBonePosition(e.Owner, bone, out location))
                        CreateParticle(ClassicTextureIds.BitmapLight + 2,
                            location, e.Angle, e.Light, 6, e.Scale);
                }
                else if (Random.Modulo(2) == 0)
                {
                    location.Z -= 20f;
                    CreateParticle(ClassicTextureIds.BitmapTwinTailWater,
                        location, e.Angle, e.Light, 2);
                }
                return true;
            }
            if (e.SubType is < 0 or > 2) return false;

            // Three native colored ring subtypes, with no equipment
            // requirement and without guessing bone indexes.
            e.Position = owner.Position + new Vector3(0f, 0f, 100f * f);
            if (e.LifeTime > 20f)
            {
                e.Alpha += 0.05f * f;
                e.BlendMeshLight += 0.05f * f;
            }
            else
            {
                e.Alpha -= 0.05f * f;
                e.BlendMeshLight -= 0.05f * f;
                if (e.Alpha < 0f) return false;
            }
            e.Angle.Z += MathHelper.ToRadians(
                (e.Type == ClassicFxEffectType.AliceBuffSkillEffect ? 8f : -8f) * f);
            e.Scale += 0.035f * f;

            // Native: 2 flare sprites, 2 shiny sprites and 3 healing
            // joints every 25-FPS tick. Avoid high-refresh sprite floods.
            // Already tick-gated: children use direct Create*, NOT FPS-checked
            // overloads (which would double-filter them at high refresh).
            if (!Clock.AdvancedReferenceFrame) return true;

            ClassicFxOwner source = ClassicFxOwner.FromWorldObject(e.ModelView);
            Vector3 flare = e.SubType switch
            {
                0 => new Vector3(0.8f, 0.1f, 0.9f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            flare *= MathF.Max(0f, e.Alpha);
            int spriteSubtype = e.SubType == 1 ? 1 : 0;
            for (int n = 0; n < 2; n++)
                CreateSprite(ClassicTextureIds.BitmapLight, e.Position, 5f,
                    flare, source, subType: spriteSubtype);

            Vector3 shiny = e.SubType switch
            {
                0 => new Vector3(0.7f, 0.6f, 0.9f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            shiny *= MathF.Max(0f, e.Alpha);
            float spin = (float)(Clock.WorldTimeMilliseconds * 0.0006) * 360f;
            CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                e.Position, 2f, shiny, source, spin, spriteSubtype);
            CreateSprite(ClassicTextureIds.BitmapShiny + 5,
                e.Position, 1f, shiny, source, -spin, spriteSubtype);

            Vector3 jointLight = e.SubType switch
            {
                0 => new Vector3(0.7f, 0.5f, 0.7f),
                1 => Vector3.One,
                _ => new Vector3(0.8f, 0.5f, 0.2f)
            };
            for (int n = 0; n < 3; n++)
            {
                Vector3 angle = new Vector3(Random.Modulo(90), 0f, Random.Modulo(360));
                Vector3 radial = ClassicMath.VectorRotate(
                    new Vector3(0f, -200f, 0f), ClassicMath.AngleMatrix(angle));
                CreateJoint(ClassicTextureIds.BitmapJointHealing,
                    e.Position - radial, e.Position, angle,
                    subType: spriteSubtype == 1 ? 16 : 15,
                    target: source, scale: 5f, priorColor: jointLight);
            }
            return true;
        }
    }
}
