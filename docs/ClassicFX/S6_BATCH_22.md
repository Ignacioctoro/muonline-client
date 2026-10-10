# ClassicFX S6 — Batch 22: native body and heavy debris

Base: `a6287bf029b2eebc6b6e5ecfc78926ed0ab19c54`; reference: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Ported

- **16 original model IDs, 228–243:** Totem Golem parts 1–6, Ice Giant parts 1–6, Bone1/2, BigStone1/2.
- Original BMDs verified in `Data_Broyal/Data/Monster` (12) and `Data_Broyal/Data/Skill` (4).
- Totem Golem: randomized 30–59 life, scale 0.17, gravity 3.5, native angular impulse, 0.8 terrain-contact damping and fade. Reuses Batch 20 physics.
- Ice Giant: parts 1–4 native randomized creation, scale 1.1 and gravity 3.5. Parts 5/6 are present in source MoveEffects/RenderEffects and assets, **but no dedicated CreateEffect initializer exists**; retain existing default creation values. All six reuse Batch 21 movement: terrain +20, damping 0.5, spin and fade.
- Bone and BigStone: original BMDs; supported subtypes **0, 1, 5, 10, 11, 12**, randomized native creation, native Bone Z-offset, BigStone randomized position, owner-follow subtype 5, existing velocity/gravity, terrain rebound and alpha handling. Unsupported subtypes rejected, not represented as complete.
- Existing ClassicFX `EffectState` pool, 25-FPS clock and `ModelObject` renderer reused. No second renderer or gameplay changes.

## Limitations

- No monster death/spawn caller is migrated here: this batch exposes primitives through `CreateEffect` only.
- The owner-follow subtype 5 uses existing WorldObject transform; exact behavior depends on the caller's offset semantics.
- Native random-number call order, complete heavy-stone subtype coverage, model animation parity and Android visual performance are **not** certified.
- Must run `dotnet build .\\Client.Main\\Client.Main.csproj` locally before merging/pushing; no local .NET compilation was executed by GitHub connector.
