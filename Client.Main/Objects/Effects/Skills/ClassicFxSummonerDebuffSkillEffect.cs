#nullable enable
// BroyalMU ClassicFX Season 6 Batch 51: Summoner Weakness / Innovation.
// Exact cast chain from MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2,
// ZzzCharacter.cpp AT_SKILL_ALICE_WEAKNESS and ALICE_ENERVATION.
// Skills 221/222 are area casts; no server debuff state is changed.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(221)] // Weakness
    [SkillVisualEffect(222)] // Innovation (ALICE_ENERVATION)
    public sealed class ClassicFxSummonerDebuffSkillEffect :
        IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            // Only the 0x1E cast emits this chain. The C++ Main sends an
            // area request, and duplicate 0x19 packets must not add a burst.
            if (!context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 pos = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            ClassicFxOwner none = ClassicFxOwner.None;
            bool weakness = context.SkillId == 221;

            // Native weakness has a dark red outer MAGIC_ZIN circle and
            // orange-red inner circles. Innovation uses aqua-green for all.
            Vector3 outer = weakness
                ? new Vector3(2f, 0.1f, 0.1f)
                : new Vector3(0.25f, 1f, 0.7f);
            Vector3 inner = weakness
                ? new Vector3(2f, 0.4f, 0.3f) : outer;

            bool emitted = false;
            emitted |= fx.CreateEffect(ClassicFxEffectType.MagicZin,
                pos, angle, outer, none, subType: 1, scale: 7f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.MagicZin,
                pos, angle, inner, none, subType: 0, scale: 2f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.MagicZin,
                pos, angle, inner, none, subType: 2, scale: 1f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.MagicZin,
                pos, angle, inner, none, subType: 2, scale: 0.2f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.MagicZin,
                pos, angle, inner, none, subType: 2, scale: 0.1f).IsValid;

            // Native BMDs: Suhwanzin2, 22, 222, subtype 1 at scale 0.6.
            // Shared ClassicFX models retain their original fade/rotation.
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting2,
                pos, angle, inner, none, subType: 1, scale: 0.6f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting22,
                pos, angle, inner, none, subType: 1, scale: 0.6f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting222,
                pos, angle, inner, none, subType: 1, scale: 0.6f).IsValid;

            // Original BITMAP_SHINY+6 and PIN_LIGHT are finite moving
            // particle-emitting roots, not one static sprite each.
            Vector3 flash = weakness
                ? new Vector3(1.4f, 0.2f, 0.2f) : inner;
            emitted |= fx.CreateEffect(ClassicFxEffectType.ShinyScatterEmitter,
                pos, angle, flash, none, subType: 0, scale: 0.5f).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.PinLightEffect,
                pos, angle, flash, none, subType: 0, scale: 1f).IsValid;

            // CharacterDataHandler already plays the original mapped files
            // SE_Ch_summoner_weakness/innovation.wav for the local cast.
            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
