# ClassicFX Season 6 — Batch 16

**Fuente fija:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`. Se contrastaron `ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `ZzzOpenData.cpp` y las rutas BMD presentes en `Ignacioctoro/Data_Broyal`.

**Base obligatoria:** `Ignacioctoro/muonline-client@classicfx-nova-pilot`, commit `1cff070093c8d11414a74ca59893d9b503628488`.

## 18 tipos nuevos: IDs 99–116

- MODEL_MULTI_SHOT1–3: tres BMD `Effect/multishot01..03.bmd`; expanden malla con scale 0.2/0.3/0.25 por tick y alpha de vida/18.
- MODEL_BIG_STONE_PART1–2: `Skill/Flysmallstone1..2.bmd`, fragmentos físicos, variante 2 y caída 3 de Part2.
- MODEL_WALL_PART1–2: `Skill/wallstone1..2.bmd`, impulso y gravedad reforzada.
- MODEL_GATE_PART1–3: `Skill/gatepart1..3.bmd`, rebote y velocidad del subtipo 1.
- MODEL_GOLEM_STONE: `Skill/golem_stone.bmd`, fuego real, humo y rebotes.
- MODEL_ARROW_STEEL / THUNDER / LASER / V / SAW / SPARK / GAMBLE: siete modelos de `Skill`. Lanzamiento original compartido, desplazamiento visual sin daño, rotaciones especiales de Spark y Gamble, flares y humo.

**Integración:** solo `ClassicFxRuntime`, `EffectState`, el pool ya existente y el BMD renderer `ClassicFxEffectModelObject`. No se han reemplazado skills en el registro ni se modifican mensajes de red, daños o servidor. Los tipos están disponibles desde `CreateEffect` y sus modelos se dibujan por `World.Objects` como los lotes anteriores.

## Límites deliberados

- Flechas: `CheckClientArrow` del C++ gestiona comprobaciones de cliente/colisión y no se duplica en ClassicFX. El desplazamiento usa la aproximación **visual** ya empleada en Batch06; no afirmar paridad en impactos o trayectorias dependientes de un objetivo.
- Los distintos subtipos de pierce generan su portador `Piercing` cuando el modelo está listo; no se crea un sistema nuevo.
- Los fragmentos usan altura real `RequestTerrainHeight`. Algunas animaciones o texturas que antiguamente se cargaban desde carpetas distintas a las del BMD dependen de la resolución de texturas de `ModelObject`; no se certifica su paridad en Android.
- No se ejecutó `dotnet build` en esta sesión (SDK .NET indisponible). Compilar localmente antes de integrar y subir.
- Los 18 registros nuevos **no equivalen** a 18 habilidades jugables ya conectadas: quedan disponibles para invocaciones ClassicFX futuras.

## Instalación desde rama nueva

La rama `classicfx-s6-batch16-20261010` parte exactamente del HEAD indicado. Sobre `classicfx-nova-pilot` con trabajo local preservado, usar `git fetch origin classicfx-s6-batch16-20261010` y `git merge --ff-only FETCH_HEAD`. Compilar, y subir con push únicamente si termina sin errores.
