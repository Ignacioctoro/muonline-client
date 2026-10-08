# ClassicFX — CreateJoint U / Flare + FlareBlue

**Base:** Ignacioctoro/muonline-client, `classicfx-engine-2026-10`, commit `f366a47519b7337ead8133c00c3bc81b44b7dc7e` (bloque T).  
**Referencia:** `sven-n/MuMain`, `src/source/Render/Effects/ZzzEffectJoint.cpp`, `CreateJoint()` líneas 1677–2068.

## Cobertura

`InitializeJointCreateU` porta la rama conjunta `BITMAP_FLARE`/`BITMAP_FLARE_BLUE`. Preserva los tipos, selección de TexType, duración, colas, factor FPS, aleatoriedad, luz, direcciones y posición. Incluye las ramas `21`, `22`, `40` del código fuente protegidas por `GUILD_WAR_EVENT`; están disponibles en el port aunque la configuración de compilación nativa pueda no incluirlas.

- `SubType 20`: usa `TryGetOwnerBonePosition(owner,33)` de `ModelObject`; rechaza el joint si el attachment real no está disponible.
- `SubType 0/18`, `10` y `4`: utilizan el `Light` real del target. Si no existe target, rechazan el joint antes de que ocupe un slot permanente.
- `SubType 3`: emite el `CreateSprite(BITMAP_SHINY+1)` secundario mediante el pool `Sprite` existente. No usa geometría inventada.
- `SubType 9`, `16`: construyen el trail en `CreateJoint()`, con `AppendJointTailQ()` ya implementado en Q.
- `SubType 23`: respeta `NumTails = -1`, `PKKey`, `HeadAngle`, `OnlyOneRender`/`RenderFace` y las posiciones específicas.
- `SubType` desconocido: el Main conserva valores comunes y desactiva futuras colas; se reproduce esa rama sin asignar valores ficticios.

## Pendientes reales, no cubiertos por este parche

1. `MoveJoints()` y `RenderJoints()` siguen sin activarse; que compile no significa que estos joints sean visibles.
2. Auditoría visual de la transformación del hueso 33 frente a `Models[type].TransformPosition` del Main, especialmente en Android.
3. Revisión de los subtipos condicionados por `GUILD_WAR_EVENT` según la configuración de compilación base exacta.
4. Cobertura de texturas `TexType` usada por Joint cuando exista su renderer; `CreateParticle` tiene un catálogo independiente.
5. `Clock.FrameFactor` antes del primer `Update` se usa como `0` en el movimiento inicial de los casos que ejecutan la fórmula durante creación; este comportamiento ya se hereda de Q/S y requiere una prueba de integración específica.

No se modifican reglas previas P–T ni la lógica del servidor OpenMU.
