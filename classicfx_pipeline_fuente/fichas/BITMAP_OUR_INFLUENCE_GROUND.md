# BITMAP_OUR_INFLUENCE_GROUND

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
                case BITMAP_OUR_INFLUENCE_GROUND:
                    if (o->SubType == 0)
                    {
                        vec3_t vLight;

                        Vector(0.6f * o->Alpha, 0.9f * o->Alpha, 1.0f * o->Alpha, vLight);
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], o->Scale, o->Scale, vLight, 45.f);

                        Vector(0.6f * o->AlphaTarget, 0.9f * o->AlphaTarget, 1.0f * o->AlphaTarget, vLight);
                        RenderTerrainAlphaBitmap(o->Type, o->Position[0], o->Position[1], 0.8f, 0.8f, vLight, 45.f);

                        Vector(0.2f * o->AlphaTarget, 0.8f * o->AlphaTarget, 1.0f * o->AlphaTarget, vLight);
                        RenderTerrainAlphaBitmap(BITMAP_LIGHT, o->Position[0], o->Position[1], 2.0f, 2.0f, vLight);
                        RenderTerrainAlphaBitmap(BITMAP_LIGHT, o->Position[0], o->Position[1], 2.0f, 2.0f, vLight);
                    }
                    break;

                case BITMAP_ENEMY_INFLUENCE_GROUND:
                    if (o->SubType == 0)
                    {
                        vec3_t vLight;

                        Vector(1.0f * o->Alpha, 0.3f * o->Alpha, 0.2f * o->Alpha, vLight);
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_OUR_INFLUENCE_GROUND(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            if (o->Owner == NULL)
            {
                o->Live = false;
                return true;
            }
            if (o->Owner->Live == false)
                o->Live = false;

            VectorCopy(o->Owner->Position, o->Position);

            o->Alpha -= (0.02f) * FPS_ANIMATION_FACTOR;
            o->Scale += (0.01f) * FPS_ANIMATION_FACTOR;

            if (o->Alpha < 0.f)
            {
                o->Alpha = 1.0f;
                o->Scale = 0.6f;
            }

            if (o->LifeTime < 25)
                o->AlphaTarget -= (0.02f) * FPS_ANIMATION_FACTOR;
            else if (o->AlphaTarget < 1.0f)
                o->AlphaTarget += (0.02f) * FPS_ANIMATION_FACTOR;

            if (1 <= o->LifeTime)
                o->LifeTime = 50;
        }
        return true;
    }
```
