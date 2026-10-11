# ClassicFX Season 6 — Batch 37 (bitmap emitters and fireworks)

Base: `b6af983524a7818f839830c7325cb43f07518f46`.
Reference: MuMain@`21728b1e5b03e0763b38ef9e23f79645e0df7ad2`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `EffectTypes.json`, `_TextureIndex.h` and `ZzzOpenData.cpp`. Assets: `Data_Broyal`.

| ClassicFX ID | Original EffectType | Runtime behavior |
| ---: | --- | --- |
| 385 | BITMAP_FIRECRACKERRISE | Native 75-tick carrier, periodic firecracker effect spawning |
| 386 | BITMAP_FIRECRACKER | Native directional seed, 4–6/12 tick lifetime, terminal particle burst |
| 387 | BITMAP_FIRECRACKER0001 | Native 31/24/17/9/1 tick spirit-joint emissions |
| 388 | BITMAP_FIRECRACKER0002 | Native 30-tick explosion particle/sprite burst and child effects |
| 389 | BITMAP_FIRECRACKER0003 | 15-tick animated seven-frame firecracker sprites and fade |
| 390 | BITMAP_CLOUD | 60-tick cloud terrain bitmap, growth and light fading |
| 391 | BITMAP_ORORA | Owner-bound aurora particle emitter with native lifetime renewal |
| 392 | BITMAP_GATHERING | 0–3 variants with bone-33 follow, sparks, shiny sprites and thunder joints |
| 393 | BITMAP_FIRE_HIK2_MONO | Two emitter variants; subtype 0 native terrain lightning overlay |
| 394 | BITMAP_PIN_LIGHT | 0–4 native variants; bounded owner-bone or spatial particle emissions and subtype-4 sprite |

- All ten use the **existing** ClassicFX Effect pool; there are **no new BMD renderers**.
- The seven `Effect/firecracker0001.jpg` … `0007.jpg` files are indexed consecutively after `BITMAP_2LINE_GHOST`, per original `_TextureIndex.h`. The `BITMAP_DS_SHOCK` slot uses the original `Effect/Shockwave.jpg`.
- Stochastic emissions are gated to native reference frames; fades and motion scale by `Clock.FrameFactor`. The five threshold values for 0001 use one static array, with trigger bits to avoid repeated child emission. No per-frame list allocations.
- The 0002 explosion uses the native 60 + 30 + 60 particle emissions at creation. The preexisting effect/particle pool imposes a fixed capacity; Android bursts merit profiling.
- Missing parity/dependencies: actual gameplay and events must invoke these IDs; native firecracker sound and temporary terrain light are not yet wired; native luminosity and some fine owner-relative offsets remain to validate. This is **not** a declaration of visual or gameplay parity.
- No .NET SDK is available in the editor environment. Run the guarded PowerShell installer to compile before pushing `classicfx-nova-pilot`.
