# MODEL_SPEARSKILL

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/RidingSpear[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_SPEARSKILL:
                o->LifeTime = 20;
                o->Scale = 1.5f;
                o->Direction[0] = 5.0f * sinf(o->Angle[2] * Q_PI / 180.0f);
                o->Direction[1] = -5.0f * cosf(o->Angle[2] * Q_PI / 180.0f);
                break;
            case MODEL_SWELL_OF_MAGICPOWER_BUFF_EFF:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 999;
                    o->Timer = WorldTime;
                }
            }break;
            case MODEL_SUMMONER_EQUIP_HEAD_SAHAMUTT:
                if (o->SubType == 0)
                {
                    o->LifeTime = 100;
                    o->Scale = 0.8f;
                    Vector(0.f, 0.f, 0.f, o->Direction);
                    o->Alpha = 0.0f;
                    OBJECT* pObject = o->Owner;
                    o->Position[0] = pObject->Position[0] + cosf(WorldTime * 0.003f) * 40.0f;
                    o->Position[1] = pObject->Position[1] + sinf(WorldTime * 0.003f) * 40.0f;
                    o->Position[2] = pObject->Position[2] + (sinf(WorldTime * 0.0010f) + 2.0f) * 80.0f - 60.0f;
                }
                else if (o->SubType == 1)
                {
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_SPEARSKILL(OBJECT* o, int index, float Luminosity)
    {
        return true;
    }
```
