using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private ClassicFxWorldBridge _worldBridge;

        public ClassicFxWorldBridge WorldBridge =>
            _worldBridge ??= new ClassicFxWorldBridge(this);

        public bool TryGetOwnerSnapshot(
            in ClassicFxOwner owner,
            out ClassicFxOwnerSnapshot snapshot) =>
            WorldBridge.TryGetOwnerSnapshot(owner, out snapshot);

        public bool TryGetOwnerPosition(
            in ClassicFxOwner owner,
            out Vector3 position) =>
            WorldBridge.TryGetOwnerPosition(owner, out position);

        public bool TryGetOwnerWorldObject(
            in ClassicFxOwner owner,
            out WorldObject worldObject) =>
            WorldBridge.TryGetOwnerWorldObject(owner, out worldObject);

        public bool TryGetOwnerBoneWorldMatrix(
            in ClassicFxOwner owner,
            int boneIndex,
            out Matrix worldMatrix) =>
            WorldBridge.TryGetOwnerBoneWorldMatrix(owner, boneIndex, out worldMatrix);

        public bool TryGetOwnerBonePosition(
            in ClassicFxOwner owner,
            int boneIndex,
            out Vector3 worldPosition) =>
            WorldBridge.TryGetOwnerBonePosition(owner, boneIndex, out worldPosition);

        public bool TryTransformOwnerBonePosition(
            in ClassicFxOwner owner,
            int boneIndex,
            in Vector3 localPosition,
            out Vector3 worldPosition) =>
            WorldBridge.TryTransformOwnerBonePosition(
                owner,
                boneIndex,
                localPosition,
                out worldPosition);

        public float RequestTerrainHeight(float x, float y) =>
            WorldBridge.RequestTerrainHeight(x, y);

        public Vector3 RequestTerrainLight(float x, float y) =>
            WorldBridge.RequestTerrainLight(x, y);
    }
}
