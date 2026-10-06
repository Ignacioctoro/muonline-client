#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Classic Dark Lord - Increase Critical Damage
    /// activation effect.
    ///
    /// Classic Main:
    ///
    ///     AT_SKILL_ADD_CRITICAL
    ///
    ///     Light =
    ///         (1.0, 0.6, 0.3)
    ///
    ///     MODEL_DARKLORD_SKILL
    ///
    ///     Data/Skill/DarkLordSkill.bmd
    ///
    /// Initial classic values:
    ///
    ///     LifeTime = 10
    ///     Scale    = 0.2
    ///     Velocity = 0.1
    ///
    /// Every classic frame:
    ///
    ///     Scale += Velocity
    ///     Velocity += 0.02
    ///
    /// When LifeTime < 7:
    ///
    ///     BlendMeshLight /= 1.8
    ///
    /// Reference rate:
    ///
    ///     25 FPS
    ///
    /// Total duration:
    ///
    ///     10 / 25 = 0.4 seconds
    ///
    /// MonoGame adaptation:
    ///
    /// The original client obtains the cast position from the
    /// equipped weapon LinkBone.
    ///
    /// For this client we deliberately anchor the effect directly
    /// to the player's left and right hand bones so both hands show
    /// the cast visual even when one hand has no equipped item.
    ///
    /// We preserve the original initial scale and growth curve,
    /// but clamp its maximum size because DarkLordSkill.bmd renders
    /// substantially larger in this MonoGame client than in the
    /// classic renderer.
    /// </summary>
    public sealed class DarkLordCriticalCastEffect :
        EffectObject
    {
        // =============================================================
        // CLASSIC TIMING
        // =============================================================

        private const float ClassicReferenceFps =
            25.0f;

        private const float ClassicLifeFrames =
            10.0f;

        private const float TotalDuration =
            ClassicLifeFrames /
            ClassicReferenceFps;

        // =============================================================
        // SOUND
        // =============================================================

        private const string SoundPath =
            "Sound/sDarkCritical.wav";

        // =============================================================
        // OWNER
        // =============================================================

        private readonly PlayerObject
            _caster;

        private float
            _elapsed;

        private bool
            _soundPlayed;

        // =============================================================
        // CONSTRUCTOR
        // =============================================================

        public DarkLordCriticalCastEffect(
            PlayerObject caster)
        {
            _caster =
                caster ??
                throw new ArgumentNullException(
                    nameof(caster));

            Interactive =
                false;

            //
            // The container itself stays at the world origin.
            //
            // Each child is positioned using the actual animated
            // world matrix of its corresponding hand.
            //
            Position =
                Vector3.Zero;

            Scale =
                1.0f;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -180f,
                        -180f,
                        -80f),
                    new Vector3(
                        180f,
                        180f,
                        240f));

            // =========================================================
            // LEFT HAND
            // =========================================================
            //
            // Do NOT depend on Weapon1.
            //
            AddHandEffect(
                isLeftHand: true,
                subType: 0);

            // =========================================================
            // RIGHT HAND
            // =========================================================
            //
            // Do NOT depend on Weapon2.
            //
            AddHandEffect(
                isLeftHand: false,
                subType: 1);
        }

        // =============================================================
        // CREATE HAND EFFECT
        // =============================================================

        private void AddHandEffect(
            bool isLeftHand,
            int subType)
        {
            Children.Add(
                new DarkLordCriticalHandEffect(
                    _caster,
                    isLeftHand,
                    subType));
        }

        // =============================================================
        // UPDATE
        // =============================================================

        public override void Update(
            GameTime gameTime)
        {
            base.Update(
                gameTime);

            if (Status !=
                GameControlStatus.Ready)
            {
                return;
            }

            if (_caster.Status ==
                    GameControlStatus.Disposed ||
                _caster.World == null)
            {
                RemoveSelf();

                return;
            }

            if (!_soundPlayed)
            {
                PlayClassicSound();

                _soundPlayed =
                    true;
            }

            _elapsed +=
                (float)
                gameTime.ElapsedGameTime
                    .TotalSeconds;

            if (_elapsed >=
                TotalDuration)
            {
                RemoveSelf();
            }
        }

        // =============================================================
        // SOUND
        // =============================================================

        private void PlayClassicSound()
        {
            Vector3 sourcePosition =
                _caster.WorldPosition
                    .Translation;

            Vector3 listenerPosition =
                sourcePosition;

            //
            // Classic PlayObject() is spatial.
            //
            if (MuGame.Instance?.ActiveScene
                    is GameScene scene &&
                scene.Hero != null)
            {
                listenerPosition =
                    scene.Hero.WorldPosition
                        .Translation;
            }

            SoundController.Instance
                .PlayBufferWithAttenuation(
                    SoundPath,
                    sourcePosition,
                    listenerPosition,
                    maxDistance: 2500f);
        }

        // =============================================================
        // REMOVE
        // =============================================================

        private void RemoveSelf()
        {
            if (Parent != null)
            {
                Parent.Children.Remove(
                    this);

                Dispose();

                return;
            }

            if (World != null)
            {
                World.RemoveObject(
                    this);

                Dispose();

                return;
            }

            Dispose();
        }

        // =============================================================
        // HAND EFFECT
        // =============================================================

        /// <summary>
        /// One MODEL_DARKLORD_SKILL instance.
        ///
        /// subtype 0:
        ///     left hand
        ///
        /// subtype 1:
        ///     right hand
        ///
        /// The effect follows the actual hand bone for the whole
        /// 0.4 second activation phase.
        /// </summary>
        private sealed class DarkLordCriticalHandEffect :
            ModelObject
        {
            private const string ModelPath =
                "Skill/DarkLordSkill.bmd";

            // =========================================================
            // ORIGINAL CLASSIC VALUES
            // =============================================================

            private const float InitialScale =
                0.2f;

            private const float InitialVelocity =
                0.1f;

            private const float VelocityIncrease =
                0.02f;

            // =========================================================
            // MONOGAME SIZE LIMIT
            // =============================================================
            //
            // IMPORTANT:
            //
            // Do NOT multiply InitialScale by another compensation
            // value.
            //
            // The effect really starts at 0.2 in the classic client.
            //
            // We only stop the later growth before the model becomes
            // larger than the character.
            //
            private const float MaximumVisualScale =
                0.65f;

            private readonly PlayerObject
                _owner;

            private readonly bool
                _isLeftHand;

            private readonly int
                _subType;

            private float
                _elapsed;

            private static readonly Vector3
                ClassicLight =
                    new Vector3(
                        1.0f,
                        0.6f,
                        0.3f);

            // =========================================================
            // CONSTRUCTOR
            // =============================================================

            public DarkLordCriticalHandEffect(
                PlayerObject owner,
                bool isLeftHand,
                int subType)
            {
                _owner =
                    owner ??
                    throw new ArgumentNullException(
                        nameof(owner));

                _isLeftHand =
                    isLeftHand;

                _subType =
                    subType;

                //
                // Temporary position until the player's animated
                // skeleton is available.
                //
                Position =
                    _owner.WorldPosition
                        .Translation;

                UpdateHandAnchor();

                // =====================================================
                // ORIGINAL MODEL ANGLES
                // =====================================================
                //
                // subtype 0:
                //
                //     (45, 45, 0)
                //
                // subtype 1:
                //
                //     (45, -45, 0)
                //
                Angle =
                    new Vector3(
                        MathHelper.ToRadians(
                            45.0f),

                        MathHelper.ToRadians(
                            _subType == 0
                                ? 45.0f
                                : -45.0f),

                        0.0f);

                // =====================================================
                // ORIGINAL INITIAL SCALE
                // =====================================================
                //
                // Do not shrink this value.
                //
                Scale =
                    InitialScale;

                // =====================================================
                // CLASSIC LIGHT
                // =============================================================

                LightEnabled =
                    false;

                Light =
                    ClassicLight;

                UseSunLight =
                    false;

                RenderShadow =
                    false;

                IsTransparent =
                    true;

                AffectedByTransparency =
                    true;

                // =====================================================
                // RENDERING
                // =============================================================

                BlendState =
                    BlendState.Additive;

                BlendMeshState =
                    BlendState.Additive;

                //
                // Renderer-space intensity only.
                //
                // Scale is no longer compensated here.
                //
                BlendMeshLight =
                    0.70f;

                //
                // Keep Alpha fixed.
                //
                // The classic fade is handled through BlendMeshLight.
                //
                Alpha =
                    0.48f;

                DepthState =
                    GraphicsManager.ReadOnlyDepth;

                ContinuousAnimation =
                    false;
            }

            /// <summary>
            /// This is a classic effect model and should not use the
            /// modern dynamic lighting shader.
            /// </summary>
            protected override bool
                AllowDynamicLightingShader =>
                    false;

            // =========================================================
            // HAND ANCHOR
            // =============================================================

            private void UpdateHandAnchor()
            {
                if (_owner.TryGetHandWorldMatrix(
                        _isLeftHand,
                        out Matrix handWorld))
                {
                    Position =
                        handWorld.Translation;

                    return;
                }

                //
                // Fallback only for the very short period before the
                // skeleton matrices are available.
                //
                Position =
                    _owner.WorldPosition
                        .Translation +
                    new Vector3(
                        _isLeftHand
                            ? -12.0f
                            : 12.0f,
                        0.0f,
                        80.0f);
            }

            // =========================================================
            // MODEL
            // =============================================================

            public override async Task Load()
            {
                Model =
                    await BMDLoader.Instance
                        .Prepare(
                            ModelPath);

                await base.Load();
            }

            // =========================================================
            // UPDATE
            // =============================================================

            public override void Update(
                GameTime gameTime)
            {
                base.Update(
                    gameTime);

                if (Status !=
                    GameControlStatus.Ready)
                {
                    return;
                }

                if (_owner.Status ==
                        GameControlStatus.Disposed ||
                    _owner.World == null)
                {
                    Hidden =
                        true;

                    return;
                }

                // =====================================================
                // FOLLOW ACTUAL HAND
                // =============================================================

                UpdateHandAnchor();

                Hidden =
                    _owner.Hidden ||
                    _owner.IsDead ||
                    _owner.Status !=
                        GameControlStatus.Ready;

                if (Hidden)
                {
                    return;
                }

                _elapsed +=
                    (float)
                    gameTime.ElapsedGameTime
                        .TotalSeconds;

                float classicFrame =
                    MathHelper.Clamp(
                        _elapsed *
                        ClassicReferenceFps,
                        0.0f,
                        ClassicLifeFrames);

                // =====================================================
                // ORIGINAL SCALE / VELOCITY CURVE
                // =====================================================
                //
                // Classic:
                //
                // frame 0:
                //
                //     Scale    = 0.2
                //     Velocity = 0.1
                //
                // every frame:
                //
                //     Scale += Velocity
                //     Velocity += 0.02
                //
                // Closed form:
                //
                //     0.2
                //     +
                //     0.1*n
                //     +
                //     0.01*n*(n - 1)
                //
                float classicScale =
                    InitialScale +
                    InitialVelocity *
                    classicFrame +
                    (
                        VelocityIncrease *
                        0.5f *
                        classicFrame *
                        (
                            classicFrame -
                            1.0f
                        )
                    );

                classicScale =
                    MathF.Max(
                        InitialScale,
                        classicScale);

                //
                // Preserve the classic growth until it reaches the
                // maximum visually acceptable MonoGame size.
                //
                Scale =
                    MathF.Min(
                        MaximumVisualScale,
                        classicScale);

                // =====================================================
                // ORIGINAL FADE
                // =====================================================
                //
                // if (LifeTime < 7)
                //
                //     BlendMeshLight /= 1.8
                //
                // Life starts at 10, so this begins after roughly
                // the third classic frame.
                //
                float fadeFrames =
                    MathF.Max(
                        0.0f,
                        classicFrame -
                        3.0f);

                float fade =
                    MathF.Pow(
                        1.0f / 1.8f,
                        fadeFrames);

                //
                // IMPORTANT:
                //
                // Only fade BlendMeshLight.
                //
                // Do NOT also reduce Alpha here, otherwise the model
                // disappears just when it reaches its useful size.
                //
                BlendMeshLight =
                    0.70f *
                    fade;
            }
        }
    }
}