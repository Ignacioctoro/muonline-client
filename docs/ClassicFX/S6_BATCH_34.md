# ClassicFX Season 6 Batch 34 — Combat, support and gate effects

Pinned MuMain: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`. Base: `e8b353982e0b1993545f38cacb3dfdc81572a248`.

## Native IDs added (365–371; total 371)

| ID | Type | BMD from Data_Broyal | Behavior |
|----|------|----------------------|----------|
| 365 | MODEL_MAGIC_CIRCLE1 | Skill/MagicCircle01.bmd | Subtypes 0–2, scale growth and owner following (0/2); subtype 1 terrain-light emission remains unbridged |
| 366 | MODEL_PROTECT | Skill/Protect01.bmd | Attached protection, owner visibility and rotating angle; native 10,000-tick lifetime |
| 367 | MODEL_TOWER_GATE_PLANE | Skill/TowerGateplane.bmd | 100-tick refreshed life and owner-linked sinusoidal height; second mirrored BMD render pass missing |
| 368 | MODEL_WARCRAFT | Skill/hellgate.bmd | 50-tick HellGate, subtype-dependent blend, owner following and native action speed |
| 369 | MODEL_SHIELD_CRASH2 | Effect/atshild2.bmd | 24-tick guard projection: caller Light becomes Direction, native RGB setup and two fading ranges |
| 370–371 | MODEL_GATE / MODEL_GATE+1 | Object12/Gate01.bmd and Gate02.bmd | Randomized initialization, 25Hz gate debris bounce, damping, rotation and throttled smoke |

## Important integration boundaries

- All seven are accepted by the common ClassicFX effect pool and existing MonoGame BMD renderer. There is no new renderer or parallel particle system.
- `TowerGatePlane` native `RenderEffects` performs **two mirrored BMD draws**. The single-view renderer cannot express the second pass yet; the model is registered and moved, not fully rendered at parity.
- `MagicCircle1` subtype 1 issues a native terrain dynamic light that is not yet connected.
- The native client callsites (combat, siege and buffs) remain unchanged. All existing legacy effects still run until their triggers are actually rerouted, then removed. These additions are **registered models and partial native behavior**, not proof of end-to-end reproduction.
- Reference clock `Clock.FrameFactor` is used for continuous movement; gate smoke is bounded to native reference-frame frequency. No claims of measured Android performance or build success. Compile locally before main fast-forward.

## Verification

Assets verified against the current Data_Broyal tree; relevant native definitions checked in `EffectTypes.json`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp` and `ZzzOpenData.cpp`.
