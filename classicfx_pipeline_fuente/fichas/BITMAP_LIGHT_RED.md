# BITMAP_LIGHT_RED

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_LIGHT_RED:
            {
                o->LifeTime = 9999;
                o->Velocity = 0.0f;
                o->Scale = 1.5f;
                Vector(1.0f, 1.0f, 1.0f, o->Light);
                VectorCopy(Position, o->Position);
                if (o->SubType == 1)
                {
                    o->LifeTime = 50;
                }
                else if (o->SubType == 3 || o->SubType == 4)
                {
                    o->Scale = Scale;
                    o->LifeTime = 100;
                    if (o->SubType == 4)
                    {
                        o->LifeTime = 50;
                    }
                    vec3_t Light;
                    float Luminosity = (float)(rand() % 5) * 0.01f;
                    Vector(Luminosity + 1.0f, Luminosity + 0.2f, Luminosity + 0.2f, Light);
                    int range = 2;
                    if (o->SubType == 4)
                    {
                        range = 4;
                    }
                    AddTerrainLight(o->Position[0], o->Position[1], Light, range, PrimaryTerrainLight);
                }
            }
            break;
            case MODEL_WOLF_HEAD_EFFECT2:
            {
                BMD* b
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_LIGHT_RED(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
    {
        if (o->Owner != NULL && o->Owner->Live == true
            && (g_isCharacterBuff(o->Owner, eBuff_Hp_up_Ourforces)
                || g_isCharacterBuff(o->Owner, eBuff_Att_up_Ourforces)
                || g_isCharacterBuff(o->Owner, eBuff_Def_up_Ourforces)))
        {
            o->LifeTime = 10;

            if (g_isCharacterBuff(o->Owner, eBuff_Def_up_Ourforces))
            {
                if (rand() % 70 == 7)
                {
                    if (!SearchEffect(MODEL_WINDFOCE_MIRROR, o->Owner))
                    {
                        vec3_t vLight;
                        Vector(0.6f, 0.6f, 1.0f, vLight);
                        CreateEffectFpsChecked(MODEL_WINDFOCE_MIRROR, o->Owner->Position, o->Angle, vLight, 0, o->Owner, -1, 0, 0, 0, 1.0f);
                    }
                }
            }
        }
        else if (o->SubType == 3 || o->SubType == 4)
        {
            vec3_t Light;
            float Luminosity = (float)(rand() % 5) * 0.1f;
            Vector(Luminosity + 0.4f, Luminosity + 0.0f, Luminosity + 0.0f, Light);
            int range = 2;
            if (o->SubType == 4)
            {
                range = 3;
            }
            AddTerrainLight(o->Position[0], o->Position[1], Light, range, PrimaryTerrainLight);
        }
        else
        {
            o->LifeTime = 0;
        }

        if (g_isCharacterBuff(o->Owner, eBuff_Cloaking) && (o->SubType != 3))
            return true;

        if (g_isCharacterBuff(o->Owner, eBuff_Att_up_Ourforces) || o->SubType == 1)
        {
            vec3_t Light, Angle;
            Vector(0.f, 0.f, 0.f, Angle);
            BMD* b = &Models[o->Owner->Type];
            vec3_t Position;

            if (g_isCharacterBuff(o->Owner, eBuff_Att_up_Ourforces))
            {
                if ((int)o->LifeTime % 2 == 0)
                {
                    VectorCopy(o->Position, Position);
                    Vector(1.0f, 0.12f, 0.0f, Light);
                    b->TransformByObjectBone(Position, o->Owner, (rand_fps_check(2)) ? 26 : 35);
                    CreateParticleFpsChecked(BITMAP_SMOKELINE1, Position, o->Angle, Light, 5, 0.005f, o->Owner);
                }
            }
        }
    }
        return true;
    }
```
