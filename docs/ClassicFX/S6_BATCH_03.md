# ClassicFX S6 Batch 03 — Lightning Shock + source-linked impact effects

## Pinned sources

- Client base: `9e2c5c3a7af034328469602caafe70b90af63297` on `classicfx-nova-pilot`.
- Original `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
- `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp`,
  `_TextureIndex.h` and `EffectTypes.json`.

## Native behavior actually covered

- `MODEL_LIGHTNING_SHOCK`: subtypes 0,1,2 with owner checks, pool lifecycle,
  cast spark layer, falling meteor, collision-triggered subtype 1, expanding
  red impact/shock particle patterns, and target-bound subtype 2.
  Its native source does **not** load a separate BMD model.
- `BITMAP_DAMAGE_01_MONO`: original physical `Effect/damage01mono.OZJ`,
  texture index `BITMAP_SHOCK_WAVE + 1`, ground shader/tessellation,
  subtypes 0/1 with native 20/10 ticks and original scale/fade.
- `MODEL_KNIGHT_PLANCRACK_A`: original `Effect/knight_plancrack_a.bmd`,
  subtype 0/1 with random yaw/size and 25/20-tick fading.
- Skill 230 (Summoner Lightning Shock) registered in attribute-based
  dispatcher. Existing skills untouched. Effect owns its child instances.
- Prefers native 25-Hz logical tick for sprites/particles to avoid 60/120-Hz
  overproduction on Windows and Android.

## Limits / remaining fidelity

- The client lacks the original action animation-frame pointer for the
  emitter; its downward motion begins at the native fallback `LifeTime < 15`
  rather than testing `Owner->AnimationFrame > 6`.
- Native `BITMAP_DAMAGE_01_MONO` and `MODEL_KNIGHT_PLANCRACK_A` use existing
  MonoGame material/mesh presentation; visual parity not yet validated.
- Native impact subtype 1 additionally emits other particle/effect subtypes;
  only the currently supported ClassicFX primitives are used here, with
  no invented textures or alternate renderer.
- Subtype 2 is available through `CreateLightningShock(owner, position,
  angle, 2)` but is not yet hooked to a target-hit packet.
- `CreateSprite`, `CreateParticle`, and `CreateJoint` allocations still
  respect existing capacities; Android profiling and visual parity remain.

## Installation

On the clean `classicfx-nova-pilot` working tree at `9e2c5c3a7af034328469602caafe70b90af63297`:

```powershell
cd C:\muonline-client-main
if ((git branch --show-current).Trim() -ne "classicfx-nova-pilot") { throw "Wrong branch" }
if ((git rev-parse HEAD).Trim() -ne "9e2c5c3a7af034328469602caafe70b90af63297") { throw "HEAD changed" }
if (git status --porcelain) { throw "Working tree dirty" }
$patch = Join-Path $env:TEMP "broyal-classicfx-s6-batch03.patch"
$url = "https://github.com/Ignacioctoro/muonline-client/compare/9e2c5c3a7af034328469602caafe70b90af63297...classicfx-s6-batch03-20261010.patch"
Invoke-WebRequest -Uri $url -OutFile $patch
git apply --check $patch
if ($LASTEXITCODE -ne 0) { throw "Patch rejected" }
git apply $patch
if ($LASTEXITCODE -ne 0) { throw "Patch failed" }
dotnet build .\Client.Main\Client.Main.csproj
if ($LASTEXITCODE -ne 0) { throw "Build failed. Do not commit" }
git diff --check
```

This environment does not provide the .NET SDK or a GPU. The source has been
reviewed, but local compiler/graphics parity remains unverified.
