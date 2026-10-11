# ClassicFX Season 6 — Batch 39

Fixed original C++: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.
Base commit: 68f1c4162d1bc32a79893b172be5f8d83ec3ddd1. Sources: ZzzEffect.cpp, ZzzOpenData.cpp, _TextureIndex.h.
The identifiers here are **Broyal enum IDs**, not original bitmap indices.

| ID | Type | Original | Accepted subtypes |
|---:|---|---|---|
| 405 | BossLaserBlue | BITMAP_BOSS_LASER | 0,1,2 |
| 406 | BossLaserRed | BITMAP_BOSS_LASER+1 | 0 |
| 407 | BossLaserShort | BITMAP_BOSS_LASER+2 | 0 |
| 408 | FireTrail02 | BITMAP_FIRE+1 | 0 |
| 409 | LightProjectile | BITMAP_LIGHT | 0 |
| 410 | MagicGroundBase | BITMAP_MAGIC | 1 |
| 411 | ShotgunJointBurst | BITMAP_SHOTGUN | 0 |
| 412 | FlareForceJointBurst | BITMAP_FLARE_FORCE | 0–7 |
| 413 | LightRedGround | BITMAP_LIGHT_RED | 3,4 |
| 414 | ChromeEnergyGround | BITMAP_CHROME_ENERGY2 | 0 |

## What was integrated

Native initializers, pooled joint fan-outs, moving particle emitter, 20-step native laser sprite paths, terrain shader batch reuse, and central Create/Move/Render dispatch. The existing Data_Broyal texture `Effect/flare01_red.OZJ` is mapped via original bitmap slot 32429. No additional renderer or fabricated model.

## Boundaries, not full parity

- Boss laser does not yet invoke client-side hit detection, terrain lighting, safe-zone logic, or subtype-1 smoke overlay; reference-tick sprite batches differ from every-display-frame native rendering.
- BITMAP_FIRE+1 terrain lights, audio, LightRed terrain light and subtype 0 owner-bone sprite array are not connected.
- Native `BITMAP_CHROME_ENERGY2` uses StartPosition XY bitmap dimensions, while the current bridge uses caller scale.
- BITMAP_LIGHT buff-dependent subtypes 1–3 and other BITMAP_MAGIC subtypes are intentionally rejected.
- GitHub connector cannot run dotnet; local Windows build required before integrating.
