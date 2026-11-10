using Client.Main.ClassicFX.Data;
using Client.Main.Graphics;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private const float GiganticStormBoltHeight = 800f;

        // MuMain ZzzCharacter.cpp places five BITMAP_JOINT_THUNDER
        // effects 200 units from the caster, 72 degrees apart.
        public void CreateGiganticStorm(Vector3 origin, Vector3 angle)
        {
            if (_disposed || !Enabled)
                return;

            Vector3 jointAngle = angle * (180f / MathHelper.Pi);
            for (int i = 0; i < 5; i++)
            {
                if (!Pools.Effects.TryAcquire(out ClassicFxHandle handle))
                    break;

                float yaw = angle.Z + MathHelper.ToRadians(i * 72f);
                Vector3 position = origin + Vector3.TransformNormal(
                    new Vector3(0f, 200f, 0f), Matrix.CreateRotationZ(yaw));
                if (World.Terrain is { } terrain)
                    position.Z = terrain.RequestTerrainHeight(position.X, position.Y);

                _effects[handle.Index] = new EffectState
                {
                    Type = ClassicFxEffectType.GiganticStormThunder,
                    Position = position,
                    StartPosition = position + new Vector3(0f, 0f, GiganticStormBoltHeight),
                    Angle = jointAngle,
                    LifeTime = 20f
                };
            }
        }

        private void MoveGiganticStormThunder(ref EffectState effect)
        {
            if (!Clock.AdvancedReferenceFrame)
                return;

            Vector3 start = effect.StartPosition + new Vector3(
                Random.Modulo(100) - 50f, Random.Modulo(100) - 50f, 0f);
            Vector3 end = effect.Position + new Vector3(
                Random.Modulo(100) - 50f, Random.Modulo(100) - 50f, 0f);
            Vector3 light = new Vector3(0.45f, 0.45f, 0.7f);

            if (Random.Modulo(2) == 0)
            {
                CreateJoint(ClassicTextureIds.BitmapJointThunder, ExtendGiganticStormBoltAboveView(start, end), end,
                    effect.Angle, subType: 16, scale: 10f + Random.Modulo(400) / 10f);
            }
            if (effect.LifeTime > 10f && Random.Modulo(2) == 0)
                CreateEffect(ClassicFxEffectType.MagicGround2,
                    end, effect.Angle, light, ClassicFxOwner.None, subType: 11);
            if (Random.Modulo(4) == 0)
                CreateParticle(ClassicTextureIds.BitmapSmoke, end, effect.Angle,
                    light, subType: 54, scale: 2.8f);
            if (Random.Modulo(4) == 0)
                CreateEffect(Random.Modulo(2) == 0 ? ClassicFxEffectType.Stone1 : ClassicFxEffectType.Stone2,
                    effect.Position, effect.Angle * (MathHelper.Pi / 180f), light,
                    ClassicFxOwner.None, subType: 13);
            if (effect.LifeTime > 5f && Random.Modulo(5) == 0)
                CreateEffect(ClassicFxEffectType.ChromeEnergyGround,
                    end, effect.Angle, new Vector3(0.15f, 0.15f, 0.4f), ClassicFxOwner.None);
        }

        private static Vector3 ExtendGiganticStormBoltAboveView(Vector3 start, Vector3 impact)
        {
            const float aboveTopEdge = 1.15f;
            var camera = Camera.Instance;
            Matrix view = camera.View;
            Vector3 lower = Vector3.Transform(impact, view);
            if (-lower.Z <= camera.ViewNear)
                return start;

            Vector3 upper = Vector3.Transform(start, view);
            float minDepth = MathHelper.Max(camera.ViewNear, -lower.Z * 0.5f);
            if (-upper.Z < minDepth)
            {
                // Preserve the impact's screen column when the nominal start is too close.
                upper.X = lower.X * minDepth / -lower.Z;
                upper.Z = -minDepth;
            }

            // At this depth, projection maps this Y to 15% beyond the top clip edge.
            upper.Y = MathHelper.Max(upper.Y, aboveTopEdge * -upper.Z / camera.Projection.M22);
            return Vector3.Transform(upper, Matrix.Invert(view));
        }
    }
}
