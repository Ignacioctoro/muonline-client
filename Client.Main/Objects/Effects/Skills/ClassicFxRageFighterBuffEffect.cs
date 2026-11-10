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
    [SkillVisualEffect(266)] // Ignore Defense
    [SkillVisualEffect(267)] // Increase Health
    [SkillVisualEffect(268)] // Increase Block
    public sealed class ClassicFxRageFighterBuffEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject { Status: GameControlStatus.Ready } caster ||
                context.World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(caster.World, context.World))
                return false;

            var (sword, wave, cloud, wind, seal, windSubtype) = context.SkillId switch
            {
                266 => (new Vector3(0.2f, 0.4f, 1f), new Vector3(0.6f, 0.65f, 1f),
                    new Vector3(0.65f, 0.8f, 1f), new Vector3(0.5f, 0.55f, 1f),
                    new Vector3(0.5f, 0.55f, 0.9f), 0),
                267 => (new Vector3(0.5f, 0.28f, 1f), new Vector3(1f, 0.35f, 1f),
                    new Vector3(0.8f, 0.4f, 1f), new Vector3(0.85f, 0.2f, 1f),
                    new Vector3(0.78f, 0.2f, 1f), 4),
                _ => (new Vector3(1f, 0.15f, 0f), new Vector3(1f, 0.1f, 0f),
                    new Vector3(1f, 0.3f, 0.2f), new Vector3(1f, 0.2f, 0f),
                    new Vector3(1f, 0.15f, 0f), 5)
            };

            Vector3 position = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            var owner = ClassicFxOwner.FromWorldObject(caster);
            const float scale = ClassicFxRuntime.RageFighterBuffVisualScale;
            bool created = fx.CreateEffect(ClassicFxEffectType.SwordMonoEmitter,
                position, angle, sword, owner, subType: context.SkillId - 266, scale: scale).IsValid;
            for (int i = 0; i < 3; i++)
            {
                created |= fx.CreateEffect(ClassicFxEffectType.ShockWaveGround01,
                    position, angle, wave, owner, scale: (1f - i * 0.15f) * scale).IsValid;
                created |= fx.CreateEffect(ClassicFxEffectType.EventCloudEffect,
                    position, angle, cloud, owner, scale: scale).IsValid;
            }
            created |= fx.CreateEffect(ClassicFxEffectType.WindForce,
                position, angle, wind, owner, subType: windSubtype, scale: scale).IsValid;
            created |= fx.CreateEffect(ClassicFxEffectType.MagicGroundBase,
                position, new Vector3(0f, 0f, MathHelper.ToDegrees(angle.Z)),
                seal, ClassicFxOwner.None, subType: 13).IsValid;
            int flashSubtype = context.SkillId switch { 266 => 1, 267 => 0, _ => 2 };
            created |= fx.CreateEffect(ClassicFxEffectType.LightRedGround,
                position, angle, Vector3.One, owner, subType: flashSubtype, scale: 1.15f).IsValid;

            if (created && !caster.IsMainWalker && SkillDefinitions.GetSkillSound(context.SkillId) is { } sound)
                SoundController.Instance.PlayBuffer(sound);

            return created;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
