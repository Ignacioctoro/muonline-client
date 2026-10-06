using System;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Tipo del pool ClassicFX.
    ///
    /// El tipo forma parte del handle para evitar que un índice de
    /// Effects pueda confundirse con uno de Joints, Particles, etc.
    /// </summary>
    public enum ClassicFxPoolKind : byte
    {
        None = 0,

        Effect,
        Sprite,
        Particle,
        Joint,
        Blur,
        ObjectBlur
    }

    /// <summary>
    /// Referencia segura a un slot dentro de ClassicFX.
    ///
    /// Generation evita que una referencia antigua vuelva a apuntar
    /// accidentalmente a un slot que ya fue reutilizado.
    /// </summary>
    public readonly struct ClassicFxHandle :
        IEquatable<ClassicFxHandle>
    {
        public static readonly ClassicFxHandle Invalid =
            new(
                ClassicFxPoolKind.None,
                -1,
                0);

        public ClassicFxPoolKind Kind
        {
            get;
        }

        public int Index
        {
            get;
        }

        public uint Generation
        {
            get;
        }

        public bool IsValid =>
            Kind !=
                ClassicFxPoolKind.None &&
            Index >= 0 &&
            Generation != 0;

        public ClassicFxHandle(
            ClassicFxPoolKind kind,
            int index,
            uint generation)
        {
            Kind =
                kind;

            Index =
                index;

            Generation =
                generation;
        }

        public bool Equals(
            ClassicFxHandle other)
        {
            return
                Kind ==
                    other.Kind &&
                Index ==
                    other.Index &&
                Generation ==
                    other.Generation;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is ClassicFxHandle other &&
                Equals(
                    other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                (byte)Kind,
                Index,
                Generation);
        }

        public static bool operator ==(
            ClassicFxHandle left,
            ClassicFxHandle right)
        {
            return
                left.Equals(
                    right);
        }

        public static bool operator !=(
            ClassicFxHandle left,
            ClassicFxHandle right)
        {
            return
                !left.Equals(
                    right);
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return
                    "ClassicFxHandle.Invalid";
            }

            return
                $"{Kind}[{Index}]#{Generation}";
        }
    }

    /// <summary>
    /// Pool fijo de slots.
    ///
    /// No crea ni destruye objetos durante gameplay.
    /// Solo marca posiciones de arrays como libres/ocupadas.
    ///
    /// Los arrays con los datos reales de Effect/Joint/Particle/etc.
    /// se agregarán encima de estos slots.
    /// </summary>
    public sealed class ClassicFxSlotPool
    {
        private readonly bool[]
            _active;

        private readonly uint[]
            _generations;

        public ClassicFxPoolKind Kind
        {
            get;
        }

        public int Capacity =>
            _active.Length;

        public int ActiveCount
        {
            get;
            private set;
        }

        public int FreeCount =>
            Capacity -
            ActiveCount;

        public ClassicFxSlotPool(
            ClassicFxPoolKind kind,
            int capacity)
        {
            if (kind ==
                ClassicFxPoolKind.None)
            {
                throw new ArgumentException(
                    "A slot pool requires a valid kind.",
                    nameof(kind));
            }

            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity));
            }

            Kind =
                kind;

            _active =
                new bool[
                    capacity
                ];

            _generations =
                new uint[
                    capacity
                ];

            // Generation 0 se reserva para handles inválidos.
            for (int i = 0;
                 i < capacity;
                 i++)
            {
                _generations[i] =
                    1;
            }
        }

        /// <summary>
        /// Busca un slot libre sin generar garbage.
        /// </summary>
        public bool TryAcquire(
            out ClassicFxHandle handle)
        {
            // Fidelidad Main:
            //
            // CreateEffect/CreateSprite/CreateParticle/etc.
            // recorren los arrays clásicos desde índice 0 y utilizan
            // siempre el primer slot que tenga Live == false.
            //
            // No utilizamos un cursor circular porque el índice del
            // pool también determina el orden posterior de procesamiento
            // y render.
            for (int index = 0;
                index < Capacity;
                index++)
            {
                if (_active[index])
                {
                    continue;
                }

                _active[index] =
                    true;

                ActiveCount++;

                handle =
                    new ClassicFxHandle(
                        Kind,
                        index,
                        _generations[
                            index
                        ]);

                return true;
            }

            handle =
                ClassicFxHandle.Invalid;

            return false;
        }

        /// <summary>
        /// Libera un slot.
        ///
        /// Incrementamos Generation para invalidar cualquier
        /// referencia antigua a ese índice.
        /// </summary>
        public bool Release(
            in ClassicFxHandle handle)
        {
            if (!IsAlive(
                    handle))
            {
                return false;
            }

            int index =
                handle.Index;

            _active[index] =
                false;

            ActiveCount--;

            AdvanceGeneration(
                index);

            return true;
        }

        public bool IsAlive(
            in ClassicFxHandle handle)
        {
            if (handle.Kind !=
                    Kind ||
                handle.Index < 0 ||
                handle.Index >=
                    Capacity)
            {
                return false;
            }

            return
                _active[
                    handle.Index
                ] &&
                _generations[
                    handle.Index
                ] ==
                handle.Generation;
        }

        /// <summary>
        /// Fast path para los loops tipo:
        ///
        /// for i = 0; i &lt; MAX_EFFECTS; i++
        ///
        /// No construye handles.
        /// </summary>
        public bool IsActive(
            int index)
        {
            return
                (uint)index <
                    (uint)Capacity &&
                _active[
                    index
                ];
        }

        /// <summary>
        /// Devuelve el handle actual del índice si está activo.
        /// </summary>
        public ClassicFxHandle GetHandle(
            int index)
        {
            if (!IsActive(
                    index))
            {
                return
                    ClassicFxHandle.Invalid;
            }

            return
                new ClassicFxHandle(
                    Kind,
                    index,
                    _generations[
                        index
                    ]);
        }

        /// <summary>
        /// Libera todos los slots.
        ///
        /// Se utilizará al cambiar mapa/destruir runtime.
        /// </summary>
        public void Clear()
        {
            for (int i = 0;
                 i < _active.Length;
                 i++)
            {
                if (!_active[i])
                {
                    continue;
                }

                _active[i] =
                    false;

                AdvanceGeneration(
                    i);
            }

            ActiveCount =
                0;
        }

        private void AdvanceGeneration(
            int index)
        {
            uint generation =
                _generations[
                    index
                ] +
                1u;

            // Generation 0 significa inválido.
            if (generation == 0)
            {
                generation =
                    1;
            }

            _generations[
                index
            ] =
                generation;
        }
    }

    /// <summary>
    /// Conjunto completo de pools del cliente clásico.
    ///
    /// Los tamaños iniciales corresponden al Main actual y,
    /// por ahora, NO se escalan según plataforma.
    ///
    /// Android y Windows simulan la misma cantidad posible
    /// de efectos. La optimización se hará en rendering/culling.
    /// </summary>
    public sealed class ClassicFxPools
    {
        // -------------------------------------------------------------
        // Valores del Main clásico actual.
        // -------------------------------------------------------------

        public const int MaxEffects =
            200;

        public const int MaxSprites =
            1000;

        public const int MaxParticles =
            3000;

        public const int MaxJoints =
            500;

        public const int MaxBlurs =
            100;

        public const int MaxObjectBlurs =
            1000;

        public ClassicFxSlotPool Effects
        {
            get;
        }

        public ClassicFxSlotPool Sprites
        {
            get;
        }

        public ClassicFxSlotPool Particles
        {
            get;
        }

        public ClassicFxSlotPool Joints
        {
            get;
        }

        public ClassicFxSlotPool Blurs
        {
            get;
        }

        public ClassicFxSlotPool ObjectBlurs
        {
            get;
        }

        public ClassicFxPools()
        {
            Effects =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.Effect,
                    MaxEffects);

            Sprites =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.Sprite,
                    MaxSprites);

            Particles =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.Particle,
                    MaxParticles);

            Joints =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.Joint,
                    MaxJoints);

            Blurs =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.Blur,
                    MaxBlurs);

            ObjectBlurs =
                new ClassicFxSlotPool(
                    ClassicFxPoolKind.ObjectBlur,
                    MaxObjectBlurs);
        }

        public ClassicFxSlotPool GetPool(
            ClassicFxPoolKind kind)
        {
            return kind switch
            {
                ClassicFxPoolKind.Effect =>
                    Effects,

                ClassicFxPoolKind.Sprite =>
                    Sprites,

                ClassicFxPoolKind.Particle =>
                    Particles,

                ClassicFxPoolKind.Joint =>
                    Joints,

                ClassicFxPoolKind.Blur =>
                    Blurs,

                ClassicFxPoolKind.ObjectBlur =>
                    ObjectBlurs,

                _ =>
                    null
            };
        }

        public int TotalActiveCount =>
            Effects.ActiveCount +
            Sprites.ActiveCount +
            Particles.ActiveCount +
            Joints.ActiveCount +
            Blurs.ActiveCount +
            ObjectBlurs.ActiveCount;

        public void Clear()
        {
            Effects.Clear();
            Sprites.Clear();
            Particles.Clear();
            Joints.Clear();
            Blurs.Clear();
            ObjectBlurs.Clear();
        }
    }
}