#nullable enable

using Client.Main.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Short-lived 3D lightning bolt used around Fenrir's body.
    ///
    /// Original MU resource:
    /// Data/Effect/lightning_type01.bmd
    /// Data/Effect/eff_lighting.ozj
    /// </summary>
    public sealed class FenrirThunderEffect : ModelObject
    {
        // The original bolt is extremely short-lived.
        private const float LifeTime = 0.18f;

        private float _life;

        public bool IsActive => !Hidden;

        public override bool IsStaticForCaching => false;

        public FenrirThunderEffect()
        {
            RenderShadow = false;

            IsTransparent = true;
            AffectedByTransparency = true;

            // Every mesh of lightning_type01 is treated as an
            // additive effect.
            BlendMesh = -2;
            BlendState = BlendState.Additive;
            BlendMeshState = BlendState.Additive;
            BlendMeshLight = 1.0f;

            DepthState = DepthStencilState.DepthRead;

            LightEnabled = false;
            Light = Vector3.One;

            Color = Color.White;

            ContinuousAnimation = false;

            Scale = 0.3f;
            Alpha = 0f;

            Hidden = true;
        }

        public override async Task Load()
        {
            // The BMD lives in Data/Effect but its texture also
            // has to be resolved against Data/Effect.
            Model = await BMDLoader.Instance.Prepare(
                "Effect/lightning_type01.bmd",
                "Effect");

            await base.Load();
        }

        /// <summary>
        /// Reuses this effect instance for a new lightning bolt.
        ///
        /// localPosition is relative to the Fenrir because this
        /// object is a child of VehicleObject.
        /// </summary>
        public void Trigger(
            Vector3 localPosition,
            Vector3 lightColor,
            float scale)
        {
            _life = 0f;

            Position = localPosition;

            Scale = MathF.Max(
                0.08f,
                scale);

            //
            // Original MU gives every bolt a completely
            // random orientation.
            //
            Angle = new Vector3(
                Random.Shared.NextSingle() * MathHelper.TwoPi,
                Random.Shared.NextSingle() * MathHelper.TwoPi,
                Random.Shared.NextSingle() * MathHelper.TwoPi);

            Light = lightColor;
            Color = Color.White;

            //
            // Original MODEL_FENRIR_THUNDER starts around
            // Alpha = 0.7.
            //
            Alpha = 0.7f;

            Hidden = false;

            //
            // Light changes dynamically depending on the
            // Fenrir colour, so force the model buffers to
            // refresh their material/light information.
            //
            InvalidateBuffers(
                BufferFlagLighting |
                BufferFlagMaterial |
                BufferFlagTransform);
        }

        public void Stop()
        {
            Alpha = 0f;
            Hidden = true;
            _life = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            if (Hidden)
                return;

            base.Update(gameTime);

            float dt =
                (float)gameTime.ElapsedGameTime.TotalSeconds;

            _life += dt;

            float progress =
                _life / LifeTime;

            if (progress >= 1f)
            {
                Stop();
                return;
            }

            //
            // Original behaviour:
            //
            // 0.7 -> 1.0 -> 0.0
            //
            if (progress < 0.25f)
            {
                float fadeIn =
                    progress / 0.25f;

                Alpha = MathHelper.Lerp(
                    0.7f,
                    1.0f,
                    fadeIn);
            }
            else
            {
                float fadeOut =
                    (progress - 0.25f) / 0.75f;

                Alpha = MathHelper.Lerp(
                    1.0f,
                    0.0f,
                    fadeOut);
            }

            //
            // MODEL_FENRIR_THUNDER also rotates slightly
            // while alive.
            //
            float rotation =
                MathHelper.ToRadians(3.75f) * dt;

            Angle += new Vector3(
                rotation,
                rotation,
                rotation);
        }
    }
}