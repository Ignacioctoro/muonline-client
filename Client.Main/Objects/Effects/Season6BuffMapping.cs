using System.Collections.Generic;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Season 6 skill -> MagicEffect mappings.
    ///
    /// OpenMU sends MagicEffectStatus (0x07) using EffectId, while
    /// MagicEffectCancelled (0x1B) contains the SkillId which caused
    /// the effect. These values are NOT generally interchangeable.
    ///
    /// Values are based on the Season 6 initialization used by OpenMU.
    /// </summary>
    public static class Season6BuffMapping
    {
        // -------------------------------------------------------------
        // Classic visible buff/debuff EffectIds
        // -------------------------------------------------------------

        public const byte GreaterDamage = 1;
        public const byte GreaterDefense = 2;
        public const byte ElfSoldierBuff = 3;
        public const byte SoulBarrier = 4;
        public const byte CriticalDamageIncrease = 5;
        public const byte InfinityArrow = 6;
        public const byte GreaterFortitude = 8;

        public const byte Poisoned = 0x37;          // 55
        public const byte Iced = 0x38;              // 56
        public const byte Freeze = 0x39;            // 57
        public const byte DefenseReduction = 0x3A; // 58
        public const byte Stunned = 0x3D;           // 61

        public const byte Reflection = 0x47;        // 71
        public const byte Sleep = 0x48;             // 72
        public const byte Blind = 0x49;             // 73
        public const byte Requiem = 0x4A;           // 74
        public const byte Explosion = 0x4B;         // 75
        public const byte Weakness = 0x4C;          // 76
        public const byte Innovation = 0x4D;        // 77

        public const byte Berserker = 0x51;         // 81
        public const byte WizardryEnhance = 0x52;   // 82
        public const byte Cold = 0x56;              // 86

        // Rage Fighter
        public const byte IgnoreDefense = 129;
        public const byte IncreaseHealth = 130;
        public const byte IncreaseBlock = 131;
        public const byte DecreaseBlock = 132;

        // Master variants used by OpenMU.
        public const byte GreaterFortitudeProficiency = 135;
        public const byte WizardryEnhanceStrengthener = 138;
        public const byte WizardryEnhanceMastery = 139;
        public const byte CriticalDamageIncreaseMastery = 148;
        public const byte IncreaseBlockPowerUp = 153;
        public const byte IncreaseBlockMastery = 154;
        public const byte IncreaseHealthStrengthener = 155;

        /// <summary>
        /// SkillId -> EffectId.
        ///
        /// This fixes the previous assumption:
        ///
        ///     effectId = skillId & 0xFF
        ///
        /// which isn't valid for MU Season 6.
        /// </summary>
        private static readonly Dictionary<ushort, byte> SkillToEffect =
            new()
            {
                // -----------------------------------------------------
                // Common status effects
                // -----------------------------------------------------

                // Poison
                [1] = Poisoned,

                // Ice
                [7] = Iced,

                // Soul Barrier
                [16] = SoulBarrier,

                // Elf Greater Defense
                [27] = GreaterDefense,

                // Elf Greater Damage
                [28] = GreaterDamage,

                // Swell Life
                [48] = GreaterFortitude,

                // Ice Arrow
                [51] = Freeze,

                // Fire Slash
                [55] = DefenseReduction,

                // Dark Lord Critical Damage
                [64] = CriticalDamageIncrease,

                // Infinity Arrow
                [77] = InfinityArrow,

                // -----------------------------------------------------
                // Summoner
                // -----------------------------------------------------

                // Damage Reflection
                [217] = Reflection,

                // Berserker
                [218] = Berserker,

                // Sleep
                [219] = Sleep,

                // Weakness
                [221] = Weakness,

                // Innovation
                [222] = Innovation,

                // Book of Sahamutt
                [223] = Explosion,

                // Book of Neil
                [224] = Requiem,

                // -----------------------------------------------------
                // Later S6 skills
                // -----------------------------------------------------

                // Strike of Destruction
                [232] = Cold,

                // Expansion of Wizardry
                [233] = WizardryEnhance,

                // -----------------------------------------------------
                // Rage Fighter
                // -----------------------------------------------------

                // Killing Blow
                [260] = Weakness,

                // Beast Uppercut
                [261] = DefenseReduction,

                [266] = IgnoreDefense,
                [267] = IncreaseHealth,
                [268] = IncreaseBlock,

                // Phoenix Shot
                [270] = DecreaseBlock,

                // -----------------------------------------------------
                // Master skills
                // -----------------------------------------------------

                // Swell Life Proficiency
                [360] = GreaterFortitudeProficiency,

                // Wizardry Enhance Strengthener
                [380] = WizardryEnhanceStrengthener,

                // Wizardry Enhance Mastery
                [383] = WizardryEnhanceMastery,

                // Infinity Arrow Strengthener
                [441] = InfinityArrow,

                // Fire Burst Mastery
                [514] = Stunned,

                // Earthshake Mastery
                [516] = Stunned,

                // Critical Damage Increase Mastery
                [517] = CriticalDamageIncreaseMastery,

                // RF Increase Block Power Up
                [569] = IncreaseBlockPowerUp,

                // RF Increase Block Mastery
                [572] = IncreaseBlockMastery,

                // RF Increase Health Strengthener
                [573] = IncreaseHealthStrengthener
            };

        public static bool TryGetEffectIdFromSkillId(
            ushort skillId,
            out byte effectId)
        {
            return SkillToEffect.TryGetValue(
                skillId,
                out effectId);
        }

        /// <summary>
        /// Effects for which the first version of our classic
        /// persistent visual renderer is implemented.
        /// </summary>
        public static bool HasImplementedVisual(
            byte effectId)
        {
            return effectId is
                GreaterDamage or
                GreaterDefense or
                ElfSoldierBuff or
                SoulBarrier or
                GreaterFortitude or
                CriticalDamageIncrease or
                CriticalDamageIncreaseMastery or
                WizardryEnhance or
                WizardryEnhanceStrengthener or
                WizardryEnhanceMastery;
        }
    }
}