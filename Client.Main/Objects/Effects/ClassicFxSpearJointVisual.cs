using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Primitives;
using Client.Main.Graphics;
using Client.Main.Controllers;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects
{
    /// <summary>
    /// Buff lifecycle adapter. The original MODEL_SPEARSKILL creation,
    /// movement and crossed-tail rendering live exclusively in ClassicFX.
    /// This WorldObject does not duplicate the renderer or geometry.
    /// Five joints are created, just as in the legacy MU aura.
    /// </summary>
    public sealed class ClassicFxSpearJointVisual : WorldObject
    {
        private const int JointCount = 5;
        private readonly PlayerObject _owner;
        private readonly int _subType;
        private readonly ClassicFxHandle[] _handles = new ClassicFxHandle[JointCount];
        private ClassicFxRuntime _runtime;
        private bool _logged;
        private int _diagnosticFrames;
        private int _createdJoints;
        private bool _diagnosticEmitted;
        private int _runtimeFrameSamples;
        private float _stableFrameFactor;

        public PlayerObject Owner => _owner;
        public int SubType => _subType;

        public ClassicFxSpearJointVisual(PlayerObject owner, int subType)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            if (subType != 0 && subType != 4)
                throw new ArgumentOutOfRangeException(nameof(subType));
            _subType = subType;
            Position = owner.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            AffectedByTransparency = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-180f, -180f, -40f),
                new Vector3(180f, 180f, 330f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready)
                return;

            if (_owner.Status != GameControlStatus.Ready ||
                _owner.World == null || World == null ||
                !ReferenceEquals(World, _owner.World))
            {
                ReleaseJoints();
                return;
            }

            if (MuGame.Instance?.IsActive == false)
            {
                _runtimeFrameSamples = 0;
                _stableFrameFactor = 0f;
                ReleaseJoints();
                return;
            }

            Position = _owner.WorldPosition.Translation;
            bool hidden = _owner.Hidden || _owner.IsDead;
            Hidden = hidden;
            if (hidden)
            {
                _runtimeFrameSamples = 0;
                _stableFrameFactor = 0f;
                ReleaseJoints();
                return;
            }

            ClassicFxRuntime currentRuntime = World.ClassicFx;
            if (!ReferenceEquals(_runtime, currentRuntime))
            {
                ReleaseJoints();
                _runtime = currentRuntime;
                _diagnosticFrames = 0;
                _createdJoints = 0;
                _diagnosticEmitted = false;
                _runtimeFrameSamples = 0;
                _stableFrameFactor = 0f;
            }
            EnsureJoints();
            RefreshPersistentTailCapacity();
            DiagnoseTailHistoryOnce();
        }

        private void EnsureJoints()
        {
            if (_runtime == null || !_runtime.Enabled || _runtime.IsDisposed)
                return;

            Vector3 position = _owner.WorldPosition.Translation;
            Vector3 angle = _owner.TotalAngle;
            ClassicFxOwner target = ClassicFxOwner.FromWorldObject(_owner);
            int created = 0;
            for (int i = 0; i < JointCount; i++)
            {
                if (_handles[i].IsValid &&
                    _runtime.TryGetJoint(_handles[i], out _))
                    continue;

                // MODEL_SPEARSKILL subtype 0/4 uses the native owner,
                // initial width, native MoveJoint and native RenderJoint.
                _handles[i] = _runtime.CreateJoint(
                    ClassicFxRuntime.ClassicModelSpearSkill,
                    position, position, angle,
                    subType: _subType,
                    target: target,
                    scale: 20f);
                if (_handles[i].IsValid)
                {
                    created++;
                    _createdJoints++;
                }
            }

            if (!_logged && created > 0)
            {
                _logged = true;
                Console.WriteLine($"[ClassicFX] SPEARSKILL {_subType}: {created} joints nuevos");
            }
        }

        // The first frames after a warp can be slow while the world loads.
        // CreateJoint takes MaxTails = 30 / FrameFactor at creation time;
        // if that one frame is slow, an otherwise fast client gets a
        // permanently tiny trail. Follow the settled frame factor instead,
        // resizing only these persistent auras' preallocated histories.
        private void RefreshPersistentTailCapacity()
        {
            if (_runtime == null || _runtime.IsDisposed)
                return;

            float sample = _runtime.Clock.FrameFactor;
            if (!float.IsFinite(sample) || sample <= 0f)
                return;

            if (_runtimeFrameSamples == 0)
                _stableFrameFactor = sample;
            else
                _stableFrameFactor += (sample - _stableFrameFactor) * 0.2f;
            _runtimeFrameSamples++;

            // Allow startup spikes to decay before changing tail capacity.
            // Recheck only every eight frames; no allocations or logging.
            if (_runtimeFrameSamples < 12 || (_runtimeFrameSamples % 8) != 0)
                return;

            for (int i = 0; i < _handles.Length; i++)
            {
                if (_handles[i].IsValid)
                    _runtime.GrowPersistentSpearTrail(_handles[i], _stableFrameFactor);
            }
        }

        // Single measurement per aura/world in DEBUG builds. The cast and
        // persistent visuals remain unchanged; this distinguishes repeatedly
        // recreated joints from valid geometry hidden by a render path.
        private void DiagnoseTailHistoryOnce()
        {
#if DEBUG
            if (_diagnosticEmitted || ++_diagnosticFrames < 50)
                return;

            _diagnosticEmitted = true;
            int active = 0;
            int candidateSegments = 0;
            int drawableSegments = 0;
            int minTails = int.MaxValue;
            int maxTails = 0;
            int maxCapacity = 0;

            for (int i = 0; i < _handles.Length; i++)
            {
                if (!_handles[i].IsValid ||
                    !_runtime.TryGetJoint(_handles[i], out ClassicJoint joint))
                    continue;

                active++;
                minTails = Math.Min(minTails, joint.NumTails);
                maxTails = Math.Max(maxTails, joint.NumTails);
                maxCapacity = Math.Max(maxCapacity, joint.MaxTails);
                if (joint.Tails == null || joint.Tails.Length < 8)
                    continue;

                int segmentCount = Math.Min(joint.NumTails,
                    Math.Min(ClassicJoint.MaxTailSegments - 1,
                        joint.Tails.Length / ClassicJoint.VerticesPerTail - 1));
                candidateSegments += Math.Max(0, segmentCount);
                for (int seg = 0; seg < segmentCount; seg++)
                {
                    int offset = seg * ClassicJoint.VerticesPerTail;
                    Vector3 midpoint0 = (joint.Tails[offset] + joint.Tails[offset + 1]) * 0.5f;
                    Vector3 midpoint1 = (joint.Tails[offset + 4] + joint.Tails[offset + 5]) * 0.5f;
                    if (float.IsFinite(midpoint0.X) && float.IsFinite(midpoint0.Y) &&
                        float.IsFinite(midpoint0.Z) && float.IsFinite(midpoint1.X) &&
                        float.IsFinite(midpoint1.Y) && float.IsFinite(midpoint1.Z) &&
                        midpoint0.LengthSquared() >= 1f &&
                        midpoint1.LengthSquared() >= 1f &&
                        Vector3.DistanceSquared(midpoint0, midpoint1) <= 3600f)
                        drawableSegments++;
                }
            }

            Console.WriteLine(
                $"[ClassicFX][SPEAR_DIAG] preset={GraphicsQualityManager.ActivePreset} " +
                $"subtype={_subType} active={active}/5 created={_createdJoints} " +
                $"tails={ (active > 0 ? minTails : 0) }-{maxTails} " +
                $"capacity={maxCapacity} drawable={drawableSegments}/{candidateSegments}");
#endif
        }

        private void ReleaseJoints()
        {
            if (_runtime != null && !_runtime.IsDisposed)
            {
                for (int i = 0; i < _handles.Length; i++)
                {
                    if (_handles[i].IsValid)
                        _runtime.ReleaseJoint(_handles[i]);
                    _handles[i] = ClassicFxHandle.Invalid;
                }
            }
            else
            {
                for (int i = 0; i < _handles.Length; i++)
                    _handles[i] = ClassicFxHandle.Invalid;
            }
        }

        public override void Dispose()
        {
            ReleaseJoints();
            _runtime = null;
            base.Dispose();
        }
    }
}
