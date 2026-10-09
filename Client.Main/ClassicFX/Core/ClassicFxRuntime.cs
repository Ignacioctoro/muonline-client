using System;
using System.Threading.Tasks;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Client.Main.Controls;
using Client.Main.Controllers;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime :
        IDisposable
    {
        private bool
            _disposed;

        private ClassicBillboardRenderer
            _billboardRenderer;

        private ClassicSpriteRenderer
            _spriteRenderer;

        private ClassicParticleRenderer
            _particleRenderer;

        private ClassicJointRenderer
            _jointRenderer;

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

            await Textures
                .LoadParticlesAsync();

            _billboardRenderer ??=
                new ClassicBillboardRenderer(
                    GraphicsManager
                        .Instance
                        .GraphicsDevice);

            _spriteRenderer ??=
                new ClassicSpriteRenderer(
                    _billboardRenderer,
                    Textures);

            _particleRenderer ??=
                new ClassicParticleRenderer(
                    _billboardRenderer,
                    Textures);

            _jointRenderer ??=
                new ClassicJointRenderer(_billboardRenderer, Textures);
        }

        public void Update(
            GameTime gameTime)
        {
            if (_disposed)
            {
                return;
            }

            Clock.Update(
                gameTime);

            if (!Enabled)
            {
                return;
            }

            // ClassicFX transient state used by MoveParticles().
            // Resets/reuses the fixed terrain-light pool; no allocations.
            BeginClassicFxMoveFrame();

            MoveEffects();
            MoveParticles();
            MoveJoints();
            MoveBlurs();

            // PrÃ³ximos:
            //
            // MoveEffects();
            // MoveJoints();
            // MoveBlurs();
            // MoveObjectBlurs();
        }

        public void Reset()
        {
            if (_disposed)
            {
                return;
            }

            Pools.Clear();

            ClearSpriteStorage();

            ClearParticleStorage();

            ClearJointStorage();
            ClearEffectStorage();
            ClearBlurStorage();

            ResetMoveBridge();

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

            ClearParticleStorage();

            ClearJointStorage();
            ClearEffectStorage();
            ClearBlurStorage();

            DisposeMoveBridge();

            _spriteRenderer =
                null;

            _particleRenderer =
                null;

            _jointRenderer =
                null;

            _billboardRenderer?
                .Dispose();

            _billboardRenderer =
                null;

            _disposed =
                true;
        }
    }
}

