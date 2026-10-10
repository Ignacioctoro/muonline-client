// ClassicFX Season 6 Batch 19: Imperial Guardian door and statue destruction.
// MuMain original @ 21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect and MoveHandlers.cpp fragment movement.
// Existing Effect pool, particle pool and ModelObject BMD renderer only.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch19CrushModel(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.DoorCrushPiece01 and <=
                ClassicFxEffectType.StatueCrushPiece04 &&
            type != ClassicFxEffectType.DoorCrushPiece09;

        private static bool IsS6Batch19FallingFragment(ClassicFxEffectType type) =>
            (type is >= ClassicFxEffectType.DoorCrushPiece01 and <=
                ClassicFxEffectType.DoorCrushPiece08) ||
            (type is >= ClassicFxEffectType.DoorCrushPiece11 and <=
                ClassicFxEffectType.DoorCrushPiece13) ||
            (type is >= ClassicFxEffectType.StatueCrushPiece01 and <=
                ClassicFxEffectType.StatueCrushPiece03);

        private static bool IsS6Batch19CrushCarrier(
            ClassicFxEffectType type, int subType) =>
            (type == ClassicFxEffectType.DoorCrushCarrier &&
                subType is 0 or 1) ||
            (type == ClassicFxEffectType.StatueCrushCarrier &&
                subType == 0);

        private static bool TryGetS6Batch19ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (subType != 0 || !IsS6Batch19CrushModel(type))
                return false;

            // These are the exact AccessModel names in MapManager.cpp.
            string path = type switch
            {
                ClassicFxEffectType.DoorCrushPiece01 => "Effect/piece01_01.bmd",
                ClassicFxEffectType.DoorCrushPiece02 => "Effect/piece01_02.bmd",
                ClassicFxEffectType.DoorCrushPiece03 => "Effect/piece01_03.bmd",
                ClassicFxEffectType.DoorCrushPiece04 => "Effect/piece01_04.bmd",
                ClassicFxEffectType.DoorCrushPiece05 => "Effect/piece01_05.bmd",
                ClassicFxEffectType.DoorCrushPiece06 => "Effect/piece01_06.bmd",
                ClassicFxEffectType.DoorCrushPiece07 => "Effect/piece01_07.bmd",
                ClassicFxEffectType.DoorCrushPiece08 => "Effect/piece01_08.bmd",
                // Main expects newdoor_break_01.bmd for Piece09.
                // Absent in Data_Broyal: fail cleanly, never substitute.
                ClassicFxEffectType.DoorCrushPiece10 => "Effect/sojghmoon02.bmd",
                ClassicFxEffectType.DoorCrushPiece11 => "Effect/sojghmj01.bmd",
                ClassicFxEffectType.DoorCrushPiece12 => "Effect/sojghmj02.bmd",
                ClassicFxEffectType.DoorCrushPiece13 => "Effect/sojghmj03.bmd",
                ClassicFxEffectType.StatueCrushPiece01 => "Effect/NpcGagoil_Crack01.bmd",
                ClassicFxEffectType.StatueCrushPiece02 => "Effect/NpcGagoil_Crack02.bmd",
                ClassicFxEffectType.StatueCrushPiece03 => "Effect/NpcGagoil_Crack03.bmd",
                ClassicFxEffectType.StatueCrushPiece04 => "Effect/NpcGagoil_Ruin.bmd",
                _ => null
            };
            if (path == null) return false;
            definition = new Season6ModelDefinition(
                path, IsS6Batch19FallingFragment(type) ? 60f : 100f,
                1f, useCallerScale: true);
            return true;
        }

        private void InitializeS6Batch19CrushModel(
            ClassicFxEffectType type, ref int subType, ref Vector3 angle,
            ref Vector3 light, ref float life, ref Vector3 headAngle,
            ref Vector3 direction, ref float velocity, ref float gravity)
        {
            // MODEL_DOOR_CRUSH_EFFECT_PIECE10 and
            // MODEL_STATUE_CRUSH_EFFECT_PIECE04 have JSON-only defaults:
            // 100 ticks, caller scale and no radial impulse.
            if (!IsS6Batch19FallingFragment(type))
            {
                life = 100f;
                return;
            }

            life = 30f + Random.Modulo(30);
            velocity = 0f;
            gravity = 2.3f;
            angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            float strength = (Random.Modulo(64) + 48f) * 0.1f;
            headAngle = Vector3.TransformNormal(
                new Vector3(0f, strength, 0f),
                Matrix.CreateRotationZ(angle.Z));
            headAngle.Z = 15f;
            subType = Random.Modulo(2);
            float brightness = 0.5f + Random.Modulo(6) * 0.1f;
            light = new Vector3(brightness);
            direction = Vector3.Zero;
        }

        private void EmitS6Batch19Crush(
            ClassicFxEffectType carrier, int subType,
            Vector3 position, Vector3 angle, Vector3 light,
            ClassicFxOwner owner)
        {
            // Original CreateEffect spawns exactly 9 door pieces or 6
            // statue pieces in the same invocation. The missing Piece09
            // intentionally produces no model and no replacement.
            if (carrier == ClassicFxEffectType.DoorCrushCarrier)
            {
                for (int i = 0; i < 9; i++)
                {
                    Vector3 at = position + new Vector3(
                        Random.Modulo(200) - 100f,
                        Random.Modulo(200) - 100f,
                        Random.Modulo(200) - 100f);
                    ClassicFxEffectType child = subType == 0
                        ? ClassicFxEffectType.DoorCrushPiece01 + i
                        : ClassicFxEffectType.DoorCrushPiece11 + (i % 3);
                    if (child != ClassicFxEffectType.DoorCrushPiece09)
                        CreateEffect(child, at, angle, light, owner);
                }
                return;
            }

            for (int i = 0; i < 6; i++)
                CreateEffect(
                    ClassicFxEffectType.StatueCrushPiece01 + (i % 3),
                    position, angle, light, owner);
        }

        private bool MoveS6Batch19CrushModel(ref EffectState e, float f)
        {
            if (!IsS6Batch19FallingFragment(e.Type))
            {
                // Two stationary ruined pieces have their own Move handler,
                // which removes an *extra* 1 tick in addition to the global
                // pool ageing. The two decays are preserved separately.
                e.LifeTime -= f;
                if (e.LifeTime < 40f)
                    e.Alpha = MathF.Max(0f, e.Alpha - 0.1f * f);
                return true;
            }

            // Shared C++ Move_MODEL_DOOR_CRUSH_EFFECT_PIECE01 handler:
            // gravity in HeadAngle.Z, horizontal ballistic impulse and
            // collision against RequestTerrainHeight + 20.
            e.HeadAngle.Z -= e.Gravity * f;
            e.Position += e.HeadAngle * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y) + 20f;
            if (e.Position.Z + e.Direction.Z <= ground)
            {
                e.Position.Z = ground;
                float damp = MathF.Pow(0.8f, f);
                e.HeadAngle.X *= damp;
                e.HeadAngle.Y *= damp;
                e.HeadAngle.Z += 0.6f * e.LifeTime * f;
                if (e.HeadAngle.Z < 5f)
                    e.HeadAngle.Z = 0f;
                e.Alpha = MathF.Max(0f, e.Alpha - 0.05f * f);

                // Native collision debris: original pooled Stone1/Stone2,
                // not a new geometry renderer.
                if (Clock.AdvancedReferenceFrame &&
                    Random.FpsCheck(4, Clock))
                {
                    Vector3 p = Vector3.TransformNormal(
                        new Vector3(0f, Random.Modulo(80) + 50f, 0f),
                        Matrix.CreateRotationZ(
                            MathHelper.ToRadians(Random.Modulo(360))));
                    CreateEffect(Random.Modulo(2) == 0
                            ? ClassicFxEffectType.Stone1
                            : ClassicFxEffectType.Stone2,
                        e.Position + p, e.Angle, e.Light,
                        ClassicFxOwner.None);
                }
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

            // Native smoke+1 subtype 6, throttled at original 25-FPS clock.
            if (Clock.AdvancedReferenceFrame && Random.FpsCheck(3, Clock))
                CreateParticle(ClassicTextureIds.BitmapSmoke + 1,
                    e.Position, e.Angle, e.Light, 6);
            return true;
        }
    }
}
