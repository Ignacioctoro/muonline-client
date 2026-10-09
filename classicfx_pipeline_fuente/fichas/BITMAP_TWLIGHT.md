# BITMAP_TWLIGHT

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_TWLIGHT:
                case BITMAP_SHOCK_WAVE:
                    if (o->Type == BITMAP_SHOCK_WAVE && gMapManager.InHellas() && o->SubType != 6)
                    {
                        DisableDepthMask();
                        RenderWaterTerrain(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light, -o->Angle[2]);
                        EnableDepthMask();
                    }
                    else
                    {
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light, -o->Angle[2]);
                    }
                    break;

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
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_TWLIGHT(OBJECT* o, int index, float Luminosity)
    {
    {
        if (o->SubType == 0)
        {
            o->Scale -= (0.1f) * FPS_ANIMATION_FACTOR;
            o->Angle[2] += (10.f) * FPS_ANIMATION_FACTOR;
        }
        else
            if (o->SubType == 1)
            {
                o->Scale -= (0.1f) * FPS_ANIMATION_FACTOR;
                o->Angle[2] += (5.f) * FPS_ANIMATION_FACTOR;
            }
            else
                if (o->SubType == 2)
                {
                    o->Scale -= (0.1f) * FPS_ANIMATION_FACTOR;
                    o->Angle[2] += (15.f) * FPS_ANIMATION_FACTOR;
                }
                else if (o->SubType == 3)
                {
                    VectorCopy(o->Owner->Position, o->Position);
                    o->Scale -= (0.15f) * FPS_ANIMATION_FACTOR;
                    o->Angle[2] += (10.f) * FPS_ANIMATION_FACTOR;
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
                }
    }
        return true;
    }
```
