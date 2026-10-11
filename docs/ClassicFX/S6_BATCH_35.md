# ClassicFX Season 6 — Batch 35

Pinned MuMain: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Base: `779e20a75efaf3c0951d11ecb16bfbfa1a9e1aa5`.

## Three native effect types (372–374)

| ID | Native effect | Implementation |
|---|---|---|
| 372 | MODEL_WINDFOCE_MIRROR | Exact existing BMD `Effect/wind_foce_mirror.bmd`; frame-scaled spin/growth/light attenuation, follows owner. Requires callback for real `eBuff_Def_up_Ourforces` state; rejects effect if unbound. |
| 373 | MODEL_CHAIN_LIGHTNING | Model-free effect pool carrier, source bones 37/28, target-bound thunder joints and one-time target-bone particles at tick 15. Real target must be supplied using optional `nativeTarget`. |
| 374 | MODEL_TARGETMON_EFFECT | Model-free pool carrier, random real owner bone each reference tick, three original FIRE_HIK/CURSED_LICH particle variants. Subtype 1 pauses emissions without a real target. |

`ChainLightning` and `TargetMonEffect` intentionally create **no BMD ModelView**. Both pass through the common pool and the preexisting joint/particle renderers. No duplicate skill FX renderer was added.

## Integration and remaining parity dependencies

- Add gameplay call sites, explicitly supplying a live target for ChainLightning using `nativeTarget`; current legacy routing is unchanged.
- Bind `Season6DefenseUpOurForcesActive` to the actual character buff system before creating WindForceMirror.
- The chain-lightning target-bone burst samples up to 24 bones to bound Android spikes; original emits across all bones. The owner/target validation and continuous joints are implemented, but **not full parity**.
- Missing MODEL_ARROW_HOLY, MODEL_ARROW_TANKER(_HIT), MODEL_BIG_METEO1–3 assets/mappings were not substituted with unrelated BMDs; they are **not claimed completed**.
- Native joint texture activation/render parity and world/gameplay triggers must be verified in-game. No local `dotnet build` has been executed here (SDK unavailable); use the guarded integration script.

## Batch 35 review fix (2026-10-11)

- Update the existing ModelView.Color from the WindForceMirror native fading Light each frame (it was only assigned at spawn).
- Match native ChainLightning subtype 1/2 rule: source == target yields no joints or target-bone burst; effect still ages normally.
- No new IDs or gameplay trigger changes; compile and verify on the guarded local merge before pushing main.
