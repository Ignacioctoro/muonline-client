#nullable enable

using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Classic Dark Lord Increase Critical Damage cast effect.
    ///
    /// Original Main:
    ///
    /// AT_SKILL_ADD_CRITICAL
    ///
    ///     Light =
    ///         (1.0, 0.6, 0.3)
    ///
    ///     Weapon[0]:
    ///
    ///         CreateEffect(
    ///             MODEL_DARKLORD_SKILL,
    ///             weaponPosition,
    ///             ...,
    ///             Light,
    ///             0);
    ///
    ///     Weapon[1]:
    ///
    ///         CreateEffect(
    ///             MODEL_DARKLORD_SKILL,
    ///             weaponPosition,
    ///             ...,
    ///             Light,
    ///             1);
    ///
    ///     PlayObject(
    ///         SOUND_CRITICAL,
    ///         owner);
    ///
    /// MODEL_DARKLORD_SKILL:
    ///
    ///     Data/Skill/DarkLordSkill.bmd
    ///
    /// Initial:
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
    /// Reference effect rate:
    ///
    ///     25 FPS
    ///
    /// Total duration:
    ///
    ///     10 / 25 = 0.4 seconds
    /// </summary>
    public sealed class DarkLordCriticalCastEffect :
        EffectObject
    {
        private const float ClassicReferenceFps =
            25.0f;

        private const float ClassicLifeFrames =
            10.0f;

        private const float TotalDuration =
            ClassicLifeFrames /
            ClassicReferenceFps;

        private const string SoundPath =
            "Sound/sDarkCritical.wav";

        private readonly PlayerObject
            _caster;

        private float
            _elapsed;

        private bool
            _soundPlayed;

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
            // El contenedor vive en coordenadas world.
            // Los dos modelos hijos reciben posiciones
            // absolutas capturadas en el momento del cast.
            //
            Position =
                Vector3.Zero;

            Scale =
                1.0f;

            BoundingBoxLocal =
                new BoundingBox(
                    new Vector3(
                        -300f,
                        -300f,
                        -100f),
                    new Vector3(
                        300f,
                        300f,
                        300f));

            // =========================================================
            // WEAPON 0 / LEFT HAND
            // =========================================================

            if (ShouldCreateLeftHandEffect())
            {
                AddHandEffect(
                    isLeftHand: true,
                    subType: 0);
            }

            // =========================================================
            // WEAPON 1 / RIGHT HAND
            // =========================================================

            if (ShouldCreateRightHandEffect())
            {
                AddHandEffect(
                    isLeftHand: false,
                    subType: 1);
            }
        }

        // =============================================================
        // CLASSIC WEAPON FILTERING
        // =============================================================

        /// <summary>
        /// Original:
        ///
        /// Weapon[0].Type != -1
        /// &&
        /// Weapon[0].Type != MODEL_BOW + 15
        ///
        /// Nuestro Weapon1 corresponde al slot Left Hand.
        /// </summary>
        private bool ShouldCreateLeftHandEffect()
        {
            WeaponObject? weapon =
                _caster.Weapon1;

            if (weapon == null ||
                weapon.Model == null)
            {
                return false;
            }

            //
            // Original exception:
            //
            // MODEL_BOW + 15
            //
            if (weapon.ItemGroup == 4 &&
                weapon.ItemNumber == 15)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Original:
        ///
        /// Weapon[1].Type != -1
        ///
        /// excluding:
        ///
        /// MODEL_BOW + 7
        /// shields
        ///
        /// Nuestro Weapon2 corresponde al slot Right Hand.
        /// </summary>
        private bool ShouldCreateRightHandEffect()
        {
            WeaponObject? weapon =
                _caster.Weapon2;

            if (weapon == null ||
                weapon.Model == null)
            {
                return false;
            }

            //
            // Shields do not receive the effect.
            //
            if (weapon.ItemGroup == 6)
            {
                return false;
            }

            //
            // Original exception:
            //
            // MODEL_BOW + 7
            //
            if (weapon.ItemGroup == 4 &&
                weapon.ItemNumber == 7)
            {
                return false;
            }

            return true;
        }

        // =============================================================
        // CREATE HAND EFFECT
        // =============================================================

        private void AddHandEffect(
            bool isLeftHand,
            int subType)
        {
            Vector3 effectPosition;

            //
            // Original:
            //
            // TransformPosition(
            //     o->BoneTransform[
            //         Weapon.LinkBone],
            //     Vector3.Zero,
            //     Position)
            //
            // En nuestro player los weapon links
            // terminan en los hand bones 33 / 42.
            //
            if (_caster.TryGetHandWorldMatrix(
                    isLeftHand,
                    out Matrix handMatrix))
            {
                effectPosition =
                    handMatrix.Translation;
            }
            else
            {
                //
                // Safety fallback.
                //
                // Normalmente no debería ejecutarse.
                //
                effectPosition =
                    _caster.WorldPosition
                        .Translation +
                    new Vector3(
                        0f,
                        0f,
                        80f);
            }

            Children.Add(
                new DarkLordCriticalHandEffect(
                    effectPosition,
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
            // PlayObject() del Main original es espacial.
            //
            // Para nuestro propio personaje la distancia será 0.
            // Para otro DL se atenuará según su distancia al hero.
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
        // ORIGINAL MODEL INSTANCE
        // =============================================================

        /// <summary>
        /// One MODEL_DARKLORD_SKILL instance.
        ///
        /// There can be:
        ///
        ///     subtype 0 = first/left weapon
        ///     subtype 1 = second/right weapon
        ///
        /// The original model is intentionally NOT kept attached to
        /// the moving hand after creation.
        ///
        /// Main calculates the weapon bone position once and creates
        /// a standalone effect there.
        /// </summary>
        private sealed class DarkLordCriticalHandEffect :
            ModelObject
        {
            private const string ModelPath =
                "Skill/DarkLordSkill.bmd";

            private const float InitialScale =
                0.2f;

            private const float InitialVelocity =
                0.1f;

            private const float VelocityIncrease =
                0.02f;

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

            public DarkLordCriticalHandEffect(
                Vector3 position,
                int subType)
            {
                _subType =
                    subType;

                Position =
                    position;

                //
                // Original MODEL_DARKLORD_SKILL:
                //
                // subtype 0:
                //     Angle = (45, 45, 0)
                //
                // subtype 1:
                //     Angle = (45, -45, 0)
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

                Scale =
                    InitialScale;

                //
                // Original:
                //
                // VectorCopy(
                //     o->Light,
                //     b->BodyLight);
                //
                // We disable terrain-light contribution so
                // this exact warm color becomes our model light.
                //
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

                //
                // RENDER_TEXTURE in the original.
                //
                // Individual bright/blend meshes are still
                // detected through the BMD/texture scripts.
                //
                BlendState =
                    BlendState.Additive;

                BlendMeshState =
                    BlendState.Additive;

                BlendMeshLight =
                    0.70f;

                Alpha =
                    0.48f;

                DepthState =
                    GraphicsManager.ReadOnlyDepth;

                DepthState =
                    GraphicsManager.ReadOnlyDepth;

                ContinuousAnimation =
                    false;
            }

            /// <summary>
            /// This is an old-school effect model.
            /// Don't send it through the modern dynamic-light shader.
            /// </summary>
            protected override bool
                AllowDynamicLightingShader =>
                    false;

            // =========================================================
            // MODEL
            // =========================================================

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
            // =========================================================

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
                // ORIGINAL SCALE / VELOCITY
                // =====================================================
                //
                // frame 0:
                //
                //     Scale    = 0.2
                //     Velocity = 0.1
                //
                // each frame:
                //
                //     Scale += Velocity
                //     Velocity += 0.02
                //
                // Closed-form equivalent:
                //
                // S(n) =
                //
                //     0.2
                //     +
                //     0.1*n
                //     +
                //     0.01*n*(n-1)
                //
                // Using fractional classicFrame gives the exact
                // progression without tying us to physical FPS.
                //
                float scale =
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

                Scale =
                    MathF.Max(
                        InitialScale,
                        scale);

                // =====================================================
                // ORIGINAL FADE
                // =====================================================
                //
                // if (LifeTime < 7)
                //
                //     BlendMeshLight /= 1.8;
                //
                // Life starts at 10, so the fade begins after
                // approximately the third classic frame.
                //
                float fadeFrames =
                    MathF.Max(
                        0.0f,
                        classicFrame -
                        3.0f);

                BlendMeshLight =
                    MathF.Pow(
                        1.0f / 1.8f,
                        fadeFrames);
            }
        }
    }
}