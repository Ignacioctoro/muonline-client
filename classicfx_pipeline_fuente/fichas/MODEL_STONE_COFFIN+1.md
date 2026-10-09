# MODEL_STONE_COFFIN+1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_STONE_COFFIN + 1:
                Vector(0.f, (float)(rand() % 128 + 32) * 0.1f, 0.f, p1);
                o->Position[2] += (50.f) * FPS_ANIMATION_FACTOR;
                o->LifeTime = rand() % 16 + 32;
                o->Scale = (float)(rand() % 4 + 8) * 0.1f;
                o->Angle[2] = (float)(rand() % 360);
                AngleMatrix(o->Angle, Matrix);
                VectorRotate(p1, Matrix, o->Direction);
                o->Gravity = (float)(rand() % 5 + 2);
                if (o->Type == MODEL_STONE_COFFIN + 1 && o->SubType == 0)
                {
                    o->SubType = 1;
                    o->Gravity += ((float)(rand() % 5)) * FPS_ANIMATION_FACTOR;
                }

                o->Angle[0] = (float)(rand() % 360);
                o->Angle[1] = (float)(rand() % 360);
                o->Angle[2] = (float)(rand() % 360);
                break;
            case MODEL_SHINE:
                if (o->SubType == 0)
                {
                    vec3_t Pos;
                    o->LifeTime = 50;
                    for (int j = 0; j < 10; ++j)
                    {
                        Pos[0] = o->Position[0] + (j - 5) * 12.f - 30.f;
                        Pos[1] = o->
```
