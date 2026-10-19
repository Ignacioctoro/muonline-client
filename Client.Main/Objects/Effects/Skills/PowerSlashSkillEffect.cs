#nullable enable
// Season 6 Power Slash: five native MODEL_MAGIC2 (subtype 2) effects.
// MuMain ZzzCharacter.cpp AT_SKILL_POWER_SLASH / _STR.
// Visual-only; server hit timing, attack packets and sound remain unchanged.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Objects.Effects;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(56)]
    public sealed class PowerSlashSkillEffect : IClassicFxSkillVisualEffect
    {
        private static readonly float[] SpreadDegrees = { -40f, -20f, 0f, 20f, 40f };

        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                !ReferenceEquals(context.Caster.World, context.World))
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = context.Caster.WorldPosition.Translation;
            Vector3 angle = context.Caster.Angle;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(context.Caster);

            // Native CreateEffect(MODEL_MAGIC2, position, angle, light, 2, owner).
            // The Batch12 mover/renderer owns the Magic02 BMD, smoke,
            // Shiny/Light sprites, lifetime and terrain lighting.
            bool created = false;
            for (int i = 0; i < SpreadDegrees.Length; i++)
            {
                Vector3 shotAngle = angle;
                shotAngle.Z += MathHelper.ToRadians(SpreadDegrees[i]);
                created |= fx.CreateEffect(
                    ClassicFxEffectType.Magic2, origin, shotAngle,
                    Vector3.One, owner, subType: 2).IsValid;
            }
            return created;
        }

        // No fallback to the old five-WorldObject + SpriteBatch implementation.
        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
