using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Threading.Tasks;

namespace Client.Main.Objects.Player
{
    /// <summary>
    /// Visual-only Season 6 Dark Raven. The item remains equipped in right-hand
    /// slot 1, but the model is a separate animated pet, not a WeaponObject.
    ///
    /// Flight follows the PET_FLY / PET_FLYING branch of MuMain's
    /// CSPetDarkSpirit::MovePet. Combat, targets and progression remain
    /// authoritative in OpenMU and are deliberately not simulated here.
    /// </summary>
    public sealed class DarkRavenObject : ModelObject
    {
        private const string ModelPath = "Skill/darkspirit.bmd";
        private const float NativeTickSeconds = 1f / 25f;
        private const float FlyRange = 150f;
        private const float RecoveryDistanceSquared = 409600f;
        // MuMain PET_STAND_START switches to PET_STAND once the raven
        // is within 50 native world units of its arm attachment.
        private const float PerchArrivalDistanceSquared = 50f * 50f;
        private const float NativeScale = 0.7f;
        private const float DegToRad = MathF.PI / 180f;

        private readonly PlayerObject _owner;
        private readonly Random _random = new();

        private Vector3 _flightPosition;
        private float _yaw;
        private float _pitch;
        private float _velocityY = -3f;
        private float _velocityZ;
        private float _stepAccumulator;
        private float _recoveryFrames;
        private bool _hasPosition;
        private bool _isPerched;
        private bool _equipped;
        private bool _loadingModel;

        public bool IsEquipped => _equipped;

        public DarkRavenObject(PlayerObject owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Hidden = true;
            LinkParentAnimation = false;
            ParentBoneLink = -1;
            RenderShadow = false;
            Interactive = false;
            CurrentAction = 0;
        }

        /// <summary>
        /// Changes visual presence only. Never writes inventory or pet packets.
        /// Lazy BMD load keeps non-Dark-Lord players free of the pet's startup cost.
        /// </summary>
        public void SetEquipped(bool equipped)
        {
            if (_equipped == equipped)
                return;

            _equipped = equipped;
            Hidden = !equipped;
            _hasPosition = false;
            _isPerched = false;
            _stepAccumulator = 0f;
            _recoveryFrames = 0f;

            if (equipped && Model == null && !_loadingModel)
                _ = LoadRavenModelAsync();
        }

        private async Task LoadRavenModelAsync()
        {
            _loadingModel = true;
            try
            {
                var model = await BMDLoader.Instance.Prepare(ModelPath);
                if (model == null)
                    return;

                Model = model;
                // MuMain calls PlayAnimation at 0.4 keys per native 25Hz tick.
                // MonoGame also applies the BMD action's PlaySpeed.
                float bmdPlaySpeed = model.Actions != null && model.Actions.Length > 0
                    ? model.Actions[0].PlaySpeed
                    : 0f;
                AnimationSpeed = bmdPlaySpeed > 0f ? 10f / bmdPlaySpeed : 10f;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dark Raven BMD load failed: {ex}");
            }
            finally
            {
                _loadingModel = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_equipped || World == null)
                return;

            // Keep native world size despite PlayerObject's 0.85 model scale.
            Scale = NativeScale / Math.Max(0.01f, _owner.Scale);

            Vector3 ownerWorld = _owner.WorldPosition.Translation;
            if (!_hasPosition)
            {
                _flightPosition = ownerWorld + new Vector3(0f, 0f, 300f);
                _yaw = _owner.Angle.Z;
                _pitch = 0f;
                _velocityY = -3f;
                _velocityZ = 0f;
                _hasPosition = true;
            }

            bool isSafeZone = _owner.World?.Terrain != null
                && _owner.World.Terrain.RequestTerrainFlag(
                    (int)_owner.Location.X, (int)_owner.Location.Y)
                    .HasFlag(Client.Data.ATT.TWFlags.SafeZone);

            if (isSafeZone)
            {
                // The original client has two distinct states:
                // PET_STAND_START approaches the arm; PET_STAND is attached
                // to bone 37 on every frame (including walking frames).
                // Smoothing PET_STAND would make the raven trail its owner.
                Vector3 shoulder = GetPerchWorldPosition(ownerWorld);

                if (!_isPerched)
                {
                    if (Vector3.DistanceSquared(_flightPosition, shoulder) >
                        PerchArrivalDistanceSquared)
                    {
                        float approach = Math.Min(1f,
                            (float)gameTime.ElapsedGameTime.TotalSeconds * 6f);
                        _flightPosition = Vector3.Lerp(
                            _flightPosition, shoulder, approach);
                    }

                    if (Vector3.DistanceSquared(_flightPosition, shoulder) <=
                        PerchArrivalDistanceSquared)
                    {
                        _isPerched = true;
                    }
                }

                if (_isPerched)
                    _flightPosition = shoulder;

                _yaw = _owner.Angle.Z - 120f * DegToRad;
                _pitch = 0f;
                CurrentAction = _isPerched ? 2 : 1;
                _stepAccumulator = 0f;
                _recoveryFrames = 0f;
            }
            else
            {
                _isPerched = false;
                _stepAccumulator = Math.Min(
                    _stepAccumulator + (float)gameTime.ElapsedGameTime.TotalSeconds,
                    NativeTickSeconds * 4f);

                // Original pet movement is expressed per simulation tick.
                // Fixed steps prevent FPS-dependent speed on Windows / Android.
                while (_stepAccumulator >= NativeTickSeconds)
                {
                    StepFlight(ownerWorld);
                    _stepAccumulator -= NativeTickSeconds;
                }
            }

            ApplyWorldSpaceTransform();

            base.Update(gameTime);
        }

