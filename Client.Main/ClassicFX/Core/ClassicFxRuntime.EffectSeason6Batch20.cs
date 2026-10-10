// ClassicFX Season 6 Batch 20: Karutan Condra / NarCondra breakage.
// Fixed MuMain source: 21728b1e5b03e0763b38ef9e23f79645e0df7ad2, #ifdef ASG_ADD_KARUTAN_MONSTERS.
// ZzzEffect.cpp CreateEffect/MoveEffects; MapManager.cpp / ZzzOpenData.cpp.
// No new renderer: ClassicFxEffectModelObject + EffectState pool.
using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch20ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.CondraArmL and <=
                ClassicFxEffectType.NarCondraStone3;

        private static bool IsS6Batch20StoneType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.CondraStone and <=
                ClassicFxEffectType.NarCondraStone3;

        private static bool IsS6Batch20AvailableModel(ClassicFxEffectType type) =>
            IsS6Batch20ModelType(type) &&
            type is not (ClassicFxEffectType.CondraPelvis or
                         ClassicFxEffectType.CondraStomach or
                         ClassicFxEffectType.CondraNeck);

        private static bool TryGetS6Batch20ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch20AvailableModel(type) ||
                subType < 0 || subType > (IsS6Batch20StoneType(type) ? 2 : 0))
                return false;

            // The mapping exactly matches original AccessModel filenames.
            // Condra pelvis/stomach/neck have no BMD in Data_Broyal/main.
            // Do not supply substitutes for those three missing assets.
            string path = type switch
            {
                ClassicFxEffectType.CondraArmL => "Monster/condra_7_arm_left.bmd",
                ClassicFxEffectType.CondraArmL2 => "Monster/condra_7_arm_left_2.bmd",
                ClassicFxEffectType.CondraShoulder => "Monster/condra_7_shoulder_right.bmd",
                ClassicFxEffectType.CondraArmR => "Monster/condra_7_arm_right.bmd",
                ClassicFxEffectType.CondraArmR2 => "Monster/condra_7_arm_right_2.bmd",
                ClassicFxEffectType.CondraConeL => "Monster/condra_7_cone_left.bmd",
                ClassicFxEffectType.CondraConeR => "Monster/condra_7_cone_right.bmd",
                ClassicFxEffectType.NarCondraArmL => "Monster/nar_condra_7_arm_left.bmd",
                ClassicFxEffectType.NarCondraArmL2 => "Monster/nar_condra_7_arm_left_2.bmd",
                ClassicFxEffectType.NarCondraShoulderL => "Monster/nar_condra_7_shoulder_left.bmd",
                ClassicFxEffectType.NarCondraShoulderR => "Monster/nar_condra_7_shoulder_right.bmd",
                ClassicFxEffectType.NarCondraArmR => "Monster/nar_condra_7_arm_right.bmd",
                ClassicFxEffectType.NarCondraArmR2 => "Monster/nar_condra_7_arm_right_2.bmd",
                ClassicFxEffectType.NarCondraArmR3 => "Monster/nar_condra_7_arm_right_3.bmd",
                ClassicFxEffectType.NarCondraCone1 => "Monster/nar_condra_7_cone_1.bmd",
                ClassicFxEffectType.NarCondraCone2 => "Monster/nar_condra_7_cone_2.bmd",
                ClassicFxEffectType.NarCondraCone3 => "Monster/nar_condra_7_cone_3.bmd",
                ClassicFxEffectType.NarCondraCone4 => "Monster/nar_condra_7_cone_4.bmd",
                ClassicFxEffectType.NarCondraCone5 => "Monster/nar_condra_7_cone_5.bmd",
                ClassicFxEffectType.NarCondraCone6 => "Monster/nar_condra_7_cone_6.bmd",
                ClassicFxEffectType.NarCondraPelvis => "Monster/nar_condra_7_pelvis.bmd",
                ClassicFxEffectType.NarCondraStomach => "Monster/nar_condra_7_stomach.bmd",
                ClassicFxEffectType.NarCondraNeck => "Monster/nar_condra_7_neck.bmd",
                ClassicFxEffectType.CondraStone => "Monster/condra_7_stone.bmd",
                ClassicFxEffectType.CondraStone1 => "Monster/condra_7_stone_2.bmd",
                ClassicFxEffectType.CondraStone2 => "Monster/condra_7_stone_3.bmd",
                ClassicFxEffectType.CondraStone3 => "Monster/condra_7_stone_4.bmd",
                ClassicFxEffectType.CondraStone4 => "Monster/condra_7_stone_5.bmd",
                ClassicFxEffectType.CondraStone5 => "Monster/condra_7_stone_6.bmd",
                ClassicFxEffectType.NarCondraStone => "Monster/nar_condra_7_stone_1.bmd",
                ClassicFxEffectType.NarCondraStone1 => "Monster/nar_condra_7_stone_2.bmd",
                ClassicFxEffectType.NarCondraStone2 => "Monster/nar_condra_7_stone_3.bmd",
                ClassicFxEffectType.NarCondraStone3 => "Monster/nar_condra_7_stone_4.bmd",
                _ => null
            };
            if (path == null) return false;
            definition = new Season6ModelDefinition(
                path, 60f, 1f, useCallerScale: true);
            return true;
        }

        private void InitializeS6Batch20Model(
            ClassicFxEffectType type, ref int subType,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref Vector3 direction, ref float gravity,
            ref Vector3 headAngle, ref float velocity)
        {
            float f = Clock.FrameFactor;
            if (IsS6Batch20StoneType(type))
            {
                // Shared native cases MODEL_EFFECT_BROKEN_ICE0..3,
                // MODEL_CONDRA_STONE* and MODEL_NARCONDRA_STONE*.
                // Later movement is also the already ported Ice handler.
                switch (subType)
                {
                    case 0:
                        direction = Vector3.Zero;
                        life = 35f + Random.Modulo(16);
                        scale = (3f + Random.Modulo(13)) * 0.2f;
                        gravity = 3f + Random.Modulo(3);
                        angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                        headAngle = Vector3.TransformNormal(
                            new Vector3(0f,
                                (64f + Random.Modulo(128)) * 0.1f, 0f),
                            Matrix.CreateRotationZ(angle.Z));
                        headAngle.Z += 25f * f;
                        break;
                    case 1:
                        life = 40f;
                        scale += (15f + Random.Modulo(8)) * 0.1f;
                        direction = new Vector3(0f, 0f, -60f);
                        headAngle = new Vector3(0f, 30f, 0f);
                        break;
                    case 2:
                        life = 100f;
                        gravity = 20f + Random.Modulo(20) * 0.5f;
                        break;
                }
                return;
            }

            // Native Condra and NarCondra body-fragment initialization
            // is the same as the TotemGolem breakage family, except scale.
            life = 30f + Random.Modulo(30);
            scale = 1.4f;
            velocity = 0f;
            gravity = 3.5f;
            angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            headAngle = Vector3.TransformNormal(
                new Vector3(0f,
                    (48f + Random.Modulo(64)) * 0.1f, 0f),
                Matrix.CreateRotationZ(angle.Z));
            headAngle.Z = 15f;
            subType = Random.Modulo(2);
            light = new Vector3(0.5f + Random.Modulo(6) * 0.1f);
            direction = Vector3.Zero;
        }

        private bool MoveS6Batch20Model(ref EffectState e, float f)
        {
            if (IsS6Batch20StoneType(e.Type))
            {
                // Exact C++ fallthrough into the BrokenIce move case.
                // Reuse the already migrated implementation and all its
                // existing Inferno/Smoke/Ice/Explosion secondary effects.
                return MoveS6Batch15Ice(ref e, f);
            }

            // Native MODEL_CONDRA_ARM_L..NECK and all NarCondra body
            // pieces share the TotemGolem physics branch. They do NOT
            // emit the Imperial Guardian stone/smoke children.
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y) + 20f;
            if (e.Position.Z + e.Direction.Z <= ground)
            {
                e.Position.Z = ground;
                float drag = MathF.Pow(0.8f, f);
                e.HeadAngle.X *= drag;
                e.HeadAngle.Y *= drag;
                e.HeadAngle.Z += 0.6f * e.LifeTime * f;
                if (e.HeadAngle.Z < 5f)
                    e.HeadAngle.Z = 0f;
                e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
            }
            else
            {
                float spin = MathHelper.ToRadians(0.15f * e.LifeTime) * f;
                if (e.SubType == 0)
                {
                    e.Angle.X += spin;
                    e.Angle.Y += spin;
                }
                else
                {
                    e.Angle.X -= spin;
                    e.Angle.Y -= spin;
                }
            }
            return true;
        }
    }
}
