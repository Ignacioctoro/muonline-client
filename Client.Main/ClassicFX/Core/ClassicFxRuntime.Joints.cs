using System;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly ClassicJoint[] _joints =
            new ClassicJoint[ClassicFxPools.MaxJoints];

        public int ActiveJointCount => Pools.Joints.ActiveCount;

        /// <summary>
        /// Common slot allocation for MuMain CreateJoint(). This does NOT
        /// claim to initialize every joint Type/SubType: the native switch,
        /// MoveJoints and RenderJoints have not been connected yet.
        /// </summary>
        public ClassicFxHandle CreateJoint(
            int type,
            Vector3 position,
            Vector3 targetPosition,
            Vector3 angle,
            int subType = 0,
            ClassicFxOwner target = default,
            float scale = 10f,
            short pkKey = -1,
            ushort skillIndex = 0,
            ushort skillSerialNum = 0,
            int characterIndex = -1,
            Vector3? priorColor = null,
            short targetIndex = -1)
        {
            if (_disposed || !Enabled ||
                !Pools.Joints.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            ref ClassicJoint joint = ref _joints[handle.Index];
            Vector3 initialLight = priorColor ?? Vector3.One;
            joint.InitializeCommon(
                type, position, targetPosition, angle, subType,
                target, scale, pkKey, skillIndex, skillSerialNum,
                characterIndex, initialLight, targetIndex);

            // Only families with complete native creation rules are
            // accepted. Unported ones must not consume an invisible slot.
            bool initialized =
                InitializeJointCreateP(
                    ref joint, targetPosition, scale,
                    priorColor.HasValue, characterIndex) ||
                InitializeJointCreateQ(
                    handle, ref joint, targetPosition, scale) ||
                InitializeJointCreateR(ref joint, scale);

            if (!initialized)
            {
                ReleaseJoint(handle);
                return ClassicFxHandle.Invalid;
            }

            // Native CreateJoint() applies this after every Type branch.
            // Avoid division by zero before the first MonoGame Update().
            float frameFactor = Clock.FrameFactor > 0f ? Clock.FrameFactor : 1f;
            joint.MaxTails = Math.Clamp(
                (int)(joint.MaxTails / frameFactor),
                0, ClassicJoint.MaxTailSegments);
            return handle;
        }

        public ClassicFxHandle CreateJointFpsChecked(
            int type,
            Vector3 position,
            Vector3 targetPosition,
            Vector3 angle,
            int subType = 0,
            ClassicFxOwner target = default,
            float scale = 10f,
            short pkKey = -1,
            ushort skillIndex = 0,
            ushort skillSerialNum = 0,
            int characterIndex = -1,
            Vector3? priorColor = null,
            short targetIndex = -1)
        {
            if (!Random.FpsCheck(1, Clock))
                return ClassicFxHandle.Invalid;
            return CreateJoint(type, position, targetPosition, angle,
                subType, target, scale, pkKey, skillIndex,
                skillSerialNum, characterIndex, priorColor, targetIndex);
        }

        public bool TryGetJoint(
            ClassicFxHandle handle,
            out ClassicJoint joint)
        {
            if (handle.Kind != ClassicFxPoolKind.Joint ||
                !Pools.Joints.IsAlive(handle))
            {
                joint = default;
                return false;
            }
            joint = _joints[handle.Index];
            return true;
        }

        public bool ReleaseJoint(ClassicFxHandle handle)
        {
            if (handle.Kind != ClassicFxPoolKind.Joint ||
                !Pools.Joints.IsAlive(handle))
                return false;

            _joints[handle.Index].Clear();
            return Pools.Joints.Release(handle);
        }

        private void ClearJointStorage()
        {
            // Pool has already been cleared. Release owner references,
            // but retain fixed tail storage for slots within this runtime.
            for (int i = 0; i < _joints.Length; i++)
                _joints[i].Clear();
        }
    }
}
