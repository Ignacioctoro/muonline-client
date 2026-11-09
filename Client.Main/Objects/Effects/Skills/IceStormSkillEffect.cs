#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Factory for Ice Storm skill visual effect (Skill ID 39 - Blast Freeze).
    /// </summary>
    [SkillVisualEffect(39)]
    public sealed class IceStormSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (!context.IsAreaSkillAnimation ||
                context.TargetPosition is not { } center ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                context.Caster == null || !ReferenceEquals(context.Caster.World, context.World))
                return false;

            // MuMain spawns ten Blizzard effects with progressively varied
            // heights. Only the first carries the impact sound marker.
            bool emitted = false;
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = center + new Vector3(0f, 0f, fx.Random.Modulo(50) * i);
                emitted |= fx.CreateEffect(ClassicFxEffectType.Blizzard, position,
                    context.Caster.TotalAngle, Vector3.One, ClassicFxOwner.None,
                    nativePkKey: i + 1).IsValid;
            }

            if (emitted)
                SoundController.Instance.PlayBuffer("Sound/eSuddenIce_1.wav");

            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
