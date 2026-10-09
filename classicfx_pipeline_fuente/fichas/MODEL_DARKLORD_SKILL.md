# MODEL_DARKLORD_SKILL

Cliente: SIN MAPEO LOCAL
Llamadas directas: 10; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/DarkLordSkill.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_DARKLORD_SKILL:
            {
                o->LifeTime = 10;
                o->Scale = 0.2f;
                o->Velocity = 0.1f;

                if (o->SubType <= 1)
                {
                    float angle = 45.f + (-90.f * o->SubType);

                    if (rand_fps_check(2))
                    {
                        if (o->SubType)
                        {
                            angle = 45.f - 90.f;
                        }
                        else
                        {
                            angle = 45.f;
                        }
                    }
                    Vector(45.f, angle, 0.f, o->Angle);
                }
                else if (o->SubType == 2)
                {
                    o->LifeTime = 12;
                    o->Velocity = 0.4f;
                }
            }
            break;

            case MODEL_GROUND_STONE:
            {
                int TargetX = (int)(o->Position[0] / TERRAIN_SCALE);
                int TargetY = (int)(o->Position[1] / TERRAIN_SCALE);

                WORD wall = TerrainWall[TERRAIN_INDEX(TargetX, TargetY)];

                if ((wall & TW_NOMOVE) != TW_NOMOVE && (wall & TW_NOGROUND) != TW_
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_DARKLORD_SKILL(OBJECT* o, int index, float Luminosity)
    {
        return true;
    }
```
