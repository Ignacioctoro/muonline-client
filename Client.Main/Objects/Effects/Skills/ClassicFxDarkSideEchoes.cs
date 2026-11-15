#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Client.Main.Controllers;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Main.Objects.Effects.Skills
{
    /// <summary>Visual-only port of the attack poses drawn by MuMain's DarksideRendering.</summary>
    internal sealed class ClassicFxDarkSideEchoes : WorldObject
    {
        private const int AttackAction = (int)PlayerAction.PlayerSkillDarksideAttack;
        private const int FlankCount = 5;
        private readonly PlayerObject _caster;
        private readonly Vector3 _start;
        private readonly Vector3 _target;
        private readonly Echo[] _echoes = new Echo[FlankCount + 1];
        private readonly float _distance;
        private readonly float _outboundTicks;
        private float _ticks;
        private bool _launchedWave;
        private bool _hit;

        public ClassicFxDarkSideEchoes(PlayerObject caster, Vector3 target)
        {
            _caster = caster;
            _start = caster.WorldPosition.Translation;
            _target = target;
            _distance = Vector3.Distance(_start, _target);
            _outboundTicks = MathF.Sqrt(_distance / 0.05f);
            Position = _start;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            Vector3 span = target - _start;
            BoundingBoxLocal = new BoundingBox(
                Vector3.Min(Vector3.Zero, span) - new Vector3(600f),
                Vector3.Max(Vector3.Zero, span) + new Vector3(600f));

            var parts = new ModelObject[]
            {
                caster, caster.HelmMask, caster.Helm, caster.Armor, caster.Pants,
                caster.Gloves, caster.Boots, caster.Weapon1, caster.Weapon2, caster.EquippedWings
            };
            int boneCount = caster.Model?.Bones?.Length ?? 0;
            for (int i = 0; i < _echoes.Length; i++)
                _echoes[i] = new Echo(this, parts, boneCount);
        }

        public override void Update(GameTime gameTime)
        {
            if (Status != GameControlStatus.Ready)
                return;
            if (World?.ClassicFx is not { Enabled: true, IsDisposed: false } fx ||
                !ReferenceEquals(_caster.World, World) || _caster.IsDead)
            {
                Finish();
                return;
            }

            _ticks += MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f) * 25f;
            float travelled = _ticks * _ticks * 0.05f;
            if (_ticks <= _outboundTicks)
            {
                Vector3 position = Vector3.Lerp(_start, _target, travelled / _distance);
                float yaw = GetYaw(_start, _target);
                _echoes[0].Pose(_caster, position, yaw, MathF.Min((_ticks + 1f) * 0.2f, 1.9f), 0.85f);
                if (travelled > 30f && !_launchedWave)
                {
                    _launchedWave = true;
                    EmitWave(fx, position, yaw, 3, Vector3.One);
                }
            }
            else
            {
                _echoes[0].Hide();
                if (!_hit)
                {
                    _hit = true;
                    fx.CreateParticle(ClassicTextureIds.BitmapDamage2,
                        _target, _caster.TotalAngle, Vector3.One, subType: 0, scale: 1f);
                    SoundController.Instance.PlayBuffer("Sound/Ragefighter/Rage_Darkside_Att.wav");
                }

                bool pending = false;
                for (int i = 0; i < FlankCount; i++)
                {
                    Echo echo = _echoes[i + 1];
                    float age = _ticks - _outboundTicks - i * 6f;
                    if (age < 0f)
                    {
                        pending = true;
                        continue;
                    }

                    float distance = 0.245f * age * (age + 1f);
                    if (distance > 500f)
                    {
                        echo.Hide();
                        continue;
                    }

                    pending = true;
                    Vector3 offset = Vector3.TransformNormal(new Vector3(150f, 150f, 0f),
                        Matrix.CreateRotationZ(MathHelper.ToRadians(60f * i)));
                    Vector3 start = _target + offset;
                    Vector3 position = start - Vector3.Normalize(offset) * distance;
                    float yaw = GetYaw(start, _target);
                    float alpha = MathHelper.Clamp(0.2f + age * 0.02f, 0.2f, 0.6f);
                    if (distance > 300f)
                        alpha *= (500f - distance) / 200f;
                    echo.Pose(_caster, position, yaw, MathF.Min(age * 0.05f, 1.9f), alpha);
                    if (age >= 30f && !echo.EmittedWave)
                    {
                        echo.EmittedWave = true;
                        EmitWave(fx, position, yaw, 4, new Vector3(0.2f));
                    }
                }

                if (!pending)
                {
                    Finish();
                    return;
                }
            }

            base.Update(gameTime);
        }

        private void EmitWave(ClassicFxRuntime fx, Vector3 position, float yaw, int subType, Vector3 light)
        {
            var actions = _caster.Model?.Actions;
            if (actions == null || actions.Length <= AttackAction || actions[AttackAction] is not { } action)
                return;
            fx.CreateEffect(ClassicFxEffectType.Shockwave01, position,
                new Vector3(MathHelper.PiOver2, 0f, yaw - MathHelper.Pi), light,
                ClassicFxOwner.FromWorldObject(_caster), subType: subType, scale: 0.8f,
                nativeAnimationSpeed: action.PlaySpeed);
        }

        private static float GetYaw(Vector3 from, Vector3 to) =>
            MathHelper.ToRadians(AngleUtils.CreateAngleDegrees(from.X, from.Y, to.X, to.Y));

        private void Finish()
        {
            World?.Objects.Remove(this);
            Dispose();
        }

        private sealed class Echo
        {
            private readonly List<EchoPart> _parts = new();
            private readonly Matrix[] _pose;
            public bool EmittedWave { get; set; }

            public Echo(WorldObject container, ModelObject[] sources, int boneCount)
            {
                _pose = new Matrix[boneCount];
                foreach (var source in sources)
                {
                    if (!source.Visible || source.Model?.Meshes is not { Length: > 0 })
                        continue;
                    var part = new EchoPart(source);
                    _parts.Add(part);
                    container.Children.Add(part);
                }
            }

            public void Pose(PlayerObject caster, Vector3 position, float yaw, float frame, float alpha)
            {
                if (!caster.TrySampleAnimationPose(AttackAction, frame, _pose))
                    return;

                Matrix transform = Matrix.CreateScale(caster.TotalScale) *
                    Matrix.CreateRotationZ(yaw) * Matrix.CreateTranslation(position);
                Matrix inverseCaster = Matrix.Invert(caster.WorldPosition);
                Matrix[] livePose = caster.GetBoneTransforms();
                foreach (var part in _parts)
                {
                    ModelObject source = part.Source;
                    Matrix attachment = source.WorldPosition * inverseCaster;
                    int bone = source.ParentBoneLink;
                    if ((uint)bone < (uint)_pose.Length && (uint)bone < (uint)livePose.Length)
                        attachment = attachment * Matrix.Invert(livePose[bone]) * _pose[bone];
                    part.SetPose(source == caster || source.LinkParentAnimation ? _pose : source.GetBoneTransforms(),
                        attachment * transform, alpha);
                }
            }

            public void Hide()
            {
                foreach (var part in _parts)
                    part.Hidden = true;
            }
        }

        private sealed class EchoPart : ModelObject
        {
            private Task? _modelLoad;
            public ModelObject Source { get; }
            protected override bool AllowAnimationUpdates => false;
            protected override bool AllowDynamicLightingShader => false;

            public EchoPart(ModelObject source)
            {
                Source = source;
                Hidden = true;
                Interactive = false;
                IsTransparent = true;
                AffectedByTransparency = true;
                RenderShadow = false;
                BlendState = BlendState.AlphaBlend;
                BlendMeshState = BlendState.AlphaBlend;
                DepthState = DepthStencilState.DepthRead;
                HiddenMesh = source.HiddenMesh;
                Light = Vector3.One;
                LightEnabled = false;
                UseSunLight = false;
            }

            public override Task Load()
            {
                Model = Source.Model;
                return base.Load();
            }

            public override Task LoadContent() => _modelLoad ??= base.LoadContent();

            public void SetPose(Matrix[]? pose, Matrix transform, float alpha)
            {
                if (pose == null || Status != GameControlStatus.Ready)
                    return;
                BoneTransform = pose;
                WorldPosition = transform;
                Alpha = alpha;
                Hidden = false;
                InvalidateBuffers(BufferFlagAnimation);
            }
        }
    }
}
