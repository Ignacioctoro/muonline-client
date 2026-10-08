# ClassicFX — Joint foundation O

Base: `Ignacioctoro/muonline-client`, branch `classicfx-engine-2026-10`, commit `b74809a7cc426745e542007789877b26e51fc424`.
Source: `sven-n/MuMain`, `src/source/Core/Globals/_struct.h`, `src/source/Render/Effects/ZzzEffectJoint.cpp`.

## Added

- `ClassicJoint` mirrors the ordinary JOINT fields, including `Tails[MAX_TAILS][4]` as one flat array of 200 × 4 positions, `TargetIndex[5]`, life, render flags, target, velocity and colour.
- `CreateJoint` and `CreateJointFpsChecked`: fixed slots of 500 joints and generational handles, as already defined in `ClassicFxPools`.
- Reuses tail arrays when a pool slot is recycled; avoids growing lists and per-frame allocations. 500 fully populated slots require around 4.8 MB in `Tails` vectors plus metadata. Arrays allocate on first use of each slot rather than all at once.
- `TryGetJoint`, `ReleaseJoint`, and cleanup on runtime reset.
- `InitializeFirstTail()` is an exact default joint quad (same orientation and half-scale formula as MuMain), provided for later Type-specific initialization. It is **not** invoked before those subtype exceptions are implemented.

## Important limits

This step **does not port** the enormous `CreateJoint` Type switch, `MoveJoints`, or `RenderJoints`. Joints do **not** draw yet. Its purpose is faithful shared storage and the common entry point, not a substitute sprite effect. Call sites should not be switched over to this API until the next blocks initialize and move the relevant joint types.

Open Particle audit issues: `BITMAP_SMOKE + 2`, `BITMAP_SWORD_FORCE`, `BITMAP_CLOUD + 2`, `BITMAP_CHROME + 2`, WorldObject `StartPosition`, and `Effect.CreateBomb` are not fixed by this step. No unverified textures or explosions were fabricated.

## Next step

Port `CreateJoint` Type/SubType initialization in source order. Keep the initial-tail suppression rules and the original lifespan/MaxTails values. Then implement `MoveJoints`, tail shift and renderer before enabling joints in the scene.
