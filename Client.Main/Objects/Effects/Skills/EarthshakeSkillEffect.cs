// Dark Lord Earthshake (62) - source MuMain@21728b1e5.
// GOBoid.cpp RenderDarkHorseSkill / MoveMount(MODEL_DARK_HORSE).
// No additional renderer: native resources travel through ClassicFX.
using System;
using Client.Data.ATT;
using Client.Main.ClassicFX.Core;
using Client.Main.Core.Utilities;
using Client.Main.Models;
using Client.Main.Objects.Player;
using Client.Main.Objects.Vehicle;
using Microsoft.Xna.Framework;

namespace Client.Main.Objects.Effects.Skills
{
    [SkillVisualEffect(62)]
    public sealed class EarthshakeSkillEffect : ISkillVisualEffect
    {
        public WorldObject? CreateEffect(SkillEffectContext context)
        {
            // MU's Earthshake is an area attack. The 0x19 individual
            // target packet can accompany 0x1E; never render twice.
            if (!context.IsAreaSkillAnimation ||
                context.Caster is not PlayerObject caster ||
                context.World == null ||
                !ReferenceEquals(caster.World, context.World) ||
                caster.Vehicle is not VehicleObject horse ||
                horse.ItemIndex != 0 || horse.Hidden ||
                context.World.ClassicFx is not ClassicFxRuntime fx ||
                !fx.Enabled || fx.IsDisposed)
                return null;

            return new ClassicFxEarthshakeSequence(caster, horse);
        }
    }

    /// <summary>
    /// Presentation-only counterpart of RenderDarkHorseSkill.
    /// A native 25-Hz logical counter schedules periodic wave, FuryStrike
    /// Kind=2 and rock bursts. Actual foot contact is gated by the
    /// animated horse's fractional BMD frame (8 to 9.5), not elapsed ms.
    /// </summary>
    internal sealed class ClassicFxEarthshakeSequence : WorldObject
    {
        private const int HorseAttackAction = 3;
        private const float ReferenceRate = 25f;
        private readonly PlayerObject _caster;
        private readonly VehicleObject _horse;
        private float _elapsed;
        private float _tickRemainder;
        private int _weaponLevel;
        private bool _furyTriggered;
        private bool _sawHorseAttack;
        private bool _finished;

        internal ClassicFxEarthshakeSequence(PlayerObject caster, VehicleObject horse)
        {
            _caster = caster;
            _horse = horse;
            Position = caster.WorldPosition.Translation;
            Interactive = false;
            IsTransparent = true;
            BoundingBoxLocal = new BoundingBox(
                new Vector3(-250f), new Vector3(250f));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_finished || Status != GameControlStatus.Ready)
                return;

            ClassicFxRuntime fx = World?.ClassicFx;
            if (fx == null || !fx.Enabled || fx.IsDisposed ||
                _caster.Status != GameControlStatus.Ready ||
                _horse.Status != GameControlStatus.Ready ||
                !ReferenceEquals(_caster.World, World) ||
                !ReferenceEquals(_horse.World, World) ||
                _horse.Hidden || _horse.ItemIndex != 0)
            {
                Finish();
                return;
            }

            // 25 Hz is the original reference cadence. The animation itself
            // still advances through its BMD PlaySpeed independently of FPS.
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!float.IsFinite(dt) || dt <= 0f)
                return;
            _elapsed += dt;

            if (_horse.CurrentAction != HorseAttackAction)
            {
                if (_sawHorseAttack || _elapsed > 1.25f)
                    Finish();
                return;
            }

            _sawHorseAttack = true;
            float frame = _horse.NativeActionFrame;
            Vector3 origin = _horse.WorldPosition.Translation;
            Vector3 angle = _horse.TotalAngle;
            ClassicFxOwner horseOwner = ClassicFxOwner.FromWorldObject(_horse);

            // GOBoid's horse attack draws a native CreateBlur from bone 9:
            // one endpoint offset (60,0,0), the other at the bone origin.
            // Its owner is the CHARACTER, so ClassicFX merges samples into
            // a single time-varying trail instead of creating a new mesh.
            if (fx.TryTransformOwnerBonePosition(horseOwner, 9,
                    new Vector3(60f, 0f, 0f), out Vector3 blurEnd) &&
                fx.TryTransformOwnerBonePosition(horseOwner, 9,
                    Vector3.Zero, out Vector3 blurStart))
            {
                fx.CreateBlur(ClassicFxOwner.FromWorldObject(_caster),
                    blurEnd, blurStart, Vector3.One, 0);
            }

