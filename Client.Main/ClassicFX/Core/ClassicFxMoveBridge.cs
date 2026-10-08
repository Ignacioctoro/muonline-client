using System;
using Client.Main.Controls;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Dependencias globales que MoveParticles() del Main usa fuera del
    /// propio array PARTICLE.
    ///
    /// Esta clase NO contiene comportamiento de una skill concreta.
    /// Es infraestructura compartida para Particle/Joint/Effect.
    /// </summary>
    public sealed class ClassicFxMoveBridge : IDisposable
    {
        // El Main escribe directamente sobre el lightmap temporal.
        // En MonoGame reutilizamos luces dinámicas preasignadas.
        //
        // 64 slots evita allocations durante gameplay y pone un límite
        // explícito para Android.
        private const int MaxTransientTerrainLights = 64;

        private readonly ClassicFxRuntime _runtime;

        private readonly DynamicLight[] _terrainLights =
            new DynamicLight[MaxTransientTerrainLights];

        private TerrainControl _registeredTerrain;
        private int _terrainLightCount;
        private bool _disposed;

        /// <summary>
        /// Hook hacia el futuro sistema Effect.
        ///
        /// MoveParticles() llama CreateBomb() una vez en el Main.
        /// No se emula con una explosión inventada: cuando Effect exista,
        /// se conectará aquí la implementación real.
        /// </summary>
        public event Action<Vector3, bool, int> BombRequested;

        /// <summary>
        /// El estado especial de Crywolf no existe todavía como subsistema
        /// equivalente en ClassicFX. El mundo podrá conectarlo aquí.
        /// </summary>
        public Func<bool> CryWolfFirstStageResolver
        {
            get;
            set;
        }

        public ClassicFxMoveBridge(
            ClassicFxRuntime runtime)
        {
            _runtime =
                runtime ??
                throw new ArgumentNullException(
                    nameof(runtime));

            for (int i = 0;
                 i < _terrainLights.Length;
                 i++)
            {
                _terrainLights[i] =
                    new DynamicLight
                    {
                        Position = Vector3.Zero,
                        Color = Vector3.Zero,
                        Radius = 100f,
                        Intensity = 0f
                    };
            }
        }

        /// <summary>
        /// Llamar una vez antes de MoveParticles().
        /// Sólo desactiva slots usados el frame anterior.
        /// </summary>
        public void BeginFrame()
        {
            if (_disposed)
            {
                return;
            }

            for (int i = 0;
                 i < _terrainLightCount;
                 i++)
            {
                _terrainLights[i].Intensity =
                    0f;
            }

            _terrainLightCount =
                0;
        }

        public void Reset()
        {
            BeginFrame();
        }

        /// <summary>
        /// Adaptación de AddTerrainLight(x,y,Light,Range,...).
        ///
        /// El parámetro range del Main está expresado en tiles de terreno.
        /// </summary>
        public bool AddTerrainLight(
            float x,
            float y,
            in Vector3 color,
            float rangeInTiles)
        {
            if (_disposed ||
                _terrainLightCount >=
                    MaxTransientTerrainLights)
            {
                return false;
            }

            TerrainControl terrain =
                _runtime
                    .World?
                    .Terrain;

            if (terrain == null)
            {
                return false;
            }

            EnsureRegistered(
                terrain);

            DynamicLight light =
                _terrainLights[
                    _terrainLightCount++
                ];

            light.Position =
                new Vector3(
                    x,
                    y,
                    terrain.RequestTerrainHeight(
                        x,
                        y) +
                    5f);

            light.Color =
                color;

            light.Radius =
                MathF.Max(
                    1f,
                    rangeInTiles *
                    Constants.TERRAIN_SCALE);

            light.Intensity =
                1f;

            return true;
        }

        public bool TryGetOwnerCurrentAction(
            in ClassicFxOwner owner,
            out int currentAction)
        {
            if (_runtime
                    .TryGetOwnerWorldObject(
                        owner,
                        out WorldObject worldObject) &&
                worldObject is ModelObject modelObject)
            {
                currentAction =
                    modelObject.CurrentAction;

                return true;
            }

            currentAction =
                0;

            return false;
        }

        public bool IsOwnerSanta2Action(
            in ClassicFxOwner owner)
        {
            return
                TryGetOwnerCurrentAction(
                    owner,
                    out int currentAction) &&
                currentAction ==
                    (int)PlayerAction.PlayerSanta2;
        }

        /// <summary>
        /// Equivalente estructural a g_isCharacterBuff(owner, buff).
        /// </summary>
        public bool IsOwnerBuffActive(
            in ClassicFxOwner owner,
            byte effectId)
        {
            if (!_runtime
                    .TryGetOwnerWorldObject(
                        owner,
                        out WorldObject worldObject))
            {
                return false;
            }

            var characterState =
                MuGame
                    .Network?
                    .GetCharacterState();

            if (characterState == null)
            {
                return false;
            }

            ushort id =
                worldObject.NetworkId;

            // Igual que PlayerObject.HasActiveBuff():
            // el player local puede no tener todavía NetworkId copiado.
            if (id == 0)
            {
                id =
                    characterState.Id;
            }

            return characterState
                .HasActiveBuff(
                    effectId,
                    id);
        }

        public bool TryGetOwnerBoneCount(
            in ClassicFxOwner owner,
            out int boneCount)
        {
            if (_runtime
                    .TryGetOwnerWorldObject(
                        owner,
                        out WorldObject worldObject) &&
                worldObject is ModelObject modelObject)
            {
                Matrix[] bones =
                    modelObject
                        .GetBoneTransforms();

                if (bones != null)
                {
                    boneCount =
                        bones.Length;

                    return boneCount >
                        0;
                }
            }

            boneCount =
                0;

            return false;
        }

        /// <summary>
        /// Adaptación de BoneManager::GetBonePosition por nombre.
        /// No crea diccionarios ni strings durante el frame.
        /// </summary>
        public bool TryGetOwnerNamedBonePosition(
            in ClassicFxOwner owner,
            string boneName,
            in Vector3 localOffset,
            out Vector3 worldPosition)
        {
            worldPosition =
                Vector3.Zero;

            if (string.IsNullOrEmpty(
                    boneName) ||
                !_runtime
                    .TryGetOwnerWorldObject(
                        owner,
                        out WorldObject worldObject) ||
                worldObject is not ModelObject modelObject)
            {
                return false;
            }

            var modelBones =
                modelObject
                    .Model?
                    .Bones;

            Matrix[] transforms =
                modelObject
                    .GetBoneTransforms();

            if (modelBones == null ||
                transforms == null)
            {
                return false;
            }

            int count =
                Math.Min(
                    modelBones.Length,
                    transforms.Length);

            for (int i = 0;
                 i < count;
                 i++)
            {
                if (!string.Equals(
                        modelBones[i].Name,
                        boneName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Matrix boneWorld =
                    transforms[i] *
                    modelObject.WorldPosition;

                worldPosition =
                    Vector3.Transform(
                        localOffset,
                        boneWorld);

                return true;
            }

            return false;
        }

        public bool RequestBomb(
            in Vector3 position,
            bool explode,
            int subType = 0)
        {
            Action<Vector3, bool, int>
                handler =
                    BombRequested;

            if (handler == null)
            {
                // Dependencia intencionalmente pendiente hasta portar Effect.
                return false;
            }

            handler(
                position,
                explode,
                subType);

            return true;
        }

        public bool IsCryWolfFirstStage()
        {
            Func<bool> resolver =
                CryWolfFirstStageResolver;

            return
                resolver != null &&
                resolver();
        }

        private void EnsureRegistered(
            TerrainControl terrain)
        {
            if (ReferenceEquals(
                    _registeredTerrain,
                    terrain))
            {
                return;
            }

            DetachTerrainLights();

            _registeredTerrain =
                terrain;

            for (int i = 0;
                 i < _terrainLights.Length;
                 i++)
            {
                terrain.AddDynamicLight(
                    _terrainLights[i]);
            }
        }

        private void DetachTerrainLights()
        {
            if (_registeredTerrain ==
                null)
            {
                return;
            }

            for (int i = 0;
                 i < _terrainLights.Length;
                 i++)
            {
                _terrainLights[i]
                    .Intensity =
                        0f;

                _registeredTerrain
                    .RemoveDynamicLight(
                        _terrainLights[i]);
            }

            _registeredTerrain =
                null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DetachTerrainLights();

            BombRequested =
                null;

            CryWolfFirstStageResolver =
                null;

            _disposed =
                true;
        }
    }
}
