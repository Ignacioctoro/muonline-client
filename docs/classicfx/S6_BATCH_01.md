# ClassicFX Season 6 — engine batch 01

Reference: `classicfx-nova-pilot@6468f739e41bf8e0c62fd23bc95f7bd128f44b51`.
Pinned Main: `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Functional engine changes

The existing `ClassicFxRuntime.Effects.cs` dispatcher now accepts these
native effect types (unsupported subtypes still return an invalid handle):

| Native Effect | C# type | Subtypes |
|---|---|---|
| MODEL_ALICE_BUFFSKILL_EFFECT | AliceBuffSkillEffect | 0,1,2,3,4 |
| MODEL_ALICE_BUFFSKILL_EFFECT2 | AliceBuffSkillEffect2 | 0,1,2 |
| MODEL_SHOCKWAVE_GROUND01 | ShockWaveGround01 | 0,1,2 |
| MODEL_WAVE | Wave | 0 |

`ClassicFxRuntime.EffectSeason6Families.cs` covers creation metadata and
native movement. Rendering uses the existing `ModelObject` BMD bridge,
not a new renderer. Alice cast rings emit sprites/shiny/healing joints and
persistent variants sample the real model bones and emit particles.
`BITMAP_SHINY+5` is registered in the shared texture repository.

**Not yet done:** complete cast dispatcher migration for all seven classes,
visual parity testing, MODEL_ALICE_DRAIN_LIFE (nested owners), Circle Light
stone children, and Lightning Shock. Wave terrain illumination still needs
the native luminosity bridge. The relevant native BMD and shiny05 assets
are present in Data_Broyal. No legacy visual, networking, control, pet or
test files were overwritten.

## Reproducible source audit

With Python 3.10+ and a local MuMain checkout on the pinned commit:

```powershell
python .\tools\classicfx\build_season6_effect_matrix.py --main C:\MuMain --client C:\muonline-client-main --out .\classicfx-audit-out
```

Outputs `season6_effect_matrix.csv` and `season6_effect_audit.json`.
These are static candidates from native call sites including children, not
proof that each record belongs to a Season 6 player skill. Review skill
mapping/class and actual reachable paths before claiming S6 completeness.

## Safe application

All changes are on an isolated branch; the pilot branch is unchanged.
On a clean worktree and matching pilot commit:

```powershell
cd C:\muonline-client-main
git status --short
git branch --show-current
if ((git rev-parse HEAD).Trim() -ne "6468f739e41bf8e0c62fd23bc95f7bd128f44b51") {
    throw "HEAD distinto: no instalar automáticamente sobre esta rama."
}
if (git status --porcelain) { throw "Working tree no está limpio." }
git fetch origin classicfx-s6-effects-batch-20261010
git log --oneline 6468f739e..origin/classicfx-s6-effects-batch-20261010
git cherry-pick 6468f739e41bf8e0c62fd23bc95f7bd128f44b51..origin/classicfx-s6-effects-batch-20261010
dotnet build .\Client.Main\Client.Main.csproj
git diff --check 6468f739e41bf8e0c62fd23bc95f7bd128f44b51 HEAD
```

The branch has not been compiled with dotnet here (not available in this
environment). No GPU parity or Android smoke test has been asserted.

## Integrated Summoner cast handlers

The existing attribute-based `SkillVisualEffectRegistry` now discovers
`AliceBuffSkillEffect.cs` for numeric IDs 217 (Thorns), 218 (Berserker),
219 (Sleep), 220 (Blind), 454 (strengthened Sleep) and 469
(strengthened Berserker). The one-shot adapter creates the existing
`BITMAP_MAGIC+1` cast decal and both native Alice BMD ring models;
Sleep/Blind/Thorns anchor to the actual target, Berserker to the caster.
The runtime fixes initial Alice yaw at creation and avoids double-FPS
filtering inside the logical 25-Hz child spawn gate.

Data_Broyal tree confirms resources `Effect/elshildring.bmd`,
`Effect/elshildring2.bmd`, `Effect/shockwave_ground01.bmd`,
`Skill/flashing.bmd` and `Effect/shiny05.OZJ` exist. This is asset
existence, not a GPU rendering validation.

GitHub also exposes the complete branch-vs-base patch via
`https://github.com/Ignacioctoro/muonline-client/compare/6468f739e41bf8e0c62fd23bc95f7bd128f44b51...classicfx-s6-effects-batch-20261010.patch`.
