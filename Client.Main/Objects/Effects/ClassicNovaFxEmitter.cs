#nullable enable
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Source-driven Nova visual pilot (MuMain Season 6):
    /// ZzzCharacter.cpp: PLAYER_SKILL_HELL_BEGIN / START,
    /// ZzzEffect.cpp: CreateForce, MODEL_CIRCLE subtype 1,
    /// Behaviors/MoveHandlers.cpp: Move_MODEL_CIRCLE subtype 1.
    ///
    /// The legacy Nova scene objects keep packet stage, cast/release lifetime,
    /// camera and audio. Only visual particles/joints are emitted here through
    /// ClassicFX shared pools. No separate SpriteBatch/particle geometry.
    /// </summary>
    internal static class ClassicNovaFxEmitter
    {
        // One-switch rollback for Nova ONLY. Set to false to restore the
        // pre-existing visual without deleting the pilot or changing packets.
        internal const bool Enabled = true;

        private static readonly Vector3 ChargeLight = new(0.3f, 0.3f, 1f);
        private static readonly Vector3 Zero = Vector3.Zero;

        internal static bool IsActive(WorldControl? world)
        {
            return Enabled && world?.ClassicFx is { Enabled: true, IsDisposed: false };
        }

        private static bool NewNativeFrame(ClassicFxRuntime fx, ref long lastReferenceFrame)
        {
            long frame = fx.Clock.ReferenceFrame;
            if (frame == lastReferenceFrame)
                return false;
            lastReferenceFrame = frame;
            return true;
        }

        /// <summary>
        /// The Main emits BITMAP_LIGHT subtype 6 on even bones 0..38,
        /// multiplied by m_bySkillCount + 1, and CreateForce creates three
        /// BITMAP_JOINT_HEALING subtype 8 trails at the caster root.
        /// </summary>
        internal static void EmitCharge(
            WorldControl world,
            WalkerObject caster,
            byte stage,
            ref long lastReferenceFrame)
        {
            if (!IsActive(world) || caster.World != world)
                return;

            ClassicFxRuntime fx = world.ClassicFx;
            if (!NewNativeFrame(fx, ref lastReferenceFrame))
                return;

            // MuMain's character animation branch requires a player skeleton.
            // If another WalkerObject invokes Nova, do not invent its bones.
            if (caster is PlayerObject player)
            {
                Matrix[]? bones = player.GetBoneTransforms();
                if (bones != null && bones.Length > 0)
                {
                    Matrix actorWorld = player.WorldPosition;
                    int repeats = Math.Clamp((int)stage + 1, 1, 13);
                    for (int bone = 0; bone < Math.Min(40, bones.Length); bone += 2)
                    {
                        Vector3 point = (bones[bone] * actorWorld).Translation;
                        for (int count = 0; count < repeats; count++)
                        {
                            fx.CreateParticle(
                                ClassicTextureIds.BitmapLight,
                                point,
                                caster.TotalAngle,
                                ChargeLight,
                                subType: 6,
                                scale: 1.3f + stage * 0.08f);
                        }
                    }
                }
            }

            Vector3 root = caster.WorldPosition.Translation;
            ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(caster);
            for (int n = 0; n < 3; n++)
            {
                Vector3 angle = new(
                    fx.Random.Modulo(90),
                    0f,
                    fx.Random.Modulo(360));
                var matrix = ClassicMath.AngleMatrix(angle);
                Vector3 displacement = ClassicMath.VectorRotate(
                    new Vector3(0f, -500f, 0f), matrix);
                Vector3 origin = root - displacement;
                origin.Z += 120f;

                fx.CreateJoint(
                    ClassicTextureIds.BitmapJointHealing,
                    origin,
                    root,
                    angle,
                    subType: 8,
                    target: owner,
                    scale: 10f);
            }
        }

        /// <summary>
        /// Original MODEL_CIRCLE subtype 1: invisible mesh, 45 native ticks,
        /// 36 spirit joints subtype 6 per active tick and, on the threshold
        /// tick, another 36 of subtype 7. Native skill stage controls how long
        /// the active emission window remains open.
        /// </summary>
        internal static void EmitRelease(
            WorldControl world,
            WorldObject circle,
            WalkerObject caster,
            byte stage,
            float lifeBeforeUpdate,
            ref long lastReferenceFrame)
        {
            if (!IsActive(world) || caster.World != world || circle.World != world)
                return;

            ClassicFxRuntime fx = world.ClassicFx;
            if (!NewNativeFrame(fx, ref lastReferenceFrame))
                return;

            int level = Math.Clamp((int)stage, 0, 12);
            if (lifeBeforeUpdate <= 44f - level)
                return;

            Vector3 origin = caster.WorldPosition.Translation + new Vector3(0f, 0f, 100f);
            ClassicFxOwner circleOwner = ClassicFxOwner.FromWorldObject(circle);
            bool threshold = (int)lifeBeforeUpdate == 45 - level;
            for (int i = 0; i < 36; i++)
            {
                Vector3 angle = new(-10f, 0f, i * (10f + fx.Random.Modulo(10)));
                fx.CreateJoint(
                    ClassicTextureIds.BitmapJointSpirit,
                    origin,
                    origin,
                    angle,
                    subType: 6,
                    target: circleOwner,
                    scale: 60f);

                if (threshold)
                {
                    angle.Z = i * 10f;
                    fx.CreateJoint(
                        ClassicTextureIds.BitmapJointSpirit,
                        origin,
                        origin,
                        angle,
                        subType: 7,
                        target: circleOwner,
                        scale: 60f);
                }
            }
        }
    }
}
