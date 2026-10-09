#nullable enable

using Client.Main.Core.Utilities;
using Client.Main.Objects.Player;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Dark Lord - Increase Critical Damage.
    ///
    /// Classic Main:
    ///
    ///     AT_SKILL_ADD_CRITICAL = 64
    ///
    /// El efecto visual es únicamente un cast corto.
    /// No existe una aura persistente mientras dura el buff.
    ///
    /// El estado persistente queda representado por:
    ///
    ///     EffectId = 5
    ///
    /// y su icono en la barra de buffs.
    /// </summary>
    [SkillVisualEffect(64)]
    public sealed class DarkLordCriticalSkillEffect :
        ISkillVisualEffect
    {
        // Set false to compare against DarkLordCriticalCastEffect.
        private const bool UseClassicFxCriticalPilot = true;
        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster
                    is not PlayerObject player ||
                context.World == null)
            {
                return null;
            }

            if (UseClassicFxCriticalPilot &&
                context.World.ClassicFx != null &&
                context.World.ClassicFx.Enabled &&
                !context.World.ClassicFx.IsDisposed)
            {
                return new ClassicFxDarkLordCriticalCastVisual(player);
            }

            return new DarkLordCriticalCastEffect(player);
        }
    }
}