# ClassicFX S6 Batch 26: physical combat, projectiles and boss models

Base `c05133506c601a10ff8b4fcb25cb89c86162be9b`; Main native reference `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Added: 12 unique model IDs 283–294

1. ShieldCrash Model/Ring: original Effect/atshild and Effect/atshild2. Subtype 0 now creates exactly one second model when it has a live world owner.
2. Combo: expanding model, falling blend light, native 20-frame lifetime.
3. Fissure and FissureLight: two original bossrock BMDs, 120 ticks, stochastic Stone1/Stone2 creation.
4. Water Wave: original seamanfx model, gravity and 4 waterfall particles per native reference tick.
5. Iron Rider and Kentauros arrows: original arrow BMD and motion; smoke/spark emission and fading.
6. Dragon Lower: native BMD/lifetime/terrain placement, alpha fade.
7. Balgas and Dark Elf model skills: original BMD identities, scaling, mesh lighting and Dark Elf smoke.
8. ArrowAutoLoad: subtype 1 BMD, follows owner bone47. Subtype 0 is an *unrendered repeating emitter* in MuMain and is intentionally not registered as a visible BMD.

Data paths verified in `Data_Broyal/Data/Effect` and `Data/Skill`. One existing ClassicFX effect pool / BMD renderer, no additional renderer or server changes.

## Pending parity / performance context

- ShieldCrash emits the paired BMD, but owner-based shockwave(9) and its remaining secondary sprites are pending.
- Combo's 60 linked BITMAP_LIGHT joints at spawn are not reproduced by this model BMD registration.
- Fissure's terrain lighting, EarthQuake, and per-bone fire/smoke are not implemented in this batch.
- Iron Rider's JOINT_HEALING and sprites/wave child, Kentauros' flare joint, and DragonLower's animation-frame-triggered sparks remain pending.
- ArrowAutoLoad bone47 relative offset -10/5/10 and source subtype 0 periodic emitter are pending. Balgas supports its mesh-light movement but not other native actor triggers.
- These are model registrations: legacy game/skill callers are not automatically migrated. Rendering fidelity cannot be asserted until callsite migration and visual comparison.
- No .NET compiler or Android build accessible in tool environment; guarded user-side build must succeed before push.
