using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private ClassicFxMoveBridge
            _moveBridge;

        public ClassicFxMoveBridge MoveBridge =>
            _moveBridge ??=
                new ClassicFxMoveBridge(
                    this);

        private void BeginClassicFxMoveFrame()
        {
            MoveBridge
                .BeginFrame();
        }

        public bool AddClassicTerrainLight(
            float x,
            float y,
            in Vector3 color,
            float rangeInTiles)
        {
            return MoveBridge
                .AddTerrainLight(
                    x,
                    y,
                    color,
                    rangeInTiles);
        }

        public bool TryGetOwnerCurrentAction(
            in ClassicFxOwner owner,
            out int currentAction)
        {
            return MoveBridge
                .TryGetOwnerCurrentAction(
                    owner,
                    out currentAction);
        }

        public bool IsOwnerSanta2Action(
            in ClassicFxOwner owner)
        {
            return MoveBridge
                .IsOwnerSanta2Action(
                    owner);
        }

        public bool IsOwnerBuffActive(
            in ClassicFxOwner owner,
            byte effectId)
        {
            return MoveBridge
                .IsOwnerBuffActive(
                    owner,
                    effectId);
        }

        public bool TryGetOwnerBoneCount(
            in ClassicFxOwner owner,
            out int boneCount)
        {
            return MoveBridge
                .TryGetOwnerBoneCount(
                    owner,
                    out boneCount);
        }

        public bool TryGetOwnerNamedBonePosition(
            in ClassicFxOwner owner,
            string boneName,
            in Vector3 localOffset,
            out Vector3 worldPosition)
        {
            return MoveBridge
                .TryGetOwnerNamedBonePosition(
                    owner,
                    boneName,
                    localOffset,
                    out worldPosition);
        }

        public bool RequestClassicBomb(
            in Vector3 position,
            bool explode,
            int subType = 0)
        {
            return MoveBridge
                .RequestBomb(
                    position,
                    explode,
                    subType);
        }

        public bool IsCryWolfFirstStage()
        {
            return MoveBridge
                .IsCryWolfFirstStage();
        }

        private void ResetMoveBridge()
        {
            _moveBridge?
                .Reset();
        }

        private void DisposeMoveBridge()
        {
            _moveBridge?
                .Dispose();

            _moveBridge =
                null;
        }
    }
}
