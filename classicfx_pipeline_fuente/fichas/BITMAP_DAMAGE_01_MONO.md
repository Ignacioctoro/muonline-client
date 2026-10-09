# BITMAP_DAMAGE_01_MONO

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 2
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_DAMAGE_01_MONO:
                    if (o->SubType == 0)
                    {
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light, -o->Angle[2]);
                    }
                    else if (o->SubType == 1)
                    {
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light, -o->Angle[2]);
                    }
                    break;

                case BITMAP_CRATER:
                    EnableAlphaTest();
                    RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->StartPosition[0], o->StartPosition[1], o->Light);
                    break;
                case MODEL_BLOW_OF_DESTRUCTION:
                    if (o->SubType == 0)
                    {
                        if (o->LifeTime <= 24)
                        {
                            RenderTerrainAlphaBitmap(BITMAP_FLARE_BLUE, o->Position[0], o->Position[1], 4.f, 4.f, o->Light, -o->Angle[2]);
                        }
                    }
                    else if (o->SubType == 1)
                    {
                        if (o->LifeTime <= 24)
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_DAMAGE_01_MONO(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            o->Scale += (5.f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 1)
        {
            o->Scale += (0.5f) * FPS_ANIMATION_FACTOR;
            if (o->Scale > 3.5f)
            {
                //o->Scale = 6.0f;

                o->Light[0] *= pow(0.5f, FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(0.5f, FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(0.5f, FPS_ANIMATION_FACTOR);
            }
        }
        return true;
    }
```
