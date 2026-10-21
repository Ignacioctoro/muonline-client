# Native companion visual reference

Verified against the local MuMain source and both Season 6E3 and Data_Broyal
Local/pet.bmd. No proprietary assets are committed.

| Pet | World model scale | Native source |
| --- | --- | --- |
| Guardian Angel | 1.0 | GOBoid.cpp RenderBoid (in-game) |
| Imp | 1.0 | GOBoid.cpp RenderBoid (in-game) |
| Demon | 0.2 | pet.bmd type 64 (both datasets) |
| Panda | 0.8 | pet.bmd type 80 (both datasets) |

The initial 0.7 in w_BasePet.cpp is only a default: w_PetProcess.cpp replaces
it with the pet.bmd scale. Demon and Panda use animation speed 0.5 per native
25-Hz tick (12.5 keys/sec). Scale is compensated for the player parent matrix.

## Terrain correction
RequestTerrainHeight returns the unscaled collision height. TerrainRenderer
draws HeightMap.R * 1.5 and adds the special Height flag elevation per vertex.
RequestTerrainRenderHeight reconstructs those triangles. All Panda ground
queries now use that render-height API. Animated mesh fitting remains in place,
but targets the visible surface, not the lower collision surface. Do not
multiply the result again or change global terrain physics to fix a pet.

For example, a flat heightmap value of 100 draws at 150: fitting the Panda's
feet to 100 necessarily buries it by 50 units. The earlier fixed +20 could not
correct this map-dependent error.

## Effects
- Angel: native spark subtype 1, scale 1, plus ephemeral green/gold light sprite.
- Imp: no continuous smoke in GOBoid; removed the invented smoke/light trail.
- Demon: native smoke bones 34/51/61/56/66, cloud subtype 11 scale 0.5;
  bone 44 fire subtype 1 scale 0.4; lightmarks and flare sprites at 8/52/58/44.
- Panda: green light+3 particles at bone 7 and pulsing light sprites at bone 4,
  with native 1.5/2.5 sprite scale plus the original 0..0.2 pulse.

Particles use bounded 25-Hz ticks; ephemeral sprites are emitted per draw.
Low quality reduces particle counts, not model or sprite size. Following
movement remains a time-based approximation, not a complete port of GOBoid
steering or Panda's collector state machine. Panda does not auto-loot Zen.

Validation: source/asset inspection and C# syntax only; no build or game run.
Check Panda in Noria and on slopes, Demon relative size, remote equip changes,
and effects at low/high FPS on Windows and Android.
