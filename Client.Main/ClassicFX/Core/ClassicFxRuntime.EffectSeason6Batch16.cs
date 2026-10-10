// BroyalMU ClassicFX S6 Batch 16 (18 native types).
// Main pinned: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp CreateEffect, Behaviors/MoveHandlers.cpp, ZzzOpenData.cpp.
// Visual only; no CheckClientArrow, combat, packet or server logic.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Controls;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch16ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.MultiShot1 and <= ClassicFxEffectType.ArrowGamble;

        private static bool IsS6Batch16MultiShot(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.MultiShot1 and <= ClassicFxEffectType.MultiShot3;

        private static bool IsS6Batch16Stone(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.BigStonePart1 and <= ClassicFxEffectType.GolemStone;

        private static bool IsS6Batch16Gate(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.GatePart1 and <= ClassicFxEffectType.GatePart3;

        private static bool IsS6Batch16Arrow(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.ArrowSteel and <= ClassicFxEffectType.ArrowGamble;

        private static bool TryGetS6Batch16ModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch16ModelType(type) || subtype < 0 ||
                (subtype > 2 &&
                    !(type == ClassicFxEffectType.BigStonePart2 && subtype == 3)))
                return false;

            string path = type switch
            {
                ClassicFxEffectType.MultiShot1 => "Effect/multishot01.bmd",
                ClassicFxEffectType.MultiShot2 => "Effect/multishot02.bmd",
                ClassicFxEffectType.MultiShot3 => "Effect/multishot03.bmd",
                ClassicFxEffectType.BigStonePart1 => "Skill/Flysmallstone1.bmd",
                ClassicFxEffectType.BigStonePart2 => "Skill/Flysmallstone2.bmd",
                ClassicFxEffectType.WallPart1 => "Skill/wallstone1.bmd",
                ClassicFxEffectType.WallPart2 => "Skill/wallstone2.bmd",
                ClassicFxEffectType.GatePart1 => "Skill/gatepart1.bmd",
                ClassicFxEffectType.GatePart2 => "Skill/gatepart2.bmd",
                ClassicFxEffectType.GatePart3 => "Skill/gatepart3.bmd",
                ClassicFxEffectType.GolemStone => "Skill/golem_stone.bmd",
                ClassicFxEffectType.ArrowSteel => "Skill/ArrowSteel01.bmd",
                ClassicFxEffectType.ArrowThunder => "Skill/ArrowThunder01.bmd",
                ClassicFxEffectType.ArrowLaser => "Skill/ArrowLaser01.bmd",
                ClassicFxEffectType.ArrowV => "Skill/ArrowV01.bmd",
                ClassicFxEffectType.ArrowSaw => "Skill/ArrowSaw01.bmd",
                ClassicFxEffectType.ArrowSpark => "Skill/Arrow_Spark.bmd",
                ClassicFxEffectType.ArrowGamble => "Skill/gamble_arrows01.bmd",
                _ => null
            };
            if (path == null) return false;

            float life = type switch
            {
                ClassicFxEffectType.MultiShot1 => 16f,
                ClassicFxEffectType.MultiShot2 => 13f,
                ClassicFxEffectType.MultiShot3 => 12f,
                _ => 30f
            };
            definition = new Season6ModelDefinition(
                path, life, 1f, useCallerScale: true);
            return true;
        }

        private void InitializeS6Batch16Model(
            ClassicFxEffectType type, ref int subtype,
            ref Vector3 position, ref Vector3 angle, ref float scale,
            ref float life, ref float alpha, ref Vector3 direction,
            ref float gravity, ref float velocity, ref float meshLight)
        {
            float f = Clock.FrameFactor;

            if (IsS6Batch16Arrow(type) || IsS6Batch16MultiShot(type))
            {
                // Native CreateEffect MODEL_MULTI_SHOT / MODEL_ARROW_*:
                // rotate local launch offset by the original caster orientation.
                position += Vector3.TransformNormal(
                    new Vector3(-10f, -60f, 135f),
                    Matrix.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z));
                velocity = 1f;

                if (IsS6Batch16MultiShot(type))
                {
                    life = type == ClassicFxEffectType.MultiShot1 ? 16f :
                           type == ClassicFxEffectType.MultiShot2 ? 13f : 12f;
                    scale = 0f;
                }
                else
                {
                    life = 30f;
                    scale = type == ClassicFxEffectType.ArrowSpark ? 1f : 0.8f;
                    direction = new Vector3(0f, -70f, 0f);
                }
                return;
            }

            // Native MODEL_BIG_STONE_PART1/2 + WALL_PART1/2 + GOLEM_STONE.
            if (type == ClassicFxEffectType.BigStonePart2 && subtype == 3)
            {
                life = 32f + Random.Modulo(16);
                scale = 1.2f + (Random.Modulo(3) + 2) * 0.12f;
                direction.Z = -(Random.Modulo(5) + 20);
                velocity = 1.8f;
                return;
            }

            bool wall = type is ClassicFxEffectType.WallPart1 or
                ClassicFxEffectType.WallPart2;
            bool gate = IsS6Batch16Gate(type);
            bool golem = type == ClassicFxEffectType.GolemStone;
            float speed;
            if (wall)
            {
                speed = (Random.Modulo(256) + 128) * 0.1f;
                scale = (Random.Modulo(2) + 7) * 0.1f;
                gravity = Random.Modulo(5) + 6f;
            }
            else if (golem)
            {
                speed = (Random.Modulo(128) + 32) * 0.1f;
                scale = (Random.Modulo(2) + 10) * 0.25f;
                gravity = Random.Modulo(5) + 2f;
            }
            else if (gate)
            {
                speed = (Random.Modulo(128) + 64) * 0.1f;
                scale = (Random.Modulo(2) + 7) * 0.1f;
                gravity = Random.Modulo(5) + 2f;
            }
            else if (subtype == 2)
            {
                speed = (Random.Modulo(128) + 32) * 0.1f;
                scale = 0.6f + (Random.Modulo(2) + 4) * 0.12f;
                gravity = Random.Modulo(5) + 2f;
            }
            else
            {
                speed = (Random.Modulo(128) + 128) * 0.1f;
                scale = (Random.Modulo(7) + 10) * 0.1f;
                gravity = Random.Modulo(5) + 2f;
            }

            life = 32f + Random.Modulo(16);
            angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            direction = Vector3.TransformNormal(
                new Vector3(0f, speed, 0f), Matrix.CreateRotationZ(angle.Z));
            position.Z += 50f * f;

            if (gate)
            {
                if (type == ClassicFxEffectType.GatePart1 && subtype == 0)
                {
                    subtype = 1;
                    gravity += Random.Modulo(5) * f;
                }
                else if (subtype == 2)
                    scale *= 0.6f;
            }
            angle = new Vector3(
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)));
        }

        private static void ConfigureS6Batch16ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type)
        {
            // Native MultiShot BMDs are non-blended expanding impact models;
            // Arrow Thunder/Laser/V use BlendMesh 0 in CreateEffect.
            if (IsS6Batch16MultiShot(type))
                view.BlendMesh = -2;
            else if (type is ClassicFxEffectType.ArrowThunder or
                     ClassicFxEffectType.ArrowLaser or
                     ClassicFxEffectType.ArrowV)
                view.BlendMesh = 0;
        }

        private bool MoveS6Batch16Model(ref EffectState e, float f)
        {
            if (IsS6Batch16MultiShot(e.Type))
            {
                e.Scale += (e.Type == ClassicFxEffectType.MultiShot1 ? 0.2f :
                    e.Type == ClassicFxEffectType.MultiShot2 ? 0.3f : 0.25f) * f;
                e.BlendMeshLight = e.LifeTime / 18f;
                e.Alpha = e.BlendMeshLight;
                return true;
            }

            if (IsS6Batch16Arrow(e.Type))
            {
                if (e.FirstMove && e.ModelView != null &&
                    e.ModelView.Status == GameControlStatus.Ready)
                {
                    e.FirstMove = false;
                    if (e.Type == ClassicFxEffectType.ArrowSpark)
                        CreateJoint(ClassicTextureIds.BitmapFlare + 1,
                            e.Position, e.Position, e.Angle, 14,
                            ClassicFxOwner.FromWorldObject(e.ModelView), 50f);
                    // Native subtype 2 creates a Piercing carrier; use the
                    // existing pool + Effect model, no new renderer.
                    if (e.SubType == 2)
                        CreateEffect(ClassicFxEffectType.Piercing,
                            e.Position, e.Angle, e.Light,
                            ClassicFxOwner.FromWorldObject(e.ModelView));
                }

                // C++ CheckClientArrow includes combat/collision and is not
                // copied. The existing Batch06 visual-only travel bridge
                // handles flight while combat remains authoritative elsewhere.
                AdvanceS6Arrow(ref e, f);
                if (e.Type == ClassicFxEffectType.ArrowSpark)
                    e.Angle.Y += MathHelper.ToRadians(35f) * f;
                if (e.Type == ClassicFxEffectType.ArrowGamble)
                {
                    e.Angle.Y += MathHelper.ToRadians(60f) * f;
                    Vector3 green = new Vector3(0.2f, 0.8f, 0.5f);
                    if (Clock.AdvancedReferenceFrame)
                    {
                        for (int n = 0; n < 2; ++n)
                        {
                            Vector3 at = e.Position + new Vector3(
                                Random.Modulo(32) - 16f,
                                Random.Modulo(64) - 32f,
                                Random.Modulo(32) - 16f);
                            CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                                at, e.Angle, green, subType: 30);
                        }
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position, e.Angle, green, subType: 24);
                    }
                    AddClassicTerrainLight(e.Position.X, e.Position.Y,
                        green, 2f);
                }
                return true;
            }

            if (e.Type == ClassicFxEffectType.BigStonePart2 &&
                e.SubType == 3)
            {
                // The source also updates position in its shared particle
                // transform; retain that movement in the pooled model state.
                e.Direction.Z -= e.Velocity * f;
                e.Velocity += 0.3f * f;
                e.Position += e.Direction * f;
                float floor = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < floor)
                {
                    e.Position.Z = floor;
                    e.Direction = Vector3.Zero;
                    e.Angle = Vector3.Zero;
                    if (Clock.AdvancedReferenceFrame && Random.FpsCheck(10, Clock))
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position, e.Angle, e.Light, 24, 1.25f * e.Scale);
                }
                else if (Clock.AdvancedReferenceFrame && Random.FpsCheck(2, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Light, 24, 0.2f);
                return true;
            }

            e.Position += e.Direction * f;
            e.Direction *= MathF.Pow(0.9f, f);
            bool wall = e.Type is ClassicFxEffectType.WallPart1 or
                ClassicFxEffectType.WallPart2;
            bool gate = IsS6Batch16Gate(e.Type);
            if (gate && e.SubType == 1)
                e.Direction *= MathF.Pow(1.1f, f);
            e.Gravity -= (wall ? 6f : gate ? 4f : 3f) * f;
            e.Position.Z += e.Gravity * f;

            float height = RequestTerrainHeight(e.Position.X, e.Position.Y);
            bool collided = e.Position.Z < height;
            if (collided)
            {
                e.Position.Z = height;
                e.Gravity = -e.Gravity * (gate ? 0.5f : 0.2f);
                e.LifeTime -= 5f * f;
            }
            e.Angle.X -= MathHelper.ToRadians(e.Scale *
                (collided ? 128f : 16f)) * f;
            if (!gate)
                e.Alpha = e.LifeTime / 10f;

            if (!Clock.AdvancedReferenceFrame)
                return true;

            if (gate)
            {
                if (Random.FpsCheck(10, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke + 1,
                        e.Position, e.Angle, Vector3.One);
                return true;
            }

            if (e.Type == ClassicFxEffectType.GolemStone)
            {
                if (Random.FpsCheck(4, Clock))
                {
                    CreateParticle(ClassicTextureIds.BitmapTrueFire,
                        e.Position, e.Angle, Vector3.One, 5, 2.8f);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, Vector3.One, 21, 1.8f);
                }
                if (Random.FpsCheck(10, Clock))
                    CreateParticle(ClassicTextureIds.BitmapSmoke + 1,
                        e.Position, e.Angle, Vector3.One);
            }
            else if (e.Type == ClassicFxEffectType.BigStonePart1 &&
                     e.SubType == 2 && Random.FpsCheck(10, Clock))
            {
                Vector3 impact = e.Position;
                impact.Z = height;
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    impact, e.Angle, new Vector3(0.2f, 0.5f, 0.35f),
                    11, 2f);
            }
            return true;
        }
    }
}
