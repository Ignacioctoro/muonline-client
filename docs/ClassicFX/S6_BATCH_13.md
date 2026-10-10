# ClassicFX Season 6 — Batch 13: Fury Strike / Rageful Blow

**Base:** `classicfx-nova-pilot@6d3ab05ddce5740fe1061191b35e3ff3c1c69dd0`
**Pinned native reference:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`
Sources: `src/source/Render/Effects/ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `Engine/Object/ZzzOpenData.cpp`. Models verified in `Data_Broyal/Data/Skill`.

## New 9 effect types

- `FuryStrike` logical carrier (native `MODEL_SKILL_FURY_STRIKE`): 20-tick lifetime, 330-degree actor yaw offset, HeadAngle +80/+180, gravity 50, original 13/11/10 milestones, 8 Tail BMDs, Explosion particle, spark joints, Wave BMD, initial center trio of EarthQuake models, radial five impacts and outward fissure paths.
- `FuryQuake1..8`: BMD `Skill/EarthQuake01.bmd` through `Skill/EarthQuake08.bmd`, original initial lifetimes (20/35/40/50/60), model blending, position descent, native light-mesh fades, original Stone1/2 debris probabilities, and terrain lights.
- `CreateFuryQuake(variant, nativePkKey,...)` provides explicit `PKKey / 100` scaling; native caller data remains typed, instead of abusing a bone-index argument.
- All child meshes reuse ClassicFX's existing ModelObject renderer and effect pool, while explosion, stones, joints and terrain light reuse the established particle/joint/world bridge.
- The logical root supports Kind 0, 2, and 3 as the existing CreateEffect `subType` argument. The separate native random 0–99 radial seed is tracked in Phase.

## Honest remaining gaps

- **Weapon rendering:** native `RenderFuryStrike()` draws the equipped weapon item, not an EarthQuake BMD; it is **NOT** reproduced in this batch. A proper weapon-model bridge remains required for full visual parity.
- **Adapter switch:** `RagefulBlowSkillEffect` currently creates `RagefulBlowEffect` with its own model/sprite systems. This batch does **not** enable the new FuryStrike root from that adapter, avoiding two simultaneous effects while weapon rendering is incomplete. The new ClassicFX entrypoints are ready for that migration.
- **Terrain permissions:** original Fury Strike radial cracks skip particular wall/water/no-ground tiles. Terrain height is preserved; the map attribute policy needs a concrete scene bridge before full parity.
- **Cameras/audio:** original EarthQuake and Rage Blow sounds are scene responsibilities, not ClassicFX visual emitters. No fake camera shake or invented sound playback.
- **Timing:** visual burst thresholds are gated at native 25Hz and trigger-mask guarded. At >25 FPS output is intentionally not multiplied by renderer FPS.
- **Source-missing variants:** native werewolf colors and Hellas water waves remain unported until those owner/map semantics are available.
- BMD blend-UV scroll (native `BlendMeshTexCoordU`) still needs a MonoGame material bridge.
- Windows compilation and visual checks must be done locally; not claimed by the remotely authored source.

## Install

Branch `classicfx-s6-batch13-20261010`; one commit descending directly from Batch 12. Fast-forward, compile `Client.MainClient.Main.csproj` once, then push after success. Do not run previous PowerShell installers.
