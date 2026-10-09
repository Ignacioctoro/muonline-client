# MODEL_STAR_SHINE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_STAR_SHINE:
            {
                switch (o->SubType)
                {
                case 0:
                {
                    o->LifeTime = 30;
                    o->Alpha = 0.2f;
                    o->Angle[0] = (float)(rand() % 360);
                }break;
                }
            }break;
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
```
