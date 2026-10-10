# Season 6 ClassicFX — Batch 17

**Base verificada:** `classicfx-nova-pilot@e331795054f7762d22adbd3abab0f7e9588f6114`.
**Main fijo:** `sven-n/MuMain@21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.
Fuentes de inicialización, movimiento y render: `src/source/Render/Effects/ZzzEffect.cpp`, `Behaviors/MoveHandlers.cpp`, `Engine/Object/ZzzOpenData.cpp`, `Data/Effects/EffectTypes.json`.

## 17 nuevos IDs (117–133)

- **Halloween (7):** Candy Blue, Orange, Yellow, Red, Hobak, Star (seis BMD de Skill) y `HalloweenEx` emisor lógico sin BMD. Emite exactamente 24 hijos con los pesos originales (Hobak y Star el doble), gravedad, rotación, rebote y fuego en calabaza.
- **Moon Harvest (4):** Gam, Songpuen1, Songpuen2 y Moon. Usa los cuatro BMD `Effect/chusukgam/chusukseung1/chusukseung2/chysukmoon`. Rebotes de frutas, rotación y fade de Luna; subtipos 0, 1, 2.
- **Change Up (3):** `Effect/Change_Up_Eff.bmd`, `Effect/changup_nasa.bmd`, `Effect/clinderlight.bmd`. Ciclo de fases Nasa 100/70/40, juntas FLARE, escalado y seguimiento al dueño para subtipo 2.
- **Flechas (3):** Best Crossbow (`Skill/kcross.bmd`), Drill (`Skill/Carow.bmd`), Ring (`Skill/CW_Bow_Skill.bmd`). Se reutiliza `AdvanceS6Arrow`, los pools Joint y Particle y `MODEL_WAVES(4)`.

**Verificación de assets:** los **16 BMD** se encontraron en `Ignacioctoro/Data_Broyal/main`, respetando sus nombres y rutas. El 17.º tipo es emisor lógico, como en el Main, no requiere BMD.

## Límites conocidos — no equivalen a paridad total

- Falta la implementación del original `BITMAP_MAGIC` subtipo 4 dentro de ClassicFX. Se conserva el Joint de `ChangeUpEffect`, pero **no** se reemplaza el efecto faltante por otro inventado.
- La emisión secundaria `BITMAP_WATERFALL_3` de `MoonHarvestMoon(1)` todavía no se encuentra conectada: quedan sus sprites, `SmokeLine` y `Smoke`.
- `ArrowBestCrossbow`, `ArrowDrill` y `ArrowRing`: los modelos y trazas son visuales. `CheckClientArrow`, `CreateBomb` y el impacto de red no se duplican; no se certifican trayectorias finales.
- El uso del reloj 25-FPS y los pools existentes reduce emisiones superfluas. No hay benchmark Android.
- Los modelos están disponibles mediante `CreateEffect`. No se cambió el registro de habilidades jugables ni se sustituyeron los antiguos.
- No se ejecutó un build en Windows; debe compilarse desde la raíz local.

## Integración

Rama `classicfx-s6-batch17-20261010`; fast-forward de `classicfx-nova-pilot` solo desde el base confirmado. Compilar `Client.Main/Client.Main.csproj` antes de hacer push.
