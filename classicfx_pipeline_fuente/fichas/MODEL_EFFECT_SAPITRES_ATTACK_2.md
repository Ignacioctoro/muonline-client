# MODEL_EFFECT_SAPITRES_ATTACK_2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 2
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Effect/Sapiatttres2.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_EFFECT_SAPITRES_ATTACK_2:
                if (o->Type == MODEL_BIG_STONE1 || o->Type == MODEL_BIG_STONE2)
                {
                    Vector((float)(rand() % 128 - 64), (float)(rand() % 128 - 64), (float)(rand() % 180), p1);
                    VectorAddScaled(o->Position, p1, o->Position, FPS_ANIMATION_FACTOR);
                }

                if (Type == MODEL_ICE_SMALL)
                {
                    o->BlendMesh = 0;
                    o->BlendMeshLight = 0.3f;
                    Vector(0.f, (float)(rand() % 256 + 64) * 0.1f, 0.f, p1);
                    o->Position[2] += (50.f) * FPS_ANIMATION_FACTOR;
                    if (o->SubType == 13)
                    {
                        Vector(0.f, (float)(rand() % 256 + 64) * 0.2, 0.f, p1);
                        o->LifeTime = rand() % 16 + 32;
                        o->Scale = (float)(rand() % 4 + 15) * 0.05f;
                        o->Angle[2] = (float)(rand() % 360);
                        AngleMatrix(o->Angle, Matrix);
                        VectorRotate(p1, Matrix, o->Direction);
                        o->Gravity = (float)(rand() % 16 + 28);
                    }
                    //						o->Scale += 50.f;
```
