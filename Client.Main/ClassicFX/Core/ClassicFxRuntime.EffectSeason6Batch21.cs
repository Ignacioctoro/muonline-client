// ClassicFX S6 Batch 21: native Swamp of Quiet Shadow Pawn/Knight/Rook debris.
// MuMain fixed at 21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// MapManager.cpp model paths; ZzzEffect.cpp CreateEffect / MoveEffects / RenderEffects.
// All 27 BMDs verified in Data_Broyal/Data/Monster. No separate renderer.
using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch21ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.ShadowPawnAnkleLeft and <=
                ClassicFxEffectType.ShadowRookWristRight;

        private static bool IsS6Batch21Rook(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.ShadowRookAnkleLeft and <=
                ClassicFxEffectType.ShadowRookWristRight;

        private static bool TryGetS6Batch21ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch21ModelType(type) || subType != 0)
                return false;

            // The original file prefix for Rook is shadow_rock (not rook).
            string path = type switch
            {
                ClassicFxEffectType.ShadowPawnAnkleLeft => "Monster/shadow_pawn_7_ankle_left.bmd",
                ClassicFxEffectType.ShadowPawnAnkleRight => "Monster/shadow_pawn_7_ankle_right.bmd",
                ClassicFxEffectType.ShadowPawnBelt => "Monster/shadow_pawn_7_belt.bmd",
                ClassicFxEffectType.ShadowPawnChest => "Monster/shadow_pawn_7_chest.bmd",
                ClassicFxEffectType.ShadowPawnHelmet => "Monster/shadow_pawn_7_helmet.bmd",
                ClassicFxEffectType.ShadowPawnKneeLeft => "Monster/shadow_pawn_7_knee_left.bmd",
                ClassicFxEffectType.ShadowPawnKneeRight => "Monster/shadow_pawn_7_knee_right.bmd",
                ClassicFxEffectType.ShadowPawnWristLeft => "Monster/shadow_pawn_7_wrist_left.bmd",
                ClassicFxEffectType.ShadowPawnWristRight => "Monster/shadow_pawn_7_wrist_right.bmd",
                ClassicFxEffectType.ShadowKnightAnkleLeft => "Monster/shadow_knight_7_ankle_left.bmd",
                ClassicFxEffectType.ShadowKnightAnkleRight => "Monster/shadow_knight_7_ankle_right.bmd",
                ClassicFxEffectType.ShadowKnightBelt => "Monster/shadow_knight_7_belt.bmd",
                ClassicFxEffectType.ShadowKnightChest => "Monster/shadow_knight_7_chest.bmd",
                ClassicFxEffectType.ShadowKnightHelmet => "Monster/shadow_knight_7_helmet.bmd",
                ClassicFxEffectType.ShadowKnightKneeLeft => "Monster/shadow_knight_7_knee_left.bmd",
                ClassicFxEffectType.ShadowKnightKneeRight => "Monster/shadow_knight_7_knee_right.bmd",
                ClassicFxEffectType.ShadowKnightWristLeft => "Monster/shadow_knight_7_wrist_left.bmd",
                ClassicFxEffectType.ShadowKnightWristRight => "Monster/shadow_knight_7_wrist_right.bmd",
                ClassicFxEffectType.ShadowRookAnkleLeft => "Monster/shadow_rock_7_ankle_left.bmd",
                ClassicFxEffectType.ShadowRookAnkleRight => "Monster/shadow_rock_7_ankle_right.bmd",
                ClassicFxEffectType.ShadowRookBelt => "Monster/shadow_rock_7_belt.bmd",
                ClassicFxEffectType.ShadowRookChest => "Monster/shadow_rock_7_chest.bmd",
                ClassicFxEffectType.ShadowRookHelmet => "Monster/shadow_rock_7_helmet.bmd",
                ClassicFxEffectType.ShadowRookKneeLeft => "Monster/shadow_rock_7_knee_left.bmd",
                ClassicFxEffectType.ShadowRookKneeRight => "Monster/shadow_rock_7_knee_right.bmd",
                ClassicFxEffectType.ShadowRookWristLeft => "Monster/shadow_rock_7_wrist_left.bmd",
                ClassicFxEffectType.ShadowRookWristRight => "Monster/shadow_rock_7_wrist_right.bmd",
                _ => null
            };
            if (path == null)
                return false;

            // Native CreateEffect overrides the initial lifetime and scale.
            definition = new Season6ModelDefinition(
                path, 60f, 1f, useCallerScale: true);
            return true;
        }

        private void InitializeS6Batch21Model(
            ref int subType, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life, ref Vector3 direction,
            ref float gravity, ref Vector3 headAngle,
            ref float velocity, ClassicFxEffectType type)
        {
            // The Pawn/Knight branch shares the Ice Giant 1..4 creation
            // handler. Rook changes only the scale: 1.3 vs 1.1.
            life = 30f + Random.Modulo(30);
            scale = IsS6Batch21Rook(type) ? 1.3f : 1.1f;
            velocity = 0f;
            gravity = 3.5f;

            angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            headAngle = Vector3.TransformNormal(
                new Vector3(0f, (48f + Random.Modulo(64)) * 0.1f, 0f),
                Matrix.CreateRotationZ(angle.Z));
            headAngle.Z = 15f;
            subType = Random.Modulo(2);
            float brightness = 0.5f + Random.Modulo(6) * 0.1f;
            light = new Vector3(brightness);
            direction = Vector3.Zero;
        }

        private bool MoveS6Batch21Model(ref EffectState e, float f)
        {
            // Native MoveEffects shared with Ice Giant and Shadow fragments;
            // unlike Karutan's Condra breakage, contact drag is 0.5, not 0.8.
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y) + 20f;
            if (e.Position.Z + e.Direction.Z <= ground)
            {
                e.Position.Z = ground;
                float damping = MathF.Pow(0.5f, f);
                e.HeadAngle.X *= damping;
                e.HeadAngle.Y *= damping;
                e.HeadAngle.Z += 0.6f * e.LifeTime * f;
                if (e.HeadAngle.Z < 5f)
                    e.HeadAngle.Z = 0f;
                e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);
            }
            else
            {
                float rotation = MathHelper.ToRadians(0.15f * e.LifeTime) * f;
                if (e.SubType == 0)
                {
                    e.Angle.X += rotation;
                    e.Angle.Y += rotation;
                }
                else
                {
                    e.Angle.X -= rotation;
                    e.Angle.Y -= rotation;
                }
            }

            // This C++ branch does not spawn secondary particles/joints.
            return true;
        }
    }
}
