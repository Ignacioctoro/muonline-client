# ClassicFX — CreateJoint Q (Ghost y Spirit)

**Base:** `Ignacioctoro/muonline-client` `classicfx-engine-2026-10`, commit `bc373e302b5dde65896ae11e3c13c88186d76bdf` (CreateJoint P compilado y publicado).
**Referencia:** `sven-n/MuMain/src/source/Render/Effects/ZzzEffectJoint.cpp`, CreateJoint, ramas 603–943.

## Cobertura

- `BITMAP_2LINE_GHOST` subtipos 0 y 1 (otros subtipos sin rama nativa se rechazan).
- `BITMAP_JOINT_SPIRIT` y `BITMAP_JOINT_SPIRIT2`: subtipos 0–25 presentes en el Main, incluidos 21–25.
- Subtipo Spirit 4: generación inicial de 9 colas mediante el algoritmo de `CreateTail` nativo, en el buffer por slot, sin nuevas asignaciones por frame.
- Se conserva el escalado final `MaxTails / FPS_ANIMATION_FACTOR`, limitado a 200, que **faltaba en P**.
- Tipos no porteados no consumen slots invisibles. `MoveJoints()` y `RenderJoints()` siguen pendientes.

## Dependencias que no se reemplazaron por emulaciones

- Spirit 0/5: `CharacterMachine->PacketSerial` se obtiene a través del resolver optativo `JointPacketSerialResolver`.
- Spirit 1: `SOUND_BRANDISH_SWORD03` se solicita mediante `JointBrandishSwordSoundRequested`.
- Ghost 0: `MODEL_DESAIR` y Spirit 24: `MODEL_SUMMONER_SUMMON_LAGUL` se notifican mediante `JointSecondaryEffectRequested`, llevando un `ClassicFxHandle` del joint propietario. Hasta portar Effect, un callback ausente **no genera un efecto secundario visible**.
- Los `TexType` del joint requieren el catálogo/renderer Joint posterior; no son texturas de Particle por defecto.
- Los efectos de sonido y secundarios que requieran integraciones aún no existentes quedan señalados para fases posteriores; su falta no es una prueba de equivalencia visual.

## Rendimiento

Conserva el pool de 500 joints y buffers reutilizables (`200 x 4` vértices por joint). La inicialización del subtipo 4 escribe sobre el buffer sin asignaciones adicionales. El cálculo del máximo de colas se hace una sola vez al crear cada joint.

## Pruebas sugeridas

1. Compilar Windows y Android (si hay workload instalado).
2. Verificar `TryGetJoint` con subtipos Ghost 0/1 y Spirit 0, 4, 6, 21, 24, 25, inspeccionando `LifeTime`, `MaxTails`, `NumTails` y `TexType`.
3. Comprobar que cada joint se libera y el pool sigue con `ActiveJointCount` correcto.
4. Activar primero el renderer y los modelos `Effect` para comparar visualmente con MuMain.
