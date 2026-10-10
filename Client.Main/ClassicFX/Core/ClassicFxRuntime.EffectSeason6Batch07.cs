// BroyalMU ClassicFX S6 Batch 07: shared Fire/Ice/Blizzard/Snow families.
// Original client: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// References: ZzzEffect.cpp, Behaviors/MoveHandlers.cpp, ZzzOpenData.cpp.
// Rendering delegates to the existing ModelObject BMD path; child FX use the
// shared fixed pools. No duplicate damage, combat, networking or skill casts.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch07ModelType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.Fire or ClassicFxEffectType.Ice or
                ClassicFxEffectType.IceSmall or ClassicFxEffectType.Blizzard or
                ClassicFxEffectType.Snow1 or ClassicFxEffectType.Snow2 or
                ClassicFxEffectType.Snow3;

        private static bool TryGetS6Batch07ModelDefinition(
            ClassicFxEffectType type, int subType,
            out Season6ModelDefinition definition)
        {
            definition = default;
            switch (type)
            {
                case ClassicFxEffectType.Fire:
                    // 6/7/8 need PKKey + special owner/joint setup; not silently
                    // implemented as regular fire or mapped to another asset.
                    if (subType is not (0 or 1 or 2 or 3 or 4 or 5 or 9)) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Fire01.bmd",
                        subType == 3 ? 80f : subType == 9 ? 600f :
                        subType == 1 ? 60f : 40f,
                        subType == 3 ? 0.3f : 1f,
                        useCallerScale: subType == 5);
                    return true;
                case ClassicFxEffectType.Ice:
                    // Native subtypes 1/2 depend on target Harden buff and
                    // action state; reject until wired with server debuffs.
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Ice01.bmd", 50f, 0.8f);
                    return true;
                case ClassicFxEffectType.IceSmall:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Ice02.bmd", 40f, 1f);
                    return true;
                case ClassicFxEffectType.Blizzard:
                    if (subType is < 0 or > 2) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/blizzard.bmd", subType == 1 ? 20f : 29f,
                        subType == 1 ? 1f : 0.5f);
                    return true;
                case ClassicFxEffectType.Snow1:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        "Skill/Snow01.bmd", 20f, 1.2f);
                    return true;
                case ClassicFxEffectType.Snow2:
                case ClassicFxEffectType.Snow3:
                    if (subType != 0) return false;
                    definition = new Season6ModelDefinition(
                        type == ClassicFxEffectType.Snow2
                            ? "Skill/Snow02.bmd" : "Skill/Snow03.bmd",
                        40f, 1f);
                    return true;
                default:
                    return false;
            }
        }

        private void InitializeS6Batch07Spawn(
            ClassicFxEffectType type, int subType,
            ref Vector3 position, ref Vector3 angle, ref Vector3 light,
            ref float scale, ref float life,
            ref Vector3 direction, ref float velocity,
            ref float gravity, ref float meshLight)
        {
            switch (type)
            {
                case ClassicFxEffectType.Fire:
                    if (subType is 0 or 2)
                    {
                        scale = (10f + Random.Modulo(8)) * 0.1f;
                        direction = new Vector3(0f, 0f, -50f);
                        if (subType == 0)
                        {
                            position.X += 130f + Random.Modulo(32);
                            position.Z += 400f;
                            angle = new Vector3(0f, MathHelper.ToRadians(20f), 0f);
                        }
                    }
                    else if (subType == 4)
                    {
                        scale = (15f + Random.Modulo(10)) * 0.1f;
                        position.X += 130f + Random.Modulo(32);
                        position.Z += 400f;
                        direction = new Vector3(0f, -(10f + Random.Modulo(20)),
                            -(20f + Random.Modulo(10)));
                        angle = new Vector3(0f, MathHelper.ToRadians(20f), 0f);
                    }
                    else if (subType == 3)
                        direction = new Vector3(0f, -12f, 0f);
                    else if (subType == 5)
                    {
                        gravity = 5f;
                        direction = new Vector3(0f, -30f, 0f);
                    }
                    else if (subType == 9)
                    {
                        scale = (20f + Random.Modulo(10)) * 0.13f;
                        position.X += (130f + Random.Modulo(32)) * Clock.FrameFactor;
                        position.Z += (400f + Random.Modulo(32)) * Clock.FrameFactor;
                        direction = new Vector3(0f, -(8f + Random.Modulo(5)),
                            -(10f + Random.Modulo(10)));
                        angle = Vector3.Zero;
                        meshLight = 0f;
                    }
                    else // native generic fallback for subtype 1.
                    {
                        scale = (8f + Random.Modulo(4)) * 0.1f;
                        position.Z += 120f * Clock.FrameFactor;
                        direction = new Vector3(0f, -50f, 0f);
                    }
                    break;
                case ClassicFxEffectType.Ice:
                    angle.X = 0f;
                    velocity = 1f;
                    break;
                case ClassicFxEffectType.IceSmall:
                case ClassicFxEffectType.Snow2:
                case ClassicFxEffectType.Snow3:
                    life = 32f + Random.Modulo(16);
                    scale = (8f + Random.Modulo(4)) * 0.1f;
                    angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                    direction = Vector3.TransformNormal(
                        new Vector3(0f, (64f + Random.Modulo(256)) * 0.1f, 0f),
                        Matrix.CreateRotationZ(angle.Z));
                    gravity = 8f + Random.Modulo(16);
                    if (type == ClassicFxEffectType.IceSmall)
                        position.Z += 50f * Clock.FrameFactor;
                    break;
                case ClassicFxEffectType.Blizzard:
                    if (subType is 0 or 2)
                    {
                        life = 15f + Random.Modulo(15);
                        gravity = -20f - (10f + Random.Modulo(30)) * Clock.FrameFactor;
                        velocity = Random.Modulo(360);
                        scale = 0.5f;
                        light = Vector3.Zero;
                        position += new Vector3(Random.Modulo(300) - 150f,
                            Random.Modulo(300) - 150f, 600f);
                        position.X += 100f * Clock.FrameFactor;
                    }
                    else
                        velocity = 0f;
                    break;
                case ClassicFxEffectType.Snow1:
                    Matrix orientation = Matrix.CreateFromYawPitchRoll(
                        angle.Y, angle.X, angle.Z);
                    position += Vector3.TransformNormal(
                        new Vector3(0f, -40f, 150f), orientation) * Clock.FrameFactor;
                    direction = new Vector3(0f, -40f, 10f);
                    break;
            }
        }

        private static void ConfigureS6Batch07ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subType)
        {
            switch (type)
            {
                case ClassicFxEffectType.Fire:
                    view.BlendMesh = 1;
                    if (subType is 3 or 5) view.HiddenMesh = 0;
                    break;
                case ClassicFxEffectType.Ice:
                    view.BlendMesh = 0;
                    break;
                case ClassicFxEffectType.IceSmall:
                    view.BlendMesh = 0;
                    break;
                case ClassicFxEffectType.Blizzard:
                    view.BlendMesh = -2;
                    break;
            }
        }

        private bool MoveS6Batch07Model(ref EffectState e, float f)
        {
            return e.Type switch
            {
                ClassicFxEffectType.Fire => MoveS6Fire(ref e, f),
                ClassicFxEffectType.Ice => MoveS6Ice(ref e, f),
                ClassicFxEffectType.IceSmall => MoveS6SnowFragment(ref e, f),
                ClassicFxEffectType.Blizzard => MoveS6Blizzard(ref e, f),
                ClassicFxEffectType.Snow1 => MoveS6Snow1(ref e, f),
                ClassicFxEffectType.Snow2 or ClassicFxEffectType.Snow3 =>
                    MoveS6SnowFragment(ref e, f),
                _ => false
            };
        }

        private void EmitS6IceOrFireDebris(ref EffectState e, Vector3 tint)
        {
            Vector3 impact = e.Position + new Vector3(0f, 0f, 80f);
            CreateParticle(ClassicTextureIds.BitmapExplotion, impact, e.Angle, tint);
            for (int i = 0; i < 6; i++)
                CreateEffect(Random.Modulo(2) == 0
                        ? ClassicFxEffectType.Stone1 : ClassicFxEffectType.Stone2,
                    e.Position, e.Angle, e.Light, ClassicFxOwner.None);
        }

        private bool MoveS6Fire(ref EffectState e, float f)
        {
            if (e.Direction != Vector3.Zero)
            {
                Matrix orientation = Matrix.CreateFromYawPitchRoll(
                    e.Angle.Y, e.Angle.X, e.Angle.Z);
                e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;
            }
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.SubType is 0 or 2 or 4)
            {
                if (e.Position.Z <= ground)
                {
                    e.Position.Z = ground;
                    EmitS6IceOrFireDebris(ref e, Vector3.One);
                    return false;
                }
            }
            else if (e.SubType == 5)
            {
                e.Position.Z += e.Gravity * f;
                e.Gravity -= 2f * f;
                if (e.Position.Z <= ground)
                {
                    e.Gravity = 10f;
                    e.Position.Z += 10f * f;
                    e.Direction.Y *= MathF.Pow(0.5f, f);
                    e.Scale *= MathF.Pow(1.1f, f);
                }
                if (e.LifeTime <= 1f)
                {
                    e.Position.Z = ground;
                    EmitS6IceOrFireDebris(ref e, Vector3.One);
                    return false;
                }
            }
            else if (e.SubType == 9 && e.Position.Z <= ground)
                return false;

            float lum = 0.7f + 0.1f * Random.Modulo(4);
            Vector3 glow = e.SubType == 9
                ? new Vector3(1f, 0.2f, 0.2f)
                : new Vector3(lum, lum * 0.1f, 0f);
            AddClassicTerrainLight(e.Position.X, e.Position.Y, glow,
                e.SubType == 9 ? 4f : 2f);
            if (Clock.AdvancedReferenceFrame)
            {
                if (e.SubType == 9)
                {
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Direction, 36, 1f + e.Scale);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Direction, 37, 2f + e.Scale);
                }
                else if (e.SubType is not 3)
                {
                    CreateParticle(ClassicTextureIds.BitmapFire,
                        e.Position, e.Angle, glow, 5);
                }
                else if (Random.Modulo(2) == 0)
                    CreateParticle(ClassicTextureIds.BitmapFire,
                        e.Position, e.Angle, glow, 5);
            }
            e.BlendMeshLight = e.SubType == 0
                ? (4f + Random.Modulo(4)) * 0.1f : 0f;
            return true;
        }

        private bool MoveS6Ice(ref EffectState e, float f)
        {
            // MODEL_ICE 0: native fades once BMD animation passes frame 5.
            // Use the ModelObject's actual animation clock, not skill cast time.
            if (e.ModelView != null && e.ModelView.CurrentFrame >= 5f)
            {
                e.Velocity = 0f;
                e.Alpha -= 0.05f * f;
                if (Clock.AdvancedReferenceFrame && Random.Modulo(2) == 0)
                {
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(64) - 32f, Random.Modulo(64) - 32f,
                        Random.Modulo(128) + 32f);
                    CreateParticle(ClassicTextureIds.BitmapSmoke, p,
                        e.Angle, e.Light);
                }
                if (e.Alpha <= 0f) return false;
            }
            float lum = 0.7f + 0.1f * Random.Modulo(4);
            AddClassicTerrainLight(e.Position.X, e.Position.Y,
                new Vector3(-lum * 0.4f, -lum * 0.3f, -lum * 0.2f), 2f);
            return true;
        }

        private bool MoveS6SnowFragment(ref EffectState e, float f)
        {
            // MODEL_ICE_SMALL / SNOW2/3 share the native throw/bounce handler.
            e.Position += e.Direction * f;
            e.Direction *= MathF.Pow(0.9f, f);
            e.Position.Z += e.Gravity * f;
            e.Gravity -= 3f * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.Position.Z <= ground)
            {
                e.Position.Z = ground;
                e.Gravity = -e.Gravity * 0.5f;
                e.LifeTime -= 4f * f;
                e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
            }
            else
                e.Angle.X -= MathHelper.ToRadians(e.Scale * 32f * f);
            if (Clock.AdvancedReferenceFrame && Random.Modulo(10) == 0)
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    e.Position, e.Angle, e.Light);
            return true;
        }

        private bool MoveS6Snow1(ref EffectState e, float f)
        {
            Matrix orientation = Matrix.CreateFromYawPitchRoll(
                e.Angle.Y, e.Angle.X, e.Angle.Z);
            e.Position += Vector3.TransformNormal(e.Direction, orientation) * f;
            e.Direction.Z -= 2.5f * f;
            // CheckTargetRange is gameplay-dependent; this fallback only
            // processes ground interception, never client-side damage.
            if (e.Position.Z > RequestTerrainHeight(e.Position.X, e.Position.Y))
                return true;
            for (int i = 0; i < 2; i++)
            {
                CreateEffect(Random.Modulo(2) == 0
                        ? ClassicFxEffectType.Snow2 : ClassicFxEffectType.Snow3,
                    e.Position, e.Angle, e.Light, ClassicFxOwner.None);
                CreateParticle(ClassicTextureIds.BitmapSmoke,
                    e.Position, e.Angle, e.Light);
            }
            return false;
        }

        private bool MoveS6Blizzard(ref EffectState e, float f)
        {
            if (e.SubType == 1)
            {
                e.BlendMeshLight *= MathF.Pow(1f / 1.1f, f);
                return true;
            }
            // Original StartPosition tracks the horizontal base independently
            // of noisy per-tick XY offset while Gravity drives falling Z.
            if (Clock.AdvancedReferenceFrame)
            {
                float noise = e.SubType == 2 ? 50f : 10f;
                e.Position.X = e.StartPosition.X +
                    MathF.Sin(Random.Modulo(1000) * 0.01f) * noise;
                e.Position.Y = e.StartPosition.Y +
                    MathF.Sin(Random.Modulo(1000) * 0.01f) * noise;
                e.StartPosition.X -= 10f; // one native tick, not render frame
                e.Gravity -= Random.Modulo(5);
            }
            e.Position.Z += e.Gravity * f;
            float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
            if (e.SubType == 2 && e.Position.Z <= ground)
                return true; // native subtype 2 has no impact children
            if (Clock.AdvancedReferenceFrame)
            {
                if (e.SubType == 0)
                {
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, Vector3.One, 0, 1.5f);
                    if (Random.Modulo(2) == 0)
                        CreateParticle(ClassicTextureIds.BitmapEnergy,
                            e.Position, e.Angle, Vector3.One, 1, 0.5f);
                    else
                        CreateParticle(ClassicTextureIds.BitmapFire + 2,
                            e.Position, e.Angle, Vector3.One, 7, e.Scale);
                }
                else if (Random.Modulo(2) == 0)
                    CreateParticle(ClassicTextureIds.BitmapFire + 2,
                        e.Position, e.Angle, Vector3.One, 7, e.Scale);
            }
            e.Light = new Vector3(e.Light.X + 0.1f * f);
            if (Clock.AdvancedReferenceFrame)
            {
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    e.Position, (4f + Random.Modulo(4)) * 0.2f, e.Light);
                CreateSprite(ClassicTextureIds.BitmapLight,
                    e.Position, 1f, e.Light);
            }
            if (e.SubType != 0 || e.Position.Z >= ground)
                return true;

            Vector3 impact = e.Position;
            impact.Z = ground + 50f;
            Vector3 iceLight = new Vector3(0.24f, 0.28f, 0.8f);
            CreateParticle(ClassicTextureIds.BitmapSmoke,
                impact, e.Angle, iceLight, 11,
                (80f + Random.Modulo(32)) * 0.025f);
            if (Random.Modulo(5) == 0)
                CreateEffect(ClassicFxEffectType.IceSmall,
                    impact, e.Angle, e.Light, ClassicFxOwner.None);
            CreateEffect(ClassicFxEffectType.Blizzard,
                impact, e.Angle, e.Light, ClassicFxOwner.None, subType: 1);
            return false;
        }
    }
}
