# BroyalMU ClassicFX — Season 6 Batch 30

Base: `dd43f3ca0c4d31a9127019c84396e0f8a14662b7`. Native reference: MuMain `21728b1e5b03e0763b38ef9e23f79645e0df7ad2` (ZzzEffect.cpp, MoveHandlers.cpp, ZzzOpenData.cpp and MapManager.cpp).

Eleven native IDs (325–335) registered in the existing pooled BMD renderer and Season 6 model dispatcher:

| IDs | Family | Confirmed Data_Broyal model files |
| --- | --- | --- |
| 325–326 | CursedTempleStatuePart1 / Part2 | NPC/songck1.bmd, NPC/songck2.bmd |
| 327–328 | PkFieldAssassinGreenHead / RedHead | Monster/pk_manhead_green.bmd, Monster/pk_manhead_red.bmd |
| 329–330 | PkFieldAssassinGreenBody / RedBody | Monster/assassin_dieg.bmd, Monster/assassin_dier.bmd |
| 331–332 | StoneCoffin1 / StoneCoffin2 | Object12/StoneCoffin01.bmd, Object12/StoneCoffin02.bmd |
| 333–334 | FlyBigStone1 / FlyBigStone2 | Skill/Flybigstone1.bmd, Skill/Flybigstone2.bmd |
| 335 | FallStoneEffect | Object47/Stoneeffec.bmd |

The batch implements native randomized initialization, debris trajectories, reference-frame particle emissions, terrain collision and bounces, fade-out, PK head movement, Cursed Temple statue ground clamping and falling-stone impact children. Uses the shared ClassicFX pools, runtime clock and existing MonoGame ModelObject BMD renderer.

## Known limitations

- FlyBigStone subtypes 0/1/88/99 intentionally remain unsupported: they depend on Castle Siege catapult targeting, camera changes, collision, and network requests. Only independent native subtype 2 debris is accepted.
- PK assassin heads still need the actual `BITMAP_FIRE_CURSEDLICH` secondary Effect; a generic fire particle would be inaccurate. PK bodies need the proper `MONSTER01_DIE` BMD action mapping; the native action speed is registered.
- Cursed Temple camera `EarthQuake` side effects remain a gameplay/camera integration task. The fixed Z=290 collision plane comes from the native code. Global movement integration should be compared visually.
- The existing client skill/event/map callsites are not yet rerouted and old effects are not removed. Registering a model is not visual parity.
- This branch was authored through the GitHub connector, which does not run `dotnet build`. Build and `git diff --check` must pass locally before pushing to the working branch.

No new BMD renderer, separate particle engine, or fabricated missing asset was added.
