# ClassicFX Season 6 — Batch 45 (targeted remaining roots and subtypes)

Base `6197a51eaddf2a253a16461688191822a07b8ff1`; pinned original `MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Added / completed

- **444 SwellMagicBuffCarrier**: genuine model-less `MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF` subtype 0, player-bone violet BITMAP_LIGHT body aura and recurring six-second two-hand `ArrowsRe06` pulse; no new BMD. The owner/buff lifecycle must call `ReleaseEffect` on buff removal. 999-tick native renewal is retained.
- **ArrowsRe06 subtype 0**: previously rejected, now accepted on the existing verified `Effect/arrowsre06.bmd`. 30 ticks; follows indexed player bone, subtracts scale at 1.0/tick and adds 0.5/tick from remaining tick 15. Existing subtype 1 remains 40 ticks and now throttles transient light sprites to the native 25Hz reference.
- **CursedLichFireEmitter original subtypes 0, 2, 3**: completes verified additional `BITMAP_FIRE_CURSEDLICH` motion rooted on the supplied animated owner bone (0), eight random real owner bones with fire-light sprites (2), and paired short fire particles (3). The existing subtypes 1/12 remain.
- **ShinyScatterEmitter original subtypes 1, 2, 3**: live owner-bone emission for 1/2 and animated-bone shiny sprite variant 3. Subtypes 1/2 renew native life 100; 3 uses native 24. Subtype 0 stays available.

## Constraints / limitations

- Sprite/particle emissions are gated at 25Hz to avoid multiplying child objects on Windows/Android high-refresh clients. No separate renderer, texture remap, placeholder BMD or new allocation per update.
- Original native subtype 3 SHINY+6 uses `CreateSprite(..., 0, 1)` where the extra blend flag cannot be passed through current sprite API; the existing pooled sprite blend path is used. Visual blend parity remains to verify.
- `MODEL_ARROWSRE06` subtype 2 in the pinned Main is behind the optional `PJH_ADD_PANDA_CHANGERING` preprocessor flag; it is not claimed for Season 6.
- `BITMAP_FIRE_CURSEDLICH` subtype 4 requests a distinct mono-green particle family absent from native `EffectTypes.json` creation variants; not claimed.
- `MODEL_BIG_METEO1–3`, `MODEL_ARROW_TANKER(_HIT)`, `MODEL_STAFF_OF_DESTRUCTION`, `MODEL_BOSS_ATTACK`, etc. still lack fully verified compatible effect BMD mappings; no fake implementation was added.
- Character-buff lifecycle hooks, skill/monster/event call-site replacement, local compilation and Android visual verification are separate tasks. This batch alone does not disable legacy emitters.

**Validation:** GitHub source tree is committed; Windows .NET 10/MonoGame must pass `dotnet build Client.Main/Client.Main.csproj` before integrating the main branch.
