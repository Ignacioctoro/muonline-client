# BITMAP_FLARE_FORCE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 3; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_FLARE_FORCE:
                if (o->SubType == 0)
                {
                    o->LifeTime = 0;
                    if (o->Owner != NULL)
                    {
                        CreateJoint(BITMAP_FLARE_FORCE, Position, Position, Angle, 1, o->Owner, 100.f);
                        CreateJoint(BITMAP_FLARE_FORCE, Position, Position, Angle, 0, o->Owner, 250.f);
                        CreateJoint(BITMAP_FLARE_FORCE, Position, Position, Angle, 2, o->Owner, 100.f);
                        CreateJoint(BITMAP_FLARE_FORCE, Position, Position, Angle, 3, o->Owner, 100.f);
                        CreateJoint(BITMAP_FLARE_FORCE, Position, Position, Angle, 4, o->Owner, 100.f);
                    }
                }
                else if (o->SubType == 1)
                {
                    CreateJoint(BITMAP_FLARE_FORCE, o->Position, o->Position, o->Angle, 5, o->Owner, 20.f, PKKey, SkillIndex);
                    CreateJoint(BITMAP_FLARE_FORCE, o->Position, o->Position, o->Angle, 6, o->Owner, 20.f, PKKey, SkillIndex);
                    CreateJoint(BITMAP_FLARE_FORCE, o->Position, o->Position, o->Angle, 7, o->Owner, 20.f, PKKey, SkillIndex);
                }
                else if
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_FLARE_FORCE(OBJECT* o, int index, float Luminosity)
    {
        return true;
    }
```
