using Client.Main.ClassicFX.Primitives;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        /// <summary>
        /// Equivalente a:
        ///
        ///     OBJECT Sprites[MAX_SPRITES];
        ///
        /// Es un array fijo.
        /// No crece.
        /// No genera objetos por sprite.
        /// </summary>
        private readonly ClassicSprite[]
            _sprites =
                new ClassicSprite[
                    ClassicFxPools.MaxSprites
                ];

        public int ActiveSpriteCount =>
            Pools
                .Sprites
                .ActiveCount;

        /// <summary>
        /// Equivalente semántico a CreateSprite() del Main.
        ///
        /// No renderiza.
        /// Solo ocupa e inicializa un slot.
        /// </summary>
        public ClassicFxHandle CreateSprite(
            int type,
            Vector3 position,
            float scale,
            Vector3 light,
            ClassicFxOwner owner,
            float rotation = 0f,
            int subType = 0)
        {
            if (_disposed ||
                !Enabled)
            {
                return
                    ClassicFxHandle.Invalid;
            }

            if (!Pools
                    .Sprites
                    .TryAcquire(
                        out ClassicFxHandle handle))
            {
                return
                    ClassicFxHandle.Invalid;
            }

            ref ClassicSprite sprite =
                ref _sprites[
                    handle.Index
                ];

            sprite.Initialize(
                type,
                subType,
                position,
                scale,
                light,
                owner,
                rotation);

            return
                handle;
        }

        /// <summary>
        /// Overload para sprites sin Owner.
        /// </summary>
        public ClassicFxHandle CreateSprite(
            int type,
            Vector3 position,
            float scale,
            Vector3 light,
            float rotation = 0f,
            int subType = 0)
        {
            return CreateSprite(
                type,
                position,
                scale,
                light,
                ClassicFxOwner.None,
                rotation,
                subType);
        }

        /// <summary>
        /// Equivalente a CheckSprites().
        ///
        /// El Main recorre los sprites Live y marca Visible=true
        /// antes del render pass.
        /// </summary>
        public void CheckSprites()
        {
            if (_disposed ||
                !Enabled)
            {
                return;
            }

            for (int i = 0;
                 i < ClassicFxPools.MaxSprites;
                 i++)
            {
                if (!Pools
                        .Sprites
                        .IsActive(
                            i))
                {
                    continue;
                }

                _sprites[i]
                    .Visible =
                        true;
            }
        }

        /// <summary>
        /// Implementa únicamente la lógica de AnimationFrame que
        /// actualmente existe en zzzeffectsprite.cpp.
        ///
        /// Aún NO dibuja.
        /// </summary>
        private float UpdateSpriteAnimationFrame(
            float currentFrame,
            bool isVisible)
        {
            const float frameMin =
                0.2f;

            const float frameMax =
                1.0f;

            const float frameStep =
                0.1f;

            float delta =
                frameStep *
                Clock.FrameFactor;

            currentFrame +=
                isVisible
                    ? delta
                    : -delta;

            return MathHelper.Clamp(
                currentFrame,
                frameMin,
                frameMax);
        }

        /// <summary>
        /// Valida y obtiene una copia de un Sprite.
        ///
        /// Pensado principalmente para debugging/tests.
        /// </summary>
        public bool TryGetSprite(
            ClassicFxHandle handle,
            out ClassicSprite sprite)
        {
            if (handle.Kind !=
                    ClassicFxPoolKind.Sprite ||
                !Pools
                    .Sprites
                    .IsAlive(
                        handle))
            {
                sprite =
                    default;

                return false;
            }

            sprite =
                _sprites[
                    handle.Index
                ];

            return true;
        }

        /// <summary>
        /// Fast path interno para el futuro renderer.
        /// </summary>
        private ref ClassicSprite GetSpriteRef(
            int index)
        {
            return ref
                _sprites[
                    index
                ];
        }

        /// <summary>
        /// Libera explícitamente un sprite.
        ///
        /// RenderSprites utilizará este método después de dibujar
        /// los sprites efímeros, igual que el Main pone Live=false.
        /// </summary>
        public bool ReleaseSprite(
            ClassicFxHandle handle)
        {
            if (handle.Kind !=
                    ClassicFxPoolKind.Sprite ||
                !Pools
                    .Sprites
                    .IsAlive(
                        handle))
            {
                return false;
            }

            _sprites[
                handle.Index
            ].Clear();

            return Pools
                .Sprites
                .Release(
                    handle);
        }

        /// <summary>
        /// Variante interna cuando el renderer ya está recorriendo
        /// el array por índice.
        /// </summary>
        private void ReleaseSpriteAt(
            int index)
        {
            ClassicFxHandle handle =
                Pools
                    .Sprites
                    .GetHandle(
                        index);

            if (!handle.IsValid)
            {
                return;
            }

            _sprites[
                index
            ].Clear();

            Pools
                .Sprites
                .Release(
                    handle);
        }
        /// <summary>
        /// Equivalente inicial de RenderSprites().
        ///
        /// renderPass:
        ///
        /// 0 = pass normal
        /// 1 = primera pasada de water map
        /// 2 = segunda pasada de water map
        ///
        /// Conservamos ya la lógica original aunque por ahora
        /// el cliente MonoGame utilice únicamente pass 0.
        /// </summary>
        public void RenderSprites(
            byte renderPass = 0)
        {
            if (_disposed ||
                !Enabled ||
                _spriteRenderer == null)
            {
                return;
            }

            CheckSprites();

            _spriteRenderer.Begin();

            for (int i = 0;
                i < ClassicFxPools.MaxSprites;
                i++)
            {
                if (!Pools
                        .Sprites
                        .IsActive(
                            i))
                {
                    continue;
                }

                ref ClassicSprite sprite =
                    ref GetSpriteRef(
                        i);

                // Main:
                //
                // byRenderOneMore == 1
                //
                // if Position.Z > 350:
                //     continue
                //
                if (renderPass == 1)
                {
                    if (sprite.Position.Z >
                        350f)
                    {
                        continue;
                    }
                }
                else if (renderPass == 2)
                {
                    // Main:
                    //
                    // if Position.Z <= 300:
                    //     Live = false
                    //     continue
                    //
                    if (sprite.Position.Z <=
                        300f)
                    {
                        ReleaseSpriteAt(
                            i);

                        continue;
                    }
                }

                // RenderSprite(OBJECT*)
                //
                // AnimationFrame se actualiza en el render,
                // no en MoveEffects.
                sprite.AnimationFrame =
                    UpdateSpriteAnimationFrame(
                        sprite.AnimationFrame,
                        sprite.Visible);

                _spriteRenderer
                    .QueueSprite(
                        sprite);

                // Main:
                //
                // if (byRenderOneMore == 0 ||
                //     byRenderOneMore == 2)
                //
                //     Live = false;
                //
                if (renderPass == 0 ||
                    renderPass == 2)
                {
                    ReleaseSpriteAt(
                        i);
                }
            }

            _spriteRenderer.End();
        }

        private void ClearSpriteStorage()
        {
            System.Array.Clear(
                _sprites);
        }
    }
}