using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly ClassicJoint[] _joints =
            new ClassicJoint[ClassicFxPools.MaxJoints];

        /// <summary>
        /// MuMain RenderJoints(): draw the world-space crossed tail geometry.
        /// Separate entrypoint so passes 1/2 can later follow water rendering.
        /// The native render pass also emits sprites; they are queued before
        /// WorldControl.RenderSprites() in this integration.
        /// </summary>
        public void RenderJoints(byte renderPass = 0)
        {
            if (_disposed || !Enabled || _jointRenderer == null)
                return;

            _jointRenderer.Begin();
            for (int i = 0; i < _joints.Length; i++)
            {
                if (!Pools.Joints.IsActive(i))
                    continue;
                ref ClassicJoint joint = ref _joints[i];
                if (joint.NumTails <= 0 || joint.RenderFace == 0)
                    continue;
                if (renderPass == 1 && joint.OnlyOneRender == 2 ||
                    renderPass == 2 && joint.OnlyOneRender == 1)
                    continue;

                _jointRenderer.QueueJoint(ref joint, renderPass,
                    (float)Clock.WorldTimeMilliseconds, Clock.FrameFactor);
                EmitJointRenderSprites(ref joint);

                // Native RenderTerrainAlphaBitmap(BITMAP_MAGIC+1) on
                // Thunder+1/6. A world/terrain integration can subscribe;
                // no invented terrain texture or geometry is substituted.
                if (joint.Type == ClassicTextureIds.BitmapJointThunder + 1 &&
                    joint.SubType == 6)
                {
                    joint.Velocity *= MathF.Pow(1f / 1.1f, Clock.FrameFactor);
                    JointThunderTerrainFlashRequested?.Invoke(
                        joint.TargetPosition, new Vector3(joint.Velocity));
                }
            }
            _jointRenderer.End();
        }

        public event Action<Vector3, Vector3> JointThunderTerrainFlashRequested;

        // Native render-side Sprite emission for original joint variants.
        private void EmitJointRenderSprites(ref ClassicJoint joint)
        {
            int count = Math.Min(joint.NumTails,
                ClassicJoint.MaxTailSegments - 1);
            if (joint.Tails == null || count <= 0)
                return;

            if (joint.Type == ClassicTextureIds.BitmapFlare + 1 &&
                (joint.SubType == 6 || joint.SubType == 8))
            {
                Vector3 mid = TailQuadCenter(ref joint, 0);
                if (joint.SubType == 6)
                {
                    CreateSprite(ClassicTextureIds.BitmapFlareBlue,
                        mid, 0.5f, joint.Light, rotation: R(360), subType: 3);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        mid, 1.5f, joint.Light, rotation: R(360), subType: 3);
                }
                else
                {
                    CreateSprite(ClassicTextureIds.BitmapFlareBlue,
                        mid, 0.3f, joint.Light, rotation: R(360), subType: 3);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        mid, 1f, joint.Light, rotation: R(360), subType: 3);
                }
            }
            else if (joint.Type == ClassicModelSpearSkill &&
                (joint.SubType == 0 || joint.SubType == 4 || joint.SubType == 9) &&
                joint.Target.HasOwner)
            {
                int seg = Math.Min(count - 1, count / 2);
                Vector3 light = joint.Light;
                if (TryGetOwnerPosition(joint.Target, out Vector3 position))
                {
                    float baseZ = joint.SubType == 9 ? 180f : 50f;
                    float height = (joint.Tails[seg * 4].Z -
                        (position.Z + baseZ)) * 0.01f;
                    if (height > 0f) light -= new Vector3(height);
                }
                CreateSprite(ClassicTextureIds.BitmapFlareBlue,
                    TailQuadCenter(ref joint, seg), 0.7f, light);
            }
            else if (joint.Type == ClassicTextureIds.BitmapJointHealing &&
                (joint.SubType == 9 || joint.SubType == 10) &&
                joint.Target.HasOwner)
            {
                float scale = joint.SubType == 9 ? 0.5f : 0.7f;
                for (int seg = 0; seg < count - 1; seg++)
                {
                    Vector3 light = joint.Light - new Vector3(seg * 0.01f);
                    CreateSprite(ClassicTextureIds.BitmapFlareBlue,
                        TailQuadCenter(ref joint, seg), scale, light);
                }
            }
        }

        private static Vector3 TailQuadCenter(ref ClassicJoint joint, int segment)
        {
            int i = segment * ClassicJoint.VerticesPerTail;
            return (joint.Tails[i] + joint.Tails[i + 1] +
                joint.Tails[i + 2] + joint.Tails[i + 3]) * 0.25f;
        }

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
                InitializeJointCreateR(ref joint, scale) ||
                InitializeJointCreateS(ref joint, targetPosition, scale,
                    priorColor, pkKey) ||
                InitializeJointCreateT(ref joint, targetPosition, scale,
                    priorColor) ||
                InitializeJointCreateU(ref joint, targetPosition, scale) ||
                InitializeJointCreateV(ref joint, targetPosition, scale,
                    pkKey, priorColor) ||
                InitializeJointCreateW(ref joint, scale) ||
                InitializeJointCreateX(ref joint, scale, angle) ||
                InitializeJointCreateY(ref joint, targetPosition, scale);

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
