# MODEL_ICE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Skill/Ice[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ICE:
                switch (o->SubType)
                {
                case 0:
                    o->LifeTime = 50;
                    o->Scale = 0.8f;
                    o->Velocity = 1.f;
                    o->Angle[0] = 0.f;
                    o->BlendMesh = 0;
                    break;

                case 1:
                case 2:
                    o->LifeTime = 20;
                    o->Scale = 0.8f;
                    o->Angle[0] = -20.f;
                    o->BlendMesh = 0;
                    o->BlendMeshLight = 0.5f;
                    o->Gravity = 5.f;
                    Vector(0.f, 0.f, (float)(rand() % 360), o->HeadAngle);

                    for (int i = 0; i < 3; ++i)
                    {
                        vec3_t Position;
                        Vector(o->Position[0] + (float)(rand() % 64 - 32),
                            o->Position[1] + (float)(rand() % 64 - 32),
                            o->Position[2] + (float)(rand() % 128 + 32), Position);
                        CreateParticle(BITMAP_SMOKE, Position, o->Angle, o->Light);
                    }
                    Vector(0.4f, 0.3f, 0.2f, Light);
                    AddTerrainLight(o->
```
