# BroyalMU ClassicFX Season 6 — Batch 09

Base: classicfx-nova-pilot@3b3a7285957be28a5172b9ee89505a68ece9c7b3
Pinned Main: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2

## Delivered native effect families

| C++ source | C# type | Supported subtypes | Connection |
|---|---|---|---|
| MODEL_SKILL_JAVELIN | Javelin | 0, 1, 2 | Skill 45 target-bound cast (3 models) |
| MODEL_ARROW_IMPACT | ArrowImpact | 0, transitions to 1 | Skill 46; FLASH4 / FLASH2 / FLASH3 |
| MODEL_SKIN_SHELL | SkinShell | 0, 1 | Runtime API, no cast registry guess |
| MODEL_STUN_STONE | StunStone | 0, 1 | Runtime API, 25Hz smoke and stone child |
| BITMAP_CRATER | Crater | 0, 1, 2 | Runtime terrain bitmap, StunStone child |

All model families use the existing MonoGame BMD effect object, pools,
movement dispatcher and lighting. Crater reuses the existing terrain-quad
renderer with the original Effect/cratered.tga texture and AlphaTest blend
semantics. No new graphics pipeline, duplicate Skill renderer, scene handler,
network event or combat damage code.

### Dependencies

- Javelin: Skill/Javelin.bmd + BITMAP_POUNDING_BALL; native target tracking.
- ArrowImpact: Skill/ArrowImpact.bmd + BITMAP_FLASH joints 4, 2 and 3.
- SkinShell: Skill/skinshell.bmd (two light color variants).
- StunStone: Skill/GroundCrystal.bmd, BITMAP_ADV_SMOKE+1 and
  BITMAP_CRATER subtype 1; subtype-1 stone emitter creates subtype-0
  pieces at fixed logical intervals.
- Crater: Effect/cratered.tga from original Main; Data_Broyal contains
  compressed Effect/Cratered.OZT.
- All children are bounded by the existing shared pools.

### Known differences, not falsely marked as pixel parity

- MoveHumming was adapted to MonoGame radians and available world-space
  coordinates. Angle convention and target-interception timing require
  scene-level validation, not repeated primitive probes.
- Deep Impact originally uses the bow-skeleton-derived ArrowPos. The
  adapter launches from the caster world position until the shared
  bow-specific arrow origin is available; no equipment is mutated.
- Native BITMAP_CRATER uses OpenGL alpha-test, whereas MonoGame's common
  AlphaTest mode falls back to nonpremultiplied alpha blending.
- Native target hit/damage/skill packets and packet serials are deliberately
  excluded. Game logic stays in existing OpenMU + Client paths.
- Runtime release of a parent BMD object may truncate any joints that still
  depend on its world-object owner; parity of remaining tails is unverified.
- Status: source/anchor review only. .NET SDK and Windows/Android GPU are
  unavailable in this environment. User compilation is necessary.

### Strict installation

Do not run on any branch or dirty tree other than a clean checkout of
classicfx-nova-pilot exactly at the SHA above. See compare patch:

https://github.com/Ignacioctoro/muonline-client/compare/3b3a7285957be28a5172b9ee89505a68ece9c7b3...classicfx-s6-batch09-20261010.patch

After applying, run dotnet build Client.Main/Client.Main.csproj,
git diff --check and inspect git status. Do not commit on failure.
