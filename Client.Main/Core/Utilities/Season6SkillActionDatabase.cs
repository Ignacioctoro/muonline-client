using System;
using System.Collections.Generic;
using Client.Data.BMD;
using Client.Main.Models;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// How the classic Season 6 client chooses the player animation
    /// for a skill.
    ///
    /// IMPORTANT:
    /// Never store raw PlayerAction integer values here.
    /// PlayerAction has changed indexes during the NeffisDev port.
    /// Always reference the enum member directly.
    /// </summary>
    public enum Season6SkillAnimationMode
    {
        /// <summary>
        /// Classic SetPlayerMagic-style animation.
        /// PlayerObject decides the correct generic magic animation
        /// based on class / mount.
        /// </summary>
        DefaultMagic,

        /// <summary>
        /// A specific PlayerAction is required.
        /// </summary>
        Explicit,

        /// <summary>
        /// Use the currently equipped weapon attack animation.
        /// Used especially by Elf physical skills and some melee skills.
        /// </summary>
        WeaponAttack,

        /// <summary>
        /// Multi-Shot selects bow/crossbow and ground/flying animation.
        /// </summary>
        ElfMultiShot,

        /// <summary>
        /// The original Main doesn't explicitly change the player action
        /// for this skill.
        /// </summary>
        KeepCurrent
    }

    public sealed class Season6SkillRoute
    {
        public Season6SkillRoute(
            ushort skillId,
            string name,
            SkillType skillType,
            Season6SkillAnimationMode animationMode,
            PlayerAction? explicitAction = null)
        {
            SkillId = skillId;
            Name = name;
            SkillType = skillType;
            AnimationMode = animationMode;
            ExplicitAction = explicitAction;
        }

        public ushort SkillId { get; }

        public string Name { get; }

        public SkillType SkillType { get; }

        public Season6SkillAnimationMode AnimationMode { get; }

        public PlayerAction? ExplicitAction { get; }
    }

    /// <summary>
    /// Central Season 6 routing table.
    ///
    /// IDs match OpenMU Season 6 SkillNumber values.
    /// Animations are based on classic Main behavior and the current
    /// PlayerAction enum used by this client.
    ///
    /// Master Level is intentionally excluded from this table.
    /// </summary>
    public static class Season6SkillActionDatabase
    {
        private static readonly Dictionary<ushort, Season6SkillRoute>
            Routes = BuildRoutes();

        public static IReadOnlyDictionary<ushort, Season6SkillRoute>
            Definitions => Routes;

        public static bool TryGet(
            ushort skillId,
            out Season6SkillRoute route)
        {
            return Routes.TryGetValue(
                skillId,
                out route);
        }

        public static Season6SkillRoute Get(
            ushort skillId)
        {
            Routes.TryGetValue(
                skillId,
                out var route);

            return route;
        }

        private static Dictionary<ushort, Season6SkillRoute>
            BuildRoutes()
        {
            var map =
                new Dictionary<ushort, Season6SkillRoute>();

            void Add(
                ushort id,
                string name,
                SkillType type,
                Season6SkillAnimationMode mode =
                    Season6SkillAnimationMode.DefaultMagic,
                PlayerAction? action = null)
            {
                if (!map.TryAdd(
                        id,
                        new Season6SkillRoute(
                            id,
                            name,
                            type,
                            mode,
                            action)))
                {
                    throw new InvalidOperationException(
                        $"Duplicate Season 6 skill route: {id} ({name}).");
                }
            }

            // =========================================================
            // DARK WIZARD / SOUL MASTER
            // =========================================================

            Add(1, "Poison",
                SkillType.Target);

            Add(2, "Meteorite",
                SkillType.Target);

            Add(3, "Lightning",
                SkillType.Target);

            Add(4, "Fire Ball",
                SkillType.Target);

            Add(5, "Flame",
                SkillType.Area);

            Add(
                6,
                "Teleport",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillTeleport);

            Add(7, "Ice",
                SkillType.Target);

            Add(8, "Twister",
                SkillType.Area);

            Add(9, "Evil Spirit",
                SkillType.Area);

            Add(
                10,
                "Hellfire",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillHell);

            Add(11, "Power Wave",
                SkillType.Target);

            Add(12, "Aqua Beam",
                SkillType.Area);

            Add(
                13,
                "Cometfall",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillHell);

            Add(
                14,
                "Inferno",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillInferno);

            // Teleport Ally requires a player/party target.
            Add(
                15,
                "Teleport Ally",
                SkillType.Target);

            // Simplified as Self for our current controller.
            // The original supports party targets too.
            Add(
                16,
                "Soul Barrier",
                SkillType.Friendly);

            Add(17, "Energy Ball",
                SkillType.Target);

            Add(38, "Decay",
                SkillType.Area);

            Add(39, "Ice Storm",
                SkillType.Area);

            Add(
                40,
                "Nova",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillHellStart);

            Add(
                58,
                "Nova Start",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillHellBegin);

            // Expansion of Wizardry / Wizardry Enhance.
            Add(
                233,
                "Expansion of Wizardry",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSwellOfMp);

            // =========================================================
            // DARK KNIGHT / BLADE KNIGHT
            // =========================================================

            Add(
                18,
                "Defense",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerDefense1);

            //
            // Classic Main:
            //
            // PLAYER_ATTACK_SKILL_SWORD1
            //     + skillId
            //     - AT_SKILL_SWORD1
            //
            // This gives the five consecutive sword animations.
            //

            Add(
                19,
                "Falling Slash",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillSword1);

            Add(
                20,
                "Lunge",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillSword2);

            Add(
                21,
                "Uppercut",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillSword3);

            Add(
                22,
                "Cyclone",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillSword4);

            Add(
                23,
                "Slash",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillSword5);

            Add(
                41,
                "Twisting Slash",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillWheel);

            Add(
                42,
                "Rageful Blow",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillFuryStrike);

            Add(
                43,
                "Death Stab",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackDeathstab);

            Add(
                44,
                "Crescent Moon Slash",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                47,
                "Impale",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                48,
                "Swell Life",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillVitality);

            Add(
                49,
                "Fire Breath",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                232,
                "Strike of Destruction",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillBlowOfDestruction);

            // =========================================================
            // ELF
            // =========================================================

            Add(
                24,
                "Triple Shot",
                SkillType.Area,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                26,
                "Heal",
                SkillType.Friendly,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            //
            // OpenMU supports player targets for these.
            //
            // For the moment we intentionally treat them as Self because
            // the current controller's friendly-target routing is not
            // separated from hostile PvP targeting yet.
            //
            // This is the behavior we just verified working.
            //

            Add(
                27,
                "Greater Defense",
                SkillType.Friendly,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                28,
                "Greater Damage",
                SkillType.Friendly,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                30,
                "Summon Goblin",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                31,
                "Summon Stone Golem",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                32,
                "Summon Assassin",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                33,
                "Summon Elite Yeti",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                34,
                "Summon Dark Knight",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                35,
                "Summon Bali",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                36,
                "Summon Soldier",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillElf1);

            Add(
                46,
                "Starfall",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                51,
                "Ice Arrow",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                52,
                "Penetration",
                SkillType.Area,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                77,
                "Infinity Arrow",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerRush1);

            Add(
                234,
                "Recovery",
                SkillType.Friendly,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerRecoverSkill);

            Add(
                235,
                "Multi-Shot",
                SkillType.Area,
                Season6SkillAnimationMode.ElfMultiShot);

            // =========================================================
            // MAGIC GLADIATOR
            // =========================================================

            //
            // Fire Slash and Power Slash both use the dedicated
            // two-hand-sword-two action in this Player.bmd.
            //
            Add(
                55,
                "Fire Slash",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackTwoHandSwordTwo);

            Add(
                56,
                "Power Slash",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackTwoHandSwordTwo);

            Add(
                57,
                "Spiral Slash",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackSkillWheel);

            Add(
                73,
                "Mana Rays",
                SkillType.Target);

            Add(
                236,
                "Flame Strike",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillFlamestrike);

            Add(
                237,
                "Gigantic Storm",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillGiganticstorm);

            Add(
                238,
                "Chaotic Diseier",
                SkillType.Area);

            // =========================================================
            // DARK LORD
            // =========================================================

            Add(
                60,
                "Force",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                61,
                "Fire Burst",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackStrike);

            Add(
                62,
                "Earthshake",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackDarkhorse);

            Add(
                63,
                "Summon",
                SkillType.Self);

            //
            // Classic Main sends this buff without selecting
            // PLAYER_ATTACK_DEATHSTAB. The old table's 71 was wrong.
            //
            Add(
                64,
                "Increase Critical Damage",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            //
            // Classic AT_SKILL_THUNDER_STRIKE:
            // SetAction(o, PLAYER_SKILL_FLASH);
            //
            Add(
                65,
                "Electric Spike",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillFlash);

            Add(
                66,
                "Force Wave",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackStrike);

            Add(
                74,
                "Fire Blast",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackStrike);

            Add(
                78,
                "Fire Scream",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackStrike);

            // =========================================================
            // COMMON / EVENT S6 ACTIVE SKILLS
            // =========================================================

            Add(
                67,
                "Stun",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerAttackStun);

            Add(
                68,
                "Cancel Stun",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                69,
                "Swell Mana",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                70,
                "Invisibility",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                71,
                "Cancel Invisibility",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                72,
                "Abolish Magic",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                76,
                "Plasma Storm",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerFenrirSkill);

            // =========================================================
            // SUMMONER
            // =========================================================

            Add(
                214,
                "Drain Life",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillDrainLife);

            Add(
                215,
                "Chain Lightning",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillChainLightning);

            Add(
                217,
                "Damage Reflection",
                SkillType.Friendly);

            Add(
                218,
                "Berserker",
                SkillType.Self,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSleep);

            Add(
                219,
                "Sleep",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSleep);

            //
            // Original Main sends Weakness / Innovation as an area
            // magic request and uses the Sleep family animation.
            //
            Add(
                221,
                "Weakness",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSleep);

            Add(
                222,
                "Innovation",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSleep);

            Add(
                223,
                "Explosion",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSummon);

            Add(
                224,
                "Requiem",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSummon);

            Add(
                225,
                "Pollution",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillSummon);

            Add(
                230,
                "Lightning Shock",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillLightningShock);

            // =========================================================
            // RAGE FIGHTER
            // =========================================================

            Add(
                260,
                "Killing Blow",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillThrust);

            Add(
                261,
                "Beast Uppercut",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillStamp);

            Add(
                262,
                "Chain Drive",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillGiantswing);

            Add(
                263,
                "Dark Side",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillDarksideReady);

            Add(
                264,
                "Dragon Roar",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillDragonkick);

            Add(
                265,
                "Dragon Slasher",
                SkillType.Target,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillDragonlore);

            //
            // The original MonkSystem handles the effects for these
            // separately and doesn't route them through the standard
            // Rage attack animation selector.
            //
            Add(
                266,
                "Ignore Defense",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                267,
                "Increase Health",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                268,
                "Increase Block",
                SkillType.Self,
                Season6SkillAnimationMode.KeepCurrent);

            Add(
                269,
                "Charge",
                SkillType.Target,
                Season6SkillAnimationMode.WeaponAttack);

            Add(
                270,
                "Phoenix Shot",
                SkillType.Area,
                Season6SkillAnimationMode.Explicit,
                PlayerAction.PlayerSkillPhoenixShot);

            return map;
        }
    }
}