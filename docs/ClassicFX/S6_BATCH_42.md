# BroyalMU ClassicFX Season 6 — Batch 42

Reference: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

Base branch HEAD: `0ff0d8204819824f0e017a81a7391c626e101142`.

## Implemented

- **436 — WaterfallOrbit**: original `BITMAP_WATERFALL_4` `CreateEffect` and `Move_BITMAP_WATERFALL_4`. Uses the native 80-tick lifetime, randomized 0.1–0.29 scale, initial timer/angle/distance, bone selected through native `PKKey`, three orbiting sprites, exponential light attenuation, 25-FPS reference-tick emission, pooled sprite renderer.
- Existing `BitmapWaterfall4` texture registration resolves to `Data/Effect/waterFall4.OZJ`; no extra render pipeline or texture slot.

## Scope and constraints

- Only subtype 0 is accepted for this bridge because that is the verified entry point. Requires a live ModelObject owner and valid bone. No new IDs were assigned for roots already represented by differently named effects (such as SummonerNeil, EventCloudEffect, and SwordMonoEmitter).
- No code falsely claims to cover a native root whose BMD is missing or whose native rendering path is unavailable.
- Static source review only. **No Windows/Android `dotnet build` has been executed in the GitHub connector environment.** The local integration command must compile before pushing the main working branch.
- This is registered runtime coverage, not a gameplay call-site migration.
