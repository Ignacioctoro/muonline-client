# BITMAP_GATHERING

Cliente: SIN MAPEO LOCAL
Llamadas directas: 3; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_GATHERING:
                o->LifeTime = 10;

                switch (o->SubType)
                {
                case 0:
                    Vector(0.f, -100.f, 0.f, p1);
                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate(p1, Matrix, p2);
                    VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);

                    o->Position[2] += (150.f) * FPS_ANIMATION_FACTOR;
                    break;
                case 1:
                case 2:
                    VectorCopy(Position, o->StartPosition);
                    o->LifeTime = 20;
                    break;
                case 3:
                    o->LifeTime = 10;
                    Vector(-10.f, 10.f, 0.f, p1);
                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate(p1, Matrix, p2);
                    VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);
                    break;
                }
                break;
            case MODEL_SKILL_BLAST:
                o->LifeTime = 30;
                o->BlendMesh = 0;
                o->Scale = (float)(rand() % 8 + 10) * 0.1f;
                o->Position[0] += (float)
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_GATHERING(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
        if (o->SubType == 1 || o->SubType == 2)
        {
            BMD* b = &Models[o->Owner->Type];
            Vector(0.f, 0.f, 10.f * FPS_ANIMATION_FACTOR, p);
            VectorCopy(o->StartPosition, b->BodyOrigin);
            b->TransformPosition(o->Owner->BoneTransform[33], p, o->Position, true);
        }

        for (int j = 0; j < 3; ++j)
        {
            Vector(0.f, 120.f, 0.f, p);

            if (o->SubType == 3)
            {
                Vector(0.f, 25.f, 0.f, p);
            }

            Vector((float)(rand() % 360), 0.f, (float)(rand() % 360), Angle);
            AngleMatrix(Angle, Matrix);
            VectorRotate(p, Matrix, Position);
            VectorAdd(o->Position, Position, Position);

            if (!rand_fps_check(1))
            {
                break;
            }

            if (o->SubType == 1)
            {
                if (((int)o->LifeTime % 2) == 0)
                {
                    CreateJointFpsChecked(BITMAP_JOINT_THUNDER, Position, o->Position, Angle, 3, NULL, 10.f, 10, 10);
                }
                else
                {
                    CreateParticleFpsChecked(BITMAP_SPARK + 1, Position, Angle, Light, 2, (rand() % 50 + 10) / 100.f, o);
                }
                CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 8 + 8) * 0.2f, o->Light, o, (float)(rand() % 360));
            }
            else if (o->SubType == 2)
            {
                if (((int)o->LifeTime % 2) == 0)
                {
                    CreateJointFpsChecked(BITMAP_JOINT_THUNDER, Position, o->Position, Angle, 3, NULL, 10.f, 10, 10);
                }
                CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 8 + 8) * 0.2f, o->Light, o, (float)(rand() % 360));
            }
            else if (o->SubType == 3)
            {
                CreateParticleFpsChecked(BITMAP_SPARK + 1, Position, Angle, o->Light, 26, (rand() % 10 + 5) / 25.f, o);
            }
            else
            {
                CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 8 + 8) * 0.3f, o->Light, o, (float)(rand() % 360));
                CreateParticleFpsChecked(BITMAP_SPARK + 1, Position, Angle, Light, 2, (rand() % 50 + 10) / 100.f, o);
            }
        }
        return true;
    }
```
