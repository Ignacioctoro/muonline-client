# BITMAP_MAGIC

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 0; hijas: 2
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_MAGIC:
                o->LifeTime = 20;
                o->Scale = 0.5f;
                if (o->SubType == 0)
                {
                    o->LifeTime = 15;
                }
                else if (o->SubType == 8)
                {
                    o->LifeTime = 30;
                    o->Scale = 1.f;
                }
                else if (o->SubType == 9)
                {
                    o->LifeTime = 40;
                    o->Scale = 2.4f;
                    Vector(0, 0, 0, o->HeadAngle);
                }
                else if (o->SubType == 10)
                {
                    o->LifeTime = 44;
                    o->Scale = 12.f;
                    o->Alpha = 0.0f;
                }
                else if (o->SubType == 11)
                {
                    o->LifeTime = 24;
                    o->Scale = 0.8f;
                    Vector(0, 0, 0, o->HeadAngle);
                }
                else if (o->SubType == 12)
                {
                    o->LifeTime = 20;
                    o->Scale = Scale * 0.1f;
                }
                else if (o->SubType == 13 || o->SubType == 14)
                {
                    o->Lif
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_MAGIC(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            CreateEffectFpsChecked(BITMAP_MAGIC, o->Position, o->Angle, o->Light, 1);
            if (o->LifeTime > 5 && o->LifeTime < 10)
            {
                CreateParticleFpsChecked(BITMAP_FLARE, o->Position, o->Angle, o->Light, 0, 0.19f, o);
            }
        }
        else if (o->SubType == 2 || o->SubType == 3 || o->SubType == 7)
        {
            if (o->LifeTime > 5 && o->LifeTime < 10)
            {
                if (o->SubType == 3)
                    CreateParticleFpsChecked(BITMAP_FLARE, o->Position, o->Angle, o->Light, 10, 0.19f, o);
                else
                    CreateParticleFpsChecked(BITMAP_FLARE, o->Position, o->Angle, o->Light, 0, 0.19f, o);
            }
        }
        else if (o->SubType == 4)
        {
            CreateParticleFpsChecked(BITMAP_FLARE, o->Position, o->Angle, o->Light, 12, 0.19f, o);
        }
        else if (o->SubType == 8)
        {
            o->Scale += (1.8f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 9)
        {
            if (o->LifeTime < 20) o->Alpha -= 0.05f;
            else if (o->Alpha < 1.0f) o->Alpha += 0.05f;

            o->HeadAngle[0] += (4.0f) * FPS_ANIMATION_FACTOR;
            o->HeadAngle[1] -= (8.0f) * FPS_ANIMATION_FACTOR;
            o->HeadAngle[2] += (4.0f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 10)
        {
            if (o->LifeTime < 20) o->Alpha -= 0.03f;
            else if (o->Alpha < 1.0f) o->Alpha += 0.05f;
        }
        else if (o->SubType == 11)
        {
            o->HeadAngle[0] += (2.0f) * FPS_ANIMATION_FACTOR;
            o->HeadAngle[1] -= (2.0f) * FPS_ANIMATION_FACTOR;
            o->HeadAngle[2] += (2.0f) * FPS_ANIMATION_FACTOR;

            if (o->LifeTime <= 10)
            {
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
                o->Light[0] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
            }
        }
        else if (o->SubType == 12)
        {
            if (o->Alpha > 0.0f)
            {
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
            }
            else
            {
                o->Alpha = 0.0f;
            }

            o->Scale += (o->Alpha * 1.0f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 5.0f)
            {
                //o->Scale = 5.0f;
                o->Light[0] *= pow(0.5f, FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(0.5f, FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(0.5f, FPS_ANIMATION_FACTOR);
            }
        }
        else if (o->SubType == 13)
        {
            o->Scale *= pow(1.1f, FPS_ANIMATION_FACTOR);
            VectorScale(o->Light, 0.95f, o->Light);
            if (o->Scale > 8)
            {
                o->Live = false;
            }
            if (o->Scale > 4)
            {
                VectorScale(o->Light, 0.5f, o->Light);
            }
        }
        else if (o->SubType == 14)
        {
            o->Scale *= pow(1.1f, FPS_ANIMATION_FACTOR);
            VectorScale(o->Light, 0.95f, o->Light);
            if (o->Scale > 8)
            {
                o->Live = false;
            }
            if (o->Scale > 4)
            {
                VectorScale(o->Light, 0.5f, o->Light);
            }
        }
        return true;
    }
```
