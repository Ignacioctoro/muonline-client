using System;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// RNG central de ClassicFX.
    ///
    /// Evita crear Random dentro de efectos y nos permite,
    /// posteriormente, usar seeds deterministas para comparar
    /// MonoGame contra capturas/recordings del Main.
    ///
    /// ClassicFX se actualiza desde el hilo principal del juego,
    /// por lo cual no necesitamos locking por cada llamada.
    /// </summary>
    public sealed class ClassicRandom
    {
        private Random
            _random;

        public ClassicRandom()
        {
            _random =
                new Random();
        }

        public ClassicRandom(
            int seed)
        {
            _random =
                new Random(
                    seed);
        }

        /// <summary>
        /// Reinicia la secuencia.
        ///
        /// Se utilizará principalmente para tests deterministas.
        /// No debe llamarse por frame.
        /// </summary>
        public void Seed(
            int seed)
        {
            _random =
                new Random(
                    seed);
        }

        /// <summary>
        /// Rango entero con ambos extremos incluidos,
        /// igual a la API moderna usada por MuMain.
        /// </summary>
        public int RangeInt(
            int minInclusive,
            int maxInclusive)
        {
            if (minInclusive >=
                maxInclusive)
            {
                return minInclusive;
            }

            long value =
                _random.NextInt64(
                    minInclusive,
                    (long)maxInclusive + 1L);

            return (int)value;
        }

        /// <summary>
        /// Float dentro de [min, max).
        /// </summary>
        public float RangeFloat(
            float minInclusive,
            float maxInclusive)
        {
            if (minInclusive >=
                maxInclusive)
            {
                return minInclusive;
            }

            return
                minInclusive +
                (float)_random.NextDouble() *
                (
                    maxInclusive -
                    minInclusive
                );
        }

        /// <summary>
        /// Valor entre 0 y 1.
        /// </summary>
        public float Unit()
        {
            return
                (float)_random
                    .NextDouble();
        }

        public double UnitDouble()
        {
            return
                _random
                    .NextDouble();
        }

        /// <summary>
        /// Sustituto cómodo para patrones antiguos:
        ///
        ///     rand() % N
        ///
        /// Devuelve [0, N).
        /// </summary>
        public int Modulo(
            int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                return 0;
            }

            return
                _random.Next(
                    exclusiveMaximum);
        }

        /// <summary>
        /// Equivalente a rand_fps_check() /
        /// Random::FpsCheck() del Main moderno.
        ///
        /// referenceFrames=1:
        ///     chance = FrameFactor
        ///
        /// referenceFrames=10:
        ///     chance =
        ///         (1 / 10) *
        ///         FrameFactor
        /// </summary>
        public bool FpsCheck(
            int referenceFrames,
            float frameFactor)
        {
            if (referenceFrames <= 0)
            {
                return false;
            }

            double clampedFactor =
                Math.Clamp(
                    frameFactor,
                    0f,
                    1f);

            double chance =
                referenceFrames == 1
                    ? clampedFactor
                    : (
                        1.0 /
                        referenceFrames
                      ) *
                      clampedFactor;

            return
                UnitDouble() <=
                chance;
        }

        /// <summary>
        /// Versión que toma directamente el reloj.
        /// </summary>
        public bool FpsCheck(
            int referenceFrames,
            ClassicFxClock clock)
        {
            if (clock == null)
            {
                return false;
            }

            return FpsCheck(
                referenceFrames,
                clock.FrameFactor);
        }
    }
}