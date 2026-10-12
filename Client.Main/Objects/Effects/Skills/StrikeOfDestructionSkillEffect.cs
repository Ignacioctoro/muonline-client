#nullable enable
// MU Online Season 6 Strike of Destruction (232 / strengthened 337).
// Native reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzCharacter.cpp -> MODEL_BLOW_OF_DESTRUCTION, with SkillX/Y in Light.
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(232)]
    [SkillVisualEffect(337)]
    public sealed class StrikeOfDestructionSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            // This is a coordinate-targeted area cast. The server's 0x1E
            // message carries SkillX/Y; 0x19 alone has no reliable target
            // tile and must not spawn another copy of the visual sequence.
            if (!context.IsAreaSkillAnimation ||
                context.TargetPosition is not Vector3 target ||
                context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = caster.WorldPosition.Translation;

            // Native Light is NOT RGB here: (SkillX + .5, SkillY + .5)
            // times TERRAIN_SCALE, with Z copied from the caster's Position.
            // CreateEffect saves it in StartPosition before resetting Light.
            target.Z = origin.Z;

            return fx.CreateEffect(
                ClassicFxEffectType.BlowOfDestruction,
                origin, caster.TotalAngle, target,
                ClassicFxOwner.FromWorldObject(caster),
                subType: 0).IsValid;
        }

        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            // Do not draw a fabricated fallback or a parallel renderer.
            return null;
        }
    }
}
