using System;
using System.Collections.Generic;
using Client.Data.BMD.Tooltip;
using Client.Main.Controls.UI.Game;
using Client.Main.Controls.UI.Game.Common;
using Client.Main.Controls.UI.Game.Inventory;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Items.Tooltips
{
    /// <summary>
    /// Builds item tooltip lines following the original MU itemtooltip.bmd
    /// and itemtooltiptext.bmd definitions.
    ///
    /// First implementation:
    /// - Core equipment groups 0..11.
    /// - Original BMD controls which base lines are displayed and their order.
    /// - Excellent/Skill/Luck/normal-option text still uses the old compatibility
    ///   logic until their original BMD tables are implemented.
    ///
    /// Wings, Ancient and miscellaneous items currently fall back to
    /// ItemUiHelper.BuildTooltipLines.
    /// </summary>
    public static class ClassicItemTooltipBuilder
    {   
        private static void AppendExcellentWingOptions(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            byte flags =
                (byte)(
                    item.Details.ExcellentFlags &
                    0x3F);

            if (flags == 0)
            {
                return;
            }

            var def =
                item.Definition;

            if (def.Group != 12)
            {
                return;
            }

            // KindB comes directly from item.bmd.
            //
            // For example, Season 3 wings use the wing category
            // stored in KindB rather than a hardcoded item ID.
            byte category =
                def.KindB;

            // MU checks the wing flags from low bit to high bit.
            //
            // bit 0 -> option 5
            // bit 1 -> option 4
            // bit 2 -> option 3
            // bit 3 -> option 2
            // bit 4 -> option 1
            // bit 5 -> option 0
            for (int bit = 0;
                bit <= 5;
                bit++)
            {
                byte mask =
                    (byte)(1 << bit);

                if ((flags & mask) == 0)
                {
                    continue;
                }

                byte optionNumber =
                    (byte)(5 - bit);

                var option =
                    ItemTooltipDataRepository
                        .GetExcellentWingOption(
                            category,
                            optionNumber);

                if (option == null)
                {
                    continue;
                }

                string text =
                    BuildExcellentWingText(
                        option);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                lines.Add(
                    (
                        text,
                        ResolveMuColor(1)
                    ));
            }
        }
        private static string BuildExcellentWingText(
            ExcellentOptionBMD option)
        {
            // ---------------------------------------------------------
            // S3 WINGS
            //
            // KindB = 25 in the Data used by this client.
            //
            // bit 0 -> Ignore Defense
            // bit 1 -> Return Damage
            // bit 2 -> Full Life Recovery
            // bit 3 -> Full Mana Recovery
            // ---------------------------------------------------------

            if (option.Category == 25)
            {
                return option.Number switch
                {
                    5 =>
                        $"Ignore opponent's defensive power " +
                        $"{option.Value}%",

                    4 =>
                        $"Return the enemy's attack power " +
                        $"in {option.Value}%",

                    3 =>
                        $"Complete recovery of life " +
                        $"in {option.Value}% rate",

                    2 =>
                        $"Complete recover of Mana " +
                        $"in {option.Value}% rate",

                    _ =>
                        string.Empty
                };
            }

            return string.Empty;
        }
        public static List<(string text, Color color)> Build(
            InventoryItem item)
        {
            if (item?.Definition == null)
            {
                return new List<(string, Color)>();
            }

            var def = item.Definition;
            var details = item.Details;

            // ---------------------------------------------------------
            // FIRST MIGRATION STAGE
            //
            // Keep wings, consumables, ancient items and newer item
            // categories on the existing implementation until their
            // specific BMD option systems are ported.
            // ---------------------------------------------------------



            if (def.Group < 0 ||
                def.Group > 12 ||
                details.IsAncient)
            {
                return ItemUiHelper.BuildTooltipLines(item);
            }
            if (!ItemTooltipDataRepository.IsLoaded)
            {
                return ItemUiHelper.BuildTooltipLines(item);
            }

            var tooltip =
                ItemTooltipDataRepository.GetItem(
                    (byte)def.Group,
                    (ushort)def.Id);

            if (tooltip == null)
            {
                return ItemUiHelper.BuildTooltipLines(item);
            }

            var result =
                new List<(string text, Color color)>();

            var values =
                CalculateValues(item);

            // ---------------------------------------------------------
            // ITEM NAME
            // ---------------------------------------------------------

            string name = def.Name ?? string.Empty;

            if (details.IsExcellent)
            {
                name = $"Excellent {name}";
            }

            if (details.Level > 0 &&
                tooltip.RenderLevel)
            {
                name += $" +{details.Level}";
            }

            result.Add(
                (
                    name,
                    ResolveMuColor(tooltip.NameColor)
                ));

            // ---------------------------------------------------------
            // BASE TOOLTIP LINES
            //
            // IMPORTANT:
            // The BMD decides WHICH properties are shown and in
            // which order.
            // ---------------------------------------------------------

            foreach (var line in tooltip.Lines)
            {
                if (line == null ||
                    !line.HasText)
                {
                    continue;
                }

                var textDefinition =
                    ItemTooltipDataRepository.GetText(
                        (ushort)line.TextId);

                if (textDefinition == null)
                {
                    continue;
                }

                if (TryBuildLine(
                    textDefinition.Type,
                    item,
                    values,
                    line.Color,
                    out string text,
                    out Color color))
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add((text, color));
                    }
                }
            }

            // ---------------------------------------------------------
            // EQUIPPABLE CLASSES
            // ---------------------------------------------------------

            AppendAllowedClasses(
                result,
                def);

            // ---------------------------------------------------------
            // GUARDIAN / LEVEL 380
            // ---------------------------------------------------------

            if (details.HasGuardian &&
                HasGuardianOptionsForGroup(def.Group))
            {
                AppendSectionSeparator(
                    result);

                AppendGuardianOptions(
                    result,
                    item);
            }

            // ---------------------------------------------------------
            // HARMONY
            // ---------------------------------------------------------

            if (details.HasHarmony &&
                details.HarmonyOption != 0)
            {
                AppendSectionSeparator(
                    result);

                AppendHarmonyOption(
                    result,
                    item);
            }

            // ---------------------------------------------------------
            // NORMAL / LUCK / SKILL / EXCELLENT
            // ---------------------------------------------------------

            bool hasBlueOptions =
                details.OptionLevel > 0 ||
                details.HasLuck ||
                details.HasSkill ||
                details.IsExcellent;

            if (hasBlueOptions)
            {
                AppendSectionSeparator(
                    result);

                AppendLegacyOptions(
                    result,
                    item);

                if (def.Group == 12)
                {
                    AppendExcellentWingOptions(
                        result,
                        item);
                }
                else
                {
                    AppendExcellentCommonOptions(
                        result,
                        item);
                }
            }

            // ---------------------------------------------------------
            // SOCKET OPTIONS
            // ---------------------------------------------------------

            if (details.HasSockets &&
                details.SocketCount > 0)
            {
                AppendSectionSeparator(
                    result);

                AppendSocketOptions(
                    result,
                    item);
            }

            // ---------------------------------------------------------
            // NPC REPAIR INFO
            // ---------------------------------------------------------

            AppendNpcRepairInformation(
                result,
                item,
                values.MaxDurability);

            return result;
        }

        private static bool TryBuildLine(
            short type,
            InventoryItem item,
            CalculatedValues values,
            byte originalColor,
            out string text,
            out Color color)
        {
            text = string.Empty;
            color = ResolveMuColor(originalColor);

            var def = item.Definition;
            var character =
                MuGame.Network?.GetCharacterState();

            switch (type)
            {
                // -----------------------------------------------------
                // Type 0
                // Physical damage min / max
                // -----------------------------------------------------
                case 0:
                {
                    if (values.DamageMin <= 0 &&
                        values.DamageMax <= 0)
                    {
                        return false;
                    }

                    string damageType =
                        def.TwoHanded
                            ? "Two-hand Damage"
                            : "One-hand Damage";

                    text =
                        $"{damageType} : " +
                        $"{values.DamageMin} ~ {values.DamageMax}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 1
                // Simple durability
                // -----------------------------------------------------
                case 1:
                {
                    text =
                        $"Durability : {item.Durability}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 2
                // Defense
                // -----------------------------------------------------
                case 2:
                {
                    if (values.Defense <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Defense : {values.Defense}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 3
                // Magic defense
                // -----------------------------------------------------
                case 3:
                {
                    if (values.MagicDefense <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Magic Defense : {values.MagicDefense}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 4
                // Current / maximum durability
                // -----------------------------------------------------
                case 4:
                {
                    if (values.MaxDurability <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Durability : " +
                        $"[{item.Durability}/{values.MaxDurability}]";

                    return true;
                }

                // -----------------------------------------------------
                // Type 5
                // Required level
                // -----------------------------------------------------
                case 5:
                {
                    if (values.RequiredLevel <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Level : " +
                        $"{values.RequiredLevel}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 6
                // Required Strength
                // -----------------------------------------------------
                case 6:
                {
                    if (values.RequiredStrength <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Strength : " +
                        $"{values.RequiredStrength}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 7
                // Required Dexterity
                // -----------------------------------------------------
                case 7:
                {
                    if (values.RequiredDexterity <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Agility : " +
                        $"{values.RequiredDexterity}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 8
                // Required Vitality
                // -----------------------------------------------------
                case 8:
                {
                    if (values.RequiredVitality <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Vitality : " +
                        $"{values.RequiredVitality}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 9
                // Required Energy
                // -----------------------------------------------------
                case 9:
                {
                    if (values.RequiredEnergy <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Energy : " +
                        $"{values.RequiredEnergy}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 10
                // Required Command / Leadership
                // -----------------------------------------------------
                case 10:
                {
                    if (values.RequiredCommand <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Required Command : " +
                        $"{values.RequiredCommand}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 11
                // Magic / Wizardry power
                // -----------------------------------------------------
                case 11:
                {
                    if (values.MagicPower <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Wizardry Damage : " +
                        $"{values.MagicPower}%";

                    return true;
                }

                // Type 12 = skill damage.
                //
                // We deliberately don't implement it yet because
                // it depends on SkillAttribute data and varies
                // depending on item type.
                case 12:
                    return false;

                // Type 13 currently represents conditional static
                // descriptions in the original client.
                case 13:
                    return false;

                // Item level + 1 style information.
                case 14:
                {
                    text =
                        $"Item Level : " +
                        $"{item.Details.Level + 1}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 15
                // Successful blocking / defense rate
                // -----------------------------------------------------
                case 15:
                {
                    if (values.DefenseRate <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Defense Rate : " +
                        $"{values.DefenseRate}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 101
                // Attack speed
                // -----------------------------------------------------
                case 101:
                {
                    if (def.AttackSpeed <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Attack Speed : " +
                        $"{def.AttackSpeed}";

                    return true;
                }

                // -----------------------------------------------------
                // Type 102
                // Walk speed
                // -----------------------------------------------------
                case 102:
                {
                    if (def.WalkSpeed <= 0)
                    {
                        return false;
                    }

                    text =
                        $"Movement Speed : " +
                        $"{def.WalkSpeed}";

                    return true;
                }

                // -----------------------------------------------------
                // 201..206:
                // Original MU only shows these when the player does
                // NOT satisfy the corresponding requirement.
                // -----------------------------------------------------

                case 201:
                {
                    if (character == null ||
                        values.RequiredLevel <= 0 ||
                        character.Level >= values.RequiredLevel)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredLevel -
                        character.Level;

                    text =
                        $"({missing} more Level required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                case 202:
                {
                    if (character == null ||
                        values.RequiredStrength <= 0 ||
                        character.TotalStrength >=
                        values.RequiredStrength)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredStrength -
                        character.TotalStrength;

                    text =
                        $"({missing} more Strength required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                case 203:
                {
                    if (character == null ||
                        values.RequiredDexterity <= 0 ||
                        character.TotalAgility >=
                        values.RequiredDexterity)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredDexterity -
                        character.TotalAgility;

                    text =
                        $"({missing} more Agility required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                case 204:
                {
                    if (character == null ||
                        values.RequiredVitality <= 0 ||
                        character.TotalVitality >=
                        values.RequiredVitality)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredVitality -
                        character.TotalVitality;

                    text =
                        $"({missing} more Vitality required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                case 205:
                {
                    if (character == null ||
                        values.RequiredEnergy <= 0 ||
                        character.TotalEnergy >=
                        values.RequiredEnergy)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredEnergy -
                        character.TotalEnergy;

                    text =
                        $"({missing} more Energy required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                case 206:
                {
                    if (character == null ||
                        values.RequiredCommand <= 0 ||
                        character.TotalLeadership >=
                        values.RequiredCommand)
                    {
                        return false;
                    }

                    int missing =
                        values.RequiredCommand -
                        character.TotalLeadership;

                    text =
                        $"({missing} more Command required)";

                    color = ResolveMuColor(2);
                    return true;
                }

                default:
                    return false;
            }
        }

        private static CalculatedValues CalculateValues(
            InventoryItem item)
        {
            var def = item.Definition;
            var details = item.Details;

            int level =
                Math.Clamp(details.Level, 0, 15);

            bool excellent =
                details.IsExcellent;

            int levelStatBonus =
                CalculateClassicLevelBonus(level);

            // ---------------------------------------------------------
            // DAMAGE
            // ---------------------------------------------------------

            int damageMin = def.DamageMin;
            int damageMax = def.DamageMax;

            if (damageMin > 0)
            {
                if (excellent &&
                    def.DropLevel > 0)
                {
                    damageMin +=
                        (def.DamageMin * 25 /
                         def.DropLevel) + 5;
                }

                damageMin +=
                    levelStatBonus;
            }

            if (damageMax > 0)
            {
                if (excellent &&
                    def.DropLevel > 0)
                {
                    // Original client intentionally uses DamageMin
                    // for the Excellent bonus calculation here.
                    damageMax +=
                        (def.DamageMin * 25 /
                         def.DropLevel) + 5;
                }

                damageMax +=
                    levelStatBonus;
            }

            // ---------------------------------------------------------
            // MAGIC POWER
            // ---------------------------------------------------------

            int magicPower =
                def.MagicPower;

            if (magicPower > 0)
            {
                if (excellent &&
                    def.DropLevel > 0)
                {
                    magicPower +=
                        (def.MagicPower * 25 /
                         def.DropLevel) + 5;
                }

                magicPower +=
                    levelStatBonus;

                magicPower /= 2;

                if (!IsScepter(def))
                {
                    magicPower +=
                        level * 2;
                }
            }

            // ---------------------------------------------------------
            // SUCCESSFUL BLOCKING / DEFENSE RATE
            // ---------------------------------------------------------

            int defenseRate =
                def.DefenseRate;

            if (defenseRate > 0)
            {
                if (excellent &&
                    def.DropLevel > 0)
                {
                    defenseRate +=
                        (def.DefenseRate * 25 /
                         def.DropLevel) + 5;
                }

                defenseRate +=
                    levelStatBonus;
            }

            // ---------------------------------------------------------
            // DEFENSE
            // ---------------------------------------------------------

            int defense =
                def.Defense;

            if (defense > 0)
            {
                bool shield =
                    def.Group == 6;

                if (shield)
                {
                    defense += level;
                }
                else
                {
                    if (excellent &&
                        def.DropLevel > 0)
                    {
                        defense +=
                            def.Defense * 12 /
                            def.DropLevel +
                            def.DropLevel / 5 +
                            4;
                    }

                    defense +=
                        levelStatBonus;
                }
            }

            // ---------------------------------------------------------
            // MAGIC DEFENSE
            // ---------------------------------------------------------

            int magicDefense =
                def.MagicResistance;

            if (magicDefense > 0)
            {
                magicDefense +=
                    levelStatBonus;
            }

            // ---------------------------------------------------------
            // REQUIREMENTS
            // ---------------------------------------------------------

            int requirementItemLevel =
                def.DropLevel;

            if (excellent)
            {
                requirementItemLevel += 25;
            }

            int requiredStrength =
                CalculatePhysicalRequirement(
                    def.RequiredStrength,
                    requirementItemLevel,
                    def.DropLevel,
                    level,
                    excellent);

            int requiredDexterity =
                CalculatePhysicalRequirement(
                    def.RequiredDexterity,
                    requirementItemLevel,
                    def.DropLevel,
                    level,
                    excellent);

            int requiredVitality =
                CalculatePhysicalRequirement(
                    def.RequiredVitality,
                    requirementItemLevel,
                    def.DropLevel,
                    level,
                    excellent);

            int requiredEnergy =
                CalculateEnergyRequirement(
                    def.RequiredEnergy,
                    requirementItemLevel,
                    def.DropLevel,
                    level,
                    excellent);

            int requiredCommand =
                CalculateCommandRequirement(
                    def.RequiredCommand,
                    requirementItemLevel,
                    level);

            int requiredLevel =
                def.RequiredLevel;

            // The +20 Excellent level requirement applies to
            // regular equipment, not wings.
            if (excellent &&
                requiredLevel > 0 &&
                def.Group >= 0 &&
                def.Group <= 11)
            {
                requiredLevel += 20;
            }

            int maxDurability =
                ItemUiHelper.CalculateMaxDurability(
                    def,
                    details,
                    def.Group == 5);

            return new CalculatedValues
            {
                DamageMin = damageMin,
                DamageMax = damageMax,

                Defense = defense,
                DefenseRate = defenseRate,
                MagicDefense = magicDefense,
                MagicPower = magicPower,

                RequiredLevel = requiredLevel,
                RequiredStrength = requiredStrength,
                RequiredDexterity = requiredDexterity,
                RequiredVitality = requiredVitality,
                RequiredEnergy = requiredEnergy,
                RequiredCommand = requiredCommand,

                MaxDurability = maxDurability
            };
        }

        /// <summary>
        /// Classic +0..+15 stat increase.
        ///
        /// +0..+9:
        ///     level * 3
        ///
        /// +10:
        ///     27 + 4
        ///
        /// +11:
        ///     27 + 4 + 5
        ///
        /// ...
        ///
        /// +15:
        ///     27 + 4 + 5 + 6 + 7 + 8 + 9
        /// </summary>
        private static int CalculateClassicLevelBonus(
            int level)
        {
            if (level <= 0)
            {
                return 0;
            }

            int bonus =
                Math.Min(level, 9) * 3;

            for (int current = 10;
                 current <= level;
                 current++)
            {
                bonus +=
                    current - 6;
            }

            return bonus;
        }

        private static int CalculatePhysicalRequirement(
            int baseRequirement,
            int calculatedItemLevel,
            int originalDropLevel,
            int level,
            bool excellent)
        {
            if (baseRequirement <= 0)
            {
                return 0;
            }

            // Modern high-level items use another formula in the
            // original client.
            if (originalDropLevel >= 220)
            {
                if (excellent)
                {
                    return (int)(
                        baseRequirement +
                        baseRequirement * 0.75f +
                        baseRequirement * 0.02f * level);
                }

                return (int)(
                    baseRequirement +
                    baseRequirement * 0.02f * level);
            }

            return
                20 +
                baseRequirement *
                (calculatedItemLevel + level * 3) *
                3 / 100;
        }

        private static int CalculateEnergyRequirement(
            int baseRequirement,
            int calculatedItemLevel,
            int originalDropLevel,
            int level,
            bool excellent)
        {
            if (baseRequirement <= 0)
            {
                return 0;
            }

            if (originalDropLevel >= 220)
            {
                if (excellent)
                {
                    return (int)(
                        baseRequirement * 2 +
                        baseRequirement * 0.035f * level);
                }

                return (int)(
                    baseRequirement +
                    baseRequirement * 0.035f * level);
            }

            // Generic equipment formula.
            //
            // Books and skill scrolls have additional special cases,
            // but groups > 11 currently use the legacy tooltip,
            // so they don't reach this code yet.
            return
                20 +
                baseRequirement *
                (calculatedItemLevel + level * 3) *
                4 / 100;
        }

        private static int CalculateCommandRequirement(
            int baseRequirement,
            int calculatedItemLevel,
            int level)
        {
            if (baseRequirement <= 0)
            {
                return 0;
            }

            return
                20 +
                baseRequirement *
                (calculatedItemLevel + level * 3) *
                3 / 100;
        }

        private static bool IsScepter(
            ItemDefinition def)
        {
            if (def.Group != 2)
            {
                return false;
            }

            return
                (def.Id >= 8 &&
                 def.Id <= 15) ||
                def.Id == 17 ||
                def.Id == 18;
        }
        private static void AppendSectionSeparator(
            List<(string text, Color color)> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            // Keep exactly one visual separator between option groups.
            //
            // A single space is intentional. Some tooltip rendering paths
            // ignore an empty string completely.
            if (lines[^1].text == " ")
            {
                return;
            }

            lines.Add(
                (
                    " ",
                    Color.Transparent
                ));
        }

        private static bool HasGuardianOptionsForGroup(
            int group)
        {
            return
                (group >= 0 && group <= 5) ||
                group == 7 ||
                group == 8 ||
                group == 9 ||
                group == 10 ||
                group == 11;
        }

        private static void AppendGuardianOptions(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            if (!item.Details.HasGuardian)
            {
                return;
            }

            int group =
                item.Definition.Group;

            Color guardianColor =
                ResolveMuColor(11);

            // OpenMU Season 6 Guardian / level 380 configuration:
            //
            // Weapons:
            //   PvP Attack Success Rate +10
            //   PvP Attack Damage +200
            //
            // Helm:
            //   PvP Defense Success Rate +10
            //   SD Recovery Rate +20
            //
            // Armor:
            //   PvP Defense Success Rate +10
            //   SD Auto Recovery
            //
            // Pants:
            //   PvP Defense Success Rate +10
            //   PvP Defense +100
            //
            // Gloves:
            //   PvP Defense Success Rate +10
            //   Max HP +200
            //
            // Boots:
            //   PvP Defense Success Rate +10
            //   Max SD +200
            //
            // Group 6 (shield) has no Guardian option definition
            // in the current OpenMU Season 6 configuration.

            if (group >= 0 &&
                group <= 5)
            {
                lines.Add(
                    (
                        "PvP Attack Success Rate +10",
                        guardianColor
                    ));

                lines.Add(
                    (
                        "PvP Attack Damage +200",
                        guardianColor
                    ));

                return;
            }

            switch (group)
            {
                // Helm
                case 7:
                    lines.Add(
                        (
                            "PvP Defense Success Rate +10",
                            guardianColor
                        ));

                    lines.Add(
                        (
                            "SD Recovery Rate +20",
                            guardianColor
                        ));
                    break;

                // Armor
                case 8:
                    lines.Add(
                        (
                            "PvP Defense Success Rate +10",
                            guardianColor
                        ));

                    lines.Add(
                        (
                            "SD Auto Recovery",
                            guardianColor
                        ));
                    break;

                // Pants
                case 9:
                    lines.Add(
                        (
                            "PvP Defense Success Rate +10",
                            guardianColor
                        ));

                    lines.Add(
                        (
                            "PvP Defense +100",
                            guardianColor
                        ));
                    break;

                // Gloves
                case 10:
                    lines.Add(
                        (
                            "PvP Defense Success Rate +10",
                            guardianColor
                        ));

                    lines.Add(
                        (
                            "Max HP +200",
                            guardianColor
                        ));
                    break;

                // Boots
                case 11:
                    lines.Add(
                        (
                            "PvP Defense Success Rate +10",
                            guardianColor
                        ));

                    lines.Add(
                        (
                            "Max SD +200",
                            guardianColor
                        ));
                    break;
            }
        }

        private static void AppendAllowedClasses(
            List<(string text, Color color)> lines,
            ItemDefinition def)
        {
            if (def.AllowedClasses == null ||
                def.AllowedClasses.Count == 0)
            {
                return;
            }

            foreach (string className in
                     def.AllowedClasses)
            {
                lines.Add(
                    (
                        $"Can be equipped by {className}",
                        Color.LightGray
                    ));
            }

            var character =
                MuGame.Network?.GetCharacterState();

            if (character == null)
            {
                return;
            }

            string currentClass =
                CharacterClassDatabase.GetClassName(
                    character.Class);

            string baseClass =
                CharacterClassDatabase.GetBaseClassName(
                    character.Class);

            if (!def.AllowedClasses.Contains(
                    baseClass))
            {
                lines.Add(
                    (
                        $"This item cannot be equipped by {currentClass}",
                        Color.Red
                    ));
            }
        }

        private static void AppendLegacyOptions(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            var details =
                item.Details;

            if (details.OptionLevel > 0)
            {
                lines.Add(
                    (
                        $"Additional Option : " +
                        $"+{details.OptionLevel * 4}",
                        ResolveMuColor(1)
                    ));
            }

            if (details.HasLuck)
            {
                lines.Add(
                    (
                        "Luck (success rate of Jewel of Soul +25%)",
                        ResolveMuColor(1)
                    ));

                lines.Add(
                    (
                        "Luck (critical damage rate +5%)",
                        ResolveMuColor(1)
                    ));
            }

            if (details.HasSkill)
            {
                lines.Add(
                    (
                        "+Skill (Right mouse click - skill)",
                        Color.CornflowerBlue
                    ));
            }
        }
        private static void AppendExcellentCommonOptions(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            byte flags =
                (byte)(
                    item.Details.ExcellentFlags &
                    0x3F);

            if (flags == 0)
            {
                return;
            }

            var def =
                item.Definition;

            byte category;

            // Original MU CalcExcellentOptions:
            //
            // Category 1:
            // Weapons / offensive items.
            //
            // Category 2:
            // Shields / armor / defensive items.
            if (def.Group >= 0 &&
                def.Group <= 5)
            {
                category = 1;
            }
            else if (def.Group >= 6 &&
                    def.Group <= 11)
            {
                category = 2;
            }
            else
            {
                // Wings/rings/pendants are handled later.
                return;
            }

            // Original MU reads Excellent bits from bit 5 down to bit 0.
            //
            // bit 5 -> option 0
            // bit 4 -> option 1
            // ...
            // bit 0 -> option 5
            for (int bit = 5;
                bit >= 0;
                bit--)
            {
                byte mask =
                    (byte)(1 << bit);

                if ((flags & mask) == 0)
                {
                    continue;
                }

                byte optionNumber =
                    (byte)(5 - bit);

                var option =
                    ItemTooltipDataRepository
                        .GetExcellentCommonOption(
                            category,
                            optionNumber);

                if (option == null)
                {
                    continue;
                }

                string text =
                    BuildExcellentCommonText(
                        def,
                        option);

                if (string.IsNullOrWhiteSpace(
                        text))
                {
                    continue;
                }

                lines.Add(
                (
                    text,
                    ResolveMuColor(1)
                ));
            }
        }

        private static string BuildExcellentCommonText(
                ItemDefinition def,
                ExcellentOptionBMD option)
        {
            // ---------------------------------------------------------
            // OFFENSIVE EXCELLENT OPTIONS
            // Category 1
            // ---------------------------------------------------------

            if (option.Category == 1)
            {
                bool magicWeapon =
                    def.Group == 5;

                string powerName =
                    magicWeapon
                        ? "Wizardry Damage"
                        : "Damage";

                return option.Number switch
                {
                    // Excellent Damage Rate +10%
                    0 =>
                        $"Excellent Damage Rate " +
                        $"+{option.Value}%",

                    // Damage/Wizardry Damage +1 per 20 levels
                    1 =>
                        $"{powerName} " +
                        $"+{option.Value} " +
                        $"per 20 levels",

                    // Damage/Wizardry Damage +2%
                    2 =>
                        $"{powerName} " +
                        $"+{option.Value}%",

                    // Attack/Wizardry Speed +7
                    3 =>
                        magicWeapon
                            ? $"Wizardry Speed +{option.Value}"
                            : $"Attack Speed +{option.Value}",

                    // Life / 8 after killing a monster
                    4 =>
                        $"Life after monster " +
                        $"+Life/{option.Value}",

                    // Mana / 8 after killing a monster
                    5 =>
                        $"Mana after monster " +
                        $"+Mana/{option.Value}",

                    _ =>
                        string.Empty
                };
            }

            // ---------------------------------------------------------
            // DEFENSIVE EXCELLENT OPTIONS
            // Category 2
            // ---------------------------------------------------------

            if (option.Category == 2)
            {
                return option.Number switch
                {
                    // Max Life +4%
                    0 =>
                        $"Max Life +{option.Value}%",

                    // Max Mana +4%
                    1 =>
                        $"Max Mana +{option.Value}%",

                    // Damage Decrease +4%
                    2 =>
                        $"Damage Decrease +{option.Value}%",

                    // Reflect Damage +5%
                    3 =>
                        $"Reflect Damage +{option.Value}%",

                    // Defense Success Rate +10%
                    4 =>
                        $"Defense Success Rate " +
                        $"+{option.Value}%",

                    // Zen after hunt +30%
                    5 =>
                        $"Zen after hunt +{option.Value}%",

                    _ =>
                        string.Empty
                };
            }

            return string.Empty;
        }
        private static string GetHarmonyOptionDisplayName(
            int table,
            int optionNumber,
            string bmdName)
        {
            return table switch
            {
                // -----------------------------------------------------
                // PHYSICAL WEAPONS
                // -----------------------------------------------------
                0 => optionNumber switch
                {
                    1 => "Minimum Attack Power Increase",
                    2 => "Maximum Attack Power Increase",
                    3 => "Required Strength Decrease",
                    4 => "Required Agility Decrease",
                    5 => "Attack Power Increase",
                    6 => "Critical Damage Increase",
                    7 => "Skill Attack Power Increase",
                    8 => "PvP Attack Success Rate Increase",
                    9 => "SD Decrease Rate Increase",
                    10 => "SD Ignore Rate Increase",
                    _ => bmdName
                },

                // -----------------------------------------------------
                // STAFF / WIZARDRY
                // -----------------------------------------------------
                1 => optionNumber switch
                {
                    1 => "Wizardry Damage Increase",
                    2 => "Required Strength Decrease",
                    3 => "Required Agility Decrease",
                    4 => "Skill Attack Power Increase",
                    5 => "Critical Damage Increase",
                    6 => "SD Decrease Rate Increase",
                    7 => "PvP Attack Success Rate Increase",
                    8 => "SD Ignore Rate Increase",
                    _ => bmdName
                },

                // -----------------------------------------------------
                // DEFENSIVE EQUIPMENT
                // -----------------------------------------------------
                2 => optionNumber switch
                {
                    1 => "Defense Increase",
                    2 => "Maximum AG Increase",
                    3 => "Maximum HP Increase",
                    4 => "HP Auto Recovery Increase",
                    5 => "MP Auto Recovery Increase",
                    6 => "PvP Defense Success Rate Increase",
                    7 => "Damage Decrease",
                    8 => "SD Ratio Increase",
                    _ => bmdName
                },

                _ => bmdName
            };
        }
        private static void AppendHarmonyOption(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            var details =
                item.Details;

            if (!details.HasHarmony ||
                details.HarmonyOption == 0)
            {
                return;
            }

            var def =
                item.Definition;

            // The original client doesn't render Harmony options
            // on socket items.
            if (details.HasSockets &&
                details.SocketCount > 0)
            {
                return;
            }

            // ---------------------------------------------------------
            // OPENMU / ORIGINAL MU HARMONY BYTE
            //
            // High nibble = Harmony option number
            // Low nibble  = Harmony option level
            // ---------------------------------------------------------

            int optionNumber =
                (details.HarmonyOption >> 4) &
                0x0F;

            int optionLevel =
                details.HarmonyOption &
                0x0F;

            if (optionNumber <= 0)
            {
                return;
            }

            // ---------------------------------------------------------
            // ORIGINAL MU HARMONY TABLE
            //
            // 0 = Physical weapons
            // 1 = Staff / Wizardry
            // 2 = Defensive equipment
            // ---------------------------------------------------------

            int table;

            if (def.Group >= 0 &&
                def.Group <= 4)
            {
                table = 0;
            }
            else if (def.Group == 5)
            {
                table = 1;
            }
            else if (def.Group >= 6 &&
                    def.Group <= 11)
            {
                table = 2;
            }
            else
            {
                return;
            }

            var harmony =
                ItemTooltipDataRepository
                    .GetHarmonyOption(
                        table,
                        optionNumber);

            if (harmony == null)
            {
                return;
            }

            if (optionLevel < 0 ||
                optionLevel >= harmony.Values.Length)
            {
                return;
            }

            int value =
                harmony.Values[optionLevel];

            if (string.IsNullOrWhiteSpace(
                    harmony.Name))
            {
                return;
            }

            // Original MU special case:
            //
            // Defense Harmony option 7
            // (Damage Decrease) is rendered as a percentage.
            string displayName =
                GetHarmonyOptionDisplayName(
                    table,
                    optionNumber,
                    harmony.Name);

            string text;

            if (table == 2 &&
                optionNumber == 7)
            {
                text =
                    $"{displayName} +{value}%";
            }
            else
            {
                text =
                    $"{displayName} +{value}";
            }

            // Original client:
            //
            // Yellow when the item level is high enough
            // for the Harmony level.
            // Gray otherwise.
            Color color =
                details.Level >= optionLevel
                    ? ResolveMuColor(3)
                    : ResolveMuColor(10);

            lines.Add(
                (
                    text,
                    color
                ));
        }
        private static void AppendSocketOptions(
            List<(string text, Color color)> lines,
            InventoryItem item)
        {
            var details =
                item.Details;

            if (!details.HasSockets ||
                details.SocketCount == 0 ||
                details.SocketOptions == null ||
                details.SocketOptions.Length == 0)
            {
                return;
            }

            // Classic MU socket section header.
            lines.Add(
                (
                    "Socket Option",
                    new Color(180, 120, 255)
                ));

            int count =
                Math.Min(
                    details.SocketCount,
                    (byte)details.SocketOptions.Length);

            for (int i = 0;
                i < count;
                i++)
            {
                byte socketByte =
                    details.SocketOptions[i];

                // ---------------------------------------------------------
                // EMPTY SOCKET
                // ---------------------------------------------------------

                if (socketByte == 0xFE)
                {
                    lines.Add(
                        (
                            "Empty Socket",
                            Color.Gray
                        ));

                    continue;
                }

                // 0xFF means this slot does not exist.
                if (socketByte == 0xFF)
                {
                    continue;
                }

                // ---------------------------------------------------------
                // OPENMU / ORIGINAL MU SOCKET ENCODING
                //
                // byte =
                //     ((sphereLevel - 1) * 50)
                //     + optionIndex
                // ---------------------------------------------------------

                int sphereLevel =
                    (socketByte / 50) + 1;

                int optionIndex =
                    socketByte % 50;

                var socket =
                    ItemTooltipDataRepository
                        .GetSocketOption(
                            0,
                            optionIndex);

                if (socket == null)
                {
                    continue;
                }

                if (sphereLevel < 1 ||
                    sphereLevel > socket.Values.Length)
                {
                    continue;
                }

                int value =
                    socket.Values[
                        sphereLevel - 1];

                string text =
                    BuildSocketOptionText(
                        socket,
                        sphereLevel,
                        value);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                lines.Add(
                    (
                        text,
                        new Color(180, 120, 255)
                    ));
            }
        }
        private static string BuildSocketOptionText(
            SocketItemBMD socket,
            int sphereLevel,
            int value)
        {
            string element =
                socket.ElementType switch
                {
                    1 => "Fire",
                    2 => "Water",
                    3 => "Ice",
                    4 => "Wind",
                    5 => "Lightning",
                    6 => "Earth",
                    _ => "Socket"
                };

            string optionText =
                socket.Id switch
                {
                    // -----------------------------------------------------
                    // FIRE
                    // -----------------------------------------------------

                    0 =>
                        $"Increase Damage/Wizardry per 20 Levels +{value}",

                    1 =>
                        $"Increase Attack/Wizardry Speed +{value}",

                    2 =>
                        $"Increase Maximum Damage/Wizardry +{value}",

                    3 =>
                        $"Increase Minimum Damage/Wizardry +{value}",

                    4 =>
                        $"Increase Damage/Wizardry +{value}",

                    5 =>
                        $"Decrease AG Consumption +{value}%",

                    // -----------------------------------------------------
                    // WATER
                    // -----------------------------------------------------

                    10 =>
                        $"Increase Defense Success Rate +{value}%",

                    11 =>
                        $"Increase Defense +{value}",

                    12 =>
                        $"Increase Shield Defense +{value}%",

                    13 =>
                        $"Damage Decrease +{value}%",

                    14 =>
                        $"Reflect Damage +{value}%",

                    // -----------------------------------------------------
                    // ICE
                    // -----------------------------------------------------

                    16 =>
                        $"Monster Destruction for Life +{value}",

                    17 =>
                        $"Monster Destruction for Mana +{value}",

                    18 =>
                        $"Increase Skill Attack +{value}",

                    19 =>
                        $"Increase Attack Success Rate +{value}",

                    20 =>
                        $"Increase Item Durability +{value}%",

                    // -----------------------------------------------------
                    // WIND
                    // -----------------------------------------------------

                    21 =>
                        $"Increase Automatic Life Recovery +{value}",

                    22 =>
                        $"Increase Maximum Life +{value}",

                    23 =>
                        $"Increase Maximum Mana +{value}",

                    24 =>
                        $"Increase Automatic Mana Recovery +{value}",

                    25 =>
                        $"Increase Maximum AG +{value}",

                    26 =>
                        $"Increase AG Recovery +{value}",

                    // -----------------------------------------------------
                    // LIGHTNING
                    // -----------------------------------------------------

                    29 =>
                        $"Increase Excellent Damage +{value}",

                    30 =>
                        $"Increase Excellent Damage Rate +{value}%",

                    31 =>
                        $"Increase Critical Damage +{value}",

                    32 =>
                        $"Increase Critical Damage Rate +{value}%",

                    // -----------------------------------------------------
                    // EARTH
                    // -----------------------------------------------------

                    36 =>
                        $"Increase Health +{value}",

                    _ =>
                        $"Socket Option {socket.Id} +{value}"
                };

            return
                $"{element} " +
                $"(Sphere Lv{sphereLevel}) : " +
                optionText;
        }

        private static void AppendNpcRepairInformation(
            List<(string text, Color color)> lines,
            InventoryItem item,
            int maxDurability)
        {
            var npcShop =
                NpcShopControl.Instance;

            if (npcShop == null ||
                !npcShop.Visible ||
                !npcShop.IsRepairMode)
            {
                return;
            }

            if (ItemPriceCalculator.IsRepairable(item))
            {
                int repairCost =
                    ItemPriceCalculator.CalculateRepairPrice(
                        item,
                        npcDiscount: true);

                if (repairCost > 0 &&
                    item.Durability < maxDurability)
                {
                    lines.Add(
                        (
                            $"Repair Cost: {repairCost} Zen",
                            new Color(212, 175, 85)
                        ));
                }
            }
            else
            {
                lines.Add(
                    (
                        "Cannot be repaired",
                        new Color(255, 100, 100)
                    ));
            }
        }

        /// <summary>
        /// MU's classic tooltip color indexes.
        ///
        /// The first values were confirmed against the original
        /// ZzzInventory tooltip renderer.
        /// </summary>
        private static Color ResolveMuColor(
            byte color)
        {
            return color switch
            {
                0 => Color.White,

                1 => new Color(
                    127,
                    178,
                    255),

                2 => new Color(
                    255,
                    51,
                    25),

                3 => new Color(
                    255,
                    204,
                    25),

                4 => new Color(
                    25,
                    200,
                    28),

                5 => new Color(
                    160,
                    0,
                    0),

                6 => new Color(
                    255,
                    25,
                    255),

                7 => new Color(
                    0,
                    0,
                    160),

                8 => new Color(
                    160,
                    102,
                    0),

                9 => new Color(
                    0,
                    255,
                    0),

                10 => new Color(
                    102,
                    102,
                    102),

                11 => new Color(
                    204,
                    128,
                    204),

                12 => new Color(
                    179,
                    102,
                    255),

                13 => new Color(
                    230,
                    107,
                    10),

                _ => Color.White
            };
        }

        private sealed class CalculatedValues
        {
            public int DamageMin { get; init; }
            public int DamageMax { get; init; }

            public int Defense { get; init; }
            public int DefenseRate { get; init; }

            public int MagicDefense { get; init; }
            public int MagicPower { get; init; }

            public int RequiredLevel { get; init; }

            public int RequiredStrength { get; init; }
            public int RequiredDexterity { get; init; }
            public int RequiredVitality { get; init; }
            public int RequiredEnergy { get; init; }
            public int RequiredCommand { get; init; }

            public int MaxDurability { get; init; }
        }
    }
}