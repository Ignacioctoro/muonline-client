#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(263)] // Dark Side
    [SkillVisualEffect(264)] // Dragon Roar
    [SkillVisualEffect(265)] // Dragon Slasher
    public sealed class ClassicFxRageFighterDragonSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.IsAreaSkillAnimation != (context.SkillId == 264) ||
                context.Caster is not PlayerObject { Status: GameControlStatus.Ready } caster ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            var owner = ClassicFxOwner.FromWorldObject(caster);
            if (context.SkillId == 263)
            {
                bool emitted = fx.CreateEffect(ClassicFxEffectType.EventCloudEffect,
                    origin, angle, new Vector3(1f, 0.6f, 0.6f), owner, subType: 1).IsValid;
                emitted |= fx.CreateEffect(ClassicFxEffectType.MagicGroundBase,
                    origin, new Vector3(0f, 0f, MathHelper.ToDegrees(angle.Z)),
                    new Vector3(1f, 0.4f, 0f), ClassicFxOwner.None, subType: 14).IsValid;
                emitted |= fx.CreateEffect(ClassicFxEffectType.WindForce,
                    origin, angle, new Vector3(1f, 0.5f, 0.2f), owner, subType: 2).IsValid;
                if (context.TargetPosition is { } struck &&
                    Vector2.DistanceSquared(new Vector2(origin.X, origin.Y), new Vector2(struck.X, struck.Y)) > 1f)
                {
                    var echo = new ClassicFxDarkSideEchoes(caster, struck);
                    context.World.Objects.Add(echo);
                    _ = echo.Load();
                    emitted = true;
                }
                return emitted;
            }

            if (context.SkillId == 264)
            {
                bool emitted = fx.CreateEffect(ClassicFxEffectType.DragonLoreLava,
                    origin, angle, Vector3.One, owner).IsValid;
                emitted |= fx.CreateEffect(ClassicFxEffectType.BlowOfDestruction,
                    origin, angle, context.TargetPosition ?? origin, owner, subType: 2).IsValid;
                return emitted;
            }

            var actions = caster.Model?.Actions;
            int kick = (int)PlayerAction.PlayerSkillDragonkick;
            float speed = actions != null && kick < actions.Length ? actions[kick]?.PlaySpeed ?? 0f : 0f;
            bool created = fx.CreateEffect(ClassicFxEffectType.DragonKickDummy,
                origin, angle, new Vector3(0.7f, 0.7f, 1f), owner, nativeAnimationSpeed: speed).IsValid;
            if (context.TargetPosition is { } target)
                angle.Z = MathHelper.ToRadians(AngleUtils.CreateAngleDegrees(origin.X, origin.Y, target.X, target.Y));
            Vector3 forward = origin + Vector3.TransformNormal(new Vector3(0f, 20f, 0f),
                Matrix.CreateRotationZ(angle.Z));
            Vector3 light = new Vector3(0.8f, 0.9f, 1.6f);
            for (int i = 0; i < 3; i++)
                created |= fx.CreateEffect(ClassicFxEffectType.MultiShot1,
                    origin, angle, light, ClassicFxOwner.None).IsValid;
            for (int i = 0; i < 2; i++)
            {
                created |= fx.CreateEffect(ClassicFxEffectType.MultiShot2,
                    forward, angle, light, ClassicFxOwner.None).IsValid;
                created |= fx.CreateEffect(ClassicFxEffectType.MultiShot3,
                    forward, angle, light, ClassicFxOwner.None).IsValid;
            }
            return created;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