            // Native LastHorseWaveEffect is stored on the horse and
            // remains valid between casts. Do not restart its interval.
            if (_horse.TryConsumeDarkHorseWave(
                    gameTime.TotalGameTime.TotalSeconds))
            {
                fx.CreateEffect(ClassicFxEffectType.ShockWave,
                    origin, angle, Vector3.One, horseOwner,
                    subType: 0, scale: 20f);
            }

            // Native GOBoid misuses WeaponLevel as a render frame counter.
            // Convert that counter into reference ticks once per cast.
            _tickRemainder += dt * ReferenceRate;
            int advanced = Math.Min(6, (int)_tickRemainder);
            _tickRemainder -= advanced;
            for (int tick = 0; tick < advanced; tick++)
            {
                _weaponLevel++;
                if (!_furyTriggered && _weaponLevel == 19 &&
                    !(frame >= 8f && frame <= 9.5f))
                {
                    // Last positional argument to CreateEffect is Kind=2
                    // in MuMain; ClassicFX uses subType to carry that Kind.
                    fx.CreateEffect(ClassicFxEffectType.FuryStrike,
                        origin, angle, Vector3.One, horseOwner,
                        subType: 2);
                    _furyTriggered = true;
                    _weaponLevel = -3;
                }

                // rand_fps_check(2): half probability at each native tick.
                if (frame >= 8f && frame <= 9.5f &&
                    fx.Random.Modulo(2) == 0)
                    EmitGroundStones(fx, origin, angle);
            }

            // GOBoid applies EarthQuake on EVERY impact-window render,
            // independent of whether the probabilistic rocks are spawned.
            // Its amplitude is a small camera-pitch change in DEGREES;
            // positional Camera.Shake would be an invented visual.
            if (frame >= 8f && frame <= 9.5f)
                Client.Main.Graphics.Camera.Instance.SetNativeEarthQuakePitch(
                    (fx.Random.Modulo(3) - 3) * 0.7f);

            // Cleanup guard only, never used to time the impact.
            if (_elapsed >= 3f)
                Finish();
        }

        private void EmitGroundStones(ClassicFxRuntime fx,
            Vector3 origin, Vector3 horseAngle)
        {
            if (World?.Terrain == null)
                return;

            // GOBoid: six stones rotated around the horse at 60 degrees.
            // o->WeaponLevel is reset to -3 when FuryStrike Kind 2 fires.
            // C++ divides the integer WeaponLevel by 2 before scaling.
            float radius = 150f * (_weaponLevel / 2);
            float start = MathHelper.ToRadians(fx.Random.Modulo(360));
            const float step = MathHelper.TwoPi / 6f;
            for (int i = 0; i < 6; i++)
            {
                float radians = start + step * (i + 1);
                Vector3 pos = origin + new Vector3(
                    -MathF.Sin(radians) * radius,
                    MathF.Cos(radians) * radius, 0f);

                // Main sampled the gameplay height for the object.
                // Neffis draws another mesh height; use its rendered surface
                // for model placement, not blindly for every other effect.
                float x = pos.X, y = pos.Y;
                if (x < 0f || y < 0f ||
                    x >= Client.Data.Constants.TERRAIN_SIZE *
                         Client.Main.Constants.TERRAIN_SCALE ||
                    y >= Client.Data.Constants.TERRAIN_SIZE *
                         Client.Main.Constants.TERRAIN_SCALE)
                    continue;

                TWFlags flags = World.Terrain.RequestTerrainFlag(
                    (int)(x / Client.Main.Constants.TERRAIN_SCALE),
                    (int)(y / Client.Main.Constants.TERRAIN_SCALE));
                if ((flags & (TWFlags.NoMove | TWFlags.NoGround |
                              TWFlags.Water)) != 0)
                    continue;

                pos.Z = World.Terrain.RequestTerrainRenderHeight(x, y);

                fx.CreateEffect(fx.Random.Modulo(2) == 0
                        ? ClassicFxEffectType.GroundStoneModel
                        : ClassicFxEffectType.GroundStone2Model,
                    pos, horseAngle, Vector3.One, ClassicFxOwner.None);
            }
        }

        private void Finish()
        {
            _finished = true;
            if (Parent != null)
                Parent.Children.Remove(this);
            else
                World?.Objects.Remove(this);
            Dispose();
        }
    }
}
