// BroyalMU ClassicFX S6 Batch 33 — Swamp EX01 Shadow Master debris, Warp03.
// Pinned MuMain: 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Reuses the existing BMD renderer, effect pool and Batch 31 warp movement.
namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch33ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.Ex01ShadowMasterAnkleLeft &&
            type <= ClassicFxEffectType.Warp6;

        private static bool IsS6Batch33Warp(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Warp3 or ClassicFxEffectType.Warp6;

        private static bool TryGetS6Batch33ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch33ModelType(type) || subType is not (0 or 1))
                return false;
            if (!IsS6Batch33Warp(type) && subType != 0)
                return false;

            // Pinned native MapManager loads ex01shadow_rock_7_*,
            // NOT ex01shadow_master_7_*. Both exist in Data_Broyal.
            string path = type switch
            {
                ClassicFxEffectType.Ex01ShadowMasterAnkleLeft =>
                    "Monster/ex01shadow_rock_7_ankle_left.bmd",
                ClassicFxEffectType.Ex01ShadowMasterAnkleRight =>
                    "Monster/ex01shadow_rock_7_ankle_right.bmd",
                ClassicFxEffectType.Ex01ShadowMasterBelt =>
                    "Monster/ex01shadow_rock_7_belt.bmd",
                ClassicFxEffectType.Ex01ShadowMasterChest =>
                    "Monster/ex01shadow_rock_7_chest.bmd",
                ClassicFxEffectType.Ex01ShadowMasterHelmet =>
                    "Monster/ex01shadow_rock_7_helmet.bmd",
                ClassicFxEffectType.Ex01ShadowMasterKneeLeft =>
                    "Monster/ex01shadow_rock_7_knee_left.bmd",
                ClassicFxEffectType.Ex01ShadowMasterKneeRight =>
                    "Monster/ex01shadow_rock_7_knee_right.bmd",
                ClassicFxEffectType.Ex01ShadowMasterWristLeft =>
                    "Monster/ex01shadow_rock_7_wrist_left.bmd",
                ClassicFxEffectType.Ex01ShadowMasterWristRight =>
                    "Monster/ex01shadow_rock_7_wrist_right.bmd",
                ClassicFxEffectType.Warp3 or ClassicFxEffectType.Warp6 =>
                    "NPC/warp03.bmd",
                _ => null
            };
            if (path == null) return false;

            // Native EffectTypes.json: Warp3/6 lifespan 16777215, scale 0.6.
            // Native has no specialized CreateEffect/MoveEffects branch for
            // the nine EX01 fragments. Generic fallback scale is 0.9;
            // their 30-tick expiry below is PROVISIONAL, not native parity.
            definition = new Season6ModelDefinition(
                path, IsS6Batch33Warp(type) ? 16777215f : 30f,
                IsS6Batch33Warp(type) ? 0.6f : 0.9f);
            return true;
        }

        private static void ConfigureS6Batch33ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            if (IsS6Batch33Warp(type))
                ConfigureS6Batch31ModelView(view, type);
        }

        private bool MoveS6Batch33Model(ref EffectState effect, float frameFactor)
        {
            if (IsS6Batch33Warp(effect.Type))
            {
                // Shared native Move_MODEL_WARP3: sinusoidal RGB and
                // (4/2 + Gravity) yaw per subtype, by FrameFactor.
                // No Batch31 randomized warp creation initialization.
                return MoveS6Batch31Model(ref effect, frameFactor);
            }

            // GMSwampOfQuiet spawns these BMDs at owner bones on death,
            // but native supplies no movement handler. Keep static.
            // Death-callsite/owner-bone rerouting is not part of this batch.
            return true;
        }
    }
}
