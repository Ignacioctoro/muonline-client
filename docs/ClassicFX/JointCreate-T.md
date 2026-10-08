# ClassicFX — JointCreate T (Fire, Spark + 1 y SpearSkill)

Base cliente: `a39b5a773ddd69d2c34efd26a8bbd768cc3d446d` (`classicfx-engine-2026-10`).
Referencia: `sven-n/MuMain`, `src/source/Render/Effects/ZzzEffectJoint.cpp`, CreateJoint() líneas 1516–1676.

## Ramas portadas
- `BITMAP_JOINT_FIRE`, ID 32221: configuración de cola, velocidad y TargetPosition.Z + 130.
- `BITMAP_SPARK + 1`: subtipos 0/1 y configuración base de cualquier otro subtipo (igual al Main).
- `MODEL_SPEARSKILL`, ID 322: subtipos 0–11 definidos, 14–17 definidos; no existen ramas de creación 12/13 en el Main usado. No inventar comportamientos.
- `BITMAP_LUCKY_SEAL_EFFECT`, ID 32324: solo referencia de TexType, NO se afirma que la textura ya esté registrada.

## Dependencias explícitas
- SpearSkill necesita un Target resoluble; el C++ dereferencia Target al entrar al caso. Se rechaza sin objetivo para evitar acceso inválido.
- SpearSkill 10/11 con `characterIndex != -1` necesita `JointSpearCharacterIndexResolver`, equivalente a `FindCharacterIndex` del Main. No se fabrica una búsqueda.
- SpearSkill 15/17 necesita `JointSpearOwnerPositionResolver`, equivalente a `Target->Owner->Position`. No se presupone que `WorldObject.Parent` sea el mismo owner.
- SpearSkill 14 requiere `priorColor`, porque el C++ usa `vPriorColor` sin comprobación.
- El subtipos 5/6/7 genera una cola inicial mediante `AppendJointTailQ`, respetando el estado `NumTails=-1`.

## Límites actuales
Este bloque porta solo **CreateJoint**. `MoveJoints` y `RenderJoints` siguen pendientes. La textura `BITMAP_LUCKY_SEAL_EFFECT` requiere auditoría de catálogo antes de que sea visible. El hecho de compilar no prueba fidelidad visual.

## Android
Se reutilizan los buffers de colas ya incluidos en la estructura; el bloque no crea buffers temporales en cada frame. Los resolvers son puntos de integración, no implementaciones ficticias.
