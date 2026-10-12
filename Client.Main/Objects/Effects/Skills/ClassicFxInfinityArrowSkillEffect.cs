#nullable enable
// ClassicFX Season 6 Batch 50: Infinity Arrow cast (77, 441).
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp MODEL_INFINITY_ARROW subtype 0 emits exactly five BMDs:
// MODEL_INFINITY_ARROW subtype 1 and MODEL_INFINITY_ARROW1..4 subtype 0.
// Only a received 0x19 SkillAnimation spawns the finite cast burst;
// persistent ammunition chrome is driven by MagicEffectStatus (0x07).
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(77)]
    [SkillVisualEffect(441)]
    public sealed class ClassicFxInfinityArrowSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(caster);

            // Native MODEL_INFINITY_ARROW subtype 0 is only a logical
            // emitter. Its five *visible* children are emitted directly;
            // no second model-less WorldObject or perpetual buff emitter.
            bool emitted = fx.CreateEffect(
                ClassicFxEffectType.InfinityArrowCore, origin, angle,
                Vector3.One, owner, subType: 1).IsValid;

            emitted |= fx.CreateEffect(
                ClassicFxEffectType.InfinityArrow1, origin, angle,
                Vector3.One, owner).IsValid;

            emitted |= fx.CreateEffect(
                ClassicFxEffectType.InfinityArrow2, origin, angle,
                Vector3.One, owner).IsValid;

            emitted |= fx.CreateEffect(
                ClassicFxEffectType.InfinityArrow3, origin, angle,
                Vector3.One, owner).IsValid;

            emitted |= fx.CreateEffect(
                ClassicFxEffectType.InfinityArrow4, origin, angle,
                new Vector3(1f, 0.5f, 0.3f), owner).IsValid;

            // The pinned Main registers SOUND_INFINITYARROW but does not
            // call PlayBuffer for it. The original audio file is present
            // in Data_Broyal; play it once on the server-confirmed cast,
            // not every frame or for the persistent 0x07 buff state.
            if (emitted)
                SoundController.Instance.PlayBuffer("Sound/infinityArrow.wav");

            return emitted;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
