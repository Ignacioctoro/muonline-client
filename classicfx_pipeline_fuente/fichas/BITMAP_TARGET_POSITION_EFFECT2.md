# BITMAP_TARGET_POSITION_EFFECT2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_TARGET_POSITION_EFFECT2:
                {
                    if (o->SubType == 0)
                    {
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
                }
                break;
                case BITMAP_RING_OF_GRADATION:
                {
                    if (o->SubType == 0)
                    {
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, o->Light);
                    }
                }break;
                }
            }
        }
    }
}

void CreateMyGensInfluenceGroundEffect()
{
    DeleteEffect(BITMAP_OUR_INFLUENCE_GROUND, &Hero->Object, 0);
    if (::IsStrifeMap(gMapManager.WorldActive))
    {
        vec3_t vTemp = { 0.f, 0.f, 0.f };
        CreateEffect(BITMAP_OUR_INFLUENCE_GROUND, Hero->Object.Position, vTemp, vTemp, 0, &Hero->Object);
    }
}
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_TARGET_POSITION_EFFECT2(OBJECT* o, int index, float Luminosity)
    {
    {
        if (o->SubType == 0)
        {
            if (o->Scale >= 1.8f)
            {
                o->m_iAnimation = 0;
            }
            else if (o->Scale <= 0.8f)
            {
                o->m_iAnimation = 1;
            }

            if (o->m_iAnimation == 0)
            {
                o->Scale -= (0.15f) * FPS_ANIMATION_FACTOR;
            }
            else if (o->m_iAnimation == 1)
            {
                o->Scale += (0.15f) * FPS_ANIMATION_FACTOR;
            }

            if (o->LifeTime <= 10)
            {
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
                o->Light[0] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(o->Alpha, FPS_ANIMATION_FACTOR);
            }
        }
    }
        return true;
    }
```
