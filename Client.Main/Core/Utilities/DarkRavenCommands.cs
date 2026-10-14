using Client.Main.Core.Client;
using System;
using System.Collections.Generic;

namespace Client.Main.Core.Utilities
{
    /// <summary>
    /// Original Season 6 PET_COMMAND_DEFAULT..TARGET = 120..123.
    /// These are client-side selectable commands, not learned skills:
    /// OpenMU accepts their modes 0..3 through C1 A7 PetCommandRequest.
    /// Never add them to the authoritative F3 11 skill list.
    /// </summary>
    public static class DarkRavenCommands
    {
        public const ushort Normal = 120;
        public const ushort AttackRandom = 121;
        public const ushort AttackWithOwner = 122;
        public const ushort AttackTarget = 123;

        private static readonly SkillEntryState[] Commands =
        {
            new SkillEntryState { SkillId = Normal },
            new SkillEntryState { SkillId = AttackRandom },
            new SkillEntryState { SkillId = AttackWithOwner },
            new SkillEntryState { SkillId = AttackTarget }
        };

        public static bool IsCommandSkill(ushort id) =>
            id >= Normal && id <= AttackTarget;

        public static byte ToMode(ushort id)
        {
            if (!IsCommandSkill(id))
                throw new ArgumentOutOfRangeException(nameof(id));

            return (byte)(id - Normal);
        }

        public static IReadOnlyList<SkillEntryState> GetSkills() => Commands;

        public static string GetName(ushort id) => id switch
        {
            Normal => "Raven: Seguir",
            AttackRandom => "Raven: Ataque aleatorio",
            AttackWithOwner => "Raven: Atacar con dueño",
            AttackTarget => "Raven: Atacar objetivo",
            _ => string.Empty
        };

        public static string GetDescription(ushort id) => id switch
        {
            Normal => "El águila sigue al personaje sin atacar.",
            AttackRandom => "Ataca monstruos cercanos automáticamente.",
            AttackWithOwner => "Ataca al mismo objetivo que su dueño.",
            AttackTarget => "Selecciona un objetivo y usa el botón derecho.",
            _ => string.Empty
        };
    }
}
