#nullable enable
// ClassicFX S6 Batch 47: native Elf cast decals, not persistent buff auras.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzCharacter.cpp: AT_SKILL_HEALING / DEFENSE / ATTACK.
// Server MagicEffectStatus and BuffVisualManager remain authoritative.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(26)]  // Healing
    [SkillVisualEffect(413)] // Healing strengthened
    [SkillVisualEffect(27)]  // Greater Defense
    [SkillVisualEffect(417)] // Greater Defense strengthened
    [SkillVisualEffect(423)] // Greater Defense mastery
    [SkillVisualEffect(28)]  // Greater Damage
    [SkillVisualEffect(420)] // Greater Damage strengthened
    [SkillVisualEffect(422)] // Greater Damage mastery
    public sealed class ElfSupportSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                !ReferenceEquals(context.Caster.World, context.World) ||
                context.Caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            // 0x19 contains the actual target for friendly skills.
            // Self-cast is valid, but never redirect an unresolved
            // nonzero target to the caster.
            WalkerObject target = context.Caster;
            if (context.TargetId != 0)
            {
                if (!context.World.TryGetWalkerById(context.TargetId,
                        out var resolved) || resolved == null)
                    return false;
                target = resolved;
            }

            if (!ReferenceEquals(target.World, context.World) ||
                target.Status != GameControlStatus.Ready)
                return false;

            int subType = context.SkillId switch
            {
                26 or 413 => 1,
                27 or 417 or 423 => 2,
                28 or 420 or 422 => 3,
                _ => -1
            };
            if (subType < 0)
                return false;

            // Native CreateEffect(BITMAP_MAGIC+1, to->Position,
            // to->Angle, to->Light, subType, to).
            // Spear tails are driven separately by 0x07 and must not
            // be spawned again here.
            return fx.CreateEffect(
                ClassicFxEffectType.MagicGround2,
                target.WorldPosition.Translation,
                target.TotalAngle, Vector3.One,
                ClassicFxOwner.FromWorldObject(target),
                subType: subType).IsValid;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
