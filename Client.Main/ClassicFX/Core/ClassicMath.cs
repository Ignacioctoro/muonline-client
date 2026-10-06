using System;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    /// <summary>
    /// Matriz 3x4 utilizada por el cliente clásico de MU.
    ///
    /// IMPORTANTE:
    ///
    /// No convertimos todavía estas operaciones directamente a Matrix
    /// de MonoGame porque eso introduciría dudas sobre transposición,
    /// orden row/column y handedness.
    ///
    /// Primero reproducimos literalmente la matemática del Main.
    /// El renderer hará la conversión a MonoGame en un único punto.
    /// </summary>
    public readonly struct ClassicMatrix3x4
    {
        public readonly float M00;
        public readonly float M01;
        public readonly float M02;
        public readonly float M03;

        public readonly float M10;
        public readonly float M11;
        public readonly float M12;
        public readonly float M13;

        public readonly float M20;
        public readonly float M21;
        public readonly float M22;
        public readonly float M23;

        public ClassicMatrix3x4(
            float m00,
            float m01,
            float m02,
            float m03,

            float m10,
            float m11,
            float m12,
            float m13,

            float m20,
            float m21,
            float m22,
            float m23)
        {
            M00 = m00;
            M01 = m01;
            M02 = m02;
            M03 = m03;

            M10 = m10;
            M11 = m11;
            M12 = m12;
            M13 = m13;

            M20 = m20;
            M21 = m21;
            M22 = m22;
            M23 = m23;
        }

        public static ClassicMatrix3x4 Identity =>
            new(
                1f, 0f, 0f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 1f, 0f);
    }

    /// <summary>
    /// Port directo de las primitivas matemáticas utilizadas
    /// por ZzzMathLib del cliente clásico.
    /// </summary>
    public static class ClassicMath
    {
        public const float Pi =
            MathF.PI;

        public const float AngleToRadians =
            MathF.PI /
            180f;

        public const float RadiansToAngle =
            180f /
            MathF.PI;

        /// <summary>
        /// Port literal de AngleMatrix().
        ///
        /// El Main comenta:
        ///
        ///     matrix = (Z * Y) * X
        ///
        /// Los ángulos se reciben en grados.
        /// </summary>
        public static ClassicMatrix3x4 AngleMatrix(
            in Vector3 angles)
        {
            float angle;

            float sr;
            float sp;
            float sy;

            float cr;
            float cp;
            float cy;

            angle =
                angles.Z *
                AngleToRadians;

            sy =
                MathF.Sin(
                    angle);

            cy =
                MathF.Cos(
                    angle);

            angle =
                angles.Y *
                AngleToRadians;

            sp =
                MathF.Sin(
                    angle);

            cp =
                MathF.Cos(
                    angle);

            angle =
                angles.X *
                AngleToRadians;

            sr =
                MathF.Sin(
                    angle);

            cr =
                MathF.Cos(
                    angle);

            return new ClassicMatrix3x4(
                // row 0
                cp * cy,
                sr * sp * cy +
                    cr * -sy,
                cr * sp * cy +
                    -sr * -sy,
                0f,

                // row 1
                cp * sy,
                sr * sp * sy +
                    cr * cy,
                cr * sp * sy +
                    -sr * cy,
                0f,

                // row 2
                -sp,
                sr * cp,
                cr * cp,
                0f
            );
        }

        /// <summary>
        /// Port literal de AngleIMatrix().
        /// </summary>
        public static ClassicMatrix3x4 AngleIMatrix(
            in Vector3 angles)
        {
            float angle;

            float sr;
            float sp;
            float sy;

            float cr;
            float cp;
            float cy;

            angle =
                angles.Z *
                AngleToRadians;

            sy =
                MathF.Sin(
                    angle);

            cy =
                MathF.Cos(
                    angle);

            angle =
                angles.Y *
                AngleToRadians;

            sp =
                MathF.Sin(
                    angle);

            cp =
                MathF.Cos(
                    angle);

            angle =
                angles.X *
                AngleToRadians;

            sr =
                MathF.Sin(
                    angle);

            cr =
                MathF.Cos(
                    angle);

            return new ClassicMatrix3x4(
                cp * cy,
                cp * sy,
                -sp,
                0f,

                sr * sp * cy +
                    cr * -sy,

                sr * sp * sy +
                    cr * cy,

                sr * cp,
                0f,

                cr * sp * cy +
                    -sr * -sy,

                cr * sp * sy +
                    -sr * cy,

                cr * cp,
                0f
            );
        }

        /// <summary>
        /// Port de VectorRotate().
        /// </summary>
        public static Vector3 VectorRotate(
            in Vector3 input,
            in ClassicMatrix3x4 matrix)
        {
            return new Vector3(
                input.X * matrix.M00 +
                input.Y * matrix.M01 +
                input.Z * matrix.M02,

                input.X * matrix.M10 +
                input.Y * matrix.M11 +
                input.Z * matrix.M12,

                input.X * matrix.M20 +
                input.Y * matrix.M21 +
                input.Z * matrix.M22
            );
        }

        /// <summary>
        /// Port de VectorIRotate().
        /// </summary>
        public static Vector3 VectorIRotate(
            in Vector3 input,
            in ClassicMatrix3x4 matrix)
        {
            return new Vector3(
                input.X * matrix.M00 +
                input.Y * matrix.M10 +
                input.Z * matrix.M20,

                input.X * matrix.M01 +
                input.Y * matrix.M11 +
                input.Z * matrix.M21,

                input.X * matrix.M02 +
                input.Y * matrix.M12 +
                input.Z * matrix.M22
            );
        }

        /// <summary>
        /// Port de VectorTranslate().
        /// </summary>
        public static Vector3 VectorTranslate(
            in Vector3 input,
            in ClassicMatrix3x4 matrix)
        {
            return new Vector3(
                input.X +
                    matrix.M03,

                input.Y +
                    matrix.M13,

                input.Z +
                    matrix.M23
            );
        }

        /// <summary>
        /// Port de VectorTransform().
        /// </summary>
        public static Vector3 VectorTransform(
            in Vector3 input,
            in ClassicMatrix3x4 matrix)
        {
            return new Vector3(
                input.X * matrix.M00 +
                input.Y * matrix.M01 +
                input.Z * matrix.M02 +
                matrix.M03,

                input.X * matrix.M10 +
                input.Y * matrix.M11 +
                input.Z * matrix.M12 +
                matrix.M13,

                input.X * matrix.M20 +
                input.Y * matrix.M21 +
                input.Z * matrix.M22 +
                matrix.M23
            );
        }

        /// <summary>
        /// Port de R_ConcatTransforms().
        /// </summary>
        public static ClassicMatrix3x4 ConcatTransforms(
            in ClassicMatrix3x4 a,
            in ClassicMatrix3x4 b)
        {
            return new ClassicMatrix3x4(
                a.M00 * b.M00 +
                a.M01 * b.M10 +
                a.M02 * b.M20,

                a.M00 * b.M01 +
                a.M01 * b.M11 +
                a.M02 * b.M21,

                a.M00 * b.M02 +
                a.M01 * b.M12 +
                a.M02 * b.M22,

                a.M00 * b.M03 +
                a.M01 * b.M13 +
                a.M02 * b.M23 +
                a.M03,


                a.M10 * b.M00 +
                a.M11 * b.M10 +
                a.M12 * b.M20,

                a.M10 * b.M01 +
                a.M11 * b.M11 +
                a.M12 * b.M21,

                a.M10 * b.M02 +
                a.M11 * b.M12 +
                a.M12 * b.M22,

                a.M10 * b.M03 +
                a.M11 * b.M13 +
                a.M12 * b.M23 +
                a.M13,


                a.M20 * b.M00 +
                a.M21 * b.M10 +
                a.M22 * b.M20,

                a.M20 * b.M01 +
                a.M21 * b.M11 +
                a.M22 * b.M21,

                a.M20 * b.M02 +
                a.M21 * b.M12 +
                a.M22 * b.M22,

                a.M20 * b.M03 +
                a.M21 * b.M13 +
                a.M22 * b.M23 +
                a.M23
            );
        }

        public static Vector3 VectorAdd(
            in Vector3 a,
            in Vector3 b)
        {
            return
                a +
                b;
        }

        public static Vector3 VectorSubtract(
            in Vector3 a,
            in Vector3 b)
        {
            return
                a -
                b;
        }

        /// <summary>
        /// Port de VectorAddScaled().
        /// </summary>
        public static Vector3 VectorAddScaled(
            in Vector3 a,
            in Vector3 b,
            float scale)
        {
            return new Vector3(
                a.X +
                    b.X * scale,

                a.Y +
                    b.Y * scale,

                a.Z +
                    b.Z * scale
            );
        }

        public static Vector3 VectorScale(
            in Vector3 value,
            float scale)
        {
            return new Vector3(
                value.X * scale,
                value.Y * scale,
                value.Z * scale
            );
        }

        public static float VectorLength(
            in Vector3 value)
        {
            return
                MathF.Sqrt(
                    value.X * value.X +
                    value.Y * value.Y +
                    value.Z * value.Z);
        }

        /// <summary>
        /// Equivalente a VectorNormalize().
        ///
        /// Devuelve además la longitud original.
        /// </summary>
        public static float VectorNormalize(
            ref Vector3 value)
        {
            float length =
                VectorLength(
                    value);

            if (length != 0f)
            {
                float inverse =
                    1f /
                    length;

                value.X *=
                    inverse;

                value.Y *=
                    inverse;

                value.Z *=
                    inverse;
            }

            return length;
        }

        /// <summary>
        /// Port de VectorMA().
        ///
        /// result =
        ///     start +
        ///     direction * scale
        /// </summary>
        public static Vector3 VectorMA(
            in Vector3 start,
            float scale,
            in Vector3 direction)
        {
            return new Vector3(
                start.X +
                    scale *
                    direction.X,

                start.Y +
                    scale *
                    direction.Y,

                start.Z +
                    scale *
                    direction.Z
            );
        }

        /// <summary>
        /// Equivalente conceptual a MovePosition()
        /// del Main.
        ///
        /// IMPORTANTE:
        /// Speed sigue usando exactamente unidades del Main.
        /// No se corrige Scale aquí.
        /// </summary>
        public static Vector3 MovePosition(
            in Vector3 position,
            in Vector3 angle,
            in Vector3 speed,
            float frameFactor)
        {
            ClassicMatrix3x4 matrix =
                AngleMatrix(
                    angle);

            Vector3 velocity =
                VectorRotate(
                    speed,
                    matrix);

            velocity *=
                frameFactor;

            return
                position +
                velocity;
        }

        /// <summary>
        /// Rota un offset local y lo suma a una posición.
        ///
        /// Será común en effects, joints y attachments.
        /// </summary>
        public static Vector3 RotateAndAdd(
            in Vector3 origin,
            in Vector3 offset,
            in Vector3 angle)
        {
            ClassicMatrix3x4 matrix =
                AngleMatrix(
                    angle);

            Vector3 rotated =
                VectorRotate(
                    offset,
                    matrix);

            return
                origin +
                rotated;
        }
    }
}