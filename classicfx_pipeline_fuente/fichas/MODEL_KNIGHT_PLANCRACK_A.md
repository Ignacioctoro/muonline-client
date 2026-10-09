# MODEL_KNIGHT_PLANCRACK_A

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 2
Create metadata: NO; Move handler: SI
Modelo AccessModel: Effect/knight_plancrack_a.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_KNIGHT_PLANCRACK_A:
                if (o->SubType == 0)
                {
                    o->LifeTime = 25;
                    o->Alpha = 1.f;
                    o->Angle[2] = rand() % 360;
                    o->Scale = Scale + rand() % 10 * 0.05f;
                    o->Position[2] += (10.f) * FPS_ANIMATION_FACTOR;
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 20;
                    o->Alpha = 1.f;
                    o->Angle[2] = rand() % 360;
                    o->Scale = Scale + rand() % 10 * 0.05f;
                    o->Position[2] += (10.f) * FPS_ANIMATION_FACTOR;
                }
                break;
            case MODEL_EFFECT_FLAME_STRIKE:
                if (o->SubType == 0)
                {
                    o->Alpha = 0;
                    o->LifeTime = 35;
                    o->Velocity = Models[o->Owner->Type].Actions[PLAYER_SKILL_FLAMESTRIKE].PlaySpeed;
                    o->AI = 0;
                    o->m_iAnimation = rand();
                }
                break;
            case MODEL_STREAMOFICEBREATH:
            {
                const	float LENSCALAR = 50.0f;
                const	float R
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_KNIGHT_PLANCRACK_A(OBJECT* o, int index, float Luminosity)
    {
        if (o->SubType == 0)
        {
            o->Alpha -= (0.04f) * FPS_ANIMATION_FACTOR;
        }
        else if (o->SubType == 1)
        {
            //if (o->LifeTime < 10)
            {
                o->Alpha *= pow(0.9f, FPS_ANIMATION_FACTOR);
            }
        }
        return true;
    }
```
