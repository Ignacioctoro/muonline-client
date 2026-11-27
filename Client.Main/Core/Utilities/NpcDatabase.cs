using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Client.Main.Objects;
using Client.Main.Objects.Monsters;

namespace Client.Main.Core.Utilities
{
    public static class NpcDatabase
    {
        private static readonly Dictionary<ushort, string> NpcNames;
        private static readonly Dictionary<ushort, Type> NpcTypes;
        public static IReadOnlyDictionary<ushort, string> Names => NpcNames;

        static NpcDatabase()
        {
            NpcNames = new Dictionary<ushort, string>();
            NpcTypes = new Dictionary<ushort, Type>();

            // Zakres skanowania: bieżące assembly
            var asm = Assembly.GetExecutingAssembly();

            foreach (var type in asm.GetTypes()
                                     .Where(t => typeof(WalkerObject).IsAssignableFrom(t)))
            {
                var attr = type.GetCustomAttribute<NpcInfoAttribute>();
                if (attr != null)
                {
                    // jeśli powtórka ID, ostatni wpis wygrywa
                    NpcNames[attr.TypeId] = attr.DisplayName;
                    NpcTypes[attr.TypeId] = type;
                }
            }

            // GMHellas::CreateHellasMonster: each Kalima tier has its own server ID,
            // while all tiers reuse the same family model. Keep protocol IDs intact.
            RegisterKalimaFamily(typeof(DeathAngel), "Death Angel", 144, 174, 182, 190, 260, 268, 334);
            RegisterKalimaFamily(typeof(DeathCenturion), "Death Centurion", 145, 175, 183, 191, 261, 269, 336);
            RegisterKalimaFamily(typeof(BloodSoldier), "Blood Soldier", 146, 176, 184, 192, 262, 270, 333);
            RegisterKalimaFamily(typeof(Aegis), "Aegis", 147, 177, 185, 193, 263, 271, 331);
            RegisterKalimaFamily(typeof(LordCenturion), "Rogue Centurion", 148, 178, 186, 194, 264, 272, 332);
            RegisterKalimaFamily(typeof(Necron), "Necron", 149, 179, 187, 195, 265, 273, 335);
            RegisterKalimaFamily(typeof(Schriker), "Schriker", 160, 180, 188, 196, 266, 274, 337);
            RegisterKalimaFamily(typeof(IllusionOfKundun), "Illusion of Kundun", 161, 181, 189, 197, 267, 338);
        }

        private static void RegisterKalimaFamily(Type type, string name, params ushort[] ids)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                NpcTypes[ids[i]] = type;
                NpcNames[ids[i]] = $"{name} {i + 1}";
            }
        }

        /// <summary>Zwraca nazwę NPC/Monstera po jego TypeId.</summary>
        public static string GetNpcName(ushort typeId)
            => NpcNames.TryGetValue(typeId, out var name)
               ? name
               : $"Unknown NPC/Monster ({typeId})";

        /// <summary>Próbuje zwrócić klasę (Type) skojarzoną z danym TypeId.</summary>
        public static bool TryGetNpcType(ushort typeId, out Type npcType)
            => NpcTypes.TryGetValue(typeId, out npcType);

        /// <summary>Pełna mapa typów (do iteracji lub debugowania).</summary>
        public static IReadOnlyDictionary<ushort, Type> AllNpcTypes => NpcTypes;

        /// <summary>
        /// Checks if an NPC can repair items by checking if the NPC type has CanRepair = true.
        /// </summary>
        /// <param name="typeId">The NPC type ID.</param>
        /// <returns>True if the NPC can repair items, false otherwise.</returns>
        public static bool CanNpcRepair(ushort typeId)
        {
            if (!NpcTypes.TryGetValue(typeId, out var npcType))
                return false;

            // Try to create an instance and check CanRepair property
            try
            {
                if (Activator.CreateInstance(npcType) is NPCObject npcInstance)
                {
                    return npcInstance.CanRepair;
                }
            }
            catch
            {
                // If we can't instantiate, return false
            }

            return false;
        }
    }
}
