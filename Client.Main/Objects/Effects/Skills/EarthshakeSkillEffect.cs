// Dark Lord Earthshake (62) - source MuMain@21728b1e5.
// GOBoid.cpp RenderDarkHorseSkill / MoveMount(MODEL_DARK_HORSE).
// No additional renderer: native resources travel through ClassicFX.
using System;
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
    /// Native-time sampling schedules the wave, FuryStrike Kind=2 and rock
    /// bursts. The stone radius follows the horse animation, not network
    /// arrival time. Actual foot contact is gated by the
    /// horse's fractional BMD frame. Adapt the native 8..9.5 impact
    /// window when the installed horse BMD has fewer animation keys.
    /// </summary>
    internal sealed class ClassicFxEarthshakeSequence : WorldObject
    {
        private const int HorseAttackAction = 3;
        private const float ReferenceRate = 25f;
        private readonly PlayerObject _caster;
        private readonly VehicleObject _horse;
        private float _elapsed;
        private float _tickRemainder;
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

            // Original MuMain checks frame 8..9.5 on action 3. However,
            // Data_Broyal/Skill/DarkHorse.bmd action 3 has only 7 keys.
            // ModelObject's one-shot animation holds at (keys - 2) = 5,
            // so the original numeric frame range is unreachable.
            // Preserve it for native-length models; for shortened BMDs,
            // use the same late 1.5-frame portion of the actual attack.
            // Exclude the final held frame so no rocks spawn forever.
            var horseAction = _horse.Model?.Actions != null &&
                _horse.CurrentAction < _horse.Model.Actions.Length
                ? _horse.Model.Actions[_horse.CurrentAction] : null;
            if (horseAction == null)
                return;
            int usableFrames = horseAction.LockPositions
                ? horseAction.NumAnimationKeys - 1
                : horseAction.NumAnimationKeys;
            float heldFrame = MathF.Max(0f, usableFrames - 2f);
            bool shortenedAttack = heldFrame < 9.5f;
            float impactBegin = shortenedAttack
                ? MathF.Max(0f, heldFrame - 1.5f) : 8f;
            float impactEnd = shortenedAttack
                ? heldFrame - 0.01f : 9.5f;
            bool inImpactWindow = usableFrames >= 4 &&
                frame >= impactBegin && frame <= impactEnd;

            // Main creates FuryStrike when WeaponLevel reaches 19, about
            // four horse-action ticks before native frame 8. Preserve that
            // relationship for a shorter installed BMD. Use the observed
            // horse frame rather than a counter started on packet arrival:
            // late 0x1E packets must not postpone or entirely skip FuryStrike.
            float actionPlaySpeed = horseAction.PlaySpeed == 0f
                ? 1f : horseAction.PlaySpeed;
            float furyFrame = Math.Clamp(
                19f * actionPlaySpeed + impactBegin - 8f,
                0f, impactBegin);

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

            // Sample rand_fps_check(2) at the 25-Hz native reference rate.
            // MuMain clamps FPS_ANIMATION_FACTOR to <= 1 at low FPS, so
            // do not replay many emission ticks after an Android hitch.
            // Stone distance is calculated from the actual horse frame below.
            _tickRemainder += MathF.Min(1f, dt * ReferenceRate);
            int advanced = (int)_tickRemainder;
            _tickRemainder -= advanced;
            for (int tick = 0; tick < advanced; tick++)
            {
                if (!_furyTriggered && frame >= furyFrame)
                {
                    // Last positional argument to CreateEffect is Kind=2
                    // in MuMain; ClassicFX uses subType to carry that Kind.
                    fx.CreateEffect(ClassicFxEffectType.FuryStrike,
                        origin, angle, Vector3.One, horseOwner,
                        subType: 2);
                    _furyTriggered = true;
                }

                // rand_fps_check(2): half probability at each native tick.
                if (inImpactWindow && fx.Random.Modulo(2) == 0)
                    EmitGroundStones(fx, origin, angle,
                        GetNativeStoneRadius(frame, impactBegin, actionPlaySpeed));
            }

            // GOBoid applies EarthQuake on EVERY impact-window render,
            // independent of whether the probabilistic rocks are spawned.
            // Its amplitude is a small camera-pitch change in DEGREES;
            // positional Camera.Shake would be an invented visual.
            if (inImpactWindow)
                Client.Main.Graphics.Camera.Instance.SetNativeEarthQuakePitch(
                    (fx.Random.Modulo(3) - 3) * 0.7f);

            // Cleanup guard only, never used to time the impact.
            if (_elapsed >= 3f)
                Finish();
        }

        private static float GetNativeStoneRadius(
            float horseFrame, float impactBegin, float actionPlaySpeed)
        {
            // GOBoid: at native 25 Hz, WeaponLevel reaches 19 around
            // frame 19 * PlaySpeed and resets to -3. At native tick 24
            // (frame >= 8 with PlaySpeed 0.34), its value is 2.
            // A late network effect must not restart this counter from 0.
            // Map the shortened BMD's contact window onto native 8..9.5
            // while retaining the original integer (WeaponLevel / 2).
            float nativeFrame = horseFrame + (8f - impactBegin);
            int nativeTick = (int)MathF.Ceiling(nativeFrame / actionPlaySpeed);
            int weaponLevel = Math.Max(0, nativeTick - 22);
            return 150f * (weaponLevel / 2);
        }

        private void EmitGroundStones(ClassicFxRuntime fx,
            Vector3 origin, Vector3 horseAngle, float radius)
        {
            if (World?.Terrain == null)
                return;

            // GOBoid: six stones at 60-degree increments around the horse.
            // The random initial angle and BMD variation remain native.
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

                // RenderDarkHorseSkill never rejects NoMove/NoGround/Water
                // tiles. That extra ClassicFX test was silently removing
                // stones (sometimes all six) near monster/map boundaries.
                // Only skip coordinates outside the allocated terrain.
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
