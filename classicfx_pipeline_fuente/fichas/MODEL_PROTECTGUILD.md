# MODEL_PROTECTGUILD

Cliente: SIN MAPEO LOCAL
Llamadas directas: 1; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/ProtectGuild.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_PROTECTGUILD:
            {
                o->Alpha = 0;
                o->Angle[2] = +45.0f;

                BMD* b = &Models[o->Owner->Type];
                vec3_t tempPosition, p;
                Vector(0.f, 0.f, 0.f, p);
                b->TransformPosition(o->Owner->BoneTransform[20], p, tempPosition, true);
                o->Position[0] = tempPosition[0];
                o->Position[1] = tempPosition[1];
                o->Position[2] = o->Owner->Position[2] + tempPosition[2] - o->Owner->Position[2] + 60;

                o->LifeTime = 130;
                o->Scale = 3.2f;
                Vector(1.0f, 1.0f, 1.0f, o->Light);
                VectorCopy(o->Position, o->StartPosition);
                for (int i = 0; i < 10; ++i)
                {
                    float fAngle = rand() % 360;
                    Vector(o->Position[0] + (rand() % 20 + 15) * sinf(fAngle),
                        o->Position[1] + (rand() % 20 + 15) * cosf(fAngle),
                        o->Position[2] /*- 25 + rand()%50*/, p);
                    CreateParticle(BITMAP_SPARK + 1, p, o->Angle, o->Light, 4, 0.6f, o);
                }
            }
            break;
            case MODEL_MOVE_TARGETPOSIT
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_PROTECTGUILD(OBJECT* o, int index, float Luminosity)
    {
        vec3_t p;
    {
        if (o->Owner == NULL/* || o->Owner->Alpha < 1.0f*/)
        {
            o->LifeTime = 0;
            return true;
        }
        if (o->Alpha >= 1.0f) o->Angle[2] += 5.0f;
        //				o->Alpha = sin((o->LifeTime/130.0f)*3.14f) - 0.1f;
        if (o->LifeTime > 120)
        {
            if (o->LifeTime < 125 && o->LifeTime > 120)
            {
                for (int i = 0; i < 5; ++i)
                    CreateParticleFpsChecked(BITMAP_SPARK, o->Position, o->Angle, o->Light, 6, 1.5f, o);
            }
        }
        else if (o->LifeTime > 95)
        {
            if (o->Alpha > 1.0f) o->Alpha = 1.0f;
            else o->Alpha += 0.4f;
        }
        else if (o->LifeTime < 50)
        {
            if (o->Alpha < 0) o->Alpha = 0;
            else o->Alpha -= 0.1f;
        }

        BMD* b = &Models[o->Owner->Type];
        vec3_t tempPosition;
        Vector(0.f, 0.f, 0.f, p);
        b->TransformPosition(o->Owner->BoneTransform[20], p, tempPosition, true);
        o->Position[0] = tempPosition[0] + (o->Owner->Position[0] - Hero->Object.Position[0]);
        o->Position[1] = tempPosition[1] + (o->Owner->Position[1] - Hero->Object.Position[1]);

        float fHeight = o->Owner->Position[2] + tempPosition[2] - o->Owner->Position[2] + 60;
        if (fHeight < o->Position[2])
            o->Position[2] += ((fHeight - o->Position[2]) * 0.1f) * FPS_ANIMATION_FACTOR;
        else
            o->Position[2] += ((fHeight - o->Position[2]) * 0.5f) * FPS_ANIMATION_FACTOR;

        if (o->LifeTime < 60 && o->LifeTime > 39)
        {
            vec3_t p;
            for (int i = 0; i < 1; ++i)
            {
                float fAngle = rand() % 360;
                Vector(o->Position[0] + (rand() % 26 - 13) * sinf(fAngle),
                    o->Position[1] + (rand() % 26 - 13) * cosf(fAngle),
                    //o->Position[2] + 45+(10-o->LifeTime)*1.5f+rand()%5, p);
                    o->Position[2] + 48 - o->LifeTime + (rand() % 5), p);
                CreateParticleFpsChecked(BITMAP_SPARK, p, o->Angle, o->Light, 5, 2.0f, o);
            }
        }
    }
        return true;
    }
```
