# MODEL_DARK_SCREAM_FIRE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 3; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/darkfirescrem01.bmd

## Fragmento de CreateEffect

```cpp
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
                    CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light);
                }
                PlayBuffer(SOUND_BREAK01);
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_DARK_SCREAM_FIRE(OBJECT* o, int index, float Luminosity)
    {
    {
        if (o->Type == MODEL_DARK_SCREAM_FIRE)
        {
            o->Scale -= (0.14f) * FPS_ANIMATION_FACTOR;
        }
        if (o->Type == MODEL_DARK_SCREAM)
        {
            o->Scale -= (0.04f) * FPS_ANIMATION_FACTOR;
        }
        if (o->Scale < 0.1f)
        {
            o->Scale = 0.f;
        }

        o->Position[2] = RequestTerrainHeight(o->Position[0], o->Position[1]) + 3.f;

        CreateParticleFpsChecked(BITMAP_FLAME, o->Position, o->Angle, o->Light, 8, (o->Scale - 0.4f) * 3.5f);
        if (o->Type == MODEL_DARK_SCREAM)
        {
            CheckClientArrow(o);
        }
    }
        return true;
    }
```
