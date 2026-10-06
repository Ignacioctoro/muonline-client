using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.Controllers;
using Client.Main.Content;
using Client.Main.Controls;
using Client.Main.Graphics;
using Client.Main.Helpers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Centralized persistent visual manager for classic MU buffs.
    ///
    /// This replaces the old ElfBuffEffectManager implementation.
    ///
    /// Buff state comes from MagicEffectStatus (0x07). Visuals are then
    /// composed according to the original client behavior.
    /// </summary>
    public sealed class BuffVisualManager
    {
        public static BuffVisualManager Instance
        {
            get;
        } = new BuffVisualManager();

        private sealed class PlayerBuffVisualState
        {
            public HashSet<byte> ActiveEffects
            {
                get;
            } = new();

            public ClassicSpearJointEffect GreenJoint;

            public ClassicSpearJointEffect SoulBarrierJoint;

            public ClassicWeaponBuffShineEffect AttackShine;
            public ClassicGreaterFortitudeGlowEffect GreaterFortitudeGlow;
            public ClassicCriticalDamageAuraEffect CriticalDamageAura;
        }

        private readonly Dictionary<
            ushort,
            PlayerBuffVisualState> _players =
                new();

        private BuffVisualManager()
        {
        }

        public void HandleBuff(
            byte effectId,
            ushort playerId,
            bool isActive)
        {
            // This first renderer only handles the first four
            // classic persistent aura definitions.
            //
            // Other effect IDs continue to live normally inside
            // CharacterState and will be implemented in this same
            // manager later.
            if (!Season6BuffMapping
                    .HasImplementedVisual(
                        effectId))
            {
                return;
            }

            ushort maskedId =
                (ushort)(
                    playerId &
                    0x7FFF);

            MuGame.ScheduleOnMainThread(
                () =>
                {
                    if (!_players.TryGetValue(
                            maskedId,
                            out PlayerBuffVisualState state))
                    {
                        state =
                            new PlayerBuffVisualState();

                        _players[maskedId] =
                            state;
                    }

                    if (isActive)
                    {
                        state.ActiveEffects.Add(
                            effectId);
                    }
                    else
                    {
                        state.ActiveEffects.Remove(
                            effectId);
                    }

                    RefreshPlayer(
                        maskedId,
                        state);

                    if (state.ActiveEffects.Count == 0)
                    {
                        DisposeVisuals(
                            state);

                        _players.Remove(
                            maskedId);
                    }
                });
        }

        /// <summary>
        /// Called when a remote PlayerObject is finally added to
        /// world.Objects after its scope packet already told us about
        /// active buffs.
        /// </summary>
        public void EnsureBuffsForPlayer(
            ushort playerId)
        {
            ushort maskedId =
                (ushort)(
                    playerId &
                    0x7FFF);

            MuGame.ScheduleOnMainThread(
                () =>
                {
                    if (_players.TryGetValue(
                            maskedId,
                            out PlayerBuffVisualState state))
                    {
                        RefreshPlayer(
                            maskedId,
                            state);
                    }
                });
        }

        private void RefreshPlayer(
            ushort playerId,
            PlayerBuffVisualState state)
        {
            if (MuGame.Instance?.ActiveScene
                is not GameScene gameScene)
            {
                DisposeVisuals(
                    state);

                return;
            }

            if (gameScene.World
                is not WalkableWorldControl world ||
                world.Status !=
                    GameControlStatus.Ready)
            {
                DisposeVisuals(
                    state);

                return;
            }

            PlayerObject target =
                world.FindPlayerById(
                    playerId);

            if (target == null &&
                gameScene.Hero != null &&
                (
                    gameScene.Hero.NetworkId &
                    0x7FFF
                ) == playerId)
            {
                target =
                    gameScene.Hero;
            }

            // The scope handler may receive the buff before the
            // PlayerObject has finished loading. Keep the active
            // state and wait for EnsureBuffsForPlayer().
            if (target == null)
            {
                DisposeVisuals(
                    state);

                return;
            }

            bool greaterDamage =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.GreaterDamage);

            bool greaterDefense =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.GreaterDefense);

            bool elfSoldier =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.ElfSoldierBuff);

            bool soulBarrier =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.SoulBarrier);
            bool greaterFortitude =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.GreaterFortitude);
            bool criticalDamage =
                state.ActiveEffects.Contains(
                    Season6BuffMapping.CriticalDamageIncrease) ||
                state.ActiveEffects.Contains(
                    Season6BuffMapping.CriticalDamageIncreaseMastery);

            // ---------------------------------------------------------
            // Classic SourceMain behavior:
            //
            // if (Attack || HelpNpc)
            // {
            //     weapon shine
            //     ensure SPEARSKILL subtype 4
            // }
            // else if (Defense)
            // {
            //     ensure SPEARSKILL subtype 4
            // }
            //
            // Therefore attack/defense/NPC helper must SHARE the same
            // green joint instead of rendering multiple copies.
            // ---------------------------------------------------------

            bool greenJointNeeded =
                greaterDamage ||
                greaterDefense ||
                elfSoldier;

            bool attackShineNeeded =
                greaterDamage ||
                elfSoldier;

            EnsureGreenJoint(
                world,
                target,
                state,
                greenJointNeeded);

            EnsureAttackShine(
                world,
                target,
                state,
                attackShineNeeded);

            EnsureSoulBarrier(
                world,
                target,
                state,
                soulBarrier);
            EnsureGreaterFortitude(
                world,
                target,
                state,
                greaterFortitude);

            EnsureCriticalDamageAura(
                world,
                target,
                state,
                criticalDamage);
        }

        private static void EnsureGreenJoint(
            WalkableWorldControl world,
            PlayerObject target,
            PlayerBuffVisualState state,
            bool needed)
        {
            if (!needed)
            {
                RemoveVisual(
                    ref state.GreenJoint);

                return;
            }

            if (IsUsable(
                    state.GreenJoint,
                    target))
            {
                return;
            }

            RemoveVisual(
                ref state.GreenJoint);

            state.GreenJoint =
                new ClassicSpearJointEffect(
                    target,
                    4);

            AddVisual(
                world,
                state.GreenJoint);
        }

        private static void EnsureSoulBarrier(
            WalkableWorldControl world,
            PlayerObject target,
            PlayerBuffVisualState state,
            bool needed)
        {
            if (!needed)
            {
                RemoveVisual(
                    ref state.SoulBarrierJoint);

                return;
            }

            if (IsUsable(
                    state.SoulBarrierJoint,
                    target))
            {
                return;
            }

            RemoveVisual(
                ref state.SoulBarrierJoint);

            state.SoulBarrierJoint =
                new ClassicSpearJointEffect(
                    target,
                    0);

            AddVisual(
                world,
                state.SoulBarrierJoint);
        }
        private static void EnsureGreaterFortitude(
            WalkableWorldControl world,
            PlayerObject target,
            PlayerBuffVisualState state,
            bool needed)
        {
            if (!needed)
            {
                RemoveVisual(
                    ref state.GreaterFortitudeGlow);

                return;
            }

            if (IsUsable(
                    state.GreaterFortitudeGlow,
                    target))
            {
                return;
            }

            RemoveVisual(
                ref state.GreaterFortitudeGlow);

            state.GreaterFortitudeGlow =
                new ClassicGreaterFortitudeGlowEffect(
                    target);

            AddVisual(
                world,
                state.GreaterFortitudeGlow);
        }
        private static void EnsureCriticalDamageAura(
            WalkableWorldControl world,
            PlayerObject target,
            PlayerBuffVisualState state,
            bool needed)
        {
            if (!needed)
            {
                RemoveVisual(
                    ref state.CriticalDamageAura);

                return;
            }

            if (IsUsable(
                    state.CriticalDamageAura,
                    target))
            {
                return;
            }

            RemoveVisual(
                ref state.CriticalDamageAura);

            state.CriticalDamageAura =
                new ClassicCriticalDamageAuraEffect(
                    target);

            AddVisual(
                world,
                state.CriticalDamageAura);
        }


        private static void EnsureAttackShine(
            WalkableWorldControl world,
            PlayerObject target,
            PlayerBuffVisualState state,
            bool needed)
        {
            if (!needed)
            {
                RemoveVisual(
                    ref state.AttackShine);

                return;
            }

            if (IsUsable(
                    state.AttackShine,
                    target))
            {
                return;
            }

            RemoveVisual(
                ref state.AttackShine);

            state.AttackShine =
                new ClassicWeaponBuffShineEffect(
                    target);

            AddVisual(
                world,
                state.AttackShine);
        }
        private static bool IsUsable(
            ClassicCriticalDamageAuraEffect effect,
            PlayerObject target)
        {
            return effect != null &&
                effect.Status !=
                    GameControlStatus.Disposed &&
                ReferenceEquals(
                    effect.Owner,
                    target);
        }

        private static bool IsUsable(
            ClassicSpearJointEffect effect,
            PlayerObject target)
        {
            return effect != null &&
                   effect.Status !=
                       GameControlStatus.Disposed &&
                   ReferenceEquals(
                       effect.Owner,
                       target);
        }
        private static bool IsUsable(
            ClassicGreaterFortitudeGlowEffect effect,
            PlayerObject target)
        {
            return effect != null &&
                effect.Status !=
                    GameControlStatus.Disposed &&
                ReferenceEquals(
                    effect.Owner,
                    target);
        }

        private static bool IsUsable(
            ClassicWeaponBuffShineEffect effect,
            PlayerObject target)
        {
            return effect != null &&
                   effect.Status !=
                       GameControlStatus.Disposed &&
                   ReferenceEquals(
                       effect.Owner,
                       target);
        }

        private static void AddVisual(
            WalkableWorldControl world,
            WorldObject visual)
        {
            world.Objects.Add(
                visual);

            _ = visual.Load();
        }

        private static void DisposeVisuals(
            PlayerBuffVisualState state)
        {
            RemoveVisual(
                ref state.GreenJoint);

            RemoveVisual(
                ref state.SoulBarrierJoint);

            RemoveVisual(
                ref state.AttackShine);
            RemoveVisual(
                ref state.GreaterFortitudeGlow);
            RemoveVisual(
                ref state.CriticalDamageAura);
        }

        private static void RemoveVisual<T>(
            ref T visual)
            where T : WorldObject
        {
            if (visual == null)
            {
                return;
            }

            T old =
                visual;

            visual =
                null;

            if (old.Parent != null)
            {
                old.Parent.Children.Remove(
                    old);
            }
            else if (old.World != null)
            {
                old.World.Objects.Remove(
                    old);
            }

            old.Dispose();
        }

        /// <summary>
        /// Persistent BITMAP_SHINY + 1 effect attached to the
        /// weapon hand/link bones.
        ///
        /// Original SourceMain:
        ///
        /// if (eBuff_Attack || eBuff_HelpNpc)
        /// {
        ///     for each weapon:
        ///         bone LinkBone
        ///         bone LinkBone - 6
        ///         bone LinkBone - 7
        /// }
        ///
        /// Light:
        /// (Luminosity, Luminosity * .3, Luminosity * .2)
        ///
        /// Scale:
        /// 1.5
        /// </summary>
        private sealed class ClassicWeaponBuffShineEffect
            : WorldObject
        {
            private const string TexturePath =
                "Effect/Shiny02.jpg";

            private const float ClassicReferenceFps =
                25.0f;

            private const float LightUpdateInterval =
                1.0f /
                ClassicReferenceFps;

            private static readonly BlendState ClassicAdditive =
                new BlendState
                {
                    ColorBlendFunction =
                        BlendFunction.Add,

                    ColorSourceBlend =
                        Blend.One,

                    ColorDestinationBlend =
                        Blend.One,

                    AlphaBlendFunction =
                        BlendFunction.Add,

                    AlphaSourceBlend =
                        Blend.One,

                    AlphaDestinationBlend =
                        Blend.One
                };

            private readonly PlayerObject _owner;

            private Texture2D _texture;

            private float _leftLuminosity =
                0.8f;

            private float _rightLuminosity =
                0.8f;

            private float _lightAccumulator;

            public PlayerObject Owner =>
                _owner;

            public ClassicWeaponBuffShineEffect(
                PlayerObject owner)
            {
                _owner =
                    owner ??
                    throw new ArgumentNullException(
                        nameof(owner));
                
                Position = _owner.WorldPosition.Translation;

                BoundingBoxLocal = new BoundingBox(
                    new Vector3(-120.0f, -120.0f, -40.0f),
                    new Vector3(120.0f, 120.0f, 240.0f));

                Interactive = false;

                IsTransparent = true;

                AffectedByTransparency = true;

                BlendState =
                    ClassicAdditive;

                DepthState =
                    GraphicsManager.ReadOnlyDepth;
            }

            public override async Task LoadContent()
            {
                await base.LoadContent();

                await TextureLoader.Instance.Prepare(
                    TexturePath);

                _texture =
                    TextureLoader.Instance
                        .GetTexture2D(
                            TexturePath);

                if (_texture == null)
                {
                    Status =
                        GameControlStatus.Error;
                }
            }

            public override void Update(
                GameTime gameTime)
            {
                base.Update(gameTime);

                if (Status !=
                    GameControlStatus.Ready)
                {
                    return;
                }

                if (_owner == null ||
                    _owner.Status ==
                        GameControlStatus.Disposed ||
                    _owner.World == null)
                {
                    RemoveSelf();
                    return;
                }

                Position = _owner.WorldPosition.Translation;

                bool hide =
                    _owner.Hidden ||
                    _owner.IsDead ||
                    _owner.Status !=
                        GameControlStatus.Ready;

                Hidden =
                    hide;

                if (hide)
                {
                    return;
                }

                _lightAccumulator +=
                    (float)
                    gameTime.ElapsedGameTime
                        .TotalSeconds;

                while (_lightAccumulator >=
                       LightUpdateInterval)
                {
                    _lightAccumulator -=
                        LightUpdateInterval;

                    // Original:
                    // rand()%30 + 70
                    //
                    _leftLuminosity =
                        MuGame.Random.Next(
                            70,
                            100) *
                        0.01f;

                    _rightLuminosity =
                        MuGame.Random.Next(
                            70,
                            100) *
                        0.01f;
                }
            }

            public override void Draw(
                GameTime gameTime)
            {
                base.Draw(gameTime);

                if (!Visible ||
                    _texture == null)
                {
                    return;
                }

                SpriteBatch spriteBatch =
                    GraphicsManager.Instance.Sprite;

                using (
                    new SpriteBatchScope(
                        spriteBatch,
                        SpriteSortMode.Deferred,
                        ClassicAdditive,
                        SamplerState.LinearClamp,
                        GraphicsManager.ReadOnlyDepth,
                        RasterizerState.CullNone))
                {
                    DrawWeaponSet(
                        spriteBatch,
                        PlayerObject.LeftHandBoneIndex,
                        _leftLuminosity);

                    DrawWeaponSet(
                        spriteBatch,
                        PlayerObject.RightHandBoneIndex,
                        _rightLuminosity);
                }
            }

            private void DrawWeaponSet(
                SpriteBatch spriteBatch,
                int linkBone,
                float luminosity)
            {
                Vector3 light =
                    new Vector3(
                        luminosity,
                        luminosity *
                            0.3f,
                        luminosity *
                            0.2f);

                DrawBoneSprite(
                    spriteBatch,
                    linkBone,
                    light);

                DrawBoneSprite(
                    spriteBatch,
                    linkBone - 6,
                    light);

                DrawBoneSprite(
                    spriteBatch,
                    linkBone - 7,
                    light);
            }

            private void DrawBoneSprite(
                SpriteBatch spriteBatch,
                int boneIndex,
                Vector3 light)
            {
                if (!_owner.TryGetBoneWorldMatrix(
                        boneIndex,
                        out Matrix boneWorld))
                {
                    return;
                }

                DrawWorldSprite(
                    spriteBatch,
                    boneWorld.Translation,
                    light,
                    1.5f);
            }

            private void DrawWorldSprite(
                SpriteBatch spriteBatch,
                Vector3 worldPosition,
                Vector3 light,
                float classicScale)
            {
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
                    Camera.Instance.ViewNear)
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
                    viewport.Height *
                    MathF.Abs(
                        projection.M22) /
                    (
                        2.0f *
                        cameraDepth
                    );

                float spriteScale =
                    classicScale *
                    pixelsPerCameraUnit;

                if (!float.IsFinite(
                        spriteScale) ||
                    spriteScale <= 0.0f)
                {
                    return;
                }

                Vector2 origin =
                    new Vector2(
                        _texture.Width *
                            0.5f,
                        _texture.Height *
                            0.5f);

                spriteBatch.Draw(
                    _texture,
                    new Vector2(
                        projected.X,
                        projected.Y),
                    null,
                    new Color(light) *
                        TotalAlpha,
                    0.0f,
                    origin,
                    spriteScale,
                    SpriteEffects.None,
                    MathHelper.Clamp(
                        projected.Z,
                        0.0f,
                        1.0f));
            }

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
                    World.Objects.Remove(
                        this);

                    Dispose();
                    return;
                }

                Dispose();
            }

            public override void Dispose()
            {
                // Texture is owned by TextureLoader cache.
                _texture = null;

                base.Dispose();
            }
        }
    }
}