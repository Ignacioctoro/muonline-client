// ClassicFX Blur/ObjectBlur port.
// Native: sven-n/MuMain src/source/Render/Effects/ZzzEffectBlurSpark.cpp
// References: CreateBlur, AddBlur, MoveBlurs, RenderBlurs,
// CreateObjectBlur, AddObjectBlur, MoveObjectBlurs, RenderObjectBlurs.
using System;
using Client.Main.ClassicFX.Data;
using Client.Main.ClassicFX.Rendering;
using Microsoft.Xna.Framework;

namespace Client.Main.ClassicFX.Core
{
    public sealed partial class ClassicFxRuntime
    {
        private const int MaxBlurTails = 30;
        private const int MaxObjectBlurTails = 600;
        private const int NormalBlurLife = 30;
        private const int ShortBlurLife = 15;

        // Reusable storage: positions allocated ONLY for an active slot the
        // first time it is used. Releasing a trail keeps its arrays for reuse.
        // This avoids allocating 1000 * 600 * 2 Vector3 at world startup.
        private struct BlurState
        {
            public ClassicFxOwner Owner;
            public int Type;
            public int SubType;
            public int LifeTime;
            public int LimitLifeTime;
            public int OwnerLevel;
            public int Number;
            public Vector3 Light;
            public Vector3[] P1;
            public Vector3[] P2;
        }

        private readonly BlurState[] _blurs =
            new BlurState[ClassicFxPools.MaxBlurs];
        private readonly BlurState[] _objectBlurs =
            new BlurState[ClassicFxPools.MaxObjectBlurs];

        public int ActiveBlurCount => Pools.Blurs.ActiveCount;
        public int ActiveObjectBlurCount => Pools.ObjectBlurs.ActiveCount;

