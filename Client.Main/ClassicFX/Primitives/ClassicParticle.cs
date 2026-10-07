using Client.Main.ClassicFX.Core;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Primitives
{
    /// <summary>
    /// Port estructural de PARTICLE del Main clásico.
    ///
    /// Original:
    ///
    /// bool    Live;
    /// int     Type;
    /// int     TexType;
    /// int     SubType;
    /// float   Scale;
    /// vec3_t  Position;
    /// vec3_t  Angle;
    /// vec3_t  Light;
    /// float   Alpha;
    /// float   LifeTime;
    /// OBJECT* Target;
    /// float   Rotation;
    /// int     Frame;
    /// bool    bEnableMove;
    /// float   Gravity;
    /// vec3_t  Velocity;
    /// vec3_t  TurningForce;
    /// vec3_t  StartPosition;
    /// int     iNumBone;
    /// bool    bRepeatedly;
    /// float   fRepeatedlyHeight;
    ///
    /// Live NO se duplica:
    /// ClassicFxSlotPool es nuestra fuente de verdad.
    /// </summary>
    public struct ClassicParticle
    {
        /// <summary>
        /// Tipo lógico de partícula.
        ///
        /// Ej:
        /// BITMAP_FLARE_BLUE
        /// BITMAP_SMOKE
        /// BITMAP_FIRE
        /// </summary>
        public int Type;

        /// <summary>
        /// Textura que realmente se renderiza.
        ///
        /// El Main comienza con:
        ///
        ///     TexType = Type
        ///
        /// pero muchos Type/SubType lo cambian después.
        /// </summary>
        public int TexType;

        public int SubType;

        public float Scale;

        public Vector3 Position;

        public Vector3 Angle;

        public Vector3 Light;

        public float Alpha;

        /// <summary>
        /// El Main expresa LifeTime en frames de referencia.
        ///
        /// MoveParticles hace:
        ///
        ///     LifeTime -= FPS_ANIMATION_FACTOR;
        /// </summary>
        public float LifeTime;

        /// <summary>
        /// Equivalente a PARTICLE::Target.
        ///
        /// Puede apuntar a un WorldObject o a otra
        /// primitiva ClassicFX.
        /// </summary>
        public ClassicFxOwner Target;

        public float Rotation;

        public int Frame;

        /// <summary>
        /// Equivalente a bEnableMove.
        /// </summary>
        public bool EnableMove;

        public float Gravity;

        public Vector3 Velocity;

        public Vector3 TurningForce;

        public Vector3 StartPosition;

        /// <summary>
        /// Equivalente a iNumBone.
        /// </summary>
        public int BoneNumber;

        /// <summary>
        /// Equivalente a bRepeatedly.
        /// </summary>
        public bool Repeatedly;

        /// <summary>
        /// Equivalente a fRepeatedlyHeight.
        /// </summary>
        public float RepeatedlyHeight;

        /// <summary>
        /// Inicialización común realizada por CreateParticle()
        /// antes del switch Type/SubType del Main.
        /// </summary>
        public void Initialize(
            int type,
            Vector3 position,
            Vector3 angle,
            Vector3 light,
            int subType,
            float scale,
            ClassicFxOwner target)
        {
            Type =
                type;

            // Main:
            //
            // o->TexType = Type;
            TexType =
                type;

            SubType =
                subType;

            Scale =
                scale;

            Position =
                position;

            StartPosition =
                position;

            Angle =
                angle;

            Light =
                light;

            // El constructor genérico del Main no fuerza Alpha.
            //
            // Nuestro storage se limpia al liberar slots,
            // por lo que el estado inicial equivalente es cero.
            Alpha =
                0f;

            // Main:
            //
            // o->LifeTime = 2;
            LifeTime =
                2f;

            Target =
                target;

            // Main:
            //
            // o->Rotation = 0.f;
            Rotation =
                0f;

            // Main:
            //
            // o->Frame = 0;
            Frame =
                0;

            // Main:
            //
            // o->bEnableMove = true;
            EnableMove =
                true;

            // Main:
            //
            // o->Gravity = 0.f;
            Gravity =
                0f;

            // Main:
            //
            // Vector(0.f, 0.f, 0.f, o->Velocity);
            Velocity =
                Vector3.Zero;

            TurningForce =
                Vector3.Zero;

            BoneNumber =
                0;

            Repeatedly =
                false;

            RepeatedlyHeight =
                0f;
        }

        public void Clear()
        {
            this =
                default;
        }
    }
}