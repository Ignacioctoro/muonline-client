# BITMAP_MAGIC_ZIN

Cliente: MAPEADO - NO IMPLICA FIDELIDAD
Llamadas directas: 10; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_MAGIC_ZIN:
                {
                    vec3_t vLight;
                    switch (o->SubType)
                    {
                    case 0:
                        Vector(o->Light[0] * o->Alpha * 2.f, o->Light[1] * o->Alpha * 2.f, o->Light[2] * o->Alpha * 2.f, vLight);
                        break;
                    case 1:
                        Vector(o->Light[0] * o->Alpha / 2.5f, o->Light[1] * o->Alpha / 2.5f, o->Light[2] * o->Alpha / 2.5f, vLight);
                        break;
                    case 2:
                        Vector(o->Light[0] * o->Alpha, o->Light[1] * o->Alpha, o->Light[2] * o->Alpha, vLight);
                        break;
                    }
                    RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, vLight, o->HeadAngle[1]);
                }
                break;

#ifdef ASG_ADD_INFLUENCE_GROUND_EFFECT
                case BITMAP_OUR_INFLUENCE_GROUND:
                    if (o->SubType == 0)
                    {
                        vec3_t vLight;

                        Vector(0.6f * o->Alpha, 0.9f * o->Alpha, 1.0f * o->Alpha, vLight);
                        RenderTerrainAlphaBitmap(o->T
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_MAGIC_ZIN(OBJECT* o, int index, float Luminosity)
    {
        switch (o->SubType)
        {
        case 0:
            if (o->LifeTime < 20)
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
            else if (o->Alpha < 1.0f)
                o->Alpha += (0.05f) * FPS_ANIMATION_FACTOR;
            break;
        case 1:
            if (o->LifeTime < 20)
                o->Alpha -= (0.03f) * FPS_ANIMATION_FACTOR;
            else if (o->Alpha < 0.7f)
                o->Alpha += (0.06f) * FPS_ANIMATION_FACTOR;
            break;
        case 2:
            if (o->Scale < 3.5f)
                o->Scale += (0.1f) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime < 20)
                o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
            else if (o->Alpha < 1.0f)
                o->Alpha += (0.05f) * FPS_ANIMATION_FACTOR;
            break;
        }
        return true;
    }
```
