#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Client.Data.BMD;
using Microsoft.Extensions.Logging;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// Static database for skill definitions loaded from skill.bmd.
    ///
    /// Data such as mana, AG, range and delay comes from skill.bmd.
    ///
    /// Season 6 routing such as cast type and animation comes from
    /// Season6SkillActionDatabase instead of relying on later-season
    /// numeric animation mappings.
    /// </summary>
    public static class SkillDatabase
    {
        private static readonly ILogger? _logger =
            MuGame.AppLoggerFactory?
                .CreateLogger("SkillDatabase");

        private static Dictionary<int, SkillBMD>
            _skillDefinitions = [];

        public static async Task Initialize()
        {
            _skillDefinitions =
                await InitializeSkillData();

            AuditSeason6SkillData();
        }

        private static async Task<Dictionary<int, SkillBMD>>
            InitializeSkillData()
        {
            var skillPath =
                Path.Combine(
                    Constants.DataPath,
                    "Local",
                    "skill.bmd");

            var reader =
                new SkillBMDReader();

            var skills =
                await reader.Load(
                    skillPath);

            _logger?.LogInformation(
                "Loaded {Count} skills from skill.bmd",
                skills.Count);

            return skills;
        }

        public static SkillBMD? GetSkillDefinition(
            int skillId)
        {
            _skillDefinitions.TryGetValue(
                skillId,
                out var def);

            return def;
        }

        public static string GetSkillName(
            int skillId)
        {
            if (Season6SkillActionDatabase.TryGet(
                    (ushort)skillId,
                    out var route))
            {
                return route.Name;
            }

            return GetSkillDefinition(skillId)?.Name ??
                   $"Unknown Skill {skillId}";
        }

        /// <summary>
        /// Season 6 routing has priority.
        ///
        /// This intentionally bypasses incorrect later-season
        /// definitions from SkillDefinitions for known S6 skills.
        /// </summary>
        public static SkillType GetSkillType(
            int skillId)
        {
            if (Season6SkillActionDatabase.TryGet(
                    (ushort)skillId,
                    out var route))
            {
                return route.SkillType;
            }

            return SkillDefinitions.GetSkillType(
                skillId);
        }

        /// <summary>
        /// Returns only an explicitly fixed PlayerAction.
        ///
        /// Dynamic animations (weapon-dependent, Multi-Shot,
        /// generic magic, etc.) are resolved by PlayerObject.
        /// </summary>
        public static int GetSkillAnimation(
            int skillId)
        {
            if (Season6SkillActionDatabase.TryGet(
                    (ushort)skillId,
                    out var route))
            {
                if (route.AnimationMode ==
                        Season6SkillAnimationMode.Explicit &&
                    route.ExplicitAction.HasValue)
                {
                    return (int)
                        route.ExplicitAction.Value;
                }

                return -1;
            }

            // Compatibility only for skills which haven't been
            // classified by our Season 6 database.
            return SkillDefinitions.GetSkillAnimation(
                skillId);
        }

        public static Season6SkillAnimationMode
            GetSkillAnimationMode(
                int skillId)
        {
            if (Season6SkillActionDatabase.TryGet(
                    (ushort)skillId,
                    out var route))
            {
                return route.AnimationMode;
            }

            return Season6SkillAnimationMode.DefaultMagic;
        }

        public static bool IsAreaSkill(
            int skillId)
        {
            return GetSkillType(skillId) ==
                   SkillType.Area;
        }

        public static bool IsTargetSkill(
            int skillId)
        {
            return GetSkillType(skillId) ==
                   SkillType.Target;
        }

        public static bool IsSelfSkill(
            int skillId)
        {
            return GetSkillType(skillId) ==
                   SkillType.Self;
        }

        public static ushort GetSkillManaCost(
            int skillId)
        {
            return GetSkillDefinition(skillId)?
                .ManaCost ?? 0;
        }

        public static ushort GetSkillAGCost(
            int skillId)
        {
            return GetSkillDefinition(skillId)?
                .AbilityGaugeCost ?? 0;
        }

        public static uint GetSkillRange(
            int skillId)
        {
            return GetSkillDefinition(skillId)?
                .Distance ?? 0;
        }

        public static int GetSkillCooldown(
            int skillId)
        {
            return GetSkillDefinition(skillId)?
                .Delay ?? 0;
        }

        public static ushort GetRequiredLevel(
            int skillId)
        {
            return GetSkillDefinition(skillId)?
                .RequiredLevel ?? 0;
        }

        public static IReadOnlyDictionary<int, SkillBMD>
            GetAllSkills()
        {
            return _skillDefinitions;
        }

        /// <summary>
        /// Automatic audit against the currently loaded Data skill.bmd.
        ///
        /// This is useful because Data_Broyal comes from a newer
        /// client version. We can immediately see if an S6 ID points
        /// to an unexpected skill name instead of discovering it by
        /// manually testing every character.
        /// </summary>
        private static void AuditSeason6SkillData()
        {
            int checkedCount = 0;
            int missingCount = 0;
            int nameMismatchCount = 0;

            foreach (var pair in
                Season6SkillActionDatabase
                    .Definitions
                    .OrderBy(x => x.Key))
            {
                ushort skillId =
                    pair.Key;

                Season6SkillRoute route =
                    pair.Value;

                checkedCount++;

                if (!_skillDefinitions.TryGetValue(
                        skillId,
                        out var dataSkill))
                {
                    missingCount++;

                    _logger?.LogWarning(
                        "[Season6SkillAudit] ID {SkillId}: expected '{ExpectedName}' but skill.bmd has no entry.",
                        skillId,
                        route.Name);

                    continue;
                }

                string expected =
                    NormalizeName(
                        route.Name);

                string actual =
                    NormalizeName(
                        dataSkill.Name);

                if (!string.Equals(
                        expected,
                        actual,
                        StringComparison.Ordinal))
                {
                    nameMismatchCount++;

                    _logger?.LogWarning(
                        "[Season6SkillAudit] ID {SkillId}: expected '{ExpectedName}', Data contains '{DataName}'. Type={SkillType}, Animation={AnimationMode}",
                        skillId,
                        route.Name,
                        dataSkill.Name,
                        route.SkillType,
                        route.AnimationMode);
                }
                else
                {
                    _logger?.LogDebug(
                        "[Season6SkillAudit] ID {SkillId}: {Name} OK. Type={SkillType}, Animation={AnimationMode}",
                        skillId,
                        route.Name,
                        route.SkillType,
                        route.AnimationMode);
                }
            }

            _logger?.LogInformation(
                "[Season6SkillAudit] Checked {Checked} Season 6 active skills. Missing={Missing}, NameMismatch={Mismatch}.",
                checkedCount,
                missingCount,
                nameMismatchCount);
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

            return new string(
                value
                    .Where(
                        char.IsLetterOrDigit)
                    .Select(
                        char.ToLowerInvariant)
                    .ToArray());
        }
    }
}