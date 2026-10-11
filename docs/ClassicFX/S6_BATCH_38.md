# ClassicFX S6 — Batch 38: native bitmap effect emitters

Pinned C++ reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
Base commit: 589ffc52aa11ff82b3d8dd59fe5cf5aaa1b5b10f.
Primary native source: `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `EffectTypes.json`, `ZzzOpenData.cpp`, `_TextureIndex.h`.

| Broyal ID | Native Effect code | Currently accepted subtypes |
| ---: | --- | --- |
| 395 | BITMAP_FLAME | 0, 1, 2, 3, 5, 6 |
| 396 | BITMAP_FIRE_CURSEDLICH | 1, 12 |
| 397 | BITMAP_SPARK + 1 | 0 |
| 398 | BITMAP_SPARK + 2 | 0 (owner required) |
| 399 | BITMAP_ENERGY | 0 |
| 400 | BITMAP_SHINY + 4 | 0 |
| 401 | BITMAP_SHINY + 6 | 0 |
| 402 | BITMAP_LIGHTNING + 1 | 0, 1 |
| 403 | BITMAP_SWORD_EFFECT_MONO | 0, 1, 2 |
| 404 | BITMAP_IMPACT | 0 (owner required) |

Native BITMAP_IMPACT numeric slot 32291 maps to `Effect/Impack03.jpg`, present physically as `Data/Effect/Impack03.OZJ`.

## Runtime integration
- All ten allocate existing ClassicFX Effects, not ModelObjects or new per-effect GPU resources.
- One logical dispatcher initializes native lifetimes and invokes effect-specific Move; two terrain families reuse the existing batched RenderTerrainAlphaBitmap bridge.
- Owner-dependent variants reject missing/disposed owners. Unimplemented subtypes are rejected rather than silently rendered incorrectly.
- Particles and sprites use preallocated existing pools. Stochastic emissions run at reference tick frequency with FrameFactor-based scalar fades.
- Sword mono's Shockwave01 child has a one-shot trigger mask.

## Known caveats (do not claim parity)
- Native additive terrain lights, sound events and network hit checks are not part of this batch.
- Effect damage and source/target linkage are not connected to gameplay here.
- Flame subtype 4 and CursedLich subtypes 0/2/3 require additional behavior and were not marked migrated.
- Particle family profiles and Android frame timings require in-game checks.
- The GitHub edit environment has no dotnet compiler. Windows build and push are deliberately gated by the installer.
