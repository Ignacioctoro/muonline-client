# BITMAP_MAGIC+1

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 10; hijas: 2
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_MAGIC + 1:
            case BITMAP_MAGIC + 2:
                o->LifeTime = 20;
                if (o->SubType == 4 || o->SubType == 10)
                {
                    o->LifeTime = 40;
                    o->Scale = ((rand() % 50) + 50) / 100.f * 4.f;
                }
                else if (o->SubType == 6)
                {
                    o->LifeTime = 60;
                    o->Scale = ((rand() % 50) + 50) / 100.f * 4.f;
                    VectorCopy(Position, o->StartPosition);
                }
                else if (o->SubType == 7)
                {
                    o->LifeTime = 40;
                    o->Angle[2] = rand() % 360;
                }
                else if (o->SubType == 8)
                {
                    o->LifeTime = 20;
                }
                else if (o->SubType == 9)
                {
                    o->LifeTime = 10;
                    o->Scale = 0.1f;
                }
                else if (o->SubType == 11)
                {
                    o->LifeTime = 20;
                }
                else if (o->SubType == 12)
                {
                    o->LifeTime = 20;
                }
                else i
```
