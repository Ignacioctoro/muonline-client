# ClassicFX Season 6 — Batch 15 (lote grande)

**Base:** `classicfx-s6-batch14-20261010@cecfb6131ed109f0b1d1e9694bd7f298a071cf72` (Batch 14 todavía no está en la rama principal al preparar este lote). El Batch 15 desciende directamente del Batch 14 y puede instalar **ambos** con un único fast-forward desde Batch 13.

**Main fijo:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`
Fuentes: `src/source/Render/Effects/ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `Engine/Object/ZzzOpenData.cpp`. Los BMD se contrastaron con `Ignacioctoro/Data_Broyal`.

## 15 tipos nuevos (84–98)

| Tipos ClassicFX | C++ original | Recurso BMD | Variantes |
| --- | --- | --- | --- |
| BrokenIce0, BrokenIce1, BrokenIce2, BrokenIce3 | MODEL_EFFECT_BROKEN_ICE0..3 | Effect/ice_stone00..03.bmd | 0,1,2 |
| CursedStatue1, CursedStatue2 | MODEL_CURSEDTEMPLE_STATUE_PART1/2 | NPC/songck1/2.bmd | 0 |
| SnowmanHead, SnowmanBody | MODEL_XMAS2008_SNOWMAN_HEAD/BODY | Item/xmas/snowman_die_head_model.bmd, snowman_die_body.bmd | 0 |
| Feather, FeatherForeign | MODEL_FEATHER, MODEL_FEATHER_FOREIGN | Skill/darkwing_hetachi.bmd | 0–3 / 4 |
| SapitresAttack1, SapitresAttack2 | MODEL_EFFECT_SAPITRES_ATTACK_1/2 | Effect/Sapiatttres.bmd, Sapiatttres2.bmd | 0 / 0,1,13,14 |
| FlameStrike | MODEL_EFFECT_FLAME_STRIKE | Effect/FlameStrike.bmd | 0 |
| StarShine | MODEL_STAR_SHINE | Sprite BITMAP_SHINY, sin BMD | 0 |
| SapitresAttackCarrier | MODEL_EFFECT_SAPITRES_ATTACK | Emisor lógico, sin BMD | 0 |

**Implementación:** vidas aleatorias originales cuando corresponde, gravedad, rebotes sobre altura real del terreno, atenuación, rotación, fragmentos BrokenIce0..2 y humo de colisión, hielo que invoca Inferno, emisión de Spark/Light, trayectorias Sapitres, modelo de Flame Strike ligado a Owner, plumas con velocidad angular, secuencias StarShine y emisión de diez fragmentos de Sapitres en el inicio más intervalos de seis ticks.

La totalidad de modelos BMD usa **ClassicFxEffectModelObject** / **ModelObject** existente, y las partículas/sprites/joints, los **pools actuales** de ClassicFX. No hay un nuevo renderer, ni lógica de daño, sockets, daño de skills o paquetes de OpenMU.

## Límites que no se ocultaron

- `FlameStrike` inicializa el BMD y fade, pero el C++ crea blurs de **huesos 9/6, 8/5** sincronizados con los frames del caster (`Move_MODEL_EFFECT_FLAME_STRIKE`). Ese puente avanzado todavía no está conectado; no afirmar paridad de Flame Strike.
- `BrokenIce` conserva el comportamiento de la fuente disponible pero los atributos/collisions por tiles no son aún una sustitución fiel de toda la lógica en C++; la reacción de subtipo 1 depende de movimiento gestionado en el caller. El fragmento crea un Inferno2 mediante la API existente (el resultado depende de la validación nativa de ese subtipo).
- `SapitresAttack1` no duplica `CheckTargetRange` ni daño: se mantiene del lado de gameplay. Los fragmentos y proyectiles usan sus entradas visuales.
- `SnowmanBody` usa la animación estándar BMD existente, sin duplicar un `PlayAnimation` manual.
- Variantes existentes que emplean `WorldTime` y `FPS_ANIMATION_FACTOR` usan el reloj y factor de frame de ClassicFX.
- **Se permiten duplicados temporales** a petición del usuario, pero este commit no redirige por sí solo todos los SkillVisualEffect adapters existentes al nuevo runtime: crea las familias completas registradas y sus hijos internos.
- La integración y el diff se verifican remotamente; **no se ejecutó** `dotnet build` de Windows ni pruebas visuales en tiempo real.

## Instalación segura

La rama `classicfx-s6-batch15-20261010` se crea encima de la rama Batch14, y queda **dos commits** por delante de `classicfx-nova-pilot` si todavía está en Batch13. Descarga Batch15 y haz `git merge --ff-only FETCH_HEAD` sobre `classicfx-nova-pilot` con el árbol limpio. Si ya instalaste Batch14, el fast-forward aplicará solamente el commit del Batch15.

Compila `Client.Main/Client.Main.csproj` y haz push solo si termina bien.
