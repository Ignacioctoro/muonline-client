using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Wings
{
    /// <summary>
    /// Classic Wing of Ruin chrome-energy particles.
    ///
    /// This is a separate partial so the existing Ruin model renderer
    /// (msword01_r + flare01) does not need to be rewritten again.
    ///
    /// Original classic behavior:
    /// - texture: Effect/energy02.jpg  (BITMAP_CHROME_ENERGY2)
    /// - 38 bone sockets
    /// - 38 particles emitted per legacy frame
    /// - lifetime: 8 legacy frames
    /// - initial scale: baseScale * random(1.28 .. 1.91)
    /// - rotation: random start, +5 degrees per legacy frame
    /// - scale shrinks by 0.04 per legacy frame
    /// - texture is a 4-column atlas; one 1/4-width frame is rendered
    /// - additive blending
    ///
    /// The short-lived stationary particles form the purple/electric
    /// cloud around the dark wing frames which was missing from the port.
    /// </summary>
    public partial class WingObject
    {
        private const string RuinChromeEnergyTexturePath =
            "Effect/energy02.OZJ";

        // The original effect was authored around the legacy 25 FPS tick.
        private const float RuinChromeLegacyFps =
            25.0f;

        private const float RuinChromeParticleLifetimeFrames =
            8.0f;

        // 38 particles per burst * ~8 active legacy frames = ~304 active.
        // Keep some margin for uneven modern frame timing.
        private const int RuinChromeParticleCapacity =
            384;

        private static readonly int[] RuinChromeEnergyBones =
        {
            // base scale 0.1
            7, 16, 25, 57, 48, 39,

            // base scale 0.3
            11, 22, 31, 63, 54, 40,
            10, 21, 30, 62, 53, 41,

            // base scale 0.5
            9, 20, 29, 61, 52, 42,
            8, 19, 28, 60, 51, 43,

            // base scale 0.7
            18, 27, 59, 50, 17, 26, 58, 49
        };

        private struct RuinChromeParticle
        {
            public bool Active;
            public Vector3 Position;
            public float LifeFrames;
            public float Scale;
            public float Rotation;
        }

        private readonly RuinChromeParticle[] _ruinChromeParticles =
            new RuinChromeParticle[RuinChromeParticleCapacity];

        private int _ruinChromeWriteIndex;

        private float _ruinChromeEmissionAccumulator;

        private bool _ruinChromeRunning;

        private Texture2D _ruinChromeEnergyTexture;

        private Task _ruinChromePrepareTask;

        private bool _ruinChromePrepareStarted;


        // ================================================================
        // NO EDIT REQUIRED IN WingObject.cs
        //
        // WingObject did not previously override DrawAfter().
        // We preserve the inherited behavior and append only Ruin's missing
        // chrome-energy particles here.
        // ================================================================

        public override void DrawAfter(
            GameTime gameTime)
        {
            base.DrawAfter(
                gameTime);

            // First draw the missing classic chrome-energy cloud.
            DrawRuinChromeEnergyEffects(
                gameTime);

            // Then restore Ruin mesh 1 on top.
            //
            // Mesh 1 uses msword01 and contains the dark metallic frame.
            // Our SpriteBatch chrome particles are rendered after the model,
            // so without this final pass the dense additive energy can wash
            // those black rods almost completely out.
            DrawRuinFrameForeground();
        }


        // ================================================================
        // RESOURCE
        // ================================================================

        private void EnsureRuinChromeEnergyTexture()
        {
            if (_ruinChromeEnergyTexture != null &&
                !_ruinChromeEnergyTexture.IsDisposed)
            {
                return;
            }

            if (!_ruinChromePrepareStarted)
            {
                _ruinChromePrepareStarted =
                    true;

                _ruinChromePrepareTask =
                    TextureLoader.Instance.Prepare(
                        RuinChromeEnergyTexturePath);

                return;
            }

            if (_ruinChromePrepareTask == null ||
                !_ruinChromePrepareTask.IsCompleted ||
                _ruinChromePrepareTask.IsFaulted ||
                _ruinChromePrepareTask.IsCanceled)
            {
                return;
            }

            _ruinChromeEnergyTexture =
                TextureLoader.Instance.GetTexture2D(
                    RuinChromeEnergyTexturePath);
        }


        // ================================================================
        // MAIN
        // ================================================================

        private void DrawRuinChromeEnergyEffects(
            GameTime gameTime)
        {
            if (!Visible ||
                !IsRuinWing ||
                Model == null ||
                BoneTransform == null)
            {
                ResetRuinChromeEnergyParticles();
                return;
            }

            EnsureRuinChromeEnergyTexture();

            if (_ruinChromeEnergyTexture == null ||
                _ruinChromeEnergyTexture.IsDisposed)
            {
                return;
            }

            float deltaSeconds =
                (float)
                gameTime
                    .ElapsedGameTime
                    .TotalSeconds;

            if (!float.IsFinite(deltaSeconds) ||
                deltaSeconds <= 0.0f)
            {
                deltaSeconds =
                    1.0f / 60.0f;
            }

            // Avoid a giant particle burst after a debugger pause / window stall.
            deltaSeconds =
                MathF.Min(
                    deltaSeconds,
                    0.12f);

            UpdateRuinChromeEnergyParticles(
                deltaSeconds);

            // Original:
            // Emissions(FPS_ANIMATION_FACTOR * 1.0)
            //
            // In practice this is one 38-particle burst per legacy frame.
            float emissionStep =
                1.0f /
                RuinChromeLegacyFps;

            if (!_ruinChromeRunning)
            {
                _ruinChromeRunning =
                    true;

                EmitRuinChromeEnergyBurst();
            }

            _ruinChromeEmissionAccumulator +=
                deltaSeconds;

            while (_ruinChromeEmissionAccumulator >=
                   emissionStep)
            {
                _ruinChromeEmissionAccumulator -=
                    emissionStep;

                EmitRuinChromeEnergyBurst();
            }

            DrawRuinChromeEnergyParticles();
        }


        // ================================================================
        // EMISSION
        // ================================================================

        private void EmitRuinChromeEnergyBurst()
        {
            for (int index = 0;
                index < RuinChromeEnergyBones.Length;
                index++)
            {
                int bone =
                    RuinChromeEnergyBones[index];

                if (!TryGetRuinBoneWorldPosition(
                        bone,
                        out Vector3 position))
                {
                    continue;
                }

                float classicBaseScale;

                if (index < 6)
                {
                    classicBaseScale =
                        0.1f;
                }
                else if (index < 18)
                {
                    classicBaseScale =
                        0.3f;
                }
                else if (index < 30)
                {
                    classicBaseScale =
                        0.5f;
                }
                else
                {
                    classicBaseScale =
                        0.7f;
                }

                // Original:
                //
                // Scale *
                // (WorldRandom() % 64 + 128) *
                // 0.01f
                //
                // => 1.28 .. 1.91 multiplier.
                float randomScale =
                    (
                        MuGame.Random.Next(
                            64)
                        +
                        128
                    )
                    *
                    0.01f;

                ref RuinChromeParticle particle =
                    ref _ruinChromeParticles[
                        _ruinChromeWriteIndex];

                particle.Active =
                    true;

                particle.Position =
                    position;

                particle.LifeFrames =
                    RuinChromeParticleLifetimeFrames;

                particle.Scale =
                    classicBaseScale *
                    randomScale;

                particle.Rotation =
                    MathHelper.ToRadians(
                        MuGame.Random.Next(
                            360));

                _ruinChromeWriteIndex++;

                if (_ruinChromeWriteIndex >=
                    _ruinChromeParticles.Length)
                {
                    _ruinChromeWriteIndex =
                        0;
                }
            }
        }


        // ================================================================
        // UPDATE
        // ================================================================

        private void UpdateRuinChromeEnergyParticles(
            float deltaSeconds)
        {
            float legacyFrames =
                deltaSeconds *
                RuinChromeLegacyFps;

            float scaleDecay =
                0.04f *
                legacyFrames;

            float rotationAdvance =
                MathHelper.ToRadians(
                    5.0f *
                    legacyFrames);

            for (int i = 0;
                i < _ruinChromeParticles.Length;
                i++)
            {
                ref RuinChromeParticle particle =
                    ref _ruinChromeParticles[i];

                if (!particle.Active)
                {
                    continue;
                }

                particle.LifeFrames -=
                    legacyFrames;

                particle.Scale -=
                    scaleDecay;

                particle.Rotation +=
                    rotationAdvance;

                if (particle.LifeFrames <= 0.0f ||
                    particle.Scale <= 0.0f)
                {
                    particle.Active =
                        false;
                }
            }
        }


        // ================================================================
        // DRAW
        // ================================================================

        private void DrawRuinChromeEnergyParticles()
        {
            SpriteBatch spriteBatch =
                GraphicsManager.Instance.Sprite;

            GraphicsDevice gd =
                GraphicsDevice;

            BlendState previousBlend =
                gd.BlendState;

            // Original CreateParticle color:
            //
            // Light = { 0.6, 0.4, 0.7 }
            Vector3 light =
                new Vector3(
                    0.6f,
                    0.4f,
                    0.7f);

            using (
                new SpriteBatchScope(
                    spriteBatch,
                    SpriteSortMode.Deferred,
                    StormBrightAdditive,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead,
                    RasterizerState.CullNone))
            {
                for (int i = 0;
                    i < _ruinChromeParticles.Length;
                    i++)
                {
                    ref RuinChromeParticle particle =
                        ref _ruinChromeParticles[i];

                    if (!particle.Active)
                    {
                        continue;
                    }

                    // Original:
                    //
                    // Frame =
                    //     (23 - LifeTime) / 6
                    //
                    // Render uses:
                    //     (int)Frame % 4
                    //
                    // With the 8-frame Ruin lifetime this mainly shows
                    // atlas columns 2 and 3, exactly like the classic client.
                    int frame =
                        (int)(
                            (
                                23.0f -
                                particle.LifeFrames
                            )
                            /
                            6.0f);

                    frame =
                        Math.Clamp(
                            frame,
                            0,
                            3);

                    DrawRuinChromeEnergyBillboard(
                        spriteBatch,
                        _ruinChromeEnergyTexture,
                        particle.Position,
                        particle.Scale,
                        light,
                        particle.Rotation,
                        frame);
                }
            }

            gd.BlendState =
                previousBlend;
        }


        // ================================================================
        // CLASSIC PARTICLE BILLBOARD
        //
        // energy02 is a 4-column atlas. The classic client draws:
        //
        //     width  = texture.Width  * Scale * 0.25
        //     height = texture.Height * Scale
        //
        // and selects one quarter of the U range.
        // ================================================================

        private void DrawRuinChromeEnergyBillboard(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector3 worldPosition,
            float classicScale,
            Vector3 light,
            float rotation,
            int frame)
        {
            if (texture == null ||
                texture.IsDisposed ||
                classicScale <= 0.0f)
            {
                return;
            }

            Matrix view =
                Camera.Instance.View;

            Matrix projection =
                Camera.Instance.Projection;

            Vector3 cameraPosition =
                Vector3.Transform(
                    worldPosition,
                    view);

            float cameraDepth =
                -cameraPosition.Z;

            if (cameraDepth <=
                0.001f)
            {
                return;
            }

            Viewport viewport =
                GraphicsDevice.Viewport;

            Vector3 projected =
                viewport.Project(
                    worldPosition,
                    projection,
                    view,
                    Matrix.Identity);

            if (projected.Z < 0.0f ||
                projected.Z > 1.0f)
            {
                return;
            }

            float pixelsPerCameraUnit =
                viewport.Height
                *
                MathF.Abs(
                    projection.M22)
                /
                (
                    2.0f
                    *
                    cameraDepth
                );

            float spriteScale =
                classicScale
                *
                pixelsPerCameraUnit;

            if (!float.IsFinite(
                    spriteScale) ||
                spriteScale <= 0.0f)
            {
                return;
            }

            int frameWidth =
                Math.Max(
                    1,
                    texture.Width /
                    4);

            frame =
                Math.Clamp(
                    frame,
                    0,
                    3);

            Rectangle source =
                new Rectangle(
                    frame *
                    frameWidth,
                    0,
                    frameWidth,
                    texture.Height);

            Vector3 clampedLight =
                Vector3.Clamp(
                    light,
                    Vector3.Zero,
                    Vector3.One);

            Color color =
                new Color(
                    clampedLight)
                *
                TotalAlpha;

            Vector2 origin =
                new Vector2(
                    source.Width *
                    0.5f,
                    source.Height *
                    0.5f);

            spriteBatch.Draw(
                texture,
                new Vector2(
                    projected.X,
                    projected.Y),
                source,
                color,
                rotation,
                origin,
                spriteScale,
                SpriteEffects.None,
                MathHelper.Clamp(
                    projected.Z + 0.00015f,
                    0.0f,
                    1.0f));
        }


        // ================================================================
        // FINAL RUIN FRAME PASS
        //
        // The original mesh 1 texture (msword01) contains the dark frame.
        // Re-draw it after the chrome-energy particles so the electric haze
        // surrounds the structure instead of erasing it visually.
        // ================================================================

        private void DrawRuinFrameForeground()
        {
            if (!Visible ||
                !IsRuinWing ||
                Model?.Meshes == null ||
                Model.Meshes.Length < 2)
            {
                return;
            }

            EnsureStormEffect();

            if (_stormEffect == null)
            {
                return;
            }

            GraphicsDevice gd =
                GraphicsDevice;

            BlendState previousBlend =
                gd.BlendState;

            DepthStencilState previousDepth =
                gd.DepthStencilState;

            RasterizerState previousRasterizer =
                gd.RasterizerState;

            try
            {
                gd.RasterizerState =
                    RasterizerState.CullNone;

                gd.DepthStencilState =
                    GraphicsManager.ReadOnlyDepth;

                gd.BlendState =
                    BlendState.AlphaBlend;

                DrawRuinMesh(
                    1,
                    Vector3.One,
                    null);
            }
            finally
            {
                gd.BlendState =
                    previousBlend;

                gd.DepthStencilState =
                    previousDepth;

                gd.RasterizerState =
                    previousRasterizer;
            }
        }


        // ================================================================
        // RESET
        // ================================================================

        private void ResetRuinChromeEnergyParticles()
        {
            if (!_ruinChromeRunning)
            {
                return;
            }

            _ruinChromeRunning =
                false;

            _ruinChromeEmissionAccumulator =
                0.0f;

            _ruinChromeWriteIndex =
                0;

            for (int i = 0;
                i < _ruinChromeParticles.Length;
                i++)
            {
                _ruinChromeParticles[i].Active =
                    false;
            }
        }
    }
}
