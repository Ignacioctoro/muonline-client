# MODEL_SAW

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Saw[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SAW:
                o->LifeTime = 10;
                o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;
                AngleMatrix(o->Angle, Matrix);
                Vector(0.f, -60.f, 0.f, p1);
                VectorRotate(p1, Matrix, o->Direction);
                break;
            case MODEL_LASER:
                if (o->SubType == 0 || o->SubType == 3)
                {
                    o->LifeTime = 1;
                    o->BlendMesh = 0;
                    o->BlendMeshLight = o->Light[0];
                    o->Scale = 1.3f;
                    o->RenderType = RENDER_DARK;
                }
                else
                {
                    o->LifeTime = 30;
                    o->Velocity = 1.f;
                    o->BlendMesh = 0;
                    o->BlendMeshLight = 1.f;
                    o->Scale = 1.3f;
                    o->RenderType = RENDER_DARK;

                    o->Position[2] += (150.f) * FPS_ANIMATION_FACTOR;
                    Vector(0.f, -1.f, 0.f, o->Direction);
                    Vector(1.f, 0.f, 0.f, o->Light);
                    Vector(30.f, 0.f, Angle[2], o->Angle);

                    CreateJoint(BITMAP_JOINT_FORCE, o->Position,
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SAW(OBJECT* o, int index, float Luminosity)
    {
        o->Angle[2] -= (30.f) * FPS_ANIMATION_FACTOR;
        return true;
    }
```
