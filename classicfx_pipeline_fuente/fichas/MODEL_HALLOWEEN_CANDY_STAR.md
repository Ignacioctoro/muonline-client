# MODEL_HALLOWEEN_CANDY_STAR

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Skill/hstar.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_HALLOWEEN_CANDY_STAR:
            {
                if (o->SubType == 0)
                {
                    if (o->Type == MODEL_HALLOWEEN_CANDY_HOBAK)
                        o->Scale = 2.0f + (rand() % 10 - 5) * 0.02f;
                    else if (o->Type == MODEL_HALLOWEEN_CANDY_STAR)
                        o->Scale = 2.0f + (rand() % 10 - 5) * 0.02f;
                    else
                        o->Scale = 0.6f + (rand() % 10 - 5) * 0.02f;
                    o->LifeTime = rand() % 10 + 50;
                    o->Angle[0] = (float)(rand() % 360);
                    o->Angle[1] = (float)(rand() % 360);
                    o->Angle[2] = (float)(rand() % 360);
                    AngleMatrix(o->Angle, Matrix);
                    o->Gravity = (float)(rand() % 10 + 10);
                    vec3_t p;
                    Vector((float)(rand() % 60 - 30) * 0.1f, (float)(rand() % 60 - 30) * 0.1f, 0.f, p);
                    VectorScale(p, 2.0f, p);
                    VectorRotate(p, Matrix, o->Direction);
                    o->m_iAnimation = rand() % 3;
                }
                else if (o->SubType == 1)
                {
                    o->Scale = 2.0f + (rand() % 10 - 5) * 0.02f;
```
