# MODEL_PIERCING+1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 5; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_PIERCING + 1:
                o->Velocity = 1.f;
                o->LifeTime = 30;
                AngleMatrix(o->Angle, Matrix);
                Vector(-10.f, -60.f, 135.f, p1);
                VectorRotate(p1, Matrix, p2);
                VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);

                o->Scale = 0.8f;
                o->Direction[1] = -70.f;

                CreateEffect(MODEL_PIERCING, o->Position, o->Angle, o->Light, SubType, o);
                break;

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

                o->Weapon = CharacterMachine->PacketSe
```
