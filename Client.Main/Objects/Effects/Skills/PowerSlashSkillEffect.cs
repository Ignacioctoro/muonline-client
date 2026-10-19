#nullable enable
// MG Power Slash: port of the previously working PowerSlashEffectGroup.
// Its five moving Shiny02/flare01 projectiles are now native pooled
// ClassicFX sprites, not a separate WorldObject/SpriteBatch renderer.
using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(56)]
    public sealed class PowerSlashSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                !ReferenceEquals(context.Caster.World, context.World))
                return false;

            var fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            return fx.EmitMgPowerSlash(
                context.Caster.WorldPosition.Translation,
                context.Caster.Angle);
        }

        // Never allocate the legacy WorldObject or a second renderer.
        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
