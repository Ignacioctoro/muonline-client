using System;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// HEALING/9: retain the original 20 reference-frame samples (~0.8 s),
        /// while moving the head smoothly at the display frame rate. This keeps
        /// the orbit, UV fade and flare falloff in native segment units.
        /// No additional particles, wider sprites or alternate texture are used.
        /// </summary>
        private static void AppendAncientSetOrbitTail(ref ClassicJoint joint,
            in ClassicMatrix3x4 matrix, float factor)
        {
            if (joint.Tails == null || joint.MaxTails <= 0 || factor <= 0f) return;
            joint.NativeTailFramePhase += factor;
            if (joint.NativeTailFramePhase >= 1f - 0.00001f)
            {
                joint.NativeTailFramePhase = MathF.Max(0f, joint.NativeTailFramePhase - 1f);
                // The shared writer shifts history and adds the new head.
                AppendJointTailMoveA(ref joint, matrix);
                return;
            }

            // Only refresh the leading cross-section between native ticks.
            // Shifting every display frame would shorten the circular trail.
            float half = joint.Scale * 0.5f;
            joint.Tails[0] = joint.Position + ClassicMath.VectorRotate(new Vector3(-half, 0f, 0f), matrix);
            joint.Tails[1] = joint.Position + ClassicMath.VectorRotate(new Vector3(half, 0f, 0f), matrix);
            joint.Tails[2] = joint.Position + ClassicMath.VectorRotate(new Vector3(0f, 0f, -half), matrix);
            joint.Tails[3] = joint.Position + ClassicMath.VectorRotate(new Vector3(0f, 0f, half), matrix);
        }
    }
}
