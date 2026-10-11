// ClassicFX Season 6 Batch 35 — genuine BMD mirror and model-free emitters.
// MuMain fixed reference: 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzOpenData.cpp + Behaviors/MoveHandlers.cpp, EffectTypes.json.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Bridge to the actual Season 6 eBuff_Def_up_Ourforces state.
        /// Without it WindForceMirror is rejected; it must never persist
        /// based only on an invented timer or a visually plausible guess.
        /// </summary>
        public Func<ClassicFxOwner, bool> Season6DefenseUpOurForcesActive { get; set; }

        private static bool IsS6Batch35ModelType(ClassicFxEffectType type) =>
            type == ClassicFxEffectType.WindForceMirror;

        private static bool IsS6Batch35LogicalType(ClassicFxEffectType type, int subType) =>
            (type == ClassicFxEffectType.ChainLightning && subType is >= 0 and <= 2) ||
            (type == ClassicFxEffectType.TargetMonEffect && subType is 0 or 1);

        private static bool TryGetS6Batch35ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (type != ClassicFxEffectType.WindForceMirror || subType != 0)
                return false;
            // MuMain: MODEL_WINDFOCE_MIRROR -> Data/Effect/wind_foce_mirror.bmd
            definition = new Season6ModelDefinition(
                "Effect/wind_foce_mirror.bmd", 50f, 1f,
                needsOwner: true, useCallerScale: true);
            return true;
        }

        private bool InitializeS6Batch35Model(ClassicFxOwner owner)
        {
            return owner.WorldObject != null &&
                ReferenceEquals(owner.WorldObject.World, World) &&
                Season6DefenseUpOurForcesActive != null &&
                Season6DefenseUpOurForcesActive(owner);
        }

        private bool MoveS6Batch35Model(ref EffectState e, float frameFactor)
        {
            var owner = e.Owner.WorldObject;
            if (owner == null || !ReferenceEquals(owner.World, World) ||
                owner.Status != GameControlStatus.Ready ||
                Season6DefenseUpOurForcesActive == null ||
                !Season6DefenseUpOurForcesActive(e.Owner))
                return false;
            // Native: angle.z = int(WorldTime)*0.4,
            // Scale *= 1.015, max 1.5, light *= 0.9, follow owner.
            e.Angle.Z = MathHelper.ToRadians(
                (int)Clock.WorldTimeMilliseconds * 0.4f);
            e.Scale = MathF.Min(1.5f,
                e.Scale * MathF.Pow(1.015f, frameFactor));
            e.Light *= MathF.Pow(0.9f, frameFactor);
            e.Position = owner.WorldPosition.Translation;
            return true;
        }

        private bool MoveS6Batch35Logical(ref EffectState e)
        {
            var source = e.Owner.WorldObject;
            if (source is not ModelObject model ||
                !ReferenceEquals(source.World, World) ||
                source.Status != GameControlStatus.Ready)
                return false;

            if (e.Type == ClassicFxEffectType.ChainLightning)
                return MoveS6Batch35ChainLightning(ref e, model);
            return MoveS6Batch35TargetMon(ref e, model);
        }

        private bool MoveS6Batch35ChainLightning(
            ref EffectState e, ModelObject source)
        {
            var target = e.TargetWorldObject;
            if (target == null ||
                !ReferenceEquals(target.World, World) ||
                target.Status != GameControlStatus.Ready)
                return false;

            // Native Move_MODEL_CHAIN_LIGHTNING subtypes 1/2 skip
            // their entire emission path when source and target are the same.
            if ((e.SubType == 1 || e.SubType == 2) &&
                ReferenceEquals(source, target))
                return true;

            if (!Clock.AdvancedReferenceFrame)
                return true;
            ClassicFxOwner targetOwner = ClassicFxOwner.FromWorldObject(target);
            Vector3 targetPosition = target.WorldPosition.Translation;
            Vector3 blue = new Vector3(0.4f, 0.4f, 1f);
            if (e.SubType == 0)
            {
                // Original source bones: 37 and 28, two angles and two widths.
                if (TryGetOwnerBonePosition(e.Owner, 37, out Vector3 left))
                    EmitS6Batch35ThunderPair(left, targetPosition,
                        source.Angle.Z, -60f, 60f, targetOwner, blue);
                if (TryGetOwnerBonePosition(e.Owner, 28, out Vector3 right))
                    EmitS6Batch35ThunderPair(right, targetPosition,
                        source.Angle.Z, -60f, -60f, targetOwner, blue);
            }
            else
            {
                Vector3 start = source.WorldPosition.Translation;
                start.Z += 80f;
                // Main subtype 1/2 attaches two joints without BMD drawing.
                CreateJoint(ClassicTextureIds.BitmapJointThunder,
                    start, targetPosition, target.Angle, 0,
                    targetOwner, 50f, priorColor: blue);
                CreateJoint(ClassicTextureIds.BitmapJointThunder,
                    start, targetPosition, target.Angle, 0,
                    targetOwner, 10f, priorColor: blue);
            }

            // Native one-time target-bone flash when life reaches tick 15.
            // Keep allocations and the joint/particle burst bounded on Android.
            if (e.LifeTime <= 15f && (e.TriggerMask & 1) == 0)
            {
                e.TriggerMask |= 1;
                if (target is ModelObject targetModel)
                {
                    Matrix[] bones = targetModel.GetBoneTransforms();
                    if (bones != null && bones.Length > 0)
                    {
                        int sampled = Math.Min(bones.Length, 24);
                        for (int i = 0; i < sampled; i++)
                        {
                            int bone = i * bones.Length / sampled;
                            if (!TryGetOwnerBonePosition(targetOwner,
                                    bone, out Vector3 p))
                                continue;
                            float scale = 3f + (Random.Modulo(20) - 10) * 0.1f;
                            CreateParticle(ClassicTextureIds.BitmapLight,
                                p, e.Angle, new Vector3(0.2f, 0.2f, 0.8f),
                                5, scale);
                        }
                    }
                }
            }
            return true;
        }

        private void EmitS6Batch35ThunderPair(
            Vector3 start, Vector3 end, float yaw,
            float xAngleDeg, float yawOffsetDeg,
            ClassicFxOwner target, Vector3 light)
        {
            Vector3 a = new Vector3(MathHelper.ToRadians(xAngleDeg), 0f, yaw);
            Vector3 b = new Vector3(0f, 0f,
                yaw + MathHelper.ToRadians(yawOffsetDeg));
            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                start, end, a, 0, target, 50f, priorColor: light);
            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                start, end, a, 0, target, 10f, priorColor: light);
            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                start, end, b, 0, target, 50f, priorColor: light);
            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                start, end, b, 0, target, 10f, priorColor: light);
        }

        private bool MoveS6Batch35TargetMon(
            ref EffectState e, ModelObject source)
        {
            if (e.SubType == 1 &&
                (e.TargetWorldObject == null ||
                 !ReferenceEquals(e.TargetWorldObject.World, World)))
                return true; // Native subtype 1 waits for a valid target.

            if (!Clock.AdvancedReferenceFrame)
                return true;
            Matrix[] bones = source.GetBoneTransforms();
            if (bones == null || bones.Length <= 1)
                return true;
            // MuMain chooses rand() % (NumBones - 1) every native tick.
            int bone = Random.Modulo(bones.Length - 1);
            if (!TryGetOwnerBonePosition(e.Owner, bone, out Vector3 at))
                return true;
            e.Position = at;
            float particleScale = e.Scale *
                (Random.Modulo(5) + 5) * 0.1f;
            for (int i = 0; i < 3; i++)
            {
                int texture = Random.Modulo(3);
                if (texture == 0)
                    CreateParticle(ClassicTextureIds.BitmapFireHik1,
                        at, e.Angle, Vector3.One, 0, particleScale);
                else if (texture == 1)
                    CreateParticle(ClassicTextureIds.BitmapFireCursedLich,
                        at, e.Angle, Vector3.One, 4, particleScale);
                else
                    CreateParticle(ClassicTextureIds.BitmapFireHik3,
                        at, e.Angle, Vector3.One, 0, particleScale);
            }
            return true;
        }
    }
}
