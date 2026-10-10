# ClassicFX Season 6 — Batch 18

**Base:** `classicfx-nova-pilot@67feea971c9940f1cb34ef240a7b053497161a44`.
**Main C++ fijo:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## Nuevos tipos: IDs 134–145 (12)

- Christmas (5): `XmasEventBox`, `XmasEventCandy`, `XmasEventTree`, `XmasEventSocks`, `XmasEventIceHeart`.
- New Year's Day (7): `NewYearsDayBeksulki`, `NewYearsDayCandy`, `NewYearsDayMoney`, `NewYearsDayHotPepperGreen`, `NewYearsDayHotPepperRed`, `NewYearsDayPig`, `NewYearsDayYut`.

## Código original y modelos

Referencias consultadas en el SHA fijo:
- `src/source/Render/Effects/ZzzEffect.cpp`, `CreateEffect` (casos Navidad y Año Nuevo).
- `src/source/Render/Effects/Behaviors/MoveHandlers.cpp`, handlers `Move_MODEL_XMAS_EVENT_BOX`, `Move_MODEL_XMAS_EVENT_ICEHEART` y `Move_MODEL_NEWYEARSDAY_EVENT_BEKSULKI`.
- `src/source/GameLogic/Events/Event.cpp`, `CXmasEvent::LoadXmasEventModel` y `CNewYearsDayEvent::LoadModel`.

Los cinco BMD navideños usan `Data/Skill/xmase*.bmd`. Los siete BMD del Año Nuevo usan `Data/Monster/sul*.bmd`. Se verificaron las rutas en `Ignacioctoro/Data_Broyal/main`.

Portado: creación, duración aleatoria, escala, ángulos en radianes, dirección inicial, selección de eje de giro, gravedad, rebotes en la altura del terreno, movimientos y giro de IceHeart. Los pimientos pueden intercambiarse al crearse mediante la probabilidad `rand_fps_check(2)` original. Todo funciona sobre EffectState, pools y el BMD ModelObject existentes.

## Límites explícitos

- `XmasEventIceHeart` en el Main desaparece si el dueño abandona la acción `PLAYER_SANTA_2`. El puente Owner actual no expone ese identificador nativo de acción; se conserva rotación y vida, sin fabricar un estado de animación.
- El cerdo de Año Nuevo recibe una iluminación de cuerpo naranja particular en el RenderObject original. La ruta BMD genérica conserva la luz de llamada, pero no reproduce esa modificación específica por malla.
- `AngleMatrix` C++ y `Matrix.CreateFromYawPitchRoll` pueden diferir en convenciones de eje. No se certifica paridad visual universal.
- Los 12 tipos son modelos invocables desde `CreateEffect`; todavía no se reemplazaron triggers visuales de evento ni registro de skills.
- No se pudo compilar desde esta sesión porque el entorno no tiene SDK .NET; compilar localmente antes de integrar.
- No se modificaron daños, red, OpenMU, eventos ni se añadieron renderers paralelos.

## Integración

Rama `classicfx-s6-batch18-20261010`, creada exclusivamente desde el HEAD indicado. En local, realizar fast-forward, compilar y solo entonces hacer push de `classicfx-nova-pilot`.
