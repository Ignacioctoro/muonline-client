# MODEL_ARROW_BEST_CROSSBOW

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/KCross.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_BEST_CROSSBOW:
                o->LifeTime = 30;
                o->BlendMesh = -2;
                o->Scale = 1.f;
                o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;
                Vector(0.f, -70.f, 0.f, o->Direction);
                VectorCopy(o->Position, o->EyeLeft);
                CreateJoint(BITMAP_JOINT_ENERGY, o->Position, o->Position, o->Angle, 5, o, 100.f);

                if (o->SubType != 0)
                {
                    CreateEffect(MODEL_PIERCING, o->Position, o->Angle, o->Light, 0, o);
                    o->AttackPoint[0] = 0;
                    o->Kind = 1;
                }

                o->Weapon = CharacterMachine->PacketSerial;
                break;

            case MODEL_ARROW_DOUBLE:
                o->LifeTime = 30;
                o->BlendMesh = -2;
                o->Scale = 1.f;
                o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;
                Vector(0.f, -70.f, 0.f, o->Direction);
                VectorCopy(o->Position, o->EyeLeft);
                CreateJoint(BITMAP_JOINT_ENERGY, o->Position, o->Position, o->Angle, 5, o, 100.f);

                if (o->SubType != 0)
                {
                    CreateEffect(M
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_BEST_CROSSBOW(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        if (o->LifeTime > 25)
        {
            o->BlendMeshLight = (30 - o->LifeTime) / 40.f;
        }
        else
        {
            o->BlendMeshLight = 1.f;
        }
        o->Angle[1] += (rand() % 60 + 30.f) * FPS_ANIMATION_FACTOR;
        VectorCopy(o->Position, o->EyeLeft);

        VectorCopy(o->Angle, Angle);
        AngleMatrix(Angle, Matrix);
        VectorRotate(o->Direction, Matrix, Position);
        VectorAdd(o->EyeLeft, Position, o->EyeLeft);

        Vector(Luminosity * 1.f, Luminosity * 0.4f, Luminosity * 0.2f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        CheckClientArrow(o);
        return true;
    }
```
