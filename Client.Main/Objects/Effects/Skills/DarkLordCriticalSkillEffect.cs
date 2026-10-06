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
        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster
                    is not PlayerObject player ||
                context.World == null)
            {
                return null;
            }

            return new DarkLordCriticalCastEffect(
                player);
        }
    }
}