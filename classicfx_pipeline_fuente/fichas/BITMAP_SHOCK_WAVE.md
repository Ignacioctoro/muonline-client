# BITMAP_SHOCK_WAVE

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 0; hijas: 2
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_SHOCK_WAVE:
                if (o->SubType == 0)
                {
                    o->LifeTime = 30;
                    o->Scale = 20.f;
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 20;
                    o->Scale = (rand() % 10 + 10.f) / 10.f;
                }
                else if (o->SubType == 2)
                {
                    o->LifeTime = 20;
                    o->Scale = (rand() % 10 + 10.f) / 10.f;
                }
                else if (o->SubType == 3)
                {
                    o->LifeTime = 15;
                    o->Scale = 1.f;
                }
                else if (o->SubType == 4)
                {
                    o->LifeTime = 10;
                    o->Scale = (rand() % 6 + 6.f) / 10.f;
                }
                else if (o->SubType == 5)
                {
                    o->LifeTime = 20;
                    o->Scale = 9.f;
                }
                else if (o->SubType == 6)
                {
                    o->LifeTime = 50;
                    o->Scale = 9.f;
                }
                else if (o->SubType == 7)
                {
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_SHOCK_WAVE(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            o->Scale -= (1.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 1)
        {
            o->Scale += ((rand() % 5) / 10.f) * FPS_ANIMATION_FACTOR;

            o->Position[0] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
            o->Position[1] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 2)
        {
            o->Scale += ((rand() % 5) / 40.f) * FPS_ANIMATION_FACTOR;

            o->Position[0] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
            o->Position[1] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 3)
        {
            o->Scale += (2.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 4)
        {
            o->Scale += (0.3f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 5)
        {
            o->Scale -= (0.4f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 6)
        {
            if (((int)o->LifeTime % 8) == 0)
            {
                CreateEffectFpsChecked(BITMAP_SHOCK_WAVE, o->Position, o->Angle, o->Light, 5);
            }
            return true;
        }
        else if (o->SubType == 7)
        {
            o->Scale += (2.5f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 8)
        {
            o->Scale += (1.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 9)
        {
            o->Scale += (0.8f) * FPS_ANIMATION_FACTOR;
            VectorCopy(o->Owner->Position, o->Position);
        }
        else if (o->SubType == 10)
        {
            o->Scale -= (0.02f) * FPS_ANIMATION_FACTOR;
            VectorCopy(o->Owner->Position, o->Position);
        }
        else if (o->SubType == 11)
        {
            o->Scale += ((rand() % 5) / 10.f) * FPS_ANIMATION_FACTOR;

            o->Position[0] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
            o->Position[1] += (rand() % 8 - 4.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 12)
        {
            if (o->LifeTime > 4.0f)
                o->Scale += ((o->LifeTime - 4.0f) * 0.25f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 13)
        {
            o->Scale += (0.08f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 14)
        {
            VectorCopy(o->Owner->Position, o->Position);
            o->Scale -= (0.15f) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime >= 20)
            {
                o->Alpha += 0.1f * FPS_ANIMATION_FACTOR;
                o->PKKey += FPS_ANIMATION_FACTOR;
                o->Light[0] = o->EyeRight[0] * (o->PKKey * 0.1f);
                o->Light[1] = o->EyeRight[1] * (o->PKKey * 0.1f);
                o->Light[2] = o->EyeRight[2] * (o->PKKey * 0.1f);
            }
            else if (o->LifeTime <= 10)
            {
                o->PKKey -= FPS_ANIMATION_FACTOR;
                o->Alpha -= 0.1f * FPS_ANIMATION_FACTOR;
                o->Light[0] = o->EyeRight[0] * (o->PKKey * 0.1f);
                o->Light[1] = o->EyeRight[1] * (o->PKKey * 0.1f);
                o->Light[2] = o->EyeRight[2] * (o->PKKey * 0.1f);
            }
            return true;
        }
        if (o->Scale < 0)
        {
            o->Scale = 0;
        }
        if (o->SubType >= 0 && o->SubType <= 3)
        {
            if (o->LifeTime <= 20)
            {
                Luminosity = o->LifeTime / 20.f;
                Vector(Luminosity, Luminosity, Luminosity, o->Light);
            }
            else
            {
                Luminosity = (40 - o->LifeTime) / 20.f;
                Vector(Luminosity, Luminosity, Luminosity, o->Light);
            }
        }
        else
        {
            if (o->LifeTime < 6)
            {
                o->Light[0] *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(1.0f / (1.3f), FPS_ANIMATION_FACTOR);
            }
        }
        return true;
    }
```
