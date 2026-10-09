# Auditoría ClassicFX — resultado reproducible

- Cliente: `C:\muonline-client-main`
- MuMain fijado en `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`
- Effect: **20/464** tipos conectados (no equivale a porcentaje Season 6).
- Particle (presencia rama Create + Move): **97/98**.
- Joint (presencia rama Create + Move): **31/31**.
- Sprite: **53** entradas de catálogo; renderizador genérico existente, assets/subtipos aún sin validar.
- Effect con metadatos de Create: **121**; muchos tipos requieren inspección de C++.

## Límites de este diagnóstico

Los archivos CSV distinguen una coincidencia textual de una implementación completa. Ningún caso está marcado como visualmente validado solo por lectura de código. La columna Temporada queda SIN CLASIFICAR hasta contrastar una fuente nativa específicamente Season 6.

Los candidatos prioritarios son hipótesis de alcance, no un listado exhaustivo de contenido Season 6.

## Salidas

- `01_Effect_catalogo_completo.csv`: todos los tipos originales, metadatos, subtipos documentados, cobertura local y dependencias.
- `02_Particle_Joint_cobertura_estatica.csv`: referencias de ramas Create/Move, no pruebas de fidelidad.
- `03_Sprite_catalogo.csv`: catálogo completo para verificar imágenes y render.
- `04_Backlog_priorizado.csv`: candidatos pendientes ordenados por prioridad y frecuencia nativa.
- `00_resumen.json`: números generados para comparar futuras auditorías.
