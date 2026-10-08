# ClassicFX — Joint Create W / JOINT_FORCE

**Base exacta:** `Ignacioctoro/muonline-client`, rama `classicfx-engine-2026-10`, commit `8aca866b89ee3be0e7733b8da17021f1ad049658` (bloque V).

**Fuente nativa:** `sven-n/MuMain`, `ZzzEffectJoint.cpp`, `CreateJoint()`, líneas 2285–2421.

## Cambios

- Agrega la inicialización de `BITMAP_JOINT_FORCE` (32215), subtipos `0–8`, `10` y `20`.
- Reproduce `LifeTime`, `Scale`, `Velocity`, `MaxTails`, `NumTails`, `MultiUse`, `HeadAngle`, `Direction`, `TexType` y transformaciones de posición del Main.
- Conserva la distinción entre subtipos `0/10` (posición desplazada sin FPS factor) y `8` (desplazamiento multiplicado por `FPS_ANIMATION_FACTOR`).
- El subtipo `1` requiere la posición y rotación reales del owner; no inventa un target.
- Agrega IDs originales `BITMAP_JOINT_FORCE = 32215`, `BITMAP_INFERNO = 32237`, `BITMAP_LAVA = 32238` (sin renumerar otros IDs).
- **Corrección detectada en auditoría:** V había agregado el archivo `JointCreate.V.cs` pero no lo conectó en `ClassicFxRuntime.Joints.cs`. W conecta tanto V como W.
- **Corrección de V:** el subtipo 3 usa `Velocity=240`, no `270`.

## Límites

Este bloque cubre **creación de estado**. `MoveJoints()` y `RenderJoints()` continúan pendientes, al igual que las cargas finales de texturas para renderizado Joint. El bridge del mundo debe aportar owners auténticos. La velocidad de V se corrige sin alterar otros subtipos.

El factor FPS previo al primer `Update` se trata como 1 en el subtipo Force 8 para no anular el desplazamiento inicial. Este fallback es deliberado; no afirma coincidir con un frame nativo previo a la inicialización del reloj.
