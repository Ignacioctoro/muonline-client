# MODEL_CIRCLE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Circle[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_CIRCLE:
                o->LifeTime = 45;
                o->BlendMesh = 0;
                if (o->SubType == 0)
                {
                    if (o->Owner == &Hero->Object)
                    {
                        o->Weapon = CharacterMachine->PacketSerial++;
                        AttackCharacterRange(o->Skill, o->Position, 300.f, o->Weapon, o->PKKey);
                    }
                }
                else if (o->SubType == 1 || o->SubType == 4)
                {
                    o->LifeTime = 45;
                    o->Scale = 1.f;
                    o->HiddenMesh = -2;
                }
                else if (o->SubType == 2)
                {
                    o->LifeTime = 250;
                    o->Scale = 1.f;
                }
                else if (o->SubType == 3)
                {
                    o->LifeTime = 30;
                    o->Scale = 1.f;
                }
                break;
            case MODEL_ICE:
                switch (o->SubType)
                {
                case 0:
                    o->LifeTime = 50;
                    o->Scale = 0.8f;
                    o->Velocity = 1.f;
                    o->Angle[0] = 0.f
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_CIRCLE(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Angle;
        vec3_t Position;
        o->BlendMeshLight = o->LifeTime * 0.1f;
        if (o->SubType == 1)
        {
            if (o->LifeTime > (44 - o->Owner->m_bySkillCount))
            {
                vec3_t Angle = { 0.0f, 0.0f, 0.0f };
                int iCount = 36;
                for (int i = 0; i < iCount; ++i)
                {
                    Angle[0] = -10.f;
                    Angle[1] = 0.f;
                    Angle[2] = i * (10.f + rand() % 10);
                    vec3_t Position;
                    VectorCopy(o->Position, Position);
                    Position[2] += 100.f;
                    CreateJointFpsChecked(BITMAP_JOINT_SPIRIT, Position, Position, Angle, 6, o, 60.f, 0, 0);

                    if ((int)o->LifeTime == (44 - o->Owner->m_bySkillCount + 1))
                    {
                        Angle[2] = i * 10.f;
                        CreateJointFpsChecked(BITMAP_JOINT_SPIRIT, Position, Position, Angle, 7, o, 60.f, 0, 0);
                    }
                }
            }
        }
        else if (o->SubType == 2)
        {
            if (o->LifeTime > 240)
            {
                o->BlendMeshLight = (250 - o->LifeTime) * 0.1f;
            }
            else
            {
                o->BlendMeshLight = o->LifeTime * 0.1f;
            }
        }
        else if (o->SubType == 3)
        {
            if (o->LifeTime > 10)
            {
                o->BlendMeshLight = (30 - o->LifeTime) * 0.1f;
            }
            else
            {
                o->BlendMeshLight = o->LifeTime * 0.1f;
            }
        }
        else if (o->SubType == 4)
        {
            if (o->LifeTime > (44 - o->Owner->m_bySkillCount))
            {
                vec3_t Angle, Position;
                for (int i = 0; i < 36; ++i)
                {
                    Angle[0] = -10.f;
                    Angle[1] = 0.f;
                    Angle[2] = i * 10.f;

                    VectorCopy(o->Position, Position);
                    Position[2] += 100.f;
                    CreateJointFpsChecked(BITMAP_JOINT_SPIRIT, Position, Position, Angle, 22, o, 2.0f, 0, 0);
                    CreateJointFpsChecked(BITMAP_JOINT_SPIRIT, Position, Position, Angle, 23, o, 1.75f, 0, 0);
                }
            }
        }
        return true;
    }
```
