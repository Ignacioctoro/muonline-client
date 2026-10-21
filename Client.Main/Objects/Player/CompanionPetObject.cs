using System;
using System.Threading.Tasks;
using Client.Main.Content;
using Client.Main.Models;
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
            _nativeScale = itemNumber is 64 or 80 ? 0.7f : 1f;
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
        }

        // Children update before WalkerObject moves. Recompute only the parent
        // compensation afterwards; don't advance animation or movement twice.
        public void SyncAfterOwnerMovement()
        {
            if (!_equipped || !_hasPosition || World == null) return;
            Position = Vector3.Transform(_position, Matrix.Invert(_owner.WorldPosition));
            Angle = new Vector3(0f, 0f, _yaw - _owner.Angle.Z);
        }

        public override void Draw(GameTime gameTime)
        {
            if (_equipped) base.Draw(gameTime);
        }

        public override void DrawAfter(GameTime gameTime)
        {
            if (_equipped) base.DrawAfter(gameTime);
        }
    }
}
