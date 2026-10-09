# MODEL_LIGHTNING_ORB

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 1
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
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
                    o->LifeTime = 100;
                }
                break;
            }
        }
    }
}

void CreateEffectFpsChecked(int Type, vec3_t Position
```
