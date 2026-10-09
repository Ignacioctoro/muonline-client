# MODEL_MANA_RUNE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/ManaRune.bmd

## Funcion MoveHandlers

```cpp
bool Move_MODEL_MANA_RUNE(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            if (o->LifeTime > 43)
            {
                o->HiddenMesh = -2;
            }
            else if (o->LifeTime > 40)
            {
                o->HiddenMesh = 0;
            }
            else if (o->LifeTime > 30)
            {
                o->Scale += (o->Gravity) * FPS_ANIMATION_FACTOR;
                o->Gravity += (0.15f) * FPS_ANIMATION_FACTOR;
                if (o->Scale > 1.f)
                {
                    o->Scale = 1.f;
                    o->Gravity = 0.01f;

                    CreateEffectFpsChecked(MODEL_MANA_RUNE, o->Position, o->Angle, o->Light, 1);
                }
            }
            else if (o->LifeTime < 15)
            {
                o->Scale -= (o->Gravity) * FPS_ANIMATION_FACTOR;
                o->Gravity += (0.2f) * FPS_ANIMATION_FACTOR;
                if (o->Scale < 0.f)
                {
                    o->Scale = 0.f;
                }
                o->Alpha = (o->LifeTime / 20.f);
                o->Position[2] -= (10.f) * FPS_ANIMATION_FACTOR;
            }
        }
        else if (o->SubType == 1)
        {
            o->Scale -= (0.02f) * FPS_ANIMATION_FACTOR;
            if (o->Scale < 1.f)
            {
                o->Scale = 1.f;
            }
            o->Alpha = (o->LifeTime / 20.f);
        }
        return true;
    }
```
