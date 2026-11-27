using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        public ClassicFxHandle CreateEquipmentSetTrail(ClassicFxOwner owner, Vector3 angle,
            int subtype, ushort skill, bool plus)
        {
            if ((plus && subtype != 7) || (!plus && subtype != 0 && subtype != 18) ||
                !TryGetOwnerPosition(owner, out Vector3 position)) return ClassicFxHandle.Invalid;
            var handle = CreateJoint(ClassicTextureIds.BitmapFlare + (plus ? 1 : 0),
                position, position, angle, subtype, owner,
                scale: plus || subtype == 18 ? 20f : 10f, pkKey: plus ? (short)40 : (short)-1, skillIndex: skill);
            if (!handle.IsValid) return handle;
            ref var joint = ref _joints[handle.Index];
            joint.IsEquipmentSetTrail = true;
            // Native temporarily sets OBJECT.Light=(1,1,1) while creating
            // Flare/0 and Flare/18. Do not mutate the player's shared lighting.
            if (!plus) joint.Light = Vector3.One;
            // Fixed 25-Hz history, as with the repaired Ancient aura; prevents
            // short trails after a slow creation frame or at very high FPS.
            joint.MaxTails = plus ? 16 : 20;
            return handle;
        }

        private static bool EquipmentSetTrailOwnerIsMoving(ref ClassicJoint joint)
        {
            if (!joint.IsEquipmentSetTrail || joint.Target.WorldObject is not PlayerObject player) return false;
            var action = player.CurrentAction;
            return !(action < PlayerAction.PlayerWalkMale || action is PlayerAction.PlayerDarklordStand
                or PlayerAction.PlayerStopRideHorse or PlayerAction.PlayerStopTwoHandSwordTwo);
        }
    }
}
