# BITMAP_FIRE+1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_FIRE + 1:
                o->LifeTime = 10;
                AngleMatrix(o->Angle, Matrix);
                Vector(0.f, -60.f, 0.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);
                o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;
                break;
            case BITMAP_FLAME:
                if (o->SubType == 0)
                {
                    o->LifeTime = 40;
                    o->Weapon = CharacterMachine->PacketSerial;
                }
                else if (o->SubType == 1 || o->SubType == 2)
                {
                    o->LifeTime = 10;
                    Vector(0.f, 0.f, 0.f, o->Angle);
                }
                else if (o->SubType == 3)
                {
                    o->LifeTime = 15;
                }
                else if (o->SubType == 4)
                {
                    o->Scale = 0.01f;
                    o->LifeTime = 10;
                    Vector(0.f, 0.f, 0.f, o->Angle);
                }
                else if (o->SubType == 5)
                {
                    o->LifeTime = 20;
                }
                else
```
