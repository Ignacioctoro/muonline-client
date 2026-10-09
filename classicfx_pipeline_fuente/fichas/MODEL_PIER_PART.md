# MODEL_PIER_PART

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/PierPart.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_PIER_PART:
                if (o->SubType == 0)
                {
                    o->LifeTime = 20;
                    o->Gravity = 2.f;
                    o->Velocity = 10.f;
                    o->HiddenMesh = 1;
                    o->Scale = 1.2f;
                    Vector(1.f, 1.f, 1.f, o->Light);
                    Vector(0.f, -26.f, 0.f, o->Direction);
                    VectorCopy(Light, o->StartPosition);
                    VectorCopy(o->Angle, o->HeadAngle);
                    Vector(0.f, 0.f, Angle[2], o->Angle);
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = o->Owner->LifeTime;
                    o->HiddenMesh = 0;
                    o->Scale = 0.5f;
                    o->Alpha = (float)((20 - o->LifeTime) / 5.f);
                    Vector(0.f, 0.f, 0.f, o->Direction);

                    CreateParticle(BITMAP_FIRE + 1, o->Position, o->Angle, o->Light, 0, 1.f, o);
                }
                else if (o->SubType == 2)
                {
                    o->LifeTime = 20;
                    o->Velocity = 50.f;
                    o->HiddenMesh = -2;
                    o->Position[2] -= (20.f)
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_PIER_PART(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
        if (o->SubType == 0)
        {
            if (o->Owner != NULL)
            {
                VectorCopy(o->Owner->Position, p);
                VectorAdd(p, o->StartPosition, p);

                for (int i = 1; i < o->Gravity; ++i)
                {
                    if (rand_fps_check(2))
                    {
                        if (o->Angle[0] < -90)
                            o->Angle[0] += (20.f) * FPS_ANIMATION_FACTOR;
                        else
                            o->Angle[0] -= (20.f) * FPS_ANIMATION_FACTOR;
                    }
                    MoveHumming(o->Position, o->Angle, p, o->Velocity);
                    o->Velocity += (0.4f) * FPS_ANIMATION_FACTOR;

                    if (o->LifeTime < 10)
                    {
                        o->Velocity += (0.1f) * FPS_ANIMATION_FACTOR;
                    }

                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate(o->Direction, Matrix, Position);
                    VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);

                    CreateEffectFpsChecked(MODEL_PIER_PART, o->Position, o->Angle, o->Light, 1, o);
                }
                o->Gravity += (0.1f) * FPS_ANIMATION_FACTOR;

                PlayBuffer(SOUND_ATTACK_FIRE_BUST_EXP);
            }
        }
        else if (o->SubType == 2)
        {
            if (o->Owner != NULL)
            {
                VectorCopy(o->Owner->Position, p);

                MoveHumming(o->Position, o->Angle, p, o->Velocity);
                o->Velocity += (2.4f) * FPS_ANIMATION_FACTOR;

                AngleMatrix(o->Angle, Matrix);
                VectorRotate(o->Direction, Matrix, Position);
                VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);

                if ((int)o->LifeTime % 3 == 0)
                {
                    Vector(-90.f, 0.f, o->Angle[2], Angle);
                    CreateJointFpsChecked(BITMAP_JOINT_FORCE, o->Position, o->Position, Angle, 2, NULL, 150.f);
                }
            }
        }
        else if (o->SubType == 1)
        {
            if (o->LifeTime < 5)
            {
                o->Alpha *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
            }
        }
        return true;
    }
```
