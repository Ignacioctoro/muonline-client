# ClassicFX / CreateJoint S — Thunder y Fenrir

**Referencia:** `sven-n/MuMain/src/source/Render/Effects/ZzzEffectJoint.cpp`, `CreateJoint()`, ramas `MODEL_FENRIR_SKILL_THUNDER` (1024–1071), `BITMAP_JOINT_THUNDER` (1101–1377), `BITMAP_JOINT_THUNDER + 1` (1378–1515). Rama cliente: `classicfx-engine-2026-10`, HEAD inicial `b2d2cc65b0efd9f71e9446867a819b4c16ade33e`.

## Implementado

- Modelo `MODEL_FENRIR_SKILL_THUNDER = 390` (enumerador validado de `_enum.h` partiendo de `MAX_WORLD_OBJECTS=160`, `MAX_CLASS=7`). **Tipo lógico de Joint**, no textura. Subtipos 0–7 con `TexType` = `BITMAP_JOINT_THUNDER` o `BITMAP_FLASH`, Light original.
- `BITMAP_JOINT_THUNDER = 32134`: subtipos 0–28 definidos en Main y 33, con velocidades, duraciones, Light, TileMapping, flags, subtipos que generan 9 colas iniciales (10/14), random y `MoveHumming` (7). El subtipo 13 se convierte en 11; 3 conserva LifeTime del PKKey original antes de fijarlo a -1.
- `BITMAP_JOINT_THUNDER + 1`: subtipos 0–12 originales, incluyendo variaciones que parten en altura y rayos de varios pases. No se alteran los `TexType` de otros sistemas.
- `BITMAP_FLASH = 32236`: slot nativo usado por Fenrir subtipos 4–7.
- `MaxTails` sigue escalándose en el dispatcher existente según `Clock.FrameFactor` después del caso.

## Integraciones y límites pendientes

1. **Subtipos Thunder 2 y 21:** requieren la matriz real de hueso `33` del modelo del dueño. `TryTransformOwnerBonePosition()` rechaza si no existe el dato.
2. **Thunder 15 (encadenamiento):** el original inspecciona `CharactersClient`, filtra monstruos vivos y visibles dentro de un radio XY=400 y excluye el owner. La propiedad `JointThunderMonsterScanResolver` admite integrar esa búsqueda sin asignaciones cada frame; hasta conectarlo no aparecen los índices objetivos. El callback debe escribir hasta 5 índices en el arreglo persistente del Joint. La política de deduplicación/orden y la correspondencia entre índices nativos y objetos MonoGame requiere revisión de integración.
3. **Subtipos Thunder 27/28:** requieren `priorColor`; el Main copia el puntero sin verificar null. El port rechaza estas variantes si no se suministra color; no se inventa uno.
4. **Thunder+1 subtipo 12:** requiere escala suficiente para generar un rango positivo de aleatoriedad. El Main usa `% (iScale * 2)` sin comprobación y podría dividir por cero; se rechaza cuando `iScale <= 0`.
5. **Render:** `CreateJoint` solo inicializa; `MoveJoints` y `RenderJoints` siguen desconectados. Las texturas Joint deben auditarse en `ClassicTextureRepository` cuando se implemente renderer.

No se ha verificado el resultado visual ni compilado con .NET en el ambiente generador; ejecutar `dotnet build` y luego pruebas en el proyecto Windows/Android.
