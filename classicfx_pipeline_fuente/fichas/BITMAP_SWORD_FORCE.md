# BITMAP_SWORD_FORCE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_SWORD_FORCE:
                o->LifeTime = 30;
                if (o->SubType == 0 || o->SubType == 1)
                    Vector(0.8f, 0.8f, 0.8f, o->Light);
                Vector(0.f, 0.f, Angle[2] + 45.f, o->HeadAngle);
                VectorCopy(o->Position, o->StartPosition);
                break;
            case BITMAP_BLIZZARD:
            {
                o->LifeTime = rand() % 15 + 15;
                o->Gravity = -20.f;
                o->Velocity = (float)(rand() % 360);
                Vector(0.f, 0.f, 0.f, o->Light);

                int rangeX, rangeY, rangeZ;
                if (o->SubType == 1)
                {
                    rangeX = 300; rangeY = 150; rangeZ = 700;
                    o->Scale = 0.5f;
                    o->Gravity -= (rand() % 20 + 10) * FPS_ANIMATION_FACTOR;
                }
                else
                {
                    rangeX = 200; rangeY = 100; rangeZ = 500;
                    o->Scale = 0.f;
                }

                o->Position[0] = o->Position[0] + rand() % rangeX - rangeY;
                o->Position[1] = o->Position[1] + rand() % rangeX - rangeY;
                o->Position[2] = o->Position[2] + 500.f;
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_SWORD_FORCE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        if (o->LifeTime == 30.f) // at the first frame of the effect
        {
            VectorCopy(o->Position, Position);
            Position[2] += 100.f;
            if (o->SubType == 1)
            {
                vec3_t Light;
                Vector(1.f, 1.f, 1.f, Light);
                CreateJointFpsChecked(BITMAP_JOINT_FORCE, Position, Position, o->HeadAngle, 10, o->Owner, 150.f, o->PKKey, o->Skill, 0, -1, Light);
            }
            else
                if (o->SubType == 0)
                    CreateJointFpsChecked(BITMAP_JOINT_FORCE, Position, Position, o->HeadAngle, 0, o->Owner, 150.f, o->PKKey, o->Skill);
                else
                    CreateJointFpsChecked(BITMAP_JOINT_FORCE, Position, Position, o->HeadAngle, 8, o->Owner, 150.f, o->PKKey, o->Skill);
        }
        return true;
    }
```
