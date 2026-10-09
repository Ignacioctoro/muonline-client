# MODEL_SKILL_WHEEL1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_SKILL_WHEEL1:
                o->LifeTime = 5;
                CharacterMachine->PacketSerial++;
                break;
            case MODEL_SKILL_WHEEL2:
                o->LifeTime = 25;//
                o->Weapon = CharacterMachine->PacketSerial;
                break;
            case MODEL_SKILL_FURY_STRIKE:
            {
                VectorCopy(o->Angle, o->HeadAngle);
                VectorCopy(o->Position, o->StartPosition);

                o->LifeTime = 20;//18;
                o->SubType = rand() % 100;
                o->Angle[2] += 330.f;
                o->HeadAngle[0] += 80.f;
                o->HeadAngle[2] += 180.f;
                o->Gravity = 50.f;

                o->Weapon = CharacterMachine->PacketSerial++;
            }
            break;
            case MODEL_SKILL_FURY_STRIKE + 1:
                if (o->SubType == 0)
                {
                    o->LifeTime = 35;
                    o->Scale = PKKey / 100.f;
                    o->BlendMesh = 0;
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 60;
                    o->Scale = PKKey / 100.f;
                    o->BlendMesh = 0;
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SKILL_WHEEL1(OBJECT* o, int index, float Luminosity)
    {
        CreateEffectFpsChecked(MODEL_SKILL_WHEEL2, o->Position, o->Angle, o->Light, 4 - o->LifeTime, o->Owner, o->PKKey, o->Skill, o->Kind);
        return true;
    }
```
