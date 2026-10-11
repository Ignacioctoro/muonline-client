# ClassicFX Season 6 — Batch 40: ten native roots

Pinned MuMain: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Base client: `d8dadcf731c0b979e8bc1a2f4c5da4d685e8144e`. Sources: `ZzzEffect.cpp`,
`Behaviors/MoveHandlers.cpp`, `_TextureIndex.h`,
`ZzzOpenData.cpp`, `EffectTypes.json`.

| Broyal ID | Root | Native source | Supported subtype |
|---:|---|---|---|
| 415 | SkullEffect | BITMAP_SKULL | 1, 2, 3, 4, 5 |
| 416 | FlareParticleEffect | BITMAP_FLARE | 1, 2, 3 |
| 417 | SwordEffCarrier | BITMAP_SWORDEFF | 0 |
| 418 | JointForceCarrier | BITMAP_JOINT_FORCE | 0 |
| 419 | SbumbImpactEmitter | BITMAP_SBUMB | 0 |
| 420 | Damage1ImpactEmitter | BITMAP_DAMAGE1 | 0 |
| 421 | IceBreathCloudCarrier | MODEL_STREAMOFICEBREATH | 0 |
| 422 | LavaGiantFootprintRed | MODEL_LAVAGIANT_FOOTPRINT_R | 0 |
| 423 | LavaGiantFootprintViolet | MODEL_LAVAGIANT_FOOTPRINT_V | 0 |
| 424 | FireHik3MonoCarrier | MODEL_EFFECT_FIRE_HIK3_MONO | 0 |

The two footprint model-coded roots are **native terrain-only**; no BMD is constructed. All families use existing pooled ClassicFX instances. Added `BITMAP_SKULL=32226`, `BITMAP_LAVAGIANT_FOOTPRINT_R=32419`, `_V=32420`, using verified Data_Broyal `Skill/Skull.OZJ`, `Effect/eff_magma_red.OZJ`, `Effect/eff_magma_violet.OZJ`. Other textures were already in the catalogue.

Creation, tick-based movement and particle/joint/sprite fan-out are dispatched in the shared runtime. Hit effects require a real `nativeTarget` and model owner; unsupported subtypes are rejected.

## Fidelity boundaries (not full parity)

- Skull subtype 0 is not included: native character debuff / cloak checks are not bridged. Other skull subtypes currently use unit owner-scale for Z offsets.
- SwordEff only handles pooled joint between owner bones 5 and 4; original giant-swing animation windows, target hit, SFX and secondary shockwave/wolf models await gameplay animation bridge.
- Force subtype 1 is omitted because its `BITMAP_JOINT_THUNDER+1` path is not fully texture-resolved. Source fallthrough to Move_MODEL_SWORD_FORCE has not been ported.
- SBUMB / DAMAGE1 emit visual impacts using explicit `nativeTarget`, not character target indexes; source SFX are omitted.
- MODEL_STREAMOFICEBREATH original emits 4 coincident Raklion particles on creation and each reference frame; this is a pooled logical carrier without a fabricated BMD.
- Footprint rotational Angle.X is maintained, although native terrain render path does not apply a bitmap rotation argument.
- FireHik3Mono only spawns its original probabilistic child at creation and releases the logical carrier; no fake model is rendered.
- No .NET compiler is provided by the GitHub connector; **local Windows build required** before push.
