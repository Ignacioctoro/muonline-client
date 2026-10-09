# MODEL_BIG_STONE2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
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
                {
                    Vector(0.f, (float)(rand() % 256 + 64) * 0.2, 0.f, p1);
                    o->LifeTime = rand() % 16 + 32;
                    o->Scale = (float)(rand() % 4 + 15) * 0.05f;
                    o->Angle[
```
