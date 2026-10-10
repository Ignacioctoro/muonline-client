# ClassicFX S6 — Batch 10

Source: MuMain commit `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`, `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `Engine/Object/ZzzOpenData.cpp`; source assets checked against `Data_Broyal/Data/Skill`.

Base: Batch 09 `8d02914c202da96565622f9eba97fddfadfbb8d3`. This branch is intentionally **not** merged into `classicfx-nova-pilot`. Compilation on user Windows remains unconfirmed.

## Included

- **MODEL_WAVES:** subtypes 0–6, native lifetime, growth/fade/rotation, 60 light joints for subtype 0, two Piercing joints for subtype 1, native PKKey value through `boneIndex` for secondary wave subtypes. Asset: `Skill/m_waves.bmd`.
- **MODEL_PIERCING2:** subtypes 0–2, native init, lifetime, hidden geometry on 1/2, trails spawning WAVES subtype 2 once per native tick; no extra position translation because the native line is commented. Asset `Skill/m_Piercing.bmd`.
- **MODEL_PIER_PART:** subtypes 0–2, parent/child lifetime bridge, homing and forward movement, child particle and force joint, native HiddenMesh variants. Asset `Skill/PierPart.bmd`.

The BMD files continue through the existing MonoGame renderer, and Sprite/Particle/Joint/Effect lifetimes stay in ClassicFX pools. No combat, hit checks, or packet logic was migrated.

## Differences and remaining integration

- `MODEL_WAVES` subtype 1 requests `RENDER_NODEPTH` in Main; the shared BMD view currently does not expose that exact mode. Not a claim of visual parity.
- `PIER_PART` uses the current homing helper (same family as Batch 09 Javelin); precise native frame interpolation still requires runtime comparison.
- `MODEL_SKILL_WHEEL2` was **not** added because its native `RenderWheelWeapon` dynamically renders the character's weapon, not a normal BMD. This requires a dedicated bridge into the current renderer rather than an invented model.
- No skill-ID adapter is introduced without a verified gameplay trigger. The families are available via `ClassicFxRuntime.CreateEffect` for later direct hookups.
- This commit has source-level checks only; `dotnet build` and visual verification must run on Windows, together with the pending Batch 09.
