# ClassicFX Season 6 — Batch 02 (native model + emission families)

Base: `classicfx-nova-pilot@d930ff3421551d422a21d543a7b46ba8336771e1`
MuMain source: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Implemented

- `MODEL_CIRCLE_LIGHT` subtypes 0-4: `Skill/Circle02.bmd`, native lifetime / two fade envelopes, colored ground illumination, random native stone fragments for 0-2, flare particles and blue flare joints for 3-4.
- `MODEL_STONE1`, `MODEL_STONE2` subtype 0: real Skill/Stone01.bmd / Stone02.bmd, native randomized direction, bounce on actual terrain, gravity, spin and fire particles. Other stone subtypes deliberately rejected until their distinct native movement rules are ported.
- `MODEL_ALICE_DRAIN_LIFE` subtype 0 (no BMD): native 70-tick source/target effect, native glowing particles, bone-18 drain ghost joints, 64-tick all-bone joint burst; a dedicated safe source/target bridge **does not mutate the character's Owner pointer**. Summoner skill ID 214 registered. Target missing => no incorrect caster-only effect.
- No legacy Hellfire, Teleport, Nova, input, pets, maps or tests altered. `CircleLight` is a reusable runtime capability, not a claim that all callers have migrated.

## Known fidelity limits / integration follow-ups

- `MODEL_CIRCLE_LIGHT` native subtype 1 uses dark render mode, which the shared BMD renderer does not yet reproduce; native scrolling `BlendMeshTexCoordU` requires extending ModelObject's shader path. No claim of GPU parity.
- Original `CreateEffectFpsChecked` and `rand_fps_check` are collapsed into one 25Hz logical child-emission gate to avoid high-refresh overproduction. Rate/perf should still be profiled on Android.
- `DrainLife` strengthened ID is not registered until its numeric mapping is verified. Validated base index 214.
- The old `ScrollOfHellFireEffect` remains as the skill-10 visual. Hooking `CircleLight` to cast dispatch requires a deliberate duplicate-visual decision.
- Existing BMD model position/angle/render-state updates are already after the family branch in `MoveEffects()`; no refactor needed.

## Install from clean matching pilot branch

```powershell
cd C:\muonline-client-main
if ((git branch --show-current).Trim() -ne "classicfx-nova-pilot") { throw "Rama inesperada" }
if ((git rev-parse HEAD).Trim() -ne "d930ff3421551d422a21d543a7b46ba8336771e1") { throw "HEAD cambió; no aplicar este parche" }
if (git status --porcelain) { throw "Hay cambios locales" }
$patch = Join-Path $env:TEMP "broyal-classicfx-batch02.patch"
$url = "https://github.com/Ignacioctoro/muonline-client/compare/d930ff3421551d422a21d543a7b46ba8336771e1...classicfx-s6-batch02-20261010.patch"
Invoke-WebRequest -Uri $url -OutFile $patch
git apply --check $patch
if ($LASTEXITCODE -ne 0) { throw "Patch check falló" }
git apply $patch
if ($LASTEXITCODE -ne 0) { throw "Patch apply falló" }
dotnet build .\Client.Main\Client.Main.csproj
if ($LASTEXITCODE -ne 0) { throw "Build falló, no hacer commit" }
git diff --check
```

This branch is source-reviewed, **not compiled or visually tested** on our side; no dotnet SDK is installed in the execution environment.
