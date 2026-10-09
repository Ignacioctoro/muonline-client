# MODEL_BLIZZARD

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/Blizzard.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_BLIZZARD:
                if (o->SubType == 0 || o->SubType == 2)
                {
                    o->BlendMesh = -2;
                    o->LifeTime = rand() % 15 + 15;
                    o->Gravity = -20.f;
                    o->Velocity = (float)(rand() % 360);
                    Vector(0.f, 0.f, 0.f, o->Light);

                    int rangeX, rangeY, rangeZ;

                    rangeX = 300; rangeY = 150; rangeZ = 700;
                    o->Scale = 0.5f;
                    o->Gravity -= (rand() % 30 + 10) * FPS_ANIMATION_FACTOR;

                    o->Position[0] = o->Position[0] + rand() % rangeX - rangeY;
                    o->Position[1] = o->Position[1] + rand() % rangeX - rangeY;
                    o->Position[2] = o->Position[2] + 600.f;
                    o->Position[0] += (100.f) * FPS_ANIMATION_FACTOR;

                    VectorCopy(o->Position, o->StartPosition);

                    if (o->SubType == 2)
                    {
                        CreateJoint(BITMAP_JOINT_THUNDER + 1, o->Position, o->Position, o->Angle, 4, o, 60.f + rand() % 10);
                        if (rand_fps_check(2))
                            CreateJoint(BITMAP_JOINT_THUNDER + 1,
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_BLIZZARD(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        float Height;
        if (o->SubType == 0)
        {
            o->Position[0] = o->StartPosition[0] + sinf((rand() % 1000) * 0.01f) * 10.f;
            o->Position[1] = o->StartPosition[1] + sinf((rand() % 1000) * 0.01f) * 10.f;
            o->Position[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity -= (rand() % 5) * FPS_ANIMATION_FACTOR;

            o->StartPosition[0] -= (10.f) * FPS_ANIMATION_FACTOR;

            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, Light, 0, 1.5f);
            if (rand_fps_check(2))
            {
                CreateParticleFpsChecked(BITMAP_ENERGY, o->Position, o->Angle, Light, 1, 0.5f);
            }
            else
            {
                CreateParticleFpsChecked(BITMAP_FIRE + 2, o->Position, o->Angle, Light, 7, o->Scale);
            }

            o->Light[0] += (0.1f) * FPS_ANIMATION_FACTOR;
            o->Light[1] = o->Light[0];
            o->Light[2] = o->Light[0];
            CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 4 + 4) * 0.2f, o->Light, o, (float)(rand() % 360));
            CreateSprite(BITMAP_LIGHT, o->Position, 1.f, o->Light, o, (float)(rand() % 360));

            Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
            if (o->Position[2] < Height)
            {
                Vector(0.24f, 0.28f, 0.8f, Light);
                VectorCopy(o->Position, Position);
                Position[2] += 50.f;
                CreateParticleFpsChecked(BITMAP_SMOKE, Position, o->Angle, Light, 11, (float)(rand() % 32 + 80) * 0.025f);

                if (rand_fps_check(5))
                {
                    CreateEffectFpsChecked(MODEL_ICE_SMALL, Position, o->Angle, o->Light);
                }

                CreateEffectFpsChecked(MODEL_BLIZZARD, Position, o->Angle, o->Light, 1, NULL, o->PKKey);

                o->Live = false;
            }
        }
        else if (o->SubType == 1)
        {
            o->BlendMeshLight *= pow(1.0f / (1.1f), FPS_ANIMATION_FACTOR);

            if ((int)o->LifeTime == 18 && o->PKKey == 1)
                PlayBuffer(SOUND_SUDDEN_ICE2);
        }
        else if (o->SubType == 2)
        {
            o->Position[0] = o->StartPosition[0] + sinf((rand() % 1000) * 0.01f) * 50.f;
            o->Position[1] = o->StartPosition[1] + sinf((rand() % 1000) * 0.01f) * 50.f;
            o->Position[2] += (o->Gravity) * FPS_ANIMATION_FACTOR;
            o->Gravity -= (rand() % 5) * FPS_ANIMATION_FACTOR;

            o->StartPosition[0] -= (10.f) * FPS_ANIMATION_FACTOR;

            Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
            if (o->Position[2] > Height)
            {
                if (rand_fps_check(2))
                {
                    CreateParticleFpsChecked(BITMAP_FIRE + 2, o->Position, o->Angle, Light, 7, o->Scale);
                }

                o->Light[0] += (0.1f) * FPS_ANIMATION_FACTOR;
                o->Light[1] = o->Light[0];
                o->Light[2] = o->Light[0];
                CreateSprite(BITMAP_SHINY + 1, o->Position, (float)(rand() % 4 + 4) * 0.2f, o->Light, o, (float)(rand() % 360));
                CreateSprite(BITMAP_LIGHT, o->Position, 1.f, o->Light, o, (float)(rand() % 360));
            }
        }
        return true;
    }
```
