using System;
using Client.Main.Objects;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Tipo de propietario de una primitiva ClassicFX.
    ///
    /// El Main utiliza OBJECT* Owner.
    ///
    /// En MonoGame ese owner puede representar:
    ///
    /// - un WorldObject real: jugador, monstruo, arma, etc.
    /// - otra primitiva ClassicFX.
    ///
    /// No utilizamos object para evitar boxing y mantener
    /// explícita la semántica.
    /// </summary>
    public enum ClassicFxOwnerKind : byte
    {
        None = 0,

        WorldObject,

        ClassicFx
    }

    /// <summary>
    /// Equivalente seguro a OBJECT* Owner.
    ///
    /// Para referencias entre primitivas ClassicFX utilizamos
    /// ClassicFxHandle, cuya Generation evita referencias
    /// accidentales a slots reciclados.
    /// </summary>
    public readonly struct ClassicFxOwner :
        IEquatable<ClassicFxOwner>
    {
        public static readonly ClassicFxOwner None =
            default;

        public ClassicFxOwnerKind Kind
        {
            get;
        }

        public WorldObject WorldObject
        {
            get;
        }

        public ClassicFxHandle FxHandle
        {
            get;
        }

        public bool HasOwner =>
            Kind !=
            ClassicFxOwnerKind.None;

        private ClassicFxOwner(
            ClassicFxOwnerKind kind,
            WorldObject worldObject,
            ClassicFxHandle fxHandle)
        {
            Kind =
                kind;

            WorldObject =
                worldObject;

            FxHandle =
                fxHandle;
        }

        public static ClassicFxOwner FromWorldObject(
            WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return None;
            }

            return new ClassicFxOwner(
                ClassicFxOwnerKind.WorldObject,
                worldObject,
                ClassicFxHandle.Invalid);
        }

        public static ClassicFxOwner FromClassicFx(
            ClassicFxHandle handle)
        {
            if (!handle.IsValid)
            {
                return None;
            }

            return new ClassicFxOwner(
                ClassicFxOwnerKind.ClassicFx,
                null,
                handle);
        }

        public bool Equals(
            ClassicFxOwner other)
        {
            if (Kind !=
                other.Kind)
            {
                return false;
            }

            return Kind switch
            {
                ClassicFxOwnerKind.None =>
                    true,

                ClassicFxOwnerKind.WorldObject =>
                    ReferenceEquals(
                        WorldObject,
                        other.WorldObject),

                ClassicFxOwnerKind.ClassicFx =>
                    FxHandle ==
                    other.FxHandle,

                _ =>
                    false
            };
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is ClassicFxOwner other &&
                Equals(
                    other);
        }

        public override int GetHashCode()
        {
            return Kind switch
            {
                ClassicFxOwnerKind.WorldObject =>
                    HashCode.Combine(
                        (byte)Kind,
                        WorldObject),

                ClassicFxOwnerKind.ClassicFx =>
                    HashCode.Combine(
                        (byte)Kind,
                        FxHandle),

                _ =>
                    0
            };
        }

        public static bool operator ==(
            ClassicFxOwner left,
            ClassicFxOwner right)
        {
            return
                left.Equals(
                    right);
        }

        public static bool operator !=(
            ClassicFxOwner left,
            ClassicFxOwner right)
        {
            return
                !left.Equals(
                    right);
        }

        public override string ToString()
        {
            return Kind switch
            {
                ClassicFxOwnerKind.WorldObject =>
                    WorldObject == null
                        ? "WorldObject(null)"
                        : $"WorldObject({WorldObject.ObjectName})",

                ClassicFxOwnerKind.ClassicFx =>
                    FxHandle.ToString(),

                _ =>
                    "None"
            };
        }
    }
}