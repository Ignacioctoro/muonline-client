# MODEL_ARROW_GAMBLE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Skill/gamble_arrows01.bmd

## Fragmento de CreateEffect

```cpp
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
            }
            break;
            case MODEL_WOOSISTONE:
                if (Range <= 30.f)
                {
                    for (int j = 0; j < 20; j++)
                    {
```
