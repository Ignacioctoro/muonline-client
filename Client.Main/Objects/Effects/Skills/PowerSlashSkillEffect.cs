#nullable enable
// MG Power Slash: port of the previously working PowerSlashEffectGroup.
// Its five moving Shiny02/flare01 projectiles are now native pooled
// ClassicFX sprites, not a separate WorldObject/SpriteBatch renderer.
using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(56)]
    public sealed class PowerSlashSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                !ReferenceEquals(context.Caster.World, context.World))
            {
                MuGame.AppLoggerFactory?.CreateLogger("ClassicFX.PowerSlash")?
                    .LogWarning("[ClassicFX][PowerSlash] Rejected cast: caster/world mismatch.");
                return false;
            }

            var fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
            {
                MuGame.AppLoggerFactory?.CreateLogger("ClassicFX.PowerSlash")?
                    .LogWarning("[ClassicFX][PowerSlash] Runtime unavailable or disabled.");
                return false;
            }

            return fx.EmitMgPowerSlash(
                context.Caster.WorldPosition.Translation,
                context.Caster.Angle,
                context.Caster);
        }

        // Never allocate the legacy WorldObject or a second renderer.
        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
