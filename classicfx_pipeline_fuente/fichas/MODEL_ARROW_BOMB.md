# MODEL_ARROW_BOMB

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/ArrowBomb[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_BOMB:
            case MODEL_LACEARROW:
            case MODEL_DARK_SCREAM:
            case MODEL_DARK_SCREAM_FIRE:
            case MODEL_ARROW_SPARK:
            case MODEL_ARROW_RING:
            case MODEL_ARROW_TANKER:
            case MODEL_ARROW_DARKSTINGER:
            case MODEL_ARROW_GAMBLE:
                break;
            case MODEL_FIRE:
                if (o->SubType == 1)
                {
                    for (int j = 0; j < 2; j++)
                        CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
                }
                break;

            case BITMAP_ENERGY:
                CreateParticleFpsChecked(BITMAP_SPARK + 1, o->Position, o->Angle, Light, 1, 6.f);
                break;

            case MODEL_LIGHTNING_ORB:
            {
                CreateEffectFpsChecked(MODEL_LIGHTNING_ORB, o->Position, o->Angle, o->Light, 1);
            }
            break;

            case MODEL_SNOW1:
            {
                for (int j = 0; j < 2; j++)
                {
                    CreateEffectFpsChecked(MODEL_SNOW2 + rand() % 2, o->Position, o->Angle, o->Light);
                    CreateParticleFpsChecked(BITMA
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_BOMB(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
    {
        Vector(1.f, 0.6f, 0.4f, Light);
        CreateSprite(BITMAP_LIGHT, o->Position, 1.f, Light, o, (int)WorldTime * 0.1f);
        CreateSprite(BITMAP_LIGHT, o->Position, 2.f, Light, o, -(int)WorldTime * 0.1f);

        for (int j = 0; j < 4; j++)
        {
            Vector((float)(rand() % 16 - 8), (float)(rand() % 16 - 8), (float)(rand() % 16 - 8), Position);
            VectorAdd(Position, o->Position, Position);
            CreateParticleFpsChecked(BITMAP_BUBBLE, Position, o->Angle, o->Light, 1);
        }
        MoveJump(o);
        if ((int)o->LifeTime == 1)
        {
            CreateBomb(o->Position, true);
            if (o->Owner == &Hero->Object && o->SubType != 99)
                AttackCharacterRange(o->Skill, o->Position, 100.f, o->Weapon, o->PKKey);
        }

        Vector(Luminosity * 0.6f, Luminosity * 0.8f, Luminosity * 0.8f, Light);
        AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        CheckClientArrow(o);
    }
        return true;
    }
```
