#nullable enable

using System;
using Client.Main.Core.Utilities;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>
    /// Plasma Storm - Fenrir skill.
    ///
    /// Original MU:
    /// AT_SKILL_PLASMA_STORM_FENRIR = 76
    ///
    /// Creates:
    /// - Main lightning group against selected target.
    /// - Additional lightning groups against nearby monsters.
    /// </summary>
    [SkillVisualEffect(76)]
    public sealed class FenrirPlasmaStormSkillEffect
        : ISkillVisualEffect
    {
        private const int MaxSecondaryTargets = 10;

        public WorldObject? CreateEffect(
            SkillEffectContext context)
        {
            if (context.Caster is not PlayerObject player ||
                context.World == null)
            {
                return null;
            }

            if (player.Vehicle == null ||
                player.Vehicle.Hidden)
            {
                return null;
            }

            short fenrirIndex =
                player.Vehicle.ItemIndex;

            if (!IsFenrir(fenrirIndex))
            {
                return null;
            }

            ushort mainTargetId =
                context.TargetId;


            // =====================================================
            // SOURCE
            // =====================================================
            //
            // Original:
            //
            // CalcAddPosition(
            //     o,
            //     0.f,
            //     -140.f,
            //     130.f,
            //     Position);
            //
            Vector3 SourceProvider()
            {
                if (player.Vehicle == null)
                {
                    return
                        player.WorldPosition.Translation +
                        Vector3.UnitZ * 130f;
                }

                return Vector3.Transform(
                    new Vector3(
                        0f,
                        -140f,
                        130f),
                    player.Vehicle.WorldPosition);
            }


            // =====================================================
            // MAIN TARGET
            // =====================================================

            Vector3 MainTargetProvider()
            {
                if (mainTargetId != 0 &&
                    context.World.TryGetWalkerById(
                        mainTargetId,
                        out var target))
                {
                    return
                        target.WorldPosition.Translation +
                        Vector3.UnitZ * 80f;
                }

                if (context.TargetPosition.HasValue)
                {
                    return
                        context.TargetPosition.Value +
                        Vector3.UnitZ * 80f;
                }

                return SourceProvider();
            }


            //
            // This is the primary target.
            //
            // The registry/caller will add this one
            // to the world for us.
            //
            var mainEffect =
                new FenrirPlasmaStormEffect(
                    SourceProvider,
                    MainTargetProvider,
                    fenrirIndex);
                //
                // Original MU also creates six electrical
                // BITMAP_FLARE_FORCE joints around the Fenrir
                // when Plasma Storm begins.
                //
                // We combine the six classic joints into one
                // optimized effect object.
                //
                Matrix VehicleMatrixProvider()
                {
                    if (player.Vehicle != null &&
                        !player.Vehicle.Hidden)
                    {
                        return
                            player.Vehicle.WorldPosition;
                    }

                    return
                        player.WorldPosition;
                }

                var castBurst =
                    new FenrirPlasmaCastBurstEffect(
                        VehicleMatrixProvider,
                        fenrirIndex);

                context.World.Objects.Add(
                    castBurst);

                _ = castBurst.Load();


            // =====================================================
            // SECONDARY TARGETS
            // =====================================================
            //
            // Original MU searches up to:
            //
            // MAX_FENRIR_SKILL_MONSTER_NUM = 10
            //
            // monsters inside Plasma Storm range.
            //

            float skillRange =
                SkillDatabase.GetSkillRange(
                    context.SkillId);

            if (skillRange <= 0f)
            {
                skillRange = 6f;
            }

            int secondaryCount = 0;

            var nearbyMonsters =
                context.World.Monsters
                    .Where(monster =>
                        monster != null &&
                        !monster.IsDead &&
                        monster.World == context.World &&
                        monster.NetworkId != mainTargetId &&
                        Vector2.Distance(
                            player.Location,
                            monster.Location) <= skillRange)
                    .OrderBy(monster =>
                        Vector2.DistanceSquared(
                            player.Location,
                            monster.Location))
                    .Take(MaxSecondaryTargets);

            foreach (var monster in nearbyMonsters)
            {
                float distance =
                    Vector2.Distance(
                        player.Location,
                        monster.Location);

                if (distance > skillRange)
                {
                    continue;
                }


                ushort secondaryTargetId =
                    monster.NetworkId;


                //
                // Each provider follows its own monster.
                //
                Func<Vector3> targetProvider =
                    () =>
                    {
                        if (context.World.TryGetWalkerById(
                            secondaryTargetId,
                            out var target))
                        {
                            return
                                target.WorldPosition.Translation +
                                Vector3.UnitZ * 80f;
                        }

                        //
                        // If the monster disappears,
                        // collapse the bolt back to
                        // its source.
                        //
                        return SourceProvider();
                    };


                var secondaryEffect =
                new FenrirPlasmaStormEffect(
                    SourceProvider,
                    targetProvider,
                    fenrirIndex,
                    isSecondaryTarget: true);


                //
                // Unlike mainEffect, these aren't returned
                // to SkillVisualEffectRegistry, so we add
                // them ourselves.
                //
                context.World.Objects.Add(
                    secondaryEffect);

                _ = secondaryEffect.Load();


                secondaryCount++;

                if (secondaryCount >=
                    MaxSecondaryTargets)
                {
                    break;
                }
            }


            return mainEffect;
        }


        private static bool IsFenrir(
            short itemIndex)
        {
            return
                itemIndex is
                    >= 11 and <= 18;
        }
    }
}