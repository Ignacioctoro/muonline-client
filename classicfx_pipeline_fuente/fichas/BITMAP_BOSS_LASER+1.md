# BITMAP_BOSS_LASER+1

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_BOSS_LASER + 1:
            case BITMAP_BOSS_LASER + 2:
                o->LifeTime = 20;
                switch (Type)
                {
                case BITMAP_BOSS_LASER:
                    Vector(0.5f, 0.7f, 1.f, o->Light);
                    Vector(0.f, -50.f, 0.f, p1);
                    o->Scale = 16.f;

                    if (SubType == 1)
                    {
                        Vector(0.f, -50.f, 0.f, p1);
                        o->Scale = 3.0f;
                        Vector(1.0f, 1.0f, 1.0f, o->Light);
                    }
                    else if (SubType == 2)
                    {
                        o->LifeTime = 35;
                        Vector(0.f, -50.f, 0.f, p1);
                        o->Scale = 2.5f;
                        Vector(1.0f, 1.0f, 1.0f, o->Light);
                    }
                    break;
                case BITMAP_BOSS_LASER + 1:
                    Vector(1.f, 0.4f, 0.2f, o->Light);
                    Vector(0.f, -50.f, 0.f, p1);
                    o->Scale = 16.f;
                    break;
                case BITMAP_BOSS_LASER + 2:
                    Vector(1.f, 0.4f, 0.2f, o->Light);
                    Vector(0.f, -15.f,
```
