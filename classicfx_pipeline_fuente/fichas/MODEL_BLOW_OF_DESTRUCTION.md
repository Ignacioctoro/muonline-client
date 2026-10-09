# MODEL_BLOW_OF_DESTRUCTION

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 1
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case MODEL_BLOW_OF_DESTRUCTION:
            {
                if (o->SubType == 0)
                {
                    o->LifeTime = 40;
                    vec3_t vPos, vPos2;
                    float Matrix[3][4];
                    Vector(-20.f, -100.f, 0.f, vPos);
                    AngleMatrix(o->Owner->Angle, Matrix);
                    VectorRotate(vPos, Matrix, vPos2);
                    VectorAdd(vPos2, o->Position, o->Position);
                    VectorCopy(o->Light, o->StartPosition);
                    Vector(1.2f, 1.2f, 1.2f, o->Light);
                    CreateEffect(MODEL_BLOW_OF_DESTRUCTION, o->StartPosition, o->Angle, o->Light, 1, o->Owner);
                }
                else if (o->SubType == 1)
                {
                    o->LifeTime = 40;
                    Vector(1.2f, 1.2f, 1.2f, o->Light);
                    o->Position[2] = 150.f;
                    o->Scale = 5.f;
                }
                else if (o->SubType == 2)
                {
                    o->LifeTime = 50;
                    vec3_t vPos, vPos2;
                    float Matrix[3][4];
                    Vector(0.f, 0.f, 0.f, vPos);
                    AngleMatrix(o->Owner->Angle, Matrix)
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_BLOW_OF_DESTRUCTION(OBJECT* o, int index, float Luminosity)
    {
        float Matrix[3][4];
    {
        if (o->SubType == 0)
        {
            vec3_t vLight;
            if (o->LifeTime <= 24)
            {
                if (o->LifeTime >= 15.f)
                {
                    EarthQuake = (float)(rand() % 8 - 4) * 0.1f;
                }
                else
                {
                    EarthQuake = 0.f;
                }

                if ((int)o->LifeTime == 23)
                {
                    Vector(0.3f, 0.3f, 1.0f, vLight);
                    CreateEffectFpsChecked(MODEL_NIGHTWATER_01, o->Position, o->Angle, vLight, 0, o);
                    CreateEffectFpsChecked(MODEL_NIGHTWATER_01, o->Position, o->Angle, vLight, 0, o);
                    CreateEffectFpsChecked(MODEL_KNIGHT_PLANCRACK_A, o->Position, o->Angle, vLight, 0, o, 0, 0, 0, 0, 1.2f);

                    vec3_t vDir, vPos, vAngle;
                    VectorSubtract(o->StartPosition, o->Position, vDir);
                    float fLength = VectorLength(vDir);
                    VectorNormalize(vDir);
                    int iNum = (int)(fLength / 100) + 1;
                    for (int i = 0; i < iNum; ++i)
                    {
                        VectorScale(vDir, 55.f * i, vPos);
                        VectorAdd(o->Position, vPos, vPos);
                        VectorCopy(o->Owner->Angle, vAngle);
                        if (i % 2 == 0)
                        {
                            vAngle[2] += (rand() % 20 + 10);
                        }
                        else
                        {
                            vAngle[2] -= (rand() % 20 + 10);
                        }
                        CreateEffectFpsChecked(MODEL_KNIGHT_PLANCRACK_B, vPos, vAngle, vLight, 0, o, 0, 0, 0, 0, 1.0f);
                    }
                }

                o->Light[0] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
            }
        }
        else if (o->SubType == 1)
        {
            if (o->LifeTime <= 24)
            {
                if (o->LifeTime >= 15)
                {
                    vec3_t vPos, vLight;
                    for (int i = 0; i < 15; ++i)
                    {
                        VectorCopy(o->Position, vPos);
                        vPos[0] += rand() % 300 - 150;
                        vPos[1] += rand() % 300 - 150;
                        vPos[2] += rand() % 300 - 150;
                        float fScale = 1.6f + rand() % 10 * 0.1f;
                        Vector(0.5f, 0.5f, 1.0f, vLight);
                        int index = (rand_fps_check(2)) ? BITMAP_WATERFALL_5 : BITMAP_WATERFALL_3;
                        CreateParticleFpsChecked(index, vPos, o->Angle, vLight, 8, fScale);
                        Vector(1.0f, 1.0f, 1.0f, vLight);
                        if (rand_fps_check(2))
                        {
                            CreateParticleFpsChecked(BITMAP_SMOKE, vPos, o->Angle, vLight, 55, 1.f);
                        }
                    }
                }

                if ((int)o->LifeTime == 23)
                {
                    vec3_t vLight;
                    Vector(0.5f, 0.5f, 1.f, vLight);

                    Vector(0.3f, 0.3f, 1.0f, vLight);
                    CreateEffectFpsChecked(MODEL_NIGHTWATER_01, o->Position, o->Angle, vLight, 0, o, -1, 0, 0, 0, 2.f);
                    CreateEffectFpsChecked(MODEL_NIGHTWATER_01, o->Position, o->Angle, vLight, 0, o, -1, 0, 0, 0, 1.f);
                    CreateEffectFpsChecked(MODEL_RAKLION_BOSS_CRACKEFFECT, o->Position, o->Angle, vLight, 0, o, -1, 0, 0, 0, 0.2f);

                    vec3_t vPos, vResult;
                    vec3_t vAngle;
                    float Matrix[3][4];
                    for (int i = 0; i < (5 + rand() % 3); ++i)
                    {
                        Vector(0.f, (float)(rand() % 150), 0.f, vPos);
                        Vector(0.f, 0.f, (float)(rand() % 360), vAngle);
                        AngleMatrix(vAngle, Matrix);
                        VectorRotate(vPos, Matrix, vResult);
                        VectorAdd(vResult, o->Position, vResult);

                        CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, vResult, o->Angle, o->Light, 13);
                    }
                }

                o->Light[0] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
                o->Light[1] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
                o->Light[2] *= pow(1.0f / (1.05f), FPS_ANIMATION_FACTOR);
            }
        }
        else if (o->SubType == 2)
        {
            vec3_t vLight;
            if (o->LifeTime <= 35.0f && o->LifeTime >= 15.0f)
            {
                EarthQuake = (float)(rand() % 8 - 4) * 0.1f;
            }
            else
            {
                EarthQuake = 0.f;
            }

            if ((int)o->LifeTime == 30)
            {
                Vector(1.0f, 1.0f, 1.0f, vLight);
                vec3_t vDir, vPos, vAngle;
                VectorSubtract(o->StartPosition, o->Position, vDir);
                VectorLength(vDir);
                VectorNormalize(vDir);
                VectorScale(vDir, 55.f, vPos);
                VectorAdd(o->Position, vPos, vPos);
                VectorCopy(o->Owner->Angle, vAngle);
             
```
