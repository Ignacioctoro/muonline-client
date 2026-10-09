# MODEL_FEATHER

Cliente: SIN MAPEO LOCAL
Llamadas directas: 4; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: Skill/darkwing_hetachi.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_FEATHER:
            {
                switch (o->SubType)
                {
                case 0:
                case 1:
                case 2:
                case 3:
                {
                    vec3_t vOriginPos;
                    VectorCopy(o->Position, vOriginPos);

                    o->Position[0] += ((float)(rand() % 20 - 10) * 4.f) * FPS_ANIMATION_FACTOR;
                    o->Position[1] += ((float)(rand() % 20 - 10) * 4.f) * FPS_ANIMATION_FACTOR;
                    o->Position[2] += ((float)(rand() % 20 - 10) * 4.f) * FPS_ANIMATION_FACTOR;

                    VectorSubtract(vOriginPos, o->Position, o->Direction);
                    VectorNormalize(o->Direction);
                    int iAddDirection = ((float)(rand() % 10 - 5) * 0.08f);
                    o->Direction[0] += (iAddDirection) * FPS_ANIMATION_FACTOR;
                    o->Direction[1] += (iAddDirection) * FPS_ANIMATION_FACTOR;
                    o->Direction[2] += (iAddDirection) * FPS_ANIMATION_FACTOR;

                    o->Scale = o->Scale + ((float)(rand() % 20 - 10) * (o->Scale * 0.03f));
                    o->LifeTime = 30 + (rand() % 20 - 10);
                    if (o->SubType == 2
```
