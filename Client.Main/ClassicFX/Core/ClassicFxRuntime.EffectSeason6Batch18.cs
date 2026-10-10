// ClassicFX Season 6 Batch 18: Christmas and New Year's event models.
// MuMain fixed SHA: 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Sources: ZzzEffect.cpp CreateEffect, MoveHandlers.cpp and Event.cpp.
// Pooled BMD effects only; no gameplay, packet or server changes.
using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch18ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.XmasEventBox and <=
                ClassicFxEffectType.NewYearsDayYut;

        private static bool IsS6Batch18XmasDrop(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.XmasEventBox and <=
                ClassicFxEffectType.XmasEventSocks;

        private static bool IsS6Batch18NewYear(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.NewYearsDayBeksulki and <=
                ClassicFxEffectType.NewYearsDayYut;

        private static bool TryGetS6Batch18ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch18ModelType(type) || subType < 0)
                return false;

            // Original paths are specified in CXmasEvent::LoadXmasEventModel
            // and CNewYearsDayEvent::LoadModel.
            string path = type switch
            {
                ClassicFxEffectType.XmasEventBox => "Skill/xmasebox.bmd",
                ClassicFxEffectType.XmasEventCandy => "Skill/xmasecandy.bmd",
                ClassicFxEffectType.XmasEventTree => "Skill/xmasetree.bmd",
                ClassicFxEffectType.XmasEventSocks => "Skill/xmaseyangbal.bmd",
                ClassicFxEffectType.XmasEventIceHeart => "Skill/xmaseicehart.bmd",
                ClassicFxEffectType.NewYearsDayBeksulki => "Monster/sulbeksulki.bmd",
                ClassicFxEffectType.NewYearsDayCandy => "Monster/sulcandy.bmd",
                ClassicFxEffectType.NewYearsDayMoney => "Monster/sulgold.bmd",
                ClassicFxEffectType.NewYearsDayHotPepperGreen => "Monster/sulgreengochu.bmd",
                ClassicFxEffectType.NewYearsDayHotPepperRed => "Monster/sulredgochu.bmd",
                ClassicFxEffectType.NewYearsDayPig => "Monster/sulpeg.bmd",
                ClassicFxEffectType.NewYearsDayYut => "Monster/sulyutnulre.bmd",
                _ => null
            };

            if (path == null)
                return false;

            definition = new Season6ModelDefinition(
                path, type == ClassicFxEffectType.XmasEventIceHeart ? 100f : 50f,
                1f, useCallerScale: true);
            return true;
        }

        private void InitializeS6Batch18Model(
            ClassicFxEffectType type, ref Vector3 angle,
            ref float scale, ref float life,
            ref Vector3 direction, ref float gravity,
            ref Vector3 animationAxis)
        {
            float f = Clock.FrameFactor;

            if (type == ClassicFxEffectType.XmasEventIceHeart)
            {
                scale = 4f + (Random.Modulo(10) - 5f) * 0.02f;
                life = 100f;
                return;
            }

            life = 50f + Random.Modulo(10);
            if (IsS6Batch18XmasDrop(type))
            {
                scale = 0.7f + (Random.Modulo(10) - 5f) * 0.02f;
                if (type == ClassicFxEffectType.XmasEventBox)
                    scale += 0.3f * f;
                gravity = 15f + Random.Modulo(5);
            }
            else
            {
                scale = 1.6f + (Random.Modulo(10) - 5f) * 0.02f;
                if (type == ClassicFxEffectType.NewYearsDayBeksulki)
                    scale = 2.5f + (Random.Modulo(10) - 5f) * 0.02f;
                if (type == ClassicFxEffectType.NewYearsDayCandy)
                    scale = 3f + (Random.Modulo(10) - 5f) * 0.02f;
                else if (type == ClassicFxEffectType.NewYearsDayPig)
                    scale = 1f + (Random.Modulo(10) - 5f) * 0.02f;
                gravity = 10f + Random.Modulo(10);
            }

            // Main stores degrees; the MonoGame BMD bridge stores radians.
            angle = new Vector3(
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)),
                MathHelper.ToRadians(Random.Modulo(360)));
            Matrix rotation = Matrix.CreateFromYawPitchRoll(
                angle.Y, angle.X, angle.Z);

            if (IsS6Batch18XmasDrop(type))
            {
                Vector3 launch = new Vector3(
                    (Random.Modulo(60) - 30f) * 0.1f,
                    (Random.Modulo(60) - 30f) * 0.1f,
                    -1f);
                direction = Vector3.TransformNormal(launch * 1.5f, rotation);
            }
            else
            {
                Vector3 launch = new Vector3(
                    (Random.Modulo(10) - 5f) * 0.1f,
                    (Random.Modulo(60) - 40f) * 0.1f,
                    0f);
                direction = Vector3.TransformNormal(launch * 1.3f, rotation);
            }
            // Reuse the already pooled HeadAngle.x for m_iAnimation 0/1/2.
            animationAxis.X = Random.Modulo(3);
        }

        private bool MoveS6Batch18Model(ref EffectState e, float f)
        {
            if (e.Type == ClassicFxEffectType.XmasEventIceHeart)
            {
                // Native Move_MODEL_XMAS_EVENT_ICEHEART.
                e.Angle.Z += MathHelper.ToRadians(10f) * f;
                // The native PLAYER_SANTA_2 action gate is outside the
                // ClassicFX owner snapshot. Do not invent an action match.
                return true;
            }

            float spin = MathHelper.ToRadians(
                IsS6Batch18XmasDrop(e.Type) ? 20f : 10f) * f;
            switch ((int)e.HeadAngle.X)
            {
                case 0: e.Angle.X += spin; break;
                case 1: e.Angle.Y += spin; break;
                default: e.Angle.Z += spin; break;
            }

            if (IsS6Batch18XmasDrop(e.Type))
            {
                // MODEL_XMAS_EVENT_BOX/CANDY/TREE/SOCKS shared handler:
                // gravity, terrain+3 rebound, and horizontal launch.
                e.Position.Z += e.Gravity * 0.5f * f;
                e.Gravity -= 1.5f * f;
                float floor = RequestTerrainHeight(e.Position.X, e.Position.Y) + 3f;
                if (e.Position.Z < floor)
                {
                    e.Position.Z = floor;
                    e.Gravity = -e.Gravity * 0.3f;
                    e.LifeTime -= 2f * f;
                }
                e.Position += e.Direction * f;
            }
            else if (IsS6Batch18NewYear(e.Type))
            {
                // MODEL_NEWYEARSDAY_EVENT_BEKSULKI shared handler.
                // Native adds x/y motion at 1.2x and again as Direction.
                e.Position.X += e.Direction.X * 1.2f * f;
                e.Position.Y += e.Direction.Y * 1.2f * f;
                e.Position.Z += e.Gravity * 1.5f * f;
                e.Gravity -= 1.5f * f;
                float floor = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < floor)
                {
                    e.Position.Z = floor;
                    e.Gravity = -e.Gravity * 0.3f;
                    e.LifeTime -= 2f * f;
                }
                e.Position += e.Direction * f;
            }
            return true;
        }
    }
}