        /// <summary>
        /// WalkerObject moves its owner AFTER ModelObject.Update has already
        /// updated child pets. Re-anchor a perched raven after the movement
        /// phase so its hand attachment never uses the previous frame's
        /// owner transform. This is intentionally visual-only and does not
        /// advance AI or animation a second time.
        /// </summary>
        public void SyncPerchAfterOwnerMovement()
        {
            if (!_equipped || !_isPerched || World == null || _owner.World == null)
                return;

            bool inSafeZone = _owner.World.Terrain.RequestTerrainFlag(
                (int)_owner.Location.X, (int)_owner.Location.Y)
                .HasFlag(Client.Data.ATT.TWFlags.SafeZone);

            if (!inSafeZone)
                return;

            _flightPosition = GetPerchWorldPosition(
                _owner.WorldPosition.Translation);
            _yaw = _owner.Angle.Z - 120f * DegToRad;
            _pitch = 0f;
            ApplyWorldSpaceTransform();
        }

        private Vector3 GetPerchWorldPosition(Vector3 ownerWorld)
        {
            // MuMain CSPetDarkSpirit::MovePet (PET_STAND): bone 37,
            // offset (-10, 0, 10) transformed by the owner's full matrix.
            var bones = _owner.GetBoneTransforms();
            return bones != null && bones.Length > 37
                ? Vector3.Transform(new Vector3(-10f, 0f, 10f),
                    bones[37] * _owner.WorldPosition)
                : ownerWorld + new Vector3(0f, 0f, 250f);
        }

        private void ApplyWorldSpaceTransform()
        {
            // Raven remains an owner child for lifetime/visibility, but
            // its flight position is world-space. Avoid applying the
            // owner's transform twice.
            Matrix parentInverse = Matrix.Invert(_owner.WorldPosition);
            Position = Vector3.Transform(_flightPosition, parentInverse);
            Angle = new Vector3(_pitch, 0f, _yaw - _owner.Angle.Z);
        }

        private void StepFlight(Vector3 ownerWorld)
        {
            float dx = ownerWorld.X - _flightPosition.X;
            float dy = ownerWorld.Y - _flightPosition.Y;
            float horizontalDistanceSquared = dx * dx + dy * dy;
            float flyRangeSquared = FlyRange * FlyRange;

            // MuMain: steer toward owner beyond FlyRange; otherwise drift and
            // intermittently adjust direction. No synthesized orbital sine path.
            if (horizontalDistanceSquared >= flyRangeSquared)
            {
                float wantedAngle = MathF.Atan2(dx, -dy);
                float maxTurn = (5f + _random.Next(15)) * DegToRad;
                _yaw += Math.Clamp(WrapAngle(wantedAngle - _yaw), -maxTurn, maxTurn);
            }

            int speedRandom = 28;
            float targetHeight = ownerWorld.Z +
                (_owner.Vehicle != null && !_owner.Vehicle.Hidden && _owner.Vehicle.ItemIndex == 0 ? 350f : 250f);

            if (_flightPosition.Z < targetHeight)
            {
                speedRandom = 10;
                _pitch = Math.Max(-15f * DegToRad, _pitch - 2f * DegToRad);
            }
            else if (_flightPosition.Z > targetHeight + 100f)
            {
                speedRandom = 20;
                _pitch = Math.Min(15f * DegToRad, _pitch + 2f * DegToRad);
            }

            if (_random.Next(speedRandom) == 0)
            {
                float speed;
                if (horizontalDistanceSquared >= flyRangeSquared)
                    speed = -(128f + _random.Next(64)) * 0.1f;
                else
                {
                    speed = -(32f + _random.Next(8)) * 0.1f;
                    _yaw += _random.Next(60) * DegToRad;
                }

                _velocityY = (speed + _velocityY) * 0.5f;
                _velocityZ = (_random.Next(64) - 32) * 0.1f;
            }

            // MuMain uses AngleMatrix + VectorRotate(o->Direction, ...).
            Matrix rotation = Matrix.CreateRotationX(_pitch) * Matrix.CreateRotationZ(_yaw);
            _flightPosition += Vector3.Transform(new Vector3(0f, _velocityY, _velocityZ), rotation);

            CurrentAction = _velocityY < -12f ? 1 : 0;

            float distanceSquared =
                (ownerWorld.X - _flightPosition.X) * (ownerWorld.X - _flightPosition.X) +
                (ownerWorld.Y - _flightPosition.Y) * (ownerWorld.Y - _flightPosition.Y);
            if (_flightPosition.Z < ownerWorld.Z - 200f || distanceSquared > RecoveryDistanceSquared)
                _recoveryFrames++;
            if (_recoveryFrames > 90f)
            {
                // Classic lost-pet recovery, not a persistent teleport loop.
                _flightPosition = ownerWorld + new Vector3(0f, 0f, 250f);
                _recoveryFrames = 0f;
            }
        }

        private static float WrapAngle(float radians)
        {
            while (radians > MathF.PI) radians -= MathF.PI * 2f;
            while (radians < -MathF.PI) radians += MathF.PI * 2f;
            return radians;
        }
    }
}
