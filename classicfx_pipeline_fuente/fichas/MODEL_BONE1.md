# MODEL_BONE1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_BONE1:
                o->Position[2] += (50.f) * FPS_ANIMATION_FACTOR;
            case MODEL_BONE2:
                o->Position[2] += (100.f) * FPS_ANIMATION_FACTOR;
            case MODEL_BIG_STONE1:
            case MODEL_BIG_STONE2:
                if (o->SubType == 5)
                {
                    o->LifeTime = 60;
                    o->Scale = (float)(rand() % 4 + 8) * 0.1f;
                    VectorCopy(o->Position, o->StartPosition);
                    VectorCopy(o->Owner->Position, o->Position);
                    break;
                }
            case MODEL_SNOW2:
            case MODEL_SNOW3:
            case MODEL_STONE1:
            case MODEL_STONE2:
                if (o->SubType == 5)
                {
                    o->LifeTime = 60;
                    o->Scale = (float)(rand() % 4 + 8) * 0.1f;
                    break;
                }
                else if (o->SubType == 11)
                {
                    o->LifeTime = 40;
                    o->Scale = (float)(rand() % 4 + 8) * 0.8f;
                    o->Angle[2] = (float)(rand() % 360);
                    break;
                }
                else if (o->SubType == 10)
```
