using System;
using Client.Main.ClassicFX.Core;
using Client.Main.ClassicFX.Primitives;
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

            Position = _owner.WorldPosition.Translation;
            bool hidden = _owner.Hidden || _owner.IsDead;
            Hidden = hidden;
            if (hidden)
            {
                ReleaseJoints();
                return;
            }

            ClassicFxRuntime currentRuntime = World.ClassicFx;
            if (!ReferenceEquals(_runtime, currentRuntime))
            {
                ReleaseJoints();
                _runtime = currentRuntime;
            }
            EnsureJoints();
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
                    created++;
            }

            if (!_logged && created > 0)
            {
                _logged = true;
                Console.WriteLine($"[ClassicFX] SPEARSKILL {_subType}: {created} joints nuevos");
            }
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
