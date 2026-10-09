# MODEL_POISON

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/Poison[index=1].bmd

## Funcion MoveHandlers

```cpp
bool Move_MODEL_POISON(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        o->BlendMeshLight = o->LifeTime * 0.1f;
        o->Alpha = o->LifeTime * 0.1f;
        Vector(Luminosity * 0.3f, Luminosity * 1.f, Luminosity * 0.6f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        return true;
    }
```
