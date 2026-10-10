#nullable enable
// Rageful Blow (42). Keep the old render path while migrating visuals.
// Temporarily allow both native ClassicFX FuryStrike and the existing
// RagefulBlowEffect until the remaining equipped-weapon bridge is ready.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(42)]
    public sealed class RagefulBlowSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            var caster = context.Caster;
            var world = context.World;
            var classicFx = world.ClassicFx;
            if (classicFx != null && classicFx.Enabled &&
                !classicFx.IsDisposed &&
                ReferenceEquals(caster.World, world) &&
                caster.Status == GameControlStatus.Ready)
            {
                // This emits ONLY the native visual carrier plus its
                // ClassicFX child models. No skill packet or damage.
                // Existing RagefulBlowEffect preserves weapon render and
                // other old visuals during this controlled overlap.
                classicFx.CreateEffect(
                    ClassicFxEffectType.FuryStrike,
                    caster.WorldPosition.Translation,
                    caster.TotalAngle,
                    Vector3.One,
                    ClassicFxOwner.FromWorldObject(caster),
                    subType: 0);
            }

            return new RagefulBlowEffect(caster, context.TargetPosition);
        }
    }
}
