# ClassicFX — Auditoría de Particle (cierre del switch de movimiento)

**Referencia:** `sven-n/MuMain/src/source/Render/Effects/ZzzEffectParticle.cpp`, `MoveParticles()`; cliente `Ignacioctoro/muonline-client`, rama `classicfx-engine-2026-10`, commit base `9e8f4a9eefa710f869111000a37f16295d68c422`.

## 1. Cobertura del movimiento

- **98 casos principales `BITMAP_*` en el switch original** (contados desde `BITMAP_EFFECT` hasta `BITMAP_DAMAGE2`, no los casos anidados de `SubType`).
- Antes de esta corrección: 96 en los dispatchers A–M, `BITMAP_SMOKE` en su condición Type directa de D, y `BITMAP_FLARE_BLUE` como el último `switch` provisional en `ClassicFxRuntime.Particles.cs`.
- Después del parche: `BITMAP_FLARE_BLUE` está en A. **Los 98 pasan por A–M**: los 96 casos explícitos más Smoke y FlareBlue. No se identificaron `Type` omitidos en la comparación de etiquetas.
- El movimiento común `LifeTime -= FrameFactor` y `EnableMove` sigue ejecutándose antes del dispatcher. Se conserva un único pool compartido.

**Alcance:** la cobertura de etiquetas no equivale a haber probado visualmente todos los `SubType` o todas las llamadas entre `CreateParticle`, `MoveParticles` y `RenderParticles`.

## 2. Recursos que siguen sin resolución verificada

| Identificador | Situación auditada | Consecuencia |
| --- | --- | --- |
| `BITMAP_SMOKE + 2` (32103) | `ClassicTextureRepository` no registra el slot; declarado sin referencia verificable | Movimiento presente, pero `QueueParticle` devuelve sin dibujar cuando se usa el `TexType` 32103 |
| `BITMAP_SWORD_FORCE` (32039) | Sin `LoadBitmap`/archivo verificado para este slot | Ídem |
| `BITMAP_CLOUD + 2` (32148) | `CreateParticle.C` puede elegirlo como `TexType` alternativo; slot específico no está registrado | La variante queda sin render al usar ese slot |
| `BITMAP_CHROME + 2` (32044) | `CreateParticle.C` puede elegirlo como `TexType` alternativo; en Main puede ser dinámico según mapa | Requiere verificar carga dinámica por mapa; no asignar arbitrariamente `Chrome02` |

**No asignar texturas parecidas ni desplazar IDs:** resolver los `LoadBitmap` originales y sus dependencias de mapa usando `Data_Broyal` y la referencia C++.

## 3. Dependencias de runtime pendientes de fidelidad

1. **Owner `StartPosition`:** `SPARK + 1`, subtipo 4, depende de `Target->StartPosition`. El bridge actual puede leerlo desde un `ClassicFxHandle` de Particle, no necesariamente desde `WorldObject`.
2. **Effect/Bomb:** `SPARK + 2`, subtipo 1, llama a `RequestClassicBomb`, cuyo bridge espera la futura implementación de `Effect`.
3. **Huesos:** los indices del Main (por ejemplo 18, 20, 28, 37) deben comprobarse en los modelos BMD y `ModelObject.GetBoneTransforms` reales, especialmente para personajes, monstruos y Android.
4. **Buff AG:** `BITMAP_AG_ADDITION_EFFECT` usa `eBuff_AG_Addition = 113` del Main; verificar correspondencia con la tabla de buff IDs del servidor OpenMU y estado del personaje.
5. **Render:** comparar pases de transparencia/depth, texturas `TexType`, sprites secundarios, orden de render y atlas frente al Main. El port está estructurado, pero faltan pruebas de fidelidad visual y rendimiento bajo Android.

## 4. Validación realizada y pendiente

- **Validado por código:** commit M encontrado, dispatcher A–M, cobertura Type 98/98, catálogos de IDs y texturas, comparación de variantes `TexType`.
- **Validado para el parche de cierre:** aplicación y reversión con `git apply` sobre una copia de comprobación que reproduce exactamente la región modificada; variantes LF y CRLF.
- **Pendiente en equipo:** `dotnet build` para Windows y Android, ejecución con datos del cliente, inspección de transparencia, escala, vida, bone attachments y diferencias de FPS en las principales familias.
- **Pendiente de integración:** resolver los recursos faltantes y completar `Effect`/`Joint`/`Blur` compartidos para reproducir las emisiones secundarias.

**No declarar `Particle` visualmente terminado todavía**: solo el *switch de movimiento* tiene cobertura de tipos completa.
