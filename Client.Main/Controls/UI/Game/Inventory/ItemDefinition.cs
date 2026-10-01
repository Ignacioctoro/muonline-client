using System.Collections.Generic;
using System;

namespace Client.Main.Controls.UI.Game.Inventory
{
    public class ItemDefinition
    {
        private static readonly HashSet<int> s_upgradeJewelIds = new() { 13, 14, 16 };
        public int Id { get; set; } // Unique ID of the item type (e.g., from Item.bmd)
        public string Name { get; set; }
        public int Width { get; set; }  // Width in slots
        public int Height { get; set; } // Height in slots
        public string TexturePath { get; set; } // Path to the item's texture/model

        // Additional stats loaded from items.json for richer tooltips
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }

        public int MagicPower { get; set; }

        /// <summary>
        /// Skill assigned to this item in item.bmd.
        /// 0 means that the item doesn't define a skill.
        /// </summary>
        public int SkillIndex { get; set; }

        public int AttackSpeed { get; set; }

        public int Defense { get; set; }

        /// <summary>
        /// Successful blocking / defense rate.
        /// Used mostly by shields.
        /// </summary>
        public int DefenseRate { get; set; }

        /// <summary>
        /// Magic resistance / magic defense value from item.bmd.
        /// </summary>
        public int MagicResistance { get; set; }
        public int BaseDurability { get; set; }
        public int MagicDurability { get; set; }  // Max durability for staffs (uses MagicDur instead of Durability)
        public int WalkSpeed { get; set; }
        public int RequiredStrength { get; set; }
        public int RequiredDexterity { get; set; }
        public int RequiredVitality { get; set; }
        public int RequiredEnergy { get; set; }

        /// <summary>
        /// Dark Lord Command/Leadership requirement.
        /// </summary>
        public int RequiredCommand { get; set; }

        public int RequiredLevel { get; set; }
        public bool TwoHanded { get; set; }
        public int Group { get; set; }
        /// <summary>
        /// Original equipment slot from item.bmd.
        ///
        /// Classic MU slots:
        /// 0  = Right hand
        /// 1  = Left hand
        /// 2  = Helm
        /// 3  = Armor
        /// 4  = Pants
        /// 5  = Gloves
        /// 6  = Boots
        /// 7  = Wings
        /// 8  = Pet / mount
        /// 9  = Pendant
        /// 10 = Ring
        /// 11 = Secondary ring / compatible accessory slot
        ///
        /// Keeping the raw value is important because Group 13 contains
        /// several unrelated item families.
        /// </summary>
        public int EquipmentSlot { get; set; }
        /// <summary>
        /// Original item classification from item.bmd.
        /// Used by option tables such as excellentwingoption.bmd.
        /// </summary>
        public byte KindA { get; set; }

        public byte KindB { get; set; }

        /// <summary>
        /// Original item Type field from item.bmd.
        /// </summary>
        public byte ItemType { get; set; }
        public bool IsExpensive { get; set; }
        public bool CanSellToNpc { get; set; }
        public int Money { get; set; } // Base buy price (iZen) from Item.bmd, can be 0
        public int ItemValue { get; set; } // Legacy value fallback if Money is missing
        public int DropLevel { get; set; } // Drop level from BMD, used as a proxy for price curve

        // Classes which can equip this item
        public List<string> AllowedClasses { get; set; } = new();

        public ItemDefinition(int id, string name, int width, int height, string texturePath = null)
        {
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            TexturePath = texturePath;
        }

        /// <summary>
        /// Checks if this item is consumable (potions, scrolls, etc.).
        /// </summary>
        public bool IsConsumable()
        {
            // Group 14 = Potions (HP, MP, SD potions)
            // Group 15 = Scrolls
            return Group == 14 || Group == 15;
        }

        /// <summary>
        /// Determines if the item is a jewel (non-consumable items in group 14/12).
        /// Jewels should not show "Right-click to use" even though they're in consumable groups.
        /// </summary>
        /// <summary>
        /// Determines whether this definition represents a jewel.
        ///
        /// Group 14 contains jewels together with potions, event items,
        /// boxes and several miscellaneous consumables, so the group alone
        /// is not enough.
        ///
        /// The name check makes this compatible with newer item.bmd versions
        /// without having to hardcode every Jewel introduced after Season 6.
        /// </summary>
        public bool IsJewel()
        {
            // Classic Jewel of Chaos.
            if (Group == 12 &&
                Id == 15)
            {
                return true;
            }

            if (Group != 14)
            {
                return false;
            }

            // Keep the known classic ids as a compatibility fast path.
            if (Id == 13 || // Bless
                Id == 14 || // Soul
                Id == 16 || // Life
                Id == 22 || // Creation
                Id == 31)   // Guardian
            {
                return true;
            }

            // Modern MU versions contain additional jewels.
            // Let item.bmd identify them through their canonical name instead
            // of maintaining a growing Season-specific ID table.
            if (!string.IsNullOrWhiteSpace(Name) &&
                Name.StartsWith(
                    "Jewel of ",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the item is an upgrade jewel (Bless, Soul, Life).
        /// </summary>
        public bool IsUpgradeJewel()
        {
            return Group == 14 && s_upgradeJewelIds.Contains(Id);
        }
    }
}
