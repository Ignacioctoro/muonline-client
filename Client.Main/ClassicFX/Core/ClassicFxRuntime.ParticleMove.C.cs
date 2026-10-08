using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // Main original: ZzzEffectParticle.cpp, MoveParticles(),
        // desde BITMAP_FLARE hasta BITMAP_TWINTAIL_WATER (inclusive).
        // No agrupa por skill: mantiene Type/SubType y el factor clásico.
        private bool MoveParticleC(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapFlare:
                    MoveClassicFlare(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapFlareRed:
                    p.Scale = MathF.Sin(
                        p.LifeTime * 10f * (MathF.PI / 180f)) * 3f;
                    // El Main exige Owner y una bone transform válida.
                    // Evitamos acceder a un owner reciclado/desconectado.
                    if (!TryApplyClassicHandPosition(ref p))
                        keepAlive = false;
                    return true;

                case ClassicTextureIds.BitmapClud64:
                    MoveClassicClud64(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapLight + 3:
                    if (p.SubType == 0)
                    {
                        float angle = (p.Velocity.X + p.LifeTime) * 0.1f;
                        p.Position.X = p.StartPosition.X + MathF.Sin(angle) * 35f;
                        p.Position.Y = p.StartPosition.Y - MathF.Cos(angle) * 35f;
                        p.Position.Z += p.Gravity * 0.1f * ff;
                        p.Scale -= 0.001f * ff;
                    }
                    else if (p.SubType == 1)
                    {
                        // Main: esta rama solo existe con PJH_ADD_PANDA_PET.
                        // La mantenemos habilitada para aceptar esos datos.
                        p.Scale = MathF.Sin(
                            p.LifeTime * 2f * (MathF.PI / 180f));
                    }
                    return true;

                case ClassicTextureIds.BitmapTwinTailWater:
                    if (p.SubType == 0 || p.SubType == 1)
                        p.Scale -= 0.013f * ff;
                    else if (p.SubType == 2)
                        p.Scale -= 0.026f * ff;

                    p.Position.Z += p.Gravity * ff;
                    p.Alpha -= 0.05f * ff;
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicFlare(ref ClassicParticle p, float ff)
        {
            // Mantener los dos bloques if / else if independientes del Main:
            // la rama LifeTime<=20 tiene prioridad sobre SubType 11 y 12.
            if (p.SubType == 0 || p.SubType == 3 ||
                p.SubType == 6 || p.SubType == 10)
            {
                float count = (p.Velocity.X + p.LifeTime) * 0.1f;
                if (p.SubType == 10)
                {
                    p.Position.X = p.StartPosition.X;
                    p.Position.Y = p.StartPosition.Y;
                }
                else
                {
                    p.Position.X = p.StartPosition.X + MathF.Sin(count) * 40f;
                    p.Position.Y = p.StartPosition.Y - MathF.Cos(count) * 40f;
                }
                p.Position.Z += p.Gravity * ff;
                p.Scale -= 0.002f * ff;
            }
            else if (p.SubType == 2)
            {
                p.Position.X = p.StartPosition.X;
                p.Position.Y = p.StartPosition.Y;
                p.Position.Z += p.Gravity * ((60f - p.LifeTime) / 10f) * ff;
                p.Scale -= 0.002f * ff;
            }
            else if (p.SubType == 5)
            {
                float count = (p.Velocity.X + p.LifeTime) * 0.1f;
                p.Position.X = p.StartPosition.X + MathF.Sin(count) * 40f;
                p.Position.Y = p.StartPosition.Y - MathF.Cos(count) * 40f;
                p.Position.Z -= p.Gravity * ff;
                p.Scale -= 0.002f * ff;
                p.StartPosition.X += 2.5f * ff;
            }

            float fade = MathF.Pow(1f / 1.1f, ff);
            if (p.SubType == 4)
            {
                float count = (p.Velocity.X + p.LifeTime) * 0.1f;
                p.Position.X = p.StartPosition.X + MathF.Sin(count) * 40f;
                p.Position.Y = p.StartPosition.Y - MathF.Cos(count) * 40f;
                p.Position.Z += p.Gravity * ff;
                p.Scale -= 0.004f * ff;
                if (p.LifeTime <= 30f)
                    p.Light *= fade;
            }
            else if (p.LifeTime <= 20f)
            {
                p.Light *= fade;
            }
            else if (p.SubType == 11)
            {
                p.Light *= fade;
                p.Scale += 1.5f * ff;
            }
            else if (p.SubType == 12)
            {
                float count = (p.Velocity.X + p.LifeTime) * 0.1f;
                p.Position.X = p.StartPosition.X + MathF.Sin(count) * 110f;
                p.Position.Y = p.StartPosition.Y - MathF.Cos(count) * 110f;
                p.Position.Z += (p.Gravity + 0.1f) * ff;
                p.Scale -= 0.002f * ff;
                if (p.LifeTime <= 35f)
                    p.Light *= fade;
            }

            if (p.SubType == 0 &&
                TryGetOwnerCurrentAction(p.Target, out int action))
            {
                bool runningOrWalking =
                    action >= (int)PlayerAction.PlayerWalkMale &&
                    action <= (int)PlayerAction.PlayerRunRideWeapon;
                bool swordSkill =
                    action >= (int)PlayerAction.PlayerAttackSkillSword1 &&
                    action <= (int)PlayerAction.PlayerAttackSkillSword5;
                bool rageRun =
                    action == (int)PlayerAction.PlayerRageUniRun ||
                    action == (int)PlayerAction.PlayerRageUniRunOneRight;

                if (runningOrWalking || swordSkill || rageRun)
                {
                    p.SubType = 1;
                    // Main usa std::min<int>, truncando la vida fraccional.
                    p.LifeTime = Math.Min(20, (int)p.LifeTime);
                    p.Velocity = Vector3.Zero;
                    p.StartPosition = p.Position;
                }
            }
        }

        private void MoveClassicClud64(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 0:
                case 11:
                    p.Light = new Vector3(p.LifeTime / 10f);
                    p.Gravity -= 0.01f * ff;
                    p.Position.Z -= p.Gravity * ff;
                    p.Scale -= 0.03f * ff;
                    p.Alpha -= 0.05f * ff;
                    return;

                case 1:
                case 2:
                case 3:
                case 5:
                    p.Scale += (p.SubType == 1 || p.SubType == 2
                        ? 0.5f : 0.08f) * ff;
                    p.Light *= MathF.Pow(1f / 1.1f, ff);
                    if (p.Light.X <= 0.05f)
                        keepAlive = false;
                    return;

                case 6:
                    if (p.LifeTime < 10f)
                        p.Alpha -= 0.2f * ff;
                    else if (p.Alpha < 1f)
                        p.Alpha += (R(2) + 3) * 0.1f * ff;
                    else
                        p.Alpha = 1f;

                    if (p.Alpha < 0.1f)
                        keepAlive = false;
                    p.Light = p.TurningForce * p.Alpha;
                    if (p.Scale > 0f)
                        p.Scale -= (R(5) + 55) * 0.001f * ff;
                    else if (p.Scale < 0.1f)
                        keepAlive = false;
                    p.Position.Z += p.Gravity * ff;
                    FollowClassicParticleOwner(ref p, ref keepAlive);
                    return;

                case 7:
                case 8:
                    if (p.LifeTime < 10f)
                    {
                        p.Alpha -= 0.1f * ff;
                        // Main: factor directo 0.9, no pow(0.9, ff).
                        p.Light *= 0.9f;
                    }
                    else if (p.Alpha < 1f)
                    {
                        p.Alpha += ff * R(20) * 0.005f;
                    }
                    p.Position += p.Velocity * ff;
                    p.Scale += R(10) * 0.01f * ff;
                    return;

                case 9:
                    // El Main invoca rand() de forma independiente para
                    // Scale, los tres canales RGB y Alpha.
                    p.Scale *= MathF.Pow(0.98f + (R(20) - 10) * 0.002f, ff);
                    p.Light.X *= MathF.Pow(0.95f + (R(20) - 10) * 0.002f, ff);
                    p.Light.Y *= MathF.Pow(0.95f + (R(20) - 10) * 0.002f, ff);
                    p.Light.Z *= MathF.Pow(0.95f + (R(20) - 10) * 0.002f, ff);
                    p.Alpha *= MathF.Pow(0.95f + (R(20) - 10) * 0.002f, ff);
                    return;

                case 10:
                    p.Scale -= (R(5) + 15) * 0.0016f * ff;
                    p.Position.Z += p.Gravity * 10f * ff;
                    if (p.Scale < 0f)
                        keepAlive = false;
                    FollowClassicParticleOwner(ref p, ref keepAlive);
                    return;
            }
        }
    }
}
