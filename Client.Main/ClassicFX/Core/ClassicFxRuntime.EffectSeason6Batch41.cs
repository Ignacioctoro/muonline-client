// BroyalMU ClassicFX S6 Batch 41.
// Pinned MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// ZzzEffect.cpp + Behaviors/MoveHandlers.cpp + EffectTypes.json.
// Shared Effect/ModelObject/Particle/Sprite/Joint pools; no parallel renderer.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // Shared arrays avoid per-frame allocations on Android.
        private static readonly int[] S6Batch41UmbrellaTicks = { 28, 18, 8 };
        private static readonly int[] S6Batch41SakuraTicks = { 30, 15, 4 };

        private static bool IsS6Batch41ModelType(ClassicFxEffectType t) =>
            t == ClassicFxEffectType.SakuraItemEffectModel;

        private static bool TryGetS6Batch41ModelDefinition(
            ClassicFxEffectType type, int sub,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch41ModelType(type) || sub is not (0 or 1))
                return false;
            // Exact original AccessModel(MODEL_EFFECT_SKURA_ITEM)
            // and real Data_Broyal Effect/cherryblossom asset.
            definition = new Season6ModelDefinition(
                "Effect/cherryblossom/Skura_iteam_event.bmd",
                52f, 1f, needsOwner: true, useCallerScale: true);
            return true;
        }

        private static bool IsS6Batch41LogicalType(ClassicFxEffectType t, int sub) =>
            t switch
            {
                ClassicFxEffectType.TraceEnergyJointCarrier => sub == 0,
                ClassicFxEffectType.UmbrellaDeathRingCarrier => sub == 0,
                ClassicFxEffectType.GuardianDefenderAttackCarrier => sub == 0,
                ClassicFxEffectType.StreamBreathFireCarrier => sub == 0,
                ClassicFxEffectType.ThunderNapinCore => sub == 0,
                ClassicFxEffectType.ThunderNapinScatter => sub == 0,
                ClassicFxEffectType.SkillFissureCarrier => sub == 0,
                ClassicFxEffectType.FenrirDamageRed => sub == 0,
                ClassicFxEffectType.FenrirDamageBlue => sub == 0,
                ClassicFxEffectType.FenrirDamageGreen => sub == 0,
                _ => false
            };

        private static bool S6Batch41NeedsOwner(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.UmbrellaDeathRingCarrier or
                ClassicFxEffectType.SakuraItemEffectModel or
                ClassicFxEffectType.FenrirDamageRed or
                ClassicFxEffectType.FenrirDamageBlue or
                ClassicFxEffectType.FenrirDamageGreen;

        private static bool S6Batch41NeedsModelOwner(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.SakuraItemEffectModel or
                ClassicFxEffectType.FenrirDamageRed or
                ClassicFxEffectType.FenrirDamageBlue or
                ClassicFxEffectType.FenrirDamageGreen;

        private static bool IsS6Batch41Fenrir(ClassicFxEffectType t) =>
            t is ClassicFxEffectType.FenrirDamageRed or
                ClassicFxEffectType.FenrirDamageBlue or
                ClassicFxEffectType.FenrirDamageGreen;

        private static void InitializeS6Batch41Logical(
            ClassicFxEffectType type, ref float scale, out float life)
        {
            life = type switch
            {
                ClassicFxEffectType.TraceEnergyJointCarrier => 50f,
                ClassicFxEffectType.UmbrellaDeathRingCarrier => 30f,
                ClassicFxEffectType.GuardianDefenderAttackCarrier => 20f,
                ClassicFxEffectType.StreamBreathFireCarrier => 30f,
                ClassicFxEffectType.ThunderNapinCore or
                    ClassicFxEffectType.ThunderNapinScatter => 30f,
                ClassicFxEffectType.SkillFissureCarrier => 20f,
                _ => 1f // native Fenrir damage emits 10 particles on Create
            };
            if (type == ClassicFxEffectType.GuardianDefenderAttackCarrier)
                scale = 0.9f;
        }

        private void EmitS6Batch41OnCreate(
            ClassicFxEffectType type, ClassicFxHandle self,
            Vector3 position, Vector3 angle, Vector3 light,
            ClassicFxOwner owner, float scale)
        {
            if (type == ClassicFxEffectType.TraceEnergyJointCarrier)
            {
                // MODEL_EFFECT_TRACE native subtype 0: joint 17 follows Effect.
                CreateJoint(ClassicTextureIds.BitmapJointEnergy,
                    position, position, angle, 17,
                    ClassicFxOwner.FromClassicFx(self), scale,
                    priorColor: light);
            }
            else if (IsS6Batch41Fenrir(type))
            {
                // MODEL_FENRIR_SKILL_DAMAGE native subtype 1/2/3.
                if (owner.WorldObject is not ModelObject model) return;
                Matrix[] bones = model.GetBoneTransforms();
                if (bones == null || bones.Length == 0) return;
                Vector3 c = type switch
                {
                    ClassicFxEffectType.FenrirDamageRed =>
                        new Vector3(0.6f, 0.2f, 0.2f),
                    ClassicFxEffectType.FenrirDamageBlue =>
                        new Vector3(0.2f, 0.2f, 0.4f),
                    _ => new Vector3(0.6f, 0.8f, 0.6f)
                };
                for (int i = 0; i < 10; i++)
                {
                    int bone = Random.Modulo(bones.Length);
                    if (TryGetOwnerBonePosition(owner, bone, out Vector3 p))
                        CreateParticle(ClassicTextureIds.BitmapEnergy,
                            p, angle, c, 2);
                }
            }
        }

        private bool MoveS6Batch41Logical(
            ref EffectState e, float f, ClassicFxHandle self)
        {
            switch (e.Type)
            {
                case ClassicFxEffectType.TraceEnergyJointCarrier:
                    return true; // joint owns its trail; no native Move branch

                case ClassicFxEffectType.UmbrellaDeathRingCarrier:
                    if (!S6Batch41OwnerReady(e.Owner)) return false;
                    if (!Clock.AdvancedReferenceFrame) return true;
                    // Original exactly at remaining ticks 28,18,8.
                    // Mask prevents multiple emissions at higher refresh.
                    for (int i = 0; i < S6Batch41UmbrellaTicks.Length; i++)
                    {
                        byte mask = (byte)(1 << i);
                        if (e.LifeTime > S6Batch41UmbrellaTicks[i] ||
                            (e.TriggerMask & mask) != 0) continue;
                        e.TriggerMask |= mask;
                        CreateEffect(ClassicFxEffectType.RingOfGradationEffect,
                            e.Owner.WorldObject.WorldPosition.Translation,
                            e.Angle, new Vector3(1f, 0.2f, 0.5f),
                            e.Owner, subType: 0, scale: 1.2f);
                    }
                    return true;

                case ClassicFxEffectType.GuardianDefenderAttackCarrier:
                    if (e.LifeTime <= 18f)
                    {
                        e.Light *= MathF.Pow(1f / 1.4f, f);
                        e.Scale *= MathF.Pow(1.1f, f);
                    }
                    if (Clock.AdvancedReferenceFrame)
                    {
                        CreateSprite(ClassicTextureIds.BitmapShockWave,
                            e.Position, e.Scale, e.Light, e.Owner);
                        CreateParticle(ClassicTextureIds.BitmapFire + 2,
                            e.Position, e.Angle, e.Light, 16, 2.5f);
                    }
                    return true;

                case ClassicFxEffectType.StreamBreathFireCarrier:
                    if (!Clock.AdvancedReferenceFrame) return true;
                    CreateParticle(ClassicTextureIds.BitmapWaterfall3,
                        e.Position, e.Angle, e.Light, 11, 0.6f);
                    CreateParticle(ClassicTextureIds.BitmapSmoke,
                        e.Position, e.Angle, e.Light, 52, 0.6f);
                    if (e.LifeTime <= 15f && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        Vector3 heading = ClassicMath.VectorRotate(
                            new Vector3(0f, -20f, 0f),
                            ClassicMath.AngleMatrix(e.Angle));
                        CreateEffect(ClassicFxEffectType.MoonHarvestMoon,
                            e.Position, heading, e.Light, ClassicFxOwner.None,
                            subType: 1, scale: 0.3f);
                    }
                    return true;

                case ClassicFxEffectType.ThunderNapinCore:
                case ClassicFxEffectType.ThunderNapinScatter:
                    if (Clock.AdvancedReferenceFrame)
                        EmitS6Batch41Thunder(ref e);
                    return true;

                case ClassicFxEffectType.SkillFissureCarrier:
                    if (!Clock.AdvancedReferenceFrame) return true;
                    if (e.LifeTime <= 8f && (e.TriggerMask & 1) == 0)
                    {
                        e.TriggerMask |= 1;
                        for (int i = 0; i < 16; i++)
                        {
                            Vector3 a = new Vector3(-10f, 0f, i * 10f);
                            Vector3 p = e.Position + new Vector3(
                                Random.Modulo(600) - 200,
                                Random.Modulo(600) - 400, 100f);
                            CreateJoint(ClassicTextureIds.BitmapFlare,
                                p, p, a, 24, ClassicFxOwner.None, 90f);
                        }
                    }
                    if (e.LifeTime <= 2f && (e.TriggerMask & 2) == 0)
                    {
                        e.TriggerMask |= 2;
                        Vector3 a = new Vector3(0f, 0f, Random.Modulo(360));
                        // The logical carrier is released this same tick.
                        // Keep its child models attached to the stable world owner.
                        ClassicFxOwner source = e.Owner;
                        CreateEffect(ClassicFxEffectType.FissureModel,
                            e.Position, a, e.Light, source);
                        CreateEffect(ClassicFxEffectType.FissureLight,
                            e.Position, a, e.Light, source);
                        return false;
                    }
                    return true;

                case ClassicFxEffectType.FenrirDamageRed:
                case ClassicFxEffectType.FenrirDamageBlue:
                case ClassicFxEffectType.FenrirDamageGreen:
                    return false; // original ten particles spawned at creation
            }
            return false;
        }

        private bool S6Batch41OwnerReady(ClassicFxOwner owner) =>
            owner.WorldObject != null &&
            ReferenceEquals(owner.WorldObject.World, World) &&
            owner.WorldObject.Status == GameControlStatus.Ready;

        private void EmitS6Batch41Thunder(ref EffectState e)
        {
            bool scatter = e.Type == ClassicFxEffectType.ThunderNapinScatter;
            Vector3 pos = e.Position;
            if (scatter)
                pos += new Vector3(Random.Modulo(200) - 100,
                    Random.Modulo(200) - 100, Random.Modulo(100) - 50);
            if (e.LifeTime >= 4f)
            {
                e.Scale = 0.8f + Random.Modulo(10) * 0.1f;
                Vector3 particlePos = pos + new Vector3(
                    3f * (Random.Modulo(40) - 20),
                    3f * (Random.Modulo(40) - 20),
                    3f * (Random.Modulo(40) - 20));
                CreateParticle(ClassicTextureIds.BitmapLightningMega1 +
                        Random.Modulo(3), particlePos, e.Angle,
                    new Vector3(0.4f, 0.7f, 1f), 0, e.Scale);
            }
            if (scatter)
                pos = e.Position + new Vector3(
                    Random.Modulo(200) - 100, Random.Modulo(200) - 100,
                    Random.Modulo(100) - 50);
            Vector3 c = new Vector3(0.1f, 0.2f, 0.8f);
            CreateSprite(ClassicTextureIds.BitmapLight,
                pos, 8f, c, e.Owner);
            CreateSprite(ClassicTextureIds.BitmapLight,
                pos, 8f, c, e.Owner);
        }

        private bool MoveS6Batch41SakuraModel(ref EffectState e, float f)
        {
            if (!S6Batch41OwnerReady(e.Owner) ||
                e.Owner.WorldObject is not ModelObject)
                return false;

            e.Position = e.Owner.WorldObject.WorldPosition.Translation;
            e.Light = new Vector3(1f, 0.6f, 0.8f);
            if (!Clock.AdvancedReferenceFrame) return true;

            // MODEL_EFFECT_SKURA_ITEM is the *actual* animated BMD. Its
            // 1/2 bones provide the two fountain origins (no fake particles).
            ClassicFxOwner viewOwner = e.ModelView == null
                ? ClassicFxOwner.None
                : ClassicFxOwner.FromWorldObject(e.ModelView);
            float spin = (float)Clock.WorldTimeMilliseconds * 0.08f;
            for (int bone = 1; bone <= 2; bone++)
            {
                if (!TryGetOwnerBonePosition(viewOwner, bone,
                        out Vector3 p)) continue;
                CreateSprite(ClassicTextureIds.BitmapShiny + 1,
                    p, 1.8f, e.Light, e.Owner,
                    bone == 1 ? spin : -spin);
                for (int i = 0; i < 7; i++)
                {
                    CreateParticle(ClassicTextureIds.BitmapShiny + 1,
                        p, e.Angle, e.Light, 5, 0.8f);
                    CreateParticle(ClassicTextureIds.BitmapCherryBlossomEventPetal,
                        p, e.Angle,
                        Random.Modulo(7) == 3 ? new Vector3(1f, 0.6f, 0.8f)
                            : new Vector3(0.3f), 0, 0.5f);
                }
            }
            for (int j = 0; j < S6Batch41SakuraTicks.Length; j++)
            {
                byte mask = (byte)(1 << j);
                if (e.LifeTime > S6Batch41SakuraTicks[j] ||
                    (e.TriggerMask & mask) != 0)
                    continue;
                e.TriggerMask |= mask;
                if (!TryGetOwnerBonePosition(e.Owner, 20,
                        out Vector3 burst)) continue;
                burst.X += Random.Modulo(2) == 0
                    ? Random.Modulo(40) + 10
                    : -(Random.Modulo(30) + 10);
                burst.Z += Random.Modulo(110) + 50;
                Vector3 a = new Vector3(0f, 1f, 0f);
                CreateParticle(ClassicTextureIds.BitmapShockWave,
                    burst, a, new Vector3(1f, 0.6f, 0.8f), 4, 0.005f);
                for (int i = 0; i < 70; i++)
                {
                    if (Random.Modulo(3) == 0)
                        CreateParticle(ClassicTextureIds.BitmapCherryBlossomEventPetal,
                            burst, e.Angle,
                            new Vector3(0.8f, 0.85f, 1f), 0, 0.4f);
                    CreateParticle(ClassicTextureIds.BitmapCherryBlossomEventFlower,
                        burst, e.Angle,
                        new Vector3(0.7f, 0.71f, 1f), 0, 0.4f);
                }
            }
            return true;
        }
    }
}
