# ClassicFX Season 6 — Batch 28

Base: `f021c07504503dda2245d03f21828b4ffbdafdae`; original Main: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Added: 9 unique Effect IDs 306–314

- PhoenixShotModel: `Effect/phoenix_shot_effect.bmd`.
- WindSpin02Model / WindSpin03Model: `Effect/wind_spin02.bmd`, `Effect/wind_spin03.bmd`. `wind_spin01.bmd` is absent from Data_Broyal.
- VolcanoOfMonkModel: visual subtype 1, native height placement, 100-tick life, 4 stones on creation plus 4 stones and existing ShockWaveGround01 subtype 2 near last 10 frames, and fire particle emission.
- VolcanoStoneModel: native 30–39-tick random lifetime, initial scale, launch arc, gravity, terrain bouncing, fade and fire particles.
- MoveTargetPositionModel: 30 ticks, reference-tick spark emission and 10-tick mesh/light fade.
- SakuraEventItemModel: `Effect/cherryblossom/Skura_iteam_event.bmd`, lifetime 52, follows live owner, bone 1/2 shiny sprites and two sets of seven petals/shiny particles per native tick.
- UmbrellaGoldModel: `Effect/japan_gold01.bmd`, native randomized gravity, orientation, lifespan, terrain height and bounce.
- EmpireGuardianFrameStrikeModel: `Effect/Karanebos_sword_framestrike.bmd`, subtype 0 / 2 reference ticks. Subtype 3 requires native interpolation and is intentionally not accepted.

One ModelObject/BMD renderer, existing ClassicFX effect pool and clock, no independent particle renderer. Asset identities checked against `ZzzOpenData.cpp` and `Data_Broyal`; behavior compared against `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `EffectTypes.json`.

## Explicit limitations

1. PhoenixShot and WindSpin02/03 have no explicit custom `CreateEffect` lifespan in pinned source; a finite 20-tick fallback is **not** confirmed parity. These are registered model BMDs, not fully verified visual migrations.
2. Volcano: native BITMAP_LIGHT_RED subtype 3, terrain light and emitter subtypes 2/3 need their real effect/owner bridge. No fake BMD is created for missing wind_spin01 or shockwave03.
3. Sakura: native 70-particle bursts at 30/15/4 frames from character bone20 remain. Base two-bone model emission is in ClassicFX; its source cost is substantial on Android and must be evaluated when real callsites are migrated.
4. MoveTargetPosition: native BITMAP_MAGIC/target-position secondary effects and MODEL_SPEARSKILL joins still need native caller bridge.
5. EmpireGuardianFrameStrike's ChromeEnable and subtype3 path interpolator/blurs are not available. Do not claim parity.
6. No legacy effect callsites were changed, so these registrations do not eliminate duplicate render paths. Compile, visual parity and Android performance are not remotely verified.
