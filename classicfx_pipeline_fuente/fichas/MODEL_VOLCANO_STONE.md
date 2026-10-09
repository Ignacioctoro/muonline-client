# MODEL_VOLCANO_STONE

Cliente: SIN MAPEO LOCAL
Llamadas directas: 0; hijas: 1
Create metadata: NO; Move handler: SI
Modelo AccessModel: Effect/volcano_stone.bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_VOLCANO_STONE:
            {
                o->Scale = Scale + (float)(rand() % 20 + 5) * 0.06f;
                VectorCopy(Light, o->Light);
                VectorCopy(Position, o->Position);
                Vector(0.0f, 0.0f, 0.0f, o->Direction);
                o->LifeTime = rand() % 10 + 30;
                o->Gravity = (float)(rand() % 2 + 2);
                o->Angle[2] = (float)(rand() % 360);
                AngleMatrix(o->Angle, Matrix);
                Vector(0.f, (float)(rand() % 128 + 64) * 0.1f, 0.f, p1);
                VectorRotate(p1, Matrix, o->HeadAngle);
                o->HeadAngle[2] += (25.0f) * FPS_ANIMATION_FACTOR;
            }
            break;
            }
            return;
        }
    }
}

void MoveParticle(OBJECT* o, int Turn)
{
    if (Turn)
    {
        float Matrix[3][4];
        vec3_t Angle;
        vec3_t Position;
        VectorCopy(o->Angle, Angle);
        AngleMatrix(Angle, Matrix);
        VectorRotate(o->Direction, Matrix, Position);
        VectorAddScaled(o->Position, Position, o->Position, FPS_ANIMATION_FACTOR);
    }
    else
    {
        VectorAddScaled(o->Position, o->Direction, o->Position, FPS_ANIMATION_FACTOR);
    }
}

void MoveParticle
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_VOLCANO_STONE(OBJECT* o, int index, float Luminosity)
    {
        float Height;
    {
        float Height;
        o->HeadAngle[2] -= (o->Gravity) * FPS_ANIMATION_FACTOR;

        o->Position[0] = o->Position[0] + o->HeadAngle[0];
        o->Position[1] = o->Position[1] + o->HeadAngle[1];
        o->Position[2] = o->Position[2] + o->HeadAngle[2];

        Height = RequestTerrainHeight(o->Position[0], o->Position[1]);
        o->Angle[0] += (0.3f * o->LifeTime) * FPS_ANIMATION_FACTOR;
        o->Angle[1] += (0.3f * o->LifeTime) * FPS_ANIMATION_FACTOR;

        if (o->Position[2] + o->Direction[2] <= Height)
        {
            o->Position[2] = Height;
            o->HeadAngle[0] *= pow(0.6f, FPS_ANIMATION_FACTOR);
            o->HeadAngle[1] *= pow(0.6f, FPS_ANIMATION_FACTOR);
            o->HeadAngle[2] += (1.0f * o->LifeTime) * FPS_ANIMATION_FACTOR;
            if (o->HeadAngle[2] < 0.5f)
                o->HeadAngle[2] = 0;

            o->Alpha -= (0.05f) * FPS_ANIMATION_FACTOR;
        }
        o->Scale -= (0.03f) * FPS_ANIMATION_FACTOR;

        vec3_t vLight;
        Vector(1.0f, 1.0f, 1.0f, vLight);
        float fScale = o->Scale * (rand() % 5 + 5) * 0.1f;
        switch (rand() % 3)
        {
        case 0:
            CreateParticleFpsChecked(BITMAP_FIRE_HIK1, o->Position, o->Angle, vLight, 0, fScale);
            break;
        case 1:
            CreateParticleFpsChecked(BITMAP_FIRE_CURSEDLICH, o->Position, o->Angle, vLight, 4, fScale);
            break;
        case 2:
            CreateParticleFpsChecked(BITMAP_FIRE_HIK3, o->Position, o->Angle, vLight, 0, fScale);
            break;
        }
    }
        return true;
    }
```
