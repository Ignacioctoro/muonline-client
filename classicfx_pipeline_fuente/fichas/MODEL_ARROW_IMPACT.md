# MODEL_ARROW_IMPACT

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/ArrowImpact.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_IMPACT:
                o->Velocity = 1.f;
                o->LifeTime = 20;
                o->Scale = 1.8f;
                o->BlendMesh = -2;
                o->Direction[1] = -30.f;

                Vector(0.3f, 0.8f, 1.f, o->Light);

                AngleMatrix(o->Angle, Matrix);
                Vector(-10.f, -80.f, 200.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);

                o->Angle[0] = -30.f;
                CreateJoint(BITMAP_FLASH, o->Position, o->Position, o->Angle, 4, o, 40.f, 50);

                o->Weapon = CharacterMachine->PacketSerial;
                break;
            case BITMAP_JOINT_FORCE:
                if (o->SubType == 0)
                {
                    o->LifeTime = 20;
                    Vector(0.f, -550.f, 0.f, o->Direction);
                    Vector(-90.f, 0.f, Angle[2], o->Angle);
                    Vector(0.f, 0.f, rand() % 360, o->HeadAngle);
                    CreateJoint(BITMAP_JOINT_FORCE, o->Position, o->Position, o->Angle, 3, NULL, 80.f);
                    VectorCopy(Position, o->StartPosition);
                }
                else if (o
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_IMPACT(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
        o->Angle[0] -= (5.f) * FPS_ANIMATION_FACTOR;
        o->Direction[1] -= (8.f) * FPS_ANIMATION_FACTOR;

        if (o->LifeTime < 2 && o->SubType == 0 && o->Owner != NULL)
        {
            o->Angle[0] = 90.f;
            o->SubType = 1;

            VectorCopy(o->Owner->Position, Position);
            Position[0] += rand() % 100 - 50.f;
            Position[1] += rand() % 100 - 50.f;
            Position[2] += 1200.f;
            CreateJointFpsChecked(BITMAP_FLASH, Position, Position, o->Angle, 2, o, 50.f);
            CreateJointFpsChecked(BITMAP_FLASH, Position, Position, o->Angle, 3, o, 50.f);
        }
        else if (o->SubType == 1)
        {
            o->Live = false;
        }
        return true;
    }
```
