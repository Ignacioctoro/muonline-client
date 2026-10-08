using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private readonly ClassicParticle[]
            _particles =
                new ClassicParticle[
                    ClassicFxPools.MaxParticles
                ];

        private Vector3
            _particleWind;

        private Vector3
            _particleWindVelocity;

        public int ActiveParticleCount =>
            Pools
                .Particles
                .ActiveCount;

        public Vector3 ParticleWind =>
            _particleWind;

        public Vector3 ParticleWindVelocity =>
            _particleWindVelocity;

        public ClassicFxHandle CreateParticle(
            int type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            int subType,
            float scale,
            ClassicFxOwner target)
        {
            if (_disposed ||
                !Enabled)
            {
                return
                    ClassicFxHandle.Invalid;
            }

            if (!Pools
                    .Particles
                    .TryAcquire(
                        out ClassicFxHandle handle))
            {
                return
                    ClassicFxHandle.Invalid;
            }

            ref ClassicParticle particle =
                ref _particles[
                    handle.Index
                ];

            particle.Initialize(
                type,
                position,
                angle,
                light,
                subType,
                scale,
                target);

            InitializeParticleByType(
                ref particle,
                position,
                angle,
                light,
                scale);

            return
                handle;
        }

        public ClassicFxHandle CreateParticle(
            int type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            int subType = 0,
            float scale = 1f)
        {
            return CreateParticle(
                type,
                position,
                angle,
                light,
                subType,
                scale,
                ClassicFxOwner.None);
        }

        public ClassicFxHandle CreateParticleFpsChecked(
            int type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            int subType,
            float scale,
            ClassicFxOwner target)
        {
            if (_disposed ||
                !Enabled)
            {
                return
                    ClassicFxHandle.Invalid;
            }

            if (!Random.FpsCheck(
                    1,
                    Clock))
            {
                return
                    ClassicFxHandle.Invalid;
            }

            return CreateParticle(
                type,
                position,
                angle,
                light,
                subType,
                scale,
                target);
        }

        public ClassicFxHandle CreateParticleFpsChecked(
            int type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            int subType = 0,
            float scale = 1f)
        {
            return CreateParticleFpsChecked(
                type,
                position,
                angle,
                light,
                subType,
                scale,
                ClassicFxOwner.None);
        }

        /// <summary>
        public void MoveParticles()
        {
            if (_disposed ||
                !Enabled)
            {
                return;
            }

            float frameFactor =
                Clock.FrameFactor;

            UpdateParticleWind(
                frameFactor);

            for (int i = 0;
                 i < ClassicFxPools.MaxParticles;
                 i++)
            {
                if (!Pools
                        .Particles
                        .IsActive(
                            i))
                {
                    continue;
                }

                ref ClassicParticle particle =
                    ref GetParticleRef(
                        i);

                // Main:
                //
                // o->LifeTime -=
                //     FPS_ANIMATION_FACTOR;
                particle.LifeTime -=
                    frameFactor;

                bool keepAlive =
                    particle.LifeTime >
                    0f;

                // Main ejecuta MovePosition antes del switch Type.
                if (particle.EnableMove)
                {
                    particle.Position =
                        ClassicMath.MovePosition(
                            particle.Position,
                            particle.Angle,
                            particle.Velocity,
                            frameFactor);
                }

                MoveParticleByType(
                    ref particle,
                    frameFactor,
                    ref keepAlive);

                if (!keepAlive)
                {
                    ReleaseParticleAt(
                        i);
                }
            }
        }

        /// <summary>
        /// Port progresivo del switch(o->Type)
        /// de MoveParticles().
        /// </summary>
        private void MoveParticleByType(
            ref ClassicParticle particle,
            float frameFactor,
            ref bool keepAlive)
        {
            if (MoveParticleA(
                    ref particle,
                    frameFactor,
                    ref keepAlive))
            {
                return;
            }

            if (MoveParticleB(
                    ref particle,
                    frameFactor,
                    ref keepAlive))
            {
                return;
            }

            if (MoveParticleC(
                    ref particle,
                    frameFactor,
                    ref keepAlive))
            {
                return;
            }

            switch (particle.Type)
            {
                case ClassicTextureIds.BitmapFlareBlue:
                {
                    if (particle.SubType ==
                        0)
                    {
                        // Main:
                        //
                        // o->Scale = 0.2f;
                        particle.Scale =
                            0.2f;

                        // o->Velocity[2] +=
                        //     0.4f *
                        //     FPS_ANIMATION_FACTOR;
                        particle.Velocity.Z +=
                            0.4f *
                            frameFactor;

                        // o->Velocity[2] =
                        //     min(
                        //         8.f *
                        //         FPS_ANIMATION_FACTOR,
                        //         o->Velocity[2]);
                        particle.Velocity.Z =
                            MathF.Min(
                                8f *
                                frameFactor,

                                particle
                                    .Velocity
                                    .Z);

                        if (particle.LifeTime <
                            5f)
                        {
                            float fade =
                                0.98f +
                                (
                                    0.02f -
                                    frameFactor
                                );

                            particle.Light.X *=
                                fade;

                            particle.Light.Y *=
                                fade;

                            particle.Light.Z *=
                                fade;
                        }
                    }
                    else if (particle.SubType ==
                             1)
                    {
                        particle.Light.X *=
                            0.99f +
                            (
                                0.01f -
                                frameFactor
                            );

                        float colorFade =
                            MathF.Pow(
                                1f /
                                1.1f,

                                frameFactor);

                        particle.Light.Y *=
                            colorFade;

                        particle.Light.Z *=
                            colorFade;

                        particle.Scale +=
                            1.5f *
                            frameFactor;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Equivalente inicial de RenderParticles().
        /// </summary>
        public void RenderParticles(
            byte renderPass = 0)
        {
            if (_disposed ||
                !Enabled ||
                _particleRenderer == null)
            {
                return;
            }

            _particleRenderer.Begin();

            for (int i = 0;
                 i < ClassicFxPools.MaxParticles;
                 i++)
            {
                if (!Pools
                        .Particles
                        .IsActive(
                            i))
                {
                    continue;
                }

                ref ClassicParticle particle =
                    ref GetParticleRef(
                        i);

                // Misma separaciÃ³n de water pass del Main.
                if (renderPass ==
                    1)
                {
                    if (particle.Position.Z >
                        350f)
                    {
                        continue;
                    }
                }
                else if (renderPass ==
                         2)
                {
                    if (particle.Position.Z <=
                        300f)
                    {
                        continue;
                    }
                }

                _particleRenderer
                    .QueueParticle(
                        ref particle,
                        i,
                        Clock.FrameFactor,
                        (float)Clock.WorldTimeMilliseconds);
            }

            _particleRenderer.End();
        }

        private void UpdateParticleWind(
            float frameFactor)
        {
            float velocityX =
                _particleWindVelocity.X;

            float velocityY =
                _particleWindVelocity.Y;

            velocityX +=
                (
                    Random.Modulo(
                        2001) -
                    1000
                ) *
                0.0006f;

            velocityY +=
                (
                    Random.Modulo(
                        2001) -
                    1000
                ) *
                0.0006f;

            velocityX =
                MathHelper.Clamp(
                    velocityX,
                    -0.6f,
                    0.6f);

            velocityY =
                MathHelper.Clamp(
                    velocityY,
                    -0.6f,
                    0.6f);

            velocityX *=
                frameFactor;

            velocityY *=
                frameFactor;

            _particleWindVelocity =
                new Vector3(
                    velocityX,
                    velocityY,
                    0f);

            float windX =
                _particleWind.X +
                velocityX;

            float windY =
                _particleWind.Y +
                velocityY;

            windX =
                MathHelper.Clamp(
                    windX,
                    -1.7f,
                    1.7f);

            windY =
                MathHelper.Clamp(
                    windY,
                    -1.7f,
                    1.7f);

            windX *=
                frameFactor;

            windY *=
                frameFactor;

            _particleWind =
                new Vector3(
                    windX,
                    windY,
                    0f);
        }

        public bool TryGetParticle(
            ClassicFxHandle handle,
            out ClassicParticle particle)
        {
            if (handle.Kind !=
                    ClassicFxPoolKind.Particle ||
                !Pools
                    .Particles
                    .IsAlive(
                        handle))
            {
                particle =
                    default;

                return false;
            }

            particle =
                _particles[
                    handle.Index
                ];

            return true;
        }

        private ref ClassicParticle GetParticleRef(
            int index)
        {
            return ref
                _particles[
                    index
                ];
        }

        public bool ReleaseParticle(
            ClassicFxHandle handle)
        {
            if (handle.Kind !=
                    ClassicFxPoolKind.Particle ||
                !Pools
                    .Particles
                    .IsAlive(
                        handle))
            {
                return false;
            }

            _particles[
                handle.Index
            ].Clear();

            return Pools
                .Particles
                .Release(
                    handle);
        }

        private void ReleaseParticleAt(
            int index)
        {
            ClassicFxHandle handle =
                Pools
                    .Particles
                    .GetHandle(
                        index);

            if (!handle.IsValid)
            {
                return;
            }

            _particles[
                index
            ].Clear();

            Pools
                .Particles
                .Release(
                    handle);
        }

        private void ResetParticleState()
        {
            _particleWind =
                Vector3.Zero;

            _particleWindVelocity =
                Vector3.Zero;
        }

        private void ClearParticleStorage()
        {
            Array.Clear(
                _particles);

            ResetParticleState();
        }
    }
}
