// ClassicFX S6 Batch 22, 16 original model types (no additional renderer).
// MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2:
// ZzzEffect.cpp CreateEffect / MoveEffects; MoveHandlers.cpp Move_MODEL_ICE_SMALL;
// MapManager.cpp / ZzzOpenData.cpp AccessModel names.
using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch22ModelType(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.TotemGolemPart1 and <=
                ClassicFxEffectType.BigStone2;

        private static bool IsS6Batch22Totem(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.TotemGolemPart1 and <=
                ClassicFxEffectType.TotemGolemPart6;

        private static bool IsS6Batch22IceGiant(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.IceGiantPart1 and <=
                ClassicFxEffectType.IceGiantPart6;

        private static bool IsS6Batch22HeavyModel(ClassicFxEffectType type) =>
            type is >= ClassicFxEffectType.Bone1 and <=
                ClassicFxEffectType.BigStone2;

        private static bool TryGetS6Batch22ModelDefinition(
            ClassicFxEffectType type, int subtype,
            out Season6ModelDefinition definition)
        {
            definition = default;
            if (!IsS6Batch22ModelType(type))
                return false;
            if (!IsS6Batch22HeavyModel(type) && subtype != 0)
                return false;
            // Supported, observed CreateEffect variations; other subtypes
            // are deliberately not guessed.
            if (IsS6Batch22HeavyModel(type) &&
                subtype is not (0 or 1 or 5 or 10 or 11 or 12))
                return false;

            string path = type switch
            {
                ClassicFxEffectType.TotemGolemPart1 => "Monster/totemhead.bmd",
                ClassicFxEffectType.TotemGolemPart2 => "Monster/totembody.bmd",
                ClassicFxEffectType.TotemGolemPart3 => "Monster/totemleft.bmd",
                ClassicFxEffectType.TotemGolemPart4 => "Monster/totemright.bmd",
                ClassicFxEffectType.TotemGolemPart5 => "Monster/totemleg.bmd",
                ClassicFxEffectType.TotemGolemPart6 => "Monster/totemleg2.bmd",
                ClassicFxEffectType.IceGiantPart1 => "Monster/icegiantpart_1.bmd",
                ClassicFxEffectType.IceGiantPart2 => "Monster/icegiantpart_2.bmd",
                ClassicFxEffectType.IceGiantPart3 => "Monster/icegiantpart_3.bmd",
                ClassicFxEffectType.IceGiantPart4 => "Monster/icegiantpart_4.bmd",
                ClassicFxEffectType.IceGiantPart5 => "Monster/icegiantpart_5.bmd",
                ClassicFxEffectType.IceGiantPart6 => "Monster/icegiantpart_6.bmd",
                ClassicFxEffectType.Bone1 => "Skill/Bone01.bmd",
                ClassicFxEffectType.Bone2 => "Skill/Bone02.bmd",
                ClassicFxEffectType.BigStone1 => "Skill/BigStone01.bmd",
                ClassicFxEffectType.BigStone2 => "Skill/BigStone02.bmd",
                _ => null
            };
            if (path == null) return false;
            definition = new Season6ModelDefinition(
                path, 60f, 1f, useCallerScale: true);
            return true;
        }

        private bool InitializeS6Batch22Model(
            ClassicFxEffectType type, ref int subtype, ClassicFxOwner owner,
            ref Vector3 position, ref Vector3 storedPosition,
            ref Vector3 angle, ref Vector3 light, ref float scale,
            ref float life, ref Vector3 direction, ref float gravity,
            ref Vector3 headAngle, ref float velocity)
        {
            if (IsS6Batch22Totem(type))
            {
                // Totem uses the same C++ ballistic creation code as Condra,
                // with one deliberate original difference: scale 0.17.
                InitializeS6Batch20Model(type, ref subtype, ref angle,
                    ref light, ref scale, ref life, ref direction,
                    ref gravity, ref headAngle, ref velocity);
                scale = 0.17f;
                return true;
            }
            if (IsS6Batch22IceGiant(type))
            {
                // The first four types use the Shadow Pawn/Knight initializer.
                // Parts 5/6 have render+move native cases but lack an explicit
                // CreateEffect initializer at the pinned SHA. Keep defaults.
                if (type <= ClassicFxEffectType.IceGiantPart4)
                    InitializeS6Batch21Model(ref subtype, ref angle,
                        ref light, ref scale, ref life, ref direction,
                        ref gravity, ref headAngle, ref velocity, type);
                return true;
            }

            float tick = Clock.FrameFactor;
            // Native BONE1 -> BONE2 fallthrough yields 150 Z for BONE1.
            if (type == ClassicFxEffectType.Bone1)
                position.Z += 150f * tick;
            else if (type == ClassicFxEffectType.Bone2)
                position.Z += 100f * tick;

            if (subtype == 5)
            {
                if (owner.WorldObject == null ||
                    !ReferenceEquals(owner.WorldObject.World, World))
                    return false;
                life = 60f;
                scale = (8f + Random.Modulo(4)) * 0.1f;
                storedPosition = position;
                position = owner.WorldObject.WorldPosition.Translation;
                return true;
            }
            if (subtype == 11)
            {
                life = 40f;
                scale = (8f + Random.Modulo(4)) * 0.8f;
                angle.Z = MathHelper.ToRadians(Random.Modulo(360));
                return true;
            }
            if (type is ClassicFxEffectType.BigStone1 or
                ClassicFxEffectType.BigStone2)
                position += new Vector3(
                    Random.Modulo(128) - 64f,
                    Random.Modulo(128) - 64f,
                    Random.Modulo(180)) * tick;

            // The native common ICE_SMALL initializer overwrites early
            // setup of 10/12; preserve the final resulting distributions.
            float speed = subtype == 1 ? 0f :
                (64f + Random.Modulo(256)) * 0.1f;
            angle.Z = MathHelper.ToRadians(Random.Modulo(360));
            direction = Vector3.TransformNormal(new Vector3(0f, speed, 0f),
                Matrix.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z));
            gravity = 8f + Random.Modulo(16);
            scale = (8f + Random.Modulo(4)) * 0.1f;
            life = 32f + Random.Modulo(16);
            return true;
        }

        private bool MoveS6Batch22Model(ref EffectState e, float f)
        {
            // Native Totem, Condra share 0.8 drag; Ice Giant, Shadow share 0.5.
            if (IsS6Batch22Totem(e.Type))
                return MoveS6Batch20Model(ref e, f);
            if (IsS6Batch22IceGiant(e.Type))
                return MoveS6Batch21Model(ref e, f);

            if (e.SubType == 5)
            {
                var owner = e.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World))
                    return false;
                // Original VectorAddScaled(Owner.Position, StartPosition,
                // Position, FPS_ANIMATION_FACTOR).
                e.Position = owner.WorldPosition.Translation +
                    e.StartPosition * f;
                return true;
            }

            // Same native Move_MODEL_ICE_SMALL dispatcher, excluding its
            // smoke emission: it only emits for IceSmall and Meteo models.
            if (e.SubType is 0 or 10 or 12)
                e.Position += e.Direction * f;
            else
                e.Position += Vector3.TransformNormal(e.Direction,
                    Matrix.CreateFromYawPitchRoll(
                        e.Angle.Y, e.Angle.X, e.Angle.Z)) * f;

            e.Direction *= MathF.Pow(0.9f, f);
            e.Position.Z += e.Gravity * f;
            if (e.SubType is 0 or 10 or 12)
            {
                e.Gravity -= 3f * f;
                float ground = RequestTerrainHeight(e.Position.X, e.Position.Y);
                if (e.Position.Z < ground)
                {
                    e.Position.Z = ground;
                    e.Gravity = -e.Gravity * 0.5f;
                    e.LifeTime -= 4f * f;
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 128f * f);
                }
                else
                    e.Angle.X -= MathHelper.ToRadians(e.Scale * 32f * f);
            }
            else if (e.SubType == 1)
                e.Gravity += 0.5f * f;
            else
            {
                e.Angle.Z += MathHelper.ToRadians(20f * f);
                e.Gravity += 0.5f * f;
            }
            return true;
        }
    }
}
