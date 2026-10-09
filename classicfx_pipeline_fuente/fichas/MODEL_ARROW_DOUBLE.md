# MODEL_ARROW_DOUBLE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/ArrowDouble[index=1].bmd

## Fragmento de CreateEffect

```cpp
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
                    CreateEffect(MODEL_PIERCING, o->Position, o->Angle, o->Light, 0, o);
                    o->AttackPoint[0] = 0;
                    o->Kind = 1;
                }

                o->Weapon = CharacterMachine->PacketSerial;
                break;

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
                VectorAdd(o->StartPosition
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_DOUBLE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        o->Angle[1] += (30.f) * FPS_ANIMATION_FACTOR;
        VectorCopy(o->Position, o->EyeLeft);
        Vector(Luminosity * 0.2f, Luminosity * 0.4f, Luminosity * 1.f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        CheckClientArrow(o);
        return true;
    }
```
