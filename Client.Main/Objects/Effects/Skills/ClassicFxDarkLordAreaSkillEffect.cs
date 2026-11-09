#nullable enable
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(65)]
    [SkillVisualEffect(238)]
    [SkillVisualEffect(523)]
    public sealed class ClassicFxDarkLordAreaSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (!context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject caster ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            if (context.TargetPosition is { } target &&
                Vector2.DistanceSquared(new Vector2(origin.X, origin.Y),
                    new Vector2(target.X, target.Y)) > 1f)
                angle.Z = MathHelper.ToRadians(AngleUtils.CreateAngleDegrees(
                    origin.X, origin.Y, target.X, target.Y));

            Vector3 jointAngle = angle * (180f / MathHelper.Pi);
            var owner = ClassicFxOwner.FromWorldObject(caster);
            if (context.SkillId == 65)
            {
                float height = caster.CurrentAction switch
                {
                    PlayerAction.PlayerAttackRideAttackFlash => 30f,
                    PlayerAction.PlayerFenrirAttackDarklordFlash => -10f,
                    _ => -50f
                };
                Vector3 position = origin + Vector3.TransformNormal(
                    new Vector3(0f, -90f, -50f), Matrix.CreateRotationZ(angle.Z));
                position.Z = origin.Z + height;
                bool emitted = fx.CreateEffect(ClassicFxEffectType.FlareForceJointBurst,
                    position, jointAngle, Vector3.One, owner).IsValid;
                if (emitted && !caster.IsMainWalker)
                    SoundController.Instance.PlayBuffer("Sound/sDarkElecSpike.wav");
                return emitted;
            }

            bool created = false;
            for (int i = 0; i < 5; i++)
                created |= fx.CreateEffect(ClassicFxEffectType.ShinyScatterEmitter,
                    origin, angle, new Vector3(0.5f), owner, subType: 3, scale: 0.5f).IsValid;

            for (int i = 0; i < 8; i++)
            {
                Vector3 start = origin + new Vector3(
                    fx.Random.Modulo(240) - 119f, 0f, 49f + fx.Random.Modulo(60));
                created |= fx.CreateJoint(ClassicTextureIds.Bitmap2LineGhost,
                    start, origin, jointAngle, target: owner, scale: 20f).IsValid;
            }

            // Use the server's cast tile for the local impact accent.
            if (caster.IsMainWalker && context.TargetPosition is { } impact)
            {
                impact.Z += 80f;
                for (int i = 0; i < 20; i++)
                {
                    Vector3 sparkAngle = new Vector3(
                        150f + fx.Random.Modulo(60), 0f, fx.Random.Modulo(30));
                    fx.CreateParticle(ClassicTextureIds.BitmapSpark,
                        impact, sparkAngle, Vector3.One, subType: 2);
                }
                fx.CreateParticle(ClassicTextureIds.BitmapExplotion,
                    impact, Vector3.Zero, new Vector3(0.7f));
            }

            if (created && (!caster.IsMainWalker || context.SkillId == 523))
                SoundController.Instance.PlayBuffer("Sound/caotic.wav");
            return created;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
