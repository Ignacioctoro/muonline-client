# MODEL_MOONHARVEST_MOON

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Effect/chysukmoon.bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_MOONHARVEST_MOON:
                {
                    if (o->SubType == 0)
                    {
                        vec3_t vLight;
                        // (Shockwave)
                        Vector(0.6f, 0.8f, 0.6f, vLight);
                        o->m_iAnimation++;
                        CreateSprite(BITMAP_SHOCK_WAVE, o->Position, 0.8f, vLight, o, -(o->m_iAnimation * 3.f));
                        // Flare1
                        Vector(0.8f, 0.6f, 0.f, vLight);
                        CreateSprite(BITMAP_LIGHT, o->Position, 5.0f, vLight, o, 0);
                        RenderObject(o);
                    }
                    else if (o->SubType == 1)
                    {
                        CreateSprite(BITMAP_LIGHT, o->Position, 5.0f, o->Light, o, 0);
                        CreateSprite(BITMAP_LIGHT, o->Position, 3.0f, o->Light, o, 0);
                        CreateSprite(BITMAP_LIGHT, o->Position, 3.0f, o->Light, o, 0);
                        CreateSprite(BITMAP_SHINY + 6, o->Position, 2.0f, o->Light, o, 0);

                        RenderObject(o);
                    }
                    else if (o->SubType == 2)
                    {
                        RenderObjec
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_MOONHARVEST_MOON(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            o->Angle[2] += (5.0f) * FPS_ANIMATION_FACTOR;
            if (o->LifeTime < 25)
            {
                o->Light[0] -= (0.05f) * FPS_ANIMATION_FACTOR;
                o->Light[1] -= (0.06f) * FPS_ANIMATION_FACTOR;
                o->Light[2] -= (0.05f) * FPS_ANIMATION_FACTOR;
            }
        }
        else if (o->SubType == 1)
        {
            VectorAddScaled(o->Position, o->Direction, o->Position, FPS_ANIMATION_FACTOR);
            CreateParticleFpsChecked(BITMAP_SMOKELINE1 + rand() % 3, o->Position, o->Angle, o->Light, 0, 1.0f);
            //CreateParticle(BITMAP_SMOKE, o->Position, o->Angle,o->Light, 23, 1.0f);
            //CreateParticle(BITMAP_SMOKE, o->Position, o->Angle,o->Light, 8, 1.0f);
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 11, 1.0f);
            if (rand_fps_check(2))
            {
                CreateParticleFpsChecked(BITMAP_WATERFALL_3, o->Position, o->Angle, o->Light, 3, 3.f);
            }
        }
        else if (o->SubType == 2)
        {
            o->Scale += (0.01f) * FPS_ANIMATION_FACTOR;
        }
        return true;
    }
```
