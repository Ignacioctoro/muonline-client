#nullable enable
// MU Season 6 Summoner Lightning Shock, native skill ID 230.
// A transient object dispatches MODEL_LIGHTNING_SHOCK to ClassicFX
// and immediately retires; effect lifetime remains in the shared pool.
using System;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(230)]
    public sealed class LightningShockSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            if (context.Caster == null || context.World == null ||
                context.World.ClassicFx == null ||
                !context.World.ClassicFx.Enabled ||
                context.World.ClassicFx.IsDisposed)
                return null;
            return new ClassicFxLightningShockCastVisual(context.Caster);
        }
    }

    internal sealed class ClassicFxLightningShockCastVisual : WorldObject
    {
        private readonly WalkerObject _caster;

        internal ClassicFxLightningShockCastVisual(WalkerObject caster)
        {
            _caster = caster ??
                throw new ArgumentNullException(nameof(caster));
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f), new Vector3(250f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Status != GameControlStatus.Ready) return;
            if (World != null &&
                ReferenceEquals(_caster.World, World) &&
                _caster.Status == GameControlStatus.Ready)
                World.ClassicFx?.CreateLightningShock(_caster,
                    _caster.WorldPosition.Translation,
                    _caster.TotalAngle, subType: 0);

            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
