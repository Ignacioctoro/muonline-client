// ClassicFX S6 Batch 26: physical combat visual BMD models.
// Based on pinned MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// Native CreateEffect, Behaviors/MoveHandlers and EffectTypes.json.
// Shared MonoGame ModelObject: no duplicate effect renderer.
using System;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch26ModelType(ClassicFxEffectType type) =>
            type >= ClassicFxEffectType.ShieldCrashModel &&
            type <= ClassicFxEffectType.ArrowAutoLoadModel;

        private static bool IsS6Batch26Shield(ClassicFxEffectType type) =>
            type == ClassicFxEffectType.ShieldCrashModel ||
            type == ClassicFxEffectType.ShieldCrashRing;

        private static bool TryGetS6Batch26ModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch26ModelType(type)) return false;
            if (type == ClassicFxEffectType.ShieldCrashModel &&
                (subtype < 0 || subtype > 2)) return false;
            if (type == ClassicFxEffectType.ArrowAutoLoadModel &&
                subtype != 1) return false;
            if (type != ClassicFxEffectType.ShieldCrashModel &&
                type != ClassicFxEffectType.ArrowAutoLoadModel &&
                subtype != 0) return false;
            string path = type switch
            {
                ClassicFxEffectType.ShieldCrashModel => "Effect/atshild.bmd",
                ClassicFxEffectType.ShieldCrashRing => "Effect/atshild2.bmd",
                ClassicFxEffectType.ComboModel => "Skill/combo.bmd",
                ClassicFxEffectType.FissureModel => "Skill/bossrock.bmd",
                ClassicFxEffectType.FissureLight => "Skill/bossrocklight.bmd",
                ClassicFxEffectType.WaterWaveModel => "Skill/seamanfx.bmd",
                ClassicFxEffectType.IronRiderArrowModel => "Effect/ironobj.bmd",
                ClassicFxEffectType.KentaurosArrowModel => "Effect/cantasarrow.bmd",
                ClassicFxEffectType.DragonLowerDummy => "Effect/knight_plancrack_dragon.bmd",
                ClassicFxEffectType.BalgasSkillModel => "Skill/WaveForce.bmd",
                ClassicFxEffectType.DarkElfSkillModel => "Skill/elf_skill.bmd",
                ClassicFxEffectType.ArrowAutoLoadModel => "Skill/arrowsrefill.bmd",
                _ => null
            };
            if (path == null) return false;
            float life = IsS6Batch26Shield(type) ? 24f :
                type == ClassicFxEffectType.ComboModel ? 20f :
                type == ClassicFxEffectType.FissureModel ||
                type == ClassicFxEffectType.FissureLight ? 120f :
                type == ClassicFxEffectType.WaterWaveModel ? 20f :
                type == ClassicFxEffectType.IronRiderArrowModel ? 12f :
                type == ClassicFxEffectType.KentaurosArrowModel ? 34f :
                type == ClassicFxEffectType.DragonLowerDummy ? 300f :
                type == ClassicFxEffectType.BalgasSkillModel ? 20f :
                type == ClassicFxEffectType.DarkElfSkillModel ? 30f : 40f;
            float scale = IsS6Batch26Shield(type) ? 1.1f :
                type == ClassicFxEffectType.FissureModel ||
                type == ClassicFxEffectType.FissureLight ? 0.8f :
                type == ClassicFxEffectType.WaterWaveModel ? 0.4f :
                type == ClassicFxEffectType.IronRiderArrowModel ? 0.5f :
                type == ClassicFxEffectType.KentaurosArrowModel ? 0.7f :
                type == ClassicFxEffectType.DarkElfSkillModel ? 0f :
                type == ClassicFxEffectType.ArrowAutoLoadModel ? 1.2f : 1f;
            definition = new Season6ModelDefinition(
                path, life, scale,
                needsOwner: IsS6Batch26Shield(type) ||
                    type == ClassicFxEffectType.ArrowAutoLoadModel,
                useCallerScale: type == ClassicFxEffectType.DragonLowerDummy);
            return true;
        }

        private void ConfigureS6Batch26ModelView(
            ClassicFxEffectModelObject view, ClassicFxEffectType type,
            int subtype)
        {
            if (type == ClassicFxEffectType.ComboModel ||
                type == ClassicFxEffectType.WaterWaveModel ||
                type == ClassicFxEffectType.DarkElfSkillModel)
                view.BlendMesh = -2;
            else if (type == ClassicFxEffectType.BalgasSkillModel)
                view.BlendMesh = 0;
            else if (type == ClassicFxEffectType.ShieldCrashModel &&
                subtype != 0)
                view.HiddenMesh = 0;
        }

        private bool InitializeS6Batch26Model(
            ClassicFxEffectType type, int subtype, ClassicFxOwner owner,
            Vector3 inputLight, ref Vector3 pos, ref Vector3 start,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref float alpha, ref float mesh,
            ref Vector3 direction, ref Vector3 head, ref float velocity,
            ref float gravity)
        {
            if (IsS6Batch26Shield(type))
            {
                if (owner.WorldObject == null) return false;
                life = 24f;
                scale = type == ClassicFxEffectType.ShieldCrashModel &&
                    subtype == 2 ? 0.7f : 1.1f;
                light = new Vector3(0.5f, 0.5f, 1f);
                direction = light;
                gravity = type == ClassicFxEffectType.ShieldCrashRing
                    ? 0.3f : velocity;
                return true;
            }
            if (type == ClassicFxEffectType.ComboModel)
            {
                life = 20f;
                gravity = 0.1f;
                mesh = 1f;
                angle = Vector3.Zero;
                start = pos;
                pos.Z += 50f * Clock.FrameFactor;
                return true;
            }
            if (type == ClassicFxEffectType.FissureModel ||
                type == ClassicFxEffectType.FissureLight)
            {
                life = 120f;
                scale = 0.8f;
                return true;
            }
            if (type == ClassicFxEffectType.WaterWaveModel)
            {
                life = 20f;
                scale = 0.4f;
                start = pos;
                velocity = -40f;
                gravity = 120f;
                direction = new Vector3(0f, -40f, 0f);
                mesh = 0.2f;
                light = Vector3.One;
                return true;
            }
            if (type == ClassicFxEffectType.IronRiderArrowModel)
            {
                life = 12f;
                scale = 0.5f;
                velocity = 70f;
                light = new Vector3(0.8f, 1f, 0.8f);
                direction = Vector3.TransformNormal(new Vector3(0f,-1f,0f),
                    Matrix.CreateFromYawPitchRoll(angle.Y,angle.X,angle.Z));
                return true;
            }
            if (type == ClassicFxEffectType.KentaurosArrowModel)
            {
                life = 34f;
                scale = 0.7f;
                velocity = 70f;
                alpha = 0f;
                light = Vector3.One;
                direction = Vector3.TransformNormal(new Vector3(0f,-1f,0f),
                    Matrix.CreateFromYawPitchRoll(angle.Y,angle.X,angle.Z));
                return true;
            }
            if (type == ClassicFxEffectType.DragonLowerDummy)
            {
                life = 300f;
                velocity = 0.3f;
                alpha = 1f;
                pos.Z = RequestTerrainHeight(pos.X,pos.Y) + 10f;
                angle.Z = MathHelper.ToRadians(45f + Random.Modulo(180));
                return true;
            }
            if (type == ClassicFxEffectType.BalgasSkillModel)
            {
                life = 20f;
                scale = 1f;
                return true;
            }
            if (type == ClassicFxEffectType.DarkElfSkillModel)
            {
                life = 30f;
                scale = 0f;
                direction = new Vector3(0f,-40f,0f);
                return true;
            }
            if (type == ClassicFxEffectType.ArrowAutoLoadModel)
            {
                if (owner.WorldObject == null) return false;
                life = 40f;
                scale = 1.2f;
                light = new Vector3(1f,0.8f,0.2f);
                direction = inputLight;
                return true;
            }
            return false;
        }

        private bool MoveS6Batch26Model(ref EffectState e,float f)
        {
            if (IsS6Batch26Shield(e.Type))
            {
                if (e.Owner.WorldObject == null ||
                    !ReferenceEquals(e.Owner.WorldObject.World, World))
                    return false;
                if (e.LifeTime < 8f)
                    e.Light = e.Direction * (e.LifeTime / 8f);
                else if (e.LifeTime < 24f)
                    e.Light = e.Direction *
                        (1f - ((e.LifeTime - 24f) / 16f));
                e.Position = e.Owner.WorldObject.WorldPosition.Translation;
                return true;
            }
            if (e.Type == ClassicFxEffectType.ComboModel)
            {
                if (e.SubType == 0)
                {
                    if (e.LifeTime > 4f)
                    {
                        e.Scale += e.Gravity * f;
                        e.Gravity += 0.1f * f;
                    }
                    e.BlendMeshLight *= MathF.Pow(1f/1.4f,f);
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.FissureModel)
            {
                if (Clock.AdvancedReferenceFrame &&
                    Random.FpsCheck(2,Clock))
                {
                    float a = MathHelper.ToRadians(Random.Modulo(360));
                    float d = 300f + Random.Modulo(150);
                    Vector3 where = e.Position + new Vector3(
                        MathF.Sin(a)*d, MathF.Cos(a)*d, 0f);
                    CreateEffect(Random.Modulo(2)==0
                        ? ClassicFxEffectType.Stone1
                        : ClassicFxEffectType.Stone2,
                        where,e.Angle,e.Light,ClassicFxOwner.None);
                }
                // MuMain also moves terrain luminosity, earthquake,
                // and model-bone TRUE_FIRE/SMOKE. Those world hooks have
                // no direct matching effect-state bridge yet.
                return true;
            }
            if (e.Type == ClassicFxEffectType.FissureLight) return true;
            if (e.Type == ClassicFxEffectType.WaterWaveModel)
            {
                e.Scale += 0.02f*f;
                e.Position.Z = e.StartPosition.Z + e.Gravity;
                e.Velocity -= 10f*f;
                e.Gravity = MathF.Max(60f,e.Gravity-20f*f);
                e.Direction = new Vector3(0f,e.Velocity,0f);
                if (Clock.AdvancedReferenceFrame)
                {
                    Vector3 waterLight=new Vector3(0.1f,0.3f,0.6f);
                    for(int j=0;j<4;j++)
                        CreateParticle(ClassicTextureIds.BitmapWaterfall5,
                            e.Position,e.Angle,waterLight,1);
                }
                return true;
            }
            if (e.Type == ClassicFxEffectType.IronRiderArrowModel)
            {
                e.Scale *= MathF.Pow(1.05f,f);
                if(e.LifeTime<5f) e.Light*=MathF.Pow(0.7f,f);
                e.Position+=e.Direction*(e.Velocity*f);
                if(Clock.AdvancedReferenceFrame)
                    CreateParticle(ClassicTextureIds.BitmapSpark+1,
                        e.Position,e.Angle,e.Light,10,3f);
                return true;
            }
            if (e.Type == ClassicFxEffectType.KentaurosArrowModel)
            {
                if(e.LifeTime<=24f)
                {
                    e.Scale*=MathF.Pow(1.05f,f);
                    e.Position+=e.Direction*(e.Velocity*f);
                    if(Clock.AdvancedReferenceFrame)
                    {
                        Vector3 v=new Vector3(0.3f,0.5f,1f);
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position,e.Angle,v,26,0.2f);
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            e.Position,e.Angle,v,26,0.2f);
                    }
                }
                e.Alpha+=0.001f*f;
                return true;
            }
            if (e.Type == ClassicFxEffectType.DragonLowerDummy)
            {
                // Native BMD AnimationFrame not exposed to EffectState.
                // Keep model lifetime and alpha decay without guessing
                // when to trigger the owner's target/contact effects.
                e.Alpha*=MathF.Pow(0.96f,f);
                return true;
            }
            if (e.Type == ClassicFxEffectType.BalgasSkillModel)
            {
                e.BlendMeshLight=e.LifeTime*0.1f;
                return true;
            }
            if (e.Type == ClassicFxEffectType.DarkElfSkillModel)
            {
                if(e.Scale<0.8f) e.Scale+=0.2f*f;
                else e.Scale-=0.4f*f;
                e.BlendMeshLight=e.LifeTime*0.1f;
                if(Clock.AdvancedReferenceFrame)
                {
                    Vector3 ray = Vector3.TransformNormal(
                        new Vector3(0f,-500f,0f),
                        Matrix.CreateRotationZ(e.Angle.Z+
                            MathHelper.ToRadians(7f)));
                    Vector3 end=e.Position+ray;
                    for(int j=0;j<6;j++)
                        CreateParticle(ClassicTextureIds.BitmapSmoke,
                            end,e.Angle,e.Light,25);
                }
                return true;
            }
            if(e.Type==ClassicFxEffectType.ArrowAutoLoadModel)
            {
                var owner=e.Owner.WorldObject;
                if(owner==null || !ReferenceEquals(owner.World,World))
                    return false;
                if(e.LifeTime<20f)
                    e.Light=e.Direction*MathF.Max(0f,
                        (e.LifeTime-20f)/20f);
                if(TryGetOwnerBonePosition(e.Owner,47,out Vector3 bone))
                    e.Position=bone;
                e.Angle=owner.Angle+
                    new Vector3(MathHelper.ToRadians(85f),
                        MathHelper.ToRadians(-17f),
                        MathHelper.ToRadians(20f));
                return true;
            }
            return true;
        }
    }
}
