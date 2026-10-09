# BITMAP_ENERGY

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
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
                        CreateEffectFpsChecked(MODEL_WOOSISTONE, o->Position, o->Angle, o->Light, 1);
                        CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, o->Light, 0, 1, o);
                    }
                    PlayBuffer(SOUND_BREAK01);
                }
                else
                {
```
