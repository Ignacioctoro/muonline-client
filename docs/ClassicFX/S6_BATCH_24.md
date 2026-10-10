# ClassicFX Season 6 — Batch 24: Summoner (13 BMDs)

Base `cdc5a6f482409608bc45e303d33945cb395beb25`. MuMain pinned `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Reference: `EffectTypes.json`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp`.

## Ported

- **13 new IDs (260–272)**: Summoner wrist ring; Sahamutt, Neil, Lagul equipped heads; Sahamutt, Neil, Lagul summons; Neil NIFE1–3 and GROUND1–3. All BMD filenames verified in Data_Broyal Skill/Effect.
- Casting circles `SummonerCasting1/11/111/2/22/222/4` were **already** in `ClassicFxRuntime.EffectModels.cs`, so **no duplicates** introduced.
- Owner-bone 37 wrist ring with original persistent life; native ring 0.7 scale, blend mesh -2.
- Three orbit heads follow owner with original WorldTime oscillator, lifetime renewal and particle-family differences; head subtypes needing nonrendering "aura" behavior are rejected until supported.
- Sahamutt, Neil, Lagul initialize native lifetimes/scales/alpha/velocity. Neil creates three possible knives and three possible ground BMDs once per cast, based on subtype. Six children preserve native 50-tick definitions.
- Reuses one ClassicFX pool, `ClassicFxEffectModelObject`, terrain bridge and 25-FPS clock. No server/combat changes.

## Precise limits

- Native BMD animation frames are not yet bridged to the EffectState. Sahamutt and Neil action/frame-dependent creation are keyed to an **explicit velocity-based phase proxy**, not full animation parity.
- Native Lagul subtype 1 has JOINT* as owner; not supported and deliberately rejected. Summoner head subtype 1 effects are currently rejected instead of drawing a wrong BMD.
- Owner-head orbit checks the native sine fade trigger but **does not incorporate Hero.SafeZone**. Spear joints attached to effect models are pending.
- Sahamutt's CreateBomb3 and bone-anchored particle cascade, plus Lagul CLOUD/TWINTAIL emission, remain pending. Other model movement/particles documented in source.
- No actual summoner skill calls migrated, no on-device render profiling or local .NET build run here. Compile locally before push.
