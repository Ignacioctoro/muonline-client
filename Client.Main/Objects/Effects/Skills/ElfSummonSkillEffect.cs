#nullable enable
// Main S6 @ 21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzCharacter.cpp AttackEffect: AT_SKILL_SUMMON (+0..+6) emits
// CreateEffect(BITMAP_MAGIC + 1, o->Position, o->Angle, o->Light, 3, o).
// The real monster is created by OpenMU and enters scope via C2 1F;
// this class MUST NOT create a client-side entity or duplicate the NPC.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(30)] // Summon Goblin
    [SkillVisualEffect(31)] // Summon Stone Golem
    [SkillVisualEffect(32)] // Summon Assassin
    [SkillVisualEffect(33)] // Summon Elite Yeti
    [SkillVisualEffect(34)] // Summon Dark Knight
    [SkillVisualEffect(35)] // Summon Bali
    [SkillVisualEffect(36)] // Summon Soldier
    public sealed class ElfSummonSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            // MuMain uses exactly one shared caster decal for all seven
            // classic summon IDs. No invented creature-specific particles.
            // MagicGround2 maps to BITMAP_MAGIC+1 / Magic_Ground2.OZJ.
            // SubType 3 has native 20-tick lifespan, amber Glow and
            // native terrain projection in the ClassicFX renderer.
            return fx.CreateEffect(
                ClassicFxEffectType.MagicGround2,
                caster.WorldPosition.Translation,
                caster.TotalAngle,
                Vector3.One,
                ClassicFxOwner.FromWorldObject(caster),
                subType: 3).IsValid;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
