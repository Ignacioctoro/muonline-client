using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // MuMain: ZzzEffectParticle.cpp / MoveParticles(), lines 7056-7445.
        // Smoke variants, Shiny variants, Cherry Blossom, Pin Light and Orora.
        // Type/SubType and original per-frame units are preserved.
        private bool MoveParticleH(
            ref ClassicParticle p,
            float ff,
            ref bool keepAlive)
        {
            switch (p.Type)
            {
                case ClassicTextureIds.BitmapSmoke + 1:
                case ClassicTextureIds.BitmapSmoke + 4:
                    MoveClassicSmokeTrailing(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapShiny:
                    MoveClassicShiny(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapCherryBlossomEventPetal:
                    MoveClassicCherryPetal(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapCherryBlossomEventFlower:
                    MoveClassicCherryFlower(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapShiny + 1:
                    MoveClassicShinyPlusOne(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapShiny + 2:
                    p.Scale = MathF.Sin(p.LifeTime * 10f * (MathF.PI / 180f)) * 3f;
                    if (!TryApplyClassicHandPosition(ref p))
                        keepAlive = false;
                    return true;

                case ClassicTextureIds.BitmapShiny + 4:
                    MoveClassicShinyPlusFour(ref p, ff);
                    return true;

                case ClassicTextureIds.BitmapShiny + 6:
                    MoveClassicShinyPlusSix(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapPinLight:
                    MoveClassicPinLight(ref p, ff, ref keepAlive);
                    return true;

                case ClassicTextureIds.BitmapOrora:
                    MoveClassicOrora(ref p, ff, ref keepAlive);
                    return true;

                default:
                    return false;
            }
        }

        private void MoveClassicSmokeTrailing(ref ClassicParticle p, float ff)
        {
            p.Light = new Vector3(p.LifeTime / 32f);
            p.Scale += 0.08f * ff;
            p.Position += p.Velocity * ff;
            // Main uses a direct 0.9 scaling without FPS_ANIMATION_FACTOR.
            p.Velocity *= 0.9f;

            if (p.SubType == 6)
            {
                p.Scale -= 0.05f * ff;
                p.Position.Z += 4f * ff;
            }
            else if (p.SubType != 5 &&
                     Textures.TryGet(p.Type, out ClassicTextureResource bitmap))
            {
                // Original: RequestTerrainHeight + Bitmaps[o->Type].Height * Scale / 2.
                // Do not invent a bitmap height if its resource is missing.
                p.Position.Z = RequestTerrainHeight(p.Position.X, p.Position.Y)
                    + bitmap.Data.Height * p.Scale * 0.5f;
            }
        }

        private void MoveClassicShiny(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            switch (p.SubType)
            {
                case 2:
                    p.Rotation -= 12f * ff;
                    p.Velocity.Z -= R(8) * 0.05f * ff;
                    p.Position += p.Velocity * ff;
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    // Main: a new one-frame BITMAP_LIGHT sprite each Move tick.
                    CreateSprite(ClassicTextureIds.BitmapLight,
                        p.Position, p.Scale / 1.5f, p.Light);
                    return;

                case 3:
                    p.Light *= MathF.Pow(1f / 1.05f, ff);
                    p.Scale -= 0.04f * ff;
                    p.Alpha -= 0.001f * ff;
                    if (p.Alpha <= 0f) keepAlive = false;
                    return;

                case 4:
                    p.Rotation += 20f * ff;
                    p.Position.Z += p.Gravity * 0.5f * ff;
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    p.Gravity -= 1.5f * ff;
                    p.Scale -= 0.01f * ff;
                    p.Position += p.Velocity * ff;
                    return;

                case 5:
                    p.Rotation += 10f * ff;
                    p.Position.Z += p.Gravity * ff;
                    p.Light *= MathF.Pow(1f / 1.02f, ff);
                    p.Scale -= 0.01f * ff;
                    return;

                case 6:
                case 8:
                case 9:
                    MoveClassicShinyLong(ref p, ff, ref keepAlive);
                    return;

                case 7:
                    p.Scale = MathF.Sin(p.LifeTime * 2f * (MathF.PI / 180f));
                    return;

                default:
                    p.Scale = MathF.Sin(p.LifeTime * 10f * (MathF.PI / 180f));
                    if (p.SubType == 1)
                    {
                        p.Scale *= MathF.Pow(0.75f, ff);
                        p.Rotation -= 12f * ff;
                    }
                    return;
            }
        }

        private void MoveClassicShinyLong(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            p.Light *= MathF.Pow(1f / 1.01f, ff);
            if (p.Light.X < 0.2f)
                keepAlive = false;

            p.StartPosition.X *= MathF.Pow(1f / 1.01f, ff);
            if (p.LifeTime < 60f && Random.FpsCheck(2, Clock))
                p.Scale = 0f;
            else
                p.Scale = p.StartPosition.X;

            if (p.LifeTime >= 60f)
                return;

            p.Velocity *= MathF.Pow(1f / 1.04f, ff);
            p.Position.Z -= p.Gravity * 0.1f * ff;
            p.Gravity += 0.5f * ff;

            if (p.SubType == 9 && Random.FpsCheck(5, Clock))
            {
                Vector3 position = p.Position;
                // In the Main these 5 random calls occur in the same order.
                position.X += R(40) - 20;
                position.Y += R(40) - 20;
                Vector3 light = new Vector3(
                    0.8f + R(200) * 0.001f,
                    0.5f + R(200) * 0.001f,
                    0.1f + R(100) * 0.001f);
                CreateParticle(ClassicTextureIds.BitmapShiny,
                    position, p.Angle, light, subType: 8, scale: 1f);
            }
        }

        private void MoveClassicCherryPetal(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            if (p.SubType == 0)
            {
                p.Rotation = R(360);
                p.Scale = MathF.Sin(p.LifeTime * (MathF.PI / 180f));
                p.Alpha -= 0.001f * ff;
                if (p.Alpha <= 0f) keepAlive = false;
            }
            else if (p.SubType == 1)
            {
                if ((int)p.LifeTime == 69)
                    p.Velocity -= new Vector3(ff);

                p.Light *= MathF.Pow(1f / 1.01f, ff);
                p.Alpha -= 0.001f * ff;
                if (p.Alpha <= 0f) keepAlive = false;
            }
        }

        private void MoveClassicCherryFlower(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            if (p.SubType != 0)
                return;

            p.Scale = MathF.Sin(p.LifeTime * (MathF.PI / 180f));
            p.Alpha -= 0.002f * ff;
            if (p.Alpha <= 0f) keepAlive = false;
            p.Position.Z += p.Gravity * 0.5f * ff;
            p.Gravity -= 1.5f * ff;
            p.Rotation = R(360);

            float height = RequestTerrainHeight(p.Position.X, p.Position.Y);
            if (p.Position.Z < height + 3f)
            {
                p.Position.Z = height + 3f;
                p.Gravity = -p.Gravity * 0.3f;
                p.LifeTime -= 2f * ff;
            }
        }

        private void MoveClassicShinyPlusOne(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            if (p.SubType == 99)
            {
                if (TryGetOwnerPosition(p.Target, out Vector3 position))
                {
                    p.Position = position;
                    p.Position.Z += 50f * ff;
                }
                else
                    keepAlive = false;
                p.Rotation -= ff;
            }
            else if (p.SubType == 5)
            {
                if (TryGetOwnerSnapshot(p.Target, out ClassicFxOwnerSnapshot snapshot))
                    p.Angle = snapshot.Angle;
                else
                    keepAlive = false;
                p.Scale -= 0.06f * ff;
                p.Position.Z += p.Gravity * 10f * ff;
            }
            else
            {
                p.Scale = MathF.Sin(p.LifeTime * 5f * (MathF.PI / 180f)) * 5f;
                if (p.SubType >= 2)
                {
                    p.Scale *= MathF.Pow(0.75f, ff);
                    p.Rotation -= 12f * ff;
                }
                if (!TryApplyClassicHandPosition(ref p))
                    keepAlive = false;
            }
        }

        private void MoveClassicShinyPlusFour(ref ClassicParticle p, float ff)
        {
            if (p.SubType == 0)
            {
                p.Scale = MathF.Sin(p.LifeTime * 10f * (MathF.PI / 180f)) * 3f + 2f;
                p.Light = new Vector3(p.LifeTime / 5f);
            }
            else if (p.SubType == 1)
            {
                p.Scale = MathF.Sin(p.Gravity * (MathF.PI / 180f)) * 3f + 1.5f;
                p.Gravity += (15f - p.LifeTime) * 6f * ff;
                if (p.Gravity > 90f)
                    p.Gravity = 90f;
                if (p.LifeTime < 6f)
                    p.Light *= MathF.Pow(0.5f, ff);
            }
            else if (p.SubType == 2)
            {
                p.Scale *= MathF.Pow(1.1f, ff);
                // Source uses VectorScale(o->Light, 0.9f) without FPS.
                p.Light *= 0.9f;
            }
        }

        private void MoveClassicShinyPlusSix(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            if (p.SubType == 0)
            {
                p.Rotation += 5f * ff;
                p.Scale -= 0.02f * ff;
                p.Alpha -= 0.001f * ff;
                if (p.Alpha <= 0f) keepAlive = false;
                p.Position.Z -= p.Gravity * ff;
                float lightChange = R(10) / 100f - 0.05f;
                p.Light += new Vector3(lightChange * ff);
            }
            else if (p.SubType == 1)
            {
                p.Scale -= 0.01f * ff;
                p.Rotation -= 5f * ff;
            }
        }

        private void MoveClassicPinLight(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            p.Scale -= 0.02f * ff;
            p.Alpha -= 0.001f * ff;
            if (p.Alpha <= 0f) keepAlive = false;

            if (p.SubType == 1)
            {
                // Original rotates Position itself; its local (-150 Y) vector
                // is declared but never used. Preserve that quirk exactly.
                p.Position = Rotate(p.Position, p.Angle);
            }
            else
            {
                p.Position.Z -= p.Gravity * ff;
            }

            float lightChange = R(10) / 100f - 0.05f;
            p.Light += new Vector3(lightChange * ff);
        }

        private void MoveClassicOrora(
            ref ClassicParticle p, float ff, ref bool keepAlive)
        {
            // Main: subtypes 0,2 -> bone 37; 1,3 -> bone 28.
            // Other subtype values leave fScale/fLight undefined in C++;
            // reject unsupported input rather than invent transforms.
            int bone;
            float scaleStep;
            float lightFactor;
            switch (p.SubType)
            {
                case 0:
                    bone = 37; scaleStep = 0.01f; lightFactor = 1.05f;
                    p.Rotation += 5f * ff;
                    break;
                case 1:
                    bone = 28; scaleStep = 0.01f; lightFactor = 1.05f;
                    p.Rotation -= 5f * ff;
                    break;
                case 2:
                    bone = 37; scaleStep = 0.04f; lightFactor = 1.33f;
                    p.Rotation += 20f * ff;
                    break;
                case 3:
                    bone = 28; scaleStep = 0.04f; lightFactor = 1.33f;
                    p.Rotation -= 20f * ff;
                    break;
                default:
                    keepAlive = false;
                    return;
            }

            if (TryGetOwnerBonePosition(p.Target, bone, out Vector3 position))
                p.Position = position;
            else
            {
                keepAlive = false;
                return;
            }

            p.Scale += scaleStep * ff;
            if (p.Scale >= 0.8f)
                p.Light *= MathF.Pow(1f / lightFactor, ff);
        }
    }
}
