# MODEL_SHIELD_CRASH2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: Effect/atshild2.bmd

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SHIELD_CRASH2(OBJECT* o, int index, float Luminosity)
    {
    {
        float ftmp = 0.f;
        if (o->LifeTime >= 0 && o->LifeTime < 8)
        {
            ftmp = (float)o->LifeTime / 8;
            VectorScale(o->Direction, ftmp, o->Light);
        }

        if (o->LifeTime >= 8 && o->LifeTime < 24)
        {
            ftmp = 1.f - ((float)(o->LifeTime - 24) / 16);
            VectorScale(o->Direction, ftmp, o->Light);
        }

        VectorCopy(o->Owner->Position, o->Position);
    }
        return true;
    }
```
