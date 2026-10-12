#nullable enable
// BroyalMU ClassicFX S6 Batch 48: native Summoner book cast chain.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// GameLogic/Skills/SummonSystem.cpp: CreateCastingEffect / CreateSummonObject.
// One network AreaSkillAnimation emits two native bitmap layers,
// a 7-BMD cast and one native summon.
// No persistent status effects, damage or server-owned logic are changed.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(223)] // Book of Sahamutt / Explosion
    [SkillVisualEffect(224)] // Book of Neil / Requiem
    [SkillVisualEffect(225)] // Book of Lagul / Pollution
    public sealed class ClassicFxSummonerBookSkillEffect : IClassicFxSkillVisualEffect
    {
        public bool TryCreateClassicFx(SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Status != GameControlStatus.Ready ||
                context.TargetPosition is not Vector3 target)
                return false;

            // The book skills are ground-target casts (0x1E). Requiring an
            // actual server-provided target avoids guessed projectile routes
            // and prevents a second emission from targetless 0x19 packets.
            if (!float.IsFinite(target.X) || !float.IsFinite(target.Y) ||
                !float.IsFinite(target.Z))
                return false;

            ClassicFxRuntime fx = context.World.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return false;

            Vector3 origin = caster.WorldPosition.Translation;
            Vector3 angle = caster.TotalAngle;
            ClassicFxOwner none = ClassicFxOwner.None;

            // MuMain CreateCastingEffect() starts with BITMAP_MAGIC 10/9.
            // These are native ground primitives, not made-up billboards.
            Vector3 impactLight = context.SkillId switch
            {
                223 => new Vector3(1f, 0.6f, 0.4f),
                224 => new Vector3(0.7f, 0.7f, 1f),
                _ => new Vector3(0.6f, 0.6f, 0.9f)
            };
            bool castGround =
                fx.CreateEffect(ClassicFxEffectType.MagicGround,
                    origin, angle, Vector3.One, none, subType: 10).IsValid;
            castGround |=
                fx.CreateEffect(ClassicFxEffectType.MagicGround,
                    origin, angle, impactLight, none, subType: 9).IsValid;

            // Native CreateCastingEffect(): six layered rotating BMDs
            // and a growing seventh layer; three fixed palettes.
            Vector3 firstLight = context.SkillId switch
            {
                223 => new Vector3(1f, 0.5f, 0f),
                224 => new Vector3(0f, 0.7f, 1f),
                _ => new Vector3(0.6f, 0.3f, 0.9f)
            };
            Vector3 secondLight = context.SkillId switch
            {
                223 => new Vector3(1f, 0.5f, 0f),
                224 => new Vector3(0f, 0f, 1f),
                _ => new Vector3(0.8f, 0.1f, 0.6f)
            };
            Vector3 outerLight = context.SkillId switch
            {
                223 => new Vector3(1f, 0.5f, 0.8f),
                224 => new Vector3(0.8f, 0.5f, 1f),
                _ => new Vector3(0.9f, 0.1f, 1f)
            };

            bool emitted = false;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting1,
                origin, angle, firstLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting11,
                origin, angle, firstLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting111,
                origin, angle, firstLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting2,
                origin, angle, secondLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting22,
                origin, angle, secondLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting222,
                origin, angle, secondLight, none).IsValid;
            emitted |= fx.CreateEffect(ClassicFxEffectType.SummonerCasting4,
                origin, angle, outerLight, none).IsValid;

            int tier = GetBookTier(caster);
            ClassicFxHandle summoned = context.SkillId switch
            {
                223 => SpawnSahamutt(fx, origin, angle, target, tier),
                224 => SpawnNeil(fx, origin, angle, target, tier),
                225 => fx.CreateEffect(ClassicFxEffectType.SummonerLagul,
                    target, Vector3.One, Vector3.One, none,
                    nativePkKey: tier),
                _ => ClassicFxHandle.Invalid
            };
            return castGround || emitted || summoned.IsValid;
        }

        private static ClassicFxHandle SpawnSahamutt(
            ClassicFxRuntime fx, Vector3 origin, Vector3 angle,
            Vector3 target, int tier)
        {
            // Main offsets the creature 150..449 / 150..399 units on
            // each signed XY axis before it travels to the target.
            float dx = Random.Shared.Next(2) == 0
                ? Random.Shared.Next(150, 450)
                : -Random.Shared.Next(150, 400);
            float dy = Random.Shared.Next(2) == 0
                ? Random.Shared.Next(150, 450)
                : -Random.Shared.Next(150, 400);
            return fx.CreateEffect(ClassicFxEffectType.SummonerSahamutt,
                origin + new Vector3(dx, dy, 0f), angle, target,
                ClassicFxOwner.None, subType: tier);
        }

        private static ClassicFxHandle SpawnNeil(
            ClassicFxRuntime fx, Vector3 origin, Vector3 angle,
            Vector3 target, int tier)
        {
            // Main: rotate (0,-100,0) by the caster's facing angle.
            Vector3 start = origin + Vector3.TransformNormal(
                new Vector3(0f, -100f, 0f),
                Matrix.CreateRotationZ(angle.Z));
            return fx.CreateEffect(ClassicFxEffectType.SummonerNeil,
                start, angle, target, ClassicFxOwner.None, subType: tier);
        }

        private static int GetBookTier(PlayerObject caster)
        {
            // MuMain Weapon[1].Level: under +7 = 0, +7..+10 = 1,
            // +11 and above = 2. The client mirrors this level in
            // WeaponObject.ItemLevel; without a book, default to tier 0.
            int level = caster.Weapon2 is { ItemGroup: 5 } right
                ? right.ItemLevel
                : caster.Weapon1 is { ItemGroup: 5 } left
                    ? left.ItemLevel : 0;
            return level >= 11 ? 2 : level >= 7 ? 1 : 0;
        }

        public WorldObject? CreateEffect(SkillEffectContext context) => null;
    }
}
