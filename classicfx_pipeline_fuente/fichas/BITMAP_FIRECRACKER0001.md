# BITMAP_FIRECRACKER0001

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_FIRECRACKER0001(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
    {
        if ((int)o->LifeTime == 1 || (int)o->LifeTime == 9 || (int)o->LifeTime == 17
            || (int)o->LifeTime == 24 || (int)o->LifeTime == 31)
        {
            Vector(o->Position[0] + (rand() % 200 - 100), o->Position[1] + (rand() % 200 - 100),
                o->Position[2], Position);
            CreateJointFpsChecked(BITMAP_JOINT_SPIRIT, Position, Position, o->Angle, 25, o, 1.f, -1, o->SubType);
        }
    }
        return true;
    }
```
