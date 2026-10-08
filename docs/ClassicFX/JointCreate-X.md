# ClassicFX — CreateJoint X: BITMAP_LIGHT, BITMAP_PIERCING y BITMAP_FLARE_FORCE

**Base exacta:** Ignacioctoro/muonline-client, branch `classicfx-engine-2026-10`, commit `005bed01724332b8e0df74ef2d3edbea133bae5a` (W).

**Fuente:** `sven-n/MuMain`, `src/source/Render/Effects/ZzzEffectJoint.cpp`, `CreateJoint()` líneas 2422–2578.

## Cobertura

- `BITMAP_LIGHT`: subtipos 0 y 1, incluyendo `Skill`, velocidad y ángulos aleatorios; `CreateTails=false`.
- `BITMAP_PIERCING`: subtipos 0 y 1, preserva `Angle[2]` de entrada, color por `rand_fps_check(2)` y tile mapping en subtipo 1.
- `BITMAP_FLARE_FORCE`: conserva estado común y todas las variantes de subtipos 0, 1–4, 5–7, 11–13, además de la rama nativa para otros valores no cero. Los subtipos 5–7 no crean el primer quad; todos deshabilitan emisión posterior hasta MoveJoints().
- ID `BITMAP_PIERCING` **32275**, cotejado por secuencia contra `_TextureIndex.h`.

## Particularidades de fidelidad

- `FLARE_FORCE` usa `TargetPosition` temporalmente como **ángulos Euler** para rotar el vector desplazamiento. No sustituir por dirección hacia enemigo.
- Antes de resetear el campo `Skill` del joint se guarda su valor original, para conservar `MultiUse = SkillIndex` en subtipos no pertenecientes a los grupos especiales.
- Se mantiene `PKKey` para modificar `Direction.Y` cuando procede.
- Se usa `Clock.FrameFactor`, con fallback 1 si todavía no ocurrió el primer Update, en el desplazamiento inicial de `FLARE_FORCE`.
- El Main construye el quad inicial antes de fijar la escala del switch (como en los bloques P–W). La fidelidad de esta sutileza depende del port global de `InitializeCommon()`.
- Por política del port, LIGHT/PIERCING con subtipos inexistentes se rechazan para no consumir slots de forma permanente.

## Pendiente

El parche **no** conecta `MoveJoints()`, `RenderJoints()`, ni carga nuevas texturas para el futuro renderer Joint. Que el build termine sin errores no confirma efectos visibles.
