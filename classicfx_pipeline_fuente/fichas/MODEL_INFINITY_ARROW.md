# MODEL_INFINITY_ARROW

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/arrowsre[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_INFINITY_ARROW:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 40;
                    CreateEffect(MODEL_INFINITY_ARROW, Owner->Position, o->Angle, o->Light, 1, Owner);
                    CreateEffect(MODEL_INFINITY_ARROW1, Owner->Position, o->Angle, o->Light, 0, Owner);
                    CreateEffect(MODEL_INFINITY_ARROW2, Owner->Position, o->Angle, o->Light, 0, Owner);
                    CreateEffect(MODEL_INFINITY_ARROW3, Owner->Position, o->Angle, o->Light, 0, Owner);
                    CreateEffect(MODEL_INFINITY_ARROW4, Owner->Position, o->Angle, o->Light, 0, Owner);
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 40;
                    o->Scale = 1.0f;
                    Vector(1.f, 1.f, 1.f, o->Light);
                    VectorCopy(o->Light, o->Direction);
                }
            }
            break;


            case MODEL_SHIELD_CRASH:
            {
                o->LifeTime = 24;
                o->Scale = 1.1f;
                o->Gravity = o->Velocity;
                Vector(0.5f, 0.5f, 1.f, o->Light);
                VectorCopy(o->Light, o->Directi
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_INFINITY_ARROW(OBJECT* o, int index, float Luminosity)
    {
    {
        vec3_t tmp = { 0.f, 0.f, 0.f };
        OBJECT* pOwner = o->Owner;
        if (o->SubType == 1)
        {
            o->Light[0] *= pow(0.95f, FPS_ANIMATION_FACTOR);
            o->Light[1] *= pow(0.95f, FPS_ANIMATION_FACTOR);
            o->Light[2] *= pow(0.95f, FPS_ANIMATION_FACTOR);
        }
        VectorTransform(tmp, pOwner->BoneTransform[29], o->Position);
        VectorScale(o->Position, pOwner->Scale, o->Position);
        VectorAddScaled(o->Position, pOwner->Position, o->Position, FPS_ANIMATION_FACTOR);
        VectorCopy(pOwner->Angle, o->Angle);
    }
        return true;
    }
```
