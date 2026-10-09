# MODEL_ICE_SMALL

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Ice[index=2].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ICE_SMALL:
            case MODEL_METEO1:
            case MODEL_METEO2:
            case MODEL_EFFECT_SAPITRES_ATTACK_2:
                if (o->Type == MODEL_BIG_STONE1 || o->Type == MODEL_BIG_STONE2)
                {
                    Vector((float)(rand() % 128 - 64), (float)(rand() % 128 - 64), (float)(rand() % 180), p1);
                    VectorAddScaled(o->Position, p1, o->Position, FPS_ANIMATION_FACTOR);
                }

                if (Type == MODEL_ICE_SMALL)
                {
                    o->BlendMesh = 0;
                    o->BlendMeshLight = 0.3f;
                    Vector(0.f, (float)(rand() % 256 + 64) * 0.1f, 0.f, p1);
                    o->Position[2] += (50.f) * FPS_ANIMATION_FACTOR;
                    if (o->SubType == 13)
                    {
                        Vector(0.f, (float)(rand() % 256 + 64) * 0.2, 0.f, p1);
                        o->LifeTime = rand() % 16 + 32;
                        o->Scale = (float)(rand() % 4 + 15) * 0.05f;
                        o->Angle[2] = (float)(rand() % 360);
                        AngleMatrix(o->Angle, Matrix);
                        VectorRotate(p1, Matrix, o->Direction);
                        o->Gr
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ICE_SMALL(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Position;
        float Matrix[3][4];
        float Height;
        if (o->SubType == 0 || o->SubType == 10 || o->SubType == 12)
        {
            VectorAddScaled(o->Position, o->Direction, o->Position, FPS_ANIMATION_FACTOR);
        }
        else if (o->Type == MODEL_EFFECT_SAPITRES_ATTACK_2 && o->SubType == 14)
        {
            auto fMoveSpeed = (float)(rand() % 5 + 25);
            o->Position[0] -= ((o->Direction[0] * fMoveSpeed)) * FPS_ANIMATION_FACTOR;
            o->Position[1] -= ((o->Direction[1] * fMoveSpeed)) * FPS_ANIMATION_FACTOR;
            o->Position[2] -= ((o->Direction[2] * fMoveSpeed)) * FPS_ANIMATION_FACTOR;

            if (o->LifeTime <= 10)
            {
                o->BlendMeshLight -= (0.1f) * FPS_ANIMATION_FACTOR;
            }
        }
        else
        {
            AngleMatrix(o->Angle, Matrix);
            VectorRotate(o->Direction, Matrix, Position);
            VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);
        }
        VectorScale(o->Direction, 0.9f, o->Direction);
        o->Position[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
        if (o->SubType == 0 || o->SubType == 10 || o->SubType == 12 || o->SubType == 13)
        {
            o->Gravity -= (3.f) * FPS_ANIMATION_FACTOR;
            Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
            if (o->Position[2] < Height)
            {
                o->Position[2] = Height;
                o->Gravity = -o->Gravity * 0.5f;
                o->LifeTime -= (4) * FPS_ANIMATION_FACTOR;
                o->Angle[0] -= (o->Scale * 128.f) * FPS_ANIMATION_FACTOR;
            }
            else
                o->Angle[0] -= (o->Scale * 32.f) * FPS_ANIMATION_FACTOR;

            if ((o->SubType == 0 || o->SubType == 12 || o->SubType == 13) && rand_fps_check(10))
            {
                if (o->Type == MODEL_ICE_SMALL || o->Type == MODEL_METEO1 || o->Type == MODEL_METEO2)
                    CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light);
                else if (o->Type == MODEL_STONE1 || o->Type == MODEL_STONE2)
                {
                    CreateParticleFpsChecked(BITMAP_FIRE, o->Position, o->Angle, o->Light, 1 + rand() % 3);
                }
            }
        }
        else if (o->SubType == 1)
            o->Gravity += (0.5f) * FPS_ANIMATION_FACTOR;
        else if (o->Type != MODEL_EFFECT_SAPITRES_ATTACK_2)
        {
            o->Angle[2] += (20.f) * FPS_ANIMATION_FACTOR;
            o->Gravity += (0.5f) * FPS_ANIMATION_FACTOR;
        }
        return true;
    }
```
