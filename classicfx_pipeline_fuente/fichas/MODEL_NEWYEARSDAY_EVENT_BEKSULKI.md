# MODEL_NEWYEARSDAY_EVENT_BEKSULKI

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_NEWYEARSDAY_EVENT_BEKSULKI:
            case MODEL_NEWYEARSDAY_EVENT_CANDY:
            case MODEL_NEWYEARSDAY_EVENT_MONEY:
            case MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_GREEN:
            case MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_RED:
            case MODEL_NEWYEARSDAY_EVENT_YUT:
            case MODEL_NEWYEARSDAY_EVENT_PIG:
            {
                if (o->Type == MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_GREEN)
                {
                    if (rand_fps_check(2))
                        o->Type = MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_RED;
                }
                else if (o->Type == MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_RED)
                {
                    if (rand_fps_check(2))
                        o->Type = MODEL_NEWYEARSDAY_EVENT_HOTPEPPER_GREEN;
                }

                o->LifeTime = rand() % 10 + 50;
                o->Scale = 1.6f + (rand() % 10 - 5) * 0.02f;

                if (o->Type == MODEL_NEWYEARSDAY_EVENT_BEKSULKI)
                {
                    o->Scale = 2.5f + (rand() % 10 - 5) * 0.02f;
                }
                if (o->Type == MODEL_NEWYEARSDAY_EVENT_CANDY)
                {
                    o->Scale = 3.0f + (rand() % 10 - 5) * 0.02f;
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_NEWYEARSDAY_EVENT_BEKSULKI(OBJECT* o, int index, float Luminosity)
    {
        float Height;
        o->Angle[o->m_iAnimation] += (10.f) * FPS_ANIMATION_FACTOR;
        o->Position[0] += ((o->Direction[0] * 1.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[1] += ((o->Direction[1] * 1.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[2] += ((o->Gravity * 1.5f)) * FPS_ANIMATION_FACTOR;
        o->Gravity -= (1.5f) * FPS_ANIMATION_FACTOR;

        Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
        if (o->Position[2] < Height)
        {
            o->Position[2] = Height;
            o->Gravity = -o->Gravity * 0.3f;
            o->LifeTime -= (2) * FPS_ANIMATION_FACTOR;
        }

        VectorAddScaled(o->Position, o->Direction, o->Position, FPS_ANIMATION_FACTOR);
        return true;
    }
```
