using System;
using System.Threading.Tasks;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Client.Main.Controls;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Runtime central del sistema clásico de efectos.
    ///
    /// Este objeto pertenece a un WorldControl.
    ///
    /// En esta primera fase todavía no dibuja ni crea efectos.
    /// Su responsabilidad inicial es establecer:
    ///
    /// - Clock
    /// - Random
    /// - Pools
    /// - ciclo Update
    ///
    /// Sobre esta base se agregarán las primitivas reales.
    /// </summary>
    public sealed partial class ClassicFxRuntime : IDisposable
    {
        private bool
            _disposed;

        private ClassicSpriteRenderer
            _spriteRenderer;

        public WorldControl World
        {
            get;
        }

        public ClassicFxClock Clock
        {
            get;
        }

        public ClassicRandom Random
        {
            get;
        }

        public ClassicFxPools Pools
        {
            get;
        }
        public ClassicTextureRepository Textures
        {
            get;
        }

        /// <summary>
        /// Interruptor global del runtime.
        ///
        /// No se utilizará para alterar la fidelidad.
        /// Es principalmente útil para debugging.
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        } = true;

        public bool IsDisposed =>
            _disposed;

        public ClassicFxRuntime(
            WorldControl world)
        {
            World =
                world ??
                throw new ArgumentNullException(
                    nameof(world));

            Clock =
                new ClassicFxClock();

            Random =
                new ClassicRandom();

            Pools =
                new ClassicFxPools();

            Textures =
                new ClassicTextureRepository();
        }
        public async Task LoadContentAsync()
        {
            if (_disposed)
            {
                return;
            }

            await Textures
                .LoadCoreAsync();

            _spriteRenderer ??=
                new ClassicSpriteRenderer(
                    GraphicsManager
                        .Instance
                        .GraphicsDevice,

                    Textures);
        }

        /// <summary>
        /// Update central.
        ///
        /// Posteriormente este será el equivalente lógico de:
        ///
        /// MoveEffects()
        /// MoveJoints()
        /// MoveParticles()
        /// MoveSprites()
        /// MoveBlurs()
        /// MoveObjectBlurs()
        ///
        /// El orden exacto se fijará según MainScene.cpp.
        /// </summary>
        public void Update(
            GameTime gameTime)
        {
            if (_disposed)
            {
                return;
            }

            // El clock se mantiene actualizado incluso si apagamos
            // temporalmente los efectos para debug.
            Clock.Update(
                gameTime);

            if (!Enabled)
            {
                return;
            }

            // ---------------------------------------------------------
            // FASE 3 irá aquí.
            //
            // Importante:
            // NO agregar WorldObject.Update() individuales.
            //
            // Los futuros loops serán del estilo:
            //
            // for (int i = 0;
            //      i < ClassicFxPools.MaxEffects;
            //      i++)
            // {
            //     if (!Pools.Effects.IsActive(i))
            //         continue;
            //
            //     ref ClassicEffect effect =
            //         ref Effects[i];
            //
            //     ...
            // }
            // ---------------------------------------------------------
        }

        /// <summary>
        /// Limpia todo el estado de efectos manteniendo el runtime.
        /// </summary>
        public void Reset()
        {
            if (_disposed)
            {
                return;
            }

            Pools.Clear();

            ClearSpriteStorage();

            Clock
                .ResetReferenceFrameCounter();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Pools.Clear();

            ClearSpriteStorage();

            _spriteRenderer?
                .Dispose();

            _spriteRenderer =
                null;

            _disposed =
                true;
        }
     }
}