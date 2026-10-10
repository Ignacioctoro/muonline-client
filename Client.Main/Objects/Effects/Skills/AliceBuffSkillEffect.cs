#nullable enable
// Six native Summoner S6 debuff/buff cast IDs wired without replacing
// gameplay, packets, existing status icons or persistent buff manager.
// Reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzCharacter.cpp: ALICE_BERSERKER / SLEEP / BLIND / THORNS.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(217)] // Thorns, target
    [SkillVisualEffect(218)] // Berserker, self
    [SkillVisualEffect(219)] // Sleep, target
    [SkillVisualEffect(220)] // Blind, target
    [SkillVisualEffect(454)] // Sleep (strengthened), target
    [SkillVisualEffect(469)] // Berserker (strengthened), self
    public sealed class AliceBuffSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            bool targeted = context.SkillId is 217 or 219 or 220 or 454;
            WalkerObject target = context.Caster;

            if (targeted)
            {
                // Never spawn hostile debuff rings on the caster when the
                // target packet is missing. It is safer to skip the effect.
                if (context.TargetId == 0 ||
                    !context.World.TryGetWalkerById(context.TargetId,
                        out var resolved) || resolved == null)
                    return null;
                target = resolved;
            }

            if (context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;

            return new ClassicFxAliceBuffCastVisual(
                context.Caster, target, context.SkillId);
        }
    }

    // WorldObject exists only until it dispatches the native-effect batch.
    // Lifetime, secondary sprites, particles and joints live in ClassicFX.
    internal sealed class ClassicFxAliceBuffCastVisual : WorldObject
    {
        private readonly WalkerObject _caster;
        private readonly WalkerObject _target;
        private readonly ushort _skillId;
        private bool _emitted;

        internal ClassicFxAliceBuffCastVisual(
            WalkerObject caster, WalkerObject target, ushort skillId)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _skillId = skillId;
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-300f, -300f, -80f),
                new Vector3(300f, 300f, 340f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (_emitted || World == null ||
                !ReferenceEquals(_caster.World, World) ||
                !ReferenceEquals(_target.World, World) ||
                _caster.Status != GameControlStatus.Ready ||
                _target.Status != GameControlStatus.Ready)
            {
                RemoveSelf();
                return;
            }

            ClassicFxRuntime fx = World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
            {
                RemoveSelf();
                return;
            }

            _emitted = true;
            bool berserker = _skillId is 218 or 469;
            int ringSubtype = _skillId == 220 ? 1 :
                              _skillId == 217 ? 2 : 0;
            Vector3 light = berserker
                ? new Vector3(1f, 0.1f, 0.2f)
                : ringSubtype == 1 ? Vector3.One
                : ringSubtype == 2 ? new Vector3(0.8f, 0.5f, 0.2f)
                : new Vector3(0.8f, 0.3f, 0.9f);

            ClassicFxOwner casterOwner =
                ClassicFxOwner.FromWorldObject(_caster);
            ClassicFxOwner targetOwner =
                ClassicFxOwner.FromWorldObject(_target);

            // Original ZzzCharacter cast ring: the MAGIC+1 decal is emitted
            // at the caster, while Alice's two BMD rings are on the target
            // of the sleep/blind/thorns skill (or self for berserker).
            Vector3 sourceAngle = _caster.TotalAngle;
            Vector3 terrainAngle = new Vector3(0f, 0f,
                MathHelper.ToDegrees(sourceAngle.Z));
            fx.CreateEffect(
                ClassicFxEffectType.MagicGround2,
                _caster.WorldPosition.Translation, terrainAngle, light,
                casterOwner, subType: ringSubtype == 1 ? 12 : 11);

            Vector3 targetPos = _target.WorldPosition.Translation;
            Vector3 targetAngle = _target.TotalAngle;
            fx.CreateEffect(ClassicFxEffectType.AliceBuffSkillEffect,
                targetPos, targetAngle, light, targetOwner,
                subType: ringSubtype);
            fx.CreateEffect(ClassicFxEffectType.AliceBuffSkillEffect2,
                targetPos, targetAngle, light, targetOwner,
                subType: ringSubtype);
            RemoveSelf();
        }

        private void RemoveSelf()
        {
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
