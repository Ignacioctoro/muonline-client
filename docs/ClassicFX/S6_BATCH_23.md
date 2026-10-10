# ClassicFX Season 6 — Batch 23: Kundun + Kanturu Maya

Base: `f8e8b796aef0709c1fdd1b3f713ccae4116b8b60`.
Pinned reference: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Implemented

- **16 original BMD identities; IDs 244–259:** 8 Kundun parts (`Monster/cd71a..h.bmd`), MayaStone1–5, MayaStoneFire, MayaHandSkill, MayaStar (`Skill/arrowsre05.bmd`).
- All 16 BMD paths verified against `Data_Broyal/main`.
- Kundun native subtype 1 (fade), 2/3 (fall, stage transition, water impact, oscillation, bubble emission), 4 (drop/fade) and 5 (fade). `CreateEffect` gained optional `nativePkKey` and `nativeSkillIndex` for 2/3/4 without changing existing callers.
- MayaStone1–3: 40 native ticks, large random scale, downward velocity, terminal terrain impact, 15 native smoke particles, 6 child MayaStone4/5 models and explosion. MayaStone4/5: native damping, gravity, bounce and fire emission.
- MayaStoneFire: original three relative scale factors (0.8/0.6/0.5), downward travel and terrain death.
- MayaHandSkill 0/1: original geometric light decay, smoke emission and frame-timed visual child emission; MayaStar: native 50-frame model / scale 50.
- Uses ClassicFX pools, `ClassicFxEffectModelObject`, world/terrain bridge and 25-FPS clock; no combat/network/server edits.

## Accuracy boundaries / intentionally pending

- Kundun water impact caps original 100 waterfall particles at 20 per effect for Android and does not mutate world water vertices. The original timed joint/smoke pre-impact phase is incomplete.
- MayaStone1–3 spawn native smoke/fragment/explosion children, but **not** the original smoke-joint attached to the Effect or Inferno subtype 2 with effect-as-owner: `ClassicFxOwner` currently wraps world objects, not pooled effects.
- MayaStone4/5 have native movement cases but no dedicated `CreateEffect` initializer in the pinned Main; use a documented 40-tick BMD fallback, not claimed native initialization parity.
- MayaHandSkill's native after-character render ordering remains within the normal shared BMD renderer, not a new pass.
- `MayaStoneFire` subtypes 0/1/2 are ClassicFX symbolic mappings of the original `MODEL_MAYASTONE1/2/3` selector.
- No skills/boss event trigger migrations, BMD animation frame parity, gameplay logic, Android profiling or local .NET build performed.
