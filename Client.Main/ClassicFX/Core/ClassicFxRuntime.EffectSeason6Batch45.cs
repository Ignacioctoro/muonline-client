// BroyalMU ClassicFX Season 6 — Batch 45.
// Native: MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2
// ZzzEffect.cpp: MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF,
// BITMAP_SHINY+6, MODEL_ARROWSRE06.
// Behaviors/MoveHandlers.cpp: BITMAP_FIRE_CURSEDLICH.
// No new shaders, standalone BMDs or effect rendering systems.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch45LogicalType(ClassicFxEffectType t, int sub) =>
            t == ClassicFxEffectType.SwellMagicBuffCarrier && sub == 0;

        private bool IsS6Batch45BuffOwnerValid(ClassicFxOwner owner) =>
            owner.WorldObject is PlayerObject player &&
            ReferenceEquals(player.World, World) &&
            player.Status == GameControlStatus.Ready &&
            !player.IsDead &&
            HasS6Batch45PlayerHands(player);

        private static bool HasS6Batch45PlayerHands(PlayerObject player)
        {
            Matrix[] bones = player.GetBoneTransforms();
            return bones != null && bones.Length > 37;
        }

        private bool ValidateS6Batch45Variants(
            ClassicFxEffectType type, int sub, ClassicFxOwner owner, int skillIndex)
        {
            bool modelRequired =
                (type == ClassicFxEffectType.CursedLichFireEmitter &&
                    sub is 0 or 2) ||
                (type == ClassicFxEffectType.ShinyScatterEmitter &&
                    sub is 1 or 2 or 3);
            if (!modelRequired) return true;

            if (owner.WorldObject is not ModelObject model ||
                !ReferenceEquals(model.World, World) ||
                model.Status != GameControlStatus.Ready)
                return false;
            Matrix[] bones = model.GetBoneTransforms();
            if (bones == null || bones.Length == 0) return false;
            return type != ClassicFxEffectType.CursedLichFireEmitter ||
                sub != 0 || (uint)skillIndex < (uint)bones.Length;
        }

        private bool MoveS6Batch45Buff(ref EffectState e)
        {
            if (!IsS6Batch45BuffOwnerValid(e.Owner))
                return false;
            PlayerObject player = (PlayerObject)e.Owner.WorldObject;
            e.Position = player.WorldPosition.Translation;
            // Native MoveEffects resets the buff emitter's 999-tick lifespan.
            // It is removed by ReleaseEffect when the corresponding buff ends.
            e.LifeTime = 999f;

            // WorldTime - Timer >= 6000: two real hand-ring BMDs once
            // per six seconds; counter in milliseconds avoids FPS dependence.
            e.Phase += (float)Clock.DeltaMilliseconds;
            if (e.Phase >= 6000f)
            {
                e.Phase %= 6000f;
                Vector3 violet = new Vector3(0.2f, 0.2f, 0.9f);
                if (TryGetOwnerBonePosition(e.Owner, 28, out Vector3 right))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        right, e.Angle, violet, e.Owner, subType: 1, boneIndex: 28);
                if (TryGetOwnerBonePosition(e.Owner, 37, out Vector3 left))
                    CreateEffect(ClassicFxEffectType.ArrowsRe06,
                        left, e.Angle, violet, e.Owner, subType: 1, boneIndex: 37);
            }

            if (!Clock.AdvancedReferenceFrame) return true;

            // Native RenderEffects: one violet BITMAP_LIGHT per player bone.
            // At 60/120fps emit at the 25Hz classic clock to avoid duplicate
            // transient sprites and preserve Android billboard budgets.
            float lum = (MathF.Abs(MathF.Sin(
                (float)(Clock.WorldTimeMilliseconds * 0.001))) + 0.2f) * 0.5f;
            Vector3 color = new Vector3(lum * 0.7f, lum * 0.3f, lum * 0.9f);
            Matrix[] bones = player.GetBoneTransforms();
            Matrix world = player.WorldPosition;
            for (int i = 0; i < bones.Length; i++)
            {
                Vector3 p = (bones[i] * world).Translation;
                CreateSprite(ClassicTextureIds.BitmapLight,
                    p, 1.8f, color, e.Owner);
            }
            return true;
        }

        private bool MoveS6Batch45CursedLich(ref EffectState e)
        {
            if (e.SubType is 0 or 2 &&
                !ValidateS6Batch45Variants(e.Type, e.SubType,
                    e.Owner, e.NativeSkillIndex))
                return false;
            if (!Clock.AdvancedReferenceFrame) return true;

            switch (e.SubType)
            {
                case 0:
                    // Native Skill holds the bone index. Source light is
                    // derived from the effect alpha, not caller RGB.
                    if (!TryGetOwnerBonePosition(e.Owner,
                        e.NativeSkillIndex, out Vector3 bone))
                        return false;
                    e.Position = bone;
                    Vector3 grey = new Vector3(e.Alpha * 0.3f);
                    CreateParticle(ClassicTextureIds.BitmapFireCursedLich,
                        bone, e.Angle, grey, 3, 1f, e.Owner);
                    return true;

                case 2:
                    // Eight separate random real bone emitters per native tick.
                    ModelObject model = (ModelObject)e.Owner.WorldObject;
                    Matrix[] bones = model.GetBoneTransforms();
                    for (int i = 0; i < 8; i++)
                    {
                        int index = Random.Modulo(bones.Length);
                        if (!TryGetOwnerBonePosition(e.Owner, index,
                            out Vector3 pos))
                            continue;
                        CreateSprite(ClassicTextureIds.BitmapLight,
                            pos, 4f, new Vector3(1f, 0.2f, 0f), e.Owner);
                        EmitS6Batch45CursedFire(pos, e.Angle, Vector3.One,
                            scale: (13f + Random.Modulo(5)) * 0.1f,
                            sub: 0);
                    }
                    return true;

                case 3:
                    // Two small sparks; subtype 4 belongs to the separate
                    // mono/green family and is not claimed in Batch 45.
                    for (int i = 0; i < 2; i++)
                        EmitS6Batch45CursedFire(e.Position, e.Angle, e.Light,
                            scale: (18f + Random.Modulo(5)) * 0.03f,
                            sub: 0);
                    return true;

                case 1:
                case 12:
                    Vector3 p = e.Position + new Vector3(
                        Random.Modulo(e.SubType == 12 ? 50 : 100) -
                            (e.SubType == 12 ? 25 : 50),
                        Random.Modulo(e.SubType == 12 ? 50 : 100) -
                            (e.SubType == 12 ? 25 : 50),
                        5 + Random.Modulo(10));
                    float size = (13f + Random.Modulo(5)) *
                        (e.SubType == 12 ? 0.05f * e.Scale : 0.1f);
                    EmitS6Batch45CursedFire(p, e.Angle, e.Light, size,
                        sub: 1);
                    return true;
            }
            return false;
        }

        private void EmitS6Batch45CursedFire(
            Vector3 position, Vector3 angle, Vector3 light,
            float scale, int sub)
        {
            int kind = Random.Modulo(3);
            int texture = kind switch
            {
                0 => ClassicTextureIds.BitmapFireHik1,
                1 => ClassicTextureIds.BitmapFireCursedLich,
                _ => ClassicTextureIds.BitmapFireHik3
            };
            CreateParticle(texture, position, angle, light,
                texture == ClassicTextureIds.BitmapFireCursedLich
                    ? (sub == 0 ? 4 : 5) : sub,
                scale);
        }

        private bool MoveS6Batch45Shiny(ref EffectState e)
        {
            if (e.SubType != 0 &&
                !ValidateS6Batch45Variants(e.Type, e.SubType,
                    e.Owner, 0))
                return false;

            if (e.SubType is 1 or 2)
                e.LifeTime = 100f; // live owner-bound emitter in native Main.
            if (!Clock.AdvancedReferenceFrame) return true;

            if (e.SubType == 0)
            {
                Vector3 p = e.Position + new Vector3(
                    Random.Modulo(500) - 250,
                    Random.Modulo(500) - 250,
                    150f - Random.Modulo(100));
                CreateParticle(ClassicTextureIds.BitmapShiny + 6,
                    p, e.Angle, e.Light, 0, e.Scale);
                return true;
            }

            ModelObject model = (ModelObject)e.Owner.WorldObject;
            Matrix[] bones = model.GetBoneTransforms();
            int bone = Random.Modulo(bones.Length);
            if (e.SubType is 1 or 2)
            {
                // Native offset (0,0,100) in selected animated bone space.
                Matrix boneWorld = bones[bone] * model.WorldPosition;
                Vector3 p = Vector3.Transform(
                    new Vector3(0f, 0f, 100f), boneWorld);
                if (Random.Modulo(2) == 0)
                {
                    p.Z -= 20f;
                    CreateParticle(ClassicTextureIds.BitmapShiny + 6,
                        p, e.Angle, e.Light, 0, e.Scale);
                }
                return true;
            }

            // Subtype 3 emits a sprite from a random animated bone, with
            // original local (0,-20,0) displacement rotated by owner angle.
            if (!TryGetOwnerBonePosition(e.Owner, bone, out Vector3 anchor))
                return false;
            Vector3 rotated = ClassicMath.VectorRotate(
                new Vector3(0f, -20f, 0f),
                ClassicMath.AngleMatrix(new Vector3(
                    MathHelper.ToDegrees(model.Angle.X),
                    MathHelper.ToDegrees(model.Angle.Y),
                    MathHelper.ToDegrees(model.Angle.Z))));
            CreateSprite(ClassicTextureIds.BitmapShiny + 6,
                anchor + rotated, 2f, e.Light, e.Owner);
            return true;
        }
    }
}
