# ClassicFX — porteo guiado por llamadas originales

Origen MuMain fijo: `21728b1e5b03e0763b38ef9e23f79645e0df7ad2`.

- Tipos Effect catalogados: **464**.
- Tipos declarados en ClassicFX: **22**.
- Llamadas directas literales en Character/MagicSkill: **227**.
- Tipos distintos en esas llamadas: **96**.
- Tipos directos sin mapeo local: **89**.

## Limites: NO es una medicion de completitud

Esta herramienta **NO** traduce ni habilita Effect en MonoGame.
Su proposito es producir lotes priorizados por evidencia de uso y fichas nativas,
para dejar de portar al azar los 464 tipos del catalogo.

Una llamada CreateEffect con tipo expresado como variable o por funciones indirectas
no aparece en este recuento. La asociacion a `AT_SKILL_*` por texto cercano es
solo orientativa y **NO valida una relacion de control de flujo**.
Los subtipos y el render visual tampoco quedan verificados por la coincidencia.
Por tanto, los 'no mapeados' de este reporte NO son todos los que necesita Season 6.

## Archivos

- `01_effect_prioridad_fuente.csv`: los 464 tipos ordenados por uso directo e indirecto.
- `02_llamadas_native.csv`: llamadas, fuente y linea para verificar manualmente.
- `03_no_mapeados_directos.txt`: tipos directos que ameritan inspeccion.
- `fichas/`: contexto del codigo C++ para cada tipo invocado.

No se modifica ningun archivo del cliente.
