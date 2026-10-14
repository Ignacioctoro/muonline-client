using Client.Main.Content;
using Client.Main.Models;
using Client.Main.Controls;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Data;
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
        // CSPetSystem::CreatePetPointer starts the Raven's native light at 0.5.
        // Use its native BMD lighting instead of a full-intensity white gust.
        private static readonly Vector3 NativeRavenLight = new(0.5f, 0.5f, 0.5f);

        // These states mirror CSPetDarkSpirit: flight, physical rush/escape,
        // and magic/range cast. They NEVER calculate hits or damage.
        private enum RavenVisualState { Flight, Rush, Escape, Magic }
        private RavenVisualState _visualState;
        private WalkerObject _attackTarget;
        private WalkerObject _queuedTarget;
        private bool _queuedMagic;
        private float _attackVelocity;
        private float _attackGravity;
        private int _attackTicks;

        // Interpolate the DISPLAY between original 25-Hz simulation samples.
        // Never interpolate the perched attachment: it must stay on bone 37.
        private Vector3 _previousFlightPosition;
        private float _previousYaw;
        private float _previousPitch;

        protected override bool RequiresPerFrameAnimation => true;
        public override bool IsStaticForCaching => false;

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
        // Native CSPetDarkSpirit::RenderPet passes the server pet level as WeaponLevel.
        private int _nativePetLevel;

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
            _visualState = RavenVisualState.Flight;
            _attackTarget = null;
            _queuedTarget = null;
            _attackTicks = 0;
            _nativePetLevel = 0;

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

        /// <summary>
        /// Consume an OpenMU C1 A8 attack notification, on the world/update
        /// thread only. In a fast 3-hit ranged burst, update the magic target
        /// without restarting its fifteen native-tick effect phase.
        /// </summary>
        public void StartServerAttack(WalkerObject target, bool isRangeAttack)
        {
            if (!_equipped || target == null || World == null ||
                !ReferenceEquals(target.World, World) || _isPerched)
                return;

            if (_visualState == RavenVisualState.Magic && isRangeAttack)
            {
                _attackTarget = target;
                return;
            }

            if (_visualState == RavenVisualState.Rush && !isRangeAttack &&
                ReferenceEquals(_attackTarget, target))
                return;

            if (_visualState != RavenVisualState.Flight)
            {
                // Keep the newest event only. Never queue unbounded animations
                // when a high-level raven attacks every 100 milliseconds.
                _queuedTarget = target;
                _queuedMagic = isRangeAttack;
                return;
            }

            BeginServerAttack(target, isRangeAttack);
        }

        private void BeginServerAttack(WalkerObject target, bool isRangeAttack)
        {
            _attackTarget = target;
            _attackTicks = 0;
            _visualState = isRangeAttack ? RavenVisualState.Magic : RavenVisualState.Rush;
            if (isRangeAttack)
            {
                CurrentAction = 0; // Original magic action uses light effects.
            }
            else
            {
                _attackVelocity = 20f + _random.Next(10);
                _attackGravity = 0.5f;
                CurrentAction = 3; // darkspirit.bmd rush/escape action
            }
        }

        private void FinishServerAttack()
        {
            _visualState = RavenVisualState.Flight;
            _attackTarget = null;
            _attackTicks = 0;
            if (_queuedTarget != null)
            {
                var nextTarget = _queuedTarget;
                bool nextIsMagic = _queuedMagic;
                _queuedTarget = null;
                if (ReferenceEquals(nextTarget.World, World))
                    BeginServerAttack(nextTarget, nextIsMagic);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_equipped || World == null)
                return;

            // Only the local pet receives the C1 A9 level/experience snapshot.
            // Other players have no equivalent known level data; use the native
            // base mesh tier rather than fabricate a high-level glow.
            if (_owner.IsMainWalker &&
                MuGame.Network?.GetCharacterState() is { } characterState &&
                characterState.TryGetPetInfo(1, out var petInfo) &&
                petInfo.PetType == 0)
            {
                _nativePetLevel = petInfo.Level;
            }

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
                _previousFlightPosition = _flightPosition;
                _previousYaw = _yaw;
                _previousPitch = _pitch;
            }

            int simulatedTicks = 0;
            bool isSafeZone = _owner.World?.Terrain != null
                && _owner.World.Terrain.RequestTerrainFlag(
                    (int)_owner.Location.X, (int)_owner.Location.Y)
                    .HasFlag(Client.Data.ATT.TWFlags.SafeZone);

            if (isSafeZone)
            {
                _visualState = RavenVisualState.Flight;
                _attackTarget = null;
                _queuedTarget = null;
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
                _previousFlightPosition = _flightPosition;
                _previousYaw = _yaw;
                _previousPitch = _pitch;
            }
            else
            {
                _isPerched = false;
                _stepAccumulator = Math.Min(
                    _stepAccumulator + (float)gameTime.ElapsedGameTime.TotalSeconds,
                    NativeTickSeconds * 4f);

                // Native 25-Hz simulation, with independent visual interpolation.
                // At most four ticks after a hitch, without taking huge steps.
                while (_stepAccumulator >= NativeTickSeconds)
                {
                    _previousFlightPosition = _flightPosition;
                    _previousYaw = _yaw;
                    _previousPitch = _pitch;
                    if (_visualState == RavenVisualState.Flight)
                        StepFlight(ownerWorld);
                    else
                        StepAttack(ownerWorld);

                    _stepAccumulator -= NativeTickSeconds;
                    simulatedTicks++;
                }
            }

            float alpha = isSafeZone ? 1f :
                MathHelper.Clamp(_stepAccumulator / NativeTickSeconds, 0f, 1f);
            Vector3 displayPosition = isSafeZone ? _flightPosition :
                Vector3.Lerp(_previousFlightPosition, _flightPosition, alpha);
            float displayYaw = isSafeZone ? _yaw :
                _previousYaw + WrapAngle(_yaw - _previousYaw) * alpha;
            float displayPitch = isSafeZone ? _pitch :
                MathHelper.Lerp(_previousPitch, _pitch, alpha);

            ApplyWorldSpaceTransform(displayPosition, displayYaw, displayPitch);
            base.Update(gameTime);

            // Emit on actual native ticks, not on every rendered frame.
            // Otherwise 120 FPS would create 4.8x the original pet particles.
            if (!isSafeZone && simulatedTicks > 0 &&
                _visualState != RavenVisualState.Flight)
                EmitNativeAttackEffects();
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
            ApplyWorldSpaceTransform(_flightPosition, _yaw, _pitch);
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

        private void ApplyWorldSpaceTransform(Vector3 worldPosition, float yaw, float pitch)
        {
            // World-space flight; compensate the owner matrix once, not twice.
            Matrix parentInverse = Matrix.Invert(_owner.WorldPosition);
            Position = Vector3.Transform(worldPosition, parentInverse);
            Angle = new Vector3(pitch, 0f, yaw - _owner.Angle.Z);
        }

        private void StepAttack(Vector3 ownerWorld)
        {
            // Native PET_ATTACK/PET_ESCAPE uses action 3 and an accelerating
            // forward velocity. PET_ATTACK_MAGIC tracks the notified target
            // for 15 reference ticks and returns to flight.
            if (_visualState == RavenVisualState.Magic)
            {
                if (_attackTarget == null || !ReferenceEquals(_attackTarget.World, World))
                {
                    FinishServerAttack();
                    return;
                }
                Vector3 target = _attackTarget.WorldPosition.Translation;
                float wanted = MathF.Atan2(target.X - _flightPosition.X,
                    -(target.Y - _flightPosition.Y));
                _yaw += Math.Clamp(WrapAngle(wanted - _yaw), -40f * DegToRad, 40f * DegToRad);
                CurrentAction = 0;
                if (++_attackTicks >= 15)
                    FinishServerAttack();
                return;
            }

            if (_visualState == RavenVisualState.Rush &&
                (_attackTarget == null || !ReferenceEquals(_attackTarget.World, World)))
            {
                FinishServerAttack();
                return;
            }

            Matrix rotation = Matrix.CreateRotationX(_pitch) * Matrix.CreateRotationZ(_yaw);
            _flightPosition += Vector3.Transform(
                new Vector3(0f, -Math.Max(0f, _attackVelocity), 0f), rotation);

            if (_visualState == RavenVisualState.Rush)
            {
                Vector3 target = _attackTarget.WorldPosition.Translation + new Vector3(0f, 0f, 50f);
                Vector3 delta = target - _flightPosition;
                float horizontal = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                float wantedYaw = MathF.Atan2(delta.X, -delta.Y);
                float wantedPitch = -MathF.Atan2(delta.Z, Math.Max(1f, horizontal));
                float maxTurn = _attackVelocity * DegToRad;
                _yaw += Math.Clamp(WrapAngle(wantedYaw - _yaw), -maxTurn, maxTurn);
                _pitch += Math.Clamp(wantedPitch - _pitch, -maxTurn, maxTurn);
                _attackVelocity += _attackGravity;
                _attackGravity += 0.2f;
                if (delta.LengthSquared() < 20f * 20f || ++_attackTicks > 20)
                {
                    _visualState = RavenVisualState.Escape;
                    _pitch = -45f * DegToRad;
                    _attackTicks = 0;
                }
            }
            else if (_visualState == RavenVisualState.Escape)
            {
                float dx = ownerWorld.X - _flightPosition.X;
                float dy = ownerWorld.Y - _flightPosition.Y;
                _attackVelocity = Math.Max(0f, _attackVelocity - 1f);
                if (dx * dx + dy * dy >= 250f * 250f || ++_attackTicks >= 45)
                    FinishServerAttack();
            }
            CurrentAction = 3;
        }

        private void EmitNativeAttackEffects()
        {
            var fx = World?.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed)
                return;

            // These are original CSPetDarkSpirit::AttackEffect primitives.
            // Unsupported native DarkLordSkill subtype 3 is intentionally
            // not substituted with an unrelated client-side effect.
            if (_visualState == RavenVisualState.Rush &&
                _attackTicks > 3 && (_attackTicks & 1) == 1 &&
                _flightPosition.Z > _owner.WorldPosition.Translation.Z + 100f)
            {
                var bones = GetBoneTransforms();
                if (bones != null && bones.Length > 6)
                {
                    Vector3 origin = Vector3.Transform(new Vector3(50f, 0f, 0f),
                        bones[6] * WorldPosition);
                    // MODEL_AIR_FORCE subtype 0 is intentionally a short
                    // world-space gust (15 native ticks). Native expands and
                    // fades it; it does NOT remain attached to the flying pet.
                    fx.CreateEffect(ClassicFxEffectType.AirForce, origin, Angle,
                        NativeRavenLight, ClassicFxOwner.FromWorldObject(this));
                }
            }
            else if (_visualState == RavenVisualState.Magic && _attackTicks < 15)
            {
                var bones = GetBoneTransforms();
                if (bones == null || bones.Length == 0)
                    return;
                // Original uses every other non-dummy BMD bone. Sample a
                // bounded subset to avoid overwhelming mobile particle pools.
                int count = Math.Min(66, bones.Length);
                int stride = count > 24 ? 4 : 2;
                for (int bone = _attackTicks & 1; bone < count; bone += stride)
                {
                    Vector3 origin = Vector3.Transform(Vector3.Zero,
                        bones[bone] * WorldPosition);
                    fx.CreateParticle(ClassicTextureIds.BitmapLight, origin, Angle,
                        new Vector3(1f, 0.6f, 0.4f), subType: 6, scale: 1.3f);
                }
            }
        }

        /// <summary>
        /// MuMain ZzzObject.cpp MODEL_DARK_SPIRIT: mesh 3 always renders.
        /// Level 0-19 uses mesh 2, 20-39 mesh 1 and 40+ mesh 0.
        /// This affects only the Raven and does not alter shared BMD assets.
        /// </summary>
        protected override bool IsNativeMeshVisible(int mesh)
        {
            if (mesh == 3 || mesh > 3)
                return true;

            return mesh == (_nativePetLevel >= 40 ? 0 : _nativePetLevel >= 20 ? 1 : 2);
        }

        public override void DrawModel(bool isAfterDraw)
        {
            base.DrawModel(isAfterDraw);

            // Native high-level Raven mesh 0: RENDER_CHROME | RENDER_BRIGHT
            // with BodyLight (0.3, 0.6, 1). Do not apply to every pet level.
            if (!isAfterDraw && _equipped && !Hidden && _nativePetLevel >= 40)
                DrawClassicChromePass(0, new Vector3(0.3f, 0.6f, 1f));
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
                _previousFlightPosition = _flightPosition; // never interpolate a recovery teleport
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
