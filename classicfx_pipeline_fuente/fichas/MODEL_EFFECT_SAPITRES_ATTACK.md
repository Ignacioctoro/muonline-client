# MODEL_EFFECT_SAPITRES_ATTACK

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_EFFECT_SAPITRES_ATTACK:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 20;

                    for (int i = 0; i < 10; i++)
                    {
                        CreateEffect(MODEL_EFFECT_SAPITRES_ATTACK_2, o->Position, o->Angle, o->Light, 14);
                    }
                }
            }
            break;
            case MODEL_EFFECT_SAPITRES_ATTACK_1:
            {
                if (o->SubType == 0)
                {
                    o->BlendMesh = 0;
                    o->BlendMeshLight = 1.0f;
                    o->Position[2] += (100.f) * FPS_ANIMATION_FACTOR;
                    o->LifeTime = 17;
                    o->Scale = 1.1f;
                    VectorSubtract(o->Owner->Position, o->Position, o->Direction);
                    VectorNormalize(o->Direction);
                    o->Angle[2] = CreateAngle2D(o->Position, o->Owner->Position);
                }
            }
            break;


            case MODEL_EFFECT_TRACE:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 50;
                    VectorCopy(o->Position, o->EyeLeft);
                    Cre
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_EFFECT_SAPITRES_ATTACK(OBJECT* o, int index, float Luminosity)
    {
    {
        if ((int)o->LifeTime % 6 == 0)
        {
            o->Position[0] += ((float)((rand() % 120) - 60)) * FPS_ANIMATION_FACTOR;
            o->Position[1] += ((float)((rand() % 120) - 60)) * FPS_ANIMATION_FACTOR;

            CreateEffectFpsChecked(MODEL_EFFECT_SAPITRES_ATTACK_1, o->Position, o->Angle, o->Light, 0, o->Owner);
        }
    }
        return true;
    }
```
