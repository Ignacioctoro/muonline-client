// Present server-authoritative hit/death feedback at the ground-contact key.
// MuMain ClassAttack.cpp sends 0x1E when casting starts, not at impact.
// This service does not delay or modify network packets or server damage.
using System;
using System.Collections.Generic;
using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Scenes
{
    internal sealed class GroundImpactPresentation
    {
        private const double MaximumImpactWaitMs = 2200.0;
        private const double CastRetentionMs = 2600.0;
        private readonly List<GroundCast> _casts = new();

        private sealed class GroundCast
        {
            public WorldControl World;
            public PlayerObject Caster;
            public ushort SkillId;
            public int Action;
            public int ImpactFrame;
            public Vector2 TargetTile;
            public double StartedMs;
            public bool ImpactPresented;
            public readonly List<Action> Feedback = new();
        }

        // BMD animation keys rather than guessed millisecond sleep durations.
        // Earthshake and Gigantic Storm: ZzzCharacter.cpp AttackStage frames 5/7.
        private static int GetContactKey(ushort skillId) => skillId switch
        {
            42 or 331 or 333 => 5, // Rageful Blow
            232 or 337 => 6,       // Strike of Destruction
            62 => 5,               // Dark Horse Earthshake
            237 => 7,              // Gigantic Storm
            _ => -1
        };

        public void RegisterCast(WorldControl world, PlayerObject caster,
            ushort skillId, Vector2 targetTile)
        {
            int contactKey = GetContactKey(skillId);
            if (contactKey < 0 || world == null || caster == null ||
                !ReferenceEquals(world, caster.World))
                return;

            double now = NowMs();
            for (int i = _casts.Count - 1; i >= 0; i--)
            {
                GroundCast prior = _casts[i];
                if (!ReferenceEquals(prior.World, world) ||
                    !ReferenceEquals(prior.Caster, caster))
                    continue;

                if (prior.SkillId == skillId && now - prior.StartedMs < 120)
                    return; // duplicated network echo

                Present(prior);
                _casts.RemoveAt(i);
            }

            int actionIndex = caster.CurrentAction;
            var actions = caster.Model?.Actions;
            if (actions != null && actionIndex >= 0 &&
                actionIndex < actions.Length && actions[actionIndex] != null)
            {
                contactKey = Math.Min(contactKey,
                    Math.Max(0, actions[actionIndex].NumAnimationKeys - 2));
            }

            _casts.Add(new GroundCast
            {
                World = world,
                Caster = caster,
                SkillId = skillId,
                Action = actionIndex,
                ImpactFrame = contactKey,
                TargetTile = targetTile,
                StartedMs = now
            });
        }

        public bool IsLocalCastAwaitingEcho(PlayerObject hero, ushort skillId)
        {
            double now = NowMs();
            for (int i = _casts.Count - 1; i >= 0; i--)
            {
                GroundCast cast = _casts[i];
                if (ReferenceEquals(cast.Caster, hero) &&
                    cast.SkillId == skillId && hero.CurrentAction == cast.Action &&
                    now - cast.StartedMs <= MaximumImpactWaitMs)
                    return true;
            }
            return false;
        }

        public bool TryDeferHit(WorldControl world, MonsterObject monster, Action callback)
            => TryDefer(world, monster, null, callback);

        public bool TryDeferDeath(WorldControl world, MonsterObject monster,
            ushort killerId, Action callback)
            => TryDefer(world, monster, (ushort)(killerId & 0x7FFF), callback);

        private bool TryDefer(WorldControl world, MonsterObject monster,
            ushort? killerId, Action callback)
        {
            if (world == null || monster == null || callback == null ||
                !ReferenceEquals(monster.World, world))
                return false;

            double now = NowMs();
            for (int i = _casts.Count - 1; i >= 0; i--)
            {
                GroundCast cast = _casts[i];
                if (!ReferenceEquals(cast.World, world) ||
                    now - cast.StartedMs < 0 ||
                    now - cast.StartedMs > MaximumImpactWaitMs)
                    continue;

                if (killerId.HasValue)
                {
                    if ((cast.Caster.NetworkId & 0x7FFF) != killerId.Value)
                        continue;
                }
                else
                {
                    // 0x11 contains no attacker: limit correlation to nearby
                    // monsters. Other fights and player HP updates stay immediate.
                    float radius = cast.SkillId == 237 ? 8f : 6f;
                    float radiusSq = radius * radius;
                    if (Vector2.DistanceSquared(monster.Location, cast.TargetTile) > radiusSq &&
                        Vector2.DistanceSquared(monster.Location, cast.Caster.Location) > radiusSq)
                        continue;
                }

                if (HasReachedContact(cast, now))
                    return false;

                cast.Feedback.Add(callback);
                return true;
            }
            return false;
        }

        // Called on the main game thread, after the world's animation update.
        public void Update(WorldControl world)
        {
            double now = NowMs();
            for (int i = _casts.Count - 1; i >= 0; i--)
            {
                GroundCast cast = _casts[i];
                if (!ReferenceEquals(cast.World, world))
                {
                    _casts.RemoveAt(i); // no callbacks in a disposed map
                    continue;
                }

                if (!cast.ImpactPresented && HasReachedContact(cast, now))
                    Present(cast);

                if (now - cast.StartedMs > CastRetentionMs)
                {
                    if (!cast.ImpactPresented)
                        Present(cast);
                    _casts.RemoveAt(i);
                }
            }
        }

        private static bool HasReachedContact(GroundCast cast, double now)
        {
            if (now - cast.StartedMs >= MaximumImpactWaitMs ||
                cast.Caster.Status == GameControlStatus.Disposed ||
                cast.Caster.IsDead ||
                cast.Caster.CurrentAction != cast.Action)
                return true;

            // Allow at least one animation update following PlayAction.
            return now - cast.StartedMs >= 35 &&
                cast.Caster.CurrentFrame >= cast.ImpactFrame;
        }

        private static void Present(GroundCast cast)
        {
            if (cast.ImpactPresented)
                return;

            cast.ImpactPresented = true;
            foreach (Action callback in cast.Feedback)
                callback();
            cast.Feedback.Clear();
        }

        private static double NowMs() =>
            MuGame.Instance?.GameTime?.TotalGameTime.TotalMilliseconds
            ?? Environment.TickCount64;
    }
}
