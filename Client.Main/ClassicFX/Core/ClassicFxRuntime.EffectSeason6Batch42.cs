// ClassicFX S6 Batch 42 — pinned MuMain 21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
// BITMAP_WATERFALL_4: ZzzEffect.cpp CreateEffect + MoveHandlers.cpp.
// Original RenderEffects sprite call is commented out: orbit sprites are
// emitted in Move_BITMAP_WATERFALL_4, not via a second BMD renderer.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.Models;
using Client.Main.Objects;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private static bool IsS6Batch42LogicalType(ClassicFxEffectType type, int sub) =>
            type == ClassicFxEffectType.WaterfallOrbit && sub == 0;

        private void InitializeS6Batch42Logical(
            ref Vector3 angle, ref float scale, ref float distance,
            out float life)
        {
            // Native CreateEffect(BITMAP_WATERFALL_4).
            life = 80f;
            scale = 0.1f + Random.Modulo(20) * 0.01f;
            angle.X = Random.Modulo(360);
            distance = 5f + Random.Modulo(10);
            // Native Timer is random 0..359 and is stored in EffectState.Phase.
        }

        private bool MoveS6Batch42Logical(ref EffectState e, float f)
        {
            // Native move is a bone-relative orbit, not a terrain bitmap.
            // Keep a valid live owner; don't keep spraying particles at a
            // stale world-object pointer after its removal.
            if (e.Owner.WorldObject is not ModelObject ||
                !ReferenceEquals(e.Owner.WorldObject.World, World) ||
                e.Owner.WorldObject.Status != GameControlStatus.Ready)
                return false;

            if (!TryGetOwnerBonePosition(e.Owner, e.NativePkKey, out Vector3 bone))
                return false;

            e.Phase += 0.1f * f;
            e.Velocity += 2.1f * f; // native Distance
            e.Angle.X += 0.7f * f;
            e.Light *= MathF.Pow(0.95f, f);

            // CreateSprite is transient and intended for reference ticks.
            // Prevent multiplying emissions with 60/120/144 FPS.
            if (!Clock.AdvancedReferenceFrame) return true;

            float sin = MathF.Sin(e.Phase);
            float cos = MathF.Cos(e.Phase);
            float radius = e.Velocity;
            Vector3 p0 = bone + new Vector3(cos * radius, 0f, sin * radius);
            Vector3 p1 = bone + new Vector3(sin * radius, cos * radius, 0f);
            Vector3 p2 = bone + new Vector3(0f, sin * radius, cos * radius);

            CreateSprite(ClassicTextureIds.BitmapWaterfall4,
                p0, e.Scale, e.Light, e.Owner, e.Angle.X);
            CreateSprite(ClassicTextureIds.BitmapWaterfall4,
                p1, e.Scale, e.Light, e.Owner, e.Angle.X);
            CreateSprite(ClassicTextureIds.BitmapWaterfall4,
                p2, e.Scale, e.Light, e.Owner, e.Angle.X);
            return true;
        }
    }
}
