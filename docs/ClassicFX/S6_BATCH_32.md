# ClassicFX Season 6 — Batch 32: Kundun / Kalima, Aida and Imperial effects

Base: `1cbc009db0237a070e672c0a1eef3cc130dc172b`.
Pinned native reference: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Relevant C++: `ZzzEffect.cpp`, `Render/Effects/Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp` and `World/MapInfra/MapManager.cpp`.

**8 additional registered types, IDs 346–353**, hooked to existing `ClassicFxRuntime.Effects.cs`, `ClassicFxRuntime.EffectSeason6Families.cs`, and the existing BMD `ClassicFxEffectModelObject`. No separate renderer or effect pool.

| ID | Type | Verified Data_Broyal BMD |
|---|---|---|
| 346 | KundunDragonHead | Skill/dragonhead.bmd |
| 347 | KundunPhoenix | Skill/phoenix.bmd |
| 348 | KundunGhost | Monster/cundun_gone.bmd |
| 349 | DeasulerBoomerang | Monster/deasther_boomerang.bmd |
| 350 | ImperialProjectile | Effect/choarms_06.bmd |
| 351 | SawSkillModel | Skill/Saw01.bmd |
| 352 | TreeAttackModel | Object34/tree_eff.bmd |
| 353 | DesairModel | Skill/desair.bmd |

The native DragonHead setup/fade behavior includes delayed Spirit2 joint initialization and occasional additional Spirit2 joints around Z=350. Phoenix has native lifetime/scale/fade and Spirit joint initialization. Deasuler subtype 0 uses the *target encoded in the caller's Light input* to construct the original two-phase linear 1700-unit path with changing angle. It evaluates the segments directly without allocating `CInterpolateContainer` arrays per instance. The Imperial projectile uses owner bone 9 for the start and switches to the original accelerating move formula. Saw rotates and advances; Aida's TreeAttack grows and fades.

## Limits requiring future caller/bridge work

- `MODEL_CUNDUN_SKILL` is a *logical emitter* with no separately verified BMD. It was not fabricated as a ninth BMD. The native dragon/phoenix/ghost emitter timing and boss trigger callsites are not rerouted.
- `KundunGhost` lacks an explicit native `CreateEffect` lifespan and its move handler depends on native effect-owner `PKKey`, exact BMD animation frames, global hero position and camera `EarthQuake`. This batch registers the BMD with provisional **60-frame expiry** and deliberately does not invent those phases.
- `DeasulerBoomerang` requires a real owner and target world-space coordinate passed as the Light argument; zero-length owner-to-target is rejected. Native BMD blur sampling and effects emitted along the trajectory remain pending. Native interpolation's rate break and angle segment are represented directly.
- `ImperialProjectile` likewise needs a world owner, bone 9 and caller-provided target/StartPosition; its 45-frame fallback lifetime is provisional because this effect has no explicit native CreateEffect initializer. Gameplay projectiles/hit detection are not replaced.
- `TreeAttackModel` and `DesairModel` have no specialized native CreateEffect lifespan in the pinned source. They use a **documented provisional 30-frame lifespan**; Desair has no native specialized Move handler.
- Original client callsites, event triggers and any legacy renderer remain unchanged. These new IDs are **registered models and partial behaviors**, not complete visual parity or measured Android performance.

The GitHub connector cannot run local MonoGame/.NET builds. Before merging, validate `git diff --check` and `dotnet build .\Client.Main\Client.Main.csproj`, and only push the main branch if both succeed.
