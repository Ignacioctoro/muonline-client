# ClassicFX Season 6 — Batch 20: Karutan Condra and NarCondra

**HEAD obligatorio:** `classicfx-nova-pilot@ea833d2366cbe93acc43936b51652f8100ef8414`.
**Fuente fija:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
**Nuevo rango:** IDs 165–200, 36 nombres originales, 33 BMD disponibles.

## Familias

- Condra: 10 partes de cuerpo, 7 renderizables y 3 reservadas sin modelo.
- NarCondra: 16 partes del cuerpo con BMD originales.
- Condra: 6 variantes de piedras.
- NarCondra: 4 variantes de piedras.
- Total: 36 IDs; 33 rutas BMD comprobadas en `Data_Broyal/main`.

`MapManager.cpp` define las rutas BMD de las 26 partes de cuerpo (Karutan 80/81) y `ZzzOpenData.cpp` define las diez piedras. `ZzzEffect.cpp` provee inicialización y movimiento, bajo `ASG_ADD_KARUTAN_MONSTERS`. Se consultó la lista de 36.135 entradas de `Data_Broyal`.

## Código portado

- Partes de cuerpo: vida aleatoria 30–59, escala 1.4, impulso relativo transformado por yaw, gravedad 3.5, subtipos aleatorios 0/1, ángulos, rebote altura+20, fricción y fade. Sin emisiones que no existan en esta familia del C++.
- Piedras: variantes 0/1/2 (creación aleatoria, escala y gravedad específicas) y movimiento original heredado de `MODEL_EFFECT_BROKEN_ICE*`. La implementación reutiliza `MoveS6Batch15Ice` para las emisiones nativas, con el pool ya implementado (Inferno, Smoke, BrokenIce, Explosion).
- Solo `EffectState` y `ClassicFxEffectModelObject`, ningún renderer paralelo. `CreateEffect` acepta las 33 variantes que tienen su BMD; la inicialización y el despachador de movimiento quedan conectados.

## Diferencias y límites

- Faltan `Monster/condra_7_pelvis.bmd`, `Monster/condra_7_stomach.bmd` y `Monster/condra_7_neck.bmd`. Se reservan sus tres IDs, pero se rechaza su creación sin reemplazar modelos por geometría inventada.
- La extensión original `ASG_ADD_KARUTAN_MONSTERS` puede no formar parte de la configuración clásica Season 6 de cada distribución. Aquí se portan los casos presentes en el SHA fijo, sin introducirlos en otro contenido jugable.
- No se añadieron llamadas desde skills/monstruos existentes; los 33 modelos quedan disponibles mediante `CreateEffect`. No hay paridad visual comprobada ni benchmark Android.
- No se ejecutó `dotnet build` aquí; compilar localmente antes de integrar a la rama principal.
- No se alteran gameplay, daño, paquetes de red ni servidor OpenMU.

## Integración

Rama `classicfx-s6-batch20-20261010`, un commit directo sobre el HEAD indicado, lista para `git merge --ff-only FETCH_HEAD`.
