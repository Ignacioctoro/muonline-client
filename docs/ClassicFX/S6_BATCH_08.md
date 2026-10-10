# ClassicFX S6 Batch 08 — Dark Lord Fire Scream + Mana Rune

Base (strict): Ignacioctoro/muonline-client classicfx-nova-pilot
5d9c37bf650c8984206bbfe3840d893f51685a21 ("Batch 07").

Pinned original: sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2.

## Actual changes

- New original Effect models: MODEL_DARK_SCREAM, MODEL_DARK_SCREAM_FIRE,
  MODEL_MANA_RUNE (subtypes 0/1).
- Creation via existing pooled CreateEffect and BMD ModelObject renderer;
  no second FX engine and no new GPU pipeline.
- DarkScream model + fire model, 19 logical ticks; landscape-aligned movement;
  25Hz BITMAP_FLAME subtype 8 children; one native JOINT_FORCE (7/20) and
  one BITMAP_BLUE_BLUR per DarkScream model once BMD load completes.
- ManaRune native life/scale/hidden mesh/gravity/alpha phases, including
  exactly one subtype-1 Effect child at threshold (the runtime TriggerMask
  prevents high-refresh duplicate emission).
- Fire Scream skill 78 produces three paired casts in a one-shot registry
  adapter. Does not mutate live caster position or animation.
- Subtype 1 DarkScream is exposed for event use but not automatically
  dispatched from unrelated gameplay.
- No legacy skills, pets, networking, assets, Android-specific controls
  or tests were replaced or deleted.

## Explicit boundaries

- MuMain CheckClientArrow, AttackCharacterRange and cast actions stay
  in client/server combat; never move hit detection to visual engine.
- Current ModelObject render mode is not a verified pixel-equivalent
  translation of MuMain's RENDER_BRIGHT/RENDER_CHROME state.
- Native Model_ManaRune subtype 1 has only been exposed through the parent
  native cascade; Brand of Skill's exact packet/skill-ID mapping is not
  verified, so no ungrounded registry ID is added.
- Requires corresponding files Skill/darkfirescrem01.bmd,
  Skill/darkfirescrem02.bmd and Skill/ManaRune.bmd from MU data.
- This change has no verified .NET build or GPU test in the source-only
  execution environment. Compile once after applying; do not claim
  Season 6 parity based only on source compilation.

## Safe install from the isolated branch

The branch carrying this change is classicfx-s6-batch08-20261010.
Download the GitHub compare patch and apply to an exact clean base:

    cd C:\muonline-client-main
    if ((git branch --show-current).Trim() -ne "classicfx-nova-pilot") { throw "Wrong branch" }
    if ((git rev-parse HEAD).Trim() -ne "5d9c37bf650c8984206bbfe3840d893f51685a21") { throw "Wrong commit" }
    if (git status --porcelain) { throw "Working tree is not clean" }
    $url = "https://github.com/Ignacioctoro/muonline-client/compare/5d9c37bf650c8984206bbfe3840d893f51685a21...classicfx-s6-batch08-20261010.patch"
    $file = Join-Path $env:TEMP "broyal-classicfx-s6-batch08.patch"
    Invoke-WebRequest -Uri $url -OutFile $file
    git apply --check $file
    if ($LASTEXITCODE -ne 0) { throw "Patch cannot apply" }
    git apply $file
    if ($LASTEXITCODE -ne 0) { throw "Patch application failed" }
    dotnet build .\Client.Main\Client.Main.csproj
    if ($LASTEXITCODE -ne 0) { throw "Build failed; do not commit" }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw "Whitespace errors; do not commit" }

A rejected patch makes no changes. The client branch itself is not
updated from this isolated delivery. No global reset or clean is used.
