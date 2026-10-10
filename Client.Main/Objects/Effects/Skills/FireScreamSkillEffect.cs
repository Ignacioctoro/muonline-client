#nullable enable
// BroyalMU: MU S6 Dark Lord Fire Scream (skill 78).
// Native ZzzCharacter.cpp emits 3 DarkScream + 3 DarkScreamFire
// without a separate gameplay projectile. One-shot ClassicFX cast.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(78)]
    public sealed class FireScreamSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;

            return new ClassicFxFireScreamCastVisual(context.Caster);
        }
    }

    internal sealed class ClassicFxFireScreamCastVisual : WorldObject
    {
        private readonly WalkerObject _caster;
        private bool _started;

        public ClassicFxFireScreamCastVisual(WalkerObject caster)
        {
            _caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Position = caster.WorldPosition.Translation;
            IsTransparent = true;
            Interactive = false;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f), new Vector3(250f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready || _started)
                return;

            _started = true;
            if (World != null && World.ClassicFx != null &&
                World.ClassicFx.Enabled &&
                ReferenceEquals(_caster.World, World) &&
                _caster.Status == GameControlStatus.Ready)
            {
                Vector3 origin = _caster.WorldPosition.Translation;
                Vector3 originalAngle = _caster.TotalAngle;
                ClassicFxOwner owner = ClassicFxOwner.FromWorldObject(_caster);

                // 3 native launch positions; unlike ZzzCharacter.cpp,
                // never temporarily mutate the live player's angle or
                // world position (important for movement prediction).
                for (int slot = -1; slot <= 1; slot++)
                {
                    Vector3 angle = originalAngle;
                    angle.Z += MathHelper.ToRadians(slot * 10f);
                    Matrix yaw = Matrix.CreateRotationZ(angle.Z);
                    Vector3 center = origin;
                    if (slot != 0)
                    {
                        center += Vector3.TransformNormal(
                            new Vector3(slot * 80f, 0f, 0f), yaw);
                    }

                    World.ClassicFx.CreateEffect(
                        ClassicFxEffectType.DarkScream, center,
                        angle, Vector3.One, owner);
                    World.ClassicFx.CreateEffect(
                        ClassicFxEffectType.DarkScreamFire, center,
                        angle, Vector3.One, owner);
                }
            }

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
