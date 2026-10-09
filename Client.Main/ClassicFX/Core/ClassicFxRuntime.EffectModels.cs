// ClassicFX Effect models, batch 1: MuMain's shared model-based CreateEffect/MoveEffects.
// Native references:
//   ZzzOpenData.cpp (AccessModel MODEL_SUMMONER_CASTING_EFFECT*, MODEL_AIR_FORCE)
//   EffectTypes.json (verified initial state / subtype support)
//   MoveHandlers.cpp (Move_MODEL_SUMMONER_CASTING_EFFECT1, Move_MODEL_AIR_FORCE)
// All model meshes are drawn through the existing ClassicFxEffectModelObject.
using System;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly struct AdditionalEffectModelDefinition
        {
            public readonly string Path;
            public readonly float LifeTime;
            public readonly float Scale;
            public readonly float BlendMeshLight;

            public AdditionalEffectModelDefinition(
                string path, float lifeTime, float scale, float blendMeshLight)
            {
                Path = path;
                LifeTime = lifeTime;
                Scale = scale;
                BlendMeshLight = blendMeshLight;
            }
        }

        private static bool IsAdditionalModelEffectType(ClassicFxEffectType type) =>
            type is ClassicFxEffectType.AirForce or
                ClassicFxEffectType.SummonerCasting1 or
                ClassicFxEffectType.SummonerCasting11 or
                ClassicFxEffectType.SummonerCasting111 or
                ClassicFxEffectType.SummonerCasting2 or
                ClassicFxEffectType.SummonerCasting22 or
                ClassicFxEffectType.SummonerCasting222 or
                ClassicFxEffectType.SummonerCasting4;

        private static bool TryGetAdditionalModelDefinition(
            ClassicFxEffectType type, int subType,
            out AdditionalEffectModelDefinition definition)
        {
            definition = default;

            // Main: MODEL_AIR_FORCE. Exactly two creation variants.
            if (type == ClassicFxEffectType.AirForce)
            {
                if (subType != 0 && subType != 1)
                    return false;
                definition = new AdditionalEffectModelDefinition(
                    "Skill/airforce.bmd",
                    subType == 0 ? 15f : 20f,
                    subType == 0 ? 0.6f : 1.2f,
                    1f);
                return true;
            }

            // Main: these seven BMDs use the same MoveEffects dispatcher,
            // but individual rotation directions and lifetime are preserved.
            // Only subtype 0 is defined by native creation metadata.
            if (subType != 0)
                return false;

            string path = type switch
            {
                ClassicFxEffectType.SummonerCasting1 => "Effect/Suhwanzin1.bmd",
                ClassicFxEffectType.SummonerCasting11 => "Effect/Suhwanzin11.bmd",
                ClassicFxEffectType.SummonerCasting111 => "Effect/Suhwanzin111.bmd",
                ClassicFxEffectType.SummonerCasting2 => "Effect/Suhwanzin2.bmd",
                ClassicFxEffectType.SummonerCasting22 => "Effect/Suhwanzin22.bmd",
                ClassicFxEffectType.SummonerCasting222 => "Effect/Suhwanzin222.bmd",
                ClassicFxEffectType.SummonerCasting4 => "Effect/Suhwanzin4.bmd",
                _ => null
            };
            if (path == null)
                return false;
            definition = new AdditionalEffectModelDefinition(
                path, type == ClassicFxEffectType.SummonerCasting4 ? 25f : 40f,
                1f, 0f);
            return true;
        }

        private bool MoveAdditionalModelEffect(ref EffectState e, float frameFactor)
        {
            if (e.Type == ClassicFxEffectType.AirForce)
            {
                // MuMain Move_MODEL_AIR_FORCE(): owner is mandatory.
                WorldObject owner = e.Owner.WorldObject;
                if (owner == null || !ReferenceEquals(owner.World, World) ||
                    owner.Status == GameControlStatus.Disposed ||
                    owner.Status == GameControlStatus.Error)
                    return false;

                if (e.SubType == 0)
                {
                    e.BlendMeshLight *= MathF.Pow(1f / 1.5f, frameFactor);
                    e.Scale += 0.2f * frameFactor;
                }
                else
                {
                    e.BlendMeshLight = e.LifeTime / 10f;
                    // Native rotates (0,-70,0) by Owner->Angle and adds 100 Z.
                    // The MonoGame model angle is in radians; the original
                    // ZzzEffect angle is in degrees.
                    e.Position = owner.WorldPosition.Translation;
                    if (owner is Client.Main.Objects.Player.PlayerObject player)
                        e.Angle = player.TotalAngle;
                    Vector3 offset = Vector3.TransformNormal(
                        new Vector3(0f, -70f, 0f),
                        Matrix.CreateRotationZ(e.Angle.Z));
                    offset.Z += 100f;
                    e.Position += offset * frameFactor;
                }
                return true;
            }

            // MuMain Move_MODEL_SUMMONER_CASTING_EFFECT1(), shared by seven
            // original models. Preserve the 25-FPS fade / angular deltas.
            if (!IsAdditionalModelEffectType(e.Type))
                return false;

            if (e.LifeTime < 20f)
                e.BlendMeshLight -= 0.03f * frameFactor;
            else if (e.BlendMeshLight < 0.5f)
                e.BlendMeshLight += 0.05f * frameFactor;

            const float rotationPerNativeFrame = 3f;
            float rotation = MathHelper.ToRadians(rotationPerNativeFrame) * frameFactor;
            switch (e.Type)
            {
                case ClassicFxEffectType.SummonerCasting1:
                case ClassicFxEffectType.SummonerCasting111:
                case ClassicFxEffectType.SummonerCasting22:
                    e.Angle.Z -= rotation;
                    break;
                case ClassicFxEffectType.SummonerCasting11:
                case ClassicFxEffectType.SummonerCasting2:
                case ClassicFxEffectType.SummonerCasting222:
                    e.Angle.Z += rotation;
                    break;
                case ClassicFxEffectType.SummonerCasting4:
                    e.Scale += 0.6f * frameFactor;
                    break;
            }
            return true;
        }
    }
}
