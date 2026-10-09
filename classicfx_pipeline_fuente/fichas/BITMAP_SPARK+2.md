# BITMAP_SPARK+2

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: SI; Move handler: NO DETECTADO
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
    case BITMAP_SPARK + 2:
        if (o->Owner == NULL || o->Owner->Live == false)
            o->Live = false;
        else
        {
            o->LifeTime = 100;

            if (rand_fps_check(60))
                CreateParticleFpsChecked(o->Type, o->Position, o->Angle, o->Light, o->SubType, 0.5f, o->Owner);
        }
        break;

    case BITMAP_SPARK + 1:
    {
        vec3_t Position;
        VectorCopy(o->Position, Position);
        float Luminosity = o->LifeTime * 0.1f;
        Vector(Luminosity, Luminosity, Luminosity, Light);
        float Scale = 6.f;
        for (int j = 0; j < 18; j++)
        {
            if (rand_fps_check(1))
            {
                Position[2] += Scale * 4.f;
                if (j == 0)
                    CreateParticleFpsChecked(BITMAP_SPARK + 1, Position, o->Angle, Light, 1, Scale * 2.f);
                else
                    CreateParticleFpsChecked(BITMAP_SPARK + 1, Position, o->Angle, Light, 1, Scale);
            }
        }
        break;
    }
    case BITMAP_ENERGY:
        if (o->SubType == 0)
        {
            Luminosity = o->LifeTime * 0.2f;
            Vector(Luminosity, Luminosity, Luminosity, Light);
            CreateParticleFpsChecked(
```
