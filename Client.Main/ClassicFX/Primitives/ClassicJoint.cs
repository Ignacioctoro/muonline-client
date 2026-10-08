using System;
using Client.Main.ClassicFX.Core;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Primitives
{
    /// <summary>
    /// Storage for the original JOINT in MuMain/_struct.h.
    /// Only the common CreateJoint fields are initialised at this stage.
    /// The per-Type creation, movement and rendering switches come next.
    /// </summary>
    public struct ClassicJoint
    {
        public const int MaxTailSegments = 200;
        public const int VerticesPerTail = 4;

        public int Type;
        public int TexType;
        public int SubType;
        public byte RenderType;
        public byte RenderFace;
        public float Scale;
        public Vector3 Position;
        public Vector3 StartPosition;
        public Vector3 Angle;
        public Vector3 HeadAngle;
        public Vector3 Light;
        public ClassicFxOwner Target;
        public Vector3 TargetPosition;
        public byte OnlyOneRender;
        public float LifeTime;
        public bool Collision;
        public float Velocity;
        public Vector3 Direction;
        public short PKKey;
        public ushort Skill;
        public float Weapon;
        public float MultiUse;
        public bool TileMapping;
        public byte ReverseUv;
        public byte SkillSerialNum;
        public int CharacterIndex;
        public short TargetIndexRef;
        public bool CreateTails;
        public int NumTails;
        public int MaxTails;

        // Flattened vec3_t Tails[MAX_TAILS][4]; one reusable array per
        // activated slot, avoiding per-frame arrays and 200 small objects.
        public Vector3[] Tails;
        public int[] TargetIndices;

        public void InitializeCommon(
            int type,
            in Vector3 position,
            in Vector3 targetPosition,
            in Vector3 angle,
            int subType,
            in ClassicFxOwner target,
            float scale,
            short pkKey,
            ushort skillIndex,
            ushort skillSerial,
            int characterIndex,
            in Vector3 light,
            short targetIndexRef)
        {
            // Preserve reusable per-slot storage across joint reuse.
            Vector3[] tails = Tails;
            int[] targetIndices = TargetIndices;
            this = default;
            Tails = tails ?? new Vector3[MaxTailSegments * VerticesPerTail];
            TargetIndices = targetIndices ?? new int[5];
            Array.Clear(Tails, 0, Tails.Length); // original memset(Tails, 0)
            Array.Clear(TargetIndices, 0, TargetIndices.Length);

            Type = type;
            TexType = type;
            SubType = subType;
            RenderType = 1; // RENDER_TYPE_ALPHA_BLEND
            RenderFace = 0x03; // RENDER_FACE_ONE | RENDER_FACE_TWO
            Scale = scale;
            Position = position;
            Angle = angle;
            Light = light;
            Target = target;
            TargetPosition = target.HasOwner ? Vector3.Zero : targetPosition;
            Direction = Vector3.Zero;
            PKKey = pkKey;
            Skill = skillIndex;
            SkillSerialNum = (byte)skillSerial;
            CharacterIndex = characterIndex;
            TargetIndexRef = targetIndexRef;
            CreateTails = true;
            NumTails = 0;
            // These are set by the original Type/SubType initialization switch:
            // LifeTime, MaxTails, Velocity, StartPosition and special TargetPosition.
        }

        /// <summary>
        /// Equivalent of the default CreateJoint initial tail quad.
        /// Must only be called by the Type initializer if its source branch
        /// sets bCreateStartTail; several original types skip the first tail.
        /// </summary>
        public void InitializeFirstTail()
        {
            if (Tails == null || Tails.Length < VerticesPerTail)
                return;

            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(Angle);
            float halfScale = Scale * 0.5f;
            Tails[0] = Position + ClassicMath.VectorRotate(
                new Vector3(-halfScale, 0f, 0f), matrix);
            Tails[1] = Position + ClassicMath.VectorRotate(
                new Vector3(halfScale, 0f, 0f), matrix);
            Tails[2] = Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, -halfScale), matrix);
            Tails[3] = Position + ClassicMath.VectorRotate(
                new Vector3(0f, 0f, halfScale), matrix);
        }

        public void Clear()
        {
            // Keep arrays to avoid subsequent allocations when reusing slot.
            Vector3[] tails = Tails;
            int[] indices = TargetIndices;
            this = default;
            Tails = tails;
            TargetIndices = indices;
        }
    }
}
