# MODEL_INFINITY_ARROW1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: SI; Move handler: SI
Modelo AccessModel: Skill/arrowsre[index=2].bmd

## Fragmento de CreateEffect

```cpp
                case MODEL_INFINITY_ARROW1:
                case MODEL_INFINITY_ARROW2:
                case MODEL_INFINITY_ARROW3:
                case MODEL_INFINITY_ARROW4:
                    RenderObject(o);
                    break;

                case MODEL_ARROW_BEST_CROSSBOW:
                    RenderObject(o);
                    break;

                case MODEL_ALICE_BUFFSKILL_EFFECT:
                case MODEL_ALICE_BUFFSKILL_EFFECT2:
                {
                    if (o->SubType == 0 || o->SubType == 1 || o->SubType == 2)
                    {
                        RenderObject(o);
                    }
                }
                break;
                case MODEL_RAKLION_BOSS_CRACKEFFECT:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_RAKLION_BOSS_MAGIC:
                {
                    RenderObject(o);
                }
                break;
                case MODEL_LAVAGIANT_FOOTPRINT_R:
                case MODEL_LAVAGIANT_FOOTPRINT_V:
                {
                    EnableAlphaBlend();
                    if (o->Type == MODEL_LAVAGIANT_FOOTPRINT_R)
                    {
                        Ren
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_INFINITY_ARROW1(OBJECT* o, int index, float Luminosity)
    {
    {
        if (o->SubType == 0)
        {
            o->Light[0] *= pow(0.95f, FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(0.95f, FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(0.95f, FPS_ANIMATION_FACTOR);
            VectorCopy(o->Owner->Position, o->Position);
        }
        else if (o->SubType == 1)
        {
            o->Light[0] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            if ((int)o->LifeTime == 40)
                CreateEffectFpsChecked(MODEL_INFINITY_ARROW3, o->Position, o->Angle, o->Light, 2, o);
            else
                if ((int)o->LifeTime == 20)
                    CreateEffectFpsChecked(MODEL_INFINITY_ARROW3, o->Position, o->Angle, o->Light, 3, o);
            VectorCopy(o->Owner->Position, o->Position);
        }
        else if (o->SubType == 2)
        {
            o->Light[0] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            VectorCopy(o->Owner->Position, o->Position);
        }
        else if (o->SubType == 3)
        {
            o->Light[0] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(0.98f, FPS_ANIMATION_FACTOR);
            VectorCopy(o->Owner->Position, o->Position);
        }
    }
        return true;
    }
```
