# BITMAP_FLAME

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: no identificado

## Fragmento de CreateEffect

```cpp
            case BITMAP_FLAME:
                if (o->SubType == 0)
                {
                    o->LifeTime = 40;
                    o->Weapon = CharacterMachine->PacketSerial;
                }
                else if (o->SubType == 1 || o->SubType == 2)
                {
                    o->LifeTime = 10;
                    Vector(0.f, 0.f, 0.f, o->Angle);
                }
                else if (o->SubType == 3)
                {
                    o->LifeTime = 15;
                }
                else if (o->SubType == 4)
                {
                    o->Scale = 0.01f;
                    o->LifeTime = 10;
                    Vector(0.f, 0.f, 0.f, o->Angle);
                }
                else if (o->SubType == 5)
                {
                    o->LifeTime = 20;
                }
                else if (o->SubType == 6)
                {
                    o->LifeTime = 40;
                }
                break;
            case MODEL_RAKLION_BOSS_CRACKEFFECT:
                if (o->SubType == 0)
                {
                    o->LifeTime = 40;
                    o->Scale = Scale + 1.f;
                    o->Position[2] += (30.f) * FPS_ANIMATION_FACTOR;
```

## Funcion MoveHandlers

```cpp
bool Move_BITMAP_FLAME(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Angle;
        vec3_t Position;
        float Matrix[3][4];
        vec3_t p;
        if (o->SubType == 0)
        {
            for (int j = 0; j < 6; j++)
            {
                Vector((float)(rand() % 50 - 25), (float)(rand() % 50 - 25), 0.f, Position);
                VectorAdd(Position, o->Position, Position);
                CreateParticleFpsChecked(BITMAP_FLAME, Position, o->Angle, Light);
            }
            if (rand_fps_check(8))
            {
                CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
            }

            Vector(Luminosity * 1.f, Luminosity * 0.4f, Luminosity * 0.f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 3, PrimaryTerrainLight);
            if (o->Owner == &Hero->Object && (int)o->LifeTime % 20 == 0)
            {
                o->LifeTime = ((int)o->LifeTime / 20) * 19.9f;
                AttackCharacterRange(o->Skill, o->Position, 150.f, o->Weapon, o->PKKey);
            }
        }
        else if (o->SubType == 1 || o->SubType == 2)
        {
            for (int j = 0; j < 18; j++)
            {
                if (rand_fps_check(1))
                {
                    Vector(0.f, 250.f, 0.f, p);
                    Vector(0.f, 0.f, j * 20.f, Angle);
                    AngleMatrix(Angle, Matrix);
                    VectorRotate(p, Matrix, Position);
                    VectorAdd(Position, o->Position, Position);
                    Position[0] += rand() % 64 - 32;
                    Position[1] += rand() % 64 - 32;
                    if (o->SubType == 1)
                        CreateParticleFpsChecked(BITMAP_FLAME, Position, o->Angle, Light, 0, 1.2f);
                    else if (o->SubType == 2)
                        CreateParticleFpsChecked(BITMAP_FIRE + 3, Position, o->Angle, Light, 13, 2.5f);
                }
            }
        }
        else if (o->SubType == 3)
        {
            for (int j = 0; j < 3; j++)
            {
                if (rand_fps_check(1))
                {
                    CreateParticleFpsChecked(BITMAP_FLAME, o->Position, o->Angle, Light, 6);
                    Vector((float)(rand() % 10 - 5), (float)(rand() % 10 - 5), 40.f, Position);
                    VectorAdd(Position, o->Position, Position);
                    CreateParticleFpsChecked(BITMAP_TRUE_FIRE, Position, o->Angle, Light, 0, 2.8f);
                }
            }
            Vector((float)(rand() % 10 - 5), (float)(rand() % 10 - 5), -40.f, Position);
            VectorAdd(Position, o->Position, Position);
            CreateParticleFpsChecked(BITMAP_SMOKE, Position, o->Angle, Light, 21, 0.8f);

            CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light, 12);

            Vector(Luminosity * 1.f, Luminosity * 0.4f, Luminosity * 0.f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 3, PrimaryTerrainLight);
        }
        else if (o->SubType == 4)
        {
            o->Scale += (20.f) * FPS_ANIMATION_FACTOR;
            for (int j = 0; j < 18; j++)
            {
                if (rand_fps_check(1))
                {
                    Vector(0.f, 150.f - o->Scale, 0.f, p);
                    Vector(0.f, 0.f, j * 20.f, Angle);
                    AngleMatrix(Angle, Matrix);
                    VectorRotate(p, Matrix, Position);
                    VectorAdd(Position, o->Position, Position);
                    Position[0] += rand() % 64 - 32;
                    Position[1] += rand() % 64 - 32;
                    CreateParticleFpsChecked(BITMAP_FLAME, Position, o->Angle, Light, 0, 1.2f);
                }
            }
            CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light, 12);
        }
        else if (o->SubType == 5)
        {
            Vector((float)(rand() % 32 - 16), (float)(rand() % 32 - 16), 0.f, Position);
            VectorAdd(Position, o->Position, Position);
            CreateParticleFpsChecked(BITMAP_FLAME, Position, o->Angle, Light, 0, o->Scale);
        }
        else if (o->SubType == 6)
        {
            Vector((float)(rand() % 32 - 16), (float)(rand() % 32 - 16), 0.f, Position);
            VectorAdd(Position, o->Position, Position);
            CreateParticleFpsChecked(BITMAP_FLAME, Position, o->Angle, o->Light, 12, o->Scale);
        }
        return true;
    }
```
