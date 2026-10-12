#nullable enable
// ClassicFX Season 6 Batch 53: direct BK Greater Fortitude cast emitter.
// Main@21728b1e5b03e0763b38ef9e23f79645e0df7ad2,
// ZzzCharacter.cpp: all SWELL_LIFE variants use the same 36 SPIRIT(2)
// joints and 2 BITMAP_MAGIC+1(4) instances. Buff state and damage unchanged.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(48)]  // Swell Life
    [SkillVisualEffect(356)] // Swell Life strengthened
    [SkillVisualEffect(360)] // Swell Life proficiency
    public sealed class SwellLifeSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                !ReferenceEquals(context.Caster.World, context.World) ||
                context.Caster.Status != GameControlStatus.Ready)
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = context.Caster.WorldPosition.Translation;
            Vector3 at = origin + new Vector3(0f, 0f, 100f);
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(context.Caster);
            int created = 0;

            // Native angles are in degrees for joints, not MonoGame radians.
            for (int i = 0; i < 36; i++)
            {
                Vector3 angle = new Vector3(-10f, 0f, i * 10f);
                if (fx.CreateJoint(ClassicTextureIds.BitmapJointSpirit,
                    at, at, angle, subType: 2, target: owner,
                    scale: 60f).IsValid)
                    created++;

                // The native cast emits ground magic only at i=0,20.
                if (i % 20 == 0 && fx.CreateEffect(
                    ClassicFxEffectType.MagicGround2, origin, angle,
                    Vector3.One, owner, subType: 4).IsValid)
                    created++;
            }

            // Pooled joints and ground effects outlive this call; do not
            // allocate a one-frame WorldObject or create a second renderer.
            return created > 0;
        }

        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null)
                return null;

            // Legacy path only when ClassicFX is genuinely disabled.
            // An exhausted ClassicFX pool must not invoke a second renderer.
            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx != null && fx.Enabled && !fx.IsDisposed)
                return null;

            return new SwellLifeCastEffect(context.Caster);
        }
    }
}
