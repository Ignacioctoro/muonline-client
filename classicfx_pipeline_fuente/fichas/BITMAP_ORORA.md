# BITMAP_ORORA

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_ORORA:
                switch (o->SubType)
                {
                case 0:
                case 1:	o->LifeTime = 100;	break;
                case 2:
                case 3:	o->LifeTime = 25;	break;
                }
                CreateParticle(o->Type, o->Position, o->Angle, o->Light, o->SubType, 1.0f, o->Owner);
                break;

            case BITMAP_GATHERING:
                o->LifeTime = 10;

                switch (o->SubType)
                {
                case 0:
                    Vector(0.f, -100.f, 0.f, p1);
                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate(p1, Matrix, p2);
                    VectorAddScaled(o->Position, p2, o->Position, FPS_ANIMATION_FACTOR);

                    o->Position[2] += (150.f) * FPS_ANIMATION_FACTOR;
                    break;
                case 1:
                case 2:
                    VectorCopy(Position, o->StartPosition);
                    o->LifeTime = 20;
                    break;
                case 3:
                    o->LifeTime = 10;
                    Vector(-10.f, 10.f, 0.f, p1);
                    AngleMatrix(o->Angle, Matrix);
                    VectorRotate
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_ORORA(OBJECT* o, int index, float Luminosity)
    {
        if (o->Owner == NULL || o->Owner->Live == false)
            o->Live = false;
        else
        {
            if (o->LifeTime <= 5)
            {
                o->Live = false;
                CreateEffectFpsChecked(o->Type, o->Position, o->Angle, o->Light, o->SubType, o->Owner);
            }
        }
        return true;
    }
```
