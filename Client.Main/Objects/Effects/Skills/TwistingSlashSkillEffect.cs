#nullable enable
// ClassicFX S6 Batch 55: BK/MG Twisting Slash native SkillWheel1/2.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Old additive slash arcs remain disabled while ClassicFX is active.
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(41)]  // Twisting Slash
    [SkillVisualEffect(330)] // Strengthened
    [SkillVisualEffect(332)] // Mastery
    [SkillVisualEffect(481)] // MG strengthened
    public sealed class TwistingSlashSkillEffect : IClassicFxSkillVisualEffect
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

            bool created = fx.CreateEffect(
                ClassicFxEffectType.SkillWheel1,
                caster.WorldPosition.Translation, caster.TotalAngle,
                Vector3.One, ClassicFxOwner.FromWorldObject(caster)).IsValid;
            if (created)
                SoundController.Instance.PlayBuffer("Sound/sKnightSkill4.wav");
            return created;
        }

        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx != null && fx.Enabled && !fx.IsDisposed)
                return null;

            return new TwistingSlashEffect(context.Caster);
        }
    }
}
