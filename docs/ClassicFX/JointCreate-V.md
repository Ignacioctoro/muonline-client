# ClassicFX — CreateJoint V / BITMAP_FLARE + 1

**Base:** `Ignacioctoro/muonline-client`, branch `classicfx-engine-2026-10`, commit `17640674b416ba96b7fc45c1ddda3eccdfcf38e0` (U).

**Referencia:** `sven-n/MuMain`, `src/source/Render/Effects/ZzzEffectJoint.cpp`, `CreateJoint()` aprox. líneas 2069–2284.

## Alcance exacto

- Se incorpora `InitializeJointCreateV` al dispatcher P–U, sin cambiar las ramas existentes.
- Se porta el `switch(Skill)` original (0, 1, 3 y sin caso por defecto).
- Subtipos 0–20: LifeTime, velocidad, dirección, escala, luz, textura, renderface, only-one-pass, multiuse y coordenadas exactamente según las ramas relevantes del Main.
- Subtipo 8: `NumTails=-1` y sin cola inicial; los demás reciben el primer quad antes de la inicialización.
- Subtipo 4: genera los diez segmentos iniciales con el helper de colas ya existente.
- Subtipo 7: reproduce deliberadamente `cos(rand()%360)` y `sin(rand()%360)` con radianes, tal como está escrito en C++.
- Subtipos 8–11: requieren snapshot real de `Target` (posición y ángulo); si falta, se rechaza la creación y se libera el slot.
- La duración inicial se toma del **parámetro PKKey** (incluso si el valor predeterminado es negativo) y luego se almacena `j.PKKey=0`.
- `TargetPosition` se restaura al argumento original al final, igual que la rama nativa.
- Los subtipos no incluidos conservan el comportamiento general de la rama, sin efectos suplementarios inventados.

## Pendientes

`MoveJoints()` y `RenderJoints()` aún no están conectados. La compilación por sí sola NO valida el aspecto visual. Se debe validar en Windows y Android. Si los shaders o blend modes aún no tienen equivalencia exacta, se resolverá en el renderer general.

En el código fuente, el comportamiento de la primera cola de `BITMAP_FLARE+1`, subtipo 8, está definido antes del `switch(Type)`; esta implementación lo reproduce explícitamente.
