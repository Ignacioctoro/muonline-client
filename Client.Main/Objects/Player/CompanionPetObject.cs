using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Controllers;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Player
{
    /// <summary>
    /// Visual Season 6 helpers. Equipment and bonuses remain server-authoritative.
    /// Each kind owns one lazy model instance, so switching pets cannot race an
    /// asynchronous model/texture reload on a different kind of pet.
    /// </summary>
    public sealed class CompanionPetObject : ModelObject
    {
        private readonly PlayerObject _owner;
        private readonly string _modelPath;
        private readonly float _nativeScale;
        private bool _equipped;
        private bool _loadRequested;
        private bool _hasPosition;
        private Vector3 _position;
        private float _yaw;
        private double _phase;
        private float _effectElapsed;
        private readonly Random _random = new();

        protected override bool RequiresPerFrameAnimation => true;
        public override bool IsStaticForCaching => false;

        public CompanionPetObject(PlayerObject owner, int itemNumber)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            ItemGroup = 13;
            ItemNumber = itemNumber;
            _modelPath = itemNumber switch
            {
                // These are byte-identical to Player/Helper01 and Helper02 in
                // Data_Broyal; their fairy/satan textures are in Item/.
                0 => "Item/helper01.bmd",
                1 => "Item/helper02.bmd",
                64 => "Item/partCharge4/demon.bmd",
                80 => "Item/PandaPet.bmd",
                _ => throw new ArgumentOutOfRangeException(nameof(itemNumber))
            };
            _nativeScale = itemNumber switch { 64 => 0.16f, 80 => 1f, _ => 0.8f };
            Hidden = true;
            Interactive = false;
            LinkParentAnimation = false;
            ParentBoneLink = -1;
            RenderShadow = itemNumber == 80;
            LightEnabled = true;
            CurrentAction = 0;
        }

        public void SetEquipped(bool equipped)
        {
            if (_equipped == equipped) return;
            _equipped = equipped;
            Hidden = !equipped;
            _hasPosition = false;
            _effectElapsed = 0f;
        }

        private async Task LoadModelAsync()
        {
            try
            {
                var model = await BMDLoader.Instance.Prepare(_modelPath);
                if (model == null)
                    throw new InvalidOperationException($"Missing pet model: {_modelPath}");
                MuGame.ScheduleOnMainThread(() =>
                {
                    if (Status == GameControlStatus.Disposed) return;
                    Model = model;
                    float speed = model.Actions != null && model.Actions.Length > 0
                        ? model.Actions[0].PlaySpeed : 0f;
                    // Original pets advance 0.5 animation keys per 25-Hz tick.
                    AnimationSpeed = speed > 0f ? 12.5f / speed : 12.5f;
                });
            }
            catch (Exception ex)
            {
                MuGame.AppLoggerFactory?.CreateLogger<CompanionPetObject>()
                    .LogWarning(ex, "Unable to load companion {Pet}: {Path}", ItemNumber, _modelPath);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_equipped || World == null || Status == GameControlStatus.Disposed) return;
            if (!_loadRequested)
            {
                _loadRequested = true;
                _ = LoadModelAsync();
            }

            float dt = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0f, 0.1f);
            _phase += dt;
            Vector3 ownerPosition = _owner.WorldPosition.Translation;
            bool groundPet = ItemNumber == 80;
            float radius = groundPet ? 70f : ItemNumber == 64 ? 50f : 100f;
            float phase = (float)((_phase * (groundPet ? MathHelper.TwoPi / 4f : 1.1f)) % MathHelper.TwoPi);
            Vector3 target = ownerPosition + new Vector3(MathF.Sin(phase) * radius,
                MathF.Cos(phase) * radius, 0f);
            float height = groundPet ? 20f : ItemNumber == 64
                ? 230f * _owner.Scale : 150f + (float)Math.Sin(_phase * 2.5) * 25f;
            target.Z = groundPet
                ? World.Terrain.RequestTerrainHeight(target.X, target.Y) + height
                : ownerPosition.Z + height;

            // Recover after warps without crossing the whole map.
            if (!_hasPosition || Vector3.DistanceSquared(_position, ownerPosition) > 900f * 900f)
            {
                _position = target;
                _yaw = _owner.Angle.Z;
                _hasPosition = true;
            }
            Vector3 delta = target - _position;
            float distance = delta.Length();
            if (distance > 1f)
            {
                float step = Math.Min(distance, Math.Max(80f, distance * 3f) * dt);
                _position += delta * (step / distance);
                if (delta.X * delta.X + delta.Y * delta.Y > 1f)
                {
                    float desired = MathF.Atan2(delta.X, -delta.Y);
                    _yaw += MathHelper.WrapAngle(desired - _yaw) * (1f - MathF.Exp(-8f * dt));
                }
            }
            if (groundPet)
                _position.Z = World.Terrain.RequestTerrainHeight(_position.X, _position.Y) + height;

            Scale = _nativeScale / Math.Max(0.01f, _owner.Scale);
            SyncAfterOwnerMovement();
            base.Update(gameTime);
            EmitCompanionEffects(dt);
        }

        // Children update before WalkerObject moves. Recompute only the parent
        // compensation afterwards; don't advance animation or movement twice.
        public void SyncAfterOwnerMovement()
        {
            if (!_equipped || !_hasPosition || World == null) return;
            Position = Vector3.Transform(_position, Matrix.Invert(_owner.WorldPosition));
            Angle = new Vector3(0f, 0f, _yaw - _owner.Angle.Z);
            if (ItemNumber == 80) FitPandaToTerrain();
        }

        private void FitPandaToTerrain()
        {
            var bones = GetBoneTransforms();
            if (Model?.Meshes == null || bones == null || bones.Length == 0) return;

            // Use the animated, transformed mesh rather than its BMD origin.
            // This includes skeletal offsets and the owner's current transform.
            float bottom = float.PositiveInfinity;
            foreach (var mesh in Model.Meshes)
            {
                if (mesh.Vertices == null) continue;
                foreach (var vertex in mesh.Vertices)
                {
                    int node = vertex.Node;
                    if (node < 0 || node >= bones.Length) continue;
                    var local = Vector3.Transform(vertex.Position, bones[node]);
                    float z = Vector3.Transform(local, WorldPosition).Z;
                    if (float.IsFinite(z)) bottom = Math.Min(bottom, z);
                }
            }
            if (!float.IsFinite(bottom)) return;
            float ground = World.Terrain.RequestTerrainHeight(_position.X, _position.Y);
            _position.Z += ground + 3f - bottom;
            Position = Vector3.Transform(_position, Matrix.Invert(_owner.WorldPosition));
        }


        protected override bool RenderBlendMeshOnLowQuality => true;

        protected override bool IsMeshTwoSided(int mesh, bool isBlendMesh) =>
            ItemNumber == 80 || base.IsMeshTwoSided(mesh, isBlendMesh);

        protected override bool IsBlendMesh(int mesh) =>
            ItemNumber == 0 || base.IsBlendMesh(mesh);

        protected override BlendState GetMeshBlendState(int mesh, bool isBlendMesh)
        {
            // Fairy JPGs have a black backdrop, not an alpha channel.
            // Keep this per instance: inventory thumbnails share the same BMD.
            if (ItemNumber == 0) return _classicItemBrightAdditive;
            return base.GetMeshBlendState(mesh, isBlendMesh);
        }

        public override void DrawModel(bool isAfterDraw)
        {
            if (!_equipped || !Visible || Model?.Meshes == null) return;
            if (ItemNumber != 80)
            {
                base.DrawModel(isAfterDraw);
                return;
            }
            if (isAfterDraw) return; // PandaPet.jpg is a solid textured mesh.
            // The fast opaque path can inherit effect colour/state left by
            // the player's equipment. Give the Panda its complete material pass.
            var effect = GraphicsManager.Instance.AlphaTestEffect3D;
            var colour = effect.DiffuseColor;
            var depth = GraphicsDevice.DepthStencilState;
            try
            {
                effect.DiffuseColor = Vector3.One;
                effect.World = WorldPosition;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                for (int mesh = 0; mesh < Model.Meshes.Length; mesh++) DrawMesh(mesh);
            }
            finally
            {
                effect.DiffuseColor = colour;
                GraphicsDevice.DepthStencilState = depth;
            }
        }

        private Vector3 EffectPosition(int bone, Vector3 fallback)
        {
            var bones = GetBoneTransforms();
            return bones != null && bone >= 0 && bone < bones.Length
                ? Vector3.Transform(Vector3.Zero, bones[bone] * WorldPosition)
                : fallback;
        }

        private void EmitCompanionEffects(float dt)
        {
            var fx = World?.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed || !Visible || Model == null) return;
            _effectElapsed += dt;
            // No catch-up bursts after frame stalls; at most 12.5 emissions/sec.
            if (_effectElapsed < 0.08f) return;
            _effectElapsed %= 0.08f;
            var offset = new Vector3(_random.Next(-10, 11), _random.Next(-10, 11), _random.Next(0, 16));
            if (ItemNumber == 64)
            {
                var colour = new Vector3(0.55f, 0.22f, 0.18f);
                // Native PetActionDemon smoke on the wing/body bones.
                int bone = _random.Next(2) == 0 ? 34 : 51;
                fx.CreateParticle(ClassicTextureIds.BitmapClud64,
                    EffectPosition(bone, _position), Angle, colour, subType: 11, scale: 0.25f);
                fx.CreateParticle(ClassicTextureIds.BitmapLight,
                    EffectPosition(8, _position), Angle, new Vector3(0.9f, 0.35f, 0.15f),
                    subType: 6, scale: 0.35f);
            }
            else if (ItemNumber == 0)
            {
                fx.CreateParticle(ClassicTextureIds.BitmapSpark, _position + offset,
                    Angle, new Vector3(0.8f, 0.7f, 0.45f), subType: 1, scale: 0.35f);
            }
            else if (ItemNumber == 1)
            {
                fx.CreateParticle(ClassicTextureIds.BitmapClud64, _position + offset,
                    Angle, new Vector3(0.25f, 0.12f, 0.1f), subType: 11, scale: 0.15f);
                fx.CreateParticle(ClassicTextureIds.BitmapLight, _position + offset,
                    Angle, new Vector3(0.65f, 0.18f, 0.08f), subType: 6, scale: 0.2f);
            }
            else
            {
                fx.CreateParticle(ClassicTextureIds.BitmapLight, _position + offset,
                    Angle, new Vector3(0.65f, 0.5f, 0.15f), subType: 6, scale: 0.2f);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (_equipped) base.Draw(gameTime);
        }

        public override void DrawShadowCaster(Effect shadowEffect, Matrix lightViewProjection)
        {
            if (_equipped && Visible && RenderShadow)
                base.DrawShadowCaster(shadowEffect, lightViewProjection);
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (_equipped) base.DrawAfter(gameTime);
        }
    }
}
