# ClassicFX Season 6 Batch 36 — 10 real native IDs

Reference: MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2; EffectTypes.json, ZzzOpenData.cpp, ZzzEffect.cpp, MoveHandlers.cpp, _TextureIndex.h. Assets: Data_Broyal.

Base: `68d24301cf3a58096f0c51db54101a3aa901e533`.

| ID | Original native effect | Implementation |
|---|---|---|
| 375 | MODEL_BLIZZARD | Skill/blizzard.bmd, falling/randomized lifetime, smoke, glow (secondary impact incomplete) |
| 376 | MODEL_CURSEDTEMPLE_PRODECTION_SKILL | Skill/eventshild.bmd, persistent bone-2 follow, ShockWave child |
| 377 | MODEL_CURSEDTEMPLE_RESTRAINT_SKILL | Skill/eventroofe.bmd, persistent owner follow |
| 378 | BITMAP_EVENT_CLOUD | Existing Effect/clouds2.jpg, native ground scale, rotation, fade |
| 379 | BITMAP_TARGET_POSITION_EFFECT1 | Effect/cursorpin01.jpg, scale/fade |
| 380 | BITMAP_TARGET_POSITION_EFFECT2 | Effect/cursorpin02.jpg, oscillating scale/fade |
| 381 | BITMAP_RING_OF_GRADATION | Effect/ring_of_gradation.jpg, native fade/growth |
| 382 | BITMAP_OUR_INFLUENCE_GROUND | Effect/guild_ring01.jpg, owner-bound two rings and glow |
| 383 | BITMAP_ENEMY_INFLUENCE_GROUND | Effect/enemy_ring02.jpg, owner-bound two rings and glow |
| 384 | BITMAP_LIGHT_MARKS | Effect/lightmarks.jpg, original 14 bone-scaled sprites |

- All ten effects have a concrete ClassicFX creation/move/render path; none uses a fake BMD or a new independent renderer.
- The six ground effects reuse the existing terrain-tessellated batch renderer. LightMarks uses the existing sprite pipeline.
- Android: LightMarks emits 14 sprites **per native 25 Hz frame**, not per display frame; the two ring overlays share batching.
- Native feature-conditional influence-ground effects still require actual game integration and side selection.
- Visual parity **not complete**: Blizzard collision secondary burst and some Cursed Temple mesh-special behavior are deferred. No gameplay trigger has been wired by this batch.
- Local .NET compiler unavailable in this environment. The guarded installer must build before pushing the integration branch.
