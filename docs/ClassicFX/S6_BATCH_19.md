# ClassicFX Season 6 — Batch 19 (Imperial Guardian destruction)

**Base:** `classicfx-nova-pilot@3c83f03cd9078e239116c0819f726d0992c5d92b`.
**Main fijo:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

## 19 IDs (146–164), 18 disponibles inmediatamente

- 13 piezas de puerta (`DoorCrushPiece01..13`); 12 con BMD confirmado, una sin asset.
- 4 piezas de estatua (`StatueCrushPiece01..04`), todas con BMD confirmado.
- 2 emisores lógicos (`DoorCrushCarrier` y `StatueCrushCarrier`), sin BMD.

La fuente es `ZzzEffect.cpp` (CreateEffect), `Behaviors/MoveHandlers.cpp` (movimiento) y `World/MapInfra/MapManager.cpp` (carga BMD en mapas Imperial Guardian). Se contrastó `Data_Broyal/main` mediante el árbol completo del repositorio (36.135 entradas): los 16 BMD citados existen en `Data/Effect`.

**Ausencia real:** `Data/Effect/newdoor_break_01.bmd` del original `MODEL_DOOR_CRUSH_EFFECT_PIECE09` no está en Data_Broyal. El ID se reserva pero su creación se rechaza. Al emitir la destrucción de puerta (subtipo 0), el intento de novena pieza no crea objeto: no se sustituye por una malla inventada.

## Comportamiento migrado

- Portadoras: nueve intentos de pieza para puerta (subtipo 0 elige 01–09; subtipo 1 reparte 11–13) y seis para estatua (01–03 repetidos). Se usan offsets nativos de puerta y el efecto se libera en el siguiente Update.
- Fragmentos dinámicos: duración aleatoria 30–59, eje angular aleatorio, impulso transformado por yaw, gravedad 2,3, rotación dependiente del subtipo, altura +20, rebote, fading y emisiones nativas de `MODEL_STONE1/2` y `BITMAP_SMOKE+1` subtipo 6.
- Dos piezas estáticas: life 100, pérdida extra de vida por el Move original y fade acelerado final.
- Pools de ClassicFX y BMD renderer `ClassicFxEffectModelObject` existentes; no se añade renderer ni nueva lógica de daños.

## Límites

- No se habilitan todavía los triggers de evento o mapas; los modelos quedan disponibles vía `CreateEffect`.
- `MODEL_DOOR_CRUSH_EFFECT_PIECE09` requiere conseguir el BMD original, no se generará con reemplazos arbitrarios.
- Fidelidad de iluminación, colisiones y emisión de hijos se debe observar en juego; no se certifica paridad universal ni rendimiento Android.
- No se pudo realizar `dotnet build` en esta sesión; compilar en Windows antes de integrar.
- Sin cambios de red, daño o servidor.

## Integración

Rama `classicfx-s6-batch19-20261010` basada **exactamente** en el HEAD indicado. Un solo commit. Integrar mediante `git merge --ff-only FETCH_HEAD`, compilar y hacer push solo tras una compilación correcta.
