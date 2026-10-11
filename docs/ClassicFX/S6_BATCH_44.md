# BroyalMU ClassicFX S6 — Batch 44

Base: `a9ee6de3b8ba71a9ee0584b602fac7b43140acd6`. Native: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## New IDs 440–443

- **440 KundunSkillCarrier:** `MODEL_CUNDUN_SKILL`, original subtypes 0/1/2 (30/30/40 ticks). Subtype 0 emits ten existing `KundunPhoenix` models; subtype 1 emits twenty `KundunDragonHead` models and the four PKKey-gated children; subtype 2 emits `KundunGhost` on creation. No stand-in BMD for the invisible carrier.
- **441 ShineFlareCarrier:** `MODEL_SHINE` subtype 0, 50 ticks; ten `BITMAP_FLARE` joints on creation and the original owner-offset `BITMAP_SHINY` subtype-2 particle until tick 10.
- **442 SpearHealingCarrier:** `MODEL__SPEAR` from `MoveHandlers.cpp` and `EffectTypes.json`, five ticks; three randomized `BITMAP_JOINT_HEALING` subtype-6 joints per native tick.
- **443 ThunderPlusOneCarrier:** native `BITMAP_JOINT_THUNDER+1`, subtype 0; ten ticks, owner-angle placement offset and subtype-5 thunder joints at ticks 4 and 2.

All emitters reuse the existing Effect/Particle/Joint/BMD pools, native reference-frame clock and original textures. No new shader, BMD model or renderer.

## Explicit limitations

- No change to gameplay call sites (monster attack dispatch must request these new carriers).
- Existing `KundunGhost` ModelObject bridge still lacks native PKKey-driven animated follow-up, as documented in Batch 32; the ghost itself renders via its real BMD but follow-up behavior isn't claimed complete.
- Source uses `MODEL__SPEAR` for the healing-joint Move handler; no separate visual BMD is claimed.
- Compiles **not verified** remotely: connector writes source; Windows .NET 10/MonoGame build must pass before updating `classicfx-nova-pilot`.
