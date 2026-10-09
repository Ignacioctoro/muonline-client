# BITMAP_RING_OF_GRADATION

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
    case BITMAP_RING_OF_GRADATION:
    {
        if (o->SubType == 0)
        {
            if ((int)o->LifeTime == 20)
                break;

            o->Scale += (0.1f) * FPS_ANIMATION_FACTOR;

            o->Light[0] *= pow(1.0f / (1.1f), FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(1.0f / (1.1f), FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(1.0f / (1.1f), FPS_ANIMATION_FACTOR);

            o->Alpha *= pow(1.0f / (1.1f), FPS_ANIMATION_FACTOR);
        }
    }break;
    case MODEL_EFFECT_UMBRELLA_DIE:
    {
        if (o->SubType == 0)
        {
            if ((int)o->LifeTime == 28 || (int)o->LifeTime == 18 || (int)o->LifeTime == 8)
            {
                vec3_t vLight;
                Vector(1.0f, 0.2f, 0.5f, vLight);
                CreateEffectFpsChecked(BITMAP_RING_OF_GRADATION, o->Owner->Position, o->Angle, vLight, 0, o->Owner, 0, 0, 0, 0, 1.2f);
            }
        }
    }break;
    case MODEL_EFFECT_UMBRELLA_GOLD:
    {
        o->Angle[0] += (10.f) * FPS_ANIMATION_FACTOR;
        o->Position[0] += ((o->Direction[0] * 2.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[1] += ((o->Direction[1] * 2.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[2] += ((o->Gravity * 1.5f)) *
```
