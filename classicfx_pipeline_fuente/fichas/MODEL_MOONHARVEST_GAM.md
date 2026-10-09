# MODEL_MOONHARVEST_GAM

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Effect/chusukgam.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_MOONHARVEST_GAM:
            case MODEL_MOONHARVEST_SONGPUEN1:
            case MODEL_MOONHARVEST_SONGPUEN2:
            {
                o->LifeTime = rand() % 10 + 50;
                if (o->Type == MODEL_MOONHARVEST_GAM)
                {
                    o->Scale = 0.5f + (rand() % 10 - 5) * 0.02f;
                }
                else
                {
                    o->Scale = 0.8f + (rand() % 10 - 5) * 0.02f;
                }
                o->Angle[0] = (float)(rand() % 360);
                o->Angle[1] = (float)(rand() % 360);
                o->Angle[2] = (float)(rand() % 360);
                AngleMatrix(o->Angle, Matrix);
                o->Gravity = (float)(rand() % 10 + 10);
                vec3_t p;
                Vector((float)(rand() % 10 - 5) * 0.1f, (float)(rand() % 60 - 30) * 0.1f, 0.0f, p);
                VectorScale(p, 1.2f, p);
                VectorRotate(p, Matrix, o->Direction);
            }
            break;
            case MODEL_BATTLE_GUARD2:
                if (o->SubType == 0)
                {
                    o->LifeTime = 20;
                    o->HiddenMesh = 3;
                    o->Velocity = 0.33f;
                    SetAction(o, 2);
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_MOONHARVEST_GAM(OBJECT* o, int index, float Luminosity)
    {
        float Height;
        o->Angle[0] += (10.f) * FPS_ANIMATION_FACTOR;
        o->Position[0] += ((o->Direction[0] * 2.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[1] += ((o->Direction[1] * 2.2f)) * FPS_ANIMATION_FACTOR;
        o->Position[2] += ((o->Gravity * 1.5f)) * FPS_ANIMATION_FACTOR;
        o->Gravity -= (1.5f) * FPS_ANIMATION_FACTOR;

        Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
        if (o->Position[2] < Height)
        {
            o->Position[2] = Height;
            o->Gravity = -o->Gravity * 0.3f;
            o->LifeTime -= (2) * FPS_ANIMATION_FACTOR;
        }
        return true;
    }
```
