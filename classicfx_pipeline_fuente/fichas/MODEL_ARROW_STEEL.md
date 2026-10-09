# MODEL_ARROW_STEEL

Cliente: SIN MAPEO LOCAL
Llamadas directas: 2; hijas: 0
Create metadata: NO; Move handler: SI
Modelo AccessModel: Skill/ArrowSteel[index=1].bmd

## Fragmento de CreateEffect

```cpp
            case MODEL_ARROW_STEEL:
            case MODEL_ARROW_THUNDER:
            case MODEL_ARROW_LASER:
            case MODEL_ARROW_V:
            case MODEL_ARROW_SAW:
            case MODEL_ARROW_NATURE:
            case MODEL_ARROW_WING:
            case MODEL_ARROW_BOMB:
            case MODEL_LACEARROW:
            case MODEL_DARK_SCREAM:
            case MODEL_DARK_SCREAM_FIRE:
            case MODEL_ARROW_SPARK:
            case MODEL_ARROW_RING:
            case MODEL_ARROW_TANKER:
            case MODEL_ARROW_DARKSTINGER:
            case MODEL_ARROW_GAMBLE:
                break;
            case MODEL_FIRE:
                if (o->SubType == 1)
                {
                    for (int j = 0; j < 2; j++)
                        CreateEffectFpsChecked(MODEL_STONE1 + rand() % 2, o->Position, o->Angle, o->Light);
                }
                break;

            case BITMAP_ENERGY:
                CreateParticleFpsChecked(BITMAP_SPARK + 1, o->Position, o->Angle, Light, 1, 6.f);
                break;

            case MODEL_LIGHTNING_ORB:
            {
                CreateEffectFpsChecked(MODEL_LIGHTNING_ORB, o->Position, o->Angle, o->Light, 1);
            }
            break;
```

## Funcion MoveHandlers

```cpp
bool Move_MODEL_ARROW_STEEL(OBJECT* o, int index, float Luminosity)
    {
        vec3_t Light;
        Vector(1.f, 1.f, 1.f, Light);
        vec3_t Position;
        if (o->Type == MODEL_ARROW_NATURE)
        {
            if (o->SubType == 1) {
                Vector(0.1f, 0.4f, 0.1f, Light);
                CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 0.3f, Light, o, (int)WorldTime * 0.1f);
                CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 0.7f, Light, o, -(int)WorldTime * 0.1f);

                Vector(Luminosity * 0.2f, Luminosity * 0.8f, Luminosity * 0.2f, Light);
                AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);

                o->Angle[1] += (60.f) * FPS_ANIMATION_FACTOR;

                if (o->LifeTime > 25 && ((int)o->LifeTime % 2) == 0)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        Vector((float)(rand() % 32 - 16), (float)(rand() % 64 - 32), (float)(rand() % 32 - 16), Position);
                        VectorAdd(Position, o->Position, Position);

                        Vector(0.4f, 1.f, 0.2f, Light);
                        CreateParticleFpsChecked(BITMAP_FLARE, Position, o->Angle, Light, 5, 0.2f);
                    }
                }
            }
            else
            {
                CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 0.5f, o->Light, o, (int)WorldTime * 0.1f);
                CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 1.f, o->Light, o, -(int)WorldTime * 0.1f);
                for (int j = 0; j < 4; j++)
                {
                    Vector((float)(rand() % 32 - 16), (float)(rand() % 64 - 32), (float)(rand() % 32 - 16), Position);
                    VectorAdd(Position, o->Position, Position);
                    CreateParticleFpsChecked(BITMAP_FLOWER01 + rand() % 3, Position, o->Angle, o->Light);
                    //CreateParticle(BITMAP_BUBBLE,Position,o->Angle,o->Light);
                }
                o->Angle[1] += (30.f) * FPS_ANIMATION_FACTOR;

                Vector(Luminosity * 0.6f, Luminosity * 0.8f, Luminosity * 0.8f, Light);
                AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
            }
        }
        else if (o->Type == MODEL_LACEARROW)
        {
            o->Angle[1] += (60.f) * FPS_ANIMATION_FACTOR;

            if (((int)o->LifeTime % 2) == 0)
            {
                for (int j = 0; j < 3; j++)
                {
                    Vector((float)(rand() % 32 - 16), (float)(rand() % 64 - 32), (float)(rand() % 32 - 16), Position);
                    VectorAdd(Position, o->Position, Position);

                    Vector(0.4f, 0.2f, 1.f, Light);
                    CreateParticleFpsChecked(BITMAP_FLARE, Position, o->Angle, Light, 5, 0.2f);
                }
            }
            VectorCopy(o->Position, o->EyeLeft);
            CreateEffectFpsChecked(MODEL_WAVES, o->Position, o->Angle, o->Light, 3, NULL, 0);

            Vector(Luminosity * 0.6f, Luminosity * 0.2f, Luminosity * 0.8f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        }
        else if (o->Type == MODEL_ARROW_WING)
        {
            CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 0.5f, o->Light, o, (int)WorldTime * 0.1f);
            CreateSprite(BITMAP_LIGHTNING + 1, o->Position, 1.f, o->Light, o, -(int)WorldTime * 0.1f);
            for (int j = 0; j < 4; j++)
            {
                Vector((float)(rand() % 16 - 8), (float)(rand() % 16 - 8), (float)(rand() % 16 - 8), Position);
                VectorAdd(Position, o->Position, Position);
                CreateParticleFpsChecked(BITMAP_BUBBLE, Position, o->Angle, o->Light, 1);
            }
            CreateParticleFpsChecked(BITMAP_SMOKE, o->Position, o->Angle, o->Light, 0);

            Vector(Luminosity * 0.6f, Luminosity * 0.8f, Luminosity * 0.8f, Light);
            AddTerrainLight(o->Position[0], o->Position[1], Light, 2, PrimaryTerrainLight);
        }
        CheckClientArrow(o);
        return true;
    }
```
