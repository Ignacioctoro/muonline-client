# ClassicFX Season 6 — Batch 21: Swamp of Quiet Shadow fragments

Base: `classicfx-nova-pilot@38a7127e1169a68cd127a19d62c1325b0c2c0fd2`.
Main original (fixed): `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Delivered
- **27 original models; IDs 201–227**: Shadow Pawn (9), Shadow Knight (9), Shadow Rook (9).
- All **27 original BMD names** verified in `Data_Broyal/Data/Monster`; Rook uses the original `shadow_rock_7_*` prefix.
- Native `CreateEffect` setup: 30–59 reference-frame lifetime, scale 1.1 for Pawn/Knight and 1.3 for Rook, gravity 3.5, randomized angle / lateral impulse / 0-or-1 spin subtype, brightness 0.5–1.0, no initial velocity.
- Native `MoveEffects`: ballistic trajectory, terrain +20 collision, 0.5 horizontal damping, lifetime-dependent bounce, alpha fading and reference-frame angular rotation. No native secondary emission in this branch.
- Existing `ClassicFxRuntime`, 25-FPS reference clock, effect pool, `ClassicFxEffectModelObject` and MonoGame `ModelObject`. No gameplay, network or server changes.
- Models wired into the existing definition, creation, movement and shared rendering dispatchers.

## Limits
- These fragments are available through `CreateEffect`; monster death/spawn callers have **not yet been migrated**, so this does not prove they render in-game automatically.
- `MODEL_EX01_SHADOW_MASTER_*` has separate BMDs but no matching `ZzzEffect.cpp` case at the pinned SHA; they are intentionally **not fabricated**.
- No visual-equivalence certification, Android benchmark or local `dotnet build` is claimed. Build before merging with the PowerShell integration block.
