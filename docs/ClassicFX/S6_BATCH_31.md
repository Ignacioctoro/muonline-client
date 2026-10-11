# ClassicFX Season 6 — Batch 31: Kanturu storms, warp objects and ambient skills

Native reference `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`. Client base: `4b22c2a5c31d81eb411deb4a4a919b4feb46103d`.

**10 registered IDs (336–345)**, using the pre-existing ClassicFX effect pool and `ClassicFxEffectModelObject`; no new BMD renderer.

| ID | Type | Data_Broyal file |
| --- | --- | --- |
| 336 | KanturuStorm2 | Skill/boswind.bmd |
| 337 | KanturuStorm3 | Skill/mayatonedo.bmd |
| 338 | AuroraModel | Skill/Aurora.bmd |
| 339 | ButterflyModel | Object1/Butterfly01.bmd |
| 340 | LaserSkillModel | Skill/Laser01.bmd |
| 341 | RidingSpearModel | Skill/RidingSpear01.bmd |
| 342 | Warp1 | NPC/warp01.bmd |
| 343 | Warp2 | NPC/warp02.bmd |
| 344 | Warp4 | NPC/warp01.bmd |
| 345 | Warp5 | NPC/warp02.bmd |

Source methods: `ZzzEffect.cpp::CreateEffect`, `MoveHandlers.cpp::Move_MODEL_STORM2/3`, `Move_MODEL_AURORA`, `Move_MODEL_BUTTERFLY01`, `Move_MODEL_LASER`, `Move_MODEL_WARP3` and `Move_MODEL_SPEARSKILL`; asset paths from `ZzzOpenData.cpp` and `MapManager.cpp`. All 10 are routed through `TryGetSeason6ModelDefinition` and `MoveSeason6Model`.

Includes the native random initialization, 25 Hz time-factor scaling, Storm2 subtitled lightning/stone/cloud emissions, Storm3 lightning emissions, butterfly altitude limiting and sparks, Warp time-based light oscillations, Laser movement and its force joint, and RidingSpear initialization. Subtype validation rejects unknown pairs.

## Fidelity and integration boundaries

- `KanturuStorm3` captures start position using a provided world object as the main client's Hero reference. The original's `Pos2[2]` in thunder emission is left uninitialized; this port uses a deterministic matching source Z rather than copying undefined memory. The global map-based after-character render override is not present.
- `AuroraModel` is only visible when Castle Siege is active in the native Main. **It is kept hidden until a proper Castle Siege event-state bridge and UV-scrolling render support exist**. It requires an owner and nonzero native PK key; registration does not imply visible parity.
- `ButterflyModel` requires a world owner; subtype colors, ambient sprites and sparks are represented. The original derives horizontal flight direction from owner Scale and animated orientation, which needs a richer owner-scale/action bridge for exact fidelity.
- `LaserSkillModel` only approximates native `RENDER_DARK` BMD render mode because this mode is not separately exposed. It does not replace projectile hit/gameplay logic.
- `Warp1/2/4/5` correspond to explicitly initialized `MODEL_WARP / WARP2 / WARP4 / WARP5` and use native long-lived lifetimes; map callers must prevent repeated spawning. `WARP3/WARP6` have no equivalent native creation initializer and were deliberately omitted.
- Native `MODEL_SKILL_WHEEL1/2` were deliberately omitted: they alter packet serial / player equipment state, and do not have verified independent BMD paths.
- Model catalogue registration does **not** replace legacy event/skill callsites. No old path removed and no parity or Android performance claimed.

The GitHub connector can publish and compare commits, but cannot execute your local `dotnet build`. Validate the build locally prior to a fast-forward integration/push.
