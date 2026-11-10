#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(236)] // Flame Strike
    [SkillVisualEffect(237)] // Gigantic Storm
    public sealed class ClassicFxMagicGladiatorAreaSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (!context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject { Status: GameControlStatus.Ready } caster ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            if (context.SkillId == 236)
            {
                return fx.CreateEffect(ClassicFxEffectType.FlameStrike,
                    caster.WorldPosition.Translation, caster.TotalAngle, Vector3.One,
                    ClassicFxOwner.FromWorldObject(caster)).IsValid;
            }

            var world = context.World;
            void EmitStorm()
            {
                if (ReferenceEquals(caster.World, world) &&
                    caster.Status == GameControlStatus.Ready && !caster.IsDead)
                    fx.CreateGiganticStorm(caster.WorldPosition.Translation, caster.TotalAngle);
            }

            if (MuGame.Instance?.ActiveScene is not GameScene scene ||
                !scene.GroundImpacts.TryQueueVisual(world, caster, context.SkillId, EmitStorm))
                EmitStorm();

            return true;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
