using Client.Main.ClassicFX.Core;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Primitives
{
    /// <summary>
    /// Representación compacta del OBJECT utilizado por
    /// OBJECT Sprites[MAX_SPRITES] en el Main.
    ///
    /// Solo contiene los campos utilizados realmente por el
    /// sistema Sprite.
    ///
    /// Live NO se duplica aquí:
    /// ClassicFxSlotPool es nuestra fuente de verdad para Live.
    /// </summary>
    public struct ClassicSprite
    {
        /// <summary>
        /// Bitmap/texture type clásico.
        /// Ej:
        ///
        /// BITMAP_FLARE
        /// BITMAP_LIGHT
        /// BITMAP_FORMATION_MARK
        /// </summary>
        public int Type;

        public int SubType;

        /// <summary>
        /// Equivalente a OBJECT* Owner.
        /// </summary>
        public ClassicFxOwner Owner;

        /// <summary>
        /// Equivalente a OBJECT::AnimationFrame.
        ///
        /// CreateSprite del Main comienza en 1.0.
        /// </summary>
        public float AnimationFrame;

        public float Scale;

        /// <summary>
        /// El Main guarda Rotation en:
        ///
        ///     o->Angle[2]
        ///
        /// Para Sprite no necesitamos almacenar los otros
        /// componentes de Angle.
        /// </summary>
        public float Rotation;

        public Vector3 Position;

        public Vector3 StartPosition;

        public Vector3 Light;

        /// <summary>
        /// Equivalente a OBJECT::Visible.
        ///
        /// CheckSprites() lo marca true antes del pass
        /// correspondiente.
        /// </summary>
        public bool Visible;

        public void Initialize(
            int type,
            int subType,
            Vector3 position,
            float scale,
            Vector3 light,
            ClassicFxOwner owner,
            float rotation)
        {
            Type =
                type;

            SubType =
                subType;

            Owner =
                owner;

            AnimationFrame =
                1f;

            Scale =
                scale;

            Rotation =
                rotation;

            Position =
                position;

            StartPosition =
                position;

            Light =
                light;

            // Igual que el flujo clásico:
            // CheckSprites determinará la visibilidad
            // inmediatamente antes del render.
            Visible =
                false;
        }

        public void Clear()
        {
            this =
                default;
        }
    }
}