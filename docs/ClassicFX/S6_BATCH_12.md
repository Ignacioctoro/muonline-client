# ClassicFX Season 6 Batch 12

Reference: sven-n/MuMain commit `21728b1e5b03e0763b38ef9e23f79645e0df7ad2` and Data_Broyal.
Base: classicfx-nova-pilot `42cbf1b3d2fd0fe31874427c2133cd352ade8ed9`.

New types:
- **LightningOrb 69**: native 20/18 tick Sprite and Particle phases (Shiny, Magic, PinLight, Spark, ShockWave, Energy, Smoke). Subtype 1 also emits native FenrirThunder subtype 3 children, preserving ClassicFX Effect handle ownership.
- **FenrirThunder 70**: native Effect/lightning_type01.bmd; subtype 0/1 100 ticks, subtype 2/3 4 ticks, random orientation and scale, original bone binding for 0, original alpha animation for 0/1 and spawn-time light sprite.
- **Magic2 71**: native Skill/Magic02.bmd; 20 tick lifetime, BlendMesh=0, smoke 3 / smoke 11, Shiny and Light sublayers for subtype 2.

All use the existing MonoGame model renderer and ClassicFX pools. No gameplay damage, hit testing or protocol duplicated.

Limitations: Magic2 original BlendMeshTexCoordU animation awaits native UV bridge in ModelObject; native magic Luminosity uses default lighting until source brightness bridge. Lightning Orb CheckTargetRange is intentionally excluded from ClassicFX because it is gameplay collision; the skill adapter must invoke the two phases. Native joint/world visuals need actual Windows runtime verification; no .NET build was run remotely.
