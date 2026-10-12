#nullable enable
// Season 6 Batch 54: Rageful Blow uses the native FuryStrike carrier,
// its EarthQuake children and the equipped-weapon BMD render bridge.
// No parallel legacy effect while ClassicFX is enabled.
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(42)]  // Rageful Blow
    [SkillVisualEffect(331)] // Strengthened
    [SkillVisualEffect(333)] // Mastery
    public sealed class RagefulBlowSkillEffect : IClassicFxSkillVisualEffect
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

            bool emitted = fx.CreateEffect(
                ClassicFxEffectType.FuryStrike,
                context.Caster.WorldPosition.Translation,
                context.Caster.TotalAngle,
                Vector3.One,
                ClassicFxOwner.FromWorldObject(context.Caster),
                subType: 0).IsValid;
            if (emitted)
                SoundController.Instance.PlayBuffer("Sound/eRageBlow_1.wav");
            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            // ClassicFX pool exhaustion must not render a duplicate skill.
            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx != null && fx.Enabled && !fx.IsDisposed)
                return null;

            return new RagefulBlowEffect(
                context.Caster, context.TargetPosition);
        }
    }
}
