# BITMAP_LIGHT

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_LIGHT:
                if (o->SubType == 0)
                {
                    //Vector( 0.5f, 0.5f, 1.0f, o->Light);
                    Vector(0.3f, 0.3f, 0.3f, o->Light);
                    float fAngle = 70.0f * Q_PI / 180.0f;
                    float fAngle2 = 30.0f;//( float)( rand() % 360);
                    //float fSpeed = ( float)( 18 + rand() % 10);
                    float fSpeed = (float)(9 + rand() % 5) * 0.5f;
                    o->Direction[0] = fSpeed * cosf(fAngle) * sinf(fAngle2 * Q_PI / 180.0f);
                    o->Direction[1] = fSpeed * cosf(fAngle) * cosf(fAngle2 * Q_PI / 180.0f);
                    o->Direction[2] = fSpeed * sinf(fAngle);
                    o->Scale = 3.f;
                    //o->LifeTime = 100;
                    o->LifeTime = 400;
                }
                else if (o->SubType == 1 || o->SubType == 2)
                {
                    o->LifeTime = 1000;
                    o->Velocity = 0.f;
                }
                else if (o->SubType == 3)
                {
                    o->LifeTime = 30;
                    o->Velocity = 0.f;
                }
                break;
            case BITMAP_FIRE + 1:
```
