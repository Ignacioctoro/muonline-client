// BroyalMU ClassicFX S6 Batch 13 — Fury Strike / Rageful Blow.
// Source: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// Native CreateEffect + Move_MODEL_SKILL_FURY_STRIKE, 8 EarthQuake BMDs.
// Existing MonoGame BMD ModelObject + fixed ClassicFX Effect/Joint/Particle
// pools only. No second mesh renderer, no damage or network logic.
using System;
using Client.Data.ATT;
using Client.Main.ClassicFX.Data;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch13QuakeType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.FuryQuake1 &&
            type <= ClassicFxEffectType.FuryQuake8;

        private static bool TryGetS6Batch13ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch13QuakeType(type))
                return false;

            int number = (int)type - (int)ClassicFxEffectType.FuryQuake1 + 1;
            float life = number switch
            {
                1 when subType == 0 => 35f,
                1 when subType == 1 => 60f,
                2 when subType is 0 or 1 => 20f,
                2 when subType == 2 => 50f,
                3 when subType == 0 => 35f,
                4 when subType == 0 => 35f,
                5 when subType is 0 or 1 => 40f,
                6 when subType == 0 => 35f,
                7 when subType == 0 => 40f,
                8 when subType is 0 or 1 => 40f,
                _ => 0f
            };
            if (life <= 0f)
                return false;

            definition = new Season6ModelDefinition(
                "Skill/EarthQuake" + number.ToString("D2") + ".bmd",
                life, 1f, useCallerScale: true);
            return true;
        }

        private static void ConfigureS6Batch13ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType)
        {
            if (!IsS6Batch13QuakeType(type)) return;
            int n = (int)type - (int)ClassicFxEffectType.FuryQuake1 + 1;
            // Native CreateEffect(): meshes 1,2,4,5,7,8 use BlendMesh 0.
            if (n is 1 or 2 or 4 or 5 or 7 or 8)
                view.BlendMesh = 0;
        }

        /// <summary>
        /// Strongly typed original PKKey bridge. Native MODEL_SKILL_FURY_STRIKE
        /// children derive their scale from PKKey/100, not from the caller's
        /// actor scaling. No gameplay action is initiated.
        /// </summary>
        public ClassicFxHandle CreateFuryQuake(
            int nativeVariant, int nativePkKey,
            Vector3 position, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, int subType = 0)
        {
            if (nativeVariant is < 1 or > 8 ||
                nativePkKey is <= 0 or > 32000)
                return ClassicFxHandle.Invalid;
            return CreateEffect(
                (ClassicFxEffectType)((int)ClassicFxEffectType.FuryQuake1 +
                                      nativeVariant - 1),
                position, angle, light, owner,
                subType: subType, scale: nativePkKey / 100f);
        }

        private bool MoveS6Batch13Quake(ref EffectState e, float f)
        {
            int n = (int)e.Type - (int)ClassicFxEffectType.FuryQuake1 + 1;
            float life = e.LifeTime;
            switch (n)
            {
                case 1:
                case 4:
                case 7:
                    e.BlendMeshLight = life * 0.1f / 3f;
                    if (life < 10f)
                        e.Position.Z -= 0.5f * f;
                    // Native subtype 1 camera earthquake belongs to camera.
                    return true;

                case 3:
                case 6:
                    e.BlendMeshLight = life * 0.1f / 10f;
                    if (life < 13f)
                        e.Position.Z -= 0.5f * f;
                    return true;

                case 2:
                    if (e.Scale > 50f)
                        return false;
                    if (life >= 10f)
                    {
                        e.BlendMeshLight = (20f - life) * 0.1f;
                    }
                    else
                    {
                        e.BlendMeshLight = life * 0.1f;
                        if (life < 5f)
                            e.Position.Z -= 0.5f * f;
                        if (life >= 5f)
                            SpawnS6FuryQuakeStones(ref e, 10);
                    }
                    AddS6FuryQuakeLight(ref e);
                    ApplyS6FuryQuakeUv(ref e);
                    return true;

                case 5:
                    if (life >= 30f)
                        e.BlendMeshLight = (40f - life) * 0.1f;
                    else
                    {
                        e.BlendMeshLight = life * 0.1f;
                        if (life < 15f)
                            e.Position.Z -= 0.5f * f;
                        if (life >= 5f)
                            SpawnS6FuryQuakeStones(ref e, 15);
                    }
                    AddS6FuryQuakeLight(ref e);
                    ApplyS6FuryQuakeUv(ref e);
                    return true;

                case 8:
                    e.BlendMeshLight = life >= 30f
                        ? (40f - life) * 0.1f : life * 0.1f;
                    if (life < 15f)
                        e.Position.Z -= 0.5f * f;
                    AddS6FuryQuakeLight(ref e);
                    ApplyS6FuryQuakeUv(ref e);
                    return true;
                default:
                    return false;
            }
        }

        // The native BlendMeshTexCoordU = -(int)LifeTime * 0.01f applies
        // only to the BlendMesh (mesh 0), not to all meshes in the BMD.
        private static void ApplyS6FuryQuakeUv(ref EffectState e)
        {
            e.ModelView?.SetFuryQuakeUvScroll(-MathF.Truncate(e.LifeTime) * 0.01f);
        }

        private void SpawnS6FuryQuakeStones(ref EffectState e, int divisor)
        {
            if (!Clock.AdvancedReferenceFrame ||
                !Random.FpsCheck(divisor, Clock))
                return;

            float distance = Random.Modulo(150);
            float theta = MathHelper.ToRadians(Random.Modulo(360));
            Vector3 offset = Vector3.TransformNormal(
                new Vector3(0f, distance, 0f),
                Matrix.CreateRotationZ(theta));
            CreateEffect(Random.Modulo(2) == 0
                    ? ClassicFxEffectType.Stone1
                    : ClassicFxEffectType.Stone2,
                e.Position + offset, e.Angle, e.Light,
                ClassicFxOwner.None, subType: 0);
        }

        private void AddS6FuryQuakeLight(ref EffectState e)
        {
            // MoveEffect() samples native Luminosity in [0.7,1.0] and
            // fades in the last five ticks. Quake 2/5/8 use red terrain
            // lighting for ordinary BK players (blue is WerewolfHero only).
            float luminosity = (Random.Modulo(4) + 7) * 0.1f;
            if (e.LifeTime < 5f)
                luminosity = MathF.Max(0f,
                    luminosity - (5f - e.LifeTime) * 0.2f);
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(luminosity, 0f, 0f), 1f);
        }

        private void InitializeS6FuryStrike(ref Vector3 angle,
            ref Vector3 headAngle, ref float gravity)
        {
            // Native main: AngleZ+=330, HeadAngleX+=80, Z+=180,
            // Gravity=50, subtype random 0..99 (stored in EffectState.Phase).
            headAngle = angle + new Vector3(
                MathHelper.ToRadians(80f), 0f, MathHelper.Pi);
            angle.Z += MathHelper.ToRadians(330f);
            gravity = 50f;
        }

        private bool MoveS6FuryStrike(ref EffectState e,
            int effectIndex, float f)
        {
            // A logical carrier for the original 20-tick animation.
            // RenderFuryStrike() draws an EQUIPPED WEAPON, not a Skill BMD.
            // Its weapon renderer bridge remains separate; it is not faked.
            if (Clock.AdvancedReferenceFrame)
            {
                if (e.LifeTime <= 13f &&
                    (e.TriggerMask & 1) == 0 && e.SubType == 0)
                {
                    e.TriggerMask |= 1;
                    EmitS6FuryTails(ref e);
                    SoundController.Instance.PlayBuffer("Sound/eRageBlow_2.wav");
                }
                if (e.LifeTime <= 11f && (e.TriggerMask & 2) == 0)
                {
                    e.TriggerMask |= 2;
                    EmitS6FuryImpact(ref e, effectIndex);
                }
                if (e.LifeTime <= 10f && (e.TriggerMask & 4) == 0)
                {
                    e.TriggerMask |= 4;
                    EmitS6FuryOutwardCracks(ref e);
                    SoundController.Instance.PlayBuffer("Sound/eRageBlow_3.wav");
                    return false;
                }
            }

            float count = e.LifeTime;
            float deltaDegrees = 15f;
            if (e.LifeTime > 9f && e.LifeTime < 16f)
            {
                count = 12.5f;
                deltaDegrees = 18f;
                if (e.LifeTime <= 15f && (e.TriggerMask & 8) == 0)
                {
                    e.TriggerMask |= 8;
                    e.Gravity = -e.Gravity;
                }
            }
            float angle = (20f - count) * deltaDegrees;
            e.Angle.X += MathHelper.ToRadians(80f * f);
            e.Direction.Y = MathF.Sin(MathHelper.ToRadians(angle)) * 260f;
            e.Gravity += (count == 12.5f ? -8f : 8f) * f;
            Matrix head = Matrix.CreateFromYawPitchRoll(
                e.HeadAngle.Y, e.HeadAngle.X, e.HeadAngle.Z);
            e.Position = e.StartPosition +
                Vector3.TransformNormal(e.Direction, head);
            e.Position.Z += e.Gravity + 200f;
            if (e.ModelView != null)
            {
                // Native RenderFuryStrike() shows the equipped weapon only
                // before the impact. Keep it synchronized with the exact
                // ClassicFX motion state: never run a second weapon animator.
                e.ModelView.Hidden = e.SubType != 0 || e.LifeTime <= 10f;
                e.ModelView.Position = e.Position;
                e.ModelView.Angle = e.Angle;
                e.ModelView.Light = RequestTerrainLight(e.Position.X,
                    e.Position.Y) + e.Light;
                e.ModelView.ApplyNativeRenderState(
                    e.Scale, e.Alpha, e.BlendMeshLight, e.Light);
            }
            return true;
        }

        private void EmitS6FuryTails(ref EffectState e)
        {
            if (!TryGetOwnerSnapshot(e.Owner,
                    out ClassicFxOwnerSnapshot caster))
                return;
            Vector3 offset = Vector3.TransformNormal(
                new Vector3(-25f, -40f, 0f),
                Matrix.CreateRotationZ(caster.Angle.Z));
            Vector3 pos = e.Position + offset;
            for (int i = 0; i < 4; i++)
                CreateEffect(ClassicFxEffectType.Tail,
                    pos - new Vector3(0f, 0f, i * 50f),
                    Vector3.Zero, e.Light, ClassicFxOwner.None);

            pos.X += Random.Modulo(30) + 20f;
            pos.Z += Random.Modulo(500) - 250f;
            for (int i = 0; i < 4; i++)
                CreateEffect(ClassicFxEffectType.Tail,
                    pos - new Vector3(0f, 0f, i * 30f),
                    Vector3.Zero, e.Light, ClassicFxOwner.None);
        }

        private void EmitS6FuryImpact(ref EffectState e, int effectIndex)
        {
            Vector3 impact = e.StartPosition;
            Vector3 zero = Vector3.Zero;
            if (e.SubType != 3 &&
                TryGetOwnerSnapshot(e.Owner, out ClassicFxOwnerSnapshot caster))
            {
                Vector3 offset = Vector3.TransformNormal(
                    new Vector3(-25f, -80f, 0f),
                    Matrix.CreateRotationZ(caster.Angle.Z));
                impact = e.Position + offset;
            }

            impact.Z = RequestTerrainHeight(impact.X, impact.Y) + 25f;
            CreateParticle(ClassicTextureIds.BitmapExplotion,
                impact, zero, Vector3.One, subType: 0, scale: 0.5f);

            if (e.SubType == 0)
            {
                for (int j = 0; j < 8; j++)
                {
                    // Native CreateJoint() / CreateParticle() consume angles
                    // in DEGREES. The parent's EffectState stores radians.
                    Vector3 spin = new Vector3(
                        MathHelper.ToDegrees(e.Angle.X) +
                            Random.Modulo(60) - 60f,
                        MathHelper.ToDegrees(e.Angle.Y),
                        MathHelper.ToDegrees(e.Angle.Z) +
                            Random.Modulo(30) + 90f);
                    Vector3 burst = impact + new Vector3(
                        Random.Modulo(20) - 10f,
                        Random.Modulo(20) - 10f, 0f);
                    CreateJoint(ClassicTextureIds.BitmapJointSpark,
                        burst, burst, spin, subType: 0,
                        scale: 10f);
                    if (Random.FpsCheck(8, Clock))
                        CreateParticle(ClassicTextureIds.BitmapSpark,
                            burst, spin, Vector3.One);
                }
                CreateEffect(ClassicFxEffectType.Wave,
                    impact, zero, e.Light, ClassicFxOwner.None);
            }

            impact.Z -= 27f;
            e.StartPosition = impact;
            ClassicFxOwner parent = ClassicFxOwner.FromClassicFx(
                Pools.Effects.GetHandle(effectIndex));

            for (int n = 1; n <= 3; n++)
            {
                int id = n == 1 ? 3 : n == 2 ? 1 : 2;
                CreateFuryQuake(id, 150, impact, zero, e.Light, parent);
            }
            for (int i = 0; i < 5; i++)
            {
                float direction = MathHelper.ToRadians(e.Phase + i * 72f);
                Vector3 spread = Vector3.TransformNormal(
                    new Vector3(0f, 100f + Random.Modulo(150), 0f),
                    Matrix.CreateRotationZ(direction));
                Vector3 pos = impact + spread;
                if (!IsS6FuryQuakeTileAllowed(pos, primary: true))
                    continue;
                pos.Z = RequestTerrainHeight(pos.X, pos.Y) + 3f;
                Vector3 tilt = new Vector3(
                    0f, 0f, MathHelper.ToRadians(45f + Random.Modulo(30) - 15f));
                int pk = 40 + Random.Modulo(50);
                CreateFuryQuake(4, pk, pos, tilt, e.Light, e.Owner);
                CreateFuryQuake(5, pk, pos, tilt, e.Light, e.Owner);
            }
        }

        // Native wall rules deliberately differ between the two phases:
        // life 11 uses AND on NoMove/NoGround/Water exclusions;
        // life 10 uses OR (allowed unless all three flags coexist).
        private bool IsS6FuryQuakeTileAllowed(in Vector3 point, bool primary)
        {
            if (World?.Terrain == null)
                return true;
            int tx = (int)(point.X / Constants.TERRAIN_SCALE);
            int ty = (int)(point.Y / Constants.TERRAIN_SCALE);
            if ((uint)tx >= (uint)Constants.TERRAIN_SIZE ||
                (uint)ty >= (uint)Constants.TERRAIN_SIZE)
                return false;
            TWFlags wall = World.Terrain.RequestTerrainFlag(tx, ty);
            const TWFlags exclusions = TWFlags.NoMove | TWFlags.NoGround | TWFlags.Water;
            return primary ? (wall & exclusions) == TWFlags.None
                           : (wall & exclusions) != exclusions;
        }

        private void EmitS6FuryOutwardCracks(ref EffectState e)
        {
            // MuMain Move_MODEL_SKILL_FURY_STRIKE LifeTime 10:
            // five evolving paths, four segments each, not five isolated
            // spokes. Every segment emits two genuine EarthQuake BMDs.
            Vector3[] locations = new Vector3[5];
            float[] headings = new float[5];
            for (int i = 0; i < 5; i++)
                locations[i] = e.StartPosition;

            for (int step = 0; step < 4; step++)
            {
                bool positive = step == 3
                    ? Random.Modulo(2) == 0
                    : (step & 1) == 0;

                for (int track = 0; track < 5; track++)
                {
                    float twist = Random.Modulo(30) + 50f;
                    headings[track] += positive ? twist : -twist;
                    float angle = MathHelper.ToRadians(
                        headings[track] + track *
                        (Random.Modulo(10) + 62f));
                    locations[track] += Vector3.TransformNormal(
                        new Vector3(0f, Random.Modulo(15) + 85f, 0f),
                        Matrix.CreateRotationZ(angle));
                    Vector3 point = locations[track];
                    if (!IsS6FuryQuakeTileAllowed(point, primary: false))
                        continue;
                    point.Z = RequestTerrainHeight(point.X, point.Y) + 3f;
                    locations[track] = point;

                    Vector3 facing = new Vector3(0f, 0f,
                        angle + MathHelper.ToRadians(270f));
                    CreateFuryQuake(7, 100, point, facing, e.Light, e.Owner);
                    CreateFuryQuake(8, 100, point, facing, e.Light, e.Owner);
                }
            }
        }
    }
}
