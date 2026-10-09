# MODEL_ARROW_DRILL

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Carow.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_DRILL:
                if (o->SubType == 0 || o->SubType == 2)
                {
                    o->LifeTime = 30;
                    o->BlendMesh = -2;
                    o->Scale = 1.f;
                    o->Gravity = -10.f;
                    o->Position[2] += (130.f) * FPS_ANIMATION_FACTOR;

                    if (o->SubType != 0)
                    {
                        CreateEffect(MODEL_PIERCING, o->Position, o->Angle, o->Light, 0, o);
                        o->AttackPoint[0] = 0;
                        o->Kind = 1;
                    }

                    Vector(0.f, -70.f, 0.f, o->Direction);
                    VectorCopy(o->Position, o->EyeLeft);
                    CreateJoint(BITMAP_JOINT_ENERGY, o->Position, o->Position, o->Angle, 5, o, 100.f);

                    o->Weapon = CharacterMachine->PacketSerial;
                }
                break;

            case MODEL_COMBO:
                o->LifeTime = 20;
                o->Gravity = 0.1f;
                o->BlendMesh = -2;
                o->BlendMeshLight = 1.f;
                Vector(0.f, 0.f, 0.f, o->Angle);
                VectorCopy(o->Position, o->StartPosition);
                o->Position[2
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_DRILL(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        if (o->SubType == 0 || o->SubType == 2)
        {
            o->Angle[1] += (30.f) * FPS_ANIMATION_FACTOR;

            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, Light, 13);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, Light, 13);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, Light, 13);

            if ((int)o->LifeTime == 1)
            {
                CreateBomb(o->Position, true);
            }

            VectorCopy(o->Position, o->EyeLeft);
            VectorCopy(o->Angle, Angle);
            AngleMatrix(Angle, Matrix);
            VectorRotate(o->Direction, Matrix, Position);
            VectorAdd(o->EyeLeft, Position, o->EyeLeft);

            Vector(Luminosity * 1.f, Luminosity * 0.4f, Luminosity * 0.2f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
            CheckClientArrow(o);
        }
        return true;
    }
```
