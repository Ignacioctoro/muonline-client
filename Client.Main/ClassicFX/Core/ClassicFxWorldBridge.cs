using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public readonly struct ClassicFxOwnerSnapshot
    {
        public Vector3 Position { get; }
        public Vector3 Angle { get; }
        public Vector3 Light { get; }
        public float Scale { get; }
        public WorldObject WorldObject { get; }
        public bool HasWorldObject => WorldObject != null;

        public ClassicFxOwnerSnapshot(
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            float scale,
            WorldObject worldObject)
        {
            Position = position;
            Angle = angle;
            Light = light;
            Scale = scale;
            WorldObject = worldObject;
        }
    }

    /// <summary>
    /// Bridge compartido OBJECT* / bones / terreno para ClassicFX.
    /// </summary>
    public sealed class ClassicFxWorldBridge
    {
        private readonly ClassicFxRuntime _runtime;

        public ClassicFxWorldBridge(ClassicFxRuntime runtime)
        {
            _runtime = runtime ?? throw new System.ArgumentNullException(nameof(runtime));
        }

        public bool TryGetOwnerSnapshot(
            in ClassicFxOwner owner,
            out ClassicFxOwnerSnapshot snapshot)
        {
            switch (owner.Kind)
            {
                case ClassicFxOwnerKind.WorldObject:
                {
                    WorldObject worldObject = owner.WorldObject;
                    if (!IsWorldObjectUsable(worldObject))
                    {
                        snapshot = default;
                        return false;
                    }

                    snapshot = new ClassicFxOwnerSnapshot(
                        worldObject.WorldPosition.Translation,
                        worldObject.TotalAngle,
                        worldObject.Light,
                        worldObject.TotalScale,
                        worldObject);

                    return true;
                }

                case ClassicFxOwnerKind.ClassicFx:
                    return TryGetClassicFxOwnerSnapshot(owner.FxHandle, out snapshot);

                default:
                    snapshot = default;
                    return false;
            }
        }

        public bool TryGetOwnerPosition(
            in ClassicFxOwner owner,
            out Vector3 position)
        {
            if (TryGetOwnerSnapshot(owner, out ClassicFxOwnerSnapshot snapshot))
            {
                position = snapshot.Position;
                return true;
            }

            position = Vector3.Zero;
            return false;
        }

        public bool TryGetOwnerWorldObject(
            in ClassicFxOwner owner,
            out WorldObject worldObject)
        {
            if (owner.Kind == ClassicFxOwnerKind.WorldObject &&
                IsWorldObjectUsable(owner.WorldObject))
            {
                worldObject = owner.WorldObject;
                return true;
            }

            worldObject = null;
            return false;
        }

        public bool TryGetOwnerBoneWorldMatrix(
            in ClassicFxOwner owner,
            int boneIndex,
            out Matrix worldMatrix)
        {
            if (boneIndex < 0 ||
                !TryGetOwnerWorldObject(owner, out WorldObject worldObject) ||
                worldObject is not ModelObject modelObject)
            {
                worldMatrix = Matrix.Identity;
                return false;
            }

            Matrix[] boneTransforms = modelObject.GetBoneTransforms();

            if (boneTransforms == null ||
                (uint)boneIndex >= (uint)boneTransforms.Length)
            {
                worldMatrix = Matrix.Identity;
                return false;
            }

            // Misma fórmula que PlayerObject.TryGetBoneWorldMatrix().
            worldMatrix =
                boneTransforms[boneIndex] *
                modelObject.WorldPosition;

            return true;
        }

        public bool TryTransformOwnerBonePosition(
            in ClassicFxOwner owner,
            int boneIndex,
            in Vector3 localPosition,
            out Vector3 worldPosition)
        {
            if (!TryGetOwnerBoneWorldMatrix(owner, boneIndex, out Matrix boneWorld))
            {
                worldPosition = Vector3.Zero;
                return false;
            }

            worldPosition = Vector3.Transform(localPosition, boneWorld);
            return true;
        }

        public bool TryGetOwnerBonePosition(
            in ClassicFxOwner owner,
            int boneIndex,
            out Vector3 worldPosition)
        {
            Vector3 localPosition = Vector3.Zero;

            return TryTransformOwnerBonePosition(
                owner,
                boneIndex,
                localPosition,
                out worldPosition);
        }

        public float RequestTerrainHeight(float x, float y)
        {
            return _runtime.World?.Terrain?.RequestTerrainHeight(x, y) ?? 0f;
        }

        public Vector3 RequestTerrainLight(float x, float y)
        {
            return _runtime.World?.Terrain?.EvaluateTerrainLight(x, y) ?? Vector3.Zero;
        }

        private bool TryGetClassicFxOwnerSnapshot(
            ClassicFxHandle handle,
            out ClassicFxOwnerSnapshot snapshot)
        {
            if (!handle.IsValid)
            {
                snapshot = default;
                return false;
            }

            switch (handle.Kind)
            {
                case ClassicFxPoolKind.Sprite:
                {
                    if (!_runtime.TryGetSprite(
                            handle,
                            out Primitives.ClassicSprite sprite))
                    {
                        break;
                    }

                    snapshot = new ClassicFxOwnerSnapshot(
                        sprite.Position,
                        new Vector3(0f, 0f, sprite.Rotation),
                        sprite.Light,
                        sprite.Scale,
                        null);

                    return true;
                }

                case ClassicFxPoolKind.Particle:
                {
                    if (!_runtime.TryGetParticle(
                            handle,
                            out Primitives.ClassicParticle particle))
                    {
                        break;
                    }

                    snapshot = new ClassicFxOwnerSnapshot(
                        particle.Position,
                        particle.Angle,
                        particle.Light,
                        particle.Scale,
                        null);

                    return true;
                }
            }

            snapshot = default;
            return false;
        }

        private static bool IsWorldObjectUsable(WorldObject worldObject)
        {
            return worldObject != null && worldObject.World != null;
        }
    }
}
