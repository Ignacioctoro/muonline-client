# MODEL_STAFF_OF_DESTRUCTION

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case MODEL_STAFF_OF_DESTRUCTION:
                case MODEL_CLOUD:
                    RenderObject(o);
                    break;
                case MODEL_SHINE:
                    break;
                case MODEL_WAVE_FORCE:
                    RenderObject(o);
                    break;
                case MODEL_MAGIC_CAPSULE2:
                    RenderObject(o);
                    break;
                case MODEL_AIR_FORCE:
                case MODEL_PIER_PART:
                    RenderObject(o);
                    break;
                case MODEL_PIERCING2:
                    if (o->SubType == 1 || o->SubType == 2)
                        break;
                    RenderObject(o);
                    break;
                case MODEL_PIERCING:
                    if (o->SubType == 3)
                        break;
                    RenderObject(o);
                    break;
                case MODEL_TOWER_GATE_PLANE:
                    RenderObject(o);

                    o->Position[2] = o->StartPosition[2] + 400.f - (o->Position[2] - o->StartPosition[2]);
                    RenderObject(o);
                    break;

                case BATTLE_CASTLE_WALL1:
                case BATTLE
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_STAFF_OF_DESTRUCTION(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        float Height;
        Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
        if (o->Position[2] < Height)
        {
            VectorCopy(o->Position, Position);
            Position[2] += 80;
            CreateParticleFpsChecked(BITMAP_EXPLOTION, Position, o->Angle, Light);

            for (int j = 0; j < 6; j++)
            {
                CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
            }
            o->Live = false;
        }
        Vector(Luminosity * 0.2f, Luminosity * 0.4f, Luminosity * 1.f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        return true;
    }
```