        /// <summary>
        /// Main CreateBlur(CHARACTER*, p1, p2, Light, Type, Short, SubType).
        /// ownerLevel is the *native CHARACTER Level* used solely to select
        /// original Glow/Subtract blending. Callers must pass the real value;
        /// zero retains the Main's level-0 effect behavior.
        /// </summary>
        public ClassicFxHandle CreateBlur(
            ClassicFxOwner owner, Vector3 p1, Vector3 p2, Vector3 light,
            int type, bool shortTrail = false, int subType = 0,
            int ownerLevel = 0)
        {
            if (_disposed || !Enabled || !owner.HasOwner ||
                !ValidBlurOwner(owner))
                return ClassicFxHandle.Invalid;

            // Native searches live trails first, and only then first free.
            for (int i = 0; i < _blurs.Length; ++i)
            {
                if (!Pools.Blurs.IsActive(i)) continue;
                ref BlurState live = ref _blurs[i];
                if (live.Owner != owner ||
                    (subType > 0 && live.SubType != subType)) continue;
                AddBlurSample(ref live, p1, p2, light, type,
                    MaxBlurTails);
                return Pools.Blurs.GetHandle(i);
            }

            if (!Pools.Blurs.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            ref BlurState state = ref _blurs[handle.Index];
            InitBlur(ref state, owner, type, subType, light,
                shortTrail ? ShortBlurLife : NormalBlurLife,
                ownerLevel, MaxBlurTails);
            AddBlurSample(ref state, p1, p2, light, type, MaxBlurTails);
            return handle;
        }

        /// <summary>
        /// Main CreateObjectBlur(OBJECT*, p1, p2, Light, Type,
        /// Short, SubType, LimitLifeTime). -1 uses 15/30 classic life.
        /// </summary>
        public ClassicFxHandle CreateObjectBlur(
            ClassicFxOwner owner, Vector3 p1, Vector3 p2, Vector3 light,
            int type, bool shortTrail = false, int subType = 0,
            int limitLifeTime = -1)
        {
            if (_disposed || !Enabled || !owner.HasOwner ||
                !ValidBlurOwner(owner))
                return ClassicFxHandle.Invalid;

            for (int i = 0; i < _objectBlurs.Length; ++i)
            {
                if (!Pools.ObjectBlurs.IsActive(i)) continue;
                ref BlurState live = ref _objectBlurs[i];
                if (live.Owner != owner ||
                    (subType > 0 && live.SubType != subType)) continue;
                AddBlurSample(ref live, p1, p2, light, type,
                    MaxObjectBlurTails);
                return Pools.ObjectBlurs.GetHandle(i);
            }

            if (!Pools.ObjectBlurs.TryAcquire(out ClassicFxHandle handle))
                return ClassicFxHandle.Invalid;

            int life = limitLifeTime >= 0 ? limitLifeTime :
                (shortTrail ? ShortBlurLife : NormalBlurLife);
            ref BlurState state = ref _objectBlurs[handle.Index];
            InitBlur(ref state, owner, type, subType, light,
                life, 0, MaxObjectBlurTails);
            AddBlurSample(ref state, p1, p2, light, type,
                MaxObjectBlurTails);
            return handle;
        }

        // Blur has CHARACTER* owner; ObjectBlur has OBJECT* owner. Both
        // travel through ClassicFxOwner without object boxing.
        private bool ValidBlurOwner(in ClassicFxOwner owner)
        {
            if (owner.Kind == ClassicFxOwnerKind.WorldObject)
                return owner.WorldObject != null &&
                       ReferenceEquals(owner.WorldObject.World, World);
            if (owner.Kind == ClassicFxOwnerKind.ClassicFx)
            {
                var pool = Pools.GetPool(owner.FxHandle.Kind);
                return pool != null && pool.IsAlive(owner.FxHandle);
            }
            return false;
        }

        private static void InitBlur(
            ref BlurState state, ClassicFxOwner owner, int type,
            int subType, Vector3 light, int life, int ownerLevel,
            int maxTails)
        {
            // Keep arrays across generations. Only allocate on first use.
            state.P1 ??= new Vector3[maxTails];
            state.P2 ??= new Vector3[maxTails];
            state.Owner = owner;
            state.Type = type;
            state.SubType = subType;
            state.LifeTime = life;
            state.LimitLifeTime = life;
            state.OwnerLevel = ownerLevel;
            state.Number = 0;
            state.Light = light;
        }

        private static void AddBlurSample(
            ref BlurState trail, Vector3 p1, Vector3 p2,
            Vector3 light, int type, int maxTails)
        {
            trail.Type = type;
            trail.Light = light;
            // Native shifts the whole chain. Clamp to MAX_*_TAILS-1,
            // leaving the last array element safe for shift writes.
            for (int i = trail.Number - 1; i >= 0; --i)
            {
                trail.P1[i + 1] = trail.P1[i];
                trail.P2[i + 1] = trail.P2[i];
            }
            trail.P1[0] = p1;
            trail.P2[0] = p2;
            trail.Number = Math.Min(trail.Number + 1, maxTails - 1);
        }

        /// <summary>
        /// Native MoveBlurs() decrements a whole reference-frame per step;
        /// do NOT decrement on every 60/120-FPS MonoGame Update().
        /// Native MoveBlurs() also calls MoveObjectBlurs().
        /// </summary>
        public void MoveBlurs()
        {
            if (_disposed || !Enabled || !Clock.AdvancedReferenceFrame)
                return;

            for (int i = 0; i < _blurs.Length; ++i)
            {
                if (!Pools.Blurs.IsActive(i)) continue;
                ref BlurState trail = ref _blurs[i];
                trail.LifeTime--;
                trail.Number = Math.Max(trail.Number - 1, 0);
                ShiftBlurTail(ref trail);
                if (trail.LifeTime <= 0)
                {
                    trail.Number = Math.Max(trail.Number - 1, 0);
                    if (trail.Number <= 0)
                        ReleaseBlur(Pools.Blurs.GetHandle(i));
                }
            }

            MoveObjectBlurs();
        }

        public void MoveObjectBlurs()
        {
            if (_disposed || !Enabled || !Clock.AdvancedReferenceFrame)
                return;
            for (int i = 0; i < _objectBlurs.Length; ++i)
            {
                if (!Pools.ObjectBlurs.IsActive(i)) continue;
                ref BlurState trail = ref _objectBlurs[i];
                trail.LifeTime--;
                trail.Number = Math.Max(trail.Number - 1, 0);
                if (trail.LifeTime <= 0)
                {
                    trail.Number = 0;
                    ReleaseObjectBlur(Pools.ObjectBlurs.GetHandle(i));
                    continue;
                }
                ShiftBlurTail(ref trail);
            }
        }

        private static void ShiftBlurTail(ref BlurState trail)
        {
            for (int i = trail.Number - 1; i >= 0; --i)
            {
                trail.P1[i + 1] = trail.P1[i];
                trail.P2[i + 1] = trail.P2[i];
            }
        }

        public bool ReleaseBlur(ClassicFxHandle handle)
        {
            if (!Pools.Blurs.IsAlive(handle)) return false;
            ref BlurState state = ref _blurs[handle.Index];
            state.Owner = default;
            state.Number = 0;
            state.LifeTime = 0;
            return Pools.Blurs.Release(handle);
        }

        public bool ReleaseObjectBlur(ClassicFxHandle handle)
        {
            if (!Pools.ObjectBlurs.IsAlive(handle)) return false;
            ref BlurState state = ref _objectBlurs[handle.Index];
            state.Owner = default;
            state.Number = 0;
            state.LifeTime = 0;
            return Pools.ObjectBlurs.Release(handle);
        }

        /// <summary>Native RemoveObjectBlurs: subType 0 matches all.</summary>
        public void RemoveObjectBlurs(ClassicFxOwner owner, int subType = 0)
        {
            if (!owner.HasOwner) return;
            for (int i = 0; i < _objectBlurs.Length; ++i)
            {
                if (!Pools.ObjectBlurs.IsActive(i)) continue;
                ref BlurState trail = ref _objectBlurs[i];
                if (trail.Owner != owner ||
                    (subType > 0 && trail.SubType != subType)) continue;
                ReleaseObjectBlur(Pools.ObjectBlurs.GetHandle(i));
            }
        }

        public void ClearAllObjectBlurs()
        {
            for (int i = 0; i < _objectBlurs.Length; ++i)
                if (Pools.ObjectBlurs.IsActive(i))
                    ReleaseObjectBlur(Pools.ObjectBlurs.GetHandle(i));
        }

        private void ClearBlurStorage()
        {
            for (int i = 0; i < _blurs.Length; ++i)
            {
                ref BlurState state = ref _blurs[i];
                state.Owner = default;
                state.Number = 0;
                state.LifeTime = 0;
            }
            for (int i = 0; i < _objectBlurs.Length; ++i)
            {
                ref BlurState state = ref _objectBlurs[i];
                state.Owner = default;
                state.Number = 0;
                state.LifeTime = 0;
            }
        }

        /// <summary>
        /// Native RenderBlurs() iterates Blur then calls RenderObjectBlurs().
        /// Both share the same batching pass with Joint's 3D quad backend.
        /// </summary>
        public void RenderBlurs()
        {
            if (_disposed || !Enabled || _billboardRenderer == null)
                return;
            _billboardRenderer.Begin();
            for (int i = 0; i < _blurs.Length; ++i)
            {
                if (!Pools.Blurs.IsActive(i)) continue;
                ref BlurState trail = ref _blurs[i];
                if (trail.Number < 2) continue;
                int textureId = ClassicTextureIds.BitmapBlur + trail.Type;
                if (trail.Type == 3) textureId = ClassicTextureIds.BitmapBlur2;
                else if (trail.Type == 4) textureId = ClassicTextureIds.BitmapBlur;
                else if (trail.Type == 5) textureId = ClassicTextureIds.BitmapBlur + 3;
                if (!Textures.TryGet(textureId, out ClassicTextureResource tex)) continue;

                // Original: owner->Level == 0 and type in [0..3,5..10]
                // uses Glow; all other cases use EnableAlphaBlendMinus().
                bool glow = trail.OwnerLevel == 0 &&
                    (trail.Type <= 3 ||
                     (trail.Type >= 5 && trail.Type <= 10));
                QueueBlurSegments(ref trail, tex,
                    glow ? ClassicBlendMode.Glow :
                           ClassicBlendMode.Subtract,
                    false);
            }
            QueueObjectBlurs();
            _billboardRenderer.End();
        }

        public void RenderObjectBlurs()
        {
            if (_disposed || !Enabled || _billboardRenderer == null)
                return;
            _billboardRenderer.Begin();
            QueueObjectBlurs();
            _billboardRenderer.End();
        }

        private void QueueObjectBlurs()
        {
            for (int i = 0; i < _objectBlurs.Length; ++i)
            {
                if (!Pools.ObjectBlurs.IsActive(i)) continue;
                ref BlurState trail = ref _objectBlurs[i];
                if (trail.Number < 2) continue;
                int textureId = ClassicTextureIds.BitmapBlur + trail.Type;
                if (trail.Type == 3) textureId = ClassicTextureIds.BitmapBlur2;
                else if (trail.Type == 4) textureId = ClassicTextureIds.BitmapBlur;
                else if (trail.Type == 5) textureId = ClassicTextureIds.BitmapLava;
                if (!Textures.TryGet(textureId, out ClassicTextureResource tex)) continue;
                QueueBlurSegments(ref trail, tex,
                    ClassicBlendMode.Glow, true);
            }
        }

        private void QueueBlurSegments(
            ref BlurState trail, ClassicTextureResource tex,
            ClassicBlendMode blend, bool objectBlur)
        {
            // Native RenderBlurSegment(): positions P1/P2, UV progress,
            // the first and last vertex pairs use their own fading light.
            int count = trail.Number;
            for (int j = 0; j < count - 1; ++j)
            {
                if (objectBlur && (trail.SubType == 113 || trail.SubType == 114))
                {
                    // Native suppresses teleport discontinuities > 300 units.
                    const float MaxJump = 300f;
                    Vector3 p = trail.P1[j];
                    if (MathF.Abs(p.X - trail.P1[j + 1].X) > MaxJump ||
                        MathF.Abs(p.Y - trail.P1[j + 1].Y) > MaxJump ||
                        MathF.Abs(p.Z - trail.P1[j + 1].Z) > MaxJump ||
                        MathF.Abs(p.X - trail.P2[j + 1].X) > MaxJump ||
                        MathF.Abs(p.Y - trail.P2[j + 1].Y) > MaxJump ||
                        MathF.Abs(p.Z - trail.P2[j + 1].Z) > MaxJump)
                        continue;
                }

                float firstFade = (count - j) / (float)count;
                float secondFade = (count - j - 1) / (float)count;
                if (!objectBlur && trail.OwnerLevel != 0)
                    firstFade = secondFade = 1f;
                float u0 = j / (float)count;
                float u1 = (j + 1) / (float)count;
                _billboardRenderer.QueueWorldQuadGradient(
                    tex, trail.P1[j], trail.P2[j],
                    trail.P2[j + 1], trail.P1[j + 1],
                    new Vector2(u0, 1f), new Vector2(u0, 0f),
                    new Vector2(u1, 0f), new Vector2(u1, 1f),
                    trail.Light * firstFade,
                    trail.Light * secondFade,
                    blend, ClassicDepthMode.ReadOnly);
            }
        }
    }
}
