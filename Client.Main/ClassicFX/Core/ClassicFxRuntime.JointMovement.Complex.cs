using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Remaining joint movement family batch: MuMain ZzzEffectJoint.cpp,
    /// source lines 4248-4612, 4619-4732, 5059-5350.
    /// Extends the existing two movement files, not one file per type.
    /// Network, gameplay, effects and source-only OBJECT fields are callbacks.
    /// </summary>
    public sealed partial class ClassicFxRuntime
    {
        // Native m_vPosSword (not equivalent to model root Position).
        public Func<ClassicFxOwner, Vector3?> JointSwordWorldPositionResolver { get; set; }

        // Original character helper/mount and SafeZone status for SPEARSKILL 4/9/10/11.
        // Second parameter: requested query: 'fenrir-mounted' or 'rideable-mounted'.
        public Func<int, string, bool?> JointSpearMountStateResolver { get; set; }
        public Func<int, bool?> JointSpearSafeZoneResolver { get; set; }

        // Native Attack/Defense/HelpNpc aura buffs. Null means unknown:
        // don't prematurely kill persistent aura when not yet wired.
        public Func<ClassicFxOwner, bool?> JointSpearAuraBuffResolver { get; set; }

        // Native OBJECT* Target has mutable Lifetime/Alpha and its own owner.
        // The resolver is deliberately read-only; never change a network object
        // from the effects subsystem directly.
        public Func<ClassicFxOwner, float?> JointSpearOwnerLifeTimeResolver { get; set; }
        public Func<ClassicFxOwner, float?> JointSpearOwnerAlphaResolver { get; set; }

        private static bool IsJointMoveComplexType(int type) =>
            type == ClassicModelSpearSkill ||
            type == ClassicModelFenrirSkillThunder ||
            type == ClassicTextureIds.BitmapBlur + 1 ||
            type == ClassicTextureIds.BitmapJointLaser + 1 ||
            type == ClassicTextureIds.BitmapJointThunder + 1 ||
            type == ClassicTextureIds.BitmapJointThunder;

        private bool MoveJointComplex(ref ClassicJoint j, int slot, ClassicFxHandle handle)
        {
            float f = Clock.FrameFactor;
            if (f <= 0f) return true;

            // ZzzEffectJoint.cpp MoveJoint(): common entry step. None of these
            // six branches is in the excluded-type list (SPEAR 5-8 ARE).
            ClassicMatrix3x4 originalMatrix = ClassicMath.AngleMatrix(j.Angle);
            bool excluded =
                (j.Type == ClassicModelSpearSkill &&
                 (j.SubType == 5 || j.SubType == 6 ||
                  j.SubType == 7 || j.SubType == 8)) ||
                (j.Type == ClassicTextureIds.BitmapJointThunder && j.SubType == 15);
            if (!excluded && j.Velocity != 0f)
                j.Position += ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), originalMatrix) * f;
            if (j.Type == ClassicTextureIds.BitmapJointThunder)
                return MoveThunderComplex(ref j, f);

            bool alive;
            if (j.Type == ClassicModelSpearSkill)
                alive = MoveSpearComplex(ref j, slot, handle, f);
            else if (j.Type == ClassicModelFenrirSkillThunder)
                alive = MoveFenrirComplex(ref j, f);
            else if (j.Type == ClassicTextureIds.BitmapJointThunder + 1)
                alive = MoveThunderPlusComplex(ref j, handle, f);
            else
                alive = MoveBlurLaserComplex(ref j, f);
            if (!alive) return false;

            // Native common CreateTail() after the switch. Internal tail
            // operations in each branch are separate and intentionally kept.
            if (j.CreateTails)
                AppendJointTailMoveA(ref j, originalMatrix);
            j.LifeTime -= f;
            return j.LifeTime >= 0f;
        }

        // --------------------------------------------------------------
        // MODEL_FENRIR_SKILL_THUNDER (native 4619-4666)
        // --------------------------------------------------------------
        private bool MoveFenrirComplex(ref ClassicJoint j, float f)
        {
            int count = Math.Clamp(j.MaxTails, 0, ClassicJoint.MaxTailSegments);
            for (int i = 0; i < count; i++)
            {
                if (j.Target.HasOwner)
                {
                    if (!TryGetOwnerPosition(j.Target, out Vector3 target))
                        return false;
                    j.TargetPosition = target;
                    j.TargetPosition.Z += 80f;
                }
                float distance = MoveJointHummingA(ref j.Angle,
                    j.Position, j.TargetPosition, 50f, f);
                float scale = MathF.Abs(j.Scale) > 0.00001f ? j.Scale : 1f;
                j.Direction.X = (R(1024) - 512f) / scale;
                j.Direction.Z = (R(1024) - 512f) / scale;
                ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle + j.Direction);
                AppendJointTailMoveA(ref j, matrix);
                if (distance < j.Velocity * 1.5f)
                {
                    if (j.Scale == 50f)
                    {
                        CreateParticleFpsChecked(ClassicTextureIds.BitmapEnergy,
                            j.Position, j.Angle, j.Light);
                        if (Random.FpsCheck(8, Clock))
                        {
                            // Main consumes random offset but passes joint.Position
                            // into CreateParticle, not that offset position.
                            _ = R(64); _ = R(64); _ = R(64);
                            CreateParticleFpsChecked(ClassicTextureIds.BitmapSmoke,
                                j.Position, j.Angle, j.Light);
                        }
                    }
                    break;
                }
                if (j.Scale >= 50f)
                {
                    float lum = (R(4) + 4f) * 0.04f;
                    AddClassicTerrainLight(j.Position.X, j.Position.Y,
                        new Vector3(lum * 0.4f), 2f);
                }
                Vector3 advance = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity * f, 0f), matrix);
                j.Position += advance * f;
            }
            return true;
        }

        // --------------------------------------------------------------
        // BITMAP_BLUR+1 / BITMAP_JOINT_LASER+1 (4667-4732)
        // --------------------------------------------------------------
        private bool MoveBlurLaserComplex(ref ClassicJoint j, float f)
        {
            int count = Math.Clamp(j.MaxTails, 0, ClassicJoint.MaxTailSegments);
            for (int i = 0; i < count; i++)
            {
                if (j.SubType != 2)
                {
                    if (!TryGetOwnerPosition(j.Target, out Vector3 target))
                        return false;
                    j.TargetPosition = target;
                    j.TargetPosition.Z += 80f;
                }
                float distance = MoveJointHummingA(ref j.Angle, j.Position,
                    j.TargetPosition, 25f, f);
                float scale = MathF.Abs(j.Scale) > 0.00001f ? j.Scale : 1f;
                j.Direction.X += f * (R(256) - 128f) / scale;
                j.Direction.Z += f * (R(256) - 128f) / scale;
                j.Direction *= MathF.Pow(0.8f, f);
                ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle + j.Direction);
                AppendJointTailMoveA(ref j, matrix);
                if (distance <= j.Velocity * 2f * f)
                {
                    if (Random.FpsCheck(2, Clock))
                    {
                        // Original computes an unused target jitter, then
                        // emits BITMAP_FIRE at o->Position. Consume RNG.
                        _ = R(64); _ = R(64); _ = R(64);
                        CreateParticleFpsChecked(ClassicTextureIds.BitmapFire,
                            j.Position, j.Angle, j.Light);
                    }
                    break;
                }
                float lum = (R(4) + 4f) * 0.05f;
                Vector3 color = j.Type == ClassicTextureIds.BitmapJointLaser + 1
                    ? j.SubType == 1
                        ? new Vector3(lum, lum * 0.1f, lum * 0.1f)
                        : new Vector3(lum, lum * 0.6f, lum * 0.3f)
                    : new Vector3(0f, lum * 0.1f, lum * 0.2f);
                AddClassicTerrainLight(j.Position.X, j.Position.Y, color, 2f);
                Vector3 offset = ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity, 0f), matrix);
                j.Position += offset * f;
            }
            return true;
        }

        // --------------------------------------------------------------
        // BITMAP_JOINT_THUNDER + 1 (5059-5350)
        // --------------------------------------------------------------
        private bool MoveThunderPlusComplex(ref ClassicJoint j,
            ClassicFxHandle handle, float f)
        {
            int max = Math.Clamp(j.MaxTails, 0, ClassicJoint.MaxTailSegments);
            ClassicMatrix3x4 matrix = ClassicMath.AngleMatrix(j.Angle);
            int s = j.SubType;
            if (s == 0)
            {
                if (j.LifeTime > 15f)
                {
                    Vector3 target = j.TargetPosition +
                        new Vector3(R(200) - 100f, R(100) - 50f, 0f);
                    j.Position = j.StartPosition;
                    for (int i = 0; i < max; i++)
                    {
                        MoveJointHummingA(ref j.Angle,
                            j.Position, target, R(80) + 60f, f);
                        float scale = MathF.Abs(j.Scale) > 0.00001f ? j.Scale : 1f;
                        j.Direction.X = (R(1400) - 700f) / scale;
                        j.Direction.Z = (R(1400) - 700f) / scale;
                        matrix = ClassicMath.AngleMatrix(j.Angle + j.Direction);
                        AppendJointTailMoveA(ref j, matrix);
                        j.Position += ClassicMath.VectorRotate(
                            new Vector3(0f, -j.Velocity * f, 0f), matrix) * f;
                    }
                }
                else j.Light.Z -= 10.12f * f;
            }
            else if (s == 1 || s == 2 || s == 3 || s == 5 ||
                     s == 6 || s == 7 || s == 8 || s == 9 ||
                     s == 10 || s == 11 || s == 12)
            {
                j.Position = j.StartPosition;
                if (s == 11 || s == 12)
                    MoveThunderPlusShapeComplex(ref j, matrix, max, f);
                else if (s == 9 || s == 10)
                    MoveThunderPlusHorizontalComplex(ref j, matrix, max, f);
                else
                    MoveThunderPlusStrikeComplex(ref j, matrix, max, f);
                if (s != 11 && s != 12 && j.LifeTime < 4f)
                    j.Light *= MathF.Pow(1f / 1.2f, f);
                if (s == 2)
                {
                    Vector3 color = new Vector3(j.Light.X * 0.5f,
                        j.Light.Y * 0.6f, j.Light.Z);
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        j.TargetPosition, 1.5f, color, R(360));
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        j.TargetPosition, 1.5f, color, R(360));
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapTrueBlue,
                        j.TargetPosition, j.Angle, color);
                }
                else if (s == 3)
                {
                    Vector3 color = new Vector3(j.Light.X * 0.5f,
                        j.Light.Y * 0.6f, j.Light.Z);
                    JointMoveEffectRequested?.Invoke("MODEL_ICE",
                        j.TargetPosition, j.Angle, color, 0);
                }
                else if (s == 6)
                {
                    j.TargetPosition.Z = RequestTerrainHeight(
                        j.TargetPosition.X, j.TargetPosition.Y) + 30f;
                    CreateParticleFpsChecked(ClassicTextureIds.BitmapTrueBlue,
                        j.TargetPosition, j.Angle, j.Light, 0, 2f);
                }
            }
            else if (s == 4)
            {
                if (!TryGetOwnerPosition(j.Target, out Vector3 anchor))
                    return false;
                j.Position = anchor;
                Vector3 delta = (j.TargetPosition - j.Position) * j.StartPosition.X;
                int first = Math.Max(0, j.TargetIndices?[0] ?? 0);
                int second = Math.Max(first, j.TargetIndices?[1] ?? 0);
                for (int i = 0; i < first; i++)
                {
                    AppendJointTailMoveA(ref j, matrix);
                    j.Position += delta * f;
                    j.Position += new Vector3(R(20) - 10f,
                        R(20) - 10f, R(20) - 10f) * f;
                }
                delta = (j.TargetPosition - j.Position) * j.StartPosition.Y;
                for (int i = first; i < second; i++)
                {
                    j.Position += delta * f;
                    AppendJointTailMoveA(ref j, matrix);
                }
                j.Position = j.TargetPosition;
                AppendJointTailMoveA(ref j, matrix);
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    j.TargetPosition, 1.5f, j.Light, R(360));
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    j.TargetPosition, 1.5f, j.Light, R(360));
                if (j.LifeTime < 8f)
                    j.Light *= MathF.Pow(1f / 1.4f, f);
            }
            // Native toggles the flag for these subtype families after the
            // internal tail generation, controlling common post-switch tail.
            if (s == 0 || s == 2 || s == 3 || s == 4 || s == 5 ||
                s == 6 || s == 8 || s == 9 || s == 10)
                j.CreateTails = j.LifeTime > 10f;
            return true;
        }

        private void MoveThunderPlusStrikeComplex(ref ClassicJoint j,
            in ClassicMatrix3x4 matrix, int max, float f)
        {
            int early = Math.Max(0, max - 5);
            int s = j.SubType;
            int jitter = s == 8 ? 10 : 20;
            float dz = s == 6 ? 13f : s == 7 || s == 8 ? 20f : 16f;
            for (int i = 0; i < early; i++)
            {
                j.Position.X += (R(jitter) - jitter * 0.5f) * f;
                j.Position.Y += (R(jitter) - jitter * 0.5f) * f;
                j.Position.Z -= dz * f;
                AppendJointTailMoveA(ref j, matrix);
            }
            Vector3 d = (j.TargetPosition - j.Position) * 0.2f;
            for (int i = early; i < max - 1; i++)
            {
                j.Position += d * f;
                j.Position.X += (R(jitter) - jitter * 0.5f) * f;
                j.Position.Y += (R(jitter) - jitter * 0.5f) * f;
                AppendJointTailMoveA(ref j, matrix);
            }
            j.Position = j.TargetPosition;
            AppendJointTailMoveA(ref j, matrix);
        }

        private void MoveThunderPlusHorizontalComplex(ref ClassicJoint j,
            in ClassicMatrix3x4 matrix, int max, float f)
        {
            int early = Math.Max(0, max - 5);
            bool xAxis = j.SubType == 9;
            for (int i = 0; i < early; i++)
            {
                if (xAxis) j.Position.X += 21f * f;
                else j.Position.Y += 20f * f;
                AppendJointTailMoveA(ref j, matrix);
            }
            Vector3 d = (j.TargetPosition - j.Position) * 0.2f;
            for (int i = early; i < max - 1; i++)
            {
                j.Position += d * f;
                if (xAxis)
                {
                    j.Position.Y += (R(20) - 10f) * f;
                    j.Position.Z += (R(20) - 10f) * f;
                }
                else
                {
                    j.Position.X += (R(20) - 10f) * f;
                    j.Position.Z += (R(20) - 10f) * f;
                }
                AppendJointTailMoveA(ref j, matrix);
            }
            j.Position = j.TargetPosition;
            AppendJointTailMoveA(ref j, matrix);
        }

        private void MoveThunderPlusShapeComplex(ref ClassicJoint j,
            in ClassicMatrix3x4 matrix, int max, float f)
        {
            int early = Math.Max(0, max - 5);
            if (j.SubType == 11)
            {
                Vector3 stride = ClassicMath.VectorRotate(
                    new Vector3(0f, j.Scale / 1.3f, 0f), matrix) * f;
                for (int i = 0; i < early; i++)
                {
                    j.Position += stride;
                    AppendJointTailMoveA(ref j, matrix);
                    j.TargetPosition = j.Position;
                }
                j.Position = j.StartPosition;
                int scatter = Math.Max(1, (int)(j.Scale / 8f));
                for (int i = 0; i < early; i++)
                {
                    j.Position += stride;
                    j.Position += new Vector3(R(scatter * 2) - scatter,
                        R(scatter * 2) - scatter,
                        R(scatter * 2) - scatter) * f;
                    AppendJointTailMoveA(ref j, matrix);
                }
            }
            else
            {
                int scatter = Math.Max(1, (int)(j.Scale / 5f));
                for (int i = 0; i < max - 1; i++)
                {
                    j.Position += new Vector3(R(scatter * 2) - scatter,
                        R(scatter * 2) - scatter,
                        R(scatter * 2) - scatter) * f;
                    AppendJointTailMoveA(ref j, matrix);
                    j.Position += (j.TargetPosition - j.Position) * 0.08f * f;
                }
            }
            j.Position = j.TargetPosition;
            AppendJointTailMoveA(ref j, matrix);
        }

        // --------------------------------------------------------------
        // MODEL_SPEARSKILL (4248-4612)
        // --------------------------------------------------------------
        private bool MoveSpearComplex(ref ClassicJoint j, int slot,
            ClassicFxHandle handle, float f)
        {
            if (j.CharacterIndex >= 0 &&
                JointSpearSafeZoneResolver?.Invoke(j.CharacterIndex) is bool safe)
            {
                bool fenrir = JointSpearMountStateResolver?.Invoke(
                    j.CharacterIndex, "fenrir-mounted") == true;
                bool rideable = JointSpearMountStateResolver?.Invoke(
                    j.CharacterIndex, "rideable-mounted") == true;
                if (j.SubType == 4 && fenrir && !safe) j.SubType = 9;
                else if (j.SubType == 9 && (!fenrir || safe)) j.SubType = 4;
                else if (j.SubType == 10 && rideable && !safe) j.SubType = 11;
                else if (j.SubType == 11 && (!rideable || safe)) j.SubType = 10;
            }

            if (j.SubType == 2)
            {
                if (!TryGetOwnerSnapshot(j.Target, out _))
                {
                    // Native breaks, preserving joint until common step.
                    return true;
                }
                j.Scale = j.LifeTime * 3f;
                float t = MathHelper.Clamp((j.LifeTime - 10f) / 10f, 0f, 1f);
                Vector3 screw = GetSpearScrewComplex(slot * 17721, f) + j.TargetPosition;
                Vector3? sword = JointSwordWorldPositionResolver?.Invoke(j.Target);
                if (!sword.HasValue) return false; // Do not fake m_vPosSword.
                j.Position = sword.Value * (1f - t) + screw * t;
                return true;
            }
            if (j.SubType == 3)
            {
                if (!TryGetOwnerSnapshot(j.Target, out ClassicFxOwnerSnapshot snap))
                    return false;
                if (Random.FpsCheck(5, Clock)) j.LifeTime -= 5f * f;
                else if (Random.FpsCheck(2, Clock)) j.LifeTime += f;
                j.Direction.X = MathF.Sin((j.LifeTime - j.Direction.Y) * 0.05f) * 3f;
                j.Position.X += j.Direction.X * f;
                float? otherLifetime = JointSpearOwnerLifeTimeResolver?.Invoke(j.Target);
                if (otherLifetime.HasValue)
                {
                    float alpha = otherLifetime > 50f
                        ? (100f - otherLifetime.Value) / 40f
                        : otherLifetime.Value / 10f;
                    j.Light = snap.Light * MathF.Min(alpha, 1f);
                }
                return true;
            }
            if (j.SubType >= 5 && j.SubType <= 7)
            {
                for (int i = 0; i < 3; i++)
                {
                    j.Angle.Z += 10f * f;
                    var matrix = ClassicMath.AngleMatrix(j.Angle);
                    j.Position = j.StartPosition + ClassicMath.VectorRotate(j.Direction, matrix);
                    AppendJointTailMoveA(ref j, matrix);
                    if (j.Weapon == 0f)
                    {
                        j.Direction.Y *= MathF.Pow(0.95f, f);
                        if (j.Direction.Y < 10f) j.Direction.Y = -10f;
                    }
                    if (j.Weapon > 40f)
                    {
                        j.Direction.Y -= 20f * f;
                        j.Angle.Z -= 5f * f;
                        j.Scale += 15f * f;
                    }
                    if (j.Direction.Y < 0f)
                    {
                        if (j.Weapon == 40f && j.SubType <= 6)
                            JointMoveEffectRequested?.Invoke("BITMAP_SHOCK_WAVE",
                                j.StartPosition, j.Angle, j.Light, 3);
                        j.Weapon += f;
                        if (j.Weapon < 20f)
                        {
                            j.Direction.Y = -30f;
                            j.Scale = 40f;
                        }
                        else j.StartPosition.Z += 5f * f;
                    }
                }
                if (j.LifeTime < 10f) j.Light *= MathF.Pow(1f / 1.2f, f);
                j.Scale -= 5f * f;
                return true;
            }
            if (j.SubType == 8)
            {
                j.Angle.Z += 25f * f;
                var matrix = ClassicMath.AngleMatrix(j.Angle);
                j.Position = j.StartPosition + ClassicMath.VectorRotate(j.Direction, matrix);
                AppendJointTailMoveA(ref j, matrix);
                j.StartPosition.Z += 15f * f;
                return true;
            }

            if (!TryGetOwnerPosition(j.Target, out Vector3 ownerPosition))
                return false;
            if (j.SubType == 10 || j.SubType == 11)
            {
                float lum = (R(4) + 4f) * 0.1f;
                AddClassicTerrainLight(j.Position.X, j.Position.Y,
                    new Vector3(lum * 1.5f, lum * 0.6f, lum * 0.6f), 1f);
                if (Random.FpsCheck(10, Clock))
                {
                    lum *= MathF.Pow(0.2f, f);
                    CreateParticle(ClassicTextureIds.BitmapSpark + 1,
                        j.Position, j.Angle,
                        new Vector3(0.9f + lum, 0.5f + lum, 0.5f + lum), 19);
                }
            }
            if (j.SubType == 4 || j.SubType == 9)
            {
                bool? hasBuff = JointSpearAuraBuffResolver?.Invoke(j.Target);
                if (hasBuff == false) return false;
                if (hasBuff == true) j.LifeTime = 100f;
            }

            bool tailReanchor = j.SubType == 0 || j.SubType == 4 || j.SubType == 9 ||
                j.SubType == 10 || j.SubType == 11 || j.SubType == 14 || j.SubType == 16;
            if (tailReanchor)
                TranslateJointTailsComplex(ref j, -j.TargetPosition);

            if (j.SubType == 14)
            {
                if (!TryGetOwnerBonePosition(j.Target, 37, out Vector3 wrist))
                    return false;
                j.TargetPosition = wrist;
            }
            else if (j.SubType == 15 || j.SubType == 17)
            {
                Vector3? masterPos = JointSpearOwnerPositionResolver?.Invoke(j.Target);
                if (!masterPos.HasValue) return false;
                TranslateJointTailsComplex(ref j, -j.StartPosition);
                j.StartPosition = masterPos.Value;
                j.TargetPosition = ownerPosition;
                TranslateJointTailsComplex(ref j, j.StartPosition);
            }
            else
            {
                j.TargetPosition = ownerPosition;
                j.TargetPosition.Z += 10f;
            }
            if (tailReanchor)
                TranslateJointTailsComplex(ref j, j.TargetPosition);

            int tick = (int)(Clock.WorldTimeMilliseconds / 40f);
            int key = ((slot % 2 != 0) ? tick : -tick) + slot * 53731;
            float rate = j.SubType == 1 ? 0.5f : 1f;
            Vector3 direction = GetSpearScrewComplex(key, f, rate);
            switch (j.SubType)
            {
                case 0: case 4:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 80f, direction.Y * 80f,
                        110f + direction.Z * 120f);
                    break;
                case 9:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 80f, direction.Y * 80f,
                        140f + direction.Z * 120f);
                    break;
                case 10:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 60f, direction.Y * 60f,
                        50f + direction.Z * 60f);
                    break;
                case 11:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 60f, direction.Y * 60f,
                        100f + direction.Z * 60f);
                    break;
                case 1:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 70f, direction.Y * 70f,
                        direction.Z * 140f);
                    float add = MathF.Sin((key + 11111f) * 0.1113f * rate);
                    j.Light = new Vector3(0.2f, 0.2f, 0.4f + 0.2f * add);
                    break;
                case 14:
                    j.LifeTime = 100f;
                    j.Position = j.TargetPosition + direction * 15f;
                    break;
                case 15:
                    j.LifeTime = 100f;
                    if (JointSpearOwnerAlphaResolver?.Invoke(j.Target) is float alpha)
                        j.Light = new Vector3(alpha);
                    j.Position = j.TargetPosition;
                    break;
                case 16:
                    j.Position = j.TargetPosition + new Vector3(
                        direction.X * 20f, direction.Y * 20f,
                        100f + direction.Z * 40f);
                    break;
                case 17:
                    j.LifeTime = 100f;
                    j.Position = j.TargetPosition;
                    break;
            }
            return true;
        }

        private static void TranslateJointTailsComplex(ref ClassicJoint j,
            in Vector3 delta)
        {
            if (j.Tails == null) return;
            int used = Math.Clamp(j.NumTails, 0, ClassicJoint.MaxTailSegments);
            for (int i = used - 1; i >= 0; i--)
            {
                int b = i * ClassicJoint.VerticesPerTail;
                for (int k = 0; k < ClassicJoint.VerticesPerTail; k++)
                    j.Tails[b + k] += delta;
            }
        }

        private Vector3 GetSpearScrewComplex(int index, float f, float rate = 1f)
        {
            float a = 0.048f * MathF.Pow(rate, f);
            float b = 0.0613f * MathF.Pow(rate, f);
            float c = 0.1113f * MathF.Pow(rate, f);
            float s = MathF.Sin((index + 55555f) * a);
            float v0 = s * MathF.Cos(index * b);
            float v1 = s * MathF.Sin(index * b);
            float v2 = MathF.Cos((index + 55555f) * a);
            float sn = MathF.Sin((index + 11111f) * c);
            float cs = MathF.Cos((index + 11111f) * c);
            return new Vector3(cs * v1 - sn * v2,
                sn * v1 + cs * v2, v0);
        }
          // The original chain lightning code reads game character objects
        // by CharacterIndex. Do not assume index == network object id.
        public Func<short, Vector3?> JointThunderTargetPositionResolver { get; set; }
        public Func<int, Vector3?> JointThunderMonsterPositionResolver { get; set; }
        // Original global g_fBoneSave[3] for subtype 12.
        public Func<ClassicFxOwner, Vector3?> JointThunderSavedBone3Resolver { get; set; }

        // --------------------------------------------------------------
        // BITMAP_JOINT_THUNDER (4733-5058)
        // --------------------------------------------------------------
        private bool MoveThunderComplex(ref ClassicJoint j, float f)
        {
            // Native early break: no subtype switch or internal tails.
            if ((j.SubType == 6 && j.LifeTime > 4f) || j.SubType == 8)
            {
                if (j.CreateTails)
                    AppendJointTailMoveA(ref j, ClassicMath.AngleMatrix(j.Angle));
                j.LifeTime -= f;
                return j.LifeTime >= 0f;
            }
            Vector3 randomizedEndpoint = j.TargetPosition;
            if (j.SubType == 4 || j.SubType == 5)
            {
                j.Position = j.TargetPosition;
                if (j.SubType == 4)
                {
                    Vector3 pos = j.TargetPosition;
                    pos.Z += 30f * f;
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                        pos, (R(8) + 8f) * 0.2f, j.Light, R(360));
                }
            }
            else if (j.SubType == 9)
            {
                randomizedEndpoint = j.TargetPosition +
                    new Vector3(R(200) - 100f, R(200) - 100f, 0f);
                j.Position = j.StartPosition;
            }
            else if (j.SubType == 6 || j.SubType == 7 ||
                     j.SubType == 18 || j.SubType == 19)
                j.Position = j.StartPosition;

            int count = Math.Clamp(j.MaxTails, 0, ClassicJoint.MaxTailSegments);
            for (int step = 0; step < count && j.SubType != 15; step++)
            {
                float distance = 0f;
                int subtype = j.SubType;
                Vector3 destination = j.TargetPosition;
                float turn = 25f;
                switch (subtype)
                {
                    case 0: case 1: case 2: case 16: case 21:
                    case 27: case 28: case 33: case 25:
                        if (j.Target.HasOwner)
                        {
                            if (!TryGetOwnerPosition(j.Target, out destination))
                                return false;
                            destination.Z += 80f;
                            j.TargetPosition = destination;
                        }
                        turn = subtype == 25 ? 40f : 50f;
                        break;
                    case 3: turn = 50f; break;
                    case 4:
                        destination.Z -= 300f;
                        turn = -10f;
                        break;
                    case 5:
                        destination.Z += 600f;
                        // The native calls MoveHumming(Position,Angle,o->Position,-10)
                        // here, mutating angle using destination as the 'from'.
                        MoveJointHummingA(ref j.Angle,
                            destination, j.Position, -10f, f);
                        distance = Vector3.Distance(destination, j.Position);
                        turn = float.NaN;
                        break;
                    case 6:
                        if (!TryGetOwnerPosition(j.Target, out destination))
                            return false;
                        destination += new Vector3((2050f + R(200)) * f,
                            (2050f + R(200)) * f, -10000f * f);
                        j.TargetPosition = destination;
                        turn = R(100) + 50f;
                        break;
                    case 7: turn = R(100) + 50f; break;
                    case 9:
                        destination = randomizedEndpoint;
                        turn = R(80) + 60f;
                        break;
                    case 12:
                    {
                        Vector3? saved = JointThunderSavedBone3Resolver?.Invoke(j.Target);
                        if (!saved.HasValue) return false;
                        j.Position = saved.Value;
                        turn = float.NaN;
                        break;
                    }
                    case 18: turn = 110f; break;
                    case 19: turn = 25f + j.Scale; break;
                    case 20:
                        j.TargetPosition.Z += 100f;
                        destination = j.TargetPosition;
                        turn = 100f;
                        break;
                    case 22: case 23: case 24:
                    {
                        if (!j.Target.HasOwner) return false;
                        if (step == 0)
                        {
                            Vector3 pos;
                            if (subtype == 24)
                            {
                                if (!TryGetOwnerPosition(j.Target, out pos))
                                    return false;
                                pos.Z += 80f * f;
                            }
                            else if (!TryGetOwnerBonePosition(j.Target,
                                subtype == 22 ? 37 : 28, out pos))
                                return false;
                            // Original creates a preliminary tail before copying
                            // the owner-bone position and character destination.
                            j.Direction.X = R(1024) - 512f / MathF.Max(1f, j.Scale);
                            j.Direction.Z = R(1024) - 512f / MathF.Max(1f, j.Scale);
                            AppendJointTailMoveA(ref j,
                                ClassicMath.AngleMatrix(j.Angle + j.Direction));
                            j.Position = pos;
                            Vector3? chainTarget = JointThunderTargetPositionResolver?.Invoke(j.TargetIndexRef);
                            if (!chainTarget.HasValue) return false;
                            j.TargetPosition = chainTarget.Value;
                            j.TargetPosition.Z += 80f;
                        }
                        destination = j.TargetPosition;
                        turn = j.Velocity;
                        break;
                    }
                }
                if (!float.IsNaN(turn))
                    distance = MoveJointHummingA(ref j.Angle,
                        j.Position, destination, turn, f);

                float scale = MathF.Abs(j.Scale) > 0.00001f ? j.Scale : 1f;
                if (subtype == 1 || subtype == 28)
                {
                    j.Direction.X = R(256) - 128f;
                    j.Direction.Z = R(256) - 128f;
                }
                else if (subtype == 4) j.Direction.X = R(64) - 32f;
                else if (subtype == 5) j.Direction.X = R(32) - 16f;
                else if (subtype == 6 || subtype == 7)
                {
                    j.Direction.X = R(100) + 20f;
                    j.Direction.Z = R(100) + 20f;
                }
                else if (subtype == 11)
                {
                    j.Direction.X = (R(1024) - 512f) * 0.7f / scale;
                    j.Direction.Z = (R(1024) - 512f) * 0.7f / scale;
                }
                else if (subtype == 18)
                {
                    j.Direction.X = R(64) - 32f;
                    j.Direction.Z = R(64) - 32f;
                }
                else if (subtype == 19)
                {
                    j.Direction.X = (R(1024) - 512f) / scale;
                    j.Direction.Y = (R(1024) - 512f) / scale;
                    j.Direction.Z = (R(1024) - 512f) / scale;
                }
                else
                {
                    j.Direction.X = (R(1024) - 512f) / scale;
                    j.Direction.Z = (R(1024) - 512f) / scale;
                }
                var matrix = ClassicMath.AngleMatrix(j.Angle + j.Direction * f);
                AppendJointTailMoveA(ref j, matrix);

                if (subtype == 3 && distance > 150f) j.LifeTime = 0f;
                else if (distance < j.Velocity * 1.5f &&
                    subtype != 6 && subtype != 9 && subtype != 7 &&
                    subtype != 18 && subtype != 19)
                {
                    if (j.Scale == 50f)
                    {
                        CreateParticleFpsChecked(ClassicTextureIds.BitmapEnergy,
                            j.Position, j.Angle, j.Light);
                        if (Random.FpsCheck(8, Clock))
                        {
                            _ = R(64); _ = R(64); _ = R(64);
                            CreateParticle(ClassicTextureIds.BitmapSmoke,
                                j.Position, j.Angle, j.Light);
                        }
                        if ((subtype == 0 || subtype == 27) &&
                            Clock.WorldTimeMilliseconds % 1000 < 500 &&
                            Random.FpsCheck(16, Clock))
                        {
                            Vector3 point = j.TargetPosition + new Vector3(
                                R(100) - 50f, R(100) - 50f, R(120) - 60f);
                            CreateJoint(ClassicTextureIds.BitmapJointThunder,
                                point, j.TargetPosition, j.Angle, 28,
                                j.Target, R(8) + 6f, priorColor: j.Light);
                        }
                    }
                    break;
                }
                if (j.Scale >= 50f && subtype != 4 && subtype != 7 &&
                    (subtype == 0 || subtype == 11 || subtype == 2 ||
                     subtype == 18 || subtype == 27))
                {
                    float lum = (R(4) + 4f) * 0.04f;
                    Vector3 light = subtype == 2
                        ? new Vector3(lum * 0.4f, lum * 0.1f, lum * 0.1f)
                        : new Vector3(lum * 0.1f, lum * 0.1f, lum * 0.5f);
                    if (subtype == 27) light = j.Light;
                    AddClassicTerrainLight(j.Position.X, j.Position.Y, light, 2f);
                }
                j.Position += ClassicMath.VectorRotate(
                    new Vector3(0f, -j.Velocity * f, 0f), matrix) * f;
            }

            if (j.SubType == 7 || j.SubType == 18)
                j.Position = j.TargetPosition;
            else if (j.SubType == 15 && ((int)j.LifeTime % 2) == 0)
            {
                CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                    j.Position, j.StartPosition, j.Angle, 0, scale: 50f);
                CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                    j.Position, j.StartPosition, j.Angle, 0, scale: 10f);
                CreateParticleFpsChecked(ClassicTextureIds.BitmapEnergy,
                    j.Position, j.Angle, j.Light);
                Vector3 chain = j.StartPosition;
                chain.Z += 100f * f;
                int maxTargets = Math.Min(Math.Max(0, (int)j.MultiUse),
                    (j.TargetIndices?.Length ?? 0) * 15);
                for (int i = 0; i < maxTargets; i += 15)
                {
                    Vector3? monster = JointThunderMonsterPositionResolver?.Invoke(
                        j.TargetIndices[i / 15]);
                    if (!monster.HasValue) continue;
                    j.TargetPosition = monster.Value;
                    j.TargetPosition.Z += 100f;
                    Vector3 angle = j.Angle;
                    angle.Z = CreateJointAngleA(chain.X, chain.Y,
                        j.TargetPosition.X, j.TargetPosition.Y);
                    CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                        chain, j.TargetPosition, angle, 0, scale: 50f);
                    CreateJointFpsChecked(ClassicTextureIds.BitmapJointThunder,
                        chain, j.TargetPosition, angle, 0, scale: 10f);
                    chain = j.TargetPosition;
                }
                if (j.MultiUse < j.Weapon)
                    j.MultiUse += f;
            }
            if (j.CreateTails)
                AppendJointTailMoveA(ref j, ClassicMath.AngleMatrix(j.Angle));
            j.LifeTime -= f;
            return j.LifeTime >= 0f;
        }
    }
}
