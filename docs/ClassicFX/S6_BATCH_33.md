# ClassicFX Season 6 — Batch 33

Pinned MuMain: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`. Base: `f5de461e4d2ec7c1095667e80bbad35aca0a6836`.

## Models (IDs 354–364; total 364)

- **354–362:** Nine `MODEL_EX01_SHADOW_MASTER_*` fragments: ankle left/right, belt, chest, helmet, knee left/right, wrist left/right. Their real pinned MapManager paths are `Monster/ex01shadow_rock_7_*.bmd`; all exist in Data_Broyal. The separate `ex01shadow_master_7_*` assets also exist, but are not used by the pinned loader.
- **363–364:** `MODEL_WARP3` / `MODEL_WARP6`, both `NPC/warp03.bmd`; Warp6 is used by Raklion.

## Native behavior and remaining limitations

- Pinned `GMSwampOfQuiet.cpp` creates EX01 fragments from nine transformed owner bones during the death action. There are no specialized CreateEffect or MoveEffects handlers for these model IDs (also documented in native effect catalogue design). ClassicFX registers their BMDs, keeps their positions static, and gives them a **provisional 30-reference-frame expiry** to avoid immortal views. This is not native physics or lifetime parity. The monster death caller remains unconnected.
- Warp3/6 use `EffectTypes.json` life **16,777,215** and scale **0.6**. Only native subtypes 0 and 1 are allowed; their Move_MODEL_WARP3 is shared with Batch 31's sine-light and yaw update and obeys `Clock.FrameFactor`. They do not inherit Batch 31's randomized creation scale/gravity.
- Shared ClassicFX pool and existing BMD renderer; no parallel renderer, gameplay reroute or legacy deletion.
- No claimed full parity, measured Android performance, or `dotnet build`. Compilation must be run locally before fast-forward and push.
