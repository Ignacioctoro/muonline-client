#nullable enable
using Client.Data.BMD;
using Client.Main.ClassicFX.Core;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(260)] // Killing Blow
    [SkillVisualEffect(262)] // Chain Drive
    public sealed class ClassicFxRageFighterStrikeEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject { Status: GameControlStatus.Ready } caster ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            var owner = ClassicFxOwner.FromWorldObject(caster);
            var target = context.TargetId == 0 ? null : context.World.FindWalkerById(context.TargetId);
            bool created;
            if (context.SkillId == 262)
            {
                created = fx.CreateEffect(ClassicFxEffectType.SwordEffCarrier,
                    origin, angle, Vector3.One, owner, nativeTarget: target).IsValid;
            }
            else
            {
                int action = (int)PlayerAction.PlayerSkillThrust;
                var actions = caster.Model?.Actions;
                float actionSpeed = actions != null && action < actions.Length
                    ? actions[action]?.PlaySpeed ?? 0f : 0f;
                created = fx.CreateEffect(ClassicFxEffectType.WolfHeadEffect,
                    origin, angle, Vector3.One, owner, nativeAnimationSpeed: actionSpeed).IsValid;

                if (target is { Status: GameControlStatus.Ready })
                    created |= fx.CreateEffect(ClassicFxEffectType.SbumbImpactEmitter,
                        target.WorldPosition.Translation, angle, Vector3.One, owner,
                        scale: 2.1f, nativeTarget: target).IsValid;
            }

            // The targeted animation handler already plays the local cast sound.
            if (created && !caster.IsMainWalker && SkillDefinitions.GetSkillSound(context.SkillId) is { } sound)
                SoundController.Instance.PlayBuffer(sound);

            return created;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
