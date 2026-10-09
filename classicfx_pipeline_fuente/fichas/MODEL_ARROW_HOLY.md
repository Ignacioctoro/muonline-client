# MODEL_ARROW_HOLY

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_HOLY:
                o->LifeTime = 30;
                o->BlendMesh = -2;
                o->Scale = 1.f;
                o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;

                Vector(0.f, -60.f, 0.f, o->Direction);
                AngleMatrix(o->Angle, Matrix);
                Vector(-10.f, -100.f, 15.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorCopy(o->Position, o->StartPosition);
                VectorAdd(o->StartPosition, p2, o->StartPosition);

                Vector(0.f, 0.f, o->Angle[2], Angle);

                if (o->SubType == 1)
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        if (i == 1)
                            CreateJoint(BITMAP_FLARE, o->StartPosition, o->StartPosition, Angle, 25, o, 50.f, -1, 1);
                        else
                            CreateJoint(BITMAP_FLARE, o->StartPosition, o->StartPosition, Angle, 25, o, 50.f);
                    }
                }
                else
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (i == 1)
                            CreateJoint(BITMAP_F
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_HOLY(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        if (o->SubType == 1)
        {
            o->Angle[1] += (60.f) * FPS_ANIMATION_FACTOR;

            if ((int)o->LifeTime == 13)
                CreateEffectFpsChecked(MODEL_PIERCING, o->Position, o->Angle, o->Light, 3, o);
            CheckClientArrow(o);

            Vector(Luminosity * 0.9f, Luminosity * 0.4f, Luminosity * 0.6f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        }
        else
        {
            o->Angle[1] += (30.f) * FPS_ANIMATION_FACTOR;

            Vector(Luminosity * 0.2f, Luminosity * 0.4f, Luminosity * 1.f, Light);
            AddTerrainLight(o->StartPosition[0], o->StartPosition[1], Light, 2, PrimaryTerrainLight);

            if (o->SubType != 0)
            {
                if ((int)o->LifeTime == 13)
                {
                    CreateEffectFpsChecked(MODEL_PIERCING, o->Position, o->Angle, o->Light, 0, o);
                }
                else if ((int)o->LifeTime == 30)
                {
                    o->AttackPoint[0] = 0;
                    o->Kind = 1;
                }
            }
        }
        return true;
    }
```
