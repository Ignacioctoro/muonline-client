using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Reloj lógico del sistema ClassicFX.
    ///
    /// MU Online fue diseñado alrededor de una referencia de 25 FPS.
    /// Los valores originales de movimiento, animación y efectos están
    /// expresados en "pasos por frame de referencia".
    ///
    /// Este reloj permite conservar literalmente esos valores aunque
    /// MonoGame esté ejecutándose a 30, 60, 120 FPS, etc.
    /// </summary>
    public sealed class ClassicFxClock
    {
        /// <summary>
        /// FPS para el cual fue diseñado el cliente clásico de MU.
        ///
        /// NO modificar para ajustar visualmente efectos.
        /// </summary>
        public const double ReferenceFps = 25.0;

        private const double
            MillisecondsPerSecond = 1000.0;

        private const double
            MinimumFrameTimeMilliseconds = 0.001;

        /// <summary>
        /// Acumulador utilizado para reconstruir un contador
        /// lógico equivalente a frames de referencia de 25 FPS.
        /// </summary>
        private double
            _referenceFrameAccumulator;

        /// <summary>
        /// FPS instantáneos calculados a partir del delta real.
        /// </summary>
        public double Fps
        {
            get;
            private set;
        }

        /// <summary>
        /// Equivalente a FPS_ANIMATION_FACTOR del Main.
        ///
        /// 25 FPS  -> 1.0
        /// 50 FPS  -> 0.5
        /// 60 FPS  -> ~0.4166667
        /// 100 FPS -> 0.25
        ///
        /// Por debajo de 25 FPS permanece limitado a 1.
        /// </summary>
        public float FrameFactor
        {
            get;
            private set;
        }

        /// <summary>
        /// Delta real del frame en milisegundos.
        /// </summary>
        public double DeltaMilliseconds
        {
            get;
            private set;
        }

        /// <summary>
        /// Delta real del frame en segundos.
        ///
        /// Este valor sirve para sistemas realmente basados en tiempo.
        /// Los efectos clásicos normalmente deben usar FrameFactor.
        /// </summary>
        public float DeltaSeconds =>
            (float)(
                DeltaMilliseconds /
                MillisecondsPerSecond);

        /// <summary>
        /// Tiempo absoluto suministrado por MonoGame.
        ///
        /// Será nuestro equivalente base a WorldTime.
        /// </summary>
        public double WorldTimeMilliseconds
        {
            get;
            private set;
        }

        /// <summary>
        /// Contador de frames lógicos de referencia.
        ///
        /// Avanza aproximadamente 25 veces por segundo cuando el cliente
        /// funciona a 25 FPS o más.
        /// </summary>
        public long ReferenceFrame
        {
            get;
            private set;
        }

        /// <summary>
        /// Indica si durante este Update avanzamos un frame lógico.
        ///
        /// Es importante para condiciones del Main como:
        ///
        ///     frame % 30 == 0
        ///
        /// porque no queremos ejecutar la condición repetidamente
        /// mientras ReferenceFrame conserva el mismo valor.
        /// </summary>
        public bool AdvancedReferenceFrame
        {
            get;
            private set;
        }

        /// <summary>
        /// Fracción restante hacia el próximo frame lógico.
        ///
        /// Útil para depuración.
        /// </summary>
        public double ReferenceFramePhase =>
            _referenceFrameAccumulator;

        public void Update(
            GameTime gameTime)
        {
            if (gameTime == null)
            {
                return;
            }

            double deltaMilliseconds =
                gameTime
                    .ElapsedGameTime
                    .TotalMilliseconds;

            if (!double.IsFinite(
                    deltaMilliseconds) ||
                deltaMilliseconds <
                    MinimumFrameTimeMilliseconds)
            {
                deltaMilliseconds =
                    MinimumFrameTimeMilliseconds;
            }

            DeltaMilliseconds =
                deltaMilliseconds;

            WorldTimeMilliseconds =
                gameTime
                    .TotalGameTime
                    .TotalMilliseconds;

            Fps =
                MillisecondsPerSecond /
                deltaMilliseconds;

            double fpsRatio =
                Fps <= 0.0
                    ? 0.0
                    : ReferenceFps / Fps;

            FrameFactor =
                (float)Math.Clamp(
                    fpsRatio,
                    0.0,
                    1.0);

            // ---------------------------------------------------------
            // Construir un contador lógico de 25 FPS.
            //
            // Ejemplo a 60 FPS:
            //
            // factor ~= 0.416666
            //
            // tras varios frames reales acumulamos 1.0 y avanzamos
            // exactamente un frame lógico.
            // ---------------------------------------------------------

            _referenceFrameAccumulator +=
                FrameFactor;

            AdvancedReferenceFrame =
                false;

            if (_referenceFrameAccumulator >=
                1.0)
            {
                _referenceFrameAccumulator -=
                    1.0;

                ReferenceFrame++;

                AdvancedReferenceFrame =
                    true;
            }
        }

        /// <summary>
        /// Equivalente seguro a patrones clásicos como:
        ///
        ///     if (MoveSceneFrame % 30 == 0)
        ///
        /// Solo devuelve true cuando acaba de avanzar realmente
        /// el frame lógico.
        /// </summary>
        public bool IsReferenceFrameInterval(
            int interval)
        {
            if (interval <= 0 ||
                !AdvancedReferenceFrame)
            {
                return false;
            }

            return
                ReferenceFrame %
                interval ==
                0;
        }

        /// <summary>
        /// Escala un cambio lineal escrito para un frame clásico.
        ///
        /// Original:
        ///
        ///     Position += 10;
        ///
        /// Port:
        ///
        ///     Position +=
        ///         Clock.ScaleLinear(10);
        /// </summary>
        public float ScaleLinear(
            float value)
        {
            return
                value *
                FrameFactor;
        }

        /// <summary>
        /// Convierte una modificación multiplicativa clásica
        /// en frame-rate independent.
        ///
        /// Original:
        ///
        ///     value /= 1.8f;
        ///
        /// Port:
        ///
        ///     value *=
        ///         Clock.ScaleExponential(
        ///             1f / 1.8f);
        /// </summary>
        public float ScaleExponential(
            float perReferenceFrameMultiplier)
        {
            return MathF.Pow(
                perReferenceFrameMultiplier,
                FrameFactor);
        }

        /// <summary>
        /// Reinicia únicamente el contador lógico de frames.
        ///
        /// WorldTime sigue viniendo del GameTime global.
        /// </summary>
        public void ResetReferenceFrameCounter()
        {
            _referenceFrameAccumulator =
                0.0;

            ReferenceFrame =
                0;

            AdvancedReferenceFrame =
                false;
        }
    }
}