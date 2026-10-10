// MODEL_ALICE_DRAIN_LIFE, independent Effect-pool primitive.
// Native OBJECT->Owner->Owner is modeled explicitly as source + target;
// never mutate a character's Owner or invent a BMD for a particle-only effect.
// MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// Behaviors/MoveHandlers.cpp Move_MODEL_ALICE_DRAIN_LIFE.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        public ClassicFxHandle CreateDrainLife(
            WorldObject source, WorldObject target, Vector3 angle)
        {
            if (_disposed || !Enabled || source == null || target == null ||
                !ReferenceEquals(source.World, World) ||
                !ReferenceEquals(target.World, World) ||
                source.Status != GameControlStatus.Ready ||
                target.Status != GameControlStatus.Ready ||
                !Pools.Effects.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            _effects[handle.Index] = new EffectState
            {
                Type = ClassicFxEffectType.AliceDrainLife,
                SubType = 0,
                Owner = ClassicFxOwner.FromWorldObject(source),
                TargetWorldObject = target,
                Position = source.WorldPosition.Translation,
                Angle = angle,
                Light = Vector3.One,
                Scale = 1f,
                Alpha = 1f,
                LifeTime = 70f,
                LastChildNativeTick = -1
            };
            return handle;
        }

        private bool MoveAliceDrainLife(ref EffectState e)
        {
            WorldObject source = e.Owner.WorldObject;
            WorldObject target = e.TargetWorldObject;
            if (source == null || target == null ||
                !ReferenceEquals(source.World, World) ||
                !ReferenceEquals(target.World, World) ||
                source.Status != GameControlStatus.Ready ||
                target.Status != GameControlStatus.Ready)
                return false;

            // The original checks CreateParticleFpsChecked on each 25-FPS
            // update. Do not emit it 2-5 times at 60-120 Hz.
            if (!Clock.AdvancedReferenceFrame)
                return true;

            Vector3 sourcePos = source.WorldPosition.Translation;
            Vector3 targetPos = target.WorldPosition.Translation;
            e.Position = sourcePos;
            int roll = Random.Modulo(10);
            int count = roll == 0 ? 0 : roll <= 7 ? 1 : 2;
            Vector3 glow = new Vector3(1f, 0.2f, 0.2f);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Modulo(80) - 40,
                    Random.Modulo(60) - 30,
                    80f + Random.Modulo(180) - 100f);
                CreateParticle(ClassicTextureIds.BitmapLight + 2,
                    sourcePos + offset, e.Angle, glow, 7, 1.8f);
                if (e.LifeTime <= 60f)
                {
                    offset = new Vector3(
                        Random.Modulo(80) - 40,
                        Random.Modulo(60) - 30,
                        80f + Random.Modulo(180) - 100f);
                    CreateParticle(ClassicTextureIds.BitmapLight + 2,
                        targetPos + offset, e.Angle, glow, 7, 1.8f);
                }
            }

            if (e.LifeTime >= 66f)
            {
                // Native drain-ghost spokes between caster bone 18 and
                // target body; single native tick batches.
                int n = Random.Modulo(10);
                int spokes = n == 0 ? 0 : n <= 3 ? 1 :
                    n <= 5 ? 3 : n <= 8 ? 3 : 4;
                for (int i = 0; i < spokes; i++)
                {
                    if (!TryGetOwnerBonePosition(e.Owner, 18,
                        out Vector3 bonePos))
                        break;
                    float yaw = e.Angle.Z;
                    Vector3 direction = new Vector3(
                        -MathF.Sin(yaw), MathF.Cos(yaw), 0f);
                    bonePos += direction * 100f + new Vector3(
                        Random.Modulo(10) * 5f,
                        Random.Modulo(10) * 5f,
                        Random.Modulo(10) * 5f);
                    Vector3 dest = targetPos +
                        new Vector3(0f, 0f,
                            100f + (Random.Modulo(10) - 5f) * 4f);
                    CreateJoint(ClassicTextureIds.BitmapDrainLifeGhost,
                        bonePos, dest, e.Angle, 0, e.Owner, 40f,
                        priorColor: new Vector3(0.8f, 0.1f, 0.2f));
                }
            }

            // Source code triggers the all-bones energy pull when lifetime
            // first reaches 64, not on every high-FPS frame at int(64).
            if (e.LifeTime <= 64f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                if (target is ModelObject model)
                {
                    Matrix[] bones = model.GetBoneTransforms();
                    if (bones != null)
                    {
                        Vector3 destination = sourcePos +
                            new Vector3(0f, 0f, 80f);
                        for (int i = 0; i < bones.Length; i++)
                        {
                            if (Random.Modulo(2) != 0 ||
                                !TryGetOwnerBonePosition(
                                    ClassicFxOwner.FromWorldObject(target),
                                    i, out Vector3 bonePos))
                                continue;
                            CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                                bonePos, destination, e.Angle,
                                subType: 45, target: e.Owner, scale: 10f,
                                priorColor: new Vector3(1f, 0f, 0.1f));
                        }
                    }
                }
            }
            return true;
        }
    }
}
