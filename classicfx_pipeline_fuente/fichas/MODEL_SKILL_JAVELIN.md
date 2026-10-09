# MODEL_SKILL_JAVELIN

Cliente: SIN MAPEO LOCAL
Llamadas directas: 3; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Javelin.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SKILL_JAVELIN:
            {
                o->LifeTime = 35;
                o->Gravity = 2.f;
                o->Velocity = 10.f;
                o->Scale = 1.2f;

                Vector(0.f, -5.f, 0.f, o->Direction);
                VectorCopy(o->Angle, o->HeadAngle);
                VectorCopy(o->Owner->Position, o->StartPosition);
                o->Position[2] += (150.f) * FPS_ANIMATION_FACTOR;

                float Ang = rand() % 80 + 10;

                o->HeadAngle[2] += (o->SubType * Ang - Ang) * FPS_ANIMATION_FACTOR;
            }
            break;
            case MODEL_FENRIR_THUNDER:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 100;
                    o->Scale = 0.3f + (float)(rand() % 100) * 0.002f;
                    o->m_iAnimation = 0;
                    o->Alpha = 0.7f;

                    o->Angle[0] = rand() % 360;
                    o->Angle[1] = rand() % 360;
                    o->Angle[2] = rand() % 360;

                    VectorCopy(Light, o->Light);

                    vec3_t vPos;
                    Vector(0.0f, 0.0f, 0.0f, vPos);
                    BMD* p_b = &Models[o->Owner->Type];
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SKILL_JAVELIN(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        float Matrix[3][4];
        float Height;
        if (o->Owner != NULL)
        {
            VectorCopy(o->Owner->Position, o->StartPosition);
            o->StartPosition[2] += (150.f) * FPS_ANIMATION_FACTOR;
        }

        o->Scale += (0.015f) * FPS_ANIMATION_FACTOR;
        if (o->LifeTime < 25)
        {
            float Distance;
            if (o->LifeTime < 20)
            {
                Distance = MoveHumming(o->Position, o->HeadAngle, o->StartPosition, o->Velocity);
                if (Distance < 100.f)
                {
                    VectorCopy(o->StartPosition, o->Position);
                    o->Scale += (0.04f) * FPS_ANIMATION_FACTOR;

                    if ((int)o->LifeTime % 3 == 0)
                    {
                        CreateParticleFpsChecked(BITMAP_POUNDING_BALL, o->Position, o->Angle, o->Light, 1);
                    }
                }
                else
                {
                    CreateParticleFpsChecked(BITMAP_POUNDING_BALL, o->Position, o->Angle, o->Light, 1);
                }
            }
            o->Velocity += (1.5f) * FPS_ANIMATION_FACTOR;

            AngleMatrix(o->HeadAngle, Matrix);
            VectorRotate(o->Direction, Matrix, Position);
            VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);

            o->Gravity = rand() % 30 + 30.f;

            if (o->Direction[1] > -50.f)
            {
                o->Direction[1] -= (8.f) * FPS_ANIMATION_FACTOR;
            }
        }
        else
        {
            o->Gravity += (10.f) * FPS_ANIMATION_FACTOR;
            AngleMatrix(o->HeadAngle, Matrix);
            VectorRotate(o->Direction, Matrix, Position);
            VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);
        }
        o->Angle[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
        o->BlendMeshLight = o->LifeTime / 10.f;
        o->Alpha = o->BlendMeshLight;

        Height = sinf(o->LifeTime * 0.1f) * 30.f;
        if (o->SubType == 1)
        {
            o->Position[2] = o->StartPosition[2] + Height;
        }
        else if (o->SubType == 2)
        {
            o->Position[2] = o->StartPosition[2] - Height;
        }

        Vector(1.f, 0.6f, 0.3f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        return true;
    }
```
