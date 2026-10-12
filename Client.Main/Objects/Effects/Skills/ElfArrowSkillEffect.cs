#nullable enable
// ClassicFX S6 Batch 47: exact weapon-dependent Elf arrow BMD selection.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffectMagicSkill.cpp CreateArrow/CreateArrows and _enum.h MODEL_BOW+N.
// Visuals are emitted on authoritative 0x19 only; no damage/packet changes.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(24)]  // Triple Shot
    [SkillVisualEffect(414)] // Triple Shot strengthened
    [SkillVisualEffect(418)] // Triple Shot mastery
    [SkillVisualEffect(51)]  // Ice Arrow
    [SkillVisualEffect(424)] // Ice Arrow strengthened
    [SkillVisualEffect(52)]  // Penetration
    [SkillVisualEffect(416)] // Penetration strengthened
    public sealed class ElfArrowSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed ||
                !TryGetArrowModel(caster, out ClassicFxEffectType model,
                    out bool specialTripleShot))
                return false;

            bool triple = context.SkillId is 24 or 414 or 418;
            int subType = context.SkillId is 52 or 416 ? 2 : 0;
            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(caster);

            if (!triple)
            {
                return fx.CreateEffect(model, origin, angle, Vector3.One,
                    owner, subType: subType).IsValid;
            }

            // Native CreateArrows(Skill==1): 3 arrows at 0/+15/-15 degrees,
            // or 4 at +5/+15/-5/-15 for five special bow/crossbow models.
            if (specialTripleShot)
            {
                Emit(fx, model, origin, angle, owner, 5f);
                Emit(fx, model, origin, angle, owner, 15f);
                Emit(fx, model, origin, angle, owner, -5f);
                Emit(fx, model, origin, angle, owner, -15f);
            }
            else
            {
                Emit(fx, model, origin, angle, owner, 0f);
                Emit(fx, model, origin, angle, owner, 15f);
                Emit(fx, model, origin, angle, owner, -15f);
            }
            return true;
        }

        private static void Emit(ClassicFxRuntime fx,
            ClassicFxEffectType model, Vector3 origin, Vector3 angle,
            ClassicFxOwner owner, float degrees)
        {
            Vector3 spread = angle;
            spread.Z += MathHelper.ToRadians(degrees);
            fx.CreateEffect(model, origin, spread, Vector3.One, owner);
        }

        private static bool TryGetArrowModel(PlayerObject caster,
            out ClassicFxEffectType model, out bool specialTripleShot)
        {
            model = default;
            specialTripleShot = false;

            // Weapon1 is the left-hand bow; Weapon2 is the right-hand
            // crossbow. Both carry native item group/number (not BMD name).
            // Exclude arrows (4,15) and bolts (4,7).
            int item = -1;
            if (caster.Weapon2 != null && caster.Weapon2.ItemGroup == 4 &&
                caster.Weapon2.Model != null)
                item = caster.Weapon2.ItemNumber;
            else if (caster.Weapon1 != null &&
                     caster.Weapon1.ItemGroup == 4 &&
                     caster.Weapon1.Model != null)
                item = caster.Weapon1.ItemNumber;

            // Main model enums: MODEL_BOW + item. Unsupported original
            // Holy Arrow (Celestial Bow, 17) is deliberately not replaced.
            model = item switch
            {
                0 or 1 or 3 or 4 or 5 => ClassicFxEffectType.ArrowBasicModel,
                2 => ClassicFxEffectType.ArrowV,
                6 => ClassicFxEffectType.ArrowNature,
                8 or 9 => ClassicFxEffectType.ArrowSteel,
                10 => ClassicFxEffectType.ArrowSaw,
                11 => ClassicFxEffectType.ArrowLaser,
                12 => ClassicFxEffectType.ArrowThunder,
                13 => ClassicFxEffectType.ArrowWing,
                14 => ClassicFxEffectType.ArrowBomb,
                16 => ClassicFxEffectType.ArrowDouble,
                18 => ClassicFxEffectType.ArrowBestCrossbow,
                19 => ClassicFxEffectType.ArrowDrill,
                20 => ClassicFxEffectType.LaceArrowModel,
                21 => ClassicFxEffectType.ArrowSpark,
                22 => ClassicFxEffectType.ArrowRing,
                23 => ClassicFxEffectType.ArrowDarkStingerModel,
                24 => ClassicFxEffectType.ArrowGamble,
                _ => default
            };
            specialTripleShot = item is 18 or 19 or 22 or 23 or 24;
            return item switch
            {
                0 or 1 or 2 or 3 or 4 or 5 or 6 or
                8 or 9 or 10 or 11 or 12 or 13 or 14 or 16 or
                18 or 19 or 20 or 21 or 22 or 23 or 24 => true,
                _ => false
            };
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
