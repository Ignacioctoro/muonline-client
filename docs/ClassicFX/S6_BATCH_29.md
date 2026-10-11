# ClassicFX Season 6 Batch 29 — ranged, ground and siege effects

Base: `74cff418c639bca7007ee5b1cca960ae23f26f84`. Pinned native MuMain: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

Ten new `ClassicFxEffectType` IDs, **315–324**, linked through `TryGetSeason6ModelDefinition` and `MoveSeason6Model` into the existing pooled BMD renderer.

| ID | Type | BMD |
|---|---|---|
| 315 | ArrowBasicModel | Skill/Arrow01.bmd |
| 316 | ArrowDarkStingerModel | Skill/sketbows_arrows.bmd |
| 317 | LaceArrowModel | Skill/LaceArrow.bmd |
| 318 | GroundStoneModel | Skill/GroundStone.bmd |
| 319 | GroundStone2Model | Skill/GroundStone2.bmd |
| 320 | SkullModel | Skill/skull.bmd |
| 321 | ProtectGuildModel | Skill/protectguild.bmd |
| 322 | DeathSpiSkillModel | Skill/deathsp_eff.bmd |
| 323 | WoosiStoneModel | Skill/woositone.bmd |
| 324 | DungeonStoneModel | Object2/DungeonStone01.bmd |

Behaviour: shared native arrow MoveParticle rotation/translation and reference-tick particles, Dark Stinger bone-local twin sprites and timed feather/joint spawns, Lace Arrow flare particles and Waves subtype 3, 40-tick GroundStone creation and last-frame fade, Skull subtype 1 owner tracking and energy joints, ProtectGuild bone20 follow + spark particles, WoosiStone subtype initialization, and dungeon stone bouncing.

### Explicit fidelity limitations

- `CheckClientArrow` is a gameplay/terrain collision routine and is not yet connected to the ClassicFX caller; do not remove original projectile paths until hit processing is ported.
- Ground Stone native creation checks `TerrainWall` NOMOVE/NOGROUND/WATER flags. This adapter currently cannot query them. Callers must check or the bridge must be added.
- Death Spike subtype 0 registers native BMD and initial state, but native `MoveHumming`, tail/particle and sound need character target/skill adapter. Subtype 1 is refused because native copies effect-owner life.
- Skull's native angular jitter and trail fine-tuning remain; homing position and decay are provided.
- `DungeonStone01` is loaded by Dungeon map object code, not the Skill model loader. Its BMD exists in `Data_Broyal` at `Data/Object2/DungeonStone01.bmd`.
- ProtectGuild's complete ownership/hero-coordinate and buff-state handling remains at caller level.
- These are registered model behaviors; none of the legacy callers were automatically substituted. No visual parity or Android throughput asserted. `dotnet build` must be executed locally.
