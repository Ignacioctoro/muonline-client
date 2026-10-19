using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Controllers;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        // Port of the *working* PowerSlashEffect, not the stationary Magic2 BMD.
        private const float MgSlashSeconds = 0.30f;
        private const float MgSlashSpeed = 1800f;
        private static readonly float[] MgSpread = { -40f, -20f, 0f, 20f, 40f };
        private static readonly float[] MgTrail = { 0f, 55f, 110f, 165f, 220f };
        private static readonly float[] MgSize = { 1.55f, 1.35f, 1.18f, 1f, 0.85f };
        private static readonly float[] MgAlpha = { 1f, .90f, .75f, .58f, .38f };

        private struct MgSlashArc
        {
            public Vector3 Start;
            public Vector3 Direction;
            public float Age;
            public bool Live;
        }

        // Up to 12 overlapping casts, no transient WorldObjects or SpriteBatch.
        private readonly MgSlashArc[] _mgSlashArcs = new MgSlashArc[60];
        private bool _mgSlashReported;
        private object _mgSlashLastCaster;
        private double _mgSlashLastCastMs = -1d;
        private ILogger _mgSlashLogger;

        public bool EmitMgPowerSlash(Vector3 origin, Vector3 casterAngle, object caster)
        {
            if (_disposed || !Enabled) return false;

            // Local prediction and server 0x1E/0x19 echoes can all visit the
            // visual registry. Suppress only the *same caster* in the short
            // echo window; do not double the five-projectile sequence.
            double now = Clock.WorldTimeMilliseconds;
            if (caster != null && ReferenceEquals(_mgSlashLastCaster, caster) &&
                _mgSlashLastCastMs >= 0d &&
                now >= _mgSlashLastCastMs && now - _mgSlashLastCastMs < 100d)
                return true;

            _mgSlashLogger ??= MuGame.AppLoggerFactory?
                .CreateLogger("ClassicFX.PowerSlash");
            bool shinyReady = Textures.TryGet(
                ClassicTextureIds.BitmapShiny + 1, out _);
            bool flareReady = Textures.TryGet(
                ClassicTextureIds.BitmapLight, out _);
            if (!shinyReady || !flareReady)
            {
                if (!_mgSlashReported)
                    _mgSlashLogger?.LogWarning(
                        "[ClassicFX][PowerSlash] Missing texture: Shiny02={ShinyReady}, flare01={FlareReady}.",
                        shinyReady, flareReady);
                _mgSlashReported = true;
                return false;
            }

            int count = 0;
            foreach (float spread in MgSpread)
            {
                int slot = -1;
                for (int i = 0; i < _mgSlashArcs.Length; ++i)
                    if (!_mgSlashArcs[i].Live) { slot = i; break; }
                if (slot < 0) break;

                float yaw = casterAngle.Z + MathHelper.ToRadians(spread);
                Vector3 dir = new Vector3(MathF.Sin(yaw), -MathF.Cos(yaw), 0f);
                _mgSlashArcs[slot] = new MgSlashArc {
                    Start = origin + new Vector3(0f, 0f, 45f) + dir * 45f,
                    Direction = dir, Live = true
                };
                ++count;
            }
            if (count > 0)
            {
                _mgSlashLastCaster = caster;
                _mgSlashLastCastMs = now;
                if (!_mgSlashReported)
                    _mgSlashLogger?.LogInformation(
                        "[ClassicFX][PowerSlash] Emitted {ArcCount} moving ClassicFX arcs.",
                        count);
            }
            else if (!_mgSlashReported)
            {
                _mgSlashLogger?.LogWarning(
                    "[ClassicFX][PowerSlash] Arc pool exhausted; no projectiles emitted.");
            }
            _mgSlashReported = true;
            return count > 0;
        }

        private void MoveMgPowerSlash()
        {
            float dt = MathHelper.Clamp(Clock.DeltaSeconds, 0f, 0.1f);
            if (dt <= 0f) return;

            var camera = Camera.Instance;
            var viewport = GraphicsManager.Instance.GraphicsDevice.Viewport;
            // SpriteBatch used pixel-space sizes. Convert to 3D billboard scale.
            float focal = MathF.Abs(camera.Projection.M22) * viewport.Height * 0.5f;
            if (focal <= 0f) return;

            Vector3 blue = new Vector3(185f / 255f, 220f / 255f, 1f);
            for (int i = 0; i < _mgSlashArcs.Length; ++i)
            {
                if (!_mgSlashArcs[i].Live) continue;
                ref var arc = ref _mgSlashArcs[i];
                arc.Age += dt;
                if (arc.Age >= MgSlashSeconds) { arc = default; continue; }

                Vector3 head = arc.Start + arc.Direction * (MgSlashSpeed * arc.Age);
                float fade = MathHelper.Lerp(.35f, 1f,
                    MathHelper.Clamp(1f - arc.Age / MgSlashSeconds, 0f, 1f));
                for (int orb = 0; orb < MgTrail.Length; ++orb)
                {
                    Vector3 spot = head - arc.Direction * MgTrail[orb] +
                        new Vector3(0f, 0f, 55f);
                    float depth = -Vector3.Transform(spot, camera.View).Z;
                    if (depth <= 1f) continue;
                    float distance = MathF.Max(.1f,
                        Vector3.Distance(camera.Position, spot));
                    float worldPerPixel = Constants.TERRAIN_SIZE *
                        Constants.RENDER_SCALE * depth / (distance * focal);
                    float scale = MgSize[orb] * worldPerPixel;
                    float alpha = MgAlpha[orb] * fade;
                    // The exact old layers: two counter-rotating Shiny02
                    // textures followed by the blue-white flare01 halo.
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1, spot,
                        11.4f * scale, new Vector3(alpha),
                        rotation: MathHelper.ToDegrees(arc.Age * 8f));
                    CreateSprite(ClassicTextureIds.BitmapShiny + 1, spot,
                        11.4f * scale * .82f, new Vector3(alpha * .75f),
                        rotation: MathHelper.ToDegrees(-arc.Age * 10f));
                    CreateSprite(ClassicTextureIds.BitmapLight, spot,
                        14.8f * scale, blue * (alpha * .70f));
                }
            }
        }

        private void ClearMgPowerSlash()
        {
            Array.Clear(_mgSlashArcs);
            _mgSlashReported = false;
            _mgSlashLastCaster = null;
            _mgSlashLastCastMs = -1d;
        }
    }
}
