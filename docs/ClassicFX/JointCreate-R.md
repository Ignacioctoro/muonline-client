# ClassicFX — CreateJoint R (Laser, Spark, Smoke y Blur)

**Base:** `Ignacioctoro/muonline-client`, rama `classicfx-engine-2026-10`, commit `6e7645da8607ca131c772c823dfc3fd55f4af85f`.

**Referencia:** `sven-n/MuMain/src/source/Render/Effects/ZzzEffectJoint.cpp`, `CreateJoint()` líneas 944–1023 y 1072–1100. Se reserva 1024–1071 (`MODEL_FENRIR_SKILL_THUNDER`) para el bloque S junto con Thunder.

## Port incluido

- `BITMAP_JOINT_LASER` con `TileMapping`, duración 49, velocidad 70, escala original y 6 colas (antes del ajuste final por FPS).
- `BITMAP_JOINT_SPARK`, subtipos 0–5 completos, incluyendo llamadas aleatorias y color por subtipo.
- `BITMAP_SMOKE` usado **como Joint**, subtipos 0–2 con cambios de `TexType` originales.
- `BITMAP_JOINT_LASER + 1` y `BITMAP_BLUR + 1`: se conserva el **fall-through** original y sus ajustes según subtipo 0, 3 u otros.
- Todas las nuevas familias se integran después de P y Q y pasan por el ajuste común `MaxTails / FPS_ANIMATION_FACTOR` que ya existe en el dispatcher Q.

## Precauciones

- La numeración se deriva de `_TextureIndex.h` (sin renumerar los IDs existentes): `BITMAP_BLUR=32018`, `BITMAP_JOINT_LASER=32137`, `BITMAP_JOINT_SPARK=32222`.
- Se preserva la inicialización del primer quad de cola **antes** de que el switch reemplace la escala, como en el Main.
- No se activan `MoveJoints()` ni `RenderJoints()` todavía. La mera compilación no confirma visualización.
- Los tipos `MODEL_FENRIR_SKILL_THUNDER` y `BITMAP_JOINT_THUNDER` van juntos en S. Esto evita adivinar un identificador de modelo y simplifica las dependencias de relámpagos.
- No se crean recursos de textura faltantes de manera arbitraria.

## Validación

Compilar `Client.Main` con el SDK .NET del proyecto. El parche puede validarse primero con `git apply --check`.
